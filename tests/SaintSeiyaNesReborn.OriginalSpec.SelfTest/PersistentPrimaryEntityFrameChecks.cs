using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class PersistentPrimaryEntityFrameChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckNmiAcceptanceRemainsDeferredUntilMainThreadProducer();
        CheckScheduledSpawnResets04ButPreservesProducerUntouchedState();
        CheckCommonSpawnResets04ButPreserves08AndAttachedHazard();
        CheckRemovalFeedsLaterProducerReuseWithoutManualRepair();
    }

    private static void CheckNmiAcceptanceRemainsDeferredUntilMainThreadProducer()
    {
        var stage = Stage(Page(0, raw: 0xA8, hp: 80));
        var state = PersistentState(
            active: Config(raw: 0x86, hp: 15),
            staged03B7: 0x86,
            slotA: FreeSlot(),
            slotB: FreeSlot(),
            cooldown: 0,
            lastTrigger: 0x10);

        var nmi = PlatformPersistentPrimaryEntityFrame.StepNmi(
            stage,
            cameraLow44: 0x3D,
            cameraHigh45: 0,
            visual07C0: 0xFE,
            state03A4: 0,
            state);

        Require(nmi.NmiRefresh.Acceptance?.Outcome == PlatformPrimaryEncounterAcceptanceOutcome.AcceptedNonzero,
            "NMI accepts the visible special encounter");
        Require(nmi.State.EncounterLatch.ActiveEngine58 == 0xA8
            && nmi.State.StagedDescriptor03B7 == 0xA8,
            "NMI state persists accepted $58/profile and staged $03B7");
        Require(nmi.State.SlotA.VisualSpritePlus1 == 0xFE
            && nmi.State.SlotA.Entity.Motion.Type == 0,
            "NMI boundary does not run or synthesize a main-thread producer");

        var main = StepMain(
            stage,
            nmi.State,
            cameraLow44: 0x3D,
            cameraDelta43: 1,
            scheduledEntries: [new PlatformSpecialSpawnEntry(0x38, 0, 0x60)]);

        Require(main.Producer.ScheduledSpecial?.SlotA.Spawned == true,
            "later main-thread boundary consumes the encounter accepted by NMI");
        Require(main.Hybrid.SlotA?.Route == PlatformHybridEntitySlotRoute.Special08090C,
            "newly produced type08 reaches the same-frame hybrid entity scheduler on the non-exit path");
        Require(main.State.SlotA.Entity.Motion.Type == 0x08
            && main.State.SlotA.VisualSpritePlus1 == 0xFD,
            "producer and hybrid mutations persist in one slot state without reconstruction");
        Require(main.State.LastTriggerLow03A2 == 0x38,
            "scheduled trigger latch persists through the composed frame");
    }

    private static void CheckScheduledSpawnResets04ButPreservesProducerUntouchedState()
    {
        var stage = Stage(Page(0, raw: 0xA8, hp: 80));
        var hazard = OccupiedFarHazard();
        var slotA = FreeSlot() with
        {
            SpecialControl04 = 0x0B,
            ParentOffset08 = 0x66,
            AttachedHazard = hazard,
        };
        var state = PersistentState(
            active: Config(raw: 0xA8, hp: 80),
            staged03B7: 0xA8,
            slotA,
            slotB: FreeSlot(),
            cooldown: 0,
            lastTrigger: 0x10);

        var main = StepMain(
            stage,
            state,
            cameraLow44: 0x3D,
            cameraDelta43: 1,
            scheduledEntries: [new PlatformSpecialSpawnEntry(0x38, 0, 0x60)]);

        Require(main.Producer.ScheduledSpecial?.SlotA.Spawned == true,
            "scheduled producer replaces free slot A");
        Require(main.State.SlotA.SpecialControl04 == 0,
            "$8927 write map resets logical +$04 on spawn");
        Require(main.State.SlotA.ParentOffset08 == 0x66,
            "$8927 skips logical +$08, so the bridge preserves it");
        Require(main.State.SlotA.AttachedHazard == hazard,
            "$8927 does not own the separate attached-hazard record");
    }

    private static void CheckCommonSpawnResets04ButPreserves08AndAttachedHazard()
    {
        var stage = Stage(Page(0, raw: 0x86, hp: 15));
        var hazard = OccupiedFarHazard();
        var slotA = FreeSlot() with
        {
            SpecialControl04 = 7,
            ParentOffset08 = 0x5A,
            AttachedHazard = hazard,
        };
        var state = PersistentState(
            active: Config(raw: 0x86, hp: 15),
            staged03B7: 0x86,
            slotA,
            slotB: FreeSlot(),
            cooldown: 0,
            lastTrigger: 0);

        var main = StepMain(
            stage,
            state,
            cameraLow44: 0x02,
            cameraDelta43: 0,
            scheduledEntries: Array.Empty<PlatformSpecialSpawnEntry>());

        Require(main.Producer.CommonEdge?.SlotA.Spawned == true,
            "generic $B6D0 producer populates the free primary slot");
        Require(main.State.SlotA.Entity.Motion.Type == 0x06,
            "common type survives producer -> hybrid composition");
        Require(main.State.SlotA.SpecialControl04 == 0,
            "$B6D0 write map resets logical +$04 on spawn");
        Require(main.State.SlotA.ParentOffset08 == 0x5A,
            "$B6D0 skips logical +$08, so the bridge preserves it");
        Require(main.State.SlotA.AttachedHazard == hazard,
            "generic primary producer leaves the independent attached-hazard record untouched");
    }

    private static void CheckRemovalFeedsLaterProducerReuseWithoutManualRepair()
    {
        // Use a one-slot encounter ($06, bit 7 clear) so slot B cannot seed the
        // shared $03B8 cooldown while slot A is still occupied in frame 1.
        var stage = Stage(Page(0, raw: 0x06, hp: 15));
        var active = PlatformHybridEntitySlotState.Common(
            Entity(type: 0x06, action: 0x40, x: 0x00, y: 0x50),
            visualSpritePlus1: 0x31);
        var state = PersistentState(
            active: Config(raw: 0x06, hp: 15),
            staged03B7: 0x06,
            slotA: active,
            slotB: FreeSlot(),
            cooldown: 0,
            lastTrigger: 0);

        var frame1 = StepMain(
            stage,
            state,
            cameraLow44: 0x02,
            cameraDelta43: 1,
            scheduledEntries: Array.Empty<PlatformSpecialSpawnEntry>(),
            engineState00: 0x20);

        Require(frame1.Producer.CommonEdge?.SlotA.Outcome == PlatformCommonEdgeSpawnOutcome.ActionFamilyBlocked,
            "producer leaves active $40 cleanup slot alone before entity processing");
        Require(frame1.Hybrid.SlotA?.RemovalA647.HasValue == true,
            "hybrid runtime reaches confirmed $A647 retirement in frame 1");
        Require(frame1.State.SlotA.VisualSpritePlus1 == 0xFE
            && frame1.State.SlotA.Entity.Motion.ActionState == 0,
            "frame 1 persistent state is directly free after removal");

        var frame2 = StepMain(
            stage,
            frame1.State,
            cameraLow44: 0x02,
            cameraDelta43: 0,
            scheduledEntries: Array.Empty<PlatformSpecialSpawnEntry>(),
            engineState00: 0x20);

        Require(frame2.Producer.CommonEdge?.SlotA.Spawned == true,
            "frame 2 producer reuses frame 1 retirement output without manual visual repair");
        Require(frame2.State.SlotA.Entity.Motion.Type == 0x06
            && frame2.State.SlotA.VisualSpritePlus1 == 0xFD,
            "reused slot remains live after the following hybrid update");
    }

    private static PlatformPersistentPrimaryEntityMainThreadResult StepMain(
        PlatformStageMap stage,
        PlatformPersistentPrimaryEntityFrameState state,
        byte cameraLow44,
        byte cameraDelta43,
        IReadOnlyList<PlatformSpecialSpawnEntry> scheduledEntries,
        byte engineState00 = 0x20) =>
        PlatformPersistentPrimaryEntityFrame.StepMainThreadNonExit(
            stage,
            cameraLow44,
            cameraHigh45: 0,
            scrollX: 0,
            playerState: BasePlayer(),
            input: PlatformInput.None,
            resources: new PlatformFrameResources(99, 50),
            contactState: new PlatformContactPhaseState(0, default),
            state,
            scheduledEntries,
            PlatformHitboxParameters.Common,
            PlatformHitboxParameters.Common,
            entropy48: 0,
            cameraDelta43,
            engineSubstate02: 1,
            engineState00,
            alternateParent08_03AB: 0x77);

    private static PlatformPersistentPrimaryEntityFrameState PersistentState(
        PlatformPrimaryEncounterSpawnConfig active,
        byte staged03B7,
        PlatformHybridEntitySlotState slotA,
        PlatformHybridEntitySlotState slotB,
        byte cooldown,
        byte lastTrigger) =>
        new(
            new PlatformPrimaryEncounterLatchState(active.Engine58, active),
            staged03B7,
            slotA,
            slotB,
            cooldown,
            lastTrigger,
            SeventhSense: 0,
            GlobalCounter039A: 0,
            FrameCounter3C: 1);

    private static PlatformHybridEntitySlotState FreeSlot() =>
        PlatformHybridEntitySlotState.Common(
            Entity(type: 0, action: 0, x: 0, y: 0),
            visualSpritePlus1: 0xFE);

    private static PlatformEntityAttachedHazardState OccupiedFarHazard() =>
        new(
            Raw2C: 0x00,
            Raw2D: 0x99,
            Raw2E: 0x12,
            Raw2F: 0x00,
            Raw30: 0xF0,
            Raw31: 0xFE,
            Raw32: 0x34,
            Raw33: 0x56);

    private static PlatformCommonEntityRuntimeState Entity(
        byte type,
        byte action,
        byte x,
        byte y) =>
        new(
            new PlatformCommonEntityMotionState(
                ActionState: action,
                X: x,
                Y: y,
                StatePhase: 0,
                GroundDescriptor: 0xA8,
                DecisionTimer: 0,
                FlagsFacing: 0x40,
                Type: type,
                TerrainProbeRight: 0,
                TerrainProbeLeft: 0),
            HitPoints: 30,
            LifeDrainTicks: 1,
            CosmoDrainTicks: 1,
            SeventhSenseRewardBcd: 0x10);

    private static PlatformPlayerActionState BasePlayer() =>
        new(
            PlatformSaintIndex.Seiya,
            new PlatformHorizontalState(0x40, 0, 0, 0x40),
            0x40,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            PlatformAttackState.Empty);

    private static PlatformPrimaryEncounterSpawnConfig Config(byte raw, int hp) =>
        PlatformPrimaryEncounterSpawnConfig.FromEncounter(Encounter(raw, hp));

    private static PlatformStagePage Page(int index, byte raw, int hp) =>
        new(index, GroundDescriptors(), Encounter(raw, hp));

    private static PlatformPrimaryEncounter Encounter(byte raw, int hp)
    {
        var type = (byte)(raw & 0x0F);
        var tier = (byte)((raw >> 4) & 0x03);
        return new(
            Raw: raw,
            TypeId: type,
            Tier: tier,
            SecondCommonSlotEnabled: (raw & 0x80) != 0,
            Bit6Unknown: (raw & 0x40) != 0,
            Stats: new PlatformEncounterStats(hp, 1, 1, 4));
    }

    private static PlatformStageMap Stage(params PlatformStagePage[] pages) => new(0, pages);

    private static byte[] GroundDescriptors()
    {
        var descriptors = new byte[PlatformStagePage.DescriptorCount];
        descriptors[4 * PlatformStagePage.Columns] = 0xA8;
        descriptors[4 * PlatformStagePage.Columns + 15] = 0xA8;
        return descriptors;
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Persistent primary entity frame self-test failed: {label}");
    }
}

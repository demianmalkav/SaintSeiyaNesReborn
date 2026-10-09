using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class SpecialEntityActive0FChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckGenericSpawnerFeedsDedicatedRoute();
        CheckSquareProjectileGeometryAndSameFrameReaction();
        CheckDirectPathOmitsAa70AndControl04Cadence();
        CheckDeathAdvancesEveryUpdateAndRetiresSlot();
        CheckLate70UsesSharedTypeAwareHelper();
    }

    private static void CheckGenericSpawnerFeedsDedicatedRoute()
    {
        var stage = Stage(Page(0, raw: 0x0F, hp: 30));
        var state = PersistentState(Config(0x0F, 30));

        var frame = PlatformPersistentPrimaryEntityFrame.StepMainThreadNonExit(
            stage,
            cameraLow44: 0x02,
            cameraHigh45: 0,
            scrollX: 0,
            playerState: Player(PlatformSaintIndex.Seiya, x: 0x40, y: 0x20),
            input: PlatformInput.None,
            resources: new PlatformFrameResources(99, 50),
            contactState: new PlatformContactPhaseState(0, default),
            state,
            scheduledEntries: Array.Empty<PlatformSpecialSpawnEntry>(),
            PlatformHitboxParameters.Common,
            PlatformHitboxParameters.Common,
            entropy48: 0x08,
            cameraDelta43: 0,
            engineSubstate02: 1,
            engineState00: 0x20,
            alternateParent08_03AB: 0x66);

        Require(frame.Producer.CommonEdge?.SlotA.Spawned == true,
            "generic $B6D0 producer creates type $0F");
        Require(frame.Hybrid.SlotA?.Route == PlatformHybridEntitySlotRoute.Special0F,
            "new type $0F routes through dedicated $A74C runtime");
        Require(frame.Hybrid.SlotA?.Special0F is not null
            && frame.Hybrid.SlotA?.Common is null
            && frame.Hybrid.SlotA?.Special is null
            && frame.Hybrid.SlotA?.Special0D0E is null,
            "type $0F never enters common or scheduled-special runtimes");
        Require(frame.State.SlotA.Entity.Motion.X == 0xB6
            && frame.State.SlotA.Entity.Motion.Y == 0x02,
            "$B6D0 right-side spawn at playerX+$78 immediately receives $A74C -2 X and +2 Y");
        Require(frame.State.SlotA.VisualSpritePlus1 == 0xFD,
            "same-frame type $0F remains visually occupied after a non-removing update");
    }

    private static void CheckSquareProjectileGeometryAndSameFrameReaction()
    {
        var attack = PlatformAttackState.Empty with
        {
            Slot0 = new PlatformAttackSlot(
                new PlatformAttackObject(
                    Y: 0x4F,
                    Type: 0x64,
                    Facing: 0x40,
                    X: 0x4D,
                    Field4: 0,
                    Field5: 0,
                    Field6: 0,
                    AuxiliaryX: 0),
                RangeCounter: 3),
        };
        var player = Player(
            PlatformSaintIndex.Hyoga,
            x: 0x10,
            y: 0x20,
            attack: attack);
        var slot = Slot(
            Entity(action: 0x10, x: 0x50, y: 0x50, flags: 0x40, hp: 30),
            visual: 0xFD);

        var frame = StepHybrid(slot, player, frame3C: 1);
        var special = frame.SlotA!.Special0F!;
        var hit = special.MainInteraction!.HitSequence.Results[0].Result;

        // After $A74C movement the entity is at $52/$52. Attack $4D/$4F is
        // inside 8/8/6/6 but exactly on the exclusive lower edge of the common
        // 8/8/5/5 box, so this fixture distinguishes the direct $0F setup.
        Require(hit.Outcome == PlatformProjectileHitOutcome.HpSurvived,
            "type $0F accepts projectile through square 8/8/6/6 geometry");
        Require(hit.StandardConsumptionApplied,
            "Hyoga standard hit consumes projectile on the ordinary type-$0F HP path");
        Require(frame.SlotA.State.Entity.HitPoints == 20,
            "type $0F receives shared platform damage");
        Require(special.Reaction40Advanced
            && frame.SlotA.State.Entity.Motion.ActionState == 0x41,
            "hit-created $40 advances to $41 in the same update at $A79E");
    }

    private static void CheckDirectPathOmitsAa70AndControl04Cadence()
    {
        var hazard = PlatformEntityAttachedHazardState.Empty with
        {
            Raw2C = 0x52,
            Raw2D = 0x90,
            Raw2F = 0x52,
        };
        var slot = new PlatformHybridEntitySlotState(
            Entity(action: 0x10, x: 0x50, y: 0x50, flags: 0x40, hp: 30, life: 2, cosmo: 3),
            VisualSpritePlus1: 0xFD,
            SpecialControl04: 0x0B,
            AttachedHazard: hazard,
            ParentOffset08: 0x55);
        var player = Player(PlatformSaintIndex.Seiya, x: 0x52, y: 0x52);

        var frame = StepHybrid(slot, player, frame3C: 1);
        var special = frame.SlotA!.Special0F!;

        Require(special.MainInteraction!.ContactPhase.Contact.Triggered,
            "type $0F executes direct $98BA contact after $A74C movement");
        Require(frame.ContactState.HazardLatch76 == 0x20
            && frame.ContactState.DrainState == new ContactDrainState(2, 3),
            "type $0F main contact seeds shared latch and drain profile");
        Require(frame.SlotA.State.AttachedHazard == hazard,
            "direct type-$0F route does not execute $AA70");
        Require(frame.SlotA.State.SpecialControl04 == 0x0B,
            "direct type-$0F route does not execute shared $A738 +$04 cadence");
        Require(frame.SlotA.State.ParentOffset08 == 0x55,
            "absence of $AA70/A908 on ordinary frame preserves parent +$08 state");
    }

    private static void CheckDeathAdvancesEveryUpdateAndRetiresSlot()
    {
        var slot = Slot(
            Entity(action: 0xDF, x: 0x50, y: 0x50, flags: 0x40, hp: 30),
            visual: 0xFD);
        var player = Player(PlatformSaintIndex.Seiya, x: 0x10, y: 0x20);

        var frame1 = StepHybrid(
            slot,
            player,
            frame3C: 1,
            engineState00: 0x20);

        Require(frame1.SlotA!.Special0F!.DeathD0Advanced,
            "type $0F advances $D0 family even when ($3C&3)!=0");
        Require(frame1.SlotA.Special0F.Outcome == PlatformSpecialEntityActive0FOutcome.RemovedByDeathCompletion,
            "$DF advances directly to terminal removal on this non-cadence frame");
        Require(frame1.SlotA.RemovalA647.HasValue,
            "terminal type $0F death composes exact $A647 retirement");
        Require(frame1.SlotA.State.Entity.Motion.ActionState == 0
            && frame1.SlotA.State.VisualSpritePlus1 == 0xFE,
            "engine state below $30 leaves terminal type $0F slot conventionally free");

        var frame2 = StepHybrid(
            frame1.SlotA.State,
            player,
            frame3C: frame1.FrameCounterAfter3C,
            engineState00: 0x20);

        Require(frame2.SlotA!.Route == PlatformHybridEntitySlotRoute.Skipped
            && frame2.SlotA.Activity.Outcome == PlatformCommonSlotActivityOutcome.VisualFreeInactive,
            "next frame consumes type $0F retirement output directly and skips free slot");
    }

    private static void CheckLate70UsesSharedTypeAwareHelper()
    {
        var slot = Slot(
            Entity(action: 0x77, x: 0x70, y: 0x40, flags: 0x40, hp: 30),
            visual: 0xFD);
        var player = Player(PlatformSaintIndex.Seiya, x: 0x10, y: 0x20);

        var midpoint = StepHybrid(slot, player, frame3C: 1);
        var mid = midpoint.SlotA!.Special0F!;
        Require(mid.LateAttack70.Advanced
            && mid.LateAttack70.State.ActionState == 0x78
            && mid.LateAttack70.CallsSecondarySpawnRoutine
            && mid.LateAttack70.SoundId == PlatformCommonEntityAttack70.MidpointSpawnSoundId,
            "type $0F shares odd-frame $77->$78 A908/sound-$2A request");
        Require(mid.LateSpawn?.Outcome == PlatformEntityAttachedHazardSpawnOutcome.NoTemplate,
            "type $0F A908 request resolves through its confirmed zero secondary-object template");

        var terminalSlot = Slot(
            Entity(action: 0x7F, x: 0x70, y: 0x40, flags: 0x40, hp: 30),
            visual: 0xFD);
        var terminal = StepHybrid(terminalSlot, player, frame3C: 1);
        Require(terminal.SlotA!.Special0F!.LateAttack70.CompletedFamily
            && terminal.SlotA.State.Entity.Motion.ActionState == 0x10,
            "type $0F terminal $70 returns to $10 through shared type-aware helper");
        Require(!terminal.SlotA.Special0F.LateAttack70.CallsSecondarySpawnRoutine,
            "type $0F terminal completion does not take $08/$09/$0C A908 branch");
    }

    private static PlatformHybridEntityCombatSliceResult StepHybrid(
        PlatformHybridEntitySlotState slotA,
        PlatformPlayerActionState player,
        byte frame3C,
        byte engineState00 = 0x20) =>
        PlatformHybridEntityCombatSlice.StepNonFatal(
            OpenStage(),
            player,
            PlatformInput.None,
            new PlatformFrameResources(99, 50),
            new PlatformContactPhaseState(player.Special76, default),
            slotA,
            FreeSlot(),
            PlatformHitboxParameters.Common,
            PlatformHitboxParameters.Common,
            seventhSense: 0,
            globalCounter039A: 0x44,
            frameCounter3C: frame3C,
            entropy48: 0,
            cameraDelta43: 0,
            engineSubstate02: 1,
            engineState00,
            alternateParent08_03AB: 0x66);

    private static PlatformPersistentPrimaryEntityFrameState PersistentState(
        PlatformPrimaryEncounterSpawnConfig active) =>
        new(
            new PlatformPrimaryEncounterLatchState(active.Engine58, active),
            StagedDescriptor03B7: active.Engine58,
            SlotA: FreeSlot(),
            SlotB: FreeSlot(),
            Cooldown03B8: 0,
            LastTriggerLow03A2: 0,
            SeventhSense: 0,
            GlobalCounter039A: 0x44,
            FrameCounter3C: 1);

    private static PlatformHybridEntitySlotState Slot(
        PlatformCommonEntityRuntimeState entity,
        byte visual) =>
        new(
            entity,
            visual,
            SpecialControl04: 0,
            AttachedHazard: PlatformEntityAttachedHazardState.Empty,
            ParentOffset08: 0);

    private static PlatformHybridEntitySlotState FreeSlot() =>
        PlatformHybridEntitySlotState.Common(
            Entity(action: 0, x: 0, y: 0, flags: 0, hp: 0, type: 0),
            visualSpritePlus1: 0xFE);

    private static PlatformCommonEntityRuntimeState Entity(
        byte action,
        byte x,
        byte y,
        byte flags,
        byte hp,
        byte type = 0x0F,
        byte life = 1,
        byte cosmo = 1) =>
        new(
            new PlatformCommonEntityMotionState(
                ActionState: action,
                X: x,
                Y: y,
                StatePhase: 0,
                GroundDescriptor: 0xA8,
                DecisionTimer: 0,
                FlagsFacing: flags,
                Type: type,
                TerrainProbeRight: 0,
                TerrainProbeLeft: 0),
            HitPoints: hp,
            LifeDrainTicks: life,
            CosmoDrainTicks: cosmo,
            SeventhSenseRewardBcd: 0x10);

    private static PlatformPlayerActionState Player(
        PlatformSaintIndex saint,
        byte x,
        byte y,
        PlatformAttackState? attack = null) =>
        new(
            saint,
            new PlatformHorizontalState(x, 0, 0, 0x40),
            y,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            attack ?? PlatformAttackState.Empty);

    private static PlatformPrimaryEncounterSpawnConfig Config(byte raw, int hp) =>
        PlatformPrimaryEncounterSpawnConfig.FromEncounter(Encounter(raw, hp));

    private static PlatformStagePage Page(int index, byte raw, int hp) =>
        new(index, GroundDescriptors(), Encounter(raw, hp));

    private static PlatformPrimaryEncounter Encounter(byte raw, int hp) =>
        new(
            Raw: raw,
            TypeId: (byte)(raw & 0x0F),
            Tier: (byte)((raw >> 4) & 0x03),
            SecondCommonSlotEnabled: (raw & 0x80) != 0,
            Bit6Unknown: (raw & 0x40) != 0,
            Stats: new PlatformEncounterStats(hp, 1, 1, 4));

    private static PlatformStageMap Stage(params PlatformStagePage[] pages) => new(0, pages);

    private static PlatformStageMap OpenStage() =>
        new(0, [new PlatformStagePage(0, new byte[PlatformStagePage.DescriptorCount])]);

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
            throw new InvalidOperationException($"Special $0F self-test failed: {label}");
    }
}

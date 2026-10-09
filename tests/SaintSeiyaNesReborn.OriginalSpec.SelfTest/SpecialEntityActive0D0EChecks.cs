using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class SpecialEntityActive0D0EChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckScheduledSpawnerFeedsDedicatedRoute(0x0D);
        CheckScheduledSpawnerFeedsDedicatedRoute(0x0E);
        CheckMainContactAndPostInteractionControl04Cadence();
        CheckType0DA0RemovalPersistsIntoNextFrame();
        CheckType0EAttack70UsesSharedTypeAwareLateHelper();
    }

    private static void CheckScheduledSpawnerFeedsDedicatedRoute(byte type)
    {
        var raw = type;
        var stage = Stage(Page(0, raw, hp: 40));
        var initial = PersistentState(
            Config(raw, 40),
            global039A: 0x7F,
            lastTrigger: 0x10);

        var frame = PlatformPersistentPrimaryEntityFrame.StepMainThread(
            stage,
            cameraLow44: 0x3D,
            cameraHigh45: 0,
            scrollX: 0,
            playerState: Player(PlatformSaintIndex.Seiya, x: 0x40, y: 0x40),
            input: PlatformInput.None,
            resources: new PlatformFrameResources(99, 50),
            contactState: new PlatformContactPhaseState(0, default),
            initial,
            scheduledEntries: [new PlatformSpecialSpawnEntry(0x38, 0x00, 0x60)],
            PlatformHitboxParameters.Common,
            PlatformHitboxParameters.Common,
            entropy48: 0x3F,
            cameraDelta43: 1,
            engineSubstate02: 0x01,
            engineState00: 0x20,
            alternateParent08_03AB: 0x66);

        Require(frame.Outcome == PlatformPersistentPrimaryEntityMainThreadOutcome.Continued,
            $"type ${type:X2} scheduled frame continues past exit gate");
        Require(frame.Producer?.ScheduledSpecial?.SlotA.Spawned == true,
            $"$8927 actually creates type ${type:X2} in slot A");
        Require(frame.Hybrid?.SlotA?.Route == PlatformHybridEntitySlotRoute.Special0D0E,
            $"type ${type:X2} routes through dedicated $0D/$0E runtime");
        Require(frame.Hybrid?.SlotA?.Special0D0E is not null
            && frame.Hybrid?.SlotA?.Special is null,
            $"type ${type:X2} never enters the $08/$09/$0C runtime");
        Require(frame.State.SlotA.Entity.Motion.Type == type
            && frame.State.SlotA.VisualSpritePlus1 == 0xFD,
            $"type ${type:X2} survives same-frame producer -> entity processing");
        Require(frame.State.SlotA.Entity.Motion.X == 0xF7,
            $"newly spawned type ${type:X2} receives the confirmed same-frame camera correction");
        Require(frame.State.GlobalCounter039A == 0x7F,
            $"type ${type:X2} bypasses the $08/$09/$0C $039A pre-dispatch");
    }

    private static void CheckMainContactAndPostInteractionControl04Cadence()
    {
        var slot = new PlatformHybridEntitySlotState(
            Entity(type: 0x0E, action: 0x00, x: 0x50, y: 0x50, life: 2, cosmo: 3),
            VisualSpritePlus1: 0xFD,
            SpecialControl04: 0x0B,
            AttachedHazard: PlatformEntityAttachedHazardState.Empty,
            ParentOffset08: 0x44);

        var frame = StepHybrid(
            slot,
            Player(PlatformSaintIndex.Seiya, x: 0x50, y: 0x50),
            global039A: 0x7F,
            frame3C: 1,
            cameraDelta43: 0,
            engineState00: 0x20);

        var special = frame.SlotA!.Special0D0E!;
        Require(frame.SlotA.Route == PlatformHybridEntitySlotRoute.Special0D0E,
            "type $0E uses dedicated route during direct interaction");
        Require(special.MainInteraction?.ContactPhase.Contact.Triggered == true,
            "type $0E executes shared main entity contact from $A700 path");
        Require(frame.ContactState.HazardLatch76 == 0x20
            && frame.ContactState.DrainState == new ContactDrainState(2, 3),
            "type $0E contact carries parent Life/Cosmo drain profile into shared state");
        Require(special.AttachedContact?.Outcome == PlatformEntityAttachedHazardContactOutcome.ContactLatchActive,
            "attached $AA70 contact runs after main contact and observes the seeded latch");
        Require(special.Control04Advanced && frame.SlotA.State.SpecialControl04 == 0,
            "shared post-interaction +$04 cadence wraps $0B->$00");
        Require(frame.GlobalCounter039A == 0x7F,
            "post-interaction +$04 cadence does not imply the bypassed $039A pre-dispatch");
    }

    private static void CheckType0DA0RemovalPersistsIntoNextFrame()
    {
        var slot = new PlatformHybridEntitySlotState(
            Entity(type: 0x0D, action: 0xAF, x: 0x50, y: 0x50),
            VisualSpritePlus1: 0xFD,
            SpecialControl04: 0,
            AttachedHazard: PlatformEntityAttachedHazardState.Empty,
            ParentOffset08: 0);

        var frame1 = StepHybrid(
            slot,
            Player(PlatformSaintIndex.Seiya, x: 0x20, y: 0x20),
            global039A: 0x55,
            frame3C: 1,
            cameraDelta43: 0,
            engineState00: 0x20);

        Require(frame1.SlotA!.Special0D0E!.Outcome ==
                PlatformSpecialEntityActive0D0EOutcome.RemovedByType0DA0Completion,
            "type $0D advances $AF->$B0 and enters the confirmed $A647 terminal branch");
        Require(frame1.SlotA.Special0D0E.Type0DA0Advanced,
            "type $0D exposes its unique $A0-family advancement");
        Require(frame1.SlotA.RemovalA647 is { Type0DExtraVisualRetired: true },
            "$A647 applies the type-$0D extra visual retirement");
        Require(frame1.SlotA.State.VisualSpritePlus1 == 0xFE
            && frame1.SlotA.State.Entity.Motion.ActionState == 0,
            "engine state below $30 leaves the terminal $0D slot conventionally free");
        Require(frame1.GlobalCounter039A == 0x55,
            "$0D terminal path leaves shared $039A untouched");

        var frame2 = StepHybrid(
            frame1.SlotA.State,
            Player(PlatformSaintIndex.Seiya, x: 0x20, y: 0x20),
            global039A: frame1.GlobalCounter039A,
            frame3C: frame1.FrameCounterAfter3C,
            cameraDelta43: 0,
            engineState00: 0x20);

        Require(frame2.SlotA!.Route == PlatformHybridEntitySlotRoute.Skipped
            && frame2.SlotA.Activity.Outcome == PlatformCommonSlotActivityOutcome.VisualFreeInactive,
            "next frame consumes $0D removal output directly and skips the free slot");
    }

    private static void CheckType0EAttack70UsesSharedTypeAwareLateHelper()
    {
        var slot = new PlatformHybridEntitySlotState(
            Entity(type: 0x0E, action: 0x7F, x: 0x50, y: 0x50),
            VisualSpritePlus1: 0xFD,
            SpecialControl04: 0,
            AttachedHazard: PlatformEntityAttachedHazardState.Empty,
            ParentOffset08: 0x22);

        var frame = StepHybrid(
            slot,
            Player(PlatformSaintIndex.Seiya, x: 0x20, y: 0x20),
            global039A: 0x33,
            frame3C: 1,
            cameraDelta43: 0,
            engineState00: 0x20);

        var late = frame.SlotA!.Special0D0E!.LateAttack70;
        Require(late.Advanced && late.CompletedFamily,
            "type $0E advances terminal $70 on odd $3C");
        Require(frame.SlotA.State.Entity.Motion.ActionState == 0x10,
            "type $0E terminal $70 returns to $10 rather than the $08/$09/$0C $00 rule");
        Require(!late.CallsSecondarySpawnRoutine && late.SoundId is null,
            "type $0E terminal $70 does not take the $08/$09/$0C terminal A908/sound branch");
        Require(frame.GlobalCounter039A == 0x33,
            "type $0E late attack path leaves $039A unchanged");
    }

    private static PlatformHybridEntityCombatSliceResult StepHybrid(
        PlatformHybridEntitySlotState slotA,
        PlatformPlayerActionState player,
        byte global039A,
        byte frame3C,
        byte cameraDelta43,
        byte engineState00) =>
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
            globalCounter039A: global039A,
            frameCounter3C: frame3C,
            entropy48: 0x3F,
            cameraDelta43,
            engineSubstate02: 1,
            engineState00,
            alternateParent08_03AB: 0x66);

    private static PlatformPersistentPrimaryEntityFrameState PersistentState(
        PlatformPrimaryEncounterSpawnConfig active,
        byte global039A,
        byte lastTrigger) =>
        new(
            new PlatformPrimaryEncounterLatchState(active.Engine58, active),
            StagedDescriptor03B7: active.Engine58,
            SlotA: FreeSlot(),
            SlotB: FreeSlot(),
            Cooldown03B8: 0,
            LastTriggerLow03A2: lastTrigger,
            SeventhSense: 0,
            GlobalCounter039A: global039A,
            FrameCounter3C: 1);

    private static PlatformHybridEntitySlotState FreeSlot() =>
        PlatformHybridEntitySlotState.Common(
            Entity(type: 0, action: 0, x: 0, y: 0),
            visualSpritePlus1: 0xFE);

    private static PlatformCommonEntityRuntimeState Entity(
        byte type,
        byte action,
        byte x,
        byte y,
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
                FlagsFacing: 0x40,
                Type: type,
                TerrainProbeRight: 0,
                TerrainProbeLeft: 0),
            HitPoints: 40,
            LifeDrainTicks: life,
            CosmoDrainTicks: cosmo,
            SeventhSenseRewardBcd: 0x10);

    private static PlatformPlayerActionState Player(
        PlatformSaintIndex saint,
        byte x,
        byte y) =>
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
            PlatformAttackState.Empty);

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
            throw new InvalidOperationException($"Special $0D/$0E self-test failed: {label}");
    }
}

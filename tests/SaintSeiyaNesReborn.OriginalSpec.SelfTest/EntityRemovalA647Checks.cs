using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class EntityRemovalA647Checks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckEngineStateControlsLogicalClear();
        CheckCommonRemovalCarriesVisualFreeStateIntoNextFrame();
        CheckVisualFreeReactionCanContinueAtEngine30();
        CheckSpecialRemovalUsesSameRetirementHelper();
        CheckFreedSlotCanBeReusedByConfirmedSpawner();
    }

    private static void CheckEngineStateControlsLogicalClear()
    {
        var entity = Entity(type: 0x0D, action: 0xD4, x: 0x50, y: 0x50);

        var low = PlatformEntityRemovalA647.Apply(entity, 0x27, engineState00: 0x2F);
        Require(low.VisualSpritePlus1 == 0xFE,
            "$A647 always retires tracked visual +1 to $FE");
        Require(low.LogicalActionCleared && low.Entity.Motion.ActionState == 0,
            "$A647 clears logical +0 while engine $00<$30");
        Require(low.BaseVisualRecordsRetired == 11 && low.Type0DExtraVisualRetired,
            "$A647 retires eleven base records and the type0D +2C/+2D record");

        var high = PlatformEntityRemovalA647.Apply(entity, 0x27, engineState00: 0x30);
        Require(high.VisualSpritePlus1 == 0xFE,
            "visual retirement is independent of the $00 threshold");
        Require(!high.LogicalActionCleared && high.Entity.Motion.ActionState == 0xD4,
            "$A647 preserves logical action at engine $00>=$30");
    }

    private static void CheckCommonRemovalCarriesVisualFreeStateIntoNextFrame()
    {
        var player = BasePlayer();
        var active = PlatformHybridEntitySlotState.Common(
            Entity(type: 0x05, action: 0x40, x: 0x00, y: 0x50),
            visualSpritePlus1: 0x27);

        var frame1 = Step(
            active,
            FreeSlot(),
            player,
            frameCounter3C: 1,
            cameraDelta43: 1,
            engineState00: 0x20);

        Require(frame1.SlotA!.Common!.RemovedBeforeInteraction,
            "camera wrap reaches the confirmed common removal path");
        Require(frame1.SlotA.RemovalA647.HasValue,
            "hybrid scheduler composes $A647 after common removal");
        Require(frame1.SlotA.State.VisualSpritePlus1 == 0xFE,
            "post-frame persistent state owns the retired visual marker");
        Require(frame1.SlotA.State.Entity.Motion.ActionState == 0,
            "engine $00<$30 clears the logical action in persistent state");

        var frame2 = Step(
            frame1.SlotA.State,
            FreeSlot(),
            player,
            frameCounter3C: frame1.FrameCounterAfter3C,
            cameraDelta43: 0,
            engineState00: 0x20);

        Require(frame2.SlotA!.Route == PlatformHybridEntitySlotRoute.Skipped,
            "next frame consumes prior output directly and skips the now-free slot");
        Require(frame2.SlotA.Activity.Outcome == PlatformCommonSlotActivityOutcome.VisualFreeInactive,
            "no manual visual repair is needed between frames");
        Require(frame2.SlotA.RemovalA647 is null,
            "already-free skipped slot does not execute retirement again");
    }

    private static void CheckVisualFreeReactionCanContinueAtEngine30()
    {
        var player = BasePlayer();
        var active = PlatformHybridEntitySlotState.Common(
            Entity(type: 0x05, action: 0x40, x: 0x00, y: 0x50),
            visualSpritePlus1: 0x31);

        var frame1 = Step(
            active,
            FreeSlot(),
            player,
            frameCounter3C: 1,
            cameraDelta43: 1,
            engineState00: 0x30);

        Require(frame1.SlotA!.State.VisualSpritePlus1 == 0xFE,
            "$A647 retires visual marker at engine $30");
        Require(frame1.SlotA.State.Entity.Motion.ActionState == 0x40,
            "engine $30 preserves logical $40 family behind the free visual slot");
        Require(frame1.SlotA.RemovalA647 is { LogicalActionCleared: false },
            "retirement metadata exposes the threshold branch");

        var frame2 = Step(
            frame1.SlotA.State,
            FreeSlot(),
            player,
            frameCounter3C: frame1.FrameCounterAfter3C,
            cameraDelta43: 8,
            engineState00: 0x30);

        Require(frame2.SlotA!.Activity.Outcome ==
                PlatformCommonSlotActivityOutcome.VisualFreeButReactionContinues,
            "$A459 admits visual-free logical $40 cleanup on the following frame");
        Require(frame2.SlotA.Route == PlatformHybridEntitySlotRoute.Common,
            "visual-free $40 state routes through common runtime rather than skipping");
        Require(frame2.SlotA.State.Entity.Motion.ActionState == 0x41,
            "once camera correction moves X back in range, reaction cleanup advances");
        Require(frame2.SlotA.State.VisualSpritePlus1 == 0xFE,
            "cleanup progression does not reactivate the retired visual slot");
    }

    private static void CheckSpecialRemovalUsesSameRetirementHelper()
    {
        var player = BasePlayer();
        var active = PlatformHybridEntitySlotState.Special08090C(
            Entity(type: 0x08, action: 0x50, x: 0x50, y: 0xAE),
            visualSpritePlus1: 0x44);

        var frame = Step(
            active,
            FreeSlot(),
            player,
            frameCounter3C: 1,
            cameraDelta43: 0,
            engineState00: 0x20);

        Require(frame.SlotA!.Route == PlatformHybridEntitySlotRoute.Special08090C,
            "type08 still routes through the dedicated special runtime");
        Require(frame.SlotA.Special!.Outcome ==
                PlatformSpecialEntityActive08090COutcome.RemovedBeforeInteraction,
            "special $50 path reaches shared removal after crossing lower band");
        Require(frame.SlotA.RemovalA647.HasValue,
            "special removal composes the same $A647 helper");
        Require(frame.SlotA.State.VisualSpritePlus1 == 0xFE
            && frame.SlotA.State.Entity.Motion.ActionState == 0,
            "special persistent slot becomes visually/logically free below engine $30");
    }

    private static void CheckFreedSlotCanBeReusedByConfirmedSpawner()
    {
        var retired = PlatformEntityRemovalA647.Apply(
            Entity(type: 0x05, action: 0x10, x: 0xF8, y: 0x50),
            visualSpritePlus1: 0x20,
            engineState00: 0x20);

        var profile = new PlatformSpecialSpawnProfile(
            HitPoints0C: 30,
            CosmoDrainTicks0D: 3,
            LifeDrainTicks0E: 2,
            SeventhSenseRewardBcd0F: 0x10);
        PlatformSpecialSpawnEntry[] schedule =
        [
            new(CameraLowAligned: 0x28, CameraHigh: 0x01, SpawnY: 0x50),
        ];

        var spawn = PlatformScheduledSpecialEntitySpawner.TrySpawn(
            retired.Entity,
            retired.VisualSpritePlus1,
            engine58: 0x08,
            cameraLow44: 0x2B,
            cameraHigh45: 0x01,
            lastTriggerLow03A2: 0x00,
            profile,
            schedule);

        Require(spawn.Outcome == PlatformScheduledSpecialSpawnOutcome.Spawned,
            "confirmed scheduled producer can reuse the exact visual-free output of $A647");
        Require(spawn.VisualSprite == 0xFD,
            "slot reuse transitions tracked visual marker $FE->$FD without repair");
        Require(spawn.Entity.Motion.Type == 0x08 && spawn.Entity.Motion.ActionState == 0,
            "producer replaces the retired logical record with scheduled type08 state");
    }

    private static PlatformHybridEntityCombatSliceResult Step(
        PlatformHybridEntitySlotState slotA,
        PlatformHybridEntitySlotState slotB,
        PlatformPlayerActionState player,
        byte frameCounter3C,
        byte cameraDelta43,
        byte engineState00) =>
        PlatformHybridEntityCombatSlice.StepNonFatal(
            OpenStage(),
            player,
            PlatformInput.None,
            new PlatformFrameResources(99, 50),
            new PlatformContactPhaseState(player.Special76, default),
            slotA,
            slotB,
            PlatformHitboxParameters.Common,
            PlatformHitboxParameters.Common,
            seventhSense: 0,
            globalCounter039A: 0,
            frameCounter3C,
            entropy48: 0,
            cameraDelta43,
            engineSubstate02: 1,
            engineState00,
            alternateParent08_03AB: 0x66);

    private static PlatformHybridEntitySlotState FreeSlot() =>
        PlatformHybridEntitySlotState.Common(
            Entity(type: 0x05, action: 0x00, x: 0, y: 0),
            visualSpritePlus1: 0xFE);

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
                GroundDescriptor: 0xE0,
                DecisionTimer: 0,
                FlagsFacing: 0x40,
                Type: type,
                TerrainProbeRight: 0,
                TerrainProbeLeft: 0),
            HitPoints: 30,
            LifeDrainTicks: 2,
            CosmoDrainTicks: 3,
            SeventhSenseRewardBcd: 0x10);

    private static PlatformPlayerActionState BasePlayer() =>
        new(
            PlatformSaintIndex.Seiya,
            new PlatformHorizontalState(0x50, 0, 0, 0x40),
            0x50,
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

    private static PlatformStageMap OpenStage() =>
        new(0, [new PlatformStagePage(0, new byte[PlatformStagePage.DescriptorCount])]);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"A647 removal self-test failed: {label}");
    }
}

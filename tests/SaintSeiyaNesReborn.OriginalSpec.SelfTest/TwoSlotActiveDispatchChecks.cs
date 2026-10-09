using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class TwoSlotActiveDispatchChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckSurvivingHitContinuesReactionNextFrame();
        CheckFallLandingStores10ButSkipsInteractionThisFrame();
    }

    private static void CheckSurvivingHitContinuesReactionNextFrame()
    {
        var active = new PlatformAttackSlot(
            new PlatformAttackObject(0x50, 0x64, 0x40, 0x50, 0, 0, 0, 0),
            3);
        var player = BasePlayer(
            PlatformSaintIndex.Hyoga,
            latch: 5,
            attack: PlatformAttackState.Empty with { Slot0 = active });
        var entityA = RuntimeEntity(action: 0x10, x: 0x50, y: 0x50, phase: 0, ground: 0xE0, hp: 30);
        var entityB = RuntimeEntity(action: 0x10, x: 0x70, y: 0x50, phase: 0, ground: 0xE0, hp: 30);

        var frameN = PlatformTwoCommonEntityCombatSlice.StepNonFatal(
            OpenStage(),
            player,
            PlatformInput.None,
            new PlatformFrameResources(99, 50),
            new PlatformContactPhaseState(5, new ContactDrainState(0, 0)),
            entityA,
            entityB,
            PlatformHitboxParameters.Common,
            PlatformHitboxParameters.Common,
            seventhSense: 0,
            frameCounter3C: 1,
            entropy48: 0,
            cameraDelta43: 1,
            engineSubstate02: 1);

        Require(frameN.SlotA!.Interaction!.HitSequence.Results[0].Result.Outcome == PlatformProjectileHitOutcome.HpSurvived,
            "frame N slot A survives Hyoga projectile hit");
        Require(frameN.SlotA.Entity.Motion.ActionState == 0x40,
            "surviving hit stores $40 reaction for next frame");
        Require(frameN.SlotA.Entity.Motion.StatePhase == 0x48,
            "right-facing hit seeds +$03 recoil byte $48");
        Require(frameN.SlotA.Entity.HitPoints == 20,
            "frame N stores reduced HP");

        var frameN1 = PlatformTwoCommonEntityCombatSlice.StepNonFatal(
            OpenStage(),
            frameN.PlayerAfterLatePhases.State,
            PlatformInput.None,
            frameN.PrePlayer.ResourcesAfterDrain,
            frameN.ContactState,
            frameN.SlotA.Entity,
            frameN.SlotB!.Entity,
            PlatformHitboxParameters.Common,
            PlatformHitboxParameters.Common,
            frameN.SeventhSense,
            frameN.FrameCounterAfter3C,
            entropy48: 0,
            cameraDelta43: 1,
            engineSubstate02: 1);

        Require(frameN1.SlotA!.Dispatch.Route == PlatformCommonEntityActiveRoute.HitReaction40,
            "frame N+1 dispatches persisted $40 family rather than forcing ordinary route");
        Require(frameN1.SlotA.Dispatch.Continuation == PlatformCommonEntityActiveContinuation.SkipInteraction,
            "$40 reaction path bypasses projectile/contact interactions");
        Require(frameN1.SlotA.Interaction is null,
            "reaction slot does not invoke $9915/$98BA");
        Require(frameN1.SlotA.Entity.Motion.ActionState == 0x41,
            "reaction timer advances $40->$41");
        Require(frameN1.SlotA.Entity.Motion.StatePhase == 0x47,
            "recoil byte advances $48->$47");
        Require(frameN1.SlotA.Entity.Motion.X == 0x54,
            "reaction applies +4 X without camera subtraction");
        Require(frameN1.SlotA.Entity.HitPoints == 20,
            "reaction frame preserves HP after prior hit");
    }

    private static void CheckFallLandingStores10ButSkipsInteractionThisFrame()
    {
        var active = new PlatformAttackSlot(
            new PlatformAttackObject(0x60, 0x64, 0x40, 0x50, 0, 0, 0, 0),
            3);
        var player = BasePlayer(
            PlatformSaintIndex.Seiya,
            latch: 5,
            attack: PlatformAttackState.Empty with { Slot0 = active });
        var falling = RuntimeEntity(action: 0x50, x: 0x50, y: 0x5F, phase: 3, ground: 0xA8, hp: 30);
        var other = RuntimeEntity(action: 0x10, x: 0x70, y: 0x50, phase: 0, ground: 0xE0, hp: 30);

        var frame = PlatformTwoCommonEntityCombatSlice.StepNonFatal(
            OpenStage(),
            player,
            PlatformInput.None,
            new PlatformFrameResources(99, 100),
            new PlatformContactPhaseState(5, new ContactDrainState(0, 0)),
            falling,
            other,
            PlatformHitboxParameters.Common,
            PlatformHitboxParameters.Common,
            seventhSense: 0,
            frameCounter3C: 1,
            entropy48: 0,
            cameraDelta43: 0,
            engineSubstate02: 1);

        Require(frame.SlotA!.Dispatch.Route == PlatformCommonEntityActiveRoute.Fall50,
            "entry $50 uses fall route");
        Require(frame.SlotA.Dispatch.Fall50!.Value.Outcome == PlatformCommonEntityFall50Outcome.Landed,
            "$50 fall lands through shared C491");
        Require(frame.SlotA.Entity.Motion.ActionState == 0x10 && frame.SlotA.Entity.Motion.Y == 0x60,
            "landing stores next-frame ordinary state and snapped Y");
        Require(frame.SlotA.Dispatch.Continuation == PlatformCommonEntityActiveContinuation.SkipInteraction,
            "same update still follows $50 renderer continuation");
        Require(frame.SlotA.Interaction is null,
            "overlapping projectile cannot hit landed entity until a later ordinary update");
        Require(frame.SlotA.Entity.HitPoints == 30,
            "skipped interaction leaves landed entity HP unchanged");
    }

    private static PlatformCommonEntityRuntimeState RuntimeEntity(
        byte action,
        byte x,
        byte y,
        byte phase,
        byte ground,
        byte hp) =>
        new(
            new PlatformCommonEntityMotionState(
                ActionState: action,
                X: x,
                Y: y,
                StatePhase: phase,
                GroundDescriptor: ground,
                DecisionTimer: 5,
                FlagsFacing: 0x40,
                Type: 0x05,
                TerrainProbeRight: 0,
                TerrainProbeLeft: 0),
            HitPoints: hp,
            LifeDrainTicks: 2,
            CosmoDrainTicks: 3,
            SeventhSenseRewardBcd: 0x10);

    private static PlatformPlayerActionState BasePlayer(
        PlatformSaintIndex saint,
        byte latch,
        PlatformAttackState attack) =>
        new(
            saint,
            new PlatformHorizontalState(0x40, 0, 0, 0x40),
            0x50,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            latch,
            0,
            attack);

    private static PlatformStageMap OpenStage() =>
        new(0, [new PlatformStagePage(0, new byte[PlatformStagePage.DescriptorCount])]);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Two-slot active dispatch self-test failed: {label}");
    }
}

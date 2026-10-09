using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class CommonEntityDropE0Checks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckE0NeverUsesLandingResolver();
        CheckE0RemovesAtLowerBand();
        CheckProjectileDropReactionContinuesNextFrame();
    }

    private static void CheckE0NeverUsesLandingResolver()
    {
        var state = Motion(action: 0xE0, type: 0x01, x: 0x50, y: 0x5F, ground: 0xA8);
        var step = PlatformCommonEntityDropE0.Step(state, cameraDelta43: 1);

        Require(step.Outcome == PlatformCommonEntityDropE0Outcome.Falling,
            "$E0 remains falling below lower removal band");
        Require(step.State.X == 0x4F && step.State.Y == 0x62,
            "$E0 applies camera -1 and raw +3 Y");
        Require(step.State.ActionState == 0xE0,
            "$E0 family is preserved while falling");
        Require(step.State.Y != 0x60,
            "ground descriptor $A8 does not invoke C491 snap as $50 would");
    }

    private static void CheckE0RemovesAtLowerBand()
    {
        var state = Motion(action: 0xE0, type: 0x01, x: 0x50, y: 0xAD, ground: 0xA8);
        var step = PlatformCommonEntityDropE0.Step(state, cameraDelta43: 0);

        Require(step.State.Y == 0xB0,
            "E0 lower test occurs after the +3 fall step");
        Require(step.Outcome == PlatformCommonEntityDropE0Outcome.RemovedLowerBand,
            "post-step Y $B0 removes the record");
    }

    private static void CheckProjectileDropReactionContinuesNextFrame()
    {
        var active = new PlatformAttackSlot(
            new PlatformAttackObject(0x50, 0x64, 0x40, 0x50, 0, 0, 0, 0),
            4);
        var player = BasePlayer(
            PlatformSaintIndex.Seiya,
            latch: 5,
            attack: PlatformAttackState.Empty with { Slot0 = active });
        var entityA = RuntimeEntity(action: 0x10, type: 0x01, x: 0x50, y: 0x50);
        var entityB = RuntimeEntity(action: 0x10, type: 0x05, x: 0x70, y: 0x50);

        var frameN = PlatformTwoCommonEntityCombatSlice.StepNonFatal(
            OpenStage(),
            player,
            PlatformInput.None,
            new PlatformFrameResources(99, 100),
            new PlatformContactPhaseState(5, new ContactDrainState(0, 0)),
            entityA,
            entityB,
            PlatformHitboxParameters.Common,
            PlatformHitboxParameters.Common,
            seventhSense: 0,
            frameCounter3C: 1,
            entropy48: 0,
            cameraDelta43: 0,
            engineSubstate02: 1);

        Require(frameN.SlotA!.Interaction!.HitSequence.Results[0].Result.Outcome == PlatformProjectileHitOutcome.DropReaction,
            "type01 projectile overlap enters special drop reaction");
        Require(frameN.SlotA.Entity.Motion.ActionState == 0xE0,
            "drop hit stores $E0 for next frame");
        Require(frameN.SlotA.Entity.Motion.X == 0x51,
            "ordinary movement happens before the hit and is retained by E0");
        Require(frameN.SlotA.Entity.Motion.Y == 0x56,
            "hit reaction applies immediate +6 Y before next-frame E0 fall");

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

        Require(frameN1.SlotA!.Dispatch.Route == PlatformCommonEntityActiveRoute.DropE0,
            "next frame dispatches persisted E0 family");
        Require(frameN1.SlotA.Dispatch.Continuation == PlatformCommonEntityActiveContinuation.SkipInteraction,
            "E0 bypasses $9915/$98BA while falling");
        Require(frameN1.SlotA.Interaction is null,
            "still-live Seiya projectile cannot hit E0 entity on the drop path");
        Require(frameN1.SlotA.Entity.Motion.X == 0x50 && frameN1.SlotA.Entity.Motion.Y == 0x59,
            "next E0 frame applies camera -1 to retained X and +3 fall");
    }

    private static PlatformCommonEntityRuntimeState RuntimeEntity(
        byte action,
        byte type,
        byte x,
        byte y) =>
        new(
            Motion(action, type, x, y, ground: 0xA8),
            HitPoints: 30,
            LifeDrainTicks: 2,
            CosmoDrainTicks: 3,
            SeventhSenseRewardBcd: 0x10);

    private static PlatformCommonEntityMotionState Motion(
        byte action,
        byte type,
        byte x,
        byte y,
        byte ground) =>
        new(
            ActionState: action,
            X: x,
            Y: y,
            StatePhase: 0,
            GroundDescriptor: ground,
            DecisionTimer: 5,
            FlagsFacing: 0x40,
            Type: type,
            TerrainProbeRight: 0,
            TerrainProbeLeft: 0);

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
            throw new InvalidOperationException($"Common entity E0 self-test failed: {label}");
    }
}

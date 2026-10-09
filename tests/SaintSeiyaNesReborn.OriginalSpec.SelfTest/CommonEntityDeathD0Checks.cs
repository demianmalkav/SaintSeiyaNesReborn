using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class CommonEntityDeathD0Checks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckCadenceAndDescriptorVerticalMotion();
        CheckTerminalRemoval();
        CheckKillEntersD0ThenContinuesNextFrame();
    }

    private static void CheckCadenceAndDescriptorVerticalMotion()
    {
        var offCadence = PlatformCommonEntityDeathD0.Step(
            Motion(action: 0xD0, x: 0x50, y: 0x50, ground: 0x70),
            frameCounter3C: 1,
            cameraDelta43: 1);
        Require(offCadence.State.X == 0x4F,
            "$D0 tracks camera every update");
        Require(offCadence.State.ActionState == 0xD0 && offCadence.State.Y == 0x50,
            "off-cadence frame leaves death phase/Y unchanged");
        Require(!offCadence.DeathPhaseAdvanced,
            "death phase only advances when ($3C & 3)==0");

        var lowDescriptor = PlatformCommonEntityDeathD0.Step(
            Motion(action: 0xD0, x: 0x50, y: 0x50, ground: 0x70),
            frameCounter3C: 4,
            cameraDelta43: 0);
        Require(lowDescriptor.State.ActionState == 0xD1,
            "cadence frame advances $D0->$D1");
        Require(lowDescriptor.State.Y == 0x52 && lowDescriptor.ScreenYDelta == 2,
            "descriptor below $80 adds +2 Y on cadence tick");

        var middleDescriptor = PlatformCommonEntityDeathD0.Step(
            Motion(action: 0xD0, x: 0x50, y: 0x50, ground: 0x80),
            frameCounter3C: 4,
            cameraDelta43: 0);
        Require(middleDescriptor.State.ActionState == 0xD1,
            "middle descriptor still advances death phase");
        Require(middleDescriptor.State.Y == 0x50,
            "$80-$EF descriptor band suppresses D0 +2 Y drift");

        var highDescriptor = PlatformCommonEntityDeathD0.Step(
            Motion(action: 0xD0, x: 0x50, y: 0x50, ground: 0xF0),
            frameCounter3C: 4,
            cameraDelta43: 0);
        Require(highDescriptor.State.Y == 0x52,
            "$F0+ descriptor restores +2 Y drift");
    }

    private static void CheckTerminalRemoval()
    {
        var result = PlatformCommonEntityDeathD0.Step(
            Motion(action: 0xDF, x: 0x50, y: 0x50, ground: 0x70),
            frameCounter3C: 4,
            cameraDelta43: 0);

        Require(result.Outcome == PlatformCommonEntityDeathD0Outcome.CompletedRemoval,
            "$DF cadence tick attempts $E0 and completes removal");
        Require(result.State.ActionState == 0,
            "terminal D0 path clears action before removal helper");
        Require(result.State.Y == 0x52,
            "terminal cadence tick applies descriptor Y drift before removal");
    }

    private static void CheckKillEntersD0ThenContinuesNextFrame()
    {
        var active = new PlatformAttackSlot(
            new PlatformAttackObject(0x50, 0x64, 0x40, 0x50, 0, 0, 0, 0),
            3);
        var player = BasePlayer(
            latch: 5,
            attack: PlatformAttackState.Empty with { Slot0 = active });
        var dyingTarget = RuntimeEntity(action: 0x10, x: 0x50, y: 0x50, ground: 0x80, hp: 10, reward: 0x10);
        var other = RuntimeEntity(action: 0x10, x: 0x70, y: 0x50, ground: 0xE0, hp: 30, reward: 0);

        // Hyoga at Cosmo50 deals 10. Starting $3C=3 means the next frame is
        // $3C=4, exactly a D0 cadence tick.
        var frameN = PlatformTwoCommonEntityCombatSlice.StepNonFatal(
            OpenStage(),
            player,
            PlatformInput.None,
            new PlatformFrameResources(99, 50),
            new PlatformContactPhaseState(5, new ContactDrainState(0, 0)),
            dyingTarget,
            other,
            PlatformHitboxParameters.Common,
            PlatformHitboxParameters.Common,
            seventhSense: 100,
            frameCounter3C: 3,
            entropy48: 0,
            cameraDelta43: 1,
            engineSubstate02: 1);

        Require(frameN.SlotA!.Interaction!.HitSequence.Results[0].Result.Outcome == PlatformProjectileHitOutcome.HpKilled,
            "frame N projectile kills slot A");
        Require(frameN.SlotA.Entity.Motion.ActionState == 0xD0,
            "ordinary kill stores D0 death family");
        Require(frameN.SeventhSense == 110,
            "kill awards packed-BCD +10 Seventh Sense once");
        Require(frameN.FrameCounterAfter3C == 4,
            "frame N closes on D0 cadence-aligned next counter");

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

        Require(frameN1.SlotA!.Dispatch.Route == PlatformCommonEntityActiveRoute.DeathD0,
            "frame N+1 dispatches persisted D0 death family");
        Require(frameN1.SlotA.Dispatch.DeathD0!.Value.DeathPhaseAdvanced,
            "$3C=4 advances D0 cadence");
        Require(frameN1.SlotA.Entity.Motion.ActionState == 0xD1,
            "death phase advances D0->D1");
        Require(frameN1.SlotA.Entity.Motion.X == 0x4F,
            "death path applies camera -1 to X");
        Require(frameN1.SlotA.Entity.Motion.Y == 0x50,
            "$80 descriptor suppresses +2 Y drift on this death tick");
        Require(frameN1.SlotA.Interaction is null,
            "D0 death frame bypasses projectile/contact interaction");
        Require(frameN1.SeventhSense == 110,
            "death continuation does not award the kill reward again");
    }

    private static PlatformCommonEntityMotionState Motion(
        byte action,
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
            Type: 0x05,
            TerrainProbeRight: 0,
            TerrainProbeLeft: 0);

    private static PlatformCommonEntityRuntimeState RuntimeEntity(
        byte action,
        byte x,
        byte y,
        byte ground,
        byte hp,
        byte reward) =>
        new(
            Motion(action, x, y, ground),
            HitPoints: hp,
            LifeDrainTicks: 2,
            CosmoDrainTicks: 3,
            SeventhSenseRewardBcd: reward);

    private static PlatformPlayerActionState BasePlayer(byte latch, PlatformAttackState attack) =>
        new(
            PlatformSaintIndex.Hyoga,
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
            throw new InvalidOperationException($"Common entity D0 self-test failed: {label}");
    }
}

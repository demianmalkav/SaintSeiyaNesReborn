using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class CommonEntityPreparationChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckOrdinaryWalkAndCameraComposition();
        CheckDecisionStartedJumpConsumesFirstSampleSameFrame();
        CheckProximityFallTransition();
        CheckE0TerrainSuppressesProximityFall();
        CheckLeftWrapRemovesEntity();
    }

    private static void CheckOrdinaryWalkAndCameraComposition()
    {
        var state = BaseState(
            action: 0x10,
            x: 0x40,
            y: 0x50,
            phase: 0,
            timer: 5,
            facing: 0x40,
            ground: 0xE0);

        var step = PlatformCommonEntityPreparation.StepOrdinaryMobile(
            state,
            playerX: 0xA0,
            playerY: 0x50,
            playerJumpPhase49: 0,
            entropy48: 0,
            frameCounter3C: 0,
            cameraDelta43: 1);

        Require(step.Decision.Outcome == PlatformEntityDecisionOutcome.TimerPending,
            "ordinary walker keeps pending decision timer");
        Require(step.HorizontalDeltaBeforeCamera == 2,
            "frameCounter divisible by four gives +2 rightward entity step");
        Require(step.State.X == 0x41,
            "horizontal +2 followed by camera -1 produces net +1 screen X");
        Require(step.Outcome == PlatformCommonEntityPreparationOutcome.ReadyForInteraction,
            "ordinary in-bounds walker reaches interaction stage");
    }

    private static void CheckDecisionStartedJumpConsumesFirstSampleSameFrame()
    {
        var state = BaseState(
            action: 0x10,
            x: 0x30,
            y: 0x60,
            phase: 0,
            timer: 1,
            facing: 0x40,
            ground: 0xE0);

        var step = PlatformCommonEntityPreparation.StepOrdinaryMobile(
            state,
            playerX: 0x50,
            playerY: 0x50,
            playerJumpPhase49: 0,
            entropy48: 0x12,
            frameCounter3C: 1,
            cameraDelta43: 0);

        Require(step.Decision.Outcome == PlatformEntityDecisionOutcome.JumpStartedRight,
            "expired timer with player to right starts $31 jump");
        Require(step.JumpStep is not null,
            "new jump immediately enters C5E6 in same update");
        Require(step.JumpStep!.Value.TableIndexUsed == 0,
            "same-frame jump consumes first vertical table sample");
        Require(step.State.StatePhase == 2,
            "decision seeds phase1 and C5E6 advances it to phase2 immediately");
        Require(step.State.Y == 0x58,
            "first jump sample moves entity upward by 8 pixels");
        Require(step.State.X == 0x31,
            "$31 jump also applies same-frame +1 horizontal step");
    }

    private static void CheckProximityFallTransition()
    {
        var state = BaseState(
            action: 0x10,
            x: 0x50,
            y: 0x40,
            phase: 0,
            timer: 5,
            facing: 0x40,
            ground: 0x80);

        var step = PlatformCommonEntityPreparation.StepOrdinaryMobile(
            state,
            playerX: 0x50,
            playerY: 0x70,
            playerJumpPhase49: 0,
            entropy48: 0,
            frameCounter3C: 1,
            cameraDelta43: 1);

        Require(step.State.X == 0x50,
            "rightward +1 and camera -1 preserve X for proximity fixture");
        Require(step.ProximityFallStarted,
            "player below and nearby triggers A6F2 transition");
        Require(step.State.ActionState == 0x50 && step.State.Y == 0x46,
            "proximity transition writes $50 and applies Y+6");
    }

    private static void CheckE0TerrainSuppressesProximityFall()
    {
        var state = BaseState(
            action: 0x10,
            x: 0x50,
            y: 0x40,
            phase: 0,
            timer: 5,
            facing: 0x40,
            ground: 0xE0);

        var step = PlatformCommonEntityPreparation.StepOrdinaryMobile(
            state,
            playerX: 0x50,
            playerY: 0x70,
            playerJumpPhase49: 0,
            entropy48: 0,
            frameCounter3C: 1,
            cameraDelta43: 1);

        Require(!step.ProximityFallStarted,
            "$E0-$EF ground descriptor bypasses A6AA-A6FE proximity fall logic");
        Require(step.State.ActionState == 0x10 && step.State.Y == 0x40,
            "suppressed proximity fall preserves ordinary state/Y");
    }

    private static void CheckLeftWrapRemovesEntity()
    {
        var state = BaseState(
            action: 0x10,
            x: 0x00,
            y: 0x50,
            phase: 0,
            timer: 5,
            facing: 0x00,
            ground: 0xE0);

        var step = PlatformCommonEntityPreparation.StepOrdinaryMobile(
            state,
            playerX: 0x80,
            playerY: 0x50,
            playerJumpPhase49: 0,
            entropy48: 0,
            frameCounter3C: 1,
            cameraDelta43: 0);

        Require(step.State.X == 0xFF,
            "left step wraps byte X to $FF exactly like 6502 arithmetic");
        Require(step.Outcome == PlatformCommonEntityPreparationOutcome.RemovedHorizontal,
            "$F8-$FF post-move X enters A647 removal path");
    }

    private static PlatformCommonEntityMotionState BaseState(
        byte action,
        byte x,
        byte y,
        byte phase,
        byte timer,
        byte facing,
        byte ground) =>
        new(
            ActionState: action,
            X: x,
            Y: y,
            StatePhase: phase,
            GroundDescriptor: ground,
            DecisionTimer: timer,
            FlagsFacing: facing,
            Type: 0x05,
            TerrainProbeRight: 0,
            TerrainProbeLeft: 0);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Common entity preparation self-test failed: {label}");
    }
}

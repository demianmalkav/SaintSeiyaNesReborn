using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class CommonEntityFall50Checks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckOpenFallWithCamera();
        CheckFallLandsAfterPlusThree();
        CheckLowerBandRemovesBeforeLanding();
        CheckSharedLandingMatchesJumpPath();
    }

    private static void CheckOpenFallWithCamera()
    {
        var result = PlatformCommonEntityFall50.Step(
            State(action: 0x50, x: 0x50, y: 0x60, phase: 0, ground: 0x80),
            cameraDelta43: 1);

        Require(result.Outcome == PlatformCommonEntityFall50Outcome.Falling,
            "descriptor below $A8 keeps $50 entity falling");
        Require(result.State.X == 0x4F,
            "$50 path subtracts camera delta from X");
        Require(result.State.Y == 0x63,
            "$50 path applies fixed +3 Y");
        Require(result.State.ActionState == 0x50,
            "unlanded $50 action remains in fall family");
    }

    private static void CheckFallLandsAfterPlusThree()
    {
        var result = PlatformCommonEntityFall50.Step(
            State(action: 0x50, x: 0x50, y: 0x5F, phase: 7, ground: 0xA8),
            cameraDelta43: 0);

        Require(result.Outcome == PlatformCommonEntityFall50Outcome.Landed,
            "$50 +3 reaches landing-eligible Y then calls C491");
        Require(result.State.Y == 0x60,
            "C491 snaps post-fall Y=$62 back to row $60");
        Require(result.State.ActionState == 0x10 && result.State.StatePhase == 0,
            "common type landing returns to $10 and clears +$03 phase");
        Require(result.ScreenYDelta == 1,
            "net screen delta is +3 fall followed by -2 landing snap");
    }

    private static void CheckLowerBandRemovesBeforeLanding()
    {
        var result = PlatformCommonEntityFall50.Step(
            State(action: 0x50, x: 0x50, y: 0xAD, phase: 0, ground: 0xF0),
            cameraDelta43: 0);

        Require(result.State.Y == 0xB0,
            "fixed fall reaches exact lower removal boundary");
        Require(result.Outcome == PlatformCommonEntityFall50Outcome.RemovedLowerBand,
            "Y >= $B0 removes $50 entity before C491 landing attempt");
        Require(result.State.ActionState == 0x50,
            "removal boundary does not invent a landing state transition");
    }

    private static void CheckSharedLandingMatchesJumpPath()
    {
        var state = State(action: 0x31, x: 0x50, y: 0x62, phase: 0x10, ground: 0xA8);
        var direct = PlatformCommonEntityLanding.Resolve(state);
        var jump = PlatformCommonEntityMotion.StepJumpVertical(state);

        Require(direct.Landed && jump.Landed,
            "shared C491 resolver and jump path both accept same landing state");
        Require(jump.State == direct.State,
            "jump path now delegates to the same public C491 landing state");
        Require(jump.ScreenYDeltaApplied == direct.ScreenYDelta,
            "jump path reports the exact shared landing snap delta");
    }

    private static PlatformCommonEntityMotionState State(
        byte action,
        byte x,
        byte y,
        byte phase,
        byte ground) =>
        new(
            ActionState: action,
            X: x,
            Y: y,
            StatePhase: phase,
            GroundDescriptor: ground,
            DecisionTimer: 5,
            FlagsFacing: 0x40,
            Type: 0x05,
            TerrainProbeRight: 0,
            TerrainProbeLeft: 0);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Common entity $50 self-test failed: {label}");
    }
}

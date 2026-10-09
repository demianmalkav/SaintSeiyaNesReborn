using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class CommonEntityActiveDispatcherChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckExistingJumpContinuesWithoutDecision();
        CheckJumpLandingCanReturnReadyForInteraction();
        CheckTerminal40Returns10ButStillSkipsInteraction();
        Check50LandingReturns10ButStillSkipsInteraction();
    }

    private static void CheckExistingJumpContinuesWithoutDecision()
    {
        var result = PlatformCommonEntityActiveDispatcher.Step(
            State(action: 0x31, x: 0x40, y: 0x58, phase: 2, ground: 0xE0),
            playerX: 0x70,
            playerY: 0x50,
            playerJumpPhase49: 0,
            entropy48: 0x3F,
            frameCounter3C: 1,
            cameraDelta43: 0);

        Require(result.Route == PlatformCommonEntityActiveRoute.Jump30,
            "entry $31 routes directly through active jump family");
        Require(result.Jump30!.Value.Vertical.TableIndexUsed == 1,
            "existing phase2 jump advances to phase3 and consumes table index1");
        Require(result.State.Y == 0x50,
            "second +8 source sample moves screen Y upward by 8");
        Require(result.State.X == 0x41,
            "$31 active jump applies +1 horizontal step at odd frame");
        Require(result.Continuation == PlatformCommonEntityActiveContinuation.ReadyForInteraction,
            "ongoing jump survives to ordinary interaction gate");
    }

    private static void CheckJumpLandingCanReturnReadyForInteraction()
    {
        var result = PlatformCommonEntityActiveDispatcher.Step(
            State(action: 0x31, x: 0x40, y: 0x62, phase: 0x10, ground: 0xA8),
            playerX: 0x20,
            playerY: 0x50,
            playerJumpPhase49: 0,
            entropy48: 0,
            frameCounter3C: 1,
            cameraDelta43: 0);

        Require(result.Jump30!.Value.Vertical.Landed,
            "active jump calls shared C491 before advancing phase");
        Require(result.State.ActionState == 0x10 && result.State.StatePhase == 0,
            "landing returns common entity to $10/phase0");
        Require(result.State.Y == 0x60,
            "landing snaps Y before post-landing horizontal work");
        Require(result.State.X == 0x41,
            "same update continues through ordinary facing movement after landing");
        Require(result.Continuation == PlatformCommonEntityActiveContinuation.ReadyForInteraction,
            "jump route still reaches interaction after same-frame landing");
    }

    private static void CheckTerminal40Returns10ButStillSkipsInteraction()
    {
        var result = PlatformCommonEntityActiveDispatcher.Step(
            State(action: 0x4F, x: 0x50, y: 0x50, phase: 0x41, ground: 0xE0),
            playerX: 0x50,
            playerY: 0x50,
            playerJumpPhase49: 0,
            entropy48: 0,
            frameCounter3C: 1,
            cameraDelta43: 0);

        Require(result.Route == PlatformCommonEntityActiveRoute.HitReaction40,
            "$4F uses reaction route");
        Require(result.State.ActionState == 0x10,
            "terminal reaction stores ordinary $10 for next frame");
        Require(result.State.X == 0x54,
            "terminal reaction still applies remaining +4 knockback");
        Require(result.Continuation == PlatformCommonEntityActiveContinuation.SkipInteraction,
            "control path skips $9915/$98BA despite final action byte being $10");
    }

    private static void Check50LandingReturns10ButStillSkipsInteraction()
    {
        var result = PlatformCommonEntityActiveDispatcher.Step(
            State(action: 0x50, x: 0x50, y: 0x5F, phase: 3, ground: 0xA8),
            playerX: 0x50,
            playerY: 0x50,
            playerJumpPhase49: 0,
            entropy48: 0,
            frameCounter3C: 1,
            cameraDelta43: 0);

        Require(result.Route == PlatformCommonEntityActiveRoute.Fall50,
            "$50 routes through fall family");
        Require(result.Fall50!.Value.Outcome == PlatformCommonEntityFall50Outcome.Landed,
            "$50 +3 fall lands via C491");
        Require(result.State.ActionState == 0x10 && result.State.Y == 0x60,
            "landing stores next-frame ordinary state");
        Require(result.Continuation == PlatformCommonEntityActiveContinuation.SkipInteraction,
            "$50 control path jumps to renderer even after same-frame landing");
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
            throw new InvalidOperationException($"Common entity active dispatcher self-test failed: {label}");
    }
}

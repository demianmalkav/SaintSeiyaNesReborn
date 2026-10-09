using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class CommonEntityHitReaction40Checks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckRightKnockback();
        CheckLeftKnockback();
        CheckFrameStartCameraPrecedesReaction();
        CheckExhaustedKnockbackKeepsReactionTimer();
        CheckTerminalReactionStillAppliesLastMotionTick();
    }

    private static void CheckRightKnockback()
    {
        var result = PlatformCommonEntityHitReaction40.AdvanceAfterPath(State(action: 0x40, x: 0x50, motion3: 0x48));
        Require(result.State.ActionState == 0x41, "$40 action advances to $41");
        Require(result.State.StatePhase == 0x47, "right knockback counter decrements before movement");
        Require(result.State.X == 0x54, "remaining $40 bit selects +4 knockback");
        Require(result.HorizontalDeltaApplied == 4 && result.KnockbackAdvanced,
            "right knockback reports +4 tick");
        Require(!result.CompletedReaction, "$40 first tick does not complete reaction");
        Require(result.ScreenXDeltaFromCamera == 0,
            "post-interaction reaction phase does not repeat camera correction");
    }

    private static void CheckLeftKnockback()
    {
        var result = PlatformCommonEntityHitReaction40.AdvanceAfterPath(State(action: 0x40, x: 0x50, motion3: 0x08));
        Require(result.State.ActionState == 0x41, "left reaction action also advances");
        Require(result.State.StatePhase == 0x07, "left knockback counter decrements 8->7");
        Require(result.State.X == 0x4C, "remaining value below $40 selects -4 knockback");
        Require(result.HorizontalDeltaApplied == -4, "left knockback reports -4 tick");
    }

    private static void CheckFrameStartCameraPrecedesReaction()
    {
        var result = PlatformCommonEntityHitReaction40.Step(
            State(action: 0x40, x: 0x50, motion3: 0x48),
            cameraDelta43: 1);

        Require(result.Outcome == PlatformCommonEntityHitReaction40Outcome.Active,
            "frame-start $40 remains active after ordinary reaction tick");
        Require(result.ScreenXDeltaFromCamera == -1,
            "frame-start $40 consumes camera delta before A79E");
        Require(result.State.X == 0x53,
            "frame-start X $50 -> camera $4F -> recoil +4 = $53");
        Require(result.State.ActionState == 0x41 && result.State.StatePhase == 0x47,
            "camera correction does not change reaction/recoil cadence");
    }

    private static void CheckExhaustedKnockbackKeepsReactionTimer()
    {
        var result = PlatformCommonEntityHitReaction40.Step(State(action: 0x47, x: 0x50, motion3: 0x40));
        Require(result.State.ActionState == 0x48, "reaction timer continues after motion low nibble is zero");
        Require(result.State.StatePhase == 0x40 && result.State.X == 0x50,
            "zero low nibble leaves motion byte/X untouched");
        Require(!result.KnockbackAdvanced && result.HorizontalDeltaApplied == 0,
            "no knockback occurs after short motion counter is exhausted");
    }

    private static void CheckTerminalReactionStillAppliesLastMotionTick()
    {
        var result = PlatformCommonEntityHitReaction40.Step(State(action: 0x4F, x: 0x50, motion3: 0x41));
        Require(result.CompletedReaction, "$4F update completes 16-state reaction family");
        Require(result.State.ActionState == 0x10, "type<8 terminal reaction returns to ordinary $10");
        Require(result.State.StatePhase == 0x40, "terminal frame still decrements +$03 counter");
        Require(result.State.X == 0x54, "terminal frame still applies +4 before renderer path");
    }

    private static PlatformCommonEntityMotionState State(byte action, byte x, byte motion3) =>
        new(
            ActionState: action,
            X: x,
            Y: 0x50,
            StatePhase: motion3,
            GroundDescriptor: 0xE0,
            DecisionTimer: 5,
            FlagsFacing: 0x40,
            Type: 0x05,
            TerrainProbeRight: 0,
            TerrainProbeLeft: 0);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Common entity $40 self-test failed: {label}");
    }
}

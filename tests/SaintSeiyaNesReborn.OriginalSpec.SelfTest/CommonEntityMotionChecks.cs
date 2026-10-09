using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class CommonEntityMotionChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        Require(PlatformCommonEntityMotion.HorizontalStep(0x01, 0) == 2, "ordinary entity moves 2 px when frame mod4 is zero");
        Require(PlatformCommonEntityMotion.HorizontalStep(0x01, 1) == 1, "ordinary entity moves 1 px on other frames");
        Require(PlatformCommonEntityMotion.HorizontalStep(0x0A, 0) == 0, "type 0A/0B cadence can be zero");
        Require(PlatformCommonEntityMotion.HorizontalStep(0x0B, 1) == 1, "type 0A/0B cadence alternates to one");

        Require(PlatformCommonEntityMotion.ReseedDecisionTimer(0) == 31, "decision timer minimum is 31");
        Require(PlatformCommonEntityMotion.ReseedDecisionTimer(0x3F) == 94, "decision timer maximum is 94");
        Require(PlatformCommonEntityMotion.ReseedDecisionTimer(0xFF) == 94, "decision timer masks entropy to six bits");

        Require(PlatformCommonEntityMotion.TerrainForcesTurn(1, 0x00, 0x88), "left-facing entity turns on $88 family");
        Require(PlatformCommonEntityMotion.TerrainForcesTurn(1, 0x40, 0x80), "right-facing entity turns on $80 family");
        Require(PlatformCommonEntityMotion.TerrainForcesTurn(1, 0x40, 0xE0), "right-facing entity turns on $E0 family");
        Require(!PlatformCommonEntityMotion.TerrainForcesTurn(1, 0x40, 0x90), "$90 family does not turn ordinary entity");
        Require(!PlatformCommonEntityMotion.TerrainForcesTurn(7, 0x40, 0xE4), "type 7 bypasses $E4+ normal turn response");
        Require(PlatformCommonEntityMotion.ToggleFacing(0x00) == 0x40, "toggle facing sets right bit");
        Require(PlatformCommonEntityMotion.ToggleFacing(0x40) == 0x00, "toggle facing clears right bit");

        Require(PlatformCommonEntityMotion.JumpHorizontalDelta(0x31, 1, 0) == 2, "$31 jump moves right by current entity cadence");
        Require(PlatformCommonEntityMotion.JumpHorizontalDelta(0x32, 1, 0) == -2, "$32 jump moves left by current entity cadence");
        Require(PlatformCommonEntityMotion.JumpHorizontalDelta(0x30, 1, 0) == 0, "other $3x jump state has no confirmed common horizontal delta");

        var metrics = PlatformCommonEntityMotion.JumpMetrics();
        Require(metrics.TableUpdates == 30, "C5E6 consumes 30 effective table entries");
        Require(metrics.MaxAscentPixels == 58, "entity jump maximum ascent is 58 pixels");
        Require(metrics.ApexUpdate == 14, "first maximum ascent is reached on table update 14");
        Require(metrics.NetScreenYDelta == -35, "effective table phase ends 35 pixels above takeoff before terminal fall");
        Require(PlatformCommonEntityMotion.JumpSource.Count == 30, "unused 31st raw table byte is not exposed as an effective update");

        var first = PlatformCommonEntityMotion.StepJumpVertical(State(action: 0x31, y: 0x80, phase: 1));
        Require(first.State.StatePhase == 2, "phase 1 increments to 2 before table lookup");
        Require(first.TableIndexUsed == 0, "phase 2 consumes table index zero");
        Require(first.ScreenYDeltaApplied == -8, "first entity jump update rises eight pixels");
        Require(first.State.Y == 0x78, "first entity jump update subtracts eight from screen Y");

        var falling = PlatformCommonEntityMotion.StepJumpVertical(State(action: 0x31, y: 0x60, phase: 31));
        Require(falling.State.StatePhase == 32, "phase 31 advances to terminal phase 32");
        Require(falling.UsedTerminalFall, "next phase 32 uses fixed fall branch");
        Require(falling.State.Y == 0x63 && falling.ScreenYDeltaApplied == 3, "terminal entity fall adds three pixels");

        // Landing check begins from CURRENT phase $10, before increment/table work.
        var land = PlatformCommonEntityMotion.StepJumpVertical(
            State(action: 0x31, y: 0x53, phase: 0x10, ground: 0xA8, type: 1));
        Require(land.Landed, "$A8 descriptor lands ordinary type when low Y nibble < 6");
        Require(land.State.Y == 0x50, "ordinary entity landing snaps to 16-pixel row");
        Require(land.State.StatePhase == 0, "landing clears state phase");
        Require(land.State.ActionState == 0x10, "ordinary entity landing returns to locomotion $10");
        Require(land.ScreenYDeltaApplied == -3, "landing reports row snap delta");

        var tooLowDescriptor = PlatformCommonEntityMotion.StepJumpVertical(
            State(action: 0x31, y: 0x53, phase: 0x10, ground: 0xA7, type: 1));
        Require(!tooLowDescriptor.Landed, "descriptor below $A8 cannot land entity");
        Require(tooLowDescriptor.State.StatePhase == 0x11, "failed landing continues phase progression");
        Require(tooLowDescriptor.State.Y == 0x53, "phase $11 consumes zero-valued table entry here");

        var upper = PlatformCommonEntityMotion.StepJumpVertical(
            State(action: 0x31, y: 0x5B, phase: 0x10, ground: 0xF0, type: 0x08));
        Require(upper.Landed, "$F0+ descriptor lands when low Y nibble >= 8");
        Require(upper.State.Y == 0x58, "$F0+ landing snaps to row+8");
        Require(upper.State.ActionState == 0x00, "type $08 landing returns to neutral instead of $10");

        var upperTooEarly = PlatformCommonEntityMotion.StepJumpVertical(
            State(action: 0x31, y: 0x57, phase: 0x10, ground: 0xF0, type: 0x09));
        Require(!upperTooEarly.Landed, "$F0+ descriptor rejects low nibble below 8");

        var lowerRegion = PlatformCommonEntityMotion.StepJumpVertical(
            State(action: 0x31, y: 0x86, phase: 0x10, ground: 0xF0, type: 1));
        Require(!lowerRegion.Landed, "entity Y $86+ bypasses C491 landing");

        // Phase $0F does not call C491 yet, even if terrain is landable.
        var preLandingHalf = PlatformCommonEntityMotion.StepJumpVertical(
            State(action: 0x31, y: 0x53, phase: 0x0F, ground: 0xA8, type: 1));
        Require(!preLandingHalf.Landed, "phase $0F does not attempt landing");
        Require(preLandingHalf.State.StatePhase == 0x10, "phase $0F only advances into landing-check half");
    }

    private static PlatformCommonEntityMotionState State(
        byte action,
        byte y,
        byte phase,
        byte ground = 0,
        byte type = 1) =>
        new(
            ActionState: action,
            X: 0x80,
            Y: y,
            StatePhase: phase,
            GroundDescriptor: ground,
            DecisionTimer: 40,
            FlagsFacing: 0x40,
            Type: type,
            TerrainProbeRight: 0,
            TerrainProbeLeft: 0);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Common entity motion self-test failed: {label}");
    }
}

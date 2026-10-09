using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class CommonEntityDecisionChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        var pending = PlatformCommonEntityDecision.Step(
            State(type: 1, x: 0x40, y: 0x60, timer: 2, facing: 0x40),
            playerX: 0x80,
            playerY: 0x60,
            playerJumpPhase49: 0,
            entropy48: 0);
        Require(pending.Outcome == PlatformEntityDecisionOutcome.TimerPending, "timer 2 only decrements to one");
        Require(pending.State.DecisionTimer == 1, "pending timer stores decremented value");
        Require(!pending.DecisionTimerReseeded, "pending timer does not reseed");

        var zeroWrap = PlatformCommonEntityDecision.Step(
            State(type: 1, x: 0x40, y: 0x60, timer: 0, facing: 0x40),
            playerX: 0x80,
            playerY: 0x60,
            playerJumpPhase49: 0,
            entropy48: 0);
        Require(zeroWrap.State.DecisionTimer == 0xFF, "raw timer zero wraps to FF under SEC/SBC #1");
        Require(zeroWrap.Outcome == PlatformEntityDecisionOutcome.TimerPending, "wrapped FF does not trigger decision");

        // Entity left of player and facing toward the player (right): timer expiry
        // immediately starts jump-right $31.
        var jumpRight = PlatformCommonEntityDecision.Step(
            State(type: 1, x: 0x40, y: 0x60, timer: 1, facing: 0x40),
            playerX: 0x80,
            playerY: 0x60,
            playerJumpPhase49: 0,
            entropy48: 0x3F);
        Require(jumpRight.DecisionTimerReseeded, "expired common timer reseeds before decision");
        Require(jumpRight.State.DecisionTimer == 94, "expired timer reseeds from masked entropy");
        Require(jumpRight.Outcome == PlatformEntityDecisionOutcome.JumpStartedRight, "left-of-player/right-facing entity starts $31");
        Require(jumpRight.State.ActionState == 0x31 && jumpRight.State.StatePhase == 1, "jump-right initializes action and phase");

        // Entity right of player and facing left: under 32 px chooses $32.
        var jumpLeft = PlatformCommonEntityDecision.Step(
            State(type: 1, x: 0x60, y: 0x60, timer: 1, facing: 0x00),
            playerX: 0x50,
            playerY: 0x60,
            playerJumpPhase49: 0,
            entropy48: 0);
        Require(jumpLeft.Outcome == PlatformEntityDecisionOutcome.JumpStartedLeft, "right-of-player/left-facing near entity starts $32");
        Require(jumpLeft.State.ActionState == 0x32 && jumpLeft.State.StatePhase == 1, "$32 decision initializes phase one");

        // The same branch at distance >=32 selects $31, unless X has reached C0+.
        var farRight = PlatformCommonEntityDecision.Step(
            State(type: 1, x: 0x80, y: 0x60, timer: 1, facing: 0x00),
            playerX: 0x50,
            playerY: 0x60,
            playerJumpPhase49: 0,
            entropy48: 0);
        Require(farRight.Outcome == PlatformEntityDecisionOutcome.JumpStartedRight, "distance >=32 selects $31 in A9DC branch");

        var rightCutoff = PlatformCommonEntityDecision.Step(
            State(type: 1, x: 0xC0, y: 0x60, timer: 1, facing: 0x00),
            playerX: 0x50,
            playerY: 0x60,
            playerJumpPhase49: 0,
            entropy48: 0);
        Require(rightCutoff.Outcome == PlatformEntityDecisionOutcome.TimerExpiredNoChange, "entity X C0+ suppresses A9DC jump selection");
        Require(rightCutoff.State.ActionState == 0x10, "C0+ cutoff leaves action unchanged");

        // Moving away: entity is right of player but faces right. When entity is
        // above the player and outside playerX+$30, A9AC toggles facing.
        var awayToggle = PlatformCommonEntityDecision.Step(
            State(type: 1, x: 0x80, y: 0x40, timer: 1, facing: 0x40),
            playerX: 0x40,
            playerY: 0x60,
            playerJumpPhase49: 0,
            entropy48: 0);
        Require(awayToggle.Outcome == PlatformEntityDecisionOutcome.FacingToggled, "away-facing entity outside wrapped window toggles direction");
        Require(awayToggle.State.FlagsFacing == 0x00, "toggle clears right-facing bit");

        // Same branch can explicitly keep facing: playerX+$30 >= entityX, then
        // wrapped lower boundary is below entityX.
        var awayNoChange = PlatformCommonEntityDecision.Step(
            State(type: 1, x: 0x60, y: 0x40, timer: 1, facing: 0x40),
            playerX: 0x40,
            playerY: 0x60,
            playerJumpPhase49: 0,
            entropy48: 0);
        Require(awayNoChange.Outcome == PlatformEntityDecisionOutcome.TimerExpiredNoChange, "entity inside A9AC wrapped window keeps facing");
        Require(awayNoChange.State.FlagsFacing == 0x40, "no-change branch preserves facing");

        // With player grounded, an entity not above the player exits before the
        // horizontal wrapped-window comparisons.
        var verticalReject = PlatformCommonEntityDecision.Step(
            State(type: 1, x: 0x80, y: 0x61, timer: 1, facing: 0x40),
            playerX: 0x40,
            playerY: 0x60,
            playerJumpPhase49: 0,
            entropy48: 0);
        Require(verticalReject.Outcome == PlatformEntityDecisionOutcome.TimerExpiredNoChange, "grounded player vertical gate can suppress away-facing decision");

        // Nonzero player jump phase bypasses the entityY-1 >= playerY rejection.
        var airbornePlayerBypass = PlatformCommonEntityDecision.Step(
            State(type: 1, x: 0x80, y: 0x61, timer: 1, facing: 0x40),
            playerX: 0x40,
            playerY: 0x60,
            playerJumpPhase49: 4,
            entropy48: 0);
        Require(airbornePlayerBypass.Outcome == PlatformEntityDecisionOutcome.FacingToggled, "player jump phase bypasses grounded vertical rejection");

        // $07/$0A/$0B skip the decision timer entirely and use terrain probes.
        var type7Turn = PlatformCommonEntityDecision.Step(
            State(type: 0x07, x: 0x40, y: 0x60, timer: 55, facing: 0x40, rightProbe: 0x80),
            playerX: 0,
            playerY: 0,
            playerJumpPhase49: 0,
            entropy48: 0);
        Require(type7Turn.Outcome == PlatformEntityDecisionOutcome.TerrainTurned, "type 7 goes directly to terrain turn path");
        Require(type7Turn.State.DecisionTimer == 55, "terrain-only type does not consume decision timer");
        Require(type7Turn.State.FlagsFacing == 0, "terrain turn toggles type7 facing");

        var type7E4 = PlatformCommonEntityDecision.Step(
            State(type: 0x07, x: 0x40, y: 0x60, timer: 55, facing: 0x40, rightProbe: 0xE4),
            playerX: 0,
            playerY: 0,
            playerJumpPhase49: 0,
            entropy48: 0);
        Require(type7E4.Outcome == PlatformEntityDecisionOutcome.TerrainNoTurn, "type7 E4+ exception bypasses turn");

        var type0A = PlatformCommonEntityDecision.Step(
            State(type: 0x0A, x: 0x40, y: 0x60, timer: 22, facing: 0x00, leftProbe: 0x88),
            playerX: 0,
            playerY: 0,
            playerJumpPhase49: 0,
            entropy48: 0);
        Require(type0A.Outcome == PlatformEntityDecisionOutcome.TerrainTurned, "type0A uses left terrain probe when facing left");

        // Other types >=08 bypass this common decision path entirely.
        var type08 = PlatformCommonEntityDecision.Step(
            State(type: 0x08, x: 0x40, y: 0x60, timer: 1, facing: 0x40),
            playerX: 0x80,
            playerY: 0x60,
            playerJumpPhase49: 0,
            entropy48: 0);
        Require(type08.Outcome == PlatformEntityDecisionOutcome.NoCommonDecisionPath, "type08 bypasses A98B/AA1F common decision path");
        Require(type08.State.DecisionTimer == 1, "type08 bypass leaves timer untouched");
    }

    private static PlatformCommonEntityMotionState State(
        byte type,
        byte x,
        byte y,
        byte timer,
        byte facing,
        byte rightProbe = 0,
        byte leftProbe = 0) =>
        new(
            ActionState: 0x10,
            X: x,
            Y: y,
            StatePhase: 0,
            GroundDescriptor: 0,
            DecisionTimer: timer,
            FlagsFacing: facing,
            Type: type,
            TerrainProbeRight: rightProbe,
            TerrainProbeLeft: leftProbe);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Common entity decision self-test failed: {label}");
    }
}

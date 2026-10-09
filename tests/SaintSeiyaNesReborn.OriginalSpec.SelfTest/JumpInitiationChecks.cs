using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class JumpInitiationChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        var neutral = new PlatformJumpInitiationState(
            ActionState4D: 0,
            JumpPhase49: 0,
            JumpButtonLatch4A: 0,
            HighJumpSelector038A: 0,
            Support038D: 0);

        var standing = PlatformJumpInitiation.Step(neutral, PlatformInput.A);
        Require(standing.StartedJump, "A starts standing jump");
        Require(standing.State.ActionState4D == 0x30, "standing jump action $30");
        Require(standing.State.JumpPhase49 == 1, "standing jump phase starts at 1");
        Require(standing.State.JumpButtonLatch4A == 1, "standing jump latches A");
        Require(standing.State.HighJumpSelector038A == 0, "ordinary vertical jump keeps high selector zero");
        Require(standing.MustRunAirborneStepThisFrame, "successful jump requires same-frame airborne step");

        var high = PlatformJumpInitiation.Step(neutral, PlatformInput.A | PlatformInput.Up);
        Require(high.StartedJump, "Up+A starts high jump");
        Require(high.State.ActionState4D == 0x30, "high jump remains action $30");
        Require(high.State.HighJumpSelector038A == 0x30, "high jump selects $038A=$30");

        var right = PlatformJumpInitiation.Step(neutral, PlatformInput.A | PlatformInput.Right);
        Require(right.State.ActionState4D == 0x31, "A+Right starts $31 trajectory");
        Require(right.State.HighJumpSelector038A == 0, "directional jump ignores high selector");

        var left = PlatformJumpInitiation.Step(neutral, PlatformInput.A | PlatformInput.Left);
        Require(left.State.ActionState4D == 0x32, "A+Left starts $32 trajectory");

        var both = PlatformJumpInitiation.Step(neutral, PlatformInput.A | PlatformInput.Left | PlatformInput.Right);
        Require(both.State.ActionState4D == 0x33, "A+Left+Right records $33 takeoff quirk");

        var upRight = PlatformJumpInitiation.Step(neutral, PlatformInput.A | PlatformInput.Up | PlatformInput.Right);
        Require(upRight.State.ActionState4D == 0x31, "directional bits take precedence over Up");
        Require(upRight.State.HighJumpSelector038A == 0, "Up+Right does not select high-jump table");

        var blockedDown = PlatformJumpInitiation.Step(neutral, PlatformInput.A | PlatformInput.Down);
        Require(!blockedDown.StartedJump && blockedDown.BlockedByDown, "Down blocks jump start");
        Require(blockedDown.State.JumpButtonLatch4A == 0xFF, "Down-blocked press latches $FF");

        var supportState = neutral with { Support038D = 1 };
        var blockedSupport = PlatformJumpInitiation.Step(supportState, PlatformInput.A);
        Require(!blockedSupport.StartedJump && blockedSupport.BlockedBySupport, "$038D blocks jump start");
        Require(blockedSupport.State.JumpButtonLatch4A == 0xFF, "support-blocked press latches $FF");

        var heldBlocked = PlatformJumpInitiation.Step(blockedSupport.State, PlatformInput.A);
        Require(!heldBlocked.StartedJump, "held A cannot retry while $FF latch remains");
        Require(heldBlocked.State.JumpButtonLatch4A == 0xFF, "held blocked latch persists");

        var released = PlatformJumpInitiation.Step(blockedSupport.State, PlatformInput.None);
        Require(released.ReleasedLatch, "A release reports latch release");
        Require(released.State.JumpButtonLatch4A == 0, "A release clears jump latch");

        var airborneHeld = neutral with { JumpPhase49 = 8, JumpButtonLatch4A = 1, ActionState4D = 0x31 };
        var noRestart = PlatformJumpInitiation.Step(airborneHeld, PlatformInput.A | PlatformInput.Right);
        Require(!noRestart.StartedJump, "active jump cannot restart while A held");
        Require(noRestart.State == airborneHeld, "active held jump preserves jump-init state");

        var airborneRelease = PlatformJumpInitiation.Step(airborneHeld, PlatformInput.Right);
        Require(airborneRelease.State.JumpPhase49 == 8, "A release during jump preserves phase");
        Require(airborneRelease.State.ActionState4D == 0x31, "A release during jump preserves action");
        Require(airborneRelease.State.JumpButtonLatch4A == 0, "A release during jump clears only latch");
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Jump initiation self-test failed: {label}");
    }
}

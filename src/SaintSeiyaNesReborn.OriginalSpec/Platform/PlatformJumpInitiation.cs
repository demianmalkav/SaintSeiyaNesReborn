namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformJumpInitiationState(
    byte ActionState4D,
    byte JumpPhase49,
    byte JumpButtonLatch4A,
    byte HighJumpSelector038A,
    byte Support038D);

public readonly record struct PlatformJumpInitiationResult(
    PlatformJumpInitiationState State,
    bool StartedJump,
    bool ReleasedLatch,
    bool BlockedByDown,
    bool BlockedBySupport)
{
    public bool MustRunAirborneStepThisFrame => StartedJump;
}

/// <summary>
/// The A-button/jump portion of PRG bank 3 $BB76-$BBCA.
/// B-button attack handling begins at $BBCA and is intentionally outside this class.
///
/// A successful jump writes $49=1 and $4D=$30-$33, then the caller immediately
/// dispatches to $BCD3 in the same platform frame. Consumers must therefore run
/// PlatformAirborneSession immediately when StartedJump is true.
/// </summary>
public static class PlatformJumpInitiation
{
    public static PlatformJumpInitiationResult Step(
        PlatformJumpInitiationState state,
        PlatformInput input)
    {
        var aPressed = (input & PlatformInput.A) != 0;

        // $BB76: when A is released, the jump button latch is cleared regardless
        // of whether a jump is already in progress. Execution then continues to
        // the separate B-button path.
        if (!aPressed)
        {
            var released = state.JumpButtonLatch4A != 0;
            return new(
                state with { JumpButtonLatch4A = 0 },
                StartedJump: false,
                ReleasedLatch: released,
                BlockedByDown: false,
                BlockedBySupport: false);
        }

        // Holding A cannot restart a jump while $49 is non-zero or while the latch
        // remains set from the previous press/block.
        if (state.JumpPhase49 != 0 || state.JumpButtonLatch4A != 0)
            return Unchanged(state);

        // Down or non-zero $038D rejects the jump and latches $FF until A release.
        if ((input & PlatformInput.Down) != 0)
        {
            return new(
                state with { JumpButtonLatch4A = 0xFF },
                StartedJump: false,
                ReleasedLatch: false,
                BlockedByDown: true,
                BlockedBySupport: false);
        }

        if (state.Support038D != 0)
        {
            return new(
                state with { JumpButtonLatch4A = 0xFF },
                StartedJump: false,
                ReleasedLatch: false,
                BlockedByDown: false,
                BlockedBySupport: true);
        }

        var horizontalBits = (byte)input & 0x03;
        var action = horizontalBits == 0
            ? (byte)0x30
            : (byte)(0x30 + horizontalBits);

        // Up selects the high-jump table only for a purely vertical takeoff.
        // If any Left/Right bit was present, the directional state wins and $038A
        // remains zero even when Up is also held.
        var highSelector = horizontalBits == 0 && (input & PlatformInput.Up) != 0
            ? (byte)0x30
            : (byte)0x00;

        var next = state with
        {
            ActionState4D = action,
            JumpPhase49 = 1,
            JumpButtonLatch4A = 1,
            HighJumpSelector038A = highSelector,
        };

        return new(
            next,
            StartedJump: true,
            ReleasedLatch: false,
            BlockedByDown: false,
            BlockedBySupport: false);
    }

    private static PlatformJumpInitiationResult Unchanged(PlatformJumpInitiationState state) =>
        new(state, false, false, false, false);
}

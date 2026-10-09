namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformCommonEntityDropE0Outcome
{
    Falling,
    RemovedLowerBand,
}

public readonly record struct PlatformCommonEntityDropE0Result(
    PlatformCommonEntityMotionState State,
    PlatformCommonEntityDropE0Outcome Outcome,
    int ScreenYDelta,
    int ScreenXDeltaFromCamera);

/// <summary>
/// Exact common-entity $E0 drop-reaction path through bank-3 $A57E-$A5B8.
///
/// Like $50, it subtracts camera delta from screen X and falls +3 Y. Unlike
/// $50, family $E0 explicitly skips fixed $C491 landing; it therefore continues
/// falling until the post-step Y reaches $B0 or beyond, at which point the
/// record is removed. The path also bypasses $9915/$98BA for the current frame.
/// </summary>
public static class PlatformCommonEntityDropE0
{
    public static PlatformCommonEntityDropE0Result Step(
        PlatformCommonEntityMotionState state,
        byte cameraDelta43)
    {
        if (state.Type >= 0x08)
            throw new ArgumentOutOfRangeException(nameof(state), state.Type, "$E0 common drop helper covers entity types $00-$07.");
        if ((state.ActionState & 0xF0) != 0xE0)
            throw new InvalidOperationException($"$E0 drop helper requires action family $E0, got ${state.ActionState:X2}.");

        state = state with
        {
            X = unchecked((byte)(state.X - cameraDelta43)),
            Y = unchecked((byte)(state.Y + 3)),
        };

        if (state.Y >= 0xB0)
        {
            return new PlatformCommonEntityDropE0Result(
                state,
                PlatformCommonEntityDropE0Outcome.RemovedLowerBand,
                ScreenYDelta: 3,
                ScreenXDeltaFromCamera: -cameraDelta43);
        }

        return new PlatformCommonEntityDropE0Result(
            state,
            PlatformCommonEntityDropE0Outcome.Falling,
            ScreenYDelta: 3,
            ScreenXDeltaFromCamera: -cameraDelta43);
    }
}

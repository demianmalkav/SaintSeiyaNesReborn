namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformCommonEntityFall50Outcome
{
    Falling,
    Landed,
    RemovedLowerBand,
}

public readonly record struct PlatformCommonEntityFall50Result(
    PlatformCommonEntityMotionState State,
    PlatformCommonEntityFall50Outcome Outcome,
    int ScreenYDelta,
    int ScreenXDeltaFromCamera);

/// <summary>
/// Exact common entity $50 path at bank 3 $A57E-$A5B8 for types $00-$07.
/// The path scrolls X by -$43, falls +3 Y, removes at Y >= $B0, otherwise
/// calls fixed $C491 and then bypasses ordinary projectile/contact interaction.
/// </summary>
public static class PlatformCommonEntityFall50
{
    public static PlatformCommonEntityFall50Result Step(
        PlatformCommonEntityMotionState state,
        byte cameraDelta43)
    {
        if (state.Type >= 0x08)
            throw new ArgumentOutOfRangeException(nameof(state), state.Type, "$50 common fall helper covers entity types $00-$07.");
        if ((state.ActionState & 0xF0) != 0x50)
            throw new InvalidOperationException($"$50 fall helper requires action family $50, got ${state.ActionState:X2}.");

        state = state with
        {
            X = unchecked((byte)(state.X - cameraDelta43)),
            Y = unchecked((byte)(state.Y + 3)),
        };

        if (state.Y >= 0xB0)
        {
            return new PlatformCommonEntityFall50Result(
                state,
                PlatformCommonEntityFall50Outcome.RemovedLowerBand,
                ScreenYDelta: 3,
                ScreenXDeltaFromCamera: -cameraDelta43);
        }

        var landing = PlatformCommonEntityLanding.Resolve(state);
        if (landing.Landed)
        {
            return new PlatformCommonEntityFall50Result(
                landing.State,
                PlatformCommonEntityFall50Outcome.Landed,
                ScreenYDelta: 3 + landing.ScreenYDelta,
                ScreenXDeltaFromCamera: -cameraDelta43);
        }

        return new PlatformCommonEntityFall50Result(
            state,
            PlatformCommonEntityFall50Outcome.Falling,
            ScreenYDelta: 3,
            ScreenXDeltaFromCamera: -cameraDelta43);
    }
}

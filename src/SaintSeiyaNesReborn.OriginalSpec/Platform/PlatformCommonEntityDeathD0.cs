namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformCommonEntityDeathD0Outcome
{
    Active,
    RemovedHorizontal,
    RemovedVerticalBand,
    CompletedRemoval,
}

public readonly record struct PlatformCommonEntityDeathD0Result(
    PlatformCommonEntityMotionState State,
    PlatformCommonEntityDeathD0Outcome Outcome,
    bool DeathPhaseAdvanced,
    int ScreenYDelta,
    int ScreenXDeltaFromCamera);

/// <summary>
/// Common type-$00-$07 death/removal family around $A5BE/$A7FB-$A839.
///
/// A frame that ENTERS in $D0 first receives the shared camera/removal path.
/// A projectile kill can also write $D0 during $9915; control then reaches
/// $A7FB in the SAME frame, so death cadence may advance immediately without a
/// second camera correction. The two entry modes are exposed separately.
/// </summary>
public static class PlatformCommonEntityDeathD0
{
    public static PlatformCommonEntityDeathD0Result Step(
        PlatformCommonEntityMotionState state,
        byte frameCounter3C,
        byte cameraDelta43)
    {
        Validate(state);

        state = state with
        {
            X = unchecked((byte)(state.X - cameraDelta43)),
        };

        if (state.X >= 0xF8)
        {
            return new(
                state,
                PlatformCommonEntityDeathD0Outcome.RemovedHorizontal,
                DeathPhaseAdvanced: false,
                ScreenYDelta: 0,
                ScreenXDeltaFromCamera: -cameraDelta43);
        }

        if (state.Y is >= 0xB0 and < 0xC0)
        {
            return new(
                state,
                PlatformCommonEntityDeathD0Outcome.RemovedVerticalBand,
                DeathPhaseAdvanced: false,
                ScreenYDelta: 0,
                ScreenXDeltaFromCamera: -cameraDelta43);
        }

        var advanced = AdvanceAfterPath(state, frameCounter3C);
        return advanced with { ScreenXDeltaFromCamera = -cameraDelta43 };
    }

    /// <summary>
    /// $A7FB-$A839 only. Used after $9915 has just created $D0 in the current
    /// interaction; the entity already passed the earlier camera/removal path.
    /// </summary>
    public static PlatformCommonEntityDeathD0Result AdvanceAfterPath(
        PlatformCommonEntityMotionState state,
        byte frameCounter3C)
    {
        Validate(state);

        if ((frameCounter3C & 3) != 0)
        {
            return new(
                state,
                PlatformCommonEntityDeathD0Outcome.Active,
                DeathPhaseAdvanced: false,
                ScreenYDelta: 0,
                ScreenXDeltaFromCamera: 0);
        }

        var yDelta = 0;
        if (state.GroundDescriptor < 0x80 || state.GroundDescriptor >= 0xF0)
        {
            state = state with { Y = unchecked((byte)(state.Y + 2)) };
            yDelta = 2;
        }

        var nextAction = unchecked((byte)(state.ActionState + 1));
        if (nextAction >= 0xE0)
        {
            state = state with { ActionState = 0 };
            return new(
                state,
                PlatformCommonEntityDeathD0Outcome.CompletedRemoval,
                DeathPhaseAdvanced: true,
                ScreenYDelta: yDelta,
                ScreenXDeltaFromCamera: 0);
        }

        state = state with { ActionState = nextAction };
        return new(
            state,
            PlatformCommonEntityDeathD0Outcome.Active,
            DeathPhaseAdvanced: true,
            ScreenYDelta: yDelta,
            ScreenXDeltaFromCamera: 0);
    }

    private static void Validate(PlatformCommonEntityMotionState state)
    {
        if (state.Type >= 0x08)
            throw new ArgumentOutOfRangeException(nameof(state), state.Type, "$D0 common death helper covers entity types $00-$07.");
        if ((state.ActionState & 0xF0) != 0xD0)
            throw new InvalidOperationException($"$D0 helper requires action family $D0, got ${state.ActionState:X2}.");
    }
}

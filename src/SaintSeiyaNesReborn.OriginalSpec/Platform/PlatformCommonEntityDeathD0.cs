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
/// Common type-$00-$07 death/removal family at $A55E/$A5BE and $A7FB-$A839.
///
/// X tracks camera every update. The $D0-$DF phase itself advances only when
/// ($3C & 3)==0. On those cadence ticks selected ground-descriptor classes also
/// add +2 Y. Advancing past $DF clears the state/removes the entity.
/// Ordinary $9915/$98BA interaction is bypassed on this control path.
/// </summary>
public static class PlatformCommonEntityDeathD0
{
    public static PlatformCommonEntityDeathD0Result Step(
        PlatformCommonEntityMotionState state,
        byte frameCounter3C,
        byte cameraDelta43)
    {
        if (state.Type >= 0x08)
            throw new ArgumentOutOfRangeException(nameof(state), state.Type, "$D0 common death helper covers entity types $00-$07.");
        if ((state.ActionState & 0xF0) != 0xD0)
            throw new InvalidOperationException($"$D0 helper requires action family $D0, got ${state.ActionState:X2}.");

        state = state with
        {
            X = unchecked((byte)(state.X - cameraDelta43)),
        };

        if (state.X >= 0xF8)
        {
            return new PlatformCommonEntityDeathD0Result(
                state,
                PlatformCommonEntityDeathD0Outcome.RemovedHorizontal,
                DeathPhaseAdvanced: false,
                ScreenYDelta: 0,
                ScreenXDeltaFromCamera: -cameraDelta43);
        }

        if (state.Y is >= 0xB0 and < 0xC0)
        {
            return new PlatformCommonEntityDeathD0Result(
                state,
                PlatformCommonEntityDeathD0Outcome.RemovedVerticalBand,
                DeathPhaseAdvanced: false,
                ScreenYDelta: 0,
                ScreenXDeltaFromCamera: -cameraDelta43);
        }

        if ((frameCounter3C & 3) != 0)
        {
            return new PlatformCommonEntityDeathD0Result(
                state,
                PlatformCommonEntityDeathD0Outcome.Active,
                DeathPhaseAdvanced: false,
                ScreenYDelta: 0,
                ScreenXDeltaFromCamera: -cameraDelta43);
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
            return new PlatformCommonEntityDeathD0Result(
                state,
                PlatformCommonEntityDeathD0Outcome.CompletedRemoval,
                DeathPhaseAdvanced: true,
                ScreenYDelta: yDelta,
                ScreenXDeltaFromCamera: -cameraDelta43);
        }

        state = state with { ActionState = nextAction };
        return new PlatformCommonEntityDeathD0Result(
            state,
            PlatformCommonEntityDeathD0Outcome.Active,
            DeathPhaseAdvanced: true,
            ScreenYDelta: yDelta,
            ScreenXDeltaFromCamera: -cameraDelta43);
    }
}

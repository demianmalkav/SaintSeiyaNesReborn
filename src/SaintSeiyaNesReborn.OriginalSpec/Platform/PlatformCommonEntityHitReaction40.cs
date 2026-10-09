namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformCommonEntityHitReaction40Result(
    PlatformCommonEntityMotionState State,
    bool CompletedReaction,
    bool KnockbackAdvanced,
    int HorizontalDeltaApplied);

/// <summary>
/// Exact type-$00-$07 hit-reaction path at bank 3 $A79E -> $A845.
///
/// The action family advances $40..$4F one state per update and returns to $10.
/// Record offset +$03 independently carries a short signed knockback counter:
/// low nibble zero means no motion; otherwise the byte is decremented first,
/// then remaining value >=$40 moves +4 X, otherwise -4 X.
///
/// This route bypasses the ordinary $9915/$98BA interaction calls and does not
/// apply camera delta $43.
/// </summary>
public static class PlatformCommonEntityHitReaction40
{
    public static PlatformCommonEntityHitReaction40Result Step(
        PlatformCommonEntityMotionState state)
    {
        if (state.Type >= 0x08)
            throw new ArgumentOutOfRangeException(nameof(state), state.Type, "Common $40 reaction helper covers entity types $00-$07.");
        if ((state.ActionState & 0xF0) != 0x40)
            throw new InvalidOperationException($"$40 reaction helper requires action family $40, got ${state.ActionState:X2}.");

        var incrementedAction = unchecked((byte)(state.ActionState + 1));
        var completed = incrementedAction >= 0x50;
        state = state with
        {
            ActionState = completed ? (byte)0x10 : incrementedAction,
        };

        var motion = state.StatePhase;
        if ((motion & 0x0F) == 0)
        {
            return new PlatformCommonEntityHitReaction40Result(
                state,
                completed,
                KnockbackAdvanced: false,
                HorizontalDeltaApplied: 0);
        }

        motion = unchecked((byte)(motion - 1));
        var delta = motion >= 0x40 ? 4 : -4;
        state = state with
        {
            StatePhase = motion,
            X = unchecked((byte)(state.X + delta)),
        };

        return new PlatformCommonEntityHitReaction40Result(
            state,
            completed,
            KnockbackAdvanced: true,
            HorizontalDeltaApplied: delta);
    }
}

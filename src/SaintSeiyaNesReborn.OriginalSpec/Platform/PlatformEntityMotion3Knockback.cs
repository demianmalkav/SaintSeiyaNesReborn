namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformEntityMotion3KnockbackResult(
    PlatformCommonEntityMotionState State,
    bool Advanced,
    int HorizontalDeltaApplied);

/// <summary>
/// Exact shared record-offset +$03 knockback consumer at bank-3 $A845-$A868.
///
/// The low nibble acts as the remaining tick count. If it is zero, the helper
/// is a no-op. Otherwise the byte is decremented first and stored back; the
/// remaining full byte then selects direction: >=$40 moves +4 X, < $40 moves
/// -4 X. This primitive is used by the type-$00-$07 $40 reaction path and by
/// types $0A/$0B after their ordinary interaction path.
/// </summary>
public static class PlatformEntityMotion3Knockback
{
    public static PlatformEntityMotion3KnockbackResult Step(PlatformCommonEntityMotionState state)
    {
        var motion3 = state.StatePhase;
        if ((motion3 & 0x0F) == 0)
            return new(state, Advanced: false, HorizontalDeltaApplied: 0);

        motion3 = unchecked((byte)(motion3 - 1));
        var delta = motion3 >= 0x40 ? 4 : -4;
        state = state with
        {
            StatePhase = motion3,
            X = unchecked((byte)(state.X + delta)),
        };

        return new(state, Advanced: true, HorizontalDeltaApplied: delta);
    }
}

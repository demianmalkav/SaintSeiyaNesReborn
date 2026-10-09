namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformCommonEntityHitReaction40Outcome
{
    Active,
    RemovedHorizontal,
    RemovedVerticalBand,
}

public readonly record struct PlatformCommonEntityHitReaction40Result(
    PlatformCommonEntityMotionState State,
    PlatformCommonEntityHitReaction40Outcome Outcome,
    bool CompletedReaction,
    bool KnockbackAdvanced,
    int HorizontalDeltaApplied,
    int ScreenXDeltaFromCamera);

/// <summary>
/// Exact common type-$00-$07 hit-reaction path around bank-3
/// $A5F1/$A636 -> $A79E -> $A845.
///
/// Two entry modes matter:
/// - an entity that begins the frame in $40 first receives the shared screen-X
///   camera correction/removal gates, then advances the reaction;
/// - an entity changed to $40 by $9915 during the current interaction reaches
///   $A79E immediately and advances the reaction in the SAME frame, without a
///   second camera correction.
///
/// Action advances $40..$4F and returns to $10. Record +$03 is then consumed by
/// the shared $A845 knockback helper.
/// </summary>
public static class PlatformCommonEntityHitReaction40
{
    /// <summary>
    /// Frame-start $40 route: apply the earlier shared camera/removal path, then
    /// enter the post-path $A79E/$A845 reaction phase.
    /// </summary>
    public static PlatformCommonEntityHitReaction40Result Step(
        PlatformCommonEntityMotionState state,
        byte cameraDelta43 = 0)
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
                PlatformCommonEntityHitReaction40Outcome.RemovedHorizontal,
                CompletedReaction: false,
                KnockbackAdvanced: false,
                HorizontalDeltaApplied: 0,
                ScreenXDeltaFromCamera: -cameraDelta43);
        }

        if (state.Y is >= 0xB0 and < 0xC0)
        {
            return new(
                state,
                PlatformCommonEntityHitReaction40Outcome.RemovedVerticalBand,
                CompletedReaction: false,
                KnockbackAdvanced: false,
                HorizontalDeltaApplied: 0,
                ScreenXDeltaFromCamera: -cameraDelta43);
        }

        var advanced = AdvanceAfterPath(state);
        return advanced with { ScreenXDeltaFromCamera = -cameraDelta43 };
    }

    /// <summary>
    /// $A79E/$A845 only. Use this after $9915/$98BA when the current interaction
    /// has just written family $40. No camera correction is repeated here because
    /// the entity already passed the earlier $A636 screen-X path this frame.
    /// </summary>
    public static PlatformCommonEntityHitReaction40Result AdvanceAfterPath(
        PlatformCommonEntityMotionState state)
    {
        Validate(state);

        var incrementedAction = unchecked((byte)(state.ActionState + 1));
        var completed = incrementedAction >= 0x50;
        state = state with
        {
            ActionState = completed ? (byte)0x10 : incrementedAction,
        };

        var knockback = PlatformEntityMotion3Knockback.Step(state);
        return new(
            knockback.State,
            PlatformCommonEntityHitReaction40Outcome.Active,
            completed,
            knockback.Advanced,
            knockback.HorizontalDeltaApplied,
            ScreenXDeltaFromCamera: 0);
    }

    private static void Validate(PlatformCommonEntityMotionState state)
    {
        if (state.Type >= 0x08)
            throw new ArgumentOutOfRangeException(nameof(state), state.Type, "Common $40 reaction helper covers entity types $00-$07.");
        if ((state.ActionState & 0xF0) != 0x40)
            throw new InvalidOperationException($"$40 reaction helper requires action family $40, got ${state.ActionState:X2}.");
    }
}

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformAttackObjectPhaseResult(
    PlatformPlayerActionDispatchResult Player,
    bool SkippedBecausePlayerLoopExited,
    bool UpdatedAttackObjects);

/// <summary>
/// Composes only the two attack-system phases whose active-platform frame
/// positions are statically closed.
///
/// Before $AAE4, bank-1 $8000 calls $926C and advances attack busy byte $4B.
/// After $AAE4 and $B94B plus the earlier object classes, fixed $C2D7 calls
/// bank-3 $A22C, which advances player attack/projectile objects while $3C still
/// contains the current frame value.
///
/// The intervening special/secondary/common entity updates are intentionally not
/// represented here; this helper establishes attack timing without pretending the
/// whole object pipeline is complete.
/// </summary>
public static class PlatformAttackFramePhases
{
    /// <summary>
    /// Semantic slice of bank-1 $8000 -> $926C. Must be applied before the
    /// player-action dispatcher sees B input on the current frame.
    /// </summary>
    public static PlatformPlayerActionState AdvanceBusyBeforePlayer(PlatformPlayerActionState state)
    {
        var attack = state.AttackState with
        {
            Busy4B = PlatformAttackSystem.AdvanceBusy(state.AttackState.Busy4B),
        };
        return state with { AttackState = attack };
    }

    /// <summary>
    /// Semantic slice of fixed $C2D7 -> bank-3 $A22C. This occurs after the
    /// player step and $B94B, but before fixed-bank $C402 increments $3C.
    /// A newly created attack can therefore be advanced on its creation frame.
    ///
    /// If $AAE4 took the exceptional $80 reload path, it never returns and $C2D7
    /// is not reached; attack objects are left untouched.
    /// </summary>
    public static PlatformAttackObjectPhaseResult UpdateObjectsAfterPlayer(
        PlatformPlayerActionDispatchResult player,
        byte frameCounter3C)
    {
        if (player.ExitsNormalPlayerLoop)
        {
            return new PlatformAttackObjectPhaseResult(
                player,
                SkippedBecausePlayerLoopExited: true,
                UpdatedAttackObjects: false);
        }

        var state = player.State;
        var updated = PlatformAttackSystem.UpdateAttackObjects(
            state.AttackState,
            state.Saint,
            frameCounter3C,
            state.PlayerY,
            state.Horizontal.PlayerX,
            player.FrameStartAction4E);

        return new PlatformAttackObjectPhaseResult(
            player with { State = state with { AttackState = updated } },
            SkippedBecausePlayerLoopExited: false,
            UpdatedAttackObjects: updated != state.AttackState);
    }
}

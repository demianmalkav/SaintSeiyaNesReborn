namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformEntityDecisionOutcome
{
    NoCommonDecisionPath,
    TimerPending,
    TimerExpiredNoChange,
    FacingToggled,
    JumpStartedRight,
    JumpStartedLeft,
    TerrainNoTurn,
    TerrainTurned,
}

public readonly record struct PlatformEntityDecisionResult(
    PlatformCommonEntityMotionState State,
    PlatformEntityDecisionOutcome Outcome,
    bool DecisionTimerReseeded);

/// <summary>
/// Exact semantic reduction of bank-3 $A970-$AA63.
///
/// Dispatch:
/// - types $07/$0A/$0B: direct terrain-facing check at $AA1F;
/// - types $00-$06: decrement decision timer; on 1->0 reseed through $AA64
///   and run the relative player-position decision at $A999;
/// - all other types >= $08: no common decision work in this path.
///
/// Arithmetic intentionally preserves 8-bit wrap where the 6502 does.
/// </summary>
public static class PlatformCommonEntityDecision
{
    public static PlatformEntityDecisionResult Step(
        PlatformCommonEntityMotionState state,
        byte playerX,
        byte playerY,
        byte playerJumpPhase49,
        byte entropy48)
    {
        if (state.Type is 0x07 or 0x0A or 0x0B)
            return StepTerrain(state);

        if (state.Type >= 0x08)
            return new(state, PlatformEntityDecisionOutcome.NoCommonDecisionPath, false);

        // $A98B: zero is not a special idle value here; SEC/SBC #1 wraps it to
        // $FF. In ordinary initialized records the timer is expected nonzero.
        var timer = unchecked((byte)(state.DecisionTimer - 1));
        state = state with { DecisionTimer = timer };
        if (timer != 0)
            return new(state, PlatformEntityDecisionOutcome.TimerPending, false);

        state = state with
        {
            DecisionTimer = PlatformCommonEntityMotion.ReseedDecisionTimer(entropy48),
        };

        var decision = DecideAfterReseed(state, playerX, playerY, playerJumpPhase49);
        return decision with { DecisionTimerReseeded = true };
    }

    private static PlatformEntityDecisionResult StepTerrain(PlatformCommonEntityMotionState state)
    {
        var descriptor = PlatformCommonEntityMotion.FacingRight(state.FlagsFacing)
            ? state.TerrainProbeRight
            : state.TerrainProbeLeft;

        if (!PlatformCommonEntityMotion.TerrainForcesTurn(state.Type, state.FlagsFacing, descriptor))
            return new(state, PlatformEntityDecisionOutcome.TerrainNoTurn, false);

        return new(
            state with { FlagsFacing = PlatformCommonEntityMotion.ToggleFacing(state.FlagsFacing) },
            PlatformEntityDecisionOutcome.TerrainTurned,
            false);
    }

    private static PlatformEntityDecisionResult DecideAfterReseed(
        PlatformCommonEntityMotionState state,
        byte playerX,
        byte playerY,
        byte playerJumpPhase49)
    {
        if (state.X < playerX)
        {
            // $A9F0: if the entity is left of the player and already faces right,
            // it selects jump-right immediately. Facing left enters the shared
            // away-facing vertical/proximity branch at $A9AC.
            if (PlatformCommonEntityMotion.FacingRight(state.FlagsFacing))
                return StartJump(state, 0x31);

            return DecideAwayFacing(state, playerX, playerY, playerJumpPhase49);
        }

        if (PlatformCommonEntityMotion.FacingRight(state.FlagsFacing))
            return DecideAwayFacing(state, playerX, playerY, playerJumpPhase49);

        // $A9DC: entity is at/right of player and faces left.
        if (state.X >= 0xC0)
            return NoChange(state);

        var distance = unchecked((byte)(state.X - playerX));
        return distance >= 0x20
            ? StartJump(state, 0x31)
            : StartJump(state, 0x32);
    }

    /// <summary>
    /// Literal $A9AC-$A9D9 branch. The two horizontal comparisons deliberately
    /// use wrapping byte arithmetic because the original computes
    /// (playerX + $30) and then subtracts $60 in A.
    /// </summary>
    private static PlatformEntityDecisionResult DecideAwayFacing(
        PlatformCommonEntityMotionState state,
        byte playerX,
        byte playerY,
        byte playerJumpPhase49)
    {
        if (playerJumpPhase49 == 0)
        {
            var entityYMinusOne = unchecked((byte)(state.Y - 1));
            if (entityYMinusOne >= playerY)
                return NoChange(state);
        }

        var upper = unchecked((byte)(playerX + 0x30));
        if (upper < state.X)
            return Toggle(state);

        var lower = unchecked((byte)(upper - 0x60));
        if (lower < state.X)
            return NoChange(state);

        return Toggle(state);
    }

    private static PlatformEntityDecisionResult StartJump(
        PlatformCommonEntityMotionState state,
        byte action)
    {
        // $A9FA contains exclusions for $08/$09/$0C/$0D/$0E. They cannot be
        // reached through the ordinary $A970 dispatch, but preserving them here
        // keeps this semantic reduction correct if reused directly.
        if (state.Type is 0x08 or 0x09 or 0x0C or 0x0D or 0x0E)
            return NoChange(state);

        var outcome = action == 0x31
            ? PlatformEntityDecisionOutcome.JumpStartedRight
            : PlatformEntityDecisionOutcome.JumpStartedLeft;

        return new(
            state with
            {
                ActionState = action,
                StatePhase = 1,
            },
            outcome,
            false);
    }

    private static PlatformEntityDecisionResult Toggle(PlatformCommonEntityMotionState state) =>
        new(
            state with { FlagsFacing = PlatformCommonEntityMotion.ToggleFacing(state.FlagsFacing) },
            PlatformEntityDecisionOutcome.FacingToggled,
            false);

    private static PlatformEntityDecisionResult NoChange(PlatformCommonEntityMotionState state) =>
        new(state, PlatformEntityDecisionOutcome.TimerExpiredNoChange, false);
}

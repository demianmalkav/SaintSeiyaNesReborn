namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformCommonEntityActiveRoute
{
    Ordinary10,
    Jump30,
    HitReaction40,
    Fall50,
    DeathD0,
}

public enum PlatformCommonEntityActiveContinuation
{
    ReadyForInteraction,
    SkipInteraction,
    Removed,
}

public readonly record struct PlatformCommonEntityActiveDispatchResult(
    PlatformCommonEntityMotionState State,
    PlatformCommonEntityActiveRoute Route,
    PlatformCommonEntityActiveContinuation Continuation,
    PlatformCommonEntityPreparationResult? Ordinary,
    PlatformCommonEntityJump30Result? Jump30,
    PlatformCommonEntityHitReaction40Result? HitReaction40,
    PlatformCommonEntityFall50Result? Fall50,
    PlatformCommonEntityDeathD0Result? DeathD0);

/// <summary>
/// Explicit dispatcher for the closed active common-entity families used by
/// types $00-$07: ordinary $10, jump $30, hit reaction $40, fall $50 and
/// ordinary death/removal $D0.
///
/// Continuation is a first-class result because several routes can end an
/// update with a different action byte while still bypassing $9915/$98BA on
/// that same control path.
/// </summary>
public static class PlatformCommonEntityActiveDispatcher
{
    public static PlatformCommonEntityActiveDispatchResult Step(
        PlatformCommonEntityMotionState state,
        byte playerX,
        byte playerY,
        byte playerJumpPhase49,
        byte entropy48,
        byte frameCounter3C,
        byte cameraDelta43)
    {
        if (state.Type >= 0x08)
            throw new ArgumentOutOfRangeException(nameof(state), state.Type, "Active common dispatcher covers entity types $00-$07.");

        return (state.ActionState & 0xF0) switch
        {
            0x10 => StepOrdinary(state, playerX, playerY, playerJumpPhase49, entropy48, frameCounter3C, cameraDelta43),
            0x30 => StepJump(state, playerX, playerY, frameCounter3C, cameraDelta43),
            0x40 => StepReaction(state),
            0x50 => StepFall(state, cameraDelta43),
            0xD0 => StepDeath(state, frameCounter3C, cameraDelta43),
            _ => throw new InvalidOperationException($"Active common dispatcher does not yet model action ${state.ActionState:X2}.")
        };
    }

    private static PlatformCommonEntityActiveDispatchResult StepOrdinary(
        PlatformCommonEntityMotionState state,
        byte playerX,
        byte playerY,
        byte playerJumpPhase49,
        byte entropy48,
        byte frameCounter3C,
        byte cameraDelta43)
    {
        var step = PlatformCommonEntityPreparation.StepOrdinaryMobile(
            state,
            playerX,
            playerY,
            playerJumpPhase49,
            entropy48,
            frameCounter3C,
            cameraDelta43);
        var continuation = step.Outcome == PlatformCommonEntityPreparationOutcome.ReadyForInteraction
            ? PlatformCommonEntityActiveContinuation.ReadyForInteraction
            : PlatformCommonEntityActiveContinuation.Removed;
        return new(step.State, PlatformCommonEntityActiveRoute.Ordinary10, continuation, step, null, null, null, null);
    }

    private static PlatformCommonEntityActiveDispatchResult StepJump(
        PlatformCommonEntityMotionState state,
        byte playerX,
        byte playerY,
        byte frameCounter3C,
        byte cameraDelta43)
    {
        var step = PlatformCommonEntityJump30.Step(
            state,
            playerX,
            playerY,
            frameCounter3C,
            cameraDelta43);
        var continuation = step.Outcome == PlatformCommonEntityJump30Outcome.ReadyForInteraction
            ? PlatformCommonEntityActiveContinuation.ReadyForInteraction
            : PlatformCommonEntityActiveContinuation.Removed;
        return new(step.State, PlatformCommonEntityActiveRoute.Jump30, continuation, null, step, null, null, null);
    }

    private static PlatformCommonEntityActiveDispatchResult StepReaction(PlatformCommonEntityMotionState state)
    {
        var step = PlatformCommonEntityHitReaction40.Step(state);
        return new(
            step.State,
            PlatformCommonEntityActiveRoute.HitReaction40,
            PlatformCommonEntityActiveContinuation.SkipInteraction,
            null,
            null,
            step,
            null,
            null);
    }

    private static PlatformCommonEntityActiveDispatchResult StepFall(
        PlatformCommonEntityMotionState state,
        byte cameraDelta43)
    {
        var step = PlatformCommonEntityFall50.Step(state, cameraDelta43);
        var continuation = step.Outcome == PlatformCommonEntityFall50Outcome.RemovedLowerBand
            ? PlatformCommonEntityActiveContinuation.Removed
            : PlatformCommonEntityActiveContinuation.SkipInteraction;
        return new(
            step.State,
            PlatformCommonEntityActiveRoute.Fall50,
            continuation,
            null,
            null,
            null,
            step,
            null);
    }

    private static PlatformCommonEntityActiveDispatchResult StepDeath(
        PlatformCommonEntityMotionState state,
        byte frameCounter3C,
        byte cameraDelta43)
    {
        var step = PlatformCommonEntityDeathD0.Step(state, frameCounter3C, cameraDelta43);
        var continuation = step.Outcome is PlatformCommonEntityDeathD0Outcome.RemovedHorizontal
            or PlatformCommonEntityDeathD0Outcome.RemovedVerticalBand
            or PlatformCommonEntityDeathD0Outcome.CompletedRemoval
            ? PlatformCommonEntityActiveContinuation.Removed
            : PlatformCommonEntityActiveContinuation.SkipInteraction;
        return new(
            step.State,
            PlatformCommonEntityActiveRoute.DeathD0,
            continuation,
            null,
            null,
            null,
            null,
            step);
    }
}

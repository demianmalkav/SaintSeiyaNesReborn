namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformCommonEntityActiveRoute
{
    Ordinary10,
    Jump30,
    HitReaction40,
    Fall50,
    Attack70,
    DeathD0,
    DropE0,
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
    PlatformCommonEntityAttack70PreparationResult? Attack70,
    PlatformCommonEntityDeathD0Result? DeathD0,
    PlatformCommonEntityDropE0Result? DropE0);

/// <summary>
/// Explicit pre-interaction dispatcher for the closed common entity families.
/// Types $00-$07 use the full promoted family set. Normal producer/writer flow
/// proves that types $0A/$0B have exactly two reachable live families: `$10`
/// ordinary and `$50` fall. Terminal removal may clear action to `$00` only
/// after the visual slot is retired, so `$00` is not an active dispatch family.
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
        var family = (byte)(state.ActionState & 0xF0);
        var common00To07 = state.Type <= 0x07;
        var slowHazard0A0B = state.Type is 0x0A or 0x0B;

        if (!common00To07 && !slowHazard0A0B)
            throw new ArgumentOutOfRangeException(nameof(state), state.Type, "Active dispatcher currently covers types $00-$07 and $0A/$0B.");

        if (slowHazard0A0B && family is not (0x10 or 0x50))
            throw new InvalidOperationException($"Type ${state.Type:X2} has confirmed reachable live action families $10/$50 only; got injected action ${state.ActionState:X2}.");

        return family switch
        {
            0x10 => StepOrdinary(state, playerX, playerY, playerJumpPhase49, entropy48, frameCounter3C, cameraDelta43),
            0x30 => StepJump(state, playerX, playerY, frameCounter3C, cameraDelta43),
            0x40 => StepReaction(state, cameraDelta43),
            0x50 => StepFall(state, cameraDelta43),
            0x70 => StepAttack70(state, playerX, playerY, playerJumpPhase49, entropy48, frameCounter3C, cameraDelta43),
            0xD0 => StepDeath(state, frameCounter3C, cameraDelta43),
            0xE0 => StepDropE0(state, cameraDelta43),
            _ => throw new InvalidOperationException($"Active dispatcher does not yet model action ${state.ActionState:X2} for type ${state.Type:X2}.")
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
        return new(step.State, PlatformCommonEntityActiveRoute.Ordinary10, continuation, step, null, null, null, null, null, null);
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
        return new(step.State, PlatformCommonEntityActiveRoute.Jump30, continuation, null, step, null, null, null, null, null);
    }

    private static PlatformCommonEntityActiveDispatchResult StepReaction(
        PlatformCommonEntityMotionState state,
        byte cameraDelta43)
    {
        var step = PlatformCommonEntityHitReaction40.Step(state, cameraDelta43);
        var continuation = step.Outcome is PlatformCommonEntityHitReaction40Outcome.RemovedHorizontal
            or PlatformCommonEntityHitReaction40Outcome.RemovedVerticalBand
            ? PlatformCommonEntityActiveContinuation.Removed
            : PlatformCommonEntityActiveContinuation.SkipInteraction;
        return new(step.State, PlatformCommonEntityActiveRoute.HitReaction40, continuation,
            null, null, step, null, null, null, null);
    }

    private static PlatformCommonEntityActiveDispatchResult StepFall(
        PlatformCommonEntityMotionState state,
        byte cameraDelta43)
    {
        var step = PlatformCommonEntityFall50.Step(state, cameraDelta43);
        var continuation = step.Outcome == PlatformCommonEntityFall50Outcome.RemovedLowerBand
            ? PlatformCommonEntityActiveContinuation.Removed
            : PlatformCommonEntityActiveContinuation.SkipInteraction;
        return new(step.State, PlatformCommonEntityActiveRoute.Fall50, continuation,
            null, null, null, step, null, null, null);
    }

    private static PlatformCommonEntityActiveDispatchResult StepAttack70(
        PlatformCommonEntityMotionState state,
        byte playerX,
        byte playerY,
        byte playerJumpPhase49,
        byte entropy48,
        byte frameCounter3C,
        byte cameraDelta43)
    {
        var step = PlatformCommonEntityAttack70.PrepareCommon(
            state,
            playerX,
            playerY,
            playerJumpPhase49,
            entropy48,
            frameCounter3C,
            cameraDelta43);
        var continuation = step.Outcome == PlatformCommonEntityAttack70PreparationOutcome.ReadyForInteraction
            ? PlatformCommonEntityActiveContinuation.ReadyForInteraction
            : PlatformCommonEntityActiveContinuation.Removed;
        return new(step.State, PlatformCommonEntityActiveRoute.Attack70, continuation,
            null, null, null, null, step, null, null);
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
        return new(step.State, PlatformCommonEntityActiveRoute.DeathD0, continuation,
            null, null, null, null, null, step, null);
    }

    private static PlatformCommonEntityActiveDispatchResult StepDropE0(
        PlatformCommonEntityMotionState state,
        byte cameraDelta43)
    {
        var step = PlatformCommonEntityDropE0.Step(state, cameraDelta43);
        var continuation = step.Outcome == PlatformCommonEntityDropE0Outcome.RemovedLowerBand
            ? PlatformCommonEntityActiveContinuation.Removed
            : PlatformCommonEntityActiveContinuation.SkipInteraction;
        return new(step.State, PlatformCommonEntityActiveRoute.DropE0, continuation,
            null, null, null, null, null, null, step);
    }
}

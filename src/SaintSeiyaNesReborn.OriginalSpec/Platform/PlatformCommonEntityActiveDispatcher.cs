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
    Special08090C,
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
    PlatformCommonEntityDropE0Result? DropE0,
    PlatformEntity08090CPreResult? Special08090C);

/// <summary>
/// Explicit pre-interaction dispatcher for the closed platform-entity families.
///
/// - types $00-$07: promoted common family set;
/// - types $0A/$0B: statically closed `$10/$50` routes;
/// - types $08/$09/$0C: basic post-trigger `$00/$40/$50/$D0` routes through
///   `PlatformEntityTypes08090C`. Their earlier `$A442-$A55B` attack-trigger
///   prelude and family `$70` remain a separate target.
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
        var type = state.Type;

        if (type is 0x08 or 0x09 or 0x0C)
        {
            if (family is not (0x00 or 0x40 or 0x50 or 0xD0))
            {
                throw new InvalidOperationException(
                    $"Type ${type:X2} basic dispatcher is currently closed only for families $00/$40/$50/$D0; got ${state.ActionState:X2}.");
            }
            return StepSpecial08090C(state, playerX, playerY, frameCounter3C, cameraDelta43);
        }

        var common00To07 = type <= 0x07;
        var slowHazard0A0B = type is 0x0A or 0x0B;

        if (!common00To07 && !slowHazard0A0B)
            throw new ArgumentOutOfRangeException(nameof(state), type, "Active dispatcher currently covers types $00-$0C except unsupported special families.");

        if (slowHazard0A0B && family is not (0x10 or 0x50))
            throw new InvalidOperationException($"Type ${type:X2} is currently closed only for action families $10/$50, got ${state.ActionState:X2}.");

        return family switch
        {
            0x10 => StepOrdinary(state, playerX, playerY, playerJumpPhase49, entropy48, frameCounter3C, cameraDelta43),
            0x30 => StepJump(state, playerX, playerY, frameCounter3C, cameraDelta43),
            0x40 => StepReaction(state, cameraDelta43),
            0x50 => StepFall(state, cameraDelta43),
            0x70 => StepAttack70(state, playerX, playerY, playerJumpPhase49, entropy48, frameCounter3C, cameraDelta43),
            0xD0 => StepDeath(state, frameCounter3C, cameraDelta43),
            0xE0 => StepDropE0(state, cameraDelta43),
            _ => throw new InvalidOperationException($"Active dispatcher does not yet model action ${state.ActionState:X2} for type ${type:X2}.")
        };
    }

    private static PlatformCommonEntityActiveDispatchResult StepSpecial08090C(
        PlatformCommonEntityMotionState state,
        byte playerX,
        byte playerY,
        byte frameCounter3C,
        byte cameraDelta43)
    {
        var step = PlatformEntityTypes08090C.StepPreInteraction(
            state,
            playerX,
            playerY,
            frameCounter3C,
            cameraDelta43);

        var continuation = step.Continuation switch
        {
            PlatformEntity08090CContinuation.ReadyForInteraction => PlatformCommonEntityActiveContinuation.ReadyForInteraction,
            PlatformEntity08090CContinuation.SkipInteraction => PlatformCommonEntityActiveContinuation.SkipInteraction,
            PlatformEntity08090CContinuation.Removed => PlatformCommonEntityActiveContinuation.Removed,
            _ => throw new InvalidOperationException("Unknown $08/$09/$0C continuation.")
        };

        return new(
            step.State,
            PlatformCommonEntityActiveRoute.Special08090C,
            continuation,
            null, null, null, null, null, null, null,
            step);
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
        return new(step.State, PlatformCommonEntityActiveRoute.Ordinary10, continuation,
            step, null, null, null, null, null, null, null);
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
        return new(step.State, PlatformCommonEntityActiveRoute.Jump30, continuation,
            null, step, null, null, null, null, null, null);
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
            null, null, step, null, null, null, null, null);
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
            null, null, null, step, null, null, null, null);
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
            null, null, null, null, step, null, null, null);
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
            null, null, null, null, null, step, null, null);
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
            null, null, null, null, null, null, step, null);
    }
}

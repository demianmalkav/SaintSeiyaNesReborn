namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformCommonEntityPreparationOutcome
{
    ReadyForInteraction,
    RemovedHorizontal,
    RemovedVerticalBand,
}

public readonly record struct PlatformCommonEntityPreparationResult(
    PlatformCommonEntityMotionState State,
    PlatformEntityDecisionResult Decision,
    PlatformEntityJumpStepResult? JumpStep,
    PlatformCommonEntityPreparationOutcome Outcome,
    bool ProximityFallStarted,
    int HorizontalDeltaBeforeCamera,
    byte CameraDelta43);

/// <summary>
/// Executable reduction of the ordinary mobile path through bank-3
/// $A55E-$A700 for entity types $00-$07 plus the directly shared `$0A/$0B`
/// route entering in action family $10.
///
/// Types $0A/$0B reuse the surrounding ordinary path but differ inside already
/// promoted helpers: `$A970` goes directly to the terrain-facing test (no
/// decision timer) and `$A60B` uses the slow `$3C & 1` horizontal cadence.
/// </summary>
public static class PlatformCommonEntityPreparation
{
    public static PlatformCommonEntityPreparationResult StepOrdinaryMobile(
        PlatformCommonEntityMotionState state,
        byte playerX,
        byte playerY,
        byte playerJumpPhase49,
        byte entropy48,
        byte frameCounter3C,
        byte cameraDelta43)
    {
        if (!IsSupportedOrdinaryType(state.Type))
            throw new ArgumentOutOfRangeException(nameof(state), state.Type, "Ordinary mobile preparation covers types $00-$07 and $0A/$0B.");
        if ((state.ActionState & 0xF0) != 0x10)
            throw new InvalidOperationException($"Ordinary mobile preparation requires entry action family $10, got ${state.ActionState:X2}.");

        // $A578 -> $A970. Types $00-$06 use the decision timer; $07/$0A/$0B
        // route directly through the terrain-facing check.
        var decision = PlatformCommonEntityDecision.Step(
            state,
            playerX,
            playerY,
            playerJumpPhase49,
            entropy48);
        state = decision.State;

        PlatformEntityJumpStepResult? jump = null;

        // Ordinary types $00-$06 may have just started $31/$32. $0A/$0B never
        // do so through A970, but retaining the family test keeps the surrounding
        // ROM flow literal.
        if ((state.ActionState & 0xF0) == 0x30)
        {
            jump = PlatformCommonEntityMotion.StepJumpVertical(state);
            state = jump.Value.State;
        }

        var horizontalDelta = 0;
        var familyAfterVertical = (byte)(state.ActionState & 0xF0);
        if (familyAfterVertical == 0x30)
        {
            horizontalDelta = PlatformCommonEntityMotion.JumpHorizontalDelta(
                state.ActionState,
                state.Type,
                frameCounter3C);
        }
        else if (familyAfterVertical == 0x10)
        {
            var step = PlatformCommonEntityMotion.HorizontalStep(state.Type, frameCounter3C);
            horizontalDelta = PlatformCommonEntityMotion.FacingRight(state.FlagsFacing)
                ? step
                : -step;
        }

        state = state with
        {
            X = unchecked((byte)(state.X + horizontalDelta - cameraDelta43)),
        };

        if (state.X >= 0xF8)
        {
            return new PlatformCommonEntityPreparationResult(
                state,
                decision,
                jump,
                PlatformCommonEntityPreparationOutcome.RemovedHorizontal,
                ProximityFallStarted: false,
                horizontalDelta,
                cameraDelta43);
        }

        if (state.Y is >= 0xB0 and < 0xC0)
        {
            return new PlatformCommonEntityPreparationResult(
                state,
                decision,
                jump,
                PlatformCommonEntityPreparationOutcome.RemovedVerticalBand,
                ProximityFallStarted: false,
                horizontalDelta,
                cameraDelta43);
        }

        var proximityFall = false;

        // $A68A-$A6FE. $0A/$0B are not in the explicit $08/$09 or $0D+
        // exclusions and therefore share the proximity-fall test with common
        // types when phase is zero.
        if (state.StatePhase == 0
            && state.GroundDescriptor is not (>= 0xE0 and < 0xF0)
            && playerY < 0x81)
        {
            var playerRow = (byte)(playerY & 0xF0);
            if (playerRow > state.Y
                && state.X is >= 0x21 and < 0xC0
                && WithinOriginalHorizontalProximity(state.X, playerX))
            {
                state = state with
                {
                    ActionState = 0x50,
                    Y = unchecked((byte)(state.Y + 6)),
                };
                proximityFall = true;
            }
        }

        return new PlatformCommonEntityPreparationResult(
            state,
            decision,
            jump,
            PlatformCommonEntityPreparationOutcome.ReadyForInteraction,
            proximityFall,
            horizontalDelta,
            cameraDelta43);
    }

    private static bool IsSupportedOrdinaryType(byte type) =>
        type <= 0x07 || type is 0x0A or 0x0B;

    private static bool WithinOriginalHorizontalProximity(byte entityX, byte playerX)
    {
        if (entityX >= playerX)
        {
            var left = unchecked((byte)(entityX - 0x20));
            return left < playerX;
        }

        if (playerX < 0x20)
            return false;
        var right = unchecked((byte)(entityX + 0x20));
        return right >= playerX;
    }
}

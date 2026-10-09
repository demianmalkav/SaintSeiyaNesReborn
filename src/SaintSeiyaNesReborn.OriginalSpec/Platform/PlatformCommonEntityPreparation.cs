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
/// $A55E-$A700 for common entity types $00-$07 entering in action family $10.
///
/// This composes the already-promoted decision and jump primitives with the
/// surrounding movement/camera/removal/proximity-fall ordering that precedes
/// $9915/$98BA. Special-state families and types $08+ remain separate paths.
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
        if (state.Type > 0x07)
            throw new ArgumentOutOfRangeException(nameof(state), state.Type, "Ordinary mobile preparation currently covers entity types $00-$07.");
        if ((state.ActionState & 0xF0) != 0x10)
            throw new InvalidOperationException($"Ordinary mobile preparation requires entry action family $10, got ${state.ActionState:X2}.");

        // $A578 -> $A970. A timer expiry can change facing or start $31/$32.
        var decision = PlatformCommonEntityDecision.Step(
            state,
            playerX,
            playerY,
            playerJumpPhase49,
            entropy48);
        state = decision.State;

        PlatformEntityJumpStepResult? jump = null;

        // If A970 started a jump this very update, A5BB immediately calls C5E6;
        // the first vertical table sample is therefore consumed in the same frame.
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

        // $A636: every path then subtracts camera delta $43 from screen X.
        state = state with
        {
            X = unchecked((byte)(state.X + horizontalDelta - cameraDelta43)),
        };

        // $A63B-$A646: wrapped/edge X $F8-$FF removes the entity record/OAM.
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

        // $A67A-$A689 removes only the $B0-$BF vertical band here.
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

        // $A68A-$A6FE. Nonzero phase skips this proximity/drop test entirely.
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

    private static bool WithinOriginalHorizontalProximity(byte entityX, byte playerX)
    {
        if (entityX >= playerX)
        {
            // $A6DA-$A6DF: equality at exactly +32 is rejected (BCS).
            var left = unchecked((byte)(entityX - 0x20));
            return left < playerX;
        }

        // $A6E3-$A6F0: the opposite side first rejects playerX<$20, then
        // accepts equality at exactly -32 because the final branch is BCC.
        if (playerX < 0x20)
            return false;
        var right = unchecked((byte)(entityX + 0x20));
        return right >= playerX;
    }
}

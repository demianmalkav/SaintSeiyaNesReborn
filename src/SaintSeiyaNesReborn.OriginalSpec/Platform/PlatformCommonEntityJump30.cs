namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformCommonEntityJump30Outcome
{
    ReadyForInteraction,
    RemovedHorizontal,
    RemovedVerticalBand,
}

public readonly record struct PlatformCommonEntityJump30Result(
    PlatformCommonEntityMotionState State,
    PlatformEntityJumpStepResult Vertical,
    PlatformCommonEntityJump30Outcome Outcome,
    bool ProximityFallStarted,
    int HorizontalDeltaBeforeCamera,
    bool CameraApplied);

/// <summary>
/// Existing common-entity jump-family path at $A5BB-$A700 for types $00-$07.
/// Unlike a jump newly started from $A970, this helper begins with family $30
/// already active and therefore skips the ordinary decision routine.
/// </summary>
public static class PlatformCommonEntityJump30
{
    public static PlatformCommonEntityJump30Result Step(
        PlatformCommonEntityMotionState state,
        byte playerX,
        byte playerY,
        byte frameCounter3C,
        byte cameraDelta43)
    {
        if (state.Type >= 0x08)
            throw new ArgumentOutOfRangeException(nameof(state), state.Type, "$30 common jump helper covers entity types $00-$07.");
        if ((state.ActionState & 0xF0) != 0x30)
            throw new InvalidOperationException($"$30 helper requires action family $30, got ${state.ActionState:X2}.");

        var vertical = PlatformCommonEntityMotion.StepJumpVertical(state);
        state = vertical.State;

        var horizontalDelta = 0;
        var cameraApplied = true;
        var family = state.ActionState & 0xF0;

        if (family == 0x30)
        {
            horizontalDelta = PlatformCommonEntityMotion.JumpHorizontalDelta(
                state.ActionState,
                state.Type,
                frameCounter3C);

            // At $A5D4, $3x values other than exact $31/$32 jump directly to
            // $A63B and therefore skip both entity X movement and camera $43.
            if (state.ActionState is not (0x31 or 0x32))
                cameraApplied = false;
        }
        else if (family is 0x00 or 0x70 or 0xD0 or 0x40 or 0xA0)
        {
            // $A5EF path: no entity step, but camera delta is still applied.
            horizontalDelta = 0;
        }
        else
        {
            // Most importantly, a same-update C491 landing returns common types
            // to $10 and the remainder of A5BE then performs ordinary facing
            // movement before camera subtraction.
            var step = PlatformCommonEntityMotion.HorizontalStep(state.Type, frameCounter3C);
            horizontalDelta = PlatformCommonEntityMotion.FacingRight(state.FlagsFacing)
                ? step
                : -step;
        }

        if (cameraApplied)
        {
            state = state with
            {
                X = unchecked((byte)(state.X + horizontalDelta - cameraDelta43)),
            };
        }

        if (state.X >= 0xF8)
        {
            return new PlatformCommonEntityJump30Result(
                state,
                vertical,
                PlatformCommonEntityJump30Outcome.RemovedHorizontal,
                ProximityFallStarted: false,
                horizontalDelta,
                cameraApplied);
        }

        if (state.Y is >= 0xB0 and < 0xC0)
        {
            return new PlatformCommonEntityJump30Result(
                state,
                vertical,
                PlatformCommonEntityJump30Outcome.RemovedVerticalBand,
                ProximityFallStarted: false,
                horizontalDelta,
                cameraApplied);
        }

        var proximityFall = false;
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

        return new PlatformCommonEntityJump30Result(
            state,
            vertical,
            PlatformCommonEntityJump30Outcome.ReadyForInteraction,
            proximityFall,
            horizontalDelta,
            cameraApplied);
    }

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

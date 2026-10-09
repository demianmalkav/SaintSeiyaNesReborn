namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformAirborneHorizontalResult(
    PlatformHorizontalState State,
    byte ActionState,
    bool PlayerMoved,
    bool CameraMoved,
    bool CollisionBlocked,
    bool EdgeBlocked,
    bool DirectionalTrajectoryCollapsed,
    bool LandingSideCorrection);

/// <summary>
/// Horizontal portion of bank 3 $BDA2-$BF48 while the player is in jump family $30-$33.
/// Vertical motion and landing are intentionally modeled separately so their frame ordering can
/// be composed explicitly by PlatformAirborneSession.
/// </summary>
public static class PlatformAirborneHorizontalMotion
{
    public static PlatformAirborneHorizontalResult Step(
        PlatformStageMap stage,
        PlatformHorizontalState state,
        PlatformInput input,
        PlatformCollisionDescriptors probes,
        byte actionState,
        byte jumpPhase,
        byte halfPhase,
        PlatformMovementIncrements increments,
        byte frameCounter3C)
    {
        if (PlatformActionState.Family(actionState) != (byte)PlatformActionFamily.Jump)
            throw new ArgumentException("Airborne horizontal step requires jump-family action state.", nameof(actionState));

        var directionBits = actionState & 0x03;
        if (directionBits == 0)
            return StepVerticalJumpDrift(stage, state, input, probes, actionState, jumpPhase, halfPhase, increments, frameCounter3C);

        if (directionBits == 1)
        {
            // A right-started trajectory always continues right. Holding the opposite direction
            // reduces the step; continuing to hold Right uses the larger airborne increment.
            var step = (input & PlatformInput.Left) != 0
                ? increments.Airborne0389
                : (input & PlatformInput.Right) != 0
                    ? increments.Airborne0388
                    : increments.Grounded0387;
            return StepRight(stage, state, probes, actionState, jumpPhase, halfPhase, step, directional: true);
        }

        // $32 and the both-directions-at-takeoff quirk $33 both use the left trajectory path.
        var leftStep = (input & PlatformInput.Right) != 0
            ? increments.Airborne0389
            : (input & PlatformInput.Left) != 0
                ? increments.Airborne0388
                : increments.Grounded0387;
        return StepLeft(stage, state, probes, actionState, jumpPhase, halfPhase, leftStep, directional: true);
    }

    private static PlatformAirborneHorizontalResult StepVerticalJumpDrift(
        PlatformStageMap stage,
        PlatformHorizontalState state,
        PlatformInput input,
        PlatformCollisionDescriptors probes,
        byte actionState,
        byte jumpPhase,
        byte halfPhase,
        PlatformMovementIncrements increments,
        byte frameCounter3C)
    {
        var drift = (byte)(frameCounter3C & 1);

        // Right has priority, matching the BIT #$01 then BIT #$02 ordering.
        if ((input & PlatformInput.Right) != 0)
        {
            if (!BlocksRightLower(probes.LowerRight))
                return StepRight(stage, state, probes, actionState, jumpPhase, halfPhase, drift, directional: false);
        }

        if ((input & PlatformInput.Left) != 0)
        {
            if (!BlocksLeftLower(probes.LowerLeft))
                return StepLeft(stage, state, probes, actionState, jumpPhase, halfPhase, drift, directional: false);
        }

        // During the descending half, a vertical jump with no usable input performs the original
        // one-pixel side correction against diagonal/solid lower probes.
        if (jumpPhase < halfPhase)
            return Unchanged(state, actionState);

        if (BlocksLeftLower(probes.LowerLeft))
        {
            if (state.PlayerX >= 0x80)
            {
                var cap = PlatformHorizontalMotion.ScrollHighCapForSubstate(stage.Substate);
                if (state.ScrollLow >= 0xF8 && state.ScrollHigh == cap)
                    return Unchanged(state, actionState) with { LandingSideCorrection = true, EdgeBlocked = true };

                var sum = state.ScrollLow + increments.Grounded0387;
                var next = state with
                {
                    ScrollLow = (byte)sum,
                    ScrollHigh = unchecked((byte)(state.ScrollHigh + (sum > 0xFF ? 1 : 0))),
                };
                return new(next, actionState, false, increments.Grounded0387 != 0, false, false, false, true);
            }

            return new(
                state with { PlayerX = unchecked((byte)(state.PlayerX + 1)) },
                actionState,
                true,
                false,
                false,
                false,
                false,
                true);
        }

        if (BlocksRightLower(probes.LowerRight))
        {
            return new(
                state with { PlayerX = unchecked((byte)(state.PlayerX - 1)) },
                actionState,
                true,
                false,
                false,
                false,
                false,
                true);
        }

        return Unchanged(state, actionState);
    }

    private static PlatformAirborneHorizontalResult StepLeft(
        PlatformStageMap stage,
        PlatformHorizontalState state,
        PlatformCollisionDescriptors probes,
        byte actionState,
        byte jumpPhase,
        byte halfPhase,
        byte step,
        bool directional)
    {
        var blocked = BlocksLeftLower(probes.LowerLeft)
            || BlocksUpper(probes.UpperLeft)
            || (jumpPhase >= halfPhase && BlocksLeftLower(probes.FloorLeft));

        if (blocked)
            return CollapseOrBlock(state, actionState, directional, collision: true, edge: false);

        var candidate = state.PlayerX - step;
        if (candidate < 0x10)
            return CollapseOrBlock(state, actionState, directional, collision: false, edge: true);

        var next = state with { PlayerX = (byte)candidate };
        return new(next, actionState, step != 0, false, false, false, false, false);
    }

    private static PlatformAirborneHorizontalResult StepRight(
        PlatformStageMap stage,
        PlatformHorizontalState state,
        PlatformCollisionDescriptors probes,
        byte actionState,
        byte jumpPhase,
        byte halfPhase,
        byte step,
        bool directional)
    {
        var cap = PlatformHorizontalMotion.ScrollHighCapForSubstate(stage.Substate);
        var finalCameraRegion = false;

        if (state.ScrollLow >= 0xF8)
        {
            if (state.ScrollHigh == cap)
            {
                finalCameraRegion = true;
            }
            else if (state.ScrollHigh > cap)
            {
                // $BF35 clears low scroll then enters the final-screen player-X path.
                state = state with { ScrollLow = 0 };
                finalCameraRegion = true;
            }
        }

        if (finalCameraRegion)
        {
            var candidate = state.PlayerX + step;
            if (candidate >= 0xE0)
                return new(state, actionState, false, false, false, true, false, false);

            var next = state with { PlayerX = (byte)candidate };
            return new(next, actionState, step != 0, false, false, false, false, false);
        }

        var blocked = BlocksRightLower(probes.LowerRight)
            || BlocksUpper(probes.UpperRight)
            || (jumpPhase >= halfPhase && BlocksRightLower(probes.FloorRight));

        if (blocked)
            return CollapseOrBlock(state, actionState, directional, collision: true, edge: false);

        var playerCandidate = state.PlayerX + step;
        if (playerCandidate < 0x80)
        {
            var next = state with { PlayerX = (byte)playerCandidate };
            return new(next, actionState, step != 0, false, false, false, false, false);
        }

        var sum = state.ScrollLow + step;
        var scrolled = state with
        {
            ScrollLow = (byte)sum,
            ScrollHigh = unchecked((byte)(state.ScrollHigh + (sum > 0xFF ? 1 : 0))),
        };
        return new(scrolled, actionState, false, step != 0, false, false, false, false);
    }

    private static PlatformAirborneHorizontalResult CollapseOrBlock(
        PlatformHorizontalState state,
        byte actionState,
        bool directional,
        bool collision,
        bool edge)
    {
        var nextAction = directional ? (byte)0x30 : actionState;
        return new(
            state,
            nextAction,
            false,
            false,
            collision,
            edge,
            directional,
            false);
    }

    private static PlatformAirborneHorizontalResult Unchanged(PlatformHorizontalState state, byte actionState) =>
        new(state, actionState, false, false, false, false, false, false);

    private static bool In(byte? descriptor, int lo, int hi) =>
        descriptor is byte value && value >= lo && value < hi;

    private static bool BlocksRightLower(byte? descriptor) =>
        In(descriptor, 0x80, 0x88) || In(descriptor, 0xE0, 0xF0);

    private static bool BlocksLeftLower(byte? descriptor) =>
        In(descriptor, 0x88, 0x90) || In(descriptor, 0xE0, 0xF0);

    private static bool BlocksUpper(byte? descriptor) => In(descriptor, 0xE0, 0xF0);
}

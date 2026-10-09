using SaintSeiyaNesReborn.OriginalSpec;

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformFallOutcome
{
    Falling,
    Landed,
    FellOut,
}

public readonly record struct PlatformCrouchDropState(
    PlatformHorizontalState Horizontal,
    byte PlayerY,
    byte PlayerYPage41,
    byte ActionState4D,
    byte DropButtonLatch038C,
    byte JumpPhase49,
    byte JumpLock038D,
    byte HazardFlag76,
    byte HorizontalAmount43);

public readonly record struct PlatformCrouchStepResult(
    PlatformCrouchDropState State,
    bool DropStarted);

public readonly record struct PlatformFallStepResult(
    PlatformCrouchDropState State,
    PlatformFallOutcome Outcome,
    bool PlayerCorrectedHorizontally,
    bool CameraCorrectedHorizontally);

/// <summary>
/// Crouch/drop-through and action-family $50 fall behavior from bank 3
/// $B829-$B8CC, reusing the same floor semantics as $B8CD.
/// </summary>
public static class PlatformCrouchDrop
{
    /// <summary>
    /// Executes one frame whose frame-start action latch was $20 (crouch).
    /// Right has facing priority over Left. Down release returns action to zero.
    /// </summary>
    public static PlatformCrouchStepResult StepCrouched(
        PlatformCrouchDropState state,
        PlatformInput input,
        PlatformCollisionDescriptors probes)
    {
        var horizontal = state.Horizontal;
        if ((input & PlatformInput.Right) != 0)
            horizontal = horizontal with { Facing42 = 0x40 };
        else if ((input & PlatformInput.Left) != 0)
            horizontal = horizontal with { Facing42 = 0x00 };

        state = state with { Horizontal = horizontal };

        if ((input & PlatformInput.Down) == 0)
            return new PlatformCrouchStepResult(state with { ActionState4D = 0 }, false);

        if ((input & PlatformInput.A) == 0)
            return new PlatformCrouchStepResult(state with { DropButtonLatch038C = 0 }, false);

        if (state.DropButtonLatch038C != 0)
            return new PlatformCrouchStepResult(state, false);

        // The latch is set before terrain/Y eligibility is tested; a rejected
        // Down+A therefore still requires A release before another attempt.
        state = state with { DropButtonLatch038C = 0x80 };

        if (probes.FloorCenter is byte floor && floor is >= 0xE0 and < 0xF0)
            return new PlatformCrouchStepResult(state, false);

        if (state.PlayerY >= 0x80)
            return new PlatformCrouchStepResult(state, false);

        state = state with
        {
            ActionState4D = 0x50,
            PlayerY = unchecked((byte)(state.PlayerY + 6)),
        };
        return new PlatformCrouchStepResult(state, true);
    }

    /// <summary>
    /// One action-family $50 fall frame. The original applies +3 Y first,
    /// performs at most one side-correction branch, then calls the shared floor
    /// resolver. Probes are the pre-simulation samples for this frame.
    /// </summary>
    public static PlatformFallStepResult StepFall(
        PlatformStageMap stage,
        PlatformCrouchDropState state,
        PlatformSaintIndex saint,
        PlatformCollisionDescriptors probes,
        byte frameCounter3C,
        byte dynamicFloorY039B = 0)
    {
        if ((state.ActionState4D & 0xF0) != 0x50)
            throw new InvalidOperationException("Fall step requires action family $50.");

        state = state with { PlayerY = unchecked((byte)(state.PlayerY + 3)) };

        var horizontal = state.Horizontal;
        var playerCorrected = false;
        var cameraCorrected = false;

        // $B884 checks the wide left floor probe first. If it corrects, the
        // routine jumps directly to floor resolution and never checks right.
        if (BlocksLeftCorrection(probes.FloorLeft))
        {
            if (horizontal.PlayerX >= 0x80)
            {
                var step = PlatformMovementIncrements.FromFrame(saint, frameCounter3C).Grounded0387;
                var before = horizontal;
                horizontal = AdvanceCamera(stage, horizontal, step);
                cameraCorrected = horizontal != before;
                state = state with { Horizontal = horizontal, HorizontalAmount43 = 1 };
            }
            else
            {
                horizontal = horizontal with { PlayerX = unchecked((byte)(horizontal.PlayerX + 1)) };
                playerCorrected = true;
                state = state with { Horizontal = horizontal, HorizontalAmount43 = 0 };
            }
        }
        else if (BlocksRightCorrection(probes.FloorRight))
        {
            horizontal = horizontal with { PlayerX = unchecked((byte)(horizontal.PlayerX - 1)) };
            playerCorrected = true;
            state = state with { Horizontal = horizontal, HorizontalAmount43 = 0 };
        }

        var floor = ResolveFloor(state, probes.FloorCenter, dynamicFloorY039B);
        return new PlatformFallStepResult(
            floor.State,
            floor.Outcome,
            playerCorrected,
            cameraCorrected);
    }

    private static (PlatformCrouchDropState State, PlatformFallOutcome Outcome) ResolveFloor(
        PlatformCrouchDropState state,
        byte? floorDescriptor,
        byte dynamicFloorY039B)
    {
        var y = state.PlayerY;

        if (y < 0x86)
        {
            if (floorDescriptor is not byte descriptor)
                return (state, PlatformFallOutcome.Falling);

            if (descriptor == 0xFF)
                return Land(state with { PlayerY = dynamicFloorY039B });

            if (descriptor < 0x80)
                return (state, PlatformFallOutcome.Falling);

            var low = y & 0x0F;
            if (descriptor >= 0xF0)
            {
                if (low < 8)
                    return (state, PlatformFallOutcome.Falling);
                return Land(state with { PlayerY = (byte)((y & 0xF0) | 0x08) });
            }

            if (low >= 6)
                return (state, PlatformFallOutcome.Falling);
            return Land(state with { PlayerY = (byte)(y & 0xF0) });
        }

        if (floorDescriptor is 0xF8 or 0xF9)
        {
            var flag = state.HazardFlag76 == 0 ? (byte)1 : state.HazardFlag76;
            return Land(state with { PlayerY = 0x88, HazardFlag76 = flag });
        }

        if (y < 0xA0 || state.PlayerYPage41 != 0)
            return (state, PlatformFallOutcome.Falling);

        return (
            state with
            {
                PlayerY = 0xA0,
                JumpPhase49 = 0,
                ActionState4D = 0x80,
                HazardFlag76 = 0x80,
            },
            PlatformFallOutcome.FellOut);
    }

    private static (PlatformCrouchDropState State, PlatformFallOutcome Outcome) Land(
        PlatformCrouchDropState state) => (
        state with
        {
            JumpPhase49 = 0,
            ActionState4D = 0,
            JumpLock038D = 0,
        },
        PlatformFallOutcome.Landed);

    private static bool BlocksLeftCorrection(byte? descriptor) =>
        descriptor is byte d && (d is >= 0x88 and < 0x90 or >= 0xE0 and < 0xF0);

    private static bool BlocksRightCorrection(byte? descriptor) =>
        descriptor is byte d && (d is >= 0x80 and < 0x88 or >= 0xE0 and < 0xF0);

    private static PlatformHorizontalState AdvanceCamera(
        PlatformStageMap stage,
        PlatformHorizontalState horizontal,
        byte step)
    {
        // Same $ABC9 helper used elsewhere: equality at terminal cap stops the
        // camera; otherwise the low byte is advanced and carry increments $45.
        var cap = PlatformHorizontalMotion.ScrollHighCapForSubstate(stage.Substate);
        if (horizontal.ScrollLow >= 0xF8 && horizontal.ScrollHigh == cap)
            return horizontal;

        var sum = horizontal.ScrollLow + step;
        return horizontal with
        {
            ScrollLow = (byte)sum,
            ScrollHigh = unchecked((byte)(horizontal.ScrollHigh + (sum > 0xFF ? 1 : 0))),
        };
    }
}

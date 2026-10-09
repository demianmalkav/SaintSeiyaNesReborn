using SaintSeiyaNesReborn.OriginalSpec;

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformAirControlState(
    PlatformHorizontalState Horizontal,
    byte ActionState4D,
    byte HorizontalAmount43);

public readonly record struct PlatformAirControlResult(
    PlatformAirControlState State,
    bool PlayerMoved,
    bool CameraMoved,
    bool DirectionalFamilyCancelled,
    bool CollisionBlocked);

/// <summary>
/// Airborne horizontal control reconstructed from bank 3 $BDA2-$BF48.
/// Vertical jump state $30 is freely steerable at 0/1 px cadence; directional
/// states preserve their takeoff sign while live input modulates magnitude.
/// </summary>
public static class PlatformAirControl
{
    public static PlatformAirControlResult Step(
        PlatformStageMap stage,
        PlatformAirControlState state,
        PlatformSaintIndex saint,
        PlatformInput input,
        PlatformCollisionDescriptors probes,
        byte jumpPhase49,
        int halfPhase,
        byte frameCounter3C)
    {
        if ((state.ActionState4D & 0xF0) != 0x30)
            throw new InvalidOperationException("Air control requires action family $30-$33.");

        var directionBits = state.ActionState4D & 0x03;
        if (directionBits == 0)
            return StepFreeVerticalFamily(stage, state, input, probes, jumpPhase49, halfPhase, frameCounter3C);

        var increments = PlatformMovementIncrements.FromFrame(saint, frameCounter3C);
        return directionBits == 1
            ? StepLockedRight(stage, state, input, probes, jumpPhase49, halfPhase, increments)
            : StepLockedLeft(state, input, probes, jumpPhase49, halfPhase, increments);
    }

    private static PlatformAirControlResult StepFreeVerticalFamily(
        PlatformStageMap stage,
        PlatformAirControlState state,
        PlatformInput input,
        PlatformCollisionDescriptors probes,
        byte phase,
        int halfPhase,
        byte frameCounter)
    {
        // $BDAB tests Right first, but a lower-right rejection falls through to
        // the Left test instead of returning. Thus Right has priority only when
        // its first lower-side gate accepts the attempt.
        var freeStep = (byte)(frameCounter & 1);
        var rejectedInput = false;

        if ((input & PlatformInput.Right) != 0)
        {
            if (!BlocksAirRight(probes.LowerRight))
                return StepRight(stage, state, probes, phase, halfPhase, freeStep, cancelOnBlock: false);
            rejectedInput = true;
        }

        if ((input & PlatformInput.Left) != 0)
        {
            if (!BlocksAirLeft(probes.LowerLeft))
                return StepLeft(state, probes, phase, halfPhase, freeStep, cancelOnBlock: false);
            rejectedInput = true;
        }

        state = state with { HorizontalAmount43 = 0 };
        if (phase < halfPhase)
            return Unmoved(state, collisionBlocked: rejectedInput);

        // If neither live-direction attempt survives its initial lower probe,
        // second-half side correction still runs exactly like the no-input path.
        var horizontal = state.Horizontal;
        var playerMoved = false;
        var cameraMoved = false;
        var amount43 = (byte)0;

        if (BlocksAirLeft(probes.LowerLeft))
        {
            if (horizontal.PlayerX >= 0x80)
            {
                var before = horizontal;
                horizontal = AdvanceCameraLikeOriginal(stage, horizontal, 1);
                cameraMoved = horizontal != before;
                amount43 = 1; // original writes $43=1 even if terminal camera does not advance
            }
            else
            {
                horizontal = horizontal with { PlayerX = unchecked((byte)(horizontal.PlayerX + 1)) };
                playerMoved = true;
                amount43 = 0;
            }
        }

        if (BlocksAirRight(probes.LowerRight))
        {
            horizontal = horizontal with { PlayerX = unchecked((byte)(horizontal.PlayerX - 1)) };
            playerMoved = true;
        }

        return new PlatformAirControlResult(
            state with { Horizontal = horizontal, HorizontalAmount43 = amount43 },
            playerMoved,
            cameraMoved,
            DirectionalFamilyCancelled: false,
            CollisionBlocked: rejectedInput);
    }

    private static PlatformAirControlResult StepLockedRight(
        PlatformStageMap stage,
        PlatformAirControlState state,
        PlatformInput input,
        PlatformCollisionDescriptors probes,
        byte phase,
        int halfPhase,
        PlatformMovementIncrements increments)
    {
        // Right-family branch checks opposite Left first. Holding both therefore
        // selects the decelerated/opposite-input amount $0389.
        var step = (input & PlatformInput.Left) != 0
            ? increments.Airborne0389
            : (input & PlatformInput.Right) != 0
                ? increments.Airborne0388
                : increments.Grounded0387;

        return StepRight(stage, state, probes, phase, halfPhase, step, cancelOnBlock: true);
    }

    private static PlatformAirControlResult StepLockedLeft(
        PlatformAirControlState state,
        PlatformInput input,
        PlatformCollisionDescriptors probes,
        byte phase,
        int halfPhase,
        PlatformMovementIncrements increments)
    {
        // Left-family branch checks opposite Right first. State $33 (both held at
        // takeoff) belongs to this family because only lowbits == 1 chooses right.
        var step = (input & PlatformInput.Right) != 0
            ? increments.Airborne0389
            : (input & PlatformInput.Left) != 0
                ? increments.Airborne0388
                : increments.Grounded0387;

        return StepLeft(state, probes, phase, halfPhase, step, cancelOnBlock: true);
    }

    private static PlatformAirControlResult StepRight(
        PlatformStageMap stage,
        PlatformAirControlState state,
        PlatformCollisionDescriptors probes,
        byte phase,
        int halfPhase,
        byte step,
        bool cancelOnBlock)
    {
        state = state with { HorizontalAmount43 = step };

        var blocked = BlocksAirRight(probes.LowerRight)
            || BlocksAirUpper(probes.UpperRight)
            || (phase >= halfPhase && BlocksAirRight(probes.FloorRight));

        if (blocked)
            return cancelOnBlock ? CancelDirectional(state) : BlockFreeSteer(state);

        var horizontal = state.Horizontal;
        var cap = PlatformHorizontalMotion.ScrollHighCapForSubstate(stage.Substate);
        var atFinalCamera = false;

        if (horizontal.ScrollLow >= 0xF8)
        {
            if (horizontal.ScrollHigh == cap)
            {
                atFinalCamera = true;
            }
            else if (horizontal.ScrollHigh > cap)
            {
                horizontal = horizontal with { ScrollLow = 0 };
                atFinalCamera = true;
            }
        }

        if (atFinalCamera)
        {
            state = state with { HorizontalAmount43 = 0 };
            var candidate = horizontal.PlayerX + step;
            if (candidate >= 0xE0)
                return Unmoved(state with { Horizontal = horizontal });

            horizontal = horizontal with { PlayerX = (byte)candidate };
            return MovedPlayer(state with { Horizontal = horizontal }, step != 0);
        }

        var localCandidate = horizontal.PlayerX + step;
        if (localCandidate < 0x80)
        {
            horizontal = horizontal with { PlayerX = (byte)localCandidate };
            // $BF20 clears $43 when movement remains screen-local.
            state = state with { Horizontal = horizontal, HorizontalAmount43 = 0 };
            return MovedPlayer(state, step != 0);
        }

        var before = horizontal;
        horizontal = AdvanceCameraLikeOriginal(stage, horizontal, step);
        return new PlatformAirControlResult(
            state with { Horizontal = horizontal },
            PlayerMoved: false,
            CameraMoved: horizontal != before,
            DirectionalFamilyCancelled: false,
            CollisionBlocked: false);
    }

    private static PlatformAirControlResult StepLeft(
        PlatformAirControlState state,
        PlatformCollisionDescriptors probes,
        byte phase,
        int halfPhase,
        byte step,
        bool cancelOnBlock)
    {
        // Original left-family path sets $43=0 before selecting magnitude.
        state = state with { HorizontalAmount43 = 0 };

        var blocked = BlocksAirLeft(probes.LowerLeft)
            || BlocksAirUpper(probes.UpperLeft)
            || (phase >= halfPhase && BlocksAirLeft(probes.FloorLeft));

        if (blocked)
            return cancelOnBlock ? CancelDirectional(state) : BlockFreeSteer(state);

        var horizontal = state.Horizontal;
        var candidate = horizontal.PlayerX - step;
        if (candidate < 0x10)
            return cancelOnBlock ? CancelDirectional(state) : BlockFreeSteer(state);

        horizontal = horizontal with { PlayerX = unchecked((byte)candidate) };
        return MovedPlayer(state with { Horizontal = horizontal }, step != 0);
    }

    private static PlatformHorizontalState AdvanceCameraLikeOriginal(
        PlatformStageMap stage,
        PlatformHorizontalState horizontal,
        byte step)
    {
        if (step == 0)
            return horizontal;

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

    private static bool BlocksAirRight(byte? descriptor) =>
        descriptor is byte d && (d is >= 0x80 and < 0x88 or >= 0xE0 and < 0xF0);

    private static bool BlocksAirLeft(byte? descriptor) =>
        descriptor is byte d && (d is >= 0x88 and < 0x90 or >= 0xE0 and < 0xF0);

    private static bool BlocksAirUpper(byte? descriptor) =>
        descriptor is byte d && d is >= 0xE0 and < 0xF0;

    private static PlatformAirControlResult CancelDirectional(PlatformAirControlState state) => new(
        state with { ActionState4D = 0x30, HorizontalAmount43 = 0 },
        PlayerMoved: false,
        CameraMoved: false,
        DirectionalFamilyCancelled: true,
        CollisionBlocked: true);

    private static PlatformAirControlResult BlockFreeSteer(PlatformAirControlState state) => new(
        state with { HorizontalAmount43 = 0 },
        PlayerMoved: false,
        CameraMoved: false,
        DirectionalFamilyCancelled: false,
        CollisionBlocked: true);

    private static PlatformAirControlResult Unmoved(
        PlatformAirControlState state,
        bool collisionBlocked = false) => new(
        state, false, false, false, collisionBlocked);

    private static PlatformAirControlResult MovedPlayer(PlatformAirControlState state, bool moved = true) => new(
        state, moved, false, false, false);
}

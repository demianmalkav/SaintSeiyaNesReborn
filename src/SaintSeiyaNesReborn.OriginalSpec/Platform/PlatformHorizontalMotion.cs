namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformMovementIncrements(
    byte Grounded0387,
    byte Airborne0388,
    byte Airborne0389)
{
    /// <summary>
    /// Reconstructs bank 1 $9211-$922F after fixed $C411-$C418 has set
    /// $30 = ($3C & 1) + 1.
    /// </summary>
    public static PlatformMovementIncrements FromFrame(PlatformSaintIndex saint, byte frameCounter3C)
    {
        var alternating = (byte)((frameCounter3C & 1) + 1);
        if (saint == PlatformSaintIndex.Shun)
            return new PlatformMovementIncrements(alternating, 2, 1);

        return new PlatformMovementIncrements(1, alternating, (byte)(frameCounter3C & 1));
    }
}

public readonly record struct PlatformHorizontalState(
    byte PlayerX,
    byte ScrollLow,
    byte ScrollHigh,
    byte Facing42)
{
    public int ScrollX => (ScrollHigh << 8) | ScrollLow;
    public int WorldPlayerX => ScrollX + PlayerX;
    public bool FacingRight => Facing42 == 0x40;
}

public readonly record struct PlatformHorizontalStepResult(
    PlatformHorizontalState State,
    bool PlayerMoved,
    bool CameraMoved,
    bool CollisionBlocked,
    bool EdgeBlocked);

/// <summary>
/// Grounded horizontal movement and forward-only camera handoff reconstructed
/// from PRG bank 3 $AB3F-$AC50.
/// </summary>
public static class PlatformHorizontalMotion
{
    private static readonly byte[] OriginalScrollHighCaps =
    [
        0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
        0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0E,
        0x0A, 0x0A, 0x0A, 0x01, 0x0A, 0x00,
    ];

    public static byte ScrollHighCapForSubstate(int substate)
    {
        if ((uint)substate >= OriginalScrollHighCaps.Length)
            throw new ArgumentOutOfRangeException(nameof(substate), substate, "Platform substate must be $00-$11.");
        return OriginalScrollHighCaps[substate];
    }

    /// <summary>
    /// One grounded horizontal update. Right has priority when both directions
    /// are held, matching the original BIT ordering.
    /// </summary>
    public static PlatformHorizontalStepResult StepGrounded(
        PlatformStageMap stage,
        PlatformHorizontalState state,
        PlatformInput input,
        PlatformCollisionDescriptors probes,
        byte groundedStep)
    {
        if (groundedStep == 0)
            throw new ArgumentOutOfRangeException(nameof(groundedStep), "Grounded movement step must be non-zero.");

        if ((input & PlatformInput.Right) != 0)
            return StepRight(stage, state with { Facing42 = 0x40 }, probes, groundedStep);

        if ((input & PlatformInput.Left) != 0)
            return StepLeft(stage, state with { Facing42 = 0x00 }, probes, groundedStep);

        return new PlatformHorizontalStepResult(state, false, false, false, false);
    }

    private static PlatformHorizontalStepResult StepRight(
        PlatformStageMap stage,
        PlatformHorizontalState state,
        PlatformCollisionDescriptors probes,
        byte step)
    {
        if (!stage.CanMoveRight(probes))
            return new PlatformHorizontalStepResult(state, false, false, true, false);

        var cap = ScrollHighCapForSubstate(stage.Substate);
        var atFinalCameraRegion = false;

        if (state.ScrollLow >= 0xF8)
        {
            if (state.ScrollHigh == cap)
            {
                atFinalCameraRegion = true;
            }
            else if (state.ScrollHigh > cap)
            {
                // Defensive/overshoot branch at $ABB0: the original clears only
                // the low scroll byte, then falls into the final-screen path.
                state = state with { ScrollLow = 0 };
                atFinalCameraRegion = true;
            }
        }

        if (atFinalCameraRegion)
        {
            if (state.PlayerX >= 0xE0)
                return new PlatformHorizontalStepResult(state, false, false, false, true);

            state = state with { PlayerX = unchecked((byte)(state.PlayerX + step)) };
            return new PlatformHorizontalStepResult(state, true, false, false, false);
        }

        if (state.PlayerX < 0x80)
        {
            state = state with { PlayerX = unchecked((byte)(state.PlayerX + step)) };
            return new PlatformHorizontalStepResult(state, true, false, false, false);
        }

        var sum = state.ScrollLow + step;
        state = state with
        {
            ScrollLow = (byte)sum,
            ScrollHigh = unchecked((byte)(state.ScrollHigh + (sum > 0xFF ? 1 : 0))),
        };
        return new PlatformHorizontalStepResult(state, false, true, false, false);
    }

    private static PlatformHorizontalStepResult StepLeft(
        PlatformStageMap stage,
        PlatformHorizontalState state,
        PlatformCollisionDescriptors probes,
        byte step)
    {
        if (!stage.CanMoveLeft(probes))
            return new PlatformHorizontalStepResult(state, false, false, true, false);

        // The grounded left path never scrolls the camera backward. It only
        // reduces screen-local player X until the original left boundary.
        if (state.PlayerX < 0x10)
            return new PlatformHorizontalStepResult(state, false, false, false, true);

        state = state with { PlayerX = unchecked((byte)(state.PlayerX - step)) };
        return new PlatformHorizontalStepResult(state, true, false, false, false);
    }
}

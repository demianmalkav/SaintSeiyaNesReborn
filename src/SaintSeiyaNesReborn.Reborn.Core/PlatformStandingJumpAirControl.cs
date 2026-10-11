namespace SaintSeiyaNesReborn.Reborn.Core;

/// <summary>
/// Horizontal drift magnitude for an ordinary standing jump. The profile is
/// semantic and phase-based; it does not expose controller masks or frame-counter
/// storage from the original implementation.
/// </summary>
public readonly record struct RebornStandingJumpAirControlProfile
{
    public int EvenDriftPixels { get; }
    public int OddDriftPixels { get; }

    public RebornStandingJumpAirControlProfile(int evenDriftPixels, int oddDriftPixels)
    {
        if (evenDriftPixels < 0)
            throw new ArgumentOutOfRangeException(nameof(evenDriftPixels));
        if (oddDriftPixels < 0)
            throw new ArgumentOutOfRangeException(nameof(oddDriftPixels));

        EvenDriftPixels = evenDriftPixels;
        OddDriftPixels = oddDriftPixels;
    }

    public int DriftFor(RebornMotionPhase phase) => phase switch
    {
        RebornMotionPhase.Even => EvenDriftPixels,
        RebornMotionPhase.Odd => OddDriftPixels,
        _ => throw new ArgumentOutOfRangeException(nameof(phase)),
    };
}

/// <summary>
/// Camera-independent horizontal state while an ordinary standing jump is active.
/// Facing is the takeoff facing and is intentionally preserved by air-control
/// input; only world X and logical phase evolve in this bounded slice.
/// </summary>
public readonly record struct RebornStandingJumpAirState(
    int WorldX,
    RebornFacing Facing,
    RebornMotionPhase Phase);

public readonly record struct RebornStandingJumpAirStepResult(
    RebornStandingJumpAirState State,
    int AppliedDelta);

/// <summary>
/// Free-space airborne horizontal control for the ordinary standing jump.
/// Directional intent requests parity-shaped drift but does not rotate facing.
/// Collision, stage edges, camera policy and landing correction are separate
/// layers. Every call advances exactly one logical phase.
/// </summary>
public static class RebornStandingJumpAirControl
{
    public static RebornStandingJumpAirStepResult Step(
        RebornStandingJumpAirState state,
        RebornHorizontalInput input,
        RebornStandingJumpAirControlProfile profile)
    {
        var drift = profile.DriftFor(state.Phase);
        var delta = input switch
        {
            RebornHorizontalInput.Neutral => 0,
            RebornHorizontalInput.Left => -drift,
            RebornHorizontalInput.Right => drift,
            _ => throw new ArgumentOutOfRangeException(nameof(input)),
        };

        var nextPhase = state.Phase switch
        {
            RebornMotionPhase.Even => RebornMotionPhase.Odd,
            RebornMotionPhase.Odd => RebornMotionPhase.Even,
            _ => throw new ArgumentOutOfRangeException(nameof(state), "Unsupported standing-jump air-control phase."),
        };

        return new RebornStandingJumpAirStepResult(
            state with
            {
                WorldX = checked(state.WorldX + delta),
                Phase = nextPhase,
            },
            delta);
    }
}

/// <summary>
/// Minimal composed ordinary standing-jump motion state. Vertical motion is
/// applied first, then horizontal air control, matching the frozen observable
/// ordering while keeping both primitives independently reusable.
/// </summary>
public readonly record struct RebornStandingJumpMotionState(
    RebornStandingJumpState Vertical,
    RebornStandingJumpAirState Horizontal);

public readonly record struct RebornStandingJumpMotionStepResult(
    RebornStandingJumpMotionState State,
    int AppliedRisePixels,
    int AppliedHorizontalDelta);

public static class RebornStandingJumpMotion
{
    public static RebornStandingJumpMotionStepResult Initiate(
        int groundedVerticalPosition,
        int worldX,
        RebornFacing facing,
        RebornMotionPhase phase,
        RebornHorizontalInput takeoffHorizontalInput,
        RebornStandingJumpProfile verticalProfile,
        RebornStandingJumpAirControlProfile horizontalProfile)
    {
        // A non-neutral takeoff belongs to the directional-jump family and is a
        // deliberately separate checkpoint.
        if (takeoffHorizontalInput != RebornHorizontalInput.Neutral)
            throw new InvalidOperationException("Ordinary standing jump requires neutral horizontal takeoff intent.");

        var vertical = RebornStandingJump.Initiate(groundedVerticalPosition, verticalProfile);
        var horizontal = RebornStandingJumpAirControl.Step(
            new RebornStandingJumpAirState(worldX, facing, phase),
            takeoffHorizontalInput,
            horizontalProfile);

        return new RebornStandingJumpMotionStepResult(
            new RebornStandingJumpMotionState(vertical.State, horizontal.State),
            vertical.AppliedRisePixels,
            horizontal.AppliedDelta);
    }

    public static RebornStandingJumpMotionStepResult Step(
        RebornStandingJumpMotionState state,
        RebornHorizontalInput horizontalInput,
        RebornStandingJumpProfile verticalProfile,
        RebornStandingJumpAirControlProfile horizontalProfile)
    {
        // The canonical observable order for an active jump frame is vertical
        // displacement first and horizontal air control second.
        var vertical = RebornStandingJump.Step(state.Vertical, verticalProfile);
        var horizontal = RebornStandingJumpAirControl.Step(
            state.Horizontal,
            horizontalInput,
            horizontalProfile);

        return new RebornStandingJumpMotionStepResult(
            new RebornStandingJumpMotionState(vertical.State, horizontal.State),
            vertical.AppliedRisePixels,
            horizontal.AppliedDelta);
    }
}

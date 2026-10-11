namespace SaintSeiyaNesReborn.Reborn.Core;

/// <summary>
/// Horizontal intent understood by REBORN gameplay. This is deliberately not a
/// controller-bit mask; host input mapping belongs outside the domain.
/// </summary>
public enum RebornHorizontalInput : byte
{
    Neutral = 0,
    Left = 1,
    Right = 2,
}

public enum RebornFacing : byte
{
    Left = 0,
    Right = 1,
}

/// <summary>
/// Two-phase cadence used by the frozen original locomotion profile. The names
/// describe deterministic logical-frame parity only; they do not expose original
/// RAM or frame-counter storage.
/// </summary>
public enum RebornMotionPhase : byte
{
    Even = 0,
    Odd = 1,
}

/// <summary>
/// Grounded horizontal displacement per logical-frame phase. Most characters use
/// 1/1; the frozen original profile with a distinct grounded cadence uses 1/2.
/// </summary>
public readonly record struct RebornGroundedHorizontalProfile
{
    public int EvenStep { get; }
    public int OddStep { get; }

    public RebornGroundedHorizontalProfile(int evenStep, int oddStep)
    {
        if (evenStep <= 0)
            throw new ArgumentOutOfRangeException(nameof(evenStep), "Horizontal step must be positive.");
        if (oddStep <= 0)
            throw new ArgumentOutOfRangeException(nameof(oddStep), "Horizontal step must be positive.");

        EvenStep = evenStep;
        OddStep = oddStep;
    }

    public int StepFor(RebornMotionPhase phase) => phase switch
    {
        RebornMotionPhase.Even => EvenStep,
        RebornMotionPhase.Odd => OddStep,
        _ => throw new ArgumentOutOfRangeException(nameof(phase)),
    };
}

/// <summary>
/// Minimum modern gameplay state required by the first platform slice.
/// WorldX is presentation-independent world-space position: camera handoff and
/// screen-local anchoring are intentionally outside this contract.
/// </summary>
public readonly record struct RebornPlatformPlayerHorizontalState(
    int WorldX,
    RebornFacing Facing,
    RebornMotionPhase Phase,
    bool IsLocomoting);

public readonly record struct RebornPlatformHorizontalStepResult(
    RebornPlatformPlayerHorizontalState State,
    int AppliedDelta);

/// <summary>
/// Deterministic free-space grounded horizontal locomotion. Collision, stage
/// boundaries, camera policy, vertical motion, attacks and rendering are separate
/// later slices. Equal state + equal intent + equal profile always yields equal
/// output.
/// </summary>
public static class RebornPlatformPlayerHorizontalLocomotion
{
    public static RebornPlatformHorizontalStepResult Step(
        RebornPlatformPlayerHorizontalState state,
        RebornHorizontalInput input,
        RebornGroundedHorizontalProfile profile)
    {
        var step = profile.StepFor(state.Phase);
        var nextPhase = state.Phase switch
        {
            RebornMotionPhase.Even => RebornMotionPhase.Odd,
            RebornMotionPhase.Odd => RebornMotionPhase.Even,
            _ => throw new ArgumentOutOfRangeException(nameof(state), "Unsupported horizontal motion phase."),
        };

        return input switch
        {
            RebornHorizontalInput.Neutral => new(
                state with
                {
                    Phase = nextPhase,
                    IsLocomoting = false,
                },
                0),

            RebornHorizontalInput.Left => new(
                state with
                {
                    WorldX = checked(state.WorldX - step),
                    Facing = RebornFacing.Left,
                    Phase = nextPhase,
                    IsLocomoting = true,
                },
                -step),

            RebornHorizontalInput.Right => new(
                state with
                {
                    WorldX = checked(state.WorldX + step),
                    Facing = RebornFacing.Right,
                    Phase = nextPhase,
                    IsLocomoting = true,
                },
                step),

            _ => throw new ArgumentOutOfRangeException(nameof(input)),
        };
    }
}

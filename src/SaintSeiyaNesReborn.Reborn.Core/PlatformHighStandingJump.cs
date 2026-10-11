namespace SaintSeiyaNesReborn.Reborn.Core;

/// <summary>
/// Semantic high-standing-jump profile families preserved from the frozen original.
/// These names describe gameplay-equivalent curve sharing only; they do not expose
/// original selectors, action bytes, table pointers or storage layout.
/// </summary>
public enum RebornHighStandingJumpProfileFamily
{
    Seiya,
    ShunIkki,
    HyogaShiryu,
}

/// <summary>
/// Semantic takeoff request for the bounded high standing jump. A valid high
/// standing takeoff requires a jump request, upward intent and no horizontal
/// takeoff direction. Ground/support gating remains a separate gameplay layer.
/// </summary>
public readonly record struct RebornHighStandingJumpTakeoffIntent(
    bool JumpRequested,
    bool UpwardIntent,
    RebornHorizontalInput HorizontalInput)
{
    public bool IsValid =>
        JumpRequested
        && UpwardIntent
        && HorizontalInput == RebornHorizontalInput.Neutral;
}

/// <summary>
/// A high-standing-jump family plus its presentation-independent positive-up
/// displacement trajectory. The bounded trajectory primitive is intentionally
/// reused from the already-frozen standing-jump slice.
/// </summary>
public sealed class RebornHighStandingJumpProfile
{
    public RebornHighStandingJumpProfile(
        RebornHighStandingJumpProfileFamily family,
        IEnumerable<int> risePixelsPerTick)
    {
        if (!Enum.IsDefined(typeof(RebornHighStandingJumpProfileFamily), family))
            throw new ArgumentOutOfRangeException(nameof(family));

        Family = family;
        Trajectory = new RebornStandingJumpProfile(risePixelsPerTick);
    }

    public RebornHighStandingJumpProfileFamily Family { get; }
    public RebornStandingJumpProfile Trajectory { get; }

    public IReadOnlyList<int> RisePixelsPerTick => Trajectory.RisePixelsPerTick;
    public int TickCount => Trajectory.TickCount;
    public int PeakRisePixels => Trajectory.PeakRisePixels;
    public int ApexTick => Trajectory.ApexTick;
    public int NetRiseAfterTrajectory => Trajectory.NetRiseAfterTrajectory;
}

/// <summary>
/// Deterministic high standing jump. Initiation validates only the semantic
/// takeoff shape owned by this slice, then consumes the first table sample in the
/// same logical tick. Collision, landing and terminal falling are deliberately
/// outside this contract.
/// </summary>
public static class RebornHighStandingJump
{
    public static RebornStandingJumpStepResult Initiate(
        int groundedVerticalPosition,
        RebornHighStandingJumpTakeoffIntent intent,
        RebornHighStandingJumpProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (!intent.IsValid)
        {
            throw new InvalidOperationException(
                "High standing jump requires jump + upward intent with neutral horizontal takeoff.");
        }

        return RebornStandingJump.Initiate(groundedVerticalPosition, profile.Trajectory);
    }

    public static RebornStandingJumpStepResult Step(
        RebornStandingJumpState state,
        RebornHighStandingJumpProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return RebornStandingJump.Step(state, profile.Trajectory);
    }
}

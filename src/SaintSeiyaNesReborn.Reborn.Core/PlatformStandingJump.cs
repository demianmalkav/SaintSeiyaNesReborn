namespace SaintSeiyaNesReborn.Reborn.Core;

/// <summary>
/// Presentation-independent vertical trajectory for the ordinary standing jump.
/// Positive values mean upward world-space displacement. The profile owns only
/// the bounded table-controlled segment; landing, terminal fall and collision
/// response are separate gameplay layers.
/// </summary>
public sealed class RebornStandingJumpProfile
{
    private readonly int[] _risePixelsPerTick;

    public RebornStandingJumpProfile(IEnumerable<int> risePixelsPerTick)
    {
        ArgumentNullException.ThrowIfNull(risePixelsPerTick);
        _risePixelsPerTick = risePixelsPerTick.ToArray();
        if (_risePixelsPerTick.Length == 0)
            throw new ArgumentException("Standing-jump trajectory must contain at least one logical tick.", nameof(risePixelsPerTick));

        var cumulative = 0;
        var peak = int.MinValue;
        var apexTick = 0;
        for (var i = 0; i < _risePixelsPerTick.Length; i++)
        {
            cumulative = checked(cumulative + _risePixelsPerTick[i]);
            if (cumulative > peak)
            {
                peak = cumulative;
                apexTick = i + 1;
            }
        }

        PeakRisePixels = peak;
        ApexTick = apexTick;
        NetRiseAfterTrajectory = cumulative;
    }

    public IReadOnlyList<int> RisePixelsPerTick => _risePixelsPerTick;
    public int TickCount => _risePixelsPerTick.Length;
    public int PeakRisePixels { get; }
    public int ApexTick { get; }
    public int NetRiseAfterTrajectory { get; }

    public int RiseAt(int zeroBasedTick)
    {
        if ((uint)zeroBasedTick >= (uint)_risePixelsPerTick.Length)
            throw new ArgumentOutOfRangeException(nameof(zeroBasedTick));
        return _risePixelsPerTick[zeroBasedTick];
    }
}

/// <summary>
/// Minimal semantic state for the bounded ordinary standing-jump trajectory.
/// VerticalPosition uses a positive-up world-space convention. TrajectoryTick is
/// the count of table samples already consumed.
/// </summary>
public readonly record struct RebornStandingJumpState(
    int VerticalPosition,
    int TrajectoryTick,
    bool IsActive,
    bool TableComplete);

public readonly record struct RebornStandingJumpStepResult(
    RebornStandingJumpState State,
    int AppliedRisePixels);

/// <summary>
/// Deterministic ordinary standing-jump trajectory. Initiation consumes the first
/// trajectory sample in the same logical tick, matching the frozen behavior while
/// keeping storage layout and screen-coordinate conventions outside Core.
/// </summary>
public static class RebornStandingJump
{
    public static RebornStandingJumpStepResult Initiate(
        int groundedVerticalPosition,
        RebornStandingJumpProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return Step(
            new RebornStandingJumpState(
                groundedVerticalPosition,
                TrajectoryTick: 0,
                IsActive: true,
                TableComplete: false),
            profile);
    }

    public static RebornStandingJumpStepResult Step(
        RebornStandingJumpState state,
        RebornStandingJumpProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (!state.IsActive || state.TableComplete)
            throw new InvalidOperationException("Standing-jump table is not active.");
        if ((uint)state.TrajectoryTick >= (uint)profile.TickCount)
            throw new InvalidOperationException("Standing-jump trajectory tick is outside the profile.");

        var rise = profile.RiseAt(state.TrajectoryTick);
        var nextTick = checked(state.TrajectoryTick + 1);
        var complete = nextTick == profile.TickCount;
        var next = state with
        {
            VerticalPosition = checked(state.VerticalPosition + rise),
            TrajectoryTick = nextTick,
            IsActive = !complete,
            TableComplete = complete,
        };

        return new RebornStandingJumpStepResult(next, rise);
    }
}

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformJumpKind
{
    Standing,
    High,
    Directional,
}

/// <summary>
/// Frame-exact vertical displacement profile reconstructed from the original
/// platform jump tables. Values use a semantic positive-up convention:
/// +N means rise N screen pixels this frame; -N means fall N pixels.
/// </summary>
public sealed class PlatformJumpProfile
{
    private readonly sbyte[] _risePerFrame;

    public PlatformJumpKind Kind { get; }
    public PlatformSaintIndex Saint { get; }
    public IReadOnlyList<sbyte> RisePerFrame => _risePerFrame;
    public int DurationFrames => _risePerFrame.Length;

    public int PeakRisePixels { get; }
    public int ApexFrame { get; }
    public int NetRiseAfterTable { get; }

    private PlatformJumpProfile(PlatformJumpKind kind, PlatformSaintIndex saint, sbyte[] risePerFrame)
    {
        Kind = kind;
        Saint = saint;
        _risePerFrame = risePerFrame;

        var cumulative = 0;
        var peak = int.MinValue;
        var apexFrame = 0;
        for (var i = 0; i < risePerFrame.Length; i++)
        {
            cumulative += risePerFrame[i];
            if (cumulative > peak)
            {
                peak = cumulative;
                apexFrame = i + 1;
            }
        }

        PeakRisePixels = peak;
        ApexFrame = apexFrame;
        NetRiseAfterTable = cumulative;
    }

    public int ScreenYDeltaAtFrame(int zeroBasedFrame)
    {
        if ((uint)zeroBasedFrame >= _risePerFrame.Length)
            throw new ArgumentOutOfRangeException(nameof(zeroBasedFrame));
        return -_risePerFrame[zeroBasedFrame];
    }

    public int CumulativeRiseAfterFrames(int frameCount)
    {
        if (frameCount < 0)
            throw new ArgumentOutOfRangeException(nameof(frameCount));

        var count = Math.Min(frameCount, _risePerFrame.Length);
        var sum = 0;
        for (var i = 0; i < count; i++)
            sum += _risePerFrame[i];
        return sum;
    }

    public static PlatformJumpProfile Get(PlatformJumpKind kind, PlatformSaintIndex saint)
    {
        ValidateSaint(saint);
        return kind switch
        {
            PlatformJumpKind.Standing => new PlatformJumpProfile(kind, saint, Standing()),
            PlatformJumpKind.High => new PlatformJumpProfile(kind, saint, High(saint)),
            PlatformJumpKind.Directional => new PlatformJumpProfile(kind, saint, Directional(saint)),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }

    private static sbyte[] Standing() => Expand(
        (8, 2), (7, 2), (6, 1), (5, 1), (4, 1), (3, 2), (2, 2), (1, 3),
        (0, 4), (-1, 4), (-2, 5), (-3, 5));

    private static sbyte[] High(PlatformSaintIndex saint) => saint switch
    {
        PlatformSaintIndex.Seiya => Expand(
            (9, 1), (8, 1), (7, 2), (6, 2), (5, 2), (4, 5), (3, 5), (2, 5),
            (1, 5), (0, 6), (-1, 4), (-2, 12), (-3, 10)),

        PlatformSaintIndex.Shun or PlatformSaintIndex.Ikki => Expand(
            (9, 1), (8, 1), (7, 2), (6, 2), (5, 2), (4, 3), (3, 3), (2, 5),
            (1, 4), (0, 6), (-1, 4), (-2, 12), (-3, 5)),

        PlatformSaintIndex.Hyoga or PlatformSaintIndex.Shiryu => Expand(
            (9, 1), (7, 1), (6, 2), (5, 2), (4, 4), (3, 3), (2, 3), (1, 2),
            (0, 6), (-1, 4), (-2, 4), (-3, 8)),

        _ => throw new ArgumentOutOfRangeException(nameof(saint), saint, null),
    };

    private static sbyte[] Directional(PlatformSaintIndex saint) => saint switch
    {
        PlatformSaintIndex.Seiya or PlatformSaintIndex.Ikki => Expand(
            (4, 1), (3, 1), (2, 10), (1, 11), (0, 1), (1, 1), (0, 6),
            (-1, 4), (-2, 8), (-3, 11)),

        PlatformSaintIndex.Shun => Expand(
            (4, 1), (3, 1), (2, 10), (1, 6), (0, 6), (-1, 4), (-2, 6), (-3, 6)),

        PlatformSaintIndex.Hyoga or PlatformSaintIndex.Shiryu => Expand(
            (4, 1), (3, 1), (2, 10), (1, 6), (0, 1), (1, 1), (0, 6),
            (-1, 4), (-2, 6), (-3, 8)),

        _ => throw new ArgumentOutOfRangeException(nameof(saint), saint, null),
    };

    private static sbyte[] Expand(params (int Value, int Count)[] runs)
    {
        var total = runs.Sum(run => run.Count);
        var result = new sbyte[total];
        var at = 0;
        foreach (var (value, count) in runs)
        {
            if (value is < sbyte.MinValue or > sbyte.MaxValue || count < 0)
                throw new InvalidOperationException("Invalid reconstructed jump run.");
            for (var i = 0; i < count; i++)
                result[at++] = (sbyte)value;
        }
        return result;
    }

    private static void ValidateSaint(PlatformSaintIndex saint)
    {
        if ((byte)saint > 4)
            throw new ArgumentOutOfRangeException(nameof(saint), saint, "Platform Saint index must be in 0..4.");
    }
}

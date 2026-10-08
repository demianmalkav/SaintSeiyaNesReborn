namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformCollisionDescriptors(
    byte? FloorCenter,
    byte? LowerRight,
    byte? UpperRight,
    byte? FloorRight,
    byte? LowerLeft,
    byte? UpperLeft,
    byte? FloorLeft,
    byte? UpperCenter)
{
    public byte? this[CollisionProbeId id] => id switch
    {
        CollisionProbeId.FloorCenter => FloorCenter,
        CollisionProbeId.LowerRight => LowerRight,
        CollisionProbeId.UpperRight => UpperRight,
        CollisionProbeId.FloorRight => FloorRight,
        CollisionProbeId.LowerLeft => LowerLeft,
        CollisionProbeId.UpperLeft => UpperLeft,
        CollisionProbeId.FloorLeft => FloorLeft,
        CollisionProbeId.UpperCenter => UpperCenter,
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, null),
    };
}

/// <summary>
/// Clean-room spatial representation of one platform engine substate.
/// A page is 256 px wide and contains the original 16x11 logical descriptor grid.
/// </summary>
public sealed class PlatformStageMap
{
    public const int CellSizePixels = 16;
    public const int PageWidthPixels = PlatformStagePage.Columns * CellSizePixels;
    public const int PageHeightPixels = PlatformStagePage.Rows * CellSizePixels;

    private readonly PlatformStagePage[] _pages;

    public int Substate { get; }
    public int DataPrgBank { get; }
    public int MetatileDefinitionBase { get; }
    public int SpriteChrBank4K { get; }
    public int BackgroundChrBank4K { get; }
    public IReadOnlyList<PlatformStagePage> Pages => _pages;
    public int WidthPixels => _pages.Length * PageWidthPixels;

    public PlatformStageMap(
        int substate,
        IReadOnlyList<PlatformStagePage> pages,
        int dataPrgBank = -1,
        int metatileDefinitionBase = -1,
        int spriteChrBank4K = -1,
        int backgroundChrBank4K = -1)
    {
        if (substate is < 0 or > 0x11)
            throw new ArgumentOutOfRangeException(nameof(substate));
        if (pages.Count == 0)
            throw new ArgumentException("A platform stage must contain at least one page.", nameof(pages));

        _pages = pages.ToArray();
        for (var i = 0; i < _pages.Length; i++)
        {
            if (_pages[i].PageIndex != i)
                throw new ArgumentException(
                    $"Platform page sequence must be contiguous: expected page {i}, got {_pages[i].PageIndex}.",
                    nameof(pages));
        }

        Substate = substate;
        DataPrgBank = dataPrgBank;
        MetatileDefinitionBase = metatileDefinitionBase;
        SpriteChrBank4K = spriteChrBank4K;
        BackgroundChrBank4K = backgroundChrBank4K;
    }

    public byte? DescriptorAt(int worldX, int worldY)
    {
        if (worldX < 0 || worldY < 0)
            return null;

        var pageIndex = worldX / PageWidthPixels;
        if ((uint)pageIndex >= _pages.Length)
            return null;

        var column = (worldX % PageWidthPixels) / CellSizePixels;
        var row = worldY / CellSizePixels;
        if ((uint)row >= PlatformStagePage.Rows)
            return null;

        return _pages[pageIndex].DescriptorAt(column, row);
    }

    public PlatformStagePage? PageAtWorldX(int worldX)
    {
        if (worldX < 0)
            return null;
        var pageIndex = worldX / PageWidthPixels;
        return (uint)pageIndex < _pages.Length ? _pages[pageIndex] : null;
    }

    public PlatformCollisionDescriptors SamplePlayer(int playerX, int playerY, int scrollX)
    {
        var layout = CollisionProbeLayout.FromPlayer(playerX, playerY, scrollX);
        byte? Sample(CollisionProbeId id)
        {
            var point = layout[id];
            return DescriptorAt(point.X, point.Y);
        }

        return new PlatformCollisionDescriptors(
            FloorCenter: Sample(CollisionProbeId.FloorCenter),
            LowerRight: Sample(CollisionProbeId.LowerRight),
            UpperRight: Sample(CollisionProbeId.UpperRight),
            FloorRight: Sample(CollisionProbeId.FloorRight),
            LowerLeft: Sample(CollisionProbeId.LowerLeft),
            UpperLeft: Sample(CollisionProbeId.UpperLeft),
            FloorLeft: Sample(CollisionProbeId.FloorLeft),
            UpperCenter: Sample(CollisionProbeId.UpperCenter));
    }

    public static bool CanMoveRight(PlatformCollisionDescriptors probes, bool airborne = false)
    {
        if (airborne)
        {
            return !BlocksAirborneRightLower(probes.LowerRight)
                && !BlocksAirborneRightUpper(probes.UpperRight);
        }

        return !BlocksGroundedRight(probes.LowerRight)
            && !BlocksGroundedRight(probes.UpperRight);
    }

    public static bool CanMoveLeft(PlatformCollisionDescriptors probes, bool airborne = false)
    {
        if (airborne)
        {
            return !BlocksAirborneLeftLower(probes.LowerLeft)
                && !BlocksAirborneLeftUpper(probes.UpperLeft);
        }

        return !BlocksGroundedLeftLower(probes.LowerLeft)
            && !BlocksGroundedLeftUpper(probes.UpperLeft);
    }

    /// <summary>
    /// Reproduces the ordinary center-floor snap for player Y below $86.
    /// $FF and lower-screen $F8/$F9 behavior are separate dynamic/special paths.
    /// </summary>
    public static int? OrdinaryCenterFloorSnap(byte? descriptor, int playerY)
    {
        if (descriptor is null || descriptor == 0xFF || descriptor < 0x80)
            return null;

        var lowNibble = playerY & 0x0F;
        if (descriptor >= 0xF0)
            return lowNibble >= 8 ? (playerY & 0xF0) | 0x08 : null;

        return lowNibble < 6 ? playerY & 0xF0 : null;
    }

    private static bool In(byte? descriptor, int lo, int hi) =>
        descriptor is byte value && value >= lo && value < hi;

    private static bool BlocksGroundedRight(byte? descriptor) =>
        In(descriptor, 0x80, 0x88) || In(descriptor, 0xE0, 0xF0);

    private static bool BlocksGroundedLeftLower(byte? descriptor) =>
        In(descriptor, 0x78, 0x80)
        || In(descriptor, 0x88, 0x90)
        || In(descriptor, 0xE0, 0xF0);

    private static bool BlocksGroundedLeftUpper(byte? descriptor) =>
        In(descriptor, 0x88, 0x90) || In(descriptor, 0xE0, 0xF0);

    private static bool BlocksAirborneRightLower(byte? descriptor) => BlocksGroundedRight(descriptor);

    private static bool BlocksAirborneRightUpper(byte? descriptor) => In(descriptor, 0xE0, 0xF0);

    private static bool BlocksAirborneLeftLower(byte? descriptor) =>
        In(descriptor, 0x88, 0x90) || In(descriptor, 0xE0, 0xF0);

    private static bool BlocksAirborneLeftUpper(byte? descriptor) => In(descriptor, 0xE0, 0xF0);
}

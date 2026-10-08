namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum CollisionProbeId
{
    FloorCenter,
    LowerRight,
    UpperRight,
    FloorRight,
    LowerLeft,
    UpperLeft,
    FloorLeft,
    UpperCenter,
}

public readonly record struct ProbePoint(int X, int Y);

public readonly record struct CollisionProbeLayout(
    ProbePoint FloorCenter,
    ProbePoint LowerRight,
    ProbePoint UpperRight,
    ProbePoint FloorRight,
    ProbePoint LowerLeft,
    ProbePoint UpperLeft,
    ProbePoint FloorLeft,
    ProbePoint UpperCenter)
{
    /// <summary>
    /// Reconstructs the geometric sample points produced by bank 0 $B3E2+.
    /// X is expressed in world coordinates (scroll + player X). Y follows the
    /// original screen-space arithmetic used by the sampler.
    /// </summary>
    public static CollisionProbeLayout FromPlayer(int playerX, int playerY, int scrollX)
    {
        var worldX = scrollX + playerX;
        var upperY = Align16(playerY + 8);
        var lowerSideOffset = playerY == 0x88 ? 0x18 : 0x10;
        var lowerY = upperY + lowerSideOffset;
        var floorY = playerY + 0x20;

        return new CollisionProbeLayout(
            FloorCenter: new ProbePoint(worldX + 8, floorY),
            LowerRight: new ProbePoint(worldX + 16, lowerY),
            UpperRight: new ProbePoint(worldX + 16, upperY),
            FloorRight: new ProbePoint(worldX + 24, floorY),
            LowerLeft: new ProbePoint(worldX, lowerY),
            UpperLeft: new ProbePoint(worldX, upperY),
            FloorLeft: new ProbePoint(worldX - 8, floorY),
            UpperCenter: new ProbePoint(worldX + 8, upperY));
    }

    public ProbePoint this[CollisionProbeId id] => id switch
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

    private static int Align16(int value) => value & ~0x0F;
}

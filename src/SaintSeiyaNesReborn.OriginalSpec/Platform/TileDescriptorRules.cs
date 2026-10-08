namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

/// <summary>
/// Behavioral rules observed in the original platform collision code.
/// These names describe effects, not visual tile identities.
/// </summary>
public static class TileDescriptorRules
{
    public static bool SupportsFloor(byte descriptor) => descriptor >= 0x80;

    public static bool StopsUpwardMotionAtUpperCenter(byte descriptor) =>
        descriptor is >= 0xE0 and < 0xF0;

    public static bool BlocksGroundedRightAtLowerProbe(byte descriptor) =>
        descriptor is >= 0x80 and < 0x88 or >= 0xE0 and < 0xF0;

    public static bool BlocksGroundedLeftAtLowerProbe(byte descriptor) =>
        descriptor is >= 0x78 and < 0x80
            or >= 0x88 and < 0x90
            or >= 0xE0 and < 0xF0;

    public static bool IsSpecialF8F9Floor(byte descriptor) =>
        descriptor is 0xF8 or 0xF9;
}

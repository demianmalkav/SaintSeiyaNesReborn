namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

/// <summary>
/// Reconstructed horizontal camera/player handoff constants from bank 3 $AB3F-$AC50.
/// </summary>
public static class PlatformCamera
{
    private static readonly byte[] TerminalHighPageBySubstate =
    [
        4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 14, 10, 10, 10, 1, 10, 0,
    ];

    public const int RightTrackingThreshold = 0x80;
    public const int LeftGroundedLimit = 0x10;
    public const int TerminalRightPlayerLimit = 0xE0;
    public const int ScrollLowBoundary = 0xF8;

    public static byte TerminalHighPage(int substate)
    {
        if ((uint)substate >= TerminalHighPageBySubstate.Length)
            throw new ArgumentOutOfRangeException(nameof(substate));
        return TerminalHighPageBySubstate[substate];
    }

    public static bool CameraMayAdvance(int substate, int scrollLow, int scrollHigh)
    {
        if (scrollLow < ScrollLowBoundary)
            return true;
        return scrollHigh < TerminalHighPage(substate);
    }

    public static bool GroundedRightMovesCamera(
        int substate,
        int playerX,
        int scrollLow,
        int scrollHigh) =>
        playerX >= RightTrackingThreshold && CameraMayAdvance(substate, scrollLow, scrollHigh);

    public static bool GroundedRightMovesPlayer(
        int substate,
        int playerX,
        int scrollLow,
        int scrollHigh)
    {
        if (playerX < RightTrackingThreshold)
            return true;
        return !CameraMayAdvance(substate, scrollLow, scrollHigh)
            && playerX < TerminalRightPlayerLimit;
    }

    public static bool GroundedLeftMovesPlayer(int playerX) => playerX >= LeftGroundedLimit;
}

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformCommonEntityLandingResult(
    PlatformCommonEntityMotionState State,
    bool Landed,
    int ScreenYDelta);

/// <summary>
/// Public semantic form of fixed-bank $C491 landing resolution used by common
/// entity jump/fall paths.
/// </summary>
public static class PlatformCommonEntityLanding
{
    public static PlatformCommonEntityLandingResult Resolve(PlatformCommonEntityMotionState state)
    {
        if (state.Y >= 0x86 || state.GroundDescriptor < 0xA8)
            return new(state, false, 0);

        byte snappedY;
        var low = state.Y & 0x0F;
        if (state.GroundDescriptor >= 0xF0)
        {
            if (low < 8)
                return new(state, false, 0);
            snappedY = (byte)((state.Y & 0xF0) | 0x08);
        }
        else
        {
            if (low >= 6)
                return new(state, false, 0);
            snappedY = (byte)(state.Y & 0xF0);
        }

        var nextAction = state.Type is 0x08 or 0x09 or 0x0C
            ? (byte)0x00
            : (byte)0x10;
        var delta = snappedY - state.Y;

        return new(
            state with
            {
                Y = snappedY,
                StatePhase = 0,
                ActionState = nextAction,
            },
            true,
            delta);
    }
}

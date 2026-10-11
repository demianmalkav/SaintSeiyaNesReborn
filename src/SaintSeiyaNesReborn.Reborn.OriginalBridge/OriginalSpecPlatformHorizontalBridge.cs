using SaintSeiyaNesReborn.OriginalSpec.Platform;
using SaintSeiyaNesReborn.Reborn.Core;

namespace SaintSeiyaNesReborn.Reborn.OriginalBridge;

/// <summary>
/// Anti-corruption mapping for the first gameplay slice. Original storage layout,
/// screen/camera split and controller bitmasks terminate here; REBORN receives
/// world position, facing, logical phase and a semantic motion profile.
/// </summary>
public static class OriginalSpecPlatformHorizontalBridge
{
    public static RebornGroundedHorizontalProfile ProfileFor(PlatformSaintIndex saint)
    {
        var even = PlatformMovementIncrements.FromFrame(saint, 0).Grounded0387;
        var odd = PlatformMovementIncrements.FromFrame(saint, 1).Grounded0387;
        return new RebornGroundedHorizontalProfile(even, odd);
    }

    public static RebornPlatformPlayerHorizontalState FromCanonicalState(
        PlatformHorizontalState state,
        byte frameCounter,
        bool isLocomoting = false) =>
        new(
            state.WorldPlayerX,
            state.FacingRight ? RebornFacing.Right : RebornFacing.Left,
            FromCanonicalFrameParity(frameCounter),
            isLocomoting);

    public static RebornHorizontalInput FromCanonicalInput(PlatformInput input)
    {
        // The original grounded path tests Right before Left, so simultaneous
        // directions project to Right at this semantic boundary.
        if ((input & PlatformInput.Right) != 0)
            return RebornHorizontalInput.Right;
        if ((input & PlatformInput.Left) != 0)
            return RebornHorizontalInput.Left;
        return RebornHorizontalInput.Neutral;
    }

    public static RebornMotionPhase FromCanonicalFrameParity(byte frameCounter) =>
        (frameCounter & 1) == 0 ? RebornMotionPhase.Even : RebornMotionPhase.Odd;
}

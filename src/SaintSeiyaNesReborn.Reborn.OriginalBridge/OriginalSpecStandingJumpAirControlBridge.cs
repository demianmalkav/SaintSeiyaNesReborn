using SaintSeiyaNesReborn.OriginalSpec.Platform;
using SaintSeiyaNesReborn.Reborn.Core;

namespace SaintSeiyaNesReborn.Reborn.OriginalBridge;

/// <summary>
/// Anti-corruption mapping for ordinary standing-jump free-space air control.
/// Canonical frame parity, controller masks and player-X/scroll storage terminate
/// here; REBORN receives only world position, takeoff facing, logical phase and a
/// semantic drift profile.
/// </summary>
public static class OriginalSpecStandingJumpAirControlBridge
{
    public static RebornStandingJumpAirControlProfile Profile =>
        new(evenDriftPixels: 0, oddDriftPixels: 1);

    public static RebornStandingJumpAirState FromCanonicalState(
        PlatformHorizontalState state,
        byte frameCounter)
    {
        var projected = OriginalSpecPlatformHorizontalBridge.FromCanonicalState(
            state,
            frameCounter);
        return new RebornStandingJumpAirState(
            projected.WorldX,
            projected.Facing,
            projected.Phase);
    }

    public static RebornHorizontalInput FromCanonicalInput(PlatformInput input) =>
        OriginalSpecPlatformHorizontalBridge.FromCanonicalInput(input);
}

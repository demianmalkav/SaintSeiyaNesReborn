using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;
using SaintSeiyaNesReborn.Reborn.Core;

namespace SaintSeiyaNesReborn.Reborn.OriginalBridge;

/// <summary>
/// Anti-corruption mapping for the canonical ordinary standing-jump trajectory.
/// Original action/phase storage and screen-coordinate direction terminate here;
/// REBORN receives a positive-up semantic trajectory profile.
/// </summary>
public static class OriginalSpecStandingJumpBridge
{
    public static RebornStandingJumpProfile ProfileFor(PlatformSaintIndex saint)
    {
        var canonical = PlatformJumpProfile.Get(PlatformJumpKind.Standing, saint);
        return new RebornStandingJumpProfile(canonical.RisePerFrame.Select(value => (int)value));
    }

    public static int RiseFromCanonicalScreenDelta(int screenYDelta) => checked(-screenYDelta);
}

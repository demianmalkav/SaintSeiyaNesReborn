using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;
using SaintSeiyaNesReborn.Reborn.Core;

namespace SaintSeiyaNesReborn.Reborn.OriginalBridge;

/// <summary>
/// Anti-corruption mapping for high standing jumps. Saint-specific original
/// profile selection and controller representation terminate here; Core receives
/// only semantic family identity, positive-up displacement samples and takeoff
/// intent.
/// </summary>
public static class OriginalSpecHighStandingJumpBridge
{
    public static RebornHighStandingJumpProfile ProfileFor(PlatformSaintIndex saint)
    {
        var canonical = PlatformJumpProfile.Get(PlatformJumpKind.High, saint);
        return new RebornHighStandingJumpProfile(
            FamilyFor(saint),
            canonical.RisePerFrame.Select(value => (int)value));
    }

    public static RebornHighStandingJumpProfileFamily FamilyFor(PlatformSaintIndex saint) => saint switch
    {
        PlatformSaintIndex.Seiya => RebornHighStandingJumpProfileFamily.Seiya,
        PlatformSaintIndex.Shun or PlatformSaintIndex.Ikki => RebornHighStandingJumpProfileFamily.ShunIkki,
        PlatformSaintIndex.Hyoga or PlatformSaintIndex.Shiryu => RebornHighStandingJumpProfileFamily.HyogaShiryu,
        _ => throw new ArgumentOutOfRangeException(nameof(saint), saint, null),
    };

    public static RebornHighStandingJumpTakeoffIntent FromCanonicalTakeoffInput(PlatformInput input) =>
        new(
            JumpRequested: (input & PlatformInput.A) != 0,
            UpwardIntent: (input & PlatformInput.Up) != 0,
            HorizontalInput: OriginalSpecStandingJumpAirControlBridge.FromCanonicalInput(input));
}

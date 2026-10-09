using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class JumpProfileChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        var standing = PlatformJumpProfile.Get(PlatformJumpKind.Standing, PlatformSaintIndex.Seiya);
        Require(standing.DurationFrames == 32, "standing duration");
        Require(standing.PeakRisePixels == 58, "standing peak rise");
        Require(standing.ApexFrame == 14, "standing apex frame");
        Require(standing.NetRiseAfterTable == 29, "standing net rise after table");
        Require(standing.ScreenYDeltaAtFrame(0) == -8, "standing first frame rises 8 pixels");
        Require(standing.ScreenYDeltaAtFrame(31) == 3, "standing final table frame falls 3 pixels");

        var seiyaHigh = PlatformJumpProfile.Get(PlatformJumpKind.High, PlatformSaintIndex.Seiya);
        Require(seiyaHigh.DurationFrames == 60, "Seiya high duration");
        Require(seiyaHigh.PeakRisePixels == 103, "Seiya high peak");
        Require(seiyaHigh.ApexFrame == 28, "Seiya high apex");

        var shunHigh = PlatformJumpProfile.Get(PlatformJumpKind.High, PlatformSaintIndex.Shun);
        Require(shunHigh.DurationFrames == 50, "Shun high duration");
        Require(shunHigh.PeakRisePixels == 88, "Shun high peak");
        Require(shunHigh.ApexFrame == 23, "Shun high apex");

        var hyogaHigh = PlatformJumpProfile.Get(PlatformJumpKind.High, PlatformSaintIndex.Hyoga);
        Require(hyogaHigh.DurationFrames == 40, "Hyoga high duration");
        Require(hyogaHigh.PeakRisePixels == 71, "Hyoga high peak");
        Require(hyogaHigh.ApexFrame == 18, "Hyoga high apex");

        var seiyaDirectional = PlatformJumpProfile.Get(PlatformJumpKind.Directional, PlatformSaintIndex.Seiya);
        Require(seiyaDirectional.DurationFrames == 54, "Seiya directional duration");
        Require(seiyaDirectional.PeakRisePixels == 39, "Seiya directional peak");
        Require(seiyaDirectional.ApexFrame == 25, "Seiya directional apex");
        Require(seiyaDirectional.NetRiseAfterTable == -14, "Seiya directional table ends below takeoff height");

        var shunDirectional = PlatformJumpProfile.Get(PlatformJumpKind.Directional, PlatformSaintIndex.Shun);
        Require(shunDirectional.DurationFrames == 40, "Shun directional duration");
        Require(shunDirectional.PeakRisePixels == 33, "Shun directional peak");
        Require(shunDirectional.ApexFrame == 18, "Shun directional apex");

        var hyogaDirectional = PlatformJumpProfile.Get(PlatformJumpKind.Directional, PlatformSaintIndex.Hyoga);
        Require(hyogaDirectional.DurationFrames == 44, "Hyoga directional duration");
        Require(hyogaDirectional.PeakRisePixels == 34, "Hyoga directional peak");
        Require(hyogaDirectional.ApexFrame == 20, "Hyoga directional apex");

        // Shared original tables by internal Saint index.
        Require(SequenceEqual(
            PlatformJumpProfile.Get(PlatformJumpKind.High, PlatformSaintIndex.Shun).RisePerFrame,
            PlatformJumpProfile.Get(PlatformJumpKind.High, PlatformSaintIndex.Ikki).RisePerFrame),
            "Shun/Ikki high profile sharing");
        Require(SequenceEqual(
            PlatformJumpProfile.Get(PlatformJumpKind.Directional, PlatformSaintIndex.Hyoga).RisePerFrame,
            PlatformJumpProfile.Get(PlatformJumpKind.Directional, PlatformSaintIndex.Shiryu).RisePerFrame),
            "Hyoga/Shiryu directional profile sharing");
    }

    private static bool SequenceEqual(IReadOnlyList<sbyte> a, IReadOnlyList<sbyte> b)
    {
        if (a.Count != b.Count)
            return false;
        for (var i = 0; i < a.Count; i++)
            if (a[i] != b[i])
                return false;
        return true;
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"JumpProfile self-test failed: {label}");
    }
}

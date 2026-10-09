using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class JumpProfileChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        var standing = PlatformJumpProfile.Get(PlatformJumpKind.Standing, PlatformSaintIndex.Seiya);
        Require(standing.PhaseLimit == 32, "standing phase limit");
        Require(standing.TableFrames == 30, "standing consumed table frames");
        Require(standing.PeakRisePixels == 58, "standing peak rise");
        Require(standing.ApexFrame == 14, "standing apex frame");
        Require(standing.NetRiseAfterTable == 35, "standing net rise after consumed table");
        Require(standing.ScreenYDeltaAtFrame(0) == -8, "standing first frame rises 8 pixels");
        Require(standing.ScreenYDeltaAtFrame(29) == 3, "standing last table frame falls 3 pixels");
        Require(standing.ScreenYDeltaAtFrame(30) == 3, "standing terminal fall begins after table");
        Require(standing.CumulativeRiseAfterFrames(32) == 29, "standing two terminal fall frames");

        var seiyaHigh = PlatformJumpProfile.Get(PlatformJumpKind.High, PlatformSaintIndex.Seiya);
        Require(seiyaHigh.PhaseLimit == 60, "Seiya high phase limit");
        Require(seiyaHigh.TableFrames == 58, "Seiya high table frames");
        Require(seiyaHigh.PeakRisePixels == 103, "Seiya high peak");
        Require(seiyaHigh.ApexFrame == 28, "Seiya high apex");
        Require(seiyaHigh.NetRiseAfterTable == 51, "Seiya high net table rise");

        var shunHigh = PlatformJumpProfile.Get(PlatformJumpKind.High, PlatformSaintIndex.Shun);
        Require(shunHigh.PhaseLimit == 50, "Shun high phase limit");
        Require(shunHigh.TableFrames == 48, "Shun high table frames");
        Require(shunHigh.PeakRisePixels == 88, "Shun high peak");
        Require(shunHigh.ApexFrame == 23, "Shun high apex");
        Require(shunHigh.NetRiseAfterTable == 51, "Shun high net table rise");

        var hyogaHigh = PlatformJumpProfile.Get(PlatformJumpKind.High, PlatformSaintIndex.Hyoga);
        Require(hyogaHigh.PhaseLimit == 40, "Hyoga high phase limit");
        Require(hyogaHigh.TableFrames == 38, "Hyoga high table frames");
        Require(hyogaHigh.PeakRisePixels == 71, "Hyoga high peak");
        Require(hyogaHigh.ApexFrame == 18, "Hyoga high apex");
        Require(hyogaHigh.NetRiseAfterTable == 41, "Hyoga high net table rise");

        var seiyaDirectional = PlatformJumpProfile.Get(PlatformJumpKind.Directional, PlatformSaintIndex.Seiya);
        Require(seiyaDirectional.PhaseLimit == 54, "Seiya directional phase limit");
        Require(seiyaDirectional.TableFrames == 52, "Seiya directional table frames");
        Require(seiyaDirectional.PeakRisePixels == 39, "Seiya directional peak");
        Require(seiyaDirectional.ApexFrame == 25, "Seiya directional apex");
        Require(seiyaDirectional.NetRiseAfterTable == -8, "Seiya directional net table rise");

        var shunDirectional = PlatformJumpProfile.Get(PlatformJumpKind.Directional, PlatformSaintIndex.Shun);
        Require(shunDirectional.PhaseLimit == 40, "Shun directional phase limit");
        Require(shunDirectional.TableFrames == 38, "Shun directional table frames");
        Require(shunDirectional.PeakRisePixels == 33, "Shun directional peak");
        Require(shunDirectional.ApexFrame == 18, "Shun directional apex");
        Require(shunDirectional.NetRiseAfterTable == 5, "Shun directional net table rise");

        var hyogaDirectional = PlatformJumpProfile.Get(PlatformJumpKind.Directional, PlatformSaintIndex.Hyoga);
        Require(hyogaDirectional.PhaseLimit == 44, "Hyoga directional phase limit");
        Require(hyogaDirectional.TableFrames == 42, "Hyoga directional table frames");
        Require(hyogaDirectional.PeakRisePixels == 34, "Hyoga directional peak");
        Require(hyogaDirectional.ApexFrame == 20, "Hyoga directional apex");
        Require(hyogaDirectional.NetRiseAfterTable == 0, "Hyoga directional net table rise");

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

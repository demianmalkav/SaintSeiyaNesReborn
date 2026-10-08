namespace SaintSeiyaNesReborn.OriginalSpec;

/// <summary>
/// Canonical/high-level character identity. This order matches selector $0533
/// and the persistent snapshot/password order used by the original game.
/// </summary>
public enum SaintId : byte
{
    Seiya = 0,
    Hyoga = 1,
    Shun = 2,
    Shiryu = 3,
    Ikki = 4,
}

/// <summary>
/// Internal platform-engine index stored at RAM $03.
/// </summary>
public enum PlatformSaintIndex : byte
{
    Seiya = 0,
    Shun = 1,
    Hyoga = 2,
    Shiryu = 3,
    Ikki = 4,
}

public static class SaintIndexMap
{
    // Original fixed-bank table $E505: [0, 2, 1, 3, 4].
    private static readonly byte[] SwapShunHyoga = [0, 2, 1, 3, 4];

    public static SaintId ToCanonical(PlatformSaintIndex internalIndex)
    {
        var value = (byte)internalIndex;
        ValidateFiveValueIndex(value, nameof(internalIndex));
        return (SaintId)SwapShunHyoga[value];
    }

    public static PlatformSaintIndex ToPlatform(SaintId canonicalId)
    {
        var value = (byte)canonicalId;
        ValidateFiveValueIndex(value, nameof(canonicalId));
        return (PlatformSaintIndex)SwapShunHyoga[value];
    }

    private static void ValidateFiveValueIndex(byte value, string parameterName)
    {
        if (value > 4)
            throw new ArgumentOutOfRangeException(parameterName, value, "Saint index must be in 0..4.");
    }
}

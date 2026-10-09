using SaintSeiyaNesReborn.OriginalSpec;

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

/// <summary>
/// One five-byte live/persistent Saint resource record:
/// [Life low-two BCD digits, Life hundreds, Cosmo low-two BCD digits,
/// Cosmo hundreds, packed Life/Cosmo cap byte].
/// </summary>
public readonly record struct PlatformSaintResourceRecord(
    byte LifeLowTwoDigits,
    byte LifeHundreds,
    byte CosmoLowTwoDigits,
    byte CosmoHundreds,
    byte ResourceCap)
{
    public int Life => PackedBcd.DecodeByte(LifeHundreds) * 100
        + PackedBcd.DecodeByte(LifeLowTwoDigits);

    public int Cosmo => PackedBcd.DecodeByte(CosmoHundreds) * 100
        + PackedBcd.DecodeByte(CosmoLowTwoDigits);

    public byte[] CopyRawBytes() =>
    [
        LifeLowTwoDigits,
        LifeHundreds,
        CosmoLowTwoDigits,
        CosmoHundreds,
        ResourceCap,
    ];
}

/// <summary>
/// Live platform resource order at $0059-$0071:
/// [Seiya, Shun, Hyoga, Shiryu, Ikki].
/// </summary>
public readonly record struct PlatformLiveResourceState(
    PlatformSaintResourceRecord Seiya,
    PlatformSaintResourceRecord Shun,
    PlatformSaintResourceRecord Hyoga,
    PlatformSaintResourceRecord Shiryu,
    PlatformSaintResourceRecord Ikki)
{
    public PlatformSaintResourceRecord For(PlatformSaintIndex saint) => saint switch
    {
        PlatformSaintIndex.Seiya => Seiya,
        PlatformSaintIndex.Shun => Shun,
        PlatformSaintIndex.Hyoga => Hyoga,
        PlatformSaintIndex.Shiryu => Shiryu,
        PlatformSaintIndex.Ikki => Ikki,
        _ => throw new ArgumentOutOfRangeException(nameof(saint)),
    };
}

/// <summary>
/// Persistent snapshot order at $058C-$05A4:
/// [Seiya, Hyoga, Shun, Shiryu, Ikki].
/// This is canonical/high-level Saint order, not internal platform order.
/// </summary>
public readonly record struct PlatformPersistentResourceSnapshot(
    PlatformSaintResourceRecord Seiya,
    PlatformSaintResourceRecord Hyoga,
    PlatformSaintResourceRecord Shun,
    PlatformSaintResourceRecord Shiryu,
    PlatformSaintResourceRecord Ikki)
{
    public PlatformSaintResourceRecord For(SaintId saint) => saint switch
    {
        SaintId.Seiya => Seiya,
        SaintId.Hyoga => Hyoga,
        SaintId.Shun => Shun,
        SaintId.Shiryu => Shiryu,
        SaintId.Ikki => Ikki,
        _ => throw new ArgumentOutOfRangeException(nameof(saint)),
    };

    /// <summary>
    /// Returns the exact semantic 25-byte layout written to $058C-$05A4 by
    /// bank-1 $951F, without embedding any ROM payload.
    /// </summary>
    public byte[] CopyCanonicalBytes()
    {
        var result = new byte[25];
        var records = new[] { Seiya, Hyoga, Shun, Shiryu, Ikki };
        for (var i = 0; i < records.Length; i++)
            records[i].CopyRawBytes().CopyTo(result, i * 5);
        return result;
    }
}

public static class PlatformResourceSnapshot
{
    /// <summary>
    /// Clean-room equivalent of bank 1 $951F: copy five live records from
    /// platform order into the persistent canonical order used by $058C-$05A4.
    /// </summary>
    public static PlatformPersistentResourceSnapshot Capture(PlatformLiveResourceState live) =>
        new(
            Seiya: live.Seiya,
            Hyoga: live.Hyoga,
            Shun: live.Shun,
            Shiryu: live.Shiryu,
            Ikki: live.Ikki);
}

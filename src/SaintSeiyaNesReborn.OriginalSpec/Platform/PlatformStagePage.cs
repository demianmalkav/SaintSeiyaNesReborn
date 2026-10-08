namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformEncounterStats(
    int HitPoints,
    byte CosmoDrainTicks,
    byte LifeDrainTicks,
    int SeventhSenseReward);

public readonly record struct PlatformPrimaryEncounter(
    byte Raw,
    byte TypeId,
    byte Tier,
    bool SecondCommonSlotEnabled,
    bool Bit6Unknown,
    PlatformEncounterStats? Stats);

/// <summary>
/// One 256x176-pixel logical platform page reconstructed from the original
/// 16x11 metatile-descriptor grid. Graphics are intentionally not part of the
/// clean-room model; descriptors are the semantic values consumed by collision.
/// </summary>
public sealed class PlatformStagePage
{
    public const int Columns = 16;
    public const int Rows = 11;
    public const int DescriptorCount = Columns * Rows;

    private readonly byte[] _descriptors;

    public int PageIndex { get; }
    public int? PoolPageId { get; }
    public PlatformPrimaryEncounter PrimaryEncounter { get; }
    public PlatformEntityArchetype? SecondaryArchetype { get; }

    public PlatformStagePage(
        int pageIndex,
        IReadOnlyList<byte> descriptorsRowMajor,
        PlatformPrimaryEncounter primaryEncounter = default,
        PlatformEntityArchetype? secondaryArchetype = null,
        int? poolPageId = null)
    {
        if (pageIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(pageIndex));
        if (descriptorsRowMajor.Count != DescriptorCount)
            throw new ArgumentException(
                $"A platform page requires exactly {DescriptorCount} descriptors.",
                nameof(descriptorsRowMajor));

        PageIndex = pageIndex;
        PoolPageId = poolPageId;
        PrimaryEncounter = primaryEncounter;
        SecondaryArchetype = secondaryArchetype;
        _descriptors = descriptorsRowMajor.ToArray();
    }

    public byte DescriptorAt(int column, int row)
    {
        if ((uint)column >= Columns)
            throw new ArgumentOutOfRangeException(nameof(column));
        if ((uint)row >= Rows)
            throw new ArgumentOutOfRangeException(nameof(row));
        return _descriptors[row * Columns + column];
    }

    public IReadOnlyList<byte> CopyDescriptorsRowMajor() => _descriptors.ToArray();
}

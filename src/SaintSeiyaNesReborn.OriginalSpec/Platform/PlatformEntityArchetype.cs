namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformEntityArchetype(
    byte Id,
    int HitPoints,
    byte CosmoDrainTicks,
    byte LifeDrainTicks,
    int SeventhSenseReward)
{
    private static readonly PlatformEntityArchetype[] Table =
    [
        new(0, 0, 0, 0, 0),
        new(1, 30, 2, 2, 32),
        new(2, 30, 4, 1, 32),
        new(3, 0, 5, 2, 0),
        new(4, 0, 4, 3, 0),
    ];

    public bool IsDamageableByOrdinaryHpPath => HitPoints > 0;
    public bool IsContactHazardProfile => HitPoints == 0 && (CosmoDrainTicks != 0 || LifeDrainTicks != 0);

    public static PlatformEntityArchetype Get(byte id)
    {
        if (id >= Table.Length)
            throw new ArgumentOutOfRangeException(nameof(id), id, "Known platform archetype must be in 0..4.");
        return Table[id];
    }
}

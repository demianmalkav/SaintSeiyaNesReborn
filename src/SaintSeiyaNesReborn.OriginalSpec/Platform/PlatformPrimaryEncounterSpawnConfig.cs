using SaintSeiyaNesReborn.OriginalSpec;

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformPrimaryEncounterSpawnConfig(
    byte Engine58,
    PlatformSpecialSpawnProfile Profile,
    byte TypeId,
    byte Tier,
    bool SecondCommonSlotEnabled,
    bool Bit6Unknown)
{
    /// <summary>
    /// Convert the semantic per-page primary encounter into the exact values
    /// staged by bank 1 for the common-entity producers.
    ///
    /// Raw is preserved as Engine58 because bits 4-5 and 6-7 have runtime
    /// meaning beyond the decoded TypeId. Stats are converted back to the byte
    /// representation written into logical offsets +$0C..+$0F.
    /// </summary>
    public static PlatformPrimaryEncounterSpawnConfig FromEncounter(
        PlatformPrimaryEncounter encounter)
    {
        if (encounter.Stats is not PlatformEncounterStats stats)
            throw new InvalidOperationException(
                $"Primary encounter ${encounter.Raw:X2} does not have a resolved stat profile.");

        if ((uint)stats.HitPoints > byte.MaxValue)
            throw new InvalidOperationException(
                $"Primary encounter HP {stats.HitPoints} does not fit original one-byte +$0C storage.");

        var rewardBcd = PackedBcd.EncodeByte(stats.SeventhSenseReward);
        var profile = new PlatformSpecialSpawnProfile(
            HitPoints0C: (byte)stats.HitPoints,
            LifeDrainTicks0D: stats.LifeDrainTicks,
            CosmoDrainTicks0E: stats.CosmoDrainTicks,
            SeventhSenseRewardBcd0F: rewardBcd);

        return new PlatformPrimaryEncounterSpawnConfig(
            Engine58: encounter.Raw,
            Profile: profile,
            TypeId: encounter.TypeId,
            Tier: encounter.Tier,
            SecondCommonSlotEnabled: encounter.SecondCommonSlotEnabled,
            Bit6Unknown: encounter.Bit6Unknown);
    }

    public static PlatformPrimaryEncounterSpawnConfig FromStagePage(PlatformStagePage page) =>
        FromEncounter(page.PrimaryEncounter);
}

public static class PlatformPrimaryEncounterSpawnExtensions
{
    /// <summary>
    /// Convenience wrapper for the generic bank-0 $B6D0 producer. This keeps
    /// page encounter decoding as the single source of truth for $58 and
    /// +$0C..+$0F instead of asking callers to duplicate those values.
    /// </summary>
    public static PlatformCommonEdgeSpawnPairResult StepCommonEdgeSpawner(
        this PlatformPrimaryEncounterSpawnConfig config,
        PlatformStageMap stage,
        int scrollX,
        byte playerX,
        byte cameraDelta43,
        byte entropy48,
        byte spawnGate03B7,
        byte cooldown03B8,
        PlatformCommonEntityRuntimeState entityA,
        byte visualSpriteA,
        PlatformCommonEntityRuntimeState entityB,
        byte visualSpriteB) =>
        PlatformCommonEdgeSpawner.StepPair(
            stage,
            scrollX,
            playerX,
            cameraDelta43,
            entropy48,
            config.Engine58,
            spawnGate03B7,
            cooldown03B8,
            config.Profile,
            entityA,
            visualSpriteA,
            entityB,
            visualSpriteB);

    /// <summary>
    /// Convenience wrapper for the bank-1 $8925 schedule producer.
    /// </summary>
    public static PlatformScheduledSpecialSpawnPairResult TryScheduledSpecialSpawn(
        this PlatformPrimaryEncounterSpawnConfig config,
        PlatformCommonEntityRuntimeState entityA,
        byte visualSpriteA,
        PlatformCommonEntityRuntimeState entityB,
        byte visualSpriteB,
        byte cameraLow44,
        byte cameraHigh45,
        byte lastTriggerLow03A2,
        IReadOnlyList<PlatformSpecialSpawnEntry> schedule) =>
        PlatformScheduledSpecialEntitySpawner.TrySpawnPair(
            entityA,
            visualSpriteA,
            entityB,
            visualSpriteB,
            config.Engine58,
            cameraLow44,
            cameraHigh45,
            lastTriggerLow03A2,
            config.Profile,
            schedule);
}

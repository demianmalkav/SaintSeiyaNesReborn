using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class PrimaryEncounterSpawnConfigChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckDescriptorAndProfileMapping();
        CheckRawBit7FeedsPairSpawner();
        CheckSpecialDescriptorFeedsScheduledSpawner();
        CheckMissingProfileRejected();
    }

    private static void CheckDescriptorAndProfileMapping()
    {
        var encounter = new PlatformPrimaryEncounter(
            Raw: 0xB6,
            TypeId: 0x06,
            Tier: 0x03,
            SecondCommonSlotEnabled: true,
            Bit6Unknown: false,
            Stats: new PlatformEncounterStats(
                HitPoints: 120,
                CosmoDrainTicks: 8,
                LifeDrainTicks: 4,
                SeventhSenseReward: 13));

        var config = PlatformPrimaryEncounterSpawnConfig.FromEncounter(encounter);

        Require(config.Engine58 == 0xB6, "raw descriptor is preserved as $58");
        Require(config.TypeId == 0x06 && config.Tier == 0x03,
            "decoded type/tier remain available");
        Require(config.SecondCommonSlotEnabled,
            "bit7 semantic remains available");
        Require(config.Profile.HitPoints0C == 120,
            "HP maps to +$0C");
        Require(config.Profile.CosmoDrainTicks0D == 8,
            "Cosmo drain maps to +$0D");
        Require(config.Profile.LifeDrainTicks0E == 4,
            "Life drain maps to +$0E");
        Require(config.Profile.SeventhSenseRewardBcd0F == 0x13,
            "decimal reward is encoded back to packed BCD for +$0F");
    }

    private static void CheckRawBit7FeedsPairSpawner()
    {
        var page = Page(new PlatformPrimaryEncounter(
            Raw: 0x86,
            TypeId: 0x06,
            Tier: 0,
            SecondCommonSlotEnabled: true,
            Bit6Unknown: false,
            Stats: new PlatformEncounterStats(15, 2, 5, 4)));
        var config = PlatformPrimaryEncounterSpawnConfig.FromStagePage(page);

        var pair = config.StepCommonEdgeSpawner(
            GroundStage(page),
            scrollX: 0,
            playerX: 0x40,
            cameraDelta43: 0,
            entropy48: 0,
            spawnGate03B7: 0,
            cooldown03B8: 1,
            Existing(), 0xFE,
            Existing(), 0xFE);

        Require(pair.SlotA.Outcome == PlatformCommonEdgeSpawnOutcome.CooldownDecremented,
            "encounter wrapper passes shared cooldown to slot A");
        Require(pair.SlotB.HasValue && pair.SlotB.Value.Spawned,
            "raw bit7 enables slot-B attempt through the real spawner");
        Require(pair.SlotB.Value.Entity.Motion.Type == 0x06,
            "raw low nibble reaches spawned type");
        Require(pair.SlotB.Value.Entity.HitPoints == 15,
            "resolved encounter HP reaches runtime record");
        Require(pair.SlotB.Value.Entity.CosmoDrainTicks == 2
            && pair.SlotB.Value.Entity.LifeDrainTicks == 5,
            "runtime semantic drain counters are reconstructed from +$0D/+0E without swapping");
    }

    private static void CheckSpecialDescriptorFeedsScheduledSpawner()
    {
        var config = PlatformPrimaryEncounterSpawnConfig.FromEncounter(
            new PlatformPrimaryEncounter(
                Raw: 0xA8,
                TypeId: 0x08,
                Tier: 2,
                SecondCommonSlotEnabled: true,
                Bit6Unknown: false,
                Stats: new PlatformEncounterStats(80, 3, 7, 16)));

        var pair = config.TryScheduledSpecialSpawn(
            Existing(), 0xFE,
            Existing(), 0xFE,
            cameraLow44: 0x38,
            cameraHigh45: 0x04,
            lastTriggerLow03A2: 0x10,
            [new PlatformSpecialSpawnEntry(0x38, 0x04, 0x60)]);

        Require(pair.SlotA.Spawned,
            "special descriptor routes unchanged to schedule producer");
        Require(pair.SlotA.Entity.Motion.Type == 0x08,
            "scheduled producer receives low-nibble type");
        Require(pair.SlotA.Entity.HitPoints == 80,
            "tier-resolved HP feeds scheduled producer");
        Require(pair.SlotA.Entity.CosmoDrainTicks == 3
            && pair.SlotA.Entity.LifeDrainTicks == 7,
            "scheduled runtime receives +$0D Cosmo and +$0E Life semantics");
        Require(pair.SlotA.Entity.SeventhSenseRewardBcd == 0x16,
            "reward 16 is re-encoded as BCD $16");
    }

    private static void CheckMissingProfileRejected()
    {
        try
        {
            _ = PlatformPrimaryEncounterSpawnConfig.FromEncounter(
                new PlatformPrimaryEncounter(0, 0, 0, false, false, null));
        }
        catch (InvalidOperationException)
        {
            return;
        }

        throw new InvalidOperationException(
            "Primary encounter spawn-config self-test failed: missing stat profile must be rejected");
    }

    private static PlatformStagePage Page(PlatformPrimaryEncounter encounter)
    {
        var descriptors = new byte[PlatformStagePage.DescriptorCount];
        descriptors[4 * PlatformStagePage.Columns] = 0xA8;
        return new PlatformStagePage(0, descriptors, encounter);
    }

    private static PlatformStageMap GroundStage(PlatformStagePage page) => new(0, [page]);

    private static PlatformCommonEntityRuntimeState Existing() =>
        new(
            new PlatformCommonEntityMotionState(
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
            0, 0, 0, 0);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Primary encounter spawn-config self-test failed: {label}");
    }
}

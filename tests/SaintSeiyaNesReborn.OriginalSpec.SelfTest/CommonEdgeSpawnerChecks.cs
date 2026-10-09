using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class CommonEdgeSpawnerChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckGateMismatchSkipsPair();
        CheckCooldownOneLetsSecondSlotSpawn();
        CheckFirstSpawnMakesSecondConsumeCooldown();
        CheckGroundSearchAdvancesBySixteenPixels();
        CheckGroundSearchRejectsAtLowerLimit();
        CheckType0FBypassesGroundSearch();
        CheckTableDrivenTypesAreExcluded();
    }

    private static void CheckGateMismatchSkipsPair()
    {
        var pair = PlatformCommonEdgeSpawner.StepPair(
            GroundStage(), 0, 0x40, 0, 0,
            engine58: 0x06,
            spawnGate03B7: 0x05,
            cooldown03B8: 7,
            Profile(), Existing(), 0xFE, Existing(), 0xFE);

        Require(!pair.PairProcessed, "$03B7 mismatch skips both slots");
        Require(pair.Cooldown03B8 == 7, "gate mismatch preserves cooldown");
    }

    private static void CheckCooldownOneLetsSecondSlotSpawn()
    {
        var pair = PlatformCommonEdgeSpawner.StepPair(
            GroundStage(), 0, 0x40, 0, 0,
            engine58: 0x86,
            spawnGate03B7: 0,
            cooldown03B8: 1,
            Profile(), Existing(), 0xFE, Existing(), 0xFE);

        Require(pair.SlotA.Outcome == PlatformCommonEdgeSpawnOutcome.CooldownDecremented,
            "slot A consumes shared cooldown 1->0");
        Require(pair.SlotB.HasValue && pair.SlotB.Value.Spawned,
            "bit7-enabled slot B can spawn after A reaches zero");
        Require(pair.Cooldown03B8 == 0x30, "B spawn reseeds shared cooldown to $30");
    }

    private static void CheckFirstSpawnMakesSecondConsumeCooldown()
    {
        var pair = PlatformCommonEdgeSpawner.StepPair(
            GroundStage(), 0, 0x40, 0, 0,
            engine58: 0x86,
            spawnGate03B7: 0,
            cooldown03B8: 0,
            Profile(), Existing(), 0xFE, Existing(), 0xFE);

        Require(pair.SlotA.Spawned, "slot A spawns when shared cooldown is zero");
        Require(pair.SlotB.HasValue
            && pair.SlotB.Value.Outcome == PlatformCommonEdgeSpawnOutcome.CooldownDecremented,
            "slot B immediately consumes A's new cooldown");
        Require(pair.Cooldown03B8 == 0x2F, "same-frame B attempt changes $30->$2F");
    }

    private static void CheckGroundSearchAdvancesBySixteenPixels()
    {
        var descriptors = new byte[PlatformStagePage.DescriptorCount];
        // X=0, probe X=8 -> column 0. Starting Y=$20 probes world Y=$40
        // (row 4), then $50 (row 5), then $60 (row 6).
        descriptors[6 * PlatformStagePage.Columns] = 0xA8;
        var stage = new PlatformStageMap(0, [new PlatformStagePage(0, descriptors)]);

        var result = PlatformCommonEdgeSpawner.StepSlot(
            stage, 0, 0x40, 0, 0,
            engine58: 0x06,
            cooldown03B8: 0,
            Profile(), Existing(), 0xFE);

        Require(result.Spawned, "ground search eventually accepts descriptor >=$A8");
        Require(result.GroundSamples == 3, "ground search samples each 16-pixel row");
        Require(result.Entity.Motion.Y == 0x40, "accepted third sample leaves entity Y=$40");
        Require(result.Entity.Motion.GroundDescriptor == 0xA8,
            "accepted descriptor is stored in logical ground field");
    }

    private static void CheckGroundSearchRejectsAtLowerLimit()
    {
        var stage = new PlatformStageMap(
            0,
            [new PlatformStagePage(0, new byte[PlatformStagePage.DescriptorCount])]);

        var result = PlatformCommonEdgeSpawner.StepSlot(
            stage, 0, 0x40, 0, 0,
            engine58: 0x06,
            cooldown03B8: 0,
            Profile(), Existing(), 0xFE);

        Require(result.Outcome == PlatformCommonEdgeSpawnOutcome.GroundSearchRejected,
            "no qualifying ground aborts producer");
        Require(result.GroundSamples == 7 && result.Entity.Motion.Y == 0x80,
            "search samples Y $20..$80 before rejection");
        Require(result.VisualSprite == 0xFE, "rejected spawn leaves visual slot free");
        Require(result.Cooldown03B8 == 0x30,
            "ground-search failure still consumes/reseeds cooldown");
    }

    private static void CheckType0FBypassesGroundSearch()
    {
        var existing = Existing() with
        {
            Motion = Existing().Motion with { GroundDescriptor = 0xE7 },
        };
        var emptyStage = new PlatformStageMap(
            0,
            [new PlatformStagePage(0, new byte[PlatformStagePage.DescriptorCount])]);

        var result = PlatformCommonEdgeSpawner.StepSlot(
            emptyStage, 0, playerX: 0x40, cameraDelta43: 0, entropy48: 0,
            engine58: 0x0F,
            cooldown03B8: 0,
            Profile(), existing, 0xFE);

        Require(result.Spawned && result.GroundSamples == 0,
            "type0F bypasses vertical ground search");
        Require(result.Entity.Motion.X == 0x00 && result.Entity.Motion.Y == 0x00,
            "type0F entropy-bit-clear spawn uses left/top origin");
        Require(result.Entity.Motion.FlagsFacing == 0x41,
            "type0F left-origin path seeds +$07=$41");
        Require(result.Entity.Motion.GroundDescriptor == 0xE7,
            "type0F leaves logical +$05 untouched");
    }

    private static void CheckTableDrivenTypesAreExcluded()
    {
        var result = PlatformCommonEdgeSpawner.StepSlot(
            GroundStage(), 0, 0x40, 0, 0,
            engine58: 0x0C,
            cooldown03B8: 0,
            Profile(), Existing(), 0xFE);

        Require(result.Outcome == PlatformCommonEdgeSpawnOutcome.ScheduledTypeExcluded,
            "B6D0 producer excludes 08/09/0C/0D/0E types");
        Require(result.Cooldown03B8 == 0,
            "excluded type returns before shared cooldown mutation");
    }

    private static PlatformStageMap GroundStage()
    {
        var descriptors = new byte[PlatformStagePage.DescriptorCount];
        descriptors[4 * PlatformStagePage.Columns] = 0xA8;
        return new PlatformStageMap(0, [new PlatformStagePage(0, descriptors)]);
    }

    private static PlatformSpecialSpawnProfile Profile() => new(0x30, 2, 3, 0x10);

    private static PlatformCommonEntityRuntimeState Existing() =>
        new(
            new PlatformCommonEntityMotionState(
                ActionState: 0x00,
                X: 0x50,
                Y: 0x50,
                StatePhase: 0x22,
                GroundDescriptor: 0xE0,
                DecisionTimer: 0x11,
                FlagsFacing: 0x40,
                Type: 0x01,
                TerrainProbeRight: 0x88,
                TerrainProbeLeft: 0x99),
            HitPoints: 0x20,
            LifeDrainTicks: 1,
            CosmoDrainTicks: 1,
            SeventhSenseRewardBcd: 0x05);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Common edge spawner self-test failed: {label}");
    }
}

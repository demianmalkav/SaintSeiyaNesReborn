using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class ScheduledSpecialEntitySpawnerChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckSpawnAtAlignedCameraTrigger();
        CheckPairAConsumesTriggerBeforeB();
        CheckOccupiedALetsBTakeTrigger();
        CheckDuplicateUsesLowByteOnly();
        CheckOccupiedVisualSlotBlocksSpawn();
        CheckUnsupportedTypeIsRejected();
        CheckUnwrittenFieldsArePreserved();
    }

    private static void CheckSpawnAtAlignedCameraTrigger()
    {
        var existing = Existing(type: 0x05);
        // Raw entity bytes +$0C..+$0F = HP, Cosmo drain, Life drain, reward.
        var profile = new PlatformSpecialSpawnProfile(0x30, 0x02, 0x03, 0x10);
        var schedule = new[]
        {
            new PlatformSpecialSpawnEntry(0x38, 0x04, 0x70),
            new PlatformSpecialSpawnEntry(0xA8, 0x04, 0x50),
        };

        var result = PlatformScheduledSpecialEntitySpawner.TrySpawn(
            existing,
            existingVisualSprite: 0xFE,
            engine58: 0x8C,
            cameraLow44: 0x3D,
            cameraHigh45: 0x04,
            lastTriggerLow03A2: 0x10,
            profile,
            schedule);

        Require(result.Spawned, "matching aligned camera position spawns");
        Require(result.Entity.Motion.ActionState == 0x00, "spawn action is zero");
        Require(result.Entity.Motion.X == 0xF8 && result.Entity.Motion.Y == 0x70,
            "spawn uses X=$F8 and table Y");
        Require(result.Entity.Motion.Type == 0x0C, "type comes from low nibble of $58");
        Require(result.Entity.Motion.FlagsFacing == 0x01, "offset +$07 is seeded to 1");
        Require(result.Entity.HitPoints == 0x30
            && result.Entity.CosmoDrainTicks == 0x02
            && result.Entity.LifeDrainTicks == 0x03
            && result.Entity.SeventhSenseRewardBcd == 0x10,
            "profile bytes +$0D/+0E become semantic Cosmo/Life drain counters");
        Require(result.VisualSprite == 0xFD, "visual sprite byte +1 becomes $FD");
        Require(result.LastTriggerLow03A2 == 0x38, "$03A2 stores trigger low byte");
    }

    private static void CheckPairAConsumesTriggerBeforeB()
    {
        var pair = PlatformScheduledSpecialEntitySpawner.TrySpawnPair(
            Existing(0x01),
            visualSpriteA: 0xFE,
            Existing(0x02),
            visualSpriteB: 0xFE,
            engine58: 0x09,
            cameraLow44: 0x38,
            cameraHigh45: 0x04,
            lastTriggerLow03A2: 0x10,
            new PlatformSpecialSpawnProfile(1, 2, 3, 4),
            [new PlatformSpecialSpawnEntry(0x38, 0x04, 0x40)]);

        Require(pair.SlotA.Spawned, "slot A takes matching trigger first");
        Require(pair.SlotB.Outcome == PlatformScheduledSpecialSpawnOutcome.DuplicateLowTrigger,
            "slot B sees slot A's updated $03A2 in same frame");
        Require(pair.LastTriggerLow03A2 == 0x38, "pair preserves shared trigger latch");
    }

    private static void CheckOccupiedALetsBTakeTrigger()
    {
        var pair = PlatformScheduledSpecialEntitySpawner.TrySpawnPair(
            Existing(0x01),
            visualSpriteA: 0x80,
            Existing(0x02),
            visualSpriteB: 0xFE,
            engine58: 0x0D,
            cameraLow44: 0xA8,
            cameraHigh45: 0x05,
            lastTriggerLow03A2: 0x10,
            new PlatformSpecialSpawnProfile(5, 6, 7, 8),
            [new PlatformSpecialSpawnEntry(0xA8, 0x05, 0x60)]);

        Require(pair.SlotA.Outcome == PlatformScheduledSpecialSpawnOutcome.SlotOccupied,
            "occupied A is skipped without changing trigger latch");
        Require(pair.SlotB.Spawned, "free B can consume trigger after occupied A");
        Require(pair.SlotB.Entity.Motion.Type == 0x0D, "B receives scheduled special type");
    }

    private static void CheckDuplicateUsesLowByteOnly()
    {
        var result = PlatformScheduledSpecialEntitySpawner.TrySpawn(
            Existing(0x05),
            0xFE,
            engine58: 0x08,
            cameraLow44: 0x3F,
            cameraHigh45: 0x07,
            lastTriggerLow03A2: 0x38,
            new PlatformSpecialSpawnProfile(1, 2, 3, 4),
            [new PlatformSpecialSpawnEntry(0x38, 0x07, 0x40)]);

        Require(result.Outcome == PlatformScheduledSpecialSpawnOutcome.DuplicateLowTrigger,
            "duplicate gate compares only the low trigger byte");
        Require(result.VisualSprite == 0xFE, "duplicate does not consume visual slot");
    }

    private static void CheckOccupiedVisualSlotBlocksSpawn()
    {
        var result = PlatformScheduledSpecialEntitySpawner.TrySpawn(
            Existing(0x05),
            existingVisualSprite: 0x80,
            engine58: 0x09,
            cameraLow44: 0x38,
            cameraHigh45: 0x04,
            lastTriggerLow03A2: 0,
            new PlatformSpecialSpawnProfile(1, 2, 3, 4),
            [new PlatformSpecialSpawnEntry(0x38, 0x04, 0x40)]);

        Require(result.Outcome == PlatformScheduledSpecialSpawnOutcome.SlotOccupied,
            "visual sprite != $FE blocks schedule scan");
    }

    private static void CheckUnsupportedTypeIsRejected()
    {
        var result = PlatformScheduledSpecialEntitySpawner.TrySpawn(
            Existing(0x05),
            0xFE,
            engine58: 0x06,
            cameraLow44: 0x38,
            cameraHigh45: 0x04,
            lastTriggerLow03A2: 0,
            new PlatformSpecialSpawnProfile(1, 2, 3, 4),
            [new PlatformSpecialSpawnEntry(0x38, 0x04, 0x40)]);

        Require(result.Outcome == PlatformScheduledSpecialSpawnOutcome.UnsupportedType,
            "producer accepts only 08/09/0C/0D/0E low-nibble types");
    }

    private static void CheckUnwrittenFieldsArePreserved()
    {
        var existing = Existing(0x05) with
        {
            Motion = Existing(0x05).Motion with
            {
                DecisionTimer = 0x5A,
                TerrainProbeRight = 0xA1,
                TerrainProbeLeft = 0xB2,
            },
        };

        var result = PlatformScheduledSpecialEntitySpawner.TrySpawn(
            existing,
            0xFE,
            engine58: 0x0E,
            cameraLow44: 0xA8,
            cameraHigh45: 0x05,
            lastTriggerLow03A2: 0,
            new PlatformSpecialSpawnProfile(9, 8, 7, 6),
            [new PlatformSpecialSpawnEntry(0xA8, 0x05, 0x50)]);

        Require(result.Entity.Motion.DecisionTimer == 0x5A,
            "offset +$06 is preserved because spawn routine does not write it");
        Require(result.Entity.Motion.TerrainProbeRight == 0xA1
            && result.Entity.Motion.TerrainProbeLeft == 0xB2,
            "offsets +$0A/+0B are preserved");
    }

    private static PlatformCommonEntityRuntimeState Existing(byte type) =>
        new(
            new PlatformCommonEntityMotionState(
                ActionState: 0x10,
                X: 0x20,
                Y: 0x30,
                StatePhase: 0x44,
                GroundDescriptor: 0xE0,
                DecisionTimer: 0x11,
                FlagsFacing: 0x40,
                Type: type,
                TerrainProbeRight: 0x88,
                TerrainProbeLeft: 0x99),
            HitPoints: 0x22,
            LifeDrainTicks: 0x23,
            CosmoDrainTicks: 0x24,
            SeventhSenseRewardBcd: 0x25);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Scheduled special entity spawner self-test failed: {label}");
    }
}

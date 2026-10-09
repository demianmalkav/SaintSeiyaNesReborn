using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class AuxiliaryHazardChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckProfileTables();
        CheckSpawnerCarrySeedAndSecondSlotTick();
        CheckKind3UsesOnlyFirstSlot();
        CheckRejectedYPreservesPartialWrites();
        CheckOccupiedFirstSlotDoesNotConsumeCooldown();
        CheckOrdinaryAnimationAndMovement();
        CheckOrdinaryBoundaryRemoval();
        CheckHomingRefreshAndMotion();
        CheckHomingVerticalRemoval();
        CheckPlayer80FamilySuppressesContactOnly();
    }

    private static void CheckProfileTables()
    {
        Require(
            PlatformAuxiliaryHazardProfile.ForKind(1) == new PlatformAuxiliaryHazardProfile(1, 0x80, 0x1E, 0x02, 0x02, 0x32),
            "kind1 profile matches C169/9C92 tables");
        Require(
            PlatformAuxiliaryHazardProfile.ForKind(2) == new PlatformAuxiliaryHazardProfile(2, 0x83, 0x1E, 0x04, 0x01, 0x32),
            "kind2 profile matches C169/9C92 tables");
        Require(
            PlatformAuxiliaryHazardProfile.ForKind(3) == new PlatformAuxiliaryHazardProfile(3, 0xF4, 0x00, 0x05, 0x02, 0x00),
            "kind3 profile matches C169/9C92 tables");
        Require(
            PlatformAuxiliaryHazardProfile.ForKind(4) == new PlatformAuxiliaryHazardProfile(4, 0xDA, 0x00, 0x04, 0x03, 0x00),
            "kind4 profile matches C169/9C92 tables");
    }

    private static void CheckSpawnerCarrySeedAndSecondSlotTick()
    {
        var state = SpawnState(kind: 1, cooldown: 0);
        var step = PlatformAuxiliaryHazardSpawner.Step(state, entropy48: 0x00, playerY40: 0x50);

        Require(step.SlotAOutcome == PlatformAuxiliaryHazardSpawnAttemptOutcome.Spawned,
            "first empty slot spawns");
        Require(step.State.SlotA.SpriteType1 == 0x80 && step.State.SlotA.Y0 == 0x50,
            "kind1 creates $80 at entropy-derived Y");
        Require(step.State.SlotA.X3 == 0x02 && step.State.SlotA.Flags2 == 0x42,
            "entropy bit3 clear spawns from left");
        Require(step.State.HorizontalStep03A5 == 1 && step.State.VerticalStep03A6 == 0,
            "left spawn seeds +1/0 homing globals");
        Require(step.State.MetadataA == new PlatformAuxiliaryHazardMetadata(1, 0x1E, 0x02, 0x02, 0x32),
            "spawn copies profile combat metadata");
        Require(step.SlotBOutcome == PlatformAuxiliaryHazardSpawnAttemptOutcome.CooldownDecremented,
            "second empty slot consumes one cooldown tick in same frame");
        Require(step.State.Cooldown03B6 == 0x1F,
            "carry-seeded $20 cooldown becomes $1F after second-slot attempt");
    }

    private static void CheckKind3UsesOnlyFirstSlot()
    {
        var state = SpawnState(kind: 3, cooldown: 0);
        var step = PlatformAuxiliaryHazardSpawner.Step(state, entropy48: 0x00, playerY40: 0x50);

        Require(step.SlotAOutcome == PlatformAuxiliaryHazardSpawnAttemptOutcome.Spawned,
            "kind3 spawns first slot");
        Require(step.State.SlotA.SpriteType1 == 0xF4 && step.State.SlotA.X3 == 0xFF,
            "kind3 forces F4/right-side spawn independent of entropy bit3");
        Require(step.State.HorizontalStep03A5 == -1,
            "kind3 right-side spawn seeds -1 horizontal velocity");
        Require(step.SlotBOutcome == PlatformAuxiliaryHazardSpawnAttemptOutcome.NotEligible,
            "kind3 never attempts second slot");
        Require(step.State.Cooldown03B6 == 0x20,
            "kind3 retains full carry-seeded cooldown because no second attempt runs");
    }

    private static void CheckRejectedYPreservesPartialWrites()
    {
        var state = SpawnState(kind: 1, cooldown: 0);
        var step = PlatformAuxiliaryHazardSpawner.Step(state, entropy48: 0x3F, playerY40: 0x10);

        Require(step.SlotAOutcome == PlatformAuxiliaryHazardSpawnAttemptOutcome.RejectedSpawnY,
            "wrapped Y >= $A0 rejects spawn");
        Require(step.State.SlotA.SpriteType1 == 0xFE,
            "rejected attempt leaves slot logically empty");
        Require(step.State.SlotA.X3 == 0xFF && step.State.SlotA.Flags2 == 0x02,
            "X/facing writes occur before Y rejection");
        Require(step.State.Cooldown03B6 == 0x3E,
            "seed $3F is immediately decremented by second empty-slot attempt");
    }

    private static void CheckOccupiedFirstSlotDoesNotConsumeCooldown()
    {
        var occupied = PlatformAuxiliaryHazardSlot.Empty with { SpriteType1 = 0x80 };
        var state = SpawnState(kind: 1, cooldown: 1) with { SlotA = occupied };
        var step = PlatformAuxiliaryHazardSpawner.Step(state, entropy48: 0, playerY40: 0x50);

        Require(step.SlotAOutcome == PlatformAuxiliaryHazardSpawnAttemptOutcome.Occupied,
            "occupied slot exits before cooldown logic");
        Require(step.SlotBOutcome == PlatformAuxiliaryHazardSpawnAttemptOutcome.CooldownDecremented,
            "second empty slot consumes the single cooldown tick");
        Require(step.State.Cooldown03B6 == 0,
            "cooldown reaches zero only on second-slot attempt");
    }

    private static void CheckOrdinaryAnimationAndMovement()
    {
        var slot = PlatformAuxiliaryHazardSlot.Empty with
        {
            Y0 = 0x40,
            SpriteType1 = 0x80,
            Flags2 = 0x40,
            X3 = 0x20,
        };
        var step = PlatformAuxiliaryHazardUpdater.Step(
            slot, 0, 0, frameCounter3C: 0, cameraDelta43: 1,
            playerX3F: 0x60, playerY40: 0x40, frameStartPlayerAction4E: 0x00);

        Require(step.Outcome == PlatformAuxiliaryHazardUpdateOutcome.Active,
            "ordinary hazard remains active");
        Require(step.Slot.SpriteType1 == 0x81,
            "$80 animation advances to $81 on 4-frame boundary");
        Require(step.Slot.X3 == 0x21,
            "facing-right +2 motion then -1 camera yields +1 net X");
        Require(step.RequestsPlayerContactCheck && step.RequestsProjectileHitCheck,
            "ordinary hazard requests both player contact and projectile-hit checks");
    }

    private static void CheckOrdinaryBoundaryRemoval()
    {
        var slot = PlatformAuxiliaryHazardSlot.Empty with
        {
            Y0 = 0x40,
            SpriteType1 = 0x83,
            Flags2 = 0x00,
            X3 = 0x03,
        };
        var step = PlatformAuxiliaryHazardUpdater.Step(
            slot, 0, 0, frameCounter3C: 1, cameraDelta43: 1,
            playerX3F: 0x40, playerY40: 0x40, frameStartPlayerAction4E: 0);

        Require(step.Outcome == PlatformAuxiliaryHazardUpdateOutcome.RemovedHorizontalBoundary,
            "post-motion X 0 triggers removal");
        Require(step.Slot.SpriteType1 == 0xFE && step.Slot.Y0 == 0xF0,
            "removal writes FE/F0 sentinels");
        Require(step.Slot.MirrorY4 == 0xFE && step.Slot.MirrorSpriteType5 == 0xFE,
            "removal also clears mirror Y/type fields");
    }

    private static void CheckHomingRefreshAndMotion()
    {
        var slot = PlatformAuxiliaryHazardSlot.Empty with
        {
            Y0 = 0x30,
            SpriteType1 = 0xF4,
            Flags2 = 0x02,
            X3 = 0x20,
        };
        var step = PlatformAuxiliaryHazardUpdater.Step(
            slot, 0, 0, frameCounter3C: 0, cameraDelta43: 0,
            playerX3F: 0x40, playerY40: 0x40, frameStartPlayerAction4E: 0);

        Require(step.Slot.SpriteType1 == 0xF5 && step.Slot.MirrorSpriteType5 == 0xF3,
            "F4/F5 pair toggles and writes paired mirror type");
        Require(step.HorizontalStep03A5 == 1 && step.VerticalStep03A6 == 1,
            "32-frame steering refresh points toward player");
        Require(step.Slot.X3 == 0x21 && step.Slot.Y0 == 0x31,
            "even frame applies refreshed X/Y homing steps immediately");
        Require(step.Slot.MirrorX7 == 0x21 && step.Slot.MirrorY4 == 0x29,
            "homing mirror coordinates follow X and Y-8");
        Require(step.RequestsPlayerContactCheck && !step.RequestsProjectileHitCheck,
            "F4/F5 family contacts player but bypasses projectile-hit check");
    }

    private static void CheckHomingVerticalRemoval()
    {
        var slot = PlatformAuxiliaryHazardSlot.Empty with
        {
            Y0 = 0xA7,
            SpriteType1 = 0xF4,
            X3 = 0x40,
        };
        var step = PlatformAuxiliaryHazardUpdater.Step(
            slot, 0, 1, frameCounter3C: 2, cameraDelta43: 0,
            playerX3F: 0x60, playerY40: 0x60, frameStartPlayerAction4E: 0);

        Require(step.Outcome == PlatformAuxiliaryHazardUpdateOutcome.RemovedHomingVerticalBoundary,
            "mirror Y reaching $A0 removes homing hazard before X step");
        Require(step.Slot.SpriteType1 == 0xFE,
            "vertical-boundary removal clears hazard type");
    }

    private static void CheckPlayer80FamilySuppressesContactOnly()
    {
        var slot = PlatformAuxiliaryHazardSlot.Empty with
        {
            Y0 = 0x40,
            SpriteType1 = 0x80,
            Flags2 = 0x40,
            X3 = 0x20,
        };
        var step = PlatformAuxiliaryHazardUpdater.Step(
            slot, 0, 0, frameCounter3C: 1, cameraDelta43: 0,
            playerX3F: 0x40, playerY40: 0x40, frameStartPlayerAction4E: 0x80);

        Require(!step.RequestsPlayerContactCheck,
            "player family $80 suppresses $9887 contact branch");
        Require(step.RequestsProjectileHitCheck,
            "ordinary hazard still reaches $9896/$9915 projectile check");
    }

    private static PlatformAuxiliaryHazardSpawnerState SpawnState(byte kind, byte cooldown) =>
        new(
            CurrentKind03B4: kind,
            ActiveKind03B5: kind,
            Cooldown03B6: cooldown,
            HorizontalStep03A5: 0,
            VerticalStep03A6: 0,
            SlotA: PlatformAuxiliaryHazardSlot.Empty,
            SlotB: PlatformAuxiliaryHazardSlot.Empty,
            MetadataA: default,
            MetadataB: default);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Auxiliary hazard self-test failed: {label}");
    }
}

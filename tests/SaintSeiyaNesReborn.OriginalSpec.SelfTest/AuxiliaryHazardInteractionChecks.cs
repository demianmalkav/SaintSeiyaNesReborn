using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class AuxiliaryHazardInteractionChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckGenericContactPreservesCommonGeometry();
        CheckFirstHazardContactBlocksSecond();
        CheckHyogaProjectileConsumedBeforeSecondHazard();
        CheckHomingHazardSkipsProjectilePath();
        CheckFreshSpawnUpdatesInSameFrame();
    }

    private static void CheckGenericContactPreservesCommonGeometry()
    {
        var legacy = PlatformEntityContact.EvaluateOrdinary(
            frameStartAction4E: 0,
            playerX: 0x40,
            playerY: 0x40,
            entityX: 0x40,
            entityY: 0x40,
            currentHazardLatch76: 0,
            entityLifeDrainTicks: 2,
            entityCosmoDrainTicks: 3);

        var generic = PlatformEntityContact.Evaluate(
            frameStartAction4E: 0,
            playerX: 0x40,
            playerY: 0x40,
            entityX: 0x40,
            entityY: 0x40,
            currentHazardLatch76: 0,
            entityLifeDrainTicks: 2,
            entityCosmoDrainTicks: 3,
            PlatformContactHitboxParameters.CommonEntity);

        Require(legacy == generic,
            "generic contact reduction preserves the original common-entity wrapper");
    }

    private static void CheckFirstHazardContactBlocksSecond()
    {
        var state = PairState(
            SlotAt(0x3E, 0x40, 0x80),
            SlotAt(0x3E, 0x40, 0x83),
            Metadata(kind: 1, cosmo: 2, life: 3),
            Metadata(kind: 2, cosmo: 8, life: 9));

        var frame = PlatformAuxiliaryHazardInteractions.StepPair(
            state,
            entropy48: 0,
            frameCounter3C: 1,
            cameraDelta43: 0,
            playerX3F: 0x40,
            playerY40: 0x40,
            frameStartPlayerAction4E: 0,
            attacks: PlatformAttackState.Empty,
            contactState: new PlatformContactPhaseState(0, default),
            saint: PlatformSaintIndex.Seiya,
            platformDamage: 10,
            seventhSense: 0,
            engineSubstate02: 1);

        Require(frame.SlotA.Contact!.Value.Outcome == PlatformEntityContactOutcome.ContactTriggered,
            "slot A triggers auxiliary contact first");
        Require(frame.SlotB.Contact!.Value.Outcome == PlatformEntityContactOutcome.ContactLatchActive,
            "slot B sees A-seeded $76=$20 and cannot trigger again");
        Require(frame.ContactState.HazardLatch76 == 0x20,
            "A contact leaves shared latch at $20");
        Require(frame.ContactState.DrainState == new ContactDrainState(LifeTicks: 3, CosmoTicks: 2),
            "B does not overwrite A's $7F/$80 drain profile");
    }

    private static void CheckHyogaProjectileConsumedBeforeSecondHazard()
    {
        var attack = new PlatformAttackSlot(
            new PlatformAttackObject(
                Y: 0x40,
                Type: 0x64,
                Facing: 0x40,
                X: 0x40,
                Field4: 0,
                Field5: 0,
                Field6: 0,
                AuxiliaryX: 0),
            RangeCounter: 3);
        var attacks = PlatformAttackState.Empty with { Slot0 = attack };

        var state = PairState(
            SlotAt(0x3E, 0x40, 0x80),
            SlotAt(0x3E, 0x40, 0x83),
            Metadata(kind: 1, cosmo: 2, life: 2),
            Metadata(kind: 2, cosmo: 4, life: 1));

        var frame = PlatformAuxiliaryHazardInteractions.StepPair(
            state,
            entropy48: 0,
            frameCounter3C: 1,
            cameraDelta43: 0,
            playerX3F: 0x70,
            playerY40: 0x70,
            frameStartPlayerAction4E: 0,
            attacks,
            contactState: new PlatformContactPhaseState(1, default),
            saint: PlatformSaintIndex.Hyoga,
            platformDamage: 20,
            seventhSense: 0,
            engineSubstate02: 1);

        var hitA = frame.SlotA.ProjectileHits!.Results[0].Result;
        var hitB = frame.SlotB.ProjectileHits!.Results[0].Result;

        Require(hitA.Outcome == PlatformProjectileHitOutcome.DropReaction,
            "kind1 auxiliary hazard takes the type<5 drop-reaction route");
        Require(hitA.StandardConsumptionApplied,
            "Hyoga consumes the projectile on slot A");
        Require(frame.SlotA.LogicalEntityAfterInteraction.State == 0xE0
            && frame.SlotA.LogicalEntityAfterInteraction.Y == 0x46,
            "auxiliary hit writes logical E0 and Y+6 after position sync");
        Require(hitB.Outcome == PlatformProjectileHitOutcome.NoOverlap,
            "slot B sees the already-consumed projectile");
        Require(frame.AttackState.Slot0.Object.Type == 0xFE,
            "shared attack state remains retired after both slots");
    }

    private static void CheckHomingHazardSkipsProjectilePath()
    {
        var attack = new PlatformAttackSlot(
            new PlatformAttackObject(0x40, 0x64, 0x40, 0x40, 0, 0, 0, 0),
            3);
        var attacks = PlatformAttackState.Empty with { Slot0 = attack };
        var slot = PlatformAuxiliaryHazardSlot.Empty with
        {
            Y0 = 0x40,
            SpriteType1 = 0xF4,
            X3 = 0x40,
            Flags2 = 0x02,
        };

        var frame = PlatformAuxiliaryHazardInteractions.StepSlot(
            slot,
            Metadata(kind: 3, cosmo: 5, life: 2),
            horizontalStep03A5: 0,
            verticalStep03A6: 0,
            frameCounter3C: 1,
            cameraDelta43: 0,
            playerX3F: 0x40,
            playerY40: 0x40,
            frameStartPlayerAction4E: 0,
            attacks,
            contactState: new PlatformContactPhaseState(1, default),
            saint: PlatformSaintIndex.Hyoga,
            platformDamage: 20,
            seventhSense: 0,
            engineSubstate02: 1);

        Require(frame.Contact.HasValue,
            "F4/F5 still requests the player-contact path");
        Require(frame.ProjectileHits is null,
            "F4/F5 never enters $9896/$9915");
        Require(frame.AttackState.Slot0.Object.Type == 0x64,
            "overlapping projectile survives because homing family skips hit routing");
    }

    private static void CheckFreshSpawnUpdatesInSameFrame()
    {
        var state = new PlatformAuxiliaryHazardSpawnerState(
            CurrentKind03B4: 1,
            ActiveKind03B5: 1,
            Cooldown03B6: 0,
            HorizontalStep03A5: 0,
            VerticalStep03A6: 0,
            SlotA: PlatformAuxiliaryHazardSlot.Empty,
            SlotB: PlatformAuxiliaryHazardSlot.Empty,
            MetadataA: default,
            MetadataB: default);

        var frame = PlatformAuxiliaryHazardInteractions.StepPair(
            state,
            entropy48: 0,
            frameCounter3C: 0,
            cameraDelta43: 0,
            playerX3F: 0x60,
            playerY40: 0x50,
            frameStartPlayerAction4E: 0,
            attacks: PlatformAttackState.Empty,
            contactState: new PlatformContactPhaseState(1, default),
            saint: PlatformSaintIndex.Seiya,
            platformDamage: 10,
            seventhSense: 0,
            engineSubstate02: 1);

        Require(frame.Spawn.SlotAOutcome == PlatformAuxiliaryHazardSpawnAttemptOutcome.Spawned,
            "$96B4 creates slot A before $9761");
        Require(frame.SlotA.Update.Slot.SpriteType1 == 0x81,
            "new $80 hazard immediately receives frame-0 animation $80->$81");
        Require(frame.SlotA.Update.Slot.X3 == 0x04,
            "new left-side hazard immediately moves +2 in the same frame");
    }

    private static PlatformAuxiliaryHazardSpawnerState PairState(
        PlatformAuxiliaryHazardSlot a,
        PlatformAuxiliaryHazardSlot b,
        PlatformAuxiliaryHazardMetadata ma,
        PlatformAuxiliaryHazardMetadata mb) =>
        new(
            CurrentKind03B4: 0,
            ActiveKind03B5: 1,
            Cooldown03B6: 0,
            HorizontalStep03A5: 0,
            VerticalStep03A6: 0,
            SlotA: a,
            SlotB: b,
            MetadataA: ma,
            MetadataB: mb);

    private static PlatformAuxiliaryHazardSlot SlotAt(byte x, byte y, byte sprite) =>
        PlatformAuxiliaryHazardSlot.Empty with
        {
            X3 = x,
            Y0 = y,
            SpriteType1 = sprite,
            Flags2 = 0x40,
            MirrorFlags6 = 0x40,
        };

    private static PlatformAuxiliaryHazardMetadata Metadata(byte kind, byte cosmo, byte life) =>
        new(
            Kind09: kind,
            HitPoints0C: 0x1E,
            CosmoDrainTicks0D: cosmo,
            LifeDrainTicks0E: life,
            SeventhSenseReward0F: 0x32);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Auxiliary hazard interaction self-test failed: {label}");
    }
}

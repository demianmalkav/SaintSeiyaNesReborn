using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class ProjectileHitRouterChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckNoOverlap();
        CheckSpecialKnockback();
        CheckDropAndAbsorbRoutes();
        CheckHpRoutesAndRecoil();
        CheckDirectTypeD();
        CheckObservedSoundTable();
    }

    private static void CheckNoOverlap()
    {
        var result = Resolve(
            Slot(y: 0x10, x: 0x10),
            Entity(type: 5),
            PlatformSaintIndex.Seiya,
            damage: 10);
        Require(result.Outcome == PlatformProjectileHitOutcome.NoOverlap, "non-overlap exits before sounds/routing");
        Require(result.PrimarySoundId is null && result.SecondarySoundId is null, "non-overlap has no hit sounds");
    }

    private static void CheckSpecialKnockback()
    {
        var typeA = Entity(type: 0x0A) with { RightTerrain0A = 0x00, Motion3 = 0x11 };
        var seiya = Resolve(Slot(), typeA, PlatformSaintIndex.Seiya, 10);
        Require(seiya.Outcome == PlatformProjectileHitOutcome.KnockbackOnly, "type A uses special knockback path");
        Require(seiya.Entity.HitPoints == typeA.HitPoints && seiya.Entity.Motion3 == 0x44,
            "right-facing type-A hit writes facing bit + amount4 without HP damage");
        Require(seiya.PrimarySoundId == 0x28 && seiya.SecondarySoundId is null,
            "type A only uses initial hit sound in this path");
        Require(!seiya.StandardConsumptionApplied && seiya.AttackSlot.Object.Type == 0x64,
            "Seiya survives standard consumption helper");

        var hyoga = Resolve(Slot(), typeA, PlatformSaintIndex.Hyoga, 10);
        Require(hyoga.StandardConsumptionApplied && hyoga.AttackSlot.Object.Type == 0xFE,
            "Hyoga type-A hit is consumed by helper $9A27");

        var blocked = Resolve(
            Slot(),
            typeA with { RightTerrain0A = 0xE5, Motion3 = 0x11 },
            PlatformSaintIndex.Seiya,
            10);
        Require(blocked.MotionWasTerrainBlocked && blocked.Entity.Motion3 == 0x11,
            "$E0-$EF right terrain blocks motion write");

        var leftAttack = Slot() with { Object = Slot().Object with { Facing = 0 } };
        var left = Resolve(
            leftAttack,
            typeA with { LeftTerrain0B = 0xF0 },
            PlatformSaintIndex.Seiya,
            10);
        Require(left.Entity.Motion3 == 0x04,
            "$F0+ terrain allows left knockback and clears facing bit");
    }

    private static void CheckDropAndAbsorbRoutes()
    {
        var type1 = Resolve(Slot(), Entity(1), PlatformSaintIndex.Hyoga, 10);
        Require(type1.Outcome == PlatformProjectileHitOutcome.DropReaction, "type1 falls into $99AA reaction");
        Require(type1.Entity.State == 0xE0 && type1.Entity.Y == 0x46,
            "drop reaction writes state E0 and Y+6");
        Require(type1.StandardConsumptionApplied && type1.AttackSlot.Object.Type == 0xFE,
            "drop reaction runs standard consumption first");
        Require(type1.SecondarySoundId == 0x28, "type1 uses type sound $28");

        var highType = Resolve(Slot(), Entity(0x10), PlatformSaintIndex.Seiya, 10);
        Require(highType.Outcome == PlatformProjectileHitOutcome.DropReaction,
            "observed high type $10 follows fallthrough reaction");
        Require(highType.SecondarySoundId == 0xB2,
            "type $10 uses observed $C0D3 sound mapping before reaction");

        var typeE = Resolve(Slot(), Entity(0x0E), PlatformSaintIndex.Hyoga, 10);
        Require(typeE.Outcome == PlatformProjectileHitOutcome.AbsorbedNoHp,
            "type E accepts hit but bypasses ordinary HP");
        Require(typeE.Entity.HitPoints == 30 && typeE.Entity.State == 0x10,
            "type E leaves entity HP/state unchanged");
        Require(typeE.StandardConsumptionApplied && typeE.AttackSlot.Object.Type == 0xFE,
            "type E still runs standard attack consumption");
        Require(typeE.SecondarySoundId == 0x28, "type E uses type-indexed sound");
    }

    private static void CheckHpRoutesAndRecoil()
    {
        var type5 = Entity(5) with { RightTerrain0A = 0x00 };
        var survive = Resolve(Slot(), type5, PlatformSaintIndex.Seiya, 10);
        Require(survive.Outcome == PlatformProjectileHitOutcome.HpSurvived,
            "type5 uses ordinary HP path");
        Require(survive.Entity.HitPoints == 20 && survive.Entity.State == 0x40,
            "type5 surviving HP/state");
        Require(survive.Entity.Motion3 == 0x48,
            "surviving type<8 gets amount8 right recoil");
        Require(survive.PrimarySoundId == 0x28 && survive.SecondarySoundId == 0x28,
            "type5 runs initial and type-indexed sounds");

        var blockedRecoil = Resolve(
            Slot(),
            type5 with { RightTerrain0A = 0xE0, Motion3 = 0x12 },
            PlatformSaintIndex.Seiya,
            10);
        Require(blockedRecoil.MotionWasTerrainBlocked && blockedRecoil.Entity.Motion3 == 0x12,
            "solid E0-EF terrain suppresses survivor recoil motion write");

        var type7 = Resolve(Slot(), Entity(7), PlatformSaintIndex.Seiya, 10);
        Require(type7.SecondarySoundId == 0x31, "type7 has distinct secondary sound $31");

        var type8 = Resolve(Slot(), Entity(8) with { Motion3 = 0x12 }, PlatformSaintIndex.Seiya, 10);
        Require(type8.Outcome == PlatformProjectileHitOutcome.HpSurvived && type8.Entity.Motion3 == 0x12,
            "type8 survives HP path but does not take type<8 recoil");

        var type0Hyoga = Resolve(Slot(), Entity(0), PlatformSaintIndex.Hyoga, 10);
        Require(type0Hyoga.StandardConsumptionApplied && type0Hyoga.AttackSlot.Object.Type == 0xFE,
            "type0 routes through standard consumption before HP");
        Require(type0Hyoga.SecondarySoundId is null,
            "type0 jumps to HP before type-indexed sound table");

        var kill = Resolve(Slot(), Entity(5) with { HitPoints = 10, SeventhSenseRewardBcd = 0x15 },
            PlatformSaintIndex.Seiya, 10, seventhSense: 1234, substate02: 1);
        Require(kill.Outcome == PlatformProjectileHitOutcome.HpKilled && kill.Entity.State == 0xD0,
            "ordinary exact kill enters D0");
        Require(kill.SeventhSense == 1249,
            "kill composes packed-BCD Seventh Sense reward");
    }

    private static void CheckDirectTypeD()
    {
        var typeD = Entity(0x0D);
        var hyoga = Resolve(Slot(), typeD, PlatformSaintIndex.Hyoga, 10);
        Require(hyoga.Outcome == PlatformProjectileHitOutcome.HpSurvived,
            "type D goes directly to HP path");
        Require(!hyoga.StandardConsumptionApplied && hyoga.AttackSlot.Object.Type == 0x64,
            "type D bypasses helper $9A27 even for Hyoga");
        Require(hyoga.SecondarySoundId is null,
            "type D bypasses type-indexed secondary sound in this hit path");
        Require(hyoga.Entity.Motion3 == typeD.Motion3,
            "type D survival bypasses type<8 recoil path");

        var killed = Resolve(Slot(), typeD with { HitPoints = 10 }, PlatformSaintIndex.Hyoga, 10);
        Require(killed.Outcome == PlatformProjectileHitOutcome.HpKilled && killed.Entity.State == 0xA0,
            "type D death uses state A0");
    }

    private static void CheckObservedSoundTable()
    {
        Require(PlatformProjectileHitRouter.SecondarySoundForObservedType(0x00) == 0x2E, "sound table type0 byte");
        Require(PlatformProjectileHitRouter.SecondarySoundForObservedType(0x07) == 0x31, "sound table type7 byte");
        Require(PlatformProjectileHitRouter.SecondarySoundForObservedType(0x0D) == 0x35, "sound table typeD byte exists even if hit router bypasses it");
        Require(PlatformProjectileHitRouter.SecondarySoundForObservedType(0x10) == 0xB2, "sound table type10 byte");
        Require(PlatformProjectileHitRouter.SecondarySoundForObservedType(0x1F) == 0x04, "sound table type1F byte");
    }

    private static PlatformProjectileHitResult Resolve(
        PlatformAttackSlot slot,
        PlatformCombatEntity entity,
        PlatformSaintIndex saint,
        int damage,
        int seventhSense = 0,
        byte substate02 = 1) => PlatformProjectileHitRouter.Resolve(
            slot,
            entity,
            saint,
            PlatformHitboxParameters.Common,
            damage,
            seventhSense,
            substate02);

    private static PlatformAttackSlot Slot(byte y = 0x40, byte x = 0x50) => new(
        new PlatformAttackObject(y, 0x64, 0x40, x, 0, 0, 0, 0),
        10);

    private static PlatformCombatEntity Entity(byte type) => new(
        State: 0x10,
        X: 0x50,
        Y: 0x40,
        Type: type,
        HitPoints: 30,
        SeventhSenseRewardBcd: 0x00);

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException($"ProjectileHitRouter self-test failed: {message}");
    }
}

using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class ProjectileHitChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckAcceptedTypesAndBounds();
        CheckHitConsumption();
        CheckOrdinaryDamage();
    }

    private static void CheckAcceptedTypesAndBounds()
    {
        Require(PlatformProjectileHit.IsAcceptedAttackType(0x64), "$64 accepted");
        Require(PlatformProjectileHit.IsAcceptedAttackType(0x65), "$65 accepted after mask");
        Require(PlatformProjectileHit.IsAcceptedAttackType(0x54), "$54 accepted");
        Require(PlatformProjectileHit.IsAcceptedAttackType(0x55), "$55 accepted after mask");
        Require(!PlatformProjectileHit.IsAcceptedAttackType(0xFE), "$FE inactive rejected");

        var entity = new PlatformCombatEntity(
            State: 0x10,
            X: 0x50,
            Y: 0x40,
            Type: 0x05,
            HitPoints: 30,
            SeventhSenseRewardBcd: 0x15);

        // Common box: Y center=$48, low=$3D exclusive, high=$4D inclusive.
        // X center=$58, low=$4B exclusive, high=$5D inclusive.
        Require(PlatformProjectileHit.Overlaps(Attack(0x3E, 0x4C), entity, PlatformHitboxParameters.Common),
            "first point inside exclusive low bounds overlaps");
        Require(PlatformProjectileHit.Overlaps(Attack(0x4D, 0x5D), entity, PlatformHitboxParameters.Common),
            "inclusive high bounds overlap");
        Require(!PlatformProjectileHit.Overlaps(Attack(0x3D, 0x4C), entity, PlatformHitboxParameters.Common),
            "low Y bound is exclusive");
        Require(!PlatformProjectileHit.Overlaps(Attack(0x3E, 0x4B), entity, PlatformHitboxParameters.Common),
            "low X bound is exclusive");
        Require(!PlatformProjectileHit.Overlaps(Attack(0x4E, 0x4C), entity, PlatformHitboxParameters.Common),
            "past high Y bound rejected");
        Require(!PlatformProjectileHit.Overlaps(Attack(0x3E, 0x5E), entity, PlatformHitboxParameters.Common),
            "past high X bound rejected");

        var shunSecond = Attack(0x40, 0x50) with { Type = 0x55 };
        Require(PlatformProjectileHit.Overlaps(shunSecond, entity, PlatformHitboxParameters.Common),
            "Shun second chain segment $55 participates in hit gate");

        var inactive = Attack(0x40, 0x50) with { Type = 0xFE };
        Require(!PlatformProjectileHit.Overlaps(inactive, entity, PlatformHitboxParameters.Common),
            "inactive object never overlaps regardless of coordinates");

        Require(PlatformHitboxParameters.Common == new PlatformHitboxParameters(8, 8, 5, 5), "common hitbox preset");
        Require(PlatformHitboxParameters.Reduced == new PlatformHitboxParameters(4, 4, 2, 2), "reduced hitbox preset");
        Require(PlatformHitboxParameters.Tall == new PlatformHitboxParameters(16, 8, 14, 4), "tall hitbox preset");
        Require(PlatformHitboxParameters.Square == new PlatformHitboxParameters(8, 8, 6, 6), "square hitbox preset");

        // Arithmetic wraps before unsigned comparison, exactly like the 6502.
        var wrapEntity = entity with { X = 0, Y = 0 };
        Require(!PlatformProjectileHit.Overlaps(Attack(1, 1), wrapEntity, PlatformHitboxParameters.Reduced),
            "wrapped low bounds preserve original unsigned comparison quirk");
    }

    private static void CheckHitConsumption()
    {
        var slot = new PlatformAttackSlot(Attack(0x40, 0x50), 10);

        Require(PlatformProjectileHit.ApplyStandardHitConsumption(slot, PlatformSaintIndex.Seiya) == slot,
            "Seiya survives standard hit helper");
        Require(PlatformProjectileHit.ApplyStandardHitConsumption(slot, PlatformSaintIndex.Shun) == slot,
            "Shun survives standard hit helper");
        Require(PlatformProjectileHit.ApplyStandardHitConsumption(slot, PlatformSaintIndex.Ikki) == slot,
            "Ikki survives standard hit helper");

        foreach (var saint in new[] { PlatformSaintIndex.Hyoga, PlatformSaintIndex.Shiryu })
        {
            var consumed = PlatformProjectileHit.ApplyStandardHitConsumption(slot, saint);
            Require(consumed.Object.Y == 0xF0 && consumed.Object.Type == 0xFE,
                $"{saint} attack is retired by $9A27");
            Require(consumed.RangeCounter == 10, "$9A27 does not alter lifetime byte");
        }
    }

    private static void CheckOrdinaryDamage()
    {
        Require(PlatformProjectileHit.UsesOrdinaryHpPath(0), "type0 ordinary HP");
        Require(PlatformProjectileHit.UsesOrdinaryHpPath(0x05), "type5 ordinary HP");
        Require(PlatformProjectileHit.UsesOrdinaryHpPath(0x09), "type9 ordinary HP");
        Require(PlatformProjectileHit.UsesOrdinaryHpPath(0x0C), "typeC ordinary HP");
        Require(PlatformProjectileHit.UsesOrdinaryHpPath(0x0D), "typeD ordinary HP");
        Require(PlatformProjectileHit.UsesOrdinaryHpPath(0x0F), "typeF ordinary HP");
        Require(!PlatformProjectileHit.UsesOrdinaryHpPath(0x0A), "typeA special path");
        Require(!PlatformProjectileHit.UsesOrdinaryHpPath(0x0B), "typeB special path");
        Require(!PlatformProjectileHit.UsesOrdinaryHpPath(0x0E), "typeE bypasses HP path");

        var enemy = new PlatformCombatEntity(0x10, 0x50, 0x40, 0x05, 30, 0x15);
        var survive = PlatformProjectileHit.ApplyOrdinaryHpDamage(enemy, damage: 10, currentSeventhSense: 1234, engineSubstate02: 1);
        Require(survive.Outcome == PlatformEntityDamageOutcome.Survived, "positive remaining HP survives");
        Require(survive.Entity.HitPoints == 20 && survive.Entity.State == 0x40, "survivor HP/state40");
        Require(survive.SeventhSense == 1234 && !survive.AwardedSeventhSense, "no reward before kill");

        var exactKill = PlatformProjectileHit.ApplyOrdinaryHpDamage(enemy, damage: 30, currentSeventhSense: 1234, engineSubstate02: 1);
        Require(exactKill.Outcome == PlatformEntityDamageOutcome.Killed && exactKill.Entity.State == 0xD0,
            "zero remaining HP kills into state D0");
        Require(exactKill.SeventhSense == 1249 && exactKill.AwardedSeventhSense,
            "packed BCD $15 awards +15 Seventh Sense");

        var overkill = PlatformProjectileHit.ApplyOrdinaryHpDamage(enemy, damage: 31, currentSeventhSense: 9990, engineSubstate02: 1);
        Require(overkill.Outcome == PlatformEntityDamageOutcome.Killed && overkill.SeventhSense == 9999,
            "borrow/overkill kills and reward clamps at 9999");

        var gatedReward = PlatformProjectileHit.ApplyOrdinaryHpDamage(enemy, damage: 30, currentSeventhSense: 1234, engineSubstate02: 0);
        Require(gatedReward.SeventhSense == 1234 && !gatedReward.AwardedSeventhSense,
            "$02==0 suppresses platform Seventh Sense reward");

        var typeD = enemy with { Type = 0x0D };
        var typeDKill = PlatformProjectileHit.ApplyOrdinaryHpDamage(typeD, damage: 30, currentSeventhSense: 0, engineSubstate02: 1);
        Require(typeDKill.Entity.State == 0xA0, "type $0D uses special death state A0");

        var damageFromCosmo = PlatformDamage.FromCosmo(PlatformSaintIndex.Seiya, 100);
        var integrated = PlatformProjectileHit.ApplyOrdinaryHpDamage(enemy, damageFromCosmo, 0, 1);
        Require(integrated.Entity.HitPoints == 11,
            "Seiya Cosmo100 damage 19 feeds ordinary entity HP path");
    }

    private static PlatformAttackObject Attack(byte y, byte x) => new(
        Y: y,
        Type: 0x64,
        Facing: 0x40,
        X: x,
        Field4: 0,
        Field5: 0,
        Field6: 0,
        AuxiliaryX: 0);

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException($"ProjectileHit self-test failed: {message}");
    }
}

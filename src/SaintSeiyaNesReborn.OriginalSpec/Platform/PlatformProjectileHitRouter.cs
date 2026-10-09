using SaintSeiyaNesReborn.OriginalSpec;

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformProjectileHitOutcome
{
    NoOverlap,
    HpSurvived,
    HpKilled,
    KnockbackOnly,
    DropReaction,
    AbsorbedNoHp,
}

public readonly record struct PlatformProjectileHitResult(
    PlatformAttackSlot AttackSlot,
    PlatformCombatEntity Entity,
    PlatformProjectileHitOutcome Outcome,
    int SeventhSense,
    byte? PrimarySoundId,
    byte? SecondarySoundId,
    bool StandardConsumptionApplied,
    bool MotionWasTerrainBlocked);

/// <summary>
/// Full type router after geometric overlap at bank 3 $9971-$9A27.
/// It composes the already isolated overlap/HP primitives without flattening
/// special entity-type responses into a universal damage rule.
/// </summary>
public static class PlatformProjectileHitRouter
{
    // Fixed table $C0D3 for the entity-type range used by the platform combat
    // router. The sound routine returns A from restored X, so the entity type is
    // still available to the comparisons following JSR $DBB6.
    private static readonly byte[] TypeSound0To0F =
    [
        0x2E, 0x28, 0x28, 0x28,
        0x28, 0x28, 0x28, 0x31,
        0x28, 0x28, 0x28, 0x28,
        0x28, 0x35, 0x28, 0x28,
    ];

    public static PlatformProjectileHitResult Resolve(
        PlatformAttackSlot attackSlot,
        PlatformCombatEntity entity,
        PlatformSaintIndex saint,
        PlatformHitboxParameters hitbox,
        int platformDamage,
        int currentSeventhSense,
        byte engineSubstate02)
    {
        if (!PlatformProjectileHit.Overlaps(attackSlot.Object, entity, hitbox))
        {
            return new PlatformProjectileHitResult(
                attackSlot,
                entity,
                PlatformProjectileHitOutcome.NoOverlap,
                currentSeventhSense,
                null,
                null,
                false,
                false);
        }

        // $9971 plays $28 for every accepted geometric hit before type routing.
        const byte primarySound = 0x28;
        var type = entity.Type;

        // $0A/$0B branch directly to $99F4: standard consumption, then a
        // 4-unit directional motion response if terrain does not block it.
        if (type is 0x0A or 0x0B)
        {
            var consumed = PlatformProjectileHit.ApplyStandardHitConsumption(attackSlot, saint);
            var impulse = ApplyDirectionalMotion(entity, attackSlot.Object.Facing, amount: 4);
            return new PlatformProjectileHitResult(
                consumed,
                impulse.Entity,
                PlatformProjectileHitOutcome.KnockbackOnly,
                currentSeventhSense,
                primarySound,
                null,
                consumed != attackSlot,
                impulse.Blocked);
        }

        // Type $0D jumps straight to HP subtraction and bypasses helper $9A27
        // and the type-indexed secondary sound table.
        if (type == 0x0D)
            return ResolveHp(
                attackSlot,
                entity,
                saint,
                platformDamage,
                currentSeventhSense,
                engineSubstate02,
                primarySound,
                secondarySound: null,
                standardConsumptionApplied: false,
                applySurvivorRecoil: false);

        // Every remaining overlapping type runs helper $9A27 first.
        var postConsumption = PlatformProjectileHit.ApplyStandardHitConsumption(attackSlot, saint);
        var consumedByHelper = postConsumption != attackSlot;

        // Type zero goes immediately to ordinary HP without the type-indexed
        // sound call.
        if (type == 0)
            return ResolveHp(
                postConsumption,
                entity,
                saint,
                platformDamage,
                currentSeventhSense,
                engineSubstate02,
                primarySound,
                secondarySound: null,
                standardConsumptionApplied: consumedByHelper,
                applySurvivorRecoil: true);

        var secondarySound = SecondarySoundForKnownType(type);

        // After DBB6, A is restored from the saved X register, so these are
        // comparisons against entity type, not the sound byte.
        if (type is 0x0F or 0x0C || type is >= 0x05 and < 0x0A)
        {
            return ResolveHp(
                postConsumption,
                entity,
                saint,
                platformDamage,
                currentSeventhSense,
                engineSubstate02,
                primarySound,
                secondarySound,
                consumedByHelper,
                applySurvivorRecoil: type < 0x08);
        }

        if (type == 0x0E)
        {
            return new PlatformProjectileHitResult(
                postConsumption,
                entity,
                PlatformProjectileHitOutcome.AbsorbedNoHp,
                currentSeventhSense,
                primarySound,
                secondarySound,
                consumedByHelper,
                false);
        }

        // Types $01-$04 and the fallthrough high-type family enter the same
        // $99AA response: entity Y += 6 and high-level state $E0.
        var reacted = entity with
        {
            Y = unchecked((byte)(entity.Y + 6)),
            State = 0xE0,
        };
        return new PlatformProjectileHitResult(
            postConsumption,
            reacted,
            PlatformProjectileHitOutcome.DropReaction,
            currentSeventhSense,
            primarySound,
            secondarySound,
            consumedByHelper,
            false);
    }

    public static byte SecondarySoundForKnownType(byte type)
    {
        if (type >= TypeSound0To0F.Length)
            throw new ArgumentOutOfRangeException(nameof(type), type, "Static sound mapping is currently promoted for entity types $00-$0F.");
        return TypeSound0To0F[type];
    }

    private static PlatformProjectileHitResult ResolveHp(
        PlatformAttackSlot slot,
        PlatformCombatEntity entity,
        PlatformSaintIndex saint,
        int damage,
        int seventhSense,
        byte engineSubstate02,
        byte primarySound,
        byte? secondarySound,
        bool standardConsumptionApplied,
        bool applySurvivorRecoil)
    {
        var damageResult = PlatformProjectileHit.ApplyOrdinaryHpDamage(
            entity,
            damage,
            seventhSense,
            engineSubstate02);

        var resolved = damageResult.Entity;
        var blocked = false;
        if (damageResult.Outcome == PlatformEntityDamageOutcome.Survived && applySurvivorRecoil)
        {
            var impulse = ApplyDirectionalMotion(resolved, slot.Object.Facing, amount: 8);
            resolved = impulse.Entity;
            blocked = impulse.Blocked;
        }

        return new PlatformProjectileHitResult(
            slot,
            resolved,
            damageResult.Outcome == PlatformEntityDamageOutcome.Killed
                ? PlatformProjectileHitOutcome.HpKilled
                : PlatformProjectileHitOutcome.HpSurvived,
            damageResult.SeventhSense,
            primarySound,
            secondarySound,
            standardConsumptionApplied,
            blocked);
    }

    private readonly record struct MotionResult(PlatformCombatEntity Entity, bool Blocked);

    /// <summary>
    /// $99F9/$9A19 terrain-gated motion write. Only descriptors $E0-$EF block;
    /// values below $E0 or at/above $F0 allow the motion field update.
    /// </summary>
    private static MotionResult ApplyDirectionalMotion(
        PlatformCombatEntity entity,
        byte attackFacing,
        byte amount)
    {
        var right = (attackFacing & 0x40) != 0;
        var terrain = right ? entity.RightTerrain0A : entity.LeftTerrain0B;
        var blocked = terrain is >= 0xE0 and < 0xF0;
        if (blocked)
            return new MotionResult(entity, true);

        var motion = (byte)((right ? 0x40 : 0x00) + amount);
        return new MotionResult(entity with { Motion3 = motion }, false);
    }
}

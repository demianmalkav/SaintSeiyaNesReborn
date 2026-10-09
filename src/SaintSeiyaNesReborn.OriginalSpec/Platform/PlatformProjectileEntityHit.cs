namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformProjectileHitboxParameters(
    byte VerticalOriginOffset79,
    byte HorizontalOriginOffset7A,
    byte VerticalExtent7B,
    byte HorizontalExtent7C)
{
    // Common A442 call site at $A719: $79=$10, A=$08, X=$0E, Y=$04.
    public static PlatformProjectileHitboxParameters CommonEntity => new(0x10, 0x08, 0x0E, 0x04);
}

public readonly record struct PlatformEntityCombatState(
    PlatformCommonEntityMotionState Motion,
    byte HitPoints,
    byte SeventhSenseRewardPackedBcd);

public enum PlatformProjectileHitOutcome
{
    InactiveAttack,
    NoOverlap,
    ReactionOnly,
    IgnoredAfterCollision,
    HitPointsReduced,
    EntityKilled,
}

public readonly record struct PlatformProjectileHitEvent(
    PlatformAttackSlotId SlotId,
    PlatformProjectileHitOutcome Outcome,
    PlatformAttackSlot AttackSlot,
    PlatformEntityCombatState Entity,
    int SeventhSense,
    bool AttackRetired,
    byte? PrimarySoundId,
    byte? SecondarySoundId)
{
    public bool Collided => Outcome is not PlatformProjectileHitOutcome.InactiveAttack
        and not PlatformProjectileHitOutcome.NoOverlap;
}

public readonly record struct PlatformProjectileHitBatchResult(
    PlatformAttackState AttackState,
    PlatformEntityCombatState Entity,
    int SeventhSense,
    IReadOnlyList<PlatformProjectileHitEvent> Events);

/// <summary>
/// Exact semantic reduction of bank-3 $9915/$992A projectile-vs-entity overlap
/// and the ordinary hit-resolution chain through $9A27.
///
/// The model preserves unsigned 8-bit collision arithmetic, slot order 0->1->2,
/// Saint-specific hit retirement, numeric type special cases, HP/reward behavior
/// and the original knockback-phase terrain gate.
/// </summary>
public static class PlatformProjectileEntityHit
{
    public const byte PrimaryHitSoundId = 0x28;

    private static readonly byte[] TypeReactionSound =
    [
        0x2E, 0x28, 0x28, 0x28, 0x28, 0x28, 0x28, 0x31,
        0x28, 0x28, 0x28, 0x28, 0x28, 0x35, 0x28, 0x28,
    ];

    public static bool IsAcceptedAttackSignature(PlatformAttackObject attack) =>
        (attack.Type & 0xFE) is 0x64 or 0x54;

    public static bool Overlaps(
        PlatformAttackObject attack,
        PlatformCommonEntityMotionState entity,
        PlatformProjectileHitboxParameters hitbox)
    {
        if (!IsAcceptedAttackSignature(attack))
            return false;

        var verticalTemp = unchecked((byte)(entity.Y + hitbox.VerticalOriginOffset79));
        var verticalLow = unchecked((byte)(verticalTemp - hitbox.VerticalExtent7B - 0x06));
        var verticalHigh = unchecked((byte)(verticalTemp + hitbox.VerticalExtent7B));
        if (verticalLow >= attack.Y || verticalHigh < attack.Y)
            return false;

        var horizontalTemp = unchecked((byte)(entity.X + hitbox.HorizontalOriginOffset7A));
        var horizontalLow = unchecked((byte)(horizontalTemp - hitbox.HorizontalExtent7C - 0x08));
        var horizontalHigh = unchecked((byte)(horizontalTemp + hitbox.HorizontalExtent7C));
        return horizontalLow < attack.X && horizontalHigh >= attack.X;
    }

    public static PlatformProjectileHitEvent ResolveSingle(
        PlatformAttackSlotId slotId,
        PlatformAttackSlot attackSlot,
        PlatformEntityCombatState entity,
        PlatformProjectileHitboxParameters hitbox,
        PlatformSaintIndex saint,
        byte attackDamage72,
        int currentSeventhSense,
        byte engineSubstate02)
    {
        if (!IsAcceptedAttackSignature(attackSlot.Object))
        {
            return Event(
                slotId,
                PlatformProjectileHitOutcome.InactiveAttack,
                attackSlot,
                entity,
                currentSeventhSense);
        }

        if (!Overlaps(attackSlot.Object, entity.Motion, hitbox))
        {
            return Event(
                slotId,
                PlatformProjectileHitOutcome.NoOverlap,
                attackSlot,
                entity,
                currentSeventhSense);
        }

        var type = entity.Motion.Type;
        var primary = PrimaryHitSoundId;
        byte? secondary = null;
        var retired = false;

        // Types $0A/$0B branch directly to the magnitude-4 reaction path.
        if (type is 0x0A or 0x0B)
        {
            (attackSlot, retired) = RetireOnHitIfRequired(attackSlot, saint);
            entity = entity with { Motion = ApplyDirectionalReactionPhase(entity.Motion, attackSlot.Object.Facing, 4) };
            return Event(
                slotId,
                PlatformProjectileHitOutcome.ReactionOnly,
                attackSlot,
                entity,
                currentSeventhSense,
                retired,
                primary,
                null);
        }

        // Type $0D bypasses $9A27 and goes straight to ordinary HP damage.
        if (type != 0x0D)
        {
            (attackSlot, retired) = RetireOnHitIfRequired(attackSlot, saint);

            if (type != 0)
            {
                if (type >= TypeReactionSound.Length)
                    throw new ArgumentOutOfRangeException(nameof(entity), type, "Known platform collision type must fit $C0D3 table 0..15.");
                secondary = TypeReactionSound[type];
            }

            if (type == 0x0E)
            {
                return Event(
                    slotId,
                    PlatformProjectileHitOutcome.IgnoredAfterCollision,
                    attackSlot,
                    entity,
                    currentSeventhSense,
                    retired,
                    primary,
                    secondary);
            }

            var takesHpDamage = type == 0
                || type is >= 0x05 and < 0x0A
                || type is 0x0C or 0x0F;

            if (!takesHpDamage)
            {
                var reactionMotion = entity.Motion with
                {
                    Y = unchecked((byte)(entity.Motion.Y + 6)),
                    ActionState = 0xE0,
                };
                return Event(
                    slotId,
                    PlatformProjectileHitOutcome.ReactionOnly,
                    attackSlot,
                    entity with { Motion = reactionMotion },
                    currentSeventhSense,
                    retired,
                    primary,
                    secondary);
            }
        }

        // $99BA ordinary HP path, reached by type 0, 5..9, 0C, 0D, 0F.
        var remaining = entity.HitPoints - attackDamage72;
        if (remaining <= 0)
        {
            var seventh = SeventhSense.AddPlatformReward(
                currentSeventhSense,
                entity.SeventhSenseRewardPackedBcd,
                engineSubstate02);
            var deathAction = type == 0x0D ? (byte)0xA0 : (byte)0xD0;
            entity = entity with
            {
                Motion = entity.Motion with { ActionState = deathAction },
                // The original kill branch does not store zero back to offset $0C.
                HitPoints = entity.HitPoints,
            };
            return Event(
                slotId,
                PlatformProjectileHitOutcome.EntityKilled,
                attackSlot,
                entity,
                seventh,
                retired,
                primary,
                secondary);
        }

        entity = entity with { HitPoints = (byte)remaining };
        var motion = entity.Motion with { ActionState = 0x40 };
        if (type < 0x08)
            motion = ApplyDirectionalReactionPhase(motion, attackSlot.Object.Facing, 8);
        entity = entity with { Motion = motion };

        return Event(
            slotId,
            PlatformProjectileHitOutcome.HitPointsReduced,
            attackSlot,
            entity,
            currentSeventhSense,
            retired,
            primary,
            secondary);
    }

    /// <summary>
    /// Mirrors $9915 slot order. State changes from one collision feed the next
    /// slot test exactly as they do while the original routine stays inside the
    /// same entity update.
    /// </summary>
    public static PlatformProjectileHitBatchResult ResolveAll(
        PlatformAttackState attacks,
        PlatformEntityCombatState entity,
        PlatformProjectileHitboxParameters hitbox,
        PlatformSaintIndex saint,
        byte attackDamage72,
        int currentSeventhSense,
        byte engineSubstate02)
    {
        var events = new List<PlatformProjectileHitEvent>(3);
        var seventh = currentSeventhSense;

        foreach (var id in new[] { PlatformAttackSlotId.Slot0, PlatformAttackSlotId.Slot1, PlatformAttackSlotId.Slot2 })
        {
            var hit = ResolveSingle(
                id,
                attacks.Slot(id),
                entity,
                hitbox,
                saint,
                attackDamage72,
                seventh,
                engineSubstate02);
            events.Add(hit);
            attacks = attacks.WithSlot(id, hit.AttackSlot);
            entity = hit.Entity;
            seventh = hit.SeventhSense;
        }

        return new PlatformProjectileHitBatchResult(attacks, entity, seventh, events);
    }

    /// <summary>
    /// $9A27: Seiya(0), Shun(1) and Ikki(4) keep the current hit object active;
    /// Hyoga(2) and Shiryu(3) hide/deactivate only Y and type of that record.
    /// </summary>
    public static (PlatformAttackSlot Slot, bool Retired) RetireOnHitIfRequired(
        PlatformAttackSlot slot,
        PlatformSaintIndex saint)
    {
        if (saint is PlatformSaintIndex.Seiya or PlatformSaintIndex.Shun or PlatformSaintIndex.Ikki)
            return (slot, false);

        return (
            slot with
            {
                Object = slot.Object with
                {
                    Y = 0xF0,
                    Type = 0xFE,
                },
            },
            true);
    }

    /// <summary>
    /// $99F9-$9A25. Only terrain descriptors $E0-$EF on the projectile-facing
    /// side suppress the phase write. Otherwise phase is magnitude for left and
    /// $40+magnitude for right.
    /// </summary>
    public static PlatformCommonEntityMotionState ApplyDirectionalReactionPhase(
        PlatformCommonEntityMotionState entity,
        byte attackFacing,
        byte magnitude)
    {
        var right = (attackFacing & 0x40) != 0;
        var descriptor = right ? entity.TerrainProbeRight : entity.TerrainProbeLeft;
        if (descriptor is >= 0xE0 and < 0xF0)
            return entity;

        var phase = unchecked((byte)((right ? 0x40 : 0x00) + magnitude));
        return entity with { StatePhase = phase };
    }

    private static PlatformProjectileHitEvent Event(
        PlatformAttackSlotId slotId,
        PlatformProjectileHitOutcome outcome,
        PlatformAttackSlot attackSlot,
        PlatformEntityCombatState entity,
        int seventhSense,
        bool retired = false,
        byte? primary = null,
        byte? secondary = null) =>
        new(slotId, outcome, attackSlot, entity, seventhSense, retired, primary, secondary);
}

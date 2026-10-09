using SaintSeiyaNesReborn.OriginalSpec;

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public sealed record PlatformProjectileHitSequenceResult(
    PlatformAttackState AttackState,
    PlatformCombatEntity Entity,
    int SeventhSense,
    IReadOnlyList<(PlatformAttackSlotId Slot, PlatformProjectileHitResult Result)> Results);

/// <summary>
/// Reproduces $9915 slot order: $0730 -> $0738 -> $0740. There is no
/// post-hit entity-state gate between slots inside this routine; each later slot
/// sees entity fields mutated by the preceding hit.
/// </summary>
public static class PlatformProjectileHitSequence
{
    private static readonly PlatformAttackSlotId[] SlotOrder =
    [
        PlatformAttackSlotId.Slot0,
        PlatformAttackSlotId.Slot1,
        PlatformAttackSlotId.Slot2,
    ];

    public static PlatformProjectileHitSequenceResult ResolveThreeSlots(
        PlatformAttackState attacks,
        PlatformCombatEntity entity,
        PlatformSaintIndex saint,
        PlatformHitboxParameters hitbox,
        int platformDamage,
        int seventhSense,
        byte engineSubstate02)
    {
        var results = new List<(PlatformAttackSlotId, PlatformProjectileHitResult)>(3);

        foreach (var id in SlotOrder)
        {
            var slot = attacks.Slot(id);
            var hit = PlatformProjectileHitRouter.Resolve(
                slot,
                entity,
                saint,
                hitbox,
                platformDamage,
                seventhSense,
                engineSubstate02);

            attacks = attacks.WithSlot(id, hit.AttackSlot);
            entity = hit.Entity;
            seventhSense = hit.SeventhSense;
            results.Add((id, hit));
        }

        return new PlatformProjectileHitSequenceResult(attacks, entity, seventhSense, results);
    }
}

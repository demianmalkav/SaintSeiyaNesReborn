using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class ProjectileHitSequenceMutationChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        var active = new PlatformAttackSlot(
            new PlatformAttackObject(0x40, 0x64, 0x40, 0x50, 0, 0, 0, 0),
            10);
        var attacks = PlatformAttackState.Empty with { Slot0 = active, Slot1 = active };
        var entity = new PlatformCombatEntity(0x10, 0x50, 0x40, 1, 30, 0);

        var sequence = PlatformProjectileHitSequence.ResolveThreeSlots(
            attacks,
            entity,
            PlatformSaintIndex.Seiya,
            PlatformHitboxParameters.Reduced,
            platformDamage: 10,
            seventhSense: 0,
            engineSubstate02: 1);

        if (sequence.Results[0].Result.Outcome != PlatformProjectileHitOutcome.DropReaction)
            throw new InvalidOperationException("slot0 must apply type1 Y+6 reaction");
        if (sequence.Results[1].Result.Outcome != PlatformProjectileHitOutcome.NoOverlap)
            throw new InvalidOperationException("slot1 must test geometry after slot0 Y mutation and miss");
        if (sequence.Entity.Y != 0x46)
            throw new InvalidOperationException("later slots must retain first-hit entity Y mutation");
    }
}

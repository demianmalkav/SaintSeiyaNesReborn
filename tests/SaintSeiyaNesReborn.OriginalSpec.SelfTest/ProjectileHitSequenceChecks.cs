using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class ProjectileHitSequenceChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckOrderedDamageThenRepeatedKill();
        CheckIkkiRepeatedRewardAfterImmediateKill();
        CheckReactionMutationAffectsLaterOverlap();
    }

    private static void CheckOrderedDamageThenRepeatedKill()
    {
        var attacks = ThreeActiveSlots();
        var entity = Enemy(type: 5, hp: 15, reward: 0x10);

        var sequence = PlatformProjectileHitSequence.ResolveThreeSlots(
            attacks,
            entity,
            PlatformSaintIndex.Shiryu,
            PlatformHitboxParameters.Common,
            platformDamage: 10,
            seventhSense: 0,
            engineSubstate02: 1);

        var r0 = sequence.Results[0].Result;
        var r1 = sequence.Results[1].Result;
        var r2 = sequence.Results[2].Result;

        Require(r0.Outcome == PlatformProjectileHitOutcome.HpSurvived && r0.Entity.HitPoints == 5,
            "slot0 reduces HP 15->5 and survives");
        Require(r1.Outcome == PlatformProjectileHitOutcome.HpKilled,
            "slot1 kills from stored HP5");
        Require(r2.Outcome == PlatformProjectileHitOutcome.HpKilled,
            "slot2 re-enters death path because kill did not rewrite HP byte");
        Require(sequence.SeventhSense == 20,
            "two kill-path entries each award packed-BCD +10 Seventh Sense");

        Require(sequence.AttackState.Slot0.Object.Type == 0xFE
            && sequence.AttackState.Slot1.Object.Type == 0xFE
            && sequence.AttackState.Slot2.Object.Type == 0xFE,
            "Shiryu standard helper consumes every overlapping slot independently");
    }

    private static void CheckIkkiRepeatedRewardAfterImmediateKill()
    {
        var attacks = PlatformAttackState.Empty with
        {
            Slot0 = Active(0x40, 0x50),
            Slot1 = Active(0x40, 0x50),
        };
        var entity = Enemy(type: 5, hp: 10, reward: 0x10);

        var sequence = PlatformProjectileHitSequence.ResolveThreeSlots(
            attacks,
            entity,
            PlatformSaintIndex.Ikki,
            PlatformHitboxParameters.Common,
            platformDamage: 10,
            seventhSense: 100,
            engineSubstate02: 1);

        Require(sequence.Results[0].Result.Outcome == PlatformProjectileHitOutcome.HpKilled,
            "Ikki slot0 immediately kills");
        Require(sequence.Results[1].Result.Outcome == PlatformProjectileHitOutcome.HpKilled,
            "Ikki slot1 still sees unchanged HP and kills again");
        Require(sequence.SeventhSense == 120,
            "same-frame repeated death path duplicates reward exactly as slot sequence implies");
        Require(sequence.AttackState.Slot0.Object.Type == 0x64
            && sequence.AttackState.Slot1.Object.Type == 0x64,
            "Ikki attacks survive standard consumption helper");
    }

    private static void CheckReactionMutationAffectsLaterOverlap()
    {
        var attacks = PlatformAttackState.Empty with
        {
            Slot0 = Active(0x40, 0x50),
            Slot1 = Active(0x40, 0x50),
        };
        var entity = Enemy(type: 1, hp: 30, reward: 0);

        var sequence = PlatformProjectileHitSequence.ResolveThreeSlots(
            attacks,
            entity,
            PlatformSaintIndex.Seiya,
            PlatformHitboxParameters.Reduced,
            platformDamage: 10,
            seventhSense: 0,
            engineSubstate02: 1);

        Require(sequence.Results[0].Result.Outcome == PlatformProjectileHitOutcome.DropReaction,
            "first type1 overlap enters E0/Y+6 reaction");
        Require(sequence.Entity.Y == 0x46,
            "entity mutation from first slot is retained for later slots");

        // The second slot is evaluated against the Y-shifted entity. We do not
        // short-circuit merely because state became E0.
        Require(sequence.Results.Count == 3,
            "$9915 always evaluates all three attack slots in order");
    }

    private static PlatformAttackState ThreeActiveSlots() => PlatformAttackState.Empty with
    {
        Slot0 = Active(0x40, 0x50),
        Slot1 = Active(0x40, 0x50),
        Slot2 = Active(0x40, 0x50),
    };

    private static PlatformAttackSlot Active(byte y, byte x) => new(
        new PlatformAttackObject(y, 0x64, 0x40, x, 0, 0, 0, 0),
        10);

    private static PlatformCombatEntity Enemy(byte type, byte hp, byte reward) => new(
        State: 0x10,
        X: 0x50,
        Y: 0x40,
        Type: type,
        HitPoints: hp,
        SeventhSenseRewardBcd: reward);

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException($"ProjectileHitSequence self-test failed: {message}");
    }
}

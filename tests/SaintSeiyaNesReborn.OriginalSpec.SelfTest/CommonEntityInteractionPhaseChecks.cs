using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class CommonEntityInteractionPhaseChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckHitMutationCanPreventLaterContact();
        CheckNoHitAllowsContact();
    }

    private static void CheckHitMutationCanPreventLaterContact()
    {
        // Type 1 takes the observed drop-reaction path on projectile overlap:
        // entity Y += 6, state -> $E0. With entityY=$50 and playerY=$37,
        // contact would succeed before the mutation because lower=$34 < $37,
        // but after the hit lower becomes $3A and the same contact must miss.
        var attacks = PlatformAttackState.Empty with
        {
            Slot0 = ActiveAttack(y: 0x50, x: 0x50),
        };
        var entity = new PlatformCombatEntity(
            State: 0x10,
            X: 0x50,
            Y: 0x50,
            Type: 0x01,
            HitPoints: 30,
            SeventhSenseRewardBcd: 0);
        var contactState = new PlatformContactPhaseState(0, new ContactDrainState(0, 0));

        var beforeHitContact = PlatformEntityContact.EvaluateOrdinary(
            frameStartAction4E: 0,
            playerX: 0x50,
            playerY: 0x37,
            entityX: entity.X,
            entityY: entity.Y,
            currentHazardLatch76: 0,
            entityLifeDrainTicks: 2,
            entityCosmoDrainTicks: 3);
        Require(beforeHitContact.Triggered,
            "fixture proves original entity position would contact player");

        var result = PlatformCommonEntityInteractionPhases.ResolveProjectileHitThenContact(
            attacks,
            entity,
            PlatformSaintIndex.Seiya,
            PlatformHitboxParameters.Common,
            platformDamage: 10,
            seventhSense: 0,
            engineSubstate02: 1,
            contactState,
            frameStartAction4E: 0,
            playerX: 0x50,
            playerY: 0x37,
            entityLifeDrainTicks: 2,
            entityCosmoDrainTicks: 3);

        Require(result.HitSequence.Results[0].Result.Outcome == PlatformProjectileHitOutcome.DropReaction,
            "projectile phase applies type1 drop reaction first");
        Require(result.Entity.Y == 0x56 && result.Entity.State == 0xE0,
            "hit phase mutates entity Y/state before contact");
        Require(result.ContactPhase.Contact.Outcome == PlatformEntityContactOutcome.OutsideVerticalWindow,
            "later $98BA contact uses post-hit entity Y and now misses");
        Require(result.ContactPhase.State.HazardLatch76 == 0,
            "missed post-hit contact does not seed latch");
        Require(result.ContactPhase.State.DrainState == new ContactDrainState(0, 0),
            "missed post-hit contact does not seed drain counters");
    }

    private static void CheckNoHitAllowsContact()
    {
        var attacks = PlatformAttackState.Empty;
        var entity = new PlatformCombatEntity(0x10, 0x50, 0x50, 0x01, 30, 0);
        var result = PlatformCommonEntityInteractionPhases.ResolveProjectileHitThenContact(
            attacks,
            entity,
            PlatformSaintIndex.Seiya,
            PlatformHitboxParameters.Common,
            platformDamage: 10,
            seventhSense: 0,
            engineSubstate02: 1,
            new PlatformContactPhaseState(0, new ContactDrainState(0, 0)),
            frameStartAction4E: 0,
            playerX: 0x50,
            playerY: 0x37,
            entityLifeDrainTicks: 2,
            entityCosmoDrainTicks: 3);

        Require(result.HitSequence.Results.All(x => x.Result.Outcome == PlatformProjectileHitOutcome.NoOverlap),
            "no active projectile leaves entity unmodified");
        Require(result.Entity.Y == 0x50,
            "no-hit control preserves original entity Y");
        Require(result.ContactPhase.Contact.Triggered,
            "without prior hit mutation the same geometry contacts player");
        Require(result.ContactPhase.State.HazardLatch76 == 0x20,
            "contact seeds ordinary 32-frame latch");
        Require(result.ContactPhase.State.DrainState == new ContactDrainState(2, 3),
            "contact copies entity Life/Cosmo drain counters");
    }

    private static PlatformAttackSlot ActiveAttack(byte y, byte x) => new(
        new PlatformAttackObject(y, 0x64, 0x40, x, 0, 0, 0, 0),
        10);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Common entity interaction phase self-test failed: {label}");
    }
}

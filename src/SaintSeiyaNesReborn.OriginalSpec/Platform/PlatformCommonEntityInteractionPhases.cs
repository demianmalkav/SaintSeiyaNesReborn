namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public sealed record PlatformCommonEntityInteractionResult(
    PlatformAttackState AttackState,
    PlatformCombatEntity Entity,
    int SeventhSense,
    PlatformProjectileHitSequenceResult HitSequence,
    PlatformContactAfterEntityResult ContactPhase);

/// <summary>
/// Composes the confirmed interaction order inside the common-entity update:
/// projectile-vs-entity resolution ($9915/$992A) precedes the immediately
/// following ordinary entity-vs-player contact test ($98BA).
///
/// The contact phase therefore observes entity coordinates/state mutations made
/// by the projectile hit phase on the same frame.
/// </summary>
public static class PlatformCommonEntityInteractionPhases
{
    public static PlatformCommonEntityInteractionResult ResolveProjectileHitThenContact(
        PlatformAttackState attacks,
        PlatformCombatEntity entity,
        PlatformSaintIndex saint,
        PlatformHitboxParameters hitbox,
        int platformDamage,
        int seventhSense,
        byte engineSubstate02,
        PlatformContactPhaseState contactState,
        byte frameStartAction4E,
        byte playerX,
        byte playerY,
        byte entityLifeDrainTicks,
        byte entityCosmoDrainTicks)
    {
        var hits = PlatformProjectileHitSequence.ResolveThreeSlots(
            attacks,
            entity,
            saint,
            hitbox,
            platformDamage,
            seventhSense,
            engineSubstate02);

        var contact = PlatformContactFramePhases.ApplyOrdinaryEntityContact(
            contactState,
            frameStartAction4E,
            playerX,
            playerY,
            hits.Entity.X,
            hits.Entity.Y,
            entityLifeDrainTicks,
            entityCosmoDrainTicks);

        return new PlatformCommonEntityInteractionResult(
            hits.AttackState,
            hits.Entity,
            hits.SeventhSense,
            hits,
            contact);
    }
}

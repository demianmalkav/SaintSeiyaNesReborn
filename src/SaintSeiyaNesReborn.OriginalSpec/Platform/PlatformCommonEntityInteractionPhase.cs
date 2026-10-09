namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public sealed record PlatformCommonEntityInteractionResult(
    PlatformPlayerActionDispatchResult Player,
    PlatformCombatEntity Entity,
    ContactDrainState DrainState,
    int SeventhSense,
    PlatformProjectileHitSequenceResult? ProjectileHits,
    PlatformEntityContactResult? PlayerContact,
    bool SkippedBecausePlayerLoopExited);

/// <summary>
/// Composes the confirmed interaction order inside the ordinary/common entity
/// collision block reached from the $A442 pipeline.
///
/// At $A719 the original performs player-projectile -> entity resolution first
/// through $9915, then ordinary entity -> player contact through $98BA. There is
/// no new high-level entity-state gate between those calls. Consequently entity
/// coordinate/state mutations caused by projectile hits are visible to the later
/// player-contact test in the same update.
///
/// This helper models one entity's interaction block only. The caller may process
/// multiple entity records sequentially and must run $A22C attack-object motion
/// only after the complete entity pipeline, matching fixed $C2D7 order.
/// </summary>
public static class PlatformCommonEntityInteractionPhase
{
    public static PlatformCommonEntityInteractionResult ResolveOne(
        PlatformPlayerActionDispatchResult player,
        PlatformCombatEntity entity,
        ContactDrainState drainState,
        PlatformHitboxParameters projectileHitbox,
        int platformDamage72,
        int seventhSense,
        byte engineSubstate02,
        byte entityLifeDrainTicks,
        byte entityCosmoDrainTicks)
    {
        if (player.ExitsNormalPlayerLoop)
        {
            return new PlatformCommonEntityInteractionResult(
                player,
                entity,
                drainState,
                seventhSense,
                ProjectileHits: null,
                PlayerContact: null,
                SkippedBecausePlayerLoopExited: true);
        }

        // $A719 -> $9915. Attack slots are tested before $98BA and before $A22C
        // later advances/animates the attack objects.
        var hits = PlatformProjectileHitSequence.ResolveThreeSlots(
            player.State.AttackState,
            entity,
            player.State.Saint,
            projectileHitbox,
            platformDamage72,
            seventhSense,
            engineSubstate02);

        var afterHits = player with
        {
            State = player.State with { AttackState = hits.AttackState },
        };

        // $A719 next calls $98BA unconditionally from this interaction block.
        // Use entity coordinates after any $9915 mutation and the current $76
        // value that survived player/B94B phases.
        var contact = PlatformContactFramePhases.ApplyOrdinaryEntityContact(
            new PlatformContactPhaseState(afterHits.State.Special76, drainState),
            afterHits.FrameStartAction4E,
            afterHits.State.Horizontal.PlayerX,
            afterHits.State.PlayerY,
            hits.Entity.X,
            hits.Entity.Y,
            entityLifeDrainTicks,
            entityCosmoDrainTicks);

        var afterContact = afterHits with
        {
            State = afterHits.State with { Special76 = contact.State.HazardLatch76 },
        };

        return new PlatformCommonEntityInteractionResult(
            afterContact,
            hits.Entity,
            contact.State.DrainState,
            hits.SeventhSense,
            hits,
            contact.Contact,
            SkippedBecausePlayerLoopExited: false);
    }
}

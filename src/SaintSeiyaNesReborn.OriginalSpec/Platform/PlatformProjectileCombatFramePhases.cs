namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public sealed record PlatformProjectileCombatFrameResult(
    PlatformPlayerActionDispatchResult PlayerAfterHitPhase,
    PlatformCombatEntity Entity,
    int SeventhSense,
    PlatformProjectileHitSequenceResult? HitSequence,
    PlatformAttackObjectPhaseResult AttackObjectPhase,
    bool SkippedBecausePlayerLoopExited);

/// <summary>
/// Composes the confirmed ordering inside the late active-platform frame:
/// common entity processing reaches projectile collision ($A442 -> $9915/$992A)
/// before the later player-attack updater ($A22C) moves/ages those projectiles.
///
/// This helper deliberately models only that ordering boundary. It does not
/// claim to replace the rest of $A442 entity AI/render/contact work.
/// </summary>
public static class PlatformProjectileCombatFramePhases
{
    public static PlatformProjectileCombatFrameResult ResolveHitsThenAdvanceAttackObjects(
        PlatformPlayerActionDispatchResult player,
        PlatformCombatEntity entity,
        PlatformHitboxParameters hitbox,
        int platformDamage,
        int seventhSense,
        byte engineSubstate02,
        byte frameCounter3C)
    {
        // The $80 reload path leaves the normal player/platform call chain before
        // either A442 or A22C can run.
        if (player.ExitsNormalPlayerLoop)
        {
            var skippedAttackPhase = PlatformAttackFramePhases.UpdateObjectsAfterPlayer(
                player,
                frameCounter3C);
            return new PlatformProjectileCombatFrameResult(
                player,
                entity,
                seventhSense,
                null,
                skippedAttackPhase,
                SkippedBecausePlayerLoopExited: true);
        }

        // $A442 reaches the projectile-vs-entity path while the attack object is
        // still at the coordinates established by BBCA (or by the prior frame).
        var hitSequence = PlatformProjectileHitSequence.ResolveThreeSlots(
            player.State.AttackState,
            entity,
            player.State.Saint,
            hitbox,
            platformDamage,
            seventhSense,
            engineSubstate02);

        var afterHits = player with
        {
            State = player.State with { AttackState = hitSequence.AttackState },
        };

        // Only after entity collision resolution does C2D7 reach A22C. The same
        // current-frame $3C value is still in force here.
        var attackPhase = PlatformAttackFramePhases.UpdateObjectsAfterPlayer(
            afterHits,
            frameCounter3C);

        return new PlatformProjectileCombatFrameResult(
            attackPhase.Player,
            hitSequence.Entity,
            hitSequence.SeventhSense,
            hitSequence,
            attackPhase,
            SkippedBecausePlayerLoopExited: false);
    }
}

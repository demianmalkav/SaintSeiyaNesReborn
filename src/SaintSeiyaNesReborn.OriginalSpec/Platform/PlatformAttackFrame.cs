using SaintSeiyaNesReborn.OriginalSpec;

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformHitTarget(
    PlatformCombatEntity Entity,
    PlatformHitboxParameters Hitbox);

public sealed record PlatformAttackFrameResult(
    PlatformAttackState AttackState,
    IReadOnlyList<PlatformHitTarget> Targets,
    IReadOnlyList<PlatformProjectileHitSequenceResult> HitSequences,
    PlatformAttackAttemptResult Attempt,
    int PlatformDamage,
    int SeventhSense);

/// <summary>
/// Attack-related slice of one active platform frame. The ordering follows the
/// fixed-bank loop around $C319-$C330: bank1 support/busy -> damage -> player
/// attack creation -> entity hit checks -> attack-object update.
/// </summary>
public static class PlatformAttackFrame
{
    public static PlatformAttackFrameResult Step(
        PlatformAttackState attackState,
        PlatformSaintIndex saint,
        PlatformInput input,
        byte playerY,
        byte playerX,
        byte facing42,
        byte frameStartAction4E,
        byte jumpPhase49,
        byte frameCounter3C,
        int cosmo,
        byte engineSubstate01,
        byte engineSubstate02,
        int seventhSense,
        IReadOnlyList<PlatformHitTarget> orderedTargets)
    {
        // Bank 1 $8000 calls $926C before fixed $C52F computes $72 and before
        // bank 3 $AAE4 can enter $BBCA.
        attackState = attackState with
        {
            Busy4B = PlatformAttackSystem.AdvanceBusy(attackState.Busy4B),
        };

        var damage = PlatformDamage.FromCosmo(saint, cosmo);

        // Player simulation can create a new attack before any of the bank-3
        // entity update paths execute.
        var attempt = PlatformAttackSystem.ApplyBButton(
            attackState,
            saint,
            input,
            playerY,
            playerX,
            facing42,
            frameStartAction4E,
            jumpPhase49,
            cosmo,
            engineSubstate01);
        attackState = attempt.State;

        // C2D7 updates secondary/special/common entities before A22C updates
        // the attack objects themselves. The caller provides targets in that
        // exact engine order once each object's hitbox identity is known.
        var targets = orderedTargets.ToArray();
        var sequences = new List<PlatformProjectileHitSequenceResult>(targets.Length);
        for (var i = 0; i < targets.Length; i++)
        {
            var target = targets[i];
            var sequence = PlatformProjectileHitSequence.ResolveThreeSlots(
                attackState,
                target.Entity,
                saint,
                target.Hitbox,
                damage,
                seventhSense,
                engineSubstate02);

            attackState = sequence.AttackState;
            seventhSense = sequence.SeventhSense;
            targets[i] = target with { Entity = sequence.Entity };
            sequences.Add(sequence);
        }

        // Only after all entity hit checks does A22C decrement lifetime and move
        // generic attacks, or synthesize/update Shun's second chain segment.
        attackState = PlatformAttackSystem.UpdateAttackObjects(
            attackState,
            saint,
            frameCounter3C,
            playerY,
            playerX,
            frameStartAction4E);

        return new PlatformAttackFrameResult(
            attackState,
            targets,
            sequences,
            attempt,
            damage,
            seventhSense);
    }
}

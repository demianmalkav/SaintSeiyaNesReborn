using SaintSeiyaNesReborn.OriginalSpec;

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public sealed record PlatformCommonEntitySlotRuntimeResult(
    PlatformCommonEntitySlotFrameResult Slot,
    PlatformAttackState AttackState,
    PlatformContactPhaseState ContactState,
    int SeventhSense);

/// <summary>
/// Reusable ordered update for one already-admitted common logical entity slot
/// after the player/post-player phases and before the later attack-object update.
///
/// This is the exact per-slot body previously embedded in
/// PlatformTwoCommonEntityCombatSlice. Extracting it creates one canonical path
/// for common slot semantics so a higher hybrid scheduler can interleave common
/// and special slots without copying $A442 behavior.
/// </summary>
public static class PlatformCommonEntitySlotRuntime
{
    public static PlatformCommonEntitySlotRuntimeResult Step(
        PlatformCommonEntityRuntimeState entity,
        PlatformAttackState attacks,
        PlatformSaintIndex saint,
        byte frameStartAction4E,
        byte playerX,
        byte playerY,
        byte playerJumpPhase49,
        PlatformContactPhaseState contactState,
        PlatformHitboxParameters hitbox,
        int platformDamage,
        int seventhSense,
        byte frameCounter3C,
        byte entropy48,
        byte cameraDelta43,
        byte engineSubstate02)
    {
        ValidateEntity(entity);

        var dispatch = PlatformCommonEntityActiveDispatcher.Step(
            entity.Motion,
            playerX,
            playerY,
            playerJumpPhase49,
            entropy48,
            frameCounter3C,
            cameraDelta43);
        entity = entity with { Motion = dispatch.State };

        if (dispatch.Continuation == PlatformCommonEntityActiveContinuation.Removed)
        {
            return FinishWithoutInteraction(
                entity,
                dispatch,
                attacks,
                contactState,
                seventhSense,
                removedBefore: true);
        }

        if (dispatch.Continuation == PlatformCommonEntityActiveContinuation.SkipInteraction)
        {
            return FinishWithoutInteraction(
                entity,
                dispatch,
                attacks,
                contactState,
                seventhSense,
                removedBefore: false);
        }

        var interaction = PlatformCommonEntityInteractionPhases.ResolveProjectileHitThenContact(
            attacks,
            entity.ToCombatEntity(),
            saint,
            hitbox,
            platformDamage,
            seventhSense,
            engineSubstate02,
            contactState,
            frameStartAction4E,
            playerX,
            playerY,
            entity.LifeDrainTicks,
            entity.CosmoDrainTicks);

        entity = entity.WithCombatEntity(interaction.Entity);
        attacks = interaction.AttackState;
        contactState = interaction.ContactPhase.State;
        seventhSense = interaction.SeventhSense;

        PlatformCommonEntityHitReaction40Result? hitReaction40Post = null;
        PlatformCommonEntityDeathD0Result? deathD0Post = null;
        var removedAfterInteraction = false;

        if (entity.Motion.Type <= 0x07 && (entity.Motion.ActionState & 0xF0) == 0x40)
        {
            hitReaction40Post = PlatformCommonEntityHitReaction40.AdvanceAfterPath(entity.Motion);
            entity = entity with { Motion = hitReaction40Post.Value.State };
        }

        if (entity.Motion.Type <= 0x07 && (entity.Motion.ActionState & 0xF0) == 0xD0)
        {
            deathD0Post = PlatformCommonEntityDeathD0.AdvanceAfterPath(
                entity.Motion,
                frameCounter3C);
            entity = entity with { Motion = deathD0Post.Value.State };
            removedAfterInteraction = deathD0Post.Value.Outcome ==
                PlatformCommonEntityDeathD0Outcome.CompletedRemoval;
        }

        PlatformCommonEntityAttack70PostResult? attack70Post = null;
        if (dispatch.Route == PlatformCommonEntityActiveRoute.Attack70)
        {
            attack70Post = PlatformCommonEntityAttack70.AdvanceAfterInteraction(
                entity.Motion,
                frameCounter3C);
            entity = entity with { Motion = attack70Post.Value.State };
        }

        PlatformEntityMotion3KnockbackResult? motion3KnockbackPost = null;
        if (entity.Motion.Type is 0x0A or 0x0B
            && dispatch.Route == PlatformCommonEntityActiveRoute.Ordinary10)
        {
            motion3KnockbackPost = PlatformEntityMotion3Knockback.Step(entity.Motion);
            entity = entity with { Motion = motion3KnockbackPost.Value.State };
        }

        var slot = new PlatformCommonEntitySlotFrameResult(
            entity,
            dispatch,
            interaction,
            hitReaction40Post,
            deathD0Post,
            attack70Post,
            motion3KnockbackPost,
            RemovedBeforeInteraction: false,
            RemovedAfterInteraction: removedAfterInteraction);

        return new(slot, attacks, contactState, seventhSense);
    }

    public static bool IsSupported(PlatformCommonEntityRuntimeState entity)
    {
        var type = entity.Motion.Type;
        var family = entity.Motion.ActionState & 0xF0;

        if (type <= 0x07)
            return family is 0x10 or 0x30 or 0x40 or 0x50 or 0x70 or 0xD0 or 0xE0;

        if (type is 0x0A or 0x0B)
            return family is 0x10 or 0x50;

        return false;
    }

    public static void ValidateEntity(PlatformCommonEntityRuntimeState entity)
    {
        if (IsSupported(entity))
            return;

        var type = entity.Motion.Type;
        if (type <= 0x07)
        {
            throw new InvalidOperationException(
                $"Action ${entity.Motion.ActionState:X2} is outside the closed type $00-$07 families.");
        }

        if (type is 0x0A or 0x0B)
        {
            throw new InvalidOperationException(
                $"Type ${type:X2} is currently closed only for action families $10/$50; got ${entity.Motion.ActionState:X2}.");
        }

        throw new InvalidOperationException(
            $"Type ${type:X2} is outside the currently promoted common entity set.");
    }

    private static PlatformCommonEntitySlotRuntimeResult FinishWithoutInteraction(
        PlatformCommonEntityRuntimeState entity,
        PlatformCommonEntityActiveDispatchResult dispatch,
        PlatformAttackState attacks,
        PlatformContactPhaseState contactState,
        int seventhSense,
        bool removedBefore)
    {
        var slot = new PlatformCommonEntitySlotFrameResult(
            entity,
            dispatch,
            null,
            null,
            null,
            null,
            null,
            RemovedBeforeInteraction: removedBefore,
            RemovedAfterInteraction: false);
        return new(slot, attacks, contactState, seventhSense);
    }
}

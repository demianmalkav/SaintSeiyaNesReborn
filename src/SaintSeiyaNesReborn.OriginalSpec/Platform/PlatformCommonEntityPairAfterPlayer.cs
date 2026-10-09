namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public sealed record PlatformCommonEntityPairAfterPlayerResult(
    PlatformCommonEntitySlotFrameResult SlotA,
    PlatformCommonEntitySlotFrameResult SlotB,
    PlatformPlayerActionDispatchResult Player,
    PlatformContactPhaseState ContactState,
    int SeventhSense);

/// <summary>
/// Reusable `$A442` two-record phase after player/post-player work has already
/// completed and before the later `$A22C` attack-object update.
///
/// This is intentionally separated from `PlatformTwoCommonEntityCombatSlice`
/// so earlier `$96B4/$9761` auxiliary hazards can mutate the same attack objects
/// and `$76/$7F/$80` state before the common records run.
/// </summary>
public static class PlatformCommonEntityPairAfterPlayer
{
    public static PlatformCommonEntityPairAfterPlayerResult Step(
        PlatformPlayerActionDispatchResult player,
        PlatformContactPhaseState contactState,
        PlatformCommonEntityRuntimeState entityA,
        PlatformCommonEntityRuntimeState entityB,
        PlatformHitboxParameters hitboxA,
        PlatformHitboxParameters hitboxB,
        int platformDamage,
        int seventhSense,
        byte frameCounter3C,
        byte entropy48,
        byte cameraDelta43,
        byte engineSubstate02)
    {
        ValidateEntity(entityA, nameof(entityA));
        ValidateEntity(entityB, nameof(entityB));

        if (player.State.Special76 != contactState.HazardLatch76)
        {
            throw new InvalidOperationException(
                $"Player Special76 (${player.State.Special76:X2}) and contact HazardLatch76 (${contactState.HazardLatch76:X2}) must match before A442.");
        }

        var slotA = ProcessSlot(
            entityA,
            player,
            contactState,
            hitboxA,
            platformDamage,
            seventhSense,
            frameCounter3C,
            entropy48,
            cameraDelta43,
            engineSubstate02);
        player = slotA.Player;
        contactState = slotA.ContactState;
        seventhSense = slotA.SeventhSense;

        var slotB = ProcessSlot(
            entityB,
            player,
            contactState,
            hitboxB,
            platformDamage,
            seventhSense,
            frameCounter3C,
            entropy48,
            cameraDelta43,
            engineSubstate02);

        return new PlatformCommonEntityPairAfterPlayerResult(
            slotA.Result,
            slotB.Result,
            slotB.Player,
            slotB.ContactState,
            slotB.SeventhSense);
    }

    private sealed record SlotCarry(
        PlatformCommonEntitySlotFrameResult Result,
        PlatformPlayerActionDispatchResult Player,
        PlatformContactPhaseState ContactState,
        int SeventhSense);

    private static SlotCarry ProcessSlot(
        PlatformCommonEntityRuntimeState entity,
        PlatformPlayerActionDispatchResult player,
        PlatformContactPhaseState contactState,
        PlatformHitboxParameters hitbox,
        int platformDamage,
        int seventhSense,
        byte frameCounter3C,
        byte entropy48,
        byte cameraDelta43,
        byte engineSubstate02)
    {
        var dispatch = PlatformCommonEntityActiveDispatcher.Step(
            entity.Motion,
            player.State.Horizontal.PlayerX,
            player.State.PlayerY,
            player.State.JumpPhase49,
            entropy48,
            frameCounter3C,
            cameraDelta43);
        entity = entity with { Motion = dispatch.State };

        if (dispatch.Continuation == PlatformCommonEntityActiveContinuation.Removed)
        {
            return new SlotCarry(
                EmptyInteractionResult(entity, dispatch, removedBefore: true),
                player,
                contactState,
                seventhSense);
        }

        if (dispatch.Continuation == PlatformCommonEntityActiveContinuation.SkipInteraction)
        {
            return new SlotCarry(
                EmptyInteractionResult(entity, dispatch, removedBefore: false),
                player,
                contactState,
                seventhSense);
        }

        var interaction = PlatformCommonEntityInteractionPhases.ResolveProjectileHitThenContact(
            player.State.AttackState,
            entity.ToCombatEntity(),
            player.State.Saint,
            hitbox,
            platformDamage,
            seventhSense,
            engineSubstate02,
            contactState,
            player.FrameStartAction4E,
            player.State.Horizontal.PlayerX,
            player.State.PlayerY,
            entity.LifeDrainTicks,
            entity.CosmoDrainTicks);

        entity = entity.WithCombatEntity(interaction.Entity);
        player = player with
        {
            State = player.State with
            {
                AttackState = interaction.AttackState,
                Special76 = interaction.ContactPhase.State.HazardLatch76,
            },
        };

        PlatformCommonEntityHitReaction40Result? hitReaction40Post = null;
        PlatformCommonEntityDeathD0Result? deathD0Post = null;
        var removedAfterInteraction = false;

        // Direct fall-through after $9915/$98BA reaches $A79E and then $A7FB.
        // A just-created $40/$D0 therefore receives its first phase in this same
        // frame, but must not receive the earlier frame-start camera path twice.
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
            removedAfterInteraction = deathD0Post.Value.Outcome == PlatformCommonEntityDeathD0Outcome.CompletedRemoval;
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

        return new SlotCarry(
            new PlatformCommonEntitySlotFrameResult(
                entity,
                dispatch,
                interaction,
                hitReaction40Post,
                deathD0Post,
                attack70Post,
                motion3KnockbackPost,
                RemovedBeforeInteraction: false,
                RemovedAfterInteraction: removedAfterInteraction),
            player,
            interaction.ContactPhase.State,
            interaction.SeventhSense);
    }

    private static PlatformCommonEntitySlotFrameResult EmptyInteractionResult(
        PlatformCommonEntityRuntimeState entity,
        PlatformCommonEntityActiveDispatchResult dispatch,
        bool removedBefore) =>
        new(
            entity,
            dispatch,
            null,
            null,
            null,
            null,
            null,
            RemovedBeforeInteraction: removedBefore,
            RemovedAfterInteraction: false);

    private static void ValidateEntity(PlatformCommonEntityRuntimeState entity, string name)
    {
        var type = entity.Motion.Type;
        var family = entity.Motion.ActionState & 0xF0;

        if (type <= 0x07)
        {
            if (family is not (0x10 or 0x30 or 0x40 or 0x50 or 0x70 or 0xD0 or 0xE0))
            {
                throw new InvalidOperationException(
                    $"{name} action ${entity.Motion.ActionState:X2} is outside the closed type $00-$07 families.");
            }
            return;
        }

        if (type is 0x0A or 0x0B)
        {
            if (family is not (0x10 or 0x50))
            {
                throw new InvalidOperationException(
                    $"{name} type ${type:X2} is currently closed only for action families $10/$50; got ${entity.Motion.ActionState:X2}.");
            }
            return;
        }

        throw new InvalidOperationException(
            $"{name} type ${type:X2} is outside the currently composed two-slot entity set.");
    }
}

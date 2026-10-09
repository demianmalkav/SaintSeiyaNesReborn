namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public sealed record PlatformCommonEntitySlotFrameResult(
    PlatformCommonEntityRuntimeState Entity,
    PlatformCommonEntityActiveDispatchResult Dispatch,
    PlatformCommonEntityInteractionResult? Interaction,
    PlatformCommonEntityHitReaction40Result? HitReaction40Post,
    PlatformCommonEntityDeathD0Result? DeathD0Post,
    PlatformEntity08090CPostResult? Special08090CPost,
    PlatformCommonEntityAttack70PostResult? Attack70Post,
    PlatformEntityMotion3KnockbackResult? Motion3KnockbackPost,
    bool RemovedBeforeInteraction,
    bool RemovedAfterInteraction);

public sealed record PlatformTwoCommonEntityCombatSliceResult(
    PlatformPrePlayerResourcePhaseResult PrePlayer,
    PlatformPostPlayerLatchResult PostPlayerLatch,
    PlatformCommonEntitySlotFrameResult? SlotA,
    PlatformCommonEntitySlotFrameResult? SlotB,
    PlatformAttackObjectPhaseResult AttackObjectPhase,
    PlatformPlayerActionDispatchResult PlayerAfterLatePhases,
    PlatformContactPhaseState ContactState,
    int SeventhSense,
    byte FrameCounterBefore3C,
    byte FrameCounterAfter3C,
    bool FrameCounterAdvanced,
    bool ExitedBeforeEntityPipeline);

/// <summary>
/// Ordered clean-room slice for the two movable-entity records processed by
/// $A442. Slot A ($03BA-$03C9) runs before slot B ($03CA-$03D9).
///
/// Supported sets:
/// - types $00-$07: common promoted families;
/// - types $08/$09/$0C: basic post-trigger `$00/$40/$50/$D0` paths;
/// - types $0A/$0B: `$10/$50` plus same-frame $A845 motion3 handling.
///
/// The late ordering after an interaction remains literal: $A79E reaction,
/// type-$0C bob at $A7D0, $A7FB death, type-$0A/$0B $A845, then $A886 `$70`.
/// </summary>
public static class PlatformTwoCommonEntityCombatSlice
{
    public static PlatformTwoCommonEntityCombatSliceResult StepNonFatal(
        PlatformStageMap stage,
        PlatformPlayerActionState playerState,
        PlatformInput input,
        PlatformFrameResources resources,
        PlatformContactPhaseState contactState,
        PlatformCommonEntityRuntimeState entityA,
        PlatformCommonEntityRuntimeState entityB,
        PlatformHitboxParameters hitboxA,
        PlatformHitboxParameters hitboxB,
        int seventhSense,
        byte frameCounter3C,
        byte entropy48,
        byte cameraDelta43,
        byte engineSubstate02,
        byte engineSubstate01 = 0,
        byte dynamicFloorY039B = 0)
    {
        ValidateEntry(playerState, contactState, entityA, nameof(entityA));
        ValidateEntity(entityB, nameof(entityB));

        var pre = PlatformPrePlayerResourcePhases.StepNonFatal(
            stage,
            playerState,
            input,
            resources,
            contactState,
            frameCounter3C,
            engineSubstate02,
            engineSubstate01,
            dynamicFloorY039B);

        var latch = PlatformPostPlayerLatch.Step(
            pre.Player.State.Special76,
            pre.Player.ExitsNormalPlayerLoop);
        var currentPlayer = PlatformPostPlayerLatch.Apply(pre.Player);
        var currentContact = pre.ContactStateAfterDrain with
        {
            HazardLatch76 = latch.After76,
        };

        if (currentPlayer.ExitsNormalPlayerLoop)
        {
            var skippedAttack = PlatformAttackFramePhases.UpdateObjectsAfterPlayer(
                currentPlayer,
                frameCounter3C);
            return new PlatformTwoCommonEntityCombatSliceResult(
                pre,
                latch,
                null,
                null,
                skippedAttack,
                skippedAttack.Player,
                currentContact,
                seventhSense,
                frameCounter3C,
                frameCounter3C,
                FrameCounterAdvanced: false,
                ExitedBeforeEntityPipeline: true);
        }

        var slotA = ProcessSlot(
            entityA, currentPlayer, currentContact, hitboxA, pre.PlatformDamage,
            seventhSense, frameCounter3C, entropy48, cameraDelta43, engineSubstate02);
        currentPlayer = slotA.Player;
        currentContact = slotA.ContactState;
        seventhSense = slotA.SeventhSense;

        var slotB = ProcessSlot(
            entityB, currentPlayer, currentContact, hitboxB, pre.PlatformDamage,
            seventhSense, frameCounter3C, entropy48, cameraDelta43, engineSubstate02);
        currentPlayer = slotB.Player;
        currentContact = slotB.ContactState;
        seventhSense = slotB.SeventhSense;

        var attackPhase = PlatformAttackFramePhases.UpdateObjectsAfterPlayer(
            currentPlayer,
            frameCounter3C);
        var nextCounter = unchecked((byte)(frameCounter3C + 1));

        return new PlatformTwoCommonEntityCombatSliceResult(
            pre,
            latch,
            slotA.Result,
            slotB.Result,
            attackPhase,
            attackPhase.Player,
            currentContact,
            seventhSense,
            frameCounter3C,
            nextCounter,
            FrameCounterAdvanced: true,
            ExitedBeforeEntityPipeline: false);
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
            return CarryWithoutInteraction(
                entity, dispatch, player, contactState, seventhSense,
                removedBefore: true);
        }

        if (dispatch.Continuation == PlatformCommonEntityActiveContinuation.SkipInteraction)
        {
            return CarryWithoutInteraction(
                entity, dispatch, player, contactState, seventhSense,
                removedBefore: false);
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
        PlatformEntity08090CPostResult? special08090CPost = null;
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
            removedAfterInteraction = deathD0Post.Value.Outcome == PlatformCommonEntityDeathD0Outcome.CompletedRemoval;
        }

        if (dispatch.Route == PlatformCommonEntityActiveRoute.Special08090C)
        {
            special08090CPost = PlatformEntityTypes08090C.AdvanceAfterInteraction(
                entity.Motion,
                frameCounter3C);
            entity = entity with { Motion = special08090CPost.Value.State };
            removedAfterInteraction |= special08090CPost.Value.Removed;
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
                special08090CPost,
                attack70Post,
                motion3KnockbackPost,
                RemovedBeforeInteraction: false,
                RemovedAfterInteraction: removedAfterInteraction),
            player,
            interaction.ContactPhase.State,
            interaction.SeventhSense);
    }

    private static SlotCarry CarryWithoutInteraction(
        PlatformCommonEntityRuntimeState entity,
        PlatformCommonEntityActiveDispatchResult dispatch,
        PlatformPlayerActionDispatchResult player,
        PlatformContactPhaseState contactState,
        int seventhSense,
        bool removedBefore)
    {
        return new SlotCarry(
            new PlatformCommonEntitySlotFrameResult(
                entity,
                dispatch,
                null,
                null,
                null,
                null,
                null,
                null,
                RemovedBeforeInteraction: removedBefore,
                RemovedAfterInteraction: false),
            player,
            contactState,
            seventhSense);
    }

    private static void ValidateEntry(
        PlatformPlayerActionState player,
        PlatformContactPhaseState contact,
        PlatformCommonEntityRuntimeState entity,
        string entityName)
    {
        if (player.Special76 != contact.HazardLatch76)
        {
            throw new InvalidOperationException(
                $"Player Special76 (${player.Special76:X2}) and contact HazardLatch76 (${contact.HazardLatch76:X2}) must match at frame entry.");
        }
        ValidateEntity(entity, entityName);
    }

    private static void ValidateEntity(PlatformCommonEntityRuntimeState entity, string name)
    {
        var type = entity.Motion.Type;
        var family = entity.Motion.ActionState & 0xF0;

        if (type <= 0x07)
        {
            if (family is not (0x10 or 0x30 or 0x40 or 0x50 or 0x70 or 0xD0 or 0xE0))
                throw new InvalidOperationException($"{name} action ${entity.Motion.ActionState:X2} is outside the closed type $00-$07 families.");
            return;
        }

        if (type is 0x08 or 0x09 or 0x0C)
        {
            if (family is not (0x00 or 0x40 or 0x50 or 0xD0))
                throw new InvalidOperationException($"{name} type ${type:X2} basic support is limited to $00/$40/$50/$D0; got ${entity.Motion.ActionState:X2}.");
            return;
        }

        if (type is 0x0A or 0x0B)
        {
            if (family is not (0x10 or 0x50))
                throw new InvalidOperationException($"{name} type ${type:X2} is currently closed only for $10/$50; got ${entity.Motion.ActionState:X2}.");
            return;
        }

        throw new InvalidOperationException(
            $"{name} type ${type:X2} is outside the currently composed two-slot entity set.");
    }
}

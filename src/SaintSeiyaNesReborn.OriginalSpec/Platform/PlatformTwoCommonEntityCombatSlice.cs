namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public sealed record PlatformCommonEntitySlotFrameResult(
    PlatformCommonEntityRuntimeState Entity,
    PlatformCommonEntityActiveDispatchResult Dispatch,
    PlatformCommonEntityInteractionResult? Interaction,
    PlatformCommonEntityAttack70PostResult? Attack70Post,
    bool RemovedBeforeInteraction);

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
/// Ordered clean-room slice for the two common movable-entity records processed
/// by $A442. Slot A ($03BA-$03C9) runs before slot B ($03CA-$03D9).
///
/// Closed common families $10/$30/$40/$50/$70/$D0/$E0 are dispatched
/// independently per entity, while attack objects, shared $76/$7F/$80 state and
/// Seventh Sense are threaded from A into B. Family $70 is deliberately split
/// around interaction: preparation occurs before $9915/$98BA and its $A886
/// counter/spawn progression occurs afterward.
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
            entityA,
            currentPlayer,
            currentContact,
            hitboxA,
            pre.PlatformDamage,
            seventhSense,
            frameCounter3C,
            entropy48,
            cameraDelta43,
            engineSubstate02);
        currentPlayer = slotA.Player;
        currentContact = slotA.ContactState;
        seventhSense = slotA.SeventhSense;

        var slotB = ProcessSlot(
            entityB,
            currentPlayer,
            currentContact,
            hitboxB,
            pre.PlatformDamage,
            seventhSense,
            frameCounter3C,
            entropy48,
            cameraDelta43,
            engineSubstate02);
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
            return new SlotCarry(
                new PlatformCommonEntitySlotFrameResult(
                    entity,
                    dispatch,
                    null,
                    null,
                    RemovedBeforeInteraction: true),
                player,
                contactState,
                seventhSense);
        }

        if (dispatch.Continuation == PlatformCommonEntityActiveContinuation.SkipInteraction)
        {
            return new SlotCarry(
                new PlatformCommonEntitySlotFrameResult(
                    entity,
                    dispatch,
                    null,
                    null,
                    RemovedBeforeInteraction: false),
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

        PlatformCommonEntityAttack70PostResult? attack70Post = null;
        if (dispatch.Route == PlatformCommonEntityActiveRoute.Attack70)
        {
            attack70Post = PlatformCommonEntityAttack70.AdvanceAfterInteraction(
                entity.Motion,
                frameCounter3C);
            entity = entity with { Motion = attack70Post.Value.State };
        }

        return new SlotCarry(
            new PlatformCommonEntitySlotFrameResult(
                entity,
                dispatch,
                interaction,
                attack70Post,
                RemovedBeforeInteraction: false),
            player,
            interaction.ContactPhase.State,
            interaction.SeventhSense);
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
        if (entity.Motion.Type > 0x07)
        {
            throw new InvalidOperationException(
                $"{name} must be common entity type $00-$07; got ${entity.Motion.Type:X2}.");
        }

        var family = entity.Motion.ActionState & 0xF0;
        if (family is not (0x10 or 0x30 or 0x40 or 0x50 or 0x70 or 0xD0 or 0xE0))
        {
            throw new InvalidOperationException(
                $"{name} action ${entity.Motion.ActionState:X2} is outside the closed common families $10/$30/$40/$50/$70/$D0/$E0.");
        }
    }
}

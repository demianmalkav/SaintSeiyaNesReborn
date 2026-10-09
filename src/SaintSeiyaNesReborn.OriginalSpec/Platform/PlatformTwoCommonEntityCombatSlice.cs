namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public sealed record PlatformCommonEntitySlotFrameResult(
    PlatformCommonEntityRuntimeState Entity,
    PlatformCommonEntityActiveDispatchResult Dispatch,
    PlatformCommonEntityInteractionResult? Interaction,
    PlatformCommonEntityHitReaction40Result? HitReaction40Post,
    PlatformCommonEntityDeathD0Result? DeathD0Post,
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
/// Per-slot semantics are delegated to PlatformCommonEntitySlotRuntime so this
/// legacy all-common slice and future hybrid schedulers share one canonical
/// implementation of the $A442 common path.
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
        PlatformCommonEntitySlotRuntime.ValidateEntity(entityB);

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

        var slotA = PlatformCommonEntitySlotRuntime.Step(
            entityA,
            currentPlayer.State.AttackState,
            currentPlayer.State.Saint,
            currentPlayer.FrameStartAction4E,
            currentPlayer.State.Horizontal.PlayerX,
            currentPlayer.State.PlayerY,
            currentPlayer.State.JumpPhase49,
            currentContact,
            hitboxA,
            pre.PlatformDamage,
            seventhSense,
            frameCounter3C,
            entropy48,
            cameraDelta43,
            engineSubstate02);
        currentPlayer = ApplySlotCarry(currentPlayer, slotA);
        currentContact = slotA.ContactState;
        seventhSense = slotA.SeventhSense;

        var slotB = PlatformCommonEntitySlotRuntime.Step(
            entityB,
            currentPlayer.State.AttackState,
            currentPlayer.State.Saint,
            currentPlayer.FrameStartAction4E,
            currentPlayer.State.Horizontal.PlayerX,
            currentPlayer.State.PlayerY,
            currentPlayer.State.JumpPhase49,
            currentContact,
            hitboxB,
            pre.PlatformDamage,
            seventhSense,
            frameCounter3C,
            entropy48,
            cameraDelta43,
            engineSubstate02);
        currentPlayer = ApplySlotCarry(currentPlayer, slotB);
        currentContact = slotB.ContactState;
        seventhSense = slotB.SeventhSense;

        var attackPhase = PlatformAttackFramePhases.UpdateObjectsAfterPlayer(
            currentPlayer,
            frameCounter3C);
        var nextCounter = unchecked((byte)(frameCounter3C + 1));

        return new PlatformTwoCommonEntityCombatSliceResult(
            pre,
            latch,
            slotA.Slot,
            slotB.Slot,
            attackPhase,
            attackPhase.Player,
            currentContact,
            seventhSense,
            frameCounter3C,
            nextCounter,
            FrameCounterAdvanced: true,
            ExitedBeforeEntityPipeline: false);
    }

    private static PlatformPlayerActionDispatchResult ApplySlotCarry(
        PlatformPlayerActionDispatchResult player,
        PlatformCommonEntitySlotRuntimeResult slot) =>
        player with
        {
            State = player.State with
            {
                AttackState = slot.AttackState,
                Special76 = slot.ContactState.HazardLatch76,
            },
        };

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

        try
        {
            PlatformCommonEntitySlotRuntime.ValidateEntity(entity);
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException($"{entityName}: {ex.Message}", ex);
        }
    }
}

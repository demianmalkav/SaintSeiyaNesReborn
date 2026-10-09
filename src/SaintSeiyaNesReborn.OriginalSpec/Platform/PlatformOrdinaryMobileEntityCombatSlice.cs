namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public sealed record PlatformOrdinaryMobileEntityCombatSliceResult(
    PlatformPrePlayerResourcePhaseResult PrePlayer,
    PlatformPostPlayerLatchResult PostPlayerLatch,
    PlatformCommonEntityPreparationResult? Preparation,
    PlatformCommonEntityInteractionResult? Interaction,
    PlatformAttackObjectPhaseResult AttackObjectPhase,
    PlatformPlayerActionDispatchResult PlayerAfterLatePhases,
    PlatformContactPhaseState ContactState,
    PlatformCommonEntityRuntimeState Entity,
    int SeventhSense,
    byte FrameCounterBefore3C,
    byte FrameCounterAfter3C,
    bool FrameCounterAdvanced,
    bool EntityRemovedBeforeInteraction,
    bool ExitedBeforeEntityPipeline);

/// <summary>
/// End-to-end active-platform slice for one ordinary mobile common entity
/// (type $00-$07, entry action family $10), now including the entity's own
/// $A55E-$A700 preparation before the already-modeled interaction phases.
///
/// The entity decision reads the player's state after $AAE4 and $B94B, matching
/// the original order: player simulation precedes the later $A442 entity path.
/// </summary>
public static class PlatformOrdinaryMobileEntityCombatSlice
{
    public static PlatformOrdinaryMobileEntityCombatSliceResult StepNonFatal(
        PlatformStageMap stage,
        PlatformPlayerActionState playerState,
        PlatformInput input,
        PlatformFrameResources resources,
        PlatformContactPhaseState contactState,
        PlatformCommonEntityRuntimeState entity,
        PlatformHitboxParameters hitbox,
        int seventhSense,
        byte frameCounter3C,
        byte entropy48,
        byte cameraDelta43,
        byte engineSubstate02,
        byte engineSubstate01 = 0,
        byte dynamicFloorY039B = 0)
    {
        if (playerState.Special76 != contactState.HazardLatch76)
        {
            throw new InvalidOperationException(
                $"Player Special76 (${playerState.Special76:X2}) and contact HazardLatch76 (${contactState.HazardLatch76:X2}) must match at frame entry.");
        }

        if (entity.Motion.Type > 0x07 || (entity.Motion.ActionState & 0xF0) != 0x10)
        {
            throw new InvalidOperationException(
                $"Ordinary mobile slice requires entity type $00-$07 in action family $10; got type ${entity.Motion.Type:X2}, action ${entity.Motion.ActionState:X2}.");
        }

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
        var playerAfterLatch = PlatformPostPlayerLatch.Apply(pre.Player);
        var contactBeforeEntity = pre.ContactStateAfterDrain with
        {
            HazardLatch76 = latch.After76,
        };

        if (playerAfterLatch.ExitsNormalPlayerLoop)
        {
            var skippedAttack = PlatformAttackFramePhases.UpdateObjectsAfterPlayer(
                playerAfterLatch,
                frameCounter3C);

            return new PlatformOrdinaryMobileEntityCombatSliceResult(
                pre,
                latch,
                null,
                null,
                skippedAttack,
                skippedAttack.Player,
                contactBeforeEntity,
                entity,
                seventhSense,
                frameCounter3C,
                frameCounter3C,
                FrameCounterAdvanced: false,
                EntityRemovedBeforeInteraction: false,
                ExitedBeforeEntityPipeline: true);
        }

        // $A442 runs after the player step. Entity AI therefore sees the player's
        // already-updated position and jump phase from this same frame.
        var preparation = PlatformCommonEntityPreparation.StepOrdinaryMobile(
            entity.Motion,
            playerAfterLatch.State.Horizontal.PlayerX,
            playerAfterLatch.State.PlayerY,
            playerAfterLatch.State.JumpPhase49,
            entropy48,
            frameCounter3C,
            cameraDelta43);
        entity = entity with { Motion = preparation.State };

        if (preparation.Outcome != PlatformCommonEntityPreparationOutcome.ReadyForInteraction)
        {
            // Removing one common entity returns from that entity update; it does
            // not abort the platform frame. A22C and C402 still execute later.
            var attackPhase = PlatformAttackFramePhases.UpdateObjectsAfterPlayer(
                playerAfterLatch,
                frameCounter3C);
            var nextCounter = unchecked((byte)(frameCounter3C + 1));

            return new PlatformOrdinaryMobileEntityCombatSliceResult(
                pre,
                latch,
                preparation,
                null,
                attackPhase,
                attackPhase.Player,
                contactBeforeEntity,
                entity,
                seventhSense,
                frameCounter3C,
                nextCounter,
                FrameCounterAdvanced: true,
                EntityRemovedBeforeInteraction: true,
                ExitedBeforeEntityPipeline: false);
        }

        var interaction = PlatformCommonEntityInteractionPhases.ResolveProjectileHitThenContact(
            playerAfterLatch.State.AttackState,
            entity.ToCombatEntity(),
            playerAfterLatch.State.Saint,
            hitbox,
            pre.PlatformDamage,
            seventhSense,
            engineSubstate02,
            contactBeforeEntity,
            playerAfterLatch.FrameStartAction4E,
            playerAfterLatch.State.Horizontal.PlayerX,
            playerAfterLatch.State.PlayerY,
            entity.LifeDrainTicks,
            entity.CosmoDrainTicks);

        entity = entity.WithCombatEntity(interaction.Entity);

        var playerAfterInteraction = playerAfterLatch with
        {
            State = playerAfterLatch.State with
            {
                AttackState = interaction.AttackState,
                Special76 = interaction.ContactPhase.State.HazardLatch76,
            },
        };

        var lateAttack = PlatformAttackFramePhases.UpdateObjectsAfterPlayer(
            playerAfterInteraction,
            frameCounter3C);
        var afterCounter = unchecked((byte)(frameCounter3C + 1));

        return new PlatformOrdinaryMobileEntityCombatSliceResult(
            pre,
            latch,
            preparation,
            interaction,
            lateAttack,
            lateAttack.Player,
            interaction.ContactPhase.State,
            entity,
            interaction.SeventhSense,
            frameCounter3C,
            afterCounter,
            FrameCounterAdvanced: true,
            EntityRemovedBeforeInteraction: false,
            ExitedBeforeEntityPipeline: false);
    }
}

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public sealed record PlatformOneEntityCombatSliceResult(
    PlatformPrePlayerResourcePhaseResult PrePlayer,
    PlatformPostPlayerLatchResult PostPlayerLatch,
    PlatformPlayerActionDispatchResult PlayerAfterLatePhases,
    PlatformCommonEntityInteractionResult? Interaction,
    PlatformAttackObjectPhaseResult AttackObjectPhase,
    PlatformContactPhaseState ContactState,
    PlatformCombatEntity Entity,
    int SeventhSense,
    byte FrameCounterBefore3C,
    byte FrameCounterAfter3C,
    bool FrameCounterAdvanced,
    bool ExitedBeforeLatePipeline);

/// <summary>
/// First end-to-end executable slice of the ordinary active-platform combat
/// frame for one already-positioned common entity.
///
/// It intentionally does not model special/secondary object updates or the
/// entity's preceding AI/movement/render preparation. The supplied entity is the
/// record as it reaches the interaction portion of $A442.
///
/// Confirmed order composed here:
/// pre-player busy/drain -> damage derivation -> $AAE4 player/B -> $B94B $76
/// decrement -> $A442 projectile hit -> $A442 player contact -> $A22C attack
/// object update -> fixed $C402 frame-counter increment.
/// </summary>
public static class PlatformOneEntityCombatSlice
{
    public static PlatformOneEntityCombatSliceResult StepNonFatal(
        PlatformStageMap stage,
        PlatformPlayerActionState playerState,
        PlatformInput input,
        PlatformFrameResources resources,
        PlatformContactPhaseState contactState,
        PlatformCombatEntity entityAtInteraction,
        byte entityLifeDrainTicks,
        byte entityCosmoDrainTicks,
        PlatformHitboxParameters hitbox,
        int seventhSense,
        byte frameCounter3C,
        byte engineSubstate02,
        byte engineSubstate01 = 0,
        byte dynamicFloorY039B = 0)
    {
        // $76 is one physical RAM byte in the original. The clean model exposes
        // it through both player and contact state, so a composed frame must not
        // begin from an impossible split value.
        if (playerState.Special76 != contactState.HazardLatch76)
        {
            throw new InvalidOperationException(
                $"Player Special76 (${playerState.Special76:X2}) and contact HazardLatch76 (${contactState.HazardLatch76:X2}) must match at frame entry.");
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

        // The $80/$76==0 path resets stack/JMPs away from the normal platform
        // call chain. Neither A442, A22C nor the later C402 increment is reached.
        if (playerAfterLatch.ExitsNormalPlayerLoop)
        {
            var skippedAttack = PlatformAttackFramePhases.UpdateObjectsAfterPlayer(
                playerAfterLatch,
                frameCounter3C);

            return new PlatformOneEntityCombatSliceResult(
                pre,
                latch,
                skippedAttack.Player,
                null,
                skippedAttack,
                contactBeforeEntity,
                entityAtInteraction,
                seventhSense,
                frameCounter3C,
                frameCounter3C,
                FrameCounterAdvanced: false,
                ExitedBeforeLatePipeline: true);
        }

        var interaction = PlatformCommonEntityInteractionPhases.ResolveProjectileHitThenContact(
            playerAfterLatch.State.AttackState,
            entityAtInteraction,
            playerAfterLatch.State.Saint,
            hitbox,
            pre.PlatformDamage,
            seventhSense,
            engineSubstate02,
            contactBeforeEntity,
            playerAfterLatch.FrameStartAction4E,
            playerAfterLatch.State.Horizontal.PlayerX,
            playerAfterLatch.State.PlayerY,
            entityLifeDrainTicks,
            entityCosmoDrainTicks);

        // Rejoin the physical $76 and attack-object state after A442. A contact
        // may have replaced the just-decremented latch with $20.
        var playerAfterInteraction = playerAfterLatch with
        {
            State = playerAfterLatch.State with
            {
                AttackState = interaction.AttackState,
                Special76 = interaction.ContactPhase.State.HazardLatch76,
            },
        };

        var attackPhase = PlatformAttackFramePhases.UpdateObjectsAfterPlayer(
            playerAfterInteraction,
            frameCounter3C);

        var nextCounter = unchecked((byte)(frameCounter3C + 1));

        return new PlatformOneEntityCombatSliceResult(
            pre,
            latch,
            attackPhase.Player,
            interaction,
            attackPhase,
            interaction.ContactPhase.State,
            interaction.Entity,
            interaction.SeventhSense,
            frameCounter3C,
            nextCounter,
            FrameCounterAdvanced: true,
            ExitedBeforeLatePipeline: false);
    }
}

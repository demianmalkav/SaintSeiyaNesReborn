namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public sealed record PlatformHazardAndCommonEntityFrameResult(
    PlatformPrePlayerResourcePhaseResult PrePlayer,
    PlatformPostPlayerLatchResult PostPlayerLatch,
    PlatformAuxiliaryHazardPairFrameResult? AuxiliaryHazards,
    PlatformCommonEntityPairAfterPlayerResult? CommonEntities,
    PlatformAuxiliaryHazardSpawnerState AuxiliaryState,
    PlatformCommonEntityRuntimeState EntityA,
    PlatformCommonEntityRuntimeState EntityB,
    PlatformAttackObjectPhaseResult AttackObjectPhase,
    PlatformPlayerActionDispatchResult PlayerAfterLatePhases,
    PlatformContactPhaseState ContactState,
    int SeventhSense,
    byte FrameCounterBefore3C,
    byte FrameCounterAfter3C,
    bool FrameCounterAdvanced,
    bool ExitedBeforeLateObjectPipeline);

/// <summary>
/// Highest currently closed clean-room platform-frame slice for the active
/// object classes reconstructed so far.
///
/// Confirmed order represented here:
/// bank-1 pre-player busy/drain -> $AAE4 player -> $B94B $76 decrement ->
/// $96B4 auxiliary spawn -> $9761 auxiliary A/B -> $A442 common A/B ->
/// $A22C player attack objects -> $C402 $3C increment.
///
/// `$9B93` common-entity spawning runs before `$96B4` in the original but is not
/// yet promoted. The supplied common entity records are therefore the records as
/// they exist after that earlier stage/spawn phase for the current frame.
/// </summary>
public static class PlatformHazardAndCommonEntityFrameSlice
{
    public static PlatformHazardAndCommonEntityFrameResult StepNonFatal(
        PlatformStageMap stage,
        PlatformPlayerActionState playerState,
        PlatformInput input,
        PlatformFrameResources resources,
        PlatformContactPhaseState contactState,
        PlatformAuxiliaryHazardSpawnerState auxiliaryState,
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

            return new PlatformHazardAndCommonEntityFrameResult(
                pre,
                latch,
                null,
                null,
                auxiliaryState,
                entityA,
                entityB,
                skippedAttack,
                skippedAttack.Player,
                currentContact,
                seventhSense,
                frameCounter3C,
                frameCounter3C,
                FrameCounterAdvanced: false,
                ExitedBeforeLateObjectPipeline: true);
        }

        // $96B4/$9761 observe player coordinates after $AAE4 and the current
        // frame's old $3C. Their attack/contact mutations must be visible to A442.
        var auxiliary = PlatformAuxiliaryHazardInteractions.StepPair(
            auxiliaryState,
            entropy48,
            frameCounter3C,
            cameraDelta43,
            currentPlayer.State.Horizontal.PlayerX,
            currentPlayer.State.PlayerY,
            currentPlayer.FrameStartAction4E,
            currentPlayer.State.AttackState,
            currentContact,
            currentPlayer.State.Saint,
            pre.PlatformDamage,
            seventhSense,
            engineSubstate02);

        auxiliaryState = auxiliary.State;
        currentContact = auxiliary.ContactState;
        seventhSense = auxiliary.SeventhSense;
        currentPlayer = currentPlayer with
        {
            State = currentPlayer.State with
            {
                AttackState = auxiliary.AttackState,
                Special76 = currentContact.HazardLatch76,
            },
        };

        var common = PlatformCommonEntityPairAfterPlayer.Step(
            currentPlayer,
            currentContact,
            entityA,
            entityB,
            hitboxA,
            hitboxB,
            pre.PlatformDamage,
            seventhSense,
            frameCounter3C,
            entropy48,
            cameraDelta43,
            engineSubstate02);

        entityA = common.SlotA.Entity;
        entityB = common.SlotB.Entity;
        currentPlayer = common.Player;
        currentContact = common.ContactState;
        seventhSense = common.SeventhSense;

        // Physical RAM byte $76 is already kept synchronized by both pipelines.
        if (currentPlayer.State.Special76 != currentContact.HazardLatch76)
        {
            throw new InvalidOperationException(
                "Late object composition split physical $76 between player and contact state.");
        }

        var attackPhase = PlatformAttackFramePhases.UpdateObjectsAfterPlayer(
            currentPlayer,
            frameCounter3C);
        var nextCounter = unchecked((byte)(frameCounter3C + 1));

        return new PlatformHazardAndCommonEntityFrameResult(
            pre,
            latch,
            auxiliary,
            common,
            auxiliaryState,
            entityA,
            entityB,
            attackPhase,
            attackPhase.Player,
            currentContact,
            seventhSense,
            frameCounter3C,
            nextCounter,
            FrameCounterAdvanced: true,
            ExitedBeforeLateObjectPipeline: false);
    }
}

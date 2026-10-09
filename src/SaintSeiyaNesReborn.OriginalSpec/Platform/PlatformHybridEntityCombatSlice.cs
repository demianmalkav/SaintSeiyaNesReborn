using SaintSeiyaNesReborn.OriginalSpec;

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformHybridEntitySlotRoute
{
    Skipped,
    Common,
    Special08090C,
}

public readonly record struct PlatformHybridEntitySlotState(
    PlatformCommonEntityRuntimeState Entity,
    byte VisualSpritePlus1,
    byte SpecialControl04,
    PlatformEntityAttachedHazardState AttachedHazard,
    byte ParentOffset08)
{
    public static PlatformHybridEntitySlotState Common(
        PlatformCommonEntityRuntimeState entity,
        byte visualSpritePlus1) =>
        new(
            entity,
            visualSpritePlus1,
            SpecialControl04: 0,
            AttachedHazard: PlatformEntityAttachedHazardState.Empty,
            ParentOffset08: 0);

    public static PlatformHybridEntitySlotState Special08090C(
        PlatformCommonEntityRuntimeState entity,
        byte visualSpritePlus1,
        byte control04 = 0,
        PlatformEntityAttachedHazardState? attachedHazard = null,
        byte parentOffset08 = 0) =>
        new(
            entity,
            visualSpritePlus1,
            control04,
            attachedHazard ?? PlatformEntityAttachedHazardState.Empty,
            parentOffset08);
}

public sealed record PlatformHybridEntitySlotFrameResult(
    PlatformHybridEntitySlotState State,
    PlatformCommonSlotActivityResult Activity,
    PlatformHybridEntitySlotRoute Route,
    PlatformCommonEntitySlotFrameResult? Common,
    PlatformSpecialEntityActive08090CResult? Special);

public sealed record PlatformHybridEntityCombatSliceResult(
    PlatformPrePlayerResourcePhaseResult PrePlayer,
    PlatformPostPlayerLatchResult PostPlayerLatch,
    PlatformHybridEntitySlotFrameResult? SlotA,
    PlatformHybridEntitySlotFrameResult? SlotB,
    PlatformAttackObjectPhaseResult AttackObjectPhase,
    PlatformPlayerActionDispatchResult PlayerAfterLatePhases,
    PlatformContactPhaseState ContactState,
    int SeventhSense,
    byte GlobalCounter039A,
    byte FrameCounterBefore3C,
    byte FrameCounterAfter3C,
    bool FrameCounterAdvanced,
    bool ExitedBeforeEntityPipeline);

/// <summary>
/// Ordered clean-room composition of the two logical platform entity slots
/// processed by $A442 after the player/post-player phases.
///
/// Each slot first passes the shared visual/logical activity gate. Admitted
/// common types use PlatformCommonEntitySlotRuntime; admitted scheduled special
/// types $08/$09/$0C use PlatformSpecialEntityActive08090C. Slot A always
/// completes before slot B, and the second slot receives the first slot's
/// mutated attack state, contact latch/drain state, Seventh Sense and global
/// $039A value.
///
/// Visual lifecycle mutation itself remains outside this slice: VisualSpritePlus1
/// is an input gate value carried unchanged here because the corresponding
/// renderer/visual retirement writes have not been promoted into this runtime.
/// </summary>
public static class PlatformHybridEntityCombatSlice
{
    public static PlatformHybridEntityCombatSliceResult StepNonFatal(
        PlatformStageMap stage,
        PlatformPlayerActionState playerState,
        PlatformInput input,
        PlatformFrameResources resources,
        PlatformContactPhaseState contactState,
        PlatformHybridEntitySlotState slotAState,
        PlatformHybridEntitySlotState slotBState,
        PlatformHitboxParameters commonHitboxA,
        PlatformHitboxParameters commonHitboxB,
        int seventhSense,
        byte globalCounter039A,
        byte frameCounter3C,
        byte entropy48,
        byte cameraDelta43,
        byte engineSubstate02,
        byte engineState00,
        byte alternateParent08_03AB,
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
            return new PlatformHybridEntityCombatSliceResult(
                pre,
                latch,
                null,
                null,
                skippedAttack,
                skippedAttack.Player,
                currentContact,
                seventhSense,
                globalCounter039A,
                frameCounter3C,
                frameCounter3C,
                FrameCounterAdvanced: false,
                ExitedBeforeEntityPipeline: true);
        }

        var slotA = StepSlot(
            slotAState,
            commonHitboxA,
            currentPlayer,
            currentContact,
            pre.PlatformDamage,
            seventhSense,
            globalCounter039A,
            frameCounter3C,
            entropy48,
            cameraDelta43,
            engineSubstate02,
            engineState00,
            alternateParent08_03AB);
        currentPlayer = ApplySlotCarry(currentPlayer, slotA.AttackState, slotA.ContactState);
        currentContact = slotA.ContactState;
        seventhSense = slotA.SeventhSense;
        globalCounter039A = slotA.GlobalCounter039A;

        var slotB = StepSlot(
            slotBState,
            commonHitboxB,
            currentPlayer,
            currentContact,
            pre.PlatformDamage,
            seventhSense,
            globalCounter039A,
            frameCounter3C,
            entropy48,
            cameraDelta43,
            engineSubstate02,
            engineState00,
            alternateParent08_03AB);
        currentPlayer = ApplySlotCarry(currentPlayer, slotB.AttackState, slotB.ContactState);
        currentContact = slotB.ContactState;
        seventhSense = slotB.SeventhSense;
        globalCounter039A = slotB.GlobalCounter039A;

        var attackPhase = PlatformAttackFramePhases.UpdateObjectsAfterPlayer(
            currentPlayer,
            frameCounter3C);
        var nextCounter = unchecked((byte)(frameCounter3C + 1));

        return new PlatformHybridEntityCombatSliceResult(
            pre,
            latch,
            slotA.Result,
            slotB.Result,
            attackPhase,
            attackPhase.Player,
            currentContact,
            seventhSense,
            globalCounter039A,
            frameCounter3C,
            nextCounter,
            FrameCounterAdvanced: true,
            ExitedBeforeEntityPipeline: false);
    }

    private sealed record SlotCarry(
        PlatformHybridEntitySlotFrameResult Result,
        PlatformAttackState AttackState,
        PlatformContactPhaseState ContactState,
        int SeventhSense,
        byte GlobalCounter039A);

    private static SlotCarry StepSlot(
        PlatformHybridEntitySlotState state,
        PlatformHitboxParameters commonHitbox,
        PlatformPlayerActionDispatchResult player,
        PlatformContactPhaseState contactState,
        int platformDamage,
        int seventhSense,
        byte globalCounter039A,
        byte frameCounter3C,
        byte entropy48,
        byte cameraDelta43,
        byte engineSubstate02,
        byte engineState00,
        byte alternateParent08_03AB)
    {
        var activity = PlatformCommonSlotActivityGate.Evaluate(
            state.VisualSpritePlus1,
            state.Entity.Motion.ActionState);

        if (!activity.ProcessSlot)
        {
            return new SlotCarry(
                new PlatformHybridEntitySlotFrameResult(
                    state,
                    activity,
                    PlatformHybridEntitySlotRoute.Skipped,
                    Common: null,
                    Special: null),
                player.State.AttackState,
                contactState,
                seventhSense,
                globalCounter039A);
        }

        var type = state.Entity.Motion.Type;
        if (type is 0x08 or 0x09 or 0x0C)
        {
            var specialState = new PlatformSpecialEntityActive08090CState(
                new PlatformSpecialEntityControlState(
                    state.Entity,
                    state.SpecialControl04),
                state.AttachedHazard,
                state.ParentOffset08,
                globalCounter039A);

            var special = PlatformSpecialEntityActive08090C.Step(
                specialState,
                player.State.AttackState,
                player.State.Saint,
                platformDamage,
                seventhSense,
                engineSubstate02,
                contactState,
                player.FrameStartAction4E,
                player.State.Horizontal.PlayerX,
                player.State.PlayerY,
                entropy48,
                frameCounter3C,
                cameraDelta43,
                engineState00,
                alternateParent08_03AB);

            var nextState = state with
            {
                Entity = special.State.Control.Entity,
                SpecialControl04 = special.State.Control.Control04,
                AttachedHazard = special.State.AttachedHazard,
                ParentOffset08 = special.State.ParentOffset08,
            };

            return new SlotCarry(
                new PlatformHybridEntitySlotFrameResult(
                    nextState,
                    activity,
                    PlatformHybridEntitySlotRoute.Special08090C,
                    Common: null,
                    Special: special),
                special.AttackState,
                special.ContactState,
                special.SeventhSense,
                special.State.GlobalCounter039A);
        }

        var common = PlatformCommonEntitySlotRuntime.Step(
            state.Entity,
            player.State.AttackState,
            player.State.Saint,
            player.FrameStartAction4E,
            player.State.Horizontal.PlayerX,
            player.State.PlayerY,
            player.State.JumpPhase49,
            contactState,
            commonHitbox,
            platformDamage,
            seventhSense,
            frameCounter3C,
            entropy48,
            cameraDelta43,
            engineSubstate02);

        var commonState = state with { Entity = common.Slot.Entity };
        return new SlotCarry(
            new PlatformHybridEntitySlotFrameResult(
                commonState,
                activity,
                PlatformHybridEntitySlotRoute.Common,
                Common: common.Slot,
                Special: null),
            common.AttackState,
            common.ContactState,
            common.SeventhSense,
            globalCounter039A);
    }

    private static PlatformPlayerActionDispatchResult ApplySlotCarry(
        PlatformPlayerActionDispatchResult player,
        PlatformAttackState attacks,
        PlatformContactPhaseState contactState) =>
        player with
        {
            State = player.State with
            {
                AttackState = attacks,
                Special76 = contactState.HazardLatch76,
            },
        };
}

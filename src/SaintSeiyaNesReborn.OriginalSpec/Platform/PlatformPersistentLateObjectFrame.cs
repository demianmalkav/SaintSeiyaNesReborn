using SaintSeiyaNesReborn.OriginalSpec;

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformPersistentLateObjectFrameState(
    PlatformPersistentPrimaryEntityFrameState Primary,
    PlatformMultisprite9B93PersistentState Multisprite,
    PlatformAuxiliaryHazardSpawnerState Auxiliary);

public sealed record PlatformPersistentLateObjectNmiResult(
    PlatformPersistentLateObjectFrameState State,
    PlatformNmiPrimaryEncounterRefreshResult NmiRefresh);

public sealed record PlatformPersistentLateObjectMainThreadResult(
    PlatformPersistentLateObjectFrameState State,
    PlatformPersistentPrimaryEntityMainThreadOutcome Outcome,
    PlatformExitTransitionKind? ExitTransition,
    PlatformLatchedCommonProducerEarlyResult EarlyProducer,
    PlatformLatchedCommonProducerPhaseResult? Producer,
    PlatformPrePlayerResourcePhaseResult? PrePlayer,
    PlatformPostPlayerLatchResult? PostPlayerLatch,
    PlatformMultisprite9B93FrameResult? Multisprite,
    PlatformAuxiliaryHazardInteractionPairResult? Auxiliary,
    PlatformHybridEntityPairResult? PrimaryPair,
    PlatformAttackObjectPhaseResult? AttackObjectPhase,
    PlatformPlayerActionDispatchResult? PlayerAfterLatePhases,
    PlatformContactPhaseState ContactState,
    bool PlayerExitedBeforeLateObjects)
{
    public bool Exited => ExitTransition.HasValue;
}

/// <summary>
/// Persistent composition of the promoted platform main-thread path through the
/// complete currently-closed late-object order:
///
///   $B6D0 generic primary producer
///   $969D-$9713 exit gate
///   $8927 scheduled primary producer
///   pre-player resources / player $AAE4 / post-player $B94B latch
///   $9B93 multisprite class
///   $96B4 auxiliary spawn + $9761 slot A + slot B
///   $A442 primary slot A + slot B
///   $A22C player attack-object update
///   one shared $3C increment
///
/// Attack objects, $76/$7F/$80 contact/drain state and Seventh Sense are one
/// physical mutable stream across all late object classes. Persistent object
/// state is carried directly into the next frame.
/// </summary>
public static class PlatformPersistentLateObjectFrame
{
    public static PlatformPersistentLateObjectNmiResult StepNmi(
        PlatformStageMap stage,
        byte cameraLow44,
        byte cameraHigh45,
        byte visual07C0,
        byte state03A4,
        PlatformPersistentLateObjectFrameState state)
    {
        var primary = PlatformPersistentPrimaryEntityFrame.StepNmi(
            stage,
            cameraLow44,
            cameraHigh45,
            visual07C0,
            state03A4,
            state.Primary);

        return new(
            state with { Primary = primary.State },
            primary.NmiRefresh);
    }

    public static PlatformPersistentLateObjectMainThreadResult StepMainThread(
        PlatformStageMap stage,
        byte cameraLow44,
        byte cameraHigh45,
        int scrollX,
        PlatformPlayerActionState playerState,
        PlatformInput input,
        PlatformFrameResources resources,
        PlatformContactPhaseState contactState,
        PlatformPersistentLateObjectFrameState state,
        IReadOnlyList<PlatformSpecialSpawnEntry> scheduledEntries,
        PlatformHitboxParameters commonHitboxA,
        PlatformHitboxParameters commonHitboxB,
        byte entropy48,
        byte cameraDelta43,
        byte engineSubstate02,
        byte engineState00,
        byte alternateParent08_03AB,
        byte multispriteFlag74,
        byte multispriteStageSelector,
        byte engineSubstate01 = 0,
        byte dynamicFloorY039B = 0)
    {
        var primary = state.Primary;
        var bridge = ToEncounterBridgeState(primary);

        // $C30A: generic primary producer runs before the platform exit gate.
        var early = PlatformLatchedCommonProducerPhase.StepCommon(
            stage,
            bridge.EncounterLatch,
            bridge.StagedDescriptor03B7,
            scrollX,
            playerState.Horizontal.PlayerX,
            cameraDelta43,
            entropy48,
            bridge.SpawnState);

        var afterCommonSlotA = ReconcileProducerSlot(
            primary.SlotA,
            early.State.EntityA,
            early.State.VisualSpriteA,
            early.CommonEdge?.SlotA.Spawned == true);
        var afterCommonSlotB = ReconcileProducerSlot(
            primary.SlotB,
            early.State.EntityB,
            early.State.VisualSpriteB,
            early.CommonEdge?.SlotB?.Spawned == true);

        var afterCommonPrimary = primary with
        {
            EncounterLatch = early.ActiveEncounter,
            StagedDescriptor03B7 = early.StagedDescriptor03B7,
            SlotA = afterCommonSlotA,
            SlotB = afterCommonSlotB,
            Cooldown03B8 = early.State.Cooldown03B8,
            LastTriggerLow03A2 = early.State.LastTriggerLow03A2,
        };

        var exit = PlatformExitGate.Evaluate(
            engineSubstate02,
            playerState.Saint,
            playerState.Horizontal.PlayerX,
            playerState.PlayerY,
            playerState.JumpPhase49);

        if (exit is PlatformExitTransitionKind transition)
        {
            var outcome = transition switch
            {
                PlatformExitTransitionKind.State3DReload => PlatformPersistentPrimaryEntityMainThreadOutcome.State3DReload,
                PlatformExitTransitionKind.State70Special => PlatformPersistentPrimaryEntityMainThreadOutcome.State70Special,
                _ => throw new ArgumentOutOfRangeException(nameof(transition), transition, null),
            };

            return new(
                state with { Primary = afterCommonPrimary },
                outcome,
                transition,
                early,
                Producer: null,
                PrePlayer: null,
                PostPlayerLatch: null,
                Multisprite: null,
                Auxiliary: null,
                PrimaryPair: null,
                AttackObjectPhase: null,
                PlayerAfterLatePhases: null,
                contactState,
                PlayerExitedBeforeLateObjects: false);
        }

        // Continuing path reaches bank-1 $8000/$8927 before player processing.
        var producer = PlatformLatchedCommonProducerPhase.StepScheduled(
            early,
            cameraLow44,
            cameraHigh45,
            scheduledEntries);

        var producerSlotA = ReconcileProducerSlot(
            afterCommonSlotA,
            producer.State.EntityA,
            producer.State.VisualSpriteA,
            producer.ScheduledSpecial?.SlotA.Spawned == true);
        var producerSlotB = ReconcileProducerSlot(
            afterCommonSlotB,
            producer.State.EntityB,
            producer.State.VisualSpriteB,
            producer.ScheduledSpecial?.SlotB.Spawned == true);

        var pre = PlatformPrePlayerResourcePhases.StepNonFatal(
            stage,
            playerState,
            input,
            resources,
            contactState,
            primary.FrameCounter3C,
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

        var producerPrimary = afterCommonPrimary with
        {
            SlotA = producerSlotA,
            SlotB = producerSlotB,
            Cooldown03B8 = producer.State.Cooldown03B8,
            LastTriggerLow03A2 = producer.State.LastTriggerLow03A2,
        };

        // Exceptional player actions return before all later object classes and
        // before the normal C402 frame-counter increment. A22C is represented by
        // its existing no-op result for an exited player.
        if (currentPlayer.ExitsNormalPlayerLoop)
        {
            var skippedAttack = PlatformAttackFramePhases.UpdateObjectsAfterPlayer(
                currentPlayer,
                primary.FrameCounter3C);

            return new(
                state with { Primary = producerPrimary },
                PlatformPersistentPrimaryEntityMainThreadOutcome.Continued,
                ExitTransition: null,
                early,
                producer,
                pre,
                latch,
                Multisprite: null,
                Auxiliary: null,
                PrimaryPair: null,
                skippedAttack,
                skippedAttack.Player,
                currentContact,
                PlayerExitedBeforeLateObjects: true);
        }

        var seventhSense = primary.SeventhSense;

        // First late object class: independent $07E0/$03FB multisprite.
        var multisprite = PlatformMultisprite9B93Runtime.Step(
            state.Multisprite,
            currentPlayer.State.AttackState,
            currentContact,
            currentPlayer.State.Saint,
            pre.PlatformDamage,
            seventhSense,
            primary.FrameCounter3C,
            cameraDelta43,
            currentPlayer.State.Horizontal.PlayerX,
            currentPlayer.State.PlayerY,
            currentPlayer.FrameStartAction4E,
            engineSubstate02,
            multispriteFlag74,
            multispriteStageSelector,
            entropy48);
        currentPlayer = ApplySharedCarry(
            currentPlayer,
            multisprite.AttackState,
            multisprite.ContactState);
        currentContact = multisprite.ContactState;
        seventhSense = multisprite.SeventhSense;

        // Then auxiliary spawn/update A -> B. Freshly spawned hazards are updated
        // in this same frame by PlatformAuxiliaryHazardInteractions.StepPair.
        var auxiliary = PlatformAuxiliaryHazardInteractions.StepPair(
            state.Auxiliary,
            currentPlayer.State.AttackState,
            currentContact,
            currentPlayer.State.Saint,
            pre.PlatformDamage,
            seventhSense,
            primary.FrameCounter3C,
            cameraDelta43,
            currentPlayer.State.Horizontal.PlayerX,
            currentPlayer.State.PlayerY,
            currentPlayer.FrameStartAction4E,
            engineSubstate02,
            entropy48);
        currentPlayer = ApplySharedCarry(
            currentPlayer,
            auxiliary.AttackState,
            auxiliary.ContactState);
        currentContact = auxiliary.ContactState;
        seventhSense = auxiliary.SeventhSense;

        // Primary logical records are later than both earlier object classes.
        var pair = PlatformHybridEntityCombatSlice.StepPairAfterPlayer(
            producerSlotA,
            producerSlotB,
            commonHitboxA,
            commonHitboxB,
            currentPlayer,
            currentContact,
            pre.PlatformDamage,
            seventhSense,
            primary.GlobalCounter039A,
            primary.FrameCounter3C,
            entropy48,
            cameraDelta43,
            engineSubstate02,
            engineState00,
            alternateParent08_03AB);

        // $A22C executes once after all collision-capable late object classes.
        var attackPhase = PlatformAttackFramePhases.UpdateObjectsAfterPlayer(
            pair.PlayerAfterSlots,
            primary.FrameCounter3C);
        var nextCounter = unchecked((byte)(primary.FrameCounter3C + 1));

        var nextPrimary = producerPrimary with
        {
            SlotA = pair.SlotA.State,
            SlotB = pair.SlotB.State,
            SeventhSense = pair.SeventhSense,
            GlobalCounter039A = pair.GlobalCounter039A,
            FrameCounter3C = nextCounter,
        };

        var nextState = new PlatformPersistentLateObjectFrameState(
            nextPrimary,
            multisprite.State,
            auxiliary.State);

        return new(
            nextState,
            PlatformPersistentPrimaryEntityMainThreadOutcome.Continued,
            ExitTransition: null,
            early,
            producer,
            pre,
            latch,
            multisprite,
            auxiliary,
            pair,
            attackPhase,
            attackPhase.Player,
            pair.ContactState,
            PlayerExitedBeforeLateObjects: false);
    }

    private static PlatformPlayerActionDispatchResult ApplySharedCarry(
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

    private static PlatformPrimaryEncounterRefreshAndSpawnState ToEncounterBridgeState(
        PlatformPersistentPrimaryEntityFrameState state) =>
        new(
            state.EncounterLatch,
            state.StagedDescriptor03B7,
            new PlatformPageEncounterSpawnState(
                state.SlotA.Entity,
                state.SlotA.VisualSpritePlus1,
                state.SlotB.Entity,
                state.SlotB.VisualSpritePlus1,
                state.Cooldown03B8,
                state.LastTriggerLow03A2));

    private static PlatformHybridEntitySlotState ReconcileProducerSlot(
        PlatformHybridEntitySlotState previous,
        PlatformCommonEntityRuntimeState producedEntity,
        byte producedVisualSprite,
        bool spawned) =>
        previous with
        {
            Entity = producedEntity,
            VisualSpritePlus1 = producedVisualSprite,
            SpecialControl04 = spawned ? (byte)0 : previous.SpecialControl04,
        };
}

using SaintSeiyaNesReborn.OriginalSpec;

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformPersistentPrimaryEntityFrameState(
    PlatformPrimaryEncounterLatchState EncounterLatch,
    byte StagedDescriptor03B7,
    PlatformHybridEntitySlotState SlotA,
    PlatformHybridEntitySlotState SlotB,
    byte Cooldown03B8,
    byte LastTriggerLow03A2,
    int SeventhSense,
    byte GlobalCounter039A,
    byte FrameCounter3C);

public enum PlatformPersistentPrimaryEntityMainThreadOutcome
{
    Continued,
    State3DReload,
    State70Special,
}

public sealed record PlatformPersistentPrimaryEntityNmiResult(
    PlatformPersistentPrimaryEntityFrameState State,
    PlatformNmiPrimaryEncounterRefreshResult NmiRefresh);

public sealed record PlatformPersistentPrimaryEntityMainThreadResult(
    PlatformPersistentPrimaryEntityFrameState State,
    PlatformLatchedCommonProducerPhaseResult Producer,
    PlatformHybridEntityCombatSliceResult Hybrid);

public sealed record PlatformPersistentPrimaryEntityFullMainThreadResult(
    PlatformPersistentPrimaryEntityFrameState State,
    PlatformPersistentPrimaryEntityMainThreadOutcome Outcome,
    PlatformExitTransitionKind? ExitTransition,
    PlatformLatchedCommonProducerEarlyResult EarlyProducer,
    PlatformLatchedCommonProducerPhaseResult? Producer,
    PlatformHybridEntityCombatSliceResult? Hybrid)
{
    public bool Exited => ExitTransition.HasValue;
}

/// <summary>
/// Persistent clean-room bridge from the already-promoted encounter/producer
/// state into the already-promoted hybrid A->B primary-entity scheduler.
///
/// NMI refresh remains a separate callable boundary. The full main-thread path
/// now preserves the confirmed ordering around the platform exit gate:
///
///   generic producer $B6D0
///   platform exit gate $969D-$9713
///   scheduled producer $8927 (continuing path only)
///   pre-player resources / player $AAE4
///   hybrid entity slot A -> slot B
///   late attack objects / shared $3C increment
///
/// An accepted exit is surfaced as a semantic $3D-reload or $70-special result.
/// NES-specific snapshot/PPU/stack/reload plumbing remains outside this logical
/// runtime boundary.
/// </summary>
public static class PlatformPersistentPrimaryEntityFrame
{
    public static PlatformPersistentPrimaryEntityNmiResult StepNmi(
        PlatformStageMap stage,
        byte cameraLow44,
        byte cameraHigh45,
        byte visual07C0,
        byte state03A4,
        PlatformPersistentPrimaryEntityFrameState state)
    {
        var bridge = ToEncounterBridgeState(state);
        var refresh = PlatformPrimaryEncounterRefreshAndSpawn.StepNmi(
            stage,
            cameraLow44,
            cameraHigh45,
            visual07C0,
            state03A4,
            bridge);

        var next = state with
        {
            EncounterLatch = refresh.State.EncounterLatch,
            StagedDescriptor03B7 = refresh.State.StagedDescriptor03B7,
        };

        return new(next, refresh.NmiRefresh);
    }

    /// <summary>
    /// Execute the confirmed main-thread primary-entity path including the
    /// platform exit diversion between $B6D0 and bank-1 $8000/$8927.
    ///
    /// Early generic-producer mutations persist even when the exit gate accepts.
    /// On that branch the scheduled producer, player/entity pipeline and shared
    /// frame-counter advance are not executed.
    /// </summary>
    public static PlatformPersistentPrimaryEntityFullMainThreadResult StepMainThread(
        PlatformStageMap stage,
        byte cameraLow44,
        byte cameraHigh45,
        int scrollX,
        PlatformPlayerActionState playerState,
        PlatformInput input,
        PlatformFrameResources resources,
        PlatformContactPhaseState contactState,
        PlatformPersistentPrimaryEntityFrameState state,
        IReadOnlyList<PlatformSpecialSpawnEntry> scheduledEntries,
        PlatformHitboxParameters commonHitboxA,
        PlatformHitboxParameters commonHitboxB,
        byte entropy48,
        byte cameraDelta43,
        byte engineSubstate02,
        byte engineState00,
        byte alternateParent08_03AB,
        byte engineSubstate01 = 0,
        byte dynamicFloorY039B = 0) =>
        StepMainThreadCore(
            stage,
            cameraLow44,
            cameraHigh45,
            scrollX,
            playerState,
            input,
            resources,
            contactState,
            state,
            scheduledEntries,
            commonHitboxA,
            commonHitboxB,
            entropy48,
            cameraDelta43,
            engineSubstate02,
            engineState00,
            alternateParent08_03AB,
            evaluateExitGate: true,
            engineSubstate01,
            dynamicFloorY039B);

    /// <summary>
    /// Compatibility entry point for callers that have independently established
    /// the frame as non-exit. It uses the same split producer/core implementation
    /// as StepMainThread(...) but deliberately skips exit-gate evaluation.
    /// </summary>
    public static PlatformPersistentPrimaryEntityMainThreadResult StepMainThreadNonExit(
        PlatformStageMap stage,
        byte cameraLow44,
        byte cameraHigh45,
        int scrollX,
        PlatformPlayerActionState playerState,
        PlatformInput input,
        PlatformFrameResources resources,
        PlatformContactPhaseState contactState,
        PlatformPersistentPrimaryEntityFrameState state,
        IReadOnlyList<PlatformSpecialSpawnEntry> scheduledEntries,
        PlatformHitboxParameters commonHitboxA,
        PlatformHitboxParameters commonHitboxB,
        byte entropy48,
        byte cameraDelta43,
        byte engineSubstate02,
        byte engineState00,
        byte alternateParent08_03AB,
        byte engineSubstate01 = 0,
        byte dynamicFloorY039B = 0)
    {
        var full = StepMainThreadCore(
            stage,
            cameraLow44,
            cameraHigh45,
            scrollX,
            playerState,
            input,
            resources,
            contactState,
            state,
            scheduledEntries,
            commonHitboxA,
            commonHitboxB,
            entropy48,
            cameraDelta43,
            engineSubstate02,
            engineState00,
            alternateParent08_03AB,
            evaluateExitGate: false,
            engineSubstate01,
            dynamicFloorY039B);

        if (full.Producer is null || full.Hybrid is null)
            throw new InvalidOperationException("Non-exit compatibility path unexpectedly terminated before scheduled/player/entity phases.");

        return new(full.State, full.Producer, full.Hybrid);
    }

    private static PlatformPersistentPrimaryEntityFullMainThreadResult StepMainThreadCore(
        PlatformStageMap stage,
        byte cameraLow44,
        byte cameraHigh45,
        int scrollX,
        PlatformPlayerActionState playerState,
        PlatformInput input,
        PlatformFrameResources resources,
        PlatformContactPhaseState contactState,
        PlatformPersistentPrimaryEntityFrameState state,
        IReadOnlyList<PlatformSpecialSpawnEntry> scheduledEntries,
        PlatformHitboxParameters commonHitboxA,
        PlatformHitboxParameters commonHitboxB,
        byte entropy48,
        byte cameraDelta43,
        byte engineSubstate02,
        byte engineState00,
        byte alternateParent08_03AB,
        bool evaluateExitGate,
        byte engineSubstate01,
        byte dynamicFloorY039B)
    {
        var bridge = ToEncounterBridgeState(state);

        // Fixed/main path calls bank-0 $B6D0 before the exit gate.
        var early = PlatformLatchedCommonProducerPhase.StepCommon(
            stage,
            bridge.EncounterLatch,
            bridge.StagedDescriptor03B7,
            scrollX,
            playerState.Horizontal.PlayerX,
            cameraDelta43,
            entropy48,
            bridge.SpawnState);

        var commonSpawnedA = early.CommonEdge?.SlotA.Spawned == true;
        var commonSpawnedB = early.CommonEdge?.SlotB?.Spawned == true;
        var afterCommonSlotA = ReconcileProducerSlot(
            state.SlotA,
            early.State.EntityA,
            early.State.VisualSpriteA,
            commonSpawnedA);
        var afterCommonSlotB = ReconcileProducerSlot(
            state.SlotB,
            early.State.EntityB,
            early.State.VisualSpriteB,
            commonSpawnedB);

        var afterCommonState = state with
        {
            EncounterLatch = early.ActiveEncounter,
            StagedDescriptor03B7 = early.StagedDescriptor03B7,
            SlotA = afterCommonSlotA,
            SlotB = afterCommonSlotB,
            Cooldown03B8 = early.State.Cooldown03B8,
            LastTriggerLow03A2 = early.State.LastTriggerLow03A2,
        };

        if (evaluateExitGate)
        {
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
                    afterCommonState,
                    outcome,
                    transition,
                    early,
                    Producer: null,
                    Hybrid: null);
            }
        }

        // Continuing path now enters bank-1 $8000, whose $8927 call runs the
        // scheduled producer before later player/entity processing.
        var producer = PlatformLatchedCommonProducerPhase.StepScheduled(
            early,
            cameraLow44,
            cameraHigh45,
            scheduledEntries);

        var scheduledSpawnedA = producer.ScheduledSpecial?.SlotA.Spawned == true;
        var scheduledSpawnedB = producer.ScheduledSpecial?.SlotB.Spawned == true;
        var producerSlotA = ReconcileProducerSlot(
            afterCommonSlotA,
            producer.State.EntityA,
            producer.State.VisualSpriteA,
            scheduledSpawnedA);
        var producerSlotB = ReconcileProducerSlot(
            afterCommonSlotB,
            producer.State.EntityB,
            producer.State.VisualSpriteB,
            scheduledSpawnedB);

        var hybrid = PlatformHybridEntityCombatSlice.StepNonFatal(
            stage,
            playerState,
            input,
            resources,
            contactState,
            producerSlotA,
            producerSlotB,
            commonHitboxA,
            commonHitboxB,
            state.SeventhSense,
            state.GlobalCounter039A,
            state.FrameCounter3C,
            entropy48,
            cameraDelta43,
            engineSubstate02,
            engineState00,
            alternateParent08_03AB,
            engineSubstate01,
            dynamicFloorY039B);

        // Producers execute before player/entity processing. If the player path
        // exits before the entity pipeline, producer mutations still persist,
        // while the logical slots simply do not receive an entity update.
        var nextSlotA = hybrid.SlotA?.State ?? producerSlotA;
        var nextSlotB = hybrid.SlotB?.State ?? producerSlotB;

        var next = afterCommonState with
        {
            SlotA = nextSlotA,
            SlotB = nextSlotB,
            Cooldown03B8 = producer.State.Cooldown03B8,
            LastTriggerLow03A2 = producer.State.LastTriggerLow03A2,
            SeventhSense = hybrid.SeventhSense,
            GlobalCounter039A = hybrid.GlobalCounter039A,
            FrameCounter3C = hybrid.FrameCounterAfter3C,
        };

        return new(
            next,
            PlatformPersistentPrimaryEntityMainThreadOutcome.Continued,
            ExitTransition: null,
            early,
            producer,
            hybrid);
    }

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
            // Both $B6D0 and $8927 explicitly zero logical +$04 on spawn.
            // Neither producer writes logical +$08, and neither owns the
            // attached $A908/$AA70 raw visual/hazard record.
            SpecialControl04 = spawned ? (byte)0 : previous.SpecialControl04,
        };
}

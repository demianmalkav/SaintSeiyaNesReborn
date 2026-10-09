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

public sealed record PlatformPersistentPrimaryEntityNmiResult(
    PlatformPersistentPrimaryEntityFrameState State,
    PlatformNmiPrimaryEncounterRefreshResult NmiRefresh);

public sealed record PlatformPersistentPrimaryEntityMainThreadResult(
    PlatformPersistentPrimaryEntityFrameState State,
    PlatformLatchedCommonProducerPhaseResult Producer,
    PlatformHybridEntityCombatSliceResult Hybrid);

/// <summary>
/// Persistent clean-room bridge from the already-promoted encounter/producer
/// state into the already-promoted hybrid A->B primary-entity scheduler.
///
/// NMI refresh remains a separate callable boundary. The non-exit main-thread
/// method composes only the confirmed active-path order:
///
///   generic producer $B6D0
///   scheduled producer $8927
///   pre-player resources / player $AAE4
///   hybrid entity slot A -> slot B
///   late attack objects / shared $3C increment
///
/// It does not model the platform-exit diversion that lies between $B6D0 and
/// later bank-1/player work; callers must use StepMainThreadNonExit only after
/// the active frame is known to continue past that diversion.
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
    /// Execute the confirmed producer -> player/entity path for a platform frame
    /// that has already passed the earlier platform-exit diversion.
    ///
    /// Producer mutations are reconciled back into the richer hybrid slot state
    /// without reconstructing it from scratch. Both promoted primary producers
    /// explicitly write logical +$04 = 0 when they spawn/replace a record, while
    /// both skip logical +$08. The attached $A908/$AA70 hazard record is external
    /// to the primary producer writes. Consequently successful producer spawn
    /// resets SpecialControl04 only; ParentOffset08 and AttachedHazard persist.
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
        var bridge = ToEncounterBridgeState(state);
        var producerBoundary = PlatformPrimaryEncounterRefreshAndSpawn.StepMainThread(
            stage,
            cameraLow44,
            cameraHigh45,
            scrollX,
            playerState.Horizontal.PlayerX,
            cameraDelta43,
            entropy48,
            bridge,
            scheduledEntries);

        var producer = producerBoundary.MainThreadProducer;
        var spawnedCommonA = producer.CommonEdge?.SlotA.Spawned == true;
        var spawnedCommonB = producer.CommonEdge?.SlotB?.Spawned == true;
        var spawnedScheduledA = producer.ScheduledSpecial?.SlotA.Spawned == true;
        var spawnedScheduledB = producer.ScheduledSpecial?.SlotB.Spawned == true;

        var producerSlotA = ReconcileProducerSlot(
            state.SlotA,
            producer.State.EntityA,
            producer.State.VisualSpriteA,
            spawnedCommonA || spawnedScheduledA);
        var producerSlotB = ReconcileProducerSlot(
            state.SlotB,
            producer.State.EntityB,
            producer.State.VisualSpriteB,
            spawnedCommonB || spawnedScheduledB);

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

        var next = state with
        {
            EncounterLatch = producerBoundary.State.EncounterLatch,
            StagedDescriptor03B7 = producerBoundary.State.StagedDescriptor03B7,
            SlotA = nextSlotA,
            SlotB = nextSlotB,
            Cooldown03B8 = producer.State.Cooldown03B8,
            LastTriggerLow03A2 = producer.State.LastTriggerLow03A2,
            SeventhSense = hybrid.SeventhSense,
            GlobalCounter039A = hybrid.GlobalCounter039A,
            FrameCounter3C = hybrid.FrameCounterAfter3C,
        };

        return new(next, producer, hybrid);
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

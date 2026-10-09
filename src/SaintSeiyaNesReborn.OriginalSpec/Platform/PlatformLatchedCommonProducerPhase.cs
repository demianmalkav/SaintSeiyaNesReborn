namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public sealed record PlatformLatchedCommonProducerEarlyResult(
    PlatformPrimaryEncounterLatchState ActiveEncounter,
    byte StagedDescriptor03B7,
    PlatformPageEncounterSpawnState State,
    PlatformCommonEdgeSpawnPairResult? CommonEdge,
    bool NoActiveEncounter);

public sealed record PlatformLatchedCommonProducerPhaseResult(
    PlatformPrimaryEncounterLatchState ActiveEncounter,
    byte StagedDescriptor03B7,
    PlatformPageEncounterSpawnState State,
    PlatformCommonEdgeSpawnPairResult? CommonEdge,
    PlatformScheduledSpecialSpawnPairResult? ScheduledSpecial,
    bool NoActiveEncounter);

/// <summary>
/// Main-thread common-entity producer phase using the encounter configuration
/// already accepted into semantic $58/profile state by the NMI-side latch.
///
/// Confirmed active-platform order is not a single uninterrupted producer block:
/// fixed/main platform code calls bank-0 $B6D0 first, then evaluates the bank-1
/// $969D platform-exit gate, and only on the continuing path reaches bank-1
/// $8000 whose $8927 call runs the scheduled-special producer.
///
/// StepCommon(...) and StepScheduled(...) expose that evidence-backed split.
/// Step(...) remains the compatibility composition for callers that already know
/// they are on a non-exit path.
/// </summary>
public static class PlatformLatchedCommonProducerPhase
{
    public static PlatformLatchedCommonProducerEarlyResult StepCommon(
        PlatformStageMap stage,
        PlatformPrimaryEncounterLatchState activeEncounter,
        byte stagedDescriptor03B7,
        int scrollX,
        byte playerX,
        byte cameraDelta43,
        byte entropy48,
        PlatformPageEncounterSpawnState state)
    {
        if (activeEncounter.ActiveEngine58 == 0 || activeEncounter.ActiveConfig is null)
        {
            return new(
                activeEncounter,
                stagedDescriptor03B7,
                state,
                null,
                NoActiveEncounter: true);
        }

        var config = ValidateAndGetConfig(activeEncounter);

        var common = config.StepCommonEdgeSpawner(
            stage,
            scrollX,
            playerX,
            cameraDelta43,
            entropy48,
            spawnGate03B7: stagedDescriptor03B7,
            cooldown03B8: state.Cooldown03B8,
            entityA: state.EntityA,
            visualSpriteA: state.VisualSpriteA,
            entityB: state.EntityB,
            visualSpriteB: state.VisualSpriteB);

        var afterCommon = state with
        {
            EntityA = common.SlotA.Entity,
            VisualSpriteA = common.SlotA.VisualSprite,
            EntityB = common.SlotB?.Entity ?? state.EntityB,
            VisualSpriteB = common.SlotB?.VisualSprite ?? state.VisualSpriteB,
            Cooldown03B8 = common.Cooldown03B8,
        };

        return new(
            activeEncounter,
            stagedDescriptor03B7,
            afterCommon,
            common,
            NoActiveEncounter: false);
    }

    public static PlatformLatchedCommonProducerPhaseResult StepScheduled(
        PlatformLatchedCommonProducerEarlyResult early,
        byte cameraLow44,
        byte cameraHigh45,
        IReadOnlyList<PlatformSpecialSpawnEntry> scheduledEntries)
    {
        if (early.NoActiveEncounter)
        {
            return new(
                early.ActiveEncounter,
                early.StagedDescriptor03B7,
                early.State,
                early.CommonEdge,
                null,
                NoActiveEncounter: true);
        }

        var config = ValidateAndGetConfig(early.ActiveEncounter);
        var scheduled = config.TryScheduledSpecialSpawn(
            early.State.EntityA,
            early.State.VisualSpriteA,
            early.State.EntityB,
            early.State.VisualSpriteB,
            cameraLow44,
            cameraHigh45,
            early.State.LastTriggerLow03A2,
            scheduledEntries);

        var afterScheduled = early.State with
        {
            EntityA = scheduled.SlotA.Entity,
            VisualSpriteA = scheduled.SlotA.VisualSprite,
            EntityB = scheduled.SlotB.Entity,
            VisualSpriteB = scheduled.SlotB.VisualSprite,
            LastTriggerLow03A2 = scheduled.LastTriggerLow03A2,
        };

        return new(
            early.ActiveEncounter,
            early.StagedDescriptor03B7,
            afterScheduled,
            early.CommonEdge,
            scheduled,
            NoActiveEncounter: false);
    }

    /// <summary>
    /// Compatibility composition for an already-established non-exit path:
    /// generic $B6D0 first, scheduled $8927 second.
    /// </summary>
    public static PlatformLatchedCommonProducerPhaseResult Step(
        PlatformStageMap stage,
        PlatformPrimaryEncounterLatchState activeEncounter,
        byte stagedDescriptor03B7,
        byte cameraLow44,
        byte cameraHigh45,
        int scrollX,
        byte playerX,
        byte cameraDelta43,
        byte entropy48,
        PlatformPageEncounterSpawnState state,
        IReadOnlyList<PlatformSpecialSpawnEntry> scheduledEntries)
    {
        var early = StepCommon(
            stage,
            activeEncounter,
            stagedDescriptor03B7,
            scrollX,
            playerX,
            cameraDelta43,
            entropy48,
            state);

        return StepScheduled(
            early,
            cameraLow44,
            cameraHigh45,
            scheduledEntries);
    }

    private static PlatformPrimaryEncounterSpawnConfig ValidateAndGetConfig(
        PlatformPrimaryEncounterLatchState activeEncounter)
    {
        if (activeEncounter.ActiveConfig is not PlatformPrimaryEncounterSpawnConfig config)
            throw new InvalidOperationException("Active encounter does not have a spawn config.");

        if (config.Engine58 != activeEncounter.ActiveEngine58)
            throw new InvalidOperationException("Active encounter config does not match active $58.");

        return config;
    }
}

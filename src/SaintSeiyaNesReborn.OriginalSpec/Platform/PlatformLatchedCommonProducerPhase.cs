namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

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
/// Confirmed order in the active platform update is bank-0 $B6D0 first, then
/// bank-1 $8000 whose $8927 call runs the scheduled-special producer. Both are
/// invoked before platform damage and bank-3 $AAE4 player processing.
///
/// The generic producer receives staged $03B7 separately from active $58; this
/// preserves the original mismatch gate during deferred page transitions. The
/// scheduled producer sees active $58/profile but has no $03B7 equality gate.
/// </summary>
public static class PlatformLatchedCommonProducerPhase
{
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
        if (activeEncounter.ActiveEngine58 == 0 || activeEncounter.ActiveConfig is null)
        {
            return new(
                activeEncounter,
                stagedDescriptor03B7,
                state,
                null,
                null,
                NoActiveEncounter: true);
        }

        var config = activeEncounter.ActiveConfig.Value;
        if (config.Engine58 != activeEncounter.ActiveEngine58)
            throw new InvalidOperationException("Active encounter config does not match active $58.");

        // First: fixed/main platform maps bank 0 and calls $B6D0.
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

        // Second: bank-1 $8000 calls $8927. It uses active $58/profile and the
        // current camera schedule. It does not compare $03B7 with $58.
        var scheduled = config.TryScheduledSpecialSpawn(
            afterCommon.EntityA,
            afterCommon.VisualSpriteA,
            afterCommon.EntityB,
            afterCommon.VisualSpriteB,
            cameraLow44,
            cameraHigh45,
            afterCommon.LastTriggerLow03A2,
            scheduledEntries);

        var afterScheduled = afterCommon with
        {
            EntityA = scheduled.SlotA.Entity,
            VisualSpriteA = scheduled.SlotA.VisualSprite,
            EntityB = scheduled.SlotB.Entity,
            VisualSpriteB = scheduled.SlotB.VisualSprite,
            LastTriggerLow03A2 = scheduled.LastTriggerLow03A2,
        };

        return new(
            activeEncounter,
            stagedDescriptor03B7,
            afterScheduled,
            common,
            scheduled,
            NoActiveEncounter: false);
    }
}

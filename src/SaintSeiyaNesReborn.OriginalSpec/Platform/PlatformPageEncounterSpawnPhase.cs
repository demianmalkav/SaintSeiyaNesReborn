namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformPageEncounterSpawnRoute
{
    None,
    CommonEdgeB6D0,
    ScheduledSpecial8925,
}

public readonly record struct PlatformPageEncounterSpawnState(
    PlatformCommonEntityRuntimeState EntityA,
    byte VisualSpriteA,
    PlatformCommonEntityRuntimeState EntityB,
    byte VisualSpriteB,
    byte Cooldown03B8,
    byte LastTriggerLow03A2);

public sealed record PlatformPageEncounterSpawnPhaseResult(
    PlatformPageEncounterSpawnRoute Route,
    PlatformPageEncounterSpawnState State,
    PlatformStagePage? Page,
    PlatformPrimaryEncounterSpawnConfig? EncounterConfig,
    PlatformCommonEdgeSpawnPairResult? CommonEdge,
    PlatformScheduledSpecialSpawnPairResult? ScheduledSpecial,
    bool PageOutOfRange,
    bool EmptyEncounter);

/// <summary>
/// Semantic composition of the page-indexed primary encounter stream with the
/// two confirmed producers for the shared $03BA/$03CA common-entity records.
///
/// Bank 1 uses $45 as the current page index when reading the per-substate
/// encounter descriptor. This clean-room phase mirrors that selection, then
/// routes the accepted page encounter to either the generic bank-0 $B6D0
/// producer or the bank-1 $8925 scheduled-special producer.
///
/// It intentionally does not model the earlier "safe to accept a changed
/// descriptor into $58" latch yet. Callers should invoke this with the encounter
/// configuration that is semantically active for the frame. Here the stage page
/// is used as the source of that configuration so level data and producers are
/// connected without duplicate type/tier/stat parameters.
/// </summary>
public static class PlatformPageEncounterSpawnPhase
{
    public static PlatformPageEncounterSpawnPhaseResult Step(
        PlatformStageMap stage,
        byte cameraLow44,
        byte cameraHigh45,
        int scrollX,
        byte playerX,
        byte cameraDelta43,
        byte entropy48,
        byte spawnGate03B7,
        PlatformPageEncounterSpawnState state,
        IReadOnlyList<PlatformSpecialSpawnEntry> scheduledEntries)
    {
        if (cameraHigh45 >= stage.Pages.Count)
        {
            return new(
                PlatformPageEncounterSpawnRoute.None,
                state,
                null,
                null,
                null,
                null,
                PageOutOfRange: true,
                EmptyEncounter: false);
        }

        var page = stage.Pages[cameraHigh45];
        var encounter = page.PrimaryEncounter;
        if (encounter.Raw == 0 || encounter.Stats is null)
        {
            return new(
                PlatformPageEncounterSpawnRoute.None,
                state,
                page,
                null,
                null,
                null,
                PageOutOfRange: false,
                EmptyEncounter: true);
        }

        var config = PlatformPrimaryEncounterSpawnConfig.FromStagePage(page);

        if (PlatformScheduledSpecialEntitySpawner.IsSupportedType(config.Engine58))
        {
            var scheduled = config.TryScheduledSpecialSpawn(
                state.EntityA,
                state.VisualSpriteA,
                state.EntityB,
                state.VisualSpriteB,
                cameraLow44,
                cameraHigh45,
                state.LastTriggerLow03A2,
                scheduledEntries);

            var next = state with
            {
                EntityA = scheduled.SlotA.Entity,
                VisualSpriteA = scheduled.SlotA.VisualSprite,
                EntityB = scheduled.SlotB.Entity,
                VisualSpriteB = scheduled.SlotB.VisualSprite,
                LastTriggerLow03A2 = scheduled.LastTriggerLow03A2,
            };

            return new(
                PlatformPageEncounterSpawnRoute.ScheduledSpecial8925,
                next,
                page,
                config,
                null,
                scheduled,
                PageOutOfRange: false,
                EmptyEncounter: false);
        }

        var common = config.StepCommonEdgeSpawner(
            stage,
            scrollX,
            playerX,
            cameraDelta43,
            entropy48,
            spawnGate03B7,
            state.Cooldown03B8,
            state.EntityA,
            state.VisualSpriteA,
            state.EntityB,
            state.VisualSpriteB);

        var commonNext = state with
        {
            EntityA = common.SlotA.Entity,
            VisualSpriteA = common.SlotA.VisualSprite,
            EntityB = common.SlotB?.Entity ?? state.EntityB,
            VisualSpriteB = common.SlotB?.VisualSprite ?? state.VisualSpriteB,
            Cooldown03B8 = common.Cooldown03B8,
        };

        return new(
            PlatformPageEncounterSpawnRoute.CommonEdgeB6D0,
            commonNext,
            page,
            config,
            common,
            null,
            PageOutOfRange: false,
            EmptyEncounter: false);
    }
}

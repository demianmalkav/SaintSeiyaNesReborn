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
/// The legacy Step(...) entry point derives the producer configuration directly
/// from the selected stage page and is retained for isolated producer tests.
/// Frame-accurate composition must instead use StepAccepted(...), passing the
/// configuration currently accepted in engine byte $58. That distinction is
/// required because bank-1 $996C can stage a new page descriptor in $03B7 while
/// deliberately keeping the previous $58/profile active until both common slots
/// are safe.
/// </summary>
public static class PlatformPageEncounterSpawnPhase
{
    /// <summary>
    /// Page-driven convenience entry point used by isolated producer fixtures.
    /// It assumes the selected page encounter has already become active $58.
    /// </summary>
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
            return NoPage(state);

        var page = stage.Pages[cameraHigh45];
        var encounter = page.PrimaryEncounter;
        if (encounter.Raw == 0 || encounter.Stats is null)
            return NoActiveEncounter(state, page);

        var config = PlatformPrimaryEncounterSpawnConfig.FromStagePage(page);
        return StepWithConfig(
            stage,
            page,
            config,
            cameraLow44,
            cameraHigh45,
            scrollX,
            playerX,
            cameraDelta43,
            entropy48,
            spawnGate03B7,
            state,
            scheduledEntries);
    }

    /// <summary>
    /// Frame-accurate producer entry point. The selected page is retained only
    /// as spatial/context metadata; spawning is driven exclusively by the active
    /// encounter configuration that corresponds to accepted engine $58.
    ///
    /// Passing null means active $58 is zero. This is intentionally independent
    /// from the current page descriptor, which may already contain the next
    /// staged encounter while acceptance is deferred.
    /// </summary>
    public static PlatformPageEncounterSpawnPhaseResult StepAccepted(
        PlatformStageMap stage,
        PlatformPrimaryEncounterSpawnConfig? activeEncounterConfig,
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
            return NoPage(state);

        var page = stage.Pages[cameraHigh45];
        if (activeEncounterConfig is not PlatformPrimaryEncounterSpawnConfig config)
            return NoActiveEncounter(state, page);

        return StepWithConfig(
            stage,
            page,
            config,
            cameraLow44,
            cameraHigh45,
            scrollX,
            playerX,
            cameraDelta43,
            entropy48,
            spawnGate03B7,
            state,
            scheduledEntries);
    }

    private static PlatformPageEncounterSpawnPhaseResult StepWithConfig(
        PlatformStageMap stage,
        PlatformStagePage page,
        PlatformPrimaryEncounterSpawnConfig config,
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

    private static PlatformPageEncounterSpawnPhaseResult NoPage(
        PlatformPageEncounterSpawnState state) =>
        new(
            PlatformPageEncounterSpawnRoute.None,
            state,
            null,
            null,
            null,
            null,
            PageOutOfRange: true,
            EmptyEncounter: false);

    private static PlatformPageEncounterSpawnPhaseResult NoActiveEncounter(
        PlatformPageEncounterSpawnState state,
        PlatformStagePage page) =>
        new(
            PlatformPageEncounterSpawnRoute.None,
            state,
            page,
            null,
            null,
            null,
            PageOutOfRange: false,
            EmptyEncounter: true);
}

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformPrimaryEncounterRefreshAndSpawnState(
    PlatformPrimaryEncounterLatchState EncounterLatch,
    byte StagedDescriptor03B7,
    PlatformPageEncounterSpawnState SpawnState);

public sealed record PlatformPrimaryEncounterRefreshBoundaryResult(
    PlatformPrimaryEncounterRefreshAndSpawnState State,
    PlatformNmiPrimaryEncounterRefreshResult NmiRefresh);

public sealed record PlatformPrimaryEncounterProducerBoundaryResult(
    PlatformPrimaryEncounterRefreshAndSpawnState State,
    PlatformLatchedCommonProducerPhaseResult MainThreadProducer);

/// <summary>
/// State bridge between the already-closed NMI-side primary encounter refresh
/// phase and the already-closed main-thread producer phase.
///
/// This class deliberately exposes two methods rather than one unordered Step:
/// - StepNmi(...) executes the $D269/$D7F2 -> bank-1 $996C refresh/acceptance side;
/// - StepMainThread(...) later executes bank-0 $B6D0 followed by bank-1 $8927.
///
/// The bridge keeps active $58/profile, staged $03B7 and producer state separate
/// across the execution-context boundary. A caller/scheduler decides when the NMI
/// boundary occurs relative to a main-thread update; this type only guarantees
/// that state produced by one closed phase is handed to the next without being
/// collapsed or reconstructed from the visible page.
/// </summary>
public static class PlatformPrimaryEncounterRefreshAndSpawn
{
    /// <summary>
    /// Execute the NMI-side refresh/acceptance phase and persist any newly staged
    /// $03B7 descriptor. When the refresh gate suppresses $996C, the previous
    /// staged descriptor is retained exactly.
    /// </summary>
    public static PlatformPrimaryEncounterRefreshBoundaryResult StepNmi(
        PlatformStageMap stage,
        byte cameraLow44,
        byte cameraHigh45,
        byte visual07C0,
        byte state03A4,
        PlatformPrimaryEncounterRefreshAndSpawnState state)
    {
        var refresh = PlatformNmiPrimaryEncounterRefreshPhase.StepPlatformState20(
            stage,
            cameraLow44,
            cameraHigh45,
            visual07C0,
            state03A4,
            state.EncounterLatch,
            state.SpawnState.VisualSpriteA,
            state.SpawnState.EntityA.Motion.ActionState,
            state.SpawnState.VisualSpriteB,
            state.SpawnState.EntityB.Motion.ActionState);

        var staged = refresh.Acceptance is PlatformPrimaryEncounterAcceptanceResult acceptance
            ? acceptance.StagedDescriptor03B7
            : state.StagedDescriptor03B7;

        var next = state with
        {
            EncounterLatch = refresh.LatchState,
            StagedDescriptor03B7 = staged,
        };

        return new(next, refresh);
    }

    /// <summary>
    /// Execute the main-thread producer order against the state already accepted
    /// at the prior NMI boundary: generic $B6D0 first, scheduled $8927 second.
    ///
    /// The generic producer receives staged $03B7 separately from active $58,
    /// while the scheduled producer consumes active $58/profile without an
    /// invented staged-descriptor equality gate.
    /// </summary>
    public static PlatformPrimaryEncounterProducerBoundaryResult StepMainThread(
        PlatformStageMap stage,
        byte cameraLow44,
        byte cameraHigh45,
        int scrollX,
        byte playerX,
        byte cameraDelta43,
        byte entropy48,
        PlatformPrimaryEncounterRefreshAndSpawnState state,
        IReadOnlyList<PlatformSpecialSpawnEntry> scheduledEntries)
    {
        var producer = PlatformLatchedCommonProducerPhase.Step(
            stage,
            state.EncounterLatch,
            state.StagedDescriptor03B7,
            cameraLow44,
            cameraHigh45,
            scrollX,
            playerX,
            cameraDelta43,
            entropy48,
            state.SpawnState,
            scheduledEntries);

        var next = state with { SpawnState = producer.State };
        return new(next, producer);
    }
}

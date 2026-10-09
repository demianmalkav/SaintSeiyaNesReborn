namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformPrimaryEncounterRefreshAndSpawnState(
    PlatformPrimaryEncounterLatchState EncounterLatch,
    byte StagedDescriptor03B7,
    PlatformPageEncounterSpawnState SpawnState);

public sealed record PlatformPrimaryEncounterRefreshAndSpawnResult(
    PlatformPrimaryEncounterRefreshAndSpawnState State,
    PlatformPrimaryEncounterRefreshGateResult RefreshGate,
    PlatformPrimaryEncounterAcceptanceResult? Acceptance,
    PlatformPageEncounterSpawnPhaseResult SpawnPhase,
    bool AcceptancePageOutOfRange)
{
    public bool AcceptanceAttempted => Acceptance is not null;
}

/// <summary>
/// Composes the closed primary-encounter control chain:
///
/// fixed $D7F2 refresh gate
///   -> bank-1 $9915/$996C safe-acceptance latch
///   -> accepted $58/profile
///   -> common/scheduled producer phase.
///
/// The essential invariant is that the producer consumes EncounterLatch.ActiveConfig,
/// not the descriptor on the currently visible page. $996C may stage a new page
/// descriptor in $03B7 and defer its acceptance while the old $58/profile remains
/// active. In that situation the old producer configuration survives, while the
/// freshly staged $03B7 still participates in the generic $B6D0 spawn gate.
/// </summary>
public static class PlatformPrimaryEncounterRefreshAndSpawn
{
    public static PlatformPrimaryEncounterRefreshAndSpawnResult Step(
        PlatformStageMap stage,
        byte cameraLow44,
        byte cameraHigh45,
        int scrollX,
        byte playerX,
        byte cameraDelta43,
        byte entropy48,
        byte visual07C0,
        byte state03A4,
        PlatformPrimaryEncounterRefreshAndSpawnState state,
        IReadOnlyList<PlatformSpecialSpawnEntry> scheduledEntries)
    {
        var gate = PlatformPrimaryEncounterRefreshGate.Evaluate(
            cameraLow44,
            visual07C0,
            state03A4);

        var latch = state.EncounterLatch;
        var staged03B7 = state.StagedDescriptor03B7;
        PlatformPrimaryEncounterAcceptanceResult? acceptance = null;
        var acceptancePageOutOfRange = false;

        if (gate.InvokeAcceptance996C)
        {
            if (cameraHigh45 >= stage.Pages.Count)
            {
                // Valid game states keep $45 inside the stage page table. The
                // clean-room model contains malformed/out-of-range inputs without
                // manufacturing a descriptor from unrelated memory.
                acceptancePageOutOfRange = true;
            }
            else
            {
                var page = stage.Pages[cameraHigh45];
                var accepted = PlatformPrimaryEncounterAcceptance.Step(
                    page.PrimaryEncounter,
                    latch,
                    state.SpawnState.VisualSpriteA,
                    state.SpawnState.EntityA.Motion.ActionState,
                    state.SpawnState.VisualSpriteB,
                    state.SpawnState.EntityB.Motion.ActionState);

                acceptance = accepted;
                staged03B7 = accepted.StagedDescriptor03B7;
                latch = accepted.State;
            }
        }

        var spawn = PlatformPageEncounterSpawnPhase.StepAccepted(
            stage,
            latch.ActiveConfig,
            cameraLow44,
            cameraHigh45,
            scrollX,
            playerX,
            cameraDelta43,
            entropy48,
            staged03B7,
            state.SpawnState,
            scheduledEntries);

        var next = new PlatformPrimaryEncounterRefreshAndSpawnState(
            latch,
            staged03B7,
            spawn.State);

        return new(
            next,
            gate,
            acceptance,
            spawn,
            acceptancePageOutOfRange);
    }
}

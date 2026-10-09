namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public sealed record PlatformNmiPrimaryEncounterRefreshResult(
    PlatformPrimaryEncounterRefreshGateResult Gate,
    PlatformPrimaryEncounterAcceptanceResult? Acceptance,
    PlatformPrimaryEncounterLatchState LatchState,
    PlatformStagePage? StagedPage,
    bool AcceptanceInvoked,
    bool PageOutOfRange);

/// <summary>
/// Composes the confirmed NMI-side primary encounter refresh path for platform
/// mode without folding it into the main-thread entity producer phase.
///
/// Fixed NMI handler $D269 dispatches global state $00==$20 through $D7F2,
/// which applies the refresh invocation gate. When that gate reaches bank-1
/// $996C, current page $45 is staged and passed through the safe-acceptance
/// latch. The resulting active $58/profile is state for subsequent execution;
/// the bank-0/bank-1 entity producers remain a distinct main-thread phase.
/// </summary>
public static class PlatformNmiPrimaryEncounterRefreshPhase
{
    public static PlatformNmiPrimaryEncounterRefreshResult StepPlatformState20(
        PlatformStageMap stage,
        byte cameraLow44,
        byte cameraHigh45,
        byte visual07C0,
        byte state03A4,
        PlatformPrimaryEncounterLatchState latchState,
        byte commonVisualSpriteA,
        byte commonActionA,
        byte commonVisualSpriteB,
        byte commonActionB)
    {
        var gate = PlatformPrimaryEncounterRefreshGate.Evaluate(
            cameraLow44,
            visual07C0,
            state03A4);

        if (!gate.InvokeAcceptance996C)
        {
            return new(
                gate,
                null,
                latchState,
                null,
                AcceptanceInvoked: false,
                PageOutOfRange: false);
        }

        if (cameraHigh45 >= stage.Pages.Count)
        {
            // The canonical stage data keeps $45 inside its current substate
            // page schedule. Keep the clean model contained if fed an impossible
            // external state rather than reading past the stage representation.
            return new(
                gate,
                null,
                latchState,
                null,
                AcceptanceInvoked: false,
                PageOutOfRange: true);
        }

        var page = stage.Pages[cameraHigh45];
        var acceptance = PlatformPrimaryEncounterAcceptance.Step(
            page.PrimaryEncounter,
            latchState,
            commonVisualSpriteA,
            commonActionA,
            commonVisualSpriteB,
            commonActionB);

        return new(
            gate,
            acceptance,
            acceptance.State,
            page,
            AcceptanceInvoked: true,
            PageOutOfRange: false);
    }
}

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformPrimaryEncounterRefreshGateOutcome
{
    AlignedScrollAlternatePath,
    AcceptanceEligibleVisualFree,
    AcceptanceEligibleStateA4,
    SpecialVisualBranchSuppressesAcceptance,
}

public readonly record struct PlatformPrimaryEncounterRefreshGateResult(
    PlatformPrimaryEncounterRefreshGateOutcome Outcome,
    bool InvokeBank1_9915,
    bool InvokeAcceptance996C,
    bool Clears03A3,
    bool TakesSpecialVisualBranch)
{
    public bool AcceptanceAttempted => InvokeAcceptance996C;
}

/// <summary>
/// Clean-room reduction of fixed-bank $D7F2-$D80B plus the entry gate at
/// bank-1 $9915-$9923 that determines whether primary encounter acceptance
/// ($996C) is reached on this invocation.
///
/// The fixed wrapper only maps bank 1 / calls $9915 when ($44 & $06) != 0.
/// When called, it first clears $03A3. Inside bank 1, acceptance is reached if
/// either $07C0 is $FE or $03A4 is not $FF. The remaining combination takes a
/// special visual/CHR path and returns without touching the acceptance latch.
/// </summary>
public static class PlatformPrimaryEncounterRefreshGate
{
    public const byte FreeVisualMarker = 0xFE;
    public const byte SpecialStateSentinel = 0xFF;

    public static PlatformPrimaryEncounterRefreshGateResult Evaluate(
        byte cameraLow44,
        byte visual07C0,
        byte state03A4)
    {
        if ((cameraLow44 & 0x06) == 0)
        {
            return new(
                PlatformPrimaryEncounterRefreshGateOutcome.AlignedScrollAlternatePath,
                InvokeBank1_9915: false,
                InvokeAcceptance996C: false,
                Clears03A3: false,
                TakesSpecialVisualBranch: false);
        }

        if (visual07C0 == FreeVisualMarker)
        {
            return new(
                PlatformPrimaryEncounterRefreshGateOutcome.AcceptanceEligibleVisualFree,
                InvokeBank1_9915: true,
                InvokeAcceptance996C: true,
                Clears03A3: true,
                TakesSpecialVisualBranch: false);
        }

        if (state03A4 != SpecialStateSentinel)
        {
            return new(
                PlatformPrimaryEncounterRefreshGateOutcome.AcceptanceEligibleStateA4,
                InvokeBank1_9915: true,
                InvokeAcceptance996C: true,
                Clears03A3: true,
                TakesSpecialVisualBranch: false);
        }

        return new(
            PlatformPrimaryEncounterRefreshGateOutcome.SpecialVisualBranchSuppressesAcceptance,
            InvokeBank1_9915: true,
            InvokeAcceptance996C: false,
            Clears03A3: true,
            TakesSpecialVisualBranch: true);
    }
}

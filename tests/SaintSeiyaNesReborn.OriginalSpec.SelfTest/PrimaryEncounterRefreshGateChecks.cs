using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class PrimaryEncounterRefreshGateChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckAlignedScrollSkipsBank1Refresh();
        CheckFreeVisualReachesAcceptance();
        CheckNonSentinelStateA4ReachesAcceptance();
        CheckSpecialVisualBranchSuppressesAcceptance();
    }

    private static void CheckAlignedScrollSkipsBank1Refresh()
    {
        var result = PlatformPrimaryEncounterRefreshGate.Evaluate(
            cameraLow44: 0x00,
            visual07C0: 0xFE,
            state03A4: 0x00);

        Require(result.Outcome == PlatformPrimaryEncounterRefreshGateOutcome.AlignedScrollAlternatePath,
            "$44&$06==0 takes fixed-bank alternate path");
        Require(!result.InvokeBank1_9915 && !result.InvokeAcceptance996C,
            "aligned path does not call bank1 $9915 or acceptance");
        Require(!result.Clears03A3,
            "$03A3 clear belongs only to the bank1-call wrapper path");
    }

    private static void CheckFreeVisualReachesAcceptance()
    {
        var result = PlatformPrimaryEncounterRefreshGate.Evaluate(
            cameraLow44: 0x02,
            visual07C0: 0xFE,
            state03A4: 0xFF);

        Require(result.Outcome == PlatformPrimaryEncounterRefreshGateOutcome.AcceptanceEligibleVisualFree,
            "$07C0==$FE branches directly from bank1 $9915 to $996C");
        Require(result.InvokeBank1_9915 && result.InvokeAcceptance996C,
            "free visual marker reaches acceptance");
        Require(result.Clears03A3,
            "fixed wrapper clears $03A3 before calling bank1");
    }

    private static void CheckNonSentinelStateA4ReachesAcceptance()
    {
        var result = PlatformPrimaryEncounterRefreshGate.Evaluate(
            cameraLow44: 0x04,
            visual07C0: 0x80,
            state03A4: 0x7F);

        Require(result.Outcome == PlatformPrimaryEncounterRefreshGateOutcome.AcceptanceEligibleStateA4,
            "occupied visual still reaches $996C when $03A4!=$FF");
        Require(result.InvokeAcceptance996C && !result.TakesSpecialVisualBranch,
            "non-sentinel $03A4 suppresses the special visual branch");
    }

    private static void CheckSpecialVisualBranchSuppressesAcceptance()
    {
        var result = PlatformPrimaryEncounterRefreshGate.Evaluate(
            cameraLow44: 0x06,
            visual07C0: 0x80,
            state03A4: 0xFF);

        Require(result.Outcome == PlatformPrimaryEncounterRefreshGateOutcome.SpecialVisualBranchSuppressesAcceptance,
            "occupied visual plus $03A4==$FF enters special visual/CHR path");
        Require(result.InvokeBank1_9915 && !result.InvokeAcceptance996C,
            "bank1 is called but acceptance is skipped");
        Require(result.TakesSpecialVisualBranch && result.Clears03A3,
            "special branch still occurs after wrapper bookkeeping");
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Primary encounter refresh-gate self-test failed: {label}");
    }
}

using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class CommonSlotActivityGateChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckActiveVisualAlwaysProcesses();
        CheckFreeOrdinarySlotSkips();
        CheckFreeReactionStillProcesses();
        CheckFreeDeathStillProcesses();
    }

    private static void CheckActiveVisualAlwaysProcesses()
    {
        var result = PlatformCommonSlotActivityGate.Evaluate(0xFD, 0x00);
        Require(result.Outcome == PlatformCommonSlotActivityOutcome.VisualActive && result.ProcessSlot,
            "visual marker != $FE enters dispatcher regardless of logical family");
    }

    private static void CheckFreeOrdinarySlotSkips()
    {
        foreach (var action in new byte[] { 0x00, 0x10, 0x30, 0x50, 0x70, 0xE0 })
        {
            var result = PlatformCommonSlotActivityGate.Evaluate(0xFE, action);
            Require(result.Outcome == PlatformCommonSlotActivityOutcome.VisualFreeInactive && result.Skipped,
                $"free visual slot skips logical action ${action:X2}");
        }
    }

    private static void CheckFreeReactionStillProcesses()
    {
        var result = PlatformCommonSlotActivityGate.Evaluate(0xFE, 0x4A);
        Require(result.Outcome == PlatformCommonSlotActivityOutcome.VisualFreeButReactionContinues
            && result.ProcessSlot,
            "free visual slot preserves $40-family cleanup");
    }

    private static void CheckFreeDeathStillProcesses()
    {
        var result = PlatformCommonSlotActivityGate.Evaluate(0xFE, 0xD7);
        Require(result.Outcome == PlatformCommonSlotActivityOutcome.VisualFreeButDeathContinues
            && result.ProcessSlot,
            "free visual slot preserves $D0-family cleanup");
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Common slot activity-gate self-test failed: {label}");
    }
}

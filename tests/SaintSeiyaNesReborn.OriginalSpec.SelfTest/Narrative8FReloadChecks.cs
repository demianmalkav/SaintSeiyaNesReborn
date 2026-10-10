using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class Narrative8FReloadChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckExactNarrativeReturnDivertsToBank0Ending();
        Check050EIsNotNormalizedOnTerminalDiversion();
        CheckColdReloadGateIsRejected();
        CheckNonzeroNarrativeSubstateIsRejected();
    }

    private static void CheckExactNarrativeReturnDivertsToBank0Ending()
    {
        var result = PlatformNarrative8FReload.ResolveNarrativeReturn(
            NarrativeReturnState(),
            persistent06AB: 0xFF,
            reloadField050E: 0x00);

        Require(result.EngineState00 == 0x3D
            && result.EngineMirror01 == 0x3D
            && result.EngineSubstate03 == 0,
            "$8F path diverts before the common state-zero commit");
        Require(result.Intermediate0533 == 0,
            "$E505[$03=$00] still produces intermediate $0533=$00 before the diversion");
        Require(result.Selector068F == 0x8F,
            "$E257 branch reaches $E20E and stores selector $068F=$8F");
        Require(result.ProgressDescriptor06CD == 0x20
            && result.StoryRoster0673 == 0x20
            && result.SaintAvailability06CC == 0x21,
            "$F38D-$F3A2 installs the ending-specific descriptor/roster/availability fields");
        Require(result.PrgBank0639 == 0x00 && result.EndingEntryCpu == 0xBC39,
            "$F3B7-$F3BC selects PRG bank 0 and tail-jumps to $BC39");
        Require(result.DivertsToBank0Ending && !result.ReturnsToMainLoopC180,
            "$8F narrative return is terminally diverted inside $F381 and never re-enters $C180");
    }

    private static void Check050EIsNotNormalizedOnTerminalDiversion()
    {
        var result = PlatformNarrative8FReload.ResolveNarrativeReturn(
            NarrativeReturnState(),
            persistent06AB: 0xFF,
            reloadField050E: 0x0F);

        Require(result.ReloadField050E == 0x0F,
            "$E214 normalization is unreachable because $F381 tail-jumps to bank 0 first");
    }

    private static void CheckColdReloadGateIsRejected()
    {
        var rejected = false;
        try
        {
            _ = PlatformNarrative8FReload.ResolveNarrativeReturn(
                NarrativeReturnState(),
                persistent06AB: 0x00,
                reloadField050E: 0x00);
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }

        Require(rejected,
            "narrative-return model does not generalize into the cold $06AB=0 reload path");
    }

    private static void CheckNonzeroNarrativeSubstateIsRejected()
    {
        var rejected = false;
        try
        {
            _ = PlatformNarrative8FReload.ResolveNarrativeReturn(
                NarrativeReturnState() with { EngineSubstate03 = 0x02 },
                persistent06AB: 0xFF,
                reloadField050E: 0x00);
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }

        Require(rejected,
            "narrative-return model requires the confirmed $72-$89 $03=$00 invariant");
    }

    private static PlatformPostExitEngineState NarrativeReturnState() =>
        new(
            EngineState00: 0x3D,
            EngineMirror01: 0x3D,
            EngineSubstate03: 0x00,
            Mode04: 0x8F,
            Scratch26: 0x00,
            Scratch27: 0x00,
            Timer57: 0x00,
            PlayerX3F: 0x00,
            PlayerY40: 0x00,
            PlayerField42: 0x00,
            PlayerAction4D: 0x20,
            PlayerAction4E: 0x20);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Narrative $8F reload self-test failed: {label}");
    }
}

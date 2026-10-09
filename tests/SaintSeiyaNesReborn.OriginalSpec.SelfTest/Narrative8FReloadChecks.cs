using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class Narrative8FReloadChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckExactNarrativeReturnCommitsStateZero();
        Check050ENormalization();
        CheckOther050EValuesArePreserved();
        CheckColdReloadGateIsRejected();
        CheckNonzeroNarrativeSubstateIsRejected();
    }

    private static void CheckExactNarrativeReturnCommitsStateZero()
    {
        var result = PlatformNarrative8FReload.ResolveNarrativeReturn(
            NarrativeReturnState(),
            persistent06AB: 0xFF,
            reloadField050E: 0x05);

        Require(result.EngineState00 == 0
            && result.EngineMirror01 == 0
            && result.EngineSubstate03 == 0,
            "$8F narrative return commits stable engine state/substate $00/$00");
        Require(result.Intermediate0533 == 0,
            "$E505[$03=$00] produces intermediate $0533=$00");
        Require(result.Selector068F == 0x8F,
            "$E257 branch reaches $E20E with A=$8F and stores selector $068F=$8F");
        Require(result.ReturnsToMainLoopC180,
            "common $E22C commit returns through fixed main-loop entry $C180");
    }

    private static void Check050ENormalization()
    {
        var result = PlatformNarrative8FReload.ResolveNarrativeReturn(
            NarrativeReturnState(),
            persistent06AB: 0xFF,
            reloadField050E: 0x0F);

        Require(result.ReloadField050E == 0x0D,
            "$E214 normalizes $050E=$0F to $0D because narrative $0533 is not $03");
    }

    private static void CheckOther050EValuesArePreserved()
    {
        var result = PlatformNarrative8FReload.ResolveNarrativeReturn(
            NarrativeReturnState(),
            persistent06AB: 0xFF,
            reloadField050E: 0x07);

        Require(result.ReloadField050E == 0x07,
            "bounded $8F branch preserves $050E values other than $0F");
    }

    private static void CheckColdReloadGateIsRejected()
    {
        var rejected = false;
        try
        {
            _ = PlatformNarrative8FReload.ResolveNarrativeReturn(
                NarrativeReturnState(),
                persistent06AB: 0x00,
                reloadField050E: 0x05);
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
                reloadField050E: 0x05);
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

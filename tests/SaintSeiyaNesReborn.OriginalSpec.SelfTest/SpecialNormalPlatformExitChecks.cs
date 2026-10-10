using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class SpecialNormalPlatformExitChecks
{
    private static readonly PlatformSaintIndex Seiya = (PlatformSaintIndex)0;
    private static readonly PlatformSaintIndex Shun = (PlatformSaintIndex)1;
    private static readonly PlatformSaintIndex Hyoga = (PlatformSaintIndex)2;
    private static readonly PlatformSaintIndex Shiryu = (PlatformSaintIndex)3;

    [ModuleInitializer]
    internal static void Run()
    {
        CheckProfilesAndProvenance();
        CheckSubstate0CPhaseReentry();
        CheckSubstate0DPhaseReentry();
        CheckSubstate0EHyogaRemap();
        CheckSubstate0FResetReentry();
        CheckSubstate10DirectCommitAndReachability();
        CheckGateRejections();
        CheckCompositionWithPrincipalDestinationModel();
    }

    private static void CheckProfilesAndProvenance()
    {
        var p0C = PlatformSpecialNormalExitPipeline.ProfileFor(0x0C);
        Require(p0C.Progression067D == 0x03
            && p0C.ReloadField050E == 0x03
            && p0C.InheritedTerminal0670 == 0x02
            && p0C.SubstateSourceAddress == 0x9DC1
            && p0C.RequiresCreation067CZero,
            "$0C profile comes from stage-3 $9D96/$9DC1 and carries $0670=$02");

        var p0D = PlatformSpecialNormalExitPipeline.ProfileFor(0x0D);
        Require(p0D.Progression067D == 0x05
            && p0D.ReloadField050E == 0x05
            && p0D.InheritedTerminal0670 == 0x02
            && p0D.SubstateWriterAddress == 0xA6E9,
            "$0D profile comes from the stage-5 $A361 special action");

        var p0E = PlatformSpecialNormalExitPipeline.ProfileFor(0x0E);
        Require(p0E.Progression067D == 0x02
            && p0E.ReloadField050E == 0x02
            && p0E.InheritedTerminal0670 == 0x02
            && p0E.SubstateWriterAddress == 0xA46A,
            "$0E profile comes from the stage-2 $A361 special action");

        var p0F = PlatformSpecialNormalExitPipeline.ProfileFor(0x0F);
        Require(p0F.Progression067D == 0x0D
            && p0F.ReloadField050E == 0x0A
            && p0F.InheritedTerminal0670 == 0x05
            && p0F.SubstateSourceAddress == 0xE4ED,
            "$0F is selected by $E4D7 table entry for progression $0D");

        var p10 = PlatformSpecialNormalExitPipeline.ProfileFor(0x10);
        Require(p10.Progression067D == 0x0C
            && p10.ReloadField050E == 0x0C
            && p10.InheritedTerminal0670 == 0x01
            && p10.SubstateSourceAddress == 0xE4EC,
            "$10 is selected by $E4D7 table entry for progression $0C");
    }

    private static void CheckSubstate0CPhaseReentry()
    {
        var result = PlatformSpecialNormalExitPipeline.ResolveAcceptedExit(
            substate: 0x0C,
            saint: Shiryu,
            playerX: 0x88,
            playerY: 0x20,
            jumpPhase: 0,
            flags0673: 0x32,
            flags06CC: 0x08);

        Require(result.HasValue, "$0C exact gate is reachable");
        var value = result.Value;
        Require(value.Disposition == PlatformSpecialNormalExitDisposition.ReenterInteractiveSelector
            && value.Profile.Progression067D == 0x03
            && value.CanonicalSaint0533 == 0x03
            && value.ReloadField050E == 0x03
            && value.Field067C == 0x01
            && value.Field06B8 == 0x00
            && value.Terminal0670 == 0x00,
            "$0C reload consumes inherited $02, increments $067C and reaches selector");
        Require(value.SelectorState == PlatformWarmReloadInteractiveState.SeedNormalWarmReload(),
            "$0C reaches the exact selector seed from #102");
    }

    private static void CheckSubstate0DPhaseReentry()
    {
        var result = PlatformSpecialNormalExitPipeline.ResolveAcceptedExit(
            substate: 0x0D,
            saint: Seiya,
            playerX: 0xB4,
            playerY: 0x30,
            jumpPhase: 0);

        Require(result.HasValue, "$0D exact gate is reachable");
        var value = result.Value;
        Require(value.Disposition == PlatformSpecialNormalExitDisposition.ReenterInteractiveSelector
            && value.Profile.Progression067D == 0x05
            && value.ReloadField050E == 0x05
            && value.Field067C == 0x01
            && value.Terminal0670 == 0,
            "$0D reload increments the one-shot $067C phase and reenters stage 5 selector");
    }

    private static void CheckSubstate0EHyogaRemap()
    {
        var ordinary = PlatformSpecialNormalExitPipeline.ResolveAcceptedExit(
            substate: 0x0E,
            saint: Seiya,
            playerX: 0xB4,
            playerY: 0x80,
            jumpPhase: 0);
        Require(ordinary.HasValue
            && ordinary.Value.ReloadField050E == 0x02
            && ordinary.Value.Field067C == 0x01
            && ordinary.Value.Field06B8 == 0,
            "$0E ordinary Saint stays on stage $02 after $ED57");

        var hyoga = PlatformSpecialNormalExitPipeline.ResolveAcceptedExit(
            substate: 0x0E,
            saint: Hyoga,
            playerX: 0xB4,
            playerY: 0x80,
            jumpPhase: 0);
        Require(hyoga.HasValue, "$0E Hyoga exit is accepted");
        Require(hyoga.Value.CanonicalSaint0533 == 0x01
            && hyoga.Value.ReloadField050E == 0x08
            && hyoga.Value.Field067C == 0x01
            && hyoga.Value.Field06B8 == 0x0A,
            "$ED57 remaps stage-2 canonical Hyoga to temporary $050E=$08/$06B8=$0A");
    }

    private static void CheckSubstate0FResetReentry()
    {
        var result = PlatformSpecialNormalExitPipeline.ResolveAcceptedExit(
            substate: 0x0F,
            saint: Hyoga,
            playerX: 0xB4,
            playerY: 0x40,
            jumpPhase: 0,
            flags0673: 0x3E,
            flags06CC: 0x02);

        Require(result.HasValue, "$0F exact gate is reachable");
        var value = result.Value;
        Require(value.Disposition == PlatformSpecialNormalExitDisposition.ReenterInteractiveSelector
            && value.Profile.Progression067D == 0x0D
            && value.ReloadField050E == 0x0A
            && value.Field067C == 0x00
            && value.Field06B8 == 0x00
            && value.Terminal0670 == 0x00,
            "$0F inherited $05 takes $ED57->$A973 and reenters the Saga selector cleanly");
    }

    private static void CheckSubstate10DirectCommitAndReachability()
    {
        var seiya = PlatformSpecialNormalExitPipeline.ResolveAcceptedExit(
            substate: 0x10,
            saint: Seiya,
            playerX: 0xB4,
            playerY: 0x70,
            jumpPhase: 0);

        Require(seiya.HasValue, "$10 Seiya exit is accepted");
        var value = seiya.Value;
        Require(value.Disposition == PlatformSpecialNormalExitDisposition.CommitStableState
            && value.SelectorState is null
            && value.StableResult.HasValue,
            "$10 does not reopen the interactive selector");

        var stable = value.StableResult!.Value;
        Require(stable.EngineState00 == 0x00
            && stable.EngineMirror01 == 0x00
            && stable.EngineSubstate03 == 0x00
            && stable.Terminal0670 == 0x05
            && stable.Progression067D == 0x0C
            && stable.CanonicalSaint0533 == 0x00
            && stable.ReloadField050E == 0x0C
            && stable.Flags0673 == 0x3E
            && stable.Flags06CC == 0x01
            && stable.ProgressionCode06CD == 0x0E
            && stable.Selector068F == 0x00,
            "$10 Seiya inherited $01 is consumed by $E26A and commits the exact state-$00 result");

        var shun = PlatformSpecialNormalExitPipeline.ResolveAcceptedExit(
            substate: 0x10,
            saint: Shun,
            playerX: 0xB4,
            playerY: 0x70,
            jumpPhase: 0);
        Require(shun is null,
            "$10 provenance can create Shun but the physical exit gate rejects internal index 1");

        var hyoga = PlatformSpecialNormalExitPipeline.ResolveAcceptedExit(
            substate: 0x10,
            saint: Hyoga,
            playerX: 0xB4,
            playerY: 0x70,
            jumpPhase: 0);
        Require(hyoga is null,
            "$10 normal progression cannot create Hyoga even though the coordinate gate alone would not reject him");
    }

    private static void CheckGateRejections()
    {
        Require(PlatformSpecialNormalExitPipeline.ResolveAcceptedExit(
                0x0C, Seiya, 0x87, 0x20, 0) is null,
            "$0C rejects X below $88");
        Require(PlatformSpecialNormalExitPipeline.ResolveAcceptedExit(
                0x0D, Seiya, 0xB4, 0x31, 0) is null,
            "$0D requires exact Y=$30");
        Require(PlatformSpecialNormalExitPipeline.ResolveAcceptedExit(
                0x0E, Seiya, 0xB4, 0x80, 1) is null,
            "all special-normal exits reject a nonzero jump phase");

        var rejected = false;
        try
        {
            _ = PlatformSpecialNormalExitPipeline.ProfileFor(0x11);
        }
        catch (ArgumentOutOfRangeException)
        {
            rejected = true;
        }
        Require(rejected, "$11 is deliberately outside this normal-exit model");
    }

    private static void CheckCompositionWithPrincipalDestinationModel()
    {
        var exit0C = PlatformSpecialNormalExitPipeline.ResolveAcceptedExit(
            0x0C, Seiya, 0x88, 0x20, 0, flags0673: 0x32, flags06CC: 0x00)!.Value;

        var afterRelease02 = PlatformNormalWarmReloadDestination.ResolvePrincipal(
            new PlatformNormalWarmReloadInput(
                Terminal0670: 0x02,
                Progression067D: exit0C.Profile.Progression067D,
                CanonicalSaint0533: exit0C.CanonicalSaint0533,
                ReloadField050E: exit0C.ReloadField050E,
                StoryPhase06CE: 0,
                Flags0673: exit0C.Flags0673,
                Flags06CC: exit0C.Flags06CC,
                ProgressionCode06CD: exit0C.Profile.ProgressionCode06CD));
        Require(afterRelease02.EngineState00 == 0x00
            && afterRelease02.Progression067D == 0x03
            && afterRelease02.ReloadField050E == 0x03,
            "$0C selector release $02 composes directly with the already-closed #104 destination model");

        var exit0EHyoga = PlatformSpecialNormalExitPipeline.ResolveAcceptedExit(
            0x0E, Hyoga, 0xB4, 0x80, 0)!.Value;
        var afterDd = PlatformNormalWarmReloadDestination.ResolvePrincipal(
            new PlatformNormalWarmReloadInput(
                Terminal0670: 0xDD,
                Progression067D: exit0EHyoga.Profile.Progression067D,
                CanonicalSaint0533: exit0EHyoga.CanonicalSaint0533,
                ReloadField050E: exit0EHyoga.ReloadField050E,
                StoryPhase06CE: 0,
                Flags0673: 0x30,
                Flags06CC: 0,
                ProgressionCode06CD: exit0EHyoga.Profile.ProgressionCode06CD));
        Require(afterDd.EngineState00 == 0x90
            && afterDd.ReloadField050E == 0x08,
            "$0E Hyoga temporary stage $08 is preserved on the existing $DD -> $90 branch");

        var exit0F = PlatformSpecialNormalExitPipeline.ResolveAcceptedExit(
            0x0F, Seiya, 0xB4, 0x40, 0)!.Value;
        var sagaPhase = PlatformNormalWarmReloadDestination.ResolvePrincipal(
            new PlatformNormalWarmReloadInput(
                Terminal0670: 0xFF,
                Progression067D: exit0F.Profile.Progression067D,
                CanonicalSaint0533: exit0F.CanonicalSaint0533,
                ReloadField050E: exit0F.ReloadField050E,
                StoryPhase06CE: 0x01,
                Flags0673: 0x30,
                Flags06CC: 0,
                ProgressionCode06CD: exit0F.Profile.ProgressionCode06CD));
        Require(sagaPhase.Disposition == PlatformNormalWarmReloadDisposition.ReenterInteractiveSelector,
            "$0F feeds the already-closed stage-$0A Saga phase-reentry branch when $06CE becomes nonzero");
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Special normal platform exit self-test failed: {label}");
    }
}

using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class NormalWarmReloadDestinationChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckPrincipalReleaseSetExcludesStage0DTalk04();
        CheckRelease02CommitsStateZeroWithoutAdvancing();
        CheckReleaseDdCommitsState90();
        CheckOrdinaryReleaseFfCommitsStateZero();
        CheckReleaseFfCompletionMaskCommitsState90();
        CheckReleaseFfSagaPhaseReentersSelector();
        CheckRelease01AdvancesAndCommitsState10();
        CheckReleaseFeForcesSaintZeroBeforeAdvance();
        CheckProgression0CBranchUsesOld0673Bit0();
        CheckProgression0DAnd0ECommitStateZeroWith0670Five();
        CheckFf0FNormalizationDependsOnCanonicalSaint3();
        CheckImpossiblePhaseCombinationIsRejected();
    }

    private static void CheckPrincipalReleaseSetExcludesStage0DTalk04()
    {
        Require(PlatformNormalWarmReloadDestination.IsPrincipalInteractiveRelease(0x01)
            && PlatformNormalWarmReloadDestination.IsPrincipalInteractiveRelease(0x02)
            && PlatformNormalWarmReloadDestination.IsPrincipalInteractiveRelease(0xDD)
            && PlatformNormalWarmReloadDestination.IsPrincipalInteractiveRelease(0xFE)
            && PlatformNormalWarmReloadDestination.IsPrincipalInteractiveRelease(0xFF),
            "principal post-loop release set includes 01/02/DD/FE/FF");
        Require(!PlatformNormalWarmReloadDestination.IsPrincipalInteractiveRelease(0x04),
            "$04 belongs to the $050E=$0D Talk context, not principal $067D->$F016 progression");
    }

    private static void CheckRelease02CommitsStateZeroWithoutAdvancing()
    {
        var result = PlatformNormalWarmReloadDestination.ResolvePrincipal(
            Input(terminal0670: 0x02, progression067D: 0x03, saint0533: 0x01));

        RequireCommitted(result, 0x00);
        Require(result.Progression067D == 0x03 && result.ReloadField050E == 0x03,
            "$0670=$02 does not advance progression and remaps $050E from current $067D");
        Require(result.CanonicalSaint0533 == 0x01 && result.EngineSubstate03 == 0x02,
            "common commit maps canonical Hyoga index 1 back to internal $03=2 through $E505");
        Require(result.Terminal0670 == 0x02,
            "$0670=$02 remains the post-loop transition value through the state-zero commit");
    }

    private static void CheckReleaseDdCommitsState90()
    {
        var result = PlatformNormalWarmReloadDestination.ResolvePrincipal(
            Input(terminal0670: 0xDD, progression067D: 0x09, saint0533: 0x03, reloadField050E: 0x07));

        RequireCommitted(result, 0x90);
        Require(result.Flags0673 == 0x3F,
            "$E417 forces $0673=$3F before the $90 handoff");
        Require(result.Selector068F == 0xDD,
            "$E187 stores $068F=$DD before committing engine state $90");
        Require(result.EngineSubstate03 == 0x03,
            "canonical/internal Shiryu index 3 survives the common commit");
    }

    private static void CheckOrdinaryReleaseFfCommitsStateZero()
    {
        var result = PlatformNormalWarmReloadDestination.ResolvePrincipal(
            Input(
                terminal0670: 0xFF,
                progression067D: 0x04,
                saint0533: 0x02,
                reloadField050E: 0x04,
                flags0673: 0x30,
                flags06CC: 0x00));

        RequireCommitted(result, 0x00);
        Require(result.ReloadField050E == 0x04,
            "ordinary $FF remaps $050E from current progression before commit");
        Require(result.Flags06CC == 0x04,
            "$E200-$E206 stores the canonical Saint bit from $FFC0[$0533] into $06CC");
        Require(result.Selector068F == 0x00,
            "ordinary non-completion $FF path stores $068F=0 at $E1F8");
        Require(result.EngineSubstate03 == 0x01,
            "canonical Shun index 2 maps back to internal $03=1");
    }

    private static void CheckReleaseFfCompletionMaskCommitsState90()
    {
        var result = PlatformNormalWarmReloadDestination.ResolvePrincipal(
            Input(
                terminal0670: 0xFF,
                progression067D: 0x09,
                saint0533: 0x00,
                reloadField050E: 0x07,
                flags0673: 0x37,
                flags06CC: 0x08));

        RequireCommitted(result, 0x90);
        Require(result.Selector068F == 0xDD,
            "completion mask gate joins $E187 and therefore stores $068F=$DD");
        Require(result.Flags0673 == 0x37,
            "completion-mask entry at $E187 does not execute the separate $E417 $0673=$3F write");
    }

    private static void CheckReleaseFfSagaPhaseReentersSelector()
    {
        var result = PlatformNormalWarmReloadDestination.ResolvePrincipal(
            Input(
                terminal0670: 0xFF,
                progression067D: 0x0D,
                saint0533: 0x00,
                reloadField050E: 0x0A,
                storyPhase06CE: 0x01));

        Require(result.Disposition == PlatformNormalWarmReloadDisposition.ReenterInteractiveSelector,
            "stage-$0A $FF with nonzero $06CE returns to $E327 instead of committing a stable state");
        Require(result.EngineState00 is null
            && result.EngineMirror01 is null
            && result.EngineSubstate03 is null,
            "selector reentry has no $E22C stable-state commit yet");
        Require(result.Terminal0670 == 0x00,
            "$E327 reseeds the next Saga interactive phase with $0670=0");
        Require(result.Progression067D == 0x0D && result.StoryPhase06CE == 0x01,
            "phase-local Saga reentry does not advance the global progression index or clear $06CE");
    }

    private static void CheckRelease01AdvancesAndCommitsState10()
    {
        var result = PlatformNormalWarmReloadDestination.ResolvePrincipal(
            Input(terminal0670: 0x01, progression067D: 0x04, saint0533: 0x04));

        RequireCommitted(result, 0x10);
        Require(result.Progression067D == 0x05 && result.ReloadField050E == 0x05,
            "$0670=$01 increments $067D then maps the new progression through $F016");
        Require(result.CanonicalSaint0533 == 0x00 && result.EngineSubstate03 == 0x00,
            "ordinary progression exit remaps canonical Ikki index 4 to Seiya before the commit");
        Require(result.Flags06CC == 0x00 && result.Flags0673 == 0x32,
            "progression advance clears $06CC and rebuilds $0673 from $E50B[new $067D] | $30");
        Require(result.ProgressionCode06CD == 0x02,
            "new progression 5 stores $E50B[5]=$02 into $06CD");
    }

    private static void CheckReleaseFeForcesSaintZeroBeforeAdvance()
    {
        var result = PlatformNormalWarmReloadDestination.ResolvePrincipal(
            Input(terminal0670: 0xFE, progression067D: 0x09, saint0533: 0x03));

        RequireCommitted(result, 0x10);
        Require(result.Terminal0670 == 0x01,
            "$FE path is normalized to progression release $01 at $E40F-$E411");
        Require(result.Progression067D == 0x0A && result.CanonicalSaint0533 == 0x00,
            "$FE path forces $0533=0 before sharing the ordinary progression-advance body");
        Require(result.EngineSubstate03 == 0x00,
            "forced canonical Saint zero commits internal $03=0");
    }

    private static void CheckProgression0CBranchUsesOld0673Bit0()
    {
        var bitClear = PlatformNormalWarmReloadDestination.ResolvePrincipal(
            Input(
                terminal0670: 0x01,
                progression067D: 0x0B,
                saint0533: 0x01,
                flags0673: 0x30));

        RequireCommitted(bitClear, 0x10);
        Require(bitClear.Progression067D == 0x0C
            && bitClear.CanonicalSaint0533 == 0x00
            && bitClear.ProgressionCode06CD == 0x0E
            && bitClear.Flags0673 == 0x3E,
            "new $067D=$0C with old $0673 bit0 clear selects canonical Saint 0 and code $0E");

        var bitSet = PlatformNormalWarmReloadDestination.ResolvePrincipal(
            Input(
                terminal0670: 0x01,
                progression067D: 0x0B,
                saint0533: 0x00,
                flags0673: 0x31));

        RequireCommitted(bitSet, 0x10);
        Require(bitSet.CanonicalSaint0533 == 0x02
            && bitSet.EngineSubstate03 == 0x01
            && bitSet.ProgressionCode06CD == 0x0B
            && bitSet.Flags0673 == 0x3B,
            "$E3C2 BIT $FFC0 tests old $0673 bit0 and selects canonical Saint 2/code $0B when set");
    }

    private static void CheckProgression0DAnd0ECommitStateZeroWith0670Five()
    {
        var into0D = PlatformNormalWarmReloadDestination.ResolvePrincipal(
            Input(terminal0670: 0x01, progression067D: 0x0C, saint0533: 0x01));
        RequireCommitted(into0D, 0x00);
        Require(into0D.Progression067D == 0x0D
            && into0D.ReloadField050E == 0x0A
            && into0D.Terminal0670 == 0x05,
            "advance into $067D=$0D maps to stage $0A and commits state zero with $0670=$05");

        var into0E = PlatformNormalWarmReloadDestination.ResolvePrincipal(
            Input(terminal0670: 0x01, progression067D: 0x0D, saint0533: 0x02));
        RequireCommitted(into0E, 0x00);
        Require(into0E.Progression067D == 0x0E
            && into0E.ReloadField050E == 0x00
            && into0E.Terminal0670 == 0x05,
            "advance into $067D=$0E maps through $F016[$0E]=$00 and shares the terminal state-zero branch");
    }

    private static void CheckFf0FNormalizationDependsOnCanonicalSaint3()
    {
        var normalized = PlatformNormalWarmReloadDestination.ResolvePrincipal(
            Input(
                terminal0670: 0xFF,
                progression067D: 0x06,
                saint0533: 0x00,
                reloadField050E: 0x0F));
        RequireCommitted(normalized, 0x00);
        Require(normalized.ReloadField050E == 0x0D,
            "$E214 rewrites mapped $050E=$0F to $0D when canonical $0533!=3");

        var preserved = PlatformNormalWarmReloadDestination.ResolvePrincipal(
            Input(
                terminal0670: 0xFF,
                progression067D: 0x06,
                saint0533: 0x03,
                reloadField050E: 0x0F));
        RequireCommitted(preserved, 0x00);
        Require(preserved.ReloadField050E == 0x0F,
            "$E214 preserves $050E=$0F for canonical Saint index 3");
    }

    private static void CheckImpossiblePhaseCombinationIsRejected()
    {
        var rejected = false;
        try
        {
            _ = PlatformNormalWarmReloadDestination.ResolvePrincipal(
                Input(
                    terminal0670: 0x02,
                    progression067D: 0x0D,
                    saint0533: 0,
                    reloadField050E: 0x0A,
                    storyPhase06CE: 0x01));
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }

        Require(rejected,
            "writer audit rejects non-$FF principal releases paired with nonzero Saga phase $06CE");
    }

    private static PlatformNormalWarmReloadInput Input(
        byte terminal0670,
        byte progression067D,
        byte saint0533,
        byte reloadField050E = 0,
        byte storyPhase06CE = 0,
        byte flags0673 = 0x30,
        byte flags06CC = 0) =>
        new(
            Terminal0670: terminal0670,
            Progression067D: progression067D,
            CanonicalSaint0533: saint0533,
            ReloadField050E: reloadField050E,
            StoryPhase06CE: storyPhase06CE,
            Flags0673: flags0673,
            Flags06CC: flags06CC);

    private static void RequireCommitted(PlatformNormalWarmReloadResult result, byte expectedState)
    {
        Require(result.Disposition == PlatformNormalWarmReloadDisposition.CommitStableState,
            "result reaches the common stable-state commit");
        Require(result.EngineState00 == expectedState && result.EngineMirror01 == expectedState,
            $"common commit writes $00/$01=${expectedState:X2}");
        Require(result.EngineSubstate03.HasValue,
            "stable commit maps final canonical $0533 back to internal $03");
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Normal warm reload destination self-test failed: {label}");
    }
}

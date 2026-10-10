using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class PlatformResourceFailureTransitionChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckFatalLifeThresholdAndOrderedCosmoDrain();
        CheckNonFatalExactLifeDepletion();
        CheckPeriodicLifeFailureWithoutConsumingTick();
        CheckFatalCosmoThreshold();
        CheckFailureTimerAndOnlyReachableFamilyState();
        CheckReloadFfMarksSaintAndCommits00();
        CheckReloadFfCompletionCommits90();
        CheckReloadFfSagaPhaseReentersSelector();
        CheckReloadFfStage0ARedirect();
        CheckReloadFfStage5PresentationState();
        CheckIkkiMaskDoesNotCompleteFourPersistentSaints();
    }

    private static void CheckFatalLifeThresholdAndOrderedCosmoDrain()
    {
        var result = PlatformResourceFailureTransition.ApplyResourceFrame(
            PlatformSaintIndex.Seiya,
            engineSubstate02: 0,
            frameCounter3C: 1,
            new ContactDrainState(LifeTicks: 1, CosmoTicks: 1),
            life: 1,
            cosmo: 5);

        Require(result.Cause == PlatformResourceFailureCause.Life,
            "Life 1 under subtract-two tick is fatal");
        Require(result.Life == 0 && result.Cosmo == 4,
            "Cosmo consumer still runs after Life already commits state $60");
        Require(result.DrainCounters == new ContactDrainState(0, 0),
            "ordinary fatal frame consumes both pending drain ticks");
        Require(result.EngineState00 == 0x60
            && result.EngineMirror01 == 0x60
            && result.FailureTimer4D == 0xD0,
            "fatal resource frame seeds exact state-$60 entry");
    }

    private static void CheckNonFatalExactLifeDepletion()
    {
        var result = PlatformResourceFailureTransition.ApplyResourceFrame(
            PlatformSaintIndex.Seiya,
            0,
            1,
            new ContactDrainState(1, 0),
            life: 2,
            cosmo: 10);

        Require(result.Cause == PlatformResourceFailureCause.None
            && result.Life == 0
            && result.EngineState00 is null,
            "Life exactly 2 becomes zero but does not underflow until a later subtract-two attempt");
    }

    private static void CheckPeriodicLifeFailureWithoutConsumingTick()
    {
        var result = PlatformResourceFailureTransition.ApplyResourceFrame(
            PlatformSaintIndex.Shun,
            engineSubstate02: 0x10,
            frameCounter3C: 0x08,
            new ContactDrainState(LifeTicks: 0, CosmoTicks: 0),
            life: 1,
            cosmo: 10);

        Require(result.Cause == PlatformResourceFailureCause.Life
            && result.Life == 0
            && result.DrainCounters.LifeTicks == 0,
            "$02=$10 periodic Shun drain can kill without consuming $7F");
    }

    private static void CheckFatalCosmoThreshold()
    {
        var result = PlatformResourceFailureTransition.ApplyResourceFrame(
            PlatformSaintIndex.Hyoga,
            0,
            1,
            new ContactDrainState(LifeTicks: 0, CosmoTicks: 1),
            life: 50,
            cosmo: 0);

        Require(result.Cause == PlatformResourceFailureCause.Cosmo
            && result.Cosmo == 0
            && result.DrainCounters.CosmoTicks == 0
            && result.EngineState00 == 0x60,
            "Cosmo subtract-one underflow at zero enters state $60");
    }

    private static void CheckFailureTimerAndOnlyReachableFamilyState()
    {
        Require(PlatformResourceFailureTransition.IsReachableFailureFamilyState(0x60),
            "$60 is reachable failure-family state");
        Require(!PlatformResourceFailureTransition.IsReachableFailureFamilyState(0x61)
            && !PlatformResourceFailureTransition.IsReachableFailureFamilyState(0x6F),
            "$61-$6F are not reachable from the fatal platform subgraph");

        var first = PlatformResourceFailureTransition.AdvanceFailureFrame(
            frameCounter3C: 0x00,
            failureTimer4D: 0xD0,
            failureMirror4E: 0x22,
            field4F: 0x70,
            field40: 0x20);
        Require(!first.ExitToReloadFf
            && first.FailureTimer4D == 0xD1
            && first.FailureMirror4E == 0xD1
            && first.Field40 == 0x21
            && first.EngineState00 == 0x60,
            "qualified state-$60 frame advances $4D/$4E and presentation field $40");

        var terminal = PlatformResourceFailureTransition.AdvanceFailureFrame(
            frameCounter3C: 0x10,
            failureTimer4D: 0xDF,
            failureMirror4E: 0xDF,
            field4F: 0x90,
            field40: 0x80);
        Require(terminal.ExitToReloadFf
            && terminal.FailureTimer4D == 0xDF
            && terminal.FailureMirror4E == 0xDF
            && terminal.ReloadMode04 == 0xFF
            && terminal.EngineState00 == 0x3D
            && terminal.EngineMirror01 == 0x3D,
            "$DF->$E0 threshold jumps to mode-$FF reload before storing $E0");
    }

    private static void CheckReloadFfMarksSaintAndCommits00()
    {
        var result = PlatformResourceFailureTransition.ResolveReloadFf(
            new PlatformFailureReloadInput(
                Saint: PlatformSaintIndex.Hyoga,
                Progression067D: 0x03,
                ReloadField050E: 0x03,
                StoryPhase06CE: 0,
                Field06B8: 0,
                Flags0673: 0x01,
                Flags06CC: 0,
                ProgressionCode06CD: 0x02));

        // Internal Hyoga index 2 maps to canonical index 1 => mask $02.
        Require(result.UpdatedFlags0673 == 0x03,
            "mode-$FF prelude marks canonical Hyoga in $0673");
        Require(result.Destination.Disposition == PlatformNormalWarmReloadDisposition.CommitStableState
            && result.Destination.EngineState00 == 0x00
            && result.Destination.EngineMirror01 == 0x00
            && result.Destination.CanonicalSaint0533 == 0x01,
            "non-complete fatal mask returns through existing terminal-$FF stable state $00");
    }

    private static void CheckReloadFfCompletionCommits90()
    {
        var result = PlatformResourceFailureTransition.ResolveReloadFf(
            new PlatformFailureReloadInput(
                Saint: PlatformSaintIndex.Seiya,
                Progression067D: 0x08,
                ReloadField050E: 0x10,
                StoryPhase06CE: 0,
                Field06B8: 0,
                Flags0673: 0x0E,
                Flags06CC: 0,
                ProgressionCode06CD: 0));

        Require((result.UpdatedFlags0673 & 0x0F) == 0x0F,
            "fatal Seiya fills the fourth persistent-Saint bit");
        Require(result.Destination.EngineState00 == 0x90
            && result.Destination.EngineMirror01 == 0x90
            && result.Destination.Selector068F == 0xDD,
            "four-Saint completion gate commits stable state $90");
    }

    private static void CheckReloadFfSagaPhaseReentersSelector()
    {
        var result = PlatformResourceFailureTransition.ResolveReloadFf(
            new PlatformFailureReloadInput(
                Saint: PlatformSaintIndex.Shiryu,
                Progression067D: 0x0D,
                ReloadField050E: 0x0A,
                StoryPhase06CE: 1,
                Field06B8: 0,
                Flags0673: 0x03,
                Flags06CC: 0,
                ProgressionCode06CD: 0x0E));

        Require(result.Destination.Disposition == PlatformNormalWarmReloadDisposition.ReenterInteractiveSelector
            && result.Destination.EngineState00 is null
            && result.Destination.Terminal0670 == 0,
            "nonzero Saga phase hands failure back to the already-promoted interactive selector");
    }

    private static void CheckReloadFfStage0ARedirect()
    {
        var result = PlatformResourceFailureTransition.ResolveReloadFf(
            new PlatformFailureReloadInput(
                Saint: PlatformSaintIndex.Hyoga,
                Progression067D: 0x0D,
                ReloadField050E: 0x0A,
                StoryPhase06CE: 0,
                Field06B8: 0x0A,
                Flags0673: 0,
                Flags06CC: 0,
                ProgressionCode06CD: 0x0E));

        Require(result.SwitchedToCanonicalSeiya
            && result.Progression067D == 0x02
            && result.CanonicalSaint0533 == 0x00,
            "stage-$0A/$06B8 special failure redirects progression and active Saint");
        Require(result.Destination.EngineState00 == 0x00
            && result.Destination.Progression067D == 0x02
            && result.Destination.ReloadField050E == 0x02,
            "redirected failure commits state $00 at progression/stage 2");
    }

    private static void CheckReloadFfStage5PresentationState()
    {
        var result = PlatformResourceFailureTransition.ResolveReloadFf(
            new PlatformFailureReloadInput(
                Saint: PlatformSaintIndex.Seiya,
                Progression067D: 0x05,
                ReloadField050E: 0x05,
                StoryPhase06CE: 0,
                Field06B8: 0,
                Flags0673: 0,
                Flags06CC: 0,
                ProgressionCode06CD: 0x02));

        Require(result.Mode04AfterPrelude == 0
            && result.SavedFailureStage067E == 0x05,
            "stage-5 failure branch records $067E and clears mode $04 before common destination logic");
    }

    private static void CheckIkkiMaskDoesNotCompleteFourPersistentSaints()
    {
        var result = PlatformResourceFailureTransition.ResolveReloadFf(
            new PlatformFailureReloadInput(
                Saint: PlatformSaintIndex.Ikki,
                Progression067D: 0x04,
                ReloadField050E: 0x04,
                StoryPhase06CE: 0,
                Field06B8: 0,
                Flags0673: 0x0E,
                Flags06CC: 0,
                ProgressionCode06CD: 0x02));

        Require(result.UpdatedFlags0673 == 0x1E,
            "Ikki contributes mask $10 outside the four-Saint completion low nibble");
        Require(result.Destination.EngineState00 == 0x00,
            "Ikki defeat alone cannot satisfy the low-nibble four-Saint completion gate");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

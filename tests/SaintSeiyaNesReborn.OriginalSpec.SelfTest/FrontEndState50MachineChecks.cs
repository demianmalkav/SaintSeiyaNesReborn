using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;

internal static class FrontEndState50MachineChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckEntryModes();
        CheckIntroProgressionAndStartSkip();
        CheckReadySelectionAndAttractHandoff();
        CheckCommitLane();
        CheckPasswordAcceptance();
        CheckExecutableChrOverlays();
        CheckAttractGraphComposition();
    }

    private static void CheckEntryModes()
    {
        var cold = FrontEndState50Machine.Enter(FrontEnd50EntryKind.ColdReset);
        var completed = FrontEndState50Machine.Enter(FrontEnd50EntryKind.AttractCompletion);
        var escape = FrontEndState50Machine.Enter(FrontEnd50EntryKind.AttractStartEscape);

        Require(cold.EngineState00 == 0x50 && cold.EngineMirror01 == 0x50
            && cold.ModalStep0200 == 0x00 && cold.MainLane0201 == 0x00 && cold.Branch0202 == 0,
            "cold reset enters paired engine $50 and seeds modal step $00");
        Require(completed.ModalStep0200 == 0x00,
            "completed attract loop re-enters the same front-end step $00");
        Require(escape.ModalStep0200 == 0x05,
            "Start escape from attract presentation enters the ready front-end step $05");
        Require(cold.LoadsChrExecutableOverlay && escape.LoadsChrExecutableOverlay,
            "front-end initialization uses the CHR-backed executable RAM overlay");
        Require(FrontEndState50Machine.ReachableModalSteps.SequenceEqual(
            new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }),
            "all and only modal substates $00-$09 are reachable");
    }

    private static void CheckIntroProgressionAndStartSkip()
    {
        var s0Stay = FrontEndState50Machine.StepIntro0(0x01, 0xEF, false);
        var s0Advance = FrontEndState50Machine.StepIntro0(0x02, 0xEF, false);
        Require(s0Stay.ModalStep0200 == 0x00 && s0Advance.ModalStep0200 == 0x01,
            "$00 advances only after the proven frame/scroll gate");

        var s1 = FrontEndState50Machine.StepIntro1(0x03, false);
        var s2 = FrontEndState50Machine.StepIntro2(true, false);
        var s3 = FrontEndState50Machine.StepIntro3(0x80, false);
        var s4 = FrontEndState50Machine.StepIntro4(false);
        Require(s1.ModalStep0200 == 0x02 && s2.ModalStep0200 == 0x03
            && s3.ModalStep0200 == 0x04 && s4.ModalStep0200 == 0x05,
            "intro path closes as $00->$01->$02->$03->$04->$05");

        var skipFrom0 = FrontEndState50Machine.StepIntro0(0x00, 0x00, true);
        var skipFrom3 = FrontEndState50Machine.StepIntro3(0x00, true);
        Require(skipFrom0.ModalStep0200 == 0x05 && skipFrom3.ModalStep0200 == 0x05,
            "new Start press redirects intro substates $00-$04 to ready step $05");

        var sameNmiStart = FrontEndState50Machine.StepIntro4(true);
        Require(sameNmiStart.ModalStep0200 == 0x07
            && sameNmiStart.Disposition == FrontEnd50Disposition.EnterSelectionCommit,
            "state $04 locally becomes $05 before common Start routing, so same-NMI Start reaches $07");
    }

    private static void CheckReadySelectionAndAttractHandoff()
    {
        var idle = FrontEndState50Machine.StepReady5(false, false, false, false);
        var down = FrontEndState50Machine.StepReady5(false, true, false, false);
        var upWins = FrontEndState50Machine.StepReady5(true, true, false, false);
        Require(idle.ModalStep0200 == 0x05 && idle.Branch0202 == 0,
            "ready state defaults $0202 to branch zero every observing NMI");
        Require(down.Branch0202 == 1,
            "new Down edge selects branch one for the current ready-state sample");
        Require(upWins.Branch0202 == 0,
            "new Up edge has priority and selects branch zero");

        var startNormal = FrontEndState50Machine.StepReady5(false, false, true, false);
        var startPassword = FrontEndState50Machine.StepReady5(false, true, true, false);
        Require(startNormal.ModalStep0200 == 0x07 && startNormal.Branch0202 == 0,
            "ready+Start commits branch zero through modal state $07");
        Require(startPassword.ModalStep0200 == 0x07 && startPassword.Branch0202 == 1,
            "Down+Start in the same input sample commits password branch one through $07");

        var attract = FrontEndState50Machine.StepReady5(false, false, false, true);
        Require(attract.ModalStep0200 == 0x06
            && attract.Disposition == FrontEnd50Disposition.EnterAttractHandoff,
            "scripted front-end object event promotes ready step $05 to attract handoff $06");

        var cancelledAttract = FrontEndState50Machine.StepReady5(false, false, true, true);
        Require(cancelledAttract.ModalStep0200 == 0x05,
            "common Start routing runs after the scripted $05->$06 event and maps simultaneous $06+Start back to $05");
    }

    private static void CheckCommitLane()
    {
        var attract = FrontEndState50Machine.CommitFromNmi(0x06);
        Require(attract.Disposition == FrontEnd50Disposition.ExitToAttractState30
            && attract.MainLane0201 == 1
            && attract.EngineState00 == 0x30 && attract.EngineMirror01 == 0x30,
            "modal $06 switches to commit lane and writes paired global state $30");

        var normal = FrontEndState50Machine.CommitFromNmi(0x07, 0);
        Require(normal.Disposition == FrontEnd50Disposition.ExitToState10
            && normal.EngineState00 == 0x10 && normal.EngineMirror01 == 0x10,
            "modal $07 branch zero exits to the already-promoted state-$10 path");

        var password = FrontEndState50Machine.CommitFromNmi(0x07, 1);
        Require(password.Disposition == FrontEnd50Disposition.EnterPasswordEntry
            && password.EngineState00 == 0x50 && password.EngineMirror01 == 0x50
            && password.ModalStep0200 == 0x09
            && password.LoadsChrExecutableOverlay
            && password.InitializesPasswordWorkspace,
            "modal $07 branch one loads the password CHR overlay and enters $50:$09");

        var accepted = FrontEndState50Machine.CommitFromNmi(0x08, 1);
        Require(accepted.Disposition == FrontEnd50Disposition.ExitToRestoredState10
            && accepted.EngineState00 == 0x10 && accepted.EngineMirror01 == 0x10
            && accepted.RestoresDecodedProgression,
            "modal $08 exits to state $10 through decoded-progression restoration");
    }

    private static void CheckPasswordAcceptance()
    {
        var invalid = FrontEndState50Machine.StepPassword9(false);
        var valid = FrontEndState50Machine.StepPassword9(true);
        Require(invalid.ModalStep0200 == 0x09 && invalid.EngineState00 == 0x50,
            "invalid password remains in the state-$50 password editor");
        Require(valid.ModalStep0200 == 0x08
            && valid.Disposition == FrontEnd50Disposition.EnterPasswordAccepted,
            "decoder success promotes password editor $09 to accepted state $08");
    }

    private static void CheckExecutableChrOverlays()
    {
        Require(FrontEndState50Machine.ExecutableOverlayChrBank == 0x1F
            && FrontEndState50Machine.ExecutableOverlayRamStart == 0x0440
            && FrontEndState50Machine.ExecutableOverlayLength == 0x03C0,
            "front-end uses CHR bank 31 as a $3C0-byte executable overlay copied to RAM $0440-$07FF");
        Require(FrontEndState50Machine.FrontEndOverlayPpuSource == 0x1000
            && FrontEndState50Machine.PasswordOverlayPpuSource == 0x1400,
            "the two bank-31 overlays originate at PPU $1000 and $1400");
    }

    private static void CheckAttractGraphComposition()
    {
        var entry = SceneBattleEngineStateGraph.EnterFromState50Resume(0x06);
        Require(entry.StateWrittenBy50Main00 == 0x30
            && entry.DispatchReadyState00 == 0x31,
            "state-$50 attract handoff composes with the #117 structural $30->$31 bootstrap");
        Require(FrontEndState50Machine.ResolveStartRedirect(0x00) == 0x05
            && FrontEndState50Machine.ResolveStartRedirect(0x05) == 0x07
            && FrontEndState50Machine.ResolveStartRedirect(0x06) == 0x05,
            "bank-0 $8871 Start redirect table is represented for the reachable front-end callers");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

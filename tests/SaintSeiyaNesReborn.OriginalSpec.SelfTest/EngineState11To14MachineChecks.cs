using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;

internal static class EngineState11To14MachineChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckBootstrapEntry();
        CheckControllerMasksAndReleaseLatch();
        CheckCursorSelection();
        CheckUpperRouteToReload3D();
        CheckPasswordRouteTo12();
        CheckState12IsNmiTransitional();
        CheckState13TextTermination();
        CheckState14IsAbsorbing();
        CheckTopLevelDispatcherComposition();
    }

    private static void CheckBootstrapEntry()
    {
        var ordinary = EngineState11To14Machine.EnterFromPromotedReload10(0x05);
        Require(ordinary.EngineState00 == 0x11
            && ordinary.EngineMirror01 == 0x11
            && ordinary.PlatformSubstate02 == 0x05
            && ordinary.FamilyIndex06 == 0x05
            && ordinary.Cursor58 == 0xBE
            && ordinary.InputReleaseLatch05 == 0x01,
            "$10 bootstrap reaches dispatch-ready $11 with $58=$BE and inherited $05=1");

        var clamped = EngineState11To14Machine.EnterFromPromotedReload10(0x10);
        Require(clamped.FamilyIndex06 == 0x0C,
            "$D444-$D44C clamps family field $06 to $0C");
    }

    private static void CheckControllerMasksAndReleaseLatch()
    {
        var decoded = EngineState11Input.FromController3D(0x1C);
        Require(decoded.Up && decoded.Down && decoded.Start,
            "$11 semantic input preserves exact $3D masks Up=$08 Down=$04 Start=$10");

        var state = EngineState11To14Machine.EnterFromPromotedReload10(0x05);
        var heldStart = EngineState11To14Machine.StepState11(
            state,
            new EngineState11Input(Up: false, Down: false, Start: true));
        Require(heldStart.Disposition == EngineState11To14Disposition.StayState11
            && heldStart.InputReleaseLatch05 == 0x01,
            "Start held across reload is blocked while inherited $05 remains nonzero");

        var released = EngineState11To14Machine.StepState11(
            state,
            new EngineState11Input(Up: false, Down: false, Start: false));
        Require(released.Disposition == EngineState11To14Disposition.StayState11
            && released.InputReleaseLatch05 == 0x00,
            "a Start-released $11 frame clears the inherited $05 edge latch");
    }

    private static void CheckCursorSelection()
    {
        var state = EngineState11To14Machine.EnterFromPromotedReload10(0x05, inheritedInputReleaseLatch05: 0);

        var down = EngineState11To14Machine.StepState11(
            state,
            new EngineState11Input(Up: false, Down: true, Start: false));
        Require(down.Cursor58 == 0xCE,
            "Down selects the lower $CE row when $02 is nonzero");

        var downState = state with { Cursor58 = down.Cursor58 };
        var upDominates = EngineState11To14Machine.StepState11(
            downState,
            new EngineState11Input(Up: true, Down: true, Start: false));
        Require(upDominates.Cursor58 == 0xBE,
            "Up has priority over Down in the canonical $C249-$C273 order");

        var noSelectableRows = EngineState11To14Machine.EnterFromPromotedReload10(0x00, 0) with
        {
            Cursor58 = 0xCE
        };
        var blockedMovement = EngineState11To14Machine.StepState11(
            noSelectableRows,
            new EngineState11Input(Up: true, Down: false, Start: false));
        Require(blockedMovement.Cursor58 == 0xCE,
            "$02=0 bypasses the cursor movement body entirely");
    }

    private static void CheckUpperRouteToReload3D()
    {
        var state = EngineState11To14Machine.EnterFromPromotedReload10(0x05, 0);
        var result = EngineState11To14Machine.StepState11(
            state,
            new EngineState11Input(Up: false, Down: false, Start: true));

        Require(result.Disposition == EngineState11To14Disposition.Reload3D
            && result.EngineState00 == 0x3D
            && result.EngineMirror01 == 0x3D
            && result.ReloadMode04 == 0x00
            && result.RefreshesSaintSnapshot
            && !result.GeneratesPasswordBuffer0600,
            "Start on default/upper $BE row takes $C2B8->$3D normal reload");

        var substateZero = EngineState11To14Machine.EnterFromPromotedReload10(0x00, 0) with
        {
            Cursor58 = 0xCE
        };
        var forcedReload = EngineState11To14Machine.StepState11(
            substateZero,
            new EngineState11Input(Up: false, Down: false, Start: true));
        Require(forcedReload.Disposition == EngineState11To14Disposition.Reload3D,
            "$02=0 forces the reload route regardless of cursor byte");
    }

    private static void CheckPasswordRouteTo12()
    {
        var state = EngineState11To14Machine.EnterFromPromotedReload10(0x05, 0) with
        {
            Cursor58 = 0xCE
        };
        var result = EngineState11To14Machine.StepState11(
            state,
            new EngineState11Input(Up: false, Down: false, Start: true));

        Require(result.Disposition == EngineState11To14Disposition.BeginPasswordState12
            && result.EngineState00 == 0x12
            && result.EngineMirror01 == 0x12
            && result.Cursor58 == 0xCE
            && result.InputReleaseLatch05 == 0xCE
            && result.GeneratesPasswordBuffer0600
            && !result.RefreshesSaintSnapshot
            && result.TextCursor14 == 0x08
            && result.TextCursor15 == 0x23,
            "lower row runs bank-0 $AE18 and enters state $12 with exact text coordinates");
    }

    private static void CheckState12IsNmiTransitional()
    {
        var result = EngineState11To14Machine.AdvanceState12OnNmi();
        Require(result.Disposition == EngineState11To14Disposition.AdvanceState13
            && result.EngineState00 == 0x13
            && result.EngineMirror01 == 0x13,
            "$12 NMI performs presentation work then increments both engine state bytes to $13");
    }

    private static void CheckState13TextTermination()
    {
        var inProgress = EngineState11To14Machine.AdvanceState13Text(false);
        Require(inProgress.Disposition == EngineState11To14Disposition.StayState13
            && inProgress.EngineState00 == 0x13
            && inProgress.EngineMirror01 == 0x13,
            "nonterminal password text keeps engine state $13");

        var terminal = EngineState11To14Machine.AdvanceState13Text(true);
        Require(terminal.Disposition == EngineState11To14Disposition.EnterTerminalState14
            && terminal.EngineState00 == 0x14
            && terminal.EngineMirror01 == 0x14
            && terminal.Timer57 == 0x80
            && terminal.TextField26 == 0x80
            && terminal.TextField27 == 0x80,
            "$0600 terminator $FF reaches $8DDB and commits exact $14 terminal fields");
    }

    private static void CheckState14IsAbsorbing()
    {
        var result = EngineState11To14Machine.StepTerminalState14();
        Require(EngineState11To14Machine.IsTerminalState14Absorbing
            && result.Disposition == EngineState11To14Disposition.StayTerminalState14
            && result.EngineState00 == 0x14
            && result.EngineMirror01 == 0x14
            && result.InputReleaseLatch05 == 0x00,
            "$14 has no normal main/NMI writer out and its main path clears $05");
    }

    private static void CheckTopLevelDispatcherComposition()
    {
        Require(EngineStateDispatcherMap.ResolveBootstrap(0x10).ResultingState00 == 0x11,
            "family composes with #109 $10->$11 bootstrap");
        Require(EngineStateDispatcherMap.ResolveMain(0x11).Route == EngineMainDispatchRoute.State11DedicatedC246,
            "$11 uses the dedicated main body");
        Require(EngineStateDispatcherMap.ResolveMain(0x12).Route == EngineMainDispatchRoute.LowGeneric9363
            && EngineStateDispatcherMap.ResolveMain(0x13).Route == EngineMainDispatchRoute.LowGeneric9363
            && EngineStateDispatcherMap.ResolveMain(0x14).Route == EngineMainDispatchRoute.LowGeneric9363,
            "$12-$14 share the low generic main body");
        Require(EngineStateDispatcherMap.ResolveNmi(0x12, 0x12).Route == EngineNmiDispatchRoute.State12AdvanceTo13,
            "$12 has its dedicated advancing NMI route");
        Require(EngineStateDispatcherMap.ResolveNmi(0x13, 0x13).Route == EngineNmiDispatchRoute.State13D42D,
            "$13 has the password-text NMI route");
        Require(EngineStateDispatcherMap.ResolveNmi(0x14, 0x14).Route == EngineNmiDispatchRoute.CommonTailOnly,
            "$14 has no dedicated NMI route");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;

internal static class EngineState91To99MachineChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckBootstrapEntry();
        CheckState91TextTermination();
        CheckState92CountdownAndAInput();
        CheckStates93To96AreOneNmiTransitions();
        CheckState97TimerAndIndexGate();
        CheckState98To99AndTerminalAbsorption();
        CheckDispatcherComposition();
    }

    private static void CheckBootstrapEntry()
    {
        var ordinary = EngineState91To99Machine.EnterFromPromotedReload90(0x05);
        Require(ordinary.EngineState00 == 0x91
            && ordinary.EngineMirror01 == 0x91
            && ordinary.FamilyIndex06 == 0x05
            && ordinary.GeneratesPasswordBuffer0600
            && ordinary.TextCursor14 == 0x08
            && ordinary.TextCursor15 == 0x23,
            "$90 bootstrap reaches $91, builds $0600 stream and seeds exact text cursor");

        var clamped = EngineState91To99Machine.EnterFromPromotedReload90(0x10);
        Require(clamped.FamilyIndex06 == 0x0C,
            "$D444-$D44C clamps high-family field $06 to $0C");
    }

    private static void CheckState91TextTermination()
    {
        var inProgress = EngineState91To99Machine.AdvanceState91Text(false);
        Require(inProgress.Disposition == EngineState91To99Disposition.StayState91
            && inProgress.EngineState00 == 0x91
            && inProgress.EngineMirror01 == 0x91,
            "state $91 remains text-driven until the $0600 terminator is consumed");

        var terminal = EngineState91To99Machine.AdvanceState91Text(true);
        Require(terminal.Disposition == EngineState91To99Disposition.EnterState92
            && terminal.EngineState00 == 0x92
            && terminal.EngineMirror01 == 0x92
            && terminal.Timer57 == 0x92
            && terminal.TextField26 == 0x80
            && terminal.TextField27 == 0x80,
            "$8DDB advances $91->$92 and applies the state-$92 special $57=$92 value");
    }

    private static void CheckState92CountdownAndAInput()
    {
        var decoded = EngineState92Input.FromController3D(0x80);
        Require(decoded.A && !EngineState92Input.FromController3D(0x10).A,
            "state $92 uses controller $3D bit $80 (A), distinct from Start $10");

        var countdown = EngineState91To99Machine.StepState92Main(0x02, decoded);
        Require(countdown.Disposition == EngineState91To99Disposition.CountdownState92
            && countdown.EngineState00 == 0x92
            && countdown.EngineMirror01 == 0x92
            && countdown.Timer57 == 0x01,
            "state $92 countdown ignores A while $57 is nonzero");

        var reachesZero = EngineState91To99Machine.StepState92Main(0x01, decoded);
        Require(reachesZero.Disposition == EngineState91To99Disposition.CountdownState92
            && reachesZero.Timer57 == 0x00
            && reachesZero.EngineState00 == 0x92,
            "the frame that decrements $57 from 1 to 0 still does not poll input");

        var waiting = EngineState91To99Machine.StepState92Main(
            0x00,
            new EngineState92Input(A: false));
        Require(waiting.Disposition == EngineState91To99Disposition.WaitForAState92
            && waiting.EngineState00 == 0x92
            && waiting.Timer57 == 0x00,
            "after countdown expiry state $92 waits for A");

        var advance = EngineState91To99Machine.StepState92Main(
            0x00,
            new EngineState92Input(A: true));
        Require(advance.Disposition == EngineState91To99Disposition.EnterState93
            && advance.EngineState00 == 0x93
            && advance.EngineMirror01 == 0x92
            && advance.Timer57 == 0x10,
            "$C3E8 advances only live $00 to $93 and seeds $57=$10");
    }

    private static void CheckStates93To96AreOneNmiTransitions()
    {
        byte mirror = 0x93;
        for (byte state = 0x93; state <= 0x96; state++)
        {
            var result = EngineState91To99Machine.AdvanceState93To96OnNmi(state, mirror);
            Require(result.Disposition == EngineState91To99Disposition.AdvanceNmiPresentation
                && result.EngineState00 == state + 1
                && result.EngineMirror01 == mirror,
                $"NMI advances live state ${state:X2} exactly once without touching mirror $01");

            // The next main frame performs the canonical $C220 mirror synchronization.
            mirror = (byte)(state + 1);
        }
    }

    private static void CheckState97TimerAndIndexGate()
    {
        var running = EngineState91To99Machine.StepState97Main(0x10, 0x0B);
        Require(running.Disposition == EngineState91To99Disposition.StayState97
            && running.EngineState00 == 0x97
            && running.EngineMirror01 == 0x97
            && running.Timer57 == 0x0F
            && running.FamilyIndex06 == 0x0B,
            "state $97 decrements $57 every main frame while keeping $06 stable");

        var oneMoreIndex = EngineState91To99Machine.StepState97Main(0x01, 0x0B);
        Require(oneMoreIndex.Disposition == EngineState91To99Disposition.StayState97
            && oneMoreIndex.Timer57 == 0x30
            && oneMoreIndex.FamilyIndex06 == 0x0C,
            "state $97 expiry reloads $57=$30 and increments $06 while new index is below $0D");

        var terminal = EngineState91To99Machine.StepState97Main(0x01, 0x0C);
        Require(terminal.Disposition == EngineState91To99Disposition.EnterState98
            && terminal.EngineState00 == 0x98
            && terminal.EngineMirror01 == 0x97
            && terminal.Timer57 == 0x30
            && terminal.FamilyIndex06 == 0x0D,
            "$06=$0C expiry reaches $0D and $9381 increments only live $00 to $98");
    }

    private static void CheckState98To99AndTerminalAbsorption()
    {
        var nmi = EngineState91To99Machine.AdvanceState98OnNmi(0x98);
        Require(nmi.Disposition == EngineState91To99Disposition.EnterTerminalState99
            && nmi.EngineState00 == 0x99
            && nmi.EngineMirror01 == 0x98,
            "$D365 advances live $98->$99 without changing the current frame mirror");

        var terminal = EngineState91To99Machine.StepTerminalState99();
        Require(EngineState91To99Machine.IsTerminalState99Absorbing
            && terminal.Disposition == EngineState91To99Disposition.StayTerminalState99
            && terminal.EngineState00 == 0x99
            && terminal.EngineMirror01 == 0x99,
            "$99 has no normal main/NMI writer out after the next main mirror synchronization");
    }

    private static void CheckDispatcherComposition()
    {
        Require(EngineStateDispatcherMap.ResolveBootstrap(0x90).ResultingState00 == 0x91,
            "family composes with #109 $90->$91 bootstrap");
        Require(EngineStateDispatcherMap.ResolveMain(0x91).Route == EngineMainDispatchRoute.State91Generic9363,
            "$91 uses generic bank-1 main presentation");
        Require(EngineStateDispatcherMap.ResolveMain(0x92).Route == EngineMainDispatchRoute.State92DedicatedC3C3,
            "$92 uses the dedicated timer/input main body");
        Require(EngineStateDispatcherMap.ResolveMain(0x97).Route == EngineMainDispatchRoute.State93To98Generic9363
            && EngineStateDispatcherMap.ResolveMain(0x98).Route == EngineMainDispatchRoute.State93To98Generic9363,
            "$97/$98 route through bank-1 $9363 before their dedicated logical transitions");
        Require(EngineStateDispatcherMap.ResolveMain(0x99).Route == EngineMainDispatchRoute.CommonTailOnly,
            "$99 has no dedicated main body");

        Require(EngineStateDispatcherMap.ResolveNmi(0x91, 0x91).Route == EngineNmiDispatchRoute.State91D320,
            "$91 has the dynamic-text NMI route");
        Require(EngineStateDispatcherMap.ResolveNmi(0x93, 0x93).ImmediateNextState00 == 0x94,
            "$93 NMI immediately advances live state to $94");
        Require(EngineStateDispatcherMap.ResolveNmi(0x96, 0x96).ImmediateNextState00 == 0x97,
            "$96 NMI immediately advances live state to $97");
        Require(EngineStateDispatcherMap.ResolveNmi(0x98, 0x98).ImmediateNextState00 == 0x99,
            "$98 NMI immediately advances live state to $99");
        Require(EngineStateDispatcherMap.ResolveNmi(0x99, 0x99).Route == EngineNmiDispatchRoute.CommonTailOnly,
            "$99 has no dedicated NMI body");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

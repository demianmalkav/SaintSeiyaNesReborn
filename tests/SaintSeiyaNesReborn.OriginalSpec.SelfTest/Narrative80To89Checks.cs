using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class Narrative80To89Checks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckScriptPointerMapping();
        CheckMainOnlyMirrorsNarrativeState();
        CheckState80CountdownAndCompletionTo81();
        CheckInteriorStateCompletion();
        CheckState88CompletesTo89();
        CheckState89ReloadsThrough3D();
    }

    private static void CheckScriptPointerMapping()
    {
        Require(PlatformNarrative80To89StateMachine.State73ScriptPointer == 0x8F9C,
            "state73 keeps the table's first special text pointer");

        ushort[] expected =
        [
            0x8FC4,
            0x8FEF,
            0x901A,
            0x9047,
            0x9070,
            0x9081,
            0x90A3,
            0x90CA,
            0x90F1,
        ];

        for (var i = 0; i < expected.Length; i++)
        {
            var state = (byte)(0x80 + i);
            Require(PlatformNarrative80To89StateMachine.ScriptPointerForState(state) == expected[i],
                $"state {state:X2} selects the confirmed $911C-derived script pointer");
        }
    }

    private static void CheckMainOnlyMirrorsNarrativeState()
    {
        var before = State(
            engine00: 0x86,
            mirror01: 0x85,
            timer57: 0x44,
            scratch26: 0x22,
            scratch27: 0x33);

        var after = PlatformNarrative80To89StateMachine.StepMainStateOnly(before);

        Require(after.EngineState00 == 0x86 && after.EngineMirror01 == 0x86,
            "narrative-state main dispatch mirrors $00->$01 without advancing the state");
        Require(after.Timer57 == 0x44
            && after.Scratch26 == 0x22
            && after.Scratch27 == 0x33,
            "main-state helper does not invent NMI-owned timer or text mutations");
    }

    private static void CheckState80CountdownAndCompletionTo81()
    {
        var state = PlatformNarrative80To89StateMachine.StepMainStateOnly(
            State(
                engine00: 0x80,
                mirror01: 0x75,
                timer57: 2,
                scratch26: 0x80,
                scratch27: 0x80));

        var toOne = PlatformNarrative80To89StateMachine.StepNmi(state);
        Require(toOne.Outcome == PlatformNarrative80To89NmiOutcome.IntroCountdown
            && toOne.State.Timer57 == 1
            && toOne.State.Scratch26 == 1,
            "$80 intro countdown reaching one executes the confirmed setup that seeds $26=1");
        Require(toOne.ScriptPointer == 0x8FC4,
            "$80 exposes the first narrative script pointer");

        var toZero = PlatformNarrative80To89StateMachine.StepNmi(toOne.State);
        Require(toZero.Outcome == PlatformNarrative80To89NmiOutcome.IntroCountdown
            && toZero.State.Timer57 == 0
            && toZero.State.Scratch26 == 1,
            "$57 1->0 leaves the seeded text gate for the following NMI");

        var completed = PlatformNarrative80To89StateMachine.StepNmi(
            toZero.State,
            scriptTerminatorReached: true);
        Require(completed.Outcome == PlatformNarrative80To89NmiOutcome.ScriptCompletedToNextState,
            "$80 script terminator advances the narrative state");
        Require(completed.State.EngineState00 == 0x81
            && completed.State.EngineMirror01 == 0x81,
            "$8DDB increments both $00/$01 from $80 to $81");
        Require(completed.State.Timer57 == 0x80
            && completed.State.Scratch26 == 0x80
            && completed.State.Scratch27 == 0x80,
            "$8DDB seeds the next narrative delay/pacing fields to $80");
    }

    private static void CheckInteriorStateCompletion()
    {
        var state = State(
            engine00: 0x84,
            mirror01: 0x84,
            timer57: 0,
            scratch26: 0);

        var waiting = PlatformNarrative80To89StateMachine.StepNmi(state);
        Require(waiting.Outcome == PlatformNarrative80To89NmiOutcome.WaitingForScriptTerminator
            && waiting.ScriptPointer == 0x9070,
            "interior state84 waits on its own mapped script without changing state");

        var completed = PlatformNarrative80To89StateMachine.StepNmi(
            waiting.State,
            scriptTerminatorReached: true);
        Require(completed.State.EngineState00 == 0x85
            && completed.State.EngineMirror01 == 0x85,
            "interior script terminator performs one-state increment");
    }

    private static void CheckState88CompletesTo89()
    {
        var state = State(
            engine00: 0x88,
            mirror01: 0x88,
            timer57: 0,
            scratch26: 0);

        var completed = PlatformNarrative80To89StateMachine.StepNmi(
            state,
            scriptTerminatorReached: true);

        Require(completed.ScriptPointer == 0x90F1,
            "state88 uses the final confirmed narrative script");
        Require(completed.State.EngineState00 == 0x89
            && completed.State.EngineMirror01 == 0x89,
            "final script terminator advances $88->$89 on both state bytes");
        Require(completed.State.Timer57 == 0x80
            && completed.State.Scratch26 == 0x80,
            "state89 begins with the same seeded preamble as earlier narrative states");
    }

    private static void CheckState89ReloadsThrough3D()
    {
        var state = State(
            engine00: 0x89,
            mirror01: 0x89,
            mode04: 0x12,
            timer57: 0,
            scratch26: 1,
            scratch27: 0x80);

        var reload = PlatformNarrative80To89StateMachine.StepNmi(state);

        Require(reload.Outcome == PlatformNarrative80To89NmiOutcome.State89ReloadE100
            && reload.ReloadE100,
            "state89 crosses its final text gate into the generic reload handoff");
        Require(reload.ScriptPointer is null,
            "state89 does not select a tenth narrative script");
        Require(reload.State.Mode04 == 0x8F,
            "$8D4A seeds $04=$8F before reload");
        Require(reload.State.EngineState00 == 0x3D
            && reload.State.EngineMirror01 == 0x3D,
            "$8D4E-$8D52 convert state89 to the normal $3D reload state");
        Require(PlatformPostExitStateMachine.MainDispatchesToReloadE100(reload.State),
            "state89 output re-enters the already-promoted $3D/$E100 boundary");
    }

    private static PlatformPostExitEngineState State(
        byte engine00,
        byte mirror01,
        byte mode04 = 0,
        byte timer57 = 0,
        byte scratch26 = 0,
        byte scratch27 = 0) =>
        new(
            EngineState00: engine00,
            EngineMirror01: mirror01,
            EngineSubstate03: 0,
            Mode04: mode04,
            Scratch26: scratch26,
            Scratch27: scratch27,
            Timer57: timer57,
            PlayerX3F: 0,
            PlayerY40: 0,
            PlayerField42: 0,
            PlayerAction4D: 0,
            PlayerAction4E: 0);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Narrative $80-$89 self-test failed: {label}");
    }
}

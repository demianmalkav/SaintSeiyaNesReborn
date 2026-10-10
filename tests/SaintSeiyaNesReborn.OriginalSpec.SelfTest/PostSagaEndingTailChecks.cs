using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class PostSagaEndingTailChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckSagaVictorySelectsFinalPlatform11();
        CheckFinalPlatformGateUsesSpecial70Path();
        CheckCompleteClosedTailReachesHardTerminal();
        CheckFinalPresentationPointerOrder();
        CheckWrongVictoryBoundaryIsRejected();
    }

    private static void CheckSagaVictorySelectsFinalPlatform11()
    {
        var entry = PostSagaEndingTail.EnterFinalPlatform(CanonicalSagaVictory());

        Require(entry.ActiveSaint0533 == 0x00,
            "post-Saga final platform is entered as Seiya");
        Require(entry.StoryProgress067D == 0x0E
            && entry.Stage050E == 0x00
            && entry.ProgressDescriptor06CD == 0x00
            && entry.StoryRoster0673 == 0x30,
            "exact Saga victory progression fields survive into the final platform boundary");
        Require(entry.Release0670 == 0x05
            && entry.EngineState00 == 0x20
            && entry.PlatformSubstate02 == 0x11,
            "progress $0E composes with the fixed platform map as active substate $11");
    }

    private static void CheckFinalPlatformGateUsesSpecial70Path()
    {
        var entry = PostSagaEndingTail.EnterFinalPlatform(CanonicalSagaVictory());

        Require(PostSagaEndingTail.EvaluateFinalPlatformExit(entry, 0xCF, 0x50, 0) is null,
            "final gate rejects X before $D0");
        Require(PostSagaEndingTail.EvaluateFinalPlatformExit(entry, 0xD0, 0x40, 0) is null,
            "final gate requires exact Y=$50");
        Require(PostSagaEndingTail.EvaluateFinalPlatformExit(entry, 0xD0, 0x50, 1) is null,
            "final gate rejects active jump phase");
        Require(PostSagaEndingTail.EvaluateFinalPlatformExit(entry, 0xD0, 0x50, 0)
                == PlatformExitTransitionKind.State70Special,
            "accepted final gate enters the special $70 path instead of $3D reload");

        var seed = PostSagaEndingTail.SeedSpecialNarrative(
            entry,
            ActivePlatformState(),
            playerX: 0xD0,
            playerY: 0x50,
            jumpPhase: 0);

        Require(seed.State.EngineState00 == 0x70
            && seed.State.Timer57 == 0xC0
            && seed.State.Scratch26 == 0
            && seed.State.Scratch27 == 0,
            "$11 accepted exit seeds the exact $70/$C0 narrative state");
        Require(!seed.SnapshotSaintsRequested,
            "special $11 exit does not execute the ordinary Saint snapshot path");
    }

    private static void CheckCompleteClosedTailReachesHardTerminal()
    {
        var entry = PostSagaEndingTail.EnterFinalPlatform(CanonicalSagaVictory());
        var seed = PostSagaEndingTail.SeedSpecialNarrative(
            entry,
            ActivePlatformState(),
            playerX: 0xD0,
            playerY: 0x50,
            jumpPhase: 0);

        var state = Advance70To80(seed.State);
        Require(state.EngineState00 == 0x80 && state.Timer57 == 0x20,
            "closed $70-$75 machine composes into narrative state $80");

        state = Advance80To89Reload(state);
        Require(state.EngineState00 == 0x3D
            && state.EngineMirror01 == 0x3D
            && state.Mode04 == 0x8F
            && state.EngineSubstate03 == 0,
            "closed $80-$89 machine ends at exact $8F/$3D reload boundary");

        var terminal = PostSagaEndingTail.ResolveEndingTerminal(
            state,
            persistent06AB: 0xFF,
            reloadField050E: entry.Stage050E);

        Require(terminal.PrgBank == 0
            && terminal.EntryCpu == 0xBC39
            && terminal.PresentationStreamCount == 10,
            "corrected $8F continuation enters ten-stream bank-0 ending at $BC39");
        Require(terminal.TerminalLoopCpu == 0xBD2F,
            "completed final presentation reaches self-loop $BD2F");
        Require(!terminal.ReturnsToGameplayOrFrontEnd,
            "original ending has no software edge back to gameplay/title/front-end");
        Require(terminal.RequiresExternalResetToLeaveMainThreadTerminal,
            "main-thread final loop is left only through external reset/power semantics");
    }

    private static void CheckFinalPresentationPointerOrder()
    {
        ushort[] expected =
        [
            0xBDF2,
            0xBE14,
            0xBE48,
            0xBE81,
            0xBEB1,
            0xBEE9,
            0xBEFD,
            0xBF3B,
            0xBF71,
            0xBF87,
        ];

        for (var i = 0; i < expected.Length; i++)
        {
            Require(PostSagaEndingTail.PresentationPointerForIndex(i) == expected[i],
                $"ending stream {i} pointer matches bank-0 table");
            Require(PostSagaEndingTail.IsHardTerminalAfterCompletedStream(i) == (i == 9),
                $"only completion of final stream {i} has the hard-terminal successor predicate");
        }
    }

    private static void CheckWrongVictoryBoundaryIsRejected()
    {
        var rejected = false;
        try
        {
            _ = PostSagaEndingTail.EnterFinalPlatform(
                CanonicalSagaVictory() with { StoryProgress067D = 0x0D });
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }

        Require(rejected,
            "post-Saga composition rejects a non-victory progression boundary");
    }

    private static PlatformPostExitEngineState Advance70To80(PlatformPostExitEngineState state)
    {
        state = PlatformPostExitStateMachine.StepSpecialMain(state).State; // mirror $70

        while (state.EngineState00 == 0x70)
            state = PlatformPostExitStateMachine.StepSpecialNmi(state, frameCounter3C: 0).State;

        while (state.EngineState00 == 0x71)
            state = PlatformPostExitStateMachine.StepSpecialMain(state).State;

        while (state.EngineState00 == 0x72)
            state = PlatformPostExitStateMachine.StepSpecialMain(state).State;

        state = PlatformPostExitStateMachine.StepSpecialMain(state).State; // mirror $73
        state = PlatformPostExitStateMachine.StepSpecialNmi(
            state,
            frameCounter3C: 0,
            state73TextTerminatorReached: true).State;

        while (state.EngineState00 == 0x74)
            state = PlatformPostExitStateMachine.StepSpecialMain(state).State;

        while (state.EngineState00 == 0x75)
            state = PlatformPostExitStateMachine.StepSpecialMain(state).State;

        return state;
    }

    private static PlatformPostExitEngineState Advance80To89Reload(PlatformPostExitEngineState state)
    {
        for (byte expectedState = 0x80; expectedState <= 0x88; expectedState++)
        {
            Require(state.EngineState00 == expectedState,
                $"narrative stream enters expected state ${expectedState:X2}");
            state = PlatformNarrative80To89StateMachine.StepMainStateOnly(state);

            while (state.Timer57 != 0)
                state = PlatformNarrative80To89StateMachine.StepNmi(state).State;

            state = PlatformNarrative80To89StateMachine.StepNmi(
                state,
                scriptTerminatorReached: true).State;
        }

        Require(state.EngineState00 == 0x89,
            "nine narrative scripts advance to state $89");
        state = PlatformNarrative80To89StateMachine.StepMainStateOnly(state);

        while (state.Timer57 != 0)
            state = PlatformNarrative80To89StateMachine.StepNmi(state).State;

        return PlatformNarrative80To89StateMachine.StepNmi(state).State;
    }

    private static SagaVictoryBoundary CanonicalSagaVictory() =>
        new(
            ActiveSaint0533: 0x00,
            StoryProgress067D: 0x0E,
            NextStage050E: 0x00,
            ProgressDescriptor06CD: 0x00,
            StoryRoster0673: 0x30,
            Release0670: 0x05,
            EngineBootstrapState00: 0x00,
            ImmediateEngineSuccessor: 0x20);

    private static PlatformPostExitEngineState ActivePlatformState() =>
        new(
            EngineState00: 0x20,
            EngineMirror01: 0x20,
            EngineSubstate03: 0x00,
            Mode04: 0x00,
            Scratch26: 0x00,
            Scratch27: 0x00,
            Timer57: 0x00,
            PlayerX3F: 0xD0,
            PlayerY40: 0x50,
            PlayerField42: 0x00,
            PlayerAction4D: 0x20,
            PlayerAction4E: 0x20);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Post-Saga ending-tail self-test failed: {label}");
    }
}

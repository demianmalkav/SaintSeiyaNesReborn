using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class PostExitStateMachineChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckNormalExitSeedsReloadBoundary();
        CheckSpecialExitSeeds70WithoutTouchingMirror();
        Check70NmiParityAndTransition();
        Check71And72MainProgression();
        Check73RequiresNmiTextTerminator();
        Check74And75CompleteTo80();
    }

    private static void CheckNormalExitSeedsReloadBoundary()
    {
        var before = State(
            engine00: 0x10,
            mirror01: 0x10,
            mode04: 0x77,
            scratch26: 0x11,
            scratch27: 0x22,
            timer57: 0x33);

        var seeded = PlatformPostExitStateMachine.ApplyAcceptedExit(
            PlatformExitTransitionKind.State3DReload,
            before);

        Require(seeded.State.EngineState00 == 0x3D
            && seeded.State.EngineMirror01 == 0x3D,
            "normal platform exit writes both $00/$01=$3D");
        Require(seeded.State.Mode04 == 0,
            "normal exit clears $04 before reload handoff");
        Require(seeded.SnapshotSaintsRequested,
            "normal exit exposes the confirmed pre-reload five-Saint snapshot");
        Require(seeded.Handoff == PlatformPostExitHandoffKind.ReloadE100,
            "normal exit hands control to fixed reload entry $E100");
        Require(PlatformPostExitStateMachine.MainDispatchesToReloadE100(seeded.State),
            "main dispatcher sees $00=$3D and routes to $E100");
        Require(PlatformPostExitStateMachine.NmiDispatchesToReloadE000(seeded.State),
            "NMI dispatcher sees $01=$3D and routes to $E000");
        Require(seeded.State.Scratch26 == 0x11
            && seeded.State.Scratch27 == 0x22
            && seeded.State.Timer57 == 0x33,
            "$3D seed does not invent unrelated logical mutations");
    }

    private static void CheckSpecialExitSeeds70WithoutTouchingMirror()
    {
        var before = State(
            engine00: 0x11,
            mirror01: 0x2A,
            mode04: 0x55,
            scratch26: 0x66,
            scratch27: 0x77,
            timer57: 0x12);

        var seeded = PlatformPostExitStateMachine.ApplyAcceptedExit(
            PlatformExitTransitionKind.State70Special,
            before);

        Require(seeded.State.EngineState00 == 0x70,
            "special substate11 exit writes $00=$70");
        Require(seeded.State.EngineMirror01 == 0x2A,
            "$96FD path does not mirror $70 into $01 itself");
        Require(seeded.State.Scratch26 == 0
            && seeded.State.Scratch27 == 0
            && seeded.State.Timer57 == 0xC0,
            "special exit clears $26/$27 and seeds $57=$C0");
        Require(seeded.State.Mode04 == 0x55,
            "special $70 entry does not borrow the normal exit $04 clear");
        Require(!seeded.SnapshotSaintsRequested,
            "special $70 path does not execute the normal $951F snapshot");
        Require(seeded.Handoff == PlatformPostExitHandoffKind.SpecialState70Sequence,
            "special exit remains in the dedicated $70 state machine");
    }

    private static void Check70NmiParityAndTransition()
    {
        var initial = State(
            engine00: 0x70,
            mirror01: 0x70,
            timer57: 2,
            playerX: 0x99,
            playerY: 0x22,
            playerField42: 0x11);

        var odd = PlatformPostExitStateMachine.StepSpecialNmi(initial, frameCounter3C: 1);
        Require(odd.Outcome == PlatformState70NmiOutcome.State70OddFrameNoCountdown
            && odd.State.Timer57 == 2,
            "$70 NMI countdown is skipped on odd $3C");

        var even = PlatformPostExitStateMachine.StepSpecialNmi(odd.State, frameCounter3C: 2);
        Require(even.Outcome == PlatformState70NmiOutcome.State70Countdown
            && even.State.Timer57 == 1
            && even.State.EngineState00 == 0x70,
            "even $3C decrements $57 while remaining in $70");

        var completed = PlatformPostExitStateMachine.StepSpecialNmi(even.State, frameCounter3C: 4);
        Require(completed.Outcome == PlatformState70NmiOutcome.State70CompletedTo71,
            "$57 reaching zero advances $70->$71 from NMI");
        Require(completed.State.EngineState00 == 0x71
            && completed.State.EngineMirror01 == 0x70,
            "$D73B increments $00 only; $01 remains $70 until next main mirror");
        Require(completed.State.Timer57 == 0x80,
            "$70 completion seeds next-phase timer $80");
        Require(completed.State.PlayerX3F == 0x00
            && completed.State.PlayerY40 == 0x80
            && completed.State.PlayerField42 == 0x40,
            "$D73B/$D3BF completion leaves the confirmed next-phase player fields");
    }

    private static void Check71And72MainProgression()
    {
        var state71 = State(
            engine00: 0x71,
            mirror01: 0x70,
            timer57: 1,
            playerX: 0x00,
            playerY: 0x80,
            playerField42: 0x40);

        var to72 = PlatformPostExitStateMachine.StepSpecialMain(state71);
        Require(to72.Outcome == PlatformState70MainOutcome.State71CompletedTo72,
            "$71 timer zero completion advances to $72");
        Require(to72.State.EngineState00 == 0x72
            && to72.State.EngineMirror01 == 0x71
            && to72.State.Timer57 == 0,
            "main mirrors entry state $71 before incrementing only $00 to $72");

        var nearThreshold = to72.State with
        {
            PlayerX3F = 0x64,
            EngineSubstate03 = 0x44,
            Timer57 = 0x55,
            PlayerAction4D = 0x01,
            PlayerAction4E = 0x02,
        };
        var to73 = PlatformPostExitStateMachine.StepSpecialMain(nearThreshold);

        Require(to73.Outcome == PlatformState70MainOutcome.State72CompletedTo73,
            "$72 reaches its transition when player X increments to $65");
        Require(to73.State.PlayerX3F == 0x65
            && to73.State.EngineSubstate03 == 0
            && to73.State.EngineState00 == 0x73
            && to73.State.EngineMirror01 == 0x72,
            "$72 clears $03, increments X and leaves $01 at mirrored entry state");
        Require(to73.State.PlayerAction4D == 0x20
            && to73.State.PlayerAction4E == 0x20
            && to73.State.Timer57 == 0x03,
            "$72 completion seeds action $20/$20 and $57=$03 for state $73");
    }

    private static void Check73RequiresNmiTextTerminator()
    {
        var state73 = State(
            engine00: 0x73,
            mirror01: 0x73,
            timer57: 3,
            scratch26: 0,
            scratch27: 0);

        var main = PlatformPostExitStateMachine.StepSpecialMain(state73);
        Require(main.Outcome == PlatformState70MainOutcome.State73WaitsForNmiText
            && main.State.EngineState00 == 0x73,
            "main-thread state73 is a wait and cannot advance itself");

        var stillRunning = PlatformPostExitStateMachine.StepSpecialNmi(
            main.State,
            frameCounter3C: 0,
            state73TextTerminatorReached: false);
        Require(stillRunning.Outcome == PlatformState70NmiOutcome.State73TextStillRunning
            && stillRunning.State == main.State,
            "state73 remains stable while NMI text stream has not reached $FF");

        var completed = PlatformPostExitStateMachine.StepSpecialNmi(
            stillRunning.State,
            frameCounter3C: 0,
            state73TextTerminatorReached: true);
        Require(completed.Outcome == PlatformState70NmiOutcome.State73TextCompletedTo74,
            "$8DDB text terminator advances state73 to74");
        Require(completed.State.EngineState00 == 0x74
            && completed.State.EngineMirror01 == 0x74,
            "$8DDB increments both $00/$01");
        Require(completed.State.Timer57 == 0x80
            && completed.State.Scratch26 == 0x80
            && completed.State.Scratch27 == 0x80,
            "$8DDB seeds $57/$26/$27=$80");
    }

    private static void Check74And75CompleteTo80()
    {
        var state74 = State(
            engine00: 0x74,
            mirror01: 0x74,
            timer57: 1);
        var to75 = PlatformPostExitStateMachine.StepSpecialMain(state74);

        Require(to75.Outcome == PlatformState70MainOutcome.State74CompletedTo75,
            "$74 timer completion advances to $75");
        Require(to75.State.EngineState00 == 0x75
            && to75.State.EngineMirror01 == 0x75
            && to75.State.Timer57 == 0,
            "$74 completion increments both $00/$01 to $75");

        var state75 = to75.State with { Timer57 = 0x5F };
        var to80 = PlatformPostExitStateMachine.StepSpecialMain(state75);
        Require(to80.Outcome == PlatformState70MainOutcome.State75CompletedTo80,
            "$75 delay reaches handoff at incremented $57=$60");
        Require(to80.State.EngineState00 == 0x80
            && to80.State.EngineMirror01 == 0x75
            && to80.State.Timer57 == 0x20,
            "$C5C3 handoff writes $00=$80 and $57=$20 while $01 stays at mirrored $75");
    }

    private static PlatformPostExitEngineState State(
        byte engine00,
        byte mirror01,
        byte engine03 = 0,
        byte mode04 = 0,
        byte scratch26 = 0,
        byte scratch27 = 0,
        byte timer57 = 0,
        byte playerX = 0,
        byte playerY = 0,
        byte playerField42 = 0,
        byte action4D = 0,
        byte action4E = 0) =>
        new(
            engine00,
            mirror01,
            engine03,
            mode04,
            scratch26,
            scratch27,
            timer57,
            playerX,
            playerY,
            playerField42,
            action4D,
            action4E);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Post-exit state-machine self-test failed: {label}");
    }
}

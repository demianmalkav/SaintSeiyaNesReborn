namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

/// <summary>
/// Minimal logical state carried by the engine paths immediately after a
/// platform exit. It intentionally excludes PPU, stack, audio and text-cursor
/// internals unless they directly gate a logical state transition.
/// </summary>
public readonly record struct PlatformPostExitEngineState(
    byte EngineState00,
    byte EngineMirror01,
    byte EngineSubstate03,
    byte Mode04,
    byte Scratch26,
    byte Scratch27,
    byte Timer57,
    byte PlayerX3F,
    byte PlayerY40,
    byte PlayerField42,
    byte PlayerAction4D,
    byte PlayerAction4E);

public enum PlatformPostExitHandoffKind
{
    ReloadE100,
    SpecialState70Sequence,
}

public readonly record struct PlatformPostExitSeedResult(
    PlatformPostExitEngineState State,
    PlatformPostExitHandoffKind Handoff,
    bool SnapshotSaintsRequested);

public enum PlatformState70MainOutcome
{
    State70Idle,
    State71Countdown,
    State71CompletedTo72,
    State72Walk,
    State72CompletedTo73,
    State73WaitsForNmiText,
    State74Countdown,
    State74CompletedTo75,
    State75Delay,
    State75CompletedTo80,
}

public readonly record struct PlatformState70MainResult(
    PlatformPostExitEngineState State,
    PlatformState70MainOutcome Outcome);

public enum PlatformState70NmiOutcome
{
    State70OddFrameNoCountdown,
    State70Countdown,
    State70CompletedTo71,
    State73TextStillRunning,
    State73TextCompletedTo74,
}

public readonly record struct PlatformState70NmiResult(
    PlatformPostExitEngineState State,
    PlatformState70NmiOutcome Outcome);

/// <summary>
/// Semantic reduction of the engine boundary after bank-1 $969D-$9713 accepts
/// a platform exit.
///
/// Normal exits use $96CB-$96E8: clear $04, snapshot all five Saints, write
/// $00/$01=$3D and jump directly to the fixed-bank reload entry $E100. The
/// renderer/audio/stack writes around that jump are deliberately represented as
/// implementation plumbing rather than logical state here.
///
/// Substate $11 instead uses $96FD-$9713: write $00=$70, clear $26/$27, seed
/// $57=$C0 and return. The following $70-$75 sequence is split between the NMI
/// path ($D3BF, bank-1 $8C19/$8D28) and main-thread handler $C538.
/// </summary>
public static class PlatformPostExitStateMachine
{
    public static PlatformPostExitSeedResult ApplyAcceptedExit(
        PlatformExitTransitionKind transition,
        PlatformPostExitEngineState before) =>
        transition switch
        {
            PlatformExitTransitionKind.State3DReload =>
                new PlatformPostExitSeedResult(
                    before with
                    {
                        EngineState00 = 0x3D,
                        EngineMirror01 = 0x3D,
                        Mode04 = 0x00,
                    },
                    PlatformPostExitHandoffKind.ReloadE100,
                    SnapshotSaintsRequested: true),

            PlatformExitTransitionKind.State70Special =>
                new PlatformPostExitSeedResult(
                    before with
                    {
                        EngineState00 = 0x70,
                        Scratch26 = 0x00,
                        Scratch27 = 0x00,
                        Timer57 = 0xC0,
                    },
                    PlatformPostExitHandoffKind.SpecialState70Sequence,
                    SnapshotSaintsRequested: false),

            _ => throw new ArgumentOutOfRangeException(nameof(transition), transition, null),
        };

    /// <summary>
    /// Fixed-bank main dispatcher semantics for the special transition.
    /// $C21E copies $00->$01 before dispatching; $C538 then handles states
    /// $70/$71/$72/$74/$75. State $73 is intentionally a main-thread wait: its
    /// advancement belongs to the NMI text sequence.
    /// </summary>
    public static PlatformState70MainResult StepSpecialMain(
        PlatformPostExitEngineState state)
    {
        if (state.EngineState00 is < 0x70 or > 0x75)
        {
            throw new InvalidOperationException(
                $"Special post-exit main step requires engine state $70-$75; got ${state.EngineState00:X2}.");
        }

        // $C21E-$C222 mirrors the current engine state before its dispatch.
        state = state with { EngineMirror01 = state.EngineState00 };

        return state.EngineState00 switch
        {
            0x70 => new(state, PlatformState70MainOutcome.State70Idle),
            0x71 => StepState71(state),
            0x72 => StepState72(state),
            0x73 => new(state, PlatformState70MainOutcome.State73WaitsForNmiText),
            0x74 => StepState74(state),
            0x75 => StepState75(state),
            _ => throw new InvalidOperationException(),
        };
    }

    /// <summary>
    /// NMI-side logical transition for states whose progression is owned by the
    /// interrupt path.
    ///
    /// For mirror state $70, $D3BF decrements $57 only on even $3C. Reaching
    /// zero calls $D73B, whose final INC $00 performs $70->$71; the caller then
    /// seeds $57=$80 and the confirmed player coordinates/field used by the next
    /// phase. $01 remains $70 until the next main-thread mirror write.
    ///
    /// For mirror state $73, exact text rendering/cursor pacing stays outside
    /// this semantic model. The caller supplies whether bank-1 $8D7D has reached
    /// its $FF terminator. At that terminator $8DDB increments both $00/$01 and
    /// seeds $57/$26/$27=$80.
    /// </summary>
    public static PlatformState70NmiResult StepSpecialNmi(
        PlatformPostExitEngineState state,
        byte frameCounter3C,
        bool state73TextTerminatorReached = false)
    {
        if (state.EngineMirror01 == 0x70)
        {
            if ((frameCounter3C & 0x01) != 0)
            {
                return new(state, PlatformState70NmiOutcome.State70OddFrameNoCountdown);
            }

            var timer = unchecked((byte)(state.Timer57 - 1));
            if (timer != 0)
            {
                return new(
                    state with { Timer57 = timer },
                    PlatformState70NmiOutcome.State70Countdown);
            }

            return new(
                state with
                {
                    EngineState00 = 0x71,
                    // NMI increments $00 only; $01 still contains $70 until the
                    // next main-thread $00->$01 mirror.
                    Timer57 = 0x80,
                    PlayerX3F = 0x00,
                    PlayerY40 = 0x80,
                    PlayerField42 = 0x40,
                },
                PlatformState70NmiOutcome.State70CompletedTo71);
        }

        if (state.EngineMirror01 == 0x73)
        {
            if (!state73TextTerminatorReached)
            {
                return new(state, PlatformState70NmiOutcome.State73TextStillRunning);
            }

            return new(
                state with
                {
                    EngineState00 = 0x74,
                    EngineMirror01 = 0x74,
                    Timer57 = 0x80,
                    Scratch26 = 0x80,
                    Scratch27 = 0x80,
                },
                PlatformState70NmiOutcome.State73TextCompletedTo74);
        }

        throw new InvalidOperationException(
            $"Special post-exit NMI step is promoted only for mirror states $70/$73; got ${state.EngineMirror01:X2}.");
    }

    /// <summary>
    /// The global dispatch rules around the normal reload are confirmed even
    /// though the stage-dependent destination selected inside $E100 remains a
    /// separate higher-level concern.
    /// </summary>
    public static bool MainDispatchesToReloadE100(PlatformPostExitEngineState state) =>
        state.EngineState00 == 0x3D;

    public static bool NmiDispatchesToReloadE000(PlatformPostExitEngineState state) =>
        state.EngineMirror01 == 0x3D;

    private static PlatformState70MainResult StepState71(
        PlatformPostExitEngineState state)
    {
        var timer = unchecked((byte)(state.Timer57 - 1));
        if (timer != 0)
        {
            return new(
                state with { Timer57 = timer },
                PlatformState70MainOutcome.State71Countdown);
        }

        return new(
            state with
            {
                EngineState00 = 0x72,
                Timer57 = 0x00,
            },
            PlatformState70MainOutcome.State71CompletedTo72);
    }

    private static PlatformState70MainResult StepState72(
        PlatformPostExitEngineState state)
    {
        var x = unchecked((byte)(state.PlayerX3F + 1));
        state = state with
        {
            EngineSubstate03 = 0x00,
            Timer57 = 0x00,
            PlayerX3F = x,
        };

        if (x < 0x65)
            return new(state, PlatformState70MainOutcome.State72Walk);

        return new(
            state with
            {
                EngineState00 = 0x73,
                PlayerAction4D = 0x20,
                PlayerAction4E = 0x20,
                Timer57 = 0x03,
            },
            PlatformState70MainOutcome.State72CompletedTo73);
    }

    private static PlatformState70MainResult StepState74(
        PlatformPostExitEngineState state)
    {
        var timer = unchecked((byte)(state.Timer57 - 1));
        if (timer != 0)
        {
            return new(
                state with { Timer57 = timer },
                PlatformState70MainOutcome.State74Countdown);
        }

        // $C57D-$C57F increments both $00 and $01 after the main-thread mirror.
        return new(
            state with
            {
                EngineState00 = 0x75,
                EngineMirror01 = 0x75,
                Timer57 = 0x00,
            },
            PlatformState70MainOutcome.State74CompletedTo75);
    }

    private static PlatformState70MainResult StepState75(
        PlatformPostExitEngineState state)
    {
        var timer = unchecked((byte)(state.Timer57 + 1));
        if (timer < 0x60)
        {
            return new(
                state with { Timer57 = timer },
                PlatformState70MainOutcome.State75Delay);
        }

        // $C5C3-$C5CA hands the engine to state $80 and seeds $57=$20. $01
        // remains $75 until the next global main-thread mirror.
        return new(
            state with
            {
                EngineState00 = 0x80,
                Timer57 = 0x20,
            },
            PlatformState70MainOutcome.State75CompletedTo80);
    }
}

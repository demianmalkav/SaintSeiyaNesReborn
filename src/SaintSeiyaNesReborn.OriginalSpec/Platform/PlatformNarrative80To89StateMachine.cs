namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformNarrative80To89NmiOutcome
{
    IntroCountdown,
    ScriptGateDelay,
    WaitingForScriptTerminator,
    ScriptCompletedToNextState,
    State89ReloadE100,
}

public readonly record struct PlatformNarrative80To89NmiResult(
    PlatformPostExitEngineState State,
    PlatformNarrative80To89NmiOutcome Outcome,
    ushort? ScriptPointer,
    bool ReloadE100);

/// <summary>
/// Clean-room semantic reduction of the NMI-driven narrative chain reached after
/// the special platform transition hands state $75 to $80.
///
/// Fixed NMI dispatch routes high-nibble $80 states through bank-1 $8C19.
/// States $80-$88 select one of nine scripts from the pointer table at $911E
/// (the preceding $911C pointer belongs to the already-promoted state-$73 text
/// gate). A script $FF terminator reaches $8DDB, increments both $00/$01 and
/// seeds $57/$26/$27=$80. State $89 instead reaches $8D4A, writes $04=$8F,
/// $00/$01=$3D and jumps to the generic $E100 reload path.
///
/// Exact character/tile upload pacing is renderer-owned and deliberately not
/// emulated here. The model retains the confirmed pre-script $57/$26 timing and
/// exposes the script pointer plus an explicit semantic terminator event.
/// </summary>
public static class PlatformNarrative80To89StateMachine
{
    private static readonly ushort[] ScriptPointers80To88 =
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

    public const ushort State73ScriptPointer = 0x8F9C;

    public static ushort ScriptPointerForState(byte engineState00)
    {
        if (engineState00 is < 0x80 or > 0x88)
        {
            throw new ArgumentOutOfRangeException(
                nameof(engineState00),
                engineState00,
                "Narrative script pointer is defined for states $80-$88.");
        }

        return ScriptPointers80To88[engineState00 - 0x80];
    }

    /// <summary>
    /// $C21E mirrors $00->$01 before the main dispatcher. States $80-$89 have no
    /// dedicated main-thread state transition in the confirmed dispatcher and
    /// fall through to generic housekeeping, so this semantic helper models only
    /// the state-byte effect relevant to the NMI chain.
    /// </summary>
    public static PlatformPostExitEngineState StepMainStateOnly(
        PlatformPostExitEngineState state)
    {
        ValidateState(state.EngineState00);
        return state with { EngineMirror01 = state.EngineState00 };
    }

    /// <summary>
    /// One semantic NMI step for states $80-$89.
    ///
    /// While $57 is nonzero, $8C19 decrements it. When the decremented value is
    /// one, the original setup path seeds $26=1; this guarantees that the first
    /// $8D28 call after $57 reaches zero can cross the text gate.
    ///
    /// With $57 already zero, $8D28 decrements $26 and returns until it reaches
    /// zero. At that point states $80-$88 enter their selected script. Rendering
    /// of intermediate tokens remains external to this logical model; the caller
    /// supplies scriptTerminatorReached only when the confirmed $FF token is the
    /// next semantic event.
    /// </summary>
    public static PlatformNarrative80To89NmiResult StepNmi(
        PlatformPostExitEngineState state,
        bool scriptTerminatorReached = false)
    {
        ValidateState(state.EngineState00);

        if (state.Timer57 != 0)
        {
            var timer = unchecked((byte)(state.Timer57 - 1));
            state = state with { Timer57 = timer };

            // $8CC4-$8CF2 is reached when the post-decrement X equals one and
            // ends by storing $26=1. PPU/cursor writes in that setup are omitted.
            if (timer == 1)
                state = state with { Scratch26 = 1 };

            return new(
                state,
                PlatformNarrative80To89NmiOutcome.IntroCountdown,
                state.EngineState00 <= 0x88 ? ScriptPointerForState(state.EngineState00) : null,
                ReloadE100: false);
        }

        if (state.Scratch26 != 0)
        {
            var scratch = unchecked((byte)(state.Scratch26 - 1));
            state = state with { Scratch26 = scratch };
            if (scratch != 0)
            {
                return new(
                    state,
                    PlatformNarrative80To89NmiOutcome.ScriptGateDelay,
                    state.EngineState00 <= 0x88 ? ScriptPointerForState(state.EngineState00) : null,
                    ReloadE100: false);
            }
        }

        if (state.EngineState00 == 0x89)
        {
            // $8D4A-$8D54: state $89 and above do not select another script.
            // They seed the next reload mode and jump immediately to $E100.
            return new(
                state with
                {
                    Mode04 = 0x8F,
                    EngineState00 = 0x3D,
                    EngineMirror01 = 0x3D,
                },
                PlatformNarrative80To89NmiOutcome.State89ReloadE100,
                ScriptPointer: null,
                ReloadE100: true);
        }

        var pointer = ScriptPointerForState(state.EngineState00);
        if (!scriptTerminatorReached)
        {
            return new(
                state,
                PlatformNarrative80To89NmiOutcome.WaitingForScriptTerminator,
                pointer,
                ReloadE100: false);
        }

        // $8DDB-$8DE8: terminator advances both state bytes and seeds the next
        // narrative state's delay/pacing values.
        return new(
            state with
            {
                EngineState00 = unchecked((byte)(state.EngineState00 + 1)),
                EngineMirror01 = unchecked((byte)(state.EngineMirror01 + 1)),
                Timer57 = 0x80,
                Scratch26 = 0x80,
                Scratch27 = 0x80,
            },
            PlatformNarrative80To89NmiOutcome.ScriptCompletedToNextState,
            pointer,
            ReloadE100: false);
    }

    private static void ValidateState(byte engineState00)
    {
        if (engineState00 is < 0x80 or > 0x89)
        {
            throw new InvalidOperationException(
                $"Narrative post-exit state must be $80-$89; got ${engineState00:X2}.");
        }
    }
}

namespace SaintSeiyaNesReborn.OriginalSpec;

public enum SceneBattleStateDisposition
{
    Stay,
    EnterState31,
    AdvanceEarlyState,
    AdvanceState35OnNmi,
    EnterState36,
    EnterState37,
    EnterState38,
    EnterState40,
    AdvanceTextState,
    EnterState50,
    StartEscapeTo50
}

public readonly record struct SceneBattleEntryResult(
    SceneBattleStateDisposition Disposition,
    byte StateWrittenBy50Main00,
    byte MirrorWrittenBy50Main01,
    byte StateAfterBootstrap00,
    byte MirrorBeforeMainSync01,
    byte DispatchReadyState00,
    byte DispatchReadyMirror01);

public readonly record struct SceneBattleStepResult(
    SceneBattleStateDisposition Disposition,
    byte EngineState00,
    byte EngineMirror01,
    byte? Timer57 = null,
    byte? Phase3F = null,
    byte? Counter03CC = null,
    byte? Field03CA = null,
    byte? Field42 = null,
    byte? Field4D = null,
    byte? Field4E = null,
    byte? TextField26 = null,
    byte? TextField27 = null);

/// <summary>
/// Structural semantic map of the reachable scene/battle engine-state graph
/// dispatched through fixed-bank $C346->$C659 and NMI bank-1 $8C19.
///
/// This intentionally models only global control-flow. Existing battle resources,
/// damage, dodge, techniques and stage-event specifications remain authoritative
/// for the mechanics executed inside these presentation/scene states.
///
/// Canonical reachable path:
/// $50 -> transient $30 -> $31 -> $32 -> $33 -> $34 -> $35 -> $36 -> $37
/// -> $38 -> $40 -> $41 -> ... -> $4C -> $4D -> $50.
///
/// $39-$3F and $4E-$4F are structurally dispatchable but have no producer in the
/// closed canonical graph. Start can hand any active $3x/$4x main state to $50.
/// </summary>
public static class SceneBattleEngineStateGraph
{
    public const byte StartMask = 0x10;
    public const byte ResumeModalStep0200 = 0x06;
    public const byte State37TerminalCounter03CC = 0x88;
    public const byte State38TerminalPhase3F = 0x60;
    public const byte TextAdvanceTimer57 = 0x80;
    public const byte TextAdvanceField26 = 0x80;

    private static readonly byte[] Reachable =
    [
        0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
        0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
        0x49, 0x4A, 0x4B, 0x4C, 0x4D
    ];

    public static IReadOnlyList<byte> ReachableStates => Reachable;

    public static bool IsReachableState(byte state00) => Array.IndexOf(Reachable, state00) >= 0;

    public static bool IsInDispatcherRangeButUnreachable(byte state00) =>
        state00 is >= 0x30 and <= 0x4F && !IsReachableState(state00);

    /// <summary>
    /// State-$50 main path $DA5C uses $0200=$06 as the resume result. $DA9D writes
    /// both state bytes to $30, then $C1D0->$D5DA increments only live $00 to $31.
    /// The same bootstrap reaches $C21E, whose $C220 mirror store synchronizes $01
    /// to $31 before ordinary scene dispatch. Therefore $30 is reachable but only
    /// as a bootstrap transient; the first dispatch-ready state is paired $31/$31.
    /// </summary>
    public static SceneBattleEntryResult EnterFromState50Resume(byte modalStep0200)
    {
        if (modalStep0200 != ResumeModalStep0200)
            throw new ArgumentOutOfRangeException(nameof(modalStep0200), modalStep0200, "Canonical scene resume requires $0200=$06.");

        return new SceneBattleEntryResult(
            SceneBattleStateDisposition.EnterState31,
            StateWrittenBy50Main00: 0x30,
            MirrorWrittenBy50Main01: 0x30,
            StateAfterBootstrap00: 0x31,
            MirrorBeforeMainSync01: 0x30,
            DispatchReadyState00: 0x31,
            DispatchReadyMirror01: 0x31);
    }

    /// <summary>
    /// Top-level scene main $C346 polls controller $3D before any state-local body.
    /// Start ($10) writes paired $00/$01=$50 and jumps to the modal engine.
    /// </summary>
    public static SceneBattleStepResult TryStartEscape(byte liveState00, byte mirror01, byte controller3D)
    {
        if (liveState00 is < 0x30 or > 0x4F)
            throw new ArgumentOutOfRangeException(nameof(liveState00), liveState00, "Start escape is defined for the $30-$4F dispatcher family.");

        if ((controller3D & StartMask) == 0)
        {
            return new SceneBattleStepResult(
                SceneBattleStateDisposition.Stay,
                liveState00,
                mirror01);
        }

        return new SceneBattleStepResult(
            SceneBattleStateDisposition.StartEscapeTo50,
            EngineState00: 0x50,
            EngineMirror01: 0x50);
    }

    public static SceneBattleStepResult StepState31(byte mirror01, byte phase3FAfterDecrement)
    {
        // $C668 decrements $3F; $C6A1-$C6B0 advances only when the post-decrement
        // value is zero. $C6B0 increments live $00 only.
        return phase3FAfterDecrement == 0
            ? new SceneBattleStepResult(SceneBattleStateDisposition.AdvanceEarlyState, 0x32, mirror01)
            : new SceneBattleStepResult(SceneBattleStateDisposition.Stay, 0x31, mirror01, Phase3F: phase3FAfterDecrement);
    }

    public static SceneBattleStepResult StepState32(byte mirror01, bool firstSceneObjectField1IsZeroAfterStep)
    {
        // Shared object-step body $C6BE-$C6DD. A zero post-step field enters $33.
        return firstSceneObjectField1IsZeroAfterStep
            ? new SceneBattleStepResult(SceneBattleStateDisposition.AdvanceEarlyState, 0x33, mirror01)
            : new SceneBattleStepResult(SceneBattleStateDisposition.Stay, 0x32, mirror01);
    }

    public static SceneBattleStepResult StepState33(byte mirror01, bool secondSceneObjectField1IsZeroAfterStep)
    {
        // $C6EE-$C70D repeats the paired scene-object step; zero advances live $00.
        return secondSceneObjectField1IsZeroAfterStep
            ? new SceneBattleStepResult(SceneBattleStateDisposition.AdvanceEarlyState, 0x34, mirror01)
            : new SceneBattleStepResult(SceneBattleStateDisposition.Stay, 0x33, mirror01);
    }

    public static SceneBattleStepResult AdvanceState34OnNmi(byte mirror01)
    {
        // NMI $D2C7 calls $D73B. After presentation/setup, $D78B increments live
        // $00 from $34 to $35. Important logical seeds for the following main
        // states are $3F/$03BB=$F8 and $03CC=0.
        return new SceneBattleStepResult(
            SceneBattleStateDisposition.AdvanceState35OnNmi,
            EngineState00: 0x35,
            EngineMirror01: mirror01,
            Phase3F: 0xF8,
            Counter03CC: 0x00);
    }

    public static SceneBattleStepResult StepState35(byte mirror01, byte scroll03BBAfterFrame)
    {
        // $C812+ owns the state-$35 presentation sequence. Its global state writer
        // is exactly $C8B7 and fires when the observed $03BB value equals $65.
        return scroll03BBAfterFrame == 0x65
            ? new SceneBattleStepResult(SceneBattleStateDisposition.EnterState36, 0x36, mirror01)
            : new SceneBattleStepResult(SceneBattleStateDisposition.Stay, 0x35, mirror01);
    }

    public static SceneBattleStepResult StepState36(byte mirror01, byte phase3FAfterDecrement)
    {
        // $C8F4 decrements $3F. $C934-$C944 advances when the new value is $44,
        // clears local animation fields and seeds state-$37 timer $57=$10.
        return phase3FAfterDecrement == 0x44
            ? new SceneBattleStepResult(
                SceneBattleStateDisposition.EnterState37,
                0x37,
                mirror01,
                Timer57: 0x10,
                Phase3F: phase3FAfterDecrement)
            : new SceneBattleStepResult(
                SceneBattleStateDisposition.Stay,
                0x36,
                mirror01,
                Phase3F: phase3FAfterDecrement);
    }

    public static SceneBattleStepResult StepState37(byte mirror01, byte timer57, byte counter03CC)
    {
        if (timer57 != 0)
        {
            return new SceneBattleStepResult(
                SceneBattleStateDisposition.Stay,
                0x37,
                mirror01,
                Timer57: (byte)(timer57 - 1),
                Counter03CC: counter03CC);
        }

        // Once $57 is zero, $03CC advances one unit per main frame until it has
        // already reached $88. The comparison precedes the increment.
        if (counter03CC < State37TerminalCounter03CC)
        {
            return new SceneBattleStepResult(
                SceneBattleStateDisposition.Stay,
                0x37,
                mirror01,
                Timer57: 0x00,
                Counter03CC: (byte)(counter03CC + 1),
                Field03CA: 0xD0);
        }

        // $C984 increments only live $00, then seeds the state-$38 presentation.
        return new SceneBattleStepResult(
            SceneBattleStateDisposition.EnterState38,
            0x38,
            mirror01,
            Timer57: 0x00,
            Counter03CC: counter03CC,
            Field03CA: 0x00,
            Field42: 0x40,
            Field4D: 0x10,
            Field4E: 0x10);
    }

    public static SceneBattleStepResult StepState38(byte mirror01, byte phase3FBeforeIncrement)
    {
        var nextPhase = (byte)(phase3FBeforeIncrement + 1);
        if (nextPhase < State38TerminalPhase3F)
        {
            return new SceneBattleStepResult(
                SceneBattleStateDisposition.Stay,
                0x38,
                mirror01,
                Phase3F: nextPhase);
        }

        // $C9A9 writes live $00=$40 rather than incrementing through $39. It also
        // seeds $4D/$4E=$20 and $57=$03. Mirror remains the previous main state.
        return new SceneBattleStepResult(
            SceneBattleStateDisposition.EnterState40,
            0x40,
            mirror01,
            Timer57: 0x03,
            Phase3F: nextPhase,
            Field4D: 0x20,
            Field4E: 0x20);
    }

    public static SceneBattleStepResult AdvanceTextState40To4COnTerminator(byte liveState00)
    {
        if (liveState00 is < 0x40 or > 0x4C)
            throw new ArgumentOutOfRangeException(nameof(liveState00), liveState00, "Text terminator advance requires state $40-$4C.");

        // NMI $D2D3 maps bank 1 and calls $8C19. Once its local $57/$26 delays
        // permit text consumption, source pointers $9100+ select one stream per
        // state. $FF reaches $8DDB, which increments BOTH $00/$01 and seeds the
        // next state's long delay fields.
        var next = (byte)(liveState00 + 1);
        return new SceneBattleStepResult(
            SceneBattleStateDisposition.AdvanceTextState,
            EngineState00: next,
            EngineMirror01: next,
            Timer57: TextAdvanceTimer57,
            TextField26: TextAdvanceField26,
            TextField27: 0x00);
    }

    public static SceneBattleStepResult StepState4DNmi(byte timer57, byte textField26)
    {
        if (timer57 != 0)
        {
            return new SceneBattleStepResult(
                SceneBattleStateDisposition.Stay,
                0x4D,
                0x4D,
                Timer57: (byte)(timer57 - 1),
                TextField26: textField26);
        }

        if (textField26 == 0)
            throw new ArgumentOutOfRangeException(nameof(textField26), textField26, "Reachable state $4D enters with nonzero $26.");

        var next26 = (byte)(textField26 - 1);
        if (next26 != 0)
        {
            return new SceneBattleStepResult(
                SceneBattleStateDisposition.Stay,
                0x4D,
                0x4D,
                Timer57: 0x00,
                TextField26: next26);
        }

        // At $8D2F, state >=$4D and < $80 bypasses the text-pointer body. $8D41
        // writes paired $00/$01=$50. Thus canonical $4D never reaches $4E.
        return new SceneBattleStepResult(
            SceneBattleStateDisposition.EnterState50,
            EngineState00: 0x50,
            EngineMirror01: 0x50,
            Timer57: 0x00,
            TextField26: 0x00);
    }
}

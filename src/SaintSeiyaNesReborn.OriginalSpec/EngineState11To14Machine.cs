namespace SaintSeiyaNesReborn.OriginalSpec;

public enum EngineState11To14Disposition
{
    StayState11,
    Reload3D,
    BeginPasswordState12,
    AdvanceState13,
    StayState13,
    EnterTerminalState14,
    StayTerminalState14
}

public readonly record struct EngineState11Input(
    bool Up,
    bool Down,
    bool Start)
{
    public static EngineState11Input FromController3D(byte controller3D) =>
        new(
            Up: (controller3D & EngineState11To14Machine.UpMask) != 0,
            Down: (controller3D & EngineState11To14Machine.DownMask) != 0,
            Start: (controller3D & EngineState11To14Machine.StartMask) != 0);
}

public readonly record struct EngineState11FamilyEntry(
    byte EngineState00,
    byte EngineMirror01,
    byte PlatformSubstate02,
    byte FamilyIndex06,
    byte Cursor58,
    byte InputReleaseLatch05);

public readonly record struct EngineState11StepResult(
    EngineState11To14Disposition Disposition,
    byte EngineState00,
    byte EngineMirror01,
    byte Cursor58,
    byte InputReleaseLatch05,
    byte? ReloadMode04,
    bool RefreshesSaintSnapshot,
    bool GeneratesPasswordBuffer0600,
    byte? TextCursor14,
    byte? TextCursor15);

public readonly record struct EngineState12NmiResult(
    EngineState11To14Disposition Disposition,
    byte EngineState00,
    byte EngineMirror01);

public readonly record struct EngineState13TextResult(
    EngineState11To14Disposition Disposition,
    byte EngineState00,
    byte EngineMirror01,
    byte? Timer57,
    byte? TextField26,
    byte? TextField27);

public readonly record struct EngineState14Result(
    EngineState11To14Disposition Disposition,
    byte EngineState00,
    byte EngineMirror01,
    byte InputReleaseLatch05);

/// <summary>
/// Semantic reduction of the reachable $11-$14 family entered from the verified
/// normal-reload stable state $10.
///
/// The family is cooperative rather than a simple main-loop sequence:
/// - $11 is an input-driven two-row choice state owned by the dedicated main body
///   at $C246-$C2A8;
/// - choosing the upper/default route returns directly to the already-promoted
///   $3D reload;
/// - choosing the lower row invokes bank-0 $AE18, which builds the password text
///   stream at $0600 from the durable snapshot staged at $0110+, then enters $12;
/// - $12 is NMI-transitional: $D2A2-$D2A9 advances both $00/$01 to $13;
/// - $13 is text-driven through bank-1 $8D5A over the dynamic $0600 stream;
/// - the stream terminator $FF reaches $8DDB and advances both state bytes to $14;
/// - $14 has no internal writer out of the family: its main route is the generic
///   $9363 visual body and its NMI route is the common tail, so it is absorbing
///   until reset/external restart.
///
/// PPU writes, audio commands and password encoding internals are deliberately
/// delegated to their existing specifications.
/// </summary>
public static class EngineState11To14Machine
{
    public const byte UpMask = 0x08;
    public const byte DownMask = 0x04;
    public const byte StartMask = 0x10;

    public const byte UpperCursor58 = 0xBE;
    public const byte LowerCursor58 = 0xCE;
    public const byte LowerSelectionThreshold58 = 0xC8;

    public static EngineState11FamilyEntry EnterFromPromotedReload10(
        byte platformSubstate02,
        byte inheritedInputReleaseLatch05 = 0x01)
    {
        // $D442 increments live $00 from $10 to $11. The ordinary main dispatcher
        // mirrors it into $01 at $C220 before running the dedicated $11 body.
        // $D444-$D44C clamps the scene/index field to $0C and $D483 seeds $58=$BE.
        return new EngineState11FamilyEntry(
            EngineState00: 0x11,
            EngineMirror01: 0x11,
            PlatformSubstate02: platformSubstate02,
            FamilyIndex06: (byte)Math.Min(platformSubstate02, (byte)0x0C),
            Cursor58: UpperCursor58,
            InputReleaseLatch05: inheritedInputReleaseLatch05);
    }

    public static EngineState11StepResult StepState11(
        EngineState11FamilyEntry state,
        EngineState11Input input)
    {
        if (state.EngineState00 != 0x11 || state.EngineMirror01 != 0x11)
            throw new ArgumentException("State-$11 step requires live and mirrored engine state $11.", nameof(state));

        var cursor = state.Cursor58;

        // $C249-$C273 only permits cursor movement when platform substate $02 is
        // nonzero. Up ($08) has priority over Down ($04) because the second test
        // is reached only when the Up bit is clear.
        if (state.PlatformSubstate02 != 0)
        {
            if (input.Up)
                cursor = UpperCursor58;
            else if (input.Down)
                cursor = LowerCursor58;
        }

        // $C275 tests Start ($10). A released Start clears $05 at $C2A9. This is
        // the required release edge after reload, whose common commit left $05=1.
        if (!input.Start)
        {
            return StayState11(state, cursor, inputReleaseLatch05: 0x00);
        }

        // While $05 is nonzero, a held Start is ignored. The latch is cleared only
        // by a frame in which Start is not pressed.
        if (state.InputReleaseLatch05 != 0)
        {
            return StayState11(state, cursor, state.InputReleaseLatch05);
        }

        // $C27F-$C287: no substate means no selectable lower branch; otherwise the
        // upper row ($BE < $C8) also takes the ordinary reload path.
        if (state.PlatformSubstate02 == 0 || cursor < LowerSelectionThreshold58)
        {
            // $C2B8 clears $04, $CA94 snapshots the Saints, then $C2BF-$C2CB sets
            // $00/$01=$3D and jumps to the already-promoted $E100 reload.
            return new EngineState11StepResult(
                Disposition: EngineState11To14Disposition.Reload3D,
                EngineState00: 0x3D,
                EngineMirror01: 0x3D,
                Cursor58: cursor,
                InputReleaseLatch05: 0x00,
                ReloadMode04: 0x00,
                RefreshesSaintSnapshot: true,
                GeneratesPasswordBuffer0600: false,
                TextCursor14: null,
                TextCursor15: null);
        }

        // Lower row: $C289 latches the cursor into $05, bank-0 $AE18 serializes the
        // already-staged durable snapshot and constructs a password text stream in
        // $0600 terminated by $FF. $C298-$C2A4 enters $12 and seeds text coordinates.
        return new EngineState11StepResult(
            Disposition: EngineState11To14Disposition.BeginPasswordState12,
            EngineState00: 0x12,
            EngineMirror01: 0x12,
            Cursor58: cursor,
            InputReleaseLatch05: cursor,
            ReloadMode04: null,
            RefreshesSaintSnapshot: false,
            GeneratesPasswordBuffer0600: true,
            TextCursor14: 0x08,
            TextCursor15: 0x23);
    }

    public static EngineState12NmiResult AdvanceState12OnNmi()
    {
        // $D543 performs presentation-only PPU writes. The logical body then reaches
        // $D2A5/$D2A7 and increments both live and mirrored state exactly once.
        return new EngineState12NmiResult(
            Disposition: EngineState11To14Disposition.AdvanceState13,
            EngineState00: 0x13,
            EngineMirror01: 0x13);
    }

    public static EngineState13TextResult AdvanceState13Text(bool passwordTerminatorFfReached)
    {
        if (!passwordTerminatorFfReached)
        {
            return new EngineState13TextResult(
                Disposition: EngineState11To14Disposition.StayState13,
                EngineState00: 0x13,
                EngineMirror01: 0x13,
                Timer57: null,
                TextField26: null,
                TextField27: null);
        }

        // NMI $D42D selects bank 1 and invokes $8D5A with A=$0D. That table entry
        // points at the dynamic text buffer $0600. When the interpreter reads its
        // terminal $FF, $8DDB increments both engine-state bytes and seeds $57/$26/$27.
        return new EngineState13TextResult(
            Disposition: EngineState11To14Disposition.EnterTerminalState14,
            EngineState00: 0x14,
            EngineMirror01: 0x14,
            Timer57: 0x80,
            TextField26: 0x80,
            TextField27: 0x80);
    }

    public static EngineState14Result StepTerminalState14()
    {
        // Main: $C242 rejects the dedicated $11 body, $C2A9 clears $05 and $9363
        // has no transition for $14. NMI: $14 matches no dedicated branch and falls
        // through $D367. Therefore no normal engine path changes $00/$01 here.
        return new EngineState14Result(
            Disposition: EngineState11To14Disposition.StayTerminalState14,
            EngineState00: 0x14,
            EngineMirror01: 0x14,
            InputReleaseLatch05: 0x00);
    }

    public static bool IsTerminalState14Absorbing => true;

    private static EngineState11StepResult StayState11(
        EngineState11FamilyEntry state,
        byte cursor,
        byte inputReleaseLatch05) =>
        new(
            Disposition: EngineState11To14Disposition.StayState11,
            EngineState00: 0x11,
            EngineMirror01: 0x11,
            Cursor58: cursor,
            InputReleaseLatch05: inputReleaseLatch05,
            ReloadMode04: null,
            RefreshesSaintSnapshot: false,
            GeneratesPasswordBuffer0600: false,
            TextCursor14: null,
            TextCursor15: null);
}

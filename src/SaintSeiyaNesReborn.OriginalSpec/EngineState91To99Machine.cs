namespace SaintSeiyaNesReborn.OriginalSpec;

public enum EngineState91To99Disposition
{
    StayState91,
    EnterState92,
    CountdownState92,
    WaitForAState92,
    EnterState93,
    AdvanceNmiPresentation,
    StayState97,
    EnterState98,
    EnterTerminalState99,
    StayTerminalState99
}

public readonly record struct EngineState91FamilyEntry(
    byte EngineState00,
    byte EngineMirror01,
    byte PlatformSubstate02,
    byte FamilyIndex06,
    bool GeneratesPasswordBuffer0600,
    byte TextCursor14,
    byte TextCursor15);

public readonly record struct EngineState91TextResult(
    EngineState91To99Disposition Disposition,
    byte EngineState00,
    byte EngineMirror01,
    byte? Timer57,
    byte? TextField26,
    byte? TextField27);

public readonly record struct EngineState92Input(bool A)
{
    public static EngineState92Input FromController3D(byte controller3D) =>
        new((controller3D & EngineState91To99Machine.AButtonMask) != 0);
}

public readonly record struct EngineState92MainResult(
    EngineState91To99Disposition Disposition,
    byte EngineState00,
    byte EngineMirror01,
    byte Timer57);

public readonly record struct EngineStateHighNmiResult(
    EngineState91To99Disposition Disposition,
    byte EngineState00,
    byte EngineMirror01);

public readonly record struct EngineState97MainResult(
    EngineState91To99Disposition Disposition,
    byte EngineState00,
    byte EngineMirror01,
    byte Timer57,
    byte FamilyIndex06);

public readonly record struct EngineState99Result(
    EngineState91To99Disposition Disposition,
    byte EngineState00,
    byte EngineMirror01);

/// <summary>
/// Semantic reduction of the reachable high engine-state family entered from the
/// verified stable reload state $90.
///
/// The family is cooperative:
/// - $90 reaches $91 through the short $C180/$D442 bootstrap; $D442 also invokes
///   bank-0 $AE18, producing the password/text stream at $0600;
/// - NMI state $91 feeds that stream to bank-1 $8D5A with source selector $0D;
///   its $FF terminator reaches $8DDB and advances both $00/$01 to $92;
/// - main state $92 first drains $57=$92 to zero, then polls controller $3D and
///   accepts the A-button bit $80 to increment live $00 to $93;
/// - NMI states $93-$96 each increment live $00 once, leaving mirror $01 unchanged
///   until the next main frame synchronizes it;
/// - main state $97 counts down $57 and increments family field $06 at each expiry;
///   reaching $06=$0D increments live $00 to $98;
/// - NMI state $98 increments live $00 to $99;
/// - $99 has no dedicated main/NMI writer and is absorbing until reset/external
///   restart.
///
/// Renderer, PPU and audio bodies are intentionally excluded.
/// </summary>
public static class EngineState91To99Machine
{
    public const byte AButtonMask = 0x80;
    public const byte State92InitialTimer57 = 0x92;
    public const byte State93InitialTimer57 = 0x10;
    public const byte State97RepeatTimer57 = 0x30;
    public const byte State97TerminalFamilyIndex06 = 0x0D;

    public static EngineState91FamilyEntry EnterFromPromotedReload90(byte platformSubstate02)
    {
        // $D442 increments $00 from $90 to $91 and clamps $02 into $06.
        // Because the new state is $91, $D454 maps bank 0 and calls $AE18,
        // which builds a $FF-terminated text/password stream at $0600.
        // $D45C-$D462 then seeds text cursor $15=$23, $14=$08.
        return new EngineState91FamilyEntry(
            EngineState00: 0x91,
            EngineMirror01: 0x91,
            PlatformSubstate02: platformSubstate02,
            FamilyIndex06: (byte)Math.Min(platformSubstate02, (byte)0x0C),
            GeneratesPasswordBuffer0600: true,
            TextCursor14: 0x08,
            TextCursor15: 0x23);
    }

    public static EngineState91TextResult AdvanceState91Text(bool terminatorFfReached)
    {
        if (!terminatorFfReached)
        {
            return new EngineState91TextResult(
                EngineState91To99Disposition.StayState91,
                EngineState00: 0x91,
                EngineMirror01: 0x91,
                Timer57: null,
                TextField26: null,
                TextField27: null);
        }

        // NMI $D42D selects bank 1 and calls $8D5A with A=$0D. Table entry
        // $911A/$911B is $0600. At terminal $FF, $8DDB increments both bytes,
        // seeds $57/$26/$27=$80, then the state-$92 special case replaces
        // $57 with $92 at $8DEA-$8DF0.
        return new EngineState91TextResult(
            EngineState91To99Disposition.EnterState92,
            EngineState00: 0x92,
            EngineMirror01: 0x92,
            Timer57: State92InitialTimer57,
            TextField26: 0x80,
            TextField27: 0x80);
    }

    public static EngineState92MainResult StepState92Main(byte timer57, EngineState92Input input)
    {
        // $C3C3-$C3C9 consumes the countdown and returns immediately. A frame that
        // decrements $57 from 1 to 0 still does not poll input; polling begins on
        // the following main frame.
        if (timer57 != 0)
        {
            return new EngineState92MainResult(
                EngineState91To99Disposition.CountdownState92,
                EngineState00: 0x92,
                EngineMirror01: 0x92,
                Timer57: (byte)(timer57 - 1));
        }

        // $C4E4 serializes controller 1 into $3D. With the canonical NES serial
        // order, bit $80 is A (Start is the already-promoted $10 bit).
        if (!input.A)
        {
            return new EngineState92MainResult(
                EngineState91To99Disposition.WaitForAState92,
                EngineState00: 0x92,
                EngineMirror01: 0x92,
                Timer57: 0x00);
        }

        // $C3E8 increments only live $00. Main had already mirrored $92 into $01
        // at $C220, so the immediate post-main state is live $93 / mirror $92.
        // $C3EA-$C3EC seeds $57=$10 for the later state-$97 countdown.
        return new EngineState92MainResult(
            EngineState91To99Disposition.EnterState93,
            EngineState00: 0x93,
            EngineMirror01: 0x92,
            Timer57: State93InitialTimer57);
    }

    public static EngineStateHighNmiResult AdvanceState93To96OnNmi(byte liveState00, byte mirror01)
    {
        if (liveState00 is < 0x93 or > 0x96)
            throw new ArgumentOutOfRangeException(nameof(liveState00), liveState00, "NMI advance requires state $93-$96.");

        // $D55E/$D571 perform presentation-only PPU writes and $D56E increments
        // live $00 exactly once. $01 is deliberately untouched by NMI.
        return new EngineStateHighNmiResult(
            EngineState91To99Disposition.AdvanceNmiPresentation,
            EngineState00: (byte)(liveState00 + 1),
            EngineMirror01: mirror01);
    }

    public static EngineState97MainResult StepState97Main(byte timer57, byte familyIndex06)
    {
        if (timer57 == 0)
            throw new ArgumentOutOfRangeException(nameof(timer57), timer57, "Reachable state-$97 frames enter with nonzero $57.");
        if (familyIndex06 > 0x0C)
            throw new ArgumentOutOfRangeException(nameof(familyIndex06), familyIndex06, "Reachable state-$97 family index is at most $0C before the terminal increment.");

        var nextTimer = (byte)(timer57 - 1);
        if (nextTimer != 0)
        {
            return new EngineState97MainResult(
                EngineState91To99Disposition.StayState97,
                EngineState00: 0x97,
                EngineMirror01: 0x97,
                Timer57: nextTimer,
                FamilyIndex06: familyIndex06);
        }

        // $9375 resets $57=$30, $9379 increments $06, and $937D-$9381 enters
        // $98 when the new index is no longer below $0D.
        var nextIndex = (byte)(familyIndex06 + 1);
        if (nextIndex < State97TerminalFamilyIndex06)
        {
            return new EngineState97MainResult(
                EngineState91To99Disposition.StayState97,
                EngineState00: 0x97,
                EngineMirror01: 0x97,
                Timer57: State97RepeatTimer57,
                FamilyIndex06: nextIndex);
        }

        // Main synchronized $01=$97 before bank-1 $9363. $9381 increments only
        // live $00, so mirror remains $97 until the next main frame.
        return new EngineState97MainResult(
            EngineState91To99Disposition.EnterState98,
            EngineState00: 0x98,
            EngineMirror01: 0x97,
            Timer57: State97RepeatTimer57,
            FamilyIndex06: nextIndex);
    }

    public static EngineStateHighNmiResult AdvanceState98OnNmi(byte mirror01 = 0x98)
    {
        // $D359-$D362 are presentation-only; $D365 increments only live $00.
        return new EngineStateHighNmiResult(
            EngineState91To99Disposition.EnterTerminalState99,
            EngineState00: 0x99,
            EngineMirror01: mirror01);
    }

    public static EngineState99Result StepTerminalState99()
    {
        // Main $C3F5 treats $99 as >= terminal bound and falls to $C3FC; its
        // common tail contains no state-family writer. NMI matches no dedicated
        // $99 case and falls to $D367. The next main frame mirrors live $99 into
        // $01, after which both remain stable under normal execution.
        return new EngineState99Result(
            EngineState91To99Disposition.StayTerminalState99,
            EngineState00: 0x99,
            EngineMirror01: 0x99);
    }

    public static bool IsTerminalState99Absorbing => true;
}

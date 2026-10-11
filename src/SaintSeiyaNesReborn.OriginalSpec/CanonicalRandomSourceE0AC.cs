namespace SaintSeiyaNesReborn.OriginalSpec;

public readonly record struct CanonicalRandomState(byte Value065F, byte Index0660);

public readonly record struct CanonicalRandomNmiContext(
    byte Gate9C,
    byte Gate9D,
    byte Gate9E,
    byte GateA0,
    byte IncomingVisiblePrgBank,
    byte MapperLock063E,
    byte MapperLock063F,
    byte Queue0641,
    byte State068F,
    byte Queue0526,
    byte Queue0538,
    byte Queue057D);

public readonly record struct CanonicalRandomStepResult(
    CanonicalRandomState State,
    byte VisiblePrgBank,
    byte AddedSourceByte,
    bool Updated);

/// <summary>
/// Clean-room contract for the pseudo-random/phase source rooted at fixed-bank $E0AC.
/// The original source byte is intentionally supplied by the caller from the currently
/// visible PRG bank's $94F0-$95EF window; original ROM table payloads are not embedded.
/// </summary>
public static class CanonicalRandomSourceE0AC
{
    public const ushort SourceWindowStart = 0x94F0;
    public const int SourceWindowBytes = 0x100;

    public static bool FullServiceUpdateEligible(CanonicalRandomNmiContext context) =>
        context.Gate9C != 0 && (context.Gate9D | context.Gate9E | context.GateA0) == 0;

    public static bool MapperServiceAvailable(CanonicalRandomNmiContext context) =>
        context.MapperLock063E != 0x04 && context.MapperLock063F != 0x04;

    /// <summary>
    /// Resolves the PRG bank visible at $94F0 immediately before $E09C JSR $E0AC.
    /// Later queue owners win because their temporary bank changes are not restored
    /// until $E0BD, after the random-source update.
    /// </summary>
    public static byte ResolveVisiblePrgBank(CanonicalRandomNmiContext context)
    {
        var bank = context.IncomingVisiblePrgBank;
        if (!MapperServiceAvailable(context))
            return bank;

        if (context.Queue0641 != 0)
            bank = context.State068F == 0x8F ? (byte)0x00 : (byte)0x06;
        if (context.Queue0526 != 0)
            bank = 0x06;
        if (context.Queue0538 != 0)
            bank = 0x05;
        if (context.Queue057D != 0)
            bank = 0x06;

        return bank;
    }

    public static CanonicalRandomStepResult Step(
        CanonicalRandomState state,
        CanonicalRandomNmiContext context,
        ReadOnlySpan<byte> visibleSourceWindow94F0)
    {
        var bank = ResolveVisiblePrgBank(context);
        if (!FullServiceUpdateEligible(context))
            return new(state, bank, AddedSourceByte: 0, Updated: false);

        if (visibleSourceWindow94F0.Length != SourceWindowBytes)
            throw new ArgumentException("Visible $94F0 source window must contain exactly $100 bytes.", nameof(visibleSourceWindow94F0));

        var source = visibleSourceWindow94F0[state.Index0660];
        var next = new CanonicalRandomState(
            unchecked((byte)(state.Value065F + source)),
            unchecked((byte)(state.Index0660 + 1)));
        return new(next, bank, source, Updated: true);
    }

    // Canonical whole-page initialization/reset owners.
    public static CanonicalRandomState ColdResetC13D() => new(0x00, 0x00);
    public static CanonicalRandomState PlatformPageReset959D() => new(0x00, 0x00);
    public static CanonicalRandomState Bank0ValidationPageResetAF0D() => new(0x00, 0x00);

    /// <summary>
    /// Bank-0 $AD4A first clears $0500/$0600, then fills $0648-$06AB with $01.
    /// Both random-source bytes therefore end as $01.
    /// </summary>
    public static CanonicalRandomState FrontEndSeedAD4A() => new(0x01, 0x01);

    /// <summary>
    /// Bank-0 $B38A writes to $0648,Y. Canonical placement offsets $17/$18 alias
    /// $065F/$0660 respectively, providing an indirect seed path.
    /// </summary>
    public static CanonicalRandomState ApplyBank0GridSeed(
        CanonicalRandomState state,
        byte offsetFrom0648,
        byte value)
    {
        return offsetFrom0648 switch
        {
            0x17 => state with { Value065F = value },
            0x18 => state with { Index0660 = value },
            _ => state,
        };
    }

    public static byte Mask01(CanonicalRandomState state) => (byte)(state.Value065F & 0x01);
    public static byte Mask03(CanonicalRandomState state) => (byte)(state.Value065F & 0x03);
    public static byte Mask07(CanonicalRandomState state) => (byte)(state.Value065F & 0x07);
    public static byte Mask0F(CanonicalRandomState state) => (byte)(state.Value065F & 0x0F);

    public static bool LowNibbleAtLeast(CanonicalRandomState state, byte threshold)
    {
        if (threshold > 0x0F)
            throw new ArgumentOutOfRangeException(nameof(threshold));
        return Mask0F(state) >= threshold;
    }

    /// <summary>
    /// $F995-$F99E maps $0660 parity to the signed direction byte used by dodge logic:
    /// even -> $FF, odd -> $01.
    /// </summary>
    public static byte CounterParityDirection(CanonicalRandomState state) =>
        (state.Index0660 & 0x01) == 0 ? (byte)0xFF : (byte)0x01;

    public static byte StageFiveSelector(CanonicalRandomState state, bool alternateHalf) =>
        alternateHalf ? (byte)(Mask01(state) + 2) : Mask01(state);

    public static byte EncounterSelector(CanonicalRandomState state, bool narrowTwoWay) =>
        narrowTwoWay ? Mask01(state) : Mask03(state);
}

namespace SaintSeiyaNesReborn.OriginalSpec;

public enum EngineBootstrapRoute
{
    FullBootstrapTo20,
    ShortBootstrap10To11,
    ShortBootstrap90To91
}

public enum EngineMainDispatchRoute
{
    CommonTailOnly,
    Reload3D,
    LowGeneric9363,
    State11DedicatedC246,
    Platform20C2F9,
    Scene30To4FC346,
    Scene60To6FC364,
    Narrative70To7FC538,
    State91Generic9363,
    State92DedicatedC3C3,
    State93To98Generic9363
}

public enum EngineNmiDispatchRoute
{
    CommonTailOnly,
    Latched50DABC,
    Latched3DE000,
    State12AdvanceTo13,
    State13D42D,
    Platform20D2BA,
    State34D2C7,
    Scene40To4FD2D3,
    Scene60To6FD2F0,
    State70D3BF,
    State73Or80To8FD30C,
    State91D320,
    State93AdvanceD55E,
    State94To96AdvanceD571,
    State98AdvanceD359
}

public readonly record struct EngineBootstrapDecision(
    EngineBootstrapRoute Route,
    byte ResultingState00,
    ushort LogicalTargetAddress);

public readonly record struct EngineMainDispatchDecision(
    EngineMainDispatchRoute Route,
    ushort LogicalTargetAddress);

public readonly record struct EngineNmiDispatchDecision(
    EngineNmiDispatchRoute Route,
    ushort LogicalTargetAddress,
    bool SelectedFromMirror01,
    byte? ImmediateNextState00 = null,
    byte? ImmediateNextMirror01 = null);

/// <summary>
/// Semantic partition of the fixed-bank top-level engine dispatchers.
///
/// This class deliberately models only logical state routing. It does not emulate
/// controller reads, PPU/OAM work, bank switching, rendering or audio bodies.
/// Addresses are CPU addresses in the canonical Japanese ROM.
/// </summary>
public static class EngineStateDispatcherMap
{
    /// <summary>
    /// Fixed-bank $C180 pre-loop bootstrap. The code has two short paths: $10 and
    /// $90 both reach $D442, whose first instruction increments $00. Every other
    /// value takes the full bootstrap which explicitly stores $20 before entering
    /// the ordinary frame loop. Current reachability only requires $00/$10/$90.
    /// </summary>
    public static EngineBootstrapDecision ResolveBootstrap(byte state00) =>
        state00 switch
        {
            0x10 => new(
                EngineBootstrapRoute.ShortBootstrap10To11,
                ResultingState00: 0x11,
                LogicalTargetAddress: 0xD442),
            0x90 => new(
                EngineBootstrapRoute.ShortBootstrap90To91,
                ResultingState00: 0x91,
                LogicalTargetAddress: 0xD442),
            _ => new(
                EngineBootstrapRoute.FullBootstrapTo20,
                ResultingState00: 0x20,
                LogicalTargetAddress: 0xC19A)
        };

    /// <summary>
    /// Main-loop dispatcher beginning at $C21E. $C220 mirrors live $00 into $01
    /// before the logical dispatch. Returned addresses identify the semantic body,
    /// not every intermediate branch instruction.
    /// </summary>
    public static EngineMainDispatchDecision ResolveMain(byte state00)
    {
        if (state00 == 0x3D)
            return new(EngineMainDispatchRoute.Reload3D, 0xE100);

        if (state00 == 0x00)
            return new(EngineMainDispatchRoute.CommonTailOnly, 0xC3FC);

        if (state00 < 0x15)
        {
            return state00 == 0x11
                ? new(EngineMainDispatchRoute.State11DedicatedC246, 0xC246)
                : new(EngineMainDispatchRoute.LowGeneric9363, 0x9363);
        }

        if (state00 == 0x20)
            return new(EngineMainDispatchRoute.Platform20C2F9, 0xC2F9);

        var family = state00 & 0xF0;
        if (family is 0x30 or 0x40)
            return new(EngineMainDispatchRoute.Scene30To4FC346, 0xC346);

        if (family == 0x60)
            return new(EngineMainDispatchRoute.Scene60To6FC364, 0xC364);

        if (family == 0x70)
            return new(EngineMainDispatchRoute.Narrative70To7FC538, 0xC538);

        if (state00 == 0x91)
            return new(EngineMainDispatchRoute.State91Generic9363, 0x9363);

        if (state00 == 0x92)
            return new(EngineMainDispatchRoute.State92DedicatedC3C3, 0xC3C3);

        if (state00 is >= 0x93 and < 0x99)
            return new(EngineMainDispatchRoute.State93To98Generic9363, 0x9363);

        return new(EngineMainDispatchRoute.CommonTailOnly, 0xC3FC);
    }

    /// <summary>
    /// NMI dispatcher beginning at $D269. Two routes are selected from the frame
    /// mirror $01 before the routine reloads live $00: $50 and $3D. All remaining
    /// logical cases below use live $00, matching the canonical instruction order.
    /// </summary>
    public static EngineNmiDispatchDecision ResolveNmi(byte mirror01, byte live00)
    {
        if (mirror01 == 0x50)
            return new(
                EngineNmiDispatchRoute.Latched50DABC,
                LogicalTargetAddress: 0xDABC,
                SelectedFromMirror01: true);

        if (mirror01 == 0x3D)
            return new(
                EngineNmiDispatchRoute.Latched3DE000,
                LogicalTargetAddress: 0xE000,
                SelectedFromMirror01: true);

        if (live00 == 0x12)
            return new(
                EngineNmiDispatchRoute.State12AdvanceTo13,
                LogicalTargetAddress: 0xD2A2,
                SelectedFromMirror01: false,
                ImmediateNextState00: 0x13,
                ImmediateNextMirror01: 0x13);

        if (live00 == 0x13)
            return new(EngineNmiDispatchRoute.State13D42D, 0xD2B0, false);

        if (live00 == 0x20)
            return new(EngineNmiDispatchRoute.Platform20D2BA, 0xD2BA, false);

        if (live00 == 0x34)
            return new(EngineNmiDispatchRoute.State34D2C7, 0xD2C7, false);

        var family = live00 & 0xF0;
        if (family == 0x40)
            return new(EngineNmiDispatchRoute.Scene40To4FD2D3, 0xD2D3, false);

        if (family == 0x60)
            return new(EngineNmiDispatchRoute.Scene60To6FD2F0, 0xD2F0, false);

        if (live00 == 0x70)
            return new(EngineNmiDispatchRoute.State70D3BF, 0xD2FE, false);

        if (live00 == 0x73 || family == 0x80)
            return new(EngineNmiDispatchRoute.State73Or80To8FD30C, 0xD30C, false);

        if (live00 == 0x91)
            return new(EngineNmiDispatchRoute.State91D320, 0xD320, false);

        if (live00 == 0x93)
            return new(
                EngineNmiDispatchRoute.State93AdvanceD55E,
                LogicalTargetAddress: 0xD32F,
                SelectedFromMirror01: false,
                ImmediateNextState00: 0x94);

        if (live00 is >= 0x94 and <= 0x96)
            return new(
                EngineNmiDispatchRoute.State94To96AdvanceD571,
                LogicalTargetAddress: 0xD34F,
                SelectedFromMirror01: false,
                ImmediateNextState00: (byte)(live00 + 1));

        if (live00 == 0x98)
            return new(
                EngineNmiDispatchRoute.State98AdvanceD359,
                LogicalTargetAddress: 0xD359,
                SelectedFromMirror01: false,
                ImmediateNextState00: 0x99);

        return new(EngineNmiDispatchRoute.CommonTailOnly, 0xD367, false);
    }

    /// <summary>
    /// The first unresolved states proved to be entered directly from already
    /// promoted normal-reload destinations through the $C180/$D442 bootstrap.
    /// </summary>
    public static bool IsDirectUnresolvedReloadSuccessor(byte state00) =>
        state00 is 0x11 or 0x91;
}

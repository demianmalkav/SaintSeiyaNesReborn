using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;

internal static class EngineStateDispatcherMapChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckBootstrapSuccessors();
        CheckMainPartition();
        CheckNmiMirrorPriority();
        CheckNmiLivePartitionAndTransitions();
        CheckDirectReloadBoundary();
    }

    private static void CheckBootstrapSuccessors()
    {
        var zero = EngineStateDispatcherMap.ResolveBootstrap(0x00);
        Require(zero.Route == EngineBootstrapRoute.FullBootstrapTo20
            && zero.ResultingState00 == 0x20
            && zero.LogicalTargetAddress == 0xC19A,
            "$00 takes full $C180 bootstrap and seeds engine state $20");

        var ten = EngineStateDispatcherMap.ResolveBootstrap(0x10);
        Require(ten.Route == EngineBootstrapRoute.ShortBootstrap10To11
            && ten.ResultingState00 == 0x11
            && ten.LogicalTargetAddress == 0xD442,
            "$10 reaches $D442 and increments to $11");

        var ninety = EngineStateDispatcherMap.ResolveBootstrap(0x90);
        Require(ninety.Route == EngineBootstrapRoute.ShortBootstrap90To91
            && ninety.ResultingState00 == 0x91
            && ninety.LogicalTargetAddress == 0xD442,
            "$90 reaches $D442 and increments to $91");
    }

    private static void CheckMainPartition()
    {
        Require(EngineStateDispatcherMap.ResolveMain(0x3D).Route == EngineMainDispatchRoute.Reload3D,
            "$3D main route jumps to $E100");
        Require(EngineStateDispatcherMap.ResolveMain(0x00).Route == EngineMainDispatchRoute.CommonTailOnly,
            "$00 has no ordinary per-state main body");
        Require(EngineStateDispatcherMap.ResolveMain(0x0A).Route == EngineMainDispatchRoute.LowGeneric9363,
            "ordinary low states use bank-1 $9363");
        Require(EngineStateDispatcherMap.ResolveMain(0x11).Route == EngineMainDispatchRoute.State11DedicatedC246,
            "$11 has a dedicated main body");
        Require(EngineStateDispatcherMap.ResolveMain(0x12).Route == EngineMainDispatchRoute.LowGeneric9363,
            "$12 returns to the generic low-state body");
        Require(EngineStateDispatcherMap.ResolveMain(0x15).Route == EngineMainDispatchRoute.CommonTailOnly,
            "$15 is outside the low generic range");
        Require(EngineStateDispatcherMap.ResolveMain(0x20).Route == EngineMainDispatchRoute.Platform20C2F9,
            "$20 is the promoted active platform family");
        Require(EngineStateDispatcherMap.ResolveMain(0x34).Route == EngineMainDispatchRoute.Scene30To4FC346,
            "$34 belongs to shared $30/$40 main family");
        Require(EngineStateDispatcherMap.ResolveMain(0x4F).Route == EngineMainDispatchRoute.Scene30To4FC346,
            "$4F belongs to shared $30/$40 main family");
        Require(EngineStateDispatcherMap.ResolveMain(0x50).Route == EngineMainDispatchRoute.CommonTailOnly,
            "$50 is NMI-owned at top level, not main-dispatched");
        Require(EngineStateDispatcherMap.ResolveMain(0x63).Route == EngineMainDispatchRoute.Scene60To6FC364,
            "$6x uses the fixed main family");
        Require(EngineStateDispatcherMap.ResolveMain(0x7A).Route == EngineMainDispatchRoute.Narrative70To7FC538,
            "$7x uses the promoted narrative main family");
        Require(EngineStateDispatcherMap.ResolveMain(0x82).Route == EngineMainDispatchRoute.CommonTailOnly,
            "$8x has no independent main body");
        Require(EngineStateDispatcherMap.ResolveMain(0x91).Route == EngineMainDispatchRoute.State91Generic9363,
            "$91 reuses bank-1 $9363");
        Require(EngineStateDispatcherMap.ResolveMain(0x92).Route == EngineMainDispatchRoute.State92DedicatedC3C3,
            "$92 has a dedicated main timer/body");
        Require(EngineStateDispatcherMap.ResolveMain(0x98).Route == EngineMainDispatchRoute.State93To98Generic9363,
            "$93-$98 reuse bank-1 $9363");
        Require(EngineStateDispatcherMap.ResolveMain(0x99).Route == EngineMainDispatchRoute.CommonTailOnly,
            "$99 falls through to the common tail");
    }

    private static void CheckNmiMirrorPriority()
    {
        var latched50 = EngineStateDispatcherMap.ResolveNmi(mirror01: 0x50, live00: 0x20);
        Require(latched50.Route == EngineNmiDispatchRoute.Latched50DABC
            && latched50.SelectedFromMirror01
            && latched50.LogicalTargetAddress == 0xDABC,
            "$01=$50 short-circuits NMI before live $00 dispatch");

        var latched3D = EngineStateDispatcherMap.ResolveNmi(mirror01: 0x3D, live00: 0x70);
        Require(latched3D.Route == EngineNmiDispatchRoute.Latched3DE000
            && latched3D.SelectedFromMirror01
            && latched3D.LogicalTargetAddress == 0xE000,
            "$01=$3D short-circuits NMI before live $00 dispatch");

        var live20 = EngineStateDispatcherMap.ResolveNmi(mirror01: 0x00, live00: 0x20);
        Require(!live20.SelectedFromMirror01
            && live20.Route == EngineNmiDispatchRoute.Platform20D2BA,
            "ordinary NMI routing reloads live $00 after mirror-only gates");
    }

    private static void CheckNmiLivePartitionAndTransitions()
    {
        var s12 = EngineStateDispatcherMap.ResolveNmi(0, 0x12);
        Require(s12.Route == EngineNmiDispatchRoute.State12AdvanceTo13
            && s12.ImmediateNextState00 == 0x13
            && s12.ImmediateNextMirror01 == 0x13,
            "$12 NMI increments both $00 and $01 to $13");

        Require(EngineStateDispatcherMap.ResolveNmi(0, 0x13).Route == EngineNmiDispatchRoute.State13D42D,
            "$13 has dedicated NMI work");
        Require(EngineStateDispatcherMap.ResolveNmi(0, 0x34).Route == EngineNmiDispatchRoute.State34D2C7,
            "$34 has dedicated NMI work inside the $30 family");
        Require(EngineStateDispatcherMap.ResolveNmi(0, 0x4A).Route == EngineNmiDispatchRoute.Scene40To4FD2D3,
            "$4x has NMI-side scene work");
        Require(EngineStateDispatcherMap.ResolveNmi(0, 0x65).Route == EngineNmiDispatchRoute.Scene60To6FD2F0,
            "$6x has NMI-side scene work");
        Require(EngineStateDispatcherMap.ResolveNmi(0, 0x70).Route == EngineNmiDispatchRoute.State70D3BF,
            "$70 has dedicated narrative NMI work");
        Require(EngineStateDispatcherMap.ResolveNmi(0, 0x73).Route == EngineNmiDispatchRoute.State73Or80To8FD30C,
            "$73 shares the banked NMI path used by $8x");
        Require(EngineStateDispatcherMap.ResolveNmi(0, 0x85).Route == EngineNmiDispatchRoute.State73Or80To8FD30C,
            "$80-$8F are NMI-owned at the top level");
        Require(EngineStateDispatcherMap.ResolveNmi(0, 0x91).Route == EngineNmiDispatchRoute.State91D320,
            "$91 has dedicated NMI work");

        var s93 = EngineStateDispatcherMap.ResolveNmi(0, 0x93);
        Require(s93.Route == EngineNmiDispatchRoute.State93AdvanceD55E
            && s93.ImmediateNextState00 == 0x94,
            "$93 NMI advances to $94");

        var s96 = EngineStateDispatcherMap.ResolveNmi(0, 0x96);
        Require(s96.Route == EngineNmiDispatchRoute.State94To96AdvanceD571
            && s96.ImmediateNextState00 == 0x97,
            "$94-$96 NMI chain advances one state");

        var s98 = EngineStateDispatcherMap.ResolveNmi(0, 0x98);
        Require(s98.Route == EngineNmiDispatchRoute.State98AdvanceD359
            && s98.ImmediateNextState00 == 0x99,
            "$98 NMI advances to $99");

        Require(EngineStateDispatcherMap.ResolveNmi(0, 0x92).Route == EngineNmiDispatchRoute.CommonTailOnly,
            "$92 is main-owned at the top-level dispatcher");
        Require(EngineStateDispatcherMap.ResolveNmi(0, 0x97).Route == EngineNmiDispatchRoute.CommonTailOnly,
            "$97 has no dedicated top-level NMI case");
    }

    private static void CheckDirectReloadBoundary()
    {
        Require(EngineStateDispatcherMap.IsDirectUnresolvedReloadSuccessor(0x11),
            "$11 is entered directly from the promoted $10 reload destination");
        Require(EngineStateDispatcherMap.IsDirectUnresolvedReloadSuccessor(0x91),
            "$91 is entered directly from the promoted $90 reload destination");
        Require(!EngineStateDispatcherMap.IsDirectUnresolvedReloadSuccessor(0x20),
            "$20 is already promoted platform state, not an unresolved reload successor");
    }

    private static void Require(bool condition, string name)
    {
        if (!condition)
            throw new InvalidOperationException(name);
    }
}

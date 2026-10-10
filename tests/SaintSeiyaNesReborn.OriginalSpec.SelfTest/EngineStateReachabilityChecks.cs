using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;

internal static class EngineStateReachabilityChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckExactProducedSet();
        CheckTransientStates();
        CheckRepresentativeDeadGaps();
        CheckNarrativeAndHighFamilies();
        CheckDispatcherDeadGapComposition();
        CheckExecutableOverlays();
    }

    private static void CheckExactProducedSet()
    {
        var classified = Enumerable.Range(0, 256)
            .Select(i => EngineStateReachability.Classify((byte)i))
            .ToArray();

        var produced = classified
            .Where(x => x.Kind != EngineStateReachabilityKind.StructuralButUnreachable)
            .Select(x => x.State)
            .ToArray();

        var expected = EngineStateReachability.CanonicalTransientStates
            .Concat(EngineStateReachability.CanonicalReachableStates)
            .OrderBy(x => x)
            .ToArray();

        Require(produced.SequenceEqual(expected),
            "all 256 global-state byte values are partitioned by the exact canonical producer set");
        Require(produced.Length == 59 && EngineStateReachability.CanonicalProducedStateCount == 59,
            "canonical global namespace contains exactly 59 produced values including bootstrap/handoff transients");
        Require(classified.Count(x => x.Kind == EngineStateReachabilityKind.StructuralButUnreachable) == 197
            && EngineStateReachability.CanonicalUnreachableStateCount == 197,
            "the remaining 197 byte values are structural dispatcher inputs with no canonical producer");
    }

    private static void CheckTransientStates()
    {
        byte[] expected = [0x00, 0x10, 0x30, 0x3D, 0x90];
        Require(EngineStateReachability.CanonicalTransientStates.SequenceEqual(expected),
            "$00/$10/$30/$3D/$90 are exactly the bootstrap or handoff transient global states");

        foreach (var state in expected)
        {
            Require(EngineStateReachability.Classify(state).Kind == EngineStateReachabilityKind.BootstrapOrHandoffTransient,
                $"state ${state:X2} is classified as a real transient rather than dead gap");
        }

        Require(EngineStateReachability.ResolveOwner(0x3D) == EngineStateOwner.ReloadBridge,
            "$3D is globally reachable as reload bridge even though it is not produced by the front-end $30-$4D attract sequence");
    }

    private static void CheckRepresentativeDeadGaps()
    {
        byte[] dead =
        [
            0x01, 0x0F,
            0x15, 0x1F,
            0x21, 0x2F,
            0x39, 0x3C, 0x3E, 0x3F,
            0x4E, 0x4F,
            0x51, 0x5F,
            0x61, 0x6F,
            0x76, 0x7F,
            0x8A, 0x8F,
            0x9A, 0xFF
        ];

        foreach (var state in dead)
        {
            var info = EngineStateReachability.Classify(state);
            Require(info.Kind == EngineStateReachabilityKind.StructuralButUnreachable
                && info.Owner == EngineStateOwner.None
                && !EngineStateReachability.HasCanonicalProducer(state),
                $"representative residual state ${state:X2} has no canonical executable producer");
        }
    }

    private static void CheckNarrativeAndHighFamilies()
    {
        foreach (var state in Enumerable.Range(0x70, 6).Select(i => (byte)i))
            Require(EngineStateReachability.ResolveOwner(state) == EngineStateOwner.SpecialPostExitNarrative,
                $"${state:X2} belongs to the closed $70->$75 special post-exit chain");

        foreach (var state in Enumerable.Range(0x80, 10).Select(i => (byte)i))
            Require(EngineStateReachability.ResolveOwner(state) == EngineStateOwner.NarrativeText,
                $"${state:X2} belongs to the closed $80->$89 narrative text chain");

        Require(EngineStateReachability.Classify(0x76).Kind == EngineStateReachabilityKind.StructuralButUnreachable
            && EngineStateReachability.Classify(0x8A).Kind == EngineStateReachabilityKind.StructuralButUnreachable,
            "$75 writes $80 directly and $89 exits to $3D, closing both adjacent narrative gaps");

        foreach (var state in Enumerable.Range(0x91, 9).Select(i => (byte)i))
            Require(EngineStateReachability.ResolveOwner(state) == EngineStateOwner.HighPresentationFamily,
                $"${state:X2} belongs to the closed $91-$99 family");

        Require(EngineStateReachability.Classify(0x9A).Kind == EngineStateReachabilityKind.StructuralButUnreachable,
            "$99 is absorbing, so the common-tail value $9A has no canonical producer");
    }

    private static void CheckDispatcherDeadGapComposition()
    {
        Require(EngineStateDispatcherMap.ResolveMain(0x15).Route == EngineMainDispatchRoute.CommonTailOnly,
            "$15 is structurally accepted by the dispatcher but has no producer");
        Require(EngineStateDispatcherMap.ResolveMain(0x21).Route == EngineMainDispatchRoute.CommonTailOnly,
            "$21 is structurally accepted by the dispatcher but has no producer");
        Require(EngineStateDispatcherMap.ResolveMain(0x51).Route == EngineMainDispatchRoute.CommonTailOnly,
            "$51 is structurally accepted by the dispatcher but has no producer");
        Require(EngineStateDispatcherMap.ResolveMain(0x61).Route == EngineMainDispatchRoute.Scene60To6FC364,
            "$61 would route through the $60-family body structurally, but resource failure produces only exact $60");
        Require(EngineStateDispatcherMap.ResolveMain(0x76).Route == EngineMainDispatchRoute.Narrative70To7FC538,
            "$76 would route through the $70-family body structurally, but $75 writes $80 directly");
        Require(EngineStateDispatcherMap.ResolveNmi(0x00, 0x8F).Route == EngineNmiDispatchRoute.State73Or80To8FD30C,
            "$8F has a structural NMI route even though the narrative chain exits at $89");
        Require(EngineStateDispatcherMap.ResolveMain(0x9A).Route == EngineMainDispatchRoute.CommonTailOnly,
            "$9A+ common-tail routing is structural only after absorbing $99");
    }

    private static void CheckExecutableOverlays()
    {
        var overlays = EngineStateReachability.ConfirmedExecutableOverlays;
        Require(overlays.Count == 2,
            "only two confirmed CHR-bank-31 executable overlays enter RAM $0440 through the promoted loader");
        Require(overlays.All(x => x.RamEntry == 0x0440 && x.ByteLength == 0x03C0 && !x.WritesGlobalState00Or01),
            "both confirmed overlays were scanned and neither writes global $00/$01");
        Require(overlays[0].PpuSource == 0x1000 && overlays[1].PpuSource == 0x1400,
            "confirmed overlay sources remain front-end $1000 and password $1400");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

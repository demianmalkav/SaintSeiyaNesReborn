using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;

internal static class NmiPresentationCoverageChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckEveryProducedStateIsClassified();
        CheckDeadNamespaceDoesNotCreatePresentationGaps();
        CheckMirrorPriority();
        CheckOracleCorrectedState00Tail();
        CheckDedicatedRouteOwnership();
        CheckBankContracts();
        CheckNoMaterialRendererGapRemains();
    }

    private static void CheckEveryProducedStateIsClassified()
    {
        var coverage = NmiPresentationCoverage.CanonicalProducedCoverage;
        Require(coverage.Count == 59 && NmiPresentationCoverage.CanonicalProducedCount == 59,
            "NMI coverage composes the exact 59-value canonical produced namespace");
        Require(coverage.All(x => x.Reachability != EngineStateReachabilityKind.StructuralButUnreachable),
            "canonical coverage contains no dead global-state values");
        Require(coverage.All(x => x.Coverage != NmiPresentationCoverageKind.Unclassified),
            "every canonically produced state has a presentation classification");
        Require(coverage.All(x => x.Owner != NmiPresentationOwner.None),
            "every produced state retains a semantic owner even when it uses common NMI infrastructure");

        var distinctRoutes = coverage.Select(x => x.DispatcherRoute).Distinct().ToArray();
        Require(distinctRoutes.Length == 16,
            "the canonical produced namespace reaches exactly sixteen distinct top-level NMI route classes");
    }

    private static void CheckDeadNamespaceDoesNotCreatePresentationGaps()
    {
        var dead = Enumerable.Range(0, 256)
            .Select(i => (byte)i)
            .Where(state => !EngineStateReachability.HasCanonicalProducer(state))
            .ToArray();

        Require(dead.Length == 197, "global reachability leaves exactly 197 structural-only byte values");

        foreach (var state in dead)
        {
            var info = NmiPresentationCoverage.ClassifyCanonicalState(state);
            Require(info.Coverage == NmiPresentationCoverageKind.StructuralUnreachable
                && info.Owner == NmiPresentationOwner.None
                && !info.MaterialGap,
                $"dead state ${state:X2} stays structural-only rather than becoming a false NMI gap");
        }

        Require(NmiPresentationCoverage.ClassifyCanonicalState(0x61).DispatcherRoute == EngineNmiDispatchRoute.Scene60To6FD2F0,
            "$61 retains its structural $6x dispatcher route while remaining unreachable");
        Require(NmiPresentationCoverage.ClassifyCanonicalState(0x8F).DispatcherRoute == EngineNmiDispatchRoute.State73Or80To8FD30C,
            "$8F retains its structural $8x dispatcher route while the narrative graph stops at $89");
    }

    private static void CheckMirrorPriority()
    {
        var modal = NmiPresentationCoverage.ResolveObservedPair(mirror01: 0x50, live00: 0x20);
        Require(modal.DispatcherRoute == EngineNmiDispatchRoute.Latched50DABC
            && modal.Owner == NmiPresentationOwner.FrontEndModal
            && modal.DispatcherTargetAddress == 0xDABC,
            "$01=$50 owns NMI before an otherwise valid live platform state");

        var reload = NmiPresentationCoverage.ResolveObservedPair(mirror01: 0x3D, live00: 0x70);
        Require(reload.DispatcherRoute == EngineNmiDispatchRoute.Latched3DE000
            && reload.Owner == NmiPresentationOwner.ReloadBridge
            && reload.DispatcherTargetAddress == 0xE000,
            "$01=$3D owns cooperative reload before live narrative dispatch");
    }

    private static void CheckOracleCorrectedState00Tail()
    {
        var zero = NmiPresentationCoverage.ClassifyCanonicalState(0x00);
        Require(zero.DispatcherRoute == EngineNmiDispatchRoute.State00TailD382
            && zero.DispatcherTargetAddress == 0xD382
            && zero.SemanticBodyAddresses.SequenceEqual(new ushort[] { 0xD382 })
            && zero.Coverage == NmiPresentationCoverageKind.ClosedCommonInfrastructure,
            "$00 enters $D382 exactly and does not falsely claim the ordinary $D367 entry");

        var ordinary = NmiPresentationCoverage.ClassifyCanonicalState(0x11);
        Require(ordinary.DispatcherRoute == EngineNmiDispatchRoute.CommonTailOnly
            && ordinary.DispatcherTargetAddress == 0xD367,
            "ordinary no-body states still enter the common NMI tail at $D367");
    }

    private static void CheckDedicatedRouteOwnership()
    {
        RequireOwned(0x12, EngineNmiDispatchRoute.State12AdvanceTo13, NmiPresentationOwner.LowPasswordFamily, 0xD543);
        RequireOwned(0x13, EngineNmiDispatchRoute.State13D42D, NmiPresentationOwner.LowPasswordFamily, 0xD42D);
        RequireOwned(0x20, EngineNmiDispatchRoute.Platform20D2BA, NmiPresentationOwner.Platform, 0xD7F2);
        RequireOwned(0x34, EngineNmiDispatchRoute.State34D2C7, NmiPresentationOwner.FrontEndAttract, 0xD73B);
        RequireOwned(0x40, EngineNmiDispatchRoute.Scene40To4FD2D3, NmiPresentationOwner.FrontEndAttract, 0x8C19);
        RequireOwned(0x60, EngineNmiDispatchRoute.Scene60To6FD2F0, NmiPresentationOwner.ResourceFailure, 0x9D69);
        RequireOwned(0x70, EngineNmiDispatchRoute.State70D3BF, NmiPresentationOwner.SpecialPostExitNarrative, 0xD3BF);
        RequireOwned(0x73, EngineNmiDispatchRoute.State73Or80To8FD30C, NmiPresentationOwner.SpecialPostExitNarrative, 0x8C19);
        RequireOwned(0x80, EngineNmiDispatchRoute.State73Or80To8FD30C, NmiPresentationOwner.NarrativeText, 0x8C19);
        RequireOwned(0x91, EngineNmiDispatchRoute.State91D320, NmiPresentationOwner.HighPresentationFamily, 0xD42D);
        RequireOwned(0x93, EngineNmiDispatchRoute.State93AdvanceD55E, NmiPresentationOwner.HighPresentationFamily, 0xD55E);
        RequireOwned(0x94, EngineNmiDispatchRoute.State94To96AdvanceD571, NmiPresentationOwner.HighPresentationFamily, 0xD571);
        RequireOwned(0x98, EngineNmiDispatchRoute.State98AdvanceD359, NmiPresentationOwner.HighPresentationFamily, 0xD53D);

        var modal = NmiPresentationCoverage.ClassifyCanonicalState(0x50);
        Require(modal.DispatcherRoute == EngineNmiDispatchRoute.Latched50DABC
            && modal.Owner == NmiPresentationOwner.FrontEndModal,
            "canonical paired state $50 resolves through the mirror-owned modal NMI dispatcher");

        var reload = NmiPresentationCoverage.ClassifyCanonicalState(0x3D);
        Require(reload.DispatcherRoute == EngineNmiDispatchRoute.Latched3DE000
            && reload.Owner == NmiPresentationOwner.ReloadBridge,
            "canonical paired state $3D resolves through the mirror-owned cooperative reload NMI path");
    }

    private static void CheckBankContracts()
    {
        Require(NmiPresentationCoverage.ClassifyCanonicalState(0x40).PrgBankContract
                == NmiPrgBankContract.DispatcherBank1ThenBank3,
            "$40-$4D dispatcher maps bank1 for $8C19 then bank3 before common tail");
        Require(NmiPresentationCoverage.ClassifyCanonicalState(0x80).PrgBankContract
                == NmiPrgBankContract.DispatcherBank1ThenBank3,
            "$73/$80-$89 shared script path maps bank1 then bank3");
        Require(NmiPresentationCoverage.ClassifyCanonicalState(0x60).PrgBankContract
                == NmiPrgBankContract.DispatcherBank1UntilCommonPersistentRestore,
            "$60 leaves bank1 active after $9D69 until common $3B restoration");
        Require(NmiPresentationCoverage.ClassifyCanonicalState(0x13).PrgBankContract
                == NmiPrgBankContract.HandlerManaged,
            "$D42D owns its own temporary bank1/bank3 transaction");
    }

    private static void CheckNoMaterialRendererGapRemains()
    {
        Require(NmiPresentationCoverage.CanonicalUnclassifiedCount == 0,
            "global NMI presentation audit leaves zero canonically produced unclassified states");
        Require(NmiPresentationCoverage.CanonicalMaterialGapCount == 0,
            "global NMI presentation audit leaves zero material renderer gaps");
    }

    private static void RequireOwned(
        byte state,
        EngineNmiDispatchRoute route,
        NmiPresentationOwner owner,
        ushort firstBody)
    {
        var info = NmiPresentationCoverage.ClassifyCanonicalState(state);
        Require(info.DispatcherRoute == route
            && info.Owner == owner
            && info.Coverage == NmiPresentationCoverageKind.ClosedSemanticOwner
            && info.SemanticBodyAddresses.Count > 0
            && info.SemanticBodyAddresses[0] == firstBody
            && !info.MaterialGap,
            $"state ${state:X2} has closed owner {owner} at ${firstBody:X4}");
    }

    private static void Require(bool condition, string name)
    {
        if (!condition)
            throw new InvalidOperationException(name);
    }
}

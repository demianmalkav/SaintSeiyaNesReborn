namespace SaintSeiyaNesReborn.OriginalSpec;

public enum NmiPresentationCoverageKind
{
    StructuralUnreachable,
    ClosedCommonInfrastructure,
    ClosedSemanticOwner,
    Unclassified
}

public enum NmiPresentationOwner
{
    None,
    CommonNmiInfrastructure,
    BootstrapReload,
    LowPasswordFamily,
    Platform,
    FrontEndAttract,
    ReloadBridge,
    FrontEndModal,
    ResourceFailure,
    SpecialPostExitNarrative,
    NarrativeText,
    HighPresentationFamily
}

public enum NmiPrgBankContract
{
    None,
    HandlerManaged,
    DispatcherBank1ThenBank3,
    DispatcherBank1UntilCommonPersistentRestore
}

public readonly record struct NmiPresentationCoverageInfo(
    byte Mirror01,
    byte LiveState00,
    EngineStateReachabilityKind Reachability,
    NmiPresentationCoverageKind Coverage,
    EngineNmiDispatchRoute DispatcherRoute,
    ushort DispatcherTargetAddress,
    IReadOnlyList<ushort> SemanticBodyAddresses,
    NmiPresentationOwner Owner,
    string Specification,
    NmiPrgBankContract PrgBankContract,
    bool MaterialGap);

/// <summary>
/// Exhaustive presentation-coverage manifest for the canonical NMI dispatcher
/// rooted at $D269. This composes the closed global-state reachability census with
/// the existing NMI dispatcher and family-specific semantic models. It does not
/// reimplement those renderer/state machines.
///
/// Two mirror-first routes ($01=$50 and $01=$3D) preempt live-state dispatch.
/// All other classification is based on live $00. Canonically unreachable byte
/// values remain visible as structural dispatcher inputs but cannot create an
/// ORIGINAL SPEC presentation gap without a new executable producer.
/// </summary>
public static class NmiPresentationCoverage
{
    public static IReadOnlyList<NmiPresentationCoverageInfo> CanonicalProducedCoverage =>
        EngineStateReachability.CanonicalTransientStates
            .Concat(EngineStateReachability.CanonicalReachableStates)
            .Distinct()
            .OrderBy(x => x)
            .Select(ClassifyCanonicalState)
            .ToArray();

    public static int CanonicalProducedCount => CanonicalProducedCoverage.Count;

    public static int CanonicalUnclassifiedCount =>
        CanonicalProducedCoverage.Count(x => x.Coverage == NmiPresentationCoverageKind.Unclassified);

    public static int CanonicalMaterialGapCount =>
        CanonicalProducedCoverage.Count(x => x.MaterialGap);

    public static NmiPresentationCoverageInfo ClassifyCanonicalState(byte state00)
    {
        var reachability = EngineStateReachability.Classify(state00);
        if (reachability.Kind == EngineStateReachabilityKind.StructuralButUnreachable)
            return StructuralUnreachable(state00);

        // Canonical producers of $50 and $3D write the mirror as well, so their
        // mirror-first routes are the canonical NMI owner for those states.
        var mirror01 = state00 switch
        {
            0x50 => (byte)0x50,
            0x3D => (byte)0x3D,
            _ => (byte)0x00
        };

        var decision = EngineStateDispatcherMap.ResolveNmi(mirror01, state00);
        return Describe(mirror01, state00, reachability.Kind, decision);
    }

    /// <summary>
    /// Resolve an observed mirror/live pair with the real dispatcher priority.
    /// This is useful for lagging-mirror transitions where $01 may intentionally
    /// differ from live $00 for part of a frame.
    /// </summary>
    public static NmiPresentationCoverageInfo ResolveObservedPair(byte mirror01, byte live00)
    {
        var decision = EngineStateDispatcherMap.ResolveNmi(mirror01, live00);

        if (decision.SelectedFromMirror01)
        {
            var mirrorReachability = EngineStateReachability.Classify(mirror01).Kind;
            return Describe(mirror01, live00, mirrorReachability, decision);
        }

        var liveReachability = EngineStateReachability.Classify(live00);
        if (liveReachability.Kind == EngineStateReachabilityKind.StructuralButUnreachable)
            return StructuralUnreachable(live00);

        return Describe(mirror01, live00, liveReachability.Kind, decision);
    }

    private static NmiPresentationCoverageInfo StructuralUnreachable(byte state00)
    {
        var decision = EngineStateDispatcherMap.ResolveNmi(0x00, state00);
        return new(
            Mirror01: 0x00,
            LiveState00: state00,
            Reachability: EngineStateReachabilityKind.StructuralButUnreachable,
            Coverage: NmiPresentationCoverageKind.StructuralUnreachable,
            DispatcherRoute: decision.Route,
            DispatcherTargetAddress: decision.LogicalTargetAddress,
            SemanticBodyAddresses: Array.Empty<ushort>(),
            Owner: NmiPresentationOwner.None,
            Specification: "ENGINE_STATE_REACHABILITY.md",
            PrgBankContract: NmiPrgBankContract.None,
            MaterialGap: false);
    }

    private static NmiPresentationCoverageInfo Describe(
        byte mirror01,
        byte live00,
        EngineStateReachabilityKind reachability,
        EngineNmiDispatchDecision decision)
    {
        var owner = ResolveOwner(live00, decision.Route);
        var bodies = BodyAddresses(decision.Route);
        var specification = SpecificationFor(owner, decision.Route);
        var coverage = decision.Route is EngineNmiDispatchRoute.CommonTailOnly or EngineNmiDispatchRoute.State00TailD382
            ? NmiPresentationCoverageKind.ClosedCommonInfrastructure
            : owner == NmiPresentationOwner.None
                ? NmiPresentationCoverageKind.Unclassified
                : NmiPresentationCoverageKind.ClosedSemanticOwner;

        var materialGap = coverage == NmiPresentationCoverageKind.Unclassified;

        return new(
            mirror01,
            live00,
            reachability,
            coverage,
            decision.Route,
            decision.LogicalTargetAddress,
            bodies,
            owner,
            specification,
            BankContractFor(decision.Route),
            materialGap);
    }

    private static NmiPresentationOwner ResolveOwner(byte live00, EngineNmiDispatchRoute route)
    {
        if (route == EngineNmiDispatchRoute.Latched50DABC)
            return NmiPresentationOwner.FrontEndModal;
        if (route == EngineNmiDispatchRoute.Latched3DE000)
            return NmiPresentationOwner.ReloadBridge;

        return EngineStateReachability.ResolveOwner(live00) switch
        {
            EngineStateOwner.BootstrapReload => NmiPresentationOwner.BootstrapReload,
            EngineStateOwner.LowPasswordFamily => NmiPresentationOwner.LowPasswordFamily,
            EngineStateOwner.Platform => NmiPresentationOwner.Platform,
            EngineStateOwner.FrontEndAttract => NmiPresentationOwner.FrontEndAttract,
            EngineStateOwner.ReloadBridge => NmiPresentationOwner.ReloadBridge,
            EngineStateOwner.FrontEndModal => NmiPresentationOwner.FrontEndModal,
            EngineStateOwner.ResourceFailure => NmiPresentationOwner.ResourceFailure,
            EngineStateOwner.SpecialPostExitNarrative => NmiPresentationOwner.SpecialPostExitNarrative,
            EngineStateOwner.NarrativeText => NmiPresentationOwner.NarrativeText,
            EngineStateOwner.HighPresentationFamily => NmiPresentationOwner.HighPresentationFamily,
            _ => NmiPresentationOwner.None
        };
    }

    private static IReadOnlyList<ushort> BodyAddresses(EngineNmiDispatchRoute route) => route switch
    {
        EngineNmiDispatchRoute.State00TailD382 => new ushort[] { 0xD382 },
        EngineNmiDispatchRoute.CommonTailOnly => new ushort[] { 0xD367 },
        EngineNmiDispatchRoute.Latched50DABC => new ushort[] { 0xDABC },
        EngineNmiDispatchRoute.Latched3DE000 => new ushort[] { 0xE000 },
        EngineNmiDispatchRoute.State12AdvanceTo13 => new ushort[] { 0xD543 },
        EngineNmiDispatchRoute.State13D42D => new ushort[] { 0xD42D },
        EngineNmiDispatchRoute.Platform20D2BA => new ushort[] { 0xD7F2, 0xD988 },
        EngineNmiDispatchRoute.State34D2C7 => new ushort[] { 0xD73B },
        EngineNmiDispatchRoute.Scene40To4FD2D3 => new ushort[] { 0x8C19 },
        EngineNmiDispatchRoute.Scene60To6FD2F0 => new ushort[] { 0x9D69 },
        EngineNmiDispatchRoute.State70D3BF => new ushort[] { 0xD3BF, 0xD73B },
        EngineNmiDispatchRoute.State73Or80To8FD30C => new ushort[] { 0x8C19 },
        EngineNmiDispatchRoute.State91D320 => new ushort[] { 0xD42D },
        EngineNmiDispatchRoute.State93AdvanceD55E => new ushort[] { 0xD55E },
        EngineNmiDispatchRoute.State94To96AdvanceD571 => new ushort[] { 0xD571, 0xD55E },
        EngineNmiDispatchRoute.State98AdvanceD359 => new ushort[] { 0xD53D, 0xD511 },
        _ => Array.Empty<ushort>()
    };

    private static NmiPrgBankContract BankContractFor(EngineNmiDispatchRoute route) => route switch
    {
        EngineNmiDispatchRoute.Scene40To4FD2D3 or
        EngineNmiDispatchRoute.State73Or80To8FD30C => NmiPrgBankContract.DispatcherBank1ThenBank3,

        EngineNmiDispatchRoute.Scene60To6FD2F0 =>
            NmiPrgBankContract.DispatcherBank1UntilCommonPersistentRestore,

        EngineNmiDispatchRoute.State13D42D or
        EngineNmiDispatchRoute.State91D320 or
        EngineNmiDispatchRoute.Platform20D2BA or
        EngineNmiDispatchRoute.Latched50DABC or
        EngineNmiDispatchRoute.Latched3DE000 => NmiPrgBankContract.HandlerManaged,

        _ => NmiPrgBankContract.None
    };

    private static string SpecificationFor(NmiPresentationOwner owner, EngineNmiDispatchRoute route)
    {
        if (route == EngineNmiDispatchRoute.Scene60To6FD2F0)
            return "PLATFORM_RESOURCE_FAILURE_STATE_60.md + PLATFORM_HUD_REFRESH_9D69.md";
        if (route == EngineNmiDispatchRoute.Platform20D2BA)
            return "PLATFORM_STATE20_NMI.md + PLATFORM_VISUAL_REFRESH_9915.md + PLATFORM_HUD_REFRESH_9D69.md";

        return owner switch
        {
            NmiPresentationOwner.CommonNmiInfrastructure => "NMI_PRESENTATION_COVERAGE.md",
            NmiPresentationOwner.BootstrapReload => "ENGINE_STATE_REACHABILITY.md",
            NmiPresentationOwner.LowPasswordFamily => "ENGINE_STATE_FAMILY_11_14.md",
            NmiPresentationOwner.Platform => "PLATFORM_STATE20_NMI.md",
            NmiPresentationOwner.FrontEndAttract => "SCENE_BATTLE_ENGINE_STATE_30_4F.md",
            NmiPresentationOwner.ReloadBridge => "PLATFORM_POST_EXIT_STATE_MACHINE.md",
            NmiPresentationOwner.FrontEndModal => "ENGINE_STATE_50_FRONTEND.md",
            NmiPresentationOwner.ResourceFailure => "PLATFORM_RESOURCE_FAILURE_STATE_60.md",
            NmiPresentationOwner.SpecialPostExitNarrative => "PLATFORM_POST_EXIT_STATE_MACHINE.md",
            NmiPresentationOwner.NarrativeText => "PLATFORM_NARRATIVE_STATES_80_89.md",
            NmiPresentationOwner.HighPresentationFamily => "ENGINE_STATE_FAMILY_91_99.md",
            _ => "UNCLASSIFIED"
        };
    }
}

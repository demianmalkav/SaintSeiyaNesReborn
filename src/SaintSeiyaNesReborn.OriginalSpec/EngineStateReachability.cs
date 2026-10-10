namespace SaintSeiyaNesReborn.OriginalSpec;

public enum EngineStateReachabilityKind
{
    StructuralButUnreachable,
    BootstrapOrHandoffTransient,
    Reachable
}

public enum EngineStateOwner
{
    None,
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

public readonly record struct EngineStateReachabilityInfo(
    byte State,
    EngineStateReachabilityKind Kind,
    EngineStateOwner Owner);

public readonly record struct ExecutableOverlayReachabilityAudit(
    ushort PpuSource,
    ushort RamEntry,
    ushort ByteLength,
    bool WritesGlobalState00Or01);

/// <summary>
/// Complete reachability classification of the global engine state byte $00
/// (with frame/NMI mirror $01) for the canonical Japanese ROM.
///
/// This artifact composes previously promoted state-family models. It does not
/// duplicate their internal mechanics. The binary writer census additionally
/// includes the CHR-bank-31 executable overlays discovered by PR #119.
///
/// Every possible byte value is deliberately classified: values outside the
/// reachable/transient set are structural dispatcher inputs only and have no
/// executable producer on the canonical control graph.
/// </summary>
public static class EngineStateReachability
{
    private static readonly byte[] TransientStates =
    [
        0x00, // stable reload commit consumed by full bootstrap -> $20
        0x10, // front-end/reload commit consumed by short bootstrap -> $11
        0x30, // front-end attract handoff, immediately bootstrapped -> $31
        0x3D, // reload bridge consumed by $E100
        0x90  // reload commit consumed by short bootstrap -> $91
    ];

    private static readonly byte[] ReachableStates =
    [
        0x11, 0x12, 0x13, 0x14,
        0x20,
        0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
        0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D,
        0x50,
        0x60,
        0x70, 0x71, 0x72, 0x73, 0x74, 0x75,
        0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89,
        0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99
    ];

    public static IReadOnlyList<byte> CanonicalTransientStates => TransientStates;
    public static IReadOnlyList<byte> CanonicalReachableStates => ReachableStates;

    public static int CanonicalProducedStateCount => TransientStates.Length + ReachableStates.Length;
    public static int CanonicalUnreachableStateCount => 256 - CanonicalProducedStateCount;

    public static IReadOnlyList<ExecutableOverlayReachabilityAudit> ConfirmedExecutableOverlays { get; } =
    [
        new(0x1000, 0x0440, 0x03C0, WritesGlobalState00Or01: false),
        new(0x1400, 0x0440, 0x03C0, WritesGlobalState00Or01: false)
    ];

    public static EngineStateReachabilityInfo Classify(byte state)
    {
        if (Array.IndexOf(TransientStates, state) >= 0)
            return new(state, EngineStateReachabilityKind.BootstrapOrHandoffTransient, ResolveOwner(state));

        if (Array.IndexOf(ReachableStates, state) >= 0)
            return new(state, EngineStateReachabilityKind.Reachable, ResolveOwner(state));

        return new(state, EngineStateReachabilityKind.StructuralButUnreachable, EngineStateOwner.None);
    }

    public static bool HasCanonicalProducer(byte state) =>
        Classify(state).Kind != EngineStateReachabilityKind.StructuralButUnreachable;

    public static EngineStateOwner ResolveOwner(byte state) => state switch
    {
        0x00 or 0x10 or 0x90 => EngineStateOwner.BootstrapReload,
        >= 0x11 and <= 0x14 => EngineStateOwner.LowPasswordFamily,
        0x20 => EngineStateOwner.Platform,
        0x30 or >= 0x31 and <= 0x38 or >= 0x40 and <= 0x4D => EngineStateOwner.FrontEndAttract,
        0x3D => EngineStateOwner.ReloadBridge,
        0x50 => EngineStateOwner.FrontEndModal,
        0x60 => EngineStateOwner.ResourceFailure,
        >= 0x70 and <= 0x75 => EngineStateOwner.SpecialPostExitNarrative,
        >= 0x80 and <= 0x89 => EngineStateOwner.NarrativeText,
        >= 0x91 and <= 0x99 => EngineStateOwner.HighPresentationFamily,
        _ => EngineStateOwner.None
    };
}

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformSpecialNormalExitOrigin
{
    Stage3Talk9D96,
    Stage5ActionA6A8,
    Stage2ActionA444,
    ProgressionMapE4D7,
}

public enum PlatformSpecialNormalExitDisposition
{
    ReenterInteractiveSelector,
    CommitStableState,
}

public readonly record struct PlatformSpecialNormalExitProfile(
    byte Substate02,
    byte Progression067D,
    byte ReloadField050E,
    byte InheritedTerminal0670,
    byte ProgressionCode06CD,
    PlatformSpecialNormalExitOrigin Origin,
    ushort SubstateSourceAddress,
    ushort SubstateWriterAddress,
    bool RequiresCreation067CZero);

public readonly record struct PlatformSpecialNormalExitResult(
    PlatformSpecialNormalExitProfile Profile,
    PlatformSpecialNormalExitDisposition Disposition,
    byte CanonicalSaint0533,
    byte ReloadField050E,
    byte Terminal0670,
    byte Field067C,
    byte Field06B8,
    byte Flags0673,
    byte Flags06CC,
    byte StoryPhase06CE,
    PlatformWarmReloadSelectorState? SelectorState,
    PlatformNormalWarmReloadResult? StableResult);

/// <summary>
/// End-to-end semantic handoff for the five normal platform substates outside
/// the principal $02=$00-$0B family: $0C-$10.
///
/// Every accepted gate still uses the common bank-1 $96CB normal exit body
/// ($04=0, Saint snapshot, $00/$01=$3D, jump $E100). The distinction is the
/// persistent state carried into $E100 by the routines that created each
/// special platform substate.
///
/// $0C/$0D/$0E are phase platforms created with $0670=$02 and $067C=0. On
/// reload, $ED57 consumes that $02 by incrementing $067C before re-entering the
/// already modeled interactive selector. $0F is entered with $0670=$05; the
/// $A973 reset clears it and re-enters the selector cleanly. $10 is entered
/// with $0670=$01 and is exceptional: $E26A handles it before $ED57, rewrites
/// it to $05 and commits stable engine state $00 without reopening the selector.
/// </summary>
public static class PlatformSpecialNormalExitPipeline
{
    public const ushort CommonExitBody96CB = 0x96CB;
    public const ushort WarmReloadE100 = 0xE100;
    public const ushort WarmFieldResetA973 = 0xA973;
    public const ushort WarmPhaseHandlerED57 = 0xED57;

    public static PlatformSpecialNormalExitProfile ProfileFor(int substate) => substate switch
    {
        0x0C => new(
            Substate02: 0x0C,
            Progression067D: 0x03,
            ReloadField050E: 0x03,
            InheritedTerminal0670: 0x02,
            ProgressionCode06CD: 0x02,
            Origin: PlatformSpecialNormalExitOrigin.Stage3Talk9D96,
            SubstateSourceAddress: 0x9DC1,
            SubstateWriterAddress: 0x9DC1,
            RequiresCreation067CZero: true),

        0x0D => new(
            Substate02: 0x0D,
            Progression067D: 0x05,
            ReloadField050E: 0x05,
            InheritedTerminal0670: 0x02,
            ProgressionCode06CD: 0x02,
            Origin: PlatformSpecialNormalExitOrigin.Stage5ActionA6A8,
            SubstateSourceAddress: 0xA6E7,
            SubstateWriterAddress: 0xA6E9,
            RequiresCreation067CZero: true),

        0x0E => new(
            Substate02: 0x0E,
            Progression067D: 0x02,
            ReloadField050E: 0x02,
            InheritedTerminal0670: 0x02,
            ProgressionCode06CD: 0x00,
            Origin: PlatformSpecialNormalExitOrigin.Stage2ActionA444,
            SubstateSourceAddress: 0xA468,
            SubstateWriterAddress: 0xA46A,
            RequiresCreation067CZero: true),

        // $E4D7 indexes the byte table at $E4E0 with $067D, then stores the
        // selected byte to $02 at shared writer $E4DD.
        0x0F => new(
            Substate02: 0x0F,
            Progression067D: 0x0D,
            ReloadField050E: 0x0A,
            InheritedTerminal0670: 0x05,
            ProgressionCode06CD: 0x0E,
            Origin: PlatformSpecialNormalExitOrigin.ProgressionMapE4D7,
            SubstateSourceAddress: 0xE4ED,
            SubstateWriterAddress: 0xE4DD,
            RequiresCreation067CZero: false),

        0x10 => new(
            Substate02: 0x10,
            Progression067D: 0x0C,
            ReloadField050E: 0x0C,
            InheritedTerminal0670: 0x01,
            ProgressionCode06CD: 0x0E,
            Origin: PlatformSpecialNormalExitOrigin.ProgressionMapE4D7,
            SubstateSourceAddress: 0xE4EC,
            SubstateWriterAddress: 0xE4DD,
            RequiresCreation067CZero: false),

        _ => throw new ArgumentOutOfRangeException(
            nameof(substate), substate, "Special normal platform substate must be $0C-$10."),
    };

    /// <summary>
    /// Applies the proven substate provenance, the already-modeled physical exit
    /// gate and the early $E100 routing that occurs before the interactive loop.
    /// Returns null when the coordinate/action gate rejects the exit or when a
    /// Saint cannot reach the substate through the confirmed provenance.
    ///
    /// flags0673/flags06CC are retained selector-time inputs for $0C-$0F. They
    /// are intentionally not interpreted here; the existing #102/#104 models
    /// own the subsequent interactive release and final destination semantics.
    /// </summary>
    public static PlatformSpecialNormalExitResult? ResolveAcceptedExit(
        int substate,
        PlatformSaintIndex saint,
        int playerX,
        int playerY,
        int jumpPhase,
        byte flags0673 = 0,
        byte flags06CC = 0)
    {
        var profile = ProfileFor(substate);

        // Progression advance into $067D=$0C (#104) can create only canonical
        // Seiya (0) or Shun (2), which correspond to internal platform indices
        // 0 and 1. Other Saints therefore cannot reach platform substate $10
        // through the confirmed normal progression path.
        if (substate == 0x10 && InternalSaintValue(saint) > 1)
            return null;

        var transition = PlatformExitGate.Evaluate(
            substate,
            saint,
            playerX,
            playerY,
            jumpPhase);

        if (transition != PlatformExitTransitionKind.State3DReload)
            return null;

        var canonicalSaint = CanonicalSaintForInternal(saint);

        return substate switch
        {
            0x0C => SelectorReentry(
                profile,
                canonicalSaint,
                reloadField050E: 0x03,
                field067C: 0x01,
                field06B8: 0x00,
                flags0673,
                flags06CC),

            0x0D => SelectorReentry(
                profile,
                canonicalSaint,
                reloadField050E: 0x05,
                field067C: 0x01,
                field06B8: 0x00,
                flags0673,
                flags06CC),

            0x0E => SelectorReentry(
                profile,
                canonicalSaint,
                // $ED57 has a unique stage-2/Hyoga branch: canonical $0533=1
                // becomes temporary $050E=$08 with $06B8=$0A.
                reloadField050E: canonicalSaint == 0x01 ? (byte)0x08 : (byte)0x02,
                field067C: 0x01,
                field06B8: canonicalSaint == 0x01 ? (byte)0x0A : (byte)0x00,
                flags0673,
                flags06CC),

            0x0F => SelectorReentry(
                profile,
                canonicalSaint,
                reloadField050E: 0x0A,
                // Incoming $0670=$05 takes $ED57->$A973, which clears both.
                field067C: 0x00,
                field06B8: 0x00,
                flags0673,
                flags06CC),

            0x10 => ResolveSubstate10(profile, canonicalSaint),

            _ => throw new InvalidOperationException("Unreachable special-normal exit dispatch."),
        };
    }

    public static bool IsSpecialNormalSubstate(int substate) =>
        substate is >= 0x0C and <= 0x10;

    private static PlatformSpecialNormalExitResult SelectorReentry(
        PlatformSpecialNormalExitProfile profile,
        byte canonicalSaint,
        byte reloadField050E,
        byte field067C,
        byte field06B8,
        byte flags0673,
        byte flags06CC)
    {
        // Every path reaches $E327 before interactive work. That block clears
        // $0670/$0584-$0586 and then seeds case 5 at $E345.
        return new PlatformSpecialNormalExitResult(
            Profile: profile,
            Disposition: PlatformSpecialNormalExitDisposition.ReenterInteractiveSelector,
            CanonicalSaint0533: canonicalSaint,
            ReloadField050E: reloadField050E,
            Terminal0670: 0x00,
            Field067C: field067C,
            Field06B8: field06B8,
            Flags0673: flags0673,
            Flags06CC: flags06CC,
            StoryPhase06CE: 0x00,
            SelectorState: PlatformWarmReloadInteractiveState.SeedNormalWarmReload(),
            StableResult: null);
    }

    private static PlatformSpecialNormalExitResult ResolveSubstate10(
        PlatformSpecialNormalExitProfile profile,
        byte canonicalSaint)
    {
        // The only provenance-compatible Saints are Seiya and Shun. The
        // PlatformExitGate rejects Shun for substate $10, so an accepted exit
        // is necessarily Seiya/canonical 0.
        if (canonicalSaint != 0x00)
            throw new InvalidOperationException("An accepted substate-$10 exit is reachable only for Seiya.");

        // $E26A sees inherited $0670=$01 before $ED57, changes it to $05 and
        // jumps to $E1F8. That branch refreshes $06CC from canonical Saint mask,
        // preserves the progression-$0C fields created by #104 and commits A=0.
        var stable = new PlatformNormalWarmReloadResult(
            Disposition: PlatformNormalWarmReloadDisposition.CommitStableState,
            EngineState00: 0x00,
            EngineMirror01: 0x00,
            EngineSubstate03: 0x00,
            Terminal0670: 0x05,
            Progression067D: 0x0C,
            CanonicalSaint0533: 0x00,
            ReloadField050E: 0x0C,
            StoryPhase06CE: 0x00,
            Flags0673: 0x3E,
            Flags06CC: 0x01,
            ProgressionCode06CD: 0x0E,
            Selector068F: 0x00);

        return new PlatformSpecialNormalExitResult(
            Profile: profile,
            Disposition: PlatformSpecialNormalExitDisposition.CommitStableState,
            CanonicalSaint0533: 0x00,
            ReloadField050E: 0x0C,
            Terminal0670: 0x05,
            Field067C: 0x00,
            Field06B8: 0x00,
            Flags0673: 0x3E,
            Flags06CC: 0x01,
            StoryPhase06CE: 0x00,
            SelectorState: null,
            StableResult: stable);
    }

    private static byte CanonicalSaintForInternal(PlatformSaintIndex saint) =>
        InternalSaintValue(saint) switch
        {
            0 => 0x00, // Seiya
            1 => 0x02, // Shun
            2 => 0x01, // Hyoga
            3 => 0x03, // Shiryu
            4 => 0x04, // Ikki
            var value => throw new ArgumentOutOfRangeException(
                nameof(saint), value, "Internal platform Saint index must be 0-4."),
        };

    private static int InternalSaintValue(PlatformSaintIndex saint) => Convert.ToInt32(saint);
}

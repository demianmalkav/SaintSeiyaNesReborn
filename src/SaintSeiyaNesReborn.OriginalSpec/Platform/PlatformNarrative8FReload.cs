namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformNarrative8FReloadResult(
    byte EngineState00,
    byte EngineMirror01,
    byte EngineSubstate03,
    byte Intermediate0533,
    byte ReloadField050E,
    byte Selector068F,
    bool ReturnsToMainLoopC180);

/// <summary>
/// Semantic reduction of the bounded $04=$8F branch through fixed-bank $E100.
///
/// This is deliberately not a generic $E100 model. It accepts only the exact
/// logical state produced by the closed $89 narrative terminator path:
/// $04=$8F, $00/$01=$3D and $03=$00.
///
/// Static writer audit shows that $06AB has one direct PRG writer, $E165, which
/// stores $FF during the earlier reload that creates the gameplay/narrative
/// lifecycle. The $70-$89 sequence does not write it. Therefore the returning
/// narrative path reaches $E100 with $06AB=$FF and takes $E13C -> $E257.
/// At $E263 the $8F selector jumps directly to $E20E, bypassing the generic
/// $0670/$067D destination branches.
///
/// $E121 first maps incoming $03 through table $E505 into $0533. The narrative
/// chain has already forced $03=$00 at state $72 and does not modify it later,
/// so $0533=$00. The common commit at $E22C then writes A=$00 to $00/$01 and
/// maps $0533 through $E505 once more, producing final $03=$00.
///
/// $E214 also normalizes $050E from $0F to $0D when $0533 != $03. On this exact
/// path $0533 is $00, so that side effect is deterministic if the incoming field
/// happens to be $0F. Other $050E values are preserved by the bounded branch.
/// </summary>
public static class PlatformNarrative8FReload
{
    public const byte RequiredMode04 = 0x8F;
    public const byte RequiredEntryState = 0x3D;
    public const byte RequiredEntrySubstate03 = 0x00;
    public const byte RequiredPersistent06AB = 0xFF;
    public const byte Intermediate0533 = 0x00;
    public const byte Selector068F = 0x8F;
    public const byte DestinationState = 0x00;
    public const byte DestinationSubstate03 = 0x00;

    public static PlatformNarrative8FReloadResult ResolveNarrativeReturn(
        PlatformPostExitEngineState state,
        byte persistent06AB,
        byte reloadField050E)
    {
        if (state.Mode04 != RequiredMode04
            || state.EngineState00 != RequiredEntryState
            || state.EngineMirror01 != RequiredEntryState)
        {
            throw new InvalidOperationException(
                "Narrative $8F reload requires the exact $89 handoff: $04=$8F and $00/$01=$3D.");
        }

        if (state.EngineSubstate03 != RequiredEntrySubstate03)
        {
            throw new InvalidOperationException(
                $"Narrative $8F reload requires the closed $72-$89 substate $03=$00; got ${state.EngineSubstate03:X2}.");
        }

        if (persistent06AB != RequiredPersistent06AB)
        {
            throw new InvalidOperationException(
                $"Narrative $8F reload requires persistent $06AB=$FF from the established lifecycle; got ${persistent06AB:X2}.");
        }

        var normalized050E = reloadField050E == 0x0F
            ? (byte)0x0D
            : reloadField050E;

        return new PlatformNarrative8FReloadResult(
            EngineState00: DestinationState,
            EngineMirror01: DestinationState,
            EngineSubstate03: DestinationSubstate03,
            Intermediate0533: Intermediate0533,
            ReloadField050E: normalized050E,
            Selector068F: Selector068F,
            ReturnsToMainLoopC180: true);
    }
}

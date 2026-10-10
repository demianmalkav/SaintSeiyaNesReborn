namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformNarrative8FReloadResult(
    byte EngineState00,
    byte EngineMirror01,
    byte EngineSubstate03,
    byte Intermediate0533,
    byte ReloadField050E,
    byte Selector068F,
    byte ProgressDescriptor06CD,
    byte StoryRoster0673,
    byte SaintAvailability06CC,
    byte PrgBank0639,
    ushort EndingEntryCpu,
    bool DivertsToBank0Ending,
    bool ReturnsToMainLoopC180);

/// <summary>
/// Semantic reduction of the exact $04=$8F narrative return through fixed-bank
/// $E100 and the ending-only branch inside $F381.
///
/// This is deliberately not a generic $E100 model. It accepts only the state
/// produced by the closed $89 narrative terminator path: $04=$8F,
/// $00/$01=$3D and $03=$00, with persistent $06AB=$FF.
///
/// $E121 maps incoming $03 through $E505 and therefore writes $0533=$00. The
/// warm branch $E257 sees $04=$8F and jumps to $E20E, which stores $068F=$8F
/// and calls $F381.
///
/// The crucial terminal discriminator is inside $F381. With $068F=$8F,
/// $F38D-$F3A2 writes $06CD=$20, $0673=$20 and $06CC=$21. Then
/// $F3B0-$F3BC selects PRG bank 0 through $E589 and performs JMP $BC39.
/// This is a tail jump, not a JSR/RTS continuation. Consequently control never
/// reaches the common $E214-$E254 commit on this path: $050E is not normalized,
/// $00/$01 are not rewritten to zero, and $C180 is not re-entered.
/// </summary>
public static class PlatformNarrative8FReload
{
    public const byte RequiredMode04 = 0x8F;
    public const byte RequiredEntryState = 0x3D;
    public const byte RequiredEntrySubstate03 = 0x00;
    public const byte RequiredPersistent06AB = 0xFF;

    public const byte Intermediate0533 = 0x00;
    public const byte Selector068F = 0x8F;
    public const byte EndingProgressDescriptor06CD = 0x20;
    public const byte EndingStoryRoster0673 = 0x20;
    public const byte EndingSaintAvailability06CC = 0x21;
    public const byte EndingPrgBank = 0x00;
    public const ushort EndingEntryCpu = 0xBC39;

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

        return new PlatformNarrative8FReloadResult(
            EngineState00: RequiredEntryState,
            EngineMirror01: RequiredEntryState,
            EngineSubstate03: RequiredEntrySubstate03,
            Intermediate0533: Intermediate0533,
            ReloadField050E: reloadField050E,
            Selector068F: Selector068F,
            ProgressDescriptor06CD: EndingProgressDescriptor06CD,
            StoryRoster0673: EndingStoryRoster0673,
            SaintAvailability06CC: EndingSaintAvailability06CC,
            PrgBank0639: EndingPrgBank,
            EndingEntryCpu: EndingEntryCpu,
            DivertsToBank0Ending: true,
            ReturnsToMainLoopC180: false);
    }
}

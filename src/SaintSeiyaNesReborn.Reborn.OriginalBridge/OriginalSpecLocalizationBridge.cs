using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.Reborn.Core;

namespace SaintSeiyaNesReborn.Reborn.OriginalBridge;

/// <summary>
/// Anti-corruption layer between frozen ORIGINAL SPEC semantics and the REBORN domain.
/// Raw NES addresses and entrypoints stop here.
/// </summary>
public sealed class OriginalSpecLocalizationBridge : IRebornLocalizationPort
{
    private readonly CanonicalRuntimeLocalization _canonical;

    public OriginalSpecLocalizationBridge(CanonicalRuntimeLocalization canonical)
    {
        _canonical = canonical ?? throw new ArgumentNullException(nameof(canonical));
    }

    public RebornTextRequest FromCanonicalEntrypoint(
        CanonicalMessageEntrypoint entrypoint,
        byte messageId) =>
        FromCanonicalRequest(CanonicalRuntimeLocalization.RequestFromEntrypoint(entrypoint, messageId));

    public RebornTextRequest FromCanonicalRequest(CanonicalMessageRequest request) =>
        new(
            new RebornMessageId(request.MessageId),
            request.Slot switch
            {
                CanonicalMessageSlot.Slot066A => RebornTextLane.Lane0,
                CanonicalMessageSlot.Slot066B => RebornTextLane.Lane1,
                _ => throw new ArgumentOutOfRangeException(nameof(request)),
            },
            request.Variant0672 switch
            {
                0x00 => RebornTextVariant.Variant0,
                0xFF => RebornTextVariant.Variant1,
                _ => throw new ArgumentOutOfRangeException(nameof(request)),
            });

    public RebornLocalizedText Resolve(RebornTextRequest request, RebornLanguage language)
    {
        var canonicalRequest = new CanonicalMessageRequest(
            checked((byte)request.MessageId.Value),
            request.Lane switch
            {
                RebornTextLane.Lane0 => CanonicalMessageSlot.Slot066A,
                RebornTextLane.Lane1 => CanonicalMessageSlot.Slot066B,
                _ => throw new ArgumentOutOfRangeException(nameof(request)),
            },
            request.Variant switch
            {
                RebornTextVariant.Variant0 => 0x00,
                RebornTextVariant.Variant1 => 0xFF,
                _ => throw new ArgumentOutOfRangeException(nameof(request)),
            });

        var resolved = _canonical.Resolve(
            canonicalRequest,
            language switch
            {
                RebornLanguage.Japanese => CanonicalLocalizationLanguage.Japanese,
                RebornLanguage.Spanish => CanonicalLocalizationLanguage.Spanish,
                _ => throw new ArgumentOutOfRangeException(nameof(language)),
            },
            CanonicalLocalizationFallback.Japanese);

        return new RebornLocalizedText(
            request,
            language,
            resolved.ResolvedLanguage switch
            {
                CanonicalLocalizationLanguage.Japanese => RebornLanguage.Japanese,
                CanonicalLocalizationLanguage.Spanish => RebornLanguage.Spanish,
                _ => throw new InvalidOperationException("Unsupported canonical localization language."),
            },
            resolved.Text,
            resolved.Speaker,
            resolved.Scene,
            resolved.SemanticAlias,
            resolved.UsedFallback);
    }
}

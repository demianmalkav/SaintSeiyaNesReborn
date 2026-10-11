using System.Runtime.CompilerServices;
using System.Text;
using SaintSeiyaNesReborn.OriginalSpec;

internal static class CanonicalRuntimeLocalizationChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckCanonicalEntrypoints();
        CheckDeterministicResolutionAndFallback();
        CheckCatalogCoverageGuards();
        CheckExternalCsvLoading();
        CheckNoDialoguePayloadInContract();
    }

    private static void CheckCanonicalEntrypoints()
    {
        var a = CanonicalRuntimeLocalization.RequestFromEntrypoint(CanonicalMessageEntrypoint.E7B3, 7);
        Equal((byte)7, a.MessageId, "E7B3 forwards message id");
        Equal(CanonicalMessageSlot.Slot066A, a.Slot, "E7B3 selects 066A");
        Equal((byte)0xFF, a.Variant0672, "E7B3 sets 0672 FF");

        var b = CanonicalRuntimeLocalization.RequestFromEntrypoint(CanonicalMessageEntrypoint.E7B7, 8);
        Equal(CanonicalMessageSlot.Slot066A, b.Slot, "E7B7 selects 066A");
        Equal((byte)0x00, b.Variant0672, "E7B7 sets 0672 00");

        var c = CanonicalRuntimeLocalization.RequestFromEntrypoint(CanonicalMessageEntrypoint.E7C3, 9);
        Equal(CanonicalMessageSlot.Slot066B, c.Slot, "E7C3 selects 066B");
        Equal((byte)0xFF, c.Variant0672, "E7C3 sets 0672 FF");

        var d = CanonicalRuntimeLocalization.RequestFromEntrypoint(CanonicalMessageEntrypoint.E7C7, 10);
        Equal(CanonicalMessageSlot.Slot066B, d.Slot, "E7C7 selects 066B");
        Equal((byte)0x00, d.Variant0672, "E7C7 sets 0672 00");
    }

    private static void CheckDeterministicResolutionAndFallback()
    {
        var catalog = new CanonicalRuntimeLocalization(BuildSyntheticEntries(spanishExcept: 42));
        var request = CanonicalRuntimeLocalization.RequestFromEntrypoint(CanonicalMessageEntrypoint.E7C3, 17);
        var es = catalog.Resolve(request, CanonicalLocalizationLanguage.Spanish);

        Equal("MSG_017", es.StableId, "stable id survives resolution");
        Equal("ES_SYNTH_017", es.Text, "Spanish payload selected");
        Equal(CanonicalLocalizationLanguage.Spanish, es.ResolvedLanguage, "Spanish resolves directly");
        Equal(CanonicalMessageSlot.Slot066B, es.Request.Slot, "slot metadata propagates");
        Equal((byte)0xFF, es.Request.Variant0672, "variant metadata propagates");
        True(!es.UsedFallback, "direct Spanish resolution does not report fallback");

        var missingEsRequest = CanonicalRuntimeLocalization.RequestFromEntrypoint(CanonicalMessageEntrypoint.E7B7, 42);
        var fallback = catalog.Resolve(missingEsRequest, CanonicalLocalizationLanguage.Spanish);
        Equal("JP_SYNTH_042", fallback.Text, "missing Spanish falls back to Japanese");
        Equal(CanonicalLocalizationLanguage.Japanese, fallback.ResolvedLanguage, "fallback language is explicit");
        True(fallback.UsedFallback, "fallback is reported");

        Throws<InvalidOperationException>(
            () => catalog.Resolve(missingEsRequest, CanonicalLocalizationLanguage.Spanish, CanonicalLocalizationFallback.None),
            "fallback none rejects missing requested payload");
    }

    private static void CheckCatalogCoverageGuards()
    {
        Throws<ArgumentException>(
            () => new CanonicalRuntimeLocalization(BuildSyntheticEntries().Take(250)),
            "incomplete catalog rejected");

        var duplicate = BuildSyntheticEntries().ToArray();
        duplicate[250] = duplicate[249] with { Japanese = "JP_DUP" };
        Throws<ArgumentException>(
            () => new CanonicalRuntimeLocalization(duplicate),
            "duplicate id rejected");

        var wrongStable = BuildSyntheticEntries().ToArray();
        wrongStable[3] = wrongStable[3] with { StableId = "WRONG" };
        Throws<ArgumentException>(
            () => new CanonicalRuntimeLocalization(wrongStable),
            "stable identity mismatch rejected");
    }

    private static void CheckExternalCsvLoading()
    {
        var csv = new StringBuilder();
        csv.AppendLine("id,stable_id,jp_original,es_draft,status,speaker,scene,semantic_alias,source_text_offset");
        for (var id = 0; id < CanonicalRuntimeLocalization.MessageCount; id++)
        {
            var jp = id == 5 ? "\"JP, quoted\"" : $"JP_SYNTH_{id:000}";
            var es = id == 6 ? "\"ES \"\"quoted\"\"\"" : $"ES_SYNTH_{id:000}";
            csv.Append(id).Append(',')
               .Append($"MSG_{id:000}").Append(',')
               .Append(jp).Append(',')
               .Append(es).Append(',')
               .Append("DRAFT,,SYNTH_SCENE,,0x1000")
               .AppendLine();
        }

        var catalog = CanonicalRuntimeLocalization.LoadCsv(new StringReader(csv.ToString()));
        Equal("JP, quoted", catalog[5].Japanese, "CSV quoted comma decoded");
        Equal("ES \"quoted\"", catalog[6].Spanish, "CSV escaped quote decoded");
        Equal("MSG_250", catalog[250].StableId, "CSV loader preserves full 0..250 coverage");
    }

    private static void CheckNoDialoguePayloadInContract()
    {
        var assembly = typeof(CanonicalRuntimeLocalization).Assembly;
        var resourceNames = assembly.GetManifestResourceNames();
        True(!resourceNames.Any(name => name.Contains("dialog", StringComparison.OrdinalIgnoreCase)), "no dialogue resource embedded");
        True(!resourceNames.Any(name => name.Contains("localization", StringComparison.OrdinalIgnoreCase)), "no localization catalog resource embedded");
    }

    private static IEnumerable<CanonicalLocalizationEntry> BuildSyntheticEntries(int? spanishExcept = null)
    {
        for (var id = 0; id < CanonicalRuntimeLocalization.MessageCount; id++)
        {
            yield return new CanonicalLocalizationEntry(
                (byte)id,
                $"MSG_{id:000}",
                $"JP_SYNTH_{id:000}",
                spanishExcept == id ? "" : $"ES_SYNTH_{id:000}",
                spanishExcept == id ? "RAW" : "DRAFT",
                "",
                "SYNTH_SCENE",
                "",
                "0x1000");
        }
    }

    private static void Equal<T>(T expected, T actual, string name) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"{name}: expected {expected}, got {actual}");
    }

    private static void True(bool value, string name)
    {
        if (!value)
            throw new InvalidOperationException($"{name}: expected true");
    }

    private static void Throws<TException>(Action action, string name) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"{name}: expected {typeof(TException).Name}, got {ex.GetType().Name}");
        }

        throw new InvalidOperationException($"{name}: expected {typeof(TException).Name}");
    }
}

using System.Collections.ObjectModel;
using System.Text;

namespace SaintSeiyaNesReborn.OriginalSpec;

public enum CanonicalMessageSlot : byte
{
    Slot066A = 0,
    Slot066B = 1,
}

public enum CanonicalMessageEntrypoint : ushort
{
    E7B3 = 0xE7B3,
    E7B7 = 0xE7B7,
    E7C3 = 0xE7C3,
    E7C7 = 0xE7C7,
}

public enum CanonicalLocalizationLanguage : byte
{
    Japanese = 0,
    Spanish = 1,
}

public enum CanonicalLocalizationFallback : byte
{
    None = 0,
    Japanese = 1,
}

/// <summary>
/// Canonical runtime request state represented by the original fixed-bank entrypoints.
/// Variant0672 intentionally remains a raw byte: $00 and $FF are proven, but no stronger
/// presentation name is assigned without contradictory/new canonical evidence.
/// </summary>
public readonly record struct CanonicalMessageRequest(
    byte MessageId,
    CanonicalMessageSlot Slot,
    byte Variant0672)
{
    public string StableId => $"MSG_{MessageId:000}";
}

public sealed record CanonicalLocalizationEntry(
    byte Id,
    string StableId,
    string Japanese,
    string Spanish,
    string Status,
    string Speaker,
    string Scene,
    string SemanticAlias,
    string SourceTextOffset);

public readonly record struct CanonicalResolvedMessage(
    CanonicalMessageRequest Request,
    CanonicalLocalizationLanguage RequestedLanguage,
    CanonicalLocalizationLanguage ResolvedLanguage,
    string StableId,
    string Text,
    string Speaker,
    string Scene,
    string SemanticAlias,
    string SourceTextOffset,
    bool UsedFallback);

/// <summary>
/// Content-independent clean-room runtime contract for canonical MSG_000..MSG_250.
/// The catalog is supplied externally; no Japanese or Spanish dialogue payload is embedded
/// in this assembly.
/// </summary>
public sealed class CanonicalRuntimeLocalization
{
    public const int MessageCount = 251;

    private static readonly string[] RequiredCsvColumns =
    [
        "id",
        "stable_id",
        "jp_original",
        "es_draft",
        "status",
        "speaker",
        "scene",
        "semantic_alias",
        "source_text_offset",
    ];

    private readonly IReadOnlyDictionary<byte, CanonicalLocalizationEntry> _entries;

    public CanonicalRuntimeLocalization(IEnumerable<CanonicalLocalizationEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var materialized = entries.ToArray();
        if (materialized.Length != MessageCount)
            throw new ArgumentException($"Expected exactly {MessageCount} localization entries, found {materialized.Length}.", nameof(entries));

        var byId = new Dictionary<byte, CanonicalLocalizationEntry>(MessageCount);
        foreach (var entry in materialized)
        {
            ValidateEntry(entry);
            if (!byId.TryAdd(entry.Id, entry))
                throw new ArgumentException($"Duplicate canonical message id {entry.Id}.", nameof(entries));
        }

        for (var id = 0; id < MessageCount; id++)
        {
            if (!byId.ContainsKey((byte)id))
                throw new ArgumentException($"Missing canonical message id {id}.", nameof(entries));
        }

        _entries = new ReadOnlyDictionary<byte, CanonicalLocalizationEntry>(byId);
    }

    public CanonicalLocalizationEntry this[byte messageId] =>
        _entries.TryGetValue(messageId, out var entry)
            ? entry
            : throw new ArgumentOutOfRangeException(nameof(messageId), "Canonical message id must be in 0..250.");

    public static CanonicalMessageRequest RequestFromEntrypoint(CanonicalMessageEntrypoint entrypoint, byte messageId) =>
        entrypoint switch
        {
            CanonicalMessageEntrypoint.E7B3 => new(messageId, CanonicalMessageSlot.Slot066A, 0xFF),
            CanonicalMessageEntrypoint.E7B7 => new(messageId, CanonicalMessageSlot.Slot066A, 0x00),
            CanonicalMessageEntrypoint.E7C3 => new(messageId, CanonicalMessageSlot.Slot066B, 0xFF),
            CanonicalMessageEntrypoint.E7C7 => new(messageId, CanonicalMessageSlot.Slot066B, 0x00),
            _ => throw new ArgumentOutOfRangeException(nameof(entrypoint)),
        };

    public CanonicalResolvedMessage Resolve(
        CanonicalMessageRequest request,
        CanonicalLocalizationLanguage language,
        CanonicalLocalizationFallback fallback = CanonicalLocalizationFallback.Japanese)
    {
        if (request.MessageId >= MessageCount)
            throw new ArgumentOutOfRangeException(nameof(request), "Canonical message id must be in 0..250.");
        if (request.Variant0672 is not (0x00 or 0xFF))
            throw new ArgumentOutOfRangeException(nameof(request), "Canonical $0672 request variant must be $00 or $FF.");

        var entry = this[request.MessageId];
        var requestedText = language switch
        {
            CanonicalLocalizationLanguage.Japanese => entry.Japanese,
            CanonicalLocalizationLanguage.Spanish => entry.Spanish,
            _ => throw new ArgumentOutOfRangeException(nameof(language)),
        };

        var resolvedLanguage = language;
        var usedFallback = false;
        var text = requestedText;

        if (string.IsNullOrWhiteSpace(text))
        {
            if (language == CanonicalLocalizationLanguage.Spanish && fallback == CanonicalLocalizationFallback.Japanese)
            {
                text = entry.Japanese;
                resolvedLanguage = CanonicalLocalizationLanguage.Japanese;
                usedFallback = true;
            }
            else
            {
                throw new InvalidOperationException($"{entry.StableId} has no text for {language} and fallback policy is {fallback}.");
            }
        }

        return new(
            request,
            language,
            resolvedLanguage,
            entry.StableId,
            text,
            entry.Speaker,
            entry.Scene,
            entry.SemanticAlias,
            entry.SourceTextOffset,
            usedFallback);
    }

    public static CanonicalRuntimeLocalization LoadCsv(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        var rows = ParseCsv(reader).ToArray();
        if (rows.Length == 0)
            throw new FormatException("Localization catalog is empty.");

        var header = rows[0];
        var index = header
            .Select((name, i) => (name: name.Trim().TrimStart('\uFEFF'), i))
            .ToDictionary(x => x.name, x => x.i, StringComparer.Ordinal);

        foreach (var required in RequiredCsvColumns)
        {
            if (!index.ContainsKey(required))
                throw new FormatException($"Missing required localization column {required}.");
        }

        var entries = new List<CanonicalLocalizationEntry>();
        foreach (var row in rows.Skip(1))
        {
            if (row.Count == 1 && string.IsNullOrWhiteSpace(row[0]))
                continue;
            if (row.Count != header.Count)
                throw new FormatException($"Localization CSV row has {row.Count} fields; expected {header.Count}.");

            var rawId = row[index["id"]].Trim();
            if (!byte.TryParse(rawId, out var id))
                throw new FormatException($"Invalid localization id {rawId}.");

            entries.Add(new CanonicalLocalizationEntry(
                id,
                row[index["stable_id"]].Trim(),
                row[index["jp_original"]],
                row[index["es_draft"]],
                row[index["status"]].Trim(),
                row[index["speaker"]],
                row[index["scene"]],
                row[index["semantic_alias"]].Trim(),
                row[index["source_text_offset"]].Trim()));
        }

        return new CanonicalRuntimeLocalization(entries);
    }

    private static void ValidateEntry(CanonicalLocalizationEntry entry)
    {
        if (entry.Id >= MessageCount)
            throw new ArgumentException($"Canonical message id {entry.Id} is outside 0..250.");

        var expectedStableId = $"MSG_{entry.Id:000}";
        if (!string.Equals(entry.StableId, expectedStableId, StringComparison.Ordinal))
            throw new ArgumentException($"Stable id for message {entry.Id} must be {expectedStableId}.");
        if (string.IsNullOrWhiteSpace(entry.Japanese))
            throw new ArgumentException($"{expectedStableId} must retain its Japanese source text in the external catalog.");
    }

    private static IEnumerable<IReadOnlyList<string>> ParseCsv(TextReader reader)
    {
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;

        while (true)
        {
            var next = reader.Read();
            if (next < 0)
            {
                if (quoted)
                    throw new FormatException("Unterminated quoted CSV field.");
                row.Add(field.ToString());
                if (row.Count > 1 || row[0].Length > 0)
                    yield return row;
                yield break;
            }

            var ch = (char)next;
            if (quoted)
            {
                if (ch == '"')
                {
                    if (reader.Peek() == '"')
                    {
                        reader.Read();
                        field.Append('"');
                    }
                    else
                    {
                        quoted = false;
                    }
                }
                else
                {
                    field.Append(ch);
                }
                continue;
            }

            switch (ch)
            {
                case '"' when field.Length == 0:
                    quoted = true;
                    break;
                case ',':
                    row.Add(field.ToString());
                    field.Clear();
                    break;
                case '\r':
                    if (reader.Peek() == '\n')
                        reader.Read();
                    row.Add(field.ToString());
                    yield return row;
                    row = new List<string>();
                    field.Clear();
                    break;
                case '\n':
                    row.Add(field.ToString());
                    yield return row;
                    row = new List<string>();
                    field.Clear();
                    break;
                default:
                    field.Append(ch);
                    break;
            }
        }
    }
}

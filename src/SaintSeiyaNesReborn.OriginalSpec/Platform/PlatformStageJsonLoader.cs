using System.Text.Json;

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public sealed class PlatformStageDocument
{
    private readonly IReadOnlyDictionary<int, PlatformStageMap> _substates;

    public string Format { get; }
    public string SourceCoreCrc32 { get; }
    public IReadOnlyDictionary<int, PlatformStageMap> Substates => _substates;

    internal PlatformStageDocument(
        string format,
        string sourceCoreCrc32,
        IReadOnlyDictionary<int, PlatformStageMap> substates)
    {
        Format = format;
        SourceCoreCrc32 = sourceCoreCrc32;
        _substates = substates;
    }

    public PlatformStageMap GetSubstate(int substate) =>
        _substates.TryGetValue(substate, out var stage)
            ? stage
            : throw new KeyNotFoundException($"Platform substate ${substate:X2} is not present.");
}

/// <summary>
/// Loads the private/local JSON emitted by tools/reverse/export_platform_stage_spec.py.
/// No ROM data is embedded in this library.
/// </summary>
public static class PlatformStageJsonLoader
{
    public const string ExpectedFormat = "SaintSeiyaNesReborn.PlatformStageSpec.v1";

    public static PlatformStageDocument Load(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var format = root.GetProperty("format").GetString()
            ?? throw new InvalidDataException("Stage spec format is missing.");
        if (!string.Equals(format, ExpectedFormat, StringComparison.Ordinal))
            throw new InvalidDataException($"Unsupported stage spec format '{format}'.");

        var crc = root.GetProperty("source_core_crc32").GetString() ?? string.Empty;
        var maps = new Dictionary<int, PlatformStageMap>();

        foreach (var substateElement in root.GetProperty("substates").EnumerateArray())
        {
            var substate = substateElement.GetProperty("substate").GetInt32();
            var pages = new List<PlatformStagePage>();

            foreach (var pageElement in substateElement.GetProperty("pages").EnumerateArray())
            {
                if (!pageElement.TryGetProperty("metatile_rows_11x16", out var gridElement))
                {
                    throw new InvalidDataException(
                        "Stage spec does not contain descriptor grids. Regenerate it with --include-grids.");
                }

                var descriptors = ParseGrid(gridElement);
                var primary = ParsePrimary(pageElement.GetProperty("primary_encounter"));
                var secondary = ParseSecondary(pageElement.GetProperty("secondary_archetype"));
                int? poolPageId = null;
                if (pageElement.TryGetProperty("pool_page_id", out var poolElement)
                    && poolElement.ValueKind != JsonValueKind.Null)
                {
                    poolPageId = poolElement.GetInt32();
                }

                pages.Add(new PlatformStagePage(
                    pageIndex: pageElement.GetProperty("page_index").GetInt32(),
                    descriptorsRowMajor: descriptors,
                    primaryEncounter: primary,
                    secondaryArchetype: secondary,
                    poolPageId: poolPageId));
            }

            var map = new PlatformStageMap(
                substate,
                pages,
                dataPrgBank: substateElement.GetProperty("data_prg_bank").GetInt32(),
                metatileDefinitionBase: substateElement.GetProperty("metatile_definition_base").GetInt32(),
                spriteChrBank4K: substateElement.GetProperty("chr0_sprite_bank_4k").GetInt32(),
                backgroundChrBank4K: substateElement.GetProperty("chr1_background_bank_4k").GetInt32());

            if (!maps.TryAdd(substate, map))
                throw new InvalidDataException($"Duplicate platform substate ${substate:X2}.");
        }

        return new PlatformStageDocument(format, crc, maps);
    }

    private static byte[] ParseGrid(JsonElement gridElement)
    {
        var descriptors = new byte[PlatformStagePage.DescriptorCount];
        var row = 0;
        foreach (var rowElement in gridElement.EnumerateArray())
        {
            if (row >= PlatformStagePage.Rows)
                throw new InvalidDataException("Platform page has too many descriptor rows.");

            var column = 0;
            foreach (var valueElement in rowElement.EnumerateArray())
            {
                if (column >= PlatformStagePage.Columns)
                    throw new InvalidDataException("Platform page has too many descriptor columns.");

                descriptors[row * PlatformStagePage.Columns + column] = valueElement.GetByte();
                column++;
            }

            if (column != PlatformStagePage.Columns)
                throw new InvalidDataException("Platform page descriptor row must contain 16 columns.");
            row++;
        }

        if (row != PlatformStagePage.Rows)
            throw new InvalidDataException("Platform page must contain 11 descriptor rows.");

        return descriptors;
    }

    private static PlatformPrimaryEncounter ParsePrimary(JsonElement element)
    {
        PlatformEncounterStats? stats = null;
        if (element.TryGetProperty("config", out var config) && config.ValueKind == JsonValueKind.Object)
        {
            stats = new PlatformEncounterStats(
                HitPoints: config.GetProperty("hp").GetInt32(),
                CosmoDrainTicks: config.GetProperty("cosmo_drain_ticks").GetByte(),
                LifeDrainTicks: config.GetProperty("life_drain_ticks").GetByte(),
                SeventhSenseReward: config.GetProperty("seventh_sense_reward").GetInt32());
        }

        return new PlatformPrimaryEncounter(
            Raw: element.GetProperty("raw").GetByte(),
            TypeId: element.GetProperty("type").GetByte(),
            Tier: element.GetProperty("tier").GetByte(),
            SecondCommonSlotEnabled: element.GetProperty("second_common_slot_enabled").GetBoolean(),
            Bit6Unknown: element.GetProperty("bit6_unknown").GetBoolean(),
            Stats: stats);
    }

    private static PlatformEntityArchetype? ParseSecondary(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Null)
            return null;

        return new PlatformEntityArchetype(
            Id: element.GetProperty("id").GetByte(),
            HitPoints: element.GetProperty("hp").GetInt32(),
            CosmoDrainTicks: element.GetProperty("cosmo_drain_ticks").GetByte(),
            LifeDrainTicks: element.GetProperty("life_drain_ticks").GetByte(),
            SeventhSenseReward: element.GetProperty("seventh_sense_reward").GetInt32());
    }
}

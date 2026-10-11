namespace SaintSeiyaNesReborn.Reborn.Core;

public readonly record struct RebornMessageId
{
    public const int MaxValue = 250;

    public int Value { get; }

    public RebornMessageId(int value)
    {
        if (value is < 0 or > MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value), "REBORN canonical message id must be in 0..250.");

        Value = value;
    }

    public string StableId => $"MSG_{Value:000}";

    public override string ToString() => StableId;
}

public enum RebornTextLane : byte
{
    Lane0 = 0,
    Lane1 = 1,
}

public enum RebornTextVariant : byte
{
    Variant0 = 0,
    Variant1 = 1,
}

public enum RebornLanguage : byte
{
    Japanese = 0,
    Spanish = 1,
}

public readonly record struct RebornTextRequest(
    RebornMessageId MessageId,
    RebornTextLane Lane,
    RebornTextVariant Variant);

public sealed record RebornLocalizedText(
    RebornTextRequest Request,
    RebornLanguage RequestedLanguage,
    RebornLanguage ResolvedLanguage,
    string Text,
    string Speaker,
    string Scene,
    string SemanticAlias,
    bool UsedFallback);

public interface IRebornLocalizationPort
{
    RebornLocalizedText Resolve(RebornTextRequest request, RebornLanguage language);
}

public readonly record struct RebornAudioCue(string CueId);

public sealed record RebornFrameInput(
    RebornLanguage Language,
    IReadOnlyList<RebornTextRequest> TextRequests)
{
    public static RebornFrameInput Empty(RebornLanguage language = RebornLanguage.Spanish) =>
        new(language, Array.Empty<RebornTextRequest>());
}

public sealed record RebornFrameOutput(
    ulong Tick,
    IReadOnlyList<RebornLocalizedText> Text,
    IReadOnlyList<RebornAudioCue> Audio);

public interface IRebornPresentationAdapter
{
    void Present(RebornFrameOutput frame);
}

public interface IRebornAudioAdapter
{
    void Present(ulong tick, IReadOnlyList<RebornAudioCue> cues);
}

public sealed record RebornSaveSnapshot(int SchemaVersion, ulong Tick);

public interface IRebornSaveStore
{
    void Save(string slot, RebornSaveSnapshot snapshot);
    RebornSaveSnapshot? Load(string slot);
}

/// <summary>
/// Deterministic, platform-independent REBORN update loop. Host time is converted into
/// discrete calls to Step; wall-clock time, rendering APIs and audio devices never enter
/// the gameplay/domain state.
/// </summary>
public sealed class RebornRuntime
{
    public const int CurrentSaveSchemaVersion = 1;

    private readonly IRebornLocalizationPort _localization;

    public RebornRuntime(IRebornLocalizationPort localization)
    {
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
    }

    public ulong Tick { get; private set; }

    public RebornFrameOutput Step(RebornFrameInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.TextRequests);

        var nextTick = checked(Tick + 1);
        var text = input.TextRequests
            .Select(request => _localization.Resolve(request, input.Language))
            .ToArray();

        Tick = nextTick;
        return new RebornFrameOutput(nextTick, text, Array.Empty<RebornAudioCue>());
    }

    public RebornSaveSnapshot CaptureSave() =>
        new(CurrentSaveSchemaVersion, Tick);

    public void Restore(RebornSaveSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.SchemaVersion != CurrentSaveSchemaVersion)
            throw new InvalidOperationException(
                $"Unsupported REBORN save schema {snapshot.SchemaVersion}; expected {CurrentSaveSchemaVersion}.");

        Tick = snapshot.Tick;
    }
}

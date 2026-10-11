using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.Reborn.Core;
using SaintSeiyaNesReborn.Reborn.OriginalBridge;

static void Check(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static CanonicalRuntimeLocalization BuildSyntheticCatalog()
{
    var entries = Enumerable.Range(0, CanonicalRuntimeLocalization.MessageCount)
        .Select(id => new CanonicalLocalizationEntry(
            checked((byte)id),
            $"MSG_{id:000}",
            $"JP_SYNTH_{id:000}",
            id == 42 ? "ES_SYNTH_042" : string.Empty,
            "fixture",
            $"speaker_{id:000}",
            $"scene_{id:000}",
            $"alias_{id:000}",
            $"offset_{id:000}"));

    return new CanonicalRuntimeLocalization(entries);
}

var catalog = BuildSyntheticCatalog();
var bridge = new OriginalSpecLocalizationBridge(catalog);

var mappingCases = new[]
{
    (CanonicalMessageEntrypoint.E7B3, RebornTextLane.Lane0, RebornTextVariant.Variant1),
    (CanonicalMessageEntrypoint.E7B7, RebornTextLane.Lane0, RebornTextVariant.Variant0),
    (CanonicalMessageEntrypoint.E7C3, RebornTextLane.Lane1, RebornTextVariant.Variant1),
    (CanonicalMessageEntrypoint.E7C7, RebornTextLane.Lane1, RebornTextVariant.Variant0),
};

foreach (var (entrypoint, expectedLane, expectedVariant) in mappingCases)
{
    var mapped = bridge.FromCanonicalEntrypoint(entrypoint, 42);
    Check(mapped.MessageId.Value == 42, $"{entrypoint} changed canonical message identity.");
    Check(mapped.MessageId.StableId == "MSG_042", $"{entrypoint} changed stable message identity.");
    Check(mapped.Lane == expectedLane, $"{entrypoint} mapped to the wrong REBORN lane.");
    Check(mapped.Variant == expectedVariant, $"{entrypoint} mapped to the wrong opaque REBORN variant.");
}

var coreReferences = typeof(RebornRuntime).Assembly
    .GetReferencedAssemblies()
    .Select(reference => reference.Name)
    .Where(name => name is not null)
    .ToArray();
Check(
    !coreReferences.Contains("SaintSeiyaNesReborn.OriginalSpec", StringComparer.Ordinal),
    "REBORN Core must not reference ORIGINAL SPEC directly.");

var request = bridge.FromCanonicalEntrypoint(CanonicalMessageEntrypoint.E7B3, 42);
var input = new RebornFrameInput(RebornLanguage.Spanish, new[] { request });
var runtimeA = new RebornRuntime(bridge);
var runtimeB = new RebornRuntime(bridge);
var frameA = runtimeA.Step(input);
var frameB = runtimeB.Step(input);

Check(frameA.Tick == 1 && frameB.Tick == 1, "Deterministic runtimes must advance exactly one tick per Step call.");
Check(frameA.Text.Count == 1 && frameB.Text.Count == 1, "Text vertical slice must emit exactly one resolved text command.");
Check(frameA.Text[0] == frameB.Text[0], "Equal initial state plus equal input must produce equal semantic output.");
Check(frameA.Text[0].Text == "ES_SYNTH_042", "Spanish catalog selection did not cross the bridge correctly.");
Check(frameA.Text[0].ResolvedLanguage == RebornLanguage.Spanish, "Spanish fixture resolved to the wrong language.");
Check(!frameA.Text[0].UsedFallback, "Complete Spanish fixture unexpectedly used fallback.");

var fallbackRequest = bridge.FromCanonicalEntrypoint(CanonicalMessageEntrypoint.E7C7, 43);
var fallback = runtimeA.Step(new RebornFrameInput(RebornLanguage.Spanish, new[] { fallbackRequest })).Text.Single();
Check(fallback.Text == "JP_SYNTH_043", "Missing Spanish text must use the frozen Japanese fallback policy.");
Check(fallback.ResolvedLanguage == RebornLanguage.Japanese && fallback.UsedFallback, "Fallback metadata was not preserved.");
Check(fallback.Request.Lane == RebornTextLane.Lane1, "Lane metadata changed during localization resolution.");
Check(fallback.Request.Variant == RebornTextVariant.Variant0, "Variant metadata changed during localization resolution.");

var saved = runtimeA.CaptureSave();
runtimeA.Step(RebornFrameInput.Empty());
Check(runtimeA.Tick == saved.Tick + 1, "Runtime did not advance after save capture.");
runtimeA.Restore(saved);
Check(runtimeA.Tick == saved.Tick, "Versioned REBORN save restore did not recover deterministic tick state.");

var rejectedUnknownSchema = false;
try
{
    runtimeA.Restore(new RebornSaveSnapshot(RebornRuntime.CurrentSaveSchemaVersion + 1, 99));
}
catch (InvalidOperationException)
{
    rejectedUnknownSchema = true;
}
Check(rejectedUnknownSchema, "Unknown REBORN save schema must be rejected explicitly.");

Console.WriteLine("REBORN architecture self-test PASS");

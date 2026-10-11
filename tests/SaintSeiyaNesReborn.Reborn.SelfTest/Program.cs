using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;
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

static PlatformStageMap BuildOpenHorizontalStage()
{
    var empty = new byte[PlatformStagePage.DescriptorCount];
    return new PlatformStageMap(
        substate: 0,
        pages: Enumerable.Range(0, 6)
            .Select(page => new PlatformStagePage(page, empty))
            .ToArray());
}

static PlatformInput ToCanonicalInput(RebornHorizontalInput input) => input switch
{
    RebornHorizontalInput.Neutral => PlatformInput.None,
    RebornHorizontalInput.Left => PlatformInput.Left,
    RebornHorizontalInput.Right => PlatformInput.Right,
    _ => throw new ArgumentOutOfRangeException(nameof(input)),
};

static void CheckHorizontalParity(
    PlatformStageMap stage,
    PlatformCollisionDescriptors openProbes,
    PlatformSaintIndex saint,
    byte frameCounter,
    PlatformHorizontalState canonicalInitial,
    RebornHorizontalInput input,
    string name)
{
    var canonicalInput = ToCanonicalInput(input);
    var groundedStep = PlatformMovementIncrements.FromFrame(saint, frameCounter).Grounded0387;
    var canonical = PlatformHorizontalMotion.StepGrounded(
        stage,
        canonicalInitial,
        canonicalInput,
        openProbes,
        groundedStep);

    var rebornInitial = OriginalSpecPlatformHorizontalBridge.FromCanonicalState(
        canonicalInitial,
        frameCounter);
    var profile = OriginalSpecPlatformHorizontalBridge.ProfileFor(saint);
    var reborn = RebornPlatformPlayerHorizontalLocomotion.Step(rebornInitial, input, profile);
    var expected = OriginalSpecPlatformHorizontalBridge.FromCanonicalState(
        canonical.State,
        unchecked((byte)(frameCounter + 1)),
        isLocomoting: input != RebornHorizontalInput.Neutral);

    Check(reborn.State.WorldX == expected.WorldX, $"{name}: REBORN world X diverged from canonical projection.");
    Check(reborn.State.Facing == expected.Facing, $"{name}: REBORN facing diverged from canonical projection.");
    Check(reborn.State.Phase == expected.Phase, $"{name}: REBORN logical phase diverged from canonical frame parity.");
    Check(reborn.State.IsLocomoting == expected.IsLocomoting, $"{name}: locomotion semantic state diverged.");
    Check(
        reborn.AppliedDelta == expected.WorldX - rebornInitial.WorldX,
        $"{name}: reported horizontal delta diverged from canonical world movement.");
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

// First gameplay vertical slice: project the frozen grounded original into a
// camera-independent world-space locomotion contract.
var horizontalStage = BuildOpenHorizontalStage();
var openProbes = new PlatformCollisionDescriptors(null, 0, 0, null, 0, 0, null, null);

CheckHorizontalParity(
    horizontalStage,
    openProbes,
    PlatformSaintIndex.Seiya,
    frameCounter: 0,
    new PlatformHorizontalState(0x7F, 0x00, 0x00, 0x00),
    RebornHorizontalInput.Right,
    "right before camera handoff");

CheckHorizontalParity(
    horizontalStage,
    openProbes,
    PlatformSaintIndex.Seiya,
    frameCounter: 0,
    new PlatformHorizontalState(0x80, 0x00, 0x00, 0x40),
    RebornHorizontalInput.Right,
    "right during canonical camera handoff");

CheckHorizontalParity(
    horizontalStage,
    openProbes,
    PlatformSaintIndex.Seiya,
    frameCounter: 0,
    new PlatformHorizontalState(0x40, 0x88, 0x03, 0x40),
    RebornHorizontalInput.Left,
    "left with nonzero canonical scroll");

CheckHorizontalParity(
    horizontalStage,
    openProbes,
    PlatformSaintIndex.Seiya,
    frameCounter: 0,
    new PlatformHorizontalState(0x40, 0x00, 0x00, 0x40),
    RebornHorizontalInput.Neutral,
    "neutral preserves position and facing");

CheckHorizontalParity(
    horizontalStage,
    openProbes,
    PlatformSaintIndex.Shun,
    frameCounter: 0,
    new PlatformHorizontalState(0x20, 0x00, 0x00, 0x00),
    RebornHorizontalInput.Right,
    "distinct profile even phase");

CheckHorizontalParity(
    horizontalStage,
    openProbes,
    PlatformSaintIndex.Shun,
    frameCounter: 1,
    new PlatformHorizontalState(0x20, 0x00, 0x00, 0x00),
    RebornHorizontalInput.Right,
    "distinct profile odd phase");

var seiyaProfile = OriginalSpecPlatformHorizontalBridge.ProfileFor(PlatformSaintIndex.Seiya);
var shunProfile = OriginalSpecPlatformHorizontalBridge.ProfileFor(PlatformSaintIndex.Shun);
Check(seiyaProfile == new RebornGroundedHorizontalProfile(1, 1), "Canonical Seiya grounded profile must project to 1/1.");
Check(shunProfile == new RebornGroundedHorizontalProfile(1, 2), "Canonical Shun grounded profile must project to 1/2.");
Check(
    OriginalSpecPlatformHorizontalBridge.FromCanonicalInput(PlatformInput.Left | PlatformInput.Right) == RebornHorizontalInput.Right,
    "Canonical simultaneous directions must preserve Right priority at the bridge.");

var sequence = new[]
{
    RebornHorizontalInput.Right,
    RebornHorizontalInput.Right,
    RebornHorizontalInput.Neutral,
    RebornHorizontalInput.Left,
    RebornHorizontalInput.Right,
};
var initialSemantic = new RebornPlatformPlayerHorizontalState(
    WorldX: 100,
    Facing: RebornFacing.Left,
    Phase: RebornMotionPhase.Even,
    IsLocomoting: false);
var sequenceA = initialSemantic;
var sequenceB = initialSemantic;
foreach (var horizontalInput in sequence)
{
    sequenceA = RebornPlatformPlayerHorizontalLocomotion.Step(sequenceA, horizontalInput, shunProfile).State;
    sequenceB = RebornPlatformPlayerHorizontalLocomotion.Step(sequenceB, horizontalInput, shunProfile).State;
}
Check(sequenceA == sequenceB, "Equal horizontal initial state and input sequence must be deterministic.");
Check(sequenceA.WorldX == 102, "Distinct 1/2 cadence sequence produced the wrong final world position.");
Check(sequenceA.Facing == RebornFacing.Right, "Final directional input must own facing.");
Check(sequenceA.Phase == RebornMotionPhase.Odd, "Five logical horizontal ticks must end on odd phase.");
Check(sequenceA.IsLocomoting, "Final directional input must leave locomotion active.");

Console.WriteLine("REBORN architecture + horizontal locomotion self-test PASS");

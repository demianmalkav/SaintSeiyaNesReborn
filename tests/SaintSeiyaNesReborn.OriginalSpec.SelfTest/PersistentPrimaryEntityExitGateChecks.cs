using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class PersistentPrimaryEntityExitGateChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckNormalExitStopsAfterEarlyCommonProducer();
        CheckShunSubstate10RejectionContinues();
        CheckSubstate11UsesSpecial70Transition();
        CheckNonExitPathMatchesCompatibilityEntryPoint();
    }

    private static void CheckNormalExitStopsAfterEarlyCommonProducer()
    {
        var stage = Stage(Page(0, raw: 0x06, hp: 15));
        var initial = State(Config(0x06, 15), staged: 0x06, cooldown: 0, lastTrigger: 0x22);
        var player = Player(PlatformSaintIndex.Seiya, x: 0xD0, y: 0x40, jump: 0);

        var result = StepFull(
            stage,
            initial,
            player,
            substate: 0x01,
            cameraLow44: 0x02,
            cameraDelta43: 0,
            scheduledEntries: Array.Empty<PlatformSpecialSpawnEntry>());

        Require(result.Outcome == PlatformPersistentPrimaryEntityMainThreadOutcome.State3DReload
            && result.ExitTransition == PlatformExitTransitionKind.State3DReload,
            "main-sequence gate returns explicit $3D reload outcome");
        Require(result.EarlyProducer.CommonEdge is not null,
            "$B6D0 phase executes before the exit gate");
        Require(result.EarlyProducer.CommonEdge!.Value.SlotA.Spawned,
            "early common producer can mutate slot A before an accepted exit");
        Require(result.State.SlotA.Entity.Motion.Type == 0x06
            && result.State.SlotA.VisualSpritePlus1 == 0xFD,
            "pre-gate common spawn persists in returned state");
        Require(result.Producer is null && result.Hybrid is null,
            "accepted exit suppresses $8927 and all later player/entity phases");
        Require(result.State.LastTriggerLow03A2 == 0x22,
            "scheduled-trigger latch is untouched because $8927 did not execute");
        Require(result.State.FrameCounter3C == initial.FrameCounter3C,
            "accepted exit does not advance shared $3C");
    }

    private static void CheckShunSubstate10RejectionContinues()
    {
        var stage = Stage(Page(0, raw: 0xA8, hp: 80));
        var initial = State(Config(0xA8, 80), staged: 0xA8, cooldown: 0, lastTrigger: 0x10);
        var player = Player(PlatformSaintIndex.Shun, x: 0xB4, y: 0x70, jump: 0);

        var result = StepFull(
            stage,
            initial,
            player,
            substate: 0x10,
            cameraLow44: 0x3D,
            cameraDelta43: 1,
            scheduledEntries: [new PlatformSpecialSpawnEntry(0x38, 0x00, 0x60)]);

        Require(result.Outcome == PlatformPersistentPrimaryEntityMainThreadOutcome.Continued
            && result.ExitTransition is null,
            "substate $10 Shun exception rejects an otherwise matching exit gate");
        Require(result.Producer?.ScheduledSpecial?.SlotA.Spawned == true,
            "rejected exit continues into later $8927 scheduled producer");
        Require(result.Hybrid is not null,
            "rejected exit continues into later player/entity pipeline");
        Require(result.State.FrameCounter3C == unchecked((byte)(initial.FrameCounter3C + 1)),
            "continuing path reaches shared $3C advance");
    }

    private static void CheckSubstate11UsesSpecial70Transition()
    {
        var stage = Stage(Page(0, raw: 0xA8, hp: 80));
        var initial = State(Config(0xA8, 80), staged: 0xA8, cooldown: 0, lastTrigger: 0x10);
        var player = Player(PlatformSaintIndex.Seiya, x: 0xD0, y: 0x50, jump: 0);

        var result = StepFull(
            stage,
            initial,
            player,
            substate: 0x11,
            cameraLow44: 0x3D,
            cameraDelta43: 1,
            scheduledEntries: [new PlatformSpecialSpawnEntry(0x38, 0x00, 0x60)]);

        Require(result.Outcome == PlatformPersistentPrimaryEntityMainThreadOutcome.State70Special
            && result.ExitTransition == PlatformExitTransitionKind.State70Special,
            "substate $11 returns distinct $70 special transition");
        Require(result.Producer is null && result.Hybrid is null,
            "$70 exit suppresses scheduled producer and later frame simulation");
        Require(result.State.LastTriggerLow03A2 == initial.LastTriggerLow03A2,
            "$8927 trigger state remains untouched on $70 exit");
        Require(result.State.FrameCounter3C == initial.FrameCounter3C,
            "$70 exit does not advance shared $3C");
    }

    private static void CheckNonExitPathMatchesCompatibilityEntryPoint()
    {
        var stage = Stage(Page(0, raw: 0xA8, hp: 80));
        var initial = State(Config(0xA8, 80), staged: 0xA8, cooldown: 0, lastTrigger: 0x10);
        var player = Player(PlatformSaintIndex.Seiya, x: 0x40, y: 0x40, jump: 0);
        PlatformSpecialSpawnEntry[] schedule =
        [
            new(0x38, 0x00, 0x60),
        ];

        var full = StepFull(
            stage,
            initial,
            player,
            substate: 0x01,
            cameraLow44: 0x3D,
            cameraDelta43: 1,
            scheduledEntries: schedule);

        var legacy = PlatformPersistentPrimaryEntityFrame.StepMainThreadNonExit(
            stage,
            cameraLow44: 0x3D,
            cameraHigh45: 0,
            scrollX: 0,
            playerState: player,
            input: PlatformInput.None,
            resources: new PlatformFrameResources(99, 50),
            contactState: new PlatformContactPhaseState(0, default),
            initial,
            schedule,
            PlatformHitboxParameters.Common,
            PlatformHitboxParameters.Common,
            entropy48: 0,
            cameraDelta43: 1,
            engineSubstate02: 0x01,
            engineState00: 0x20,
            alternateParent08_03AB: 0x77);

        Require(full.Outcome == PlatformPersistentPrimaryEntityMainThreadOutcome.Continued,
            "non-exit coordinates continue through full entry point");
        Require(full.State == legacy.State,
            "full non-exit persistent state is identical to compatibility path");
        Require(full.Producer == legacy.Producer,
            "full non-exit producer result is identical to compatibility path");
        Require(full.Hybrid is not null,
            "full non-exit path returns a hybrid frame result");

        var actual = full.Hybrid!;
        var expected = legacy.Hybrid;
        Require(
            actual.FrameCounterBefore3C == expected.FrameCounterBefore3C
            && actual.FrameCounterAfter3C == expected.FrameCounterAfter3C
            && actual.FrameCounterAdvanced == expected.FrameCounterAdvanced
            && actual.ExitedBeforeEntityPipeline == expected.ExitedBeforeEntityPipeline
            && actual.SlotA?.Route == expected.SlotA?.Route
            && actual.SlotA?.State == expected.SlotA?.State
            && actual.SlotB?.Route == expected.SlotB?.Route
            && actual.SlotB?.State == expected.SlotB?.State
            && actual.ContactState == expected.ContactState
            && actual.SeventhSense == expected.SeventhSense
            && actual.GlobalCounter039A == expected.GlobalCounter039A
            && actual.PlayerAfterLatePhases.State == expected.PlayerAfterLatePhases.State,
            "full non-exit hybrid observable state is identical to compatibility path");
    }

    private static PlatformPersistentPrimaryEntityFullMainThreadResult StepFull(
        PlatformStageMap stage,
        PlatformPersistentPrimaryEntityFrameState state,
        PlatformPlayerActionState player,
        byte substate,
        byte cameraLow44,
        byte cameraDelta43,
        IReadOnlyList<PlatformSpecialSpawnEntry> scheduledEntries) =>
        PlatformPersistentPrimaryEntityFrame.StepMainThread(
            stage,
            cameraLow44,
            cameraHigh45: 0,
            scrollX: 0,
            playerState: player,
            input: PlatformInput.None,
            resources: new PlatformFrameResources(99, 50),
            contactState: new PlatformContactPhaseState(0, default),
            state,
            scheduledEntries,
            PlatformHitboxParameters.Common,
            PlatformHitboxParameters.Common,
            entropy48: 0,
            cameraDelta43,
            engineSubstate02: substate,
            engineState00: 0x20,
            alternateParent08_03AB: 0x77);

    private static PlatformPersistentPrimaryEntityFrameState State(
        PlatformPrimaryEncounterSpawnConfig active,
        byte staged,
        byte cooldown,
        byte lastTrigger) =>
        new(
            new PlatformPrimaryEncounterLatchState(active.Engine58, active),
            staged,
            FreeSlot(),
            FreeSlot(),
            cooldown,
            lastTrigger,
            SeventhSense: 0,
            GlobalCounter039A: 0,
            FrameCounter3C: 1);

    private static PlatformHybridEntitySlotState FreeSlot() =>
        PlatformHybridEntitySlotState.Common(
            Entity(type: 0, action: 0, x: 0, y: 0),
            visualSpritePlus1: 0xFE);

    private static PlatformCommonEntityRuntimeState Entity(byte type, byte action, byte x, byte y) =>
        new(
            new PlatformCommonEntityMotionState(
                ActionState: action,
                X: x,
                Y: y,
                StatePhase: 0,
                GroundDescriptor: 0xA8,
                DecisionTimer: 0,
                FlagsFacing: 0x40,
                Type: type,
                TerrainProbeRight: 0,
                TerrainProbeLeft: 0),
            HitPoints: 30,
            LifeDrainTicks: 1,
            CosmoDrainTicks: 1,
            SeventhSenseRewardBcd: 0x10);

    private static PlatformPlayerActionState Player(
        PlatformSaintIndex saint,
        byte x,
        byte y,
        byte jump) =>
        new(
            saint,
            new PlatformHorizontalState(x, 0, 0, 0x40),
            y,
            jump,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            PlatformAttackState.Empty);

    private static PlatformPrimaryEncounterSpawnConfig Config(byte raw, int hp) =>
        PlatformPrimaryEncounterSpawnConfig.FromEncounter(Encounter(raw, hp));

    private static PlatformStagePage Page(int index, byte raw, int hp) =>
        new(index, GroundDescriptors(), Encounter(raw, hp));

    private static PlatformPrimaryEncounter Encounter(byte raw, int hp) =>
        new(
            Raw: raw,
            TypeId: (byte)(raw & 0x0F),
            Tier: (byte)((raw >> 4) & 0x03),
            SecondCommonSlotEnabled: (raw & 0x80) != 0,
            Bit6Unknown: (raw & 0x40) != 0,
            Stats: new PlatformEncounterStats(hp, 1, 1, 4));

    private static PlatformStageMap Stage(params PlatformStagePage[] pages) => new(0, pages);

    private static byte[] GroundDescriptors()
    {
        var descriptors = new byte[PlatformStagePage.DescriptorCount];
        descriptors[4 * PlatformStagePage.Columns] = 0xA8;
        descriptors[4 * PlatformStagePage.Columns + 15] = 0xA8;
        return descriptors;
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Persistent primary entity exit-gate self-test failed: {label}");
    }
}

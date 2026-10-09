using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class PrimaryEncounterRefreshAndSpawnChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckDeferredChangeCarriesStagedDescriptorIntoMainThreadGate();
        CheckSafeChangeFeedsNewAcceptedSpecialIntoScheduledProducer();
        CheckSuppressedNmiRefreshPreservesPriorMainThreadEncounter();
        CheckDeferredOldSpecialStillRunsScheduledProducer();
        CheckAcceptedZeroLeavesMainThreadWithNoActiveEncounter();
    }

    private static void CheckDeferredChangeCarriesStagedDescriptorIntoMainThreadGate()
    {
        var stage = Stage(Page(0, raw: 0xA8, hp: 80));
        var state = State(
            active: Config(raw: 0x86, hp: 15),
            staged03B7: 0x86,
            spawn: InitialSpawnState(cooldown: 0) with
            {
                EntityA = Existing(action: 0x40),
            });

        var nmi = PlatformPrimaryEncounterRefreshAndSpawn.StepNmi(
            stage,
            cameraLow44: 0x02,
            cameraHigh45: 0,
            visual07C0: 0xFE,
            state03A4: 0,
            state);

        Require(nmi.NmiRefresh.Acceptance?.Outcome == PlatformPrimaryEncounterAcceptanceOutcome.DeferredUnsafe,
            "unsafe common slot defers changed descriptor at NMI boundary");
        Require(nmi.State.EncounterLatch.ActiveEngine58 == 0x86
            && nmi.State.StagedDescriptor03B7 == 0xA8,
            "NMI leaves old $58 active while staging new $03B7");

        var main = PlatformPrimaryEncounterRefreshAndSpawn.StepMainThread(
            stage,
            cameraLow44: 0x02,
            cameraHigh45: 0,
            scrollX: 0,
            playerX: 0x40,
            cameraDelta43: 0,
            entropy48: 0,
            nmi.State,
            scheduledEntries: Array.Empty<PlatformSpecialSpawnEntry>());

        Require(main.MainThreadProducer.ActiveEncounter.ActiveEngine58 == 0x86,
            "main-thread producer consumes accepted old $58");
        Require(main.MainThreadProducer.StagedDescriptor03B7 == 0xA8,
            "main-thread producer also receives newly staged $03B7");
        Require(main.MainThreadProducer.CommonEdge?.SlotA.Outcome == PlatformCommonEdgeSpawnOutcome.SpawnGateMismatch,
            "generic B6D0 is blocked by staged/active mismatch");
    }

    private static void CheckSafeChangeFeedsNewAcceptedSpecialIntoScheduledProducer()
    {
        var stage = Stage(Page(0, raw: 0xA8, hp: 80));
        var state = State(
            active: Config(raw: 0x86, hp: 15),
            staged03B7: 0x86,
            spawn: InitialSpawnState(cooldown: 0) with { LastTriggerLow03A2 = 0x10 });

        var nmi = PlatformPrimaryEncounterRefreshAndSpawn.StepNmi(
            stage,
            cameraLow44: 0x3D,
            cameraHigh45: 0,
            visual07C0: 0xFE,
            state03A4: 0,
            state);

        Require(nmi.NmiRefresh.Acceptance?.Outcome == PlatformPrimaryEncounterAcceptanceOutcome.AcceptedNonzero,
            "safe NMI boundary accepts changed special encounter");
        Require(nmi.State.EncounterLatch.ActiveEngine58 == 0xA8
            && nmi.State.StagedDescriptor03B7 == 0xA8,
            "safe acceptance synchronizes active $58 and staged $03B7");

        var main = PlatformPrimaryEncounterRefreshAndSpawn.StepMainThread(
            stage,
            cameraLow44: 0x3D,
            cameraHigh45: 0,
            scrollX: 0,
            playerX: 0x40,
            cameraDelta43: 0,
            entropy48: 0,
            nmi.State,
            scheduledEntries: [new PlatformSpecialSpawnEntry(0x38, 0x00, 0x60)]);

        Require(main.MainThreadProducer.CommonEdge?.SlotA.Outcome == PlatformCommonEdgeSpawnOutcome.ScheduledTypeExcluded,
            "generic producer sees accepted special type and excludes it normally");
        Require(main.MainThreadProducer.ScheduledSpecial?.SlotA.Spawned == true,
            "later $8927 scheduled producer consumes newly accepted special $58");
        Require(main.State.SpawnState.EntityA.Motion.Type == 0x08
            && main.State.SpawnState.EntityA.Motion.Y == 0x60,
            "scheduled spawn mutation is carried across the bridge state");
    }

    private static void CheckSuppressedNmiRefreshPreservesPriorMainThreadEncounter()
    {
        var stage = Stage(Page(0, raw: 0xA8, hp: 80));
        var state = State(
            active: Config(raw: 0x86, hp: 15),
            staged03B7: 0x86,
            spawn: InitialSpawnState(cooldown: 1));

        var nmi = PlatformPrimaryEncounterRefreshAndSpawn.StepNmi(
            stage,
            cameraLow44: 0x02,
            cameraHigh45: 0,
            visual07C0: 0xFD,
            state03A4: 0xFF,
            state);

        Require(nmi.NmiRefresh.Gate.Outcome == PlatformPrimaryEncounterRefreshGateOutcome.SpecialVisualBranchSuppressesAcceptance,
            "special visual path suppresses $996C");
        Require(nmi.NmiRefresh.Acceptance is null,
            "suppressed NMI refresh has no acceptance result");
        Require(nmi.State.EncounterLatch.ActiveEngine58 == 0x86
            && nmi.State.StagedDescriptor03B7 == 0x86,
            "suppressed NMI refresh preserves both active and staged descriptors");

        var main = PlatformPrimaryEncounterRefreshAndSpawn.StepMainThread(
            stage,
            cameraLow44: 0x02,
            cameraHigh45: 0,
            scrollX: 0,
            playerX: 0x40,
            cameraDelta43: 0,
            entropy48: 0,
            nmi.State,
            scheduledEntries: Array.Empty<PlatformSpecialSpawnEntry>());

        Require(main.MainThreadProducer.CommonEdge?.SlotA.Outcome == PlatformCommonEdgeSpawnOutcome.CooldownDecremented,
            "prior common encounter continues on main thread when refresh was suppressed");
    }

    private static void CheckDeferredOldSpecialStillRunsScheduledProducer()
    {
        var stage = Stage(Page(0, raw: 0x96, hp: 25));
        var state = State(
            active: Config(raw: 0xA8, hp: 80),
            staged03B7: 0xA8,
            spawn: InitialSpawnState(cooldown: 0) with
            {
                EntityA = Existing(action: 0x40),
                LastTriggerLow03A2 = 0x10,
            });

        var nmi = PlatformPrimaryEncounterRefreshAndSpawn.StepNmi(
            stage,
            cameraLow44: 0x3D,
            cameraHigh45: 0,
            visual07C0: 0xFE,
            state03A4: 0,
            state);

        Require(nmi.NmiRefresh.Acceptance?.Outcome == PlatformPrimaryEncounterAcceptanceOutcome.DeferredUnsafe,
            "new common descriptor is deferred while old special remains active");
        Require(nmi.State.EncounterLatch.ActiveEngine58 == 0xA8
            && nmi.State.StagedDescriptor03B7 == 0x96,
            "deferred transition carries active old special plus staged new common descriptor");

        // Free the logical/visual slot before the later main-thread producer.
        // The point of this fixture is the descriptor asymmetry, not slot occupancy.
        var producerInput = nmi.State with
        {
            SpawnState = nmi.State.SpawnState with
            {
                EntityA = Existing(),
                VisualSpriteA = 0xFE,
            },
        };

        var main = PlatformPrimaryEncounterRefreshAndSpawn.StepMainThread(
            stage,
            cameraLow44: 0x3D,
            cameraHigh45: 0,
            scrollX: 0,
            playerX: 0x40,
            cameraDelta43: 0,
            entropy48: 0,
            producerInput,
            scheduledEntries: [new PlatformSpecialSpawnEntry(0x38, 0x00, 0x60)]);

        Require(main.MainThreadProducer.CommonEdge?.SlotA.Outcome == PlatformCommonEdgeSpawnOutcome.SpawnGateMismatch,
            "generic producer sees staged/active mismatch");
        Require(main.MainThreadProducer.ScheduledSpecial?.SlotA.Spawned == true,
            "scheduled producer still evaluates old active special encounter without invented mismatch gate");
    }

    private static void CheckAcceptedZeroLeavesMainThreadWithNoActiveEncounter()
    {
        var stage = Stage(EmptyPage(0));
        var state = State(
            active: Config(raw: 0x86, hp: 15),
            staged03B7: 0x86,
            spawn: InitialSpawnState(cooldown: 7));

        var nmi = PlatformPrimaryEncounterRefreshAndSpawn.StepNmi(
            stage,
            cameraLow44: 0x02,
            cameraHigh45: 0,
            visual07C0: 0xFE,
            state03A4: 0,
            state);

        Require(nmi.NmiRefresh.Acceptance?.Outcome == PlatformPrimaryEncounterAcceptanceOutcome.AcceptedZero,
            "safe zero descriptor clears active encounter at NMI boundary");
        Require(nmi.State.EncounterLatch.ActiveEngine58 == 0
            && nmi.State.EncounterLatch.ActiveConfig is null
            && nmi.State.StagedDescriptor03B7 == 0,
            "zero acceptance clears active state and stages zero");

        var main = PlatformPrimaryEncounterRefreshAndSpawn.StepMainThread(
            stage,
            cameraLow44: 0x02,
            cameraHigh45: 0,
            scrollX: 0,
            playerX: 0x40,
            cameraDelta43: 0,
            entropy48: 0,
            nmi.State,
            scheduledEntries: Array.Empty<PlatformSpecialSpawnEntry>());

        Require(main.MainThreadProducer.NoActiveEncounter,
            "main-thread producer is contained no-op after accepted zero");
        Require(main.State.SpawnState.Cooldown03B8 == 7,
            "no-active producer preserves unrelated producer state");
    }

    private static PlatformPrimaryEncounterRefreshAndSpawnState State(
        PlatformPrimaryEncounterSpawnConfig active,
        byte staged03B7,
        PlatformPageEncounterSpawnState spawn) =>
        new(
            new PlatformPrimaryEncounterLatchState(active.Engine58, active),
            staged03B7,
            spawn);

    private static PlatformPrimaryEncounterSpawnConfig Config(byte raw, int hp) =>
        PlatformPrimaryEncounterSpawnConfig.FromEncounter(Encounter(raw, hp));

    private static PlatformStagePage Page(int index, byte raw, int hp) =>
        new(index, GroundDescriptors(), Encounter(raw, hp));

    private static PlatformStagePage EmptyPage(int index) =>
        new(index, GroundDescriptors(), new PlatformPrimaryEncounter(0, 0, 0, false, false, null));

    private static PlatformPrimaryEncounter Encounter(byte raw, int hp)
    {
        var type = (byte)(raw & 0x0F);
        var tier = (byte)((raw >> 4) & 0x03);
        return new(
            Raw: raw,
            TypeId: type,
            Tier: tier,
            SecondCommonSlotEnabled: (raw & 0x80) != 0,
            Bit6Unknown: (raw & 0x40) != 0,
            Stats: new PlatformEncounterStats(hp, 1, 1, 4));
    }

    private static PlatformStageMap Stage(params PlatformStagePage[] pages) => new(0, pages);

    private static byte[] GroundDescriptors()
    {
        var descriptors = new byte[PlatformStagePage.DescriptorCount];
        descriptors[4 * PlatformStagePage.Columns] = 0xA8;
        descriptors[4 * PlatformStagePage.Columns + 15] = 0xA8;
        return descriptors;
    }

    private static PlatformPageEncounterSpawnState InitialSpawnState(byte cooldown) =>
        new(
            Existing(), 0xFE,
            Existing(), 0xFE,
            cooldown,
            0);

    private static PlatformCommonEntityRuntimeState Existing(byte action = 0) =>
        new(
            new PlatformCommonEntityMotionState(action, 0, 0, 0, 0, 0, 0, 0, 0, 0),
            0, 0, 0, 0);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Primary encounter refresh/main-thread bridge self-test failed: {label}");
    }
}

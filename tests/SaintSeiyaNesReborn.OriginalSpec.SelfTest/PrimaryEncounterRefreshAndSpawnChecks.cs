using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class PrimaryEncounterRefreshAndSpawnChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckDeferredChangeKeepsOldAcceptedConfigButStagesNew03B7();
        CheckSafeChangeFeedsNewAcceptedSpecialEncounter();
        CheckSuppressedRefreshKeepsPriorLatchAndStagedDescriptor();
        CheckAcceptedZeroClearsProducerConfig();
    }

    private static void CheckDeferredChangeKeepsOldAcceptedConfigButStagesNew03B7()
    {
        var stage = Stage(Page(0, raw: 0xA8, hp: 80));
        var oldConfig = Config(raw: 0x86, hp: 15);
        var spawn = InitialSpawnState(cooldown: 0) with
        {
            EntityA = Existing(action: 0x40),
        };
        var state = new PlatformPrimaryEncounterRefreshAndSpawnState(
            new PlatformPrimaryEncounterLatchState(0x86, oldConfig),
            StagedDescriptor03B7: 0x86,
            spawn);

        var result = PlatformPrimaryEncounterRefreshAndSpawn.Step(
            stage,
            cameraLow44: 0x02,
            cameraHigh45: 0,
            scrollX: 0,
            playerX: 0x40,
            cameraDelta43: 0,
            entropy48: 0,
            visual07C0: 0xFE,
            state03A4: 0,
            state,
            scheduledEntries: [new PlatformSpecialSpawnEntry(0x00, 0x00, 0x60)]);

        Require(result.Acceptance?.Outcome == PlatformPrimaryEncounterAcceptanceOutcome.DeferredUnsafe,
            "unsafe common slot defers changed page descriptor");
        Require(result.State.EncounterLatch.ActiveEngine58 == 0x86
            && result.State.EncounterLatch.ActiveConfig?.Engine58 == 0x86,
            "deferred acceptance preserves old $58/profile");
        Require(result.State.StagedDescriptor03B7 == 0xA8,
            "changed page descriptor is still staged into $03B7");
        Require(result.SpawnPhase.Route == PlatformPageEncounterSpawnRoute.CommonEdgeB6D0
            && result.SpawnPhase.EncounterConfig?.Engine58 == 0x86,
            "producer routes from accepted old $58 rather than current page descriptor");
        Require(result.SpawnPhase.CommonEdge?.SlotA.Outcome == PlatformCommonEdgeSpawnOutcome.SpawnGateMismatch,
            "new staged $03B7 gates the still-active old common producer");
    }

    private static void CheckSafeChangeFeedsNewAcceptedSpecialEncounter()
    {
        var stage = Stage(Page(0, raw: 0xA8, hp: 80));
        var oldConfig = Config(raw: 0x86, hp: 15);
        var state = new PlatformPrimaryEncounterRefreshAndSpawnState(
            new PlatformPrimaryEncounterLatchState(0x86, oldConfig),
            StagedDescriptor03B7: 0x86,
            InitialSpawnState(cooldown: 0) with { LastTriggerLow03A2 = 0x10 });

        var result = PlatformPrimaryEncounterRefreshAndSpawn.Step(
            stage,
            cameraLow44: 0x3C,
            cameraHigh45: 0,
            scrollX: 0,
            playerX: 0x40,
            cameraDelta43: 0,
            entropy48: 0,
            visual07C0: 0xFE,
            state03A4: 0,
            state,
            scheduledEntries: [new PlatformSpecialSpawnEntry(0x38, 0x00, 0x60)]);

        Require(result.Acceptance?.Outcome == PlatformPrimaryEncounterAcceptanceOutcome.AcceptedNonzero,
            "safe changed descriptor is accepted");
        Require(result.State.EncounterLatch.ActiveEngine58 == 0xA8
            && result.State.StagedDescriptor03B7 == 0xA8,
            "accepted descriptor synchronizes active $58 and staged $03B7");
        Require(result.SpawnPhase.Route == PlatformPageEncounterSpawnRoute.ScheduledSpecial8925
            && result.SpawnPhase.EncounterConfig?.Engine58 == 0xA8,
            "new accepted special encounter feeds scheduled producer immediately");
        Require(result.SpawnPhase.ScheduledSpecial?.SlotA.Spawned == true
            && result.State.SpawnState.EntityA.Motion.Type == 0x08,
            "scheduled producer materializes type08 from accepted profile");
    }

    private static void CheckSuppressedRefreshKeepsPriorLatchAndStagedDescriptor()
    {
        var stage = Stage(Page(0, raw: 0xA8, hp: 80));
        var oldConfig = Config(raw: 0x86, hp: 15);
        var state = new PlatformPrimaryEncounterRefreshAndSpawnState(
            new PlatformPrimaryEncounterLatchState(0x86, oldConfig),
            StagedDescriptor03B7: 0x86,
            InitialSpawnState(cooldown: 1));

        var result = PlatformPrimaryEncounterRefreshAndSpawn.Step(
            stage,
            cameraLow44: 0x02,
            cameraHigh45: 0,
            scrollX: 0,
            playerX: 0x40,
            cameraDelta43: 0,
            entropy48: 0,
            visual07C0: 0xFD,
            state03A4: 0xFF,
            state,
            scheduledEntries: Array.Empty<PlatformSpecialSpawnEntry>());

        Require(result.RefreshGate.Outcome == PlatformPrimaryEncounterRefreshGateOutcome.SpecialVisualBranchSuppressesAcceptance,
            "special visual branch suppresses $996C acceptance");
        Require(result.Acceptance is null,
            "suppressed refresh does not fabricate an acceptance result");
        Require(result.State.EncounterLatch.ActiveEngine58 == 0x86
            && result.State.StagedDescriptor03B7 == 0x86,
            "suppressed refresh preserves old latch and old staged descriptor");
        Require(result.SpawnPhase.Route == PlatformPageEncounterSpawnRoute.CommonEdgeB6D0
            && result.SpawnPhase.EncounterConfig?.Engine58 == 0x86,
            "producer continues from previously accepted encounter");
        Require(result.SpawnPhase.CommonEdge?.SlotA.Outcome == PlatformCommonEdgeSpawnOutcome.CooldownDecremented,
            "old common producer remains live on suppressed refresh frame");
    }

    private static void CheckAcceptedZeroClearsProducerConfig()
    {
        var stage = Stage(EmptyPage(0));
        var oldConfig = Config(raw: 0x86, hp: 15);
        var state = new PlatformPrimaryEncounterRefreshAndSpawnState(
            new PlatformPrimaryEncounterLatchState(0x86, oldConfig),
            StagedDescriptor03B7: 0x86,
            InitialSpawnState(cooldown: 7));

        var result = PlatformPrimaryEncounterRefreshAndSpawn.Step(
            stage,
            cameraLow44: 0x02,
            cameraHigh45: 0,
            scrollX: 0,
            playerX: 0x40,
            cameraDelta43: 0,
            entropy48: 0,
            visual07C0: 0xFE,
            state03A4: 0,
            state,
            scheduledEntries: Array.Empty<PlatformSpecialSpawnEntry>());

        Require(result.Acceptance?.Outcome == PlatformPrimaryEncounterAcceptanceOutcome.AcceptedZero,
            "safe zero descriptor clears active encounter");
        Require(result.State.EncounterLatch.ActiveEngine58 == 0
            && result.State.EncounterLatch.ActiveConfig is null
            && result.State.StagedDescriptor03B7 == 0,
            "zero acceptance clears active $58/profile and stages zero");
        Require(result.SpawnPhase.Route == PlatformPageEncounterSpawnRoute.None
            && result.SpawnPhase.EmptyEncounter,
            "cleared active encounter prevents both producer routes");
        Require(result.State.SpawnState.Cooldown03B8 == 7,
            "no active encounter leaves producer state untouched");
    }

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
            throw new InvalidOperationException($"Primary encounter refresh/spawn self-test failed: {label}");
    }
}

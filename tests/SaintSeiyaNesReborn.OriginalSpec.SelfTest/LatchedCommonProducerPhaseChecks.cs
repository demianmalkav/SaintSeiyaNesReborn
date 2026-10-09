using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class LatchedCommonProducerPhaseChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckDeferredCommonEncounterBlocksB6D0();
        CheckDeferredSpecialEncounterCanStillUse8927();
        CheckAcceptedCommonEncounterReenablesB6D0();
        CheckNoActiveEncounterIsNoOp();
    }

    private static void CheckDeferredCommonEncounterBlocksB6D0()
    {
        var active = Active(Encounter(0x85, 10, 1, 1, 2));
        var state = InitialState(cooldown: 0);

        var result = PlatformLatchedCommonProducerPhase.Step(
            GroundStage(),
            active,
            stagedDescriptor03B7: 0x96,
            cameraLow44: 0x38,
            cameraHigh45: 0,
            scrollX: 0,
            playerX: 0x40,
            cameraDelta43: 0,
            entropy48: 0,
            state,
            scheduledEntries: Array.Empty<PlatformSpecialSpawnEntry>());

        Require(result.CommonEdge?.SlotA.Outcome == PlatformCommonEdgeSpawnOutcome.SpawnGateMismatch,
            "staged $03B7 != active $58 blocks generic B6D0 producer");
        Require(result.State.Cooldown03B8 == 0,
            "mismatch returns before generic cooldown mutation");
        Require(result.ScheduledSpecial?.SlotA.Outcome == PlatformScheduledSpecialSpawnOutcome.UnsupportedType,
            "old common type is not incorrectly routed through scheduled producer");
    }

    private static void CheckDeferredSpecialEncounterCanStillUse8927()
    {
        var active = Active(Encounter(0xA8, 80, 8, 8, 16));
        var state = InitialState(cooldown: 0) with { LastTriggerLow03A2 = 0x10 };

        var result = PlatformLatchedCommonProducerPhase.Step(
            GroundStage(),
            active,
            stagedDescriptor03B7: 0x96,
            cameraLow44: 0x3D,
            cameraHigh45: 0,
            scrollX: 0,
            playerX: 0x40,
            cameraDelta43: 0,
            entropy48: 0,
            state,
            scheduledEntries: [new PlatformSpecialSpawnEntry(0x38, 0x00, 0x60)]);

        Require(result.CommonEdge?.SlotA.Outcome == PlatformCommonEdgeSpawnOutcome.SpawnGateMismatch,
            "B6D0 still sees staged/active mismatch before special exclusion");
        Require(result.ScheduledSpecial?.SlotA.Spawned == true,
            "$8927 uses active old special $58 despite staged page mismatch");
        Require(result.State.EntityA.Motion.Type == 0x08
            && result.State.EntityA.Motion.Y == 0x60,
            "scheduled producer instantiates from active old special config");
        Require(result.State.LastTriggerLow03A2 == 0x38,
            "$8927 result propagates trigger latch");
    }

    private static void CheckAcceptedCommonEncounterReenablesB6D0()
    {
        var active = Active(Encounter(0x86, 15, 1, 1, 4));
        var state = InitialState(cooldown: 1);

        var result = PlatformLatchedCommonProducerPhase.Step(
            GroundStage(),
            active,
            stagedDescriptor03B7: 0x86,
            cameraLow44: 0,
            cameraHigh45: 0,
            scrollX: 0,
            playerX: 0x40,
            cameraDelta43: 0,
            entropy48: 0,
            state,
            scheduledEntries: Array.Empty<PlatformSpecialSpawnEntry>());

        Require(result.CommonEdge?.SlotA.Outcome == PlatformCommonEdgeSpawnOutcome.CooldownDecremented,
            "matching staged/active descriptor reaches generic cooldown path");
        Require(result.CommonEdge?.SlotB?.Spawned == true,
            "active descriptor bit7 permits slot-B attempt after A reaches zero");
        Require(result.State.EntityB.Motion.Type == 0x06
            && result.State.EntityB.HitPoints == 15,
            "accepted common config reaches runtime entity");
    }

    private static void CheckNoActiveEncounterIsNoOp()
    {
        var state = InitialState(cooldown: 9) with { LastTriggerLow03A2 = 0x28 };
        var result = PlatformLatchedCommonProducerPhase.Step(
            GroundStage(),
            PlatformPrimaryEncounterLatchState.Empty,
            stagedDescriptor03B7: 0,
            cameraLow44: 0,
            cameraHigh45: 0,
            scrollX: 0,
            playerX: 0x40,
            cameraDelta43: 0,
            entropy48: 0,
            state,
            scheduledEntries: Array.Empty<PlatformSpecialSpawnEntry>());

        Require(result.NoActiveEncounter,
            "semantic $58=0 skips both producer models");
        Require(result.State.Equals(state),
            "no-active-encounter phase preserves complete producer state");
    }

    private static PlatformStageMap GroundStage()
    {
        var descriptors = new byte[PlatformStagePage.DescriptorCount];
        descriptors[4 * PlatformStagePage.Columns] = 0xA8;
        descriptors[4 * PlatformStagePage.Columns + 15] = 0xA8;
        return new PlatformStageMap(0, [new PlatformStagePage(0, descriptors)]);
    }

    private static PlatformPageEncounterSpawnState InitialState(byte cooldown) =>
        new(
            Existing(), 0xFE,
            Existing(), 0xFE,
            cooldown,
            0);

    private static PlatformCommonEntityRuntimeState Existing() =>
        new(
            new PlatformCommonEntityMotionState(0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
            0, 0, 0, 0);

    private static PlatformPrimaryEncounterLatchState Active(PlatformPrimaryEncounter encounter)
    {
        var config = PlatformPrimaryEncounterSpawnConfig.FromEncounter(encounter);
        return new PlatformPrimaryEncounterLatchState(encounter.Raw, config);
    }

    private static PlatformPrimaryEncounter Encounter(
        byte raw,
        int hp,
        byte cosmo,
        byte life,
        int ss) =>
        new(
            Raw: raw,
            TypeId: (byte)(raw & 0x0F),
            Tier: (byte)((raw >> 4) & 0x03),
            SecondCommonSlotEnabled: (raw & 0x80) != 0,
            Bit6Unknown: (raw & 0x40) != 0,
            Stats: new PlatformEncounterStats(hp, cosmo, life, ss));

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Latched common producer-phase self-test failed: {label}");
    }
}

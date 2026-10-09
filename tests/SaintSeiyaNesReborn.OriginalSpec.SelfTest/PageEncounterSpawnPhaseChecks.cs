using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class PageEncounterSpawnPhaseChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckCameraHighSelectsPageAndCommonSpawner();
        CheckSpecialPageRoutesToScheduleSpawner();
        CheckEmptyPageIsNoOp();
        CheckOutOfRangePageIsNoOp();
    }

    private static void CheckCameraHighSelectsPageAndCommonSpawner()
    {
        var stage = Stage(
            EncounterPage(0, raw: 0x85, hp: 10, cosmo: 1, life: 1, ss: 2),
            EncounterPage(1, raw: 0x86, hp: 15, cosmo: 1, life: 1, ss: 4));
        var state = InitialState(cooldown: 1);

        var result = PlatformPageEncounterSpawnPhase.Step(
            stage,
            cameraLow44: 0,
            cameraHigh45: 1,
            scrollX: 0,
            playerX: 0x40,
            cameraDelta43: 0,
            entropy48: 0,
            spawnGate03B7: 0,
            state,
            scheduledEntries: Array.Empty<PlatformSpecialSpawnEntry>());

        Require(result.Route == PlatformPageEncounterSpawnRoute.CommonEdgeB6D0,
            "page 1 type06 routes to B6D0");
        Require(result.Page?.PageIndex == 1 && result.EncounterConfig?.Engine58 == 0x86,
            "$45 selects page 1 encounter descriptor");
        Require(result.CommonEdge?.SlotA.Outcome == PlatformCommonEdgeSpawnOutcome.CooldownDecremented,
            "slot A consumes shared cooldown");
        Require(result.CommonEdge?.SlotB?.Spawned == true,
            "bit7 descriptor allows slot B to spawn after cooldown reaches zero");
        Require(result.State.EntityB.Motion.Type == 0x06 && result.State.EntityB.HitPoints == 15,
            "page encounter type/stats populate slot B");
    }

    private static void CheckSpecialPageRoutesToScheduleSpawner()
    {
        var stage = Stage(EncounterPage(0, raw: 0xA8, hp: 80, cosmo: 8, life: 8, ss: 16));
        var state = InitialState(cooldown: 0) with { LastTriggerLow03A2 = 0x10 };

        var result = PlatformPageEncounterSpawnPhase.Step(
            stage,
            cameraLow44: 0x3C,
            cameraHigh45: 0,
            scrollX: 0,
            playerX: 0x40,
            cameraDelta43: 0,
            entropy48: 0,
            spawnGate03B7: 0,
            state,
            [new PlatformSpecialSpawnEntry(0x38, 0x00, 0x60)]);

        Require(result.Route == PlatformPageEncounterSpawnRoute.ScheduledSpecial8925,
            "type08 routes to schedule producer");
        Require(result.ScheduledSpecial?.SlotA.Spawned == true,
            "matching camera trigger spawns slot A");
        Require(result.State.EntityA.Motion.Type == 0x08
            && result.State.EntityA.Motion.Y == 0x60
            && result.State.EntityA.HitPoints == 80,
            "scheduled page config reaches runtime slot");
        Require(result.State.LastTriggerLow03A2 == 0x38,
            "scheduled route propagates $03A2");
        Require(result.State.Cooldown03B8 == 0,
            "scheduled route leaves generic $03B8 cooldown untouched");
    }

    private static void CheckEmptyPageIsNoOp()
    {
        var descriptors = GroundDescriptors();
        var empty = new PlatformStagePage(
            0,
            descriptors,
            new PlatformPrimaryEncounter(0, 0, 0, false, false, null));
        var stage = Stage(empty);
        var initial = InitialState(cooldown: 7) with { LastTriggerLow03A2 = 0x28 };

        var result = PlatformPageEncounterSpawnPhase.Step(
            stage, 0, 0, 0, 0x40, 0, 0, 0, initial,
            Array.Empty<PlatformSpecialSpawnEntry>());

        Require(result.Route == PlatformPageEncounterSpawnRoute.None && result.EmptyEncounter,
            "zero descriptor page is an explicit no-op");
        Require(result.State.Equals(initial), "empty encounter preserves complete spawn state");
    }

    private static void CheckOutOfRangePageIsNoOp()
    {
        var stage = Stage(EncounterPage(0, raw: 0x85, hp: 10, cosmo: 1, life: 1, ss: 2));
        var initial = InitialState(cooldown: 9);

        var result = PlatformPageEncounterSpawnPhase.Step(
            stage, 0, cameraHigh45: 2, 0, 0x40, 0, 0, 0, initial,
            Array.Empty<PlatformSpecialSpawnEntry>());

        Require(result.Route == PlatformPageEncounterSpawnRoute.None && result.PageOutOfRange,
            "out-of-range page is contained as no-op");
        Require(result.State.Equals(initial), "out-of-range page preserves state");
    }

    private static PlatformStagePage EncounterPage(
        int index,
        byte raw,
        int hp,
        byte cosmo,
        byte life,
        int ss)
    {
        var type = (byte)(raw & 0x0F);
        var tier = (byte)((raw >> 4) & 0x03);
        return new PlatformStagePage(
            index,
            GroundDescriptors(),
            new PlatformPrimaryEncounter(
                Raw: raw,
                TypeId: type,
                Tier: tier,
                SecondCommonSlotEnabled: (raw & 0x80) != 0,
                Bit6Unknown: (raw & 0x40) != 0,
                Stats: new PlatformEncounterStats(hp, cosmo, life, ss)));
    }

    private static PlatformStageMap Stage(params PlatformStagePage[] pages) => new(0, pages);

    private static byte[] GroundDescriptors()
    {
        var descriptors = new byte[PlatformStagePage.DescriptorCount];
        descriptors[4 * PlatformStagePage.Columns] = 0xA8;
        descriptors[4 * PlatformStagePage.Columns + 15] = 0xA8;
        return descriptors;
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

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Page encounter spawn-phase self-test failed: {label}");
    }
}

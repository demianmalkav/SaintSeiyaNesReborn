using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class NmiPrimaryEncounterRefreshPhaseChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckGateSuppressionPreservesLatch();
        CheckEligibleSafePageAccepts();
        CheckEligibleUnsafePageDefers();
    }

    private static void CheckGateSuppressionPreservesLatch()
    {
        var stage = Stage(Encounter(0x96, 30, 2, 2, 6));
        var active = Active(Encounter(0x85, 10, 1, 1, 2));

        var result = PlatformNmiPrimaryEncounterRefreshPhase.StepPlatformState20(
            stage,
            cameraLow44: 0x00,
            cameraHigh45: 0,
            visual07C0: 0xFE,
            state03A4: 0,
            latchState: active,
            commonVisualSpriteA: 0xFE,
            commonActionA: 0x10,
            commonVisualSpriteB: 0xFE,
            commonActionB: 0x10);

        Require(!result.AcceptanceInvoked,
            "aligned-scroll NMI branch does not invoke $996C");
        Require(result.LatchState.Equals(active),
            "suppressed refresh preserves active $58/profile");
    }

    private static void CheckEligibleSafePageAccepts()
    {
        var stage = Stage(Encounter(0xB6, 120, 8, 4, 13));

        var result = PlatformNmiPrimaryEncounterRefreshPhase.StepPlatformState20(
            stage,
            cameraLow44: 0x02,
            cameraHigh45: 0,
            visual07C0: 0xFE,
            state03A4: 0xFF,
            latchState: PlatformPrimaryEncounterLatchState.Empty,
            commonVisualSpriteA: 0xFE,
            commonActionA: 0x10,
            commonVisualSpriteB: 0xFE,
            commonActionB: 0x10);

        Require(result.AcceptanceInvoked,
            "eligible NMI gate invokes acceptance");
        Require(result.Acceptance?.Outcome == PlatformPrimaryEncounterAcceptanceOutcome.AcceptedNonzero,
            "safe changed page accepts encounter");
        Require(result.LatchState.ActiveEngine58 == 0xB6,
            "accepted page becomes active encounter state");
    }

    private static void CheckEligibleUnsafePageDefers()
    {
        var stage = Stage(Encounter(0x96, 30, 2, 2, 6));
        var active = Active(Encounter(0x85, 10, 1, 1, 2));

        var result = PlatformNmiPrimaryEncounterRefreshPhase.StepPlatformState20(
            stage,
            cameraLow44: 0x06,
            cameraHigh45: 0,
            visual07C0: 0xFE,
            state03A4: 0xFF,
            latchState: active,
            commonVisualSpriteA: 0xFE,
            commonActionA: 0x40,
            commonVisualSpriteB: 0xFE,
            commonActionB: 0x10);

        Require(result.AcceptanceInvoked,
            "NMI invocation gate can reach acceptance while common slot is unsafe");
        Require(result.Acceptance?.Outcome == PlatformPrimaryEncounterAcceptanceOutcome.DeferredUnsafe,
            "$996C defers changed descriptor because slot A is in $40");
        Require(result.LatchState.Equals(active),
            "deferred NMI refresh leaves old active encounter intact");
        Require(result.StagedPage?.PrimaryEncounter.Raw == 0x96,
            "current page descriptor remains visible separately from active $58");
    }

    private static PlatformStageMap Stage(PlatformPrimaryEncounter encounter)
    {
        var descriptors = new byte[PlatformStagePage.DescriptorCount];
        return new PlatformStageMap(0, [new PlatformStagePage(0, descriptors, encounter)]);
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

    private static PlatformPrimaryEncounterLatchState Active(PlatformPrimaryEncounter encounter)
    {
        var config = PlatformPrimaryEncounterSpawnConfig.FromEncounter(encounter);
        return new PlatformPrimaryEncounterLatchState(encounter.Raw, config);
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"NMI encounter refresh self-test failed: {label}");
    }
}

using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class PrePlayerResourcePhaseChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckCosmoDrainCrossesRangeAndDamageBoundary();
        CheckNoDrainKeepsHigherBracket();
    }

    private static void CheckCosmoDrainCrossesRangeAndDamageBoundary()
    {
        var stage = OpenStage();
        var playerState = BaseState();
        var contact = new PlatformContactPhaseState(
            HazardLatch76: 0,
            DrainState: new ContactDrainState(LifeTicks: 0, CosmoTicks: 1));

        var frame = PlatformPrePlayerResourcePhases.StepNonFatal(
            stage,
            playerState,
            PlatformInput.B,
            new PlatformFrameResources(Life: 99, Cosmo: 200),
            contact,
            frameCounter3C: 1,
            engineSubstate02: 0);

        Require(frame.Drain.CosmoLoss == 1, "pre-player phase consumes one pending Cosmo tick");
        Require(frame.ResourcesAfterDrain.Cosmo == 199, "Cosmo becomes 199 before damage/range calculation");
        Require(frame.PlatformDamage == 36, "Seiya damage uses post-drain 199 rather than frame-start 200");
        Require(frame.Player.AttackAttempt?.Outcome == PlatformAttackAttemptOutcome.Created,
            "B still creates projectile after non-fatal drain");
        Require(frame.Player.State.AttackState.Slot0.RangeCounter == 3,
            "BBCA selects the 0-1 hundreds range bracket from post-drain 199");
        Require(frame.ContactStateAfterDrain.DrainState.CosmoTicks == 0,
            "consumed Cosmo tick is carried forward as zero");
    }

    private static void CheckNoDrainKeepsHigherBracket()
    {
        var frame = PlatformPrePlayerResourcePhases.StepNonFatal(
            OpenStage(),
            BaseState(),
            PlatformInput.B,
            new PlatformFrameResources(Life: 99, Cosmo: 200),
            new PlatformContactPhaseState(0, new ContactDrainState(0, 0)),
            frameCounter3C: 1,
            engineSubstate02: 0);

        Require(frame.ResourcesAfterDrain.Cosmo == 200, "no pending tick preserves Cosmo 200");
        Require(frame.PlatformDamage == 38, "Seiya damage at 200 remains 38");
        Require(frame.Player.State.AttackState.Slot0.RangeCounter == 6,
            "Cosmo 200 selects next projectile-range bracket");
    }

    private static PlatformPlayerActionState BaseState() =>
        new(
            PlatformSaintIndex.Seiya,
            new PlatformHorizontalState(0x40, 0, 0, 0x40),
            0x50,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            PlatformAttackState.Empty);

    private static PlatformStageMap OpenStage() =>
        new(0, [new PlatformStagePage(0, new byte[PlatformStagePage.DescriptorCount])]);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Pre-player resource phase self-test failed: {label}");
    }
}

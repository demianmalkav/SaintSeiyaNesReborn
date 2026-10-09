using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class EntityContactChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        var hit = PlatformEntityContact.EvaluateOrdinary(
            frameStartAction4E: 0,
            playerX: 0x50,
            playerY: 0x50,
            entityX: 0x50,
            entityY: 0x50,
            currentHazardLatch76: 0,
            entityLifeDrainTicks: 2,
            entityCosmoDrainTicks: 4);
        Require(hit.Triggered, "overlapping entity contact triggers");
        Require(hit.HazardLatch76 == 0x20, "contact seeds $76=$20");
        Require(hit.DrainState.LifeTicks == 2, "contact copies entity +0E Life drain ticks");
        Require(hit.DrainState.CosmoTicks == 4, "contact copies entity +0D Cosmo drain ticks");
        Require(hit.SoundId == 0x26, "contact requests sound $26");

        var immune40 = PlatformEntityContact.EvaluateOrdinary(
            0x4F, 0x50, 0x50, 0x50, 0x50, 0, 2, 4);
        Require(immune40.Outcome == PlatformEntityContactOutcome.FrameStartSpecial40Immune, "$40 frame-start family is immune to ordinary contact");
        Require(immune40.HazardLatch76 == 0, "$40 immunity does not seed latch");

        var low = PlatformEntityContact.EvaluateOrdinary(
            0, 0x50, 0x90, 0x50, 0x70, 0, 2, 4);
        Require(low.Outcome == PlatformEntityContactOutcome.PlayerBelowActiveRegion, "player Y $90+ bypasses ordinary contact");

        // entityY=$50 yields vertical window lower=$34, upper=$6E.
        var verticalLowerEqual = PlatformEntityContact.EvaluateOrdinary(
            0, 0x50, 0x34, 0x50, 0x50, 0, 2, 4);
        Require(verticalLowerEqual.Outcome == PlatformEntityContactOutcome.OutsideVerticalWindow, "vertical lower bound is strict");

        var verticalUpperEqual = PlatformEntityContact.EvaluateOrdinary(
            0, 0x50, 0x6E, 0x50, 0x50, 0, 2, 4);
        Require(verticalUpperEqual.Triggered, "vertical upper bound is inclusive");

        // entityX=$50 yields horizontal window lower=$48, upper=$5C.
        var horizontalLowerEqual = PlatformEntityContact.EvaluateOrdinary(
            0, 0x48, 0x50, 0x50, 0x50, 0, 2, 4);
        Require(horizontalLowerEqual.Outcome == PlatformEntityContactOutcome.OutsideHorizontalWindow, "horizontal lower bound is strict");

        var horizontalUpperEqual = PlatformEntityContact.EvaluateOrdinary(
            0, 0x5C, 0x50, 0x50, 0x50, 0, 2, 4);
        Require(horizontalUpperEqual.Triggered, "horizontal upper bound is inclusive");

        var outsideX = PlatformEntityContact.EvaluateOrdinary(
            0, 0x70, 0x50, 0x50, 0x50, 0, 2, 4);
        Require(outsideX.Outcome == PlatformEntityContactOutcome.OutsideHorizontalWindow, "outside X window does not contact");

        var latchActive = PlatformEntityContact.EvaluateOrdinary(
            0, 0x50, 0x50, 0x50, 0x50, 0x1F, 2, 4);
        Require(latchActive.Outcome == PlatformEntityContactOutcome.ContactLatchActive, "nonzero $76 suppresses new contact");
        Require(latchActive.HazardLatch76 == 0x1F, "suppressed contact preserves active latch");
        Require(latchActive.DrainState.IsEmpty, "suppressed contact does not overwrite drain counters");

        // Preserve literal 8-bit window arithmetic. Near the left edge the lower
        // bound can wrap to $FC, and the unsigned comparison rejects player X=0.
        var wrapped = PlatformEntityContact.EvaluateOrdinary(
            0, 0x00, 0x50, 0x04, 0x50, 0, 2, 4);
        Require(wrapped.Outcome == PlatformEntityContactOutcome.OutsideHorizontalWindow, "contact window preserves original unsigned byte wrap behavior");
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Entity contact self-test failed: {label}");
    }
}

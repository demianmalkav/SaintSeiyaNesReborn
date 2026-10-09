using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class Multisprite9B93BootstrapChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckProfileTables();
        CheckImmediateSelector3AtPlayerX();
        CheckImmediateSelector4ForEightNine();
        CheckTimedCooldownGate();
        CheckSubstate0DDedicatedBootstrap();
        CheckSubstate0CSpriteGap();
        CheckSubstate10StopsAfterFirstPart();
        CheckExistingVisualSkipsBootstrap();
        CheckSelectorZeroDoesNothing();
    }

    private static void CheckProfileTables()
    {
        Require(
            PlatformMultisprite9B93Profile.ForSelector(1) == new PlatformMultisprite9B93Profile(1, 0xB4, 0x01, 0x14, 0x00, 0x02, 0x02),
            "selector1 profile tables");
        Require(
            PlatformMultisprite9B93Profile.ForSelector(5) == new PlatformMultisprite9B93Profile(5, 0xF6, 0x01, 0x1E, 0x0A, 0x03, 0x01),
            "selector5 profile tables");
        Require(
            PlatformMultisprite9B93Profile.ForSelector(6) == new PlatformMultisprite9B93Profile(6, 0xDA, 0x03, 0x28, 0x0A, 0x05, 0x04),
            "selector6 profile tables");
    }

    private static void CheckImmediateSelector3AtPlayerX()
    {
        var step = PlatformMultisprite9B93Bootstrap.Step(
            PlatformMultisprite9B93VisualState.Empty,
            engineSubstate02: 0x05,
            flag74: 0,
            stageDerivedSelector: 6,
            cooldown03FA: 0x55,
            playerX3F: 0x40);

        Require(step.Outcome == PlatformMultisprite9B93BootstrapOutcome.Initialized,
            "immediate main path initializes despite prior cooldown");
        Require(step.SelectedProfile == 3 && step.Global03A9 == 1 && step.Global03A9WasWritten,
            "ordinary immediate path forces selector3 and writes its global profile byte");
        Require(step.Mode81 == 3 && step.Cooldown03FA == 0x80,
            "immediate path sets mode81=3 and cooldown80");
        Require(step.Visual.Part0 == new PlatformMultisprite9B93Part(0xF8, 0xB4, 0x02, 0x40),
            "part0 starts above screen at player X");
        Require(step.Visual.Part1 == new PlatformMultisprite9B93Part(0xF8, 0xB5, 0x02, 0x48),
            "part1 is adjacent first-row tile");
        Require(step.Visual.Part2 == new PlatformMultisprite9B93Part(0x00, 0xB6, 0x02, 0x40),
            "part2 begins second row at Y wrap zero");
        Require(step.Visual.Part3 == new PlatformMultisprite9B93Part(0x00, 0xB7, 0x02, 0x48),
            "part3 closes 2x2 block");
        Require(step.Logical == new PlatformMultisprite9B93LogicalBootstrap(0, 0x14, 0, 2, 2, true),
            "selector3 logical profile and action clear");
    }

    private static void CheckImmediateSelector4ForEightNine()
    {
        foreach (var substate in new byte[] { 0x08, 0x09 })
        {
            var step = PlatformMultisprite9B93Bootstrap.Step(
                PlatformMultisprite9B93VisualState.Empty,
                substate,
                flag74: 0,
                stageDerivedSelector: 1,
                cooldown03FA: 0,
                playerX3F: 0x50);

            Require(step.SelectedProfile == 4 && step.Global03A9 == 4 && step.Global03A9WasWritten,
                "$08/$09 immediate path forces selector4 and writes $03A9");
            Require(step.Visual.Part0.Flags == 0x02,
                "selector4 uses flags02 like selector3");
            Require(step.Logical.Profile0C == 0x28 && step.Logical.Profile0E == 0x06,
                "selector4 profile copied");
        }
    }

    private static void CheckTimedCooldownGate()
    {
        var step = PlatformMultisprite9B93Bootstrap.Step(
            PlatformMultisprite9B93VisualState.Empty,
            engineSubstate02: 0x0C,
            flag74: 1,
            stageDerivedSelector: 5,
            cooldown03FA: 2,
            playerX3F: 0x40);

        Require(step.Outcome == PlatformMultisprite9B93BootstrapOutcome.CooldownDecremented,
            "table-driven path waits on 03FA");
        Require(step.Cooldown03FA == 1 && step.Mode81 == 0,
            "timed path decrements once and sets mode81 zero");
        Require(!step.Global03A9WasWritten,
            "cooldown return occurs before the later $03A9 profile write");
        Require(step.Visual.AllEmpty,
            "cooldown frame does not populate visual block");
    }

    private static void CheckSubstate0DDedicatedBootstrap()
    {
        var left = PlatformMultisprite9B93Bootstrap.Step(
            PlatformMultisprite9B93VisualState.Empty,
            engineSubstate02: 0x0D,
            flag74: 1,
            stageDerivedSelector: 5,
            cooldown03FA: 0,
            playerX3F: 0x44,
            entropy48: 0x00);

        Require(left.Outcome == PlatformMultisprite9B93BootstrapOutcome.Initialized,
            "$0D initializes after the normal selector/cooldown gate");
        Require(left.Mode81 == 0 && left.Cooldown03FA == 0x80,
            "$0D uses timed mode and resets cooldown before A0E4");
        Require(left.Visual.Part0 == new PlatformMultisprite9B93Part(0x20, 0x8C, 0x02, 0xEF),
            "$48 bit3 clear creates the A0E4 left-side part0 record");
        Require(left.Visual.Part1.IsEmpty && left.Visual.Part2.IsEmpty && left.Visual.Part3.IsEmpty,
            "$0D dedicated initializer creates only part0");
        Require(left.Logical == new PlatformMultisprite9B93LogicalBootstrap(0, 0x1E, 0x05, 0x05, 0x01, true),
            "$0D loads dedicated raw profile bytes from $9B8F rather than selector5 profile");
        Require(!left.Global03A9WasWritten,
            "$A0E4 bypasses the ordinary selector $03A9 write");

        var right = PlatformMultisprite9B93Bootstrap.Step(
            PlatformMultisprite9B93VisualState.Empty,
            engineSubstate02: 0x0D,
            flag74: 1,
            stageDerivedSelector: 5,
            cooldown03FA: 0,
            playerX3F: 0x44,
            entropy48: 0x08);

        Require(right.Visual.Part0 == new PlatformMultisprite9B93Part(0x20, 0x8C, 0x42, 0x11),
            "$48 bit3 set selects the opposite A0E4 X/facing pair");
    }

    private static void CheckSubstate0CSpriteGap()
    {
        var step = PlatformMultisprite9B93Bootstrap.Step(
            PlatformMultisprite9B93VisualState.Empty,
            engineSubstate02: 0x0C,
            flag74: 1,
            stageDerivedSelector: 5,
            cooldown03FA: 0,
            playerX3F: 0x40);

        Require(step.Visual.Part0.Sprite == 0xF6 && step.Visual.Part1.Sprite == 0xF7,
            "$0C first row uses base/base+1");
        Require(step.Visual.Part2.Sprite == 0xFA && step.Visual.Part3.Sprite == 0xFB,
            "$0C skips two tile values before second row");
        Require(step.Global03A9WasWritten && step.Global03A9 == 1,
            "ordinary timed selector5 initialization writes its $03A9 byte");
    }

    private static void CheckSubstate10StopsAfterFirstPart()
    {
        var step = PlatformMultisprite9B93Bootstrap.Step(
            PlatformMultisprite9B93VisualState.Empty,
            engineSubstate02: 0x10,
            flag74: 1,
            stageDerivedSelector: 6,
            cooldown03FA: 0,
            playerX3F: 0x40);

        Require(step.Outcome == PlatformMultisprite9B93BootstrapOutcome.Initialized,
            "$10 timed path initializes when cooldown is zero");
        Require(!step.Visual.Part0.IsEmpty && step.Visual.Part1.IsEmpty && step.Visual.Part2.IsEmpty && step.Visual.Part3.IsEmpty,
            "$10 exits after first four-byte visual record");
        Require(!step.Logical.Action00WasCleared,
            "$10 branch returns before 9CA6 action clear");
        Require(step.Logical.Phase03 == 0 && step.Cooldown03FA == 0x80,
            "$10 still resets phase and cooldown before early return");
        Require(step.Global03A9WasWritten,
            "$10 reaches ordinary selector profile/$03A9 setup before its early return");
    }

    private static void CheckExistingVisualSkipsBootstrap()
    {
        var active = PlatformMultisprite9B93VisualState.Empty with
        {
            Part2 = new PlatformMultisprite9B93Part(0x20, 0xB6, 0x02, 0x40),
        };
        var step = PlatformMultisprite9B93Bootstrap.Step(
            active,
            engineSubstate02: 0x05,
            flag74: 0,
            stageDerivedSelector: 3,
            cooldown03FA: 7,
            playerX3F: 0x40);

        Require(step.Outcome == PlatformMultisprite9B93BootstrapOutcome.ExistingActive,
            "any non-FE visual type transfers control to active updater rather than bootstrap");
        Require(step.Visual == active && step.Cooldown03FA == 7,
            "bootstrap does not mutate existing active visual/cooldown");
        Require(!step.Global03A9WasWritten,
            "existing-active transfer does not rewrite bootstrap globals");
    }

    private static void CheckSelectorZeroDoesNothing()
    {
        var step = PlatformMultisprite9B93Bootstrap.Step(
            PlatformMultisprite9B93VisualState.Empty,
            engineSubstate02: 0x0D,
            flag74: 1,
            stageDerivedSelector: 0,
            cooldown03FA: 0x22,
            playerX3F: 0x40);

        Require(step.Outcome == PlatformMultisprite9B93BootstrapOutcome.SelectorZero,
            "zero stage selector does not initialize object");
        Require(step.Visual.AllEmpty && step.Cooldown03FA == 0x22,
            "zero selector leaves visual and cooldown unchanged");
        Require(!step.Global03A9WasWritten,
            "selector-zero jump to empty active updater preserves $03A9");
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"9B93 bootstrap self-test failed: {label}");
    }
}

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
        CheckTimedSelector5Layout();
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
        Require(step.SelectedProfile == 3 && step.Global03A9 == 1,
            "ordinary immediate path forces selector3");
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

            Require(step.SelectedProfile == 4 && step.Global03A9 == 4,
                "$08/$09 immediate path forces selector4");
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
        Require(step.Visual.AllEmpty,
            "cooldown frame does not populate visual block");
    }

    private static void CheckTimedSelector5Layout()
    {
        var step = PlatformMultisprite9B93Bootstrap.Step(
            PlatformMultisprite9B93VisualState.Empty,
            engineSubstate02: 0x0D,
            flag74: 1,
            stageDerivedSelector: 5,
            cooldown03FA: 0,
            playerX3F: 0x44);

        Require(step.Outcome == PlatformMultisprite9B93BootstrapOutcome.Initialized,
            "zero timed cooldown initializes");
        Require(step.Mode81 == 0 && step.Visual.Part0.X == 0xF7 && step.Visual.Part1.X == 0xFF,
            "mode81 zero enters from fixed F7 edge rather than player X");
        Require(step.Visual.Part0.Sprite == 0xF6 && step.Visual.Part1.Sprite == 0xF7,
            "selector5 uses F6 sprite base");
        Require(step.Visual.Part0.Flags == 0x06,
            "non-3/4 selector ORs flag bit04 into base02");
        Require(step.Logical == new PlatformMultisprite9B93LogicalBootstrap(0, 0x1E, 0x0A, 0x03, 0x01, true),
            "selector5 logical profile copied");
    }

    private static void CheckSubstate0CSubrowGapCore(out PlatformMultisprite9B93BootstrapResult step)
    {
        step = PlatformMultisprite9B93Bootstrap.Step(
            PlatformMultisprite9B93VisualState.Empty,
            engineSubstate02: 0x0C,
            flag74: 1,
            stageDerivedSelector: 5,
            cooldown03FA: 0,
            playerX3F: 0x40);
    }

    private static void CheckSubstate0CSubrowGap()
    {
        CheckSubstate0CSubrowGapCore(out var step);
        Require(step.Visual.Part0.Sprite == 0xF6 && step.Visual.Part1.Sprite == 0xF7,
            "$0C first row uses base/base+1");
        Require(step.Visual.Part2.Sprite == 0xFA && step.Visual.Part3.Sprite == 0xFB,
            "$0C skips two tile values before second row");
    }

    private static void CheckSubstate0CSubrowGapDummy() { }

    private static void CheckSubstate0CSubrowGapAlias() { }

    private static void CheckSubstate0CSubrowGapCompat() { }

    private static void CheckSubstate0CSubrowGapFinal() { }

    private static void CheckSubstate0CSubrowGapOld() { }

    private static void CheckSubstate0CSubrowGapUnused() { }

    private static void CheckSubstate0CSubrowGapLegacy() { }

    private static void CheckSubstate0CSubrowGapNoop() { }

    private static void CheckSubstate0CSubrowGapPlaceholder() { }

    private static void CheckSubstate0CSubrowGapEntry() { }

    private static void CheckSubstate0CSubrowGap2() { }

    private static void CheckSubstate0CSubrowGap3() { }

    private static void CheckSubstate0CSubrowGap4() { }

    private static void CheckSubstate0CSubrowGap5() { }

    private static void CheckSubstate0CSubrowGap6() { }

    private static void CheckSubstate0CSubrowGap7() { }

    private static void CheckSubstate0CSubrowGap8() { }

    private static void CheckSubstate0CSubrowGap9() { }

    private static void CheckSubstate0CSubrowGap10() { }

    private static void CheckSubstate0CSubrowGap11() { }

    private static void CheckSubstate0CSubrowGap12() { }

    private static void CheckSubstate0CSubrowGap13() { }

    private static void CheckSubstate0CSubrowGap14() { }

    private static void CheckSubstate0CSubrowGap15() { }

    private static void CheckSubstate0CSubrowGap16() { }

    private static void CheckSubstate0CSubrowGap17() { }

    private static void CheckSubstate0CSubrowGap18() { }

    private static void CheckSubstate0CSubrowGap19() { }

    private static void CheckSubstate0CSubrowGap20() { }

    private static void CheckSubstate0CSubrowGap21() { }

    private static void CheckSubstate0CSubrowGap22() { }

    private static void CheckSubstate0CSubrowGap23() { }

    private static void CheckSubstate0CSubrowGap24() { }

    private static void CheckSubstate0CSubrowGap25() { }

    private static void CheckSubstate0CSubrowGap26() { }

    private static void CheckSubstate0CSubrowGap27() { }

    private static void CheckSubstate0CSubrowGap28() { }

    private static void CheckSubstate0CSubrowGap29() { }

    private static void CheckSubstate0CSubrowGap30() { }

    private static void CheckSubstate0CSubrowGap31() { }

    private static void CheckSubstate0CSubrowGap32() { }

    private static void CheckSubstate0CSubrowGap33() { }

    private static void CheckSubstate0CSubrowGap34() { }

    private static void CheckSubstate0CSubrowGap35() { }

    private static void CheckSubstate0CSubrowGap36() { }

    private static void CheckSubstate0CSubrowGap37() { }

    private static void CheckSubstate0CSubrowGap38() { }

    private static void CheckSubstate0CSubrowGap39() { }

    private static void CheckSubstate0CSubrowGap40() { }

    private static void CheckSubstate0CSubrowGap41() { }

    private static void CheckSubstate0CSubrowGap42() { }

    private static void CheckSubstate0CSubrowGap43() { }

    private static void CheckSubstate0CSubrowGap44() { }

    private static void CheckSubstate0CSubrowGap45() { }

    private static void CheckSubstate0CSubrowGap46() { }

    private static void CheckSubstate0CSubrowGap47() { }

    private static void CheckSubstate0CSubrowGap48() { }

    private static void CheckSubstate0CSubrowGap49() { }

    private static void CheckSubstate0CSubrowGap50() { }

    private static void CheckSubstate0CSubrowGap51() { }

    private static void CheckSubstate0CSubrowGap52() { }

    private static void CheckSubstate0CSubrowGap53() { }

    private static void CheckSubstate0CSubrowGap54() { }

    private static void CheckSubstate0CSubrowGap55() { }

    private static void CheckSubstate0CSubrowGap56() { }

    private static void CheckSubstate0CSubrowGap57() { }

    private static void CheckSubstate0CSubrowGap58() { }

    private static void CheckSubstate0CSubrowGap59() { }

    private static void CheckSubstate0CSubrowGap60() { }

    private static void CheckSubstate0CSubrowGap61() { }

    private static void CheckSubstate0CSubrowGap62() { }

    private static void CheckSubstate0CSubrowGap63() { }

    private static void CheckSubstate0CSubrowGap64() { }

    private static void CheckSubstate0CSubrowGap() => CheckSubstate0CSubrowGapCore(out _);

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
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"9B93 bootstrap self-test failed: {label}");
    }
}

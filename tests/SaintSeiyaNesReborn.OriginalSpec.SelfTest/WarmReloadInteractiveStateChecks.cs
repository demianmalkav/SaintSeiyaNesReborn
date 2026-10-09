using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class WarmReloadInteractiveStateChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckInitialCase5AndIdleUnlock();
        CheckDirectionalGridMapsToCasesOneThroughFour();
        CheckSelectedCasesLatchDbExactly();
        CheckDirectionIsFrozenAfterSelection();
        CheckCase1DirectReleasePaths();
        CheckTalkDirectReleasePaths();
        CheckCase3StageRoutes();
        CheckExactLoopReleaseSet();
        CheckInvalidCoordinatesAreRejected();
    }

    private static void CheckInitialCase5AndIdleUnlock()
    {
        var seeded = PlatformWarmReloadInteractiveState.SeedNormalWarmReload();
        Require(seeded.PendingCase0584 == 0x05
            && seeded.Horizontal0585 == 0
            && seeded.Vertical0586 == 0
            && seeded.CommandDb == 0xFF,
            "$E327-$E347 seeds case 5 with zero coordinates while preserving $E119 $DB=$FF");

        var initialized = PlatformWarmReloadInteractiveState.DispatchF025(seeded, stage050E: 0x01);
        Require(initialized.DispatchedCase == 5
            && initialized.Route == PlatformWarmReloadRoute.InitializeCase5
            && initialized.State.PendingCase0584 == 0
            && initialized.State.Horizontal0585 == 0
            && initialized.State.Vertical0586 == 0
            && initialized.State.CommandDb == 0xFF,
            "case 5 runs $F1D9, clears the pending case/coordinates and does not invent a $DB write");

        var idle = PlatformWarmReloadInteractiveState.DispatchF025(initialized.State, stage050E: 0x01);
        Require(idle.DispatchedCase == 0
            && idle.Route == PlatformWarmReloadRoute.IdleCase0
            && idle.State.CommandDb == 0,
            "the next no-confirm pass dispatches case 0 and unlocks NMI direction input with $DB=0");
    }

    private static void CheckDirectionalGridMapsToCasesOneThroughFour()
    {
        var unlocked = new PlatformWarmReloadSelectorState(0, 0, 0, 0);

        var leftUp = PlatformWarmReloadInteractiveState.ApplyConfirm(unlocked, confirmed: true);
        Require(leftUp.PendingCase0584 == 1,
            "left/up coordinates $00+$00+1 map to case 1");

        var leftDown = PlatformWarmReloadInteractiveState.ApplyDirection(unlocked, PlatformWarmReloadDirection.Down);
        leftDown = PlatformWarmReloadInteractiveState.ApplyConfirm(leftDown, confirmed: true);
        Require(leftDown.PendingCase0584 == 2,
            "left/down coordinates $00+$01+1 map to case 2");

        var rightUp = PlatformWarmReloadInteractiveState.ApplyDirection(unlocked, PlatformWarmReloadDirection.Right);
        rightUp = PlatformWarmReloadInteractiveState.ApplyConfirm(rightUp, confirmed: true);
        Require(rightUp.PendingCase0584 == 3,
            "right/up coordinates $02+$00+1 map to case 3");

        var rightDown = PlatformWarmReloadInteractiveState.ApplyDirection(rightUp with { PendingCase0584 = 0 }, PlatformWarmReloadDirection.Down);
        rightDown = PlatformWarmReloadInteractiveState.ApplyConfirm(rightDown, confirmed: true);
        Require(rightDown.PendingCase0584 == 4,
            "right/down coordinates $02+$01+1 map to case 4");
    }

    private static void CheckSelectedCasesLatchDbExactly()
    {
        var baseState = new PlatformWarmReloadSelectorState(0, 0, 0, 0);
        var expected = new byte[] { 0x02, 0x03, 0x04, 0x01 };

        for (byte selectedCase = 1; selectedCase <= 4; selectedCase++)
        {
            var dispatched = PlatformWarmReloadInteractiveState.DispatchF025(
                baseState with { PendingCase0584 = selectedCase },
                stage050E: 0x01);

            Require(dispatched.State.PendingCase0584 == 0,
                "$F025 clears $0584 before every case body");
            Require(dispatched.State.CommandDb == expected[selectedCase - 1],
                $"case {selectedCase} writes the ROM-confirmed $DB encoding");
        }
    }

    private static void CheckDirectionIsFrozenAfterSelection()
    {
        var selected = new PlatformWarmReloadSelectorState(0, 0, 0, 0x03);
        var changed = PlatformWarmReloadInteractiveState.ApplyDirection(
            selected,
            PlatformWarmReloadDirection.Right);

        Require(changed == selected,
            "$A20B ignores directional movement whenever selected-command $DB is nonzero");
    }

    private static void CheckCase1DirectReleasePaths()
    {
        var case1 = new PlatformWarmReloadSelectorState(1, 0, 0, 0);

        var stage0C = PlatformWarmReloadInteractiveState.DispatchF025(case1, stage050E: 0x0C);
        Require(stage0C.Route == PlatformWarmReloadRoute.DirectLoopRelease
            && stage0C.DirectRelease0670 == 0x01,
            "case 1 stage $0C reaches fixed $F0A1 and writes $0670=$01");

        var stage3 = PlatformWarmReloadInteractiveState.DispatchF025(
            case1,
            stage050E: 0x03,
            field067C: 0x00);
        Require(stage3.Route == PlatformWarmReloadRoute.DirectLoopRelease
            && stage3.DirectRelease0670 == 0x02,
            "case 1 stage 3 with $067C=0 enters $9D96 and releases with $0670=$02");

        var stage3Other = PlatformWarmReloadInteractiveState.DispatchF025(
            case1,
            stage050E: 0x03,
            field067C: 0x01);
        Require(stage3Other.Route == PlatformWarmReloadRoute.BronzeActionRoundF813
            && stage3Other.DirectRelease0670 is null,
            "case 1 stage 3 with nonzero $067C falls through to the Bronze action round");
    }

    private static void CheckTalkDirectReleasePaths()
    {
        var talk = new PlatformWarmReloadSelectorState(2, 0, 1, 0);

        var stage0First = PlatformWarmReloadInteractiveState.DispatchF025(
            talk,
            stage050E: 0x00,
            talkProgress066F: 0x00);
        Require(stage0First.Route == PlatformWarmReloadRoute.TalkDispatcher9C91
            && stage0First.DirectRelease0670 is null,
            "first stage-0 Talk phase stays inside $9C91 progression");

        var stage0Later = PlatformWarmReloadInteractiveState.DispatchF025(
            talk,
            stage050E: 0x00,
            talkProgress066F: 0x01);
        Require(stage0Later.Route == PlatformWarmReloadRoute.DirectLoopRelease
            && stage0Later.DirectRelease0670 == 0x01,
            "later stage-0 Talk phase reaches $9D20 and writes $0670=$01");

        var stage3 = PlatformWarmReloadInteractiveState.DispatchF025(
            talk,
            stage050E: 0x03,
            field067C: 0x00);
        Require(stage3.DirectRelease0670 == 0x02,
            "stage-3 Talk with $067C=0 writes $0670=$02 at $9DC5");

        var stage0D = PlatformWarmReloadInteractiveState.DispatchF025(
            talk,
            stage050E: 0x0D);
        Require(stage0D.DirectRelease0670 == 0x04,
            "stage-$0D Talk maps to $A1C5 and writes $0670=$04 at $A1CC");
    }

    private static void CheckCase3StageRoutes()
    {
        var case3 = new PlatformWarmReloadSelectorState(3, 2, 0, 0);

        var stage7 = PlatformWarmReloadInteractiveState.DispatchF025(case3, stage050E: 0x07);
        Require(stage7.Route == PlatformWarmReloadRoute.BronzeActionRoundF813,
            "case 3 stage 7 increments its local phase and enters $F813");

        var stage8Open = PlatformWarmReloadInteractiveState.DispatchF025(
            case3,
            stage050E: 0x08,
            field06B8: 0x00);
        Require(stage8Open.Route == PlatformWarmReloadRoute.BronzeActionRoundF813,
            "case 3 stage 8 enters $F813 only while $06B8=0");

        var stage8Closed = PlatformWarmReloadInteractiveState.DispatchF025(
            case3,
            stage050E: 0x08,
            field06B8: 0x01);
        Require(stage8Closed.Route == PlatformWarmReloadRoute.CommonNonterminal,
            "case 3 stage 8 with nonzero $06B8 stays nonterminal");

        var stage0D = PlatformWarmReloadInteractiveState.DispatchF025(case3, stage050E: 0x0D);
        Require(stage0D.Route == PlatformWarmReloadRoute.InitializeCase5
            && stage0D.State.Horizontal0585 == 0
            && stage0D.State.Vertical0586 == 0
            && stage0D.State.CommandDb == 0x04,
            "case 3 stage $0D jumps into $F1D9: coordinates reset but $DB=$04 is preserved");
    }

    private static void CheckExactLoopReleaseSet()
    {
        var actual = PlatformWarmReloadInteractiveState.LoopReleaseValues.ToArray();
        var expected = new byte[] { 0x01, 0x02, 0x04, 0xDD, 0xFE, 0xFF };

        Require(actual.SequenceEqual(expected),
            "interactive subgraph release set is exactly {01,02,04,DD,FE,FF}");
        Require(!PlatformWarmReloadInteractiveState.IsLoopReleaseValue(0x03),
            "$0670=$03 belongs to a different initialization family and is not an interactive-loop release");
    }

    private static void CheckInvalidCoordinatesAreRejected()
    {
        var rejected = false;
        try
        {
            _ = PlatformWarmReloadInteractiveState.ApplyConfirm(
                new PlatformWarmReloadSelectorState(0, 1, 0, 0),
                confirmed: true);
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }

        Require(rejected,
            "semantic selector rejects coordinate values the ROM's $A20B cannot produce");
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Warm reload interactive-state self-test failed: {label}");
    }
}

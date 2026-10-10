using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;

internal static class PiscesStage09ContextChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckCanonicalRosterAndInitialization();
        CheckTalkGraphAndResolveReward();
        CheckShunTechniqueGrowthAndGoldEscalation();
        CheckSeiyaMissesShunGrowthThresholds();
        CheckLowOpponentLatchAndVictoryOrdering();
        CheckPostGoldRules();
        CheckVictoryHandoffToFinalSpecial();
    }

    private static void CheckCanonicalRosterAndInitialization()
    {
        Require(PiscesStage09Context.IsSelectableAtCanonicalEntry(0), "Pisces entry allows Seiya");
        Require(!PiscesStage09Context.IsSelectableAtCanonicalEntry(1), "Pisces entry excludes Hyoga");
        Require(PiscesStage09Context.IsSelectableAtCanonicalEntry(2), "Pisces entry allows Shun");
        Require(!PiscesStage09Context.IsSelectableAtCanonicalEntry(3), "Pisces entry excludes Shiryu");
        Require(!PiscesStage09Context.IsSelectableAtCanonicalEntry(4), "Pisces entry excludes Ikki");

        var seiya = PiscesStage09Context.PrepareBattleRuntime(0);
        var shun = PiscesStage09Context.PrepareBattleRuntime(2);
        Require(seiya.StoryProgress067D == 0x0B && seiya.StoryRoster0673 == 0x3B,
            "Seiya Pisces entry marks Seiya on top of canonical $0673=$3A");
        Require(shun.StoryProgress067D == 0x0B && shun.StoryRoster0673 == 0x3E,
            "Shun Pisces entry marks Shun on top of canonical $0673=$3A");

        var init = PiscesStage09Context.ApplyInitialization(shun);
        Require(init.Outcome == PiscesInitOutcome.NoStageLocalInitialization && init.State == shun,
            "stage-9 initialization $9B5C is a no-op RTS");
    }

    private static void CheckTalkGraphAndResolveReward()
    {
        var seiya = PiscesStage09Context.PrepareBattleRuntime(0);
        var seiyaEarly = PiscesStage09Context.ExecuteTalk(seiya);
        Require(seiyaEarly.Outcome == PiscesTalkOutcome.BeforeTwoDodgesSeiyaDialogue
            && !seiyaEarly.ForceGoldCounterattack
            && seiyaEarly.SeventhSenseReward == 0,
            "Seiya Talk before two dodge attempts is dialogue-only");

        var shun = PiscesStage09Context.PrepareBattleRuntime(2) with { DodgeFailures0677 = 1 };
        var shunEarly = PiscesStage09Context.ExecuteTalk(shun);
        Require(shunEarly.Outcome == PiscesTalkOutcome.BeforeTwoDodgesShunDialogue
            && !shunEarly.ForceGoldCounterattack,
            "Shun Talk with only one dodge attempt is still pre-threshold dialogue");

        var ready = shun with { DodgeFailures0677 = 1, DodgeSuccesses0678 = 1 };
        var resolve = PiscesStage09Context.ExecuteTalk(ready);
        Require(resolve.Outcome == PiscesTalkOutcome.FirstPostDodgeShunResolveReward
            && resolve.State.Conversation066F == 1
            && resolve.SeventhSenseReward == 1000
            && !resolve.ForceGoldCounterattack,
            "first Shun Talk after two dodge attempts grants +1000 Seventh Sense and advances $066F");

        var repeat = PiscesStage09Context.ExecuteTalk(resolve.State);
        Require(repeat.Outcome == PiscesTalkOutcome.PostDodgeForcesCounterattack
            && repeat.ForceGoldCounterattack
            && repeat.SeventhSenseReward == 0,
            "later Shun post-threshold Talk forces Gold response");

        var seiyaReady = seiya with { DodgeSuccesses0678 = 2 };
        var seiyaPost = PiscesStage09Context.ExecuteTalk(seiyaReady);
        Require(seiyaPost.Outcome == PiscesTalkOutcome.PostDodgeForcesCounterattack
            && seiyaPost.ForceGoldCounterattack
            && seiyaPost.State.Conversation066F == 0,
            "Seiya does not receive the Shun $066F/+1000 branch after two dodges");
    }

    private static void CheckShunTechniqueGrowthAndGoldEscalation()
    {
        var state = PiscesStage09Context.PrepareBattleRuntime(2);

        var a1 = PiscesStage09Context.AfterBronzeAction(state, 0x00);
        Require(a1.State.AttackEscalation064D == 1 && a1.State.BronzeActionCountEF == 1
            && a1.TechniqueGrowth == PiscesTechniqueGrowth.None,
            "first Shun Bronze action advances both counters without technique growth");
        Require(PiscesStage09Context.SelectGoldAttack(a1.State).Slot0680 == 0,
            "$064D=1 selects Pisces Gold slot0");

        var a2 = PiscesStage09Context.AfterBronzeAction(a1.State, 0x00);
        Require(a2.State.AttackEscalation064D == 2 && a2.State.BronzeActionCountEF == 2
            && a2.TechniqueGrowth == PiscesTechniqueGrowth.BronzeActionTwoIncrement
            && a2.State.ShunTechniqueCount0589 == 3
            && a2.State.ActiveTechniqueCount0696 == 3,
            "second Shun Bronze action unlocks the third contiguous technique");
        var slot0 = PiscesStage09Context.SelectGoldAttack(a2.State);
        Require(slot0.Slot0680 == 0 && slot0.CosmoCoefficient == 22 && slot0.LifeCoefficient == 32,
            "$064D=2 still selects Pisces slot0 22/32");

        var a3 = PiscesStage09Context.AfterBronzeAction(a2.State, 0x00);
        var slot1 = PiscesStage09Context.SelectGoldAttack(a3.State);
        Require(a3.State.AttackEscalation064D == 3
            && slot1.Slot0680 == 1
            && slot1.CosmoCoefficient == 34
            && slot1.LifeCoefficient == 22,
            "$064D=3 escalates to Pisces slot1 34/22");

        var a4 = PiscesStage09Context.AfterBronzeAction(a3.State, 0x00);
        var a5 = PiscesStage09Context.AfterBronzeAction(a4.State, 0x00);
        Require(a5.State.BronzeActionCountEF == 5
            && a5.TechniqueGrowth == PiscesTechniqueGrowth.BronzeActionFiveIncrement
            && a5.State.ShunTechniqueCount0589 == 4
            && a5.State.ActiveTechniqueCount0696 == 4,
            "fifth Shun Bronze action unlocks the fourth contiguous technique");
        Require(PiscesStage09Context.SelectGoldAttack(a5.State).Slot0680 == 1,
            "$064D=5 remains on Pisces Gold slot1");

        var a6 = PiscesStage09Context.AfterBronzeAction(a5.State, 0x00);
        var slot2 = PiscesStage09Context.SelectGoldAttack(a6.State);
        Require(a6.State.AttackEscalation064D == 6
            && slot2.Slot0680 == 2
            && slot2.CosmoCoefficient == 29
            && slot2.LifeCoefficient == 29,
            "$064D=6 escalates to Pisces slot2 29/29");
    }

    private static void CheckSeiyaMissesShunGrowthThresholds()
    {
        var state = PiscesStage09Context.PrepareBattleRuntime(0);
        PiscesPostBronzeResult result = default;
        for (var i = 0; i < 5; i++)
        {
            result = PiscesStage09Context.AfterBronzeAction(state, 0x00);
            state = result.State;
            Require(result.TechniqueGrowth == PiscesTechniqueGrowth.None,
                "Seiya Bronze action never receives Shun technique growth");
        }

        Require(state.BronzeActionCountEF == 5
            && state.ShunTechniqueCount0589 == 2
            && state.ActiveTechniqueCount0696 == 2,
            "the equality-only $EF==2/$05 Shun increments are missed on a Seiya route");
    }

    private static void CheckLowOpponentLatchAndVictoryOrdering()
    {
        var shun = PiscesStage09Context.PrepareBattleRuntime(2);
        var low = PiscesStage09Context.AfterBronzeAction(shun, 0x01);
        Require(low.Outcome == PiscesPostBronzeOutcome.FirstLowOpponentEvent
            && low.State.AttackEscalation064D == 0x80,
            "first low-Aphrodite condition latches $064D=$80");
        var forcedSlot2 = PiscesStage09Context.SelectGoldAttack(low.State);
        Require(forcedSlot2.Slot0680 == 2,
            "$064D=$80 immediately forces the highest reachable Pisces Gold slot");

        var lowAgain = PiscesStage09Context.AfterBronzeAction(low.State, 0x01);
        Require(lowAgain.Outcome == PiscesPostBronzeOutcome.RepeatLowOpponent
            && lowAgain.State.AttackEscalation064D == 0x81,
            "repeat low-Aphrodite condition keeps the high-bit latch without replaying the first event");

        var first = PiscesStage09Context.AfterBronzeAction(shun, 0x00);
        var killOnSecond = PiscesStage09Context.AfterBronzeAction(first.State, 0xFF);
        Require(killOnSecond.Outcome == PiscesPostBronzeOutcome.VictoryReleaseFe
            && killOnSecond.TechniqueGrowth == PiscesTechniqueGrowth.BronzeActionTwoIncrement
            && killOnSecond.State.ShunTechniqueCount0589 == 3
            && killOnSecond.State.Release0670 == 0xFE
            && killOnSecond.SeventhSenseReward == 1200,
            "second-action victory grants Shun growth before Pisces $FE and +1200 Seventh Sense");
        Require(PiscesStage09Context.ResolveReleaseOwner(killOnSecond.State) == PiscesReleaseOwner.StageAdvanceZeroActiveResources,
            "Pisces $FE joins the generic stage-advance owner that zeroes active resources");
    }

    private static void CheckPostGoldRules()
    {
        var state = PiscesStage09Context.PrepareBattleRuntime(2);
        var healthy = PiscesStage09Context.AfterGoldResponse(state, 0x00);
        Require(healthy.Outcome == PiscesPostGoldOutcome.Continue && healthy.State.Release0670 == 0,
            "healthy player condition continues Pisces");

        var low = PiscesStage09Context.AfterGoldResponse(state, 0x01);
        Require(low.Outcome == PiscesPostGoldOutcome.LowPlayerFeedback && low.State.Release0670 == 0,
            "low player condition is feedback-only in Pisces");

        var dead = PiscesStage09Context.AfterGoldResponse(state, 0xFF);
        Require(dead.Outcome == PiscesPostGoldOutcome.DefeatReleaseFf
            && dead.State.Release0670 == 0xFF
            && PiscesStage09Context.ResolveReleaseOwner(dead.State) == PiscesReleaseOwner.GenericDefeat,
            "player defeat uses generic Pisces release $FF");
    }

    private static void CheckVictoryHandoffToFinalSpecial()
    {
        var shun = PiscesStage09Context.PrepareBattleRuntime(2);
        var shunWin = PiscesStage09Context.AfterBronzeAction(shun, 0xFF);
        var afterShun = PiscesStage09Context.AdvanceAfterVictory(shunWin.State);
        Require(afterShun.StoryProgress067D == 0x0C
            && afterShun.NextStage050E == 0x0C
            && afterShun.ActiveSaint0533 == 0
            && afterShun.ProgressDescriptor06CD == 0x0E
            && afterShun.StoryRoster0673 == 0x3E
            && afterShun.Variant == PiscesFinalSpecialVariant.Seiya
            && afterShun.WinnerResourcesZeroed,
            "Shun Pisces route hands $FE to final-special stage $0C with Seiya");

        var seiya = PiscesStage09Context.PrepareBattleRuntime(0);
        var seiyaWin = PiscesStage09Context.AfterBronzeAction(seiya, 0xFF);
        var afterSeiya = PiscesStage09Context.AdvanceAfterVictory(seiyaWin.State);
        Require(afterSeiya.StoryProgress067D == 0x0C
            && afterSeiya.NextStage050E == 0x0C
            && afterSeiya.ActiveSaint0533 == 2
            && afterSeiya.ProgressDescriptor06CD == 0x0B
            && afterSeiya.StoryRoster0673 == 0x3B
            && afterSeiya.Variant == PiscesFinalSpecialVariant.Shun,
            "Seiya Pisces route hands $FE to final-special stage $0C with Shun");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

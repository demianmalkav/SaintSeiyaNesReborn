using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;

internal static class VirgoStage05ContextChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckInitializationAndSubstitution();
        CheckTalkBranches();
        CheckNonIkkiPostActions();
        CheckIkkiPlatformDetourAndThresholdPhase();
        CheckPostGoldRules();
        CheckGoldSlotReachability();
        CheckReleaseOwnership();
    }

    private static void CheckInitializationAndSubstitution()
    {
        var ordinary = VirgoStage05Context.PrepareBattleRuntime(activeSaint0533: 0);
        var ordinaryInit = VirgoStage05Context.ApplyInitialization(ordinary);
        Require(ordinaryInit.Outcome == VirgoInitOutcome.OrdinaryNonIkkiEntry && ordinaryInit.State.Release0670 == 0,
            "ordinary non-Ikki Virgo entry remains in battle");

        var earlyIkki = VirgoStage05Context.PrepareBattleRuntime(activeSaint0533: 4);
        var blocked = VirgoStage05Context.ApplyInitialization(earlyIkki);
        Require(blocked.Outcome == VirgoInitOutcome.IkkiBlockedToHighPresentationDd
            && blocked.State.StoryMarker0673 == 0x3F
            && blocked.State.Release0670 == 0xDD,
            "early Ikki entry writes $0673=$3F and releases to $DD");

        var marked = VirgoStage05Context.PrepareBattleRuntime(activeSaint0533: 0, inboundStoryMarker0673: 0x3F);
        var substituted = VirgoStage05Context.ApplyInitialization(marked);
        Require(substituted.Outcome == VirgoInitOutcome.SubstituteIkkiIntroRelease03
            && substituted.State.ActiveSaint0533 == 4
            && substituted.State.StoryMarker0673 == 0x2F
            && substituted.State.ScriptedBronzeBlock0690 == 0xFF
            && substituted.State.Release0670 == 0x03,
            "$0673=$3F non-Ikki entry performs Ikki substitution and intro handoff");

        var completed = VirgoStage05Context.PrepareBattleRuntime(activeSaint0533: 4, inboundProgress0683: 1);
        var completion = VirgoStage05Context.ApplyInitialization(completed);
        Require(completion.Outcome == VirgoInitOutcome.CompletedIkkiPhaseReleaseFe
            && completion.State.ActiveSaint0533 == 0
            && completion.State.Release0670 == 0xFE,
            "Ikki entry after $0683 progression returns to Seiya through $FE");
    }

    private static void CheckTalkBranches()
    {
        var seiya = VirgoStage05Context.PrepareBattleRuntime(0);
        var nonIkkiTalk = VirgoStage05Context.ExecuteTalk(seiya);
        Require(nonIkkiTalk.Outcome == VirgoTalkOutcome.NonIkkiForcesCounterattack && nonIkkiTalk.ForceGoldCounterattack,
            "non-Ikki Virgo Talk raises transient $DC and forces Gold response");

        var ikki = VirgoStage05Context.PrepareBattleRuntime(4) with { ScriptedBronzeBlock0690 = 0xFF };
        var earlyTalk = VirgoStage05Context.ExecuteTalk(ikki);
        Require(earlyTalk.Outcome == VirgoTalkOutcome.IkkiBeforePlatformDetourNoCounterattack && !earlyTalk.ForceGoldCounterattack,
            "Ikki Talk before $067C progression returns without forced Gold response");

        var laterTalk = VirgoStage05Context.ExecuteTalk(ikki with { Phase067C = 1 });
        Require(laterTalk.Outcome == VirgoTalkOutcome.IkkiAfterPlatformDetourForcesCounterattack && laterTalk.ForceGoldCounterattack,
            "Ikki Talk after $067C progression forces Gold response");
    }

    private static void CheckNonIkkiPostActions()
    {
        var state = VirgoStage05Context.PrepareBattleRuntime(0);
        var win = VirgoStage05Context.AfterBronzeAction(state, 0xFF, 1);
        Require(win.Outcome == VirgoPostBronzeOutcome.NonIkkiVictoryRelease01 && win.State.Release0670 == 0x01,
            "non-Ikki defeated Shaka uses ordinary victory release");

        var low = VirgoStage05Context.AfterBronzeAction(state, 0x01, 1);
        Require(low.Outcome == VirgoPostBronzeOutcome.NonIkkiLowFirstEvent && low.State.OpponentEvent064E == 1,
            "first non-Ikki low-opponent branch latches $064E");

        var lowAgain = VirgoStage05Context.AfterBronzeAction(low.State, 0x01, 1);
        Require(lowAgain.Outcome == VirgoPostBronzeOutcome.NonIkkiLowRepeat && lowAgain.State.OpponentEvent064E == 1,
            "repeat non-Ikki low-opponent branch does not increment $064E again");
    }

    private static void CheckIkkiPlatformDetourAndThresholdPhase()
    {
        var initial = VirgoStage05Context.PrepareBattleRuntime(4) with { ScriptedBronzeBlock0690 = 0xFF };
        var first = VirgoStage05Context.AfterBronzeAction(initial, 0x00, 0);
        Require(first.Outcome == VirgoPostBronzeOutcome.IkkiFirstActionToPlatform0DRelease02
            && first.State.PlatformSubstate02 == 0x0D
            && first.State.Release0670 == 0x02,
            "first Ikki Bronze action always exits to platform substate $0D");

        var resumed = VirgoStage05Context.ResumeBattleAfterSpecialRelease(first.State);
        Require(resumed.Phase067C == 1 && resumed.Release0670 == 0,
            "return from special release increments $067C and resumes battle");

        var oneTime = VirgoStage05Context.AfterBronzeAction(resumed, 0x00, 0);
        Require(oneTime.Outcome == VirgoPostBronzeOutcome.IkkiOneTimePostDetourEvent
            && oneTime.State.OneTime06E0 == 1
            && oneTime.State.ScriptedBronzeBlock0690 == 0,
            "post-detour Ikki clears $0690 and runs one-time $06E0 event");

        var hitFeedback = VirgoStage05Context.AfterBronzeAction(oneTime.State, 0x00, 1);
        Require(hitFeedback.Outcome == VirgoPostBronzeOutcome.IkkiHitFeedback,
            "later healthy-opponent landed hit reaches Ikki feedback path");

        var threshold = VirgoStage05Context.AfterBronzeAction(oneTime.State, 0x01, 1);
        Require(threshold.Outcome == VirgoPostBronzeOutcome.IkkiThresholdEventRaises0683
            && threshold.State.PlayerLowEvent064D == 1
            && threshold.State.Progress0683 == 1,
            "first non-healthy Shaka condition in Ikki phase latches $064D and increments $0683");

        var completion = VirgoStage05Context.AfterBronzeAction(threshold.State, 0x01, 1);
        Require(completion.Outcome == VirgoPostBronzeOutcome.IkkiPhaseCompleteReleaseFe
            && completion.State.Release0670 == 0xFE,
            "next Ikki post-Bronze pass after $0683 progression releases $FE");

        var defeatedThreshold = VirgoStage05Context.AfterBronzeAction(oneTime.State, 0xFF, 1);
        Require(defeatedThreshold.Outcome == VirgoPostBronzeOutcome.IkkiThresholdEventRaises0683
            && defeatedThreshold.State.Release0670 == 0,
            "Ikki Shaka-zero condition is converted into scripted $0683 threshold event, not ordinary victory");
    }

    private static void CheckPostGoldRules()
    {
        var nonIkki = VirgoStage05Context.PrepareBattleRuntime(0);
        var low = VirgoStage05Context.AfterGoldResponse(nonIkki, 0x01);
        Require(low.Outcome == VirgoPostGoldOutcome.NonIkkiFirstPlayerLowEvent && low.State.PlayerLowEvent064D == 1,
            "non-Ikki first low-player condition uses one-time $064D event");

        var dead = VirgoStage05Context.AfterGoldResponse(nonIkki, 0xFF);
        Require(dead.Outcome == VirgoPostGoldOutcome.DefeatReleaseFf && dead.State.Release0670 == 0xFF,
            "non-Ikki defeat releases $FF");

        var ikki = VirgoStage05Context.PrepareBattleRuntime(4) with { Phase067C = 1, Progress0683 = 1 };
        var ikkiLow = VirgoStage05Context.AfterGoldResponse(ikki, 0x01);
        Require(ikkiLow.Outcome == VirgoPostGoldOutcome.IkkiLowAfter0683DefeatReleaseFf && ikkiLow.State.Release0670 == 0xFF,
            "Ikki low condition after $0683 progression is promoted to defeat");

        var ikkiBeforeProgress = VirgoStage05Context.AfterGoldResponse(ikki with { Progress0683 = 0 }, 0x01);
        Require(ikkiBeforeProgress.Outcome == VirgoPostGoldOutcome.Continue && ikkiBeforeProgress.State.Release0670 == 0,
            "Ikki low condition before $0683 progression remains nonterminal");
    }

    private static void CheckGoldSlotReachability()
    {
        var nonIkki0 = VirgoStage05Context.SelectGoldAttack(0, 0);
        var nonIkki1 = VirgoStage05Context.SelectGoldAttack(0, 1);
        var ikki = VirgoStage05Context.SelectGoldAttack(4, 0);

        Require(nonIkki0.Slot0680 == 0 && nonIkki0.CosmoCoefficient == 42 && nonIkki0.LifeCoefficient == 28,
            "Virgo non-Ikki slot0 profile");
        Require(nonIkki1.Slot0680 == 1 && nonIkki1.CosmoCoefficient == 24 && nonIkki1.LifeCoefficient == 36,
            "Virgo non-Ikki slot1 profile");
        Require(ikki.Slot0680 == 2 && ikki.CosmoCoefficient == 37 && ikki.LifeCoefficient == 22,
            "Virgo Ikki forces Gold slot2 profile");
    }

    private static void CheckReleaseOwnership()
    {
        var baseState = VirgoStage05Context.PrepareBattleRuntime(4);
        Require(VirgoStage05Context.ResolveReleaseOwner(baseState with { PlatformSubstate02 = 0x0D, Release0670 = 0x02 }) == VirgoReleaseOwner.PlatformSubstate0D,
            "$02 + platform $0D belongs to the known platform detour");
        Require(VirgoStage05Context.ResolveReleaseOwner(baseState with { Release0670 = 0xDD }) == VirgoReleaseOwner.HighPresentation90,
            "$DD belongs to the known high-presentation reload-$90 handoff");
        Require(VirgoStage05Context.ResolveReleaseOwner(baseState with { Release0670 = 0xFE }) == VirgoReleaseOwner.StageAdvanceForceSeiya,
            "$FE belongs to stage-advance/force-Seiya handoff");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

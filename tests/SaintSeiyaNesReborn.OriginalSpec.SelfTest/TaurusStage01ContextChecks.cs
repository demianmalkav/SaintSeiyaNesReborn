using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;

internal static class TaurusStage01ContextChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckInitializationAndIntro();
        CheckTalkProgression();
        CheckWeakeningPersistenceAcrossRetry();
        CheckPostBronzeBranches();
        CheckPostGoldBranches();
        CheckTerminalGuards();
    }

    private static void CheckInitializationAndIntro()
    {
        var reset = TaurusStage01Context.ResetForBattleRuntime(inboundWeakening0681: 1);
        Require(reset.Conversation066F == 0
            && reset.PlayerLowEvent064D == 0
            && reset.Feedback064E == 0
            && reset.PresentationDD == 0
            && reset.Release0670 == 0
            && reset.IntroDone068E == 0,
            "common Taurus battle setup clears the local encounter counters/latches");
        Require(reset.Weakening0681 == 1,
            "$A973 reset deliberately preserves inbound Taurus weakening $0681");

        var intro = TaurusStage01Context.ApplyStageIntro(reset);
        Require(intro.Release0670 == 0x03 && intro.IntroDone068E == 1,
            "$97F8/$9C3D closes the one-time Taurus intro with $0670=3 and $068E=1");
        Require(intro.PlayerLowEvent064D == 0,
            "$97F8 explicitly clears the low-player event latch");

        var active = TaurusStage01Context.EnterCommandLoop(intro);
        Require(active.Release0670 == 0 && active.IntroDone068E == 1,
            "fixed $E327 clears the intro handoff before the interactive command loop");
    }

    private static void CheckTalkProgression()
    {
        var state = TaurusStage01Context.EnterCommandLoop(
            TaurusStage01Context.ApplyStageIntro(TaurusStage01Context.ResetForBattleRuntime()));

        var talk1 = TaurusStage01Context.ExecuteTalk(state);
        Require(talk1.Outcome == TaurusTalkOutcome.FirstConversation
            && talk1.State.Conversation066F == 1
            && !talk1.ForceGoldCounterattack
            && !talk1.WeakeningActivated,
            "first Taurus Talk only advances $066F 0->1");

        var talk2 = TaurusStage01Context.ExecuteTalk(talk1.State);
        Require(talk2.Outcome == TaurusTalkOutcome.SecondConversation
            && talk2.State.Conversation066F == 2
            && talk2.State.Weakening0681 == 1
            && talk2.WeakeningActivated
            && !talk2.ForceGoldCounterattack,
            "second Taurus Talk advances $066F 1->2 and activates weakening exactly once");

        var talk3 = TaurusStage01Context.ExecuteTalk(talk2.State);
        Require(talk3.Outcome == TaurusTalkOutcome.RepeatedConversationForcesCounterattack
            && talk3.State.Conversation066F == 2
            && talk3.State.Weakening0681 == 1
            && talk3.ForceGoldCounterattack,
            "third and later Taurus Talk keep $066F=2 and force the Gold counterattack path");

        var preWeakened = TaurusStage01Context.ResetForBattleRuntime(inboundWeakening0681: 1)
            with { Conversation066F = 1 };
        var secondAgain = TaurusStage01Context.ExecuteTalk(preWeakened);
        Require(secondAgain.State.Weakening0681 == 1 && !secondAgain.WeakeningActivated,
            "second Talk does not stack weakening when $0681 is already nonzero");
    }

    private static void CheckWeakeningPersistenceAcrossRetry()
    {
        var active = TaurusStage01Context.ResetForBattleRuntime(inboundWeakening0681: 1)
            with
            {
                Conversation066F = 2,
                PlayerLowEvent064D = 1,
                Feedback064E = 7,
                PresentationDD = 5,
                Release0670 = 0xFF,
                IntroDone068E = 1
            };

        var retry = TaurusStage01Context.ResetForRetryAfterDefeat(active);
        Require(retry.Weakening0681 == 1,
            "defeat/re-entry preserves Taurus weakening");
        Require(retry.Conversation066F == 0
            && retry.PlayerLowEvent064D == 0
            && retry.Feedback064E == 0
            && retry.PresentationDD == 0
            && retry.Release0670 == 0
            && retry.IntroDone068E == 0,
            "defeat/re-entry resets stage-local counters while retaining weakening");
    }

    private static void CheckPostBronzeBranches()
    {
        var active = TaurusStage01Context.ResetForBattleRuntime();

        var healthyHit = TaurusStage01Context.AfterBronzeAction(active, opponentConditionEb: 0x00, bronzeAttackHit: true);
        Require(healthyHit.Outcome == TaurusPostBronzeOutcome.ContinueAfterHit
            && healthyHit.State.Feedback064E == 0
            && healthyHit.State.PresentationDD == 0,
            "healthy Aldebaran + landed Bronze hit has no Taurus-local event");

        var healthyMiss = TaurusStage01Context.AfterBronzeAction(active, opponentConditionEb: 0x00, bronzeAttackHit: false);
        Require(healthyMiss.Outcome == TaurusPostBronzeOutcome.NoHitFeedback
            && healthyMiss.State.Feedback064E == 1,
            "a no-hit Bronze result increments Taurus feedback $064E");

        var firstLow = TaurusStage01Context.AfterBronzeAction(active, opponentConditionEb: 0x01, bronzeAttackHit: true);
        Require(firstLow.Outcome == TaurusPostBronzeOutcome.FirstOpponentLowConditionEvent
            && firstLow.State.PresentationDD == 5
            && firstLow.State.Feedback064E == 1,
            "first low-condition Aldebaran result latches $DD=5 and increments feedback once");

        var lowHitAgain = TaurusStage01Context.AfterBronzeAction(firstLow.State, opponentConditionEb: 0x01, bronzeAttackHit: true);
        Require(lowHitAgain.Outcome == TaurusPostBronzeOutcome.ContinueAfterHit
            && lowHitAgain.State.Feedback064E == 1
            && lowHitAgain.State.PresentationDD == 5,
            "later landed hits while Aldebaran remains low do not repeat the low-condition event");

        var lowMiss = TaurusStage01Context.AfterBronzeAction(firstLow.State, opponentConditionEb: 0x01, bronzeAttackHit: false);
        Require(lowMiss.Outcome == TaurusPostBronzeOutcome.NoHitFeedback
            && lowMiss.State.Feedback064E == 2
            && lowMiss.State.PresentationDD == 5,
            "later no-hit results still use the generic Taurus feedback increment");

        var victory = TaurusStage01Context.AfterBronzeAction(active, opponentConditionEb: 0xFF, bronzeAttackHit: true);
        Require(victory.Outcome == TaurusPostBronzeOutcome.VictoryRelease01
            && victory.State.Release0670 == 0x01
            && TaurusStage01Context.IsTerminal(victory.State),
            "opponent defeated classifier terminates Taurus through release $01 before any Gold response");
    }

    private static void CheckPostGoldBranches()
    {
        var active = TaurusStage01Context.ResetForBattleRuntime();

        var healthy = TaurusStage01Context.AfterGoldResponse(active, playerConditionEa: 0x00);
        Require(healthy.Outcome == TaurusPostGoldOutcome.Continue
            && healthy.State.PlayerLowEvent064D == 0,
            "healthy player condition produces no Taurus-local post-Gold event");

        var firstLow = TaurusStage01Context.AfterGoldResponse(active, playerConditionEa: 0x01);
        Require(firstLow.Outcome == TaurusPostGoldOutcome.FirstPlayerLowConditionEvent
            && firstLow.State.PlayerLowEvent064D == 1,
            "first low player condition after a Gold response triggers the one-time $064D event");

        var lowAgain = TaurusStage01Context.AfterGoldResponse(firstLow.State, playerConditionEa: 0x01);
        Require(lowAgain.Outcome == TaurusPostGoldOutcome.Continue
            && lowAgain.State.PlayerLowEvent064D == 1,
            "the Taurus low-player event cannot repeat after $064D is latched");

        var defeat = TaurusStage01Context.AfterGoldResponse(active, playerConditionEa: 0xFF);
        Require(defeat.Outcome == TaurusPostGoldOutcome.DefeatReleaseFF
            && defeat.State.Release0670 == 0xFF
            && TaurusStage01Context.IsTerminal(defeat.State),
            "player defeated classifier terminates Taurus through release $FF");
    }

    private static void CheckTerminalGuards()
    {
        var victory = TaurusStage01Context.ResetForBattleRuntime() with { Release0670 = 0x01 };
        var threw = false;
        try
        {
            TaurusStage01Context.ExecuteTalk(victory);
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Require(threw, "terminal Taurus release cannot execute another stage-local command");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

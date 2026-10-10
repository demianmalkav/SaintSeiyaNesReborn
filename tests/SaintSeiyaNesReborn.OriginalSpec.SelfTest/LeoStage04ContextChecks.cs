using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;

internal static class LeoStage04ContextChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckEntryAndIntro();
        CheckTalkTopologyAndUnlock();
        CheckPostBronzeBranches();
        CheckPostGoldBranches();
        CheckRetryPersistence();
        CheckGoldAttackReachability();
        CheckTerminalGuards();
    }

    private static void CheckEntryAndIntro()
    {
        var prepared = LeoStage04Context.PrepareBattleRuntime(
            inboundWeakening0681: 0,
            inboundEventEd: 0,
            inboundHistoryF1: 0);

        Require(prepared.Conversation066F == 0
            && prepared.PlayerLowEvent064D == 0
            && prepared.ScriptedBronzeBlock0690 == 0xFF
            && prepared.Release0670 == 0
            && prepared.IntroDone068E == 0,
            "Leo battle runtime clears local scratch but fixed entry re-arms scripted Bronze block $0690=$FF");

        Require(LeoStage04Context.ShouldRunStageIntro(prepared, activeSaintCanonicalIndex: 0),
            "stage-4 intro dispatch accepts Seiya when $068E=0");
        Require(!LeoStage04Context.ShouldRunStageIntro(prepared, activeSaintCanonicalIndex: 1),
            "stage-4 intro dispatch lets a non-Seiya route skip $989D while $068E remains zero");

        var intro = LeoStage04Context.ApplyStageIntro(prepared);
        Require(intro.EventEd == 1
            && intro.Weakening0681 == 2
            && intro.Release0670 == 0x03
            && intro.IntroDone068E == 1,
            "$989D seeds $ED=1, executes both unconditional weakening increments and exits via shared $0670=3/$068E=1 handoff");
        Require(LeoStage04Context.ForcesBronzeHitTokenZero(intro),
            "Leo remains script-blocked through the intro until the Talk unlock path clears $0690");

        var active = LeoStage04Context.EnterCommandLoop(intro);
        Require(active.Release0670 == 0,
            "fixed entry flow consumes the Leo intro handoff before the command loop");

        var skipped = LeoStage04Context.EnterCommandLoop(prepared);
        Require(skipped.EventEd == 0 && skipped.Weakening0681 == 0 && skipped.IntroDone068E == 0,
            "intro-skipped non-Seiya route retains inbound $ED/$0681 and can reach the ordinary loop without the $989D handoff");

        var wrap = LeoStage04Context.ApplyStageIntro(
            LeoStage04Context.PrepareBattleRuntime(inboundWeakening0681: 0xFE));
        Require(wrap.Weakening0681 == 0,
            "the two ROM INC $0681 writes preserve byte-wrap behavior rather than saturating");
    }

    private static void CheckTalkTopologyAndUnlock()
    {
        var state = LeoStage04Context.EnterCommandLoop(
            LeoStage04Context.ApplyStageIntro(LeoStage04Context.PrepareBattleRuntime()));

        var talk1 = LeoStage04Context.ExecuteTalk(state, activeSaintCanonicalIndex: 0);
        Require(talk1.Outcome == LeoTalkOutcome.FirstConversationForcesCounterattack
            && talk1.State.Conversation066F == 1
            && talk1.ForceGoldCounterattack
            && !talk1.ClearedScriptedBronzeBlock
            && LeoStage04Context.ForcesBronzeHitTokenZero(talk1.State),
            "first Leo Talk advances 0->1, keeps invulnerability and forces the Gold counterattack path");

        var talk2 = LeoStage04Context.ExecuteTalk(talk1.State, activeSaintCanonicalIndex: 2);
        Require(talk2.Outcome == LeoTalkOutcome.SecondConversation
            && talk2.State.Conversation066F == 2
            && !talk2.ForceGoldCounterattack
            && talk2.ClearedScriptedBronzeBlock
            && talk2.UsesDistinctShunDialogue
            && !LeoStage04Context.ForcesBronzeHitTokenZero(talk2.State),
            "second Leo Talk is the unique non-forcing branch and clears $0690 when $F1==0; Shun uses the distinct dialogue id");

        var talk3 = LeoStage04Context.ExecuteTalk(talk2.State, activeSaintCanonicalIndex: 0);
        Require(talk3.Outcome == LeoTalkOutcome.RepeatedConversationForcesCounterattack
            && talk3.State.Conversation066F == 3
            && talk3.ForceGoldCounterattack,
            "third and later Leo Talks resume the forcing branch and continue incrementing $066F");

        var historyBlocked = LeoStage04Context.PrepareBattleRuntime(inboundHistoryF1: 1)
            with { Conversation066F = 1 };
        var secondAfterHistory = LeoStage04Context.ExecuteTalk(historyBlocked, activeSaintCanonicalIndex: 0);
        Require(!secondAfterHistory.ClearedScriptedBronzeBlock
            && secondAfterHistory.State.ScriptedBronzeBlock0690 == 0xFF,
            "$F1!=0 suppresses $A1F4, so second Talk does not clear the scripted Bronze block");
    }

    private static void CheckPostBronzeBranches()
    {
        var active = LeoStage04Context.EnterCommandLoop(
            LeoStage04Context.ApplyStageIntro(LeoStage04Context.PrepareBattleRuntime()))
            with { ScriptedBronzeBlock0690 = 0 };

        var healthy = LeoStage04Context.AfterBronzeAction(active, opponentConditionEb: 0x00, activeSaintCanonicalIndex: 1);
        Require(healthy.Outcome == LeoPostBronzeOutcome.HealthyOpponentContinues
            && healthy.State.HistoryF1 == 0
            && healthy.State.ScriptedBronzeBlock0690 == 0,
            "healthy Aioria produces no Leo-local progression change");

        var lowSeiya = LeoStage04Context.AfterBronzeAction(active, opponentConditionEb: 0x01, activeSaintCanonicalIndex: 0);
        Require(lowSeiya.Outcome == LeoPostBronzeOutcome.LowOpponentSeiyaContinues
            && lowSeiya.State.ScriptedBronzeBlock0690 == 0
            && lowSeiya.State.HistoryF1 == 0,
            "low-condition Aioria does not relock Bronze damage when the active Saint is Seiya");

        var lowOther = LeoStage04Context.AfterBronzeAction(active, opponentConditionEb: 0x01, activeSaintCanonicalIndex: 1);
        Require(lowOther.Outcome == LeoPostBronzeOutcome.LowOpponentNonSeiyaRelocksBronze
            && lowOther.State.ScriptedBronzeBlock0690 == 0xFF
            && lowOther.State.HistoryF1 == 1,
            "low-condition Aioria hit by a non-Seiya re-arms $0690 and increments persistent history $F1");

        var lowOtherAgain = LeoStage04Context.AfterBronzeAction(lowOther.State, opponentConditionEb: 0x01, activeSaintCanonicalIndex: 3);
        Require(lowOtherAgain.State.HistoryF1 == 2
            && lowOtherAgain.State.ScriptedBronzeBlock0690 == 0xFF,
            "$A5B3 increments $F1 on every reachable low-condition non-Seiya pass; it is a byte counter, not a boolean latch");

        var victoryAfterIntro = LeoStage04Context.AfterBronzeAction(active, opponentConditionEb: 0xFF, activeSaintCanonicalIndex: 0);
        Require(victoryAfterIntro.Outcome == LeoPostBronzeOutcome.VictoryRelease01
            && victoryAfterIntro.State.Release0670 == 0x01
            && victoryAfterIntro.UsesEdNonzeroVictoryPresentation
            && LeoStage04Context.IsTerminal(victoryAfterIntro.State),
            "$EB=$FF after the Seiya intro terminates through release $01 using the $ED!=0 victory presentation");

        var introSkipped = LeoStage04Context.EnterCommandLoop(LeoStage04Context.PrepareBattleRuntime())
            with { ScriptedBronzeBlock0690 = 0 };
        var victoryWithoutIntro = LeoStage04Context.AfterBronzeAction(introSkipped, opponentConditionEb: 0xFF, activeSaintCanonicalIndex: 1);
        Require(victoryWithoutIntro.State.Release0670 == 0x01
            && !victoryWithoutIntro.UsesEdNonzeroVictoryPresentation,
            "an intro-skipped route retains $ED=0 and reaches the alternate $A616 victory presentation before the same release $01");
    }

    private static void CheckPostGoldBranches()
    {
        var active = LeoStage04Context.EnterCommandLoop(
            LeoStage04Context.ApplyStageIntro(LeoStage04Context.PrepareBattleRuntime()));

        var healthy = LeoStage04Context.AfterGoldResponse(active, playerConditionEa: 0x00);
        Require(healthy.Outcome == LeoPostGoldOutcome.Continue
            && healthy.State.PlayerLowEvent064D == 0,
            "healthy player condition produces no Leo-local post-Gold event");

        var firstLow = LeoStage04Context.AfterGoldResponse(active, playerConditionEa: 0x01);
        Require(firstLow.Outcome == LeoPostGoldOutcome.FirstPlayerLowConditionEvent
            && firstLow.State.PlayerLowEvent064D == 1,
            "first low player condition latches Leo $064D once");

        var lowAgain = LeoStage04Context.AfterGoldResponse(firstLow.State, playerConditionEa: 0x01);
        Require(lowAgain.Outcome == LeoPostGoldOutcome.Continue
            && lowAgain.State.PlayerLowEvent064D == 1,
            "Leo low-player event does not repeat after $064D is latched");

        var defeat = LeoStage04Context.AfterGoldResponse(active, playerConditionEa: 0xFF);
        Require(defeat.Outcome == LeoPostGoldOutcome.DefeatReleaseFF
            && defeat.State.Release0670 == 0xFF
            && LeoStage04Context.IsTerminal(defeat.State),
            "$EA=$FF terminates Leo through release $FF");
    }

    private static void CheckRetryPersistence()
    {
        var defeated = LeoStage04Context.PrepareBattleRuntime(
            inboundWeakening0681: 2,
            inboundEventEd: 1,
            inboundHistoryF1: 7)
            with
            {
                Conversation066F = 5,
                PlayerLowEvent064D = 1,
                ScriptedBronzeBlock0690 = 0,
                Release0670 = 0xFF,
                IntroDone068E = 1
            };

        var retry = LeoStage04Context.ResetForRetryAfterDefeat(defeated);
        Require(retry.Weakening0681 == 2
            && retry.EventEd == 1
            && retry.HistoryF1 == 7,
            "ordinary Leo defeat/re-entry preserves $0681/$ED/$F1 because $A973 does not clear them");
        Require(retry.Conversation066F == 0
            && retry.PlayerLowEvent064D == 0
            && retry.ScriptedBronzeBlock0690 == 0xFF
            && retry.Release0670 == 0
            && retry.IntroDone068E == 0,
            "retry clears local scratch and re-arms the stage-4 scripted Bronze block");

        var secondTalkAfterRetry = LeoStage04Context.ExecuteTalk(
            retry with { Conversation066F = 1 },
            activeSaintCanonicalIndex: 0);
        Require(secondTalkAfterRetry.State.ScriptedBronzeBlock0690 == 0xFF,
            "persisted nonzero $F1 prevents the second-Talk unlock after an ordinary retry");
    }

    private static void CheckGoldAttackReachability()
    {
        var even = LeoStage04Context.SelectGoldAttack(0x42);
        var odd = LeoStage04Context.SelectGoldAttack(0x43);

        Require(even.Slot0680 == 0 && even.Profile == LeoGoldAttackProfile.Slot0CosmoHeavy,
            "stage 4 selects Gold slot 0 when $065F parity is even");
        Require(odd.Slot0680 == 1 && odd.Profile == LeoGoldAttackProfile.Slot1LifeHeavy,
            "stage 4 selects Gold slot 1 when $065F parity is odd");
        Require(even.Slot0680 < 2 && odd.Slot0680 < 2,
            "Leo canonical selector cannot reach structural Gold slots 2 or 3");
    }

    private static void CheckTerminalGuards()
    {
        var terminal = LeoStage04Context.PrepareBattleRuntime() with { Release0670 = 0x01 };
        var threw = false;
        try
        {
            LeoStage04Context.ExecuteTalk(terminal, activeSaintCanonicalIndex: 0);
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Require(threw, "terminal Leo release cannot execute another stage-local command");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

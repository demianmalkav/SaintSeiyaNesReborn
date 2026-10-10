using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;

internal static class AquariusStage08ContextChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckPhaseEntryAndInitialization();
        CheckFirstCamusTalkGateAndScriptedEnding();
        CheckFinalTalkAndHyogaUnlocks();
        CheckPostActionRules();
        CheckGoldSlotReachability();
        CheckReleaseOwnershipAndSuccessors();
    }

    private static void CheckPhaseEntryAndInitialization()
    {
        var redirected = AquariusStage08Context.EnterRedirectedFirstCamus(
            sourceStage050E: 0x02,
            activeSaint0533: AquariusStage08Context.HyogaIndex,
            sourceRelease0670: 0x02);

        Require(AquariusStage08Context.GetPhase(redirected) == AquariusPhase.RedirectedFirstCamus
            && redirected.StoryProgress067D == 0x02
            && redirected.Phase067C == 1
            && redirected.FirstEncounter06B8 == 0x0A
            && redirected.ScriptedBronzeBlock0690 == 0xFF,
            "stage-2 Hyoga special resume redirects to first Camus with $06B8=$0A and $0690=$FF");

        var setupMarked = AquariusStage08Context.MarkStage8BattleSetup(redirected);
        var reset = AquariusStage08Context.ApplyOrdinaryStage8Reset(setupMarked);
        Require(reset.FirstEncounter06B8 == 0
            && reset.Prelude06E1 == 1
            && reset.ScriptedBronzeBlock0690 == 0xFF,
            "ordinary stage-8 reset clears $06B8, preserves $06E1 and re-arms $0690");

        var progress02Init = AquariusStage08Context.ApplyInitialization(
            AquariusStage08Context.PrepareBattleRuntime(
                activeSaint0533: AquariusStage08Context.HyogaIndex,
                storyProgress067D: 0x02));
        Require(progress02Init.Outcome == AquariusInitOutcome.Progress02RestoreSeiyaWithoutUnlock
            && progress02Init.State.ActiveSaint0533 == AquariusStage08Context.SeiyaIndex
            && progress02Init.State.HyogaTechniqueCount0588 == 2
            && progress02Init.State.Release0670 == 0,
            "$9B14 progress-$02 branch restores Seiya without a technique unlock");

        var final = AquariusStage08Context.PrepareFinalCamus(prelude06E1: 1);
        Require(AquariusStage08Context.GetPhase(final) == AquariusPhase.FinalCamus
            && final.StoryProgress067D == 0x0A
            && final.FirstEncounter06B8 == 0,
            "story progress $0A naturally identifies final Camus with $06B8=0");

        var finalInit = AquariusStage08Context.ApplyInitialization(final);
        Require(finalInit.Outcome == AquariusInitOutcome.FinalCamusUnlockThirdTechniqueRelease03
            && finalInit.State.HyogaTechniqueCount0588 == 3
            && finalInit.State.ActiveTechniqueCount0696 == 3
            && finalInit.State.Release0670 == 0x03,
            "final Camus initialization performs Hyoga 2->3 unlock and release $03");
    }

    private static void CheckFirstCamusTalkGateAndScriptedEnding()
    {
        var state = AquariusStage08Context.EnterRedirectedFirstCamus(
            sourceStage050E: 0x02,
            activeSaint0533: AquariusStage08Context.HyogaIndex,
            sourceRelease0670: 0x02);

        var tooEarly = AquariusStage08Context.AfterBronzeAction(state, opponentConditionEb: 0x00);
        Require(tooEarly.Outcome == AquariusPostBronzeOutcome.FirstCamusAwaitThreeTalksAbortTurn
            && tooEarly.AbortOuterTurn
            && tooEarly.State.Release0670 == 0,
            "first Camus Bronze action before three Talks performs the special unwind");

        var talk1 = AquariusStage08Context.ExecuteTalk(state);
        var talk2 = AquariusStage08Context.ExecuteTalk(talk1.State);
        var talk3 = AquariusStage08Context.ExecuteTalk(talk2.State);

        Require(talk1.Outcome == AquariusTalkOutcome.FirstCamusChallengeRefusal
            && talk2.Outcome == AquariusTalkOutcome.FirstCamusPatriarchExchange
            && talk3.Outcome == AquariusTalkOutcome.FirstCamusFinalExchange
            && talk3.State.Conversation066F == 3
            && !talk1.ForceGoldCounterattack
            && !talk2.ForceGoldCounterattack
            && !talk3.ForceGoldCounterattack,
            "first Camus Talk is a three-step $066F script without forced Gold responses");

        var scripted = AquariusStage08Context.AfterBronzeAction(talk3.State, opponentConditionEb: 0x00);
        Require(scripted.Outcome == AquariusPostBronzeOutcome.FirstCamusScriptedFreezingReleaseFe
            && scripted.State.Release0670 == 0xFE
            && !scripted.AbortOuterTurn,
            "after three Talks the next Bronze action scripts Hyoga freezing through $FE");

        var defeat = AquariusStage08Context.AfterGoldResponse(state, playerConditionEa: 0xFF);
        Require(defeat.Outcome == AquariusPostGoldOutcome.FirstCamusScriptedFreezingReleaseFe
            && defeat.State.Release0670 == 0xFE,
            "actual Hyoga defeat in first Camus is converted to scripted freezing $FE, not $FF");

        var alive = AquariusStage08Context.AfterGoldResponse(state, playerConditionEa: 0x01);
        Require(alive.Outcome == AquariusPostGoldOutcome.FirstCamusContinue
            && alive.State.Release0670 == 0,
            "first Camus remains active while Hyoga is alive");
    }

    private static void CheckFinalTalkAndHyogaUnlocks()
    {
        var final = AquariusStage08Context.PrepareFinalCamus(
            prelude06E1: 0,
            hyogaTechniqueCount0588: 3,
            activeTechniqueCount0696: 3);

        var firstNoDodge = AquariusStage08Context.ExecuteTalk(final);
        Require(firstNoDodge.Outcome == AquariusTalkOutcome.FinalNoDodgeFirstDialogue
            && firstNoDodge.State.Prelude06E1 == 1
            && !firstNoDodge.ForceGoldCounterattack,
            "final Camus $06E1=0 permits exactly one no-dodge Talk without counterattack");

        var repeatNoDodge = AquariusStage08Context.ExecuteTalk(firstNoDodge.State);
        Require(repeatNoDodge.Outcome == AquariusTalkOutcome.FinalNoDodgeRepeatForcesCounterattack
            && repeatNoDodge.ForceGoldCounterattack,
            "final Camus repeated no-dodge Talk forces Gold response once $06E1!=0");

        var afterDodge = final with
        {
            Prelude06E1 = 1,
            DodgeSuccesses0678 = 1,
            ScriptedBronzeBlock0690 = 0xFF
        };
        var unlock = AquariusStage08Context.ExecuteTalk(afterDodge);
        Require(unlock.Outcome == AquariusTalkOutcome.FinalFirstPostDodgeTalkUnlocksFourthTechnique
            && unlock.State.Conversation066F == 1
            && unlock.State.HyogaTechniqueCount0588 == 4
            && unlock.State.ActiveTechniqueCount0696 == 4
            && unlock.State.ScriptedBronzeBlock0690 == 0
            && !unlock.ForceGoldCounterattack,
            "first post-dodge Hyoga Talk performs 3->4 unlock and clears $0690");

        var repeatPostDodge = AquariusStage08Context.ExecuteTalk(unlock.State);
        Require(repeatPostDodge.Outcome == AquariusTalkOutcome.FinalRepeatPostDodgeTalkForcesCounterattack
            && repeatPostDodge.ForceGoldCounterattack,
            "later post-dodge Talk forces Gold response once $066F!=0");

        var nonHyoga = afterDodge with { ActiveSaint0533 = AquariusStage08Context.SeiyaIndex };
        var noUnlock = AquariusStage08Context.ExecuteTalk(nonHyoga);
        Require(noUnlock.Outcome == AquariusTalkOutcome.FinalFirstPostDodgeTalk
            && noUnlock.State.HyogaTechniqueCount0588 == 3
            && noUnlock.State.ScriptedBronzeBlock0690 == 0xFF,
            "non-Hyoga first post-dodge Talk advances $066F but does not unlock or clear the block");
    }

    private static void CheckPostActionRules()
    {
        var final = AquariusStage08Context.PrepareFinalCamus(
            prelude06E1: 1,
            hyogaTechniqueCount0588: 4,
            activeTechniqueCount0696: 4) with
        {
            ScriptedBronzeBlock0690 = 0
        };

        var healthy = AquariusStage08Context.AfterBronzeAction(final, opponentConditionEb: 0x00);
        var low = AquariusStage08Context.AfterBronzeAction(final, opponentConditionEb: 0x01);
        var defeated = AquariusStage08Context.AfterBronzeAction(final, opponentConditionEb: 0xFF);

        Require(healthy.Outcome == AquariusPostBronzeOutcome.FinalCamusContinue
            && low.Outcome == AquariusPostBronzeOutcome.FinalCamusContinue,
            "final Camus post-Bronze treats both healthy and low opponent conditions as nonterminal");
        Require(defeated.Outcome == AquariusPostBronzeOutcome.FinalCamusVictoryReleaseFe
            && defeated.State.Release0670 == 0xFE,
            "final Camus defeated opponent exits through scripted $FE");

        var goldHealthy = AquariusStage08Context.AfterGoldResponse(final, playerConditionEa: 0x00);
        var goldLow = AquariusStage08Context.AfterGoldResponse(final, playerConditionEa: 0x01);
        var goldDefeat = AquariusStage08Context.AfterGoldResponse(final, playerConditionEa: 0xFF);

        Require(goldHealthy.Outcome == AquariusPostGoldOutcome.FinalCamusContinue,
            "final Camus healthy player condition continues");
        Require(goldLow.Outcome == AquariusPostGoldOutcome.FinalCamusLowFeedback
            && goldLow.State.Release0670 == 0,
            "final Camus low player condition is feedback-only, not defeat");
        Require(goldDefeat.Outcome == AquariusPostGoldOutcome.FinalCamusDefeatReleaseFf
            && goldDefeat.State.Release0670 == 0xFF,
            "final Camus actual player defeat uses ordinary $FF");
    }

    private static void CheckGoldSlotReachability()
    {
        var first = AquariusStage08Context.EnterRedirectedFirstCamus(
            sourceStage050E: 0x02,
            activeSaint0533: AquariusStage08Context.HyogaIndex,
            sourceRelease0670: 0x02);
        var firstSlot = AquariusStage08Context.SelectGoldAttack(first);
        Require(firstSlot.Slot0680 == 2
            && firstSlot.CosmoCoefficient == 30
            && firstSlot.LifeCoefficient == 20,
            "$06B8!=0 forces Aquarius Gold slot2 profile 30/20");

        var final = AquariusStage08Context.PrepareFinalCamus();
        var zeroAttempts = AquariusStage08Context.SelectGoldAttack(final);
        var oneAttempt = AquariusStage08Context.SelectGoldAttack(final with { DodgeFailures0677 = 1 });
        var twoAttempts = AquariusStage08Context.SelectGoldAttack(final with { DodgeFailures0677 = 1, DodgeSuccesses0678 = 1 });

        Require(zeroAttempts.Slot0680 == 1 && zeroAttempts.CosmoCoefficient == 21 && zeroAttempts.LifeCoefficient == 31,
            "final Camus with zero dodge attempts selects slot1 profile 21/31");
        Require(oneAttempt.Slot0680 == 1,
            "final Camus with one total dodge attempt still selects slot1");
        Require(twoAttempts.Slot0680 == 0 && twoAttempts.CosmoCoefficient == 30 && twoAttempts.LifeCoefficient == 20,
            "final Camus with two total dodge attempts selects slot0 profile 30/20");

        var reachable = new[]
        {
            firstSlot.Slot0680,
            zeroAttempts.Slot0680,
            oneAttempt.Slot0680,
            twoAttempts.Slot0680
        };
        Require(!reachable.Contains((byte)3),
            "Aquarius structural slot3 is unreachable from canonical selector branches");
    }

    private static void CheckReleaseOwnershipAndSuccessors()
    {
        var final = AquariusStage08Context.PrepareFinalCamus();
        var finalInit = AquariusStage08Context.ApplyInitialization(final);
        Require(AquariusStage08Context.ResolveReleaseOwner(finalInit.State) == AquariusReleaseOwner.IntroHandoff,
            "$03 belongs to the shared intro handoff");

        var first = AquariusStage08Context.EnterRedirectedFirstCamus(
            sourceStage050E: 0x02,
            activeSaint0533: AquariusStage08Context.HyogaIndex,
            sourceRelease0670: 0x02) with
        {
            Conversation066F = 3
        };
        var firstFe = AquariusStage08Context.AfterBronzeAction(first, opponentConditionEb: 0x00).State;
        Require(AquariusStage08Context.ResolveReleaseOwner(firstFe) == AquariusReleaseOwner.StageAdvanceForceSeiya,
            "first Camus $FE belongs to stage-advance/force-Seiya owner");
        var firstAdvance = AquariusStage08Context.ApplyStageAdvanceReleaseFe(firstFe);
        Require(firstAdvance.Path == AquariusAdvancePath.FirstCamusToCancerStage03
            && firstAdvance.ActiveSaint0533 == AquariusStage08Context.SeiyaIndex
            && firstAdvance.StoryProgress067D == 0x03
            && firstAdvance.NextStage050E == 0x03,
            "first Camus $FE resolves progress 2->3 and stage3 Cancer with Seiya");

        var finalFe = AquariusStage08Context.AfterBronzeAction(final, opponentConditionEb: 0xFF).State;
        var finalAdvance = AquariusStage08Context.ApplyStageAdvanceReleaseFe(finalFe);
        Require(finalAdvance.Path == AquariusAdvancePath.FinalCamusToPiscesStage09
            && finalAdvance.ActiveSaint0533 == AquariusStage08Context.SeiyaIndex
            && finalAdvance.StoryProgress067D == 0x0B
            && finalAdvance.NextStage050E == 0x09,
            "final Camus $FE resolves progress $0A->$0B and stage9 Pisces with Seiya");

        var defeat = AquariusStage08Context.AfterGoldResponse(final, playerConditionEa: 0xFF).State;
        Require(AquariusStage08Context.ResolveReleaseOwner(defeat) == AquariusReleaseOwner.GenericDefeat,
            "final Camus $FF belongs to generic defeat");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

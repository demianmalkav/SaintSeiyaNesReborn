using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;

internal static class SagaStage0AContextChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckCanonicalIngressAndPhase0Script();
        CheckPhase0GoldEscalationAndIkkiReentry();
        CheckIkkiTalkHitBlockAndSelectors();
        CheckIkkiToSeiyaReentryAndHitBlockPersistence();
        CheckFinalTalkSupportGateAndRollingCrashUnlock();
        CheckFinalOpponentLatchVictoryAndDefeatBoundaries();
        CheckStructuralPhase2InitIsUnreachable();
    }

    private static SagaStage0AState ReachIkki(SagaIngressVariant variant = SagaIngressVariant.Seiya)
    {
        var state = SagaStage0AContext.PrepareIngress(variant);
        state = SagaStage0AContext.AfterBronzeAction(state).State; // scripted miss / unwind
        state = SagaStage0AContext.ExecuteTalk(state).State;      // opens phase0 normal action path
        var action = SagaStage0AContext.AfterBronzeAction(state, playerConditionEa: 0x00);
        Require(action.GoldResponseReachable, "phase0 helper reaches Gold response only after second Bronze action");
        state = SagaStage0AContext.AfterGoldResponse(action.State, 0x01).State;
        return SagaStage0AContext.ResolvePhaseAdvance(state).State;
    }

    private static SagaStage0AState ReachFinalSeiya(bool useIkkiTalk = true)
    {
        var state = ReachIkki();
        if (useIkkiTalk)
            state = SagaStage0AContext.ExecuteTalk(state).State;

        var action = SagaStage0AContext.AfterBronzeAction(
            state,
            playerConditionEa: 0x00,
            selectedBronzeSlot0649: 0);
        Require(action.GoldResponseReachable, "phase1 helper reaches Gold response through a Bronze action");
        state = SagaStage0AContext.AfterGoldResponse(action.State, 0x01).State;
        return SagaStage0AContext.ResolvePhaseAdvance(state).State;
    }

    private static void CheckCanonicalIngressAndPhase0Script()
    {
        var seiya = SagaStage0AContext.PrepareIngress(SagaIngressVariant.Seiya);
        var shun = SagaStage0AContext.PrepareIngress(SagaIngressVariant.Shun);

        Require(seiya.ActiveSaint0533 == 0 && shun.ActiveSaint0533 == 2,
            "Saga preserves both final-special ingress Saints before the first phase transition");
        Require(seiya.StoryProgress067D == 0x0D && seiya.Stage050E == 0x0A
            && seiya.StoryRoster0673 == 0x3E && seiya.Phase06CE == 0,
            "Saga canonical ingress is story $0D / stage $0A / phase0");
        Require(seiya.Conversation066F == 0 && seiya.PhaseFlag06CF == 0
            && seiya.PhaseState06D0 == 0 && seiya.DodgeFailures0677 == 0
            && seiya.DodgeSuccesses0678 == 0,
            "initial Saga entry receives common transient reset");
        Require(SagaStage0AContext.IsBronzeConnectionScriptBlocked(seiya),
            "initial Saga phase re-arms scripted player-hit block $0690=$FF");

        var earlyTalk = SagaStage0AContext.ExecuteTalk(seiya);
        Require(earlyTalk.Outcome == SagaTalkOutcome.Phase0BeforeScriptedMiss
            && earlyTalk.State.Conversation066F == 0 && !earlyTalk.ForceGoldResponse,
            "phase0 Talk before scripted miss does not advance");

        var miss = SagaStage0AContext.AfterBronzeAction(seiya);
        Require(miss.Outcome == SagaPostBronzeOutcome.Phase0ScriptedMissUnwinds
            && miss.UnwindsOuterAction && !miss.GoldResponseReachable
            && miss.State.DodgeSuccesses0678 == 1,
            "phase0 first Bronze action increments $0678 and unwinds before Gold response");

        var transitionTalk = SagaStage0AContext.ExecuteTalk(miss.State);
        Require(transitionTalk.Outcome == SagaTalkOutcome.Phase0FirstPostMissTransition
            && transitionTalk.State.Conversation066F == 1
            && transitionTalk.State.PhaseFlag06CF == 1
            && transitionTalk.State.PhaseState06D0 == 1,
            "first phase0 Talk after miss advances $066F/$06CF/$06D0");

        var repeat = SagaStage0AContext.ExecuteTalk(transitionTalk.State);
        Require(repeat.Outcome == SagaTalkOutcome.Phase0RepeatDialogue
            && repeat.State == transitionTalk.State,
            "phase0 repeat Talk is state-stable");
    }

    private static void CheckPhase0GoldEscalationAndIkkiReentry()
    {
        var state = SagaStage0AContext.PrepareIngress(SagaIngressVariant.Shun);
        state = SagaStage0AContext.AfterBronzeAction(state).State;
        state = SagaStage0AContext.ExecuteTalk(state).State;

        var healthy = SagaStage0AContext.AfterBronzeAction(state, playerConditionEa: 0x00);
        var slot0 = SagaStage0AContext.SelectGoldAttack(healthy.State);
        Require(healthy.Outcome == SagaPostBronzeOutcome.Phase0Continue
            && healthy.GoldResponseReachable && slot0.Slot0680 == 0
            && slot0.CosmoCoefficient == 35 && slot0.LifeCoefficient == 23,
            "phase0 healthy state selects Gold slot0 35/23");

        var lowBeforeGold = SagaStage0AContext.AfterBronzeAction(state, playerConditionEa: 0x01);
        var slot1 = SagaStage0AContext.SelectGoldAttack(lowBeforeGold.State);
        Require(lowBeforeGold.State.PhaseState06D0 == 0xFF
            && slot1.Slot0680 == 1 && slot1.CosmoCoefficient == 30 && slot1.LifeCoefficient == 30,
            "phase0 low player condition marks $06D0=$FF and selects slot1 30/30");

        var released = SagaStage0AContext.AfterGoldResponse(healthy.State, 0x01);
        Require(released.Outcome == SagaPostGoldOutcome.Phase0ReleaseFf
            && released.State.Release0670 == 0xFF
            && SagaStage0AContext.ResolveReleaseOwner(released.State) == SagaReleaseOwner.PhaseAdvanceReentry,
            "phase0 low/dead Gold result emits reentry release $FF");

        var reentry = SagaStage0AContext.ResolvePhaseAdvance(released.State);
        Require(reentry.EnteredPhase == SagaPhase.Ikki && reentry.SeventhSenseReward == 0
            && reentry.SavedOutgoingSaintRecord
            && reentry.State.ActiveSaint0533 == 4 && reentry.State.Phase06CE == 1
            && reentry.State.StoryRoster0673 == 0x2E
            && reentry.State.Conversation066F == 0 && reentry.State.PhaseFlag06CF == 0
            && reentry.State.PhaseState06D0 == 0
            && reentry.State.ScriptedHitBlock0690 == 0xFF,
            "first $FF runs init phase0, saves inherited Saint and forces blocked Ikki phase");
    }

    private static void CheckIkkiTalkHitBlockAndSelectors()
    {
        var ikki = ReachIkki();
        Require(SagaStage0AContext.IsBronzeConnectionScriptBlocked(ikki),
            "Ikki enters phase1 with scripted hit block armed");

        var firstTalk = SagaStage0AContext.ExecuteTalk(ikki);
        Require(firstTalk.Outcome == SagaTalkOutcome.Phase1FirstTalkClearsHitBlock
            && firstTalk.State.Conversation066F == 1
            && firstTalk.State.ScriptedHitBlock0690 == 0,
            "Ikki first Talk clears $0690 and permits normal Bronze connection");
        Require(!SagaStage0AContext.IsBronzeConnectionScriptBlocked(firstTalk.State),
            "Ikki Talk removes story-level connection block");

        var slotZeroAction = SagaStage0AContext.AfterBronzeAction(
            firstTalk.State, playerConditionEa: 0x00, selectedBronzeSlot0649: 0).State;
        var gold2 = SagaStage0AContext.SelectGoldAttack(slotZeroAction);
        Require(gold2.Slot0680 == 2 && gold2.CosmoCoefficient == 60 && gold2.LifeCoefficient == 60,
            "phase1 Bronze slot0 selects Saga Gold slot2 60/60");

        var nonzeroAction = SagaStage0AContext.AfterBronzeAction(
            firstTalk.State, playerConditionEa: 0x00, selectedBronzeSlot0649: 1).State;
        var gold0 = SagaStage0AContext.SelectGoldAttack(nonzeroAction);
        Require(gold0.Slot0680 == 0 && gold0.CosmoCoefficient == 35 && gold0.LifeCoefficient == 23,
            "phase1 nonzero Bronze slot selects Saga Gold slot0 35/23");

        var low = SagaStage0AContext.AfterBronzeAction(
            firstTalk.State, playerConditionEa: 0x01, selectedBronzeSlot0649: 1).State;
        var gold3 = SagaStage0AContext.SelectGoldAttack(low);
        Require(low.PhaseState06D0 == 0xFF && gold3.Slot0680 == 3
            && gold3.CosmoCoefficient == 60 && gold3.LifeCoefficient == 60,
            "phase1 $06D0=$FF overrides Bronze slot and selects Gold slot3 60/60");

        var repeatTalk = SagaStage0AContext.ExecuteTalk(firstTalk.State);
        Require(repeatTalk.Outcome == SagaTalkOutcome.Phase1RepeatDialogue
            && repeatTalk.State.ScriptedHitBlock0690 == 0,
            "phase1 repeat Talk does not re-arm scripted hit block");
    }

    private static void CheckIkkiToSeiyaReentryAndHitBlockPersistence()
    {
        var final = ReachFinalSeiya(useIkkiTalk: true);
        Require(final.ActiveSaint0533 == 0 && final.Phase06CE == 2
            && final.Conversation066F == 0 && final.PhaseFlag06CF == 0
            && final.PhaseState06D0 == 0 && final.LowOpponentLatch064D == 0
            && final.AttackWeakening0681 == 0 && final.ScriptedHitBlock0690 == 0,
            "second $FF returns to Seiya phase2 and preserves Ikki Talk hit-block clear");

        var ikki = ReachIkki();
        var action = SagaStage0AContext.AfterBronzeAction(
            ikki, playerConditionEa: 0x00, selectedBronzeSlot0649: 0);
        var release = SagaStage0AContext.AfterGoldResponse(action.State, 0x01).State;
        var blockedFinalResult = SagaStage0AContext.ResolvePhaseAdvance(release);
        Require(blockedFinalResult.EnteredPhase == SagaPhase.SeiyaFinal
            && blockedFinalResult.SeventhSenseReward == 1000,
            "Ikki->Seiya init itself grants +1000 Seventh Sense");
        var blockedFinal = blockedFinalResult.State;
        Require(blockedFinal.ActiveSaint0533 == 0 && blockedFinal.Phase06CE == 2
            && blockedFinal.ScriptedHitBlock0690 == 0xFF
            && SagaStage0AContext.IsBronzeConnectionScriptBlocked(blockedFinal),
            "skipping Ikki Talk carries $0690=$FF into Seiya final phase");
    }

    private static void CheckFinalTalkSupportGateAndRollingCrashUnlock()
    {
        var state = ReachFinalSeiya();

        var earlyEscape = SagaStage0AContext.ExecuteEscape(SagaStage0AContext.PrepareIngress(SagaIngressVariant.Seiya));
        Require(earlyEscape.Outcome == SagaEscapeOutcome.EarlyPhaseBlocked
            && earlyEscape.MessageId == 0xE1 && !earlyEscape.OpensSupportOverlay,
            "Saga Escape is blocked with message $E1 before final phase");

        var consumed = SagaStage0AContext.ExecuteEscape(state);
        Require(consumed.Outcome == SagaEscapeOutcome.FinalPhaseGateConsumedBeforeTalk
            && consumed.State.PhaseState06D0 == 1 && consumed.State.PhaseFlag06CF == 1
            && !consumed.OpensSupportOverlay,
            "final Escape before Talk consumes one-shot support gate without overlay");
        var afterTooLateTalk = SagaStage0AContext.ExecuteTalk(consumed.State).State;
        var cannotReopen = SagaStage0AContext.ExecuteEscape(afterTooLateTalk);
        Require(cannotReopen.Outcome == SagaEscapeOutcome.FinalPhaseAlreadyConsumed
            && !cannotReopen.OpensSupportOverlay,
            "support overlay is missable after consuming final Escape gate before Talk");

        var finalTalk = SagaStage0AContext.ExecuteTalk(state);
        Require(finalTalk.Outcome == SagaTalkOutcome.Phase2FirstFinalTalk
            && finalTalk.State.Conversation066F == 1,
            "first final-phase Talk advances $066F");
        var overlay = SagaStage0AContext.ExecuteEscape(finalTalk.State);
        Require(overlay.Outcome == SagaEscapeOutcome.FinalSupportOverlayOpened
            && overlay.OpensSupportOverlay
            && overlay.State.PhaseState06D0 == 1
            && overlay.State.PhaseFlag06CF == 1
            && overlay.State.SupportMode068F == 0x55
            && overlay.State.SupportRewardBits06D4 == 0,
            "Talk then first final Escape opens $068F=$55 support overlay");

        var firstSupport = SagaStage0AContext.ConfirmSupportSelection(overlay.State, 0x02);
        Require(firstSupport.Outcome == SagaSupportSelectionOutcome.NewSupportRewardAndRollingCrashUnlock
            && firstSupport.SeventhSenseReward == 1000 && firstSupport.RollingCrashAvailable
            && firstSupport.State.SupportRewardBits06D4 == 0x02
            && firstSupport.State.SeiyaTechniqueCount0587 == 3
            && firstSupport.State.ActiveTechniqueCount0696 == 3,
            "first new support grants +1000 and exact Rolling Crash count3 unlock");

        var repeat = SagaStage0AContext.ConfirmSupportSelection(firstSupport.State, 0x02);
        Require(repeat.Outcome == SagaSupportSelectionOutcome.RepeatSupportNoReward
            && repeat.SeventhSenseReward == 0 && repeat.State.SupportRewardBits06D4 == 0x02,
            "same support bit cannot repeat reward");

        var secondUnique = SagaStage0AContext.ConfirmSupportSelection(firstSupport.State, 0x10);
        Require(secondUnique.SeventhSenseReward == 1000
            && secondUnique.State.SupportRewardBits06D4 == 0x12,
            "$06D4 is a per-support reward bitset inside one final overlay");
    }

    private static void CheckFinalOpponentLatchVictoryAndDefeatBoundaries()
    {
        var state = ReachFinalSeiya();
        var selector = SagaStage0AContext.SelectGoldAttack(state);
        Require(selector.Slot0680 == 3 && selector.CosmoCoefficient == 60 && selector.LifeCoefficient == 60,
            "final Saga phase always selects Gold slot3 60/60");

        var low = SagaStage0AContext.AfterBronzeAction(state, opponentConditionEb: 0x01);
        Require(low.Outcome == SagaPostBronzeOutcome.Phase2FirstLowOpponentEvent
            && low.State.LowOpponentLatch064D == 1,
            "first low-Saga condition latches $064D once");
        var lowAgain = SagaStage0AContext.AfterBronzeAction(low.State, opponentConditionEb: 0x01);
        Require(lowAgain.Outcome == SagaPostBronzeOutcome.Phase2Continue
            && lowAgain.State.LowOpponentLatch064D == 1,
            "repeat low-Saga condition does not replay first-low event");

        var lowPlayer = SagaStage0AContext.AfterGoldResponse(state, 0x01);
        Require(lowPlayer.Outcome == SagaPostGoldOutcome.Phase2LowPlayerFeedback
            && lowPlayer.State.Release0670 == 0,
            "final-phase low player condition is nonterminal feedback");

        var victory = SagaStage0AContext.AfterBronzeAction(state, opponentConditionEb: 0xFF);
        Require(victory.Outcome == SagaPostBronzeOutcome.Phase2VictoryRelease01
            && victory.State.Phase06CE == 0 && victory.State.Release0670 == 0x01
            && SagaStage0AContext.ResolveReleaseOwner(victory.State) == SagaReleaseOwner.StoryVictory,
            "Saga opponent defeat resets phase and emits story victory $01");
        var winBoundary = SagaStage0AContext.ResolveVictoryBoundary(victory.State);
        Require(winBoundary.ActiveSaint0533 == 0
            && winBoundary.StoryProgress067D == 0x0E
            && winBoundary.NextStage050E == 0x00
            && winBoundary.ProgressDescriptor06CD == 0x00
            && winBoundary.StoryRoster0673 == 0x30
            && winBoundary.Release0670 == 0x05
            && winBoundary.EngineBootstrapState00 == 0x00
            && winBoundary.ImmediateEngineSuccessor == 0x20,
            "Saga victory exits boss ownership at progress $0E / release $05 / bootstrap $00->$20");

        var finalAction = SagaStage0AContext.AfterBronzeAction(state, opponentConditionEb: 0x00);
        Require(finalAction.GoldResponseReachable, "final defeat fixture reaches Gold response through Bronze action");
        var defeat = SagaStage0AContext.AfterGoldResponse(finalAction.State, 0xFF);
        Require(defeat.Outcome == SagaPostGoldOutcome.Phase2DefeatReleaseDd
            && defeat.State.Phase06CE == 0 && defeat.State.Release0670 == 0xDD
            && SagaStage0AContext.ResolveReleaseOwner(defeat.State) == SagaReleaseOwner.FinalDefeat,
            "final Seiya defeat resets phase and emits special $DD");
        var lossBoundary = SagaStage0AContext.ResolveFinalDefeatBoundary(defeat.State);
        Require(lossBoundary.StoryProgress067D == 0x0D
            && lossBoundary.Stage050E == 0x0A
            && lossBoundary.StoryRoster0673 == 0x3F
            && lossBoundary.SupportMode068F == 0xDD
            && lossBoundary.EngineBootstrapState00 == 0x90
            && lossBoundary.ImmediateEngineSuccessor == 0x91,
            "Saga final defeat exits through $DD overlay and bootstrap $90->$91");
    }

    private static void CheckStructuralPhase2InitIsUnreachable()
    {
        Require(!SagaStage0AContext.IsPhase2InitializationReachableFromCanonicalGraph(),
            "init phase2 $9C2C is structural only because final phase never emits $FF");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

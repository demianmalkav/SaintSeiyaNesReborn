using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;

internal static class FinalSpecialStage0CContextChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckExactPiscesEntriesAndInitBypass();
        CheckTalkIsSharedAndRewardIsOneShot();
        CheckRoseClearAttackOwnsTheAdvance();
        CheckEscapeAndResourceCommandsCannotAdvance();
        CheckSagaHandoffPreservesActiveSaint();
        CheckUnsupportedStatesAreRejected();
    }

    private static void CheckExactPiscesEntriesAndInitBypass()
    {
        var seiya = FinalSpecialStage0CContext.PrepareFromPisces(
            FinalSpecialEntryVariant.SeiyaAfterShunPiscesVictory);
        var shun = FinalSpecialStage0CContext.PrepareFromPisces(
            FinalSpecialEntryVariant.ShunAfterSeiyaPiscesVictory);

        Require(seiya.ActiveSaint0533 == 0
            && seiya.StoryProgress067D == 0x0C
            && seiya.Stage050E == 0x0C
            && seiya.ProgressDescriptor06CD == 0x0E
            && seiya.StoryRoster0673 == 0x3E,
            "Shun Pisces victory enters final-special $0C with Seiya/$0E/$3E");

        Require(shun.ActiveSaint0533 == 2
            && shun.StoryProgress067D == 0x0C
            && shun.Stage050E == 0x0C
            && shun.ProgressDescriptor06CD == 0x0B
            && shun.StoryRoster0673 == 0x3B,
            "Seiya Pisces victory enters final-special $0C with Shun/$0B/$3B");

        Require(FinalSpecialStage0CContext.ClassifyEntry(seiya)
                == FinalSpecialEntryVariant.SeiyaAfterShunPiscesVictory,
            "Seiya final-special variant classifies from exact Pisces state");
        Require(FinalSpecialStage0CContext.ClassifyEntry(shun)
                == FinalSpecialEntryVariant.ShunAfterSeiyaPiscesVictory,
            "Shun final-special variant classifies from exact Pisces state");

        Require(FinalSpecialStage0CContext.RawInitializationOverrunPointer == 0x8D00,
            "stage-12 init table overrun decodes structurally as $8D00");
        Require(!FinalSpecialStage0CContext.IsInitializationDispatcherReachable(seiya)
            && !FinalSpecialStage0CContext.IsInitializationDispatcherReachable(shun),
            "canonical final-special entries bypass the malformed init dispatcher slot");

        var reach = FinalSpecialStage0CContext.GetSubsystemReachability(seiya);
        Require(!reach.InitializationDispatcher
            && !reach.GenericOpponentDamage
            && !reach.PostBronzeDispatcher
            && !reach.GoldResponseOrDodge
            && !reach.PostGoldDispatcher
            && !reach.ResourceAllocation,
            "final-special stage excludes ordinary boss and resource-allocation subsystems");
    }

    private static void CheckTalkIsSharedAndRewardIsOneShot()
    {
        foreach (var variant in Enum.GetValues<FinalSpecialEntryVariant>())
        {
            var state = FinalSpecialStage0CContext.PrepareFromPisces(variant);
            var first = FinalSpecialStage0CContext.ExecuteTalk(state);

            Require(first.Outcome == FinalSpecialTalkOutcome.FirstMarinTalkReward
                && first.State.Conversation066F == 1
                && first.State.TransientDc == 0
                && first.State.Release0670 == 0
                && first.SeventhSenseReward == 1000
                && first.FirstMessageId == 0xD3
                && first.SecondMessageId == 0xD5,
                "first final-special Talk is shared D3/D5, grants +1000 and advances only $066F");

            var repeat = FinalSpecialStage0CContext.ExecuteTalk(first.State);
            Require(repeat.Outcome == FinalSpecialTalkOutcome.RepeatMarinTalk
                && repeat.State.Conversation066F == 1
                && repeat.State.TransientDc == 0
                && repeat.State.Release0670 == 0
                && repeat.SeventhSenseReward == 0
                && repeat.FirstMessageId == 0xD3
                && repeat.SecondMessageId == 0xD5,
                "repeat final-special Talk replays shared dialogue without reward or release");
        }
    }

    private static void CheckRoseClearAttackOwnsTheAdvance()
    {
        var state = FinalSpecialStage0CContext.PrepareFromPisces(
            FinalSpecialEntryVariant.SeiyaAfterShunPiscesVictory);
        var attack = FinalSpecialStage0CContext.ExecuteAttack(state);

        Require(attack.Outcome == FinalSpecialAttackOutcome.RoseClearEffectRelease01
            && attack.TemporaryEffectStage050E == 0x12
            && attack.EffectIterations == 0x40
            && !attack.TechniqueSelectionCanCancel,
            "stage-$0C Attack forces selection and runs the 64-step temporary stage-$12 rose effect");

        Require(attack.State.Stage050E == 0x0C
            && attack.State.EffectState0632 == 0x1A
            && attack.State.BronzeHitToken06BC == 0
            && attack.State.Release0670 == 0x01,
            "rose effect restores stage $0C, ends $0632=$1A and emits release $01");

        Require(!attack.UsesGenericOpponentDamage
            && !attack.UsesPostBronzeDispatcher
            && !attack.UsesGoldResponseOrDodge
            && !attack.UsesPostGoldDispatcher,
            "stage-$0C Attack bypasses ordinary opponent damage and every boss-response dispatcher");

        Require(FinalSpecialStage0CContext.ResolveReleaseOwner(attack.State)
                == FinalSpecialReleaseOwner.StoryAdvanceToSaga,
            "release $01 is owned by fixed story advance");
    }

    private static void CheckEscapeAndResourceCommandsCannotAdvance()
    {
        var state = FinalSpecialStage0CContext.PrepareFromPisces(
            FinalSpecialEntryVariant.ShunAfterSeiyaPiscesVictory);

        var escape = FinalSpecialStage0CContext.ExecuteEscape(state);
        Require(escape.Outcome == FinalSpecialPassiveCommandOutcome.EscapeBlockedDialogue
            && escape.MessageId == 0xD4
            && escape.State.Release0670 == 0
            && !escape.OpensResourceAllocation,
            "stage-$0C Escape is intercepted by D4 and cannot advance");

        var resource = FinalSpecialStage0CContext.ExecuteResourceAllocation(state);
        Require(resource.Outcome == FinalSpecialPassiveCommandOutcome.ResourceAllocationSuppressed
            && resource.MessageId is null
            && resource.State.Release0670 == 0
            && !resource.OpensResourceAllocation,
            "stage-$0C resource-allocation command is redraw-only/suppressed");

        Require(FinalSpecialStage0CContext.ResolveReleaseOwner(state)
                == FinalSpecialReleaseOwner.ActiveSpecialContext,
            "no-op special commands leave the context active");
    }

    private static void CheckSagaHandoffPreservesActiveSaint()
    {
        var seiya = FinalSpecialStage0CContext.PrepareFromPisces(
            FinalSpecialEntryVariant.SeiyaAfterShunPiscesVictory);
        var seiyaAttack = FinalSpecialStage0CContext.ExecuteAttack(seiya);
        var afterSeiya = FinalSpecialStage0CContext.AdvanceAfterRoseClear(seiyaAttack.State);
        Require(afterSeiya.ActiveSaint0533 == 0
            && afterSeiya.StoryProgress067D == 0x0D
            && afterSeiya.NextStage050E == 0x0A
            && afterSeiya.ProgressDescriptor06CD == 0x0E
            && afterSeiya.StoryRoster0673 == 0x3E,
            "Seiya stage-$0C route advances exactly to progress $0D / Saga stage $0A while preserving Seiya");

        var shun = FinalSpecialStage0CContext.PrepareFromPisces(
            FinalSpecialEntryVariant.ShunAfterSeiyaPiscesVictory);
        var shunAttack = FinalSpecialStage0CContext.ExecuteAttack(shun);
        var afterShun = FinalSpecialStage0CContext.AdvanceAfterRoseClear(shunAttack.State);
        Require(afterShun.ActiveSaint0533 == 2
            && afterShun.StoryProgress067D == 0x0D
            && afterShun.NextStage050E == 0x0A
            && afterShun.ProgressDescriptor06CD == 0x0E
            && afterShun.StoryRoster0673 == 0x3E,
            "Shun stage-$0C route advances exactly to progress $0D / Saga stage $0A while preserving Shun");
    }

    private static void CheckUnsupportedStatesAreRejected()
    {
        var seiya = FinalSpecialStage0CContext.PrepareFromPisces(
            FinalSpecialEntryVariant.SeiyaAfterShunPiscesVictory);

        Throws(() => FinalSpecialStage0CContext.AdvanceAfterRoseClear(seiya),
            "final-special cannot advance without rose-clear release $01");

        Throws(() => FinalSpecialStage0CContext.ExecuteTalk(seiya with { ActiveSaint0533 = 1 }),
            "non-Pisces-derived active Saint is rejected");

        Throws(() => FinalSpecialStage0CContext.ResolveReleaseOwner(seiya with { Release0670 = 0xFE }),
            "unreachable boss-style release is rejected in final-special stage");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void Throws(Action action, string message)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException)
        {
            return;
        }

        throw new InvalidOperationException($"{message}: expected InvalidOperationException");
    }
}
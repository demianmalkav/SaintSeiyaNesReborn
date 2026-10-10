using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;

internal static class GeminiStage02ContextChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckCanonicalSeedAndInitialization();
        CheckTalkAlwaysForcesCounterattack();
        CheckMandatoryFirstBronzeDetourPrecedesOpponentClassification();
        CheckOrdinaryPlatformResumePreservesConversation();
        CheckHyogaPlatformResumeRedirectsToFirstCamus();
        CheckOrdinaryPostDetourBranchesAndCancerBoundary();
        CheckPostGoldAndRetryRearmDetour();
        CheckFirstCamusCanonicalCancerTerminals();
        CheckGoldSelectorCoverageReuse();
    }

    private static void CheckCanonicalSeedAndInitialization()
    {
        for (byte saint = 0; saint <= 3; saint++)
        {
            var entry = GeminiStage02Context.CreateCanonicalEntry(saint);
            Require(entry.ActiveSaint0533 == saint, $"stage 02 entry preserves reachable Saint {saint}");
            Require(entry.StoryProgress067D == 0x02 && entry.Stage050E == 0x02,
                "stage 02 canonical seed is progress/stage 02");
            Require(entry.Phase067C == 0 && entry.Conversation066F == 0
                && entry.Release0670 == 0 && entry.IntroDone068E == 0,
                "common reset seeds stage-2 local fields to zero");

            var init = GeminiStage02Context.ApplyInitialization(entry);
            Require(init.TemporaryPresentationStage == 0x11,
                "stage 02 init temporarily loads presentation index 11");
            Require(init.SeventhSenseReward == 300,
                "stage 02 init grants +300 Seventh Sense through packed BCD 03");
            Require(init.State.Release0670 == 0x03 && init.State.IntroDone068E == 1,
                "shared intro handoff writes release 03 and intro flag 1");
            Require(init.State.Stage050E == 0x02 && init.State.Phase067C == 0,
                "stage 02 init restores stage 02 and does not advance phase");

            var loop = GeminiStage02Context.EnterCommandLoop(init.State);
            Require(loop.Release0670 == 0 && loop.IntroDone068E == 1,
                "intro release 03 is consumed without resetting intro state");
        }

        Require(!GeminiStage02Context.IsReachableEntrySaint(0x04),
            "Ikki is excluded from progress-02 entry by story marker 30");
    }

    private static void CheckTalkAlwaysForcesCounterattack()
    {
        var state = ActiveEntry(GeminiStage02Context.ShunIndex);
        var first = GeminiStage02Context.ExecuteTalk(state);
        Require(first.Outcome == GeminiStage02TalkOutcome.FirstTalkForcesCounterattack,
            "first stage-2 Talk takes the one-time branch");
        Require(first.State.Conversation066F == 1 && first.ForceGoldCounterattack,
            "first Talk writes 066F=1 and forces Gold response through DC");

        var repeat = GeminiStage02Context.ExecuteTalk(first.State);
        Require(repeat.Outcome == GeminiStage02TalkOutcome.RepeatedTalkForcesCounterattack,
            "repeated stage-2 Talk uses repeat branch");
        Require(repeat.State.Conversation066F == 1 && repeat.ForceGoldCounterattack,
            "repeat Talk preserves nonzero 066F and still forces Gold response");
    }

    private static void CheckMandatoryFirstBronzeDetourPrecedesOpponentClassification()
    {
        var state = ActiveEntry(GeminiStage02Context.SeiyaIndex);

        var hypotheticalKill = GeminiStage02Context.AfterBronzeAction(
            state,
            opponentConditionEb: 0xFF,
            bronzeAttackHit: true);

        Require(hypotheticalKill.Outcome == GeminiStage02PostBronzeOutcome.MandatoryPlatformDetourRelease02,
            "phase-0 first Bronze action detours before any opponent-condition victory branch");
        Require(hypotheticalKill.RequiresPlatformDetour
            && hypotheticalKill.State.Release0670 == 0x02
            && hypotheticalKill.State.Phase067C == 0,
            "phase-0 detour emits release 02 while phase remains zero until platform exit");
    }

    private static void CheckOrdinaryPlatformResumePreservesConversation()
    {
        var state = ActiveEntry(GeminiStage02Context.ShunIndex);
        var talked = GeminiStage02Context.ExecuteTalk(state).State;
        Require(talked.Conversation066F == 1, "fixture enters detour after first Talk");

        var detour = GeminiStage02Context.AfterBronzeAction(talked, 0x00, true).State;

        Require(GeminiStage02Context.ResolvePlatformDetourExit(detour, 0xB3, 0x80, 0) is null,
            "platform 0E gate rejects X below B4");
        Require(GeminiStage02Context.ResolvePlatformDetourExit(detour, 0xB4, 0x70, 0) is null,
            "platform 0E gate rejects wrong Y");
        Require(GeminiStage02Context.ResolvePlatformDetourExit(detour, 0xB4, 0x80, 1) is null,
            "platform 0E gate rejects nonzero jump phase");

        var resume = GeminiStage02Context.ResolvePlatformDetourExit(detour, 0xB4, 0x80, 0);
        Require(resume.HasValue, "canonical platform 0E exit gate accepts B4/80/phase0");
        var result = resume!.Value;
        Require(result.Kind == GeminiStage02PlatformResumeKind.OrdinaryStage02Continuation,
            "non-Hyoga platform 0E exit returns to ordinary stage 02");
        Require(result.GeminiState.HasValue && !result.FirstCamusState.HasValue,
            "ordinary resume returns Gemini state only");

        var resumed = result.GeminiState!.Value;
        Require(resumed.Stage050E == 0x02 && resumed.Phase067C == 1 && resumed.Release0670 == 0,
            "special resume increments 067C and consumes release without A973 reset");
        Require(resumed.Conversation066F == 1,
            "release-02 platform resume preserves pre-detour Talk progress 066F");
        Require(result.PlatformExit.Field067C == 1 && result.PlatformExit.ReloadField050E == 0x02,
            "compositor reuses the closed platform pipeline phase/stage result");
    }

    private static void CheckHyogaPlatformResumeRedirectsToFirstCamus()
    {
        var state = ActiveEntry(GeminiStage02Context.HyogaIndex);
        var detour = GeminiStage02Context.AfterBronzeAction(state, 0x00, true).State;
        var resume = GeminiStage02Context.ResolvePlatformDetourExit(detour, 0xB4, 0x80, 0);

        Require(resume.HasValue, "Hyoga can leave platform 0E through canonical gate");
        var result = resume!.Value;
        Require(result.Kind == GeminiStage02PlatformResumeKind.RedirectedFirstCamus,
            "Hyoga platform 0E exit redirects to first Camus");
        Require(!result.GeminiState.HasValue && result.FirstCamusState.HasValue,
            "Hyoga resume leaves Gemini state and returns Aquarius first-Camus state");
        Require(result.PlatformExit.ReloadField050E == 0x08
            && result.PlatformExit.Field067C == 1
            && result.PlatformExit.Field06B8 == 0x0A,
            "closed platform pipeline reports Hyoga 08/phase1/06B8=0A redirect");

        var camus = result.FirstCamusState!.Value;
        Require(AquariusStage08Context.GetPhase(camus) == AquariusPhase.RedirectedFirstCamus,
            "composed Hyoga state is the existing redirected first-Camus phase");
        Require(camus.ActiveSaint0533 == 0x01
            && camus.StoryProgress067D == 0x02
            && camus.Phase067C == 1
            && camus.FirstEncounter06B8 == 0x0A
            && camus.ScriptedBronzeBlock0690 == 0xFF,
            "existing Aquarius entry owns exact first-Camus redirect state");
    }

    private static void CheckOrdinaryPostDetourBranchesAndCancerBoundary()
    {
        var state = OrdinaryResumed(GeminiStage02Context.ShiryuIndex);

        var hit = GeminiStage02Context.AfterBronzeAction(state, 0x00, true);
        Require(hit.Outcome == GeminiStage02PostBronzeOutcome.ContinueAfterHit
            && hit.State.Release0670 == 0,
            "phase-1 healthy hit continues ordinary stage 02");

        var miss = GeminiStage02Context.AfterBronzeAction(state, 0x00, false);
        Require(miss.Outcome == GeminiStage02PostBronzeOutcome.MissFeedback,
            "phase-1 healthy miss takes stage-2 miss feedback");

        var low = GeminiStage02Context.AfterBronzeAction(state, 0x01, true);
        Require(low.Outcome == GeminiStage02PostBronzeOutcome.OpponentLowFeedback,
            "phase-1 low opponent takes stage-2 low feedback");

        var win = GeminiStage02Context.AfterBronzeAction(state, 0xFF, true);
        Require(win.Outcome == GeminiStage02PostBronzeOutcome.VictoryRelease01
            && win.State.Release0670 == 0x01,
            "phase-1 defeated opponent emits ordinary victory release 01");

        var boundary = GeminiStage02Context.ResolveOrdinaryCancerBoundary(win.State);
        Require(boundary.Path == GeminiStage02CancerPath.OrdinaryStage02Victory,
            "ordinary stage-2 win is labeled ordinary Cancer path");
        Require(boundary.ActiveSaint0533 == GeminiStage02Context.ShiryuIndex,
            "ordinary release 01 preserves reachable non-Ikki active Saint");
        Require(boundary.StoryProgress067D == 0x03
            && boundary.Stage050E == 0x03
            && boundary.StoryDescriptor06CD == 0x02
            && boundary.StoryMarker0673 == 0x32,
            "ordinary stage-2 victory resolves exact Cancer boundary");
    }

    private static void CheckPostGoldAndRetryRearmDetour()
    {
        var phase0 = ActiveEntry(GeminiStage02Context.SeiyaIndex);
        var healthy = GeminiStage02Context.AfterGoldResponse(phase0, 0x00);
        Require(healthy.Outcome == GeminiStage02PostGoldOutcome.HealthyFeedback
            && healthy.State.Release0670 == 0,
            "healthy stage-2 Gold response is nonterminal");

        var low = GeminiStage02Context.AfterGoldResponse(phase0, 0x01);
        Require(low.Outcome == GeminiStage02PostGoldOutcome.LowFeedback
            && low.State.Release0670 == 0,
            "low-player stage-2 Gold response is nonterminal");

        var defeated = GeminiStage02Context.AfterGoldResponse(phase0, 0xFF);
        Require(defeated.Outcome == GeminiStage02PostGoldOutcome.DefeatReleaseFF
            && defeated.State.Release0670 == 0xFF,
            "player defeat emits generic release FF");

        var retry = GeminiStage02Context.ResetForRetryAfterDefeat(defeated.State);
        Require(retry.Phase067C == 0 && retry.Conversation066F == 0
            && retry.Release0670 == 0 && retry.IntroDone068E == 0,
            "FF retry re-enters A973 reset and clears stage-2 local phase/Talk/intro state");

        var retryInit = GeminiStage02Context.ApplyInitialization(retry);
        Require(retryInit.SeventhSenseReward == 300,
            "retry re-enters the stage initializer and repeats its +300 intro reward contract");
        var retryLoop = GeminiStage02Context.EnterCommandLoop(retryInit.State);
        var firstRetryBronze = GeminiStage02Context.AfterBronzeAction(retryLoop, 0xFF, true);
        Require(firstRetryBronze.Outcome == GeminiStage02PostBronzeOutcome.MandatoryPlatformDetourRelease02,
            "retry phase zero re-arms mandatory platform 0E detour");
    }

    private static void CheckFirstCamusCanonicalCancerTerminals()
    {
        var firstCamus = RedirectedFirstCamus();

        var talk1 = AquariusStage08Context.ExecuteTalk(firstCamus).State;
        var talk2 = AquariusStage08Context.ExecuteTalk(talk1).State;
        var talk3 = AquariusStage08Context.ExecuteTalk(talk2).State;
        Require(talk3.Conversation066F == 3,
            "existing first-Camus model reaches three-Talk scripted threshold");

        var scripted = AquariusStage08Context.AfterBronzeAction(talk3, 0x00);
        Require(scripted.Outcome == AquariusPostBronzeOutcome.FirstCamusScriptedFreezingReleaseFe
            && scripted.State.Release0670 == 0xFE,
            "three-Talk first-Camus Bronze route emits release FE");

        var bronzeBoundary = GeminiStage02Context.ResolveFirstCamusCancerBoundary(scripted.State);
        Require(bronzeBoundary.Path == GeminiStage02CancerPath.RedirectedFirstCamusReleaseFE
            && bronzeBoundary.ActiveSaint0533 == 0x00,
            "first-Camus FE owner forces Seiya for Cancer boundary");
        Require(bronzeBoundary.StoryProgress067D == 0x03
            && bronzeBoundary.Stage050E == 0x03
            && bronzeBoundary.StoryDescriptor06CD == 0x02
            && bronzeBoundary.StoryMarker0673 == 0x32,
            "scripted first-Camus completion reaches exact Cancer boundary");

        var defeatedByCamus = AquariusStage08Context.AfterGoldResponse(firstCamus, 0xFF);
        Require(defeatedByCamus.Outcome == AquariusPostGoldOutcome.FirstCamusScriptedFreezingReleaseFe
            && defeatedByCamus.State.Release0670 == 0xFE,
            "first-Camus player defeat is scripted FE progression rather than generic FF defeat");
        var defeatBoundary = GeminiStage02Context.ResolveFirstCamusCancerBoundary(defeatedByCamus.State);
        Require(defeatBoundary.StoryProgress067D == 0x03 && defeatBoundary.Stage050E == 0x03,
            "first-Camus defeat-script path converges on Cancer too");
    }

    private static void CheckGoldSelectorCoverageReuse()
    {
        var coverage = BattleStageContextCoverage.Get(0x02);
        Require(coverage.GoldSelectorKind == BattleStageGoldSelectorKind.GenericParitySlots01,
            "stage 02 reuses generic parity Gold selector");
        Require(coverage.ReachableGoldSlotMask == 0b0000_0011,
            "stage 02 canonical Gold slots remain exactly 0 and 1");
    }

    private static GeminiStage02State ActiveEntry(byte saint)
    {
        var entry = GeminiStage02Context.CreateCanonicalEntry(saint);
        var init = GeminiStage02Context.ApplyInitialization(entry);
        return GeminiStage02Context.EnterCommandLoop(init.State);
    }

    private static GeminiStage02State OrdinaryResumed(byte saint)
    {
        if (saint == GeminiStage02Context.HyogaIndex)
            throw new InvalidOperationException("Hyoga does not canonically resume ordinary stage 02 after platform 0E.");

        var state = ActiveEntry(saint);
        var detour = GeminiStage02Context.AfterBronzeAction(state, 0x00, true).State;
        var resume = GeminiStage02Context.ResolvePlatformDetourExit(detour, 0xB4, 0x80, 0);
        Require(resume.HasValue && resume.Value.GeminiState.HasValue,
            "ordinary resume fixture must return stage-2 state");
        return resume!.Value.GeminiState!.Value;
    }

    private static AquariusStage08State RedirectedFirstCamus()
    {
        var state = ActiveEntry(GeminiStage02Context.HyogaIndex);
        var detour = GeminiStage02Context.AfterBronzeAction(state, 0x00, true).State;
        var resume = GeminiStage02Context.ResolvePlatformDetourExit(detour, 0xB4, 0x80, 0);
        Require(resume.HasValue && resume.Value.FirstCamusState.HasValue,
            "Hyoga redirect fixture must return first-Camus state");
        return resume!.Value.FirstCamusState!.Value;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

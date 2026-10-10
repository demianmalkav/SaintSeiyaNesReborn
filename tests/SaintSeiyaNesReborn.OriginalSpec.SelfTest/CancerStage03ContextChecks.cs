using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;

internal static class CancerStage03ContextChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckCanonicalRosterAndSeed();
        CheckInitializationAndCommandLoop();
        CheckPhaseZeroTalkCreatesPlatformDetour();
        CheckPlatformResumePreservesStageLocalLatch();
        CheckPhaseOneTalkForcesCounterattack();
        CheckPostBronzeBranchesAndDirectPhaseZeroVictory();
        CheckPostGoldFirstLowLatchAndDefeat();
        CheckGenericParityGoldSlots();
        CheckRetryRearmsPhaseZeroTalkDetour();
        CheckBothVictoryFamiliesReachExactLeoBoundary();
    }

    private static void CheckCanonicalRosterAndSeed()
    {
        foreach (var saint in new byte[] { 0x00, 0x02, 0x03 })
        {
            var state = CancerStage03Context.CreateCanonicalEntry(saint);
            Require(state.ActiveSaint0533 == saint, $"Cancer accepts reachable Saint ${saint:X2}");
            Require(state.StoryProgress067D == 0x03 && state.Stage050E == 0x03,
                "Cancer canonical story/stage seed is $03/$03");
            Require(state.Phase067C == 0 && state.PlayerLowLatch064D == 0
                && state.Release0670 == 0 && state.IntroDone068E == 0,
                "Cancer common-reset seed clears phase/low/release/intro latches");
        }

        RequireThrows(() => CancerStage03Context.CreateCanonicalEntry(0x01),
            "Hyoga is blocked at Cancer story marker $32");
        RequireThrows(() => CancerStage03Context.CreateCanonicalEntry(0x04),
            "Ikki is blocked at Cancer story marker $32");
    }

    private static void CheckInitializationAndCommandLoop()
    {
        var entry = CancerStage03Context.CreateCanonicalEntry(CancerStage03Context.ShunIndex);
        var init = CancerStage03Context.ApplyInitialization(entry);

        Require(init.TemporaryPresentationStage == 0x0E,
            "Cancer initializer temporarily presents stage $0E");
        Require(init.SeventhSenseReward == 400,
            "Cancer initializer grants +400 Seventh Sense through #$04 -> $F31E");
        Require(init.State.Release0670 == 0x03 && init.State.IntroDone068E == 1,
            "Cancer shared intro handoff writes release $03 and $068E=1");
        Require(init.State.Stage050E == 0x03,
            "shared $9C3D restores real Cancer stage $03");

        var loop = CancerStage03Context.EnterCommandLoop(init.State);
        Require(loop.Release0670 == 0 && loop.IntroDone068E == 1,
            "command-loop entry consumes intro release without clearing intro latch");
    }

    private static void CheckPhaseZeroTalkCreatesPlatformDetour()
    {
        var state = EnterActive(CancerStage03Context.CreateCanonicalEntry());
        var talk = CancerStage03Context.ExecuteTalk(state);

        Require(talk.Outcome == CancerStage03TalkOutcome.PhaseZeroPlatformDetourRelease02,
            "phase-zero Cancer Talk takes the platform-detour branch");
        Require(talk.State.Release0670 == 0x02 && talk.PlatformSubstate02 == 0x0C,
            "phase-zero Talk writes release $02 and platform substate $0C");
        Require(talk.CallerUnwind && !talk.ForceGoldCounterattack,
            "phase-zero Talk double-PLA unwinds caller and does not force Gold response");
        Require(talk.State.Phase067C == 0,
            "phase increment belongs to special platform resume, not Talk itself");

        Require(CancerStage03Context.ResolvePlatformDetourExit(talk.State, 0x87, 0x20, 0) is null,
            "platform $0C rejects X below $88");
        Require(CancerStage03Context.ResolvePlatformDetourExit(talk.State, 0x88, 0x21, 0) is null,
            "platform $0C requires exact Y=$20");
        Require(CancerStage03Context.ResolvePlatformDetourExit(talk.State, 0x88, 0x20, 1) is null,
            "platform $0C rejects nonzero jump phase");
    }

    private static void CheckPlatformResumePreservesStageLocalLatch()
    {
        var active = EnterActive(CancerStage03Context.CreateCanonicalEntry(CancerStage03Context.ShiryuIndex));
        var low = CancerStage03Context.AfterGoldResponse(active, 0x01);
        Require(low.State.PlayerLowLatch064D == 1,
            "pre-detour first-low Gold feedback sets Cancer $064D latch");

        var detour = CancerStage03Context.ExecuteTalk(low.State);
        var resumed = CancerStage03Context.ResolvePlatformDetourExit(detour.State, 0x88, 0x20, 0);
        Require(resumed.HasValue, "canonical platform $0C gate accepts X=$88/Y=$20/jump=0");

        var value = resumed!.Value;
        Require(value.PlatformExit.ReloadField050E == 0x03 && value.PlatformExit.Field067C == 1,
            "special resume returns to stage $03 with $067C=1");
        Require(value.State.ActiveSaint0533 == CancerStage03Context.ShiryuIndex,
            "Cancer special resume preserves active reachable Saint");
        Require(value.State.PlayerLowLatch064D == 1,
            "release $02 special resume skips $A973 and preserves $064D");
        Require(value.State.IntroDone068E == 1,
            "release $02 special resume preserves intro latch $068E");
        Require(value.State.Release0670 == 0,
            "interactive selector re-entry consumes platform release $02");
    }

    private static void CheckPhaseOneTalkForcesCounterattack()
    {
        var phaseOne = ResumePhaseOne(CancerStage03Context.ShunIndex);
        var talk = CancerStage03Context.ExecuteTalk(phaseOne);

        Require(talk.Outcome == CancerStage03TalkOutcome.PhaseOneTalkForcesCounterattack,
            "phase-one Cancer Talk uses ordinary dialogue branch");
        Require(talk.State.Release0670 == 0 && talk.PlatformSubstate02 is null,
            "phase-one Talk does not recreate platform transition");
        Require(!talk.CallerUnwind && talk.ForceGoldCounterattack,
            "phase-one Talk raises transient $DC and forces Gold response");
    }

    private static void CheckPostBronzeBranchesAndDirectPhaseZeroVictory()
    {
        var phaseZero = EnterActive(CancerStage03Context.CreateCanonicalEntry(CancerStage03Context.SeiyaIndex));

        var victory = CancerStage03Context.AfterBronzeAction(
            phaseZero, opponentConditionEb: 0xFF, bronzeAttackHit: true, opponentScratch064A: 0x55);
        Require(victory.Outcome == CancerStage03PostBronzeOutcome.VictoryRelease01
            && victory.State.Release0670 == 0x01,
            "Cancer can win directly in phase zero before Talk/platform detour");
        Require(victory.OpponentScratch064A == 0x55 && !victory.OpponentScratchIncremented,
            "victory does not apply the stage-local $064A increment");

        var low1 = CancerStage03Context.AfterBronzeAction(
            phaseZero, opponentConditionEb: 0x01, bronzeAttackHit: true, opponentScratch064A: 0x10);
        Require(low1.Outcome == CancerStage03PostBronzeOutcome.OpponentLowFeedback
            && low1.OpponentScratch064A == 0x11 && low1.OpponentScratchIncremented,
            "$EB=$01 performs exactly one INC $064A before low-opponent feedback");

        var low2 = CancerStage03Context.AfterBronzeAction(
            phaseZero, opponentConditionEb: 0x01, bronzeAttackHit: false, opponentScratch064A: low1.OpponentScratch064A);
        Require(low2.OpponentScratch064A == 0x12 && low2.OpponentScratchIncremented,
            "Cancer low-opponent branch is repeatable and is not a one-shot latch");

        var miss = CancerStage03Context.AfterBronzeAction(
            phaseZero, opponentConditionEb: 0x00, bronzeAttackHit: false, opponentScratch064A: 0x22);
        Require(miss.Outcome == CancerStage03PostBronzeOutcome.MissFeedback
            && miss.OpponentScratch064A == 0x22,
            "$EB=$00 plus no-hit token selects miss feedback without touching $064A");

        var hit = CancerStage03Context.AfterBronzeAction(
            phaseZero, opponentConditionEb: 0x00, bronzeAttackHit: true, opponentScratch064A: 0x33);
        Require(hit.Outcome == CancerStage03PostBronzeOutcome.ContinueAfterHit,
            "$EB=$00 plus hit continues battle");

        var phaseOne = ResumePhaseOne(CancerStage03Context.ShiryuIndex);
        var phaseOneVictory = CancerStage03Context.AfterBronzeAction(phaseOne, 0xFF, true);
        Require(phaseOneVictory.State.Release0670 == 0x01,
            "post-detour phase-one Cancer uses the same ordinary victory owner");
    }

    private static void CheckPostGoldFirstLowLatchAndDefeat()
    {
        var state = EnterActive(CancerStage03Context.CreateCanonicalEntry(CancerStage03Context.ShunIndex));

        var firstLow = CancerStage03Context.AfterGoldResponse(state, 0x01);
        Require(firstLow.Outcome == CancerStage03PostGoldOutcome.FirstPlayerLowFeedback
            && firstLow.State.PlayerLowLatch064D == 1,
            "first $EA=$01 runs special low-player presentation and increments $064D");

        var repeatLow = CancerStage03Context.AfterGoldResponse(firstLow.State, 0x01);
        Require(repeatLow.Outcome == CancerStage03PostGoldOutcome.CommonHealthyOrRepeatFeedback
            && repeatLow.State.PlayerLowLatch064D == 1,
            "repeat low-player state uses common feedback and preserves one-shot latch");

        var healthy = CancerStage03Context.AfterGoldResponse(firstLow.State, 0x00);
        Require(healthy.Outcome == CancerStage03PostGoldOutcome.CommonHealthyOrRepeatFeedback,
            "$EA=$00 uses the same common feedback branch");

        var defeat = CancerStage03Context.AfterGoldResponse(firstLow.State, 0xFF);
        Require(defeat.Outcome == CancerStage03PostGoldOutcome.DefeatReleaseFF
            && defeat.State.Release0670 == 0xFF,
            "$EA=$FF emits canonical Cancer defeat release $FF");
    }

    private static void CheckGenericParityGoldSlots()
    {
        Require(CancerStage03Context.SelectGoldSlot(0x00) == 0,
            "generic parity selector chooses Cancer Gold slot 0 for even $065F");
        Require(CancerStage03Context.SelectGoldSlot(0x01) == 1,
            "generic parity selector chooses Cancer Gold slot 1 for odd $065F");
        Require(CancerStage03Context.SelectGoldSlot(0xFE) == 0
            && CancerStage03Context.SelectGoldSlot(0xFF) == 1,
            "Cancer exposes only Gold slots 0/1 across full parity byte range");
    }

    private static void CheckRetryRearmsPhaseZeroTalkDetour()
    {
        var phaseOne = ResumePhaseOne(CancerStage03Context.ShiryuIndex);
        var low = CancerStage03Context.AfterGoldResponse(phaseOne, 0x01);
        var defeat = CancerStage03Context.AfterGoldResponse(low.State, 0xFF);
        var retry = CancerStage03Context.ResetForRetryAfterDefeat(defeat.State);

        Require(retry.StoryProgress067D == 0x03 && retry.Stage050E == 0x03,
            "Cancer defeat retry does not advance story");
        Require(retry.Phase067C == 0 && retry.PlayerLowLatch064D == 0
            && retry.Release0670 == 0 && retry.IntroDone068E == 0,
            "$A973 retry reset clears phase/player-low/release/intro state");

        var retryActive = EnterActive(retry);
        var talk = CancerStage03Context.ExecuteTalk(retryActive);
        Require(talk.PlatformSubstate02 == 0x0C && talk.State.Release0670 == 0x02,
            "retry phase zero rearms the Talk-created platform $0C detour");

        var switchedRetry = CancerStage03Context.ResetForRetryAfterDefeat(defeat.State, CancerStage03Context.SeiyaIndex);
        Require(switchedRetry.ActiveSaint0533 == CancerStage03Context.SeiyaIndex,
            "retry may select another Cancer-reachable Saint");
        RequireThrows(() => CancerStage03Context.ResetForRetryAfterDefeat(defeat.State, 0x01),
            "retry cannot select blocked Hyoga at Cancer boundary");
    }

    private static void CheckBothVictoryFamiliesReachExactLeoBoundary()
    {
        var phaseZero = EnterActive(CancerStage03Context.CreateCanonicalEntry(CancerStage03Context.ShunIndex));
        var directVictory = CancerStage03Context.AfterBronzeAction(phaseZero, 0xFF, true).State;
        var directBoundary = CancerStage03Context.ResolveLeoBoundary(directVictory);

        Require(directBoundary.ActiveSaint0533 == CancerStage03Context.ShunIndex,
            "phase-zero direct Cancer victory preserves active Shun");
        Require(directBoundary.StoryProgress067D == 0x04 && directBoundary.Stage050E == 0x04,
            "phase-zero Cancer victory advances exactly to Leo progress/stage $04/$04");
        Require(directBoundary.StoryDescriptor06CD == 0x02 && directBoundary.StoryMarker0673 == 0x32,
            "Leo boundary reconstructs descriptor/marker $02/$32");

        var phaseOne = ResumePhaseOne(CancerStage03Context.ShiryuIndex);
        var postDetourVictory = CancerStage03Context.AfterBronzeAction(phaseOne, 0xFF, true).State;
        var postDetourBoundary = CancerStage03Context.ResolveLeoBoundary(postDetourVictory);

        Require(postDetourBoundary.ActiveSaint0533 == CancerStage03Context.ShiryuIndex,
            "post-detour Cancer victory preserves active Shiryu");
        Require(postDetourBoundary.StoryProgress067D == 0x04 && postDetourBoundary.Stage050E == 0x04
            && postDetourBoundary.StoryDescriptor06CD == 0x02 && postDetourBoundary.StoryMarker0673 == 0x32,
            "both canonical Cancer victory families converge on the same frozen Leo boundary");
    }

    private static CancerStage03State EnterActive(CancerStage03State entry)
    {
        var init = CancerStage03Context.ApplyInitialization(entry);
        return CancerStage03Context.EnterCommandLoop(init.State);
    }

    private static CancerStage03State ResumePhaseOne(byte saint)
    {
        var active = EnterActive(CancerStage03Context.CreateCanonicalEntry(saint));
        var detour = CancerStage03Context.ExecuteTalk(active);
        var resumed = CancerStage03Context.ResolvePlatformDetourExit(detour.State, 0x88, 0x20, 0);
        if (!resumed.HasValue)
            throw new InvalidOperationException("fixture expected canonical platform $0C exit");
        return resumed.Value.State;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void RequireThrows(Action action, string message)
    {
        try
        {
            action();
        }
        catch (ArgumentOutOfRangeException)
        {
            return;
        }
        catch (InvalidOperationException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }
}

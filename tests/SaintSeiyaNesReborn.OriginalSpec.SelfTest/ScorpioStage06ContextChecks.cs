using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class ScorpioStage06ContextChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckCanonicalSeedRosterAndNoOpInitialization();
        CheckLowHistoryTalkBranches();
        CheckHighHistoryTalkBranches();
        CheckByteSumAndGoldSelector();
        CheckPostBronzeBranches();
        CheckPostGoldBranches();
        CheckDefeatRetryRearmsTalkState();
        CheckVictoryBridgeToProgress08Stage10Platform08();
        CheckPlatformGateAndExactCapricornBoundary();
    }

    private static void CheckCanonicalSeedRosterAndNoOpInitialization()
    {
        foreach (var saint in new byte[] { 0x00, 0x01, 0x02, 0x03 })
        {
            var entry = ScorpioStage06Context.CreateCanonicalEntry(saint);
            Require(entry.ActiveSaint0533 == saint, $"Scorpio accepts reachable Saint ${saint:X2}");
            Require(entry.StoryProgress067D == 0x07 && entry.Stage050E == 0x06,
                "Scorpio canonical progress/stage seed is $07/$06");
            Require(entry.StoryDescriptor06CD == 0 && entry.StoryMarker0673 == 0x30,
                "Scorpio canonical descriptor/marker is $00/$30");
            Require(entry.TalkMilestone066F == 0 && entry.DodgeHistory0677 == 0
                && entry.DodgeHistory0678 == 0 && entry.HyogaRewardLatch068A == 0
                && entry.Release0670 == 0,
                "Scorpio common-reset seed clears reward/dodge/release state");

            var initialized = ScorpioStage06Context.ApplyInitialization(entry);
            Require(initialized == entry, "$9ACE RTS is an exact no-op Scorpio initializer");
        }

        RequireThrows(() => ScorpioStage06Context.CreateCanonicalEntry(0x04),
            "Ikki is masked by Scorpio story marker $30");
    }

    private static void CheckLowHistoryTalkBranches()
    {
        var hyoga = ScorpioStage06Context.CreateCanonicalEntry(ScorpioStage06Context.HyogaIndex);
        var first = ScorpioStage06Context.ExecuteTalk(hyoga);
        Require(first.Outcome == ScorpioStage06TalkOutcome.LowHistoryHyogaReward300,
            "low-history first Hyoga Talk takes the $068A reward branch");
        Require(first.Message1 == 0x84 && first.Message2 == 0x85,
            "low-history Hyoga Talk uses messages $84/$85");
        Require(first.SeventhSenseReward == 300 && first.State.HyogaRewardLatch068A == 1,
            "first low-history Hyoga Talk sets $068A and grants +300 Seventh Sense");
        Require(!first.ForceGoldCounterattack && first.State.TalkMilestone066F == 0,
            "low-history Hyoga reward does not touch $066F or force Gold");

        var repeat = ScorpioStage06Context.ExecuteTalk(first.State);
        Require(repeat.Outcome == ScorpioStage06TalkOutcome.LowHistoryHyogaRepeat
            && repeat.SeventhSenseReward == 0 && repeat.State.HyogaRewardLatch068A == 1,
            "$068A blocks a second +300 reward in the same runtime");

        foreach (var saint in new byte[] { 0x00, 0x02, 0x03 })
        {
            var ordinary = ScorpioStage06Context.ExecuteTalk(ScorpioStage06Context.CreateCanonicalEntry(saint));
            Require(ordinary.Outcome == ScorpioStage06TalkOutcome.LowHistoryOrdinary,
                "low-history non-Hyoga Talk takes ordinary branch");
            Require(ordinary.Message1 == 0xF8 && ordinary.Message2 == 0x3E
                && ordinary.SeventhSenseReward == 0 && !ordinary.ForceGoldCounterattack,
                "low-history ordinary branch is $F8/$3E with no reward/counterattack");
        }
    }

    private static void CheckHighHistoryTalkBranches()
    {
        foreach (var saint in new byte[] { 0x00, 0x01, 0x02, 0x03 })
        {
            var state = ScorpioStage06Context.WithDodgeHistory(
                ScorpioStage06Context.CreateCanonicalEntry(saint), 1, 1);
            var first = ScorpioStage06Context.ExecuteTalk(state);

            var expectedMessage = saint == ScorpioStage06Context.ShunIndex ? (byte)0x87 : (byte)0x86;
            Require(first.Outcome == ScorpioStage06TalkOutcome.HighHistoryFirstReward200,
                "first high-history Talk takes the shared milestone branch");
            Require(first.Message1 == expectedMessage && first.Message2 == 0xA3,
                "high-history Talk uses exact per-Saint table plus $A3");
            Require(first.State.TalkMilestone066F == 1 && first.SeventhSenseReward == 200,
                "first high-history Talk sets $066F and grants +200 Seventh Sense");
            Require(!first.ForceGoldCounterattack,
                "first high-history milestone does not force Gold response");

            var repeat = ScorpioStage06Context.ExecuteTalk(first.State);
            if (saint == ScorpioStage06Context.HyogaIndex)
            {
                Require(repeat.Outcome == ScorpioStage06TalkOutcome.HighHistoryRepeatHyoga
                    && !repeat.ForceGoldCounterattack,
                    "repeated high-history Hyoga Talk returns without $DC");
            }
            else
            {
                Require(repeat.Outcome == ScorpioStage06TalkOutcome.HighHistoryRepeatForcesCounterattack
                    && repeat.ForceGoldCounterattack,
                    "repeated high-history non-Hyoga Talk increments transient $DC and forces Gold");
            }
            Require(repeat.SeventhSenseReward == 0 && repeat.State.TalkMilestone066F == 1,
                "repeated high-history Talk does not repeat +200 reward");
        }
    }

    private static void CheckByteSumAndGoldSelector()
    {
        Require(ScorpioStage06Context.SelectGoldSlot(0, 0) == 1,
            "Scorpio selector chooses slot1 below two dodge events");
        Require(ScorpioStage06Context.SelectGoldSlot(1, 0) == 1,
            "Scorpio selector still chooses slot1 at dodge total one");
        Require(ScorpioStage06Context.SelectGoldSlot(1, 1) == 0
            && ScorpioStage06Context.SelectGoldSlot(2, 0) == 0,
            "Scorpio selector chooses slot0 at dodge total >=2");
        Require(ScorpioStage06Context.SelectGoldSlot(0xFF, 0x03) == 0,
            "dodge helper uses 8-bit ADC wrap: $FF+$03 -> $02");
        Require(ScorpioStage06Context.SelectGoldSlot(0xFF, 0x02) == 1,
            "dodge helper uses 8-bit ADC wrap: $FF+$02 -> $01");
    }

    private static void CheckPostBronzeBranches()
    {
        var state = ScorpioStage06Context.CreateCanonicalEntry(ScorpioStage06Context.ShiryuIndex);

        var silent = ScorpioStage06Context.AfterBronzeAction(state, 0x00, bronzeAttackHit: false);
        Require(silent.Outcome == ScorpioStage06PostBronzeOutcome.ContinueNoStageFeedback
            && silent.State.Release0670 == 0,
            "nonvictory no-hit Scorpio Bronze branch continues silently");

        var hit = ScorpioStage06Context.AfterBronzeAction(state, 0x01, bronzeAttackHit: true);
        Require(hit.Outcome == ScorpioStage06PostBronzeOutcome.ContinueHitFeedback
            && hit.State.Release0670 == 0,
            "nonvictory hit token selects stage message $A6 and continues");

        var victory = ScorpioStage06Context.AfterBronzeAction(state, 0xFF, bronzeAttackHit: false);
        Require(victory.Outcome == ScorpioStage06PostBronzeOutcome.VictoryRelease01
            && victory.State.Release0670 == 0x01,
            "$EB=$FF is terminal regardless of hit token and emits release $01");
    }

    private static void CheckPostGoldBranches()
    {
        var state = ScorpioStage06Context.CreateCanonicalEntry(ScorpioStage06Context.SeiyaIndex);

        var healthy = ScorpioStage06Context.AfterGoldResponse(state, 0x00);
        Require(healthy.Outcome == ScorpioStage06PostGoldOutcome.ContinueHealthy
            && healthy.State.Release0670 == 0,
            "$EA=$00 continues Scorpio battle");

        var low1 = ScorpioStage06Context.AfterGoldResponse(state, 0x01);
        var low2 = ScorpioStage06Context.AfterGoldResponse(low1.State, 0x01);
        Require(low1.Outcome == ScorpioStage06PostGoldOutcome.LowPlayerFeedback
            && low2.Outcome == ScorpioStage06PostGoldOutcome.LowPlayerFeedback,
            "$EA=$01 low-player feedback is repeatable with no Scorpio one-shot latch");

        var defeat = ScorpioStage06Context.AfterGoldResponse(state, 0xFF);
        Require(defeat.Outcome == ScorpioStage06PostGoldOutcome.DefeatReleaseFF
            && defeat.State.Release0670 == 0xFF,
            "$EA=$FF emits canonical Scorpio defeat release $FF");
    }

    private static void CheckDefeatRetryRearmsTalkState()
    {
        var high = ScorpioStage06Context.WithDodgeHistory(
            ScorpioStage06Context.CreateCanonicalEntry(ScorpioStage06Context.HyogaIndex), 1, 1);
        var milestone = ScorpioStage06Context.ExecuteTalk(high).State;
        var withHyogaLatch = milestone with { HyogaRewardLatch068A = 1 };
        var defeat = ScorpioStage06Context.AfterGoldResponse(withHyogaLatch, 0xFF).State;
        var retry = ScorpioStage06Context.ResetForRetryAfterDefeat(defeat);

        Require(retry.StoryProgress067D == 0x07 && retry.Stage050E == 0x06,
            "Scorpio defeat retry keeps canonical story progress/stage");
        Require(retry.TalkMilestone066F == 0 && retry.DodgeHistory0677 == 0
            && retry.DodgeHistory0678 == 0 && retry.HyogaRewardLatch068A == 0
            && retry.Release0670 == 0,
            "$A973 retry clears $066F/$0677/$0678/$068A/release and rearms Talk rewards");

        var rewardedAgain = ScorpioStage06Context.ExecuteTalk(retry);
        Require(rewardedAgain.Outcome == ScorpioStage06TalkOutcome.LowHistoryHyogaReward300,
            "retry makes Hyoga's low-history +300 branch eligible again");

        var switched = ScorpioStage06Context.ResetForRetryAfterDefeat(defeat, ScorpioStage06Context.ShunIndex);
        Require(switched.ActiveSaint0533 == ScorpioStage06Context.ShunIndex,
            "retry may select another Scorpio-reachable Saint");
    }

    private static void CheckVictoryBridgeToProgress08Stage10Platform08()
    {
        var state = ScorpioStage06Context.CreateCanonicalEntry(ScorpioStage06Context.ShunIndex);
        var victory = ScorpioStage06Context.AfterBronzeAction(state, 0xFF, true).State;
        var bridge = ScorpioStage06Context.ResolveProgress08BridgeAfterVictory(victory);

        Require(bridge.ActiveSaint0533 == ScorpioStage06Context.ShunIndex,
            "Scorpio victory preserves active Saint into successor bridge");
        Require(bridge.StoryProgress067D == 0x08 && bridge.StoryStage050E == 0x10,
            "first release $01 advances to progress $08 / story-stage $10");
        Require(bridge.StoryDescriptor06CD == 0 && bridge.StoryMarker0673 == 0x30,
            "progress-$08 bridge keeps descriptor/marker $00/$30");
        Require(bridge.InheritedRelease0670 == 0x01 && bridge.PlatformSubstate02 == 0x08,
            "$E4D7 maps progress $08 to principal platform substate $02=$08");
        Require(bridge.StoryStage050E != bridge.PlatformSubstate02,
            "story-stage $10 and principal platform substate $08 remain distinct namespaces");
    }

    private static void CheckPlatformGateAndExactCapricornBoundary()
    {
        var victory = ScorpioStage06Context.AfterBronzeAction(
            ScorpioStage06Context.CreateCanonicalEntry(ScorpioStage06Context.ShiryuIndex), 0xFF, true).State;
        var bridge = ScorpioStage06Context.ResolveProgress08BridgeAfterVictory(victory);

        Require(ScorpioStage06Context.ResolveCapricornBoundary(bridge, 0xCF, 0x40, 0) is null,
            "principal platform $08 rejects X below $D0");
        Require(ScorpioStage06Context.ResolveCapricornBoundary(bridge, 0xD0, 0x41, 0) is null,
            "principal platform $08 requires exact Y=$40");
        Require(ScorpioStage06Context.ResolveCapricornBoundary(bridge, 0xD0, 0x40, 1) is null,
            "principal platform $08 rejects nonzero jump phase");

        var accepted = ScorpioStage06Context.ResolveCapricornBoundary(bridge, 0xD0, 0x40, 0);
        Require(accepted.HasValue, "principal platform $08 accepts canonical common gate");
        var result = accepted!.Value;
        Require(result.PlatformTransition == PlatformExitTransitionKind.State3DReload,
            "platform $08 accepted exit uses normal $3D/$E100 reload");
        Require(result.Stage10AutoRelease01,
            "$E2DD recognizes reconstructed story-stage $10 and synthesizes release $01");
        Require(result.Boundary.ActiveSaint0533 == ScorpioStage06Context.ShiryuIndex,
            "two-step Scorpio success chain preserves active Saint");
        Require(result.Boundary.StoryProgress067D == 0x09 && result.Boundary.Stage050E == 0x07,
            "second release $01 advances exactly to Capricorn progress $09 / stage $07");
        Require(result.Boundary.StoryDescriptor06CD == 0 && result.Boundary.StoryMarker0673 == 0x30,
            "Capricorn boundary reconstructs descriptor/marker $00/$30");
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

using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class CapricornStage07ContextChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckCanonicalSeedAndOuterInitializationGate();
        CheckShiryuInitializationAndFeGuard();
        CheckTalkCounterAndMessages();
        CheckPostBronzeAnd0690Consumer();
        CheckPostGoldBranches();
        CheckGenericParityGoldSelector();
        CheckDefeatRetrySkipsInitializer();
        CheckVictoryForcedSeiyaAquariusBoundary();
    }

    private static void CheckCanonicalSeedAndOuterInitializationGate()
    {
        foreach (var saint in new byte[] { 0x00, 0x01, 0x02, 0x03 })
        {
            var entry = CapricornStage07Context.CreateCanonicalEntry(saint);
            Require(entry.ActiveSaint0533 == saint,
                $"Capricorn accepts reachable Saint ${saint:X2}");
            Require(entry.StoryProgress067D == 0x09 && entry.Stage050E == 0x07,
                "Capricorn canonical progress/stage seed is $09/$07");
            Require(entry.StoryDescriptor06CD == 0 && entry.StoryMarker0673 == 0x30,
                "Capricorn canonical descriptor/marker is $00/$30");
            Require(entry.Conversation066F == 0 && entry.Release0670 == 0
                && entry.ForcedMiss0690 == 0 && entry.IntroDone068E == 0,
                "Capricorn seed starts with cleared encounter-local state");
            Require(entry.ShiryuTechniqueCount058A == 1,
                "Shiryu enters Capricorn with canonical persistent technique count 1");
            Require(entry.ActiveTechniqueCount0696 == (saint == 0x03 ? 1 : 2),
                "active technique count matches canonical pre-Capricorn roster state");

            var init = CapricornStage07Context.ApplyCanonicalEntryInitialization(entry);
            if (saint == CapricornStage07Context.ShiryuIndex)
            {
                Require(init.Outcome == CapricornStage07InitializationOutcome.ShiryuUnlockReward600Release03,
                    "$F36F[$07]=$03 dispatches $9ACF for fresh Shiryu");
            }
            else
            {
                Require(init.Outcome == CapricornStage07InitializationOutcome.CallerBypassNonShiryu,
                    "$F36F[$07]=$03 bypasses $9ACF for Seiya/Hyoga/Shun");
                Require(init.SeventhSenseReward == 0 && init.State == entry,
                    "non-Shiryu Capricorn entry receives no initializer reward or technique mutation");
            }
        }

        RequireThrows(() => CapricornStage07Context.CreateCanonicalEntry(0x04),
            "Ikki is excluded by Capricorn story marker $30");
    }

    private static void CheckShiryuInitializationAndFeGuard()
    {
        var entry = CapricornStage07Context.CreateCanonicalEntry(CapricornStage07Context.ShiryuIndex);
        var initialized = CapricornStage07Context.ApplyCanonicalEntryInitialization(entry);

        Require(initialized.SeventhSenseReward == 600,
            "ordinary Shiryu initialization grants exact +600 Seventh Sense");
        Require(initialized.State.ShiryuTechniqueCount058A == 2
            && initialized.State.ActiveTechniqueCount0696 == 2,
            "$9B06/$9B09 unlock Shiryu's second technique 1->2");
        Require(initialized.State.StoryScratch0672 == 0,
            "$9AFC clears $0672 during ordinary Capricorn initialization");
        Require(initialized.State.Release0670 == 0x03 && initialized.State.IntroDone068E == 1,
            "shared $9C3D emits release $03 and marks intro done");

        var commandLoop = CapricornStage07Context.EnterCommandLoop(initialized.State);
        Require(commandLoop.Release0670 == 0,
            "fixed flow consumes Capricorn intro release $03 before commands");

        var repeatedDispatch = CapricornStage07Context.ApplyCanonicalEntryInitialization(initialized.State);
        Require(repeatedDispatch.Outcome == CapricornStage07InitializationOutcome.CallerBypassAlreadyInitialized
            && repeatedDispatch.SeventhSenseReward == 0
            && repeatedDispatch.State.ShiryuTechniqueCount058A == 2,
            "$068E!=0 outer gate prevents duplicate initialization in the same entry");

        var guarded = entry with { Release0670 = 0xFE };
        var feResult = CapricornStage07Context.ApplyInitializationHandler(guarded);
        Require(feResult.Outcome == CapricornStage07InitializationOutcome.ReleaseFeGuardNoOp
            && feResult.State == guarded && feResult.SeventhSenseReward == 0,
            "$9ACF returns immediately when inbound release is $FE");
    }

    private static void CheckTalkCounterAndMessages()
    {
        foreach (var saint in new byte[] { 0x00, 0x01, 0x02, 0x03 })
        {
            var state = EnterCapricornCommandLoop(saint);
            var first = CapricornStage07Context.ExecuteTalk(state);
            var expectedSaintMessage = saint == CapricornStage07Context.ShiryuIndex ? (byte)0xAF : (byte)0x43;

            Require(first.Outcome == CapricornStage07TalkOutcome.FirstConversation
                && first.CommonMessage == 0xAE && first.SaintMessage == expectedSaintMessage,
                "first Capricorn Talk uses $AE plus exact per-Saint $43/$43/$43/$AF table");
            Require(first.State.Conversation066F == 1 && !first.ForceGoldCounterattack,
                "first Talk increments $066F without raising transient $DC");

            var repeat = CapricornStage07Context.ExecuteTalk(first.State);
            Require(repeat.Outcome == CapricornStage07TalkOutcome.RepeatConversationForcesCounterattack
                && repeat.State.Conversation066F == 2 && repeat.ForceGoldCounterattack,
                "second Talk increments continuing counter and forces Gold response");

            var third = CapricornStage07Context.ExecuteTalk(repeat.State);
            Require(third.State.Conversation066F == 3 && third.ForceGoldCounterattack,
                "later Talks keep incrementing $066F rather than clamping it as a boolean");
        }
    }

    private static void CheckPostBronzeAnd0690Consumer()
    {
        var seiya = EnterCapricornCommandLoop(CapricornStage07Context.SeiyaIndex);
        var hit = CapricornStage07Context.AfterBronzeAction(seiya, 0x00, bronzeAttackHit: true);
        Require(hit.Outcome == CapricornStage07PostBronzeOutcome.ContinueAfterHit,
            "$EB=$00/$06BC!=0 continues Capricorn battle without stage feedback");

        var miss = CapricornStage07Context.AfterBronzeAction(seiya, 0x00, bronzeAttackHit: false);
        Require(miss.Outcome == CapricornStage07PostBronzeOutcome.MissFeedback8B,
            "$EB=$00/$06BC=0 selects Capricorn message $8B feedback");

        foreach (var saint in new byte[] { 0x00, 0x01, 0x02 })
        {
            var ordinary = EnterCapricornCommandLoop(saint);
            var low = CapricornStage07Context.AfterBronzeAction(ordinary, 0x01, bronzeAttackHit: true);
            Require(low.Outcome == CapricornStage07PostBronzeOutcome.OpponentLowArmsForcedMiss0690
                && low.State.ForcedMiss0690 == 0xFF,
                "$EB=$01 non-Shiryu arms $0690=$FF");
            Require(CapricornStage07Context.ApplyFixed0690Consumer(low.State, 0x7F) == 0,
                "$FAB9+ consumes nonzero $0690 and forces $06BC=0");
        }

        var shiryu = EnterCapricornCommandLoop(CapricornStage07Context.ShiryuIndex);
        var shiryuLow = CapricornStage07Context.AfterBronzeAction(shiryu, 0x01, bronzeAttackHit: true);
        Require(shiryuLow.Outcome == CapricornStage07PostBronzeOutcome.OpponentLowShiryuNoForcedMiss
            && shiryuLow.State.ForcedMiss0690 == 0,
            "$EB=$01 active Shiryu takes the explicit exception and does not arm $0690");
        Require(CapricornStage07Context.ApplyFixed0690Consumer(shiryuLow.State, 0x7F) == 0x7F,
            "with $0690=0 the compositor leaves generic $06BC ownership untouched");

        var victory = CapricornStage07Context.AfterBronzeAction(shiryu, 0xFF, bronzeAttackHit: false);
        Require(victory.Outcome == CapricornStage07PostBronzeOutcome.VictoryReleaseFE,
            "$EB=$FF enters Capricorn scripted victory sequence");
        Require(victory.SeventhSenseReward == 800 && victory.State.BossDefeated06B1 == 0xFF,
            "scripted Capricorn victory writes $06B1=$FF and grants exact +800 Seventh Sense");
        Require(victory.State.Release0670 == 0xFE,
            "scripted Capricorn victory emits release $FE");
    }

    private static void CheckPostGoldBranches()
    {
        var state = EnterCapricornCommandLoop(CapricornStage07Context.HyogaIndex);

        var healthy = CapricornStage07Context.AfterGoldResponse(state, 0x00);
        Require(healthy.Outcome == CapricornStage07PostGoldOutcome.ContinueHealthy
            && healthy.State.Release0670 == 0,
            "$EA=$00 continues Capricorn battle");

        var low1 = CapricornStage07Context.AfterGoldResponse(state, 0x01);
        var low2 = CapricornStage07Context.AfterGoldResponse(low1.State, 0x01);
        Require(low1.Outcome == CapricornStage07PostGoldOutcome.LowPlayerFeedback
            && low2.Outcome == CapricornStage07PostGoldOutcome.LowPlayerFeedback,
            "$EA=$01 feedback $40/$91 is repeatable with no one-shot Capricorn latch");

        var defeat = CapricornStage07Context.AfterGoldResponse(state, 0xFF);
        Require(defeat.Outcome == CapricornStage07PostGoldOutcome.DefeatReleaseFF
            && defeat.State.Release0670 == 0xFF,
            "$EA=$FF emits canonical Capricorn defeat release $FF");
    }

    private static void CheckGenericParityGoldSelector()
    {
        Require(CapricornStage07Context.SelectGoldSlot(0x00) == 0
            && CapricornStage07Context.SelectGoldSlot(0x02) == 0
            && CapricornStage07Context.SelectGoldSlot(0xFE) == 0,
            "generic parity selector sends even $065F values to Gold slot0");
        Require(CapricornStage07Context.SelectGoldSlot(0x01) == 1
            && CapricornStage07Context.SelectGoldSlot(0x03) == 1
            && CapricornStage07Context.SelectGoldSlot(0xFF) == 1,
            "generic parity selector sends odd $065F values to Gold slot1");
    }

    private static void CheckDefeatRetrySkipsInitializer()
    {
        var shiryuEntry = CapricornStage07Context.CreateCanonicalEntry(CapricornStage07Context.ShiryuIndex);
        var initialized = CapricornStage07Context.ApplyCanonicalEntryInitialization(shiryuEntry).State;
        var active = CapricornStage07Context.EnterCommandLoop(initialized);
        var talked = CapricornStage07Context.ExecuteTalk(active).State;
        var defeated = CapricornStage07Context.AfterGoldResponse(talked, 0xFF).State;

        Require(CapricornStage07Context.ResolveRetryAfterDefeat(defeated, 0xCF, 0x40, 0) is null,
            "retry platform $09 rejects X below $D0");
        Require(CapricornStage07Context.ResolveRetryAfterDefeat(defeated, 0xD0, 0x41, 0) is null,
            "retry platform $09 requires exact Y=$40");
        Require(CapricornStage07Context.ResolveRetryAfterDefeat(defeated, 0xD0, 0x40, 1) is null,
            "retry platform $09 rejects nonzero jump phase");

        var retry = CapricornStage07Context.ResolveRetryAfterDefeat(defeated, 0xD0, 0x40, 0);
        Require(retry.HasValue, "retry platform $09 accepts canonical common gate");
        var result = retry!.Value;
        Require(result.PlatformSubstate02 == 0x09
            && result.PlatformTransition == PlatformExitTransitionKind.State3DReload,
            "$FF at progress $09 routes through principal platform substate $09 and normal warm reload");
        Require(result.CommonResetApplied && !result.InitializationReexecuted,
            "$ED57/$A973 reset executes but warm reload resumes at $E33D without $9ACF");
        Require(result.State.Conversation066F == 0 && result.State.Release0670 == 0
            && result.State.ForcedMiss0690 == 0 && result.State.IntroDone068E == 0,
            "retry reset clears $066F/$0670/$0690/$068E");
        Require(result.State.ShiryuTechniqueCount058A == 2
            && result.State.ActiveTechniqueCount0696 == 2,
            "retry preserves the one-time Shiryu technique unlock without incrementing it again");
        Require(result.State.StoryScratch0672 == 0,
            "$0672 survives retry reset; $A973 does not own it");

        var seiya = EnterCapricornCommandLoop(CapricornStage07Context.SeiyaIndex);
        var armed = CapricornStage07Context.AfterBronzeAction(seiya, 0x01, true).State;
        var seiyaDefeat = CapricornStage07Context.AfterGoldResponse(armed, 0xFF).State;
        var seiyaRetry = CapricornStage07Context.ResolveRetryAfterDefeat(seiyaDefeat, 0xD0, 0x40, 0)!.Value;
        Require(seiyaRetry.State.ForcedMiss0690 == 0,
            "generic Capricorn retry clears an armed non-Shiryu $0690 forced-miss state");
    }

    private static void CheckVictoryForcedSeiyaAquariusBoundary()
    {
        foreach (var saint in new byte[] { 0x00, 0x01, 0x02, 0x03 })
        {
            var active = EnterCapricornCommandLoop(saint);
            var victory = CapricornStage07Context.AfterBronzeAction(active, 0xFF, true).State;
            var boundary = CapricornStage07Context.ResolveAquariusBoundaryAfterVictory(victory);

            Require(boundary.SavedWinningSaint0533 == saint,
                "$E3ED path saves the actual Capricorn winning Saint before the forced swap");
            Require(boundary.ActiveSaint0533 == CapricornStage07Context.SeiyaIndex,
                "release $FE forces active Saint to Seiya");
            Require(boundary.StoryProgress067D == 0x0A
                && boundary.Stage050E == AquariusStage08Context.StageIndex,
                "release $FE rewritten to $01 advances exactly to progress $0A / Aquarius stage $08");
            Require(boundary.StoryDescriptor06CD == 0x08 && boundary.StoryMarker0673 == 0x38,
                "Aquarius successor reconstructs $E50B[$0A]=$08 and marker $38");
        }
    }

    private static CapricornStage07State EnterCapricornCommandLoop(byte saint)
    {
        var entry = CapricornStage07Context.CreateCanonicalEntry(saint);
        var initialized = CapricornStage07Context.ApplyCanonicalEntryInitialization(entry).State;
        return CapricornStage07Context.EnterCommandLoop(initialized);
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

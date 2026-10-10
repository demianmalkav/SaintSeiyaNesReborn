using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;

internal static class MuStage00ContextChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckCanonicalSeedAndRoster();
        CheckCommonResetOwnership();
        CheckBlockedCommandOwner();
        CheckTalkTablesAndProgression();
        CheckExactTaurusBoundary();
        CheckNegativeBattleReachability();
        CheckTerminalGuard();
    }

    private static void CheckCanonicalSeedAndRoster()
    {
        var state = MuStage00Context.CreateCanonicalEntry();
        Require(state.ActiveSaint0533 == 0,
            "canonical new-game Mu entry starts with Seiya");
        Require(state.Conversation066F == 0 && state.Release0670 == 0 && state.BlockedCommandCount06BB == 0,
            "global clear plus common battle reset seed $066F/$0670/$06BB to zero");

        for (byte saint = 0; saint <= 3; saint++)
            Require(MuStage00Context.IsReachableSaint(saint), $"Mu roster includes canonical Saint {saint}");

        Require(!MuStage00Context.IsReachableSaint(4),
            "Mu initial story marker $30 masks out Ikki");

        var threw = false;
        try
        {
            _ = MuStage00Context.CreateCanonicalEntry(4);
        }
        catch (ArgumentOutOfRangeException)
        {
            threw = true;
        }

        Require(threw, "canonical Mu context rejects structurally indexed but unreachable Ikki");
    }

    private static void CheckCommonResetOwnership()
    {
        var dirty = new MuStage00State(
            ActiveSaint0533: 2,
            Conversation066F: 7,
            Release0670: 0x01,
            BlockedCommandCount06BB: 5);

        var reset = MuStage00Context.ApplyCommonBattleReset(dirty);
        Require(reset.ActiveSaint0533 == 2,
            "$A973 does not replace the active reachable Saint");
        Require(reset.Conversation066F == 0 && reset.Release0670 == 0,
            "$A973 explicitly clears Mu conversation/release state");
        Require(reset.BlockedCommandCount06BB == 5,
            "$A973 does not clear $06BB; blocked-command history is preserved");
    }

    private static void CheckBlockedCommandOwner()
    {
        foreach (var command in new[]
        {
            MuStage00Command.ResourceAllocation,
            MuStage00Command.Attack,
            MuStage00Command.Escape
        })
        {
            var initial = MuStage00Context.CreateCanonicalEntry(activeSaint0533: 1);
            var first = MuStage00Context.ExecuteCommand(initial, command);
            Require(first.Outcome == MuStage00CommandOutcome.BlockedFirstAttempt,
                $"first {command} uses first-attempt $F238 branch");
            Require(first.State.BlockedCommandCount06BB == 1,
                $"first {command} increments $06BB 0->1");
            Require(first.BlockedPreludeMessage is null && first.BlockedMainMessage == 0x39,
                $"first {command} omits $48 and retains shared blocked message $39");
            Require(first.State.Release0670 == 0,
                $"blocked {command} emits no story release");

            var second = MuStage00Context.ExecuteCommand(first.State, command);
            Require(second.Outcome == MuStage00CommandOutcome.BlockedRepeatedAttempt,
                $"later {command} uses repeated $F238 branch");
            Require(second.State.BlockedCommandCount06BB == 2,
                $"later {command} increments $06BB again");
            Require(second.BlockedPreludeMessage == 0x48 && second.BlockedMainMessage == 0x39,
                $"later {command} adds $48 before shared $39");
            Require(!second.ReachesBronzePipeline && !second.ReachesGoldPipeline,
                $"blocked {command} cannot enter ordinary battle arithmetic");
        }

        var wrapSeed = MuStage00Context.CreateCanonicalEntry() with { BlockedCommandCount06BB = 0xFF };
        var wrapped = MuStage00Context.ExecuteCommand(wrapSeed, MuStage00Command.Attack);
        Require(wrapped.Outcome == MuStage00CommandOutcome.BlockedRepeatedAttempt
            && wrapped.State.BlockedCommandCount06BB == 0,
            "$F238 tests nonzero before 8-bit INC $06BB, so $FF wraps to zero after taking repeated branch");
    }

    private static void CheckTalkTablesAndProgression()
    {
        byte[] firstSaintLines = [0x35, 0x35, 0x36, 0x34];
        byte[] secondSaintLines = [0x3B, 0x3B, 0x11, 0x3B];

        for (byte saint = 0; saint <= 3; saint++)
        {
            var initial = MuStage00Context.CreateCanonicalEntry(saint);
            var first = MuStage00Context.ExecuteCommand(initial, MuStage00Command.Talk);

            Require(first.Outcome == MuStage00CommandOutcome.FirstTalk,
                $"Saint {saint} first Mu Talk uses the $066F==0 branch");
            Require(first.State.Conversation066F == 1 && first.State.Release0670 == 0,
                $"Saint {saint} first Talk writes $066F=1 without release");
            Require(first.CommonMessage1 == 0x32 && first.CommonMessage2 == 0x33,
                $"Saint {saint} first Talk preserves common selectors $32/$33");
            Require(first.SaintMessage == firstSaintLines[saint],
                $"Saint {saint} first Talk uses exact $9D24 selector");
            Require(first.State.BlockedCommandCount06BB == 0,
                $"Talk does not mutate blocked-command counter for Saint {saint}");

            var second = MuStage00Context.ExecuteCommand(first.State, MuStage00Command.Talk);
            Require(second.Outcome == MuStage00CommandOutcome.ProgressRelease01,
                $"Saint {saint} second Mu Talk uses nonzero-$066F progression branch");
            Require(second.State.Conversation066F == 1 && second.State.Release0670 == 0x01,
                $"Saint {saint} second Talk preserves $066F and emits release $01");
            Require(second.CommonMessage1 == 0x37 && second.CommonMessage2 == 0x38,
                $"Saint {saint} second Talk preserves common selectors $37/$38");
            Require(second.SaintMessage == secondSaintLines[saint],
                $"Saint {saint} second Talk uses exact $9D28 selector");
            Require(MuStage00Context.IsTerminal(second.State),
                $"Saint {saint} second Talk is the sole Mu progression terminal");
        }

        var afterBlocked = MuStage00Context.ExecuteCommand(
            MuStage00Context.CreateCanonicalEntry(),
            MuStage00Command.Attack).State;
        var talkAfterBlocked = MuStage00Context.ExecuteCommand(afterBlocked, MuStage00Command.Talk);
        Require(talkAfterBlocked.State.BlockedCommandCount06BB == 1,
            "Mu Talk leaves accumulated $06BB history untouched");
    }

    private static void CheckExactTaurusBoundary()
    {
        for (byte saint = 0; saint <= 3; saint++)
        {
            var state = MuStage00Context.CreateCanonicalEntry(saint);
            state = MuStage00Context.ExecuteCommand(state, MuStage00Command.Talk).State;
            state = MuStage00Context.ExecuteCommand(state, MuStage00Command.Talk).State;

            var boundary = MuStage00Context.ResolveTaurusBoundary(state);
            Require(boundary.StoryProgress067D == 0x01 && boundary.Stage050E == 0x01,
                $"Saint {saint} release $01 joins fixed progress $00->$01 / Taurus stage $01");
            Require(boundary.StoryDescriptor06CD == 0x00 && boundary.StoryMarker0673 == 0x30,
                $"Saint {saint} fixed $E50B[1] descriptor reconstructs Taurus pre-entry marker $30");
            Require(boundary.ActiveSaint0533 == saint,
                $"Saint {saint} is preserved because unreachable Ikki-only substitution cannot fire");
            Require(BattleStageContextCoverage.StageForStoryProgress(boundary.StoryProgress067D) == boundary.Stage050E,
                $"Saint {saint} boundary agrees with frozen $F016 story-stage table");
        }
    }

    private static void CheckNegativeBattleReachability()
    {
        foreach (var command in Enum.GetValues<MuStage00Command>())
        {
            var result = MuStage00Context.ExecuteCommand(MuStage00Context.CreateCanonicalEntry(), command);
            Require(!result.ReachesBronzePipeline,
                $"stage $00 command {command} cannot reach Bronze damage/post-Bronze dispatch");
            Require(!result.ReachesGoldPipeline,
                $"stage $00 command {command} cannot reach Gold selector/dodge/damage/post-Gold dispatch");
        }
    }

    private static void CheckTerminalGuard()
    {
        var state = MuStage00Context.CreateCanonicalEntry();
        state = MuStage00Context.ExecuteCommand(state, MuStage00Command.Talk).State;
        state = MuStage00Context.ExecuteCommand(state, MuStage00Command.Talk).State;

        var threw = false;
        try
        {
            _ = MuStage00Context.ExecuteCommand(state, MuStage00Command.Attack);
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Require(threw, "terminal Mu release cannot execute another local command");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

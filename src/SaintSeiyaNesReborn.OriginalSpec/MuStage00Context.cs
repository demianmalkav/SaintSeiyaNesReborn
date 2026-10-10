namespace SaintSeiyaNesReborn.OriginalSpec;

public enum MuStage00Command
{
    ResourceAllocation,
    Attack,
    Talk,
    Escape
}

public enum MuStage00CommandOutcome
{
    BlockedFirstAttempt,
    BlockedRepeatedAttempt,
    FirstTalk,
    ProgressRelease01
}

public readonly record struct MuStage00State(
    byte ActiveSaint0533,
    byte Conversation066F,
    byte Release0670,
    byte BlockedCommandCount06BB);

public readonly record struct MuStage00CommandResult(
    MuStage00State State,
    MuStage00Command Command,
    MuStage00CommandOutcome Outcome,
    byte? CommonMessage1,
    byte? CommonMessage2,
    byte? SaintMessage,
    byte? BlockedPreludeMessage,
    byte? BlockedMainMessage,
    bool ReachesBronzePipeline,
    bool ReachesGoldPipeline);

public readonly record struct MuStage00TaurusBoundary(
    byte StoryProgress067D,
    byte Stage050E,
    byte StoryDescriptor06CD,
    byte StoryMarker0673,
    byte ActiveSaint0533);

/// <summary>
/// Canonical stage-local control model for story progress $067D=$00 /
/// battle-event stage $050E=$00 (Mu / pre-battle repair context).
///
/// This is deliberately not modeled as an ordinary Gold-Saint battle. Fixed
/// command owners divert Resource Allocation, Attack and Escape to $F238 before
/// Bronze/Gold arithmetic. Talk alone reaches bank-5 handler $9CB7 and advances
/// the story on the second invocation.
/// </summary>
public static class MuStage00Context
{
    public const byte StageIndex = 0x00;
    public const byte StoryProgressSeed = 0x00;
    public const byte ProgressRelease = 0x01;
    public const byte SuccessorStoryProgress = 0x01;
    public const byte SuccessorStage = 0x01;

    public const byte CanonicalInitialSaint = 0x00; // Seiya
    public const byte InitialStoryMarker0673 = 0x30;
    public const byte SuccessorStoryDescriptor06CD = 0x00;
    public const byte SuccessorStoryMarker0673 = 0x30;

    public const ushort GlobalRamClear = 0xAD4A;
    public const ushort NewGameStateInitializer = 0xA100;
    public const ushort CommonBattleReset = 0xA973;
    public const ushort StageInitialization = 0x97F7;
    public const ushort TalkHandler = 0x9CB7;
    public const ushort BlockedCommandOwner = 0xF238;
    public const ushort ProgressReleaseOwner = 0xE399;
    public const ushort ProgressIncrementOwner = 0xE3B3;

    public const byte BlockedRepeatedPreludeMessage = 0x48;
    public const byte BlockedMainMessage = 0x39;
    public const byte FirstTalkCommonMessage1 = 0x32;
    public const byte FirstTalkCommonMessage2 = 0x33;
    public const byte SecondTalkCommonMessage1 = 0x37;
    public const byte SecondTalkCommonMessage2 = 0x38;

    // $9D24 and $9D28 are four-byte tables indexed by canonical battle Saint.
    // Initial story marker $30 rejects Ikki's $10 bit, so only 0..3 are reachable.
    private static readonly byte[] FirstTalkSaintMessages = [0x35, 0x35, 0x36, 0x34];
    private static readonly byte[] SecondTalkSaintMessages = [0x3B, 0x3B, 0x11, 0x3B];

    /// <summary>
    /// Exact canonical entry after the global RAM clear and common battle reset.
    /// $AD4A-$AD54 clears $0600-$06FF, which seeds $06BB=0. Bank-1 $A100+
    /// derives $0673=$30 and selects Seiya initially. The fixed Saint selector
    /// can still choose any of 0..3; Ikki (4) is rejected by the $30 story mask.
    /// </summary>
    public static MuStage00State CreateCanonicalEntry(byte activeSaint0533 = CanonicalInitialSaint)
    {
        ValidateReachableSaint(activeSaint0533);
        return new(
            ActiveSaint0533: activeSaint0533,
            Conversation066F: 0,
            Release0670: 0,
            BlockedCommandCount06BB: 0);
    }

    /// <summary>
    /// Bank-1 common reset $A973 clears $066F/$0670 but does not touch $06BB.
    /// This helper makes that ownership explicit for re-entry/fixture composition.
    /// </summary>
    public static MuStage00State ApplyCommonBattleReset(MuStage00State state)
    {
        ValidateReachableSaint(state.ActiveSaint0533);
        return state with
        {
            Conversation066F = 0,
            Release0670 = 0
        };
    }

    public static MuStage00CommandResult ExecuteCommand(MuStage00State state, MuStage00Command command)
    {
        EnsureActive(state);
        ValidateReachableSaint(state.ActiveSaint0533);

        return command switch
        {
            MuStage00Command.Talk => ExecuteTalk(state),
            MuStage00Command.ResourceAllocation or MuStage00Command.Attack or MuStage00Command.Escape
                => ExecuteBlockedCommand(state, command),
            _ => throw new ArgumentOutOfRangeException(nameof(command), command, null)
        };
    }

    /// <summary>
    /// Fixed stage-zero gates at $F041/$F057/$F0D3 all tail into $F238.
    /// $F238 tests $06BB before incrementing it. The first blocked command omits
    /// message $48; every later command includes $48 before the shared $39 line.
    /// No release or ordinary Bronze/Gold battle surface is reached.
    /// </summary>
    private static MuStage00CommandResult ExecuteBlockedCommand(MuStage00State state, MuStage00Command command)
    {
        var repeated = state.BlockedCommandCount06BB != 0;
        var next = state with
        {
            BlockedCommandCount06BB = unchecked((byte)(state.BlockedCommandCount06BB + 1))
        };

        return new(
            next,
            command,
            repeated ? MuStage00CommandOutcome.BlockedRepeatedAttempt : MuStage00CommandOutcome.BlockedFirstAttempt,
            CommonMessage1: null,
            CommonMessage2: null,
            SaintMessage: null,
            BlockedPreludeMessage: repeated ? BlockedRepeatedPreludeMessage : null,
            BlockedMainMessage: BlockedMainMessage,
            ReachesBronzePipeline: false,
            ReachesGoldPipeline: false);
    }

    /// <summary>
    /// Bank-5 $9CB7. The only control predicate is $066F==0 versus nonzero.
    /// Active Saint changes only the final text selector through $9D24/$9D28.
    /// First Talk writes $066F=1; second/repeated Talk emits $0670=$01.
    /// </summary>
    private static MuStage00CommandResult ExecuteTalk(MuStage00State state)
    {
        if (state.Conversation066F == 0)
        {
            return new(
                state with { Conversation066F = 1 },
                MuStage00Command.Talk,
                MuStage00CommandOutcome.FirstTalk,
                FirstTalkCommonMessage1,
                FirstTalkCommonMessage2,
                FirstTalkSaintMessages[state.ActiveSaint0533],
                BlockedPreludeMessage: null,
                BlockedMainMessage: null,
                ReachesBronzePipeline: false,
                ReachesGoldPipeline: false);
        }

        return new(
            state with { Release0670 = ProgressRelease },
            MuStage00Command.Talk,
            MuStage00CommandOutcome.ProgressRelease01,
            SecondTalkCommonMessage1,
            SecondTalkCommonMessage2,
            SecondTalkSaintMessages[state.ActiveSaint0533],
            BlockedPreludeMessage: null,
            BlockedMainMessage: null,
            ReachesBronzePipeline: false,
            ReachesGoldPipeline: false);
    }

    /// <summary>
    /// Composes release $01 through fixed $E399/$E3B3 and the already-promoted
    /// story tables. For reachable Mu Saints 0..3, the special Ikki substitution
    /// in $E399 is impossible, so the active Saint is preserved. $E50B[1]=0 and
    /// $F016[1]=1 yield the exact Taurus boundary below.
    /// </summary>
    public static MuStage00TaurusBoundary ResolveTaurusBoundary(MuStage00State terminalState)
    {
        if (terminalState.Release0670 != ProgressRelease)
            throw new InvalidOperationException("Mu progression boundary requires canonical release $01.");

        ValidateReachableSaint(terminalState.ActiveSaint0533);

        return new(
            StoryProgress067D: SuccessorStoryProgress,
            Stage050E: SuccessorStage,
            StoryDescriptor06CD: SuccessorStoryDescriptor06CD,
            StoryMarker0673: SuccessorStoryMarker0673,
            ActiveSaint0533: terminalState.ActiveSaint0533);
    }

    public static bool IsTerminal(MuStage00State state) => state.Release0670 == ProgressRelease;

    public static bool IsReachableSaint(byte activeSaint0533) => activeSaint0533 <= 0x03;

    private static void ValidateReachableSaint(byte activeSaint0533)
    {
        if (!IsReachableSaint(activeSaint0533))
            throw new ArgumentOutOfRangeException(nameof(activeSaint0533), activeSaint0533,
                "Canonical Mu entry permits Seiya/Hyoga/Shun/Shiryu (0..3); Ikki is masked out by $0673=$30.");
    }

    private static void EnsureActive(MuStage00State state)
    {
        if (state.Release0670 != 0)
            throw new InvalidOperationException("A terminal Mu release cannot execute another stage-local command.");
    }
}

namespace SaintSeiyaNesReborn.OriginalSpec;

public enum FinalSpecialEntryVariant
{
    SeiyaAfterShunPiscesVictory,
    ShunAfterSeiyaPiscesVictory
}

public enum FinalSpecialTalkOutcome
{
    FirstMarinTalkReward,
    RepeatMarinTalk
}

public enum FinalSpecialAttackOutcome
{
    RoseClearEffectRelease01
}

public enum FinalSpecialPassiveCommandOutcome
{
    EscapeBlockedDialogue,
    ResourceAllocationSuppressed
}

public enum FinalSpecialReleaseOwner
{
    ActiveSpecialContext,
    StoryAdvanceToSaga
}

public readonly record struct FinalSpecialStage0CState(
    byte ActiveSaint0533,
    byte StoryProgress067D,
    byte Stage050E,
    byte ProgressDescriptor06CD,
    byte StoryRoster0673,
    byte Conversation066F,
    byte Release0670,
    byte TransientDc,
    byte EffectState0632,
    byte BronzeHitToken06BC);

public readonly record struct FinalSpecialTalkResult(
    FinalSpecialStage0CState State,
    FinalSpecialTalkOutcome Outcome,
    int SeventhSenseReward,
    byte FirstMessageId,
    byte SecondMessageId);

public readonly record struct FinalSpecialAttackResult(
    FinalSpecialStage0CState State,
    FinalSpecialAttackOutcome Outcome,
    byte TemporaryEffectStage050E,
    int EffectIterations,
    bool TechniqueSelectionCanCancel,
    bool UsesGenericOpponentDamage,
    bool UsesPostBronzeDispatcher,
    bool UsesGoldResponseOrDodge,
    bool UsesPostGoldDispatcher);

public readonly record struct FinalSpecialPassiveCommandResult(
    FinalSpecialStage0CState State,
    FinalSpecialPassiveCommandOutcome Outcome,
    byte? MessageId,
    bool OpensResourceAllocation);

public readonly record struct FinalSpecialSagaHandoff(
    byte ActiveSaint0533,
    byte StoryProgress067D,
    byte NextStage050E,
    byte ProgressDescriptor06CD,
    byte StoryRoster0673);

public readonly record struct FinalSpecialSubsystemReachability(
    bool InitializationDispatcher,
    bool GenericOpponentDamage,
    bool PostBronzeDispatcher,
    bool GoldResponseOrDodge,
    bool PostGoldDispatcher,
    bool ResourceAllocation);

/// <summary>
/// Executable control model for the canonical final-special context at
/// story progress/stage $067D/$050E=$0C.
///
/// This is deliberately not modeled as a Gold-Saint boss battle. Canonical entry
/// bypasses the malformed stage-12 initialization-table overrun, fixed attack code
/// suppresses ordinary opponent damage and all post-Bronze/Gold machinery, and the
/// only advancing action is the special rose-clearing attack that emits release $01.
/// </summary>
public static class FinalSpecialStage0CContext
{
    public const byte StageIndex = 0x0C;
    public const byte StoryProgress = 0x0C;
    public const byte SagaStoryProgress = 0x0D;
    public const byte SagaStageIndex = 0x0A;

    public const byte SeiyaIndex = 0x00;
    public const byte ShunIndex = 0x02;

    public const byte SeiyaEntryDescriptor06CD = 0x0E;
    public const byte SeiyaEntryRoster0673 = 0x3E;
    public const byte ShunEntryDescriptor06CD = 0x0B;
    public const byte ShunEntryRoster0673 = 0x3B;

    public const byte SagaDescriptor06CD = 0x0E;
    public const byte SagaRoster0673 = 0x3E;

    public const byte StoryAdvanceRelease = 0x01;
    public const byte TemporaryRoseEffectStage = 0x12;
    public const byte RoseEffectFinalState0632 = 0x1A;
    public const int RoseEffectIterations = 0x40;

    public const byte TalkMessageMarin = 0xD3;
    public const byte TalkMessageSeiya = 0xD5;
    public const byte EscapeBlockedMessage = 0xD4;
    public const int FirstTalkSeventhSenseReward = 1000;

    // Stage index $0C overruns the intended $97E1 init pointer table and would read
    // bytes $97F9/$97FA as little-endian $8D00. Canonical entry never dispatches it:
    // the mandatory-Saint table $F36F[$0C]=$FF cannot match active Saint 0 or 2.
    public const ushort RawInitializationOverrunPointer = 0x8D00;

    public static FinalSpecialStage0CState PrepareFromPisces(FinalSpecialEntryVariant variant)
        => variant switch
        {
            FinalSpecialEntryVariant.SeiyaAfterShunPiscesVictory => new(
                ActiveSaint0533: SeiyaIndex,
                StoryProgress067D: StoryProgress,
                Stage050E: StageIndex,
                ProgressDescriptor06CD: SeiyaEntryDescriptor06CD,
                StoryRoster0673: SeiyaEntryRoster0673,
                Conversation066F: 0,
                Release0670: 0,
                TransientDc: 0,
                EffectState0632: 0,
                BronzeHitToken06BC: 0),

            FinalSpecialEntryVariant.ShunAfterSeiyaPiscesVictory => new(
                ActiveSaint0533: ShunIndex,
                StoryProgress067D: StoryProgress,
                Stage050E: StageIndex,
                ProgressDescriptor06CD: ShunEntryDescriptor06CD,
                StoryRoster0673: ShunEntryRoster0673,
                Conversation066F: 0,
                Release0670: 0,
                TransientDc: 0,
                EffectState0632: 0,
                BronzeHitToken06BC: 0),

            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unknown final-special entry variant.")
        };

    public static FinalSpecialEntryVariant ClassifyEntry(FinalSpecialStage0CState state)
    {
        ValidateCanonicalEntry(state);
        return state.ActiveSaint0533 == SeiyaIndex
            ? FinalSpecialEntryVariant.SeiyaAfterShunPiscesVictory
            : FinalSpecialEntryVariant.ShunAfterSeiyaPiscesVictory;
    }

    /// <summary>
    /// Canonical stage-$0C entry returns before the normal bank-5 initialization
    /// dispatcher. The raw stage-12 pointer-table overrun is therefore structural
    /// garbage/data reachability, not an executable initialization handler.
    /// </summary>
    public static bool IsInitializationDispatcherReachable(FinalSpecialStage0CState state)
    {
        ValidateCanonicalEntry(state);
        return false;
    }

    /// <summary>
    /// Models bank-5 Talk handler $A1AD. It contains no active-Saint branch:
    /// both exact Pisces-derived entries display $D3 then $D5. On the first Talk,
    /// $066F==0 causes #$10 -> $A1FF -> fixed $F31E, i.e. +1000 Seventh Sense,
    /// then increments $066F. Repeat Talk replays the dialogue without reward.
    /// It never raises transient $DC and never emits a release.
    /// </summary>
    public static FinalSpecialTalkResult ExecuteTalk(FinalSpecialStage0CState state)
    {
        EnsureCommandActive(state);
        ValidateCanonicalEntryIdentity(state);

        if (state.Conversation066F == 0)
        {
            return new(
                state with { Conversation066F = 1 },
                FinalSpecialTalkOutcome.FirstMarinTalkReward,
                FirstTalkSeventhSenseReward,
                TalkMessageMarin,
                TalkMessageSeiya);
        }

        return new(
            state,
            FinalSpecialTalkOutcome.RepeatMarinTalk,
            0,
            TalkMessageMarin,
            TalkMessageSeiya);
    }

    /// <summary>
    /// Models command 1 / Attack at stage $0C.
    ///
    /// Fixed selection code makes technique cancellation loop back into selection.
    /// Bank-1 action code sets $0632=2, clears $06BC, renders the selected technique,
    /// calls special $FF9F and explicitly skips generic opponent damage. $FF9F maps
    /// bank 0 and runs $B900: that effect temporarily writes $050E=$12, executes until
    /// $06C1==$40, writes $0632=$1A and restores $050E=$0C. Fixed $F08B+ then emits
    /// release $0670=$01. Stage >=$0B gates prevent post-Bronze, Gold dodge/damage and
    /// post-Gold dispatchers from executing.
    /// </summary>
    public static FinalSpecialAttackResult ExecuteAttack(FinalSpecialStage0CState state)
    {
        EnsureCommandActive(state);
        ValidateCanonicalEntryIdentity(state);

        var next = state with
        {
            Stage050E = StageIndex,
            EffectState0632 = RoseEffectFinalState0632,
            BronzeHitToken06BC = 0,
            Release0670 = StoryAdvanceRelease
        };

        return new(
            next,
            FinalSpecialAttackOutcome.RoseClearEffectRelease01,
            TemporaryRoseEffectStage,
            RoseEffectIterations,
            TechniqueSelectionCanCancel: false,
            UsesGenericOpponentDamage: false,
            UsesPostBronzeDispatcher: false,
            UsesGoldResponseOrDodge: false,
            UsesPostGoldDispatcher: false);
    }

    /// <summary>
    /// Command 3 / Escape is intercepted at fixed $F14A+ for stage $0C and displays
    /// message $D4. It does not emit a release or advance story progress.
    /// </summary>
    public static FinalSpecialPassiveCommandResult ExecuteEscape(FinalSpecialStage0CState state)
    {
        EnsureCommandActive(state);
        ValidateCanonicalEntryIdentity(state);
        return new(
            state,
            FinalSpecialPassiveCommandOutcome.EscapeBlockedDialogue,
            EscapeBlockedMessage,
            OpensResourceAllocation: false);
    }

    /// <summary>
    /// Command 4 normally enters the Life/Cosmo/Seventh-Sense allocation UI through
    /// fixed $FB89. Fixed $F041+ treats stage >=$0C as a redraw-only path, so canonical
    /// stage $0C suppresses the resource-allocation subsystem entirely.
    /// </summary>
    public static FinalSpecialPassiveCommandResult ExecuteResourceAllocation(FinalSpecialStage0CState state)
    {
        EnsureCommandActive(state);
        ValidateCanonicalEntryIdentity(state);
        return new(
            state,
            FinalSpecialPassiveCommandOutcome.ResourceAllocationSuppressed,
            MessageId: null,
            OpensResourceAllocation: false);
    }

    /// <summary>
    /// Release $01 joins fixed story progression. From $067D=$0C, fixed $E3B3 increments
    /// to $0D, ordinary descriptor lookup supplies $E50B[$0D]=$0E and therefore
    /// $06CD=$0E / $0673=$3E. Fixed stage table $F016[$0D]=$0A selects Saga.
    /// The current active Saint is preserved for both reachable variants.
    /// </summary>
    public static FinalSpecialSagaHandoff AdvanceAfterRoseClear(FinalSpecialStage0CState state)
    {
        ValidateCanonicalEntryIdentity(state);
        if (state.Release0670 != StoryAdvanceRelease)
            throw new InvalidOperationException("Final-special story advance requires release $01 from the rose-clearing attack.");
        if (state.StoryProgress067D != StoryProgress || state.Stage050E != StageIndex)
            throw new InvalidOperationException("Final-special story advance requires canonical progress/stage $0C.");

        return new(
            ActiveSaint0533: state.ActiveSaint0533,
            StoryProgress067D: SagaStoryProgress,
            NextStage050E: SagaStageIndex,
            ProgressDescriptor06CD: SagaDescriptor06CD,
            StoryRoster0673: SagaRoster0673);
    }

    public static FinalSpecialReleaseOwner ResolveReleaseOwner(FinalSpecialStage0CState state)
        => state.Release0670 switch
        {
            0x00 => FinalSpecialReleaseOwner.ActiveSpecialContext,
            StoryAdvanceRelease => FinalSpecialReleaseOwner.StoryAdvanceToSaga,
            _ => throw new InvalidOperationException($"Unsupported reachable final-special release ${state.Release0670:X2}.")
        };

    public static FinalSpecialSubsystemReachability GetSubsystemReachability(FinalSpecialStage0CState state)
    {
        ValidateCanonicalEntryIdentity(state);
        return new(
            InitializationDispatcher: false,
            GenericOpponentDamage: false,
            PostBronzeDispatcher: false,
            GoldResponseOrDodge: false,
            PostGoldDispatcher: false,
            ResourceAllocation: false);
    }

    private static void EnsureCommandActive(FinalSpecialStage0CState state)
    {
        ValidateCanonicalEntryIdentity(state);
        if (state.Release0670 != 0)
            throw new InvalidOperationException("Final-special command execution requires active release $0670=0.");
    }

    private static void ValidateCanonicalEntry(FinalSpecialStage0CState state)
    {
        if (state.StoryProgress067D != StoryProgress || state.Stage050E != StageIndex)
            throw new InvalidOperationException("Canonical final-special entry requires $067D/$050E=$0C.");
        ValidateCanonicalEntryIdentity(state);
    }

    private static void ValidateCanonicalEntryIdentity(FinalSpecialStage0CState state)
    {
        var seiya = state.ActiveSaint0533 == SeiyaIndex
            && state.ProgressDescriptor06CD == SeiyaEntryDescriptor06CD
            && state.StoryRoster0673 == SeiyaEntryRoster0673;
        var shun = state.ActiveSaint0533 == ShunIndex
            && state.ProgressDescriptor06CD == ShunEntryDescriptor06CD
            && state.StoryRoster0673 == ShunEntryRoster0673;

        if (!seiya && !shun)
            throw new InvalidOperationException("State is not one of the two proven Pisces-derived final-special entry identities.");
    }
}
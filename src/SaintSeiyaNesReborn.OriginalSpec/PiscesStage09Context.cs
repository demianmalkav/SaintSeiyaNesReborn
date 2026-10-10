namespace SaintSeiyaNesReborn.OriginalSpec;

public enum PiscesInitOutcome
{
    NoStageLocalInitialization
}

public enum PiscesTalkOutcome
{
    BeforeTwoDodgesSeiyaDialogue,
    BeforeTwoDodgesShunDialogue,
    FirstPostDodgeShunResolveReward,
    PostDodgeForcesCounterattack
}

public enum PiscesTechniqueGrowth
{
    None,
    BronzeActionTwoIncrement,
    BronzeActionFiveIncrement
}

public enum PiscesPostBronzeOutcome
{
    HealthyContinue,
    FirstLowOpponentEvent,
    RepeatLowOpponent,
    VictoryReleaseFe
}

public enum PiscesPostGoldOutcome
{
    Continue,
    LowPlayerFeedback,
    DefeatReleaseFf
}

public enum PiscesReleaseOwner
{
    ActiveBattle,
    StageAdvanceZeroActiveResources,
    GenericDefeat
}

public enum PiscesFinalSpecialVariant
{
    Seiya,
    Shun
}

public readonly record struct PiscesStage09State(
    byte ActiveSaint0533,
    byte StoryProgress067D,
    byte StoryRoster0673,
    byte Conversation066F,
    byte Release0670,
    byte DodgeFailures0677,
    byte DodgeSuccesses0678,
    byte AttackEscalation064D,
    byte BronzeActionCountEF,
    byte ShunTechniqueCount0589,
    byte ActiveTechniqueCount0696);

public readonly record struct PiscesInitResult(PiscesStage09State State, PiscesInitOutcome Outcome);
public readonly record struct PiscesTalkResult(
    PiscesStage09State State,
    PiscesTalkOutcome Outcome,
    bool ForceGoldCounterattack,
    int SeventhSenseReward);
public readonly record struct PiscesPostBronzeResult(
    PiscesStage09State State,
    PiscesPostBronzeOutcome Outcome,
    PiscesTechniqueGrowth TechniqueGrowth,
    int SeventhSenseReward);
public readonly record struct PiscesPostGoldResult(PiscesStage09State State, PiscesPostGoldOutcome Outcome);
public readonly record struct PiscesGoldAttackSelection(byte Slot0680, int CosmoCoefficient, int LifeCoefficient);
public readonly record struct PiscesAdvanceResult(
    byte StoryProgress067D,
    byte NextStage050E,
    byte ActiveSaint0533,
    byte ProgressDescriptor06CD,
    byte StoryRoster0673,
    PiscesFinalSpecialVariant Variant,
    bool WinnerResourcesZeroed);

/// <summary>
/// Stage-local control model for canonical battle stage $050E=$09 (Pisces/Aphrodite).
/// Generic resource arithmetic, Bronze/Gold damage and dodge resolution remain owned by
/// the existing specifications. This model owns stage-9 Talk, Bronze-action progression,
/// Shun's two technique-growth thresholds, Gold-slot escalation and terminal handoffs.
/// </summary>
public static class PiscesStage09Context
{
    public const byte StageIndex = 0x09;
    public const byte StoryProgress = 0x0B;
    public const byte FinalSpecialStoryProgress = 0x0C;
    public const byte FinalSpecialStageIndex = 0x0C;

    public const byte SeiyaIndex = 0x00;
    public const byte ShunIndex = 0x02;
    public const byte HighestSaintIndex = 0x04;

    // Fixed $E50B[$0B]=$0A followed by OR #$30 produces $0673=$3A.
    // Generic selection at $F6FE tests bits $01/$02/$04/$08/$10: a set bit is
    // unavailable. Therefore only Seiya ($01 clear) and Shun ($04 clear) are
    // selectable at canonical Pisces entry.
    public const byte CanonicalPreEntryStoryRoster0673 = 0x3A;

    public const byte StageAdvanceRelease = 0xFE;
    public const byte DefeatRelease = 0xFF;

    public const int ShunResolveSeventhSenseReward = 1000;
    public const int VictorySeventhSenseReward = 1200;

    /// <summary>
    /// Builds a reachable stage-9 battle state. The common bank-5 battle-entry path
    /// $9780-$9789 ORs the selected Saint's bit into $0673, so the stored story roster
    /// represents the already-selected/otherwise unavailable set seen by later fixed
    /// progression. $EF is dedicated to Pisces and starts at zero on canonical entry.
    /// </summary>
    public static PiscesStage09State PrepareBattleRuntime(
        byte activeSaint0533,
        byte shunTechniqueCount0589 = 2,
        byte activeTechniqueCount0696 = 2)
    {
        ValidateSaintIndex(activeSaint0533);
        if (!IsSelectableAtCanonicalEntry(activeSaint0533))
            throw new ArgumentOutOfRangeException(nameof(activeSaint0533), activeSaint0533,
                "Canonical Pisces entry allows only Seiya or Shun.");

        var enteredRoster = (byte)(CanonicalPreEntryStoryRoster0673 | SaintBit(activeSaint0533));
        return new(
            ActiveSaint0533: activeSaint0533,
            StoryProgress067D: StoryProgress,
            StoryRoster0673: enteredRoster,
            Conversation066F: 0,
            Release0670: 0,
            DodgeFailures0677: 0,
            DodgeSuccesses0678: 0,
            AttackEscalation064D: 0,
            BronzeActionCountEF: 0,
            ShunTechniqueCount0589: shunTechniqueCount0589,
            ActiveTechniqueCount0696: activeTechniqueCount0696);
    }

    /// <summary>
    /// Stage-9 initialization pointer $9B5C is a bare RTS. All meaningful Pisces
    /// setup comes from the generic battle entry and the story-progression state.
    /// </summary>
    public static PiscesInitResult ApplyInitialization(PiscesStage09State state)
    {
        EnsureTurnActive(state);
        ValidateReachableActiveSaint(state.ActiveSaint0533);
        return new(state, PiscesInitOutcome.NoStageLocalInitialization);
    }

    /// <summary>
    /// Models Talk handler $9F99. Helper $A1EC supplies the byte sum
    /// $0677+$0678. Before two Gold-attack dodge attempts Talk is dialogue-only.
    /// At two or more attempts, the first Shun Talk while $066F==0 grants the
    /// scripted $A1FF reward (+1000 Seventh Sense through fixed $F31E), increments
    /// $066F and returns without a Gold response. Every other post-threshold Talk
    /// increments transient $DC in the ROM and therefore forces a Gold response.
    /// </summary>
    public static PiscesTalkResult ExecuteTalk(PiscesStage09State state)
    {
        EnsureTurnActive(state);
        ValidateReachableActiveSaint(state.ActiveSaint0533);

        var totalDodgeAttempts = AddByte(state.DodgeFailures0677, state.DodgeSuccesses0678);
        if (totalDodgeAttempts < 2)
        {
            return state.ActiveSaint0533 == SeiyaIndex
                ? new(state, PiscesTalkOutcome.BeforeTwoDodgesSeiyaDialogue, false, 0)
                : new(state, PiscesTalkOutcome.BeforeTwoDodgesShunDialogue, false, 0);
        }

        if (state.Conversation066F == 0 && state.ActiveSaint0533 == ShunIndex)
        {
            return new(
                state with { Conversation066F = Increment(state.Conversation066F) },
                PiscesTalkOutcome.FirstPostDodgeShunResolveReward,
                false,
                ShunResolveSeventhSenseReward);
        }

        return new(state, PiscesTalkOutcome.PostDodgeForcesCounterattack, true, 0);
    }

    /// <summary>
    /// Models post-Bronze handler $AA57. Every Bronze action first increments
    /// stage counter $064D and dedicated zero-page action counter $EF. In reachable
    /// stage-9 states the raw ROM test "$0533 != 0" is equivalent to active Shun,
    /// because the story roster permits only Seiya (0) and Shun (2).
    ///
    /// When the incremented $EF equals 2 or 5, Shun's persistent technique count
    /// $0589 and active menu count $0696 are incremented before opponent-condition
    /// handling. Thus a threshold action that defeats Aphrodite still grants its
    /// technique increment first. A Seiya route permanently misses those exact
    /// equality-triggered increments.
    /// </summary>
    public static PiscesPostBronzeResult AfterBronzeAction(
        PiscesStage09State state,
        byte opponentConditionEb)
    {
        EnsureTurnActive(state);
        ValidateReachableActiveSaint(state.ActiveSaint0533);
        ValidateCondition(opponentConditionEb, nameof(opponentConditionEb));

        var next = state with
        {
            AttackEscalation064D = Increment(state.AttackEscalation064D),
            BronzeActionCountEF = Increment(state.BronzeActionCountEF)
        };

        var growth = PiscesTechniqueGrowth.None;
        if (state.ActiveSaint0533 != SeiyaIndex)
        {
            if (next.BronzeActionCountEF == 2)
                growth = PiscesTechniqueGrowth.BronzeActionTwoIncrement;
            else if (next.BronzeActionCountEF == 5)
                growth = PiscesTechniqueGrowth.BronzeActionFiveIncrement;

            if (growth != PiscesTechniqueGrowth.None)
            {
                next = next with
                {
                    ShunTechniqueCount0589 = Increment(next.ShunTechniqueCount0589),
                    ActiveTechniqueCount0696 = Increment(next.ActiveTechniqueCount0696)
                };
            }
        }

        if (opponentConditionEb == 0xFF)
        {
            return new(
                next with { Release0670 = StageAdvanceRelease },
                PiscesPostBronzeOutcome.VictoryReleaseFe,
                growth,
                VictorySeventhSenseReward);
        }

        if (opponentConditionEb == 0x01)
        {
            if (next.AttackEscalation064D < 0x80)
            {
                return new(
                    next with { AttackEscalation064D = 0x80 },
                    PiscesPostBronzeOutcome.FirstLowOpponentEvent,
                    growth,
                    0);
            }

            return new(next, PiscesPostBronzeOutcome.RepeatLowOpponent, growth, 0);
        }

        return new(next, PiscesPostBronzeOutcome.HealthyContinue, growth, 0);
    }

    /// <summary>
    /// Models post-Gold handler $AAF0 after generic Gold attack/dodge/damage and
    /// player-condition classification. $EA=$01 is repeatable feedback only;
    /// $EA=$FF releases the ordinary defeat token $FF.
    /// </summary>
    public static PiscesPostGoldResult AfterGoldResponse(
        PiscesStage09State state,
        byte playerConditionEa)
    {
        EnsureTurnActive(state);
        ValidateReachableActiveSaint(state.ActiveSaint0533);
        ValidateCondition(playerConditionEa, nameof(playerConditionEa));

        return playerConditionEa switch
        {
            0xFF => new(state with { Release0670 = DefeatRelease }, PiscesPostGoldOutcome.DefeatReleaseFf),
            0x01 => new(state, PiscesPostGoldOutcome.LowPlayerFeedback),
            _ => new(state, PiscesPostGoldOutcome.Continue)
        };
    }

    /// <summary>
    /// Bank-6 $90D5-$90EA selects the stage-9 Gold attack solely from $064D:
    /// 0..2 -> slot0, 3..5 -> slot1, >=6 -> slot2. The scripted low-opponent
    /// latch $064D=$80 therefore jumps immediately to slot2. Slot3 is structurally
    /// present in the coefficient table but unreachable through this selector.
    /// </summary>
    public static PiscesGoldAttackSelection SelectGoldAttack(PiscesStage09State state)
    {
        EnsureTurnActive(state);

        if (state.AttackEscalation064D >= 6)
            return new(0x02, 29, 29);
        if (state.AttackEscalation064D >= 3)
            return new(0x01, 34, 22);
        return new(0x00, 22, 32);
    }

    /// <summary>
    /// Joins Pisces release $FE to the already-existing generic/fixed owner.
    /// Bank-5 $ACBD zeroes the active Life/Cosmo mirrors before the outer fixed
    /// progression saves/switches the battle record. Fixed $E3B3 increments
    /// $067D from $0B to $0C. The special $067D==$0C branch then chooses the only
    /// survivor for stage $0C from Seiya's bit in the pre-existing $0673 roster:
    /// if Seiya's bit is clear -> Seiya with descriptor $0E; otherwise -> Shun
    /// with descriptor $0B. OR #$30 produces $0673=$3E or $3B respectively.
    /// The story-stage table maps progress $0C to special stage $0C.
    /// </summary>
    public static PiscesAdvanceResult AdvanceAfterVictory(PiscesStage09State state)
    {
        if (state.Release0670 != StageAdvanceRelease)
            throw new InvalidOperationException("Pisces stage advance requires release $FE.");
        if (state.StoryProgress067D != StoryProgress)
            throw new InvalidOperationException("Canonical Pisces stage advance requires story progress $0B.");

        var seiyaAlreadyMarked = (state.StoryRoster0673 & SaintBit(SeiyaIndex)) != 0;
        if (seiyaAlreadyMarked)
        {
            const byte descriptor = 0x0B;
            return new(
                StoryProgress067D: FinalSpecialStoryProgress,
                NextStage050E: FinalSpecialStageIndex,
                ActiveSaint0533: ShunIndex,
                ProgressDescriptor06CD: descriptor,
                StoryRoster0673: (byte)(descriptor | 0x30),
                Variant: PiscesFinalSpecialVariant.Shun,
                WinnerResourcesZeroed: true);
        }

        {
            const byte descriptor = 0x0E;
            return new(
                StoryProgress067D: FinalSpecialStoryProgress,
                NextStage050E: FinalSpecialStageIndex,
                ActiveSaint0533: SeiyaIndex,
                ProgressDescriptor06CD: descriptor,
                StoryRoster0673: (byte)(descriptor | 0x30),
                Variant: PiscesFinalSpecialVariant.Seiya,
                WinnerResourcesZeroed: true);
        }
    }

    public static PiscesReleaseOwner ResolveReleaseOwner(PiscesStage09State state) => state.Release0670 switch
    {
        0x00 => PiscesReleaseOwner.ActiveBattle,
        StageAdvanceRelease => PiscesReleaseOwner.StageAdvanceZeroActiveResources,
        DefeatRelease => PiscesReleaseOwner.GenericDefeat,
        _ => throw new InvalidOperationException($"Unsupported canonical Pisces release ${state.Release0670:X2}.")
    };

    public static bool IsSelectableAtCanonicalEntry(byte saintIndex)
    {
        ValidateSaintIndex(saintIndex);
        return (CanonicalPreEntryStoryRoster0673 & SaintBit(saintIndex)) == 0;
    }

    private static byte SaintBit(byte saintIndex) => (byte)(1 << saintIndex);

    private static byte Increment(byte value) => unchecked((byte)(value + 1));

    private static byte AddByte(byte left, byte right) => unchecked((byte)(left + right));

    private static void EnsureTurnActive(PiscesStage09State state)
    {
        if (state.Release0670 != 0)
            throw new InvalidOperationException("Pisces turn logic requires active $0670=0.");
    }

    private static void ValidateCondition(byte value, string name)
    {
        if (value is not (0x00 or 0x01 or 0xFF))
            throw new ArgumentOutOfRangeException(name, value, "Battle condition must be $00, $01 or $FF.");
    }

    private static void ValidateReachableActiveSaint(byte value)
    {
        if (value is not (SeiyaIndex or ShunIndex))
            throw new ArgumentOutOfRangeException(nameof(value), value,
                "Reachable Pisces battle states allow only Seiya or Shun.");
    }

    private static void ValidateSaintIndex(byte value)
    {
        if (value > HighestSaintIndex)
            throw new ArgumentOutOfRangeException(nameof(value), value, "Canonical battle Saint index is 0..4.");
    }
}

namespace SaintSeiyaNesReborn.OriginalSpec;

public enum VirgoInitOutcome
{
    OrdinaryNonIkkiEntry,
    IkkiBlockedToHighPresentationDd,
    SubstituteIkkiIntroRelease03,
    CompletedIkkiPhaseReleaseFe
}

public enum VirgoTalkOutcome
{
    NonIkkiForcesCounterattack,
    IkkiBeforePlatformDetourNoCounterattack,
    IkkiAfterPlatformDetourForcesCounterattack
}

public enum VirgoPostBronzeOutcome
{
    NonIkkiHealthyFeedback,
    NonIkkiLowFirstEvent,
    NonIkkiLowRepeat,
    NonIkkiVictoryRelease01,
    IkkiFirstActionToPlatform0DRelease02,
    IkkiThresholdEventRaises0683,
    IkkiPhaseCompleteReleaseFe,
    IkkiOneTimePostDetourEvent,
    IkkiHitFeedback,
    IkkiContinue
}

public enum VirgoPostGoldOutcome
{
    Continue,
    NonIkkiFirstPlayerLowEvent,
    DefeatReleaseFf,
    IkkiLowAfter0683DefeatReleaseFf
}

public enum VirgoReleaseOwner
{
    ActiveBattle,
    GenericVictory,
    PlatformSubstate0D,
    IntroHandoff,
    HighPresentation90,
    StageAdvanceForceSeiya,
    GenericDefeat
}

public readonly record struct VirgoStage05State(
    byte ActiveSaint0533,
    byte PlatformSubstate02,
    byte PlayerLowEvent064D,
    byte OpponentEvent064E,
    byte Phase067C,
    byte StoryMarker0673,
    byte Progress0683,
    byte OneTime06E0,
    byte ScriptedBronzeBlock0690,
    byte Release0670,
    byte IntroDone068E);

public readonly record struct VirgoInitResult(VirgoStage05State State, VirgoInitOutcome Outcome);
public readonly record struct VirgoTalkResult(VirgoStage05State State, VirgoTalkOutcome Outcome, bool ForceGoldCounterattack);
public readonly record struct VirgoPostBronzeResult(VirgoStage05State State, VirgoPostBronzeOutcome Outcome);
public readonly record struct VirgoPostGoldResult(VirgoStage05State State, VirgoPostGoldOutcome Outcome);
public readonly record struct VirgoGoldAttackSelection(byte Slot0680, int CosmoCoefficient, int LifeCoefficient);

/// <summary>
/// Stage-local control model for canonical battle stage $050E=$05 (Virgo/Shaka).
/// Generic battle arithmetic remains owned by the existing resource, damage,
/// technique and dodge specifications. This model owns only stage-5 control,
/// Ikki substitution, special platform/reload handoffs and Gold-slot reachability.
/// </summary>
public static class VirgoStage05Context
{
    public const byte StageIndex = 0x05;
    public const byte IkkiIndex = 0x04;
    public const byte SeiyaIndex = 0x00;
    public const byte ScriptedBronzeBlockEnabled = 0xFF;

    public const byte VictoryRelease = 0x01;
    public const byte PlatformDetourRelease = 0x02;
    public const byte IntroRelease = 0x03;
    public const byte HighPresentationRelease = 0xDD;
    public const byte StageAdvanceRelease = 0xFE;
    public const byte DefeatRelease = 0xFF;

    public static VirgoStage05State PrepareBattleRuntime(
        byte activeSaint0533,
        byte inboundStoryMarker0673 = 0,
        byte inboundProgress0683 = 0,
        byte inboundOneTime06E0 = 0)
    {
        ValidateSaint(activeSaint0533);
        return new(
            activeSaint0533,
            PlatformSubstate02: 0,
            PlayerLowEvent064D: 0,
            OpponentEvent064E: 0,
            Phase067C: 0,
            StoryMarker0673: inboundStoryMarker0673,
            Progress0683: inboundProgress0683,
            OneTime06E0: inboundOneTime06E0,
            ScriptedBronzeBlock0690: 0,
            Release0670: 0,
            IntroDone068E: 0);
    }

    /// <summary>
    /// Models bank-5 $9A28.
    /// Ikki before $0683 progression is rejected to release $DD after writing
    /// $0673=$3F. A later non-Ikki entry carrying that marker performs the long
    /// substitution sequence, installs Ikki, arms $0690 and ends through shared
    /// $9C56 release $03. Ikki with $0683!=0 instead exits through $FE and is
    /// switched back to Seiya.
    /// </summary>
    public static VirgoInitResult ApplyInitialization(VirgoStage05State state)
    {
        EnsureActive(state);

        if (state.ActiveSaint0533 == IkkiIndex)
        {
            if (state.Progress0683 == 0)
            {
                return new(
                    state with { StoryMarker0673 = 0x3F, Release0670 = HighPresentationRelease },
                    VirgoInitOutcome.IkkiBlockedToHighPresentationDd);
            }

            return new(
                state with { ActiveSaint0533 = SeiyaIndex, Release0670 = StageAdvanceRelease },
                VirgoInitOutcome.CompletedIkkiPhaseReleaseFe);
        }

        if (state.StoryMarker0673 != 0x3F)
            return new(state, VirgoInitOutcome.OrdinaryNonIkkiEntry);

        return new(
            state with
            {
                ActiveSaint0533 = IkkiIndex,
                StoryMarker0673 = 0x2F,
                ScriptedBronzeBlock0690 = ScriptedBronzeBlockEnabled,
                Release0670 = IntroRelease,
                IntroDone068E = 1
            },
            VirgoInitOutcome.SubstituteIkkiIntroRelease03);
    }

    public static VirgoStage05State EnterCommandLoop(VirgoStage05State state)
    {
        if (state.Release0670 is not (0x00 or IntroRelease))
            throw new InvalidOperationException("Only active Virgo state or release $03 can enter the command loop.");
        return state with { Release0670 = 0 };
    }

    /// <summary>
    /// Models $9E1B. Non-Ikki always raises transient $DC. Ikki before the
    /// platform detour ($067C==0) has the one Talk path that returns without
    /// forcing Shaka's Gold response. After the detour, Ikki Talk forces it.
    /// </summary>
    public static VirgoTalkResult ExecuteTalk(VirgoStage05State state)
    {
        EnsureTurnActive(state);

        if (state.ActiveSaint0533 != IkkiIndex)
            return new(state, VirgoTalkOutcome.NonIkkiForcesCounterattack, true);

        if (state.Phase067C == 0)
            return new(state, VirgoTalkOutcome.IkkiBeforePlatformDetourNoCounterattack, false);

        return new(state, VirgoTalkOutcome.IkkiAfterPlatformDetourForcesCounterattack, true);
    }

    /// <summary>
    /// Models stage-5 post-Bronze handler $A661. $EB is supplied by the generic
    /// opponent classifier and $06BC by the generic hit gate/damage layer.
    /// </summary>
    public static VirgoPostBronzeResult AfterBronzeAction(
        VirgoStage05State state,
        byte opponentConditionEb,
        byte hitToken06BC)
    {
        EnsureTurnActive(state);
        ValidateCondition(opponentConditionEb, nameof(opponentConditionEb));

        if (state.ActiveSaint0533 != IkkiIndex)
        {
            if (opponentConditionEb == 0xFF)
                return new(state with { Release0670 = VictoryRelease }, VirgoPostBronzeOutcome.NonIkkiVictoryRelease01);

            if (opponentConditionEb == 0x01)
            {
                if (state.OpponentEvent064E == 0)
                    return new(state with { OpponentEvent064E = 1 }, VirgoPostBronzeOutcome.NonIkkiLowFirstEvent);
                return new(state, VirgoPostBronzeOutcome.NonIkkiLowRepeat);
            }

            return new(state, VirgoPostBronzeOutcome.NonIkkiHealthyFeedback);
        }

        // First Ikki Bronze action is consumed by the special platform detour,
        // independent of $EB/$06BC. $A6E4 also clears $064D.
        if (state.Phase067C == 0)
        {
            return new(
                state with
                {
                    PlatformSubstate02 = 0x0D,
                    PlayerLowEvent064D = 0,
                    Release0670 = PlatformDetourRelease
                },
                VirgoPostBronzeOutcome.IkkiFirstActionToPlatform0DRelease02);
        }

        var unlocked = state with { ScriptedBronzeBlock0690 = 0 };

        // In the Ikki phase, any non-healthy opponent condition (low or defeated)
        // becomes a scripted threshold event rather than normal victory. $A701+
        // latches $064D and increments $0683, then unwinds the current action.
        if (opponentConditionEb != 0x00 && unlocked.PlayerLowEvent064D == 0)
        {
            return new(
                unlocked with
                {
                    PlayerLowEvent064D = 1,
                    Progress0683 = unchecked((byte)(unlocked.Progress0683 + 1))
                },
                VirgoPostBronzeOutcome.IkkiThresholdEventRaises0683);
        }

        if (unlocked.Progress0683 != 0)
            return new(unlocked with { Release0670 = StageAdvanceRelease }, VirgoPostBronzeOutcome.IkkiPhaseCompleteReleaseFe);

        if (unlocked.OneTime06E0 == 0)
            return new(unlocked with { OneTime06E0 = 1 }, VirgoPostBronzeOutcome.IkkiOneTimePostDetourEvent);

        if (hitToken06BC != 0)
            return new(unlocked, VirgoPostBronzeOutcome.IkkiHitFeedback);

        return new(unlocked, VirgoPostBronzeOutcome.IkkiContinue);
    }

    /// <summary>
    /// Models $A7B3 after generic Gold dodge/damage/classification.
    /// </summary>
    public static VirgoPostGoldResult AfterGoldResponse(VirgoStage05State state, byte playerConditionEa)
    {
        EnsureTurnActive(state);
        ValidateCondition(playerConditionEa, nameof(playerConditionEa));

        if (state.ActiveSaint0533 != IkkiIndex)
        {
            if (playerConditionEa == 0xFF)
                return new(state with { Release0670 = DefeatRelease }, VirgoPostGoldOutcome.DefeatReleaseFf);

            if (playerConditionEa == 0x01 && state.PlayerLowEvent064D == 0)
                return new(state with { PlayerLowEvent064D = 1 }, VirgoPostGoldOutcome.NonIkkiFirstPlayerLowEvent);

            return new(state, VirgoPostGoldOutcome.Continue);
        }

        if (playerConditionEa == 0xFF)
            return new(state with { Release0670 = DefeatRelease }, VirgoPostGoldOutcome.DefeatReleaseFf);

        if (state.Progress0683 != 0 && playerConditionEa == 0x01)
            return new(state with { Release0670 = DefeatRelease }, VirgoPostGoldOutcome.IkkiLowAfter0683DefeatReleaseFf);

        return new(state, VirgoPostGoldOutcome.Continue);
    }

    /// <summary>
    /// Fixed $ED57 increments $067C when returning from releases $02/$03. For
    /// Virgo the material special case is release $02 after platform substate
    /// $0D: runtime reset is skipped and the resumed battle observes $067C+1.
    /// </summary>
    public static VirgoStage05State ResumeBattleAfterSpecialRelease(VirgoStage05State state)
    {
        if (state.Release0670 is not (PlatformDetourRelease or IntroRelease))
            throw new InvalidOperationException("Only Virgo releases $02/$03 use the special resume phase increment.");

        return state with
        {
            Phase067C = unchecked((byte)(state.Phase067C + 1)),
            Release0670 = 0
        };
    }

    /// <summary>
    /// Bank-6 $9079-$9089 forces Gold slot 2 exactly for stage 5 + Ikki.
    /// Non-Ikki falls through to the generic final selector $065F&1, so only
    /// slots 0/1 are reachable there. Stage-5 slot3 is structurally present but
    /// unreachable by canonical selector control flow.
    /// </summary>
    public static VirgoGoldAttackSelection SelectGoldAttack(byte activeSaint0533, byte phaseAccumulator065F)
    {
        ValidateSaint(activeSaint0533);
        if (activeSaint0533 == IkkiIndex)
            return new(0x02, 37, 22);

        var slot = (byte)(phaseAccumulator065F & 0x01);
        return slot == 0
            ? new(0x00, 42, 28)
            : new(0x01, 24, 36);
    }

    public static VirgoReleaseOwner ResolveReleaseOwner(VirgoStage05State state) => state.Release0670 switch
    {
        0x00 => VirgoReleaseOwner.ActiveBattle,
        VictoryRelease => VirgoReleaseOwner.GenericVictory,
        PlatformDetourRelease when state.PlatformSubstate02 == 0x0D => VirgoReleaseOwner.PlatformSubstate0D,
        IntroRelease => VirgoReleaseOwner.IntroHandoff,
        HighPresentationRelease => VirgoReleaseOwner.HighPresentation90,
        StageAdvanceRelease => VirgoReleaseOwner.StageAdvanceForceSeiya,
        DefeatRelease => VirgoReleaseOwner.GenericDefeat,
        _ => throw new InvalidOperationException($"Unsupported canonical Virgo release ${state.Release0670:X2}.")
    };

    private static void EnsureActive(VirgoStage05State state)
    {
        if (state.Release0670 != 0)
            throw new InvalidOperationException("Virgo initialization expects active $0670=0.");
    }

    private static void EnsureTurnActive(VirgoStage05State state)
    {
        if (state.Release0670 != 0)
            throw new InvalidOperationException("Virgo turn logic requires active $0670=0.");
    }

    private static void ValidateCondition(byte value, string name)
    {
        if (value is not (0x00 or 0x01 or 0xFF))
            throw new ArgumentOutOfRangeException(name, value, "Battle condition must be $00, $01 or $FF.");
    }

    private static void ValidateSaint(byte value)
    {
        if (value > IkkiIndex)
            throw new ArgumentOutOfRangeException(nameof(value), value, "Canonical battle Saint index is 0..4.");
    }
}

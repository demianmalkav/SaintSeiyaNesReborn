using SaintSeiyaNesReborn.OriginalSpec.Platform;

namespace SaintSeiyaNesReborn.OriginalSpec;

public enum ScorpioStage06TalkOutcome
{
    LowHistoryHyogaReward300,
    LowHistoryHyogaRepeat,
    LowHistoryOrdinary,
    HighHistoryFirstReward200,
    HighHistoryRepeatHyoga,
    HighHistoryRepeatForcesCounterattack
}

public enum ScorpioStage06PostBronzeOutcome
{
    ContinueNoStageFeedback,
    ContinueHitFeedback,
    VictoryRelease01
}

public enum ScorpioStage06PostGoldOutcome
{
    ContinueHealthy,
    LowPlayerFeedback,
    DefeatReleaseFF
}

public readonly record struct ScorpioStage06State(
    byte ActiveSaint0533,
    byte StoryProgress067D,
    byte Stage050E,
    byte StoryDescriptor06CD,
    byte StoryMarker0673,
    byte TalkMilestone066F,
    byte DodgeHistory0677,
    byte DodgeHistory0678,
    byte HyogaRewardLatch068A,
    byte Release0670);

public readonly record struct ScorpioStage06TalkResult(
    ScorpioStage06State State,
    ScorpioStage06TalkOutcome Outcome,
    byte Message1,
    byte Message2,
    int SeventhSenseReward,
    bool ForceGoldCounterattack);

public readonly record struct ScorpioStage06PostBronzeResult(
    ScorpioStage06State State,
    ScorpioStage06PostBronzeOutcome Outcome);

public readonly record struct ScorpioStage06PostGoldResult(
    ScorpioStage06State State,
    ScorpioStage06PostGoldOutcome Outcome);

public readonly record struct ScorpioStage06Progress08Bridge(
    byte ActiveSaint0533,
    byte StoryProgress067D,
    byte StoryStage050E,
    byte StoryDescriptor06CD,
    byte StoryMarker0673,
    byte InheritedRelease0670,
    byte PlatformSubstate02);

public readonly record struct ScorpioStage06CapricornBoundary(
    byte ActiveSaint0533,
    byte StoryProgress067D,
    byte Stage050E,
    byte StoryDescriptor06CD,
    byte StoryMarker0673);

public readonly record struct ScorpioStage06BridgeResult(
    PlatformExitTransitionKind PlatformTransition,
    bool Stage10AutoRelease01,
    ScorpioStage06CapricornBoundary Boundary);

/// <summary>
/// Canonical composed model for story progress $067D=$07 / battle stage
/// $050E=$06 (Scorpio / Milo), including the mandatory success bridge through
/// progress $08 / story-stage $10 / principal platform substate $08 to the
/// exact Capricorn boundary at progress $09 / stage $07.
///
/// Generic battle arithmetic remains owned by the shared specifications. This
/// context owns only Scorpio-local Talk/reward state, post-action terminals,
/// its dodge-history Gold selector and the fixed progression composition.
/// Story-stage $050E=$10 is intentionally kept distinct from platform RAM
/// $02=$10; this bridge uses principal platform substate $02=$08.
/// </summary>
public static class ScorpioStage06Context
{
    public const byte StageIndex = 0x06;
    public const byte StoryProgressSeed = 0x07;
    public const byte StoryDescriptor06CD = 0x00;
    public const byte StoryMarker0673 = 0x30;

    public const byte SeiyaIndex = 0x00;
    public const byte HyogaIndex = 0x01;
    public const byte ShunIndex = 0x02;
    public const byte ShiryuIndex = 0x03;

    public const byte VictoryRelease = 0x01;
    public const byte DefeatRelease = 0xFF;

    public const ushort InitializationHandler = 0x9ACE;
    public const ushort TalkHandler = 0x9E51;
    public const ushort DodgeHistoryHelper = 0xA1EC;
    public const ushort RewardHelper = 0xA1FF;
    public const ushort PostBronzeHandler = 0xA7FF;
    public const ushort PostGoldHandler = 0xA847;
    public const ushort GoldSelectorHandler = 0x908C;
    public const ushort CommonBattleReset = 0xA973;

    public const byte LowHistoryHyogaMessage1 = 0x84;
    public const byte LowHistoryHyogaMessage2 = 0x85;
    public const byte LowHistoryOrdinaryMessage1 = 0xF8;
    public const byte LowHistoryOrdinaryMessage2 = 0x3E;
    public const byte HighHistoryCommonMessage2 = 0xA3;
    public const byte PostBronzeHitMessage = 0xA6;
    public const byte PostGoldLowMessage1 = 0xA4;
    public const byte PostGoldLowMessage2 = 0x91;

    public const int HyogaLowHistoryReward = 300;
    public const int FirstHighHistoryReward = 200;

    public const byte Progress08 = 0x08;
    public const byte Progress08StoryStage10 = 0x10;
    public const byte Progress08StoryDescriptor06CD = 0x00;
    public const byte Progress08StoryMarker0673 = 0x30;
    public const byte Progress08PrincipalPlatformSubstate = 0x08;
    public const ushort ProgressToPlatformOwnerE4D7 = 0xE4D7;
    public const ushort StoryStage10AutoReleaseOwnerE2DD = 0xE2DD;

    public const byte CapricornProgress = 0x09;
    public const byte CapricornStage = 0x07;
    public const byte CapricornStoryDescriptor06CD = 0x00;
    public const byte CapricornStoryMarker0673 = 0x30;

    private static readonly byte[] HighHistorySaintMessages = [0x86, 0x86, 0x87, 0x86];

    public static ScorpioStage06State CreateCanonicalEntry(byte activeSaint0533 = SeiyaIndex)
    {
        ValidateReachableSaint(activeSaint0533);
        return new(
            ActiveSaint0533: activeSaint0533,
            StoryProgress067D: StoryProgressSeed,
            Stage050E: StageIndex,
            StoryDescriptor06CD: StoryDescriptor06CD,
            StoryMarker0673: StoryMarker0673,
            TalkMilestone066F: 0,
            DodgeHistory0677: 0,
            DodgeHistory0678: 0,
            HyogaRewardLatch068A: 0,
            Release0670: 0);
    }

    /// <summary>
    /// Bank-5 $9ACE is exactly RTS. Scorpio has no dedicated initializer reward,
    /// presentation or internal release handoff.
    /// </summary>
    public static ScorpioStage06State ApplyInitialization(ScorpioStage06State state)
    {
        EnsureScorpioActive(state);
        return state;
    }

    public static ScorpioStage06State WithDodgeHistory(
        ScorpioStage06State state,
        byte dodgeHistory0677,
        byte dodgeHistory0678)
    {
        EnsureScorpioActive(state);
        return state with
        {
            DodgeHistory0677 = dodgeHistory0677,
            DodgeHistory0678 = dodgeHistory0678
        };
    }

    /// <summary>
    /// Bank-5 $9E51. Helper $A1EC returns the 8-bit 6502 sum
    /// $0677+$0678. Below two dodges, Hyoga owns a one-time $068A-gated +300
    /// reward while other Saints receive ordinary dialogue. At two or more,
    /// the first Talk sets $066F and grants +200; repeated high-history Talk
    /// forces the Gold response only for non-Hyoga Saints.
    /// </summary>
    public static ScorpioStage06TalkResult ExecuteTalk(ScorpioStage06State state)
    {
        EnsureScorpioActive(state);
        var total = DodgeHistoryByteSum(state.DodgeHistory0677, state.DodgeHistory0678);

        if (total < 2)
        {
            if (state.ActiveSaint0533 == HyogaIndex)
            {
                if (state.HyogaRewardLatch068A == 0)
                {
                    return new(
                        state with { HyogaRewardLatch068A = 1 },
                        ScorpioStage06TalkOutcome.LowHistoryHyogaReward300,
                        LowHistoryHyogaMessage1,
                        LowHistoryHyogaMessage2,
                        HyogaLowHistoryReward,
                        ForceGoldCounterattack: false);
                }

                return new(
                    state,
                    ScorpioStage06TalkOutcome.LowHistoryHyogaRepeat,
                    LowHistoryHyogaMessage1,
                    LowHistoryHyogaMessage2,
                    SeventhSenseReward: 0,
                    ForceGoldCounterattack: false);
            }

            return new(
                state,
                ScorpioStage06TalkOutcome.LowHistoryOrdinary,
                LowHistoryOrdinaryMessage1,
                LowHistoryOrdinaryMessage2,
                SeventhSenseReward: 0,
                ForceGoldCounterattack: false);
        }

        var saintMessage = HighHistorySaintMessages[state.ActiveSaint0533];
        if (state.TalkMilestone066F == 0)
        {
            return new(
                state with { TalkMilestone066F = 1 },
                ScorpioStage06TalkOutcome.HighHistoryFirstReward200,
                saintMessage,
                HighHistoryCommonMessage2,
                FirstHighHistoryReward,
                ForceGoldCounterattack: false);
        }

        if (state.ActiveSaint0533 == HyogaIndex)
        {
            return new(
                state,
                ScorpioStage06TalkOutcome.HighHistoryRepeatHyoga,
                saintMessage,
                HighHistoryCommonMessage2,
                SeventhSenseReward: 0,
                ForceGoldCounterattack: false);
        }

        return new(
            state,
            ScorpioStage06TalkOutcome.HighHistoryRepeatForcesCounterattack,
            saintMessage,
            HighHistoryCommonMessage2,
            SeventhSenseReward: 0,
            ForceGoldCounterattack: true);
    }

    /// <summary>
    /// Bank-5 $A7FF after shared generic helpers. Only $EB=$FF is terminal.
    /// Other classified states continue; a nonzero $06BC hit token adds message
    /// $A6, while zero returns without Scorpio-local feedback.
    /// </summary>
    public static ScorpioStage06PostBronzeResult AfterBronzeAction(
        ScorpioStage06State state,
        byte opponentConditionEb,
        bool bronzeAttackHit)
    {
        EnsureScorpioActive(state);
        ValidateBattleCondition(opponentConditionEb, nameof(opponentConditionEb));

        if (opponentConditionEb == 0xFF)
        {
            return new(
                state with { Release0670 = VictoryRelease },
                ScorpioStage06PostBronzeOutcome.VictoryRelease01);
        }

        return new(
            state,
            bronzeAttackHit
                ? ScorpioStage06PostBronzeOutcome.ContinueHitFeedback
                : ScorpioStage06PostBronzeOutcome.ContinueNoStageFeedback);
    }

    /// <summary>
    /// Bank-5 $A847 after shared player classifier $AD4D.
    /// Healthy state returns, low state presents $A4/$91, and defeat emits the
    /// generic $FF release. No stage-local one-shot low-player latch exists.
    /// </summary>
    public static ScorpioStage06PostGoldResult AfterGoldResponse(
        ScorpioStage06State state,
        byte playerConditionEa)
    {
        EnsureScorpioActive(state);
        ValidateBattleCondition(playerConditionEa, nameof(playerConditionEa));

        return playerConditionEa switch
        {
            0x00 => new(state, ScorpioStage06PostGoldOutcome.ContinueHealthy),
            0x01 => new(state, ScorpioStage06PostGoldOutcome.LowPlayerFeedback),
            0xFF => new(state with { Release0670 = DefeatRelease }, ScorpioStage06PostGoldOutcome.DefeatReleaseFF),
            _ => throw new InvalidOperationException("unreachable validated Scorpio player condition")
        };
    }

    /// <summary>
    /// Bank-6 $908C-$90A5. The same 8-bit dodge-history sum used by Talk selects
    /// exactly slot 1 below two and slot 0 at two or more.
    /// </summary>
    public static byte SelectGoldSlot(byte dodgeHistory0677, byte dodgeHistory0678) =>
        DodgeHistoryByteSum(dodgeHistory0677, dodgeHistory0678) < 2 ? (byte)1 : (byte)0;

    /// <summary>
    /// Generic $FF retry leaves progress $07 unchanged and normal re-entry calls
    /// common reset $A973, clearing $066F/$0677/$0678/$068A/$0670. Both Talk
    /// reward gates and the dodge-history selector therefore restart from their
    /// canonical Scorpio seed.
    /// </summary>
    public static ScorpioStage06State ResetForRetryAfterDefeat(
        ScorpioStage06State defeatedState,
        byte? activeSaintForRetry = null)
    {
        EnsureScorpio(defeatedState);
        if (defeatedState.Release0670 != DefeatRelease)
            throw new InvalidOperationException("Scorpio retry reset requires defeat release $FF.");

        return CreateCanonicalEntry(activeSaintForRetry ?? defeatedState.ActiveSaint0533);
    }

    /// <summary>
    /// Fixed $E399/$E3B3 consumes Scorpio release $01 at progress $07 and moves
    /// to progress $08. $F016[08]=$10 reconstructs story-stage $050E=$10 and
    /// $E50B[08]=$00 restores descriptor $06CD=$00. Fixed $E4D7 indexes its
    /// progress table with $08 and writes principal platform substate $02=$08.
    /// </summary>
    public static ScorpioStage06Progress08Bridge ResolveProgress08BridgeAfterVictory(
        ScorpioStage06State terminalState)
    {
        EnsureScorpio(terminalState);
        if (terminalState.Release0670 != VictoryRelease)
            throw new InvalidOperationException("Scorpio successor bridge requires victory release $01.");

        return new(
            terminalState.ActiveSaint0533,
            StoryProgress067D: Progress08,
            StoryStage050E: Progress08StoryStage10,
            StoryDescriptor06CD: Progress08StoryDescriptor06CD,
            StoryMarker0673: Progress08StoryMarker0673,
            InheritedRelease0670: VictoryRelease,
            PlatformSubstate02: Progress08PrincipalPlatformSubstate);
    }

    /// <summary>
    /// Reuses the already-closed principal platform exit gate for substate $08.
    /// Accepted exit requires X >= $D0, Y == $40 and jump phase zero, producing
    /// the normal State3DReload. Reload at progress $08 reconstructs $050E=$10;
    /// fixed $E2DD recognizes exactly that story-stage, synthesizes release $01,
    /// and fixed progression advances to exact Capricorn progress $09/stage $07.
    /// </summary>
    public static ScorpioStage06BridgeResult? ResolveCapricornBoundary(
        ScorpioStage06Progress08Bridge bridge,
        int playerX,
        int playerY,
        int jumpPhase)
    {
        ValidateReachableSaint(bridge.ActiveSaint0533);
        if (bridge.StoryProgress067D != Progress08
            || bridge.StoryStage050E != Progress08StoryStage10
            || bridge.StoryDescriptor06CD != Progress08StoryDescriptor06CD
            || bridge.StoryMarker0673 != Progress08StoryMarker0673
            || bridge.InheritedRelease0670 != VictoryRelease
            || bridge.PlatformSubstate02 != Progress08PrincipalPlatformSubstate)
        {
            throw new InvalidOperationException("Capricorn bridge requires canonical progress-$08 / stage-$10 / platform-$08 state.");
        }

        // Principal substate $08 has no Saint-specific exit restriction, so the
        // exact enum identity is immaterial to this gate; zero is a valid input.
        var transition = PlatformExitGate.Evaluate(
            Progress08PrincipalPlatformSubstate,
            (PlatformSaintIndex)0,
            playerX,
            playerY,
            jumpPhase);

        if (!transition.HasValue)
            return null;

        if (transition.Value != PlatformExitTransitionKind.State3DReload)
            throw new InvalidOperationException("Principal platform $08 must use normal State3DReload.");

        var boundary = new ScorpioStage06CapricornBoundary(
            ActiveSaint0533: bridge.ActiveSaint0533,
            StoryProgress067D: CapricornProgress,
            Stage050E: CapricornStage,
            StoryDescriptor06CD: CapricornStoryDescriptor06CD,
            StoryMarker0673: CapricornStoryMarker0673);

        return new(
            PlatformTransition: transition.Value,
            Stage10AutoRelease01: true,
            Boundary: boundary);
    }

    public static bool IsReachableEntrySaint(byte activeSaint0533) => activeSaint0533 <= ShiryuIndex;

    private static byte DodgeHistoryByteSum(byte dodgeHistory0677, byte dodgeHistory0678) =>
        unchecked((byte)(dodgeHistory0677 + dodgeHistory0678));

    private static void EnsureScorpio(ScorpioStage06State state)
    {
        ValidateReachableSaint(state.ActiveSaint0533);
        if (state.StoryProgress067D != StoryProgressSeed
            || state.Stage050E != StageIndex
            || state.StoryDescriptor06CD != StoryDescriptor06CD
            || state.StoryMarker0673 != StoryMarker0673)
        {
            throw new InvalidOperationException("State is outside canonical Scorpio progress/stage boundary.");
        }
    }

    private static void EnsureScorpioActive(ScorpioStage06State state)
    {
        EnsureScorpio(state);
        if (state.Release0670 != 0)
            throw new InvalidOperationException("Scorpio command/post-action state must have no pending release.");
    }

    private static void ValidateReachableSaint(byte activeSaint0533)
    {
        if (!IsReachableEntrySaint(activeSaint0533))
            throw new ArgumentOutOfRangeException(nameof(activeSaint0533), activeSaint0533, "Scorpio permits only Seiya/Hyoga/Shun/Shiryu.");
    }

    private static void ValidateBattleCondition(byte condition, string parameterName)
    {
        if (condition is not (0x00 or 0x01 or 0xFF))
            throw new ArgumentOutOfRangeException(parameterName, condition, "Shared battle classifier condition must be $00/$01/$FF.");
    }
}

using SaintSeiyaNesReborn.OriginalSpec.Platform;

namespace SaintSeiyaNesReborn.OriginalSpec;

public enum CapricornStage07InitializationOutcome
{
    CallerBypassNonShiryu,
    CallerBypassAlreadyInitialized,
    ReleaseFeGuardNoOp,
    ShiryuUnlockReward600Release03
}

public enum CapricornStage07TalkOutcome
{
    FirstConversation,
    RepeatConversationForcesCounterattack
}

public enum CapricornStage07PostBronzeOutcome
{
    ContinueAfterHit,
    MissFeedback8B,
    OpponentLowShiryuNoForcedMiss,
    OpponentLowArmsForcedMiss0690,
    VictoryReleaseFE
}

public enum CapricornStage07PostGoldOutcome
{
    ContinueHealthy,
    LowPlayerFeedback,
    DefeatReleaseFF
}

public readonly record struct CapricornStage07State(
    byte ActiveSaint0533,
    byte StoryProgress067D,
    byte Stage050E,
    byte StoryDescriptor06CD,
    byte StoryMarker0673,
    byte Conversation066F,
    byte Release0670,
    byte ForcedMiss0690,
    byte StoryScratch0672,
    byte IntroDone068E,
    byte ShiryuTechniqueCount058A,
    byte ActiveTechniqueCount0696,
    byte BossDefeated06B1);

public readonly record struct CapricornStage07InitializationResult(
    CapricornStage07State State,
    CapricornStage07InitializationOutcome Outcome,
    int SeventhSenseReward);

public readonly record struct CapricornStage07TalkResult(
    CapricornStage07State State,
    CapricornStage07TalkOutcome Outcome,
    byte CommonMessage,
    byte SaintMessage,
    bool ForceGoldCounterattack);

public readonly record struct CapricornStage07PostBronzeResult(
    CapricornStage07State State,
    CapricornStage07PostBronzeOutcome Outcome,
    int SeventhSenseReward);

public readonly record struct CapricornStage07PostGoldResult(
    CapricornStage07State State,
    CapricornStage07PostGoldOutcome Outcome);

public readonly record struct CapricornStage07RetryResult(
    CapricornStage07State State,
    byte PlatformSubstate02,
    PlatformExitTransitionKind PlatformTransition,
    bool CommonResetApplied,
    bool InitializationReexecuted);

public readonly record struct CapricornStage07AquariusBoundary(
    byte SavedWinningSaint0533,
    byte ActiveSaint0533,
    byte StoryProgress067D,
    byte Stage050E,
    byte StoryDescriptor06CD,
    byte StoryMarker0673);

/// <summary>
/// Canonical stage-local model for story progress $067D=$09 / battle stage
/// $050E=$07 (Capricorn / Shura).
///
/// The model composes the outer bank-5 battle-entry gate at $970A+ with the
/// stage handler $9ACF: $F36F[$07]=$03 means only a fresh Shiryu entry dispatches
/// the Capricorn initializer. Seiya/Hyoga/Shun bypass it. Generic damage,
/// classifiers, resource arithmetic and platform physics remain owned by the
/// existing shared specifications.
///
/// The model also closes the defeat route through principal platform $09 and
/// proves that warm reload $E100 -> $ED57/$A973 resumes at $E33D without
/// re-running $9ACF, so the +600 reward / Shiryu technique unlock cannot be
/// farmed by intentional defeat. Scripted victory release $FE is composed only
/// to the exact already-closed Aquarius story boundary.
/// </summary>
public static class CapricornStage07Context
{
    public const byte StageIndex = 0x07;
    public const byte StoryProgressSeed = 0x09;
    public const byte StoryDescriptor06CD = 0x00;
    public const byte StoryMarker0673 = 0x30;

    public const byte SeiyaIndex = 0x00;
    public const byte HyogaIndex = 0x01;
    public const byte ShunIndex = 0x02;
    public const byte ShiryuIndex = 0x03;

    public const byte IntroRelease = 0x03;
    public const byte VictoryRelease = 0xFE;
    public const byte DefeatRelease = 0xFF;

    public const ushort BattleEntryOwner = 0x970A;
    public const ushort InitEligibilityCheck = 0x9770;
    public const ushort InitSaintTable = 0xF36F;
    public const byte InitSaintTableStage07Value = ShiryuIndex;
    public const ushort InitializationHandler = 0x9ACF;
    public const byte TemporaryIntroStage = 0x0F;
    public const ushort SharedIntroHandoff = 0x9C3D;

    public const ushort TalkHandler = 0x9ED6;
    public const ushort PostBronzeHandler = 0xA86B;
    public const ushort PostGoldHandler = 0xA8D8;
    public const ushort GenericGoldParitySelector = 0x913E;
    public const ushort FixedForcedMissConsumer = 0xFAB9;
    public const ushort CommonBattleReset = 0xA973;

    public const byte TalkCommonMessage = 0xAE;
    public const byte PostBronzeMissMessage = 0x8B;
    public const byte PostGoldLowMessage1 = 0x40;
    public const byte PostGoldLowMessage2 = 0x91;

    public const int ShiryuInitializationReward = 600;
    public const int ScriptedVictoryReward = 800;

    public const byte RetryPlatformSubstate = 0x09;
    public const ushort ProgressToPlatformOwner = 0xE4D7;
    public const ushort WarmReloadOwner = 0xE100;
    public const ushort WarmPhaseResetOwner = 0xED57;
    public const ushort WarmRetryCommandLoop = 0xE33D;

    public const ushort ReleaseFeOwner = 0xE3ED;
    public const byte AquariusStoryProgress = 0x0A;
    public const byte AquariusStoryDescriptor06CD = 0x08;
    public const byte AquariusStoryMarker0673 = 0x38;

    private static readonly byte[] TalkSaintMessages = [0x43, 0x43, 0x43, 0xAF];

    public static CapricornStage07State CreateCanonicalEntry(byte activeSaint0533 = ShiryuIndex)
    {
        ValidateReachableSaint(activeSaint0533);
        var activeTechniqueCount = activeSaint0533 == ShiryuIndex ? (byte)1 : (byte)2;

        return new(
            ActiveSaint0533: activeSaint0533,
            StoryProgress067D: StoryProgressSeed,
            Stage050E: StageIndex,
            StoryDescriptor06CD: StoryDescriptor06CD,
            StoryMarker0673: StoryMarker0673,
            Conversation066F: 0,
            Release0670: 0,
            ForcedMiss0690: 0,
            StoryScratch0672: 0,
            IntroDone068E: 0,
            ShiryuTechniqueCount058A: 1,
            ActiveTechniqueCount0696: activeTechniqueCount,
            BossDefeated06B1: 0);
    }

    /// <summary>
    /// Composes outer battle-entry ownership $9770-$97B8 with handler $9ACF.
    /// For stage $07, $F36F[$07]=$03, therefore only a fresh Shiryu entry reaches
    /// the handler. Other reachable Saints bypass it and enter with no reward or
    /// technique mutation. Once $068E is nonzero the caller also skips the handler.
    /// </summary>
    public static CapricornStage07InitializationResult ApplyCanonicalEntryInitialization(
        CapricornStage07State state)
    {
        EnsureCapricorn(state);

        if (state.IntroDone068E != 0)
        {
            return new(
                state,
                CapricornStage07InitializationOutcome.CallerBypassAlreadyInitialized,
                SeventhSenseReward: 0);
        }

        if (state.ActiveSaint0533 != InitSaintTableStage07Value)
        {
            return new(
                state,
                CapricornStage07InitializationOutcome.CallerBypassNonShiryu,
                SeventhSenseReward: 0);
        }

        return ApplyInitializationHandler(state);
    }

    /// <summary>
    /// Exact material effects of bank-5 $9ACF once the outer caller dispatches it.
    /// $0670==$FE returns immediately. Otherwise the handler temporarily presents
    /// stage $0F, clears $0672, increments persistent Shiryu count $058A and the
    /// active count $0696, grants +600 through #$06->$F31E and exits through
    /// shared $9C3D, which produces release $03 / $068E=1.
    /// </summary>
    public static CapricornStage07InitializationResult ApplyInitializationHandler(
        CapricornStage07State state)
    {
        EnsureCapricorn(state);
        if (state.ActiveSaint0533 != ShiryuIndex)
            throw new InvalidOperationException("Capricorn $9ACF is canonically dispatched only for Shiryu by $F36F[$07]=$03.");

        if (state.Release0670 == VictoryRelease)
        {
            return new(
                state,
                CapricornStage07InitializationOutcome.ReleaseFeGuardNoOp,
                SeventhSenseReward: 0);
        }

        if (state.Release0670 is not (0x00 or IntroRelease))
            throw new InvalidOperationException("Capricorn initializer requires active state or the internal intro handoff.");

        return new(
            state with
            {
                StoryScratch0672 = 0,
                ShiryuTechniqueCount058A = Increment(state.ShiryuTechniqueCount058A),
                ActiveTechniqueCount0696 = Increment(state.ActiveTechniqueCount0696),
                Release0670 = IntroRelease,
                IntroDone068E = 1
            },
            CapricornStage07InitializationOutcome.ShiryuUnlockReward600Release03,
            ShiryuInitializationReward);
    }

    public static CapricornStage07State EnterCommandLoop(CapricornStage07State state)
    {
        EnsureCapricorn(state);
        if (state.Release0670 is not (0x00 or IntroRelease))
            throw new InvalidOperationException("Only active Capricorn state or release $03 can enter the command loop.");

        return state with { Release0670 = 0 };
    }

    /// <summary>
    /// Bank-5 $9ED6. First Talk displays $AE plus the per-Saint table
    /// $43/$43/$43/$AF and increments $066F without forcing Gold. Every Talk with
    /// inbound $066F!=0 uses the repeat-form display, increments transient $DC in
    /// the original caller-visible state, increments $066F again and forces Gold.
    /// $066F remains an 8-bit counter rather than a boolean latch.
    /// </summary>
    public static CapricornStage07TalkResult ExecuteTalk(CapricornStage07State state)
    {
        EnsureTurnActive(state);
        var first = state.Conversation066F == 0;
        var nextCounter = Increment(state.Conversation066F);

        return new(
            state with { Conversation066F = nextCounter },
            first
                ? CapricornStage07TalkOutcome.FirstConversation
                : CapricornStage07TalkOutcome.RepeatConversationForcesCounterattack,
            TalkCommonMessage,
            TalkSaintMessages[state.ActiveSaint0533],
            ForceGoldCounterattack: !first);
    }

    /// <summary>
    /// Bank-5 $A86B after shared opponent classifier $ACD6.
    /// $EB=$00 consumes the already-computed $06BC hit token; zero emits $8B.
    /// $EB=$01 arms $0690=$FF for every reachable Saint except Shiryu.
    /// $EB=$FF owns the scripted victory, writes $06B1=$FF, grants +800 and
    /// emits release $FE.
    /// </summary>
    public static CapricornStage07PostBronzeResult AfterBronzeAction(
        CapricornStage07State state,
        byte opponentConditionEb,
        bool bronzeAttackHit)
    {
        EnsureTurnActive(state);
        ValidateBattleCondition(opponentConditionEb, nameof(opponentConditionEb));

        if (opponentConditionEb == 0xFF)
        {
            return new(
                state with
                {
                    BossDefeated06B1 = 0xFF,
                    Release0670 = VictoryRelease
                },
                CapricornStage07PostBronzeOutcome.VictoryReleaseFE,
                ScriptedVictoryReward);
        }

        if (opponentConditionEb == 0x01)
        {
            if (state.ActiveSaint0533 == ShiryuIndex)
            {
                return new(
                    state,
                    CapricornStage07PostBronzeOutcome.OpponentLowShiryuNoForcedMiss,
                    SeventhSenseReward: 0);
            }

            return new(
                state with { ForcedMiss0690 = 0xFF },
                CapricornStage07PostBronzeOutcome.OpponentLowArmsForcedMiss0690,
                SeventhSenseReward: 0);
        }

        return new(
            state,
            bronzeAttackHit
                ? CapricornStage07PostBronzeOutcome.ContinueAfterHit
                : CapricornStage07PostBronzeOutcome.MissFeedback8B,
            SeventhSenseReward: 0);
    }

    /// <summary>
    /// Fixed $FAB9-$FAE5 consumes $0690 before generic hit-token logic. Nonzero
    /// $0690 forces A=0 and stores $06BC=0. If $0690 is zero, the shared battle
    /// setup remains authoritative, so this compositor passes its supplied token
    /// through unchanged rather than duplicating that calculation.
    /// </summary>
    public static byte ApplyFixed0690Consumer(CapricornStage07State state, byte genericHitToken06BC)
    {
        EnsureCapricorn(state);
        return state.ForcedMiss0690 != 0 ? (byte)0 : genericHitToken06BC;
    }

    /// <summary>
    /// Bank-5 $A8D8 after shared player classifier $AD4D.
    /// $EA=$00 returns; $EA=$01 presents repeatable $40/$91 feedback; $EA=$FF
    /// emits the generic defeat release $FF. No Capricorn-local low-player latch
    /// exists.
    /// </summary>
    public static CapricornStage07PostGoldResult AfterGoldResponse(
        CapricornStage07State state,
        byte playerConditionEa)
    {
        EnsureTurnActive(state);
        ValidateBattleCondition(playerConditionEa, nameof(playerConditionEa));

        return playerConditionEa switch
        {
            0x00 => new(state, CapricornStage07PostGoldOutcome.ContinueHealthy),
            0x01 => new(state, CapricornStage07PostGoldOutcome.LowPlayerFeedback),
            0xFF => new(state with { Release0670 = DefeatRelease }, CapricornStage07PostGoldOutcome.DefeatReleaseFF),
            _ => throw new InvalidOperationException("unreachable validated Capricorn player condition")
        };
    }

    /// <summary>
    /// Stage $07 has no dedicated Gold-selector branch and falls through bank-6
    /// $913E: slot = $065F & 1.
    /// </summary>
    public static byte SelectGoldSlot(byte field065F) => (byte)(field065F & 0x01);

    /// <summary>
    /// Exact generic defeat retry composition.
    ///
    /// $FF keeps story progress $09. Fixed $E4D7 therefore selects principal
    /// platform substate $09. After the common $D0/$40/jump=0 exit, warm reload
    /// reaches $ED57, which calls $A973 for non-$02/$03 releases. $A973 clears
    /// $066F/$0670/$068E/$0690 and related encounter counters. The crucial
    /// continuation is $E2DD->$E33D: the engine resumes the battle command loop
    /// directly and does not call $970A/$97DB/$9ACF again. Persistent/active
    /// technique counts and $0672 therefore survive unchanged, and no second
    /// +600 reward occurs.
    /// </summary>
    public static CapricornStage07RetryResult? ResolveRetryAfterDefeat(
        CapricornStage07State defeatedState,
        int playerX,
        int playerY,
        int jumpPhase)
    {
        EnsureCapricorn(defeatedState);
        if (defeatedState.Release0670 != DefeatRelease)
            throw new InvalidOperationException("Capricorn retry requires defeat release $FF.");

        var transition = PlatformExitGate.Evaluate(
            RetryPlatformSubstate,
            (PlatformSaintIndex)0,
            playerX,
            playerY,
            jumpPhase);

        if (!transition.HasValue)
            return null;
        if (transition.Value != PlatformExitTransitionKind.State3DReload)
            throw new InvalidOperationException("Principal platform $09 must use normal State3DReload.");

        var retryState = defeatedState with
        {
            Conversation066F = 0,
            Release0670 = 0,
            ForcedMiss0690 = 0,
            IntroDone068E = 0
        };

        return new(
            State: retryState,
            PlatformSubstate02: RetryPlatformSubstate,
            PlatformTransition: transition.Value,
            CommonResetApplied: true,
            InitializationReexecuted: false);
    }

    /// <summary>
    /// Fixed $E3ED-$E414 handles release $FE by saving the winning Saint,
    /// forcing $0533=$00, rewriting release to $01 and joining ordinary story
    /// advancement. From progress $09, $F016[$0A]=$08 and $E50B[$0A]=$08,
    /// producing marker $38. This method stops at that already-closed Aquarius
    /// story boundary and does not execute Aquarius internals.
    /// </summary>
    public static CapricornStage07AquariusBoundary ResolveAquariusBoundaryAfterVictory(
        CapricornStage07State terminalState)
    {
        EnsureCapricorn(terminalState);
        if (terminalState.Release0670 != VictoryRelease || terminalState.BossDefeated06B1 != 0xFF)
            throw new InvalidOperationException("Aquarius handoff requires scripted Capricorn victory release $FE.");

        return new(
            SavedWinningSaint0533: terminalState.ActiveSaint0533,
            ActiveSaint0533: SeiyaIndex,
            StoryProgress067D: AquariusStoryProgress,
            Stage050E: AquariusStage08Context.StageIndex,
            StoryDescriptor06CD: AquariusStoryDescriptor06CD,
            StoryMarker0673: AquariusStoryMarker0673);
    }

    public static bool IsReachableEntrySaint(byte activeSaint0533) => activeSaint0533 <= ShiryuIndex;

    private static void EnsureTurnActive(CapricornStage07State state)
    {
        EnsureCapricorn(state);
        if (state.Release0670 != 0)
            throw new InvalidOperationException("Capricorn command/post-action logic requires active $0670=0.");
    }

    private static void EnsureCapricorn(CapricornStage07State state)
    {
        ValidateReachableSaint(state.ActiveSaint0533);
        if (state.StoryProgress067D != StoryProgressSeed
            || state.Stage050E != StageIndex
            || state.StoryDescriptor06CD != StoryDescriptor06CD)
        {
            throw new InvalidOperationException("State is not the canonical Capricorn progress-$09 / stage-$07 context.");
        }
    }

    private static void ValidateReachableSaint(byte activeSaint0533)
    {
        if (!IsReachableEntrySaint(activeSaint0533))
            throw new ArgumentOutOfRangeException(nameof(activeSaint0533), activeSaint0533, "Capricorn marker $30 permits only Saints 0..3.");
    }

    private static void ValidateBattleCondition(byte value, string parameterName)
    {
        if (value is not (0x00 or 0x01 or 0xFF))
            throw new ArgumentOutOfRangeException(parameterName, value, "Battle condition must be $00, $01 or $FF.");
    }

    private static byte Increment(byte value) => unchecked((byte)(value + 1));
}

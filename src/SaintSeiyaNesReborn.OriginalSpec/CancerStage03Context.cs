using SaintSeiyaNesReborn.OriginalSpec.Platform;

namespace SaintSeiyaNesReborn.OriginalSpec;

public enum CancerStage03TalkOutcome
{
    PhaseZeroPlatformDetourRelease02,
    PhaseOneTalkForcesCounterattack
}

public enum CancerStage03PostBronzeOutcome
{
    ContinueAfterHit,
    MissFeedback,
    OpponentLowFeedback,
    VictoryRelease01
}

public enum CancerStage03PostGoldOutcome
{
    CommonHealthyOrRepeatFeedback,
    FirstPlayerLowFeedback,
    DefeatReleaseFF
}

public readonly record struct CancerStage03State(
    byte ActiveSaint0533,
    byte StoryProgress067D,
    byte Stage050E,
    byte Phase067C,
    byte PlayerLowLatch064D,
    byte Release0670,
    byte IntroDone068E);

public readonly record struct CancerStage03InitResult(
    CancerStage03State State,
    byte TemporaryPresentationStage,
    int SeventhSenseReward);

public readonly record struct CancerStage03TalkResult(
    CancerStage03State State,
    CancerStage03TalkOutcome Outcome,
    byte? PlatformSubstate02,
    bool CallerUnwind,
    bool ForceGoldCounterattack);

public readonly record struct CancerStage03PostBronzeResult(
    CancerStage03State State,
    CancerStage03PostBronzeOutcome Outcome,
    byte OpponentScratch064A,
    bool OpponentScratchIncremented);

public readonly record struct CancerStage03PostGoldResult(
    CancerStage03State State,
    CancerStage03PostGoldOutcome Outcome);

public readonly record struct CancerStage03PlatformResumeResult(
    PlatformSpecialNormalExitResult PlatformExit,
    CancerStage03State State);

public readonly record struct CancerStage03LeoBoundary(
    byte ActiveSaint0533,
    byte StoryProgress067D,
    byte Stage050E,
    byte StoryDescriptor06CD,
    byte StoryMarker0673);

/// <summary>
/// Canonical composed model for story progress $067D=$03 / stage $050E=$03
/// (Cancer / Death Mask).
///
/// Cancer differs from Gemini: the special platform detour is created only by
/// phase-zero Talk. Ordinary Bronze/Gold battle remains reachable before Talk,
/// so phase-zero direct victory is canonical. Talk at $067C=0 writes platform
/// substate $0C and release $02 with a caller unwind; the already-closed $0C
/// platform exit increments $067C to 1 without invoking common reset $A973.
/// Phase-one Talk instead raises transient $DC, whose caller forces a Gold
/// response. Generic resource, damage, dodge and classifier arithmetic remains
/// owned by the existing shared specifications.
/// </summary>
public static class CancerStage03Context
{
    public const byte StageIndex = 0x03;
    public const byte StoryProgressSeed = 0x03;
    public const byte StoryDescriptor06CD = 0x02;
    public const byte StoryMarker0673 = 0x32;

    public const byte TemporaryIntroStage = 0x0E;
    public const byte IntroHandoffRelease = 0x03;
    public const byte PlatformDetourRelease = 0x02;
    public const byte PlatformDetourSubstate = 0x0C;
    public const byte VictoryRelease = 0x01;
    public const byte DefeatRelease = 0xFF;

    public const byte SeiyaIndex = 0x00;
    public const byte ShunIndex = 0x02;
    public const byte ShiryuIndex = 0x03;

    public const byte LeoStoryProgress = 0x04;
    public const byte LeoStage = 0x04;
    public const byte LeoStoryDescriptor06CD = 0x02;
    public const byte LeoStoryMarker0673 = 0x32;

    public const ushort CommonBattleReset = 0xA973;
    public const ushort InitializationHandler = 0x9851;
    public const ushort TalkHandler = 0x9D96;
    public const ushort PostBronzeHandler = 0xA50F;
    public const ushort PostGoldHandler = 0xA560;
    public const ushort PlatformResumeOwner = 0xED57;
    public const ushort ProgressReleaseOwner = 0xE399;
    public const ushort ProgressIncrementOwner = 0xE3B3;

    public const int IntroSeventhSenseReward = 400;
    public const byte PhaseZeroTalkOpeningMessage = 0x56;
    public const byte PhaseZeroTalkClosingMessage = 0x69;
    public const byte PhaseOneTalkMessage1 = 0x58;
    public const byte PhaseOneTalkMessage2 = 0x57;
    public const byte VictoryMessage = 0x89;
    public const byte OpponentLowMessage = 0x8D;
    public const byte MissMessage = 0x3D;
    public const byte FirstPlayerLowMessage = 0x8B;
    public const byte FirstPlayerLowClosingMessage = 0x91;
    public const byte CommonGoldFeedbackMessage = 0x8E;

    /// <summary>
    /// Canonical stage-3 entry after the story owner has reconstructed
    /// $06CD=$02/$0673=$32 and common reset $A973 has cleared the stage-local
    /// phase, player-low latch, release and intro latch. Fixed Saint selection
    /// allows exactly Seiya (0), Shun (2) and Shiryu (3); Hyoga and Ikki are
    /// blocked at this story boundary.
    /// </summary>
    public static CancerStage03State CreateCanonicalEntry(byte activeSaint0533 = SeiyaIndex)
    {
        ValidateReachableSaint(activeSaint0533);
        return new(
            ActiveSaint0533: activeSaint0533,
            StoryProgress067D: StoryProgressSeed,
            Stage050E: StageIndex,
            Phase067C: 0,
            PlayerLowLatch064D: 0,
            Release0670: 0,
            IntroDone068E: 0);
    }

    /// <summary>
    /// Bank-5 $9851. The initializer uses temporary presentation stage $0E,
    /// calls fixed $F31E with packed-BCD #$04 (+400 Seventh Sense), then joins
    /// shared $9C3D, which restores stage $03 and writes release $03/$068E=1.
    /// </summary>
    public static CancerStage03InitResult ApplyInitialization(CancerStage03State state)
    {
        EnsureStage03TurnActive(state);

        return new(
            state with
            {
                Release0670 = IntroHandoffRelease,
                IntroDone068E = 1
            },
            TemporaryPresentationStage: TemporaryIntroStage,
            SeventhSenseReward: IntroSeventhSenseReward);
    }

    /// <summary>
    /// Fixed reload handling consumes the internal intro handoff before normal
    /// command selection. No common reset is performed here.
    /// </summary>
    public static CancerStage03State EnterCommandLoop(CancerStage03State state)
    {
        EnsureStage03(state);
        if (state.Release0670 is not (0x00 or IntroHandoffRelease))
            throw new InvalidOperationException("Only active Cancer state or intro release $03 can enter the command loop.");

        return state with { Release0670 = 0 };
    }

    /// <summary>
    /// Bank-5 $9D96.
    ///
    /// At $067C=0, Talk performs the scripted branch, writes $02=$0C and
    /// $0670=$02, then executes two PLA instructions before RTS. The caller
    /// unwind means this command leaves directly for the platform transition;
    /// it does not raise $DC and does not force a Gold response.
    ///
    /// At $067C!=0, Talk displays selectors $58/$57 and increments transient
    /// $DC. The fixed command caller consumes that latch by forcing the Gold
    /// response path. No persistent Cancer-local counter is mutated.
    /// </summary>
    public static CancerStage03TalkResult ExecuteTalk(CancerStage03State state)
    {
        EnsureStage03TurnActive(state);

        if (state.Phase067C == 0)
        {
            return new(
                state with { Release0670 = PlatformDetourRelease },
                CancerStage03TalkOutcome.PhaseZeroPlatformDetourRelease02,
                PlatformSubstate02: PlatformDetourSubstate,
                CallerUnwind: true,
                ForceGoldCounterattack: false);
        }

        return new(
            state,
            CancerStage03TalkOutcome.PhaseOneTalkForcesCounterattack,
            PlatformSubstate02: null,
            CallerUnwind: false,
            ForceGoldCounterattack: true);
    }

    /// <summary>
    /// Composes phase-zero Talk with the already-closed special-normal platform
    /// $0C exit. The gate/reload implementation remains owned by
    /// PlatformSpecialNormalExitPipeline: accepted exit requires X >= $88,
    /// Y == $20 and jump phase 0. Release $02 reaches fixed $ED57/$ED8F,
    /// increments $067C 0->1 and skips common reset $A973. The selected Cancer
    /// Saint, $064D latch and intro latch therefore survive the special resume.
    /// </summary>
    public static CancerStage03PlatformResumeResult? ResolvePlatformDetourExit(
        CancerStage03State detourState,
        int playerX,
        int playerY,
        int jumpPhase)
    {
        EnsureStage03(detourState);
        if (detourState.Release0670 != PlatformDetourRelease || detourState.Phase067C != 0)
            throw new InvalidOperationException("Platform $0C composition requires canonical Cancer phase-zero release $02.");

        var exit = PlatformSpecialNormalExitPipeline.ResolveAcceptedExit(
            PlatformDetourSubstate,
            ToPlatformSaint(detourState.ActiveSaint0533),
            playerX,
            playerY,
            jumpPhase,
            flags0673: StoryMarkerWithActiveSaint(detourState.ActiveSaint0533),
            flags06CC: 0x00);

        if (!exit.HasValue)
            return null;

        var accepted = exit.Value;
        var resumed = detourState with
        {
            Stage050E = accepted.ReloadField050E,
            Phase067C = accepted.Field067C,
            Release0670 = accepted.Terminal0670
        };

        return new(accepted, resumed);
    }

    /// <summary>
    /// Bank-5 $A50F after shared opponent classifier $ACD6.
    /// The handler does not inspect $067C, so all branches are reachable in
    /// phase zero as well as phase one. In particular, phase-zero direct victory
    /// before ever taking the Talk-created platform detour is canonical.
    ///
    /// $064A is classifier scratch rather than a Cancer one-shot latch. On each
    /// $EB=$01 execution the handler performs INC $064A before low-opponent
    /// feedback. The caller supplies the scratch value observed after generic
    /// classification; this model returns the exact stage-local increment without
    /// claiming that generic code preserves it between rounds.
    /// </summary>
    public static CancerStage03PostBronzeResult AfterBronzeAction(
        CancerStage03State state,
        byte opponentConditionEb,
        bool bronzeAttackHit,
        byte opponentScratch064A = 0)
    {
        EnsureStage03TurnActive(state);
        ValidateCondition(opponentConditionEb, nameof(opponentConditionEb));

        if (opponentConditionEb == 0xFF)
        {
            return new(
                state with { Release0670 = VictoryRelease },
                CancerStage03PostBronzeOutcome.VictoryRelease01,
                opponentScratch064A,
                OpponentScratchIncremented: false);
        }

        if (opponentConditionEb == 0x01)
        {
            return new(
                state,
                CancerStage03PostBronzeOutcome.OpponentLowFeedback,
                unchecked((byte)(opponentScratch064A + 1)),
                OpponentScratchIncremented: true);
        }

        if (!bronzeAttackHit)
        {
            return new(
                state,
                CancerStage03PostBronzeOutcome.MissFeedback,
                opponentScratch064A,
                OpponentScratchIncremented: false);
        }

        return new(
            state,
            CancerStage03PostBronzeOutcome.ContinueAfterHit,
            opponentScratch064A,
            OpponentScratchIncremented: false);
    }

    /// <summary>
    /// Bank-5 $A560 after shared player classifier $AD4D.
    /// $EA=$FF emits defeat release $FF. The first $EA=$01 while $064D=0 runs
    /// the special low-player presentation and increments $064D to 1. Later low
    /// states and healthy $EA=$00 share the common feedback branch.
    /// </summary>
    public static CancerStage03PostGoldResult AfterGoldResponse(
        CancerStage03State state,
        byte playerConditionEa)
    {
        EnsureStage03TurnActive(state);
        ValidateCondition(playerConditionEa, nameof(playerConditionEa));

        if (playerConditionEa == 0xFF)
        {
            return new(
                state with { Release0670 = DefeatRelease },
                CancerStage03PostGoldOutcome.DefeatReleaseFF);
        }

        if (playerConditionEa == 0x01 && state.PlayerLowLatch064D == 0)
        {
            return new(
                state with { PlayerLowLatch064D = 1 },
                CancerStage03PostGoldOutcome.FirstPlayerLowFeedback);
        }

        return new(state, CancerStage03PostGoldOutcome.CommonHealthyOrRepeatFeedback);
    }

    /// <summary>
    /// Stage $03 has no dedicated Gold-selector branch. Bank-6 generic fallback
    /// uses slot = $065F & 1, so exactly slots 0 and 1 are canonically reachable.
    /// Coefficients and damage remain owned by the generic battle specification.
    /// </summary>
    public static byte SelectGoldSlot(byte selectorParity065F) =>
        (byte)(selectorParity065F & 0x01);

    /// <summary>
    /// Generic defeat $FF leaves story progress at $03. On normal re-entry,
    /// fixed $ED57 takes the ordinary reset path through $A973, clearing
    /// $067C/$064D/$0670/$068E. Retry therefore returns to phase zero and the
    /// first later Talk can create platform $0C again.
    /// </summary>
    public static CancerStage03State ResetForRetryAfterDefeat(
        CancerStage03State defeatedState,
        byte? activeSaintForRetry = null)
    {
        if (defeatedState.Release0670 != DefeatRelease)
            throw new InvalidOperationException("Retry reset requires canonical Cancer defeat release $FF.");

        var saint = activeSaintForRetry ?? defeatedState.ActiveSaint0533;
        return CreateCanonicalEntry(saint);
    }

    /// <summary>
    /// Ordinary Cancer victory release $01 joins fixed $E399/$E3B3.
    /// $067D advances 03->04; $F016[04]=04 and $E50B[04]=02 reconstruct the
    /// exact Leo boundary $050E=$04/$06CD=$02/$0673=$32. Ikki is unreachable
    /// here, so the fixed release owner preserves active Seiya/Shun/Shiryu.
    /// Victory can arise either before or after the optional Talk/platform detour.
    /// </summary>
    public static CancerStage03LeoBoundary ResolveLeoBoundary(CancerStage03State terminalState)
    {
        EnsureStage03(terminalState);
        if (terminalState.Release0670 != VictoryRelease)
            throw new InvalidOperationException("Leo boundary requires canonical Cancer victory release $01.");

        return new(
            ActiveSaint0533: terminalState.ActiveSaint0533,
            StoryProgress067D: LeoStoryProgress,
            Stage050E: LeoStage,
            StoryDescriptor06CD: LeoStoryDescriptor06CD,
            StoryMarker0673: LeoStoryMarker0673);
    }

    public static bool IsReachableEntrySaint(byte activeSaint0533) =>
        activeSaint0533 is SeiyaIndex or ShunIndex or ShiryuIndex;

    private static byte StoryMarkerWithActiveSaint(byte activeSaint0533)
    {
        ValidateReachableSaint(activeSaint0533);
        return (byte)(StoryMarker0673 | (1 << activeSaint0533));
    }

    private static PlatformSaintIndex ToPlatformSaint(byte canonicalSaint0533)
    {
        ValidateReachableSaint(canonicalSaint0533);
        var internalIndex = canonicalSaint0533 switch
        {
            SeiyaIndex => 0,
            ShunIndex => 1,
            ShiryuIndex => 3,
            _ => throw new InvalidOperationException("Unreachable canonical Cancer Saint.")
        };
        return (PlatformSaintIndex)internalIndex;
    }

    private static void EnsureStage03(CancerStage03State state)
    {
        ValidateReachableSaint(state.ActiveSaint0533);
        if (state.StoryProgress067D != StoryProgressSeed || state.Stage050E != StageIndex)
            throw new InvalidOperationException("Cancer stage-local logic requires canonical progress $03 / stage $03.");
    }

    private static void EnsureActive(CancerStage03State state)
    {
        if (state.Release0670 != 0)
            throw new InvalidOperationException("Cancer stage-local action requires active $0670=0.");
    }

    private static void EnsureStage03TurnActive(CancerStage03State state)
    {
        EnsureStage03(state);
        EnsureActive(state);
    }

    private static void ValidateReachableSaint(byte activeSaint0533)
    {
        if (!IsReachableEntrySaint(activeSaint0533))
            throw new ArgumentOutOfRangeException(nameof(activeSaint0533), activeSaint0533,
                "Canonical Cancer entry permits Seiya/Shun/Shiryu (0,2,3); Hyoga and Ikki are blocked by $0673=$32 selection semantics.");
    }

    private static void ValidateCondition(byte value, string parameterName)
    {
        if (value is not (0x00 or 0x01 or 0xFF))
            throw new ArgumentOutOfRangeException(parameterName, value, "Battle condition must be $00, $01 or $FF.");
    }
}

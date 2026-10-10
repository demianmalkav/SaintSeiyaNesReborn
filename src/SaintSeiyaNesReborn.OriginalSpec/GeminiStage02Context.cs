using SaintSeiyaNesReborn.OriginalSpec.Platform;

namespace SaintSeiyaNesReborn.OriginalSpec;

public enum GeminiStage02TalkOutcome
{
    FirstTalkForcesCounterattack,
    RepeatedTalkForcesCounterattack
}

public enum GeminiStage02PostBronzeOutcome
{
    MandatoryPlatformDetourRelease02,
    ContinueAfterHit,
    MissFeedback,
    OpponentLowFeedback,
    VictoryRelease01
}

public enum GeminiStage02PostGoldOutcome
{
    HealthyFeedback,
    LowFeedback,
    DefeatReleaseFF
}

public enum GeminiStage02PlatformResumeKind
{
    OrdinaryStage02Continuation,
    RedirectedFirstCamus
}

public enum GeminiStage02CancerPath
{
    OrdinaryStage02Victory,
    RedirectedFirstCamusReleaseFE
}

public readonly record struct GeminiStage02State(
    byte ActiveSaint0533,
    byte StoryProgress067D,
    byte Stage050E,
    byte Phase067C,
    byte Conversation066F,
    byte Release0670,
    byte IntroDone068E);

public readonly record struct GeminiStage02InitResult(
    GeminiStage02State State,
    byte TemporaryPresentationStage,
    int SeventhSenseReward);

public readonly record struct GeminiStage02TalkResult(
    GeminiStage02State State,
    GeminiStage02TalkOutcome Outcome,
    bool ForceGoldCounterattack);

public readonly record struct GeminiStage02PostBronzeResult(
    GeminiStage02State State,
    GeminiStage02PostBronzeOutcome Outcome,
    bool RequiresPlatformDetour);

public readonly record struct GeminiStage02PostGoldResult(
    GeminiStage02State State,
    GeminiStage02PostGoldOutcome Outcome);

public readonly record struct GeminiStage02PlatformResumeResult(
    PlatformSpecialNormalExitResult PlatformExit,
    GeminiStage02PlatformResumeKind Kind,
    GeminiStage02State? GeminiState,
    AquariusStage08State? FirstCamusState);

public readonly record struct GeminiStage02CancerBoundary(
    byte ActiveSaint0533,
    byte StoryProgress067D,
    byte Stage050E,
    byte StoryDescriptor06CD,
    byte StoryMarker0673,
    GeminiStage02CancerPath Path);

/// <summary>
/// Canonical composed model for story progress $067D=$02 / stage $050E=$02.
///
/// Stage $02 is not a single linear boss encounter. The first Bronze action is
/// intercepted by $A444 while $067C=0 and exits through special platform
/// substate $0E. The already-closed $0E exit pipeline increments $067C and then
/// either resumes ordinary stage $02 or, for active Hyoga, redirects to the
/// already-closed first-Camus phase of Aquarius stage $08.
///
/// Generic resource arithmetic, Bronze/Gold damage, dodge resolution and the
/// existing platform/Aquarius internals stay owned by their established specs.
/// This class owns only stage-$02 state, stage-local handlers and composition.
/// </summary>
public static class GeminiStage02Context
{
    public const byte StageIndex = 0x02;
    public const byte StoryProgressSeed = 0x02;
    public const byte TemporaryIntroStage = 0x11;
    public const byte IntroHandoffRelease = 0x03;
    public const byte PlatformDetourRelease = 0x02;
    public const byte VictoryRelease = 0x01;
    public const byte DefeatRelease = 0xFF;
    public const byte PlatformDetourSubstate = 0x0E;

    public const byte SeiyaIndex = 0x00;
    public const byte HyogaIndex = 0x01;
    public const byte ShunIndex = 0x02;
    public const byte ShiryuIndex = 0x03;

    public const byte InitialStoryDescriptor06CD = 0x00;
    public const byte InitialStoryMarker0673 = 0x30;
    public const byte CancerStoryProgress = 0x03;
    public const byte CancerStage = 0x03;
    public const byte CancerStoryDescriptor06CD = 0x02;
    public const byte CancerStoryMarker0673 = 0x32;

    public const ushort CommonBattleReset = 0xA973;
    public const ushort InitializationHandler = 0x981F;
    public const ushort TalkHandler = 0x9D81;
    public const ushort PostBronzeHandler = 0xA444;
    public const ushort PostGoldHandler = 0xA4CC;
    public const ushort PlatformResumeOwner = 0xED57;
    public const ushort ProgressReleaseOwner = 0xE399;
    public const ushort ProgressIncrementOwner = 0xE3B3;

    public const byte IntroSharedMessage = 0x4D;
    public const byte IntroClosingMessage = 0x4C;
    public const byte TalkSharedMessage = 0x45;
    public const byte FirstTalkExtraMessage = 0x43;
    public const int IntroSeventhSenseReward = 300;

    /// <summary>
    /// Canonical pre-intro entry after story progress $02 has selected stage $02
    /// and common reset $A973 has cleared the stage-local counters.
    /// $E50B[$02]=$00 -> $0673=$30, which permits Saints 0..3 and excludes Ikki.
    /// </summary>
    public static GeminiStage02State CreateCanonicalEntry(byte activeSaint0533 = SeiyaIndex)
    {
        ValidateReachableSaint(activeSaint0533);
        return new(
            ActiveSaint0533: activeSaint0533,
            StoryProgress067D: StoryProgressSeed,
            Stage050E: StageIndex,
            Phase067C: 0,
            Conversation066F: 0,
            Release0670: 0,
            IntroDone068E: 0);
    }

    /// <summary>
    /// Bank-5 $981F. The initializer temporarily presents stage index $11,
    /// grants packed-BCD #$03 through fixed $F31E (+300 Seventh Sense), then
    /// shared $9C3D restores stage $02, writes $0670=$03 and $068E=1.
    /// </summary>
    public static GeminiStage02InitResult ApplyInitialization(GeminiStage02State state)
    {
        EnsureStage02(state);
        EnsureActive(state);

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
    /// Fixed reload handling consumes the internal $03 intro handoff before the
    /// interactive command loop. It does not invoke common reset $A973.
    /// </summary>
    public static GeminiStage02State EnterCommandLoop(GeminiStage02State state)
    {
        EnsureStage02(state);
        if (state.Release0670 is not (0x00 or IntroHandoffRelease))
            throw new InvalidOperationException("Only active stage $02 or intro release $03 can enter the command loop.");

        return state with { Release0670 = 0 };
    }

    /// <summary>
    /// Bank-5 $9D81. Every Talk starts by incrementing transient $DC, so the
    /// fixed caller forces a Gold response on every invocation. Only the first
    /// Talk mutates persistent stage-local state: $066F 0->1 and extra selector
    /// $43. Repeated Talk keeps $066F nonzero.
    /// </summary>
    public static GeminiStage02TalkResult ExecuteTalk(GeminiStage02State state)
    {
        EnsureStage02TurnActive(state);

        if (state.Conversation066F == 0)
        {
            return new(
                state with { Conversation066F = 1 },
                GeminiStage02TalkOutcome.FirstTalkForcesCounterattack,
                ForceGoldCounterattack: true);
        }

        return new(
            state,
            GeminiStage02TalkOutcome.RepeatedTalkForcesCounterattack,
            ForceGoldCounterattack: true);
    }

    /// <summary>
    /// Bank-5 $A444. The $067C test occurs before generic opponent
    /// classification. Therefore with canonical phase 0 the first Bronze action
    /// is always intercepted, even if hypothetical damage would otherwise have
    /// produced $EB=$FF. It writes platform substate $02=$0E and release $02.
    ///
    /// Once $067C!=0, the caller-supplied generic opponent condition and hit
    /// token select the ordinary stage-2 feedback/victory branches.
    /// </summary>
    public static GeminiStage02PostBronzeResult AfterBronzeAction(
        GeminiStage02State state,
        byte opponentConditionEb,
        bool bronzeAttackHit)
    {
        EnsureStage02TurnActive(state);

        if (state.Phase067C == 0)
        {
            return new(
                state with { Release0670 = PlatformDetourRelease },
                GeminiStage02PostBronzeOutcome.MandatoryPlatformDetourRelease02,
                RequiresPlatformDetour: true);
        }

        ValidateCondition(opponentConditionEb, nameof(opponentConditionEb));

        if (opponentConditionEb == 0xFF)
        {
            return new(
                state with { Release0670 = VictoryRelease },
                GeminiStage02PostBronzeOutcome.VictoryRelease01,
                RequiresPlatformDetour: false);
        }

        if (opponentConditionEb == 0x01)
        {
            return new(
                state,
                GeminiStage02PostBronzeOutcome.OpponentLowFeedback,
                RequiresPlatformDetour: false);
        }

        if (!bronzeAttackHit)
        {
            return new(
                state,
                GeminiStage02PostBronzeOutcome.MissFeedback,
                RequiresPlatformDetour: false);
        }

        return new(
            state,
            GeminiStage02PostBronzeOutcome.ContinueAfterHit,
            RequiresPlatformDetour: false);
    }

    /// <summary>
    /// Bank-5 $A4CC after generic Gold attack/dodge/damage. $EA=$FF emits
    /// generic defeat release $FF; $EA=$01 and $00 are nonterminal feedback.
    /// </summary>
    public static GeminiStage02PostGoldResult AfterGoldResponse(
        GeminiStage02State state,
        byte playerConditionEa)
    {
        EnsureStage02TurnActive(state);
        ValidateCondition(playerConditionEa, nameof(playerConditionEa));

        return playerConditionEa switch
        {
            0xFF => new(
                state with { Release0670 = DefeatRelease },
                GeminiStage02PostGoldOutcome.DefeatReleaseFF),
            0x01 => new(state, GeminiStage02PostGoldOutcome.LowFeedback),
            _ => new(state, GeminiStage02PostGoldOutcome.HealthyFeedback)
        };
    }

    /// <summary>
    /// Composes the mandatory $0E platform detour with the already-closed
    /// PlatformSpecialNormalExitPipeline. The physical gate is therefore not
    /// duplicated here: accepted exit requires X >= $B4, Y == $80, jump phase 0.
    ///
    /// Release $02 reaches fixed $ED57/$ED8F, which increments $067C without
    /// calling $A973. Ordinary Saints return to stage $02 and preserve $066F.
    /// Hyoga is redirected by the existing owner to stage $08 with
    /// $06B8=$0A/$0690=$FF and is handed directly to AquariusStage08Context.
    /// </summary>
    public static GeminiStage02PlatformResumeResult? ResolvePlatformDetourExit(
        GeminiStage02State detourState,
        int playerX,
        int playerY,
        int jumpPhase)
    {
        EnsureStage02(detourState);
        if (detourState.Release0670 != PlatformDetourRelease || detourState.Phase067C != 0)
            throw new InvalidOperationException("Platform $0E composition requires canonical stage-2 phase-0 release $02.");

        var platformSaint = ToPlatformSaint(detourState.ActiveSaint0533);
        var exit = PlatformSpecialNormalExitPipeline.ResolveAcceptedExit(
            PlatformDetourSubstate,
            platformSaint,
            playerX,
            playerY,
            jumpPhase,
            flags0673: StoryMarkerWithActiveSaint(detourState.ActiveSaint0533),
            flags06CC: 0x00);

        if (!exit.HasValue)
            return null;

        var accepted = exit.Value;

        if (detourState.ActiveSaint0533 == HyogaIndex)
        {
            var firstCamus = AquariusStage08Context.EnterRedirectedFirstCamus(
                sourceStage050E: StageIndex,
                activeSaint0533: HyogaIndex,
                sourceRelease0670: PlatformDetourRelease,
                inboundPhase067C: detourState.Phase067C);

            return new(
                accepted,
                GeminiStage02PlatformResumeKind.RedirectedFirstCamus,
                GeminiState: null,
                FirstCamusState: firstCamus);
        }

        var resumed = detourState with
        {
            Stage050E = accepted.ReloadField050E,
            Phase067C = accepted.Field067C,
            Release0670 = accepted.Terminal0670
        };

        return new(
            accepted,
            GeminiStage02PlatformResumeKind.OrdinaryStage02Continuation,
            GeminiState: resumed,
            FirstCamusState: null);
    }

    /// <summary>
    /// Generic defeat $FF does not advance story progress. On the subsequent
    /// normal stage re-entry, fixed $ED57 sees a release other than $02/$03 and
    /// calls common reset $A973, which clears $067C/$066F/$0670/$068E. The next
    /// Bronze action therefore repeats the mandatory platform detour.
    ///
    /// activeSaintForRetry represents the Saint selected for the retry; when
    /// omitted, the outgoing Saint is retained for a same-Saint retry fixture.
    /// </summary>
    public static GeminiStage02State ResetForRetryAfterDefeat(
        GeminiStage02State defeatedState,
        byte? activeSaintForRetry = null)
    {
        if (defeatedState.Release0670 != DefeatRelease)
            throw new InvalidOperationException("Retry reset requires canonical stage-2 defeat release $FF.");

        var saint = activeSaintForRetry ?? defeatedState.ActiveSaint0533;
        return CreateCanonicalEntry(saint);
    }

    /// <summary>
    /// Ordinary stage-2 victory release $01 joins fixed $E399/$E3B3.
    /// For canonical ordinary post-detour Saints (Seiya/Shun/Shiryu), the active
    /// Saint is preserved. $067D 02->03, $F016[03]=03 and $E50B[03]=02 yield the
    /// exact Cancer boundary $050E=$03/$06CD=$02/$0673=$32.
    /// </summary>
    public static GeminiStage02CancerBoundary ResolveOrdinaryCancerBoundary(GeminiStage02State terminalState)
    {
        EnsureStage02(terminalState);
        if (terminalState.Release0670 != VictoryRelease || terminalState.Phase067C == 0)
            throw new InvalidOperationException("Ordinary Cancer boundary requires post-detour stage-2 victory release $01.");
        if (terminalState.ActiveSaint0533 == HyogaIndex)
            throw new InvalidOperationException("Canonical Hyoga phase-1 resume redirects to first Camus instead of ordinary stage $02.");

        ValidateReachableSaint(terminalState.ActiveSaint0533);

        return new(
            ActiveSaint0533: terminalState.ActiveSaint0533,
            StoryProgress067D: CancerStoryProgress,
            Stage050E: CancerStage,
            StoryDescriptor06CD: CancerStoryDescriptor06CD,
            StoryMarker0673: CancerStoryMarker0673,
            Path: GeminiStage02CancerPath.OrdinaryStage02Victory);
    }

    /// <summary>
    /// Redirected first Camus owns release $FE. Composition is delegated to the
    /// already-closed Aquarius context, whose fixed $FE owner forces Seiya and
    /// advances progress $02->$03. This wrapper only adds the already-proven
    /// Cancer descriptor/marker values so both stage-2 canonical victory routes
    /// terminate at the same explicit boundary.
    /// </summary>
    public static GeminiStage02CancerBoundary ResolveFirstCamusCancerBoundary(AquariusStage08State terminalFirstCamus)
    {
        var advance = AquariusStage08Context.ApplyStageAdvanceReleaseFe(terminalFirstCamus);
        if (advance.Path != AquariusAdvancePath.FirstCamusToCancerStage03)
            throw new InvalidOperationException("Expected redirected first-Camus progress-$02 advance to Cancer.");

        return new(
            ActiveSaint0533: advance.ActiveSaint0533,
            StoryProgress067D: advance.StoryProgress067D,
            Stage050E: advance.NextStage050E,
            StoryDescriptor06CD: CancerStoryDescriptor06CD,
            StoryMarker0673: CancerStoryMarker0673,
            Path: GeminiStage02CancerPath.RedirectedFirstCamusReleaseFE);
    }

    public static bool IsReachableEntrySaint(byte activeSaint0533) => activeSaint0533 <= ShiryuIndex;

    private static byte StoryMarkerWithActiveSaint(byte activeSaint0533)
    {
        ValidateReachableSaint(activeSaint0533);
        return (byte)(InitialStoryMarker0673 | (1 << activeSaint0533));
    }

    private static PlatformSaintIndex ToPlatformSaint(byte canonicalSaint0533)
    {
        ValidateReachableSaint(canonicalSaint0533);
        var internalIndex = canonicalSaint0533 switch
        {
            0x00 => 0, // Seiya
            0x01 => 2, // Hyoga
            0x02 => 1, // Shun
            0x03 => 3, // Shiryu
            _ => throw new InvalidOperationException("Unreachable canonical stage-2 Saint.")
        };
        return (PlatformSaintIndex)internalIndex;
    }

    private static void EnsureStage02(GeminiStage02State state)
    {
        ValidateReachableSaint(state.ActiveSaint0533);
        if (state.StoryProgress067D != StoryProgressSeed || state.Stage050E != StageIndex)
            throw new InvalidOperationException("Gemini stage-local logic requires canonical progress $02 / stage $02.");
    }

    private static void EnsureActive(GeminiStage02State state)
    {
        if (state.Release0670 != 0)
            throw new InvalidOperationException("Gemini stage-local action requires active $0670=0.");
    }

    private static void EnsureStage02TurnActive(GeminiStage02State state)
    {
        EnsureStage02(state);
        EnsureActive(state);
    }

    private static void ValidateReachableSaint(byte activeSaint0533)
    {
        if (!IsReachableEntrySaint(activeSaint0533))
            throw new ArgumentOutOfRangeException(nameof(activeSaint0533), activeSaint0533,
                "Canonical stage $02 entry permits Seiya/Hyoga/Shun/Shiryu (0..3); Ikki is masked by $0673=$30.");
    }

    private static void ValidateCondition(byte value, string parameterName)
    {
        if (value is not (0x00 or 0x01 or 0xFF))
            throw new ArgumentOutOfRangeException(parameterName, value, "Battle condition must be $00, $01 or $FF.");
    }
}

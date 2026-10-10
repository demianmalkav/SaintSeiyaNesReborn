namespace SaintSeiyaNesReborn.OriginalSpec;

public enum TaurusTalkOutcome
{
    FirstConversation,
    SecondConversation,
    RepeatedConversationForcesCounterattack
}

public enum TaurusPostBronzeOutcome
{
    ContinueAfterHit,
    NoHitFeedback,
    FirstOpponentLowConditionEvent,
    VictoryRelease01
}

public enum TaurusPostGoldOutcome
{
    Continue,
    FirstPlayerLowConditionEvent,
    DefeatReleaseFF
}

public readonly record struct TaurusStage01State(
    byte Conversation066F,
    byte PlayerLowEvent064D,
    byte Feedback064E,
    byte Weakening0681,
    byte PresentationDD,
    byte Release0670,
    byte IntroDone068E);

public readonly record struct TaurusTalkResult(
    TaurusStage01State State,
    TaurusTalkOutcome Outcome,
    bool WeakeningActivated,
    bool ForceGoldCounterattack);

public readonly record struct TaurusPostBronzeResult(
    TaurusStage01State State,
    TaurusPostBronzeOutcome Outcome);

public readonly record struct TaurusPostGoldResult(
    TaurusStage01State State,
    TaurusPostGoldOutcome Outcome);

/// <summary>
/// Stage-local control model for canonical battle stage $050E=$01
/// (Taurus/Aldebaran).
///
/// This composes the already-promoted generic battle resource, damage,
/// technique and dodge systems. It models only stage-1 state/event decisions:
/// initialization, Talk progression, post-Bronze branching, post-Gold
/// branching and the two terminal $0670 releases.
/// </summary>
public static class TaurusStage01Context
{
    public const byte StageIndex = 0x01;
    public const byte TemporaryIntroStageIndex = 0x0C;
    public const byte IntroHandoffRelease = 0x03;
    public const byte VictoryRelease = 0x01;
    public const byte DefeatRelease = 0xFF;
    public const byte LowOpponentPresentationSelector = 0x05;

    /// <summary>
    /// Fixed battle-runtime setup $A973 clears the Taurus-local counters/latches,
    /// but notably does not clear $0681. This is why Talk weakening can survive a
    /// defeat/re-entry. The first new-game/progression setup owns the initial zero.
    /// </summary>
    public static TaurusStage01State ResetForBattleRuntime(byte inboundWeakening0681 = 0) =>
        new(
            Conversation066F: 0,
            PlayerLowEvent064D: 0,
            Feedback064E: 0,
            Weakening0681: inboundWeakening0681,
            PresentationDD: 0,
            Release0670: 0,
            IntroDone068E: 0);

    /// <summary>
    /// Stage handler $97F8 clears $064D, temporarily uses stage $0C for its
    /// presentation setup, then shared $9C3D restores stage $01, sets $068E=1
    /// and emits the internal entry handoff $0670=$03.
    /// Presentation details are deliberately outside this state model.
    /// </summary>
    public static TaurusStage01State ApplyStageIntro(TaurusStage01State state)
    {
        EnsureNonTerminal(state);
        return state with
        {
            PlayerLowEvent064D = 0,
            Release0670 = IntroHandoffRelease,
            IntroDone068E = 1
        };
    }

    /// <summary>
    /// Fixed battle flow consumes the $03 entry handoff and clears $0670 before
    /// entering the interactive command loop ($E327-$E34C).
    /// </summary>
    public static TaurusStage01State EnterCommandLoop(TaurusStage01State state)
    {
        if (state.Release0670 is not (0x00 or IntroHandoffRelease))
            throw new InvalidOperationException("Only an active or Taurus-intro state can enter the command loop.");

        return state with { Release0670 = 0 };
    }

    /// <summary>
    /// Stage Talk handler $9D2C as observed through its fixed caller.
    /// $066F is canonically bounded to 0,1,2: the first two Talks advance it;
    /// the second Talk raises $0681 from 0 to 1 exactly once; every later Talk
    /// leaves $066F at 2 and raises the transient $DC latch, whose caller
    /// immediately consumes it by forcing the Gold counterattack path.
    /// </summary>
    public static TaurusTalkResult ExecuteTalk(TaurusStage01State state)
    {
        EnsureTurnActive(state);

        if (state.Conversation066F == 0)
        {
            return new(
                state with { Conversation066F = 1 },
                TaurusTalkOutcome.FirstConversation,
                WeakeningActivated: false,
                ForceGoldCounterattack: false);
        }

        if (state.Conversation066F == 1)
        {
            var changed = state.Weakening0681 == 0;
            var weakening = changed ? (byte)1 : state.Weakening0681;
            return new(
                state with
                {
                    Conversation066F = 2,
                    Weakening0681 = weakening
                },
                TaurusTalkOutcome.SecondConversation,
                WeakeningActivated: changed,
                ForceGoldCounterattack: false);
        }

        // Canonical execution reaches exactly 2. The handler itself uses BCS,
        // so preserving >=2 behavior is more faithful than inventing a clamp.
        return new(
            state,
            TaurusTalkOutcome.RepeatedConversationForcesCounterattack,
            WeakeningActivated: false,
            ForceGoldCounterattack: true);
    }

    /// <summary>
    /// Stage post-Bronze handler $A3A2. The caller supplies the already-computed
    /// opponent condition $EB and the generic $06BC hit token; no damage formula
    /// is duplicated here.
    /// </summary>
    public static TaurusPostBronzeResult AfterBronzeAction(
        TaurusStage01State state,
        byte opponentConditionEb,
        bool bronzeAttackHit)
    {
        EnsureTurnActive(state);
        ValidateCondition(opponentConditionEb, nameof(opponentConditionEb));

        if (opponentConditionEb == 0xFF)
        {
            return new(
                state with { Release0670 = VictoryRelease },
                TaurusPostBronzeOutcome.VictoryRelease01);
        }

        if (opponentConditionEb == 0x01 && state.PresentationDD != LowOpponentPresentationSelector)
        {
            return new(
                state with
                {
                    PresentationDD = LowOpponentPresentationSelector,
                    Feedback064E = unchecked((byte)(state.Feedback064E + 1))
                },
                TaurusPostBronzeOutcome.FirstOpponentLowConditionEvent);
        }

        if (!bronzeAttackHit)
        {
            return new(
                state with { Feedback064E = unchecked((byte)(state.Feedback064E + 1)) },
                TaurusPostBronzeOutcome.NoHitFeedback);
        }

        return new(state, TaurusPostBronzeOutcome.ContinueAfterHit);
    }

    /// <summary>
    /// Stage post-Gold handler $A415. The caller supplies the already-computed
    /// player condition $EA after generic dodge/damage processing.
    /// </summary>
    public static TaurusPostGoldResult AfterGoldResponse(
        TaurusStage01State state,
        byte playerConditionEa)
    {
        EnsureTurnActive(state);
        ValidateCondition(playerConditionEa, nameof(playerConditionEa));

        if (playerConditionEa == 0xFF)
        {
            return new(
                state with { Release0670 = DefeatRelease },
                TaurusPostGoldOutcome.DefeatReleaseFF);
        }

        if (playerConditionEa == 0x01 && state.PlayerLowEvent064D == 0)
        {
            return new(
                state with { PlayerLowEvent064D = 1 },
                TaurusPostGoldOutcome.FirstPlayerLowConditionEvent);
        }

        return new(state, TaurusPostGoldOutcome.Continue);
    }

    /// <summary>
    /// A defeat re-enters common battle setup, which clears the local encounter
    /// counters but preserves the weakening tier. Victory is intentionally not
    /// modeled here: release $01 crosses the established progression boundary,
    /// where $A75C/$A777 later clears $0681 while advancing progression.
    /// </summary>
    public static TaurusStage01State ResetForRetryAfterDefeat(TaurusStage01State defeatedState)
    {
        if (defeatedState.Release0670 != DefeatRelease)
            throw new InvalidOperationException("Retry reset requires the canonical Taurus defeat release $FF.");

        return ResetForBattleRuntime(defeatedState.Weakening0681);
    }

    public static bool IsTerminal(TaurusStage01State state) =>
        state.Release0670 is VictoryRelease or DefeatRelease;

    private static void EnsureTurnActive(TaurusStage01State state)
    {
        if (state.Release0670 != 0)
            throw new InvalidOperationException("Taurus command/post-action logic requires active $0670=0.");
    }

    private static void EnsureNonTerminal(TaurusStage01State state)
    {
        if (IsTerminal(state))
            throw new InvalidOperationException("A terminal Taurus encounter cannot re-enter stage-local initialization.");
    }

    private static void ValidateCondition(byte value, string parameterName)
    {
        if (value is not (0x00 or 0x01 or 0xFF))
            throw new ArgumentOutOfRangeException(parameterName, value, "Battle condition must be $00, $01 or $FF.");
    }
}

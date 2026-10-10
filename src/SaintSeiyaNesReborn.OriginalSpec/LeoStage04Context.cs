namespace SaintSeiyaNesReborn.OriginalSpec;

public enum LeoTalkOutcome
{
    FirstConversationForcesCounterattack,
    SecondConversation,
    RepeatedConversationForcesCounterattack
}

public enum LeoPostBronzeOutcome
{
    HealthyOpponentContinues,
    LowOpponentSeiyaContinues,
    LowOpponentNonSeiyaRelocksBronze,
    VictoryRelease01
}

public enum LeoPostGoldOutcome
{
    Continue,
    FirstPlayerLowConditionEvent,
    DefeatReleaseFF
}

public enum LeoGoldAttackProfile
{
    Slot0CosmoHeavy,
    Slot1LifeHeavy
}

public readonly record struct LeoStage04State(
    byte Conversation066F,
    byte PlayerLowEvent064D,
    byte ScriptedBronzeBlock0690,
    byte Weakening0681,
    byte EventEd,
    byte HistoryF1,
    byte Release0670,
    byte IntroDone068E);

public readonly record struct LeoTalkResult(
    LeoStage04State State,
    LeoTalkOutcome Outcome,
    bool ForceGoldCounterattack,
    bool ClearedScriptedBronzeBlock,
    bool UsesDistinctShunDialogue);

public readonly record struct LeoPostBronzeResult(
    LeoStage04State State,
    LeoPostBronzeOutcome Outcome,
    bool UsesEdNonzeroVictoryPresentation);

public readonly record struct LeoPostGoldResult(
    LeoStage04State State,
    LeoPostGoldOutcome Outcome);

public readonly record struct LeoGoldAttackSelection(
    byte Slot0680,
    LeoGoldAttackProfile Profile);

/// <summary>
/// Stage-local control model for canonical battle stage $050E=$04 (Leo/Aioria).
///
/// Generic resource arithmetic, Bronze/Gold damage, technique selection and
/// dodge resolution remain owned by their existing ORIGINAL SPEC artifacts.
/// This class models only stage-4 control: entry/invulnerability, intro
/// weakening, Talk progression, post-action event decisions, Gold slot
/// reachability and the terminal $0670 releases.
/// </summary>
public static class LeoStage04Context
{
    public const byte StageIndex = 0x04;
    public const byte ExpectedIntroSaintIndex = 0x00; // Seiya, from fixed $F36F[4]
    public const byte IntroHandoffRelease = 0x03;
    public const byte VictoryRelease = 0x01;
    public const byte DefeatRelease = 0xFF;
    public const byte ScriptedBronzeBlockEnabled = 0xFF;

    /// <summary>
    /// Normal battle-runtime entry first clears stage scratch through bank-1
    /// $A973, then fixed $ED72-$ED89 marks stages 2/4/8/10 with $0690=$FF.
    /// $A973 does not clear $0681, $ED or $F1, so those values survive an
    /// ordinary defeat/re-entry unless some broader bootstrap owns the reset.
    /// </summary>
    public static LeoStage04State PrepareBattleRuntime(
        byte inboundWeakening0681 = 0,
        byte inboundEventEd = 0,
        byte inboundHistoryF1 = 0) =>
        new(
            Conversation066F: 0,
            PlayerLowEvent064D: 0,
            ScriptedBronzeBlock0690: ScriptedBronzeBlockEnabled,
            Weakening0681: inboundWeakening0681,
            EventEd: inboundEventEd,
            HistoryF1: inboundHistoryF1,
            Release0670: 0,
            IntroDone068E: 0);

    /// <summary>
    /// Generic stage-entry dispatch reaches $989D when $068E==0 and the active
    /// Saint equals fixed table $F36F[4]==0 (Seiya). A non-Seiya route can enter
    /// the ordinary battle loop without running this intro, leaving inbound $ED
    /// and $0681 untouched until Seiya later satisfies the intro gate.
    /// </summary>
    public static bool ShouldRunStageIntro(LeoStage04State state, byte activeSaintCanonicalIndex)
    {
        ValidateActiveSaint(activeSaintCanonicalIndex);
        return state.IntroDone068E == 0 && activeSaintCanonicalIndex == ExpectedIntroSaintIndex;
    }

    /// <summary>
    /// Handler $989D seeds $ED=1 and contains the only two Leo weakening writes:
    /// unconditional INC $0681 at $996A and $9A05. The surrounding presentation
    /// branches do not bypass either increment. Shared $9C3D emits $0670=3 and
    /// sets $068E=1 at the end of the intro.
    /// </summary>
    public static LeoStage04State ApplyStageIntro(LeoStage04State state)
    {
        EnsureNonTerminal(state);

        return state with
        {
            EventEd = 1,
            Weakening0681 = unchecked((byte)(state.Weakening0681 + 2)),
            Release0670 = IntroHandoffRelease,
            IntroDone068E = 1
        };
    }

    public static LeoStage04State EnterCommandLoop(LeoStage04State state)
    {
        if (state.Release0670 is not (0x00 or IntroHandoffRelease))
            throw new InvalidOperationException("Only an active or Leo-intro state can enter the command loop.");

        return state with { Release0670 = 0 };
    }

    /// <summary>
    /// Stage Talk handler $9DD8 through its fixed caller.
    ///
    /// Pre-state $066F==1 is the unique non-forcing branch. It uses a distinct
    /// dialogue id for Shun (canonical index 2) and calls $A1F4 only when
    /// $F1==0; $A1F4 clears the scripted Bronze block $0690.
    ///
    /// Every other conversation value (including 0 and >=2) increments $066F,
    /// raises transient $DC, and therefore forces the generic Gold response.
    /// </summary>
    public static LeoTalkResult ExecuteTalk(LeoStage04State state, byte activeSaintCanonicalIndex)
    {
        EnsureTurnActive(state);
        ValidateActiveSaint(activeSaintCanonicalIndex);

        if (state.Conversation066F == 1)
        {
            var clearsBlock = state.HistoryF1 == 0;
            return new(
                state with
                {
                    Conversation066F = 2,
                    ScriptedBronzeBlock0690 = clearsBlock ? (byte)0 : state.ScriptedBronzeBlock0690
                },
                LeoTalkOutcome.SecondConversation,
                ForceGoldCounterattack: false,
                ClearedScriptedBronzeBlock: clearsBlock,
                UsesDistinctShunDialogue: activeSaintCanonicalIndex == 2);
        }

        var nextConversation = unchecked((byte)(state.Conversation066F + 1));
        return new(
            state with { Conversation066F = nextConversation },
            state.Conversation066F == 0
                ? LeoTalkOutcome.FirstConversationForcesCounterattack
                : LeoTalkOutcome.RepeatedConversationForcesCounterattack,
            ForceGoldCounterattack: true,
            ClearedScriptedBronzeBlock: false,
            UsesDistinctShunDialogue: false);
    }

    /// <summary>
    /// $0690 is consumed by fixed $FAB9-$FAE2 before Bronze damage: any nonzero
    /// value forces generic hit token $06BC to zero. This helper exposes that
    /// stage-local invulnerability gate without duplicating the generic hit code.
    /// </summary>
    public static bool ForcesBronzeHitTokenZero(LeoStage04State state) =>
        state.ScriptedBronzeBlock0690 != 0;

    /// <summary>
    /// Stage post-Bronze handler $A5B3. Opponent condition $EB is supplied by
    /// the already-promoted classifier. The handler does not inspect $06BC.
    /// Both $ED==0 and $ED!=0 victory presentation branches converge on release
    /// $01; $989D makes $ED!=0 after the Seiya intro, while an intro-skipped
    /// route may retain $ED==0 and reach the alternate presentation.
    /// </summary>
    public static LeoPostBronzeResult AfterBronzeAction(
        LeoStage04State state,
        byte opponentConditionEb,
        byte activeSaintCanonicalIndex)
    {
        EnsureTurnActive(state);
        ValidateCondition(opponentConditionEb, nameof(opponentConditionEb));
        ValidateActiveSaint(activeSaintCanonicalIndex);

        if (opponentConditionEb == 0xFF)
        {
            return new(
                state with { Release0670 = VictoryRelease },
                LeoPostBronzeOutcome.VictoryRelease01,
                UsesEdNonzeroVictoryPresentation: state.EventEd != 0);
        }

        if (opponentConditionEb == 0x00)
        {
            return new(
                state,
                LeoPostBronzeOutcome.HealthyOpponentContinues,
                UsesEdNonzeroVictoryPresentation: false);
        }

        if (activeSaintCanonicalIndex == 0)
        {
            return new(
                state,
                LeoPostBronzeOutcome.LowOpponentSeiyaContinues,
                UsesEdNonzeroVictoryPresentation: false);
        }

        return new(
            state with
            {
                ScriptedBronzeBlock0690 = ScriptedBronzeBlockEnabled,
                HistoryF1 = unchecked((byte)(state.HistoryF1 + 1))
            },
            LeoPostBronzeOutcome.LowOpponentNonSeiyaRelocksBronze,
            UsesEdNonzeroVictoryPresentation: false);
    }

    /// <summary>
    /// Stage post-Gold handler $A63E. Player condition $EA is supplied by the
    /// generic classifier after Gold dodge/damage processing.
    /// </summary>
    public static LeoPostGoldResult AfterGoldResponse(
        LeoStage04State state,
        byte playerConditionEa)
    {
        EnsureTurnActive(state);
        ValidateCondition(playerConditionEa, nameof(playerConditionEa));

        if (playerConditionEa == 0xFF)
        {
            return new(
                state with { Release0670 = DefeatRelease },
                LeoPostGoldOutcome.DefeatReleaseFF);
        }

        if (playerConditionEa == 0x01 && state.PlayerLowEvent064D == 0)
        {
            return new(
                state with { PlayerLowEvent064D = 1 },
                LeoPostGoldOutcome.FirstPlayerLowConditionEvent);
        }

        return new(state, LeoPostGoldOutcome.Continue);
    }

    /// <summary>
    /// Bank-6 Gold selection has one canonical writer to $0680 at $9143.
    /// Stage 4 matches none of the special stage branches and therefore executes
    /// $0680 = $065F & 1. Slots 2/3 are structurally present in the coefficient
    /// table but cannot be selected by canonical Leo control flow.
    /// </summary>
    public static LeoGoldAttackSelection SelectGoldAttack(byte phaseAccumulator065F)
    {
        var slot = (byte)(phaseAccumulator065F & 0x01);
        return new(
            slot,
            slot == 0 ? LeoGoldAttackProfile.Slot0CosmoHeavy : LeoGoldAttackProfile.Slot1LifeHeavy);
    }

    /// <summary>
    /// Ordinary defeat/re-entry runs $A973 again: local scratch is cleared,
    /// $0690 is re-armed for stage 4, while $0681/$ED/$F1 are not cleared.
    /// Whether $989D is dispatched again remains controlled by the generic
    /// $068E/active-Saint intro gate and is intentionally a separate step.
    /// </summary>
    public static LeoStage04State ResetForRetryAfterDefeat(LeoStage04State defeatedState)
    {
        if (defeatedState.Release0670 != DefeatRelease)
            throw new InvalidOperationException("Retry reset requires canonical Leo defeat release $FF.");

        return PrepareBattleRuntime(
            defeatedState.Weakening0681,
            defeatedState.EventEd,
            defeatedState.HistoryF1);
    }

    public static bool IsTerminal(LeoStage04State state) =>
        state.Release0670 is VictoryRelease or DefeatRelease;

    private static void EnsureTurnActive(LeoStage04State state)
    {
        if (state.Release0670 != 0)
            throw new InvalidOperationException("Leo command/post-action logic requires active $0670=0.");
    }

    private static void EnsureNonTerminal(LeoStage04State state)
    {
        if (IsTerminal(state))
            throw new InvalidOperationException("A terminal Leo encounter cannot re-enter stage-local initialization.");
    }

    private static void ValidateCondition(byte value, string parameterName)
    {
        if (value is not (0x00 or 0x01 or 0xFF))
            throw new ArgumentOutOfRangeException(parameterName, value, "Battle condition must be $00, $01 or $FF.");
    }

    private static void ValidateActiveSaint(byte value)
    {
        // Leo's second-Talk dialogue table contains four canonical entries;
        // Ikki (index 4) is not reachable in this stage's normal roster.
        if (value > 3)
            throw new ArgumentOutOfRangeException(nameof(value), value, "Leo stage canonical active Saint index is 0..3.");
    }
}

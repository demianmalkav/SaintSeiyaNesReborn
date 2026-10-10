namespace SaintSeiyaNesReborn.OriginalSpec;

public enum AquariusPhase
{
    RedirectedFirstCamus,
    FinalCamus
}

public enum AquariusInitOutcome
{
    Progress02RestoreSeiyaWithoutUnlock,
    FinalCamusUnlockThirdTechniqueRelease03
}

public enum AquariusTalkOutcome
{
    FirstCamusChallengeRefusal,
    FirstCamusPatriarchExchange,
    FirstCamusFinalExchange,
    FinalNoDodgeFirstDialogue,
    FinalNoDodgeRepeatForcesCounterattack,
    FinalFirstPostDodgeTalk,
    FinalFirstPostDodgeTalkUnlocksFourthTechnique,
    FinalRepeatPostDodgeTalkForcesCounterattack
}

public enum AquariusPostBronzeOutcome
{
    FirstCamusAwaitThreeTalksAbortTurn,
    FirstCamusScriptedFreezingReleaseFe,
    FinalCamusContinue,
    FinalCamusVictoryReleaseFe
}

public enum AquariusPostGoldOutcome
{
    FirstCamusContinue,
    FirstCamusScriptedFreezingReleaseFe,
    FinalCamusContinue,
    FinalCamusLowFeedback,
    FinalCamusDefeatReleaseFf
}

public enum AquariusReleaseOwner
{
    ActiveBattle,
    IntroHandoff,
    StageAdvanceForceSeiya,
    GenericDefeat
}

public enum AquariusAdvancePath
{
    FirstCamusToCancerStage03,
    FinalCamusToPiscesStage09
}

public readonly record struct AquariusStage08State(
    byte ActiveSaint0533,
    byte StoryProgress067D,
    byte Phase067C,
    byte Conversation066F,
    byte Release0670,
    byte DodgeFailures0677,
    byte DodgeSuccesses0678,
    byte FirstEncounter06B8,
    byte Prelude06E1,
    byte HyogaTechniqueCount0588,
    byte ActiveTechniqueCount0696,
    byte ScriptedBronzeBlock0690,
    byte IntroDone068E);

public readonly record struct AquariusInitResult(AquariusStage08State State, AquariusInitOutcome Outcome);
public readonly record struct AquariusTalkResult(AquariusStage08State State, AquariusTalkOutcome Outcome, bool ForceGoldCounterattack);
public readonly record struct AquariusPostBronzeResult(AquariusStage08State State, AquariusPostBronzeOutcome Outcome, bool AbortOuterTurn);
public readonly record struct AquariusPostGoldResult(AquariusStage08State State, AquariusPostGoldOutcome Outcome);
public readonly record struct AquariusGoldAttackSelection(byte Slot0680, int CosmoCoefficient, int LifeCoefficient);
public readonly record struct AquariusAdvanceResult(
    byte ActiveSaint0533,
    byte StoryProgress067D,
    byte NextStage050E,
    AquariusAdvancePath Path);

/// <summary>
/// Stage-local control model for canonical battle stage $050E=$08 (Aquarius/Camus).
/// Generic resource arithmetic, Bronze damage, Gold damage and dodge resolution remain
/// owned by their existing specifications. This model owns only the two Camus phases,
/// Hyoga's two technique unlock events, stage-8 Talk/post-action control, the
/// $06B8/$06E1 lifecycle and Gold-slot reachability.
/// </summary>
public static class AquariusStage08Context
{
    public const byte StageIndex = 0x08;
    public const byte Stage02Index = 0x02;
    public const byte CancerStageIndex = 0x03;
    public const byte PiscesStageIndex = 0x09;

    public const byte SeiyaIndex = 0x00;
    public const byte HyogaIndex = 0x01;
    public const byte IkkiIndex = 0x04;

    public const byte FirstCamusStoryProgress = 0x02;
    public const byte FinalCamusStoryProgress = 0x0A;
    public const byte FirstEncounterMarker = 0x0A;
    public const byte ScriptedBronzeBlockEnabled = 0xFF;

    public const byte IntroRelease = 0x03;
    public const byte StageAdvanceRelease = 0xFE;
    public const byte DefeatRelease = 0xFF;

    public static AquariusStage08State PrepareBattleRuntime(
        byte activeSaint0533,
        byte storyProgress067D,
        byte firstEncounter06B8 = 0,
        byte prelude06E1 = 0,
        byte hyogaTechniqueCount0588 = 2,
        byte activeTechniqueCount0696 = 2,
        byte scriptedBronzeBlock0690 = ScriptedBronzeBlockEnabled)
    {
        ValidateSaint(activeSaint0533);

        return new(
            activeSaint0533,
            storyProgress067D,
            Phase067C: 0,
            Conversation066F: 0,
            Release0670: 0,
            DodgeFailures0677: 0,
            DodgeSuccesses0678: 0,
            FirstEncounter06B8: firstEncounter06B8,
            Prelude06E1: prelude06E1,
            HyogaTechniqueCount0588: hyogaTechniqueCount0588,
            ActiveTechniqueCount0696: activeTechniqueCount0696,
            ScriptedBronzeBlock0690: scriptedBronzeBlock0690,
            IntroDone068E: 0);
    }

    /// <summary>
    /// Models the fixed-bank $ED8F-$EDAF special-resume redirection.
    /// Returning from release $02/$03 increments $067C without the ordinary reset.
    /// If the source stage is $02 and the active Saint is Hyoga, the engine forces
    /// $050E=$08, writes $06B8=$0A and arms $0690=$FF. The semantic model
    /// normalizes the consumed release back to zero after the handoff.
    /// </summary>
    public static AquariusStage08State EnterRedirectedFirstCamus(
        byte sourceStage050E,
        byte activeSaint0533,
        byte sourceRelease0670,
        byte inboundPhase067C = 0,
        byte hyogaTechniqueCount0588 = 2,
        byte activeTechniqueCount0696 = 2,
        byte prelude06E1 = 0)
    {
        if (sourceStage050E != Stage02Index)
            throw new ArgumentOutOfRangeException(nameof(sourceStage050E), sourceStage050E, "First Camus redirection requires source stage $02.");
        if (activeSaint0533 != HyogaIndex)
            throw new ArgumentOutOfRangeException(nameof(activeSaint0533), activeSaint0533, "First Camus redirection requires active Hyoga.");
        if (sourceRelease0670 is not (0x02 or 0x03))
            throw new ArgumentOutOfRangeException(nameof(sourceRelease0670), sourceRelease0670, "Special resume only redirects releases $02/$03.");

        return new(
            ActiveSaint0533: HyogaIndex,
            StoryProgress067D: FirstCamusStoryProgress,
            Phase067C: unchecked((byte)(inboundPhase067C + 1)),
            Conversation066F: 0,
            Release0670: 0,
            DodgeFailures0677: 0,
            DodgeSuccesses0678: 0,
            FirstEncounter06B8: FirstEncounterMarker,
            Prelude06E1: prelude06E1,
            HyogaTechniqueCount0588: hyogaTechniqueCount0588,
            ActiveTechniqueCount0696: activeTechniqueCount0696,
            ScriptedBronzeBlock0690: ScriptedBronzeBlockEnabled,
            IntroDone068E: 0);
    }

    public static AquariusStage08State PrepareFinalCamus(
        byte activeSaint0533 = HyogaIndex,
        byte prelude06E1 = 1,
        byte hyogaTechniqueCount0588 = 2,
        byte activeTechniqueCount0696 = 2)
        => PrepareBattleRuntime(
            activeSaint0533: activeSaint0533,
            storyProgress067D: FinalCamusStoryProgress,
            firstEncounter06B8: 0,
            prelude06E1: prelude06E1,
            hyogaTechniqueCount0588: hyogaTechniqueCount0588,
            activeTechniqueCount0696: activeTechniqueCount0696,
            scriptedBronzeBlock0690: ScriptedBronzeBlockEnabled);

    public static AquariusPhase GetPhase(AquariusStage08State state)
        => state.FirstEncounter06B8 != 0 ? AquariusPhase.RedirectedFirstCamus : AquariusPhase.FinalCamus;

    /// <summary>
    /// Models bank-5 $9B14.
    /// $067D==$02 saves the current active record, loads Seiya and returns without
    /// presentation or technique growth. Every other path runs the Aquarius intro,
    /// increments Hyoga's persistent technique count $0588 and the active menu count
    /// $0696, then exits through shared release $03.
    /// Canonical final Camus entry is $067D=$0A, so this is the 2->3 unlock.
    /// </summary>
    public static AquariusInitResult ApplyInitialization(AquariusStage08State state)
    {
        EnsureActive(state);

        if (state.StoryProgress067D == FirstCamusStoryProgress)
        {
            return new(
                state with { ActiveSaint0533 = SeiyaIndex },
                AquariusInitOutcome.Progress02RestoreSeiyaWithoutUnlock);
        }

        return new(
            state with
            {
                HyogaTechniqueCount0588 = Increment(state.HyogaTechniqueCount0588),
                ActiveTechniqueCount0696 = Increment(state.ActiveTechniqueCount0696),
                Release0670 = IntroRelease,
                IntroDone068E = 1
            },
            AquariusInitOutcome.FinalCamusUnlockThirdTechniqueRelease03);
    }

    /// <summary>
    /// Fixed $F1C6-$F1D2 writes $06E1=1 whenever the stage-8 battle setup path is
    /// entered. The ordinary stage reset does not clear $06E1, so the flag can
    /// bridge the redirected first Camus encounter and the later final encounter.
    /// </summary>
    public static AquariusStage08State MarkStage8BattleSetup(AquariusStage08State state)
        => state with { Prelude06E1 = 1 };

    /// <summary>
    /// Models the material stage-8 effects of bank-1 $A973 plus fixed $ED72-$ED8C.
    /// The ordinary reset clears $06B8, Talk/dodge counters and $0690, then the
    /// stage-index check immediately re-arms $0690=$FF for stage $08. $06E1 is
    /// intentionally preserved.
    /// </summary>
    public static AquariusStage08State ApplyOrdinaryStage8Reset(AquariusStage08State state)
        => state with
        {
            Phase067C = 0,
            Conversation066F = 0,
            Release0670 = 0,
            DodgeFailures0677 = 0,
            DodgeSuccesses0678 = 0,
            FirstEncounter06B8 = 0,
            ScriptedBronzeBlock0690 = ScriptedBronzeBlockEnabled,
            IntroDone068E = 0
        };

    public static AquariusStage08State EnterCommandLoop(AquariusStage08State state)
    {
        if (state.Release0670 is not (0x00 or IntroRelease))
            throw new InvalidOperationException("Only active Aquarius state or release $03 can enter the command loop.");
        return state with { Release0670 = 0 };
    }

    /// <summary>
    /// Models bank-5 $9F00.
    /// In the redirected first encounter ($06B8!=0), Talk is a three-step scripted
    /// exchange driven only by $066F and never raises transient $DC.
    /// In final Camus ($06B8==0), total dodge history $0677+$0678 partitions the
    /// branches. The first post-dodge Talk increments $066F; with active Hyoga it
    /// also calls $A1F4, clearing $0690, then performs the 3->4 technique unlock.
    /// </summary>
    public static AquariusTalkResult ExecuteTalk(AquariusStage08State state)
    {
        EnsureTurnActive(state);

        if (state.FirstEncounter06B8 != 0)
        {
            var next = state with { Conversation066F = Increment(state.Conversation066F) };
            var outcome = state.Conversation066F switch
            {
                0 => AquariusTalkOutcome.FirstCamusChallengeRefusal,
                1 => AquariusTalkOutcome.FirstCamusPatriarchExchange,
                _ => AquariusTalkOutcome.FirstCamusFinalExchange
            };
            return new(next, outcome, ForceGoldCounterattack: false);
        }

        var totalDodgeAttempts = AddByte(state.DodgeFailures0677, state.DodgeSuccesses0678);
        if (totalDodgeAttempts == 0)
        {
            if (state.Prelude06E1 == 0)
            {
                return new(
                    state with { Prelude06E1 = Increment(state.Prelude06E1) },
                    AquariusTalkOutcome.FinalNoDodgeFirstDialogue,
                    ForceGoldCounterattack: false);
            }

            return new(
                state,
                AquariusTalkOutcome.FinalNoDodgeRepeatForcesCounterattack,
                ForceGoldCounterattack: true);
        }

        if (state.Conversation066F != 0)
        {
            return new(
                state,
                AquariusTalkOutcome.FinalRepeatPostDodgeTalkForcesCounterattack,
                ForceGoldCounterattack: true);
        }

        var advanced = state with { Conversation066F = 1 };
        if (state.ActiveSaint0533 != HyogaIndex)
        {
            return new(
                advanced,
                AquariusTalkOutcome.FinalFirstPostDodgeTalk,
                ForceGoldCounterattack: false);
        }

        advanced = advanced with
        {
            HyogaTechniqueCount0588 = Increment(advanced.HyogaTechniqueCount0588),
            ActiveTechniqueCount0696 = Increment(advanced.ActiveTechniqueCount0696),
            ScriptedBronzeBlock0690 = 0
        };

        return new(
            advanced,
            AquariusTalkOutcome.FinalFirstPostDodgeTalkUnlocksFourthTechnique,
            ForceGoldCounterattack: false);
    }

    /// <summary>
    /// Models stage-8 post-Bronze handler $A8FC.
    /// During first Camus, $066F<3 performs the four-PLA unwind that aborts the
    /// enclosing post-action chain. Once the three Talk steps have occurred, the
    /// next Bronze action runs the scripted Aurora Execution/Freezing Coffin sequence
    /// and releases $FE, independent of generic opponent condition.
    /// Final Camus uses the generic opponent classifier: only $EB=$FF is terminal,
    /// and victory also exits through $FE.
    /// </summary>
    public static AquariusPostBronzeResult AfterBronzeAction(
        AquariusStage08State state,
        byte opponentConditionEb)
    {
        EnsureTurnActive(state);
        ValidateCondition(opponentConditionEb, nameof(opponentConditionEb));

        if (state.FirstEncounter06B8 != 0)
        {
            if (state.Conversation066F < 3)
            {
                return new(
                    state,
                    AquariusPostBronzeOutcome.FirstCamusAwaitThreeTalksAbortTurn,
                    AbortOuterTurn: true);
            }

            return new(
                state with { Release0670 = StageAdvanceRelease },
                AquariusPostBronzeOutcome.FirstCamusScriptedFreezingReleaseFe,
                AbortOuterTurn: false);
        }

        if (opponentConditionEb != 0xFF)
        {
            return new(
                state,
                AquariusPostBronzeOutcome.FinalCamusContinue,
                AbortOuterTurn: false);
        }

        return new(
            state with { Release0670 = StageAdvanceRelease },
            AquariusPostBronzeOutcome.FinalCamusVictoryReleaseFe,
            AbortOuterTurn: false);
    }

    /// <summary>
    /// Models stage-8 post-Gold handler $A9D3 after generic Gold attack/dodge/damage.
    /// First Camus converts actual Hyoga defeat into the same scripted $FE freezing
    /// progression used by the three-Talk path; it is not a game-over.
    /// Final Camus keeps $EA=$01 nonterminal and uses ordinary defeat release $FF
    /// only for $EA=$FF.
    /// </summary>
    public static AquariusPostGoldResult AfterGoldResponse(
        AquariusStage08State state,
        byte playerConditionEa)
    {
        EnsureTurnActive(state);
        ValidateCondition(playerConditionEa, nameof(playerConditionEa));

        if (state.FirstEncounter06B8 != 0)
        {
            if (playerConditionEa == 0xFF)
            {
                return new(
                    state with { Release0670 = StageAdvanceRelease },
                    AquariusPostGoldOutcome.FirstCamusScriptedFreezingReleaseFe);
            }

            return new(state, AquariusPostGoldOutcome.FirstCamusContinue);
        }

        if (playerConditionEa == 0xFF)
        {
            return new(
                state with { Release0670 = DefeatRelease },
                AquariusPostGoldOutcome.FinalCamusDefeatReleaseFf);
        }

        if (playerConditionEa == 0x01)
            return new(state, AquariusPostGoldOutcome.FinalCamusLowFeedback);

        return new(state, AquariusPostGoldOutcome.FinalCamusContinue);
    }

    /// <summary>
    /// Bank-6 $90A8-$90CB:
    /// $06B8!=0 forces slot 2. Otherwise total dodge attempts below 2 select slot 1,
    /// and totals >=2 select slot 0. Slot 3 is structurally present in the coefficient
    /// row but unreachable from the canonical stage-8 selector.
    /// </summary>
    public static AquariusGoldAttackSelection SelectGoldAttack(AquariusStage08State state)
    {
        if (state.FirstEncounter06B8 != 0)
            return new(0x02, 30, 20);

        var totalDodgeAttempts = AddByte(state.DodgeFailures0677, state.DodgeSuccesses0678);
        return totalDodgeAttempts < 2
            ? new(0x01, 21, 31)
            : new(0x00, 30, 20);
    }

    /// <summary>
    /// Fixed $E3F7-$E414 owns release $FE: it saves the outgoing Saint, loads Seiya,
    /// converts the release to ordinary progression, increments $067D and then
    /// resolves the next stage from the story table at $F016.
    /// For the two canonical Aquarius contexts this is exactly 2->3 (Cancer stage 3)
    /// and $0A->$0B (Pisces stage 9).
    /// </summary>
    public static AquariusAdvanceResult ApplyStageAdvanceReleaseFe(AquariusStage08State state)
    {
        if (state.Release0670 != StageAdvanceRelease)
            throw new InvalidOperationException("Aquarius stage advance requires release $FE.");

        return state.StoryProgress067D switch
        {
            FirstCamusStoryProgress => new(
                ActiveSaint0533: SeiyaIndex,
                StoryProgress067D: 0x03,
                NextStage050E: CancerStageIndex,
                AquariusAdvancePath.FirstCamusToCancerStage03),
            FinalCamusStoryProgress => new(
                ActiveSaint0533: SeiyaIndex,
                StoryProgress067D: 0x0B,
                NextStage050E: PiscesStageIndex,
                AquariusAdvancePath.FinalCamusToPiscesStage09),
            _ => throw new InvalidOperationException(
                $"Unsupported canonical Aquarius $FE progress ${state.StoryProgress067D:X2}.")
        };
    }

    public static AquariusReleaseOwner ResolveReleaseOwner(AquariusStage08State state) => state.Release0670 switch
    {
        0x00 => AquariusReleaseOwner.ActiveBattle,
        IntroRelease => AquariusReleaseOwner.IntroHandoff,
        StageAdvanceRelease => AquariusReleaseOwner.StageAdvanceForceSeiya,
        DefeatRelease => AquariusReleaseOwner.GenericDefeat,
        _ => throw new InvalidOperationException($"Unsupported canonical Aquarius release ${state.Release0670:X2}.")
    };

    private static byte Increment(byte value) => unchecked((byte)(value + 1));
    private static byte AddByte(byte left, byte right) => unchecked((byte)(left + right));

    private static void EnsureActive(AquariusStage08State state)
    {
        if (state.Release0670 != 0)
            throw new InvalidOperationException("Aquarius initialization expects active $0670=0.");
    }

    private static void EnsureTurnActive(AquariusStage08State state)
    {
        if (state.Release0670 != 0)
            throw new InvalidOperationException("Aquarius turn logic requires active $0670=0.");
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

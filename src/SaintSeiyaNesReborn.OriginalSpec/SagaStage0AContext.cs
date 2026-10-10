namespace SaintSeiyaNesReborn.OriginalSpec;

public enum SagaIngressVariant
{
    Seiya,
    Shun
}

public enum SagaPhase : byte
{
    InheritedSaint = 0,
    Ikki = 1,
    SeiyaFinal = 2
}

public enum SagaTalkOutcome
{
    Phase0BeforeScriptedMiss,
    Phase0FirstPostMissTransition,
    Phase0RepeatDialogue,
    Phase1FirstTalkClearsHitBlock,
    Phase1RepeatDialogue,
    Phase2FirstFinalTalk,
    Phase2RepeatDialogue
}

public enum SagaPostBronzeOutcome
{
    Phase0ScriptedMissUnwinds,
    Phase0Continue,
    Phase1Continue,
    Phase2Continue,
    Phase2FirstLowOpponentEvent,
    Phase2VictoryRelease01
}

public enum SagaPostGoldOutcome
{
    Continue,
    Phase0ReleaseFf,
    Phase1ReleaseFf,
    Phase2LowPlayerFeedback,
    Phase2DefeatReleaseDd
}

public enum SagaEscapeOutcome
{
    EarlyPhaseBlocked,
    FinalPhaseAlreadyConsumed,
    FinalPhaseGateConsumedBeforeTalk,
    FinalSupportOverlayOpened
}

public enum SagaSupportSelectionOutcome
{
    NewSupportRewardAndRollingCrashUnlock,
    RepeatSupportNoReward
}

public enum SagaReleaseOwner
{
    ActiveBattle,
    PhaseAdvanceReentry,
    StoryVictory,
    FinalDefeat
}

public readonly record struct SagaStage0AState(
    byte ActiveSaint0533,
    byte StoryProgress067D,
    byte Stage050E,
    byte StoryRoster0673,
    byte Phase06CE,
    byte Conversation066F,
    byte PhaseFlag06CF,
    byte PhaseState06D0,
    byte LowOpponentLatch064D,
    byte SelectedBronzeSlot0649,
    byte ScriptedHitBlock0690,
    byte AttackWeakening0681,
    byte Release0670,
    byte DodgeFailures0677,
    byte DodgeSuccesses0678,
    byte SupportRewardBits06D4,
    byte SupportMode068F,
    byte SeiyaTechniqueCount0587,
    byte ActiveTechniqueCount0696,
    byte PlayerHitToken06BC);

public readonly record struct SagaTalkResult(
    SagaStage0AState State,
    SagaTalkOutcome Outcome,
    bool ForceGoldResponse);

public readonly record struct SagaPostBronzeResult(
    SagaStage0AState State,
    SagaPostBronzeOutcome Outcome,
    bool UnwindsOuterAction,
    bool GoldResponseReachable);

public readonly record struct SagaPostGoldResult(
    SagaStage0AState State,
    SagaPostGoldOutcome Outcome);

public readonly record struct SagaGoldAttackSelection(
    byte Slot0680,
    int CosmoCoefficient,
    int LifeCoefficient);

public readonly record struct SagaPhaseReentryResult(
    SagaStage0AState State,
    SagaPhase EnteredPhase,
    int SeventhSenseReward,
    bool SavedOutgoingSaintRecord);

public readonly record struct SagaEscapeResult(
    SagaStage0AState State,
    SagaEscapeOutcome Outcome,
    bool OpensSupportOverlay,
    byte? MessageId);

public readonly record struct SagaSupportSelectionResult(
    SagaStage0AState State,
    SagaSupportSelectionOutcome Outcome,
    int SeventhSenseReward,
    bool RollingCrashAvailable);

public readonly record struct SagaVictoryBoundary(
    byte ActiveSaint0533,
    byte StoryProgress067D,
    byte NextStage050E,
    byte ProgressDescriptor06CD,
    byte StoryRoster0673,
    byte Release0670,
    byte EngineBootstrapState00,
    byte ImmediateEngineSuccessor);

public readonly record struct SagaDefeatBoundary(
    byte StoryProgress067D,
    byte Stage050E,
    byte StoryRoster0673,
    byte SupportMode068F,
    byte EngineBootstrapState00,
    byte ImmediateEngineSuccessor);

/// <summary>
/// Executable stage-owned control model for canonical final boss stage $050E=$0A.
/// Generic Bronze/Gold damage, resource arithmetic, hit calculation and dodge remain
/// owned by their existing specifications. Saga owns the $06CE phase graph, scripted
/// hit block, Talk/event latches, phase-specific Gold selector, special support overlay
/// gate, phase re-entry releases and final story/defeat boundaries.
/// </summary>
public static class SagaStage0AContext
{
    public const byte StageIndex = 0x0A;
    public const byte StoryProgress = 0x0D;
    public const byte PostSagaStoryProgress = 0x0E;

    public const byte SeiyaIndex = 0x00;
    public const byte ShunIndex = 0x02;
    public const byte IkkiIndex = 0x04;

    public const byte CanonicalIngressRoster0673 = 0x3E;
    public const byte Phase1RosterMask = 0x2F;
    public const byte Phase2RosterMask = 0x3E;

    public const byte PhaseAdvanceRelease = 0xFF;
    public const byte VictoryRelease = 0x01;
    public const byte FinalDefeatRelease = 0xDD;
    public const byte PostSagaRelease = 0x05;

    public const byte ScriptedHitBlocked = 0xFF;
    public const byte SupportOverlayMode = 0x55;
    public const byte SupportDeathMode = 0xDD;
    public const int Phase1To2SeventhSenseReward = 1000;
    public const int NewSupportSeventhSenseReward = 1000;

    public const byte EarlyEscapeMessage = 0xE1;

    private static readonly byte[] SupportMasks = [0x01, 0x10, 0x08, 0x02, 0x20, 0x04];

    /// <summary>
    /// Canonical story entry from final-special stage $0C preserves active Seiya or
    /// Shun. Global RAM initialization originally zeroed $06CE. The ordinary battle
    /// reset $ED57->$A973 clears Saga transient state but preserves $06CE, then the
    /// stage-$0A/$06CE==0 special case re-arms $0690=$FF. The stage init dispatcher
    /// is not called on this initial story handoff.
    /// </summary>
    public static SagaStage0AState PrepareIngress(
        SagaIngressVariant variant,
        byte activeTechniqueCount0696 = 2,
        byte seiyaTechniqueCount0587 = 2)
    {
        var active = variant switch
        {
            SagaIngressVariant.Seiya => SeiyaIndex,
            SagaIngressVariant.Shun => ShunIndex,
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unknown Saga ingress variant.")
        };

        return new(
            ActiveSaint0533: active,
            StoryProgress067D: StoryProgress,
            Stage050E: StageIndex,
            StoryRoster0673: CanonicalIngressRoster0673,
            Phase06CE: (byte)SagaPhase.InheritedSaint,
            Conversation066F: 0,
            PhaseFlag06CF: 0,
            PhaseState06D0: 0,
            LowOpponentLatch064D: 0,
            SelectedBronzeSlot0649: 0,
            ScriptedHitBlock0690: ScriptedHitBlocked,
            AttackWeakening0681: 0,
            Release0670: 0,
            DodgeFailures0677: 0,
            DodgeSuccesses0678: 0,
            SupportRewardBits06D4: 0,
            SupportMode068F: 0,
            SeiyaTechniqueCount0587: seiyaTechniqueCount0587,
            ActiveTechniqueCount0696: activeTechniqueCount0696,
            PlayerHitToken06BC: 0);
    }

    public static SagaPhase PhaseOf(SagaStage0AState state)
    {
        ValidateStage(state);
        return state.Phase06CE switch
        {
            0 => SagaPhase.InheritedSaint,
            1 => SagaPhase.Ikki,
            2 => SagaPhase.SeiyaFinal,
            _ => throw new InvalidOperationException($"Unsupported canonical Saga phase ${state.Phase06CE:X2}.")
        };
    }

    /// <summary>
    /// $0690 is the already-closed story-level player-hit block. Phase 0 is always
    /// blocked. Phase 1 begins blocked and first Talk clears it. Phase 2 inherits the
    /// phase-1 value: skipping Ikki's Talk can therefore carry the block into Seiya's
    /// final phase and prevent normal Bronze attacks from connecting.
    /// </summary>
    public static bool IsBronzeConnectionScriptBlocked(SagaStage0AState state)
    {
        ValidateStage(state);
        return state.ScriptedHitBlock0690 != 0;
    }

    /// <summary>
    /// Models Talk dispatcher $9FF4 and its three $06CE handlers.
    /// Saga Talk never increments transient $DC, so Talk itself never forces the
    /// ordinary immediate Gold response.
    /// </summary>
    public static SagaTalkResult ExecuteTalk(SagaStage0AState state)
    {
        EnsureActive(state);
        return PhaseOf(state) switch
        {
            SagaPhase.InheritedSaint => ExecutePhase0Talk(state),
            SagaPhase.Ikki => ExecutePhase1Talk(state),
            SagaPhase.SeiyaFinal => ExecutePhase2Talk(state),
            _ => throw new InvalidOperationException()
        };
    }

    private static SagaTalkResult ExecutePhase0Talk(SagaStage0AState state)
    {
        ValidatePhaseActiveSaint(state, SagaPhase.InheritedSaint);

        if (state.Conversation066F != 0)
            return new(state, SagaTalkOutcome.Phase0RepeatDialogue, false);

        var attempts = unchecked((byte)(state.DodgeFailures0677 + state.DodgeSuccesses0678));
        if (attempts == 0)
            return new(state, SagaTalkOutcome.Phase0BeforeScriptedMiss, false);

        var next = state with { Conversation066F = Increment(state.Conversation066F) };
        if (state.PhaseFlag06CF != 0)
            return new(next, SagaTalkOutcome.Phase0RepeatDialogue, false);

        next = next with
        {
            PhaseFlag06CF = Increment(next.PhaseFlag06CF),
            PhaseState06D0 = Increment(next.PhaseState06D0)
        };
        return new(next, SagaTalkOutcome.Phase0FirstPostMissTransition, false);
    }

    private static SagaTalkResult ExecutePhase1Talk(SagaStage0AState state)
    {
        ValidatePhaseActiveSaint(state, SagaPhase.Ikki);
        if (state.Conversation066F != 0)
            return new(state, SagaTalkOutcome.Phase1RepeatDialogue, false);

        return new(
            state with
            {
                Conversation066F = Increment(state.Conversation066F),
                ScriptedHitBlock0690 = 0
            },
            SagaTalkOutcome.Phase1FirstTalkClearsHitBlock,
            false);
    }

    private static SagaTalkResult ExecutePhase2Talk(SagaStage0AState state)
    {
        ValidatePhaseActiveSaint(state, SagaPhase.SeiyaFinal);
        if (state.Conversation066F != 0)
            return new(state, SagaTalkOutcome.Phase2RepeatDialogue, false);

        return new(
            state with { Conversation066F = Increment(state.Conversation066F) },
            SagaTalkOutcome.Phase2FirstFinalTalk,
            false);
    }

    /// <summary>
    /// Models post-Bronze dispatcher $AB18. Conditions are values produced by the
    /// already-closed generic player/opponent classifiers. In phase 0, $06D0==0
    /// performs a four-PLA unwind, increments $0678 and suppresses the Gold response.
    /// </summary>
    public static SagaPostBronzeResult AfterBronzeAction(
        SagaStage0AState state,
        byte playerConditionEa = 0,
        byte opponentConditionEb = 0,
        byte playerHitToken06BC = 0,
        byte selectedBronzeSlot0649 = 0)
    {
        EnsureActive(state);
        ValidateCondition(playerConditionEa, nameof(playerConditionEa));
        ValidateCondition(opponentConditionEb, nameof(opponentConditionEb));

        var next = state with
        {
            PlayerHitToken06BC = playerHitToken06BC,
            SelectedBronzeSlot0649 = selectedBronzeSlot0649
        };

        return PhaseOf(state) switch
        {
            SagaPhase.InheritedSaint => AfterPhase0Bronze(next, playerConditionEa),
            SagaPhase.Ikki => AfterPhase1Bronze(next, playerConditionEa),
            SagaPhase.SeiyaFinal => AfterPhase2Bronze(next, opponentConditionEb),
            _ => throw new InvalidOperationException()
        };
    }

    private static SagaPostBronzeResult AfterPhase0Bronze(SagaStage0AState state, byte playerConditionEa)
    {
        ValidatePhaseActiveSaint(state, SagaPhase.InheritedSaint);
        if (state.PhaseState06D0 == 0)
        {
            return new(
                state with { DodgeSuccesses0678 = Increment(state.DodgeSuccesses0678) },
                SagaPostBronzeOutcome.Phase0ScriptedMissUnwinds,
                true,
                false);
        }

        var next = playerConditionEa == 0
            ? state
            : state with { PhaseState06D0 = 0xFF };
        return new(next, SagaPostBronzeOutcome.Phase0Continue, false, true);
    }

    private static SagaPostBronzeResult AfterPhase1Bronze(SagaStage0AState state, byte playerConditionEa)
    {
        ValidatePhaseActiveSaint(state, SagaPhase.Ikki);
        var next = playerConditionEa == 0
            ? state
            : state with { PhaseState06D0 = 0xFF };
        return new(next, SagaPostBronzeOutcome.Phase1Continue, false, true);
    }

    private static SagaPostBronzeResult AfterPhase2Bronze(SagaStage0AState state, byte opponentConditionEb)
    {
        ValidatePhaseActiveSaint(state, SagaPhase.SeiyaFinal);

        if (opponentConditionEb == 0xFF)
        {
            return new(
                state with
                {
                    Phase06CE = 0,
                    Release0670 = VictoryRelease
                },
                SagaPostBronzeOutcome.Phase2VictoryRelease01,
                false,
                false);
        }

        if (opponentConditionEb == 0x01 && state.LowOpponentLatch064D == 0)
        {
            return new(
                state with { LowOpponentLatch064D = 1 },
                SagaPostBronzeOutcome.Phase2FirstLowOpponentEvent,
                false,
                true);
        }

        return new(state, SagaPostBronzeOutcome.Phase2Continue, false, true);
    }

    /// <summary>
    /// Models phase-specific Saga Gold selector at bank 6 $90EC+.
    /// Stage-10 coefficient row is 35/23, 30/30, 60/60, 60/60 Cosmo/Life.
    /// </summary>
    public static SagaGoldAttackSelection SelectGoldAttack(SagaStage0AState state)
    {
        EnsureActive(state);
        var slot = PhaseOf(state) switch
        {
            SagaPhase.InheritedSaint => state.PhaseState06D0 == 0xFF ? (byte)1 : (byte)0,
            SagaPhase.Ikki when state.PhaseState06D0 == 0xFF => 3,
            SagaPhase.Ikki => state.SelectedBronzeSlot0649 == 0 ? (byte)2 : (byte)0,
            SagaPhase.SeiyaFinal => 3,
            _ => throw new InvalidOperationException()
        };

        return slot switch
        {
            0 => new(0, 35, 23),
            1 => new(1, 30, 30),
            2 => new(2, 60, 60),
            3 => new(3, 60, 60),
            _ => throw new InvalidOperationException()
        };
    }

    /// <summary>
    /// Models post-Gold dispatcher $AC05. Phases 0 and 1 emit $FF for either low
    /// or defeated player condition. Phase 2 keeps $EA=$01 nonterminal and converts
    /// only $EA=$FF to special defeat $DD after resetting $06CE.
    /// </summary>
    public static SagaPostGoldResult AfterGoldResponse(SagaStage0AState state, byte playerConditionEa)
    {
        EnsureActive(state);
        ValidateCondition(playerConditionEa, nameof(playerConditionEa));

        return PhaseOf(state) switch
        {
            SagaPhase.InheritedSaint when playerConditionEa != 0 => new(
                state with { Release0670 = PhaseAdvanceRelease },
                SagaPostGoldOutcome.Phase0ReleaseFf),
            SagaPhase.Ikki when playerConditionEa != 0 => new(
                state with { Release0670 = PhaseAdvanceRelease },
                SagaPostGoldOutcome.Phase1ReleaseFf),
            SagaPhase.SeiyaFinal when playerConditionEa == 0x01 => new(
                state,
                SagaPostGoldOutcome.Phase2LowPlayerFeedback),
            SagaPhase.SeiyaFinal when playerConditionEa == 0xFF => new(
                state with { Phase06CE = 0, Release0670 = FinalDefeatRelease },
                SagaPostGoldOutcome.Phase2DefeatReleaseDd),
            _ => new(state, SagaPostGoldOutcome.Continue)
        };
    }

    /// <summary>
    /// Resolves release $FF through fixed $E3ED->$F2E4->$970A->$9B5D. This callback
    /// happens before the outer release path continues. Phase 0 saves the inherited
    /// Saint and forces Ikki. Phase 1 runs the long Ikki->Seiya transition and invokes
    /// $FDE0 exactly 1000 times, producing a nominal +1000 Seventh Sense reward.
    /// Because the init increments $06CE before fixed $E168, the common $ED57 reset is
    /// bypassed on both reentries; only the explicit init writes are modeled here.
    /// </summary>
    public static SagaPhaseReentryResult ResolvePhaseAdvance(
        SagaStage0AState state,
        byte ikkiTechniqueCount058B = 2)
    {
        if (state.Release0670 != PhaseAdvanceRelease)
            throw new InvalidOperationException("Saga phase advance requires release $FF.");

        return PhaseOf(state) switch
        {
            SagaPhase.InheritedSaint => new(
                state with
                {
                    ActiveSaint0533 = IkkiIndex,
                    StoryRoster0673 = (byte)(state.StoryRoster0673 & Phase1RosterMask),
                    Phase06CE = (byte)SagaPhase.Ikki,
                    Conversation066F = 0,
                    PhaseFlag06CF = 0,
                    PhaseState06D0 = 0,
                    ScriptedHitBlock0690 = ScriptedHitBlocked,
                    Release0670 = 0,
                    ActiveTechniqueCount0696 = ikkiTechniqueCount058B
                },
                SagaPhase.Ikki,
                0,
                true),

            SagaPhase.Ikki => new(
                state with
                {
                    ActiveSaint0533 = SeiyaIndex,
                    StoryRoster0673 = (byte)(state.StoryRoster0673 & Phase2RosterMask),
                    Phase06CE = (byte)SagaPhase.SeiyaFinal,
                    Conversation066F = 0,
                    PhaseFlag06CF = 0,
                    PhaseState06D0 = 0,
                    LowOpponentLatch064D = 0,
                    AttackWeakening0681 = 0,
                    Release0670 = 0,
                    ActiveTechniqueCount0696 = state.SeiyaTechniqueCount0587
                },
                SagaPhase.SeiyaFinal,
                Phase1To2SeventhSenseReward,
                true),

            _ => throw new InvalidOperationException("Final Saga phase has no reachable $FF reentry.")
        };
    }

    /// <summary>
    /// Stage-$0A Escape is repurposed. Phases 0/1 display $E1. In phase 2, the first
    /// use while $06D0==0 increments $06CF/$06D0. If final Talk has already occurred,
    /// it clears $06D4, writes $068F=$55 and opens the special support-selection
    /// overlay. Using it before Talk consumes the one-shot $06D0 gate without opening
    /// the overlay; no reachable writer restores $06D0 to zero in phase 2.
    /// </summary>
    public static SagaEscapeResult ExecuteEscape(SagaStage0AState state)
    {
        EnsureActive(state);
        var phase = PhaseOf(state);
        if (phase != SagaPhase.SeiyaFinal)
            return new(state, SagaEscapeOutcome.EarlyPhaseBlocked, false, EarlyEscapeMessage);

        ValidatePhaseActiveSaint(state, SagaPhase.SeiyaFinal);
        if (state.PhaseState06D0 != 0)
            return new(state, SagaEscapeOutcome.FinalPhaseAlreadyConsumed, false, null);

        var next = state with
        {
            PhaseFlag06CF = Increment(state.PhaseFlag06CF),
            PhaseState06D0 = Increment(state.PhaseState06D0)
        };

        if (state.Conversation066F == 0)
            return new(next, SagaEscapeOutcome.FinalPhaseGateConsumedBeforeTalk, false, null);

        next = next with
        {
            SupportRewardBits06D4 = 0,
            SupportMode068F = SupportOverlayMode
        };
        return new(next, SagaEscapeOutcome.FinalSupportOverlayOpened, true, null);
    }

    /// <summary>
    /// Fixed $F477-$F49A runs inside the $068F=$55 support overlay. $F786 supplies
    /// one of six single-bit masks. A previously unseen bit is ORed into $06D4,
    /// grants #$0A->$F31E (+1000 Seventh Sense), and writes 3 to both $0587 and
    /// $0696. Re-confirming the same bit gives no reward. The first new confirmation
    /// is therefore Seiya's exact Pegasus Rolling Crash unlock event.
    /// </summary>
    public static SagaSupportSelectionResult ConfirmSupportSelection(
        SagaStage0AState state,
        byte supportMask)
    {
        EnsureActive(state);
        ValidatePhaseActiveSaint(state, SagaPhase.SeiyaFinal);
        if (state.SupportMode068F != SupportOverlayMode)
            throw new InvalidOperationException("Saga support selection requires active $068F=$55 overlay.");
        if (!SupportMasks.Contains(supportMask))
            throw new ArgumentOutOfRangeException(nameof(supportMask), supportMask, "Unsupported Saga support mask.");

        if ((state.SupportRewardBits06D4 & supportMask) != 0)
        {
            return new(
                state,
                SagaSupportSelectionOutcome.RepeatSupportNoReward,
                0,
                state.SeiyaTechniqueCount0587 >= 3);
        }

        return new(
            state with
            {
                SupportRewardBits06D4 = (byte)(state.SupportRewardBits06D4 | supportMask),
                SeiyaTechniqueCount0587 = 3,
                ActiveTechniqueCount0696 = 3
            },
            SagaSupportSelectionOutcome.NewSupportRewardAndRollingCrashUnlock,
            NewSupportSeventhSenseReward,
            true);
    }

    /// <summary>
    /// Phase-2 opponent defeat $ABBC-$AC02 resets $06CE and emits release $01.
    /// Fixed story owner increments $067D $0D->$0E; $E50B[$0E]=0 and $F016[$0E]=0.
    /// Fixed $E1CF recognizes progress $0E, rewrites release to $05 and commits engine
    /// bootstrap state $00. Already-closed global dispatch then maps bootstrap $00->$20.
    /// This is the exact boundary where Saga ownership ends.
    /// </summary>
    public static SagaVictoryBoundary ResolveVictoryBoundary(SagaStage0AState state)
    {
        if (state.Release0670 != VictoryRelease || state.Phase06CE != 0)
            throw new InvalidOperationException("Saga victory boundary requires phase-2 victory release $01 after $06CE reset.");

        return new(
            ActiveSaint0533: SeiyaIndex,
            StoryProgress067D: PostSagaStoryProgress,
            NextStage050E: 0x00,
            ProgressDescriptor06CD: 0x00,
            StoryRoster0673: 0x30,
            Release0670: PostSagaRelease,
            EngineBootstrapState00: 0x00,
            ImmediateEngineSuccessor: 0x20);
    }

    /// <summary>
    /// Phase-2 player defeat resets $06CE and emits $DD. Fixed $E417 sets roster
    /// $0673=$3F, selects special mode $068F=$DD, runs the dedicated defeat overlay,
    /// then commits bootstrap engine state $90. The closed dispatcher maps $90->$91.
    /// Story progress remains $0D; this is a defeat/recovery boundary, not Saga reentry.
    /// </summary>
    public static SagaDefeatBoundary ResolveFinalDefeatBoundary(SagaStage0AState state)
    {
        if (state.Release0670 != FinalDefeatRelease || state.Phase06CE != 0)
            throw new InvalidOperationException("Saga final defeat boundary requires release $DD after $06CE reset.");

        return new(
            StoryProgress067D: StoryProgress,
            Stage050E: StageIndex,
            StoryRoster0673: 0x3F,
            SupportMode068F: SupportDeathMode,
            EngineBootstrapState00: 0x90,
            ImmediateEngineSuccessor: 0x91);
    }

    public static SagaReleaseOwner ResolveReleaseOwner(SagaStage0AState state) => state.Release0670 switch
    {
        0x00 => SagaReleaseOwner.ActiveBattle,
        PhaseAdvanceRelease => SagaReleaseOwner.PhaseAdvanceReentry,
        VictoryRelease => SagaReleaseOwner.StoryVictory,
        FinalDefeatRelease => SagaReleaseOwner.FinalDefeat,
        _ => throw new InvalidOperationException($"Unsupported Saga release ${state.Release0670:X2}.")
    };

    /// <summary>
    /// $9C2C is structurally present as init phase 2, but the only canonical init
    /// caller is release-$FF reentry. Phase 2 has no reachable $FF terminal: victory
    /// is $01 and final defeat is $DD. Therefore this branch is not reachable in the
    /// canonical Saga graph.
    /// </summary>
    public static bool IsPhase2InitializationReachableFromCanonicalGraph() => false;

    private static void EnsureActive(SagaStage0AState state)
    {
        ValidateStage(state);
        if (state.Release0670 != 0)
            throw new InvalidOperationException("Saga command/action logic requires active release $0670=0.");
    }

    private static void ValidateStage(SagaStage0AState state)
    {
        if (state.StoryProgress067D != StoryProgress || state.Stage050E != StageIndex)
            throw new InvalidOperationException("Saga stage model requires $067D=$0D and $050E=$0A.");
    }

    private static void ValidatePhaseActiveSaint(SagaStage0AState state, SagaPhase phase)
    {
        var valid = phase switch
        {
            SagaPhase.InheritedSaint => state.ActiveSaint0533 is SeiyaIndex or ShunIndex,
            SagaPhase.Ikki => state.ActiveSaint0533 == IkkiIndex,
            SagaPhase.SeiyaFinal => state.ActiveSaint0533 == SeiyaIndex,
            _ => false
        };
        if (!valid)
            throw new InvalidOperationException($"Active Saint ${state.ActiveSaint0533:X2} is not reachable in Saga phase {(byte)phase}.");
    }

    private static void ValidateCondition(byte value, string name)
    {
        if (value is not (0x00 or 0x01 or 0xFF))
            throw new ArgumentOutOfRangeException(name, value, "Battle condition must be $00, $01 or $FF.");
    }

    private static byte Increment(byte value) => unchecked((byte)(value + 1));
}

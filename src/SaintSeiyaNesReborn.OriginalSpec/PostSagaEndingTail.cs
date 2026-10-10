using SaintSeiyaNesReborn.OriginalSpec.Platform;

namespace SaintSeiyaNesReborn.OriginalSpec;

public readonly record struct PostSagaPlatformEntry(
    byte ActiveSaint0533,
    byte StoryProgress067D,
    byte Stage050E,
    byte ProgressDescriptor06CD,
    byte StoryRoster0673,
    byte Release0670,
    byte EngineState00,
    byte PlatformSubstate02);

public readonly record struct PostSagaEndingTerminal(
    byte PrgBank,
    ushort EntryCpu,
    int PresentationStreamCount,
    ushort TerminalLoopCpu,
    bool ReturnsToGameplayOrFrontEnd,
    bool RequiresExternalResetToLeaveMainThreadTerminal);

/// <summary>
/// Composition boundary from the already-closed Saga victory into the original
/// game's final platform/narrative/ending tail.
///
/// This class intentionally reuses the existing platform exit, $70-$75,
/// $80-$89 and $8F reload specifications. It owns only the cross-subsystem join
/// and the newly proven bank-0 terminal sequence at $BC39-$BD2F.
/// </summary>
public static class PostSagaEndingTail
{
    public const byte RequiredStoryProgress = 0x0E;
    public const byte RequiredPostSagaRelease = 0x05;
    public const byte RequiredBootstrapState = 0x00;
    public const byte RequiredPlatformState = 0x20;
    public const byte FinalPlatformSubstate = 0x11;
    public const byte FinalPlatformSaint = 0x00; // canonical/internal Seiya are both zero.

    public const byte EndingPrgBank = 0x00;
    public const ushort EndingEntryCpu = 0xBC39;
    public const ushort EndingTerminalLoopCpu = 0xBD2F;
    public const int EndingPresentationStreamCount = 10;

    private static readonly ushort[] EndingPresentationPointers =
    [
        0xBDF2,
        0xBE14,
        0xBE48,
        0xBE81,
        0xBEB1,
        0xBEE9,
        0xBEFD,
        0xBF3B,
        0xBF71,
        0xBF87,
    ];

    /// <summary>
    /// Joins SagaStage0AContext's exact victory boundary to the fixed
    /// progress-to-platform mapping $E4D7/$E4E0. Progress $0E selects platform
    /// substate $11. This is the two-page final platform area whose accepted
    /// coordinate gate is already owned by PlatformExitGate.
    /// </summary>
    public static PostSagaPlatformEntry EnterFinalPlatform(SagaVictoryBoundary victory)
    {
        if (victory.ActiveSaint0533 != SagaStage0AContext.SeiyaIndex
            || victory.StoryProgress067D != RequiredStoryProgress
            || victory.NextStage050E != 0x00
            || victory.ProgressDescriptor06CD != 0x00
            || victory.StoryRoster0673 != 0x30
            || victory.Release0670 != RequiredPostSagaRelease
            || victory.EngineBootstrapState00 != RequiredBootstrapState
            || victory.ImmediateEngineSuccessor != RequiredPlatformState)
        {
            throw new InvalidOperationException(
                "Post-Saga final platform requires the exact verified Saga victory boundary.");
        }

        return new PostSagaPlatformEntry(
            ActiveSaint0533: victory.ActiveSaint0533,
            StoryProgress067D: victory.StoryProgress067D,
            Stage050E: victory.NextStage050E,
            ProgressDescriptor06CD: victory.ProgressDescriptor06CD,
            StoryRoster0673: victory.StoryRoster0673,
            Release0670: victory.Release0670,
            EngineState00: victory.ImmediateEngineSuccessor,
            PlatformSubstate02: FinalPlatformSubstate);
    }

    /// <summary>
    /// Uses the frozen $11 platform gate. The canonical final area is Seiya-only
    /// at this boundary; success requires X >= $D0, Y == $50 and jump phase zero.
    /// </summary>
    public static PlatformExitTransitionKind? EvaluateFinalPlatformExit(
        PostSagaPlatformEntry entry,
        int playerX,
        int playerY,
        int jumpPhase)
    {
        ValidateFinalPlatformEntry(entry);
        return PlatformExitGate.Evaluate(
            FinalPlatformSubstate,
            PlatformSaintIndex.Seiya,
            playerX,
            playerY,
            jumpPhase);
    }

    /// <summary>
    /// Composes an accepted $11 exit with the already-closed special seed. The
    /// returned state must be advanced only through PlatformPostExitStateMachine
    /// and PlatformNarrative80To89StateMachine; their internals are not duplicated.
    /// </summary>
    public static PlatformPostExitSeedResult SeedSpecialNarrative(
        PostSagaPlatformEntry entry,
        PlatformPostExitEngineState currentPlatformState,
        int playerX,
        int playerY,
        int jumpPhase)
    {
        var transition = EvaluateFinalPlatformExit(entry, playerX, playerY, jumpPhase);
        if (transition != PlatformExitTransitionKind.State70Special)
            throw new InvalidOperationException("Final platform exit has not satisfied the canonical $11 gate.");

        return PlatformPostExitStateMachine.ApplyAcceptedExit(transition.Value, currentPlatformState);
    }

    /// <summary>
    /// Resolves the state-$89 reload handoff with the corrected $8F model and
    /// proves entry into the bank-0 final presentation. $F381 tail-jumps to $BC39;
    /// there is no second $00->$20 bootstrap.
    /// </summary>
    public static PostSagaEndingTerminal ResolveEndingTerminal(
        PlatformPostExitEngineState state89Reload,
        byte persistent06AB,
        byte reloadField050E)
    {
        var reload = PlatformNarrative8FReload.ResolveNarrativeReturn(
            state89Reload,
            persistent06AB,
            reloadField050E);

        if (!reload.DivertsToBank0Ending
            || reload.ReturnsToMainLoopC180
            || reload.Selector068F != PlatformNarrative8FReload.Selector068F
            || reload.ProgressDescriptor06CD != PlatformNarrative8FReload.EndingProgressDescriptor06CD
            || reload.StoryRoster0673 != PlatformNarrative8FReload.EndingStoryRoster0673
            || reload.SaintAvailability06CC != PlatformNarrative8FReload.EndingSaintAvailability06CC
            || reload.PrgBank0639 != EndingPrgBank
            || reload.EndingEntryCpu != EndingEntryCpu)
        {
            throw new InvalidOperationException("Corrected $8F reload did not reach the verified bank-0 ending entry.");
        }

        return new PostSagaEndingTerminal(
            PrgBank: EndingPrgBank,
            EntryCpu: EndingEntryCpu,
            PresentationStreamCount: EndingPresentationStreamCount,
            TerminalLoopCpu: EndingTerminalLoopCpu,
            ReturnsToGameplayOrFrontEnd: false,
            RequiresExternalResetToLeaveMainThreadTerminal: true);
    }

    /// <summary>
    /// $BDA0 uses a pointer base stored at $BDDC/$BDDD (= $BDDE), adds 2*index,
    /// then loads one of ten final presentation stream pointers. Canonical $BC39
    /// invokes indices 0 through 9 exactly once, in order.
    /// </summary>
    public static ushort PresentationPointerForIndex(int index)
    {
        if ((uint)index >= EndingPresentationPointers.Length)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Ending stream index must be 0..9.");

        return EndingPresentationPointers[index];
    }

    /// <summary>
    /// After stream 9 completes ($0641 reaches zero), bank 0 executes
    /// $BD2F: JMP $BD2F. There is no ROM control-flow edge back to the engine,
    /// title or front-end from this main-thread terminal.
    /// </summary>
    public static bool IsHardTerminalAfterCompletedStream(int index) =>
        index == EndingPresentationStreamCount - 1;

    private static void ValidateFinalPlatformEntry(PostSagaPlatformEntry entry)
    {
        if (entry.ActiveSaint0533 != FinalPlatformSaint
            || entry.StoryProgress067D != RequiredStoryProgress
            || entry.Stage050E != 0x00
            || entry.ProgressDescriptor06CD != 0x00
            || entry.StoryRoster0673 != 0x30
            || entry.Release0670 != RequiredPostSagaRelease
            || entry.EngineState00 != RequiredPlatformState
            || entry.PlatformSubstate02 != FinalPlatformSubstate)
        {
            throw new InvalidOperationException("State is not the canonical post-Saga final-platform entry.");
        }
    }
}

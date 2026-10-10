namespace SaintSeiyaNesReborn.OriginalSpec;

public enum BattleStageCoverageClassification
{
    DedicatedContextClosed,
    MaterialContextMissing,
    StructuralNoCanonicalBattle
}

public enum BattleStageGoldSelectorKind
{
    Unreachable,
    GenericParitySlots01,
    VirgoStage05,
    ScorpioStage06,
    AquariusStage08,
    PiscesStage09,
    SagaStage0A
}

[Flags]
public enum BattleStageSurface
{
    None = 0,
    Initialization = 1 << 0,
    Talk = 1 << 1,
    PostBronze = 1 << 2,
    PostGold = 1 << 3,
    GoldSelector = 1 << 4,
    OrdinaryBattle = Initialization | Talk | PostBronze | PostGold | GoldSelector
}

public readonly record struct BattleStageCoverageRow(
    byte StageIndex,
    string Identity,
    BattleStageCoverageClassification Classification,
    ushort InitializationHandler,
    ushort TalkHandler,
    ushort PostBronzeHandler,
    ushort PostGoldHandler,
    BattleStageGoldSelectorKind GoldSelectorKind,
    byte ReachableGoldSlotMask,
    byte? CanonicalBattleStoryProgress,
    BattleStageSurface ReachableSurfaces,
    string? DedicatedContextArtifact);

/// <summary>
/// Coverage audit for the canonical stage-indexed battle/event namespace
/// $050E=$00-$0B.
///
/// This is a coverage/specification layer, not a second battle engine. It records
/// the four bank-5 dispatcher families, canonical story provenance, reachable
/// Gold-selector topology and whether each numeric stage already has a dedicated
/// executable context. Generic damage/resources/dodge/technique arithmetic stays
/// owned by the existing battle specifications.
/// </summary>
public static class BattleStageContextCoverage
{
    public const byte FirstMaterialGapStage = 0x02;

    // Closed stage $00 / Mu repair context.
    public const ushort Stage00InitializationHandler = 0x97F7;
    public const ushort Stage00TalkHandler = 0x9CB7;
    public const ushort Stage00PostActionHandler = 0xA3A1;
    public const ushort Stage00BlockedCommandOwner = 0xF238;
    public const ushort Stage00ConversationCounterAddress = 0x066F;
    public const byte Stage00ProgressSeed = 0x00;
    public const byte Stage00ProgressRelease = 0x01;
    public const byte Stage00SuccessorProgress = 0x01;
    public const byte Stage00SuccessorStage = 0x01;

    // Structural $0B evidence. The only canonical immediate uses of $0B are
    // temporary presentation loads through $F2ED, not stable battle entries.
    public const ushort Stage0BInitializationPointer = 0xA960;
    public const ushort Stage0BContainingInstructionStart = 0xA95F;
    public const ushort TemporaryStageLoader = 0xF2ED;
    public const ushort TemporaryStage0BCallSite1 = 0x9B31;
    public const ushort TemporaryStage0BCallSite2 = 0xA16D;

    private static readonly byte[] StoryProgressStageTable =
    [
        0x00, // $067D=00
        0x01,
        0x02,
        0x03,
        0x04,
        0x05,
        0x0F, // non-ordinary story context
        0x06,
        0x10, // non-ordinary story context
        0x07,
        0x08,
        0x09,
        0x0C, // final-special bridge
        0x0A,
        0x00  // post-Saga platform tail, not a battle re-entry
    ];

    private static readonly BattleStageCoverageRow[] RowsInternal =
    [
        new(
            0x00,
            "Mu / pre-battle repair",
            BattleStageCoverageClassification.DedicatedContextClosed,
            0x97F7,
            0x9CB7,
            0xA3A1,
            0xA3A1,
            BattleStageGoldSelectorKind.Unreachable,
            0x00,
            0x00,
            BattleStageSurface.Initialization | BattleStageSurface.Talk,
            nameof(MuStage00Context)),
        new(
            0x01,
            "Taurus / Aldebaran",
            BattleStageCoverageClassification.DedicatedContextClosed,
            0x97F8,
            0x9D2C,
            0xA3A2,
            0xA415,
            BattleStageGoldSelectorKind.GenericParitySlots01,
            0b0000_0011,
            0x01,
            BattleStageSurface.OrdinaryBattle,
            nameof(TaurusStage01Context)),
        new(
            0x02,
            "Gemini / first Camus branch",
            BattleStageCoverageClassification.MaterialContextMissing,
            0x981F,
            0x9D81,
            0xA444,
            0xA4CC,
            BattleStageGoldSelectorKind.GenericParitySlots01,
            0b0000_0011,
            0x02,
            BattleStageSurface.OrdinaryBattle,
            null),
        new(
            0x03,
            "Cancer / Death Mask",
            BattleStageCoverageClassification.MaterialContextMissing,
            0x9851,
            0x9D96,
            0xA50F,
            0xA560,
            BattleStageGoldSelectorKind.GenericParitySlots01,
            0b0000_0011,
            0x03,
            BattleStageSurface.OrdinaryBattle,
            null),
        new(
            0x04,
            "Leo / Aioria",
            BattleStageCoverageClassification.DedicatedContextClosed,
            0x989D,
            0x9DD8,
            0xA5B3,
            0xA63E,
            BattleStageGoldSelectorKind.GenericParitySlots01,
            0b0000_0011,
            0x04,
            BattleStageSurface.OrdinaryBattle,
            nameof(LeoStage04Context)),
        new(
            0x05,
            "Virgo / Shaka",
            BattleStageCoverageClassification.DedicatedContextClosed,
            0x9A28,
            0x9E1B,
            0xA661,
            0xA7B3,
            BattleStageGoldSelectorKind.VirgoStage05,
            0b0000_0111,
            0x05,
            BattleStageSurface.OrdinaryBattle,
            nameof(VirgoStage05Context)),
        new(
            0x06,
            "Scorpio / Milo",
            BattleStageCoverageClassification.MaterialContextMissing,
            0x9ACE,
            0x9E51,
            0xA7FF,
            0xA847,
            BattleStageGoldSelectorKind.ScorpioStage06,
            0b0000_0011,
            0x07,
            BattleStageSurface.OrdinaryBattle,
            null),
        new(
            0x07,
            "Capricorn / Shura",
            BattleStageCoverageClassification.MaterialContextMissing,
            0x9ACF,
            0x9ED6,
            0xA86B,
            0xA8D8,
            BattleStageGoldSelectorKind.GenericParitySlots01,
            0b0000_0011,
            0x09,
            BattleStageSurface.OrdinaryBattle,
            null),
        new(
            0x08,
            "Aquarius / Camus",
            BattleStageCoverageClassification.DedicatedContextClosed,
            0x9B14,
            0x9F00,
            0xA8FC,
            0xA9D3,
            BattleStageGoldSelectorKind.AquariusStage08,
            0b0000_0111,
            0x0A,
            BattleStageSurface.OrdinaryBattle,
            nameof(AquariusStage08Context)),
        new(
            0x09,
            "Pisces / Aphrodite",
            BattleStageCoverageClassification.DedicatedContextClosed,
            0x9B5C,
            0x9F99,
            0xAA57,
            0xAAF0,
            BattleStageGoldSelectorKind.PiscesStage09,
            0b0000_0111,
            0x0B,
            BattleStageSurface.OrdinaryBattle,
            nameof(PiscesStage09Context)),
        new(
            0x0A,
            "Pope / Saga",
            BattleStageCoverageClassification.DedicatedContextClosed,
            0x9B5D,
            0x9FF4,
            0xAB18,
            0xAC05,
            BattleStageGoldSelectorKind.SagaStage0A,
            0b0000_1111,
            0x0D,
            BattleStageSurface.OrdinaryBattle,
            nameof(SagaStage0AContext)),
        new(
            0x0B,
            "structural/transient presentation index",
            BattleStageCoverageClassification.StructuralNoCanonicalBattle,
            0xA960,
            0x9FF4,
            0xA3A1,
            0xA3A1,
            BattleStageGoldSelectorKind.Unreachable,
            0x00,
            null,
            BattleStageSurface.None,
            null)
    ];

    public static IReadOnlyList<BattleStageCoverageRow> Rows => RowsInternal;

    public static BattleStageCoverageRow Get(byte stageIndex)
    {
        if (stageIndex > 0x0B)
            throw new ArgumentOutOfRangeException(nameof(stageIndex), stageIndex, "Coverage audit is bounded to $00-$0B.");

        return RowsInternal[stageIndex];
    }

    /// <summary>
    /// Exact fixed-bank $F016 story-progress table for $067D=$00-$0E.
    /// Values $0F/$10/$0C are non-ordinary battle contexts and progress $0E
    /// reuses numeric stage $00 during the already-closed post-Saga platform tail.
    /// </summary>
    public static byte StageForStoryProgress(byte storyProgress067D)
    {
        if (storyProgress067D >= StoryProgressStageTable.Length)
            throw new ArgumentOutOfRangeException(nameof(storyProgress067D), storyProgress067D, "Canonical table is defined for $067D=$00-$0E.");

        return StoryProgressStageTable[storyProgress067D];
    }

    public static bool IsCanonicalOrdinaryBattleEntryProgress(byte storyProgress067D) =>
        storyProgress067D is <= 0x05 or 0x07 or 0x09 or 0x0A or 0x0B or 0x0D;

    public static BattleStageCoverageRow FirstMaterialGap()
    {
        foreach (var row in RowsInternal)
        {
            if (row.Classification == BattleStageCoverageClassification.MaterialContextMissing)
                return row;
        }

        throw new InvalidOperationException("Coverage matrix contains no material stage-context gap.");
    }
}

using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;

internal static class BattleStageContextCoverageChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckDispatcherMatrix();
        CheckStoryProgressProvenance();
        CheckCoverageClassification();
        CheckGoldSelectorReachability();
        CheckStage00FirstGapContract();
        CheckStage0BStructuralOnlyContract();
    }

    private static void CheckDispatcherMatrix()
    {
        var rows = BattleStageContextCoverage.Rows;
        Require(rows.Count == 12, "coverage matrix contains exactly stages $00-$0B");

        ushort[] init =
        [
            0x97F7, 0x97F8, 0x981F, 0x9851,
            0x989D, 0x9A28, 0x9ACE, 0x9ACF,
            0x9B14, 0x9B5C, 0x9B5D, 0xA960
        ];
        ushort[] talk =
        [
            0x9CB7, 0x9D2C, 0x9D81, 0x9D96,
            0x9DD8, 0x9E1B, 0x9E51, 0x9ED6,
            0x9F00, 0x9F99, 0x9FF4, 0x9FF4
        ];
        ushort[] postBronze =
        [
            0xA3A1, 0xA3A2, 0xA444, 0xA50F,
            0xA5B3, 0xA661, 0xA7FF, 0xA86B,
            0xA8FC, 0xAA57, 0xAB18, 0xA3A1
        ];
        ushort[] postGold =
        [
            0xA3A1, 0xA415, 0xA4CC, 0xA560,
            0xA63E, 0xA7B3, 0xA847, 0xA8D8,
            0xA9D3, 0xAAF0, 0xAC05, 0xA3A1
        ];

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            Require(row.StageIndex == i, $"coverage row {i:X2} keeps numeric stage identity");
            Require(row.InitializationHandler == init[i], $"stage {i:X2} init pointer");
            Require(row.TalkHandler == talk[i], $"stage {i:X2} Talk pointer");
            Require(row.PostBronzeHandler == postBronze[i], $"stage {i:X2} post-Bronze pointer");
            Require(row.PostGoldHandler == postGold[i], $"stage {i:X2} post-Gold pointer");
        }
    }

    private static void CheckStoryProgressProvenance()
    {
        byte[] expected =
        [
            0x00, 0x01, 0x02, 0x03, 0x04,
            0x05, 0x0F, 0x06, 0x10, 0x07,
            0x08, 0x09, 0x0C, 0x0A, 0x00
        ];

        for (byte progress = 0; progress < expected.Length; progress++)
            Require(BattleStageContextCoverage.StageForStoryProgress(progress) == expected[progress], $"$F016 progress map {progress:X2}");

        Require(BattleStageContextCoverage.IsCanonicalOrdinaryBattleEntryProgress(0x00), "progress $00 is the Mu battle/event entry");
        Require(BattleStageContextCoverage.IsCanonicalOrdinaryBattleEntryProgress(0x07), "progress $07 enters Scorpio stage $06");
        Require(BattleStageContextCoverage.IsCanonicalOrdinaryBattleEntryProgress(0x09), "progress $09 enters Capricorn stage $07");
        Require(BattleStageContextCoverage.IsCanonicalOrdinaryBattleEntryProgress(0x0D), "progress $0D enters Saga stage $0A");
        Require(!BattleStageContextCoverage.IsCanonicalOrdinaryBattleEntryProgress(0x06), "progress $06 maps to non-ordinary stage $0F");
        Require(!BattleStageContextCoverage.IsCanonicalOrdinaryBattleEntryProgress(0x08), "progress $08 maps to non-ordinary stage $10");
        Require(!BattleStageContextCoverage.IsCanonicalOrdinaryBattleEntryProgress(0x0C), "progress $0C is the final-special bridge rather than ordinary $00-$0B battle");
        Require(BattleStageContextCoverage.StageForStoryProgress(0x0E) == 0x00
            && !BattleStageContextCoverage.IsCanonicalOrdinaryBattleEntryProgress(0x0E),
            "post-Saga progress $0E reuses numeric stage $00 without re-entering the Mu context");
    }

    private static void CheckCoverageClassification()
    {
        byte[] closed = [0x01, 0x04, 0x05, 0x08, 0x09, 0x0A];
        byte[] missing = [0x00, 0x02, 0x03, 0x06, 0x07];

        foreach (var stage in closed)
            Require(BattleStageContextCoverage.Get(stage).Classification == BattleStageCoverageClassification.DedicatedContextClosed,
                $"stage {stage:X2} remains a closed dedicated context");

        foreach (var stage in missing)
            Require(BattleStageContextCoverage.Get(stage).Classification == BattleStageCoverageClassification.MaterialContextMissing,
                $"stage {stage:X2} is a material uncovered context");

        var stage0B = BattleStageContextCoverage.Get(0x0B);
        Require(stage0B.Classification == BattleStageCoverageClassification.StructuralNoCanonicalBattle
            && stage0B.CanonicalBattleStoryProgress is null,
            "stage $0B is structural/transient rather than a canonical battle gap");

        var first = BattleStageContextCoverage.FirstMaterialGap();
        Require(first.StageIndex == BattleStageContextCoverage.FirstMaterialGapStage && first.StageIndex == 0x00,
            "first material uncovered context in canonical order is stage $00");
    }

    private static void CheckGoldSelectorReachability()
    {
        byte[] masks =
        [
            0x00, // $00: attack path is blocked; Gold selector unreachable
            0x03, // $01
            0x03, // $02
            0x03, // $03
            0x03, // $04
            0x07, // $05: generic 0/1 plus Ikki-specific slot2
            0x03, // $06: dodge-history selector 0/1
            0x03, // $07
            0x07, // $08
            0x07, // $09
            0x0F, // $0A: all four slots reachable across Saga phases
            0x00  // $0B: no canonical stable battle entry
        ];

        for (var i = 0; i < masks.Length; i++)
            Require(BattleStageContextCoverage.Get((byte)i).ReachableGoldSlotMask == masks[i], $"stage {i:X2} Gold-slot reachability mask");

        Require(BattleStageContextCoverage.Get(0x06).GoldSelectorKind == BattleStageGoldSelectorKind.ScorpioStage06,
            "Scorpio owns its dodge-history Gold selector branch");
        Require(BattleStageContextCoverage.Get(0x08).GoldSelectorKind == BattleStageGoldSelectorKind.AquariusStage08,
            "Aquarius owns its phase/dodge Gold selector branch");
        Require(BattleStageContextCoverage.Get(0x0A).GoldSelectorKind == BattleStageGoldSelectorKind.SagaStage0A,
            "Saga owns the phase-dispatched four-slot selector");
    }

    private static void CheckStage00FirstGapContract()
    {
        var mu = BattleStageContextCoverage.Get(0x00);
        Require(mu.CanonicalBattleStoryProgress == 0x00,
            "Mu context is selected from canonical story progress $067D=$00");
        Require(mu.InitializationHandler == BattleStageContextCoverage.Stage00InitializationHandler
            && mu.InitializationHandler == 0x97F7,
            "Mu init entry is the $97F7 RTS slot");
        Require(mu.TalkHandler == BattleStageContextCoverage.Stage00TalkHandler
            && mu.TalkHandler == 0x9CB7,
            "Mu material state machine is Talk $9CB7");
        Require(mu.PostBronzeHandler == BattleStageContextCoverage.Stage00PostActionHandler
            && mu.PostGoldHandler == BattleStageContextCoverage.Stage00PostActionHandler,
            "Mu structural post-action entries are the shared $A3A1 RTS");
        Require(mu.ReachableSurfaces == (BattleStageSurface.Initialization | BattleStageSurface.Talk),
            "Mu canonical command topology reaches init/Talk but no attack/Gold post-action surface");
        Require(mu.ReachableGoldSlotMask == 0,
            "Mu cannot reach a canonical Gold response selector");
        Require(BattleStageContextCoverage.Stage00ProgressRelease == 0x01
            && BattleStageContextCoverage.Stage00SuccessorProgress == 0x01
            && BattleStageContextCoverage.Stage00SuccessorStage == 0x01,
            "Mu second-Talk release $01 advances progress $00->$01 and hands off to Taurus");
    }

    private static void CheckStage0BStructuralOnlyContract()
    {
        var stage = BattleStageContextCoverage.Get(0x0B);
        Require(stage.ReachableSurfaces == BattleStageSurface.None,
            "$0B owns no canonical stable battle surface");
        Require(stage.InitializationHandler == BattleStageContextCoverage.Stage0BInitializationPointer
            && BattleStageContextCoverage.Stage0BInitializationPointer == 0xA960,
            "$0B init table structurally points to $A960");
        Require(BattleStageContextCoverage.Stage0BContainingInstructionStart == 0xA95F,
            "$A960 is the second byte of the real instruction beginning at $A95F");
        Require(BattleStageContextCoverage.TemporaryStageLoader == 0xF2ED
            && BattleStageContextCoverage.TemporaryStage0BCallSite1 == 0x9B31
            && BattleStageContextCoverage.TemporaryStage0BCallSite2 == 0xA16D,
            "the two canonical immediate $0B uses are temporary $F2ED presentation loads");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

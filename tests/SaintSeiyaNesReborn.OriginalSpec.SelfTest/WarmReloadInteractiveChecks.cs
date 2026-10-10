using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class WarmReloadInteractiveChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckDirectionalPriorityAndFourCaseGrid();
        CheckIdleAndInitializationCases();
        CheckCase1Routing();
        CheckCase2Stage3DirectTerminal();
        CheckCase3Routing();
        CheckCase4Routing();
        CheckCommonDispatcherSelection();
        CheckTerminalSourceMatrices();
        CheckInvalidSelectorsAreRejected();
    }

    private static void CheckDirectionalPriorityAndFourCaseGrid()
    {
        var state = new PlatformWarmReloadSelectorState(0x00, 0x00);

        state = PlatformWarmReloadInteractive.ApplyDirectionalInput(
            state,
            (byte)(PlatformWarmReloadInteractive.InputRight | PlatformWarmReloadInteractive.InputDown));
        Require(state.Horizontal0585 == 0x02 && state.Vertical0586 == 0x00,
            "$A20B gives right priority over simultaneous down input");

        state = PlatformWarmReloadInteractive.ApplyDirectionalInput(
            state,
            PlatformWarmReloadInteractive.InputDown);
        Require(state.Horizontal0585 == 0x02 && state.Vertical0586 == 0x01,
            "subsequent down input moves the vertical selector");

        Require(
            PlatformWarmReloadInteractive.ResolveConfirmedCase(
                new(0x00, 0x00), PlatformWarmReloadInteractive.InputA)
            == PlatformWarmReloadMenuCase.UpperLeft,
            "$A275 maps selectors 0+0+1 to case 1");
        Require(
            PlatformWarmReloadInteractive.ResolveConfirmedCase(
                new(0x00, 0x01), PlatformWarmReloadInteractive.InputA)
            == PlatformWarmReloadMenuCase.LowerLeft,
            "$A275 maps selectors 0+1+1 to case 2");
        Require(
            PlatformWarmReloadInteractive.ResolveConfirmedCase(
                new(0x02, 0x00), PlatformWarmReloadInteractive.InputA)
            == PlatformWarmReloadMenuCase.UpperRight,
            "$A275 maps selectors 2+0+1 to case 3");
        Require(
            PlatformWarmReloadInteractive.ResolveConfirmedCase(
                new(0x02, 0x01), PlatformWarmReloadInteractive.InputA)
            == PlatformWarmReloadMenuCase.LowerRight,
            "$A275 maps selectors 2+1+1 to case 4");
    }

    private static void CheckIdleAndInitializationCases()
    {
        Require(
            PlatformWarmReloadInteractive.ResolveConfirmedCase(
                new(0x02, 0x01), inputMask: 0x00)
            == PlatformWarmReloadMenuCase.Idle,
            "$A275 leaves the next case idle when A is not pressed");

        var idle = PlatformWarmReloadInteractive.ResolveDispatch(
            0x05,
            PlatformWarmReloadMenuCase.Idle,
            flag067C: 0,
            flag06B8: 0);
        Require(idle.Route == PlatformWarmReloadRoute.Idle,
            "$F025 case 0 is the idle/input-enabled body");

        var init = PlatformWarmReloadInteractive.ResolveDispatch(
            0x05,
            PlatformWarmReloadMenuCase.Initialize,
            flag067C: 0,
            flag06B8: 0);
        Require(init.Route == PlatformWarmReloadRoute.Initialize,
            "$F025 case 5 is the warm-reload initialization body");
    }

    private static void CheckCase1Routing()
    {
        var stage0 = PlatformWarmReloadInteractive.ResolveDispatch(
            0x00,
            PlatformWarmReloadMenuCase.UpperLeft,
            flag067C: 0,
            flag06B8: 0);
        Require(stage0.Route == PlatformWarmReloadRoute.Stage0InterludeF238,
            "case 1 stage 0 routes to $F238");

        var stage3Direct = PlatformWarmReloadInteractive.ResolveDispatch(
            0x03,
            PlatformWarmReloadMenuCase.UpperLeft,
            flag067C: 0,
            flag06B8: 0);
        Require(stage3Direct.Route == PlatformWarmReloadRoute.StageNarrative9C91
            && stage3Direct.CommonDispatcher == PlatformWarmReloadStageDispatcher.NineC91
            && stage3Direct.DeterministicTerminal0670 == 0x02,
            "case 1 stage 3 with $067C=0 reaches the direct $9DC5 terminal $0670=$02");

        var stage3Common = PlatformWarmReloadInteractive.ResolveDispatch(
            0x03,
            PlatformWarmReloadMenuCase.UpperLeft,
            flag067C: 1,
            flag06B8: 0);
        Require(stage3Common.Route == PlatformWarmReloadRoute.CommonActionF813
            && stage3Common.CommonDispatcher == PlatformWarmReloadStageDispatcher.A381,
            "case 1 stage 3 with $067C!=0 falls to the common $F813/$A381 action");

        var stage2Special = PlatformWarmReloadInteractive.ResolveDispatch(
            0x02,
            PlatformWarmReloadMenuCase.UpperLeft,
            flag067C: 0,
            flag06B8: 0);
        Require(stage2Special.CommonDispatcher == PlatformWarmReloadStageDispatcher.A361,
            "case 1 stage 2 with $067C=0 uses the $A361 special dispatcher");
    }

    private static void CheckCase2Stage3DirectTerminal()
    {
        var result = PlatformWarmReloadInteractive.ResolveDispatch(
            0x03,
            PlatformWarmReloadMenuCase.LowerLeft,
            flag067C: 0,
            flag06B8: 0);

        Require(result.Route == PlatformWarmReloadRoute.StageNarrative9C91ThenConditionalCommon
            && result.CommonDispatcher == PlatformWarmReloadStageDispatcher.NineC91
            && result.DeterministicTerminal0670 == 0x02,
            "case 2 enters $9C91 first and stage 3/$067C=0 terminates at $9DC5 before any common second leg");
    }

    private static void CheckCase3Routing()
    {
        var stage7 = PlatformWarmReloadInteractive.ResolveDispatch(
            0x07,
            PlatformWarmReloadMenuCase.UpperRight,
            flag067C: 0,
            flag06B8: 0);
        Require(stage7.Route == PlatformWarmReloadRoute.CommonActionF813
            && stage7.CommonDispatcher == PlatformWarmReloadStageDispatcher.A381,
            "case 3 stage 7 raises $DC and enters the common $F813/$A381 action");

        var stage8Blocked = PlatformWarmReloadInteractive.ResolveDispatch(
            0x08,
            PlatformWarmReloadMenuCase.UpperRight,
            flag067C: 0,
            flag06B8: 1);
        Require(stage8Blocked.Route == PlatformWarmReloadRoute.PassiveCleanupF1C6,
            "case 3 stage 8 with $06B8!=0 bypasses the common action");

        var stage10 = PlatformWarmReloadInteractive.ResolveDispatch(
            0x0A,
            PlatformWarmReloadMenuCase.UpperRight,
            flag067C: 0,
            flag06B8: 0);
        Require(stage10.Route == PlatformWarmReloadRoute.Case3Stage10Progression,
            "case 3 stage 10 stays in its dedicated $06CE/$06D0 progression path");

        var stage4 = PlatformWarmReloadInteractive.ResolveDispatch(
            0x04,
            PlatformWarmReloadMenuCase.UpperRight,
            flag067C: 0,
            flag06B8: 0);
        Require(stage4.Route == PlatformWarmReloadRoute.Case3SharedProgressionF180,
            "case 3 stage 4 reaches the shared $F180 progression path");
    }

    private static void CheckCase4Routing()
    {
        var stage0 = PlatformWarmReloadInteractive.ResolveDispatch(
            0x00,
            PlatformWarmReloadMenuCase.LowerRight,
            flag067C: 0,
            flag06B8: 0);
        Require(stage0.Route == PlatformWarmReloadRoute.Stage0InterludeF238,
            "case 4 stage 0 uses the same $F238 interlude");

        var stage6 = PlatformWarmReloadInteractive.ResolveDispatch(
            0x06,
            PlatformWarmReloadMenuCase.LowerRight,
            flag067C: 0,
            flag06B8: 0);
        Require(stage6.Route == PlatformWarmReloadRoute.PassiveCase4,
            "case 4 principal nonzero stages remain nonterminal presentation/wait work");
    }

    private static void CheckCommonDispatcherSelection()
    {
        Require(
            PlatformWarmReloadInteractive.ResolveCommonActionDispatcher(0x02, flag067C: 0)
            == PlatformWarmReloadStageDispatcher.A361,
            "$F813 stage 2/$067C=0 selects $A361");
        Require(
            PlatformWarmReloadInteractive.ResolveCommonActionDispatcher(0x02, flag067C: 1)
            == PlatformWarmReloadStageDispatcher.A381,
            "$F813 stage 2/$067C!=0 selects $F936->$A381");
        Require(
            PlatformWarmReloadInteractive.ResolveCommonActionDispatcher(0x08, flag067C: 0)
            == PlatformWarmReloadStageDispatcher.A381,
            "all other principal stages select $A381");
    }

    private static void CheckTerminalSourceMatrices()
    {
        var stage5A361 = PlatformWarmReloadInteractive.GetPotentialTerminalSources(
            PlatformWarmReloadStageDispatcher.A361,
            0x05);
        Require(Contains(stage5A361, 0xA685, 0x01)
            && Contains(stage5A361, 0xA6EB, 0x02)
            && Contains(stage5A361, 0xA781, 0xFE),
            "$A361 stage-5 static audit retains terminal sources $01/$02/$FE");

        var stage8A381 = PlatformWarmReloadInteractive.GetPotentialTerminalSources(
            PlatformWarmReloadStageDispatcher.A381,
            0x08);
        Require(Contains(stage8A381, 0xAA01, 0xFF)
            && Contains(stage8A381, 0xAA51, 0xFE),
            "$A381 stage-8 static audit retains terminal sources $FF/$FE");

        var stage0Narrative = PlatformWarmReloadInteractive.GetPotentialTerminalSources(
            PlatformWarmReloadStageDispatcher.NineC91,
            0x00);
        Require(Contains(stage0Narrative, 0x9D20, 0x01),
            "$9C91 stage-0 direct writer is $9D20 -> $0670=$01");

        var stage0A381 = PlatformWarmReloadInteractive.GetPotentialTerminalSources(
            PlatformWarmReloadStageDispatcher.A381,
            0x00);
        Require(stage0A381.Count == 0,
            "$A381 stage 0 has no terminal $0670 source site");
    }

    private static bool Contains(
        IReadOnlyList<PlatformWarmReloadTerminalSource> sources,
        ushort sourceAddress,
        byte terminal0670)
    {
        foreach (var source in sources)
        {
            if (source.SourceAddress == sourceAddress
                && source.Terminal0670 == terminal0670)
                return true;
        }

        return false;
    }

    private static void CheckInvalidSelectorsAreRejected()
    {
        var rejected = false;
        try
        {
            _ = PlatformWarmReloadInteractive.ResolveConfirmedCase(
                new PlatformWarmReloadSelectorState(0x01, 0x00),
                PlatformWarmReloadInteractive.InputA);
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }

        Require(rejected,
            "interactive model rejects selector states not produced by $A20B");
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Warm reload interactive self-test failed: {label}");
    }
}

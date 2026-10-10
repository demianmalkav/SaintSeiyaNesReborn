using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;

internal static class SceneBattleEngineStateGraphChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckState50ResumeBootstrap();
        CheckReachableStatePartition();
        CheckStartEscape();
        CheckEarlySceneStates();
        CheckState34NmiTransition();
        CheckStates35And36();
        CheckState37Gate();
        CheckState38Skips39();
        CheckTextChain40To4D();
        CheckState4DExitsTo50();
        CheckDispatcherComposition();
    }

    private static void CheckState50ResumeBootstrap()
    {
        var entry = SceneBattleEngineStateGraph.EnterFromState50Resume(0x06);
        Require(entry.Disposition == SceneBattleStateDisposition.EnterState31
            && entry.StateWrittenBy50Main00 == 0x30
            && entry.MirrorWrittenBy50Main01 == 0x30
            && entry.StateAfterBootstrap00 == 0x31
            && entry.MirrorBeforeMainSync01 == 0x30
            && entry.DispatchReadyState00 == 0x31
            && entry.DispatchReadyMirror01 == 0x31,
            "$50 resume writes paired $30, bootstrap increments live to $31, and $C220 synchronizes mirror before dispatch");

        var threw = false;
        try
        {
            _ = SceneBattleEngineStateGraph.EnterFromState50Resume(0x05);
        }
        catch (ArgumentOutOfRangeException)
        {
            threw = true;
        }

        Require(threw, "$0200=$06 is the canonical state-$50 resume producer for scene state $30");
    }

    private static void CheckReachableStatePartition()
    {
        byte[] expected =
        [
            0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
            0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
            0x49, 0x4A, 0x4B, 0x4C, 0x4D
        ];

        Require(SceneBattleEngineStateGraph.ReachableStates.SequenceEqual(expected),
            "reachable scene/battle states are exactly transient $30, $31-$38 and $40-$4D");

        foreach (var state in new byte[] { 0x39, 0x3A, 0x3F, 0x4E, 0x4F })
        {
            Require(SceneBattleEngineStateGraph.IsInDispatcherRangeButUnreachable(state),
                $"state ${state:X2} is structurally routed but has no producer in the canonical graph");
        }
    }

    private static void CheckStartEscape()
    {
        var stay = SceneBattleEngineStateGraph.TryStartEscape(0x36, 0x36, 0x00);
        Require(stay.Disposition == SceneBattleStateDisposition.Stay
            && stay.EngineState00 == 0x36
            && stay.EngineMirror01 == 0x36,
            "without Start, top-level $3x/$4x dispatcher preserves the current state before local logic");

        var escape = SceneBattleEngineStateGraph.TryStartEscape(0x46, 0x46, 0x10);
        Require(escape.Disposition == SceneBattleStateDisposition.StartEscapeTo50
            && escape.EngineState00 == 0x50
            && escape.EngineMirror01 == 0x50,
            "Start bit $10 preempts scene-local logic and writes paired state $50");
    }

    private static void CheckEarlySceneStates()
    {
        var s31Stay = SceneBattleEngineStateGraph.StepState31(0x31, 0x01);
        var s31Advance = SceneBattleEngineStateGraph.StepState31(0x31, 0x00);
        Require(s31Stay.EngineState00 == 0x31
            && s31Advance.EngineState00 == 0x32
            && s31Advance.EngineMirror01 == 0x31,
            "$31 advances only when post-decrement $3F reaches zero, and only live $00 increments");

        var s32Advance = SceneBattleEngineStateGraph.StepState32(0x32, true);
        Require(s32Advance.EngineState00 == 0x33 && s32Advance.EngineMirror01 == 0x32,
            "$32 advances when the first scene-object field 1 is zero after its step");

        var s33Advance = SceneBattleEngineStateGraph.StepState33(0x33, true);
        Require(s33Advance.EngineState00 == 0x34 && s33Advance.EngineMirror01 == 0x33,
            "$33 advances when the second scene-object field 1 is zero after its step");
    }

    private static void CheckState34NmiTransition()
    {
        var result = SceneBattleEngineStateGraph.AdvanceState34OnNmi(0x34);
        Require(result.Disposition == SceneBattleStateDisposition.AdvanceState35OnNmi
            && result.EngineState00 == 0x35
            && result.EngineMirror01 == 0x34
            && result.Phase3F == 0xF8
            && result.Counter03CC == 0x00,
            "$34 is a one-observing-NMI setup state; $D78B increments only live $00 to $35");
    }

    private static void CheckStates35And36()
    {
        var s35Stay = SceneBattleEngineStateGraph.StepState35(0x35, 0x66);
        var s35Advance = SceneBattleEngineStateGraph.StepState35(0x35, 0x65);
        Require(s35Stay.EngineState00 == 0x35
            && s35Advance.Disposition == SceneBattleStateDisposition.EnterState36
            && s35Advance.EngineState00 == 0x36
            && s35Advance.EngineMirror01 == 0x35,
            "$35 global writer fires exactly at observed $03BB=$65");

        var s36Advance = SceneBattleEngineStateGraph.StepState36(0x36, 0x44);
        Require(s36Advance.Disposition == SceneBattleStateDisposition.EnterState37
            && s36Advance.EngineState00 == 0x37
            && s36Advance.EngineMirror01 == 0x36
            && s36Advance.Timer57 == 0x10,
            "$36 post-decrement $3F=$44 advances to $37 and seeds $57=$10");
    }

    private static void CheckState37Gate()
    {
        var countdown = SceneBattleEngineStateGraph.StepState37(0x37, 0x02, 0x20);
        Require(countdown.EngineState00 == 0x37
            && countdown.Timer57 == 0x01
            && countdown.Counter03CC == 0x20,
            "$37 drains nonzero $57 before touching $03CC");

        var incrementCounter = SceneBattleEngineStateGraph.StepState37(0x37, 0x00, 0x87);
        Require(incrementCounter.EngineState00 == 0x37
            && incrementCounter.Counter03CC == 0x88
            && incrementCounter.Field03CA == 0xD0,
            "$37 compares $03CC before incrementing, so $87 becomes $88 but stays $37 for that frame");

        var advance = SceneBattleEngineStateGraph.StepState37(0x37, 0x00, 0x88);
        Require(advance.Disposition == SceneBattleStateDisposition.EnterState38
            && advance.EngineState00 == 0x38
            && advance.EngineMirror01 == 0x37
            && advance.Field03CA == 0x00
            && advance.Field42 == 0x40
            && advance.Field4D == 0x10
            && advance.Field4E == 0x10,
            "$37 advances only when entering the zero-timer frame with $03CC >= $88");
    }

    private static void CheckState38Skips39()
    {
        var stay = SceneBattleEngineStateGraph.StepState38(0x38, 0x5E);
        Require(stay.EngineState00 == 0x38 && stay.Phase3F == 0x5F,
            "$38 remains active while incremented $3F is below $60");

        var advance = SceneBattleEngineStateGraph.StepState38(0x38, 0x5F);
        Require(advance.Disposition == SceneBattleStateDisposition.EnterState40
            && advance.EngineState00 == 0x40
            && advance.EngineMirror01 == 0x38
            && advance.Timer57 == 0x03
            && advance.Field4D == 0x20
            && advance.Field4E == 0x20,
            "$38 writes $40 directly at threshold and therefore never enters $39");
    }

    private static void CheckTextChain40To4D()
    {
        var state = (byte)0x40;
        while (state <= 0x4C)
        {
            var result = SceneBattleEngineStateGraph.AdvanceTextState40To4COnTerminator(state);
            Require(result.Disposition == SceneBattleStateDisposition.AdvanceTextState
                && result.EngineState00 == state + 1
                && result.EngineMirror01 == state + 1
                && result.Timer57 == 0x80
                && result.TextField26 == 0x80
                && result.TextField27 == 0x00,
                $"state ${state:X2} text terminator advances both state bytes and seeds canonical NMI delays");
            state = result.EngineState00;
        }

        Require(state == 0x4D, "$40-$4C text chain terminates at reachable state $4D");
    }

    private static void CheckState4DExitsTo50()
    {
        var timer = SceneBattleEngineStateGraph.StepState4DNmi(0x02, 0x80);
        Require(timer.EngineState00 == 0x4D && timer.Timer57 == 0x01 && timer.TextField26 == 0x80,
            "$4D first drains $57 without touching $26");

        var field26 = SceneBattleEngineStateGraph.StepState4DNmi(0x00, 0x02);
        Require(field26.EngineState00 == 0x4D && field26.TextField26 == 0x01,
            "$4D then drains $26 while remaining in state");

        var exit = SceneBattleEngineStateGraph.StepState4DNmi(0x00, 0x01);
        Require(exit.Disposition == SceneBattleStateDisposition.EnterState50
            && exit.EngineState00 == 0x50
            && exit.EngineMirror01 == 0x50,
            "$4D terminal countdown uses $8D41 to write paired $50; $4E/$4F are not reached");
    }

    private static void CheckDispatcherComposition()
    {
        Require(EngineStateDispatcherMap.ResolveMain(0x31).Route == EngineMainDispatchRoute.Scene30To4FC346
            && EngineStateDispatcherMap.ResolveMain(0x40).Route == EngineMainDispatchRoute.Scene30To4FC346,
            "$31 and $40 compose with the #109 shared $C346 main dispatcher");

        Require(EngineStateDispatcherMap.ResolveNmi(0x34, 0x34).Route == EngineNmiDispatchRoute.State34D2C7,
            "$34 composes with its dedicated setup NMI route");

        Require(EngineStateDispatcherMap.ResolveNmi(0x40, 0x40).Route == EngineNmiDispatchRoute.Scene40To4FD2D3,
            "$40 composes with bank-1 $8C19 NMI text/presentation route");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}

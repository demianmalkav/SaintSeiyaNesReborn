using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class SpecialEntityPreDispatch08090CChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckFacesPlayerAndAdvancesCounter();
        CheckThresholdStarts70WhenPhaseZero();
        CheckThresholdSpawnsSecondaryWhenPhaseNonzero();
        CheckControl04BypassesCounter();
        CheckCleanupFamiliesBypassCounter();
    }

    private static void CheckFacesPlayerAndAdvancesCounter()
    {
        var state = State(type: 0x08, x: 0x40, flags: 0x01, phase: 0, action: 0x00, control04: 0);
        var result = PlatformSpecialEntityPreDispatch08090C.Step(
            state,
            playerX: 0x80,
            globalCounter039A: 0x20,
            entropy48: 0x7F);

        Require(result.Outcome == PlatformSpecialEntityPreDispatchOutcome.CounterAdvanced,
            "below threshold increments global counter");
        Require(result.GlobalCounter039A == 0x21,
            "$039A increments exactly once");
        Require((result.State.Entity.Motion.FlagsFacing & 0x40) != 0 && result.FacingChanged,
            "entity left of player is forced to face right");
    }

    private static void CheckThresholdStarts70WhenPhaseZero()
    {
        var state = State(type: 0x09, x: 0xA0, flags: 0x41, phase: 0, action: 0x00, control04: 0);
        var result = PlatformSpecialEntityPreDispatch08090C.Step(
            state,
            playerX: 0x40,
            globalCounter039A: 0x7F,
            entropy48: 0x6A);

        Require(result.Outcome == PlatformSpecialEntityPreDispatchOutcome.Attack70Started,
            "threshold with +$03==0 enters family $70");
        Require(result.State.Entity.Motion.ActionState == 0x70,
            "action becomes exactly $70");
        Require(result.GlobalCounter039A == (0x6A & 0x3F),
            "threshold resets $039A from entropy low six bits");
        Require((result.State.Entity.Motion.FlagsFacing & 0x40) == 0,
            "entity right of player is forced to face left");
        Require(!result.CallsSecondarySpawnRoutine,
            "phase-zero threshold does not call A908 immediately");
    }

    private static void CheckThresholdSpawnsSecondaryWhenPhaseNonzero()
    {
        var state = State(type: 0x0C, x: 0x40, flags: 0x41, phase: 3, action: 0x00, control04: 0);
        var result = PlatformSpecialEntityPreDispatch08090C.Step(
            state,
            playerX: 0x20,
            globalCounter039A: 0x7F,
            entropy48: 0x45);

        Require(result.Outcome == PlatformSpecialEntityPreDispatchOutcome.SecondarySpawnTriggered,
            "threshold with +$03!=0 takes immediate A908 route");
        Require(result.State.Control04 == 1,
            "immediate route sets logical +$04=1");
        Require(result.State.Entity.Motion.ActionState == 0x00,
            "immediate route does not force family $70");
        Require(result.CallsSecondarySpawnRoutine && result.SoundId == 0x29,
            "immediate route calls A908 and sound $29");
        Require(result.SpawnTemplate.ObjectType == 0x8F && result.SpawnTemplate.VerticalOffset == 5,
            "type0C uses its exact A908 template");
        Require(result.GlobalCounter039A == 0x05,
            "entropy $45 resets counter to low six bits $05");
    }

    private static void CheckControl04BypassesCounter()
    {
        var state = State(type: 0x08, x: 0x40, flags: 0x01, phase: 0, action: 0x00, control04: 1);
        var result = PlatformSpecialEntityPreDispatch08090C.Step(
            state,
            playerX: 0x80,
            globalCounter039A: 0x7F,
            entropy48: 0x00);

        Require(result.Outcome == PlatformSpecialEntityPreDispatchOutcome.BypassedControl04,
            "+$04 nonzero jumps directly to active-family path");
        Require(result.GlobalCounter039A == 0x7F,
            "bypass leaves $039A unchanged");
        Require(!result.FacingChanged,
            "bypass occurs before facing correction");
    }

    private static void CheckCleanupFamiliesBypassCounter()
    {
        foreach (var action in new byte[] { 0x40, 0xD3, 0xE0 })
        {
            var state = State(type: 0x09, x: 0x40, flags: 0x01, phase: 0, action: action, control04: 0);
            var result = PlatformSpecialEntityPreDispatch08090C.Step(
                state,
                playerX: 0x80,
                globalCounter039A: 0x7F,
                entropy48: 0x00);

            Require(result.Outcome == PlatformSpecialEntityPreDispatchOutcome.BypassedSpecialActionFamily,
                $"family ${action & 0xF0:X2} bypasses type08/09/0C trigger logic");
            Require(result.GlobalCounter039A == 0x7F,
                "cleanup bypass leaves global counter untouched");
        }
    }

    private static PlatformSpecialEntityControlState State(
        byte type,
        byte x,
        byte flags,
        byte phase,
        byte action,
        byte control04)
    {
        var entity = new PlatformCommonEntityRuntimeState(
            new PlatformCommonEntityMotionState(
                ActionState: action,
                X: x,
                Y: 0x50,
                StatePhase: phase,
                GroundDescriptor: 0,
                DecisionTimer: 0,
                FlagsFacing: flags,
                Type: type,
                TerrainProbeRight: 0,
                TerrainProbeLeft: 0),
            HitPoints: 20,
            LifeDrainTicks: 2,
            CosmoDrainTicks: 2,
            SeventhSenseRewardBcd: 0x06);
        return new PlatformSpecialEntityControlState(entity, control04);
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Special 08/09/0C pre-dispatch self-test failed: {label}");
    }
}

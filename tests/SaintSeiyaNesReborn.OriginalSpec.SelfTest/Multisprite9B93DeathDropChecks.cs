using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class Multisprite9B93DeathDropChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckD0ProgressionAndMotion();
        CheckSpriteThresholds();
        CheckSubstate0CSpriteThresholds();
        CheckActionF0CompletesBeforeMotion();
        CheckVerticalBoundaryClearsClass();
    }

    private static void CheckD0ProgressionAndMotion()
    {
        var state = State(action: 0xD0, phase: 7, y: 0x40, x: 0x50);
        var step = PlatformMultisprite9B93DeathDrop.Step(state, engineSubstate02: 1, cameraDelta43: 1);

        Require(step.Outcome == PlatformMultisprite9B93DeathDropOutcome.Active,
            "D0 path remains active below terminal boundaries");
        Require(step.State.Logical.Action00 == 0xD1,
            "action increments before terminal checks");
        Require(step.State.Logical.Phase03 == 8 && step.VerticalDeltaApplied == 1,
            "phase increments and phase>>3 supplies vertical delta");
        Require(step.State.Visual.Part0.Y == 0x41 && step.State.Visual.Part0.X == 0x4D,
            "part0 applies +1Y and -2-camera X");
        Require(step.State.Visual.Part0.Sprite == 0xD6 && step.State.Visual.Part3.Sprite == 0xD6,
            "pre-E8 normal substate uses D6 for all four parts");
        Require((step.State.Visual.Part0.Flags & 0xC0) == 0x00
            && (step.State.Visual.Part1.Flags & 0xC0) == 0x40
            && (step.State.Visual.Part2.Flags & 0xC0) == 0x80
            && (step.State.Visual.Part3.Flags & 0xC0) == 0xC0,
            "A0E0 orientation table applies 00/40/80/C0 by part index");
    }

    private static void CheckSpriteThresholds()
    {
        var before = PlatformMultisprite9B93DeathDrop.Step(
            State(action: 0xE6, phase: 0, y: 0x40, x: 0x50), 1, 0);
        var after = PlatformMultisprite9B93DeathDrop.Step(
            State(action: 0xE7, phase: 0, y: 0x40, x: 0x50), 1, 0);

        Require(before.State.Logical.Action00 == 0xE7 && before.SpriteTypeUsed == 0xD6,
            "normal path remains D6 before action reaches E8");
        Require(after.State.Logical.Action00 == 0xE8 && after.SpriteTypeUsed == 0xD7,
            "normal path switches to D7 exactly when incremented action reaches E8");
    }

    private static void CheckSubstate0CSpriteThresholds()
    {
        var before = PlatformMultisprite9B93DeathDrop.Step(
            State(action: 0xE6, phase: 0, y: 0x40, x: 0x50), 0x0C, 0);
        var after = PlatformMultisprite9B93DeathDrop.Step(
            State(action: 0xE7, phase: 0, y: 0x40, x: 0x50), 0x0C, 0);

        Require(before.SpriteTypeUsed == 0x6B,
            "$0C uses 6B before E8");
        Require(after.SpriteTypeUsed == 0x6A,
            "$0C switches to 6A at E8");
    }

    private static void CheckActionF0CompletesBeforeMotion()
    {
        var state = State(action: 0xEF, phase: 7, y: 0x40, x: 0x50);
        var step = PlatformMultisprite9B93DeathDrop.Step(state, 1, 5);

        Require(step.Outcome == PlatformMultisprite9B93DeathDropOutcome.CompletedActionRange,
            "EF increments to F0 and immediately completes");
        Require(step.State.Logical.Action00 == 0xF0 && step.State.Logical.Phase03 == 0,
            "terminal action persists while cleanup clears phase");
        Require(step.State.Visual.AllEmpty,
            "terminal action clears all four visual parts");
        Require(step.VerticalDeltaApplied == 0,
            "terminal cleanup occurs before phase/movement work");
    }

    private static void CheckVerticalBoundaryClearsClass()
    {
        var state = State(action: 0xD0, phase: 7, y: 0x9F, x: 0x50);
        var step = PlatformMultisprite9B93DeathDrop.Step(state, 1, 0);

        Require(step.Outcome == PlatformMultisprite9B93DeathDropOutcome.RemovedVerticalBoundary,
            "part0 reaching A0 removes whole class after motion");
        Require(step.State.Visual.AllEmpty && step.State.Logical.Phase03 == 0,
            "vertical-boundary cleanup writes F0/FE to all parts and clears phase");
        Require(step.State.Logical.Action00 == 0xD1,
            "vertical cleanup keeps already-incremented action byte");
    }

    private static PlatformMultisprite9B93RuntimeState State(byte action, byte phase, byte y, byte x)
    {
        var visual = new PlatformMultisprite9B93VisualState(
            new PlatformMultisprite9B93Part(y, 0xB4, 0x02, x),
            new PlatformMultisprite9B93Part(y, 0xB5, 0x02, unchecked((byte)(x + 8))),
            new PlatformMultisprite9B93Part(unchecked((byte)(y + 8)), 0xB6, 0x02, x),
            new PlatformMultisprite9B93Part(unchecked((byte)(y + 8)), 0xB7, 0x02, unchecked((byte)(x + 8))));
        var logical = new PlatformMultisprite9B93LogicalState(
            Action00: action,
            X01: x,
            Y02: y,
            Phase03: phase,
            Field05: 0,
            Type09: 5,
            Profile0C: 100,
            CosmoDrain0D: 4,
            LifeDrain0E: 2,
            SeventhSenseReward0F: 0x10);
        return new PlatformMultisprite9B93RuntimeState(visual, logical, Mode81: 0);
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"9B93 death/drop self-test failed: {label}");
    }
}

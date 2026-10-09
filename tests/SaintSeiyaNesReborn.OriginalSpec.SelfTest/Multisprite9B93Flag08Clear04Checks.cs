using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class Multisprite9B93Flag08Clear04Checks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckSharedCurveMutatesAcrossOddSpriteParts();
        CheckPhaseZeroUsesTerminalFeCurve();
        CheckSubstate10ReadsCodeByteAsHorizontalData();
        CheckContactUsesPostMovePosition();
        CheckEmptyPartStallsContactPointer();
        CheckAllPartsRetiredClearsPhase();
        CheckHorizontalCodeDataMap();
    }

    private static void CheckSharedCurveMutatesAcrossOddSpriteParts()
    {
        var state = State(phase: 1, x: 0x50, y: 0x50);
        var step = PlatformMultisprite9B93Flag08Clear04.Step(
            state,
            new PlatformContactPhaseState(1, default),
            cameraDelta43: 0,
            playerX3F: 0x10,
            playerY40: 0x10,
            frameStartPlayerAction4E: 0,
            engineSubstate02: 1);

        Require(step.InitialCurveByte == 0x08,
            "phase1 -> nextPhase2 -> curve index0 uses C639 byte08");
        Require(step.CurvePart0 == 0x08
            && step.CurvePart1 == 0x04
            && step.CurvePart2 == 0x04
            && step.CurvePart3 == 0x02,
            "odd B9/BB sprites arithmetic-shift shared curve and later parts inherit it");
        Require(step.State.Visual.Part0.Y == 0x48,
            "part0 subtracts full curve8");
        Require(step.State.Visual.Part1.Y == 0x4C,
            "part1 subtracts halved curve4");
        Require(step.State.Visual.Part2.Y == 0x54,
            "part2 inherits curve4");
        Require(step.State.Visual.Part3.Y == 0x56,
            "part3 halves shared curve again to2");
        Require(step.State.Visual.Part0.Sprite == 0xB8
            && step.State.Visual.Part1.Sprite == 0xB9
            && step.State.Visual.Part2.Sprite == 0xBA
            && step.State.Visual.Part3.Sprite == 0xBB,
            "route writes B8-BB sprite sequence");
    }

    private static void CheckPhaseZeroUsesTerminalFeCurve()
    {
        var step = PlatformMultisprite9B93Flag08Clear04.Step(
            State(phase: 0, x: 0x40, y: 0x40),
            new PlatformContactPhaseState(1, default),
            0,
            0x10,
            0x10,
            0,
            1);

        Require(step.InitialCurveByte == 0xFE,
            "phase0 increments to1; phase-2 wraps FF and selects fixed FE fallback");
        Require(step.State.Visual.Part0.Y == 0x42,
            "raw SBC FE moves part0 downward by two pixels");
    }

    private static void CheckSubstate10ReadsCodeByteAsHorizontalData()
    {
        var visual = PlatformMultisprite9B93VisualState.Empty with
        {
            Part0 = new PlatformMultisprite9B93Part(0x40, 0xDA, 0x0A, 0x50),
        };
        var state = State(phase: 1, x: 0x50, y: 0x40) with { Visual = visual };
        var step = PlatformMultisprite9B93Flag08Clear04.Step(
            state,
            new PlatformContactPhaseState(1, default),
            cameraDelta43: 1,
            playerX3F: 0x70,
            playerY40: 0x70,
            frameStartPlayerAction4E: 0,
            engineSubstate02: 0x10);

        Require(step.State.Visual.Part0.Sprite == 0xDA,
            "$10 route assigns sprite DA");
        Require(PlatformMultisprite9B93Flag08Clear04.HorizontalOffsetForSprite(0xDA) == -16,
            "DA indexes sprite-B8 into raw ROM address 9F97, byte F0");
        Require(step.State.Visual.Part0.X == 0x3F,
            "X50 + F0(-16) - camera1 = X3F");
        Require(step.State.Visual.Part1.IsEmpty && step.State.Visual.Part2.IsEmpty && step.State.Visual.Part3.IsEmpty,
            "canonical $10 bootstrap remains a one-part visual object");
    }

    private static void CheckContactUsesPostMovePosition()
    {
        var step = PlatformMultisprite9B93Flag08Clear04.Step(
            State(phase: 0, x: 0x20, y: 0x40),
            new PlatformContactPhaseState(0, default),
            cameraDelta43: 0,
            playerX3F: 0x15,
            playerY40: 0x42,
            frameStartPlayerAction4E: 0,
            engineSubstate02: 1);

        Require(step.State.Visual.Part0.X == 0x1F && step.State.Visual.Part0.Y == 0x42,
            "part0 moves before A039 contact sweep");
        Require(step.Contacts.Call0!.Value.Outcome == PlatformEntityContactOutcome.ContactTriggered,
            "player X15 is outside pre-move X20 box but inside post-move X1F box");
        Require(step.Contacts.ContactState.HazardLatch76 == 0x20,
            "post-move contact seeds shared latch");
    }

    private static void CheckEmptyPartStallsContactPointer()
    {
        var state = State(phase: 1, x: 0x05, y: 0x50);
        var step = PlatformMultisprite9B93Flag08Clear04.Step(
            state,
            new PlatformContactPhaseState(0, default),
            cameraDelta43: 0,
            playerX3F: 0x59,
            playerY40: 0x4C,
            frameStartPlayerAction4E: 0,
            engineSubstate02: 1);

        Require(step.State.Visual.Part0.IsEmpty,
            "part0 X5 + offset(-1) becomes4 and retires before contact");
        Require(!step.State.Visual.Part1.IsEmpty,
            "part1 remains active and geometrically could contact player");
        Require(step.Contacts.Call0 is null && step.Contacts.Call1 is null && step.Contacts.Call2 is null,
            "A042 empty return prevents all three calls from advancing beyond empty part0");
        Require(step.Contacts.FinalVisualIndex == 0,
            "post-move contact pointer remains stalled at part0");
        Require(step.Contacts.ContactState.HazardLatch76 == 0,
            "later active part cannot trigger contact behind stalled empty part");
    }

    private static void CheckAllPartsRetiredClearsPhase()
    {
        var baseState = State(phase: 1, x: 0x20, y: 0x50);
        var visual = baseState.Visual with
        {
            Part0 = baseState.Visual.Part0 with { X = 0x05 },
            Part1 = baseState.Visual.Part1 with { X = 0x03 },
            Part2 = baseState.Visual.Part2 with { X = 0x06 },
            Part3 = baseState.Visual.Part3 with { X = 0x02 },
        };
        var step = PlatformMultisprite9B93Flag08Clear04.Step(
            baseState with { Visual = visual },
            new PlatformContactPhaseState(1, default),
            0,
            0x70,
            0x70,
            0,
            1);

        Require(step.Outcome == PlatformMultisprite9B93Flag08Clear04Outcome.AllPartsRetired,
            "per-part horizontal bounds can retire the whole visual set");
        Require(step.State.Visual.AllEmpty && step.State.Logical.Phase03 == 0,
            "A00F all-empty cleanup clears logical phase");
    }

    private static void CheckHorizontalCodeDataMap()
    {
        Require(PlatformMultisprite9B93Flag08Clear04.HorizontalOffsetForSprite(0xB8) == -1
            && PlatformMultisprite9B93Flag08Clear04.HorizontalOffsetForSprite(0xB9) == 1
            && PlatformMultisprite9B93Flag08Clear04.HorizontalOffsetForSprite(0xBA) == -2
            && PlatformMultisprite9B93Flag08Clear04.HorizontalOffsetForSprite(0xBB) == 2,
            "explicit 9F75 table maps B8-BB to -1,+1,-2,+2");
        Require(PlatformMultisprite9B93Flag08Clear04.HorizontalOffsetForSprite(0xDB) == 4
            && PlatformMultisprite9B93Flag08Clear04.HorizontalOffsetForSprite(0xDC) == -87
            && PlatformMultisprite9B93Flag08Clear04.HorizontalOffsetForSprite(0xDD) == -72,
            "$10 continuation preserves literal code bytes 04/A9/B8 as data if later parts are active");
    }

    private static PlatformMultisprite9B93RuntimeState State(byte phase, byte x, byte y)
    {
        const byte flags = 0x0A; // base02 + bit08, bit04 clear
        var visual = new PlatformMultisprite9B93VisualState(
            new PlatformMultisprite9B93Part(y, 0xB8, flags, x),
            new PlatformMultisprite9B93Part(y, 0xB9, flags, unchecked((byte)(x + 8))),
            new PlatformMultisprite9B93Part(unchecked((byte)(y + 8)), 0xBA, flags, x),
            new PlatformMultisprite9B93Part(unchecked((byte)(y + 8)), 0xBB, flags, unchecked((byte)(x + 8))));
        var logical = new PlatformMultisprite9B93LogicalState(
            Action00: 0,
            X01: x,
            Y02: y,
            Phase03: phase,
            Field05: 0,
            Type09: 5,
            Profile0C: 100,
            CosmoDrain0D: 4,
            LifeDrain0E: 2,
            SeventhSenseReward0F: 0x10);
        return new PlatformMultisprite9B93RuntimeState(visual, logical, Mode81: 3);
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"9B93 flag08/clear04 self-test failed: {label}");
    }
}

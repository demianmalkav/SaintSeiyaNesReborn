using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class EntityAttachedHazardChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckSpawnRightUsesTemplateAndRawOffsets();
        CheckSpawnLeftUsesNegativeOffsetsAndParent08Source();
        CheckOccupiedAndTemplateZeroDoNotMutate();
        CheckContactSeedsCorrectDrainSemanticsAndClearsRawBytes();
        CheckCrouchSpecificVerticalUpperAdjustment();
        CheckLatchAndSpecial40BlockContactWithoutClearing();
    }

    private static void CheckSpawnRightUsesTemplateAndRawOffsets()
    {
        var existing = PlatformEntityAttachedHazardState.Empty with
        {
            Raw30 = 0x44,
            Raw31 = 0x55,
            Raw33 = 0x66,
        };
        var parent = Parent(type: 0x06, x: 0x50, y: 0x40, flags: 0x40);

        var result = PlatformEntityAttachedHazard.TrySpawn(
            existing,
            parent,
            engineState00: 0x10,
            alternateParent08_03AB: 0x77);

        Require(result.Spawned, "type06 free slot spawns");
        Require(result.Template.ObjectType == 0xB0 && result.Template.VerticalOffset == 2,
            "type06 uses fixed A908 template B0/+2");
        Require(result.State.Raw2C == 0x42 && result.State.Raw2D == 0xB0,
            "A908 writes child Y and object type");
        Require(result.State.Raw2E == 0x43 && result.State.Raw32 == 0x43,
            "parent facing bit is combined with low control bits and copied to +2E/+32");
        Require(result.State.Raw2F == 0x59,
            "right-facing parent places child +9 X");
        Require(result.State.Raw30 == 0x44 && result.State.Raw31 == 0x55 && result.State.Raw33 == 0x66,
            "A908 preserves raw bytes it does not write");
        Require(result.ParentOffset08Value == 0xF0,
            "non-$20 engine state writes parent +08=$F0");
    }

    private static void CheckSpawnLeftUsesNegativeOffsetsAndParent08Source()
    {
        var parent = Parent(type: 0x09, x: 0x50, y: 0x40, flags: 0x00);
        var result = PlatformEntityAttachedHazard.TrySpawn(
            PlatformEntityAttachedHazardState.Empty,
            parent,
            engineState00: 0x20,
            alternateParent08_03AB: 0x6A);

        Require(result.Spawned, "type09 free slot spawns");
        Require(result.Template.ObjectType == 0xA1 && result.Template.VerticalOffset == -2,
            "type09 uses fixed A908 template A1/-2");
        Require(result.State.Raw2C == 0x3E && result.State.Raw2F == 0x47,
            "signed Y offset and left-facing -9 X wrap arithmetic are preserved");
        Require(result.State.Raw2E == 0x03 && result.State.Raw32 == 0x03,
            "left-facing control bytes contain low bits only");
        Require(result.ParentOffset08Value == 0x6A,
            "engine state $20 copies $03AB into parent +08");
    }

    private static void CheckOccupiedAndTemplateZeroDoNotMutate()
    {
        var occupied = PlatformEntityAttachedHazardState.Empty with { Raw2D = 0x80 };
        var occupiedResult = PlatformEntityAttachedHazard.TrySpawn(
            occupied,
            Parent(0x06, 0x50, 0x40, 0x40),
            0x10,
            0);
        Require(occupiedResult.Outcome == PlatformEntityAttachedHazardSpawnOutcome.SlotOccupied
            && occupiedResult.State.Equals(occupied)
            && occupiedResult.ParentOffset08Value is null,
            "A908 returns immediately when +2D is not $FE");

        var noTemplate = PlatformEntityAttachedHazard.TrySpawn(
            PlatformEntityAttachedHazardState.Empty,
            Parent(0x07, 0x50, 0x40, 0x40),
            0x10,
            0);
        Require(noTemplate.Outcome == PlatformEntityAttachedHazardSpawnOutcome.NoTemplate
            && noTemplate.State.Equals(PlatformEntityAttachedHazardState.Empty),
            "zero A908 object-type table entry performs no spawn");
    }

    private static void CheckContactSeedsCorrectDrainSemanticsAndClearsRawBytes()
    {
        var state = new PlatformEntityAttachedHazardState(
            Raw2C: 0x50,
            Raw2D: 0xB0,
            Raw2E: 0x43,
            Raw2F: 0x60,
            Raw30: 0x51,
            Raw31: 0xB1,
            Raw32: 0x43,
            Raw33: 0x68);

        var result = PlatformEntityAttachedHazard.EvaluateContact(
            state,
            frameStartAction4E: 0x00,
            playerX: 0x60,
            playerY: 0x50,
            currentHazardLatch76: 0,
            parentLifeDrainTicks: 5,
            parentCosmoDrainTicks: 2);

        Require(result.Triggered, "overlapping attached hazard triggers contact");
        Require(result.HazardLatch76 == 0x20 && result.SoundId == 0x26,
            "AA70 seeds $76=$20 and sound $26");
        Require(result.DrainState.LifeTicks == 5 && result.DrainState.CosmoTicks == 2,
            "+0E Life / +0D Cosmo semantics feed $7F/$80 in correct order");
        Require(result.State.Raw2C == 0xF0 && result.State.Raw2D == 0xFE
            && result.State.Raw30 == 0xF0 && result.State.Raw31 == 0xFE,
            "AA70 deactivates exactly +2C/+2D/+30/+31 on contact");
        Require(result.State.Raw2E == 0x43 && result.State.Raw2F == 0x60
            && result.State.Raw32 == 0x43 && result.State.Raw33 == 0x68,
            "AA70 preserves raw bytes it does not write");
    }

    private static void CheckCrouchSpecificVerticalUpperAdjustment()
    {
        var state = PlatformEntityAttachedHazardState.Empty with
        {
            Raw2C = 0x50,
            Raw2D = 0xB0,
            Raw2F = 0x60,
        };

        var ordinary = PlatformEntityAttachedHazard.EvaluateContact(
            state, 0x00, 0x60, 0x50, 0, 1, 1);
        var crouched = PlatformEntityAttachedHazard.EvaluateContact(
            state, 0x20, 0x60, 0x50, 0, 1, 1);

        Require(ordinary.Triggered,
            "same geometry contacts when player did not start frame in $20");
        Require(crouched.Outcome == PlatformEntityAttachedHazardContactOutcome.OutsideVerticalWindow,
            "AA70 subtracts $10 from vertical upper bound when frame-start action is exactly $20");
    }

    private static void CheckLatchAndSpecial40BlockContactWithoutClearing()
    {
        var state = PlatformEntityAttachedHazardState.Empty with
        {
            Raw2C = 0x50,
            Raw2D = 0xB0,
            Raw2F = 0x60,
        };

        var latched = PlatformEntityAttachedHazard.EvaluateContact(
            state, 0x00, 0x60, 0x50, 1, 5, 2);
        Require(latched.Outcome == PlatformEntityAttachedHazardContactOutcome.ContactLatchActive
            && latched.State.Equals(state),
            "existing $76 blocks contact and leaves attached record intact");

        var immune = PlatformEntityAttachedHazard.EvaluateContact(
            state, 0x40, 0x60, 0x50, 0, 5, 2);
        Require(immune.Outcome == PlatformEntityAttachedHazardContactOutcome.FrameStartSpecial40Immune
            && immune.State.Equals(state),
            "frame-start player family $40 bypasses AA70 contact");
    }

    private static PlatformCommonEntityMotionState Parent(
        byte type,
        byte x,
        byte y,
        byte flags) =>
        new(
            ActionState: 0x70,
            X: x,
            Y: y,
            StatePhase: 0,
            GroundDescriptor: 0,
            DecisionTimer: 0,
            FlagsFacing: flags,
            Type: type,
            TerrainProbeRight: 0,
            TerrainProbeLeft: 0);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Entity attached-hazard self-test failed: {label}");
    }
}

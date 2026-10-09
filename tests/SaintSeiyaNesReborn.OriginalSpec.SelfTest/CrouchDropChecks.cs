using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class CrouchDropChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckCrouch();
        CheckFall();
    }

    private static void CheckCrouch()
    {
        var baseState = State(action: 0x20, y: 0x60, x: 0x30);

        var faceRight = PlatformCrouchDrop.StepCrouched(
            baseState,
            PlatformInput.Down | PlatformInput.Left | PlatformInput.Right,
            default);
        Require(faceRight.State.Horizontal.Facing42 == 0x40,
            "Right has facing priority when both horizontal directions are held while crouched.");

        var releaseDown = PlatformCrouchDrop.StepCrouched(baseState, PlatformInput.None, default);
        Require(releaseDown.State.ActionState4D == 0,
            "Releasing Down exits crouch to action zero.");

        var clearLatch = PlatformCrouchDrop.StepCrouched(
            baseState with { DropButtonLatch038C = 0x80 },
            PlatformInput.Down,
            default);
        Require(clearLatch.State.DropButtonLatch038C == 0,
            "A release while crouched clears independent drop latch $038C.");

        var blockedFloor = new PlatformCollisionDescriptors(
            FloorCenter: 0xE0, LowerRight: null, UpperRight: null, FloorRight: null,
            LowerLeft: null, UpperLeft: null, FloorLeft: null, UpperCenter: null);
        var rejected = PlatformCrouchDrop.StepCrouched(
            baseState,
            PlatformInput.Down | PlatformInput.A,
            blockedFloor);
        Require(!rejected.DropStarted && rejected.State.ActionState4D == 0x20,
            "$E0-$EF center floor rejects drop-through.");
        Require(rejected.State.DropButtonLatch038C == 0x80,
            "Rejected drop still consumes the A press into $038C.");

        var allowed = PlatformCrouchDrop.StepCrouched(
            baseState,
            PlatformInput.Down | PlatformInput.A,
            blockedFloor with { FloorCenter = 0xF0 });
        Require(allowed.DropStarted && allowed.State.ActionState4D == 0x50,
            "$F0+ center descriptor allows drop-through.");
        Require(allowed.State.PlayerY == 0x66,
            "Drop-through initiation advances screen Y by 6.");

        var tooLow = PlatformCrouchDrop.StepCrouched(
            baseState with { PlayerY = 0x80 },
            PlatformInput.Down | PlatformInput.A,
            default);
        Require(!tooLow.DropStarted && tooLow.State.DropButtonLatch038C == 0x80,
            "Y >= $80 rejects drop after latching A.");

        var alreadyLatched = PlatformCrouchDrop.StepCrouched(
            baseState with { DropButtonLatch038C = 0x80 },
            PlatformInput.Down | PlatformInput.A,
            default);
        Require(!alreadyLatched.DropStarted,
            "Held A cannot retrigger drop while $038C is nonzero.");
    }

    private static void CheckFall()
    {
        var stage = new PlatformStageMap(
            0,
            [new PlatformStagePage(0, new byte[PlatformStagePage.DescriptorCount])]);

        // +3 Y happens before the shared floor snap.
        var floor = new PlatformCollisionDescriptors(
            FloorCenter: 0x90, LowerRight: null, UpperRight: null, FloorRight: null,
            LowerLeft: null, UpperLeft: null, FloorLeft: null, UpperCenter: null);
        var landing = PlatformCrouchDrop.StepFall(
            stage,
            State(action: 0x50, y: 0x4D, x: 0x30) with { JumpLock038D = 5 },
            PlatformSaintIndex.Seiya,
            floor,
            frameCounter3C: 0);
        Require(landing.Outcome == PlatformFallOutcome.Landed && landing.State.PlayerY == 0x50,
            "Fall applies +3 then lands/snaps through shared floor semantics.");
        Require(landing.State.ActionState4D == 0 && landing.State.JumpLock038D == 0,
            "Fall landing clears action and jump lock.");

        // Left correction has branch priority and skips the right correction.
        var bothSides = new PlatformCollisionDescriptors(
            FloorCenter: null, LowerRight: null, UpperRight: null, FloorRight: 0x80,
            LowerLeft: null, UpperLeft: null, FloorLeft: 0x88, UpperCenter: null);
        var corrected = PlatformCrouchDrop.StepFall(
            stage,
            State(action: 0x50, y: 0x40, x: 0x20),
            PlatformSaintIndex.Seiya,
            bothSides,
            frameCounter3C: 0);
        Require(corrected.State.Horizontal.PlayerX == 0x21,
            "Left floor correction pushes right and skips right-floor branch.");
        Require(corrected.PlayerCorrectedHorizontally,
            "Local side correction is reported.");

        var rightOnly = bothSides with { FloorLeft = null };
        var correctedLeft = PlatformCrouchDrop.StepFall(
            stage,
            State(action: 0x50, y: 0x40, x: 0x20),
            PlatformSaintIndex.Seiya,
            rightOnly,
            frameCounter3C: 0);
        Require(correctedLeft.State.Horizontal.PlayerX == 0x1F,
            "Right floor family pushes player left by one.");

        // At/after local X $80 the left-side correction advances camera by $0387
        // but still writes literal $43=1.
        var cameraState = State(action: 0x50, y: 0x40, x: 0x80) with
        {
            Horizontal = new PlatformHorizontalState(0x80, 0, 0, 0x40),
        };
        var cameraCorrection = PlatformCrouchDrop.StepFall(
            stage,
            cameraState,
            PlatformSaintIndex.Shun,
            bothSides with { FloorRight = null },
            frameCounter3C: 1);
        Require(cameraCorrection.State.Horizontal.ScrollLow == 2,
            "Shun odd-frame $0387=2 is used by fall camera correction.");
        Require(cameraCorrection.State.HorizontalAmount43 == 1,
            "Fall camera correction stores literal $43=1, not movement magnitude.");

        var dynamic = PlatformCrouchDrop.StepFall(
            stage,
            State(action: 0x50, y: 0x4D, x: 0x20),
            PlatformSaintIndex.Seiya,
            floor with { FloorCenter = 0xFF },
            frameCounter3C: 0,
            dynamicFloorY039B: 0x47);
        Require(dynamic.Outcome == PlatformFallOutcome.Landed && dynamic.State.PlayerY == 0x47,
            "$FF floor snaps to dynamic $039B.");

        var special = PlatformCrouchDrop.StepFall(
            stage,
            State(action: 0x50, y: 0x8D, x: 0x20),
            PlatformSaintIndex.Seiya,
            floor with { FloorCenter = 0xF8 },
            frameCounter3C: 0);
        Require(special.Outcome == PlatformFallOutcome.Landed && special.State.PlayerY == 0x88,
            "$F8/$F9 lower-screen fall lands at $88.");

        var fellOut = PlatformCrouchDrop.StepFall(
            stage,
            State(action: 0x50, y: 0x9D, x: 0x20),
            PlatformSaintIndex.Seiya,
            default,
            frameCounter3C: 0);
        Require(fellOut.Outcome == PlatformFallOutcome.FellOut && fellOut.State.ActionState4D == 0x80,
            "Fall reaching $A0 with page byte zero enters fall-out family $80.");
    }

    private static PlatformCrouchDropState State(byte action, byte y, byte x) => new(
        Horizontal: new PlatformHorizontalState(x, 0, 0, 0x40),
        PlayerY: y,
        PlayerYPage41: 0,
        ActionState4D: action,
        DropButtonLatch038C: 0,
        JumpPhase49: 0,
        JumpLock038D: 0,
        HazardFlag76: 0,
        HorizontalAmount43: 0);

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException($"CrouchDrop self-test failed: {message}");
    }
}

using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class AirControlChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        var stage = EmptyStage();
        CheckFreeVerticalSteering(stage);
        CheckDirectionalFamilies(stage);
        CheckCollisionCancellation(stage);
        CheckCameraHandoff(stage);
        CheckSecondHalfCorrections(stage);
    }

    private static void CheckFreeVerticalSteering(PlatformStageMap stage)
    {
        var state = AirState(0x30, playerX: 0x20);

        var oddRight = PlatformAirControl.Step(
            stage, state, PlatformSaintIndex.Seiya, PlatformInput.Right, default,
            jumpPhase49: 4, halfPhase: 16, frameCounter3C: 1);
        Require(oddRight.State.Horizontal.PlayerX == 0x21, "$30 free steer Right moves 1 px on odd frame.");

        var evenRight = PlatformAirControl.Step(
            stage, state, PlatformSaintIndex.Shun, PlatformInput.Right, default,
            jumpPhase49: 4, halfPhase: 16, frameCounter3C: 0);
        Require(evenRight.State.Horizontal.PlayerX == 0x20, "$30 free steer is 0 px on even frame for every Saint.");

        // Right's initial lower probe rejection falls through to Left when both are held.
        var rightBlocked = new PlatformCollisionDescriptors(
            FloorCenter: null, LowerRight: 0x80, UpperRight: null, FloorRight: null,
            LowerLeft: null, UpperLeft: null, FloorLeft: null, UpperCenter: null);
        var fallbackLeft = PlatformAirControl.Step(
            stage, state, PlatformSaintIndex.Seiya, PlatformInput.Right | PlatformInput.Left,
            rightBlocked, jumpPhase49: 4, halfPhase: 16, frameCounter3C: 1);
        Require(fallbackLeft.State.Horizontal.PlayerX == 0x1F,
            "Blocked free-steer Right falls through and accepts Left.");
    }

    private static void CheckDirectionalFamilies(PlatformStageMap stage)
    {
        var rightFamily = AirState(0x31, playerX: 0x20);

        var rightSame = PlatformAirControl.Step(
            stage, rightFamily, PlatformSaintIndex.Seiya, PlatformInput.Right, default,
            jumpPhase49: 4, halfPhase: 27, frameCounter3C: 1);
        Require(rightSame.State.Horizontal.PlayerX == 0x22,
            "$31 same-direction input uses $0388 accelerated amount (2 on Seiya odd frame).");

        var rightOpposite = PlatformAirControl.Step(
            stage, rightFamily, PlatformSaintIndex.Seiya, PlatformInput.Left, default,
            jumpPhase49: 4, halfPhase: 27, frameCounter3C: 1);
        Require(rightOpposite.State.Horizontal.PlayerX == 0x21,
            "$31 opposite input decelerates but still moves right.");

        var rightNeutral = PlatformAirControl.Step(
            stage, rightFamily, PlatformSaintIndex.Seiya, PlatformInput.None, default,
            jumpPhase49: 4, halfPhase: 27, frameCounter3C: 1);
        Require(rightNeutral.State.Horizontal.PlayerX == 0x21,
            "$31 neutral input uses base $0387 and remains rightward.");

        var leftFamily = AirState(0x32, playerX: 0x30);
        var leftSame = PlatformAirControl.Step(
            stage, leftFamily, PlatformSaintIndex.Seiya, PlatformInput.Left, default,
            jumpPhase49: 4, halfPhase: 22, frameCounter3C: 1);
        Require(leftSame.State.Horizontal.PlayerX == 0x2E,
            "$32 same-direction input uses $0388 and moves left by 2.");

        var leftOpposite = PlatformAirControl.Step(
            stage, leftFamily, PlatformSaintIndex.Seiya, PlatformInput.Right, default,
            jumpPhase49: 4, halfPhase: 22, frameCounter3C: 1);
        Require(leftOpposite.State.Horizontal.PlayerX == 0x2F,
            "$32 opposite input slows but cannot reverse sign.");

        var bothFamily = AirState(0x33, playerX: 0x30);
        var bothHeld = PlatformAirControl.Step(
            stage, bothFamily, PlatformSaintIndex.Seiya, PlatformInput.Right | PlatformInput.Left, default,
            jumpPhase49: 4, halfPhase: 22, frameCounter3C: 1);
        Require(bothHeld.State.Horizontal.PlayerX == 0x2F,
            "$33 belongs to left family; Right is checked first and selects opposite-input speed.");
    }

    private static void CheckCollisionCancellation(PlatformStageMap stage)
    {
        var rightFamily = AirState(0x31, playerX: 0x30);
        var lowerBlock = new PlatformCollisionDescriptors(
            FloorCenter: null, LowerRight: 0x80, UpperRight: null, FloorRight: null,
            LowerLeft: null, UpperLeft: null, FloorLeft: null, UpperCenter: null);
        var cancelled = PlatformAirControl.Step(
            stage, rightFamily, PlatformSaintIndex.Seiya, PlatformInput.Right, lowerBlock,
            jumpPhase49: 5, halfPhase: 27, frameCounter3C: 1);
        Require(cancelled.DirectionalFamilyCancelled && cancelled.State.ActionState4D == 0x30,
            "Directional side collision degrades family to $30 without ending vertical jump.");
        Require(cancelled.State.Horizontal.PlayerX == 0x30 && cancelled.State.HorizontalAmount43 == 0,
            "Collision cancellation leaves horizontal position and clears $43.");

        var floorOnly = lowerBlock with { LowerRight = null, FloorRight = 0x80 };
        var beforeHalf = PlatformAirControl.Step(
            stage, rightFamily, PlatformSaintIndex.Seiya, PlatformInput.Right, floorOnly,
            jumpPhase49: 10, halfPhase: 27, frameCounter3C: 1);
        Require(!beforeHalf.DirectionalFamilyCancelled && beforeHalf.State.Horizontal.PlayerX == 0x32,
            "Wide floor-right probe is ignored before midpoint.");

        var afterHalf = PlatformAirControl.Step(
            stage, rightFamily, PlatformSaintIndex.Seiya, PlatformInput.Right, floorOnly,
            jumpPhase49: 27, halfPhase: 27, frameCounter3C: 1);
        Require(afterHalf.DirectionalFamilyCancelled,
            "Wide floor-right probe participates at/after midpoint.");

        var leftBoundary = AirState(0x32, playerX: 0x10);
        var edgeCancelled = PlatformAirControl.Step(
            stage, leftBoundary, PlatformSaintIndex.Seiya, PlatformInput.None, default,
            jumpPhase49: 4, halfPhase: 22, frameCounter3C: 0);
        Require(edgeCancelled.DirectionalFamilyCancelled && edgeCancelled.State.Horizontal.PlayerX == 0x10,
            "Directional left candidate below $10 cancels to vertical family.");
    }

    private static void CheckCameraHandoff(PlatformStageMap stage)
    {
        // Airborne right uses candidate-based handoff: $7F + 1 starts camera and
        // leaves local player X at $7F, unlike the grounded pre-position check.
        var atHandoff = new PlatformAirControlState(
            new PlatformHorizontalState(0x7F, 0x00, 0x00, 0x40),
            ActionState4D: 0x31,
            HorizontalAmount43: 0);
        var result = PlatformAirControl.Step(
            stage, atHandoff, PlatformSaintIndex.Seiya, PlatformInput.Right, default,
            jumpPhase49: 4, halfPhase: 27, frameCounter3C: 0);
        Require(result.State.Horizontal.PlayerX == 0x7F && result.State.Horizontal.ScrollLow == 1,
            "Airborne candidate reaching $80 hands movement to camera.");
        Require(result.CameraMoved && result.State.HorizontalAmount43 == 1,
            "Camera-moving air frame preserves movement amount in $43.");

        var terminal = new PlatformAirControlState(
            new PlatformHorizontalState(
                0xDF,
                0xF8,
                PlatformHorizontalMotion.ScrollHighCapForSubstate(0x00),
                0x40),
            0x31,
            0);
        var stopped = PlatformAirControl.Step(
            stage, terminal, PlatformSaintIndex.Seiya, PlatformInput.None, default,
            jumpPhase49: 4, halfPhase: 27, frameCounter3C: 0);
        Require(stopped.State.Horizontal.PlayerX == 0xDF && !stopped.PlayerMoved,
            "Terminal air-right candidate at/over $E0 is not stored.");
    }

    private static void CheckSecondHalfCorrections(PlatformStageMap stage)
    {
        var state = AirState(0x30, playerX: 0x20);
        var leftWall = new PlatformCollisionDescriptors(
            FloorCenter: null, LowerRight: null, UpperRight: null, FloorRight: null,
            LowerLeft: 0x88, UpperLeft: null, FloorLeft: null, UpperCenter: null);
        var pushRight = PlatformAirControl.Step(
            stage, state, PlatformSaintIndex.Seiya, PlatformInput.None, leftWall,
            jumpPhase49: 16, halfPhase: 16, frameCounter3C: 0);
        Require(pushRight.State.Horizontal.PlayerX == 0x21,
            "Second-half no-input left-side contact nudges player right by 1.");

        var rightWall = leftWall with { LowerLeft = null, LowerRight = 0x80 };
        var pushLeft = PlatformAirControl.Step(
            stage, state, PlatformSaintIndex.Seiya, PlatformInput.None, rightWall,
            jumpPhase49: 16, halfPhase: 16, frameCounter3C: 0);
        Require(pushLeft.State.Horizontal.PlayerX == 0x1F,
            "Second-half no-input right-side contact nudges player left by 1.");
    }

    private static PlatformAirControlState AirState(byte action, byte playerX) => new(
        new PlatformHorizontalState(playerX, 0, 0, 0x40),
        ActionState4D: action,
        HorizontalAmount43: 0);

    private static PlatformStageMap EmptyStage() => new(
        0x00,
        [new PlatformStagePage(0, new byte[PlatformStagePage.DescriptorCount])]);

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException($"AirControl self-test failed: {message}");
    }
}

using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class PlatformHorizontalMotionSelfTest
{
    [ModuleInitializer]
    internal static void Run()
    {
        static void Equal<T>(T expected, T actual, string name) where T : notnull
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException($"{name}: expected {expected}, got {actual}");
        }

        static void True(bool value, string name)
        {
            if (!value)
                throw new InvalidOperationException($"{name}: expected true");
        }

        var empty = new byte[PlatformStagePage.DescriptorCount];
        var stage = new PlatformStageMap(
            substate: 0,
            pages:
            [
                new PlatformStagePage(0, empty),
                new PlatformStagePage(1, empty),
                new PlatformStagePage(2, empty),
                new PlatformStagePage(3, empty),
                new PlatformStagePage(4, empty),
                new PlatformStagePage(5, empty),
            ]);
        var open = new PlatformCollisionDescriptors(null, 0, 0, null, 0, 0, null, null);

        // Original scroll-high cap is page_count - 2 for every reconstructed substate.
        Equal((byte)0x04, PlatformHorizontalMotion.ScrollHighCapForSubstate(0x00), "Stage 00 camera cap");
        Equal((byte)0x0E, PlatformHorizontalMotion.ScrollHighCapForSubstate(0x0B), "Stage 0B camera cap");
        Equal((byte)0x01, PlatformHorizontalMotion.ScrollHighCapForSubstate(0x0F), "Stage 0F camera cap");
        Equal((byte)0x00, PlatformHorizontalMotion.ScrollHighCapForSubstate(0x11), "Stage 11 camera cap");

        // Before the center threshold, Right moves screen-local player X.
        var local = PlatformHorizontalMotion.StepGrounded(
            stage,
            new PlatformHorizontalState(0x7F, 0x00, 0x00, 0x00),
            PlatformInput.Right,
            open,
            groundedStep: 1);
        Equal((byte)0x80, local.State.PlayerX, "Right reaches handoff threshold");
        Equal((byte)0x00, local.State.ScrollLow, "No scroll before handoff");
        True(local.PlayerMoved && !local.CameraMoved, "Local movement before handoff");
        True(local.State.FacingRight, "Right input updates facing");

        // At/after $80, Right advances scroll instead of local X.
        var camera = PlatformHorizontalMotion.StepGrounded(
            stage,
            local.State,
            PlatformInput.Right,
            open,
            groundedStep: 1);
        Equal((byte)0x80, camera.State.PlayerX, "Player anchors during camera handoff");
        Equal((byte)0x01, camera.State.ScrollLow, "Camera advances at handoff");
        True(!camera.PlayerMoved && camera.CameraMoved, "Camera movement after handoff");

        // Low-byte carry increments the scroll-high/page byte.
        var carry = PlatformHorizontalMotion.StepGrounded(
            stage,
            new PlatformHorizontalState(0x80, 0xFF, 0x02, 0x40),
            PlatformInput.Right,
            open,
            groundedStep: 1);
        Equal((byte)0x00, carry.State.ScrollLow, "Scroll low wraps");
        Equal((byte)0x03, carry.State.ScrollHigh, "Scroll high receives carry");

        // At the final camera region, local movement resumes and scroll freezes.
        var finalScreen = PlatformHorizontalMotion.StepGrounded(
            stage,
            new PlatformHorizontalState(0x80, 0xF8, 0x04, 0x40),
            PlatformInput.Right,
            open,
            groundedStep: 1);
        Equal((byte)0x81, finalScreen.State.PlayerX, "Final screen resumes local movement");
        Equal((byte)0xF8, finalScreen.State.ScrollLow, "Final screen freezes scroll low");
        True(finalScreen.PlayerMoved && !finalScreen.CameraMoved, "Final-screen local movement");

        // Final local boundary is checked before addition, matching the 6502 path.
        var rightEdge = PlatformHorizontalMotion.StepGrounded(
            stage,
            new PlatformHorizontalState(0xE0, 0xF8, 0x04, 0x40),
            PlatformInput.Right,
            open,
            groundedStep: 1);
        True(rightEdge.EdgeBlocked, "Right edge blocks further grounded movement");
        Equal((byte)0xE0, rightEdge.State.PlayerX, "Right edge leaves X unchanged");

        // Grounded Left never rewinds the camera.
        var left = PlatformHorizontalMotion.StepGrounded(
            stage,
            new PlatformHorizontalState(0x40, 0x88, 0x03, 0x40),
            PlatformInput.Left,
            open,
            groundedStep: 1);
        Equal((byte)0x3F, left.State.PlayerX, "Left moves player locally");
        Equal((byte)0x88, left.State.ScrollLow, "Left does not rewind scroll low");
        Equal((byte)0x03, left.State.ScrollHigh, "Left does not rewind scroll high");
        True(!left.State.FacingRight, "Left updates facing");

        // When both directions are pressed, Right wins because it is tested first.
        var both = PlatformHorizontalMotion.StepGrounded(
            stage,
            new PlatformHorizontalState(0x20, 0, 0, 0),
            PlatformInput.Left | PlatformInput.Right,
            open,
            groundedStep: 1);
        Equal((byte)0x21, both.State.PlayerX, "Right priority when both directions held");
        True(both.State.FacingRight, "Right priority updates facing right");

        // Grounded upper-side collision is special to substates $0C-$0E.
        var upperRightOnly = new PlatformCollisionDescriptors(null, 0, 0x80, null, 0, 0, null, null);
        True(PlatformStageMap.CanMoveRight(upperRightOnly, 0x00), "Main stage ignores grounded upper-right blocker");
        True(!PlatformStageMap.CanMoveRight(upperRightOnly, 0x0C), "$0C uses grounded upper-right blocker");
        var upperLeftOnly = new PlatformCollisionDescriptors(null, 0, 0, null, 0, 0x88, null, null);
        True(PlatformStageMap.CanMoveLeft(upperLeftOnly, 0x00), "Main stage ignores grounded upper-left blocker");
        True(!PlatformStageMap.CanMoveLeft(upperLeftOnly, 0x0E), "$0E uses grounded upper-left blocker");

        // Per-Saint movement increments from $9211: Shun owns the distinct grounded profile.
        Equal(
            new PlatformMovementIncrements(1, 1, 0),
            PlatformMovementIncrements.FromFrame(PlatformSaintIndex.Seiya, 0),
            "Seiya even-frame movement increments");
        Equal(
            new PlatformMovementIncrements(1, 2, 1),
            PlatformMovementIncrements.FromFrame(PlatformSaintIndex.Seiya, 1),
            "Seiya odd-frame movement increments");
        Equal(
            new PlatformMovementIncrements(1, 2, 1),
            PlatformMovementIncrements.FromFrame(PlatformSaintIndex.Shun, 0),
            "Shun even-frame movement increments");
        Equal(
            new PlatformMovementIncrements(2, 2, 1),
            PlatformMovementIncrements.FromFrame(PlatformSaintIndex.Shun, 1),
            "Shun odd-frame movement increments");
    }
}

using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class AirborneHorizontalMotionChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        var stage = SyntheticStage(substate: 0);
        var open = Probes();
        var increments = new PlatformMovementIncrements(Grounded0387: 1, Airborne0388: 2, Airborne0389: 1);

        var right = PlatformAirborneHorizontalMotion.Step(
            stage,
            new PlatformHorizontalState(0x60, 0, 0, 0x40),
            PlatformInput.Right,
            open,
            actionState: 0x31,
            jumpPhase: 8,
            halfPhase: 16,
            increments,
            frameCounter3C: 0);
        Require(right.State.PlayerX == 0x62, "right trajectory uses 0388 while Right held");
        Require(right.ActionState == 0x31, "right trajectory remains directional");

        var rightCountersteer = PlatformAirborneHorizontalMotion.Step(
            stage,
            new PlatformHorizontalState(0x60, 0, 0, 0x40),
            PlatformInput.Left,
            open,
            actionState: 0x31,
            jumpPhase: 8,
            halfPhase: 16,
            increments,
            frameCounter3C: 0);
        Require(rightCountersteer.State.PlayerX == 0x61, "opposite input slows right trajectory with 0389");

        var leftBoth = PlatformAirborneHorizontalMotion.Step(
            stage,
            new PlatformHorizontalState(0x60, 0, 0, 0),
            PlatformInput.Left | PlatformInput.Right,
            open,
            actionState: 0x33,
            jumpPhase: 8,
            halfPhase: 16,
            increments,
            frameCounter3C: 0);
        Require(leftBoth.State.PlayerX == 0x5F, "$33 follows left trajectory and both-held selects 0389");

        var rightCollision = PlatformAirborneHorizontalMotion.Step(
            stage,
            new PlatformHorizontalState(0x60, 0, 0, 0x40),
            PlatformInput.Right,
            Probes(lowerRight: 0x80),
            actionState: 0x31,
            jumpPhase: 8,
            halfPhase: 16,
            increments,
            frameCounter3C: 0);
        Require(rightCollision.State.PlayerX == 0x60, "right collision prevents movement");
        Require(rightCollision.ActionState == 0x30, "directional collision collapses to vertical jump family");
        Require(rightCollision.DirectionalTrajectoryCollapsed, "directional collision collapse flag");

        var cameraHandoff = PlatformAirborneHorizontalMotion.Step(
            stage,
            new PlatformHorizontalState(0x7F, 0x20, 0, 0x40),
            PlatformInput.Right,
            open,
            actionState: 0x31,
            jumpPhase: 8,
            halfPhase: 16,
            increments,
            frameCounter3C: 0);
        Require(cameraHandoff.State.PlayerX == 0x7F, "airborne camera handoff holds player X");
        Require(cameraHandoff.State.ScrollLow == 0x22, "airborne camera handoff advances scroll");
        Require(cameraHandoff.CameraMoved, "airborne camera movement flag");

        var finalEdge = PlatformAirborneHorizontalMotion.Step(
            stage,
            new PlatformHorizontalState(0xDE, 0xF8, 0x04, 0x40),
            PlatformInput.Right,
            open,
            actionState: 0x31,
            jumpPhase: 8,
            halfPhase: 16,
            increments,
            frameCounter3C: 0);
        Require(finalEdge.State.PlayerX == 0xDE, "final camera region preserves player at E0 boundary");
        Require(finalEdge.EdgeBlocked, "final airborne edge flag");

        var verticalDrift = PlatformAirborneHorizontalMotion.Step(
            stage,
            new PlatformHorizontalState(0x60, 0, 0, 0x40),
            PlatformInput.Right,
            open,
            actionState: 0x30,
            jumpPhase: 6,
            halfPhase: 16,
            increments,
            frameCounter3C: 1);
        Require(verticalDrift.State.PlayerX == 0x61, "vertical jump drift uses frame parity");

        var verticalNoDriftEven = PlatformAirborneHorizontalMotion.Step(
            stage,
            new PlatformHorizontalState(0x60, 0, 0, 0x40),
            PlatformInput.Right,
            open,
            actionState: 0x30,
            jumpPhase: 6,
            halfPhase: 16,
            increments,
            frameCounter3C: 0);
        Require(verticalNoDriftEven.State.PlayerX == 0x60, "vertical jump even frame has zero drift");

        var descendingCorrection = PlatformAirborneHorizontalMotion.Step(
            stage,
            new PlatformHorizontalState(0x60, 0, 0, 0x40),
            PlatformInput.Right,
            Probes(lowerRight: 0x80, lowerLeft: 0x88),
            actionState: 0x30,
            jumpPhase: 20,
            halfPhase: 16,
            increments,
            frameCounter3C: 1);
        Require(descendingCorrection.State.PlayerX == 0x61, "blocked vertical drift falls through to descending side correction");
        Require(descendingCorrection.LandingSideCorrection, "descending side correction flag");

        var leftBoundary = PlatformAirborneHorizontalMotion.Step(
            stage,
            new PlatformHorizontalState(0x10, 0, 0, 0),
            PlatformInput.Left,
            open,
            actionState: 0x32,
            jumpPhase: 8,
            halfPhase: 16,
            new PlatformMovementIncrements(1, 2, 1),
            frameCounter3C: 0);
        Require(leftBoundary.State.PlayerX == 0x10, "left boundary blocks directional movement");
        Require(leftBoundary.ActionState == 0x30, "left boundary collapses directional trajectory");
    }

    private static PlatformStageMap SyntheticStage(int substate)
    {
        var page = new PlatformStagePage(0, new byte[PlatformStagePage.DescriptorCount]);
        return new PlatformStageMap(substate, [page]);
    }

    private static PlatformCollisionDescriptors Probes(
        byte? floorCenter = 0,
        byte? lowerRight = 0,
        byte? upperRight = 0,
        byte? floorRight = 0,
        byte? lowerLeft = 0,
        byte? upperLeft = 0,
        byte? floorLeft = 0,
        byte? upperCenter = 0) =>
        new(floorCenter, lowerRight, upperRight, floorRight, lowerLeft, upperLeft, floorLeft, upperCenter);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Airborne horizontal self-test failed: {label}");
    }
}

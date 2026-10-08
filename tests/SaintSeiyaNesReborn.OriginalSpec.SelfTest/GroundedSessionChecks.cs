using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class GroundedSessionChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        var emptyPage = new PlatformStagePage(0, new byte[PlatformStagePage.DescriptorCount]);
        var stage = new PlatformStageMap(0x00, [emptyPage]);

        // Exit is checked before bank-3 movement. Reaching $D0 this frame exits next frame.
        var nearExit = new PlatformGroundedState(
            PlatformSaintIndex.Seiya,
            PlayerY: 0x40,
            FrameCounter3C: 0,
            Horizontal: new PlatformHorizontalState(
                PlayerX: 0xCF,
                ScrollLow: 0xF8,
                ScrollHigh: PlatformHorizontalMotion.ScrollHighCapForSubstate(0x00),
                Facing42: 0x40));

        var reachGate = PlatformGroundedSession.Step(stage, nearExit, PlatformInput.Right);
        Require(!reachGate.Exited, "Exit must not fire on the same frame movement reaches $D0.");
        Require(reachGate.State.Horizontal.PlayerX == 0xD0, "First frame must move player from $CF to $D0.");
        Require(reachGate.State.FrameCounter3C == 1, "Normal grounded frame must advance $3C.");

        var leave = PlatformGroundedSession.Step(stage, reachGate.State, PlatformInput.Right);
        Require(leave.Exited, "Next frame must observe the exit gate at $D0/$40.");
        Require(leave.ExitTransition == PlatformExitTransitionKind.State3DReload, "Main stage uses $3D reload exit.");
        Require(leave.HorizontalStep is null, "Successful exit bypasses horizontal simulation.");
        Require(leave.State.FrameCounter3C == 1, "Successful exit bypasses normal frame-counter advance.");

        // Player-to-camera handoff at screen X $80.
        var atTrackingBand = new PlatformGroundedState(
            PlatformSaintIndex.Seiya,
            PlayerY: 0x50,
            FrameCounter3C: 0,
            Horizontal: new PlatformHorizontalState(0x80, 0x00, 0x00, 0x40));
        var cameraStep = PlatformGroundedSession.Step(stage, atTrackingBand, PlatformInput.Right);
        Require(cameraStep.State.Horizontal.PlayerX == 0x80, "Tracking-band frame keeps local player X.");
        Require(cameraStep.State.Horizontal.ScrollLow == 0x01, "Tracking-band frame advances camera by Seiya step 1.");
        Require(cameraStep.HorizontalStep?.CameraMoved == true, "Tracking-band result must identify camera movement.");

        // Shun's odd-frame grounded increment is 2; ordinary Saints remain 1.
        var shun = new PlatformGroundedState(
            PlatformSaintIndex.Shun,
            PlayerY: 0x50,
            FrameCounter3C: 1,
            Horizontal: new PlatformHorizontalState(0x20, 0x00, 0x00, 0x40));
        var shunStep = PlatformGroundedSession.Step(stage, shun, PlatformInput.Right);
        Require(shunStep.State.Horizontal.PlayerX == 0x22, "Shun odd-frame grounded step must be 2.");

        var seiya = shun with { Saint = PlatformSaintIndex.Seiya };
        var seiyaStep = PlatformGroundedSession.Step(stage, seiya, PlatformInput.Right);
        Require(seiyaStep.State.Horizontal.PlayerX == 0x21, "Ordinary Saint grounded step remains 1.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException($"GroundedSession self-test failed: {message}");
    }
}

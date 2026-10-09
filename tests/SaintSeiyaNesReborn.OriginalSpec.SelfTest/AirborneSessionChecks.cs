using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class AirborneSessionChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        var openStage = Stage();

        var standing = PlatformAirborneSession.Step(
            openStage,
            PlatformSaintIndex.Seiya,
            State(x: 0x60, y: 0x60, action: 0x30, phase: 1, frame: 1),
            PlatformInput.Right);
        Require(standing.State.PlayerY == 0x58, "standing session applies first vertical delta");
        Require(standing.State.Horizontal.PlayerX == 0x61, "standing session applies 1px odd-frame air drift");
        Require(standing.State.JumpPhase49 == 2, "standing session advances phase");
        Require(standing.State.FrameCounter3C == 2, "standing session advances frame counter after simulation");
        Require(standing.Horizontal is not null, "standing session executes horizontal path after vertical");

        var directional = PlatformAirborneSession.Step(
            openStage,
            PlatformSaintIndex.Seiya,
            State(x: 0x60, y: 0x60, action: 0x31, phase: 1, frame: 1),
            PlatformInput.Right);
        Require(directional.State.PlayerY == 0x5C, "directional session uses directional vertical profile");
        Require(directional.State.Horizontal.PlayerX == 0x62, "directional session uses 0388 horizontal step");
        Require(directional.State.ActionState == 0x31, "unblocked directional state persists");

        var collisionStage = Stage((0x50, 0x70, 0x80)); // lower-right at x=$40,y=$60 fixture
        var collapsed = PlatformAirborneSession.Step(
            collisionStage,
            PlatformSaintIndex.Seiya,
            State(x: 0x40, y: 0x60, action: 0x31, phase: 1, frame: 1),
            PlatformInput.Right);
        Require(collapsed.State.PlayerY == 0x5C, "horizontal collision happens after directional vertical motion");
        Require(collapsed.State.Horizontal.PlayerX == 0x40, "airborne collision blocks X movement");
        Require(collapsed.State.ActionState == 0x30, "airborne collision collapses directional action only");
        Require(collapsed.State.JumpPhase49 == 2, "airborne collision preserves progressed jump phase");

        var landingStage = Stage((0x48, 0x72, 0x90));
        var landed = PlatformAirborneSession.Step(
            landingStage,
            PlatformSaintIndex.Seiya,
            State(x: 0x40, y: 0x52, action: 0x30, phase: 16, frame: 4, support: 0x55),
            PlatformInput.Right);
        Require(landed.Vertical.Landed, "session landing recognized before vertical increment");
        Require(landed.State.PlayerY == 0x50, "session landing snap");
        Require(landed.State.ActionState == 0 && landed.State.JumpPhase49 == 0, "session landing clears jump state");
        Require(landed.State.Support038D == 0, "session landing clears support state");
        Require(landed.Horizontal is null, "landing skips horizontal air control");
        Require(landed.State.Horizontal.PlayerX == 0x40, "landing frame does not apply requested right input");
        Require(landed.State.FrameCounter3C == 5, "landing still reaches normal frame-counter advance");

        var ceilingStage = Stage((0x48, 0x60, 0xE0));
        var ceiling = PlatformAirborneSession.Step(
            ceilingStage,
            PlatformSaintIndex.Seiya,
            State(x: 0x40, y: 0x60, action: 0x30, phase: 1, frame: 1),
            PlatformInput.Right);
        Require(ceiling.State.PlayerY == 0x58, "ceiling session applies vertical delta first");
        Require(ceiling.Vertical.CeilingInterrupted, "ceiling session detects full-solid head descriptor");
        Require(ceiling.State.ActionState == 0x50 && ceiling.State.JumpPhase49 == 0, "ceiling switches to $50 and clears phase");
        Require(ceiling.Horizontal is null, "ceiling interruption skips horizontal air control");

        var hazardStage = Stage();
        var hazard = PlatformAirborneSession.Step(
            hazardStage,
            PlatformSaintIndex.Seiya,
            State(x: 0x40, y: 0xA0, action: 0x30, phase: 16, frame: 9),
            PlatformInput.None);
        Require(hazard.Vertical.HazardTriggered, "session propagates lower-screen hazard");
        Require(hazard.State.ActionState == 0x80 && hazard.State.Special76 == 0x80, "session preserves hazard state");
        Require(hazard.Horizontal is null, "hazard terminates horizontal air path");

        var camera = PlatformAirborneSession.Step(
            openStage,
            PlatformSaintIndex.Seiya,
            State(x: 0x7F, y: 0x60, action: 0x31, phase: 1, frame: 1, scrollLow: 0x20),
            PlatformInput.Right);
        Require(camera.State.Horizontal.PlayerX == 0x7F, "session camera handoff holds player X");
        Require(camera.State.Horizontal.ScrollLow == 0x22, "session camera handoff advances scroll by airborne step");
        Require(camera.Horizontal?.CameraMoved == true, "session exposes camera movement result");
    }

    private static PlatformAirborneSessionState State(
        byte x,
        byte y,
        byte action,
        byte phase,
        byte frame,
        byte yHigh = 0,
        byte highSelector = 0,
        byte support = 0,
        byte special76 = 0,
        byte scrollLow = 0,
        byte scrollHigh = 0,
        byte facing = 0x40) =>
        new(
            new PlatformHorizontalState(x, scrollLow, scrollHigh, facing),
            y,
            yHigh,
            action,
            phase,
            highSelector,
            support,
            special76,
            frame);

    private static PlatformStageMap Stage(params (int X, int Y, byte Descriptor)[] cells)
    {
        var descriptors = new byte[PlatformStagePage.DescriptorCount];
        foreach (var (x, y, descriptor) in cells)
        {
            var col = x / PlatformStageMap.CellSizePixels;
            var row = y / PlatformStageMap.CellSizePixels;
            descriptors[row * PlatformStagePage.Columns + col] = descriptor;
        }
        return new PlatformStageMap(0, [new PlatformStagePage(0, descriptors)]);
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Airborne session self-test failed: {label}");
    }
}

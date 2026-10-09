using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class AirborneVerticalMotionChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        var open = Probes();

        var firstStanding = PlatformAirborneVerticalMotion.Step(
            PlatformSaintIndex.Seiya,
            State(y: 0x60, action: 0x30, phase: 1),
            open);
        Require(firstStanding.State.JumpPhase49 == 2, "standing phase increments before table lookup");
        Require(firstStanding.State.PlayerY == 0x58, "standing first consumed delta rises 8 pixels");
        Require(firstStanding.PhaseLimit == 32 && firstStanding.HalfPhase == 16, "standing phase constants");
        Require(firstStanding.ContinueHorizontal, "ordinary airborne frame continues to horizontal control");

        var firstHigh = PlatformAirborneVerticalMotion.Step(
            PlatformSaintIndex.Seiya,
            State(y: 0x60, action: 0x30, phase: 1, highSelector: 0x30),
            open);
        Require(firstHigh.State.PlayerY == 0x57, "Seiya high jump first delta rises 9 pixels");
        Require(firstHigh.PhaseLimit == 60 && firstHigh.HalfPhase == 30, "Seiya high phase constants");

        var firstDirectional = PlatformAirborneVerticalMotion.Step(
            PlatformSaintIndex.Seiya,
            State(y: 0x60, action: 0x31, phase: 1),
            open);
        Require(firstDirectional.State.PlayerY == 0x5C, "directional first delta rises 4 pixels");
        Require(firstDirectional.PhaseLimit == 54 && firstDirectional.HalfPhase == 27, "Seiya directional phase constants");

        // Landing is evaluated before phase increment/vertical movement once current phase reaches half.
        var landing = PlatformAirborneVerticalMotion.Step(
            PlatformSaintIndex.Seiya,
            State(y: 0x52, action: 0x30, phase: 16, support: 0x7F),
            Probes(floorCenter: 0x90));
        Require(landing.Landed, "ordinary floor landing accepted at half phase");
        Require(landing.State.PlayerY == 0x50, "ordinary landing snaps to row boundary");
        Require(landing.State.JumpPhase49 == 0, "landing clears jump phase");
        Require(landing.State.ActionState == 0x00, "landing clears action state");
        Require(landing.State.Support038D == 0, "landing clears $038D");
        Require(!landing.ContinueHorizontal, "landing terminates airborne path before horizontal control");

        var halfFloor = PlatformAirborneVerticalMotion.Step(
            PlatformSaintIndex.Seiya,
            State(y: 0x5A, action: 0x30, phase: 16),
            Probes(floorCenter: 0xF0));
        Require(halfFloor.Landed && halfFloor.State.PlayerY == 0x58, "$F0 floor snaps to half-row height");

        var dynamicFloor = PlatformAirborneVerticalMotion.Step(
            PlatformSaintIndex.Seiya,
            State(y: 0x52, action: 0x30, phase: 16),
            Probes(floorCenter: 0xFF),
            dynamicFloorY039B: 0x47);
        Require(dynamicFloor.Landed && dynamicFloor.State.PlayerY == 0x47, "$FF uses dynamic floor Y");

        var lowerSpecial = PlatformAirborneVerticalMotion.Step(
            PlatformSaintIndex.Seiya,
            State(y: 0x88, action: 0x30, phase: 16, special76: 0),
            Probes(floorCenter: 0xF8));
        Require(lowerSpecial.Landed, "$F8 lower-screen special lands");
        Require(lowerSpecial.State.PlayerY == 0x88, "$F8 lower-screen target Y");
        Require(lowerSpecial.State.Special76 == 1, "$F8 initializes $76 when zero");

        var lowerSpecialPreserves76 = PlatformAirborneVerticalMotion.Step(
            PlatformSaintIndex.Seiya,
            State(y: 0x88, action: 0x30, phase: 16, special76: 7),
            Probes(floorCenter: 0xF9));
        Require(lowerSpecialPreserves76.State.Special76 == 7, "$F9 preserves nonzero $76");

        var hazard = PlatformAirborneVerticalMotion.Step(
            PlatformSaintIndex.Seiya,
            State(y: 0xA0, action: 0x30, phase: 16, yHigh: 0),
            Probes(floorCenter: 0x00));
        Require(hazard.HazardTriggered, "lower-screen no-support hazard triggers");
        Require(hazard.State.ActionState == 0x80, "hazard enters $80 action family");
        Require(hazard.State.Special76 == 0x80, "hazard sets $76=$80");
        Require(hazard.State.JumpPhase49 == 0, "hazard clears jump phase");
        Require(!hazard.ContinueHorizontal, "hazard terminates airborne control");

        var highPageAvoidsHazard = PlatformAirborneVerticalMotion.Step(
            PlatformSaintIndex.Seiya,
            State(y: 0xA0, action: 0x30, phase: 16, yHigh: 1),
            Probes(floorCenter: 0x00));
        Require(!highPageAvoidsHazard.HazardTriggered, "nonzero $41 bypasses lower-screen hazard branch");
        Require(highPageAvoidsHazard.ContinueHorizontal, "nonzero $41 continues airborne frame");

        var terminal = PlatformAirborneVerticalMotion.Step(
            PlatformSaintIndex.Seiya,
            State(y: 0x60, action: 0x30, phase: 31),
            open);
        Require(terminal.UsedTerminalFall, "phase limit switches to fixed terminal fall");
        Require(terminal.State.JumpPhase49 == 32, "terminal fall retains incremented phase");
        Require(terminal.State.PlayerY == 0x63, "terminal fall adds 3 screen pixels");
        Require(terminal.ScreenYDeltaApplied == 3, "terminal fall delta is reported");

        var terminalCarry = PlatformAirborneVerticalMotion.Step(
            PlatformSaintIndex.Seiya,
            State(y: 0xFE, action: 0x30, phase: 31, yHigh: 7),
            open);
        Require(terminalCarry.State.PlayerY == 0x01, "terminal fall wraps Y byte on carry");
        Require(terminalCarry.State.PlayerYHigh41 == 0, "terminal fall carry clears $41 exactly like original");

        var ceiling = PlatformAirborneVerticalMotion.Step(
            PlatformSaintIndex.Seiya,
            State(y: 0x60, action: 0x30, phase: 1),
            Probes(upperCenter: 0xE0));
        Require(ceiling.State.PlayerY == 0x58, "ceiling test happens after vertical delta");
        Require(ceiling.CeilingInterrupted, "$E0 ceiling interrupts jump");
        Require(ceiling.State.JumpPhase49 == 0, "ceiling clears jump phase");
        Require(ceiling.State.ActionState == 0x50, "ceiling enters $50 fall/drop state");
        Require(!ceiling.ContinueHorizontal, "ceiling skips horizontal air control");

        var risingBorrow = PlatformAirborneVerticalMotion.Step(
            PlatformSaintIndex.Seiya,
            State(y: 0x03, action: 0x30, phase: 1, yHigh: 1),
            open);
        Require(risingBorrow.State.PlayerY == 0xFB, "rising subtraction wraps low Y byte");
        Require(risingBorrow.State.PlayerYHigh41 == 0, "rising borrow decrements $41");

        var zeroPhase = PlatformAirborneVerticalMotion.Step(
            PlatformSaintIndex.Seiya,
            State(y: 0x60, action: 0x30, phase: 0),
            open);
        Require(zeroPhase.State.PlayerY == 0x60 && !zeroPhase.ContinueHorizontal, "zero jump phase returns immediately");
    }

    private static PlatformAirborneVerticalState State(
        byte y,
        byte action,
        byte phase,
        byte yHigh = 0,
        byte highSelector = 0,
        byte support = 0,
        byte special76 = 0) =>
        new(y, yHigh, action, phase, highSelector, support, special76);

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
            throw new InvalidOperationException($"Airborne vertical self-test failed: {label}");
    }
}

using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;
using SaintSeiyaNesReborn.Reborn.Core;
using SaintSeiyaNesReborn.Reborn.OriginalBridge;

internal static class StandingJumpAirControlChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        var airProfile = OriginalSpecStandingJumpAirControlBridge.Profile;
        Require(
            airProfile == new RebornStandingJumpAirControlProfile(0, 1),
            "canonical standing-jump air-control profile must project to even 0 / odd 1 drift");

        var oddRight = RebornStandingJumpAirControl.Step(
            new RebornStandingJumpAirState(100, RebornFacing.Left, RebornMotionPhase.Odd),
            RebornHorizontalInput.Right,
            airProfile);
        Require(oddRight.AppliedDelta == 1 && oddRight.State.WorldX == 101, "odd Right must drift +1 world pixel");
        Require(oddRight.State.Facing == RebornFacing.Left, "air-control input must not rotate takeoff facing");
        Require(oddRight.State.Phase == RebornMotionPhase.Even, "odd air-control tick must advance to even phase");

        var oddLeft = RebornStandingJumpAirControl.Step(
            new RebornStandingJumpAirState(100, RebornFacing.Right, RebornMotionPhase.Odd),
            RebornHorizontalInput.Left,
            airProfile);
        Require(oddLeft.AppliedDelta == -1 && oddLeft.State.WorldX == 99, "odd Left must drift -1 world pixel");
        Require(oddLeft.State.Facing == RebornFacing.Right, "Left air-control must preserve takeoff facing");

        var evenRight = RebornStandingJumpAirControl.Step(
            new RebornStandingJumpAirState(100, RebornFacing.Left, RebornMotionPhase.Even),
            RebornHorizontalInput.Right,
            airProfile);
        Require(evenRight.AppliedDelta == 0 && evenRight.State.WorldX == 100, "even directional input must have zero canonical drift");
        Require(evenRight.State.Phase == RebornMotionPhase.Odd, "even air-control tick must advance to odd phase");

        Require(
            OriginalSpecStandingJumpAirControlBridge.FromCanonicalInput(PlatformInput.Left | PlatformInput.Right)
                == RebornHorizontalInput.Right,
            "canonical simultaneous airborne directions must preserve Right-before-Left priority at the bridge");

        var standingProfile = OriginalSpecStandingJumpBridge.ProfileFor(PlatformSaintIndex.Seiya);
        var rejectedDirectionalTakeoff = false;
        try
        {
            RebornStandingJumpMotion.Initiate(
                groundedVerticalPosition: 1000,
                worldX: 100,
                facing: RebornFacing.Left,
                phase: RebornMotionPhase.Even,
                takeoffHorizontalInput: RebornHorizontalInput.Right,
                verticalProfile: standingProfile,
                horizontalProfile: airProfile);
        }
        catch (InvalidOperationException)
        {
            rejectedDirectionalTakeoff = true;
        }
        Require(rejectedDirectionalTakeoff, "standing-jump composition must reject directional takeoff rather than silently model a directional jump");

        var stage = BuildOpenStage();
        var initiation = PlatformJumpInitiation.Step(
            new PlatformJumpInitiationState(
                ActionState4D: 0,
                JumpPhase49: 0,
                JumpButtonLatch4A: 0,
                HighJumpSelector038A: 0,
                Support038D: 0),
            PlatformInput.A);
        Require(initiation.StartedJump && initiation.State.ActionState4D == 0x30, "fixture must begin as canonical ordinary standing jump");

        var canonicalTakeoffY = (byte)0x80;
        var canonical = new PlatformAirborneSessionState(
            Horizontal: new PlatformHorizontalState(
                PlayerX: 0x7F,
                ScrollLow: 0x20,
                ScrollHigh: 0,
                Facing42: 0),
            PlayerY: canonicalTakeoffY,
            PlayerYHigh41: 0,
            ActionState: initiation.State.ActionState4D,
            JumpPhase49: initiation.State.JumpPhase49,
            HighJumpSelector038A: initiation.State.HighJumpSelector038A,
            Support038D: initiation.State.Support038D,
            Special76: 0,
            FrameCounter3C: 0);

        var inputs = Enumerable.Range(0, standingProfile.TickCount)
            .Select(tick => tick switch
            {
                0 => RebornHorizontalInput.Neutral,
                _ when tick % 6 is 1 or 2 or 3 => RebornHorizontalInput.Right,
                _ when tick % 6 is 4 or 5 => RebornHorizontalInput.Left,
                _ => RebornHorizontalInput.Neutral,
            })
            .ToArray();

        const int semanticTakeoffY = 1000;
        var canonicalInitialWorldX = canonical.Horizontal.WorldPlayerX;
        RebornStandingJumpMotionStepResult reborn = default;
        RebornStandingJumpMotionStepResult deterministic = default;
        var sawCanonicalPlayerMove = false;
        var sawCanonicalCameraMove = false;

        for (var tick = 0; tick < inputs.Length; tick++)
        {
            var semanticInput = inputs[tick];
            var canonicalInput = ToCanonicalInput(semanticInput);
            var beforeWorldX = canonical.Horizontal.WorldPlayerX;
            var canonicalStep = PlatformAirborneSession.Step(
                stage,
                PlatformSaintIndex.Seiya,
                canonical,
                canonicalInput);

            Require(canonicalStep.Horizontal is not null, $"air-control tick {tick + 1}: canonical horizontal path must execute");
            var canonicalHorizontal = canonicalStep.Horizontal.Value;
            sawCanonicalPlayerMove |= canonicalHorizontal.PlayerMoved;
            sawCanonicalCameraMove |= canonicalHorizontal.CameraMoved;

            if (tick == 0)
            {
                var initialAir = OriginalSpecStandingJumpAirControlBridge.FromCanonicalState(
                    canonical.Horizontal,
                    canonical.FrameCounter3C);
                reborn = RebornStandingJumpMotion.Initiate(
                    semanticTakeoffY,
                    initialAir.WorldX,
                    initialAir.Facing,
                    initialAir.Phase,
                    semanticInput,
                    standingProfile,
                    airProfile);
                deterministic = RebornStandingJumpMotion.Initiate(
                    semanticTakeoffY,
                    initialAir.WorldX,
                    initialAir.Facing,
                    initialAir.Phase,
                    semanticInput,
                    standingProfile,
                    airProfile);
            }
            else
            {
                reborn = RebornStandingJumpMotion.Step(
                    reborn.State,
                    semanticInput,
                    standingProfile,
                    airProfile);
                deterministic = RebornStandingJumpMotion.Step(
                    deterministic.State,
                    semanticInput,
                    standingProfile,
                    airProfile);
            }

            var afterWorldX = canonicalStep.State.Horizontal.WorldPlayerX;
            var expectedAir = OriginalSpecStandingJumpAirControlBridge.FromCanonicalState(
                canonicalStep.State.Horizontal,
                canonicalStep.State.FrameCounter3C);
            var expectedHorizontalDelta = afterWorldX - beforeWorldX;
            var expectedRise = OriginalSpecStandingJumpBridge.RiseFromCanonicalScreenDelta(
                canonicalStep.Vertical.ScreenYDeltaApplied);
            var canonicalCumulativeRise = canonicalTakeoffY - canonicalStep.State.PlayerY;

            Require(
                reborn.AppliedRisePixels == expectedRise,
                $"air-control tick {tick + 1}: vertical-first composition diverged from canonical rise");
            Require(
                reborn.State.Vertical.VerticalPosition - semanticTakeoffY == canonicalCumulativeRise,
                $"air-control tick {tick + 1}: composed vertical position diverged");
            Require(
                reborn.AppliedHorizontalDelta == expectedHorizontalDelta,
                $"air-control tick {tick + 1}: horizontal delta diverged from canonical world movement");
            Require(
                reborn.State.Horizontal.WorldX == canonicalInitialWorldX + (afterWorldX - canonicalInitialWorldX),
                $"air-control tick {tick + 1}: semantic WorldX diverged from canonical projection");
            Require(
                reborn.State.Horizontal.Facing == expectedAir.Facing,
                $"air-control tick {tick + 1}: facing must remain the canonical takeoff facing");
            Require(
                reborn.State.Horizontal.Phase == expectedAir.Phase,
                $"air-control tick {tick + 1}: logical parity diverged from canonical frame advance");
            Require(
                reborn.State.Vertical.TrajectoryTick == tick + 1,
                $"air-control tick {tick + 1}: vertical trajectory phase diverged");
            Require(
                reborn == deterministic,
                $"air-control tick {tick + 1}: equal state/input sequence must be deterministic");
            Require(
                !canonicalStep.Vertical.UsedTerminalFall,
                $"air-control tick {tick + 1}: bounded slice must stop before terminal fall");
            Require(
                !canonicalHorizontal.CollisionBlocked && !canonicalHorizontal.EdgeBlocked && !canonicalHorizontal.LandingSideCorrection,
                $"air-control tick {tick + 1}: fixture must remain collision-free");

            canonical = canonicalStep.State;
        }

        Require(reborn.State.Vertical.TableComplete, "composed standing jump must complete exactly at the end of the 30-sample table");
        Require(reborn.State.Horizontal.Facing == RebornFacing.Left, "complete air-control sequence must preserve left takeoff facing despite directional input");
        Require(sawCanonicalPlayerMove, "parity fixture must exercise screen-local player movement before camera handoff");
        Require(sawCanonicalCameraMove, "parity fixture must exercise canonical camera handoff while preserving semantic WorldX");
    }

    private static PlatformStageMap BuildOpenStage()
    {
        var empty = new byte[PlatformStagePage.DescriptorCount];
        return new PlatformStageMap(
            substate: 0,
            pages: Enumerable.Range(0, 6)
                .Select(page => new PlatformStagePage(page, empty))
                .ToArray());
    }

    private static PlatformInput ToCanonicalInput(RebornHorizontalInput input) => input switch
    {
        RebornHorizontalInput.Neutral => PlatformInput.None,
        RebornHorizontalInput.Left => PlatformInput.Left,
        RebornHorizontalInput.Right => PlatformInput.Right,
        _ => throw new ArgumentOutOfRangeException(nameof(input)),
    };

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Standing-jump air-control self-test failed: {label}");
    }
}

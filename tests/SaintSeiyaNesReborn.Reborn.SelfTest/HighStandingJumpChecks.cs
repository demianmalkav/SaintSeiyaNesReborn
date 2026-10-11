using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;
using SaintSeiyaNesReborn.Reborn.Core;
using SaintSeiyaNesReborn.Reborn.OriginalBridge;

internal static class HighStandingJumpChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        var cases = new[]
        {
            new ProfileCase(PlatformSaintIndex.Seiya, RebornHighStandingJumpProfileFamily.Seiya, 58, 103, 28, 51),
            new ProfileCase(PlatformSaintIndex.Shun, RebornHighStandingJumpProfileFamily.ShunIkki, 48, 88, 23, 51),
            new ProfileCase(PlatformSaintIndex.Ikki, RebornHighStandingJumpProfileFamily.ShunIkki, 48, 88, 23, 51),
            new ProfileCase(PlatformSaintIndex.Hyoga, RebornHighStandingJumpProfileFamily.HyogaShiryu, 38, 71, 18, 41),
            new ProfileCase(PlatformSaintIndex.Shiryu, RebornHighStandingJumpProfileFamily.HyogaShiryu, 38, 71, 18, 41),
        };

        foreach (var profileCase in cases)
            CheckCompleteProfileParity(profileCase);

        var seiya = OriginalSpecHighStandingJumpBridge.ProfileFor(PlatformSaintIndex.Seiya);
        var shun = OriginalSpecHighStandingJumpBridge.ProfileFor(PlatformSaintIndex.Shun);
        var ikki = OriginalSpecHighStandingJumpBridge.ProfileFor(PlatformSaintIndex.Ikki);
        var hyoga = OriginalSpecHighStandingJumpBridge.ProfileFor(PlatformSaintIndex.Hyoga);
        var shiryu = OriginalSpecHighStandingJumpBridge.ProfileFor(PlatformSaintIndex.Shiryu);

        Require(
            shun.RisePixelsPerTick.SequenceEqual(ikki.RisePixelsPerTick),
            "Shun and Ikki must share the frozen high-jump family.");
        Require(
            hyoga.RisePixelsPerTick.SequenceEqual(shiryu.RisePixelsPerTick),
            "Hyoga and Shiryu must share the frozen high-jump family.");
        Require(
            !seiya.RisePixelsPerTick.SequenceEqual(shun.RisePixelsPerTick),
            "Seiya high-jump family must remain distinct from Shun/Ikki.");
        Require(
            !shun.RisePixelsPerTick.SequenceEqual(hyoga.RisePixelsPerTick),
            "Shun/Ikki high-jump family must remain distinct from Hyoga/Shiryu.");

        var canonicalValid = PlatformInput.A | PlatformInput.Up;
        var validIntent = OriginalSpecHighStandingJumpBridge.FromCanonicalTakeoffInput(canonicalValid);
        Require(validIntent.IsValid, "canonical Up+A neutral takeoff must project to a valid semantic high jump.");

        var ordinaryIntent = OriginalSpecHighStandingJumpBridge.FromCanonicalTakeoffInput(PlatformInput.A);
        Require(!ordinaryIntent.IsValid, "A without upward intent must not select the semantic high jump.");

        var directionalIntent = OriginalSpecHighStandingJumpBridge.FromCanonicalTakeoffInput(
            PlatformInput.A | PlatformInput.Up | PlatformInput.Right);
        Require(!directionalIntent.IsValid, "horizontal takeoff must remain outside the high-standing-jump family.");

        var rejectedOrdinary = false;
        try
        {
            RebornHighStandingJump.Initiate(1000, ordinaryIntent, seiya);
        }
        catch (InvalidOperationException)
        {
            rejectedOrdinary = true;
        }
        Require(rejectedOrdinary, "high-jump API must reject ordinary standing-jump takeoff intent.");

        var rejectedDirectional = false;
        try
        {
            RebornHighStandingJump.Initiate(1000, directionalIntent, seiya);
        }
        catch (InvalidOperationException)
        {
            rejectedDirectional = true;
        }
        Require(rejectedDirectional, "high-jump API must reject directional takeoff intent.");
    }

    private static void CheckCompleteProfileParity(ProfileCase profileCase)
    {
        var profile = OriginalSpecHighStandingJumpBridge.ProfileFor(profileCase.Saint);
        Require(profile.Family == profileCase.Family, $"{profileCase.Saint}: wrong semantic profile family.");
        Require(profile.TickCount == profileCase.TickCount, $"{profileCase.Saint}: wrong table tick count.");
        Require(profile.PeakRisePixels == profileCase.PeakRise, $"{profileCase.Saint}: wrong peak rise.");
        Require(profile.ApexTick == profileCase.ApexTick, $"{profileCase.Saint}: wrong first apex tick.");
        Require(profile.NetRiseAfterTrajectory == profileCase.NetRise, $"{profileCase.Saint}: wrong net table rise.");
        Require(profile.RisePixelsPerTick[0] == 9, $"{profileCase.Saint}: high jump must begin with the canonical +9 rise.");

        var neutralCanonicalState = new PlatformJumpInitiationState(
            ActionState4D: 0,
            JumpPhase49: 0,
            JumpButtonLatch4A: 0,
            HighJumpSelector038A: 0,
            Support038D: 0);
        var canonicalInput = PlatformInput.A | PlatformInput.Up;
        var initiation = PlatformJumpInitiation.Step(neutralCanonicalState, canonicalInput);
        Require(initiation.StartedJump, $"{profileCase.Saint}: canonical high jump did not start.");
        Require(initiation.MustRunAirborneStepThisFrame, $"{profileCase.Saint}: canonical high jump must consume its first sample on takeoff.");

        var semanticIntent = OriginalSpecHighStandingJumpBridge.FromCanonicalTakeoffInput(canonicalInput);
        Require(semanticIntent.IsValid, $"{profileCase.Saint}: canonical high-jump input did not project to valid semantic intent.");

        const byte canonicalTakeoffY = 0x80;
        const int semanticTakeoffY = 1000;
        var canonical = new PlatformAirborneVerticalState(
            PlayerY: canonicalTakeoffY,
            PlayerYHigh41: 0,
            ActionState: initiation.State.ActionState4D,
            JumpPhase49: initiation.State.JumpPhase49,
            HighJumpSelector038A: initiation.State.HighJumpSelector038A,
            Support038D: initiation.State.Support038D,
            Special76: 0);
        var openAir = new PlatformCollisionDescriptors(null, null, null, null, null, null, null, null);

        var reborn = RebornHighStandingJump.Initiate(semanticTakeoffY, semanticIntent, profile);
        var deterministic = RebornHighStandingJump.Initiate(semanticTakeoffY, semanticIntent, profile);
        Require(reborn.State.TrajectoryTick == 1, $"{profileCase.Saint}: takeoff must consume semantic sample 1 immediately.");
        Require(reborn.AppliedRisePixels == 9, $"{profileCase.Saint}: same-tick takeoff must apply the first +9 sample.");

        for (var tick = 1; tick <= profile.TickCount; tick++)
        {
            var canonicalStep = PlatformAirborneVerticalMotion.Step(profileCase.Saint, canonical, openAir);

            if (tick > 1)
            {
                reborn = RebornHighStandingJump.Step(reborn.State, profile);
                deterministic = RebornHighStandingJump.Step(deterministic.State, profile);
            }

            var expectedRise = OriginalSpecStandingJumpBridge.RiseFromCanonicalScreenDelta(
                canonicalStep.ScreenYDeltaApplied);
            var canonicalCumulativeRise = canonicalTakeoffY - canonicalStep.State.PlayerY;

            Require(
                reborn.AppliedRisePixels == expectedRise,
                $"{profileCase.Saint} tick {tick}: semantic rise diverged from frozen oracle.");
            Require(
                reborn.State.VerticalPosition - semanticTakeoffY == canonicalCumulativeRise,
                $"{profileCase.Saint} tick {tick}: cumulative vertical position diverged.");
            Require(
                reborn.State.TrajectoryTick == tick,
                $"{profileCase.Saint} tick {tick}: semantic trajectory phase diverged.");
            Require(
                reborn == deterministic,
                $"{profileCase.Saint} tick {tick}: equal high-jump state/profile must replay deterministically.");
            Require(
                !canonicalStep.UsedTerminalFall,
                $"{profileCase.Saint} tick {tick}: bounded high-jump slice crossed into terminal fall.");

            canonical = canonicalStep.State;
        }

        Require(reborn.State.TableComplete, $"{profileCase.Saint}: high-jump table must be complete after its last sample.");
        Require(!reborn.State.IsActive, $"{profileCase.Saint}: bounded high-jump activity must stop at table completion.");
        Require(
            reborn.State.VerticalPosition == semanticTakeoffY + profile.NetRiseAfterTrajectory,
            $"{profileCase.Saint}: final semantic position must equal takeoff plus net table rise.");

        var canonicalProfile = PlatformJumpProfile.Get(PlatformJumpKind.High, profileCase.Saint);
        Require(
            canonical.JumpPhase49 == canonicalProfile.PhaseLimit - 1,
            $"{profileCase.Saint}: canonical terminal table phase mismatch.");

        var rejectedPostTableStep = false;
        try
        {
            RebornHighStandingJump.Step(reborn.State, profile);
        }
        catch (InvalidOperationException)
        {
            rejectedPostTableStep = true;
        }
        Require(rejectedPostTableStep, $"{profileCase.Saint}: high-jump slice must reject implicit terminal-fall continuation.");
    }

    private readonly record struct ProfileCase(
        PlatformSaintIndex Saint,
        RebornHighStandingJumpProfileFamily Family,
        int TickCount,
        int PeakRise,
        int ApexTick,
        int NetRise);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"REBORN high-standing-jump self-test failed: {label}");
    }
}

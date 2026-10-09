using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class JumpCoreChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckInitiation();
        CheckProfiles();
        CheckLandingAndCeiling();
    }

    private static void CheckInitiation()
    {
        var idle = new PlatformJumpState(0x70, 0, 0, 0, 0, 0, 0, 0);

        var ordinary = PlatformJumpCore.ApplyJumpButton(idle, PlatformInput.A);
        Require(ordinary.JumpPhase49 == 1 && ordinary.ActionState4D == 0x30, "A starts ordinary vertical jump at phase 1.");
        Require(ordinary.JumpButtonLatch4A == 1 && ordinary.HighJumpFlag038A == 0, "Ordinary A latch/high flag.");

        var high = PlatformJumpCore.ApplyJumpButton(idle, PlatformInput.A | PlatformInput.Up);
        Require(high.ActionState4D == 0x30 && high.HighJumpFlag038A == 0x30, "Up+A selects high vertical profile.");

        var right = PlatformJumpCore.ApplyJumpButton(idle, PlatformInput.A | PlatformInput.Up | PlatformInput.Right);
        Require(right.ActionState4D == 0x31 && right.HighJumpFlag038A == 0, "Directional input wins over Up high-jump selection.");

        var left = PlatformJumpCore.ApplyJumpButton(idle, PlatformInput.A | PlatformInput.Left);
        Require(left.ActionState4D == 0x32, "Left+A starts state $32.");

        var both = PlatformJumpCore.ApplyJumpButton(idle, PlatformInput.A | PlatformInput.Left | PlatformInput.Right);
        Require(both.ActionState4D == 0x33, "Both directions at takeoff preserve low bits $03.");

        var downBlocked = PlatformJumpCore.ApplyJumpButton(idle, PlatformInput.A | PlatformInput.Down);
        Require(downBlocked.JumpPhase49 == 0 && downBlocked.JumpButtonLatch4A == 0xFF, "Standing Down+A rejects jump and latches A as $FF.");

        var lockBlocked = PlatformJumpCore.ApplyJumpButton(idle with { JumpLock038D = 2 }, PlatformInput.A);
        Require(lockBlocked.JumpPhase49 == 0 && lockBlocked.JumpButtonLatch4A == 0xFF, "$038D blocks jump start.");

        var released = PlatformJumpCore.ApplyJumpButton(lockBlocked, PlatformInput.None);
        Require(released.JumpButtonLatch4A == 0, "Releasing A clears jump-button latch.");
    }

    private static void CheckProfiles()
    {
        var ordinary = Jumping(0x30, high: false);
        var ordinaryDesc = PlatformJumpCore.DescribeProfile(ordinary, PlatformSaintIndex.Seiya);
        Require(ordinaryDesc.Kind == PlatformJumpProfileKind.OrdinaryVertical && ordinaryDesc.Duration == 32 && ordinaryDesc.HalfPhase == 16,
            "Ordinary profile duration/half phase.");
        Require(MaxRise(ordinary, PlatformSaintIndex.Seiya) == 58, "Ordinary vertical max rise is 58 px.");

        var high = Jumping(0x30, high: true);
        Require(ProfileDuration(high, PlatformSaintIndex.Seiya) == 60 && MaxRise(high, PlatformSaintIndex.Seiya) == 103, "Seiya high jump.");
        Require(ProfileDuration(high, PlatformSaintIndex.Shun) == 50 && MaxRise(high, PlatformSaintIndex.Shun) == 88, "Shun high jump.");
        Require(ProfileDuration(high, PlatformSaintIndex.Hyoga) == 40 && MaxRise(high, PlatformSaintIndex.Hyoga) == 71, "Hyoga high jump.");
        Require(ProfileDuration(high, PlatformSaintIndex.Shiryu) == 40 && MaxRise(high, PlatformSaintIndex.Shiryu) == 71, "Shiryu high jump.");
        Require(ProfileDuration(high, PlatformSaintIndex.Ikki) == 50 && MaxRise(high, PlatformSaintIndex.Ikki) == 88, "Ikki high jump.");

        var directional = Jumping(0x31, high: false);
        Require(ProfileDuration(directional, PlatformSaintIndex.Seiya) == 54 && MaxRise(directional, PlatformSaintIndex.Seiya) == 39, "Seiya directional jump.");
        Require(ProfileDuration(directional, PlatformSaintIndex.Shun) == 40 && MaxRise(directional, PlatformSaintIndex.Shun) == 33, "Shun directional jump.");
        Require(ProfileDuration(directional, PlatformSaintIndex.Hyoga) == 44 && MaxRise(directional, PlatformSaintIndex.Hyoga) == 34, "Hyoga directional jump.");
        Require(ProfileDuration(directional, PlatformSaintIndex.Shiryu) == 44 && MaxRise(directional, PlatformSaintIndex.Shiryu) == 34, "Shiryu directional jump.");
        Require(ProfileDuration(directional, PlatformSaintIndex.Ikki) == 54 && MaxRise(directional, PlatformSaintIndex.Ikki) == 39, "Ikki directional jump.");

        var first = PlatformJumpCore.StepVertical(ordinary, PlatformSaintIndex.Seiya, default);
        Require(first.State.JumpPhase49 == 2 && first.AppliedDelta == 8 && first.State.PlayerY == ordinary.PlayerY - 8,
            "First ordinary jump frame consumes table index zero.");

        var freeFallStart = ordinary with { JumpPhase49 = 31, PlayerY = 0x60 };
        var falling = PlatformJumpCore.StepVertical(freeFallStart, PlatformSaintIndex.Seiya, default);
        Require(falling.State.JumpPhase49 == 32 && falling.AppliedDelta == -3 && falling.State.PlayerY == 0x63,
            "Phase == duration enters +3 px/frame free fall.");
    }

    private static void CheckLandingAndCeiling()
    {
        var descendingHalf = Jumping(0x30, high: false) with { JumpPhase49 = 16, PlayerY = 0x50, JumpLock038D = 7 };
        var floor = new PlatformCollisionDescriptors(0x90, null, null, null, null, null, null, null);
        var landed = PlatformJumpCore.StepVertical(descendingHalf, PlatformSaintIndex.Seiya, floor);
        Require(landed.Outcome == PlatformJumpVerticalOutcome.Landed, "Second-half floor probe lands before phase increment.");
        Require(landed.State.JumpPhase49 == 0 && landed.State.ActionState4D == 0 && landed.State.JumpLock038D == 0,
            "Landing clears phase/action/jump lock.");

        var specialFloor = descendingHalf with { PlayerY = 0x90, HazardFlag76 = 0 };
        var f8 = floor with { FloorCenter = 0xF8 };
        var specialLanding = PlatformJumpCore.StepVertical(specialFloor, PlatformSaintIndex.Seiya, f8);
        Require(specialLanding.Outcome == PlatformJumpVerticalOutcome.Landed && specialLanding.State.PlayerY == 0x88,
            "$F8/$F9 lower-screen path lands at Y=$88.");
        Require(specialLanding.State.HazardFlag76 == 1, "First $F8/$F9 landing increments $76.");

        var voidFall = descendingHalf with { PlayerY = 0xA0, PlayerYPage41 = 0 };
        var fellOut = PlatformJumpCore.StepVertical(voidFall, PlatformSaintIndex.Seiya, default);
        Require(fellOut.Outcome == PlatformJumpVerticalOutcome.FellOut && fellOut.State.ActionState4D == 0x80,
            "Y >= $A0 with page 0 enters fall-out/damage family $80.");

        var rising = Jumping(0x30, high: false) with { PlayerY = 0x70 };
        var ceiling = new PlatformCollisionDescriptors(null, null, null, null, null, null, null, 0xE0);
        var interrupted = PlatformJumpCore.StepVertical(rising, PlatformSaintIndex.Seiya, ceiling);
        Require(interrupted.Outcome == PlatformJumpVerticalOutcome.CeilingInterrupted, "$E0-$EF head probe interrupts rise.");
        Require(interrupted.State.JumpPhase49 == 0 && interrupted.State.ActionState4D == 0x50,
            "Ceiling interruption enters action family $50.");
    }

    private static PlatformJumpState Jumping(byte action, bool high) => new(
        PlayerY: 0x90,
        PlayerYPage41: 0,
        JumpPhase49: 1,
        JumpButtonLatch4A: 1,
        ActionState4D: action,
        HighJumpFlag038A: high ? (byte)0x30 : (byte)0,
        JumpLock038D: 0,
        HazardFlag76: 0);

    private static int ProfileDuration(PlatformJumpState state, PlatformSaintIndex saint) =>
        PlatformJumpCore.DescribeProfile(state, saint).Duration;

    private static int MaxRise(PlatformJumpState initial, PlatformSaintIndex saint)
    {
        var desc = PlatformJumpCore.DescribeProfile(initial, saint);
        var state = initial;
        var startY = state.PlayerY;
        var minRelative = 0;
        while (state.JumpPhase49 < desc.Duration - 1)
        {
            var step = PlatformJumpCore.StepVertical(state, saint, default);
            state = step.State;
            var relative = state.PlayerY - startY;
            minRelative = Math.Min(minRelative, relative);
        }
        return -minRelative;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException($"JumpCore self-test failed: {message}");
    }
}

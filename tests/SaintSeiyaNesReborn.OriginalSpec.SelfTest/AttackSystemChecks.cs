using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class AttackSystemChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckRangeBusyAndLatch();
        CheckCreationAndSlots();
        CheckGenericProjectile();
        CheckShunChain();
    }

    private static void CheckRangeBusyAndLatch()
    {
        Require(PlatformAttackSystem.RangeParameter(PlatformSaintIndex.Seiya, 0) == 3, "Seiya low range");
        Require(PlatformAttackSystem.RangeParameter(PlatformSaintIndex.Seiya, 999) == 48, "Seiya high range");
        Require(PlatformAttackSystem.RangeParameter(PlatformSaintIndex.Shun, 999) == 16, "Shun high range");
        Require(PlatformAttackSystem.RangeParameter(PlatformSaintIndex.Hyoga, 999) == 28, "Hyoga high range");
        Require(PlatformAttackSystem.RangeParameter(PlatformSaintIndex.Shiryu, 999) == 22, "Shiryu high range");
        Require(PlatformAttackSystem.RangeParameter(PlatformSaintIndex.Ikki, 0) == 60, "Ikki fixed range");
        Require(PlatformAttackSystem.RangeParameter(PlatformSaintIndex.Seiya, 0, 0x30) == 60, "$01>=30 fixed range");
        Require(PlatformAttackSystem.RangeParameter(PlatformSaintIndex.Hyoga, 399) == 10, "Cosmo bracket 2-3");

        byte busy = 1;
        for (var expected = 2; expected <= 7; expected++)
        {
            busy = PlatformAttackSystem.AdvanceBusy(busy);
            Require(busy == expected, $"busy -> {expected}");
        }
        Require(PlatformAttackSystem.AdvanceBusy(7) == 0, "busy 7 -> 0");
        Require(PlatformAttackSystem.AdvanceBusy(0) == 0, "busy zero stable");

        var empty = PlatformAttackState.Empty;
        var release = Attempt(empty with { BButtonLatch4C = 1 }, PlatformSaintIndex.Seiya, PlatformInput.None);
        Require(release.State.BButtonLatch4C == 0 && release.Outcome == PlatformAttackAttemptOutcome.NoInput, "B release clears latch");

        var blockedBusy = Attempt(empty with { Busy4B = 3 }, PlatformSaintIndex.Seiya, PlatformInput.B);
        Require(blockedBusy.Outcome == PlatformAttackAttemptOutcome.Busy && blockedBusy.State.BButtonLatch4C == 1, "busy still consumes B latch");
        Require(blockedBusy.SoundId is null, "busy rejects before sound");

        var highY = PlatformAttackSystem.ApplyBButton(
            empty, PlatformSaintIndex.Seiya, PlatformInput.B,
            0x90, 0x30, 0x40, 0, 0, 999);
        Require(highY.Outcome == PlatformAttackAttemptOutcome.HeightRejected && highY.SoundId == 0x24, "Y>=90 rejects after sound");
        Require(highY.State.BButtonLatch4C == 1 && highY.State.Busy4B == 0, "height reject latch/busy semantics");
    }

    private static void CheckCreationAndSlots()
    {
        var empty = PlatformAttackState.Empty;
        var seiya = PlatformAttackSystem.ApplyBButton(
            empty with { ActionState4D = 0x10 },
            PlatformSaintIndex.Seiya, PlatformInput.B,
            0x40, 0x30, 0x40, 0x10, 0, 999);
        Require(seiya.Outcome == PlatformAttackAttemptOutcome.Created && seiya.CreatedSlot == PlatformAttackSlotId.Slot0, "Seiya slot0");
        Require(seiya.State.Slot0.Object is { Y: 0x47, X: 0x42, Facing: 0x40, Type: 0x64 }, "Seiya origin/type");
        Require(seiya.State.Slot0.RangeCounter == 48 && seiya.State.Busy4B == 1, "Seiya range/busy");
        Require(seiya.State.ActionState4D == 0, "moving family resets after grounded attack");

        var crouched = PlatformAttackSystem.ApplyBButton(
            empty, PlatformSaintIndex.Seiya, PlatformInput.B,
            0x40, 0x30, 0, 0x20, 0, 0);
        Require(crouched.State.Slot0.Object.Y == 0x4F && crouched.State.Slot0.Object.X == 0x27, "crouch Y+8 and left X-9");

        var exactOnly = PlatformAttackSystem.ApplyBButton(
            empty, PlatformSaintIndex.Seiya, PlatformInput.B,
            0x40, 0x30, 0x40, 0x21, 0, 0);
        Require(exactOnly.State.Slot0.Object.Y == 0x47, "origin helper checks exact $4E==$20");

        var occupied = ActiveGeneric(0x20, 5);
        var noFree = Attempt(empty with { Slot0 = occupied }, PlatformSaintIndex.Seiya, PlatformInput.B);
        Require(noFree.Outcome == PlatformAttackAttemptOutcome.NoFreeSlot && noFree.State.BButtonLatch4C == 1, "no slot still consumes latch");
        Require(noFree.SoundId is null, "no slot rejects before sound");

        var ikki = Attempt(empty, PlatformSaintIndex.Ikki, PlatformInput.B, cosmo: 0);
        Require(ikki.CreatedSlot == PlatformAttackSlotId.Slot1 && ikki.State.Slot1.RangeCounter == 60, "Ikki prefers slot1");
        var ikkiFallback = Attempt(empty with { Slot1 = occupied }, PlatformSaintIndex.Ikki, PlatformInput.B, cosmo: 0);
        Require(ikkiFallback.CreatedSlot == PlatformAttackSlotId.Slot0, "Ikki slot1->slot0 fallback");

        var shiryu = Attempt(empty, PlatformSaintIndex.Shiryu, PlatformInput.B);
        Require(shiryu.CreatedSlot == PlatformAttackSlotId.Slot2, "Shiryu prefers slot2");
        var shiryu2 = Attempt(shiryu.State with { BButtonLatch4C = 0, Busy4B = 0 }, PlatformSaintIndex.Shiryu, PlatformInput.B);
        Require(shiryu2.CreatedSlot == PlatformAttackSlotId.Slot1, "Shiryu second uses slot1");

        var shun = Attempt(empty, PlatformSaintIndex.Shun, PlatformInput.B);
        Require(shun.CreatedSlot == PlatformAttackSlotId.Slot0 && shun.State.Slot0.Object.Type == 0x54, "Shun special slot/type");
        Require(shun.State.ShunExtension0391 == 5 && shun.SoundId == 0x34, "Shun extension/sound");
    }

    private static void CheckGenericProjectile()
    {
        var slot = ActiveGeneric(0x20, 3);
        var one = PlatformAttackSystem.UpdateGenericProjectile(slot, 1);
        Require(one.RangeCounter == 2 && one.Object.X == 0x25 && one.Object.Type == 0x65, "generic decrement + move5 + parity");
        Require(one.Object.AuxiliaryX == 0x1D, "right auxiliary X-8");
        var two = PlatformAttackSystem.UpdateGenericProjectile(one, 0);
        Require(two.RangeCounter == 1 && two.Object.X == 0x2A && two.Object.Type == 0x64, "second move5");
        var retired = PlatformAttackSystem.UpdateGenericProjectile(two, 1);
        Require(retired.RangeCounter == 0 && retired.Object is { Y: 0xF0, Type: 0xFE, Field4: 0xF0, Field5: 0xFE }, "range zero retires before movement");

        var left = slot with { Object = slot.Object with { Facing = 0, X = 0x20 } };
        var leftStep = PlatformAttackSystem.UpdateGenericProjectile(left, 0);
        Require(leftStep.Object.X == 0x1B && leftStep.Object.AuxiliaryX == 0x23, "left move -5, auxiliary +8");

        var edge = slot with { Object = slot.Object with { X = 0xF4 } };
        Require(PlatformAttackSystem.UpdateGenericProjectile(edge, 0).Object.Type == 0xFE, "F8-family X retires");
    }

    private static void CheckShunChain()
    {
        var created = Attempt(PlatformAttackState.Empty, PlatformSaintIndex.Shun, PlatformInput.B).State;
        var extend = PlatformAttackSystem.UpdateShunChain(created, 0x40, 0x30, 0);
        Require(extend.Slot0.RangeCounter == 15 && extend.ShunExtension0391 == 10, "Shun range-- / extension+5");
        Require(extend.Slot0.Object.X == 0x52 && extend.Slot1.Object.X == 0x4A, "Shun right far/near positions");
        Require(extend.Slot1.Object.Type == 0x55 && extend.Slot1.Object.Facing == 0x40, "Shun second segment");
        Require(extend.Slot0.Object.Y == 0x47 && extend.Slot1.Object.Y == 0x47, "chain follows current origin Y");

        var retract = extend with { Slot0 = extend.Slot0 with { RangeCounter = 0 }, ShunExtension0391 = 5 };
        var zero = PlatformAttackSystem.UpdateShunChain(retract, 0x40, 0x30, 0);
        Require(zero.ShunExtension0391 == 0 && zero.Slot0.Object.Type == 0x54, "retraction 5->0 stays active");
        var gone = PlatformAttackSystem.UpdateShunChain(zero, 0x40, 0x30, 0);
        Require(gone.Slot0.Object is { Y: 0xF0, Type: 0xFE } && gone.Slot1.Object is { Y: 0xF0, Type: 0xFE }, "negative retraction retires both");

        // After the first outward update extension becomes 10. Player X $D6 gives
        // near=$F0 and far=$F8 exactly, so the original edge test forces range=0.
        var edgeForced = PlatformAttackSystem.UpdateShunChain(created, 0x40, 0xD6, 0);
        Require(edgeForced.Slot0.Object.X == 0xF8 && edgeForced.Slot0.RangeCounter == 0, "Shun far edge forces retraction");
    }

    private static PlatformAttackAttemptResult Attempt(
        PlatformAttackState state,
        PlatformSaintIndex saint,
        PlatformInput input,
        int cosmo = 999) => PlatformAttackSystem.ApplyBButton(
            state, saint, input,
            playerY: 0x40,
            playerX: 0x30,
            facing42: 0x40,
            frameStartAction4E: 0,
            jumpPhase49: 0,
            cosmo: cosmo);

    private static PlatformAttackSlot ActiveGeneric(byte x, byte range) => new(
        new PlatformAttackObject(0x40, 0x64, 0x40, x, 0, 0, 0, 0),
        range);

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException($"AttackSystem self-test failed: {message}");
    }
}

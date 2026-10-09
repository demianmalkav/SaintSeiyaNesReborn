using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class AttackSystemChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckRangeAndBusy();
        CheckInputAndCreation();
        CheckSlotAllocation();
        CheckGenericUpdate();
        CheckShunChain();
    }

    private static void CheckRangeAndBusy()
    {
        Require(PlatformAttackSystem.RangeParameter(PlatformSaintIndex.Seiya, 0) == 3, "Seiya low-Cosmo range.");
        Require(PlatformAttackSystem.RangeParameter(PlatformSaintIndex.Seiya, 999) == 48, "Seiya high-Cosmo range.");
        Require(PlatformAttackSystem.RangeParameter(PlatformSaintIndex.Shun, 999) == 16, "Shun high-Cosmo range.");
        Require(PlatformAttackSystem.RangeParameter(PlatformSaintIndex.Hyoga, 999) == 28, "Hyoga high-Cosmo range.");
        Require(PlatformAttackSystem.RangeParameter(PlatformSaintIndex.Shiryu, 999) == 22, "Shiryu high-Cosmo range.");
        Require(PlatformAttackSystem.RangeParameter(PlatformSaintIndex.Ikki, 0) == 60, "Ikki fixed range 60.");
        Require(PlatformAttackSystem.RangeParameter(PlatformSaintIndex.Seiya, 0, engineSubstate01: 0x30) == 60,
            "$01 >= $30 forces fixed range 60.");
        Require(PlatformAttackSystem.RangeParameter(PlatformSaintIndex.Hyoga, 399) == 10,
            "Cosmo hundreds are grouped into 0-1/2-3/4-5/6-7/8-9 brackets.");

        byte busy = 1;
        for (var expected = 2; expected <= 7; expected++)
        {
            busy = PlatformAttackSystem.AdvanceBusy(busy);
            Require(busy == expected, $"Busy counter advances to {expected}.");
        }
        Require(PlatformAttackSystem.AdvanceBusy(7) == 0, "Busy counter wraps 7 -> 0.");
        Require(PlatformAttackSystem.AdvanceBusy(0) == 0, "Busy zero stays zero.");
    }

    private static void CheckInputAndCreation()
    {
        var empty = PlatformAttackState.Empty;

        var released = PlatformAttackSystem.ApplyBButton(
            empty with { BButtonLatch4C = 1 },
            PlatformSaintIndex.Seiya,
            PlatformInput.None,
            0x40, 0x30, 0x40, 0, 0, 999);
        Require(released.State.BButtonLatch4C == 0 && released.Outcome == PlatformAttackAttemptOutcome.NoInput,
            "B release clears $4C.");

        var held = PlatformAttackSystem.ApplyBButton(
            empty with { BButtonLatch4C = 1 },
            PlatformSaintIndex.Seiya,
            PlatformInput.B,
            0x40, 0x30, 0x40, 0, 0, 999);
        Require(held.Outcome == PlatformAttackAttemptOutcome.Latched,
            "Held B cannot create again until release.");

        var busy = PlatformAttackSystem.ApplyBButton(
            empty with { Busy4B = 3 },
            PlatformSaintIndex.Seiya,
            PlatformInput.B,
            0x40, 0x30, 0x40, 0, 0, 999);
        Require(busy.Outcome == PlatformAttackAttemptOutcome.Busy && busy.State.BButtonLatch4C == 1,
            "Busy rejection still consumes B latch.");
        Require(busy.SoundId is null, "Busy rejection occurs before attack sound.");

        var height = PlatformAttackSystem.ApplyBButton(
            empty,
            PlatformSaintIndex.Seiya,
            PlatformInput.B,
            0x90, 0x30, 0x40, 0, 0, 999);
        Require(height.Outcome == PlatformAttackAttemptOutcome.HeightRejected && height.SoundId == 0x24,
            "Y >= $90 rejects only after ordinary attack sound.");
        Require(height.State.BButtonLatch4C == 1 && height.State.Busy4B == 0,
            "Height rejection consumes latch but does not start busy cycle.");
        Require(height.State.Slot0.Object.Type == 0xFE, "Height rejection does not mutate slot.");

        var created = PlatformAttackSystem.ApplyBButton(
            empty with { ActionState4D = 0x10 },
            PlatformSaintIndex.Seiya,
            PlatformInput.B,
            playerY: 0x40,
            playerX: 0x30,
            facing42: 0x40,
            frameStartAction4E: 0x10,
            jumpPhase49: 0,
            cosmo: 999);
        Require(created.Outcome == PlatformAttackAttemptOutcome.Created && created.CreatedSlot == PlatformAttackSlotId.Slot0,
            "Seiya creates in slot0.");
        Require(created.State.Busy4B == 1 && created.State.BButtonLatch4C == 1,
            "Successful attack starts busy and keeps latch.");
        Require(created.State.ActionState4D == 0,
            "Grounded moving-family attack resets current action after creation.");
        var object0 = created.State.Slot0.Object;
        Require(object0.Y == 0x47 && object0.X == 0x42 && object0.Facing == 0x40 && object0.Type == 0x64,
            "Standing right-facing origin/type.");
        Require(created.State.Slot0.RangeCounter == 48, "Seiya 999 range counter.");
        Require(created.SoundId == 0x24, "Ordinary Saint attack sound.");

        var crouched = PlatformAttackSystem.ApplyBButton(
            empty,
            PlatformSaintIndex.Seiya,
            PlatformInput.B,
            playerY: 0x40,
            playerX: 0x30,
            facing42: 0,
            frameStartAction4E: 0x20,
            jumpPhase49: 0,
            cosmo: 0);
        Require(crouched.State.Slot0.Object.Y == 0x4F,
            "Exact frame-start crouch state $20 adds 8 to attack origin Y.");
        Require(crouched.State.Slot0.Object.X == 0x27,
            "Left-facing origin is player X - 9.");

        var notExactCrouch = PlatformAttackSystem.ApplyBButton(
            empty,
            PlatformSaintIndex.Seiya,
            PlatformInput.B,
            0x40, 0x30, 0x40,
            frameStartAction4E: 0x21,
            jumpPhase49: 0,
            cosmo: 0);
        Require(notExactCrouch.State.Slot0.Object.Y == 0x47,
            "Attack Y helper compares exact $4E==$20, not the whole $2x family.");
    }

    private static void CheckSlotAllocation()
    {
        var occupied = ActiveGeneric(0x20, 5);
        var baseState = PlatformAttackState.Empty with { Slot0 = occupied };

        var noFree = PlatformAttackSystem.ApplyBButton(
            baseState,
            PlatformSaintIndex.Seiya,
            PlatformInput.B,
            0x40, 0x30, 0x40, 0, 0, 999);
        Require(noFree.Outcome == PlatformAttackAttemptOutcome.NoFreeSlot && noFree.State.BButtonLatch4C == 1,
            "Single-slot Saint consumes B even when slot0 is occupied.");
        Require(noFree.SoundId is null, "No-free-slot rejection occurs before sound.");

        var ikki = PlatformAttackSystem.ApplyBButton(
            PlatformAttackState.Empty,
            PlatformSaintIndex.Ikki,
            PlatformInput.B,
            0x40, 0x30, 0x40, 0, 0, 0);
        Require(ikki.CreatedSlot == PlatformAttackSlotId.Slot1,
            "Ikki prefers slot1 before slot0.");
        Require(ikki.State.Slot1.RangeCounter == 60, "Ikki slot1 receives fixed range.");

        var ikkiFallback = PlatformAttackSystem.ApplyBButton(
            PlatformAttackState.Empty with { Slot1 = occupied },
            PlatformSaintIndex.Ikki,
            PlatformInput.B,
            0x40, 0x30, 0x40, 0, 0, 0);
        Require(ikkiFallback.CreatedSlot == PlatformAttackSlotId.Slot0,
            "Ikki falls back slot1 -> slot0.");

        var shiryu = PlatformAttackSystem.ApplyBButton(
            PlatformAttackState.Empty,
            PlatformSaintIndex.Shiryu,
            PlatformInput.B,
            0x40, 0x30, 0x40, 0, 0, 999);
        Require(shiryu.CreatedSlot == PlatformAttackSlotId.Slot2,
            "Shiryu prefers slot2, enabling three concurrent ordinary objects.");

        var shiryuSecond = PlatformAttackSystem.ApplyBButton(
            (shiryu.State with { BButtonLatch4C = 0, Busy4B = 0 }),
            PlatformSaintIndex.Shiryu,
            PlatformInput.B,
            0x40, 0x30, 0x40, 0, 0, 999);
        Require(shiryuSecond.CreatedSlot == PlatformAttackSlotId.Slot1,
            "Shiryu second object falls back to slot1.");

        var shun = PlatformAttackSystem.ApplyBButton(
            PlatformAttackState.Empty,
            PlatformSaintIndex.Shun,
            PlatformInput.B,
            0x40, 0x30, 0x40, 0, 0, 999);
        Require(shun.CreatedSlot == PlatformAttackSlotId.Slot0 && shun.State.Slot0.Object.Type == 0x54,
            "Shun creates special type $54 in slot0.");
        Require(shun.State.ShunExtension0391 == 5 && shun.SoundId == 0x34,
            "Shun initializes extension 5 and uses sound $34.");
    }

    private static void CheckGenericUpdate()
    {
        var slot = ActiveGeneric(x: 0x20, range: 3);

        var first = PlatformAttackSystem.UpdateGenericProjectile(slot, frameCounter3C: 1);
        Require(first.RangeCounter == 2 && first.Object.X == 0x25,
            "Generic projectile decrements lifetime then moves +5.");
        Require(first.Object.Type == 0x65 && first.Object.AuxiliaryX == 0x1D,
            "Generic visual toggles 64/65 by parity and stores right auxiliary X-8.");

        var second = PlatformAttackSystem.UpdateGenericProjectile(first, frameCounter3C: 0);
        Require(second.RangeCounter == 1 && second.Object.X == 0x2A && second.Object.Type == 0x64,
            "Range 3 yields a second 5px movement.");

        var third = PlatformAttackSystem.UpdateGenericProjectile(second, frameCounter3C: 1);
        Require(third.RangeCounter == 0 && third.Object.Type == 0xFE && third.Object.Y == 0xF0,
            "When decrement reaches zero, projectile retires before another move.");
        Require(third.Object.Field4 == 0xF0 && third.Object.Field5 == 0xFE,
            "Generic retirement writes original marker fields.");

        var left = slot with { Object = slot.Object with { Facing = 0, X = 0x20 } };
        var leftStep = PlatformAttackSystem.UpdateGenericProjectile(left, 0);
        Require(leftStep.Object.X == 0x1B && leftStep.Object.AuxiliaryX == 0x23,
            "Left projectile moves -5 and stores moved X+8.");

        var edge = slot with { Object = slot.Object with { X = 0xF4 } };
        var retiredEdge = PlatformAttackSystem.UpdateGenericProjectile(edge, 0);
        Require(retiredEdge.Object.Type == 0xFE,
            "Post-move X in $F8-$FF retires projectile.");
    }

    private static void CheckShunChain()
    {
        var created = PlatformAttackSystem.ApplyBButton(
            PlatformAttackState.Empty,
            PlatformSaintIndex.Shun,
            PlatformInput.B,
            0x40, 0x30, 0x40, 0, 0, 999).State;

        var extend = PlatformAttackSystem.UpdateShunChain(created, 0x40, 0x30, 0);
        Require(extend.Slot0.RangeCounter == 15 && extend.ShunExtension0391 == 10,
            "Shun outward update decrements range and extends by 5.");
        Require(extend.Slot0.Object.X == 0x52 && extend.Slot1.Object.X == 0x4A,
            "Right-facing Shun chain rebuilds far/near segments around player.");
        Require(extend.Slot1.Object.Type == 0x55 && extend.Slot1.Object.Facing == 0x40,
            "Second Shun segment is type $55 with copied facing.");
        Require(extend.Slot0.Object.Y == 0x47 && extend.Slot1.Object.Y == 0x47,
            "Both chain segments track current attack-origin Y.");

        var retractZero = extend with
        {
            Slot0 = extend.Slot0 with { RangeCounter = 0 },
            ShunExtension0391 = 5,
        };
        var atPlayer = PlatformAttackSystem.UpdateShunChain(retractZero, 0x40, 0x30, 0);
        Require(atPlayer.ShunExtension0391 == 0 && atPlayer.Slot0.Object.Type == 0x54,
            "First retraction step 5 -> 0 remains active.");

        var retired = PlatformAttackSystem.UpdateShunChain(atPlayer, 0x40, 0x30, 0);
        Require(retired.Slot0.Object.Type == 0xFE && retired.Slot1.Object.Type == 0xFE,
            "Next retraction step underflows negative and retires both chain segments.");
        Require(retired.Slot0.Object.Y == 0xF0 && retired.Slot1.Object.Y == 0xF0,
            "Shun retirement writes Y=$F0 markers.");

        var nearEdge = created with
        {
            Slot0 = created.Slot0 with { Object = created.Slot0.Object with { X = 0xEE } },
            ShunExtension0391 = 5,
        };
        // Shun position is rebuilt from player, so choose player X high enough for far segment to enter $F8 family.
        var edgeForced = PlatformAttackSystem.UpdateShunChain(nearEdge, 0x40, 0xE0, 0);
        Require(edgeForced.Slot0.RangeCounter == 0,
            "Shun far segment reaching $F8-$FF forces outward counter to zero for retraction.");
    }

    private static PlatformAttackSlot ActiveGeneric(byte x, byte range) => new(
        new PlatformAttackObject(
            Y: 0x40,
            Type: 0x64,
            Facing: 0x40,
            X: x,
            Field4: 0,
            Field5: 0,
            Field6: 0,
            AuxiliaryX: 0),
        range);

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException($"AttackSystem self-test failed: {message}");
    }
}

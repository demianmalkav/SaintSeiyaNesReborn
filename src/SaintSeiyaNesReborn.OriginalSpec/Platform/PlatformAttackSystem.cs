using SaintSeiyaNesReborn.OriginalSpec;

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformAttackSlotId : byte
{
    Slot0 = 0,
    Slot1 = 1,
    Slot2 = 2,
}

public enum PlatformAttackAttemptOutcome
{
    NoInput,
    Latched,
    Busy,
    NoFreeSlot,
    HeightRejected,
    Created,
}

public readonly record struct PlatformAttackObject(
    byte Y,
    byte Type,
    byte Facing,
    byte X,
    byte Field4,
    byte Field5,
    byte Field6,
    byte AuxiliaryX)
{
    public bool IsInactive => Type == 0xFE;
    public bool IsGenericProjectile => (Type & 0xFE) == 0x64;

    public static PlatformAttackObject Inactive => new(
        Y: 0xF0,
        Type: 0xFE,
        Facing: 0,
        X: 0,
        Field4: 0xF0,
        Field5: 0xFE,
        Field6: 0,
        AuxiliaryX: 0);
}

public readonly record struct PlatformAttackSlot(
    PlatformAttackObject Object,
    byte RangeCounter);

public readonly record struct PlatformAttackState(
    byte BButtonLatch4C,
    byte Busy4B,
    byte ActionState4D,
    byte ShunExtension0391,
    PlatformAttackSlot Slot0,
    PlatformAttackSlot Slot1,
    PlatformAttackSlot Slot2)
{
    public PlatformAttackSlot Slot(PlatformAttackSlotId id) => id switch
    {
        PlatformAttackSlotId.Slot0 => Slot0,
        PlatformAttackSlotId.Slot1 => Slot1,
        PlatformAttackSlotId.Slot2 => Slot2,
        _ => throw new ArgumentOutOfRangeException(nameof(id)),
    };

    public PlatformAttackState WithSlot(PlatformAttackSlotId id, PlatformAttackSlot value) => id switch
    {
        PlatformAttackSlotId.Slot0 => this with { Slot0 = value },
        PlatformAttackSlotId.Slot1 => this with { Slot1 = value },
        PlatformAttackSlotId.Slot2 => this with { Slot2 = value },
        _ => throw new ArgumentOutOfRangeException(nameof(id)),
    };

    public static PlatformAttackState Empty => new(
        BButtonLatch4C: 0,
        Busy4B: 0,
        ActionState4D: 0,
        ShunExtension0391: 0,
        Slot0: new PlatformAttackSlot(PlatformAttackObject.Inactive, 0),
        Slot1: new PlatformAttackSlot(PlatformAttackObject.Inactive, 0),
        Slot2: new PlatformAttackSlot(PlatformAttackObject.Inactive, 0));
}

public readonly record struct PlatformAttackAttemptResult(
    PlatformAttackState State,
    PlatformAttackAttemptOutcome Outcome,
    PlatformAttackSlotId? CreatedSlot,
    byte? SoundId);

/// <summary>
/// Platform B-attack creation and attack-object updates reconstructed from bank 3
/// $BBCA-$BCCD and $A22C-$A391 plus busy-counter update bank 1 $926C.
/// </summary>
public static class PlatformAttackSystem
{
    private static readonly byte[,] RangeByCosmoBracket =
    {
        { 3, 1, 4, 6 },
        { 6, 3, 10, 10 },
        { 12, 8, 16, 14 },
        { 24, 12, 22, 18 },
        { 48, 16, 28, 22 },
    };

    public static byte RangeParameter(
        PlatformSaintIndex saint,
        int cosmo,
        byte engineSubstate01 = 0)
    {
        if ((uint)cosmo > 999)
            throw new ArgumentOutOfRangeException(nameof(cosmo), cosmo, "Cosmo must be in 0..999.");

        if (saint == PlatformSaintIndex.Ikki || engineSubstate01 >= 0x30)
            return 60;

        var saintIndex = (int)saint;
        if (saintIndex > 3)
            throw new ArgumentOutOfRangeException(nameof(saint));

        var hundreds = cosmo / 100;
        var bracket = hundreds >> 1;
        return RangeByCosmoBracket[bracket, saintIndex];
    }

    /// <summary>
    /// Bank-1 $926C: a nonzero attack-busy value advances 1..7 and wraps to 0.
    /// </summary>
    public static byte AdvanceBusy(byte busy4B)
    {
        if (busy4B == 0)
            return 0;
        var next = busy4B + 1;
        return next >= 8 ? (byte)0 : (byte)next;
    }

    /// <summary>
    /// One pass through bank-3 $BBCA. SoundId is returned because the original
    /// emits sound after selecting a slot but before rejecting player Y >= $90.
    /// </summary>
    public static PlatformAttackAttemptResult ApplyBButton(
        PlatformAttackState state,
        PlatformSaintIndex saint,
        PlatformInput input,
        byte playerY,
        byte playerX,
        byte facing42,
        byte frameStartAction4E,
        byte jumpPhase49,
        int cosmo,
        byte engineSubstate01 = 0)
    {
        if ((input & PlatformInput.B) == 0)
        {
            return new PlatformAttackAttemptResult(
                state with { BButtonLatch4C = 0 },
                PlatformAttackAttemptOutcome.NoInput,
                null,
                null);
        }

        if (state.BButtonLatch4C != 0)
            return new PlatformAttackAttemptResult(state, PlatformAttackAttemptOutcome.Latched, null, null);

        // $4C is consumed before checking $4B, slots, or player height.
        state = state with { BButtonLatch4C = 1 };

        if (state.Busy4B != 0)
            return new PlatformAttackAttemptResult(state, PlatformAttackAttemptOutcome.Busy, null, null);

        var chosen = SelectFreeSlot(state, saint);
        if (chosen is null)
            return new PlatformAttackAttemptResult(state, PlatformAttackAttemptOutcome.NoFreeSlot, null, null);

        var sound = saint == PlatformSaintIndex.Shun ? (byte)0x34 : (byte)0x24;

        // Sound has already been requested at this point in the original.
        if (playerY >= 0x90)
            return new PlatformAttackAttemptResult(state, PlatformAttackAttemptOutcome.HeightRejected, null, sound);

        var originY = unchecked((byte)(playerY + 7 + (frameStartAction4E == 0x20 ? 8 : 0)));
        var originX = facing42 == 0x40
            ? unchecked((byte)(playerX + 0x12))
            : unchecked((byte)(playerX - 9));
        var type = saint == PlatformSaintIndex.Shun ? (byte)0x54 : (byte)0x64;
        var range = RangeParameter(saint, cosmo, engineSubstate01);

        var slot = state.Slot(chosen.Value);
        slot = slot with
        {
            Object = slot.Object with
            {
                Y = originY,
                Type = type,
                Facing = facing42,
                X = originX,
            },
            RangeCounter = range,
        };

        var action = state.ActionState4D;
        if ((frameStartAction4E & 0xF0) == 0x10 && jumpPhase49 == 0)
            action = 0;

        state = state.WithSlot(chosen.Value, slot) with
        {
            Busy4B = 1,
            ActionState4D = action,
            ShunExtension0391 = saint == PlatformSaintIndex.Shun ? (byte)5 : state.ShunExtension0391,
        };

        return new PlatformAttackAttemptResult(
            state,
            PlatformAttackAttemptOutcome.Created,
            chosen,
            sound);
    }

    /// <summary>
    /// Generic $64/$65 projectile update. The original uses five pixels per
    /// update for this family (A266 LDX #$05), not three.
    /// </summary>
    public static PlatformAttackSlot UpdateGenericProjectile(
        PlatformAttackSlot slot,
        byte frameCounter3C)
    {
        if (!slot.Object.IsGenericProjectile)
            return slot;

        var range = unchecked((byte)(slot.RangeCounter - 1));
        if (range == 0)
            return RetireGeneric(slot with { RangeCounter = range });

        var obj = slot.Object with { Type = (byte)(0x64 + (frameCounter3C & 1)) };
        byte movedX;
        byte auxiliary;

        if ((obj.Facing & 0x40) == 0)
        {
            movedX = unchecked((byte)(obj.X - 5));
            auxiliary = unchecked((byte)(movedX + 8));
        }
        else
        {
            movedX = unchecked((byte)(obj.X + 5));
            auxiliary = unchecked((byte)(movedX - 8));
        }

        obj = obj with { X = movedX, AuxiliaryX = auxiliary };
        slot = slot with { Object = obj, RangeCounter = range };

        return (movedX & 0xF8) == 0xF8 ? RetireGeneric(slot) : slot;
    }

    /// <summary>
    /// Shun index-1 two-part chain update at $A311+. Slot0 is type $54 and
    /// Slot1 is synthesized as type $55 around the current player position.
    /// </summary>
    public static PlatformAttackState UpdateShunChain(
        PlatformAttackState state,
        byte playerY,
        byte playerX,
        byte frameStartAction4E)
    {
        if (state.Slot0.Object.Type != 0x54)
            return state;

        var slot0 = state.Slot0;
        var slot1 = state.Slot1;
        var extension = state.ShunExtension0391;

        if (slot0.RangeCounter != 0)
        {
            slot0 = slot0 with { RangeCounter = unchecked((byte)(slot0.RangeCounter - 1)) };
            extension = unchecked((byte)(extension + 5));
        }
        else
        {
            extension = unchecked((byte)(extension - 5));
            if ((extension & 0x80) != 0)
            {
                slot0 = slot0 with { Object = slot0.Object with { Y = 0xF0, Type = 0xFE } };
                slot1 = slot1 with { Object = slot1.Object with { Y = 0xF0, Type = 0xFE } };
                return state with
                {
                    Slot0 = slot0,
                    Slot1 = slot1,
                    ShunExtension0391 = extension,
                };
            }
        }

        var originY = unchecked((byte)(playerY + 7 + (frameStartAction4E == 0x20 ? 8 : 0)));
        var facing = slot0.Object.Facing;
        byte nearX;
        byte farX;

        if ((facing & 0x40) != 0)
        {
            nearX = unchecked((byte)(playerX + 0x10 + extension));
            farX = unchecked((byte)(nearX + 8));
        }
        else
        {
            nearX = unchecked((byte)(playerX - 8 - extension));
            farX = unchecked((byte)(nearX - 8));
        }

        slot0 = slot0 with
        {
            Object = slot0.Object with { Y = originY, X = farX },
        };
        slot1 = slot1 with
        {
            Object = slot1.Object with
            {
                Y = originY,
                Type = 0x55,
                Facing = facing,
                X = nearX,
            },
        };

        // Reaching the horizontal edge forces the outward counter to zero so
        // the next update begins retraction.
        if ((farX & 0xF8) == 0xF8)
            slot0 = slot0 with { RangeCounter = 0 };

        return state with
        {
            Slot0 = slot0,
            Slot1 = slot1,
            ShunExtension0391 = extension,
        };
    }

    public static PlatformAttackState UpdateAttackObjects(
        PlatformAttackState state,
        PlatformSaintIndex saint,
        byte frameCounter3C,
        byte playerY,
        byte playerX,
        byte frameStartAction4E)
    {
        if (saint == PlatformSaintIndex.Shun)
            return UpdateShunChain(state, playerY, playerX, frameStartAction4E);

        return state with
        {
            Slot0 = UpdateGenericProjectile(state.Slot0, frameCounter3C),
            Slot1 = UpdateGenericProjectile(state.Slot1, frameCounter3C),
            Slot2 = UpdateGenericProjectile(state.Slot2, frameCounter3C),
        };
    }

    private static PlatformAttackSlotId? SelectFreeSlot(
        PlatformAttackState state,
        PlatformSaintIndex saint)
    {
        return saint switch
        {
            PlatformSaintIndex.Shiryu => FirstFree(state, PlatformAttackSlotId.Slot2, PlatformAttackSlotId.Slot1, PlatformAttackSlotId.Slot0),
            PlatformSaintIndex.Ikki => FirstFree(state, PlatformAttackSlotId.Slot1, PlatformAttackSlotId.Slot0),
            _ => FirstFree(state, PlatformAttackSlotId.Slot0),
        };
    }

    private static PlatformAttackSlotId? FirstFree(
        PlatformAttackState state,
        params PlatformAttackSlotId[] order)
    {
        foreach (var id in order)
        {
            if (state.Slot(id).Object.Type == 0xFE)
                return id;
        }
        return null;
    }

    private static PlatformAttackSlot RetireGeneric(PlatformAttackSlot slot) => slot with
    {
        Object = slot.Object with
        {
            Y = 0xF0,
            Type = 0xFE,
            Field4 = 0xF0,
            Field5 = 0xFE,
        },
    };
}

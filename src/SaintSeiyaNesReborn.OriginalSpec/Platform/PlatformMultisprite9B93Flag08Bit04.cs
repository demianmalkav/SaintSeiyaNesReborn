using SaintSeiyaNesReborn.OriginalSpec;

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformMultisprite9B93Flag08Bit04Outcome
{
    Active,
    RemovedVerticalBoundary,
    AllPartsRetired,
}

public readonly record struct PlatformMultisprite9B93Flag08Bit04Result(
    PlatformMultisprite9B93RuntimeState State,
    PlatformMultisprite9B93Flag08Bit04Outcome Outcome,
    PlatformProjectileHitSequenceResult? ProjectileHits,
    PlatformEntityContactResult Contact,
    PlatformAttackState AttackState,
    PlatformContactPhaseState ContactState,
    int SeventhSense,
    byte CurveIndex,
    byte CurveByte,
    byte? SoundId);

/// <summary>
/// Exact flag-$08-set + flag-$04-set branch at `$9ED1-$9F74`.
/// Logical $D0/$E0 diverts to A06E before this routine and is not accepted here.
/// </summary>
public static class PlatformMultisprite9B93Flag08Bit04
{
    private static readonly byte[] Curve9EA1 =
    [
        0x07,0x06,0x05,0x04,0x03,0x03,0x02,0x02,
        0x02,0x01,0x01,0x01,0x01,0x00,0x00,0x00,
        0x00,0x00,0x00,0xFF,0xFF,0xFF,0xFF,0xFE,
        0xFE,0xFE,0xFD,0xFD,0xFC,0xFB,0xFA,0xF9,
        0x05,0x04,0x03,0x02,0x02,0x01,0x01,0x00,
        0x00,0xFF,0xFF,0xFE,0xFE,0xFD,0xFC,0xFB,
    ];

    private static readonly byte[] Animation0C =
    [
        0xF6,0x00, 0xF7,0x00, 0xFA,0x00, 0xFB,0x00,
        0xF8,0x00, 0xF9,0x00, 0xFC,0x00, 0xFD,0x00,
        0xFB,0xC2, 0xFA,0xC2, 0xF7,0xC2, 0xF6,0xC2,
        0xFD,0xC2, 0xFC,0xC2, 0xF9,0xC2, 0xF8,0xC2,
    ];

    private static readonly byte[] AnimationDefault =
    [
        0xB4,0x00, 0xB5,0x00, 0xB6,0x00, 0xB7,0x00,
        0xB6,0x82, 0xB7,0x82, 0xB4,0x82, 0xB5,0x82,
        0xB7,0xC2, 0xB6,0xC2, 0xB5,0xC2, 0xB4,0xC2,
        0xB5,0x42, 0xB4,0x42, 0xB7,0x42, 0xB6,0x42,
    ];

    private static readonly byte[] Animation10 =
    [
        0xDA,0x00, 0,0, 0,0, 0,0,
        0xDA,0x80, 0,0, 0,0, 0,0,
        0xDA,0xC0, 0,0, 0,0, 0,0,
        0xDA,0x40, 0,0, 0,0, 0,0,
    ];

    public static PlatformMultisprite9B93Flag08Bit04Result Step(
        PlatformMultisprite9B93RuntimeState state,
        PlatformAttackState attacks,
        PlatformContactPhaseState contactState,
        PlatformSaintIndex saint,
        int platformDamage,
        int seventhSense,
        byte frameCounter3C,
        byte cameraDelta43,
        byte playerX3F,
        byte playerY40,
        byte frameStartPlayerAction4E,
        byte engineSubstate02)
    {
        ValidateEntry(state, engineSubstate02);

        var visual = state.Visual;
        var logical = state.Logical;
        var box = engineSubstate02 == 0x10
            ? new PlatformContactHitboxParameters(4, 4, 2, 2)
            : new PlatformContactHitboxParameters(8, 8, 5, 5);

        PlatformProjectileHitSequenceResult? hits = null;
        if (engineSubstate02 == 0x0C)
        {
            logical = SyncPosition(logical, visual.Part0);
            hits = PlatformProjectileHitSequence.ResolveThreeSlots(
                attacks,
                ToCombatEntity(logical),
                saint,
                ToProjectileBox(box),
                platformDamage,
                seventhSense,
                engineSubstate02);
            attacks = hits.AttackState;
            seventhSense = hits.SeventhSense;
            logical = FromCombatEntity(logical, hits.Entity);
        }

        // $9EFD re-syncs visual position after the optional $0C hit and before contact.
        logical = SyncPosition(logical, visual.Part0);
        var contact = PlatformEntityContact.Evaluate(
            frameStartPlayerAction4E,
            playerX3F,
            playerY40,
            logical.X01,
            logical.Y02,
            contactState.HazardLatch76,
            logical.LifeDrain0E,
            logical.CosmoDrain0D,
            box);
        if (contact.Triggered)
            contactState = new PlatformContactPhaseState(contact.HazardLatch76, contact.DrainState);

        var index = logical.Phase03;
        if (index >= 0x30)
            index = (byte)((index & 0x0F) | 0x20);
        if (index >= Curve9EA1.Length)
            throw new InvalidOperationException($"9EA1 curve index ${index:X2} is outside promoted range 00-2F.");

        byte? sound = null;
        if (index is 0x1F or 0x2F && logical.Field05 >= 0x80)
            sound = 0x2B;

        logical = logical with { Phase03 = unchecked((byte)(logical.Phase03 + 1)) };
        var curve = Curve9EA1[index];
        visual = Animate(visual, engineSubstate02, frameCounter3C);

        var count = engineSubstate02 == 0x10 ? 1 : 4;
        for (var i = 0; i < count; i++)
        {
            var part = GetPart(visual, i);
            var y = unchecked((byte)(part.Y - curve));
            if (y >= 0xB0)
            {
                visual = ClearAll(visual);
                logical = logical with { Phase03 = 0 };
                state = state with { Visual = visual, Logical = logical };
                return new(
                    state,
                    PlatformMultisprite9B93Flag08Bit04Outcome.RemovedVerticalBoundary,
                    hits,
                    contact,
                    attacks,
                    contactState,
                    seventhSense,
                    index,
                    curve,
                    sound);
            }

            var x = unchecked((byte)(part.X - 2 - cameraDelta43));
            part = part with { Y = y, X = x };
            if (x < 0x04)
                part = part with { Y = 0xF0, Sprite = 0xFE };
            visual = SetPart(visual, i, part);
        }

        if (visual.AllEmpty)
        {
            logical = logical with { Phase03 = 0 };
            state = state with { Visual = visual, Logical = logical };
            return new(
                state,
                PlatformMultisprite9B93Flag08Bit04Outcome.AllPartsRetired,
                hits,
                contact,
                attacks,
                contactState,
                seventhSense,
                index,
                curve,
                sound);
        }

        state = state with { Visual = visual, Logical = logical };
        return new(
            state,
            PlatformMultisprite9B93Flag08Bit04Outcome.Active,
            hits,
            contact,
            attacks,
            contactState,
            seventhSense,
            index,
            curve,
            sound);
    }

    private static void ValidateEntry(PlatformMultisprite9B93RuntimeState state, byte substate)
    {
        if (state.Visual.AllEmpty)
            throw new InvalidOperationException("Flag08/bit04 route requires an active visual record.");
        if (substate == 0x0D)
            throw new InvalidOperationException("Substate $0D uses A12A, not the flag08 common route.");
        if ((state.Visual.Part0.Flags & 0x0C) != 0x0C)
            throw new InvalidOperationException("Route requires both visual flag $08 and flag $04 set.");
        var family = state.Logical.Action00 & 0xF0;
        if (family is 0xD0 or 0xE0)
            throw new InvalidOperationException("Logical D0/E0 diverts to A06E before this route.");
    }

    private static PlatformMultisprite9B93LogicalState SyncPosition(
        PlatformMultisprite9B93LogicalState logical,
        PlatformMultisprite9B93Part part) =>
        logical with { X01 = part.X, Y02 = part.Y };

    private static PlatformCombatEntity ToCombatEntity(PlatformMultisprite9B93LogicalState logical) =>
        new(logical.Action00, logical.X01, logical.Y02, logical.Type09,
            logical.Profile0C, logical.SeventhSenseReward0F);

    private static PlatformMultisprite9B93LogicalState FromCombatEntity(
        PlatformMultisprite9B93LogicalState logical,
        PlatformCombatEntity entity) =>
        logical with
        {
            Action00 = entity.State,
            X01 = entity.X,
            Y02 = entity.Y,
            Type09 = entity.Type,
            Profile0C = entity.HitPoints,
            SeventhSenseReward0F = entity.SeventhSenseRewardBcd,
        };

    private static PlatformHitboxParameters ToProjectileBox(PlatformContactHitboxParameters box) =>
        new(box.VerticalOrigin79, box.HorizontalOrigin7A, box.VerticalExtent7B, box.HorizontalExtent7C);

    private static PlatformMultisprite9B93VisualState Animate(
        PlatformMultisprite9B93VisualState visual,
        byte substate,
        byte frameCounter3C)
    {
        var table = substate == 0x0C ? Animation0C : substate == 0x10 ? Animation10 : AnimationDefault;
        var baseIndex = frameCounter3C & 0x18;
        var count = substate == 0x10 ? 1 : 4;
        for (var i = 0; i < count; i++)
        {
            var part = GetPart(visual, i);
            if (part.IsEmpty)
                continue;
            var index = baseIndex + i * 2;
            part = part with
            {
                Sprite = table[index],
                Flags = (byte)((part.Flags & 0x3F) | table[index + 1]),
            };
            visual = SetPart(visual, i, part);
        }
        return visual;
    }

    private static PlatformMultisprite9B93VisualState ClearAll(PlatformMultisprite9B93VisualState visual) =>
        new(Clear(visual.Part0), Clear(visual.Part1), Clear(visual.Part2), Clear(visual.Part3));

    private static PlatformMultisprite9B93Part Clear(PlatformMultisprite9B93Part part) =>
        part with { Y = 0xF0, Sprite = 0xFE };

    private static PlatformMultisprite9B93Part GetPart(PlatformMultisprite9B93VisualState visual, int index) => index switch
    {
        0 => visual.Part0, 1 => visual.Part1, 2 => visual.Part2, 3 => visual.Part3,
        _ => throw new ArgumentOutOfRangeException(nameof(index)),
    };

    private static PlatformMultisprite9B93VisualState SetPart(
        PlatformMultisprite9B93VisualState visual,
        int index,
        PlatformMultisprite9B93Part value) => index switch
    {
        0 => visual with { Part0 = value },
        1 => visual with { Part1 = value },
        2 => visual with { Part2 = value },
        3 => visual with { Part3 = value },
        _ => throw new ArgumentOutOfRangeException(nameof(index)),
    };
}

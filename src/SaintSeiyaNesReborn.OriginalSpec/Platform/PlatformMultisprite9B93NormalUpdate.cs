using SaintSeiyaNesReborn.OriginalSpec;

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformMultisprite9B93LogicalState(
    byte Action00,
    byte X01,
    byte Y02,
    byte Phase03,
    byte Field05,
    byte Type09,
    byte Profile0C,
    byte CosmoDrain0D,
    byte LifeDrain0E,
    byte SeventhSenseReward0F);

public readonly record struct PlatformMultisprite9B93RuntimeState(
    PlatformMultisprite9B93VisualState Visual,
    PlatformMultisprite9B93LogicalState Logical,
    byte Mode81);

public enum PlatformMultisprite9B93NormalOutcome
{
    Active,
    RemovedVerticalBand,
    TransitionedToFlag08,
}

public readonly record struct PlatformMultisprite9B93NormalUpdateResult(
    PlatformMultisprite9B93RuntimeState State,
    PlatformMultisprite9B93NormalOutcome Outcome,
    PlatformEntityContactResult Contact,
    PlatformProjectileHitSequenceResult? ProjectileHits,
    PlatformAttackState AttackState,
    PlatformContactPhaseState ContactState,
    int SeventhSense,
    byte? SoundId,
    bool ProjectileCheckRequested);

/// <summary>
/// Exact normal/flag-$08-clear branch of `$9CAC-$9DCC` plus the shared animation
/// table update at `$9DCD-$9E2C`.
///
/// Supported entry conditions:
/// - at least one visual part active;
/// - engine substate != $0D;
/// - part0 flags bit $08 clear;
/// - logical action family not $D0/$E0.
///
/// Other active branches are rejected explicitly and are promoted separately.
/// </summary>
public static class PlatformMultisprite9B93NormalUpdate
{
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

    public static PlatformMultisprite9B93NormalUpdateResult Step(
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

        var logical = state.Logical with
        {
            Y02 = state.Visual.Part0.Y,
            X01 = state.Visual.Part0.X,
        };
        var box = ContactBox(engineSubstate02);

        // $9D17: contact occurs before this class moves the visual parts.
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
        {
            contactState = new PlatformContactPhaseState(
                contact.HazardLatch76,
                contact.DrainState);
        }

        logical = logical with { Phase03 = unchecked((byte)(logical.Phase03 + 1)) };
        var verticalDelta = (byte)(logical.Phase03 >> 3);

        var visual = state.Visual;
        var count = engineSubstate02 == 0x10 ? 1 : 4;
        for (var i = 0; i < count; i++)
        {
            var part = GetPart(visual, i);
            var y = unchecked((byte)(part.Y + verticalDelta));
            if (y is >= 0xB0 and < 0xC0)
            {
                visual = ClearAll(visual);
                logical = logical with { Phase03 = 0 };
                state = state with { Visual = visual, Logical = logical };
                return new(
                    state,
                    PlatformMultisprite9B93NormalOutcome.RemovedVerticalBand,
                    contact,
                    null,
                    attacks,
                    contactState,
                    seventhSense,
                    null,
                    ProjectileCheckRequested: false);
            }

            var x = part.X;
            if (state.Mode81 == 0)
                x = unchecked((byte)(x - 2));
            x = unchecked((byte)(x - cameraDelta43));
            visual = SetPart(visual, i, part with { Y = y, X = x });
        }

        logical = logical with
        {
            Y02 = visual.Part0.Y,
            X01 = visual.Part0.X,
        };

        var requestProjectile = engineSubstate02 is 0x0C or 0x10
            || (visual.Part0.Flags & 0x04) == 0;
        PlatformProjectileHitSequenceResult? hits = null;

        if (requestProjectile)
        {
            var entity = ToCombatEntity(logical);
            hits = PlatformProjectileHitSequence.ResolveThreeSlots(
                attacks,
                entity,
                saint,
                new PlatformHitboxParameters(
                    box.VerticalOrigin79,
                    box.HorizontalOrigin7A,
                    box.VerticalExtent7B,
                    box.HorizontalExtent7C),
                platformDamage,
                seventhSense,
                engineSubstate02);
            attacks = hits.AttackState;
            seventhSense = hits.SeventhSense;
            logical = FromCombatEntity(logical, hits.Entity);
        }

        byte? sound = null;
        var transitioned = false;

        // $9D77-$9DC6: only the immediate/player-X mode can arm bit $08.
        if (state.Mode81 != 0
            && visual.Part0.Y >= playerY40
            && visual.Part0.Y < 0xF0
            && visual.Part0.Y >= 0x30
            && logical.Field05 >= 0x80)
        {
            visual = visual with
            {
                Part0 = visual.Part0 with { Flags = (byte)(visual.Part0.Flags | 0x08) },
            };
            logical = logical with
            {
                Phase03 = 0,
                CosmoDrain0D = (byte)(logical.CosmoDrain0D >> 1),
                LifeDrain0E = (byte)(logical.LifeDrain0E >> 1),
            };
            sound = (visual.Part0.Flags & 0x04) != 0 ? (byte)0x2B : (byte)0x2C;
            transitioned = true;
        }

        visual = Animate(visual, engineSubstate02, frameCounter3C);
        state = state with { Visual = visual, Logical = logical };

        return new(
            state,
            transitioned
                ? PlatformMultisprite9B93NormalOutcome.TransitionedToFlag08
                : PlatformMultisprite9B93NormalOutcome.Active,
            contact,
            hits,
            attacks,
            contactState,
            seventhSense,
            sound,
            requestProjectile);
    }

    private static PlatformContactHitboxParameters ContactBox(byte substate) =>
        substate == 0x10
            ? new PlatformContactHitboxParameters(0x04, 0x04, 0x02, 0x02)
            : new PlatformContactHitboxParameters(0x08, 0x08, 0x05, 0x05);

    private static PlatformCombatEntity ToCombatEntity(PlatformMultisprite9B93LogicalState logical) =>
        new(
            logical.Action00,
            logical.X01,
            logical.Y02,
            logical.Type09,
            logical.Profile0C,
            logical.SeventhSenseReward0F);

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

    private static PlatformMultisprite9B93VisualState Animate(
        PlatformMultisprite9B93VisualState visual,
        byte substate,
        byte frameCounter3C)
    {
        var table = substate == 0x0C
            ? Animation0C
            : substate == 0x10
                ? Animation10
                : AnimationDefault;
        var baseIndex = frameCounter3C & 0x18;
        var count = substate == 0x10 ? 1 : 4;

        for (var i = 0; i < count; i++)
        {
            var part = GetPart(visual, i);
            if (part.IsEmpty)
                continue;

            var index = baseIndex + i * 2;
            var sprite = table[index];
            var tableFlags = table[index + 1];
            part = part with
            {
                Sprite = sprite,
                Flags = (byte)((part.Flags & 0x3F) | tableFlags),
            };
            visual = SetPart(visual, i, part);
        }

        return visual;
    }

    private static void ValidateEntry(
        PlatformMultisprite9B93RuntimeState state,
        byte substate)
    {
        if (state.Visual.AllEmpty)
            throw new InvalidOperationException("Normal 9B93 update requires an active visual record.");
        if (substate == 0x0D)
            throw new InvalidOperationException("Substate $0D uses the dedicated A12A path.");
        if ((state.Visual.Part0.Flags & 0x08) != 0)
            throw new InvalidOperationException("Flag-$08-set 9B93 branch is not the normal path.");

        var family = state.Logical.Action00 & 0xF0;
        if (family is 0xD0 or 0xE0)
            throw new InvalidOperationException("Logical $D0/$E0 family uses the A06E path.");
    }

    private static PlatformMultisprite9B93VisualState ClearAll(PlatformMultisprite9B93VisualState visual) =>
        new(
            Clear(GetPart(visual, 0)),
            Clear(GetPart(visual, 1)),
            Clear(GetPart(visual, 2)),
            Clear(GetPart(visual, 3)));

    private static PlatformMultisprite9B93Part Clear(PlatformMultisprite9B93Part part) =>
        part with { Y = 0xF0, Sprite = 0xFE };

    private static PlatformMultisprite9B93Part GetPart(PlatformMultisprite9B93VisualState visual, int index) => index switch
    {
        0 => visual.Part0,
        1 => visual.Part1,
        2 => visual.Part2,
        3 => visual.Part3,
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

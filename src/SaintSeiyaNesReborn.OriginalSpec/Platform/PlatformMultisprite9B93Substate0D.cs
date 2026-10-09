using SaintSeiyaNesReborn.OriginalSpec;

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformMultisprite9B93Substate0DOutcome
{
    Active,
    RemovedHorizontalBoundary,
    DeathAnimating,
    DeathCompleted,
}

public readonly record struct PlatformMultisprite9B93Substate0DResult(
    PlatformMultisprite9B93RuntimeState State,
    PlatformMultisprite9B93Substate0DOutcome Outcome,
    PlatformEntityContactResult? Contact,
    PlatformProjectileHitSequenceResult? ProjectileHits,
    PlatformAttackState AttackState,
    PlatformContactPhaseState ContactState,
    int SeventhSense,
    bool UsedDeathPath,
    byte SpriteBaseUsed);

/// <summary>
/// Exact special `$02==$0D` updater at `$A12A-$A22B` for the independent
/// `$07E0/$03FB` multisprite class.
///
/// Normal route: player-relative turn/three-part formation -> animation ->
/// player contact -> projectile hit. Logical family `$D0` takes the dedicated
/// `$A20D` terminal animation and performs no collision work.
/// </summary>
public static class PlatformMultisprite9B93Substate0D
{
    public static readonly PlatformContactHitboxParameters ContactBox = new(0x04, 0x0C, 0x03, 0x0A);
    public static readonly PlatformHitboxParameters ProjectileBox = new(0x04, 0x0C, 0x03, 0x0A);

    public static PlatformMultisprite9B93Substate0DResult Step(
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
        byte frameStartPlayerAction4E)
    {
        if (state.Visual.AllEmpty)
            throw new InvalidOperationException("Substate $0D requires an active multisprite visual record.");

        if ((state.Logical.Action00 & 0xF0) == 0xD0)
            return StepDeath(state, attacks, contactState, seventhSense);

        var visual = state.Visual;
        var logical = state.Logical;
        var part0 = visual.Part0;

        // `$A137-$A150`: X register preserves the original part0 Y. Vertical
        // movement occurs only when byte(Y-16) < playerY, at 0/1 px depending
        // on frame parity. `$31` then becomes the resulting formation Y.
        var formationY = part0.Y;
        var yMinus16 = unchecked((byte)(part0.Y - 0x10));
        if (yMinus16 < playerY40)
        {
            formationY = unchecked((byte)(part0.Y + (frameCounter3C & 1)));
            part0 = part0 with { Y = formationY };
        }

        // `$A151-$A170`: turn only on the original unsigned boundary/relative
        // comparisons. All arithmetic is 8-bit and deliberately wraps.
        var flags = part0.Flags;
        var xBeforeMove = part0.X;
        if (xBeforeMove < 0x0C)
        {
            flags = 0x42;
        }
        else
        {
            var xPlus32 = unchecked((byte)(xBeforeMove + 0x20));
            if (xPlus32 < playerX3F)
            {
                flags = 0x42;
            }
            else if (xPlus32 >= 0xE0)
            {
                flags = 0x02;
            }
        }
        part0 = part0 with { Flags = flags };

        // `$A173-$A1AE`: part0 moves +/-2 relative to facing and then subtracts
        // camera. Parts1/2 are reconstructed horizontally at +/-8 and +/-16,
        // sharing part0 Y and flags. Part3 is deliberately untouched.
        byte movedX;
        byte middleX;
        byte farX;
        if ((flags & 0x40) == 0)
        {
            movedX = unchecked((byte)(xBeforeMove - 2 - cameraDelta43));
            middleX = unchecked((byte)(movedX + 8));
            farX = unchecked((byte)(movedX + 16));
        }
        else
        {
            movedX = unchecked((byte)(xBeforeMove + 2 - cameraDelta43));
            middleX = unchecked((byte)(movedX - 8));
            farX = unchecked((byte)(movedX - 16));
        }

        part0 = part0 with { X = movedX };
        var part1 = visual.Part1 with { Y = formationY, Flags = flags, X = middleX };
        var part2 = visual.Part2 with { Y = formationY, Flags = flags, X = farX };
        visual = visual with { Part0 = part0, Part1 = part1, Part2 = part2 };

        if (movedX < 0x04)
        {
            logical = logical with { Phase03 = 0 };
            state = state with { Visual = ClearAll(visual), Logical = logical };
            return new(
                state,
                PlatformMultisprite9B93Substate0DOutcome.RemovedHorizontalBoundary,
                null,
                null,
                attacks,
                contactState,
                seventhSense,
                UsedDeathPath: false,
                SpriteBaseUsed: 0);
        }

        // `$A1BA-$A1D8`: frame bit3 selects three sequential sprite values.
        var spriteBase = (frameCounter3C & 0x08) == 0 ? (byte)0x8C : (byte)0xD1;
        part0 = visual.Part0 with { Sprite = spriteBase };
        part1 = visual.Part1 with { Sprite = unchecked((byte)(spriteBase + 1)) };
        part2 = visual.Part2 with { Sprite = unchecked((byte)(spriteBase + 2)) };
        visual = visual with { Part0 = part0, Part1 = part1, Part2 = part2 };

        // `$A1E5` sync/geometry -> `$9CFC/$98BA`: contact first.
        logical = logical with { Y02 = part0.Y, X01 = part0.X };
        var contact = PlatformEntityContact.Evaluate(
            frameStartPlayerAction4E,
            playerX3F,
            playerY40,
            part0.X,
            part0.Y,
            contactState.HazardLatch76,
            logical.LifeDrain0E,
            logical.CosmoDrain0D,
            ContactBox);
        if (contact.Triggered)
            contactState = new PlatformContactPhaseState(contact.HazardLatch76, contact.DrainState);

        // `$A1DF A1E5 -> $9E2D/$9915`: the same part0 position and wide box are
        // prepared again, then projectile collision runs after contact.
        logical = logical with { Y02 = part0.Y, X01 = part0.X };
        var hits = PlatformProjectileHitSequence.ResolveThreeSlots(
            attacks,
            ToCombatEntity(logical),
            saint,
            ProjectileBox,
            platformDamage,
            seventhSense,
            engineSubstate02: 0x0D);
        attacks = hits.AttackState;
        seventhSense = hits.SeventhSense;
        logical = FromCombatEntity(logical, hits.Entity);

        state = state with { Visual = visual, Logical = logical };
        return new(
            state,
            PlatformMultisprite9B93Substate0DOutcome.Active,
            contact,
            hits,
            attacks,
            contactState,
            seventhSense,
            UsedDeathPath: false,
            SpriteBaseUsed: spriteBase);
    }

    private static PlatformMultisprite9B93Substate0DResult StepDeath(
        PlatformMultisprite9B93RuntimeState state,
        PlatformAttackState attacks,
        PlatformContactPhaseState contactState,
        int seventhSense)
    {
        var logical = state.Logical with
        {
            Action00 = unchecked((byte)(state.Logical.Action00 + 1)),
        };

        if (logical.Action00 >= 0xE0)
        {
            logical = logical with { Phase03 = 0 };
            state = state with { Visual = ClearAll(state.Visual), Logical = logical };
            return new(
                state,
                PlatformMultisprite9B93Substate0DOutcome.DeathCompleted,
                null,
                null,
                attacks,
                contactState,
                seventhSense,
                UsedDeathPath: true,
                SpriteBaseUsed: 0);
        }

        // `$A21D-$A22B`: only the first three sprite bytes become $6A. Part3 is
        // preserved until the terminal all-record cleanup at action $E0.
        var visual = state.Visual with
        {
            Part0 = state.Visual.Part0 with { Sprite = 0x6A },
            Part1 = state.Visual.Part1 with { Sprite = 0x6A },
            Part2 = state.Visual.Part2 with { Sprite = 0x6A },
        };
        state = state with { Visual = visual, Logical = logical };

        return new(
            state,
            PlatformMultisprite9B93Substate0DOutcome.DeathAnimating,
            null,
            null,
            attacks,
            contactState,
            seventhSense,
            UsedDeathPath: true,
            SpriteBaseUsed: 0x6A);
    }

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

    private static PlatformMultisprite9B93VisualState ClearAll(PlatformMultisprite9B93VisualState visual) =>
        new(Clear(visual.Part0), Clear(visual.Part1), Clear(visual.Part2), Clear(visual.Part3));

    private static PlatformMultisprite9B93Part Clear(PlatformMultisprite9B93Part part) =>
        part with { Y = 0xF0, Sprite = 0xFE };
}

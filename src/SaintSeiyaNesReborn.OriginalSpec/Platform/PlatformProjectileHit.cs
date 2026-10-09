using SaintSeiyaNesReborn.OriginalSpec;

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformHitboxParameters(
    byte VerticalOrigin79,
    byte HorizontalOrigin7A,
    byte VerticalExtent7B,
    byte HorizontalExtent7C)
{
    public static PlatformHitboxParameters Common => new(8, 8, 5, 5);
    public static PlatformHitboxParameters Reduced => new(4, 4, 2, 2);
    public static PlatformHitboxParameters Auxiliary => new(4, 4, 3, 3);
    public static PlatformHitboxParameters Tall => new(16, 8, 14, 4);
    public static PlatformHitboxParameters Square => new(8, 8, 6, 6);
}

public readonly record struct PlatformCombatEntity(
    byte State,
    byte X,
    byte Y,
    byte Type,
    byte HitPoints,
    byte SeventhSenseRewardBcd,
    byte Motion3 = 0,
    byte RightTerrain0A = 0,
    byte LeftTerrain0B = 0);

public enum PlatformEntityDamageOutcome
{
    Survived,
    Killed,
}

public readonly record struct PlatformEntityDamageResult(
    PlatformCombatEntity Entity,
    PlatformEntityDamageOutcome Outcome,
    int SeventhSense,
    bool AwardedSeventhSense);

/// <summary>
/// Projectile point-vs-entity rectangle gate from bank 3 $9915/$992A and the
/// ordinary HP subtraction path at $99BA+.
/// </summary>
public static class PlatformProjectileHit
{
    public static bool IsAcceptedAttackType(byte type)
    {
        var masked = type & 0xFE;
        return masked is 0x64 or 0x54;
    }

    /// <summary>
    /// Reproduces the original unsigned byte comparisons. Low bounds are
    /// exclusive; high bounds are inclusive. All additions/subtractions wrap
    /// as 8-bit 6502 arithmetic before the comparison.
    /// </summary>
    public static bool Overlaps(
        PlatformAttackObject attack,
        PlatformCombatEntity entity,
        PlatformHitboxParameters box)
    {
        if (!IsAcceptedAttackType(attack.Type))
            return false;

        var yCenter = Add(entity.Y, box.VerticalOrigin79);
        var lowY = Sub(Sub(yCenter, box.VerticalExtent7B), 6);
        if (lowY >= attack.Y)
            return false;
        var highY = Add(yCenter, box.VerticalExtent7B);
        if (highY < attack.Y)
            return false;

        var xCenter = Add(entity.X, box.HorizontalOrigin7A);
        var lowX = Sub(Sub(xCenter, box.HorizontalExtent7C), 8);
        if (lowX >= attack.X)
            return false;
        var highX = Add(xCenter, box.HorizontalExtent7C);
        if (highX < attack.X)
            return false;

        return true;
    }

    /// <summary>
    /// Helper $9A27: on the standard hit path only Hyoga/Shiryu consume the
    /// current attack object immediately. Seiya/Shun/Ikki leave it active.
    /// Some enemy-type branches bypass this helper entirely.
    /// </summary>
    public static PlatformAttackSlot ApplyStandardHitConsumption(
        PlatformAttackSlot slot,
        PlatformSaintIndex saint)
    {
        if (saint is not (PlatformSaintIndex.Hyoga or PlatformSaintIndex.Shiryu))
            return slot;

        return slot with
        {
            Object = slot.Object with
            {
                Y = 0xF0,
                Type = 0xFE,
            },
        };
    }

    public static bool UsesOrdinaryHpPath(byte entityType) =>
        entityType == 0
        || entityType is >= 0x05 and <= 0x09
        || entityType is 0x0C or 0x0D or 0x0F;

    /// <summary>
    /// $99BA+ ordinary enemy HP path. This deliberately excludes the type-
    /// specific pre-routing that decides whether $99BA is entered.
    /// </summary>
    public static PlatformEntityDamageResult ApplyOrdinaryHpDamage(
        PlatformCombatEntity entity,
        int damage,
        int currentSeventhSense,
        byte engineSubstate02)
    {
        if (damage is < 0 or > 255)
            throw new ArgumentOutOfRangeException(nameof(damage));
        if (!UsesOrdinaryHpPath(entity.Type))
            throw new InvalidOperationException($"Entity type ${entity.Type:X2} is not on the ordinary HP path.");

        var remaining = entity.HitPoints - damage;
        if (remaining > 0)
        {
            return new PlatformEntityDamageResult(
                entity with
                {
                    HitPoints = (byte)remaining,
                    State = 0x40,
                },
                PlatformEntityDamageOutcome.Survived,
                currentSeventhSense,
                AwardedSeventhSense: false);
        }

        var nextSense = SeventhSense.AddPlatformReward(
            currentSeventhSense,
            entity.SeventhSenseRewardBcd,
            engineSubstate02);
        var deathState = entity.Type == 0x0D ? (byte)0xA0 : (byte)0xD0;

        return new PlatformEntityDamageResult(
            entity with { State = deathState },
            PlatformEntityDamageOutcome.Killed,
            nextSense,
            AwardedSeventhSense: engineSubstate02 != 0 && entity.SeventhSenseRewardBcd != 0);
    }

    private static byte Add(byte a, byte b) => unchecked((byte)(a + b));
    private static byte Sub(byte a, byte b) => unchecked((byte)(a - b));
    private static byte Sub(byte a, int b) => unchecked((byte)(a - b));
}

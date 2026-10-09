namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformEntityContactOutcome
{
    NoContact,
    FrameStartSpecial40Immune,
    PlayerBelowActiveRegion,
    OutsideVerticalWindow,
    OutsideHorizontalWindow,
    ContactLatchActive,
    ContactTriggered,
}

public readonly record struct PlatformEntityContactResult(
    PlatformEntityContactOutcome Outcome,
    byte HazardLatch76,
    ContactDrainState DrainState,
    byte? SoundId)
{
    public bool Triggered => Outcome == PlatformEntityContactOutcome.ContactTriggered;
}

/// <summary>
/// Exact ordinary entity-to-player contact gate at bank 3 $98BA-$9914.
///
/// This routine is called from the common entity path with parameters prepared
/// by $9915: $79=$10, $7A=$08, $7B=$0E, $7C=$04. The reduction below keeps the
/// resulting 8-bit comparison arithmetic literal instead of replacing it with a
/// generic modern rectangle intersection.
/// </summary>
public static class PlatformEntityContact
{
    public const byte ContactLatchSeed = 0x20;
    public const byte ContactSoundId = 0x26;

    public static PlatformEntityContactResult EvaluateOrdinary(
        byte frameStartAction4E,
        byte playerX,
        byte playerY,
        byte entityX,
        byte entityY,
        byte currentHazardLatch76,
        byte entityLifeDrainTicks,
        byte entityCosmoDrainTicks)
    {
        if (PlatformActionState.Family(frameStartAction4E) == (byte)PlatformActionFamily.Special40)
            return NoContact(PlatformEntityContactOutcome.FrameStartSpecial40Immune, currentHazardLatch76);

        if (playerY >= 0x90)
            return NoContact(PlatformEntityContactOutcome.PlayerBelowActiveRegion, currentHazardLatch76);

        // $98C8-$98E0 with $79=$10 and $7B=$0E:
        // temp = byte(entityY + 16)
        // lower = byte(temp - 14 - 30) = byte(entityY - 28)
        // upper = byte(temp + 14)      = byte(entityY + 30)
        // accept only lower < playerY <= upper (unsigned byte compares).
        var verticalTemp = unchecked((byte)(entityY + 0x10));
        var verticalLower = unchecked((byte)(verticalTemp - 0x0E - 0x1E));
        var verticalUpper = unchecked((byte)(verticalTemp + 0x0E));
        if (verticalLower >= playerY || verticalUpper < playerY)
            return NoContact(PlatformEntityContactOutcome.OutsideVerticalWindow, currentHazardLatch76);

        // $98E2-$98FA with $7A=$08 and $7C=$04:
        // temp = byte(entityX + 8)
        // lower = byte(temp - 4 - 12) = byte(entityX - 8)
        // upper = byte(temp + 4)      = byte(entityX + 12)
        // accept only lower < playerX <= upper.
        var horizontalTemp = unchecked((byte)(entityX + 0x08));
        var horizontalLower = unchecked((byte)(horizontalTemp - 0x04 - 0x0C));
        var horizontalUpper = unchecked((byte)(horizontalTemp + 0x04));
        if (horizontalLower >= playerX || horizontalUpper < playerX)
            return NoContact(PlatformEntityContactOutcome.OutsideHorizontalWindow, currentHazardLatch76);

        if (currentHazardLatch76 != 0)
            return NoContact(PlatformEntityContactOutcome.ContactLatchActive, currentHazardLatch76);

        return new PlatformEntityContactResult(
            PlatformEntityContactOutcome.ContactTriggered,
            HazardLatch76: ContactLatchSeed,
            DrainState: new ContactDrainState(entityLifeDrainTicks, entityCosmoDrainTicks),
            SoundId: ContactSoundId);
    }

    private static PlatformEntityContactResult NoContact(
        PlatformEntityContactOutcome outcome,
        byte hazardLatch76) =>
        new(
            outcome,
            hazardLatch76,
            default,
            null);
}

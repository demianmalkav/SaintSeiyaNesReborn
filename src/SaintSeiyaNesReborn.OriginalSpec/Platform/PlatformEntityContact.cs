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

public readonly record struct PlatformContactHitboxParameters(
    byte VerticalOrigin79,
    byte HorizontalOrigin7A,
    byte VerticalExtent7B,
    byte HorizontalExtent7C)
{
    /// <summary>$9915 common-entity setup before $98BA.</summary>
    public static PlatformContactHitboxParameters CommonEntity => new(0x10, 0x08, 0x0E, 0x04);

    /// <summary>$989C auxiliary-slot setup before $98BA.</summary>
    public static PlatformContactHitboxParameters AuxiliaryHazard => new(0x04, 0x04, 0x03, 0x03);
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
/// Exact entity/hazard-to-player contact gate at bank 3 $98BA-$9914.
///
/// The original receives geometry through zero-page $79-$7C. Common entities
/// and the independent $07B0/$07B8 auxiliary slots install different values,
/// so geometry is explicit rather than hard-coded to one caller.
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
        byte entityCosmoDrainTicks) =>
        Evaluate(
            frameStartAction4E,
            playerX,
            playerY,
            entityX,
            entityY,
            currentHazardLatch76,
            entityLifeDrainTicks,
            entityCosmoDrainTicks,
            PlatformContactHitboxParameters.CommonEntity);

    public static PlatformEntityContactResult Evaluate(
        byte frameStartAction4E,
        byte playerX,
        byte playerY,
        byte entityX,
        byte entityY,
        byte currentHazardLatch76,
        byte entityLifeDrainTicks,
        byte entityCosmoDrainTicks,
        PlatformContactHitboxParameters box)
    {
        if (PlatformActionState.Family(frameStartAction4E) == (byte)PlatformActionFamily.Special40)
            return NoContact(PlatformEntityContactOutcome.FrameStartSpecial40Immune, currentHazardLatch76);

        if (playerY >= 0x90)
            return NoContact(PlatformEntityContactOutcome.PlayerBelowActiveRegion, currentHazardLatch76);

        // $98C8-$98E0:
        // temp = byte(entityY + $79)
        // lower = byte(temp - $7B - $1E)
        // upper = byte(temp + $7B)
        // accept only lower < playerY <= upper.
        var verticalTemp = unchecked((byte)(entityY + box.VerticalOrigin79));
        var verticalLower = unchecked((byte)(verticalTemp - box.VerticalExtent7B - 0x1E));
        var verticalUpper = unchecked((byte)(verticalTemp + box.VerticalExtent7B));
        if (verticalLower >= playerY || verticalUpper < playerY)
            return NoContact(PlatformEntityContactOutcome.OutsideVerticalWindow, currentHazardLatch76);

        // $98E2-$98FA:
        // temp = byte(entityX + $7A)
        // lower = byte(temp - $7C - $0C)
        // upper = byte(temp + $7C)
        // accept only lower < playerX <= upper.
        var horizontalTemp = unchecked((byte)(entityX + box.HorizontalOrigin7A));
        var horizontalLower = unchecked((byte)(horizontalTemp - box.HorizontalExtent7C - 0x0C));
        var horizontalUpper = unchecked((byte)(horizontalTemp + box.HorizontalExtent7C));
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

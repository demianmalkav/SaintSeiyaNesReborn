namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

/// <summary>
/// Raw semantic view of the parent-attached visual/hazard bytes addressed from
/// the entity visual pointer $18. Offset names are intentionally retained where
/// the renderer-side role has not yet been proven.
///
/// $A908 writes +$2C,+$2D,+$2E,+$2F,+$32. $AA70 reads +$2C/+2F for collision
/// and, on contact, clears +$2C,+$2D,+$30,+$31. Keeping those bytes distinct
/// avoids inventing a sprite-layout interpretation before the render helper is
/// reconstructed.
/// </summary>
public readonly record struct PlatformEntityAttachedHazardState(
    byte Raw2C,
    byte Raw2D,
    byte Raw2E,
    byte Raw2F,
    byte Raw30,
    byte Raw31,
    byte Raw32,
    byte Raw33)
{
    public const byte FreeSpriteMarker = 0xFE;
    public const byte HiddenYMarker = 0xF0;

    public bool SpawnSlotFree => Raw2D == FreeSpriteMarker;

    public static PlatformEntityAttachedHazardState Empty =>
        new(
            Raw2C: HiddenYMarker,
            Raw2D: FreeSpriteMarker,
            Raw2E: 0,
            Raw2F: 0,
            Raw30: HiddenYMarker,
            Raw31: FreeSpriteMarker,
            Raw32: 0,
            Raw33: 0);
}

public enum PlatformEntityAttachedHazardSpawnOutcome
{
    Spawned,
    SlotOccupied,
    NoTemplate,
}

public readonly record struct PlatformEntityAttachedHazardSpawnResult(
    PlatformEntityAttachedHazardSpawnOutcome Outcome,
    PlatformEntityAttachedHazardState State,
    PlatformSecondarySpawnTemplate Template,
    byte? ParentOffset08Value)
{
    public bool Spawned => Outcome == PlatformEntityAttachedHazardSpawnOutcome.Spawned;
}

public enum PlatformEntityAttachedHazardContactOutcome
{
    NoContact,
    FrameStartSpecial40Immune,
    PlayerBelowActiveRegion,
    OutsideVerticalWindow,
    OutsideHorizontalWindow,
    ContactLatchActive,
    ContactTriggered,
}

public readonly record struct PlatformEntityAttachedHazardContactResult(
    PlatformEntityAttachedHazardContactOutcome Outcome,
    PlatformEntityAttachedHazardState State,
    byte HazardLatch76,
    ContactDrainState DrainState,
    byte? SoundId)
{
    public bool Triggered => Outcome == PlatformEntityAttachedHazardContactOutcome.ContactTriggered;
}

/// <summary>
/// Exact clean-room reduction of bank-3 $A908-$A96F and $AA70-$AAE5.
///
/// $A908 creates a small visual/hazard record attached to a parent entity.
/// $AA70 later tests that record against the player with a reduced 4/4/2/2
/// geometry, seeds the parent's drain profile on contact, and deactivates the
/// attached record.
/// </summary>
public static class PlatformEntityAttachedHazard
{
    public const byte ContactLatchSeed = 0x20;
    public const byte ContactSoundId = 0x26;

    /// <summary>
    /// Bank-3 $A908-$A96F.
    ///
    /// The parent +$08 side effect is returned explicitly because that byte is
    /// not yet represented by PlatformCommonEntityRuntimeState. When engine $00
    /// equals $20 it receives $03AB; otherwise it receives $F0.
    /// </summary>
    public static PlatformEntityAttachedHazardSpawnResult TrySpawn(
        PlatformEntityAttachedHazardState state,
        PlatformCommonEntityMotionState parent,
        byte engineState00,
        byte alternateParent08_03AB)
    {
        if (!state.SpawnSlotFree)
        {
            return new(
                PlatformEntityAttachedHazardSpawnOutcome.SlotOccupied,
                state,
                default,
                ParentOffset08Value: null);
        }

        var template = PlatformSecondarySpawnTemplate.ForEntityType(parent.Type);
        if (!template.CanCreateObject)
        {
            return new(
                PlatformEntityAttachedHazardSpawnOutcome.NoTemplate,
                state,
                template,
                ParentOffset08Value: null);
        }

        var flags = (byte)((parent.FlagsFacing & 0x40) | 0x03);
        var xDelta = (flags & 0x40) != 0 ? 9 : -9;
        var next = state with
        {
            Raw2C = unchecked((byte)(parent.Y + template.VerticalOffset)),
            Raw2D = template.ObjectType,
            Raw2E = flags,
            Raw2F = unchecked((byte)(parent.X + xDelta)),
            Raw32 = flags,
        };

        var parent08 = engineState00 == 0x20
            ? alternateParent08_03AB
            : (byte)0xF0;

        return new(
            PlatformEntityAttachedHazardSpawnOutcome.Spawned,
            next,
            template,
            parent08);
    }

    /// <summary>
    /// Bank-3 $AA70-$AAE5.
    ///
    /// Geometry is intentionally written literally rather than routed through
    /// $98BA because $AA70 has one additional rule: when frame-start player
    /// action $4E equals exactly $20, the vertical upper bound is reduced by
    /// $10 before the final Y comparison.
    /// </summary>
    public static PlatformEntityAttachedHazardContactResult EvaluateContact(
        PlatformEntityAttachedHazardState state,
        byte frameStartAction4E,
        byte playerX,
        byte playerY,
        byte currentHazardLatch76,
        byte parentLifeDrainTicks,
        byte parentCosmoDrainTicks)
    {
        if (PlatformActionState.Family(frameStartAction4E) == (byte)PlatformActionFamily.Special40)
            return NoContact(PlatformEntityAttachedHazardContactOutcome.FrameStartSpecial40Immune, state, currentHazardLatch76);

        if (playerY >= 0x90)
            return NoContact(PlatformEntityAttachedHazardContactOutcome.PlayerBelowActiveRegion, state, currentHazardLatch76);

        // $AA7E-$AA9D with $79=$04 and $7B=$02.
        var verticalTemp = unchecked((byte)(state.Raw2C + 0x04));
        var verticalLower = unchecked((byte)(verticalTemp - 0x02 - 0x1E));
        if (verticalLower >= playerY)
            return NoContact(PlatformEntityAttachedHazardContactOutcome.OutsideVerticalWindow, state, currentHazardLatch76);

        var verticalUpper = unchecked((byte)(verticalTemp + 0x02));
        if (frameStartAction4E == 0x20)
            verticalUpper = unchecked((byte)(verticalUpper - 0x10));
        if (verticalUpper < playerY)
            return NoContact(PlatformEntityAttachedHazardContactOutcome.OutsideVerticalWindow, state, currentHazardLatch76);

        // $AAA1-$AAB7 with $7A=$04 and $7C=$02.
        var horizontalTemp = unchecked((byte)(state.Raw2F + 0x04));
        var horizontalLower = unchecked((byte)(horizontalTemp - 0x02 - 0x0C));
        if (horizontalLower >= playerX)
            return NoContact(PlatformEntityAttachedHazardContactOutcome.OutsideHorizontalWindow, state, currentHazardLatch76);

        var horizontalUpper = unchecked((byte)(horizontalTemp + 0x02));
        if (horizontalUpper < playerX)
            return NoContact(PlatformEntityAttachedHazardContactOutcome.OutsideHorizontalWindow, state, currentHazardLatch76);

        if (currentHazardLatch76 != 0)
            return NoContact(PlatformEntityAttachedHazardContactOutcome.ContactLatchActive, state, currentHazardLatch76);

        // $AAB9-$AAE4: seed latch/drains, play $26, then deactivate four raw
        // bytes. Flags/positions not explicitly written by the ROM are retained.
        var deactivated = state with
        {
            Raw2C = PlatformEntityAttachedHazardState.HiddenYMarker,
            Raw2D = PlatformEntityAttachedHazardState.FreeSpriteMarker,
            Raw30 = PlatformEntityAttachedHazardState.HiddenYMarker,
            Raw31 = PlatformEntityAttachedHazardState.FreeSpriteMarker,
        };

        return new(
            PlatformEntityAttachedHazardContactOutcome.ContactTriggered,
            deactivated,
            HazardLatch76: ContactLatchSeed,
            DrainState: new ContactDrainState(parentLifeDrainTicks, parentCosmoDrainTicks),
            SoundId: ContactSoundId);
    }

    private static PlatformEntityAttachedHazardContactResult NoContact(
        PlatformEntityAttachedHazardContactOutcome outcome,
        PlatformEntityAttachedHazardState state,
        byte hazardLatch76) =>
        new(
            outcome,
            state,
            hazardLatch76,
            default,
            null);
}

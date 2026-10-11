namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformMetaspriteResourceKind
{
    PointerTable,
    DirectDefinition,
    Unsupported,
}

/// <summary>
/// Mechanical result of the shared bank-3 compositor selector at $B987-$BACF.
/// Address is either a 16-entry pointer table or a direct metasprite definition.
/// UpdatedAction models the renderer-owned $28 write performed by the dynamic
/// entity path at $BA62-$BA70.
/// </summary>
public readonly record struct PlatformMetaspriteResourceSelection(
    PlatformMetaspriteResourceKind Kind,
    ushort Address,
    byte UpdatedAction,
    bool ActionWasWritten)
{
    public bool IsSupported => Kind != PlatformMetaspriteResourceKind.Unsupported;
}

/// <summary>
/// Exact selector layer shared by the platform player and primary-entity renderer.
/// This API intentionally models only primary entity types $05-$0F. Player indices
/// $00-$04 reuse the same lower-level compositor but have a distinct family-$10
/// cadence through $4D and are outside this boundary.
/// </summary>
public static class PlatformMetaspriteResourceSelector
{
    public const ushort DirectBlankDefinitionB647 = 0xB647;

    public const ushort TableB671 = 0xB671;
    public const ushort TableB699 = 0xB699;
    public const ushort TableB6C1 = 0xB6C1;
    public const ushort TableB6E9 = 0xB6E9;
    public const ushort TableB711 = 0xB711;
    public const ushort TableB739 = 0xB739;
    public const ushort TableB761 = 0xB761;
    public const ushort TableB789 = 0xB789;
    public const ushort TableB7B1 = 0xB7B1;
    public const ushort TableB7D9 = 0xB7D9;
    public const ushort TableB801 = 0xB801;

    /// <summary>
    /// Resolve the ROM selector for one primary entity render call.
    ///
    /// entityType = logical +$09 / zero-page $26
    /// action     = logical +$00 / zero-page $28
    /// control04  = logical +$04 / zero-page $27
    /// ground05   = logical +$05 / zero-page $29
    /// frame3C    = global frame counter $3C
    /// animation03B9 = global animation gate $03B9
    /// </summary>
    public static PlatformMetaspriteResourceSelection ResolvePrimary(
        byte entityType,
        byte action,
        byte control04,
        byte ground05,
        byte frame3C,
        byte animation03B9)
    {
        if (entityType is < 0x05 or > 0x0F)
            throw new ArgumentOutOfRangeException(nameof(entityType), entityType, "Primary visual type must be $05-$0F.");

        var family = (byte)(action & 0xF0);

        if (family is 0x80 or 0x40 or 0xE0)
            return (frame3C & 0x04) == 0
                ? Direct(DirectBlankDefinitionB647, action)
                : Table(TableB7D9, action);

        if (family == 0x70)
        {
            if (entityType == 0x0C)
                return (frame3C & 0x10) == 0
                    ? Table(TableB711, action)
                    : Table(TableB761, action);

            if (entityType is 0x08 or 0x09)
                return action < 0x78
                    ? Table(TableB761, action)
                    : Table(TableB711, action);

            return action < 0x78
                ? Table(TableB6E9, action)
                : Table(TableB711, action);
        }

        if (family == 0xA0)
        {
            if (action < 0xA4)
                return Table(TableB6E9, action);
            if (action < 0xA8)
                return (frame3C & 0x10) == 0
                    ? Table(TableB711, action)
                    : Table(TableB761, action);
            if (action < 0xAC)
                return Table(TableB739, action);
            return Table(TableB761, action);
        }

        if (family == 0xD0)
        {
            if (action < 0xD4)
                return (frame3C & 0x04) == 0
                    ? Direct(DirectBlankDefinitionB647, action)
                    : Table(TableB7D9, action);

            if ((frame3C & 0x04) == 0)
                return Direct(DirectBlankDefinitionB647, action);

            if (ground05 < 0x80 || ground05 >= 0xF0)
                return Table(TableB7D9, action);

            return Table(TableB801, action);
        }

        // With logical +$04 nonzero, $BA97 routes by action family and skips
        // the family-$00/$10 dynamic branches below.
        if (control04 != 0)
        {
            return family switch
            {
                0x20 => Table(TableB761, action),
                0x30 => Table(TableB7B1, action),
                _ => Table(TableB711, action),
            };
        }

        if (family == 0x00)
        {
            if (entityType == 0x0C)
                return (frame3C & 0x10) == 0
                    ? Table(TableB6E9, action)
                    : Table(TableB739, action);

            if (entityType == 0x0F)
                return (frame3C & 0x02) == 0
                    ? Table(TableB6E9, action)
                    : Table(TableB739, action);

            if (entityType == 0x0E)
                return ResolveDynamic(action, animation03B9);

            return Table(TableB6E9, action);
        }

        if (family == 0x20)
            return Table(TableB739, action);

        if (family is 0x30 or 0x50)
            return Table(TableB789, action);

        if (family == 0x10)
            return ResolveDynamic(action, animation03B9);

        // The original code at $BA58 is a self-loop for other family/control=0
        // combinations. Preserve that as unsupported instead of inventing art state.
        return new(
            PlatformMetaspriteResourceKind.Unsupported,
            Address: 0,
            UpdatedAction: action,
            ActionWasWritten: false);
    }

    /// <summary>
    /// Primary slots normally own eleven 4-byte OAM records. Type $0D is the
    /// proven exception: one reachable definition has twelve records and $A647
    /// explicitly retires the extra record at visual +$2C/+2D.
    /// </summary>
    public static int VisualRecordCapacity(byte entityType)
    {
        if (entityType is < 0x05 or > 0x0F)
            throw new ArgumentOutOfRangeException(nameof(entityType), entityType, "Primary visual type must be $05-$0F.");
        return entityType == 0x0D ? 12 : 11;
    }

    /// <summary>
    /// Address of the little-endian definition pointer owned by one 16-entry table.
    /// </summary>
    public static ushort PointerEntryAddress(ushort tableAddress, byte typeOrSaintIndex)
    {
        if (typeOrSaintIndex > 0x0F)
            throw new ArgumentOutOfRangeException(nameof(typeOrSaintIndex), typeOrSaintIndex, "Shared compositor index must be $00-$0F.");
        return checked((ushort)(tableAddress + typeOrSaintIndex * 2));
    }

    private static PlatformMetaspriteResourceSelection ResolveDynamic(byte action, byte animation03B9)
    {
        var updatedAction = action;
        var actionWasWritten = false;

        if (animation03B9 == 0)
        {
            updatedAction = (byte)((action + 1) & 0x13);
            actionWasWritten = true;
        }

        var phase = updatedAction & 0x03;
        var table = phase switch
        {
            0 => TableB699,
            1 => TableB671,
            2 => TableB699,
            3 => TableB6C1,
            _ => throw new InvalidOperationException(),
        };

        return new(
            PlatformMetaspriteResourceKind.PointerTable,
            table,
            updatedAction,
            actionWasWritten);
    }

    private static PlatformMetaspriteResourceSelection Table(ushort address, byte action) =>
        new(PlatformMetaspriteResourceKind.PointerTable, address, action, ActionWasWritten: false);

    private static PlatformMetaspriteResourceSelection Direct(ushort address, byte action) =>
        new(PlatformMetaspriteResourceKind.DirectDefinition, address, action, ActionWasWritten: false);
}

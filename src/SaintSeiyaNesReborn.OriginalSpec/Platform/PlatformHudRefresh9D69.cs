namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformHudPpuOperationKind
{
    WritePpuControl,
    WritePpuAddress,
    WritePpuData,
}

public readonly record struct PlatformHudPpuOperation(
    PlatformHudPpuOperationKind Kind,
    ushort Address,
    byte Value);

/// <summary>
/// Active current-Saint resource values consumed by bank-1 $9D69-$9EED.
/// The one-byte hundreds fields use only their low nibble, matching the ROM.
/// ResourceCaps packs Life cap in the high nibble and Cosmo cap in the low nibble.
/// </summary>
public readonly record struct PlatformHudResourceState(
    byte LifeLowTwoDigits,
    byte LifeHundreds,
    byte CosmoLowTwoDigits,
    byte CosmoHundreds,
    byte ResourceCaps,
    byte SeventhSenseLowTwoDigits,
    byte SeventhSenseHighTwoDigits);

public sealed record PlatformHudRefresh9D69Result(
    byte Phase73,
    byte ExecutedPhase,
    byte? Scratch39Write,
    IReadOnlyList<PlatformHudPpuOperation> Operations)
{
    public bool WrotePpuData => Operations.Any(op => op.Kind == PlatformHudPpuOperationKind.WritePpuData);
}

/// <summary>
/// Clean-room semantic model of bank-1 $9D69-$9EED.
///
/// The routine is the downstream HUD/status writer reached from $9915 when no
/// palette mutation owns the current refresh. Every invocation first writes
/// PPUCTRL=0, advances $73 modulo four, then executes one bounded presentation
/// phase. This model emits only semantic PPU register writes and tile IDs; it
/// contains no extracted nametable or graphics payloads.
/// </summary>
public static class PlatformHudRefresh9D69
{
    public const ushort CosmoDigitsAddress = 0x22F0;
    public const ushort CosmoGaugeAddress = 0x22F4;
    public const ushort LifeDigitsAddress = 0x2330;
    public const ushort LifeGaugeAddress = 0x2334;
    public const ushort SeventhSenseDigitsAddress = 0x236F;
    public const ushort SeventhSenseGaugeAddress = 0x2374;

    public const byte DigitTileBase = 0x80;
    public const byte EmptyGaugeTile = 0xA7;
    public const byte ResourceFullGaugeTile = 0xBF;
    public const byte SeventhSenseFullGaugeTile = 0xBE;
    public const int SeventhSenseGaugeSegments = 10;

    public static PlatformHudRefresh9D69Result Step(
        byte incomingPhase73,
        byte platformSubstate02,
        PlatformHudResourceState resources)
    {
        var operations = new List<PlatformHudPpuOperation>(64)
        {
            new(PlatformHudPpuOperationKind.WritePpuControl, 0x2000, 0x00),
        };

        var phase = (byte)((incomingPhase73 + 1) & 0x03);
        byte? scratch39Write = null;

        switch (phase)
        {
            case 0:
                EmitPhase0Digits(operations, platformSubstate02, resources);
                break;

            case 1:
                EmitResourceGauge(
                    operations,
                    CosmoGaugeAddress,
                    capSegments: resources.ResourceCaps & 0x0F,
                    fullSegments: resources.CosmoHundreds & 0x0F,
                    partialPackedBcd: resources.CosmoLowTwoDigits);
                break;

            case 2:
                EmitResourceGauge(
                    operations,
                    LifeGaugeAddress,
                    capSegments: (resources.ResourceCaps >> 4) & 0x0F,
                    fullSegments: resources.LifeHundreds & 0x0F,
                    partialPackedBcd: resources.LifeLowTwoDigits);
                break;

            case 3:
                if (platformSubstate02 != 0)
                {
                    SetPpuAddress(operations, SeventhSenseGaugeAddress);
                    RepeatTile(operations, EmptyGaugeTile, SeventhSenseGaugeSegments);

                    SetPpuAddress(operations, SeventhSenseGaugeAddress);
                    var fullThousands = (resources.SeventhSenseHighTwoDigits >> 4) & 0x0F;
                    RepeatTile(operations, SeventhSenseFullGaugeTile, fullThousands);

                    scratch39Write = (byte)((resources.SeventhSenseLowTwoDigits >> 4) & 0x0F);
                    var fractionalBand = SeventhSenseFractionByte(
                        resources.SeventhSenseLowTwoDigits,
                        resources.SeventhSenseHighTwoDigits);
                    WriteData(operations, SeventhSensePartialGaugeTile(fractionalBand));
                }
                break;

            default:
                throw new InvalidOperationException();
        }

        return new(phase, phase, scratch39Write, operations);
    }

    public static byte DigitTileFromNibble(byte value) =>
        (byte)(DigitTileBase + (value & 0x0F));

    public static byte[] PackedBcdDigitTiles(byte packedBcd) =>
    [
        DigitTileFromNibble((byte)(packedBcd >> 4)),
        DigitTileFromNibble(packedBcd),
    ];

    /// <summary>
    /// Exact raw-byte thresholds from $9E84-$9EB7. Canonical callers provide
    /// packed BCD 00-99, but the comparison itself is bytewise and is preserved.
    /// </summary>
    public static byte ResourceGaugePartialTile(byte packedBcd)
    {
        if (packedBcd < 0x05) return 0xA7;
        if (packedBcd < 0x25) return 0xB1;
        if (packedBcd < 0x37) return 0xB2;
        if (packedBcd < 0x50) return 0xB3;
        if (packedBcd < 0x62) return 0xB4;
        if (packedBcd < 0x75) return 0xB5;
        if (packedBcd < 0x87) return 0xB6;
        return 0xBF;
    }

    /// <summary>
    /// Phase 3 ignores the ones digit. It packs the hundreds digit from the low
    /// nibble of $05AB with the tens digit from the high nibble of $05AA.
    /// </summary>
    public static byte SeventhSenseFractionByte(
        byte seventhSenseLowTwoDigits,
        byte seventhSenseHighTwoDigits) =>
        (byte)(((seventhSenseHighTwoDigits & 0x0F) << 4) |
               ((seventhSenseLowTwoDigits >> 4) & 0x0F));

    public static byte SeventhSensePartialGaugeTile(byte fractionalPackedBcd)
    {
        var tile = ResourceGaugePartialTile(fractionalPackedBcd);
        if (tile == EmptyGaugeTile)
            return EmptyGaugeTile;

        var shifted = (byte)(tile + 0x07);
        return shifted >= ResourceFullGaugeTile
            ? SeventhSenseFullGaugeTile
            : shifted;
    }

    private static void EmitPhase0Digits(
        List<PlatformHudPpuOperation> operations,
        byte platformSubstate02,
        PlatformHudResourceState resources)
    {
        SetPpuAddress(operations, CosmoDigitsAddress);
        WriteData(operations, DigitTileFromNibble(resources.CosmoHundreds));
        WritePackedBcd(operations, resources.CosmoLowTwoDigits);

        SetPpuAddress(operations, LifeDigitsAddress);
        WriteData(operations, DigitTileFromNibble(resources.LifeHundreds));
        WritePackedBcd(operations, resources.LifeLowTwoDigits);

        // The original sets $236F before testing $02. At substate zero the PPU
        // address changes but no Seventh Sense digit bytes are written.
        SetPpuAddress(operations, SeventhSenseDigitsAddress);
        if (platformSubstate02 == 0)
            return;

        WritePackedBcd(operations, resources.SeventhSenseHighTwoDigits);
        WritePackedBcd(operations, resources.SeventhSenseLowTwoDigits);
    }

    private static void EmitResourceGauge(
        List<PlatformHudPpuOperation> operations,
        ushort ppuAddress,
        int capSegments,
        int fullSegments,
        byte partialPackedBcd)
    {
        // First pass clears the complete cap width with $A7.
        SetPpuAddress(operations, ppuAddress);
        RepeatTile(operations, EmptyGaugeTile, capSegments);

        // Second pass overwrites full hundreds with $BF and then writes one
        // partial segment selected by the packed two-digit remainder.
        SetPpuAddress(operations, ppuAddress);
        RepeatTile(operations, ResourceFullGaugeTile, fullSegments);
        WriteData(operations, ResourceGaugePartialTile(partialPackedBcd));
    }

    private static void WritePackedBcd(
        List<PlatformHudPpuOperation> operations,
        byte packedBcd)
    {
        var tiles = PackedBcdDigitTiles(packedBcd);
        WriteData(operations, tiles[0]);
        WriteData(operations, tiles[1]);
    }

    private static void RepeatTile(
        List<PlatformHudPpuOperation> operations,
        byte tile,
        int count)
    {
        for (var i = 0; i < count; i++)
            WriteData(operations, tile);
    }

    private static void SetPpuAddress(
        List<PlatformHudPpuOperation> operations,
        ushort address)
    {
        operations.Add(new(
            PlatformHudPpuOperationKind.WritePpuAddress,
            0x2006,
            (byte)(address >> 8)));
        operations.Add(new(
            PlatformHudPpuOperationKind.WritePpuAddress,
            0x2006,
            (byte)address));
    }

    private static void WriteData(
        List<PlatformHudPpuOperation> operations,
        byte value) =>
        operations.Add(new(PlatformHudPpuOperationKind.WritePpuData, 0x2007, value));
}

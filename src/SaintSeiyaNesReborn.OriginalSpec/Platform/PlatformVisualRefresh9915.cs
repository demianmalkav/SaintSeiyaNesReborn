namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformVisualRefresh9915Route
{
    SpecialPaletteChrRefresh,
    GeneralVisualRefresh996C,
}

public enum PlatformBackgroundPalette0DAction
{
    None,
    WriteSourceA022,
    WriteSourceA02B,
}

public readonly record struct PlatformPalettePointerState(
    ushort Slot0392,
    ushort Slot0394,
    ushort Slot0396,
    ushort Slot0398)
{
    public ushort[] ToArray() => [Slot0392, Slot0394, Slot0396, Slot0398];
}

public readonly record struct PlatformVisualRefresh9915State(
    byte Substate02,
    byte SpecialVisualY07C0,
    byte RefreshLatch03A4,
    PlatformPalettePointerState PalettePointers);

public readonly record struct PlatformVisualRefreshArmResult(
    byte RefreshLatch03A4,
    bool ArmedNow);

public readonly record struct PlatformVisualRefresh9915Result(
    PlatformVisualRefresh9915Route Route,
    PlatformVisualRefresh9915State State,
    ushort? ForcedFourthPaletteDescriptor,
    byte? DynamicChr0Bank,
    PlatformPaletteTransferPlan? PaletteTransfer);

public sealed record PlatformPaletteTransferPlan(
    ushort PpuStartAddress,
    IReadOnlyList<ushort> DescriptorPointers,
    int DescriptorBytes,
    byte PrefixColor,
    IReadOnlyList<byte> PpuAddressResetSequence);

public readonly record struct PlatformPrimaryPaletteSelector(
    byte EntityType,
    byte VariantIndex,
    ushort PointerListEntryAddress,
    byte PointerListOffset,
    ushort SecondaryDescriptorEntryAddress);

public readonly record struct PlatformBackgroundPalette0DResult(
    PlatformBackgroundPalette0DAction Action,
    byte PaletteLatch03A7,
    ushort? SourceAddress);

/// <summary>
/// Clean-room semantic model of the bank-1 platform visual-refresh owner rooted at
/// $9915. It separates the one-shot special palette/CHR branch from the general
/// $996C fallback while exposing the exact palette descriptor and CHR selector
/// contracts without embedding original palette payloads.
/// </summary>
public static class PlatformVisualRefresh9915
{
    public const ushort SpritePaletteStart = 0x3F10;
    public const int SubpaletteCount = 4;
    public const int DescriptorBytes = 3;
    public const int SpritePaletteBytes = 16;
    public const byte PalettePrefixColor = 0x0F;

    public const ushort PlayerPrimaryPaletteOuterTable9F46 = 0x9F46;
    public const ushort PrimarySecondaryPaletteTable9F66 = 0x9F66;
    public const ushort SecondaryPaletteTable9C7F = 0x9C7F;
    public const ushort PendingPaletteTable9CC6 = 0x9CC6;

    public const ushort SpecialDescriptor9960 = 0x9960;
    public const ushort SpecialDescriptor9963 = 0x9963;
    public const ushort BackgroundPaletteSourceA022 = 0xA022;
    public const ushort BackgroundPaletteSourceA02B = 0xA02B;

    private static readonly byte[] SpecialTriggerLowBySubstate =
    [
        0xA0,
        0xE0,
        0xD0,
        0xD0,
        0xD0,
        0xA5,
    ];

    private static readonly byte[] DynamicChr0BySubstate =
    [
        0x1D,
        0x1D,
        0x1B,
        0x00,
        0x19,
        0x00,
    ];

    /// <summary>
    /// Models the sole canonical arming owner at $9130-$9169.
    /// The latch is reset to zero by platform initialization at $98F1.
    /// </summary>
    public static PlatformVisualRefreshArmResult ArmSpecialRefresh(
        byte substate02,
        PlatformSaintIndex saint03,
        byte cameraLow44,
        byte cameraPage45,
        byte refreshLatch03A4)
    {
        if (substate02 is < 0x0C or > 0x11)
            return new(refreshLatch03A4, ArmedNow: false);

        if (substate02 == 0x10 && saint03 == PlatformSaintIndex.Shun)
            return new(refreshLatch03A4, ArmedNow: false);

        var index = substate02 - 0x0C;
        if (cameraPage45 != 0x0A || cameraLow44 < SpecialTriggerLowBySubstate[index])
            return new(refreshLatch03A4, ArmedNow: false);

        if (refreshLatch03A4 != 0)
            return new(refreshLatch03A4, ArmedNow: false);

        return new(0xFF, ArmedNow: true);
    }

    /// <summary>
    /// Models the top branch at $9915-$995F. A failed $07C0/$03A4 gate does
    /// not return; the original transfers to the general refresh path at $996C.
    /// </summary>
    public static PlatformVisualRefresh9915Result StepTopBranch(PlatformVisualRefresh9915State state)
    {
        if (state.SpecialVisualY07C0 == 0xFE || state.RefreshLatch03A4 != 0xFF)
        {
            return new(
                PlatformVisualRefresh9915Route.GeneralVisualRefresh996C,
                state,
                ForcedFourthPaletteDescriptor: null,
                DynamicChr0Bank: null,
                PaletteTransfer: null);
        }

        if (state.Substate02 is < 0x0C or > 0x11)
            throw new InvalidOperationException(
                "Canonical $03A4=$FF reachability is limited to platform substates $0C-$11.");

        var fourth = state.Substate02 == 0x10
            ? SpecialDescriptor9963
            : SpecialDescriptor9960;

        var pointers = state.PalettePointers with { Slot0398 = fourth };
        var updated = state with
        {
            RefreshLatch03A4 = 0xFE,
            PalettePointers = pointers,
        };

        var transfer = new PlatformPaletteTransferPlan(
            SpritePaletteStart,
            pointers.ToArray(),
            DescriptorBytes,
            PalettePrefixColor,
            PpuAddressResetSequence());

        return new(
            PlatformVisualRefresh9915Route.SpecialPaletteChrRefresh,
            updated,
            fourth,
            DynamicChr0BankFor(state.Substate02),
            transfer);
    }

    public static byte DynamicChr0BankFor(byte substate02)
    {
        if (substate02 is < 0x0C or > 0x11)
            throw new ArgumentOutOfRangeException(
                nameof(substate02),
                substate02,
                "Dynamic $9915 CHR0 selection exists only for substates $0C-$11.");

        return DynamicChr0BySubstate[substate02 - 0x0C];
    }

    public static byte[] ComposeSpritePalette(
        ReadOnlySpan<byte> descriptor0,
        ReadOnlySpan<byte> descriptor1,
        ReadOnlySpan<byte> descriptor2,
        ReadOnlySpan<byte> descriptor3)
    {
        var sources = new[] {
            descriptor0.ToArray(),
            descriptor1.ToArray(),
            descriptor2.ToArray(),
            descriptor3.ToArray(),
        };

        if (sources.Any(source => source.Length != DescriptorBytes))
            throw new ArgumentException("Every platform palette descriptor must contain exactly three bytes.");

        var result = new byte[SpritePaletteBytes];
        for (var i = 0; i < SubpaletteCount; i++)
        {
            var dest = i * 4;
            result[dest] = PalettePrefixColor;
            sources[i].CopyTo(result, dest + 1);
        }

        return result;
    }

    public static byte[] PpuAddressResetSequence() => [0x3F, 0x00, 0x00, 0x00];

    public static PlatformPrimaryPaletteSelector PrimaryPaletteSelector(byte profile03B7)
    {
        var entityType = (byte)(profile03B7 & 0x0F);
        if (entityType is < 0x05 or > 0x0F)
            throw new ArgumentOutOfRangeException(
                nameof(profile03B7),
                profile03B7,
                "General primary palette profiles use type nibble $05-$0F.");

        var variant = (byte)((profile03B7 & 0x30) >> 4);
        return new(
            entityType,
            variant,
            checked((ushort)(PlayerPrimaryPaletteOuterTable9F46 + entityType * 2)),
            checked((byte)(variant * 2)),
            checked((ushort)(PrimarySecondaryPaletteTable9F66 + entityType * 2)));
    }

    public static ushort SecondaryPalettePointerEntry(byte profile03B5)
    {
        if (profile03B5 is < 0x01 or > 0x04)
            throw new ArgumentOutOfRangeException(nameof(profile03B5));
        return checked((ushort)(SecondaryPaletteTable9C7F + profile03B5 * 2));
    }

    public static ushort PendingPalettePointerEntry(byte pending03A9)
    {
        if (pending03A9 is < 0x01 or > 0x05)
            throw new ArgumentOutOfRangeException(nameof(pending03A9));
        return checked((ushort)(PendingPaletteTable9CC6 + (pending03A9 - 1) * 2));
    }

    public static PlatformBackgroundPalette0DResult BackgroundPalette0D(
        byte substate02,
        byte cameraPage45,
        byte frame3C,
        byte paletteLatch03A7)
    {
        if (substate02 != 0x0D || cameraPage45 is < 0x02 or >= 0x05)
            return new(PlatformBackgroundPalette0DAction.None, paletteLatch03A7, SourceAddress: null);

        if ((frame3C & 0x08) != 0)
        {
            if (paletteLatch03A7 == 0)
                return new(PlatformBackgroundPalette0DAction.None, paletteLatch03A7, SourceAddress: null);

            return new(
                PlatformBackgroundPalette0DAction.WriteSourceA022,
                PaletteLatch03A7: 0x00,
                SourceAddress: BackgroundPaletteSourceA022);
        }

        if (paletteLatch03A7 != 0)
            return new(PlatformBackgroundPalette0DAction.None, paletteLatch03A7, SourceAddress: null);

        return new(
            PlatformBackgroundPalette0DAction.WriteSourceA02B,
            PaletteLatch03A7: 0xFF,
            SourceAddress: BackgroundPaletteSourceA02B);
    }

    public static byte[] ComposeBackgroundPalette0D(ReadOnlySpan<byte> source9)
    {
        if (source9.Length != 9)
            throw new ArgumentException("Substate $0D background palette source must contain exactly nine bytes.");

        var result = new byte[16];
        for (var i = 0; i < 3; i++)
        {
            var dest = i * 4;
            result[dest] = 0x0F;
            result[dest + 1] = source9[i * 3];
            result[dest + 2] = source9[i * 3 + 1];
            result[dest + 3] = source9[i * 3 + 2];
        }

        result[12] = 0x0F;
        result[13] = 0x10;
        result[14] = 0x11;
        result[15] = 0x16;
        return result;
    }
}

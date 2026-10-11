using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class PlatformVisualRefresh9915Checks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckArmingDomainAndOneShotLatch();
        CheckTopGateFallsThroughInsteadOfReturning();
        CheckSpecialPaletteAndChrSelection();
        CheckSpritePaletteDescriptorFormat();
        CheckGeneralProfileSelectorFormulas();
        CheckSubstate0DBackgroundPaletteAnimation();
    }

    private static void CheckArmingDomainAndOneShotLatch()
    {
        var tooEarly = PlatformVisualRefresh9915.ArmSpecialRefresh(
            0x0C, PlatformSaintIndex.Seiya, 0x9F, 0x0A, 0x00);
        Equal((byte)0x00, tooEarly.RefreshLatch03A4, "special refresh before low-scroll threshold");
        True(!tooEarly.ArmedNow, "special refresh not armed before threshold");

        var armed0C = PlatformVisualRefresh9915.ArmSpecialRefresh(
            0x0C, PlatformSaintIndex.Seiya, 0xA0, 0x0A, 0x00);
        Equal((byte)0xFF, armed0C.RefreshLatch03A4, "substate0C arms at exact threshold");
        True(armed0C.ArmedNow, "substate0C armed");

        var wrongPage = PlatformVisualRefresh9915.ArmSpecialRefresh(
            0x0D, PlatformSaintIndex.Seiya, 0xFF, 0x09, 0x00);
        True(!wrongPage.ArmedNow, "special refresh requires camera page0A");

        var shun10 = PlatformVisualRefresh9915.ArmSpecialRefresh(
            0x10, PlatformSaintIndex.Shun, 0xFF, 0x0A, 0x00);
        True(!shun10.ArmedNow, "substate10 excludes internal Shun");

        var seiya10 = PlatformVisualRefresh9915.ArmSpecialRefresh(
            0x10, PlatformSaintIndex.Seiya, 0xD0, 0x0A, 0x00);
        True(seiya10.ArmedNow, "substate10 another Saint can arm");

        var consumed = PlatformVisualRefresh9915.ArmSpecialRefresh(
            0x0C, PlatformSaintIndex.Seiya, 0xFF, 0x0A, 0xFE);
        Equal((byte)0xFE, consumed.RefreshLatch03A4, "consumed latch stays FE");
        True(!consumed.ArmedNow, "consumed latch cannot rearm");
    }

    private static void CheckTopGateFallsThroughInsteadOfReturning()
    {
        var pointers = new PlatformPalettePointerState(0x1111, 0x2222, 0x3333, 0x4444);

        var hiddenVisual = PlatformVisualRefresh9915.StepTopBranch(
            new(0x0C, 0xFE, 0xFF, pointers));
        Equal(PlatformVisualRefresh9915Route.GeneralVisualRefresh996C, hiddenVisual.Route,
            "$07C0 FE falls through to 996C");
        Equal((byte)0xFF, hiddenVisual.State.RefreshLatch03A4,
            "hidden visual does not consume pending special latch");

        var noPendingLatch = PlatformVisualRefresh9915.StepTopBranch(
            new(0x0C, 0x20, 0xFE, pointers));
        Equal(PlatformVisualRefresh9915Route.GeneralVisualRefresh996C, noPendingLatch.Route,
            "non-FF latch falls through to 996C");
        True(noPendingLatch.PaletteTransfer is null, "fallback top branch has no special palette plan");
    }

    private static void CheckSpecialPaletteAndChrSelection()
    {
        var pointers = new PlatformPalettePointerState(0x9F86, 0x9FDD, 0x9C89, 0xA013);

        var ordinarySpecial = PlatformVisualRefresh9915.StepTopBranch(
            new(0x0C, 0x20, 0xFF, pointers));
        Equal(PlatformVisualRefresh9915Route.SpecialPaletteChrRefresh, ordinarySpecial.Route,
            "substate0C special route");
        Equal((ushort)0x9960, ordinarySpecial.ForcedFourthPaletteDescriptor!.Value,
            "ordinary special fourth palette pointer");
        Equal((ushort)0x9960, ordinarySpecial.State.PalettePointers.Slot0398,
            "0398 pointer overwritten");
        Equal((byte)0xFE, ordinarySpecial.State.RefreshLatch03A4, "special latch FF to FE");
        Equal((byte)0x1D, ordinarySpecial.DynamicChr0Bank!.Value, "substate0C dynamic CHR0");
        Equal(PlatformVisualRefresh9915.SpritePaletteStart, ordinarySpecial.PaletteTransfer!.PpuStartAddress,
            "sprite palette starts at 3F10");
        Equal(4, ordinarySpecial.PaletteTransfer.DescriptorPointers.Count,
            "four palette descriptors transferred");
        Equal((ushort)0x9960, ordinarySpecial.PaletteTransfer.DescriptorPointers[3],
            "forced descriptor is fourth transfer");

        var substate10 = PlatformVisualRefresh9915.StepTopBranch(
            new(0x10, 0x70, 0xFF, pointers));
        Equal((ushort)0x9963, substate10.ForcedFourthPaletteDescriptor!.Value,
            "substate10 uses alternate descriptor");
        Equal((byte)0x19, substate10.DynamicChr0Bank!.Value, "substate10 dynamic CHR0");

        Equal((byte)0x1D, PlatformVisualRefresh9915.DynamicChr0BankFor(0x0D), "substate0D CHR0");
        Equal((byte)0x1B, PlatformVisualRefresh9915.DynamicChr0BankFor(0x0E), "substate0E CHR0");
        Equal((byte)0x00, PlatformVisualRefresh9915.DynamicChr0BankFor(0x0F),
            "substate0F intentional CHR0 zero override");
        Equal((byte)0x00, PlatformVisualRefresh9915.DynamicChr0BankFor(0x11),
            "substate11 intentional CHR0 zero override");
    }

    private static void CheckSpritePaletteDescriptorFormat()
    {
        var result = PlatformVisualRefresh9915.ComposeSpritePalette(
            new byte[] { 0x01, 0x02, 0x03 },
            new byte[] { 0x04, 0x05, 0x06 },
            new byte[] { 0x07, 0x08, 0x09 },
            new byte[] { 0x0A, 0x0B, 0x0C });

        Equal(16, result.Length, "sprite palette transfer length");
        Equal((byte)0x0F, result[0], "subpalette0 prefix");
        Equal((byte)0x01, result[1], "subpalette0 first source byte");
        Equal((byte)0x0F, result[4], "subpalette1 prefix");
        Equal((byte)0x0F, result[8], "subpalette2 prefix");
        Equal((byte)0x0F, result[12], "subpalette3 prefix");
        Equal((byte)0x0C, result[15], "subpalette3 final source byte");

        var reset = PlatformVisualRefresh9915.PpuAddressResetSequence();
        Equal(4, reset.Length, "9D58 PPUADDR write count");
        Equal((byte)0x3F, reset[0], "9D58 first high address");
        Equal((byte)0x00, reset[3], "9D58 ends at PPU address zero");
    }

    private static void CheckGeneralProfileSelectorFormulas()
    {
        var primary = PlatformVisualRefresh9915.PrimaryPaletteSelector(0x25);
        Equal((byte)0x05, primary.EntityType, "primary profile low nibble type");
        Equal((byte)0x02, primary.VariantIndex, "primary profile bits4-5 variant");
        Equal((ushort)0x9F50, primary.PointerListEntryAddress, "9F46 type5 outer entry");
        Equal((byte)0x04, primary.PointerListOffset, "variant2 pointer-list offset");
        Equal((ushort)0x9F70, primary.SecondaryDescriptorEntryAddress, "9F66 type5 secondary entry");

        Equal((ushort)0x9C81,
            PlatformVisualRefresh9915.SecondaryPalettePointerEntry(0x01),
            "secondary profile1 pointer entry");
        Equal((ushort)0x9C87,
            PlatformVisualRefresh9915.SecondaryPalettePointerEntry(0x04),
            "secondary profile4 pointer entry");
        Equal((ushort)0x9CC6,
            PlatformVisualRefresh9915.PendingPalettePointerEntry(0x01),
            "pending palette1 entry");
        Equal((ushort)0x9CCE,
            PlatformVisualRefresh9915.PendingPalettePointerEntry(0x05),
            "pending palette5 entry");
    }

    private static void CheckSubstate0DBackgroundPaletteAnimation()
    {
        var lowHalf = PlatformVisualRefresh9915.BackgroundPalette0D(
            0x0D, 0x02, frame3C: 0x00, paletteLatch03A7: 0x00);
        Equal(PlatformBackgroundPalette0DAction.WriteSourceA02B, lowHalf.Action,
            "0D background low frame half");
        Equal((byte)0xFF, lowHalf.PaletteLatch03A7, "0D low half arms palette latch");
        Equal((ushort)0xA02B, lowHalf.SourceAddress!.Value, "0D low half source");

        var lowRepeat = PlatformVisualRefresh9915.BackgroundPalette0D(
            0x0D, 0x02, frame3C: 0x00, paletteLatch03A7: 0xFF);
        Equal(PlatformBackgroundPalette0DAction.None, lowRepeat.Action,
            "0D low half does not rewrite same palette");

        var highHalf = PlatformVisualRefresh9915.BackgroundPalette0D(
            0x0D, 0x04, frame3C: 0x08, paletteLatch03A7: 0xFF);
        Equal(PlatformBackgroundPalette0DAction.WriteSourceA022, highHalf.Action,
            "0D background high frame half");
        Equal((byte)0x00, highHalf.PaletteLatch03A7, "0D high half clears palette latch");
        Equal((ushort)0xA022, highHalf.SourceAddress!.Value, "0D high half source");

        var outside = PlatformVisualRefresh9915.BackgroundPalette0D(
            0x0D, 0x05, frame3C: 0x00, paletteLatch03A7: 0x00);
        Equal(PlatformBackgroundPalette0DAction.None, outside.Action,
            "0D background animation stops outside pages2-4");

        var composed = PlatformVisualRefresh9915.ComposeBackgroundPalette0D(
            new byte[] { 1,2,3, 4,5,6, 7,8,9 });
        Equal(16, composed.Length, "0D background palette transfer length");
        Equal((byte)0x0F, composed[0], "0D first prefix");
        Equal((byte)0x0F, composed[4], "0D second prefix");
        Equal((byte)0x0F, composed[8], "0D third prefix");
        Equal((byte)0x0F, composed[12], "0D fixed fourth prefix");
        Equal((byte)0x10, composed[13], "0D fixed fourth byte1");
        Equal((byte)0x11, composed[14], "0D fixed fourth byte2");
        Equal((byte)0x16, composed[15], "0D fixed fourth byte3");
    }

    private static void Equal<T>(T expected, T actual, string name) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"{name}: expected {expected}, got {actual}");
    }

    private static void True(bool value, string name)
    {
        if (!value)
            throw new InvalidOperationException($"{name}: expected true");
    }
}

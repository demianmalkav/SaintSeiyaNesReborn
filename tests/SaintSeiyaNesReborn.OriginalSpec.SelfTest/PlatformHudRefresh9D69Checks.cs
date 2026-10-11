using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class PlatformHudRefresh9D69Checks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckPhaseCadenceAndPhase0Digits();
        CheckSubstateZeroSeventhSenseDigitGate();
        CheckCosmoGaugePhase();
        CheckLifeGaugePhase();
        CheckGaugeThresholdBoundaries();
        CheckSeventhSenseGaugePhase();
        CheckSeventhSenseSubstateZeroNoWriteBranch();
    }

    private static readonly PlatformHudResourceState Fixture = new(
        LifeLowTwoDigits: 0x34,
        LifeHundreds: 0x02,
        CosmoLowTwoDigits: 0x56,
        CosmoHundreds: 0x01,
        ResourceCaps: 0x32,
        SeventhSenseLowTwoDigits: 0x34,
        SeventhSenseHighTwoDigits: 0x12);

    private static void CheckPhaseCadenceAndPhase0Digits()
    {
        var result = PlatformHudRefresh9D69.Step(0x03, platformSubstate02: 1, Fixture);
        Equal((byte)0x00, result.Phase73, "phase3 wraps to phase0");
        Equal((byte)0x00, result.ExecutedPhase, "phase0 executed");
        Equal(PlatformHudPpuOperationKind.WritePpuControl, result.Operations[0].Kind,
            "HUD invocation first operation kind");
        Equal((byte)0x00, result.Operations[0].Value, "HUD invocation PPUCTRL zero");

        EqualSequence(
            new ushort[] { 0x22F0, 0x2330, 0x236F },
            AddressTargets(result.Operations),
            "phase0 address targets");

        EqualSequence(
            new byte[]
            {
                0x81, 0x85, 0x86, // Cosmo 156
                0x82, 0x83, 0x84, // Life 234
                0x81, 0x82, 0x83, 0x84, // Seventh Sense 1234
            },
            DataBytes(result.Operations),
            "phase0 digit tile stream");
    }

    private static void CheckSubstateZeroSeventhSenseDigitGate()
    {
        var result = PlatformHudRefresh9D69.Step(0x03, platformSubstate02: 0, Fixture);
        EqualSequence(
            new ushort[] { 0x22F0, 0x2330, 0x236F },
            AddressTargets(result.Operations),
            "phase0 substate0 still addresses 236F");
        Equal(6, DataBytes(result.Operations).Count, "phase0 substate0 writes only Life/Cosmo digits");
    }

    private static void CheckCosmoGaugePhase()
    {
        var resources = Fixture with
        {
            ResourceCaps = 0x42, // Cosmo cap width 2
            CosmoHundreds = 0x01,
            CosmoLowTwoDigits = 0x25,
        };
        var result = PlatformHudRefresh9D69.Step(0x00, 1, resources);
        Equal((byte)0x01, result.Phase73, "phase0 input advances to phase1");
        EqualSequence(
            new ushort[] { 0x22F4, 0x22F4 },
            AddressTargets(result.Operations),
            "Cosmo gauge two-pass target");
        EqualSequence(
            new byte[] { 0xA7, 0xA7, 0xBF, 0xB2 },
            DataBytes(result.Operations),
            "Cosmo cap/full/partial gauge stream");
    }

    private static void CheckLifeGaugePhase()
    {
        var resources = Fixture with
        {
            ResourceCaps = 0x32, // Life cap width 3
            LifeHundreds = 0x02,
            LifeLowTwoDigits = 0x62,
        };
        var result = PlatformHudRefresh9D69.Step(0x01, 1, resources);
        Equal((byte)0x02, result.Phase73, "phase1 input advances to phase2");
        EqualSequence(
            new ushort[] { 0x2334, 0x2334 },
            AddressTargets(result.Operations),
            "Life gauge two-pass target");
        EqualSequence(
            new byte[] { 0xA7, 0xA7, 0xA7, 0xBF, 0xBF, 0xB5 },
            DataBytes(result.Operations),
            "Life cap/full/partial gauge stream");
    }

    private static void CheckGaugeThresholdBoundaries()
    {
        var cases = new (byte Value, byte Tile)[]
        {
            (0x00, 0xA7), (0x04, 0xA7),
            (0x05, 0xB1), (0x24, 0xB1),
            (0x25, 0xB2), (0x36, 0xB2),
            (0x37, 0xB3), (0x49, 0xB3),
            (0x50, 0xB4), (0x61, 0xB4),
            (0x62, 0xB5), (0x74, 0xB5),
            (0x75, 0xB6), (0x86, 0xB6),
            (0x87, 0xBF), (0x99, 0xBF),
        };

        foreach (var (value, tile) in cases)
            Equal(tile, PlatformHudRefresh9D69.ResourceGaugePartialTile(value),
                $"resource gauge threshold {value:X2}");

        EqualSequence(
            new byte[] { 0x84, 0x82 },
            PlatformHudRefresh9D69.PackedBcdDigitTiles(0x42),
            "packed BCD digit tiles");
    }

    private static void CheckSeventhSenseGaugePhase()
    {
        var result = PlatformHudRefresh9D69.Step(0x02, 1, Fixture);
        Equal((byte)0x03, result.Phase73, "phase2 input advances to phase3");
        EqualSequence(
            new ushort[] { 0x2374, 0x2374 },
            AddressTargets(result.Operations),
            "Seventh Sense gauge two-pass target");

        var expected = Enumerable.Repeat((byte)0xA7, 10)
            .Concat(new byte[] { 0xBE, 0xB8 })
            .ToArray();
        EqualSequence(expected, DataBytes(result.Operations), "Seventh Sense gauge stream");
        Equal((byte)0x03, result.Scratch39Write!.Value, "$39 stores Seventh Sense tens digit");
        Equal((byte)0x23,
            PlatformHudRefresh9D69.SeventhSenseFractionByte(0x34, 0x12),
            "Seventh Sense fractional packed byte");

        Equal((byte)0xA7, PlatformHudRefresh9D69.SeventhSensePartialGaugeTile(0x04),
            "Seventh Sense near-empty partial");
        Equal((byte)0xB8, PlatformHudRefresh9D69.SeventhSensePartialGaugeTile(0x05),
            "Seventh Sense first partial band");
        Equal((byte)0xBD, PlatformHudRefresh9D69.SeventhSensePartialGaugeTile(0x75),
            "Seventh Sense upper partial band");
        Equal((byte)0xBE, PlatformHudRefresh9D69.SeventhSensePartialGaugeTile(0x87),
            "Seventh Sense full partial segment clamp");
    }

    private static void CheckSeventhSenseSubstateZeroNoWriteBranch()
    {
        var result = PlatformHudRefresh9D69.Step(0x02, platformSubstate02: 0, Fixture);
        Equal((byte)0x03, result.Phase73, "phase advances even when phase3 is suppressed");
        Equal(1, result.Operations.Count, "suppressed phase3 only writes PPUCTRL zero");
        True(!result.WrotePpuData, "suppressed phase3 writes no HUD data");
        True(result.Scratch39Write is null, "suppressed phase3 does not touch scratch39");
    }

    private static List<ushort> AddressTargets(IReadOnlyList<PlatformHudPpuOperation> operations)
    {
        var result = new List<ushort>();
        for (var i = 0; i + 1 < operations.Count; i++)
        {
            if (operations[i].Kind != PlatformHudPpuOperationKind.WritePpuAddress ||
                operations[i + 1].Kind != PlatformHudPpuOperationKind.WritePpuAddress)
                continue;

            result.Add((ushort)((operations[i].Value << 8) | operations[i + 1].Value));
            i++;
        }
        return result;
    }

    private static List<byte> DataBytes(IReadOnlyList<PlatformHudPpuOperation> operations) =>
        operations
            .Where(op => op.Kind == PlatformHudPpuOperationKind.WritePpuData)
            .Select(op => op.Value)
            .ToList();

    private static void Equal<T>(T expected, T actual, string name) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"{name}: expected {expected}, got {actual}");
    }

    private static void EqualSequence<T>(
        IEnumerable<T> expected,
        IEnumerable<T> actual,
        string name)
    {
        if (!expected.SequenceEqual(actual))
            throw new InvalidOperationException(
                $"{name}: expected [{string.Join(",", expected)}], got [{string.Join(",", actual)}]");
    }

    private static void True(bool value, string name)
    {
        if (!value)
            throw new InvalidOperationException($"{name}: expected true");
    }
}

using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class PersistentResourceSnapshotChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        var seiya = new PlatformSaintResourceRecord(0x11, 0x01, 0x12, 0x02, 0x13);
        var shun = new PlatformSaintResourceRecord(0x21, 0x01, 0x22, 0x02, 0x23);
        var hyoga = new PlatformSaintResourceRecord(0x31, 0x01, 0x32, 0x02, 0x33);
        var shiryu = new PlatformSaintResourceRecord(0x41, 0x01, 0x42, 0x02, 0x43);
        var ikki = new PlatformSaintResourceRecord(0x51, 0x01, 0x52, 0x02, 0x53);

        var live = new PlatformLiveResourceState(
            Seiya: seiya,
            Shun: shun,
            Hyoga: hyoga,
            Shiryu: shiryu,
            Ikki: ikki);

        Require(live.For(PlatformSaintIndex.Shun) == shun, "live platform index 1 is Shun");
        Require(live.For(PlatformSaintIndex.Hyoga) == hyoga, "live platform index 2 is Hyoga");

        var snapshot = PlatformResourceSnapshot.Capture(live);
        Require(snapshot.For(SaintId.Seiya) == seiya, "snapshot canonical slot 0 is Seiya");
        Require(snapshot.For(SaintId.Hyoga) == hyoga, "snapshot canonical slot 1 is Hyoga");
        Require(snapshot.For(SaintId.Shun) == shun, "snapshot canonical slot 2 is Shun");
        Require(snapshot.For(SaintId.Shiryu) == shiryu, "snapshot canonical slot 3 is Shiryu");
        Require(snapshot.For(SaintId.Ikki) == ikki, "snapshot canonical slot 4 is Ikki");

        var expected = new byte[]
        {
            0x11, 0x01, 0x12, 0x02, 0x13, // Seiya -> $058C-$0590
            0x31, 0x01, 0x32, 0x02, 0x33, // Hyoga -> $0591-$0595
            0x21, 0x01, 0x22, 0x02, 0x23, // Shun  -> $0596-$059A
            0x41, 0x01, 0x42, 0x02, 0x43, // Shiryu-> $059B-$059F
            0x51, 0x01, 0x52, 0x02, 0x53, // Ikki  -> $05A0-$05A4
        };
        Require(snapshot.CopyCanonicalBytes().SequenceEqual(expected), "$951F persistent bytes use canonical Seiya/Hyoga/Shun/Shiryu/Ikki order");

        Require(seiya.Life == 111, "resource record decodes BCD Life");
        Require(seiya.Cosmo == 212, "resource record decodes BCD Cosmo");
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Persistent resource snapshot self-test failed: {label}");
    }
}

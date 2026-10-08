using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

static void Equal<T>(T expected, T actual, string name) where T : notnull
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{name}: expected {expected}, got {actual}");
}

static void True(bool value, string name)
{
    if (!value)
        throw new InvalidOperationException($"{name}: expected true");
}

// The ROM's $E505 mapping is an involution that swaps only Shun/Hyoga.
foreach (SaintId saint in Enum.GetValues<SaintId>())
{
    var platform = SaintIndexMap.ToPlatform(saint);
    Equal(saint, SaintIndexMap.ToCanonical(platform), $"Saint map round-trip {saint}");
}
Equal(PlatformSaintIndex.Shun, SaintIndexMap.ToPlatform(SaintId.Shun), "Shun internal index");
Equal(PlatformSaintIndex.Hyoga, SaintIndexMap.ToPlatform(SaintId.Hyoga), "Hyoga internal index");

// Reconstructed platform damage fixtures.
Equal(18, PlatformDamage.FromCosmo(PlatformSaintIndex.Seiya, 99), "Seiya damage @99");
Equal(19, PlatformDamage.FromCosmo(PlatformSaintIndex.Seiya, 100), "Seiya damage @100");
Equal(93, PlatformDamage.FromCosmo(PlatformSaintIndex.Seiya, 499), "Seiya damage @499");
Equal(247, PlatformDamage.FromCosmo(PlatformSaintIndex.Shun, 999), "Shun damage @999");
Equal(148, PlatformDamage.FromCosmo(PlatformSaintIndex.Ikki, 999), "Ikki damage @999");
// Original three-digit quirk: ones digit no longer contributes.
Equal(
    PlatformDamage.FromCosmo(PlatformSaintIndex.Hyoga, 990),
    PlatformDamage.FromCosmo(PlatformSaintIndex.Hyoga, 999),
    "Three-digit Cosmo ones digit is ignored");

// Contact drain: one Life tick costs 2, one Cosmo tick costs 1.
var drain = ContactDrain.Step(
    PlatformSaintIndex.Seiya,
    engineSubstate02: 0,
    frameCounter3C: 1,
    new ContactDrainState(LifeTicks: 3, CosmoTicks: 2));
Equal(2, drain.LifeLoss, "Contact Life loss");
Equal(1, drain.CosmoLoss, "Contact Cosmo loss");
Equal((byte)2, drain.State.LifeTicks, "Remaining Life ticks");
Equal((byte)1, drain.State.CosmoTicks, "Remaining Cosmo ticks");

// Special $02==$10 attrition is four times more frequent for Shun.
True(ContactDrain.IsPeriodicEnvironmentDrainFrame(PlatformSaintIndex.Shun, 8), "Shun periodic frame 8");
True(!ContactDrain.IsPeriodicEnvironmentDrainFrame(PlatformSaintIndex.Seiya, 8), "Seiya not periodic frame 8");
True(ContactDrain.IsPeriodicEnvironmentDrainFrame(PlatformSaintIndex.Seiya, 32), "Seiya periodic frame 32");
var periodic = ContactDrain.Step(
    PlatformSaintIndex.Shun,
    engineSubstate02: 0x10,
    frameCounter3C: 8,
    new ContactDrainState(LifeTicks: 3, CosmoTicks: 0));
Equal(2, periodic.LifeLoss, "Periodic Life loss");
Equal((byte)3, periodic.State.LifeTicks, "Periodic branch does not consume contact Life tick");
True(periodic.PeriodicEnvironmentLifeLoss, "Periodic branch flag");

// Collision sample geometry from bank 0 $B3E2+.
var probes = CollisionProbeLayout.FromPlayer(playerX: 0x40, playerY: 0x52, scrollX: 0x100);
Equal(new ProbePoint(0x148, 0x72), probes.FloorCenter, "Floor center probe");
Equal(new ProbePoint(0x150, 0x60), probes.LowerRight, "Lower right probe");
Equal(new ProbePoint(0x150, 0x50), probes.UpperRight, "Upper right probe");
Equal(new ProbePoint(0x158, 0x72), probes.FloorRight, "Floor right probe");
Equal(new ProbePoint(0x140, 0x60), probes.LowerLeft, "Lower left probe");
Equal(new ProbePoint(0x140, 0x50), probes.UpperLeft, "Upper left probe");
Equal(new ProbePoint(0x138, 0x72), probes.FloorLeft, "Floor left probe");
Equal(new ProbePoint(0x148, 0x50), probes.UpperCenter, "Upper center probe");

// Special y==$88 lower-side sampler uses +$18 instead of +$10 after alignment.
var specialYProbes = CollisionProbeLayout.FromPlayer(playerX: 0, playerY: 0x88, scrollX: 0);
Equal(0xA8, specialYProbes.LowerRight.Y, "Special lower-side Y offset at $88");

// Collision descriptor behavior observed by direction/probe.
True(TileDescriptorRules.SupportsFloor(0x80), "$80 supports floor");
True(!TileDescriptorRules.SupportsFloor(0x7F), "$7F lacks ordinary floor support");
True(TileDescriptorRules.BlocksGroundedRightAtLowerProbe(0x80), "$80 right-side blocker");
True(!TileDescriptorRules.BlocksGroundedLeftAtLowerProbe(0x80), "$80 not left-side blocker");
True(TileDescriptorRules.BlocksGroundedLeftAtLowerProbe(0x88), "$88 left-side blocker");
True(TileDescriptorRules.StopsUpwardMotionAtUpperCenter(0xE0), "$E0 ceiling blocker");
True(!TileDescriptorRules.StopsUpwardMotionAtUpperCenter(0xF0), "$F0 special non-ceiling branch");
True(TileDescriptorRules.IsSpecialF8F9Floor(0xF8), "$F8 special floor");

// State-family extraction keeps low directional bits separate.
Equal((byte)0x30, PlatformActionState.Family(0x33), "Jump family mask");
Equal(3, PlatformActionState.JumpDirectionalBits(0x33), "Jump low directional bits");

Console.WriteLine("OriginalSpec self-test: all reconstructed invariants passed.");

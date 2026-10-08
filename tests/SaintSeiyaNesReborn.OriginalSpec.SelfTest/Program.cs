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

// Packed-decimal primitives used throughout original stats/password state.
Equal(99, PackedBcd.DecodeByte(0x99), "BCD decode 99");
Equal((byte)0x42, PackedBcd.EncodeByte(42), "BCD encode 42");
Equal(9999, PackedBcd.DecodeFourDigits(0x99, 0x99), "BCD four-digit decode");
Equal(((byte)0x34, (byte)0x12), PackedBcd.EncodeFourDigits(1234), "BCD four-digit encode");

// Reconstructed platform damage fixtures.
Equal(18, PlatformDamage.FromCosmo(PlatformSaintIndex.Seiya, 99), "Seiya damage @99");
Equal(19, PlatformDamage.FromCosmo(PlatformSaintIndex.Seiya, 100), "Seiya damage @100");
Equal(93, PlatformDamage.FromCosmo(PlatformSaintIndex.Seiya, 499), "Seiya damage @499");
Equal(247, PlatformDamage.FromCosmo(PlatformSaintIndex.Shun, 999), "Shun damage @999");
Equal(148, PlatformDamage.FromCosmo(PlatformSaintIndex.Ikki, 999), "Ikki damage @999");
Equal(
    PlatformDamage.FromCosmo(PlatformSaintIndex.Hyoga, 990),
    PlatformDamage.FromCosmo(PlatformSaintIndex.Hyoga, 999),
    "Three-digit Cosmo ones digit is ignored");

// Enemy-death Seventh Sense reward: one packed-BCD byte, $02==0 gate, clamp 9999.
Equal(1276, SeventhSense.AddPlatformReward(1234, 0x42, engineSubstate02: 1), "Seventh Sense reward");
Equal(1234, SeventhSense.AddPlatformReward(1234, 0x42, engineSubstate02: 0), "Seventh Sense mode gate");
Equal(9999, SeventhSense.AddPlatformReward(9990, 0x42, engineSubstate02: 1), "Seventh Sense clamp");

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

// Stage model: synthetic descriptors exercise page lookup, collision sampling and snap rules.
var page0Descriptors = new byte[PlatformStagePage.DescriptorCount];
var page1Descriptors = new byte[PlatformStagePage.DescriptorCount];
page0Descriptors[7 * PlatformStagePage.Columns + 4] = 0x90;
page0Descriptors[6 * PlatformStagePage.Columns + 5] = 0x80;
page1Descriptors[0] = 0xE0;

var stage = new PlatformStageMap(
    substate: 0,
    pages:
    [
        new PlatformStagePage(0, page0Descriptors),
        new PlatformStagePage(1, page1Descriptors),
    ]);
Equal(512, stage.WidthPixels, "Two-page stage width");
Equal((byte)0x90, stage.DescriptorAt(0x48, 0x72)!.Value, "Stage descriptor lookup");
Equal((byte)0xE0, stage.DescriptorAt(0x100, 0)!.Value, "Second-page descriptor lookup");
True(stage.DescriptorAt(-1, 0) is null, "Negative stage coordinate is outside");
True(stage.DescriptorAt(0x200, 0) is null, "Past-stage coordinate is outside");

var sampledStage = stage.SamplePlayer(playerX: 0x40, playerY: 0x52, scrollX: 0);
Equal((byte)0x90, sampledStage.FloorCenter!.Value, "Stage floor-center sample");
Equal((byte)0x80, sampledStage.LowerRight!.Value, "Stage lower-right sample");
True(!stage.CanMoveRight(sampledStage), "Stage right collision blocks grounded movement");
True(stage.CanMoveLeft(sampledStage), "Stage left side remains open");
Equal(0x50, PlatformStageMap.OrdinaryCenterFloorSnap(0x90, 0x52)!.Value, "Normal floor snap");
Equal(0x58, PlatformStageMap.OrdinaryCenterFloorSnap(0xF0, 0x5A)!.Value, "Half-row floor snap");
True(PlatformStageMap.OrdinaryCenterFloorSnap(0xFF, 0x5A) is null, "$FF uses dynamic floor path");

// Grounded upper-side probes are a special-map rule: only substates $0C-$0E consult them.
var upperRightOnly = new PlatformCollisionDescriptors(
    FloorCenter: null,
    LowerRight: null,
    UpperRight: 0x80,
    FloorRight: null,
    LowerLeft: null,
    UpperLeft: null,
    FloorLeft: null,
    UpperCenter: null);
var upperLeftOnly = upperRightOnly with { UpperRight = null, UpperLeft = 0x88 };
var normalProbeStage = new PlatformStageMap(0x00, [new PlatformStagePage(0, new byte[PlatformStagePage.DescriptorCount])]);
var specialProbeStage = new PlatformStageMap(0x0C, [new PlatformStagePage(0, new byte[PlatformStagePage.DescriptorCount])]);
True(normalProbeStage.CanMoveRight(upperRightOnly), "Normal grounded stage ignores upper-right $80");
True(!specialProbeStage.CanMoveRight(upperRightOnly), "$0C grounded stage checks upper-right $80");
True(normalProbeStage.CanMoveLeft(upperLeftOnly), "Normal grounded stage ignores upper-left $88");
True(!specialProbeStage.CanMoveLeft(upperLeftOnly), "$0C grounded stage checks upper-left $88");
var airborneUpperSolid = upperRightOnly with { UpperRight = 0xE0 };
True(!normalProbeStage.CanMoveRight(airborneUpperSolid, airborne: true), "Airborne upper-side $E0 blocks in normal stages too");
True(PlatformStageMap.UsesGroundedUpperSideProbe(0x0E), "$0E uses grounded upper-side probe");
True(!PlatformStageMap.UsesGroundedUpperSideProbe(0x0F), "$0F no longer uses grounded upper-side probe");

// Platform completion is a coordinate gate plus jump-phase predicate, not raw stage width.
var mainExitGate = PlatformExitGate.ForSubstate(0x00);
Equal(0xD0, mainExitGate.MinimumPlayerX, "Main exit min X");
Equal(0x40, mainExitGate.RequiredPlayerY, "Main exit Y");
Equal(
    PlatformExitTransitionKind.State3DReload,
    PlatformExitGate.Evaluate(0x00, PlatformSaintIndex.Seiya, 0xD0, 0x40, jumpPhase: 0)!.Value,
    "Main stage exit transition");
True(
    PlatformExitGate.Evaluate(0x00, PlatformSaintIndex.Seiya, 0xCF, 0x40, jumpPhase: 0) is null,
    "Main exit rejects X before threshold");
True(
    PlatformExitGate.Evaluate(0x00, PlatformSaintIndex.Seiya, 0xD0, 0x40, jumpPhase: 1) is null,
    "Main exit rejects active jump");

var special0C = PlatformExitGate.ForSubstate(0x0C);
Equal(0x88, special0C.MinimumPlayerX, "$0C exit X");
Equal(0x20, special0C.RequiredPlayerY, "$0C exit Y");
True(
    PlatformExitGate.Evaluate(0x10, PlatformSaintIndex.Shun, 0xB4, 0x70, jumpPhase: 0) is null,
    "$10 exit explicitly rejects Shun");
Equal(
    PlatformExitTransitionKind.State3DReload,
    PlatformExitGate.Evaluate(0x10, PlatformSaintIndex.Seiya, 0xB4, 0x70, jumpPhase: 0)!.Value,
    "$10 exit accepts another Saint");
Equal(
    PlatformExitTransitionKind.State70Special,
    PlatformExitGate.Evaluate(0x11, PlatformSaintIndex.Seiya, 0xD0, 0x50, jumpPhase: 0)!.Value,
    "$11 uses special state-70 exit");

// State-family extraction keeps low directional bits separate.
Equal((byte)0x30, PlatformActionState.Family(0x33), "Jump family mask");
Equal(3, PlatformActionState.JumpDirectionalBits(0x33), "Jump low directional bits");

Console.WriteLine("OriginalSpec self-test: all reconstructed invariants passed.");

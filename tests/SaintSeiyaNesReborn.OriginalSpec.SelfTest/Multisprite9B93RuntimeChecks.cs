using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class Multisprite9B93RuntimeChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckDedicated0DBootstrapReturnsWithoutActiveUpdate();
        CheckCooldownAndSelectorZeroPreserveUnwrittenState();
        CheckSubstate0DPrecedesGenericDeathDrop();
        CheckDeathDropPrecedesFlag08Dispatch();
        CheckThreeFlagRoutes();
        CheckNormalRouteCarriesProjectileMutation();
    }

    private static void CheckDedicated0DBootstrapReturnsWithoutActiveUpdate()
    {
        var logical = Logical(
            action: 0x70,
            x: 0xAA,
            y: 0xBB,
            phase: 0x55,
            field05: 0xCC,
            type: 0x06,
            hp: 0x44,
            cosmo: 0x22,
            life: 0x33,
            reward: 0x09);
        var state = new PlatformMultisprite9B93PersistentState(
            new PlatformMultisprite9B93RuntimeState(
                PlatformMultisprite9B93VisualState.Empty,
                logical,
                Mode81: 3),
            Cooldown03FA: 0,
            Global03A9: 0x77);

        var frame = Step(
            state,
            engineSubstate02: 0x0D,
            stageSelector: 5,
            entropy48: 0x00);

        Require(frame.Route == PlatformMultisprite9B93Route.BootstrapInitialized,
            "empty substate $0D takes bootstrap initialization only");
        Require(frame.State.Runtime.Visual.Part0 == new PlatformMultisprite9B93Part(0x20, 0x8C, 0x02, 0xEF),
            "dedicated A0E4 part0 survives unchanged through return");
        Require(frame.State.Runtime.Visual.Part1.IsEmpty
            && frame.State.Runtime.Visual.Part2.IsEmpty
            && frame.State.Runtime.Visual.Part3.IsEmpty,
            "new $0D bootstrap does not fabricate the ordinary 2x2 block");
        Require(frame.State.Runtime.Logical.Action00 == 0
            && frame.State.Runtime.Logical.Phase03 == 0
            && frame.State.Runtime.Logical.Profile0C == 0x1E
            && frame.State.Runtime.Logical.CosmoDrain0D == 0x05
            && frame.State.Runtime.Logical.LifeDrain0E == 0x05
            && frame.State.Runtime.Logical.SeventhSenseReward0F == 0x01,
            "A0E4 action/phase/profile writes are merged into persistent logical state");
        Require(frame.State.Runtime.Logical.X01 == 0xAA
            && frame.State.Runtime.Logical.Y02 == 0xBB
            && frame.State.Runtime.Logical.Field05 == 0xCC
            && frame.State.Runtime.Logical.Type09 == 0x06,
            "bootstrap preserves logical fields the ROM does not write");
        Require(frame.State.Runtime.Mode81 == 0
            && frame.State.Cooldown03FA == 0x80
            && frame.State.Global03A9 == 0x77,
            "$0D timed mode/cooldown persist while unwritten $03A9 is preserved");

        // If the active A12A route had incorrectly run in the same call, part0
        // X would already have moved away from $EF.
        Require(frame.State.Runtime.Visual.Part0.X == 0xEF,
            "new bootstrap returns at $9CAB without same-frame active update");
    }

    private static void CheckCooldownAndSelectorZeroPreserveUnwrittenState()
    {
        var logical = Logical(action: 0x31, x: 1, y: 2, phase: 3, field05: 4, type: 5,
            hp: 6, cosmo: 7, life: 8, reward: 9);
        var baseState = new PlatformMultisprite9B93PersistentState(
            new PlatformMultisprite9B93RuntimeState(
                PlatformMultisprite9B93VisualState.Empty,
                logical,
                Mode81: 3),
            Cooldown03FA: 2,
            Global03A9: 0x66);

        var cooldown = Step(baseState, engineSubstate02: 0x0C, stageSelector: 5);
        Require(cooldown.Route == PlatformMultisprite9B93Route.BootstrapCooldown,
            "nonzero timed cooldown returns before initialization");
        Require(cooldown.State.Cooldown03FA == 1 && cooldown.State.Runtime.Mode81 == 0,
            "timed path persists its pre-cooldown $81=0 write and one decrement");
        Require(cooldown.State.Global03A9 == 0x66 && cooldown.State.Runtime.Logical == logical,
            "cooldown return preserves unwritten $03A9 and logical record");

        var zeroState = baseState with { Cooldown03FA = 0x22 };
        var selectorZero = Step(zeroState, engineSubstate02: 0x0D, stageSelector: 0);
        Require(selectorZero.Route == PlatformMultisprite9B93Route.BootstrapSelectorZero,
            "zero selector routes to empty active scan without initialization");
        Require(selectorZero.State.Equals(zeroState),
            "selector-zero path writes neither mode/global/cooldown nor logical state");
    }

    private static void CheckSubstate0DPrecedesGenericDeathDrop()
    {
        var state = ActiveState(
            flags: 0x0C,
            action: 0xD0,
            phase: 0,
            mode81: 0);

        var frame = Step(state, engineSubstate02: 0x0D, stageSelector: 5);

        Require(frame.Route == PlatformMultisprite9B93Route.Substate0D,
            "$02==$0D diverts to A12A before generic D0/E0 dispatch");
        Require(frame.Substate0D?.UsedDeathPath == true
            && frame.DeathDrop is null,
            "substate $0D uses its dedicated D0 lifecycle rather than A06E");
        Require(frame.State.Runtime.Logical.Action00 == 0xD1,
            "dedicated $0D death path advances one action step");
    }

    private static void CheckDeathDropPrecedesFlag08Dispatch()
    {
        var state = ActiveState(
            flags: 0x0C,
            action: 0xD0,
            phase: 0,
            mode81: 0);

        var frame = Step(state, engineSubstate02: 0x05, stageSelector: 3);

        Require(frame.Route == PlatformMultisprite9B93Route.DeathDrop,
            "non-$0D D0/E0 diverts to A06E before flag08/flag04 routing");
        Require(frame.DeathDrop is not null
            && frame.Flag08Bit04 is null
            && frame.Flag08Clear04 is null,
            "active flag bits cannot steal a D0/E0 record from terminal dispatcher");
    }

    private static void CheckThreeFlagRoutes()
    {
        var normal = Step(
            ActiveState(flags: 0x02, action: 0x10, phase: 0, mode81: 0),
            engineSubstate02: 0x05,
            stageSelector: 3);
        Require(normal.Route == PlatformMultisprite9B93Route.Normal && normal.Normal is not null,
            "flag08 clear selects normal $9D05 route");

        var bit04 = Step(
            ActiveState(flags: 0x0C, action: 0x10, phase: 0, mode81: 0),
            engineSubstate02: 0x05,
            stageSelector: 3);
        Require(bit04.Route == PlatformMultisprite9B93Route.Flag08Bit04 && bit04.Flag08Bit04 is not null,
            "flag08+flag04 selects $9ED1 branch");

        var clear04 = Step(
            ActiveState(flags: 0x08, action: 0x10, phase: 1, mode81: 0),
            engineSubstate02: 0x05,
            stageSelector: 3);
        Require(clear04.Route == PlatformMultisprite9B93Route.Flag08Clear04 && clear04.Flag08Clear04 is not null,
            "flag08 with flag04 clear selects $9F79 branch");
    }

    private static void CheckNormalRouteCarriesProjectileMutation()
    {
        var attack = PlatformAttackState.Empty with
        {
            Slot0 = new PlatformAttackSlot(
                new PlatformAttackObject(
                    Y: 0x50,
                    Type: 0x64,
                    Facing: 0x40,
                    X: 0x4E,
                    Field4: 0,
                    Field5: 0,
                    Field6: 0,
                    AuxiliaryX: 0),
                RangeCounter: 3),
        };
        var state = ActiveState(
            flags: 0x02,
            action: 0x10,
            phase: 0,
            mode81: 0,
            hp: 30,
            type: 0x05);

        var frame = Step(
            state,
            engineSubstate02: 0x05,
            stageSelector: 3,
            attacks: attack,
            saint: PlatformSaintIndex.Hyoga,
            platformDamage: 10);

        Require(frame.Route == PlatformMultisprite9B93Route.Normal,
            "projectile fixture enters normal active branch");
        Require(frame.Normal?.ProjectileHits is not null,
            "normal route requested projectile collision for flag04-clear part0");
        Require(frame.State.Runtime.Logical.Profile0C == 20,
            "projectile HP mutation is persisted back into top-level logical state");
        Require(frame.AttackState.Slot0.Object.Type == 0xFE,
            "Hyoga projectile retirement is carried out of top-level runtime");
        Require(frame.AttackState.Equals(frame.Normal!.Value.AttackState)
            && frame.ContactState.Equals(frame.Normal.Value.ContactState)
            && frame.SeventhSense == frame.Normal.Value.SeventhSense,
            "top-level result exposes the selected branch's shared mutable outputs exactly");
    }

    private static PlatformMultisprite9B93FrameResult Step(
        PlatformMultisprite9B93PersistentState state,
        byte engineSubstate02,
        byte stageSelector,
        byte entropy48 = 0,
        PlatformAttackState? attacks = null,
        PlatformSaintIndex saint = PlatformSaintIndex.Seiya,
        int platformDamage = 4) =>
        PlatformMultisprite9B93Runtime.Step(
            state,
            attacks ?? PlatformAttackState.Empty,
            new PlatformContactPhaseState(0, default),
            saint,
            platformDamage,
            seventhSense: 0,
            frameCounter3C: 1,
            cameraDelta43: 0,
            playerX3F: 0x10,
            playerY40: 0x20,
            frameStartPlayerAction4E: 0,
            engineSubstate02,
            flag74: 1,
            stageDerivedSelector: stageSelector,
            entropy48);

    private static PlatformMultisprite9B93PersistentState ActiveState(
        byte flags,
        byte action,
        byte phase,
        byte mode81,
        byte hp = 30,
        byte type = 0x05)
    {
        var visual = new PlatformMultisprite9B93VisualState(
            new PlatformMultisprite9B93Part(0x50, 0xB4, flags, 0x50),
            new PlatformMultisprite9B93Part(0x50, 0xB5, flags, 0x58),
            new PlatformMultisprite9B93Part(0x58, 0xB6, flags, 0x50),
            new PlatformMultisprite9B93Part(0x58, 0xB7, flags, 0x58));
        var logical = Logical(
            action,
            x: 0x50,
            y: 0x50,
            phase,
            field05: 0,
            type,
            hp,
            cosmo: 2,
            life: 3,
            reward: 0x10);
        return new(
            new PlatformMultisprite9B93RuntimeState(visual, logical, mode81),
            Cooldown03FA: 7,
            Global03A9: 0x55);
    }

    private static PlatformMultisprite9B93LogicalState Logical(
        byte action,
        byte x,
        byte y,
        byte phase,
        byte field05,
        byte type,
        byte hp,
        byte cosmo,
        byte life,
        byte reward) =>
        new(
            Action00: action,
            X01: x,
            Y02: y,
            Phase03: phase,
            Field05: field05,
            Type09: type,
            Profile0C: hp,
            CosmoDrain0D: cosmo,
            LifeDrain0E: life,
            SeventhSenseReward0F: reward);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"9B93 top-level runtime self-test failed: {label}");
    }
}

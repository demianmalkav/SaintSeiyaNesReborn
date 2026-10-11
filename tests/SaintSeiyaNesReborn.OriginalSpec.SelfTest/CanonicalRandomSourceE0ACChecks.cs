using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;

internal static class CanonicalRandomSourceE0ACChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckGateAndRecurrence();
        CheckBankResolutionPriority();
        CheckResetAndIndirectSeedOwners();
        CheckConsumers();
    }

    private static void CheckGateAndRecurrence()
    {
        var table = new byte[0x100];
        table[0xFE] = 0x05;
        table[0xFF] = 0x02;

        var context = new CanonicalRandomNmiContext(
            Gate9C: 1, Gate9D: 0, Gate9E: 0, GateA0: 0,
            IncomingVisiblePrgBank: 4,
            MapperLock063E: 0, MapperLock063F: 0,
            Queue0641: 0, State068F: 0,
            Queue0526: 0, Queue0538: 0, Queue057D: 0);

        var first = CanonicalRandomSourceE0AC.Step(new(0xFD, 0xFE), context, table);
        True(first.Updated, "eligible E0AC update");
        Equal((byte)0x02, first.State.Value065F, "065F wraps on addition");
        Equal((byte)0xFF, first.State.Index0660, "0660 increments");
        Equal((byte)4, first.VisiblePrgBank, "incoming bank retained without queue service");

        var second = CanonicalRandomSourceE0AC.Step(first.State, context, table);
        Equal((byte)0x04, second.State.Value065F, "next source byte added");
        Equal((byte)0x00, second.State.Index0660, "0660 wraps FF to 00");

        var blocked = context with { Gate9D = 1 };
        var skipped = CanonicalRandomSourceE0AC.Step(second.State, blocked, table);
        True(!skipped.Updated, "short E000 service path skips E0AC");
        Equal(second.State, skipped.State, "skipped update preserves state");
    }

    private static void CheckBankResolutionPriority()
    {
        var baseContext = new CanonicalRandomNmiContext(
            1, 0, 0, 0, IncomingVisiblePrgBank: 4,
            MapperLock063E: 0, MapperLock063F: 0,
            Queue0641: 1, State068F: 0x8F,
            Queue0526: 0, Queue0538: 0, Queue057D: 0);

        Equal((byte)0, CanonicalRandomSourceE0AC.ResolveVisiblePrgBank(baseContext),
            "0641 special route selects bank0");
        Equal((byte)6, CanonicalRandomSourceE0AC.ResolveVisiblePrgBank(baseContext with { State068F = 0 }),
            "0641 ordinary route selects bank6");
        Equal((byte)5, CanonicalRandomSourceE0AC.ResolveVisiblePrgBank(baseContext with { Queue0538 = 1 }),
            "later 0538 service overrides earlier bank");
        Equal((byte)6, CanonicalRandomSourceE0AC.ResolveVisiblePrgBank(baseContext with { Queue0538 = 1, Queue057D = 1 }),
            "057D is final temporary-bank owner before E0AC");
        Equal((byte)4, CanonicalRandomSourceE0AC.ResolveVisiblePrgBank(baseContext with { MapperLock063E = 4 }),
            "busy protected mapper writer blocks temporary services");
        Equal((byte)4, CanonicalRandomSourceE0AC.ResolveVisiblePrgBank(baseContext with { MapperLock063F = 4 }),
            "busy raw mapper writer blocks temporary services");
    }

    private static void CheckResetAndIndirectSeedOwners()
    {
        Equal(new CanonicalRandomState(0, 0), CanonicalRandomSourceE0AC.ColdResetC13D(), "cold reset seed");
        Equal(new CanonicalRandomState(1, 1), CanonicalRandomSourceE0AC.FrontEndSeedAD4A(), "AD4A fill seed");
        Equal(new CanonicalRandomState(0, 0), CanonicalRandomSourceE0AC.PlatformPageReset959D(), "959D page reset");
        Equal(new CanonicalRandomState(0, 0), CanonicalRandomSourceE0AC.Bank0ValidationPageResetAF0D(), "AF0D page reset");

        var state = new CanonicalRandomState(0x12, 0x34);
        state = CanonicalRandomSourceE0AC.ApplyBank0GridSeed(state, 0x17, 0x56);
        Equal((byte)0x56, state.Value065F, "0648+17 aliases 065F");
        state = CanonicalRandomSourceE0AC.ApplyBank0GridSeed(state, 0x18, 0x78);
        Equal((byte)0x78, state.Index0660, "0648+18 aliases 0660");
        Equal(state, CanonicalRandomSourceE0AC.ApplyBank0GridSeed(state, 0x16, 0xAA),
            "neighboring grid write does not alias RNG state");
    }

    private static void CheckConsumers()
    {
        var state = new CanonicalRandomState(0xAD, 0x04);
        Equal((byte)1, CanonicalRandomSourceE0AC.Mask01(state), "mask01 range");
        Equal((byte)1, CanonicalRandomSourceE0AC.Mask03(state), "mask03 range");
        Equal((byte)5, CanonicalRandomSourceE0AC.Mask07(state), "mask07 range");
        Equal((byte)13, CanonicalRandomSourceE0AC.Mask0F(state), "mask0F range");
        True(CanonicalRandomSourceE0AC.LowNibbleAtLeast(state, 6), "FAC9/FAD7 threshold helper");
        Equal((byte)3, CanonicalRandomSourceE0AC.StageFiveSelector(state, alternateHalf: true),
            "EC20 alternate stage-five half");
        Equal((byte)1, CanonicalRandomSourceE0AC.EncounterSelector(state, narrowTwoWay: true),
            "F65D narrow encounter selector");
        Equal((byte)1, CanonicalRandomSourceE0AC.EncounterSelector(state, narrowTwoWay: false),
            "F665 four-way encounter selector");
        Equal((byte)0xFF, CanonicalRandomSourceE0AC.CounterParityDirection(state),
            "even 0660 maps to FF direction");
        Equal((byte)0x01, CanonicalRandomSourceE0AC.CounterParityDirection(state with { Index0660 = 5 }),
            "odd 0660 maps to 01 direction");
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

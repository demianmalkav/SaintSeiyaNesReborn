using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class PlatformMetaspriteResourceSelectorChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckSharedTableEntryAddressing();
        CheckFlashFamilies();
        CheckFamily70TypeRules();
        CheckFamilyA0Ranges();
        CheckFamilyD0GroundRule();
        CheckControl04Override();
        CheckControlZeroOrdinaryFamilies();
        CheckDynamicEntityAnimation();
        CheckUnsupportedFamilyIsNotInvented();
        CheckVisualRecordCapacity();
    }

    private static void CheckSharedTableEntryAddressing()
    {
        Equal((ushort)0xB67B,
            PlatformMetaspriteResourceSelector.PointerEntryAddress(0xB671, 0x05),
            "table $B671 type $05 pointer entry");
        Equal((ushort)0xB68F,
            PlatformMetaspriteResourceSelector.PointerEntryAddress(0xB671, 0x0F),
            "table $B671 type $0F pointer entry");
    }

    private static void CheckFlashFamilies()
    {
        foreach (byte action in new byte[] { 0x40, 0x80, 0xE0 })
        {
            var hidden = Resolve(0x05, action, frame: 0x00);
            Equal(PlatformMetaspriteResourceKind.DirectDefinition, hidden.Kind, $"family {action:X2} hidden kind");
            Equal((ushort)0xB647, hidden.Address, $"family {action:X2} hidden definition");

            var visible = Resolve(0x05, action, frame: 0x04);
            Equal(PlatformMetaspriteResourceKind.PointerTable, visible.Kind, $"family {action:X2} visible kind");
            Equal((ushort)0xB7D9, visible.Address, $"family {action:X2} visible table");
        }
    }

    private static void CheckFamily70TypeRules()
    {
        Equal((ushort)0xB711, Resolve(0x0C, 0x70, frame: 0x00).Address,
            "type $0C family70 frame bit4 clear");
        Equal((ushort)0xB761, Resolve(0x0C, 0x70, frame: 0x10).Address,
            "type $0C family70 frame bit4 set");

        foreach (byte type in new byte[] { 0x08, 0x09 })
        {
            Equal((ushort)0xB761, Resolve(type, 0x77).Address, $"type {type:X2} pre-$78 family70");
            Equal((ushort)0xB711, Resolve(type, 0x78).Address, $"type {type:X2} post-$78 family70");
        }

        Equal((ushort)0xB6E9, Resolve(0x05, 0x77).Address, "ordinary pre-$78 family70");
        Equal((ushort)0xB711, Resolve(0x05, 0x78).Address, "ordinary post-$78 family70");
    }

    private static void CheckFamilyA0Ranges()
    {
        Equal((ushort)0xB6E9, Resolve(0x05, 0xA3).Address, "A0 pre-A4");
        Equal((ushort)0xB711, Resolve(0x05, 0xA4, frame: 0x00).Address, "A4-A7 bit4 clear");
        Equal((ushort)0xB761, Resolve(0x05, 0xA4, frame: 0x10).Address, "A4-A7 bit4 set");
        Equal((ushort)0xB739, Resolve(0x05, 0xA8).Address, "A8-AB");
        Equal((ushort)0xB761, Resolve(0x05, 0xAC).Address, "AC+ family A0");
    }

    private static void CheckFamilyD0GroundRule()
    {
        var earlyHidden = Resolve(0x05, 0xD3, frame: 0x00);
        Equal(PlatformMetaspriteResourceKind.DirectDefinition, earlyHidden.Kind, "D0-D3 hidden direct kind");
        Equal((ushort)0xB647, earlyHidden.Address, "D0-D3 hidden direct definition");
        Equal((ushort)0xB7D9, Resolve(0x05, 0xD3, frame: 0x04).Address, "D0-D3 visible table");

        var lateHidden = Resolve(0x05, 0xD4, ground: 0x90, frame: 0x00);
        Equal(PlatformMetaspriteResourceKind.DirectDefinition, lateHidden.Kind, "D4+ hidden direct kind");
        Equal((ushort)0xB647, lateHidden.Address, "D4+ hidden direct definition");

        Equal((ushort)0xB7D9, Resolve(0x05, 0xD4, ground: 0x7F, frame: 0x04).Address,
            "D4+ visible low ground");
        Equal((ushort)0xB801, Resolve(0x05, 0xD4, ground: 0x80, frame: 0x04).Address,
            "D4+ visible supported ground lower boundary");
        Equal((ushort)0xB801, Resolve(0x05, 0xD4, ground: 0xEF, frame: 0x04).Address,
            "D4+ visible supported ground upper boundary");
        Equal((ushort)0xB7D9, Resolve(0x05, 0xD4, ground: 0xF0, frame: 0x04).Address,
            "D4+ visible special ground");
    }

    private static void CheckControl04Override()
    {
        Equal((ushort)0xB711, Resolve(0x05, 0x00, control: 1).Address, "control +4 family00");
        Equal((ushort)0xB761, Resolve(0x05, 0x20, control: 1).Address, "control +4 family20");
        Equal((ushort)0xB7B1, Resolve(0x05, 0x30, control: 1).Address, "control +4 family30");
        Equal((ushort)0xB711, Resolve(0x05, 0x50, control: 1).Address, "control +4 fallback family");
    }

    private static void CheckControlZeroOrdinaryFamilies()
    {
        Equal((ushort)0xB6E9, Resolve(0x05, 0x00).Address, "ordinary idle");
        Equal((ushort)0xB6E9, Resolve(0x0C, 0x00, frame: 0x00).Address, "type0C idle bit4 clear");
        Equal((ushort)0xB739, Resolve(0x0C, 0x00, frame: 0x10).Address, "type0C idle bit4 set");
        Equal((ushort)0xB6E9, Resolve(0x0F, 0x00, frame: 0x00).Address, "type0F idle bit1 clear");
        Equal((ushort)0xB739, Resolve(0x0F, 0x00, frame: 0x02).Address, "type0F idle bit1 set");
        Equal((ushort)0xB739, Resolve(0x05, 0x20).Address, "family20");
        Equal((ushort)0xB789, Resolve(0x05, 0x30).Address, "family30");
        Equal((ushort)0xB789, Resolve(0x05, 0x50).Address, "family50");
    }

    private static void CheckDynamicEntityAnimation()
    {
        // With $03B9==0 the renderer increments action, masks with $13, stores
        // it back to logical +$00 and then uses the low two phase bits.
        var incremented = Resolve(0x05, 0x10, animation: 0);
        Equal((ushort)0xB671, incremented.Address, "dynamic $10->$11 selects phase1");
        Equal((byte)0x11, incremented.UpdatedAction, "dynamic action write");
        True(incremented.ActionWasWritten, "dynamic action write signal");

        var wrap = Resolve(0x05, 0x13, animation: 0);
        Equal((ushort)0xB699, wrap.Address, "dynamic $13 wraps to phase0");
        Equal((byte)0x10, wrap.UpdatedAction, "dynamic masked wrap action");

        // Nonzero $03B9 freezes the action byte but still selects by its low bits.
        var frozen = Resolve(0x05, 0x12, animation: 1);
        Equal((ushort)0xB699, frozen.Address, "dynamic frozen phase2 shares B699");
        Equal((byte)0x12, frozen.UpdatedAction, "dynamic frozen action");
        True(!frozen.ActionWasWritten, "dynamic frozen no action write");

        var phase3 = Resolve(0x0E, 0x03, animation: 1);
        Equal((ushort)0xB6C1, phase3.Address, "type0E idle dynamic phase3");
    }

    private static void CheckUnsupportedFamilyIsNotInvented()
    {
        var unsupported = Resolve(0x05, 0x60);
        Equal(PlatformMetaspriteResourceKind.Unsupported, unsupported.Kind, "family60/control0 unsupported");
        True(!unsupported.IsSupported, "unsupported selection flag");
        Equal((ushort)0, unsupported.Address, "unsupported has no invented resource address");
    }

    private static void CheckVisualRecordCapacity()
    {
        foreach (byte type in Enumerable.Range(0x05, 0x0B).Select(v => (byte)v))
            Equal(type == 0x0D ? 12 : 11,
                PlatformMetaspriteResourceSelector.VisualRecordCapacity(type),
                $"type {type:X2} visual-record capacity");
    }

    private static PlatformMetaspriteResourceSelection Resolve(
        byte type,
        byte action,
        byte control = 0,
        byte ground = 0,
        byte frame = 0,
        byte animation = 1) =>
        PlatformMetaspriteResourceSelector.ResolvePrimary(type, action, control, ground, frame, animation);

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

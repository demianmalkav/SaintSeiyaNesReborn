namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformMultisprite9B93Profile(
    byte Selector,
    byte SpriteBase,
    byte Global03A9,
    byte Profile0C,
    byte Profile0D,
    byte Profile0E,
    byte Profile0F)
{
    public static PlatformMultisprite9B93Profile ForSelector(byte selector) => selector switch
    {
        0 => new(0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00),
        1 => new(1, 0xB4, 0x01, 0x14, 0x00, 0x02, 0x02),
        2 => new(2, 0xB4, 0x04, 0x28, 0x00, 0x06, 0x04),
        3 => new(3, 0xB4, 0x01, 0x14, 0x00, 0x02, 0x02),
        4 => new(4, 0xB4, 0x04, 0x28, 0x00, 0x06, 0x04),
        5 => new(5, 0xF6, 0x01, 0x1E, 0x0A, 0x03, 0x01),
        6 => new(6, 0xDA, 0x03, 0x28, 0x0A, 0x05, 0x04),
        _ => throw new ArgumentOutOfRangeException(nameof(selector), selector, "9B93 selector must be 0..6."),
    };
}

public readonly record struct PlatformMultisprite9B93Part(byte Y, byte Sprite, byte Flags, byte X)
{
    public static PlatformMultisprite9B93Part Empty => new(0xF0, 0xFE, 0, 0);
    public bool IsEmpty => Sprite == 0xFE;
}

public readonly record struct PlatformMultisprite9B93VisualState(
    PlatformMultisprite9B93Part Part0,
    PlatformMultisprite9B93Part Part1,
    PlatformMultisprite9B93Part Part2,
    PlatformMultisprite9B93Part Part3)
{
    public static PlatformMultisprite9B93VisualState Empty =>
        new(
            PlatformMultisprite9B93Part.Empty,
            PlatformMultisprite9B93Part.Empty,
            PlatformMultisprite9B93Part.Empty,
            PlatformMultisprite9B93Part.Empty);

    public bool AllEmpty => Part0.IsEmpty && Part1.IsEmpty && Part2.IsEmpty && Part3.IsEmpty;
}

public readonly record struct PlatformMultisprite9B93LogicalBootstrap(
    byte Phase03,
    byte Profile0C,
    byte Profile0D,
    byte Profile0E,
    byte Profile0F,
    bool Action00WasCleared);

public enum PlatformMultisprite9B93BootstrapOutcome
{
    ExistingActive,
    SelectorZero,
    CooldownDecremented,
    Initialized,
}

public readonly record struct PlatformMultisprite9B93BootstrapResult(
    PlatformMultisprite9B93BootstrapOutcome Outcome,
    PlatformMultisprite9B93VisualState Visual,
    PlatformMultisprite9B93LogicalBootstrap Logical,
    byte SelectedProfile,
    byte Global03A9,
    byte Mode81,
    byte Cooldown03FA);

/// <summary>
/// Bootstrap portion of bank-3 `$9B93-$9CAB` for the independent multisprite
/// class rooted at visual `$07E0` and logical `$03FB`.
///
/// This intentionally stops before the `$9CAC+` active updater. The caller may
/// supply the stage-derived selector used on the table-driven path; for the
/// `$02<0C && $74==0` path the ROM overrides it with selector 3 or 4.
/// </summary>
public static class PlatformMultisprite9B93Bootstrap
{
    public static PlatformMultisprite9B93BootstrapResult Step(
        PlatformMultisprite9B93VisualState visual,
        byte engineSubstate02,
        byte flag74,
        byte stageDerivedSelector,
        byte cooldown03FA,
        byte playerX3F)
    {
        if (!visual.AllEmpty)
        {
            return new(
                PlatformMultisprite9B93BootstrapOutcome.ExistingActive,
                visual,
                default,
                SelectedProfile: 0,
                Global03A9: 0,
                Mode81: 0,
                Cooldown03FA: cooldown03FA);
        }

        var immediate = engineSubstate02 < 0x0C && flag74 == 0;
        byte selector;
        if (immediate)
        {
            selector = (engineSubstate02 & 0xFE) == 0x08
                ? (byte)4
                : (byte)3;
        }
        else
        {
            selector = stageDerivedSelector;
        }

        if (selector == 0)
        {
            return new(
                PlatformMultisprite9B93BootstrapOutcome.SelectorZero,
                visual,
                default,
                selector,
                Global03A9: 0,
                Mode81: 0,
                cooldown03FA);
        }

        var profile = PlatformMultisprite9B93Profile.ForSelector(selector);
        var mode81 = immediate ? (byte)3 : (byte)0;

        if (!immediate && cooldown03FA != 0)
        {
            return new(
                PlatformMultisprite9B93BootstrapOutcome.CooldownDecremented,
                visual,
                default,
                selector,
                profile.Global03A9,
                mode81,
                unchecked((byte)(cooldown03FA - 1)));
        }

        cooldown03FA = 0x80;
        var flags = (byte)(0x02 | (selector is 3 or 4 ? 0x00 : 0x04));
        var xBase = mode81 == 0 ? (byte)0xF7 : playerX3F;
        var sprite = profile.SpriteBase;

        var p0 = new PlatformMultisprite9B93Part(0xF8, sprite, flags, xBase);

        // `$02==$10` branches directly to RTS after the first record. It has
        // already reset logical phase/profile and cooldown, but does not clear
        // logical action +$00 through the later `$9CA6` epilogue.
        if (engineSubstate02 == 0x10)
        {
            var specialVisual = visual with { Part0 = p0 };
            return new(
                PlatformMultisprite9B93BootstrapOutcome.Initialized,
                specialVisual,
                Logical(profile, actionCleared: false),
                selector,
                profile.Global03A9,
                mode81,
                cooldown03FA);
        }

        var p1 = new PlatformMultisprite9B93Part(0xF8, unchecked((byte)(sprite + 1)), flags, unchecked((byte)(xBase + 8)));
        sprite = unchecked((byte)(sprite + 2));

        // `$02==$0C` skips two sprite/tile values between the two rows.
        if (engineSubstate02 == 0x0C)
            sprite = unchecked((byte)(sprite + 2));

        var p2 = new PlatformMultisprite9B93Part(0x00, sprite, flags, xBase);
        var p3 = new PlatformMultisprite9B93Part(0x00, unchecked((byte)(sprite + 1)), flags, unchecked((byte)(xBase + 8)));

        return new(
            PlatformMultisprite9B93BootstrapOutcome.Initialized,
            new PlatformMultisprite9B93VisualState(p0, p1, p2, p3),
            Logical(profile, actionCleared: true),
            selector,
            profile.Global03A9,
            mode81,
            cooldown03FA);
    }

    private static PlatformMultisprite9B93LogicalBootstrap Logical(
        PlatformMultisprite9B93Profile profile,
        bool actionCleared) =>
        new(
            Phase03: 0,
            profile.Profile0C,
            profile.Profile0D,
            profile.Profile0E,
            profile.Profile0F,
            Action00WasCleared: actionCleared);
}

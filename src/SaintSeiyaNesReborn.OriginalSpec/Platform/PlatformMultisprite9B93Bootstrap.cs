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
    bool Global03A9WasWritten,
    byte Mode81,
    byte Cooldown03FA);

/// <summary>
/// Bootstrap portion of bank-3 `$9B93-$9CAB` for the independent multisprite
/// class rooted at visual `$07E0` and logical `$03FB`.
///
/// Existing active visuals transfer to `$9CAC+`. Empty visuals may initialize
/// through the ordinary selector profile path, except engine substate `$0D`,
/// which branches from `$9C0F` to its dedicated initializer at `$A0E4`.
/// </summary>
public static class PlatformMultisprite9B93Bootstrap
{
    private static readonly PlatformMultisprite9B93LogicalBootstrap Substate0DLogical =
        new(
            Phase03: 0,
            Profile0C: 0x1E,
            Profile0D: 0x05,
            Profile0E: 0x05,
            Profile0F: 0x01,
            Action00WasCleared: true);

    public static PlatformMultisprite9B93BootstrapResult Step(
        PlatformMultisprite9B93VisualState visual,
        byte engineSubstate02,
        byte flag74,
        byte stageDerivedSelector,
        byte cooldown03FA,
        byte playerX3F,
        byte entropy48 = 0)
    {
        if (!visual.AllEmpty)
        {
            return new(
                PlatformMultisprite9B93BootstrapOutcome.ExistingActive,
                visual,
                default,
                SelectedProfile: 0,
                Global03A9: 0,
                Global03A9WasWritten: false,
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

        // Selector zero jumps directly to the active updater at $9CAC. With an
        // all-empty visual block that updater immediately returns; neither $81
        // nor $03A9 is written on this path.
        if (selector == 0)
        {
            return new(
                PlatformMultisprite9B93BootstrapOutcome.SelectorZero,
                visual,
                default,
                selector,
                Global03A9: 0,
                Global03A9WasWritten: false,
                Mode81: 0,
                cooldown03FA);
        }

        var profile = PlatformMultisprite9B93Profile.ForSelector(selector);
        var mode81 = immediate ? (byte)3 : (byte)0;

        // Timed path writes $81=0 before checking/decrementing $03FA. It returns
        // before the later $03A9 profile write when cooldown is still nonzero.
        if (!immediate && cooldown03FA != 0)
        {
            return new(
                PlatformMultisprite9B93BootstrapOutcome.CooldownDecremented,
                visual,
                default,
                selector,
                Global03A9: 0,
                Global03A9WasWritten: false,
                mode81,
                unchecked((byte)(cooldown03FA - 1)));
        }

        cooldown03FA = 0x80;

        // `$9C03` clears logical phase before the `$02==$0D` branch. `$A0E4`
        // then creates one dedicated part and loads raw profile bytes from
        // `$9B8F`: 1E,05,05,01. It does NOT write global `$03A9` and does not
        // use the selector's ordinary sprite/profile tables.
        if (engineSubstate02 == 0x0D)
        {
            var rightVariant = (entropy48 & 0x08) != 0;
            var part0 = new PlatformMultisprite9B93Part(
                Y: 0x20,
                Sprite: 0x8C,
                Flags: rightVariant ? (byte)0x42 : (byte)0x02,
                X: rightVariant ? (byte)0x11 : (byte)0xEF);

            return new(
                PlatformMultisprite9B93BootstrapOutcome.Initialized,
                visual with { Part0 = part0 },
                Substate0DLogical,
                selector,
                Global03A9: 0,
                Global03A9WasWritten: false,
                Mode81: 0,
                cooldown03FA);
        }

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
                Global03A9WasWritten: true,
                mode81,
                cooldown03FA);
        }

        var p1 = new PlatformMultisprite9B93Part(
            0xF8,
            unchecked((byte)(sprite + 1)),
            flags,
            unchecked((byte)(xBase + 8)));
        sprite = unchecked((byte)(sprite + 2));

        // `$02==$0C` skips two sprite/tile values between the two rows.
        if (engineSubstate02 == 0x0C)
            sprite = unchecked((byte)(sprite + 2));

        var p2 = new PlatformMultisprite9B93Part(0x00, sprite, flags, xBase);
        var p3 = new PlatformMultisprite9B93Part(
            0x00,
            unchecked((byte)(sprite + 1)),
            flags,
            unchecked((byte)(xBase + 8)));

        return new(
            PlatformMultisprite9B93BootstrapOutcome.Initialized,
            new PlatformMultisprite9B93VisualState(p0, p1, p2, p3),
            Logical(profile, actionCleared: true),
            selector,
            profile.Global03A9,
            Global03A9WasWritten: true,
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

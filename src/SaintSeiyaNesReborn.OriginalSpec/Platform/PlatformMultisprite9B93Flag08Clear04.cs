namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformMultisprite9B93Flag08Clear04Outcome
{
    Active,
    AllPartsRetired,
}

public readonly record struct PlatformMultisprite9B93PostMoveContacts(
    PlatformEntityContactResult? Call0,
    PlatformEntityContactResult? Call1,
    PlatformEntityContactResult? Call2,
    int FinalVisualIndex,
    PlatformContactPhaseState ContactState,
    PlatformMultisprite9B93LogicalState LogicalState);

public readonly record struct PlatformMultisprite9B93Flag08Clear04Result(
    PlatformMultisprite9B93RuntimeState State,
    PlatformMultisprite9B93Flag08Clear04Outcome Outcome,
    PlatformMultisprite9B93PostMoveContacts Contacts,
    byte InitialCurveByte,
    byte CurvePart0,
    byte CurvePart1,
    byte CurvePart2,
    byte CurvePart3);

/// <summary>
/// Exact flag-$08-set / flag-$04-clear route at `$9F79-$A041` for the
/// independent `$07E0/$03FB` multisprite class.
///
/// Unlike the bit-$04-set route, this branch performs all visual movement first
/// and only then runs up to three `$A042` player-contact calls. It never calls
/// `$9915` for player projectiles.
/// </summary>
public static class PlatformMultisprite9B93Flag08Clear04
{
    // Fixed-bank `$C639-$C658`. This route can consume all 32 bytes; the common
    // entity jump route reaches a smaller effective subset.
    private static readonly byte[] CurveC639 =
    [
        0x08,0x08,0x07,0x07,0x06,0x05,0x04,0x03,
        0x03,0x02,0x02,0x01,0x01,0x01,0x00,0x00,
        0x00,0x00,0xFF,0xFF,0xFF,0xFF,0xFE,0xFE,
        0xFE,0xFE,0xFE,0xFD,0xFD,0xFD,0xFD,0xFD,
    ];

    public static PlatformMultisprite9B93Flag08Clear04Result Step(
        PlatformMultisprite9B93RuntimeState state,
        PlatformContactPhaseState contactState,
        byte cameraDelta43,
        byte playerX3F,
        byte playerY40,
        byte frameStartPlayerAction4E,
        byte engineSubstate02)
    {
        ValidateEntry(state, engineSubstate02);

        var logical = state.Logical;
        var nextPhase = unchecked((byte)(logical.Phase03 + 1));
        logical = logical with { Phase03 = nextPhase };

        var curveIndex = unchecked((byte)(nextPhase - 2));
        var curve = curveIndex < 0x20 ? CurveC639[curveIndex] : (byte)0xFE;
        var initialCurve = curve;

        var spriteBase = engineSubstate02 == 0x10 ? (byte)0xDA : (byte)0xB8;
        var visual = state.Visual;
        var curves = new byte[4];

        for (var i = 0; i < 4; i++)
        {
            var part = GetPart(visual, i);
            var sprite = unchecked((byte)(spriteBase + i));

            if (part.IsEmpty)
            {
                curves[i] = curve;
                continue;
            }

            part = part with { Sprite = sprite };

            // `$9FB1-$9FBE`: odd sprite values arithmetic-shift the shared curve
            // byte right once. The mutated value remains in `$31` for all later
            // parts in the same update.
            if ((sprite & 1) != 0)
                curve = ArithmeticShiftRight(curve);
            curves[i] = curve;

            var y = unchecked((byte)(part.Y - curve));
            part = part with { Y = y };
            if (y >= 0xA8)
                part = part with { Y = 0xF0, Sprite = 0xFE };

            // `$9FC0-$9FC9` indexes from sprite-$B8 into raw ROM bytes rooted at
            // `$9F75`. For `$DA` this deliberately lands on `$9F97`, byte `$F0`,
            // which is also executable code. Preserve this code-as-data quirk.
            var horizontal = HorizontalOffsetForSprite(sprite);
            var x = unchecked((byte)(part.X + horizontal - cameraDelta43));
            part = part with { X = x };
            if (x < 0x05)
                part = part with { Y = 0xF0, Sprite = 0xFE };

            visual = SetPart(visual, i, part);
        }

        if (visual.AllEmpty)
            logical = logical with { Phase03 = 0 };

        // `$A039` runs after movement. Three calls to `$A042` share one moving
        // visual pointer. Crucially, an empty sprite returns before pointer +4,
        // so later calls repeatedly see that same empty record rather than
        // skipping ahead.
        var contacts = ApplyThreePostMoveContacts(
            visual,
            logical,
            contactState,
            playerX3F,
            playerY40,
            frameStartPlayerAction4E);
        logical = contacts.LogicalState;
        contactState = contacts.ContactState;

        state = state with { Visual = visual, Logical = logical };
        return new(
            state,
            visual.AllEmpty
                ? PlatformMultisprite9B93Flag08Clear04Outcome.AllPartsRetired
                : PlatformMultisprite9B93Flag08Clear04Outcome.Active,
            contacts,
            initialCurve,
            curves[0],
            curves[1],
            curves[2],
            curves[3]);
    }

    /// <summary>
    /// Literal bytes selected by `$9FC0-$9FC9` for sprite types that can be
    /// assigned by this route. `$B8-$BB` are the explicit four-byte table at
    /// `$9F75`. `$DA-$DD` are the bytes at `$9F97-$9F9A`; only `$DA` is reachable
    /// from the canonical `$10` bootstrap, but the remaining cases are kept as
    /// the 6502 would read them if a later visual part were active.
    /// </summary>
    public static sbyte HorizontalOffsetForSprite(byte sprite) => sprite switch
    {
        0xB8 => -1,
        0xB9 => 1,
        0xBA => -2,
        0xBB => 2,
        0xDA => -16,   // raw byte $F0 at executable address $9F97
        0xDB => 4,     // raw byte $04 at $9F98
        0xDC => -87,   // raw byte $A9 at $9F99
        0xDD => -72,   // raw byte $B8 at $9F9A
        _ => throw new InvalidOperationException($"9F75 code/data offset is not closed for sprite ${sprite:X2}."),
    };

    private static PlatformMultisprite9B93PostMoveContacts ApplyThreePostMoveContacts(
        PlatformMultisprite9B93VisualState visual,
        PlatformMultisprite9B93LogicalState logical,
        PlatformContactPhaseState contactState,
        byte playerX,
        byte playerY,
        byte frameStartAction4E)
    {
        PlatformEntityContactResult? c0 = null;
        PlatformEntityContactResult? c1 = null;
        PlatformEntityContactResult? c2 = null;
        var visualIndex = 0;

        for (var call = 0; call < 3; call++)
        {
            var part = GetPart(visual, visualIndex);
            if (part.IsEmpty)
            {
                // `$A048 BEQ $A06D`: no logical position sync and, critically,
                // no `$12 += 4`. The next JSR A042 re-checks the same record.
                continue;
            }

            logical = logical with { Y02 = part.Y, X01 = part.X };
            var contact = PlatformEntityContact.Evaluate(
                frameStartAction4E,
                playerX,
                playerY,
                part.X,
                part.Y,
                contactState.HazardLatch76,
                logical.LifeDrain0E,
                logical.CosmoDrain0D,
                PlatformContactHitboxParameters.AuxiliaryHazard);

            if (contact.Triggered)
                contactState = new PlatformContactPhaseState(contact.HazardLatch76, contact.DrainState);

            switch (call)
            {
                case 0: c0 = contact; break;
                case 1: c1 = contact; break;
                case 2: c2 = contact; break;
            }

            visualIndex++;
        }

        return new(c0, c1, c2, visualIndex, contactState, logical);
    }

    private static byte ArithmeticShiftRight(byte value) =>
        unchecked((byte)((value >> 1) | (value & 0x80)));

    private static void ValidateEntry(PlatformMultisprite9B93RuntimeState state, byte substate)
    {
        if (state.Visual.AllEmpty)
            throw new InvalidOperationException("Flag08/clear04 route requires an active visual record.");
        if (substate == 0x0D)
            throw new InvalidOperationException("Substate $0D uses A12A, not the flag08 common route.");
        if ((state.Visual.Part0.Flags & 0x08) == 0 || (state.Visual.Part0.Flags & 0x04) != 0)
            throw new InvalidOperationException("Route requires visual flag $08 set and flag $04 clear.");

        var family = state.Logical.Action00 & 0xF0;
        if (family is 0xD0 or 0xE0)
            throw new InvalidOperationException("Logical D0/E0 diverts to A06E before this route.");
    }

    private static PlatformMultisprite9B93Part GetPart(PlatformMultisprite9B93VisualState visual, int index) => index switch
    {
        0 => visual.Part0,
        1 => visual.Part1,
        2 => visual.Part2,
        3 => visual.Part3,
        _ => throw new ArgumentOutOfRangeException(nameof(index)),
    };

    private static PlatformMultisprite9B93VisualState SetPart(
        PlatformMultisprite9B93VisualState visual,
        int index,
        PlatformMultisprite9B93Part value) => index switch
    {
        0 => visual with { Part0 = value },
        1 => visual with { Part1 = value },
        2 => visual with { Part2 = value },
        3 => visual with { Part3 = value },
        _ => throw new ArgumentOutOfRangeException(nameof(index)),
    };
}

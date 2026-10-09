namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformMultisprite9B93DeathDropOutcome
{
    Active,
    CompletedActionRange,
    RemovedVerticalBoundary,
}

public readonly record struct PlatformMultisprite9B93DeathDropResult(
    PlatformMultisprite9B93RuntimeState State,
    PlatformMultisprite9B93DeathDropOutcome Outcome,
    byte SpriteTypeUsed,
    int VerticalDeltaApplied);

/// <summary>
/// Exact `$A06E-$A0DF` terminal path reached when logical action family is
/// `$D0` or `$E0` in the `$9B93` multisprite class.
///
/// This path performs no player contact and no projectile collision.
/// </summary>
public static class PlatformMultisprite9B93DeathDrop
{
    private static readonly byte[] OrientationFlags = [0x00, 0x40, 0x80, 0xC0];

    public static PlatformMultisprite9B93DeathDropResult Step(
        PlatformMultisprite9B93RuntimeState state,
        byte engineSubstate02,
        byte cameraDelta43)
    {
        var family = state.Logical.Action00 & 0xF0;
        if (family is not (0xD0 or 0xE0))
            throw new InvalidOperationException($"A06E path requires logical $D0/$E0 family, got ${state.Logical.Action00:X2}.");

        var logical = state.Logical with
        {
            Action00 = unchecked((byte)(state.Logical.Action00 + 1)),
        };

        if (logical.Action00 >= 0xF0)
        {
            logical = logical with { Phase03 = 0 };
            state = state with { Visual = ClearAll(state.Visual), Logical = logical };
            return new(
                state,
                PlatformMultisprite9B93DeathDropOutcome.CompletedActionRange,
                SpriteTypeUsed: 0,
                VerticalDeltaApplied: 0);
        }

        var sprite = engineSubstate02 == 0x0C
            ? (logical.Action00 >= 0xE8 ? (byte)0x6A : (byte)0x6B)
            : (logical.Action00 >= 0xE8 ? (byte)0xD7 : (byte)0xD6);

        logical = logical with { Phase03 = unchecked((byte)(logical.Phase03 + 1)) };
        var verticalDelta = logical.Phase03 >> 3;
        var visual = state.Visual;

        for (var i = 0; i < 4; i++)
        {
            var part = GetPart(visual, i);
            part = part with
            {
                Y = unchecked((byte)(part.Y + verticalDelta)),
                Sprite = sprite,
                Flags = (byte)((part.Flags & 0x3F) | OrientationFlags[i]),
                X = unchecked((byte)(part.X - 2 - cameraDelta43)),
            };
            visual = SetPart(visual, i, part);
        }

        if (visual.Part0.Y >= 0xA0)
        {
            logical = logical with { Phase03 = 0 };
            state = state with { Visual = ClearAll(visual), Logical = logical };
            return new(
                state,
                PlatformMultisprite9B93DeathDropOutcome.RemovedVerticalBoundary,
                sprite,
                verticalDelta);
        }

        state = state with { Visual = visual, Logical = logical };
        return new(
            state,
            PlatformMultisprite9B93DeathDropOutcome.Active,
            sprite,
            verticalDelta);
    }

    private static PlatformMultisprite9B93VisualState ClearAll(PlatformMultisprite9B93VisualState visual) =>
        new(Clear(visual.Part0), Clear(visual.Part1), Clear(visual.Part2), Clear(visual.Part3));

    private static PlatformMultisprite9B93Part Clear(PlatformMultisprite9B93Part part) =>
        part with { Y = 0xF0, Sprite = 0xFE };

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

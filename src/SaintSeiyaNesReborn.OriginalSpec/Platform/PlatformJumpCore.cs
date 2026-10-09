using SaintSeiyaNesReborn.OriginalSpec;

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformJumpProfileKind
{
    OrdinaryVertical,
    HighVertical,
    Directional,
}

public enum PlatformJumpVerticalOutcome
{
    InAir,
    Landed,
    CeilingInterrupted,
    FellOut,
}

public readonly record struct PlatformJumpState(
    byte PlayerY,
    byte PlayerYPage41,
    byte JumpPhase49,
    byte JumpButtonLatch4A,
    byte ActionState4D,
    byte HighJumpFlag038A,
    byte JumpLock038D,
    byte HazardFlag76)
{
    public bool Active => JumpPhase49 != 0;
    public bool IsJumpFamily => (ActionState4D & 0xF0) == 0x30;
    public int DirectionBits => ActionState4D & 0x03;
}

public readonly record struct PlatformJumpVerticalStep(
    PlatformJumpState State,
    PlatformJumpProfileKind ProfileKind,
    int Duration,
    int HalfPhase,
    int AppliedDelta,
    PlatformJumpVerticalOutcome Outcome);

/// <summary>
/// Table-shaped jump behavior reconstructed from bank 3 $BB76/$BCD3-$BF48.
/// Positive curve deltas move upward (subtract screen Y); negative deltas move down.
/// </summary>
public static class PlatformJumpCore
{
    private sealed record JumpProfile(int Duration, sbyte[] Deltas)
    {
        public int HalfPhase => Duration >> 1;

        public sbyte DeltaForPhase(int phase)
        {
            var index = phase - 2;
            if ((uint)index >= Deltas.Length)
                throw new ArgumentOutOfRangeException(nameof(phase), phase, "Table-controlled jump phase must be 2..duration-1.");
            return Deltas[index];
        }
    }

    // Mechanical displacement curves reconstructed from the canonical game.
    // RLE keeps the clean-room specification readable while preserving frame parity.
    private static readonly JumpProfile Ordinary = new(0x20, Expand(
        (8, 2), (7, 2), (6, 1), (5, 1), (4, 1), (3, 2), (2, 2), (1, 3),
        (0, 4), (-1, 4), (-2, 5), (-3, 3)));

    private static readonly JumpProfile HighA = new(60, Expand(
        (9, 1), (8, 1), (7, 2), (6, 2), (5, 2), (4, 5), (3, 5), (2, 5),
        (1, 5), (0, 6), (-1, 4), (-2, 12), (-3, 8)));

    private static readonly JumpProfile HighB = new(50, Expand(
        (9, 1), (8, 1), (7, 2), (6, 2), (5, 2), (4, 3), (3, 3), (2, 5),
        (1, 4), (0, 6), (-1, 4), (-2, 12), (-3, 3)));

    private static readonly JumpProfile HighC = new(40, Expand(
        (9, 1), (7, 1), (6, 2), (5, 2), (4, 4), (3, 3), (2, 3), (1, 2),
        (0, 6), (-1, 4), (-2, 4), (-3, 6)));

    private static readonly JumpProfile DirectionalA = new(54, Expand(
        (4, 1), (3, 1), (2, 10), (1, 11), (0, 1), (1, 1), (0, 6),
        (-1, 4), (-2, 8), (-3, 9)));

    private static readonly JumpProfile DirectionalB = new(40, Expand(
        (4, 1), (3, 1), (2, 10), (1, 6), (0, 6), (-1, 4), (-2, 6), (-3, 4)));

    private static readonly JumpProfile DirectionalC = new(44, Expand(
        (4, 1), (3, 1), (2, 10), (1, 6), (0, 1), (1, 1), (0, 6),
        (-1, 4), (-2, 6), (-3, 6)));

    /// <summary>
    /// Mirrors the A-button gate at $BB76. This omits audio only.
    /// Down+A from the crouch state is handled by the separate drop-through path;
    /// in the ordinary standing path Down blocks jump start and latches A as $FF.
    /// </summary>
    public static PlatformJumpState ApplyJumpButton(
        PlatformJumpState state,
        PlatformInput input)
    {
        var aHeld = (input & PlatformInput.A) != 0;
        if (!aHeld)
            return state with { JumpButtonLatch4A = 0 };

        if (state.JumpPhase49 != 0 || state.JumpButtonLatch4A != 0)
            return state;

        if ((input & PlatformInput.Down) != 0 || state.JumpLock038D != 0)
            return state with { JumpButtonLatch4A = 0xFF };

        var directionBits = (byte)((byte)input & 0x03);
        if (directionBits != 0)
        {
            return state with
            {
                JumpPhase49 = 1,
                JumpButtonLatch4A = 1,
                HighJumpFlag038A = 0,
                ActionState4D = (byte)(0x30 + directionBits),
            };
        }

        return state with
        {
            JumpPhase49 = 1,
            JumpButtonLatch4A = 1,
            ActionState4D = 0x30,
            HighJumpFlag038A = (byte)(((input & PlatformInput.Up) != 0) ? 0x30 : 0),
        };
    }

    public static PlatformJumpVerticalStep StepVertical(
        PlatformJumpState state,
        PlatformSaintIndex saint,
        PlatformCollisionDescriptors probes,
        byte dynamicFloorY039B = 0)
    {
        if (state.JumpPhase49 == 0)
            throw new InvalidOperationException("Vertical jump step requires a nonzero $49 phase.");
        if (!state.IsJumpFamily)
            throw new InvalidOperationException("Vertical jump step requires action family $30-$33.");

        var (kind, profile) = SelectProfile(state, saint);
        var half = profile.HalfPhase;

        // $BCD3 checks floor contact before incrementing the current phase once
        // the jump has reached the second half of its selected duration.
        if (state.JumpPhase49 >= half)
        {
            var landing = ResolveLanding(state, probes.FloorCenter, dynamicFloorY039B);
            if (landing is { } resolved)
            {
                return new PlatformJumpVerticalStep(
                    resolved.State,
                    kind,
                    profile.Duration,
                    half,
                    AppliedDelta: 0,
                    resolved.Outcome);
            }
        }

        var phase = unchecked((byte)(state.JumpPhase49 + 1));
        state = state with { JumpPhase49 = phase };
        var appliedDelta = 0;

        if (phase >= profile.Duration)
        {
            // Table exhausted: original enters fixed +3 screen-Y fall while
            // keeping $49 active until floor resolution clears it.
            var sum = state.PlayerY + 3;
            state = state with
            {
                PlayerY = (byte)sum,
                PlayerYPage41 = (byte)(sum > 0xFF ? 0 : state.PlayerYPage41),
            };
            appliedDelta = -3;
        }
        else
        {
            var delta = profile.DeltaForPhase(phase);
            appliedDelta = delta;
            state = ApplySignedVerticalDelta(state, delta);
        }

        // Head probe $56 was sampled before player simulation this frame. Only
        // descriptor family $E0-$EF interrupts the jump into action family $50.
        if (probes.UpperCenter is byte head && head is >= 0xE0 and < 0xF0)
        {
            state = state with { JumpPhase49 = 0, ActionState4D = 0x50 };
            return new PlatformJumpVerticalStep(
                state, kind, profile.Duration, half, appliedDelta,
                PlatformJumpVerticalOutcome.CeilingInterrupted);
        }

        return new PlatformJumpVerticalStep(
            state, kind, profile.Duration, half, appliedDelta,
            PlatformJumpVerticalOutcome.InAir);
    }

    public static (PlatformJumpProfileKind Kind, int Duration, int HalfPhase) DescribeProfile(
        PlatformJumpState state,
        PlatformSaintIndex saint)
    {
        var (kind, profile) = SelectProfile(state, saint);
        return (kind, profile.Duration, profile.HalfPhase);
    }

    private static (PlatformJumpProfileKind Kind, JumpProfile Profile) SelectProfile(
        PlatformJumpState state,
        PlatformSaintIndex saint)
    {
        if ((state.ActionState4D & 0x03) != 0)
            return (PlatformJumpProfileKind.Directional, DirectionalProfile(saint));

        if (state.HighJumpFlag038A != 0)
            return (PlatformJumpProfileKind.HighVertical, HighProfile(saint));

        return (PlatformJumpProfileKind.OrdinaryVertical, Ordinary);
    }

    private static JumpProfile HighProfile(PlatformSaintIndex saint) => saint switch
    {
        PlatformSaintIndex.Seiya => HighA,
        PlatformSaintIndex.Shun => HighB,
        PlatformSaintIndex.Hyoga => HighC,
        PlatformSaintIndex.Shiryu => HighC,
        PlatformSaintIndex.Ikki => HighB,
        _ => throw new ArgumentOutOfRangeException(nameof(saint)),
    };

    private static JumpProfile DirectionalProfile(PlatformSaintIndex saint) => saint switch
    {
        PlatformSaintIndex.Seiya => DirectionalA,
        PlatformSaintIndex.Shun => DirectionalB,
        PlatformSaintIndex.Hyoga => DirectionalC,
        PlatformSaintIndex.Shiryu => DirectionalC,
        PlatformSaintIndex.Ikki => DirectionalA,
        _ => throw new ArgumentOutOfRangeException(nameof(saint)),
    };

    private static PlatformJumpState ApplySignedVerticalDelta(PlatformJumpState state, sbyte delta)
    {
        if (delta == 0)
            return state;

        if (delta > 0)
        {
            var y = state.PlayerY - delta;
            return state with
            {
                PlayerY = unchecked((byte)y),
                PlayerYPage41 = unchecked((byte)(state.PlayerYPage41 - (y < 0 ? 1 : 0))),
            };
        }

        var magnitude = -delta;
        var sum = state.PlayerY + magnitude;
        return state with
        {
            PlayerY = unchecked((byte)sum),
            PlayerYPage41 = unchecked((byte)(state.PlayerYPage41 + (sum > 0xFF ? 1 : 0))),
        };
    }

    private readonly record struct LandingResolution(
        PlatformJumpState State,
        PlatformJumpVerticalOutcome Outcome);

    private static LandingResolution? ResolveLanding(
        PlatformJumpState state,
        byte? floorDescriptor,
        byte dynamicFloorY039B)
    {
        var y = state.PlayerY;

        if (y < 0x86)
        {
            if (floorDescriptor is not byte descriptor)
                return null;

            if (descriptor == 0xFF)
                return Land(state with { PlayerY = dynamicFloorY039B });

            if (descriptor < 0x80)
                return null;

            var low = y & 0x0F;
            if (descriptor >= 0xF0)
            {
                if (low < 8)
                    return null;
                return Land(state with { PlayerY = (byte)((y & 0xF0) | 0x08) });
            }

            if (low >= 6)
                return null;
            return Land(state with { PlayerY = (byte)(y & 0xF0) });
        }

        if (floorDescriptor is 0xF8 or 0xF9)
        {
            var flag = state.HazardFlag76 == 0 ? (byte)1 : state.HazardFlag76;
            return Land(state with { PlayerY = 0x88, HazardFlag76 = flag });
        }

        if (y < 0xA0 || state.PlayerYPage41 != 0)
            return null;

        return new LandingResolution(
            state with
            {
                PlayerY = 0xA0,
                JumpPhase49 = 0,
                ActionState4D = 0x80,
                HazardFlag76 = 0x80,
            },
            PlatformJumpVerticalOutcome.FellOut);
    }

    private static LandingResolution Land(PlatformJumpState state) => new(
        state with
        {
            JumpPhase49 = 0,
            ActionState4D = 0,
            JumpLock038D = 0,
        },
        PlatformJumpVerticalOutcome.Landed);

    private static sbyte[] Expand(params (int Value, int Count)[] runs)
    {
        var result = new List<sbyte>();
        foreach (var (value, count) in runs)
        {
            for (var i = 0; i < count; i++)
                result.Add(checked((sbyte)value));
        }
        return result.ToArray();
    }
}

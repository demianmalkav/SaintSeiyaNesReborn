namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformAirborneVerticalState(
    byte PlayerY,
    byte PlayerYHigh41,
    byte ActionState,
    byte JumpPhase49,
    byte HighJumpSelector038A,
    byte Support038D,
    byte Special76);

public readonly record struct PlatformAirborneVerticalResult(
    PlatformAirborneVerticalState State,
    int PhaseLimit,
    int HalfPhase,
    bool Landed,
    bool CeilingInterrupted,
    bool HazardTriggered,
    bool UsedTerminalFall,
    bool ContinueHorizontal,
    int ScreenYDeltaApplied);

/// <summary>
/// Clean-room reconstruction of the vertical/landing portion of bank 3 $BCD3+,
/// including the pre-increment landing call at $B8CD and the post-delta head test.
/// Collision descriptors are sampled before this routine in the original frame.
/// </summary>
public static class PlatformAirborneVerticalMotion
{
    public static PlatformAirborneVerticalResult Step(
        PlatformSaintIndex saint,
        PlatformAirborneVerticalState state,
        PlatformCollisionDescriptors probes,
        byte dynamicFloorY039B = 0)
    {
        if (state.JumpPhase49 == 0)
            return new(state, 0, 0, false, false, false, false, false, 0);

        if (PlatformActionState.Family(state.ActionState) != (byte)PlatformActionFamily.Jump)
            throw new ArgumentException("Non-zero jump phase requires jump-family action state.", nameof(state));

        var profile = SelectProfile(saint, state.ActionState, state.HighJumpSelector038A);
        var phaseLimit = profile.PhaseLimit;
        var halfPhase = phaseLimit / 2;

        // $BCD3 calls $B8CD before incrementing $49 once the current phase reaches
        // the midpoint. Landing can therefore terminate the frame before any new
        // vertical table entry is consumed.
        if (state.JumpPhase49 >= halfPhase)
        {
            var landing = TryResolveLanding(state, probes.FloorCenter, dynamicFloorY039B);
            if (landing.Terminated)
            {
                return new(
                    landing.State,
                    phaseLimit,
                    halfPhase,
                    landing.Landed,
                    false,
                    landing.HazardTriggered,
                    false,
                    false,
                    0);
            }
        }

        var nextPhase = unchecked((byte)(state.JumpPhase49 + 1));
        var next = state with { JumpPhase49 = nextPhase };
        var usedTerminal = nextPhase >= phaseLimit;
        var screenDelta = 0;

        if (usedTerminal)
        {
            screenDelta = 3;
            var sum = next.PlayerY + 3;
            next = next with
            {
                PlayerY = (byte)sum,
                // Original $BD27 clears $41 on carry instead of incrementing it.
                PlayerYHigh41 = sum > 0xFF ? (byte)0 : next.PlayerYHigh41,
            };
        }
        else
        {
            var tableIndex = nextPhase - 2;
            if ((uint)tableIndex >= profile.TableFrames)
                throw new InvalidOperationException(
                    $"Jump phase/table mismatch: phase {nextPhase}, limit {phaseLimit}, table frames {profile.TableFrames}.");

            var rise = profile.RisePerFrame[tableIndex];
            screenDelta = -rise;
            next = ApplySignedRise(next, rise);
        }

        // The head descriptor was sampled before movement. $E0-$EF stops the jump
        // after the vertical displacement has already been applied, switches to $50,
        // and skips the horizontal air-control path for this frame.
        if (IsFullCeiling(probes.UpperCenter))
        {
            next = next with
            {
                JumpPhase49 = 0,
                ActionState = (byte)PlatformActionFamily.FallOrDrop,
            };
            return new(next, phaseLimit, halfPhase, false, true, false, usedTerminal, false, screenDelta);
        }

        return new(next, phaseLimit, halfPhase, false, false, false, usedTerminal, true, screenDelta);
    }

    private static PlatformJumpProfile SelectProfile(
        PlatformSaintIndex saint,
        byte actionState,
        byte highJumpSelector038A)
    {
        if ((actionState & 0x03) != 0)
            return PlatformJumpProfile.Get(PlatformJumpKind.Directional, saint);

        return PlatformJumpProfile.Get(
            highJumpSelector038A == 0 ? PlatformJumpKind.Standing : PlatformJumpKind.High,
            saint);
    }

    private static PlatformAirborneVerticalState ApplySignedRise(
        PlatformAirborneVerticalState state,
        sbyte rise)
    {
        if (rise == 0)
            return state;

        if (rise > 0)
        {
            var value = state.PlayerY - rise;
            return state with
            {
                PlayerY = unchecked((byte)value),
                PlayerYHigh41 = value < 0
                    ? unchecked((byte)(state.PlayerYHigh41 - 1))
                    : state.PlayerYHigh41,
            };
        }

        var fall = -rise;
        var sum = state.PlayerY + fall;
        return state with
        {
            PlayerY = unchecked((byte)sum),
            PlayerYHigh41 = sum > 0xFF
                ? unchecked((byte)(state.PlayerYHigh41 + 1))
                : state.PlayerYHigh41,
        };
    }

    private readonly record struct LandingResolution(
        PlatformAirborneVerticalState State,
        bool Terminated,
        bool Landed,
        bool HazardTriggered);

    private static LandingResolution TryResolveLanding(
        PlatformAirborneVerticalState state,
        byte? floorCenter,
        byte dynamicFloorY039B)
    {
        var y = state.PlayerY;

        if (y < 0x86)
        {
            if (floorCenter is null)
                return NoLanding(state);

            if (floorCenter == 0xFF)
                return Land(state, dynamicFloorY039B);

            if (floorCenter < 0x80)
                return NoLanding(state);

            var low = y & 0x0F;
            if (floorCenter < 0xF0)
                return low < 6 ? Land(state, (byte)(y & 0xF0)) : NoLanding(state);

            return low >= 8
                ? Land(state, (byte)((y & 0xF0) | 0x08))
                : NoLanding(state);
        }

        if (floorCenter is 0xF8 or 0xF9)
        {
            var special = state.Special76 == 0
                ? (byte)1
                : state.Special76;
            return Land(state with { Special76 = special }, 0x88);
        }

        if (y < 0xA0 || state.PlayerYHigh41 != 0)
            return NoLanding(state);

        var hazard = state with
        {
            PlayerY = 0xA0,
            JumpPhase49 = 0,
            ActionState = (byte)PlatformActionFamily.DamageOrHazard,
            Special76 = 0x80,
        };
        return new LandingResolution(hazard, true, false, true);
    }

    private static LandingResolution Land(PlatformAirborneVerticalState state, byte targetY)
    {
        var landed = state with
        {
            PlayerY = targetY,
            JumpPhase49 = 0,
            ActionState = (byte)PlatformActionFamily.Neutral,
            Support038D = 0,
        };
        return new LandingResolution(landed, true, true, false);
    }

    private static LandingResolution NoLanding(PlatformAirborneVerticalState state) =>
        new(state, false, false, false);

    private static bool IsFullCeiling(byte? descriptor) =>
        descriptor is >= 0xE0 and < 0xF0;
}

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformCommonEntityMotionState(
    byte ActionState,
    byte X,
    byte Y,
    byte StatePhase,
    byte GroundDescriptor,
    byte DecisionTimer,
    byte FlagsFacing,
    byte Type,
    byte TerrainProbeRight,
    byte TerrainProbeLeft);

public readonly record struct PlatformEntityJumpStepResult(
    PlatformCommonEntityMotionState State,
    bool Landed,
    bool UsedTerminalFall,
    int ScreenYDeltaApplied,
    int TableIndexUsed);

/// <summary>
/// Clean-room common movable-entity motion primitives reconstructed from bank 3
/// and fixed-bank helpers used by the $A442 entity pipeline.
///
/// This class intentionally does not claim the complete stochastic chase/jump
/// decision state machine yet. It promotes only the closed horizontal cadence,
/// terrain-facing predicates, jump vertical phase and landing rules.
/// </summary>
public static class PlatformCommonEntityMotion
{
    public const byte FacingRightBit = 0x40;
    public const byte JumpLandingCheckPhase = 0x10;
    public const byte JumpTableLimit = 0x20;

    // Fixed $C639 contains 31 bytes, but $C5E6 consumes only indices 0..29:
    // current phase is incremented first and the table index is nextPhase-2;
    // nextPhase >= $20 takes the fixed +3 fall branch instead.
    private static readonly sbyte[] UsedJumpSource =
    [
        8, 8, 7, 7, 6, 5, 4, 3, 3, 2, 2, 1, 1, 1,
        0, 0, 0, 0, -1, -1, -1, -1, -2, -2, -2, -2, -2,
        -3, -3, -3,
    ];

    public static IReadOnlyList<sbyte> JumpSource => UsedJumpSource;

    /// <summary>$A60B common horizontal cadence.</summary>
    public static byte HorizontalStep(byte entityType, byte frameCounter3C)
    {
        if ((entityType & 0xFE) == 0x0A)
            return (byte)(frameCounter3C & 1);
        return (frameCounter3C & 3) == 0 ? (byte)2 : (byte)1;
    }

    /// <summary>$AA64 common decision-timer reseed, inclusive range 31..94.</summary>
    public static byte ReseedDecisionTimer(byte entropy48) =>
        (byte)((entropy48 & 0x3F) + 0x1F);

    public static bool FacingRight(byte flagsFacing) =>
        (flagsFacing & FacingRightBit) != 0;

    public static byte ToggleFacing(byte flagsFacing) =>
        (byte)(flagsFacing ^ FacingRightBit);

    /// <summary>
    /// $AA1F+ terrain-facing test. The caller supplies the active side probe:
    /// offset +$0A when facing right, +$0B when facing left.
    /// </summary>
    public static bool TerrainForcesTurn(byte entityType, byte flagsFacing, byte descriptor)
    {
        // Type $07 explicitly bypasses the normal turn response for $E4+.
        if (entityType == 0x07 && descriptor >= 0xE4)
            return false;

        if (descriptor >= 0xF0)
            return false;

        if (FacingRight(flagsFacing))
            return descriptor is >= 0x80 and < 0x88 or >= 0xE0 and < 0xF0;

        return descriptor is >= 0x88 and < 0x90 or >= 0xE0 and < 0xF0;
    }

    /// <summary>
    /// Confirmed horizontal component of entity jump states: $31 -> +X,
    /// $32 -> -X. Other $3x states have no common horizontal delta here.
    /// </summary>
    public static int JumpHorizontalDelta(byte actionState, byte entityType, byte frameCounter3C)
    {
        var step = HorizontalStep(entityType, frameCounter3C);
        return actionState switch
        {
            0x31 => step,
            0x32 => -step,
            _ => 0,
        };
    }

    /// <summary>
    /// Fixed $C5E6 vertical phase. From current phase $10 onward, landing is
    /// attempted before phase increment. If no landing occurs, phase increments;
    /// nextPhase >= $20 falls at +3 px/update, otherwise table[nextPhase-2] is
    /// subtracted from the 8-bit Y coordinate.
    /// </summary>
    public static PlatformEntityJumpStepResult StepJumpVertical(PlatformCommonEntityMotionState state)
    {
        if (state.StatePhase == 0)
            return new(state, false, false, 0, -1);

        if (state.StatePhase >= JumpLandingCheckPhase)
        {
            var landing = TryLand(state);
            if (landing.Landed)
                return new(landing.State, true, false, landing.ScreenYDelta, -1);
        }

        var nextPhase = unchecked((byte)(state.StatePhase + 1));
        state = state with { StatePhase = nextPhase };

        if (nextPhase >= JumpTableLimit)
        {
            state = state with { Y = unchecked((byte)(state.Y + 3)) };
            return new(state, false, true, 3, -1);
        }

        var tableIndex = nextPhase - 2;
        if ((uint)tableIndex >= UsedJumpSource.Length)
            throw new InvalidOperationException($"Entity jump phase/table mismatch at phase {nextPhase}.");

        var source = UsedJumpSource[tableIndex];
        // 6502 performs SEC/SBC on the raw two's-complement table byte.
        var nextY = unchecked((byte)(state.Y - unchecked((byte)source)));
        var screenDelta = -source;
        state = state with { Y = nextY };
        return new(state, false, false, screenDelta, tableIndex);
    }

    public static (int TableUpdates, int MaxAscentPixels, int ApexUpdate, int NetScreenYDelta) JumpMetrics()
    {
        var y = 0;
        var minimum = 0;
        var apex = 0;
        for (var i = 0; i < UsedJumpSource.Length; i++)
        {
            y -= UsedJumpSource[i];
            if (y < minimum)
            {
                minimum = y;
                apex = i + 1;
            }
        }
        return (UsedJumpSource.Length, -minimum, apex, y);
    }

    private readonly record struct LandingResult(
        PlatformCommonEntityMotionState State,
        bool Landed,
        int ScreenYDelta);

    /// <summary>
    /// Fixed $C491 landing resolver. Exact acceptance rules:
    /// - entity Y must be below $86;
    /// - ground descriptor must be >= $A8;
    /// - descriptor < $F0 lands only when Y low nibble < 6, snapping to row;
    /// - descriptor >= $F0 lands only when Y low nibble >= 8, snapping to row+8.
    /// On landing phase clears; types $08/$09/$0C return to state $00, all other
    /// types return to state $10.
    /// </summary>
    private static LandingResult TryLand(PlatformCommonEntityMotionState state)
    {
        if (state.Y >= 0x86 || state.GroundDescriptor < 0xA8)
            return new(state, false, 0);

        byte snappedY;
        var low = state.Y & 0x0F;
        if (state.GroundDescriptor >= 0xF0)
        {
            if (low < 8)
                return new(state, false, 0);
            snappedY = (byte)((state.Y & 0xF0) | 0x08);
        }
        else
        {
            if (low >= 6)
                return new(state, false, 0);
            snappedY = (byte)(state.Y & 0xF0);
        }

        var nextAction = state.Type is 0x08 or 0x09 or 0x0C
            ? (byte)0x00
            : (byte)0x10;
        var delta = snappedY - state.Y;

        return new(
            state with
            {
                Y = snappedY,
                StatePhase = 0,
                ActionState = nextAction,
            },
            true,
            delta);
    }
}

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformEntity08090CActiveRoute
{
    Idle00,
    HitReaction40,
    Fall50,
    DeathD0,
}

public enum PlatformEntity08090CContinuation
{
    ReadyForInteraction,
    SkipInteraction,
    Removed,
}

public readonly record struct PlatformEntity08090CPreResult(
    PlatformCommonEntityMotionState State,
    PlatformEntity08090CActiveRoute Route,
    PlatformEntity08090CContinuation Continuation,
    bool ProximityFallStarted,
    bool ReactionAdvanced,
    bool ReactionCompleted,
    bool Landed,
    bool DeathPhaseAdvanced,
    int BobYDelta,
    int ScreenXDeltaFromCamera);

public readonly record struct PlatformEntity08090CPostResult(
    PlatformCommonEntityMotionState State,
    bool ReactionAdvanced,
    bool ReactionCompleted,
    bool DeathPhaseAdvanced,
    bool Removed,
    int BobYDelta);

/// <summary>
/// Exact active-path reduction for platform entity types $08, $09 and $0C,
/// beginning at the shared $A55E portion of bank 3.
///
/// The earlier $A442-$A55B attack-trigger/orientation prelude is deliberately a
/// separate target. Given the state that reaches $A55E, this class closes:
/// - idle family $00 with camera-relative X and optional type-$0C proximity fall;
/// - hit reaction $40 with terminal return to $00 and no $A845 recoil;
/// - fall $50 with fixed $C491 landing back to $00;
/// - death $D0-$DF at the normal 4-frame cadence;
/// - type-$0C vertical bob at $A7D0, after $40 handling and before $D0 cadence.
/// </summary>
public static class PlatformEntityTypes08090C
{
    private static readonly sbyte[] Type0CBob = [1, 1, -1, -1];

    public static PlatformEntity08090CPreResult StepPreInteraction(
        PlatformCommonEntityMotionState state,
        byte playerX,
        byte playerY,
        byte frameCounter3C,
        byte cameraDelta43)
    {
        ValidateType(state.Type);
        var family = (byte)(state.ActionState & 0xF0);

        return family switch
        {
            0x00 => StepIdle(state, playerX, playerY, cameraDelta43),
            0x40 => StepFrameStartReaction(state, frameCounter3C, cameraDelta43),
            0x50 => StepFall(state, cameraDelta43),
            0xD0 => StepFrameStartDeath(state, frameCounter3C, cameraDelta43),
            _ => throw new InvalidOperationException(
                $"Types $08/$09/$0C basic active helper does not model action ${state.ActionState:X2}.")
        };
    }

    /// <summary>
    /// Late $A79E -> $A7D0 -> $A7FB sequence after $9915/$98BA. This is used
    /// for idle entries that were allowed to interact. A hit-created $40/$D0
    /// can therefore advance in the same frame.
    /// </summary>
    public static PlatformEntity08090CPostResult AdvanceAfterInteraction(
        PlatformCommonEntityMotionState state,
        byte frameCounter3C)
    {
        ValidateType(state.Type);

        var reactionAdvanced = false;
        var reactionCompleted = false;
        if ((state.ActionState & 0xF0) == 0x40)
        {
            (state, reactionCompleted) = AdvanceReaction40(state);
            reactionAdvanced = true;
        }

        var bob = ApplyType0CBob(state, frameCounter3C);
        state = bob.State;

        var deathAdvanced = false;
        var removed = false;
        if ((state.ActionState & 0xF0) == 0xD0)
        {
            var death = AdvanceDeathD0(state, frameCounter3C);
            state = death.State;
            deathAdvanced = death.Advanced;
            removed = death.Removed;
        }

        return new(
            state,
            reactionAdvanced,
            reactionCompleted,
            deathAdvanced,
            removed,
            bob.Delta);
    }

    private static PlatformEntity08090CPreResult StepIdle(
        PlatformCommonEntityMotionState state,
        byte playerX,
        byte playerY,
        byte cameraDelta43)
    {
        var screen = ApplyScreenPath(state, cameraDelta43);
        state = screen.State;
        if (screen.Removed)
        {
            return new(
                state,
                PlatformEntity08090CActiveRoute.Idle00,
                PlatformEntity08090CContinuation.Removed,
                false, false, false, false, false, 0,
                -cameraDelta43);
        }

        var proximity = false;
        if (state.Type == 0x0C
            && state.StatePhase == 0
            && GroundAllowsProximity(state.GroundDescriptor)
            && playerY < 0x81)
        {
            var playerRow = (byte)(playerY & 0xF0);
            if (playerRow > state.Y
                && state.X is >= 0x21 and < 0xC0
                && WithinHorizontalProximity(state.X, playerX))
            {
                state = state with
                {
                    ActionState = 0x50,
                    Y = unchecked((byte)(state.Y + 6)),
                };
                proximity = true;
            }
        }

        // $08/$09 are explicitly excluded at $A6AA-$A6B4; type $0C is not.
        return new(
            state,
            PlatformEntity08090CActiveRoute.Idle00,
            PlatformEntity08090CContinuation.ReadyForInteraction,
            proximity,
            false,
            false,
            false,
            false,
            0,
            -cameraDelta43);
    }

    private static PlatformEntity08090CPreResult StepFrameStartReaction(
        PlatformCommonEntityMotionState state,
        byte frameCounter3C,
        byte cameraDelta43)
    {
        var screen = ApplyScreenPath(state, cameraDelta43);
        state = screen.State;
        if (screen.Removed)
        {
            return new(
                state,
                PlatformEntity08090CActiveRoute.HitReaction40,
                PlatformEntity08090CContinuation.Removed,
                false, false, false, false, false, 0,
                -cameraDelta43);
        }

        (state, var completed) = AdvanceReaction40(state);
        var bob = ApplyType0CBob(state, frameCounter3C);
        state = bob.State;

        return new(
            state,
            PlatformEntity08090CActiveRoute.HitReaction40,
            PlatformEntity08090CContinuation.SkipInteraction,
            false,
            ReactionAdvanced: true,
            ReactionCompleted: completed,
            Landed: false,
            DeathPhaseAdvanced: false,
            BobYDelta: bob.Delta,
            ScreenXDeltaFromCamera: -cameraDelta43);
    }

    private static PlatformEntity08090CPreResult StepFall(
        PlatformCommonEntityMotionState state,
        byte cameraDelta43)
    {
        state = state with
        {
            X = unchecked((byte)(state.X - cameraDelta43)),
            Y = unchecked((byte)(state.Y + 3)),
        };

        if (state.Y >= 0xB0)
        {
            return new(
                state,
                PlatformEntity08090CActiveRoute.Fall50,
                PlatformEntity08090CContinuation.Removed,
                false, false, false, false, false, 0,
                -cameraDelta43);
        }

        var landing = PlatformCommonEntityLanding.Resolve(state);
        state = landing.State;

        return new(
            state,
            PlatformEntity08090CActiveRoute.Fall50,
            PlatformEntity08090CContinuation.SkipInteraction,
            false,
            false,
            false,
            landing.Landed,
            false,
            0,
            -cameraDelta43);
    }

    private static PlatformEntity08090CPreResult StepFrameStartDeath(
        PlatformCommonEntityMotionState state,
        byte frameCounter3C,
        byte cameraDelta43)
    {
        var screen = ApplyScreenPath(state, cameraDelta43);
        state = screen.State;
        if (screen.Removed)
        {
            return new(
                state,
                PlatformEntity08090CActiveRoute.DeathD0,
                PlatformEntity08090CContinuation.Removed,
                false, false, false, false, false, 0,
                -cameraDelta43);
        }

        var bob = ApplyType0CBob(state, frameCounter3C);
        state = bob.State;
        var death = AdvanceDeathD0(state, frameCounter3C);
        state = death.State;

        return new(
            state,
            PlatformEntity08090CActiveRoute.DeathD0,
            death.Removed
                ? PlatformEntity08090CContinuation.Removed
                : PlatformEntity08090CContinuation.SkipInteraction,
            false,
            false,
            false,
            false,
            death.Advanced,
            bob.Delta,
            -cameraDelta43);
    }

    private readonly record struct ScreenPathResult(
        PlatformCommonEntityMotionState State,
        bool Removed);

    private static ScreenPathResult ApplyScreenPath(
        PlatformCommonEntityMotionState state,
        byte cameraDelta43)
    {
        state = state with
        {
            X = unchecked((byte)(state.X - cameraDelta43)),
        };

        if (state.X >= 0xF8)
            return new(state, true);
        if (state.Y is >= 0xB0 and < 0xC0)
            return new(state, true);
        return new(state, false);
    }

    private static (PlatformCommonEntityMotionState State, bool Completed) AdvanceReaction40(
        PlatformCommonEntityMotionState state)
    {
        var next = unchecked((byte)(state.ActionState + 1));
        var completed = next >= 0x50;
        state = state with { ActionState = completed ? (byte)0x00 : next };
        return (state, completed);
    }

    private readonly record struct BobResult(
        PlatformCommonEntityMotionState State,
        int Delta);

    private static BobResult ApplyType0CBob(
        PlatformCommonEntityMotionState state,
        byte frameCounter3C)
    {
        if (state.Type != 0x0C || (frameCounter3C & 0x07) != 0)
            return new(state, 0);

        var index = (frameCounter3C >> 3) & 0x03;
        var delta = Type0CBob[index];
        state = state with
        {
            Y = unchecked((byte)(state.Y + delta)),
        };
        return new(state, delta);
    }

    private readonly record struct DeathAdvanceResult(
        PlatformCommonEntityMotionState State,
        bool Advanced,
        bool Removed);

    private static DeathAdvanceResult AdvanceDeathD0(
        PlatformCommonEntityMotionState state,
        byte frameCounter3C)
    {
        // Types $08/$09/$0C take the normal A801-$A805 cadence gate.
        if ((frameCounter3C & 0x03) != 0)
            return new(state, false, false);

        if (state.GroundDescriptor < 0x80 || state.GroundDescriptor >= 0xF0)
        {
            state = state with { Y = unchecked((byte)(state.Y + 2)) };
        }

        var next = unchecked((byte)(state.ActionState + 1));
        if (next >= 0xE0)
        {
            state = state with { ActionState = 0x00 };
            return new(state, true, true);
        }

        state = state with { ActionState = next };
        return new(state, true, false);
    }

    private static bool GroundAllowsProximity(byte descriptor) =>
        descriptor < 0xE0 || descriptor >= 0xF0;

    private static bool WithinHorizontalProximity(byte entityX, byte playerX)
    {
        if (entityX >= playerX)
        {
            var left = unchecked((byte)(entityX - 0x20));
            return left < playerX;
        }

        if (playerX < 0x20)
            return false;
        var right = unchecked((byte)(entityX + 0x20));
        return right >= playerX;
    }

    private static void ValidateType(byte type)
    {
        if (type is not (0x08 or 0x09 or 0x0C))
            throw new ArgumentOutOfRangeException(nameof(type), type, "Helper covers entity types $08/$09/$0C only.");
    }
}

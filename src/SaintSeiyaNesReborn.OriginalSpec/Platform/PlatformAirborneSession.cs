namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformAirborneSessionState(
    PlatformHorizontalState Horizontal,
    byte PlayerY,
    byte PlayerYHigh41,
    byte ActionState,
    byte JumpPhase49,
    byte HighJumpSelector038A,
    byte Support038D,
    byte Special76,
    byte FrameCounter3C);

public readonly record struct PlatformAirborneSessionResult(
    PlatformAirborneSessionState State,
    PlatformCollisionDescriptors Probes,
    PlatformAirborneVerticalResult Vertical,
    PlatformAirborneHorizontalResult? Horizontal,
    bool FrameCounterAdvanced);

/// <summary>
/// One complete existing-jump player simulation slice in the order reconstructed
/// from the active platform frame and bank 3 $BCD3+:
/// pre-movement probes -> pre-increment landing -> vertical step -> ceiling gate
/// -> horizontal air control -> end-of-frame counter advance.
///
/// Jump initiation itself is intentionally a separate concern; this session starts
/// with a non-zero $49 exactly as $BCD3 does after $BB76 has created a jump.
/// </summary>
public static class PlatformAirborneSession
{
    public static PlatformAirborneSessionResult Step(
        PlatformStageMap stage,
        PlatformSaintIndex saint,
        PlatformAirborneSessionState state,
        PlatformInput input,
        byte dynamicFloorY039B = 0)
    {
        if (state.JumpPhase49 == 0)
            throw new ArgumentException("Airborne session requires a non-zero jump phase.", nameof(state));
        if (PlatformActionState.Family(state.ActionState) != (byte)PlatformActionFamily.Jump)
            throw new ArgumentException("Airborne session requires jump-family action state.", nameof(state));

        // The original collision sampler runs before bank-3 player simulation.
        var probes = stage.SamplePlayer(
            state.Horizontal.PlayerX,
            state.PlayerY,
            state.Horizontal.ScrollX);

        var verticalState = new PlatformAirborneVerticalState(
            state.PlayerY,
            state.PlayerYHigh41,
            state.ActionState,
            state.JumpPhase49,
            state.HighJumpSelector038A,
            state.Support038D,
            state.Special76);

        var vertical = PlatformAirborneVerticalMotion.Step(
            saint,
            verticalState,
            probes,
            dynamicFloorY039B);

        var horizontalState = state.Horizontal;
        var actionState = vertical.State.ActionState;
        PlatformAirborneHorizontalResult? horizontal = null;

        if (vertical.ContinueHorizontal)
        {
            var increments = PlatformMovementIncrements.FromFrame(saint, state.FrameCounter3C);
            var horizontalResult = PlatformAirborneHorizontalMotion.Step(
                stage,
                horizontalState,
                input,
                probes,
                actionState,
                vertical.State.JumpPhase49,
                (byte)vertical.HalfPhase,
                increments,
                state.FrameCounter3C);
            horizontal = horizontalResult;
            horizontalState = horizontalResult.State;
            actionState = horizontalResult.ActionState;
        }

        var next = new PlatformAirborneSessionState(
            horizontalState,
            vertical.State.PlayerY,
            vertical.State.PlayerYHigh41,
            actionState,
            vertical.State.JumpPhase49,
            vertical.State.HighJumpSelector038A,
            vertical.State.Support038D,
            vertical.State.Special76,
            unchecked((byte)(state.FrameCounter3C + 1)));

        return new PlatformAirborneSessionResult(next, probes, vertical, horizontal, true);
    }
}

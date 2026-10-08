namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

/// <summary>
/// Minimal executable state for the original grounded platform slice.
/// Vertical/jump/attack state is intentionally outside this first composition.
/// </summary>
public readonly record struct PlatformGroundedState(
    PlatformSaintIndex Saint,
    byte PlayerY,
    byte FrameCounter3C,
    PlatformHorizontalState Horizontal);

public readonly record struct PlatformGroundedFrameResult(
    PlatformGroundedState State,
    PlatformCollisionDescriptors Probes,
    PlatformHorizontalStepResult? HorizontalStep,
    PlatformExitTransitionKind? ExitTransition)
{
    public bool Exited => ExitTransition is not null;
}

/// <summary>
/// Clean-room composition of the already reconstructed grounded platform frame.
///
/// Order matches the original fixed-bank loop:
/// 1. evaluate the platform exit gate ($C312 -> bank 1 $969D);
/// 2. if still in platform mode, sample the stage and run bank-3 horizontal motion;
/// 3. advance frame counter $3C for the next frame.
///
/// An exit reached by movement this frame therefore fires on the next frame,
/// matching the original one-frame ordering.
/// </summary>
public static class PlatformGroundedSession
{
    public static PlatformGroundedFrameResult Step(
        PlatformStageMap stage,
        PlatformGroundedState state,
        PlatformInput input)
    {
        var exit = PlatformExitGate.Evaluate(
            stage.Substate,
            state.Saint,
            state.Horizontal.PlayerX,
            state.PlayerY,
            jumpPhase: 0);

        if (exit is not null)
        {
            return new PlatformGroundedFrameResult(
                State: state,
                Probes: default,
                HorizontalStep: null,
                ExitTransition: exit);
        }

        var probes = stage.SamplePlayer(
            state.Horizontal.PlayerX,
            state.PlayerY,
            state.Horizontal.ScrollX);

        var increments = PlatformMovementIncrements.FromFrame(state.Saint, state.FrameCounter3C);
        var horizontal = PlatformHorizontalMotion.StepGrounded(
            stage,
            state.Horizontal,
            input,
            probes,
            increments.Grounded0387);

        var next = state with
        {
            Horizontal = horizontal.State,
            FrameCounter3C = unchecked((byte)(state.FrameCounter3C + 1)),
        };

        return new PlatformGroundedFrameResult(
            State: next,
            Probes: probes,
            HorizontalStep: horizontal,
            ExitTransition: null);
    }
}

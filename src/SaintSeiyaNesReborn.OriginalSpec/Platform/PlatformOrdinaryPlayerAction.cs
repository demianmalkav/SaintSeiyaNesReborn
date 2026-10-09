using SaintSeiyaNesReborn.OriginalSpec;

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformOrdinaryPlayerRoute
{
    Grounded,
    Airborne,
}

public readonly record struct PlatformOrdinaryPlayerActionState(
    PlatformSaintIndex Saint,
    PlatformHorizontalState Horizontal,
    byte PlayerY,
    byte PlayerYHigh41,
    byte ActionState4D,
    byte JumpPhase49,
    byte JumpButtonLatch4A,
    byte HighJumpSelector038A,
    byte Support038D,
    byte Special76,
    PlatformAttackState AttackState);

public readonly record struct PlatformOrdinaryPlayerActionResult(
    PlatformOrdinaryPlayerActionState State,
    byte FrameStartAction4E,
    PlatformCollisionDescriptors Probes,
    PlatformJumpInitiationResult JumpInitiation,
    PlatformAttackAttemptResult AttackAttempt,
    PlatformOrdinaryPlayerRoute Route,
    PlatformAirborneVerticalResult? AirborneVertical,
    PlatformAirborneHorizontalResult? AirborneHorizontal,
    PlatformHorizontalStepResult? GroundedHorizontal);

/// <summary>
/// Clean-room composition of the ordinary $AAE4 player-action route.
///
/// This deliberately stops before the later attack-object/entity pipeline and
/// before fixed-bank $C402 increments frame counter $3C. `frameCounter3C` is
/// therefore an input only. That boundary is required for exact full-frame
/// composition later.
///
/// Ordering reproduced here:
///   frame-start $4D->$4E already captured by caller
///   pre-simulation collision probes
///   $BB76 A/jump logic
///   $BBCA B/attack creation using OLD $4E but updated jump phase
///   if current action is $30-$3F: same-frame $BCD3 vertical + air control
///   otherwise: $AB3F grounded movement/action-family update
/// </summary>
public static class PlatformOrdinaryPlayerAction
{
    public static PlatformOrdinaryPlayerActionResult Step(
        PlatformStageMap stage,
        PlatformOrdinaryPlayerActionState state,
        PlatformInput input,
        byte frameCounter3C,
        int cosmo,
        byte engineSubstate01 = 0,
        byte dynamicFloorY039B = 0)
    {
        var frameStartAction4E = state.ActionState4D;

        // Collision samples are prepared before $AAE4 and are therefore shared by
        // jump vertical/air-control or the grounded branch in this update.
        var probes = stage.SamplePlayer(
            state.Horizontal.PlayerX,
            state.PlayerY,
            state.Horizontal.ScrollX);

        var jumpState = new PlatformJumpInitiationState(
            state.ActionState4D,
            state.JumpPhase49,
            state.JumpButtonLatch4A,
            state.HighJumpSelector038A,
            state.Support038D);
        var jump = PlatformJumpInitiation.Step(jumpState, input);

        state = state with
        {
            ActionState4D = jump.State.ActionState4D,
            JumpPhase49 = jump.State.JumpPhase49,
            JumpButtonLatch4A = jump.State.JumpButtonLatch4A,
            HighJumpSelector038A = jump.State.HighJumpSelector038A,
        };

        // $BBCA is physically inside/follows $BB76 and executes before the caller
        // decides whether the updated action enters $BCD3. The attack receives the
        // OLD frame-start $4E but the NEW $49 created by A this same frame.
        var attackInputState = state.AttackState with { ActionState4D = state.ActionState4D };
        var attack = PlatformAttackSystem.ApplyBButton(
            attackInputState,
            state.Saint,
            input,
            state.PlayerY,
            state.Horizontal.PlayerX,
            state.Horizontal.Facing42,
            frameStartAction4E,
            state.JumpPhase49,
            cosmo,
            engineSubstate01);

        state = state with
        {
            ActionState4D = attack.State.ActionState4D,
            AttackState = attack.State,
        };

        if (PlatformActionState.Family(state.ActionState4D) == (byte)PlatformActionFamily.Jump)
        {
            var verticalState = new PlatformAirborneVerticalState(
                state.PlayerY,
                state.PlayerYHigh41,
                state.ActionState4D,
                state.JumpPhase49,
                state.HighJumpSelector038A,
                state.Support038D,
                state.Special76);

            var vertical = PlatformAirborneVerticalMotion.Step(
                state.Saint,
                verticalState,
                probes,
                dynamicFloorY039B);

            var horizontalState = state.Horizontal;
            var actionState = vertical.State.ActionState;
            PlatformAirborneHorizontalResult? airborneHorizontal = null;

            if (vertical.ContinueHorizontal)
            {
                var increments = PlatformMovementIncrements.FromFrame(state.Saint, frameCounter3C);
                var horizontal = PlatformAirborneHorizontalMotion.Step(
                    stage,
                    horizontalState,
                    input,
                    probes,
                    actionState,
                    vertical.State.JumpPhase49,
                    (byte)vertical.HalfPhase,
                    increments,
                    frameCounter3C);
                airborneHorizontal = horizontal;
                horizontalState = horizontal.State;
                actionState = horizontal.ActionState;
            }

            var finalAttack = attack.State with { ActionState4D = actionState };
            var next = state with
            {
                Horizontal = horizontalState,
                PlayerY = vertical.State.PlayerY,
                PlayerYHigh41 = vertical.State.PlayerYHigh41,
                ActionState4D = actionState,
                JumpPhase49 = vertical.State.JumpPhase49,
                HighJumpSelector038A = vertical.State.HighJumpSelector038A,
                Support038D = vertical.State.Support038D,
                Special76 = vertical.State.Special76,
                AttackState = finalAttack,
            };

            return new PlatformOrdinaryPlayerActionResult(
                next,
                frameStartAction4E,
                probes,
                jump,
                attack,
                PlatformOrdinaryPlayerRoute.Airborne,
                vertical,
                airborneHorizontal,
                null);
        }

        var movementIncrements = PlatformMovementIncrements.FromFrame(state.Saint, frameCounter3C);
        var grounded = PlatformHorizontalMotion.StepGrounded(
            stage,
            state.Horizontal,
            input,
            probes,
            movementIncrements.Grounded0387);

        var groundedAction = ResolveGroundedAction(
            state.ActionState4D,
            input,
            grounded);
        var groundedAttack = attack.State with { ActionState4D = groundedAction };
        var groundedNext = state with
        {
            Horizontal = grounded.State,
            ActionState4D = groundedAction,
            AttackState = groundedAttack,
        };

        return new PlatformOrdinaryPlayerActionResult(
            groundedNext,
            frameStartAction4E,
            probes,
            jump,
            attack,
            PlatformOrdinaryPlayerRoute.Grounded,
            null,
            null,
            grounded);
    }

    /// <summary>
    /// $AB3F/$ABC0/$AC3B action-state side effects after the horizontal helper.
    /// Right has already won directional priority inside StepGrounded.
    /// </summary>
    public static byte ResolveGroundedAction(
        byte currentAction,
        PlatformInput input,
        PlatformHorizontalStepResult horizontal)
    {
        var hasRight = (input & PlatformInput.Right) != 0;
        var hasLeft = (input & PlatformInput.Left) != 0;
        var hasDirection = hasRight || hasLeft;

        if (hasDirection)
        {
            // The left/right hard screen boundaries route through $AC37 and clear
            // action. Normal movement, camera handoff, and terrain collision all
            // route through $ABC0 and set family $10 while preserving low nibble.
            currentAction = horizontal.EdgeBlocked
                ? (byte)0
                : (byte)((currentAction & 0x0F) | 0x10);
        }

        // $AC3B: Down overrides the locomotion result.
        if ((input & PlatformInput.Down) != 0)
            return 0x20;

        // The tail masks only Right/Left/Down ($07); Up/A/B alone are idle here.
        if (((byte)input & 0x07) == 0)
            return 0;

        return currentAction;
    }
}

using SaintSeiyaNesReborn.OriginalSpec;

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformPlayerActionRoute
{
    OrdinaryGrounded,
    OrdinaryAirborne,
    Crouch,
    Special40,
    Fall,
    UnsupportedDamage80,
}

public readonly record struct PlatformPlayerActionState(
    PlatformSaintIndex Saint,
    PlatformHorizontalState Horizontal,
    byte PlayerY,
    byte PlayerYHigh41,
    byte ActionState4D,
    byte JumpPhase49,
    byte JumpButtonLatch4A,
    byte HighJumpSelector038A,
    byte DropButtonLatch038C,
    byte Support038D,
    byte Special76,
    byte HorizontalAmount43,
    PlatformAttackState AttackState);

public readonly record struct PlatformPlayerActionDispatchResult(
    PlatformPlayerActionState State,
    byte FrameStartAction4E,
    PlatformPlayerActionRoute Route,
    PlatformCollisionDescriptors Probes,
    PlatformOrdinaryPlayerActionResult? Ordinary,
    PlatformCrouchStepResult? Crouch,
    PlatformSpecial40StepResult? Special40,
    PlatformFallStepResult? Fall,
    PlatformAttackAttemptResult? AttackAttempt)
{
    public bool IsModeled => Route != PlatformPlayerActionRoute.UnsupportedDamage80;
}

/// <summary>
/// Frame-start action dispatcher corresponding to the reconstructed portion of
/// PRG bank 3 $AAE4. It composes every branch whose semantics are already known:
/// ordinary/default, crouch $20, special cycle $40, and fall/drop $50.
///
/// $80 is surfaced explicitly as unsupported rather than being routed through
/// ordinary behavior. This layer also stops before the later object pipeline and
/// before global frame-counter $3C is advanced.
/// </summary>
public static class PlatformPlayerActionDispatcher
{
    public static PlatformPlayerActionDispatchResult Step(
        PlatformStageMap stage,
        PlatformPlayerActionState state,
        PlatformInput input,
        byte frameCounter3C,
        int cosmo,
        byte engineSubstate01 = 0,
        byte dynamicFloorY039B = 0)
    {
        var frameStartAction4E = state.ActionState4D;
        var family = PlatformActionState.Family(frameStartAction4E);

        if (family == (byte)PlatformActionFamily.DamageOrHazard)
            return Unsupported(stage, state, frameStartAction4E, PlatformPlayerActionRoute.UnsupportedDamage80);

        if (family == (byte)PlatformActionFamily.Crouch)
            return StepCrouch(
                stage,
                state,
                input,
                frameCounter3C,
                cosmo,
                engineSubstate01,
                frameStartAction4E);

        if (family == (byte)PlatformActionFamily.Special40)
            return StepSpecial40(stage, state, frameStartAction4E);

        if (family == (byte)PlatformActionFamily.FallOrDrop)
            return StepFall(
                stage,
                state,
                input,
                frameCounter3C,
                cosmo,
                engineSubstate01,
                dynamicFloorY039B,
                frameStartAction4E);

        var ordinaryState = new PlatformOrdinaryPlayerActionState(
            state.Saint,
            state.Horizontal,
            state.PlayerY,
            state.PlayerYHigh41,
            state.ActionState4D,
            state.JumpPhase49,
            state.JumpButtonLatch4A,
            state.HighJumpSelector038A,
            state.Support038D,
            state.Special76,
            state.AttackState);

        var ordinary = PlatformOrdinaryPlayerAction.Step(
            stage,
            ordinaryState,
            input,
            frameCounter3C,
            cosmo,
            engineSubstate01,
            dynamicFloorY039B);

        var next = state with
        {
            Horizontal = ordinary.State.Horizontal,
            PlayerY = ordinary.State.PlayerY,
            PlayerYHigh41 = ordinary.State.PlayerYHigh41,
            ActionState4D = ordinary.State.ActionState4D,
            JumpPhase49 = ordinary.State.JumpPhase49,
            JumpButtonLatch4A = ordinary.State.JumpButtonLatch4A,
            HighJumpSelector038A = ordinary.State.HighJumpSelector038A,
            Support038D = ordinary.State.Support038D,
            Special76 = ordinary.State.Special76,
            AttackState = ordinary.State.AttackState,
        };

        return new PlatformPlayerActionDispatchResult(
            next,
            frameStartAction4E,
            ordinary.Route == PlatformOrdinaryPlayerRoute.Airborne
                ? PlatformPlayerActionRoute.OrdinaryAirborne
                : PlatformPlayerActionRoute.OrdinaryGrounded,
            ordinary.Probes,
            ordinary,
            null,
            null,
            null,
            ordinary.AttackAttempt);
    }

    private static PlatformPlayerActionDispatchResult StepCrouch(
        PlatformStageMap stage,
        PlatformPlayerActionState state,
        PlatformInput input,
        byte frameCounter3C,
        int cosmo,
        byte engineSubstate01,
        byte frameStartAction4E)
    {
        var probes = stage.SamplePlayer(
            state.Horizontal.PlayerX,
            state.PlayerY,
            state.Horizontal.ScrollX);

        var crouchState = new PlatformCrouchDropState(
            state.Horizontal,
            state.PlayerY,
            state.PlayerYHigh41,
            state.ActionState4D,
            state.DropButtonLatch038C,
            state.JumpPhase49,
            state.Support038D,
            state.Special76,
            state.HorizontalAmount43);

        // $AAE4: JSR $B829 first, then JSR $BBCA.
        var crouch = PlatformCrouchDrop.StepCrouched(crouchState, input, probes);
        var afterCrouch = crouch.State;

        var attackInput = state.AttackState with { ActionState4D = afterCrouch.ActionState4D };
        var attack = PlatformAttackSystem.ApplyBButton(
            attackInput,
            state.Saint,
            input,
            afterCrouch.PlayerY,
            afterCrouch.Horizontal.PlayerX,
            afterCrouch.Horizontal.Facing42,
            frameStartAction4E,
            afterCrouch.JumpPhase49,
            cosmo,
            engineSubstate01);

        var next = state with
        {
            Horizontal = afterCrouch.Horizontal,
            PlayerY = afterCrouch.PlayerY,
            PlayerYHigh41 = afterCrouch.PlayerYPage41,
            ActionState4D = attack.State.ActionState4D,
            JumpPhase49 = afterCrouch.JumpPhase49,
            DropButtonLatch038C = afterCrouch.DropButtonLatch038C,
            Support038D = afterCrouch.JumpLock038D,
            Special76 = afterCrouch.HazardFlag76,
            HorizontalAmount43 = afterCrouch.HorizontalAmount43,
            AttackState = attack.State,
        };

        return new PlatformPlayerActionDispatchResult(
            next,
            frameStartAction4E,
            PlatformPlayerActionRoute.Crouch,
            probes,
            null,
            crouch,
            null,
            null,
            attack);
    }

    private static PlatformPlayerActionDispatchResult StepSpecial40(
        PlatformStageMap stage,
        PlatformPlayerActionState state,
        byte frameStartAction4E)
    {
        var probes = stage.SamplePlayer(
            state.Horizontal.PlayerX,
            state.PlayerY,
            state.Horizontal.ScrollX);

        // $AAE4: JSR $C5CC then return. No B/attack or grounded/airborne path.
        var special = PlatformSpecial40Motion.Step(
            new PlatformSpecial40State(state.PlayerY, state.ActionState4D));

        var next = state with
        {
            PlayerY = special.State.PlayerY,
            ActionState4D = special.State.ActionState4D,
            AttackState = state.AttackState with { ActionState4D = special.State.ActionState4D },
        };

        return new PlatformPlayerActionDispatchResult(
            next,
            frameStartAction4E,
            PlatformPlayerActionRoute.Special40,
            probes,
            null,
            null,
            special,
            null,
            null);
    }

    private static PlatformPlayerActionDispatchResult StepFall(
        PlatformStageMap stage,
        PlatformPlayerActionState state,
        PlatformInput input,
        byte frameCounter3C,
        int cosmo,
        byte engineSubstate01,
        byte dynamicFloorY039B,
        byte frameStartAction4E)
    {
        var probes = stage.SamplePlayer(
            state.Horizontal.PlayerX,
            state.PlayerY,
            state.Horizontal.ScrollX);

        var fallState = new PlatformCrouchDropState(
            state.Horizontal,
            state.PlayerY,
            state.PlayerYHigh41,
            state.ActionState4D,
            state.DropButtonLatch038C,
            state.JumpPhase49,
            state.Support038D,
            state.Special76,
            state.HorizontalAmount43);

        // $AAE4: JSR $B87D first, then JSR $BBCA.
        var fall = PlatformCrouchDrop.StepFall(
            stage,
            fallState,
            state.Saint,
            probes,
            frameCounter3C,
            dynamicFloorY039B);
        var afterFall = fall.State;

        var attackInput = state.AttackState with { ActionState4D = afterFall.ActionState4D };
        var attack = PlatformAttackSystem.ApplyBButton(
            attackInput,
            state.Saint,
            input,
            afterFall.PlayerY,
            afterFall.Horizontal.PlayerX,
            afterFall.Horizontal.Facing42,
            frameStartAction4E,
            afterFall.JumpPhase49,
            cosmo,
            engineSubstate01);

        var next = state with
        {
            Horizontal = afterFall.Horizontal,
            PlayerY = afterFall.PlayerY,
            PlayerYHigh41 = afterFall.PlayerYPage41,
            ActionState4D = attack.State.ActionState4D,
            JumpPhase49 = afterFall.JumpPhase49,
            DropButtonLatch038C = afterFall.DropButtonLatch038C,
            Support038D = afterFall.JumpLock038D,
            Special76 = afterFall.HazardFlag76,
            HorizontalAmount43 = afterFall.HorizontalAmount43,
            AttackState = attack.State,
        };

        return new PlatformPlayerActionDispatchResult(
            next,
            frameStartAction4E,
            PlatformPlayerActionRoute.Fall,
            probes,
            null,
            null,
            null,
            fall,
            attack);
    }

    private static PlatformPlayerActionDispatchResult Unsupported(
        PlatformStageMap stage,
        PlatformPlayerActionState state,
        byte frameStartAction4E,
        PlatformPlayerActionRoute route)
    {
        var probes = stage.SamplePlayer(
            state.Horizontal.PlayerX,
            state.PlayerY,
            state.Horizontal.ScrollX);
        return new PlatformPlayerActionDispatchResult(
            state,
            frameStartAction4E,
            route,
            probes,
            null,
            null,
            null,
            null,
            null);
    }
}

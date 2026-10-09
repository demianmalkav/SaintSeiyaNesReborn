using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class PlayerActionDispatcherChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        var open = OpenStage();

        var ordinary = PlatformPlayerActionDispatcher.Step(
            open,
            BaseState(action: 0x00, x: 0x40, y: 0x60),
            PlatformInput.Right,
            frameCounter3C: 0,
            cosmo: 100);
        Require(ordinary.Route == PlatformPlayerActionRoute.OrdinaryGrounded, "neutral frame dispatches ordinary grounded");
        Require(ordinary.State.Horizontal.PlayerX == 0x41, "ordinary dispatcher preserves grounded movement");
        Require(ordinary.State.ActionState4D == 0x10, "ordinary dispatcher preserves locomotion action");

        var activeJump = PlatformPlayerActionDispatcher.Step(
            open,
            BaseState(action: 0x31, x: 0x40, y: 0x60, jumpPhase: 8, facing: 0x40),
            PlatformInput.Right,
            frameCounter3C: 1,
            cosmo: 100);
        Require(activeJump.Route == PlatformPlayerActionRoute.OrdinaryAirborne, "jump-family frame dispatches ordinary airborne");
        Require(activeJump.State.JumpPhase49 == 9, "dispatcher advances existing jump phase");

        // $20 path: B is processed only after crouch/drop. Down+A first moves the
        // player six pixels into $50; B then sees the new Y, while its origin rule
        // still receives OLD frame-start $4E==$20 and adds the crouch +8 offset.
        var crouchDropAttack = PlatformPlayerActionDispatcher.Step(
            open,
            BaseState(action: 0x20, x: 0x40, y: 0x50, facing: 0x40),
            PlatformInput.Down | PlatformInput.A | PlatformInput.B,
            frameCounter3C: 0,
            cosmo: 100);
        Require(crouchDropAttack.Route == PlatformPlayerActionRoute.Crouch, "$20 dispatches crouch route");
        Require(crouchDropAttack.Crouch?.DropStarted == true, "Down+A starts drop before B");
        Require(crouchDropAttack.State.PlayerY == 0x56, "crouch drop applies +6 Y before attack");
        Require(crouchDropAttack.State.ActionState4D == 0x50, "drop action remains $50 after attack creation");
        Require(crouchDropAttack.AttackAttempt?.Outcome == PlatformAttackAttemptOutcome.Created, "B creates attack after drop");
        Require(crouchDropAttack.State.AttackState.Slot0.Object.Y == 0x65, "attack origin uses post-drop Y plus old-$20 crouch offset");
        Require(crouchDropAttack.State.AttackState.Slot0.Object.X == 0x52, "crouch-path attack uses current X/facing");

        // Releasing Down exits crouch before B. The attack still sees old $4E==$20,
        // so the +15 crouching origin survives despite current action becoming zero.
        var crouchReleaseAttack = PlatformPlayerActionDispatcher.Step(
            open,
            BaseState(action: 0x20, x: 0x40, y: 0x50, facing: 0x40),
            PlatformInput.B,
            frameCounter3C: 0,
            cosmo: 100);
        Require(crouchReleaseAttack.State.ActionState4D == 0, "crouch release clears current action before B");
        Require(crouchReleaseAttack.State.AttackState.Slot0.Object.Y == 0x5F, "B origin still uses frame-start $20 snapshot");

        // Facing is updated by B829 before BBCA. Right wins over Left exactly as in
        // the crouch helper, so the projectile must use right-facing origin here.
        var crouchFacing = PlatformPlayerActionDispatcher.Step(
            open,
            BaseState(action: 0x20, x: 0x40, y: 0x50, facing: 0x00),
            PlatformInput.Down | PlatformInput.Right | PlatformInput.Left | PlatformInput.B,
            frameCounter3C: 0,
            cosmo: 100);
        Require(crouchFacing.State.Horizontal.Facing42 == 0x40, "crouch updates facing before B");
        Require(crouchFacing.State.AttackState.Slot0.Object.X == 0x52, "B uses post-crouch right facing");

        // $50 path: fall movement happens before B. Starting at $60 therefore
        // attacks from $63+7 rather than the frame-start Y.
        var fallAttack = PlatformPlayerActionDispatcher.Step(
            open,
            BaseState(action: 0x50, x: 0x40, y: 0x60, facing: 0x40),
            PlatformInput.B,
            frameCounter3C: 0,
            cosmo: 100);
        Require(fallAttack.Route == PlatformPlayerActionRoute.Fall, "$50 dispatches fall route");
        Require(fallAttack.Fall?.Outcome == PlatformFallOutcome.Falling, "open-space $50 remains falling");
        Require(fallAttack.State.PlayerY == 0x63, "fall applies +3 Y before B");
        Require(fallAttack.AttackAttempt?.Outcome == PlatformAttackAttemptOutcome.Created, "falling B creates attack after fall step");
        Require(fallAttack.State.AttackState.Slot0.Object.Y == 0x6A, "falling attack origin uses post-fall Y+7");

        // A frame can cross the $90 attack-height threshold because of the fall step.
        // That proves BBCA observes the post-B87D Y, not frame-start Y.
        var fallHeightReject = PlatformPlayerActionDispatcher.Step(
            open,
            BaseState(action: 0x50, x: 0x40, y: 0x8E, facing: 0x40),
            PlatformInput.B,
            frameCounter3C: 0,
            cosmo: 100);
        Require(fallHeightReject.State.PlayerY == 0x91, "fall crosses attack height threshold first");
        Require(fallHeightReject.AttackAttempt?.Outcome == PlatformAttackAttemptOutcome.HeightRejected, "post-fall Y rejects B at $90+");
        Require(fallHeightReject.AttackAttempt?.SoundId == 0x24, "height-rejected fall attack still requests sound");
        Require(fallHeightReject.State.AttackState.BButtonLatch4C == 1, "height rejection still consumes B latch");

        // FloorCenter samples world X+8 and Y+0x20 from the frame-start player
        // coordinates. Put a solid descriptor exactly under that probe.
        var solid = StageWithDescriptor(x: 0x48, y: 0x7D, descriptor: 0x80);
        var landAttack = PlatformPlayerActionDispatcher.Step(
            solid,
            BaseState(action: 0x50, x: 0x40, y: 0x5D, facing: 0x40),
            PlatformInput.B,
            frameCounter3C: 0,
            cosmo: 100);
        Require(landAttack.Fall?.Outcome == PlatformFallOutcome.Landed, "fall lands before B");
        Require(landAttack.State.PlayerY == 0x60, "landing snap precedes attack");
        Require(landAttack.State.ActionState4D == 0, "landing returns current action to neutral");
        Require(landAttack.State.AttackState.Slot0.Object.Y == 0x67, "post-landing B origin uses snapped Y");

        // Unknown dispatcher branches must remain explicit no-ops at this layer.
        var attackSeed = PlatformAttackState.Empty with { BButtonLatch4C = 7, Busy4B = 3 };
        var special40 = PlatformPlayerActionDispatcher.Step(
            open,
            BaseState(action: 0x40, x: 0x40, y: 0x60, attack: attackSeed),
            PlatformInput.B | PlatformInput.Right,
            frameCounter3C: 0,
            cosmo: 100);
        Require(special40.Route == PlatformPlayerActionRoute.UnsupportedSpecial40, "$40 is surfaced as unsupported");
        Require(!special40.IsModeled, "$40 reports not modeled");
        Require(special40.State == BaseState(action: 0x40, x: 0x40, y: 0x60, attack: attackSeed), "$40 unsupported route does not invent state mutation");
        Require(special40.AttackAttempt is null, "$40 unsupported route does not fake B processing");

        var damage80 = PlatformPlayerActionDispatcher.Step(
            open,
            BaseState(action: 0x80, x: 0x40, y: 0x60, attack: attackSeed),
            PlatformInput.B,
            frameCounter3C: 0,
            cosmo: 100);
        Require(damage80.Route == PlatformPlayerActionRoute.UnsupportedDamage80, "$80 is surfaced as unsupported");
        Require(!damage80.IsModeled, "$80 reports not modeled");
        Require(damage80.State.AttackState == attackSeed, "$80 unsupported route preserves attack state");
    }

    private static PlatformPlayerActionState BaseState(
        byte action,
        byte x,
        byte y,
        byte jumpPhase = 0,
        byte jumpLatch = 0,
        byte highSelector = 0,
        byte dropLatch = 0,
        byte support = 0,
        byte special76 = 0,
        byte horizontal43 = 0,
        byte facing = 0x40,
        byte scrollLow = 0,
        byte scrollHigh = 0,
        PlatformAttackState? attack = null) =>
        new(
            PlatformSaintIndex.Seiya,
            new PlatformHorizontalState(x, scrollLow, scrollHigh, facing),
            y,
            0,
            action,
            jumpPhase,
            jumpLatch,
            highSelector,
            dropLatch,
            support,
            special76,
            horizontal43,
            attack ?? PlatformAttackState.Empty);

    private static PlatformStageMap OpenStage() =>
        new(0, [new PlatformStagePage(0, new byte[PlatformStagePage.DescriptorCount])]);

    private static PlatformStageMap StageWithDescriptor(int x, int y, byte descriptor)
    {
        var data = new byte[PlatformStagePage.DescriptorCount];
        var col = x / PlatformStageMap.CellSizePixels;
        var row = y / PlatformStageMap.CellSizePixels;
        data[row * PlatformStagePage.Columns + col] = descriptor;
        return new PlatformStageMap(0, [new PlatformStagePage(0, data)]);
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Player action dispatcher self-test failed: {label}");
    }
}

using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class OrdinaryPlayerActionChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        var stage = OpenStage();

        var right = PlatformOrdinaryPlayerAction.Step(
            stage,
            BaseState(x: 0x40, y: 0x60),
            PlatformInput.Right,
            frameCounter3C: 0,
            cosmo: 100);
        Require(right.Route == PlatformOrdinaryPlayerRoute.Grounded, "Right remains grounded");
        Require(right.State.Horizontal.PlayerX == 0x41, "Right moves one pixel for Seiya");
        Require(right.State.ActionState4D == 0x10, "Right enters locomotion family");
        Require(right.AttackAttempt.Outcome == PlatformAttackAttemptOutcome.NoInput, "Right has no attack");

        var down = PlatformOrdinaryPlayerAction.Step(
            stage,
            BaseState(x: 0x40, y: 0x60),
            PlatformInput.Down,
            frameCounter3C: 0,
            cosmo: 100);
        Require(down.Route == PlatformOrdinaryPlayerRoute.Grounded, "Down remains grounded");
        Require(down.State.ActionState4D == 0x20, "Down enters crouch family");
        Require(down.State.Horizontal.PlayerX == 0x40, "Down does not move horizontally");

        var jump = PlatformOrdinaryPlayerAction.Step(
            stage,
            BaseState(x: 0x40, y: 0x60),
            PlatformInput.A,
            frameCounter3C: 0,
            cosmo: 100);
        Require(jump.JumpInitiation.StartedJump, "A starts jump");
        Require(jump.Route == PlatformOrdinaryPlayerRoute.Airborne, "new jump routes airborne same frame");
        Require(jump.FrameStartAction4E == 0, "jump preserves old frame-start action");
        Require(jump.State.JumpPhase49 == 2, "new jump immediately consumes first airborne phase");
        Require(jump.State.PlayerY == 0x58, "new ordinary jump rises 8 pixels same frame");
        Require(jump.State.ActionState4D == 0x30, "ordinary jump action persists");

        var directional = PlatformOrdinaryPlayerAction.Step(
            stage,
            BaseState(x: 0x40, y: 0x60),
            PlatformInput.A | PlatformInput.Right,
            frameCounter3C: 1,
            cosmo: 100);
        Require(directional.State.PlayerY == 0x5C, "directional jump rises 4 pixels first frame");
        Require(directional.State.Horizontal.PlayerX == 0x42, "directional jump uses odd-frame 0388=2");
        Require(directional.State.ActionState4D == 0x31, "directional jump keeps $31");

        var simultaneous = PlatformOrdinaryPlayerAction.Step(
            stage,
            BaseState(x: 0x40, y: 0x60, facing: 0x40),
            PlatformInput.A | PlatformInput.B,
            frameCounter3C: 0,
            cosmo: 100);
        Require(simultaneous.JumpInitiation.StartedJump, "A+B starts jump");
        Require(simultaneous.AttackAttempt.Outcome == PlatformAttackAttemptOutcome.Created, "A+B creates attack before jump dispatch");
        Require(simultaneous.AttackAttempt.CreatedSlot == PlatformAttackSlotId.Slot0, "Seiya A+B uses slot0");
        var simultaneousProjectile = simultaneous.State.AttackState.Slot0.Object;
        Require(simultaneousProjectile.Y == 0x67, "A+B attack origin Y uses pre-jump Y+7");
        Require(simultaneousProjectile.X == 0x52, "A+B attack origin X uses pre-jump X and facing");
        Require(simultaneous.State.PlayerY == 0x58, "A+B vertical movement occurs after projectile creation");
        Require(simultaneous.State.JumpPhase49 == 2, "A+B jump phase advances after attack creation");
        Require(simultaneous.State.AttackState.Busy4B == 1, "A+B attack busy state persists");

        var movingAttack = PlatformOrdinaryPlayerAction.Step(
            stage,
            BaseState(x: 0x40, y: 0x60, action: 0x10, facing: 0x40),
            PlatformInput.B,
            frameCounter3C: 0,
            cosmo: 100);
        Require(movingAttack.FrameStartAction4E == 0x10, "moving attack preserves old frame-start action");
        Require(movingAttack.AttackAttempt.Outcome == PlatformAttackAttemptOutcome.Created, "moving-family B creates attack");
        Require(movingAttack.AttackAttempt.State.ActionState4D == 0, "attack creation resets moving family before grounded tail");
        Require(movingAttack.State.AttackState.Slot0.Object.Y == 0x67, "non-crouch old $4E uses player Y+7 origin");
        Require(movingAttack.State.ActionState4D == 0, "B-only grounded tail remains neutral");

        var movingAttackRight = PlatformOrdinaryPlayerAction.Step(
            stage,
            BaseState(x: 0x40, y: 0x60, action: 0x10, facing: 0x40),
            PlatformInput.B | PlatformInput.Right,
            frameCounter3C: 0,
            cosmo: 100);
        Require(movingAttackRight.AttackAttempt.State.ActionState4D == 0, "B+Right first resets prior moving action");
        Require(movingAttackRight.State.ActionState4D == 0x10, "later AB3F Right restores locomotion family");
        Require(movingAttackRight.State.Horizontal.PlayerX == 0x41, "B+Right still moves after attack creation");

        var downRight = PlatformOrdinaryPlayerAction.Step(
            stage,
            BaseState(x: 0x40, y: 0x60),
            PlatformInput.Right | PlatformInput.Down,
            frameCounter3C: 0,
            cosmo: 100);
        Require(downRight.State.Horizontal.PlayerX == 0x41, "Right+Down moves before action tail");
        Require(downRight.State.ActionState4D == 0x20, "Down overrides locomotion at tail");

        var finalEdge = PlatformOrdinaryPlayerAction.Step(
            stage,
            BaseState(x: 0xE0, y: 0x60, scrollLow: 0xF8, scrollHigh: 0x04),
            PlatformInput.Right,
            frameCounter3C: 0,
            cosmo: 100);
        Require(finalEdge.GroundedHorizontal?.EdgeBlocked == true, "final right edge is blocked");
        Require(finalEdge.State.ActionState4D == 0, "hard edge clears grounded action");

        var collisionStage = StageWithDescriptor(x: 0x50, y: 0x70, descriptor: 0x80);
        var collision = PlatformOrdinaryPlayerAction.Step(
            collisionStage,
            BaseState(x: 0x40, y: 0x60),
            PlatformInput.Right,
            frameCounter3C: 0,
            cosmo: 100);
        Require(collision.GroundedHorizontal?.CollisionBlocked == true, "terrain blocks grounded right");
        Require(collision.State.Horizontal.PlayerX == 0x40, "blocked grounded right does not move");
        Require(collision.State.ActionState4D == 0x10, "terrain-blocked direction still enters locomotion family");

        var activeJumpRelease = PlatformOrdinaryPlayerAction.Step(
            stage,
            BaseState(
                x: 0x40,
                y: 0x60,
                action: 0x31,
                jumpPhase: 8,
                jumpLatch: 1,
                facing: 0x40),
            PlatformInput.Right,
            frameCounter3C: 1,
            cosmo: 100);
        Require(!activeJumpRelease.JumpInitiation.StartedJump, "active jump cannot restart");
        Require(activeJumpRelease.State.JumpButtonLatch4A == 0, "A release during active jump clears latch");
        Require(activeJumpRelease.Route == PlatformOrdinaryPlayerRoute.Airborne, "active jump remains airborne");
        Require(activeJumpRelease.State.JumpPhase49 == 9, "active jump advances existing phase");

        var airborneCollision = PlatformOrdinaryPlayerAction.Step(
            collisionStage,
            BaseState(
                x: 0x40,
                y: 0x60,
                action: 0x31,
                jumpPhase: 8,
                jumpLatch: 0,
                facing: 0x40),
            PlatformInput.Right,
            frameCounter3C: 1,
            cosmo: 100);
        Require(airborneCollision.State.ActionState4D == 0x30, "directional airborne collision collapses action to $30");
        Require(airborneCollision.State.JumpPhase49 == 9, "directional airborne collision preserves progressed phase");

        // $20/$40/$50/$80 frame-start families are routed elsewhere by $AAE4.
        var rejectedSpecial = false;
        try
        {
            PlatformOrdinaryPlayerAction.Step(
                stage,
                BaseState(x: 0x40, y: 0x60, action: 0x20),
                PlatformInput.None,
                frameCounter3C: 0,
                cosmo: 100);
        }
        catch (ArgumentException)
        {
            rejectedSpecial = true;
        }
        Require(rejectedSpecial, "ordinary compositor rejects crouch frame-start branch");

        // This compositor intentionally does not own $3C or the later object update.
        Require(simultaneous.State.AttackState.Busy4B == 1, "ordinary player action does not advance attack busy counter");
        Require(simultaneous.State.AttackState.Slot0.Object.Type == 0x64, "ordinary player action does not advance projectile animation/type");
    }

    private static PlatformOrdinaryPlayerActionState BaseState(
        byte x,
        byte y,
        byte action = 0,
        byte jumpPhase = 0,
        byte jumpLatch = 0,
        byte highSelector = 0,
        byte support = 0,
        byte special76 = 0,
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
            support,
            special76,
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
            throw new InvalidOperationException($"Ordinary player action self-test failed: {label}");
    }
}

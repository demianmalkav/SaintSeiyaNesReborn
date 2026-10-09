using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class AttackFramePhaseChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        var stage = OpenStage();

        // $926C executes before BBCA. Busy 7 therefore wraps to zero first,
        // allowing a fresh B press this frame when the B latch is clear.
        var busy7 = BaseState(PlatformSaintIndex.Seiya) with
        {
            AttackState = PlatformAttackState.Empty with { Busy4B = 7 },
        };
        var pre = PlatformAttackFramePhases.AdvanceBusyBeforePlayer(busy7);
        Require(pre.AttackState.Busy4B == 0, "$926C wraps busy 7 to zero before player input");

        var canFireAfterWrap = PlatformPlayerActionDispatcher.Step(
            stage,
            pre,
            PlatformInput.B,
            frameCounter3C: 0,
            cosmo: 100);
        Require(canFireAfterWrap.AttackAttempt?.Outcome == PlatformAttackAttemptOutcome.Created, "B can fire after same-frame pre-player busy wrap");
        Require(canFireAfterWrap.State.AttackState.Busy4B == 1, "BBCA reseeds busy to one after creation");

        // Seiya, Cosmo 100: creation range=3 and right-facing origin X=$52.
        // Later that very frame $A22C decrements range 3->2 and moves +5 to $57.
        var seiyaStart = PlatformAttackFramePhases.AdvanceBusyBeforePlayer(BaseState(PlatformSaintIndex.Seiya));
        var seiyaPlayer = PlatformPlayerActionDispatcher.Step(
            stage,
            seiyaStart,
            PlatformInput.B,
            frameCounter3C: 0,
            cosmo: 100);
        Require(seiyaPlayer.State.AttackState.Slot0.RangeCounter == 3, "Seiya projectile is created with range three before A22C");
        Require(seiyaPlayer.State.AttackState.Slot0.Object.X == 0x52, "Seiya projectile creation origin precedes object update");

        var seiyaPost = PlatformAttackFramePhases.UpdateObjectsAfterPlayer(seiyaPlayer, frameCounter3C: 0);
        Require(seiyaPost.UpdatedAttackObjects, "A22C updates newly-created Seiya projectile on creation frame");
        Require(seiyaPost.Player.State.AttackState.Slot0.RangeCounter == 2, "same-frame A22C consumes one range unit");
        Require(seiyaPost.Player.State.AttackState.Slot0.Object.X == 0x57, "same-frame A22C advances projectile five pixels");
        Require(seiyaPost.Player.State.AttackState.Slot0.Object.AuxiliaryX == 0x4F, "same-frame A22C writes auxiliary X from moved coordinate");
        Require(seiyaPost.Player.State.AttackState.Slot0.Object.Type == 0x64, "even-frame projectile renders type $64");
        Require(seiyaPost.Player.State.AttackState.Busy4B == 1, "A22C does not advance attack busy counter");

        var oddStart = PlatformAttackFramePhases.AdvanceBusyBeforePlayer(BaseState(PlatformSaintIndex.Seiya));
        var oddPlayer = PlatformPlayerActionDispatcher.Step(
            stage,
            oddStart,
            PlatformInput.B,
            frameCounter3C: 1,
            cosmo: 100);
        var oddPost = PlatformAttackFramePhases.UpdateObjectsAfterPlayer(oddPlayer, frameCounter3C: 1);
        Require(oddPost.Player.State.AttackState.Slot0.Object.Type == 0x65, "A22C uses current old $3C parity before end-of-frame increment");

        // Shun low bracket starts at range 1 / extension 5. A22C runs later in
        // the creation frame, decrements range to zero and extends to 10 pixels.
        var shunStart = PlatformAttackFramePhases.AdvanceBusyBeforePlayer(BaseState(PlatformSaintIndex.Shun));
        var shunPlayer = PlatformPlayerActionDispatcher.Step(
            stage,
            shunStart,
            PlatformInput.B,
            frameCounter3C: 0,
            cosmo: 100);
        Require(shunPlayer.State.AttackState.Slot0.Object.Type == 0x54, "Shun creates chain root type $54");
        Require(shunPlayer.State.AttackState.Slot0.RangeCounter == 1, "Shun low bracket creates range one");
        Require(shunPlayer.State.AttackState.ShunExtension0391 == 5, "Shun creation seeds extension five");

        var shunPost = PlatformAttackFramePhases.UpdateObjectsAfterPlayer(shunPlayer, frameCounter3C: 0);
        Require(shunPost.Player.State.AttackState.Slot0.RangeCounter == 0, "same-frame Shun update consumes final outward range");
        Require(shunPost.Player.State.AttackState.ShunExtension0391 == 10, "same-frame Shun update extends chain to ten");
        Require(shunPost.Player.State.AttackState.Slot1.Object.Type == 0x55, "same-frame Shun update synthesizes near segment");
        Require(shunPost.Player.State.AttackState.Slot1.Object.X == 0x5A, "Shun near segment uses player X + 16 + extension");
        Require(shunPost.Player.State.AttackState.Slot0.Object.X == 0x62, "Shun far segment sits eight pixels beyond near segment");

        // $80 waiting still returns to the normal frame, so already-existing
        // projectiles continue through $C2D7/$A22C even though player input is frozen.
        var existing = new PlatformAttackSlot(
            new PlatformAttackObject(0x60, 0x64, 0x40, 0x40, 0, 0, 0, 0),
            RangeCounter: 3);
        var hazardWaitState = BaseState(PlatformSaintIndex.Seiya) with
        {
            ActionState4D = 0x80,
            Special76 = 2,
            AttackState = PlatformAttackState.Empty with
            {
                ActionState4D = 0x80,
                Slot0 = existing,
            },
        };
        var hazardWait = PlatformPlayerActionDispatcher.Step(
            stage,
            hazardWaitState,
            PlatformInput.B,
            frameCounter3C: 0,
            cosmo: 100);
        Require(hazardWait.Route == PlatformPlayerActionRoute.Damage80Waiting, "$80 waiting skips player B logic");
        var waitObjects = PlatformAttackFramePhases.UpdateObjectsAfterPlayer(hazardWait, frameCounter3C: 0);
        Require(waitObjects.Player.State.AttackState.Slot0.Object.X == 0x45, "existing projectile still moves during $80 waiting frame");
        Require(waitObjects.Player.State.AttackState.Slot0.RangeCounter == 2, "existing projectile range still advances during $80 waiting frame");

        // $80 reload is a JMP away from the active path; $C2D7/$A22C is never reached.
        var reloadState = hazardWaitState with { Special76 = 0 };
        var reload = PlatformPlayerActionDispatcher.Step(
            stage,
            reloadState,
            PlatformInput.None,
            frameCounter3C: 0,
            cosmo: 100);
        Require(reload.ExitsNormalPlayerLoop, "$80 reload exits before post-player object pipeline");
        var reloadObjects = PlatformAttackFramePhases.UpdateObjectsAfterPlayer(reload, frameCounter3C: 0);
        Require(reloadObjects.SkippedBecausePlayerLoopExited, "$80 reload explicitly skips A22C");
        Require(reloadObjects.Player.State.AttackState.Slot0 == existing, "$80 reload leaves attack object untouched because updater is not reached");
    }

    private static PlatformPlayerActionState BaseState(PlatformSaintIndex saint) =>
        new(
            saint,
            new PlatformHorizontalState(0x40, 0, 0, 0x40),
            PlayerY: 0x60,
            PlayerYHigh41: 0,
            ActionState4D: 0,
            JumpPhase49: 0,
            JumpButtonLatch4A: 0,
            HighJumpSelector038A: 0,
            DropButtonLatch038C: 0,
            Support038D: 0,
            Special76: 0,
            HorizontalAmount43: 0,
            AttackState: PlatformAttackState.Empty);

    private static PlatformStageMap OpenStage() =>
        new(0, [new PlatformStagePage(0, new byte[PlatformStagePage.DescriptorCount])]);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Attack frame phase self-test failed: {label}");
    }
}

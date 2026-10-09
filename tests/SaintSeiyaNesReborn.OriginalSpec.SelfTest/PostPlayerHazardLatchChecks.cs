using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class PostPlayerHazardLatchChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        var zero = PlatformPostPlayerLatch.Step(0, playerLoopExited: false);
        Require(!zero.Decremented && zero.After76 == 0, "$76 zero remains zero");

        var ordinary = PlatformPostPlayerLatch.Step(0x20, playerLoopExited: false);
        Require(ordinary.Decremented, "nonzero $76 decrements in B94B");
        Require(ordinary.Before76 == 0x20 && ordinary.After76 == 0x1F, "$20 contact latch decrements exactly once");

        var one = PlatformPostPlayerLatch.Step(1, playerLoopExited: false);
        Require(one.After76 == 0, "$76=1 expires after the player step");

        var skipped = PlatformPostPlayerLatch.Step(0, playerLoopExited: true);
        Require(skipped.SkippedBecausePlayerLoopExited, "reload exit skips B94B");
        Require(!skipped.Decremented, "reload exit does not run post-player countdown");

        // Critical two-frame $80 timing:
        // frame N: AAe4 sees $76=1, returns through Damage80Waiting;
        // later that frame B94B decrements 1->0;
        // frame N+1: AAe4 sees zero and takes the exceptional reload path.
        var stage = OpenStage();
        var start = State(action: 0x80, hazard76: 1);

        var frameNPlayer = PlatformPlayerActionDispatcher.Step(
            stage,
            start,
            PlatformInput.B | PlatformInput.Right,
            frameCounter3C: 0,
            cosmo: 100);
        Require(frameNPlayer.Route == PlatformPlayerActionRoute.Damage80Waiting, "frame N waits while $76=1");
        Require(frameNPlayer.State.Special76 == 1, "AAe4 itself does not decrement $76");

        var frameNPost = PlatformPostPlayerLatch.Apply(frameNPlayer);
        Require(frameNPost.State.Special76 == 0, "B94B expires $76 after AAe4 on frame N");
        Require(!frameNPost.ExitsNormalPlayerLoop, "frame N still completes normal player path");

        var frameNPlus1 = PlatformPlayerActionDispatcher.Step(
            stage,
            frameNPost.State,
            PlatformInput.None,
            frameCounter3C: 1,
            cosmo: 100);
        Require(frameNPlus1.Route == PlatformPlayerActionRoute.Damage80Reload, "frame N+1 reloads after prior B94B expiration");
        Require(frameNPlus1.ExitsNormalPlayerLoop, "frame N+1 exits normal platform continuation");

        var afterExit = PlatformPostPlayerLatch.Apply(frameNPlus1);
        Require(afterExit.State.Special76 == 0, "reload exit preserves zero latch");
        Require(afterExit.ExitsNormalPlayerLoop, "post-player helper cannot turn reload into normal continuation");
    }

    private static PlatformPlayerActionState State(byte action, byte hazard76) =>
        new(
            PlatformSaintIndex.Seiya,
            new PlatformHorizontalState(0x40, 0, 0, 0x40),
            PlayerY: 0xA0,
            PlayerYHigh41: 0,
            ActionState4D: action,
            JumpPhase49: 0,
            JumpButtonLatch4A: 0,
            HighJumpSelector038A: 0,
            DropButtonLatch038C: 0,
            Support038D: 0,
            Special76: hazard76,
            HorizontalAmount43: 0,
            AttackState: PlatformAttackState.Empty with { ActionState4D = action });

    private static PlatformStageMap OpenStage() =>
        new(0, [new PlatformStagePage(0, new byte[PlatformStagePage.DescriptorCount])]);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Post-player hazard latch self-test failed: {label}");
    }
}

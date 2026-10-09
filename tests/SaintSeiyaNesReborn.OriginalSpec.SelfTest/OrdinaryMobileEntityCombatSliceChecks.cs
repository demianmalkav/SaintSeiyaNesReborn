using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class OrdinaryMobileEntityCombatSliceChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckEntityDecisionUsesPostPlayerPosition();
        CheckRemovedEntitySkipsInteractionButNotLateFrame();
    }

    private static void CheckEntityDecisionUsesPostPlayerPosition()
    {
        var entity = RuntimeEntity(
            action: 0x10,
            x: 0x40,
            y: 0x60,
            phase: 0,
            timer: 1,
            facing: 0x40,
            ground: 0xE0);

        // Control: against the player's frame-entry X=$40, this timer-expiry
        // decision would not start the immediate rightward jump.
        var hypotheticalPrePlayer = PlatformCommonEntityDecision.Step(
            entity.Motion,
            playerX: 0x40,
            playerY: 0x50,
            playerJumpPhase49: 0,
            entropy48: 0x12);
        Require(hypotheticalPrePlayer.Outcome == PlatformEntityDecisionOutcome.TimerExpiredNoChange,
            "pre-player coordinate control would not start jump");

        var frame = PlatformOrdinaryMobileEntityCombatSlice.StepNonFatal(
            OpenStage(),
            BasePlayer(x: 0x40, y: 0x50, action: 0, latch: 5),
            PlatformInput.Right,
            new PlatformFrameResources(99, 100),
            new PlatformContactPhaseState(5, new ContactDrainState(0, 0)),
            entity,
            PlatformHitboxParameters.Common,
            seventhSense: 0,
            frameCounter3C: 1,
            entropy48: 0x12,
            cameraDelta43: 0,
            engineSubstate02: 1);

        Require(frame.PrePlayer.Player.State.Horizontal.PlayerX == 0x41,
            "$AAE4 moves player right before A442 entity decision");
        Require(frame.PostPlayerLatch.After76 == 4,
            "$B94B decrements shared latch before entity pipeline");
        Require(frame.Preparation is not null,
            "normal player path reaches ordinary entity preparation");
        Require(frame.Preparation!.Value.Decision.Outcome == PlatformEntityDecisionOutcome.JumpStartedRight,
            "entity decision sees post-player X=$41 and starts $31 jump");
        Require(frame.Preparation.Value.JumpStep is not null
            && frame.Preparation.Value.JumpStep!.Value.TableIndexUsed == 0,
            "newly started jump consumes first C5E6 sample in same entity update");
        Require(frame.Entity.Motion.ActionState == 0x31,
            "entity remains in rightward jump family after interaction phase");
        Require(frame.Entity.Motion.StatePhase == 2,
            "jump phase advances 1->2 immediately");
        Require(frame.Entity.Motion.Y == 0x58,
            "same-frame first jump sample moves entity Y upward by 8");
        Require(frame.Entity.Motion.X == 0x41,
            "same-frame $31 horizontal step moves entity X +1");
        Require(frame.Interaction is not null,
            "prepared in-bounds entity reaches interaction phase");
        Require(frame.Interaction!.ContactPhase.Contact.Outcome == PlatformEntityContactOutcome.ContactLatchActive,
            "remaining latch 4 suppresses contact and isolates AI-order fixture");
        Require(frame.FrameCounterAdvanced && frame.FrameCounterAfter3C == 2,
            "normal ordinary-mobile slice reaches C402 increment");
    }

    private static void CheckRemovedEntitySkipsInteractionButNotLateFrame()
    {
        var existingAttack = PlatformAttackState.Empty with
        {
            Slot0 = new PlatformAttackSlot(
                new PlatformAttackObject(0x57, 0x64, 0x40, 0x52, 0, 0, 0, 0),
                3),
        };
        var entity = RuntimeEntity(
            action: 0x10,
            x: 0x00,
            y: 0x60,
            phase: 0,
            timer: 5,
            facing: 0x00,
            ground: 0xE0);

        var frame = PlatformOrdinaryMobileEntityCombatSlice.StepNonFatal(
            OpenStage(),
            BasePlayer(x: 0x40, y: 0x50, action: 0, latch: 0, attack: existingAttack),
            PlatformInput.None,
            new PlatformFrameResources(99, 100),
            new PlatformContactPhaseState(0, new ContactDrainState(0, 0)),
            entity,
            PlatformHitboxParameters.Common,
            seventhSense: 0,
            frameCounter3C: 1,
            entropy48: 0,
            cameraDelta43: 0,
            engineSubstate02: 1);

        Require(frame.Preparation!.Value.Outcome == PlatformCommonEntityPreparationOutcome.RemovedHorizontal,
            "left-moving X=0 entity wraps to $FF and removes before interactions");
        Require(frame.Entity.Motion.X == 0xFF,
            "runtime entity retains exact wrapped X produced before removal");
        Require(frame.EntityRemovedBeforeInteraction,
            "slice reports entity removal boundary explicitly");
        Require(frame.Interaction is null,
            "removed entity never reaches $9915/$98BA");
        Require(frame.ContactState == new PlatformContactPhaseState(0, new ContactDrainState(0, 0)),
            "removed entity cannot seed contact latch/drains");

        var slot = frame.PlayerAfterLatePhases.State.AttackState.Slot0;
        Require(slot.Object.X == 0x57 && slot.RangeCounter == 2,
            "entity removal returns to frame; later A22C still moves/ages player projectile");
        Require(slot.Object.Type == 0x65,
            "late projectile parity still uses current $3C=1");
        Require(frame.FrameCounterAdvanced && frame.FrameCounterAfter3C == 2,
            "entity removal does not skip later C402 frame-counter increment");
    }

    private static PlatformCommonEntityRuntimeState RuntimeEntity(
        byte action,
        byte x,
        byte y,
        byte phase,
        byte timer,
        byte facing,
        byte ground) =>
        new(
            new PlatformCommonEntityMotionState(
                ActionState: action,
                X: x,
                Y: y,
                StatePhase: phase,
                GroundDescriptor: ground,
                DecisionTimer: timer,
                FlagsFacing: facing,
                Type: 0x05,
                TerrainProbeRight: 0,
                TerrainProbeLeft: 0),
            HitPoints: 40,
            LifeDrainTicks: 2,
            CosmoDrainTicks: 3,
            SeventhSenseRewardBcd: 0x10);

    private static PlatformPlayerActionState BasePlayer(
        byte x,
        byte y,
        byte action,
        byte latch,
        PlatformAttackState? attack = null) =>
        new(
            PlatformSaintIndex.Seiya,
            new PlatformHorizontalState(x, 0, 0, 0x40),
            y,
            0,
            action,
            0,
            0,
            0,
            0,
            0,
            latch,
            0,
            attack ?? PlatformAttackState.Empty);

    private static PlatformStageMap OpenStage() =>
        new(0, [new PlatformStagePage(0, new byte[PlatformStagePage.DescriptorCount])]);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Ordinary mobile entity combat slice self-test failed: {label}");
    }
}

using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class EntityTypes08090CChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        Check08ExcludesProximityFall();
        Check0CStartsProximityFallAndStillBobsPostInteraction();
        Check0CBobFourPhaseCycle();
        CheckSpecialReactionReturnsTo00();
        CheckSpecialFallLandsTo00();
        Check0CDeathOrdersBobBeforeCadence();
        Check08SurvivorHitAdvances40SameFrame();
        Check0CKillCombinesBobAndDeathSameFrame();
    }

    private static void Check08ExcludesProximityFall()
    {
        var step = PlatformEntityTypes08090C.StepPreInteraction(
            Motion(type: 0x08, action: 0x00, x: 0x50, y: 0x40, phase: 0, ground: 0xA8),
            playerX: 0x50,
            playerY: 0x60,
            frameCounter3C: 1,
            cameraDelta43: 1);

        Require(step.Route == PlatformEntity08090CActiveRoute.Idle00,
            "type08 action00 uses special idle path");
        Require(step.State.X == 0x4F,
            "idle special entity is stationary in world and only subtracts camera");
        Require(!step.ProximityFallStarted && step.State.ActionState == 0x00 && step.State.Y == 0x40,
            "type08 is explicitly excluded from A6AA proximity fall");
        Require(step.Continuation == PlatformEntity08090CContinuation.ReadyForInteraction,
            "in-bounds type08 idle still reaches interaction");
    }

    private static void Check0CStartsProximityFallAndStillBobsPostInteraction()
    {
        var pre = PlatformEntityTypes08090C.StepPreInteraction(
            Motion(type: 0x0C, action: 0x00, x: 0x50, y: 0x40, phase: 0, ground: 0xA8),
            playerX: 0x50,
            playerY: 0x60,
            frameCounter3C: 0,
            cameraDelta43: 0);

        Require(pre.ProximityFallStarted && pre.State.ActionState == 0x50,
            "type0C shares proximity fall path and stores $50");
        Require(pre.State.Y == 0x46,
            "proximity fall applies immediate +6 Y");
        Require(pre.Continuation == PlatformEntity08090CContinuation.ReadyForInteraction,
            "new $50 still follows current idle update into interaction");

        var post = PlatformEntityTypes08090C.AdvanceAfterInteraction(pre.State, frameCounter3C: 0);
        Require(post.BobYDelta == 1 && post.State.Y == 0x47,
            "type0C A7D0 bob still runs later in same entry update");
        Require(post.State.ActionState == 0x50,
            "bob does not erase newly stored fall family");
    }

    private static void Check0CBobFourPhaseCycle()
    {
        var baseState = Motion(type: 0x0C, action: 0x00, x: 0x50, y: 0x50, phase: 1, ground: 0xA8);
        var p0 = PlatformEntityTypes08090C.AdvanceAfterInteraction(baseState, 0);
        var p8 = PlatformEntityTypes08090C.AdvanceAfterInteraction(baseState, 8);
        var p16 = PlatformEntityTypes08090C.AdvanceAfterInteraction(baseState, 16);
        var p24 = PlatformEntityTypes08090C.AdvanceAfterInteraction(baseState, 24);
        var p1 = PlatformEntityTypes08090C.AdvanceAfterInteraction(baseState, 1);

        Require(p0.BobYDelta == 1 && p8.BobYDelta == 1,
            "bob table first two phases are +1/+1");
        Require(p16.BobYDelta == -1 && p24.BobYDelta == -1,
            "bob table last two phases are -1/-1");
        Require(p1.BobYDelta == 0,
            "bob updates only when ($3C & 7)==0");
    }

    private static void CheckSpecialReactionReturnsTo00()
    {
        var pre = PlatformEntityTypes08090C.StepPreInteraction(
            Motion(type: 0x08, action: 0x4F, x: 0x50, y: 0x50, phase: 0x48, ground: 0xA8),
            playerX: 0x20,
            playerY: 0x40,
            frameCounter3C: 1,
            cameraDelta43: 1);

        Require(pre.Route == PlatformEntity08090CActiveRoute.HitReaction40,
            "type08 frame-start $4F uses special reaction path");
        Require(pre.State.X == 0x4F,
            "special $40 path consumes camera but has no A845 recoil");
        Require(pre.State.ActionState == 0x00 && pre.ReactionCompleted,
            "type>=08 terminal reaction returns to $00, not common $10");
        Require(pre.State.StatePhase == 0x48,
            "special reaction leaves +$03 untouched because A845 is type<08 only");
        Require(pre.Continuation == PlatformEntity08090CContinuation.SkipInteraction,
            "current $40 control path still bypasses interactions after terminal reset");
    }

    private static void CheckSpecialFallLandsTo00()
    {
        var pre = PlatformEntityTypes08090C.StepPreInteraction(
            Motion(type: 0x09, action: 0x50, x: 0x50, y: 0x5F, phase: 3, ground: 0xA8),
            playerX: 0x20,
            playerY: 0x40,
            frameCounter3C: 1,
            cameraDelta43: 0);

        Require(pre.Route == PlatformEntity08090CActiveRoute.Fall50 && pre.Landed,
            "type09 $50 calls shared C491 and lands");
        Require(pre.State.Y == 0x60 && pre.State.ActionState == 0x00 && pre.State.StatePhase == 0,
            "special landing snaps Y, clears phase and returns to $00");
        Require(pre.Continuation == PlatformEntity08090CContinuation.SkipInteraction,
            "landing frame remains on $50 renderer continuation");
    }

    private static void Check0CDeathOrdersBobBeforeCadence()
    {
        var pre = PlatformEntityTypes08090C.StepPreInteraction(
            Motion(type: 0x0C, action: 0xD0, x: 0x50, y: 0x50, phase: 0, ground: 0x70),
            playerX: 0x20,
            playerY: 0x40,
            frameCounter3C: 0,
            cameraDelta43: 1);

        Require(pre.State.X == 0x4F,
            "frame-start special death consumes camera first");
        Require(pre.BobYDelta == 1,
            "type0C bob executes before D0 cadence at $3C=0");
        Require(pre.DeathPhaseAdvanced && pre.State.ActionState == 0xD1,
            "same update advances D0->D1 on cadence");
        Require(pre.State.Y == 0x53,
            "Y $50 + bob1 + descriptor<80 death2 = $53");
    }

    private static void Check08SurvivorHitAdvances40SameFrame()
    {
        var attack = ActiveAttack(0x50, 0x50);
        var frame = StepSlotA(
            Runtime(type: 0x08, action: 0x00, x: 0x50, y: 0x50, hp: 20, reward: 0x10, ground: 0xA8),
            attack,
            frameCounter3C: 1,
            cosmo: 50,
            seventhSense: 0);

        Require(frame.SlotA!.Dispatch.Route == PlatformCommonEntityActiveRoute.Special08090C,
            "type08 is routed through special basic dispatcher");
        Require(frame.SlotA.Interaction!.HitSequence.Results[0].Result.Outcome == PlatformProjectileHitOutcome.HpSurvived,
            "type08 uses HP path and survives damage10");
        Require(frame.SlotA.Special08090CPost.HasValue && frame.SlotA.Special08090CPost.Value.ReactionAdvanced,
            "hit-created $40 immediately reaches special A79E phase");
        Require(frame.SlotA.Entity.Motion.ActionState == 0x41,
            "same hit frame ends at $41 rather than delayed $40");
        Require(frame.SlotA.Entity.Motion.StatePhase == 0,
            "type08 survivor hit has no common recoil byte");
        Require(frame.SlotA.Entity.HitPoints == 10,
            "HP reduction is retained through special post phase");
    }

    private static void Check0CKillCombinesBobAndDeathSameFrame()
    {
        var attack = ActiveAttack(0x50, 0x50);
        var frame = StepSlotA(
            Runtime(type: 0x0C, action: 0x00, x: 0x50, y: 0x50, hp: 10, reward: 0x10, ground: 0x70),
            attack,
            frameCounter3C: 0,
            cosmo: 50,
            seventhSense: 100);

        Require(frame.SlotA!.Interaction!.HitSequence.Results[0].Result.Outcome == PlatformProjectileHitOutcome.HpKilled,
            "type0C lethal overlap writes D0");
        Require(frame.SlotA.Special08090CPost.HasValue,
            "kill-created D0 continues through special post path same frame");
        var post = frame.SlotA.Special08090CPost.Value;
        Require(post.BobYDelta == 1 && post.DeathPhaseAdvanced,
            "type0C kill frame performs bob before cadence-aligned death advance");
        Require(frame.SlotA.Entity.Motion.ActionState == 0xD1,
            "kill frame ends D1 on $3C=0 cadence");
        Require(frame.SlotA.Entity.Motion.Y == 0x53,
            "kill frame combines bob +1 and death +2");
        Require(frame.SeventhSense == 110,
            "lethal special entity awards its BCD reward once");
    }

    private static PlatformTwoCommonEntityCombatSliceResult StepSlotA(
        PlatformCommonEntityRuntimeState entityA,
        PlatformAttackSlot attack,
        byte frameCounter3C,
        int cosmo,
        int seventhSense)
    {
        var player = new PlatformPlayerActionState(
            PlatformSaintIndex.Hyoga,
            new PlatformHorizontalState(0x30, 0, 0, 0x40),
            0x40,
            0, 0, 0, 0, 0, 0, 0,
            5,
            0,
            PlatformAttackState.Empty with { Slot0 = attack });

        var entityB = Runtime(type: 0x05, action: 0x10, x: 0x90, y: 0x50, hp: 30, reward: 0, ground: 0xE0);

        return PlatformTwoCommonEntityCombatSlice.StepNonFatal(
            OpenStage(),
            player,
            PlatformInput.None,
            new PlatformFrameResources(99, cosmo),
            new PlatformContactPhaseState(5, new ContactDrainState(0, 0)),
            entityA,
            entityB,
            PlatformHitboxParameters.Common,
            PlatformHitboxParameters.Common,
            seventhSense,
            frameCounter3C,
            entropy48: 0,
            cameraDelta43: 0,
            engineSubstate02: 1);
    }

    private static PlatformAttackSlot ActiveAttack(byte x, byte y) =>
        new(new PlatformAttackObject(y, 0x64, 0x40, x, 0, 0, 0, 0), 8);

    private static PlatformCommonEntityRuntimeState Runtime(
        byte type,
        byte action,
        byte x,
        byte y,
        byte hp,
        byte reward,
        byte ground) =>
        new(
            Motion(type, action, x, y, phase: 0, ground),
            HitPoints: hp,
            LifeDrainTicks: 2,
            CosmoDrainTicks: 3,
            SeventhSenseRewardBcd: reward);

    private static PlatformCommonEntityMotionState Motion(
        byte type,
        byte action,
        byte x,
        byte y,
        byte phase,
        byte ground) =>
        new(
            ActionState: action,
            X: x,
            Y: y,
            StatePhase: phase,
            GroundDescriptor: ground,
            DecisionTimer: 5,
            FlagsFacing: 0x40,
            Type: type,
            TerrainProbeRight: 0,
            TerrainProbeLeft: 0);

    private static PlatformStageMap OpenStage() =>
        new(0, [new PlatformStagePage(0, new byte[PlatformStagePage.DescriptorCount])]);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Entity types 08/09/0C self-test failed: {label}");
    }
}

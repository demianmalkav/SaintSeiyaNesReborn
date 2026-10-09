using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class CommonEntityAttack70Checks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckNoOrdinaryWalkDuring70();
        CheckDecisionCanReplace70WithImmediateJump();
        CheckMidpointSpawnAt78();
        CheckCommonTerminalReturnsTo10();
        CheckSpecialTerminalTemplate();
        CheckProjectileHitCancelsLate70Advance();
    }

    private static void CheckNoOrdinaryWalkDuring70()
    {
        var state = Motion(action: 0x72, type: 0x06, x: 0x50, y: 0x50, timer: 5);
        var step = PlatformCommonEntityAttack70.PrepareCommon(
            state,
            playerX: 0x20,
            playerY: 0x40,
            playerJumpPhase49: 0,
            entropy48: 0,
            frameCounter3C: 0,
            cameraDelta43: 1);

        Require(step.Outcome == PlatformCommonEntityAttack70PreparationOutcome.ReadyForInteraction,
            "$70 preparation remains interaction-capable");
        Require(step.HorizontalDeltaBeforeCamera == 0,
            "$70 itself does not apply ordinary facing walk");
        Require(step.State.X == 0x4F,
            "$70 screen X changes only by camera delta when no jump is started");
    }

    private static void CheckDecisionCanReplace70WithImmediateJump()
    {
        var state = Motion(action: 0x72, type: 0x06, x: 0x40, y: 0x50, timer: 1);
        var step = PlatformCommonEntityAttack70.PrepareCommon(
            state,
            playerX: 0x60,
            playerY: 0x40,
            playerJumpPhase49: 0,
            entropy48: 0,
            frameCounter3C: 1,
            cameraDelta43: 0);

        Require(step.Decision.Outcome == PlatformEntityDecisionOutcome.JumpStartedRight,
            "expired decision timer can replace $70 with $31");
        Require(step.State.ActionState == 0x31,
            "new jump family persists after preparation");
        Require(step.JumpStep.HasValue && step.JumpStep.Value.TableIndexUsed == 0,
            "jump started from $70 consumes first C5E6 sample in same update");
        Require(step.State.StatePhase == 2 && step.State.Y == 0x48,
            "first jump sample advances phase 1->2 and raises Y by 8");
        Require(step.State.X == 0x41,
            "$31 also applies its same-frame horizontal +1 at frame counter 1");
    }

    private static void CheckMidpointSpawnAt78()
    {
        var step = PlatformCommonEntityAttack70.AdvanceAfterInteraction(
            Motion(action: 0x77, type: 0x06, x: 0x50, y: 0x50, timer: 5),
            frameCounter3C: 1);

        Require(step.Advanced && step.State.ActionState == 0x78,
            "odd frame advances $77->$78");
        Require(step.CallsSecondarySpawnRoutine,
            "$78 for type06 calls A908");
        Require(step.SoundId == PlatformCommonEntityAttack70.MidpointSpawnSoundId,
            "$78 plays sound $2A");
        Require(step.SpawnTemplate == new PlatformSecondarySpawnTemplate(0xB0, 2),
            "type06 A908 template is object $B0 at Y+2");
    }

    private static void CheckCommonTerminalReturnsTo10()
    {
        var step = PlatformCommonEntityAttack70.AdvanceAfterInteraction(
            Motion(action: 0x7F, type: 0x06, x: 0x50, y: 0x50, timer: 5),
            frameCounter3C: 3);

        Require(step.Advanced && step.CompletedFamily,
            "odd $7F terminal update completes $70 family");
        Require(step.State.ActionState == 0x10,
            "ordinary/common terminal returns to $10");
        Require(!step.CallsSecondarySpawnRoutine && step.SoundId is null,
            "type06 terminal does not call A908 a second time");
    }

    private static void CheckSpecialTerminalTemplate()
    {
        var step = PlatformCommonEntityAttack70.AdvanceAfterInteraction(
            Motion(action: 0x7F, type: 0x08, x: 0x50, y: 0x50, timer: 5),
            frameCounter3C: 1);

        Require(step.State.ActionState == 0x00 && step.CompletedFamily,
            "type08 terminal returns to action $00");
        Require(step.CallsSecondarySpawnRoutine && step.SoundId == 0x2D,
            "type08 terminal calls A908 and sound $2D");
        Require(step.SpawnTemplate == new PlatformSecondarySpawnTemplate(0xD2, 4),
            "type08 terminal spawn template is $D2 at Y+4");
    }

    private static void CheckProjectileHitCancelsLate70Advance()
    {
        var attack = new PlatformAttackSlot(
            new PlatformAttackObject(0x50, 0x64, 0x40, 0x50, 0, 0, 0, 0),
            3);
        var player = new PlatformPlayerActionState(
            PlatformSaintIndex.Hyoga,
            new PlatformHorizontalState(0x40, 0, 0, 0x40),
            0x50,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            5,
            0,
            PlatformAttackState.Empty with { Slot0 = attack });

        var entityA = new PlatformCommonEntityRuntimeState(
            Motion(action: 0x77, type: 0x06, x: 0x50, y: 0x50, timer: 5),
            HitPoints: 30,
            LifeDrainTicks: 2,
            CosmoDrainTicks: 3,
            SeventhSenseRewardBcd: 0x10);
        var entityB = new PlatformCommonEntityRuntimeState(
            Motion(action: 0x10, type: 0x05, x: 0x70, y: 0x50, timer: 5),
            HitPoints: 30,
            LifeDrainTicks: 2,
            CosmoDrainTicks: 3,
            SeventhSenseRewardBcd: 0x10);

        var frame = PlatformTwoCommonEntityCombatSlice.StepNonFatal(
            OpenStage(),
            player,
            PlatformInput.None,
            new PlatformFrameResources(99, 100),
            new PlatformContactPhaseState(5, new ContactDrainState(0, 0)),
            entityA,
            entityB,
            PlatformHitboxParameters.Common,
            PlatformHitboxParameters.Common,
            seventhSense: 0,
            frameCounter3C: 1,
            entropy48: 0,
            cameraDelta43: 0,
            engineSubstate02: 1);

        Require(frame.SlotA!.Dispatch.Route == PlatformCommonEntityActiveRoute.Attack70,
            "slot enters attack70 pre-interaction route");
        Require(frame.SlotA.Interaction!.HitSequence.Results[0].Result.Outcome == PlatformProjectileHitOutcome.HpSurvived,
            "overlapping Hyoga projectile resolves before late entity phases");
        Require(frame.SlotA.HitReaction40Post.HasValue,
            "hit-created $40 immediately reaches A79E in the same frame");
        Require(frame.SlotA.Entity.Motion.ActionState == 0x41,
            "surviving hit advances $40->$41 before the late $70 phase");
        Require(frame.SlotA.Entity.Motion.StatePhase == 0x47,
            "same-frame A845 consumes the freshly seeded right recoil $48->$47");
        Require(frame.SlotA.Attack70Post!.Value.Advanced == false,
            "late attack70 progression sees mutated $41 and does not advance to $78");
        Require(!frame.SlotA.Attack70Post.Value.CallsSecondarySpawnRoutine,
            "cancelled $70 cannot spawn its midpoint secondary object");
    }

    private static PlatformCommonEntityMotionState Motion(
        byte action,
        byte type,
        byte x,
        byte y,
        byte timer) =>
        new(
            ActionState: action,
            X: x,
            Y: y,
            StatePhase: 0,
            GroundDescriptor: 0xE0,
            DecisionTimer: timer,
            FlagsFacing: 0x40,
            Type: type,
            TerrainProbeRight: 0,
            TerrainProbeLeft: 0);

    private static PlatformStageMap OpenStage() =>
        new(0, [new PlatformStagePage(0, new byte[PlatformStagePage.DescriptorCount])]);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Common entity attack70 self-test failed: {label}");
    }
}

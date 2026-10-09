using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class EntityTypes0A0BChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckGenericProducerSeeds10AndScheduledProducerExcludesTypes();
        CheckSlowCadenceAndTerrainTurn();
        CheckRightHitConsumesMotion3SameFrame();
        CheckPersistedMotion3ContinuesWithoutNewHit();
        CheckLeftHitMovesNegative();
        CheckTerrainBlocksHitImpulse();
        CheckProximityFallStillRunsCurrentInteractionAndKnockback();
        CheckFallLandingReturnsBothTypesTo10();
        CheckRemovalIsTerminalRatherThanThirdActiveFamily();
        CheckInjectedUnreachableFamiliesRemainRejected();
    }

    private static void CheckGenericProducerSeeds10AndScheduledProducerExcludesTypes()
    {
        foreach (var type in new byte[] { 0x0A, 0x0B })
        {
            var result = PlatformCommonEdgeSpawner.StepSlot(
                GroundStage(),
                scrollX: 0,
                playerX: 0x40,
                cameraDelta43: 0,
                entropy48: 0,
                engine58: type,
                cooldown03B8: 0,
                new PlatformSpecialSpawnProfile(0x30, 2, 3, 0x10),
                ProducerExisting(),
                existingVisualSprite: 0xFE);

            Require(result.Spawned,
                $"generic B6D0 producer creates type {type:X2}");
            Require(result.Entity.Motion.Type == type && result.Entity.Motion.ActionState == 0x10,
                $"generic producer seeds type {type:X2} directly into the ordinary $10 family");
            Require(!PlatformScheduledSpecialEntitySpawner.IsSupportedType(type),
                $"scheduled $8925 producer cannot create type {type:X2}");
        }
    }

    private static void CheckSlowCadenceAndTerrainTurn()
    {
        var even = PlatformCommonEntityPreparation.StepOrdinaryMobile(
            Motion(0x10, 0x0A, 0x50, 0x50, phase: 0, ground: 0xE0, timer: 0x44, facing: 0x40, rightTerrain: 0),
            playerX: 0x20,
            playerY: 0x40,
            playerJumpPhase49: 0,
            entropy48: 0,
            frameCounter3C: 2,
            cameraDelta43: 0);
        Require(even.HorizontalDeltaBeforeCamera == 0 && even.State.X == 0x50,
            "type0A uses $3C&1 cadence and does not move on even frame");
        Require(even.Decision.Outcome == PlatformEntityDecisionOutcome.TerrainNoTurn,
            "type0A bypasses timer and performs terrain-facing test directly");
        Require(even.State.DecisionTimer == 0x44,
            "direct terrain route leaves decision timer untouched");
        Require(even.State.ActionState == 0x10,
            "ordinary 0A route does not invent another active family");

        var odd = PlatformCommonEntityPreparation.StepOrdinaryMobile(
            Motion(0x10, 0x0B, 0x50, 0x50, phase: 0, ground: 0xE0, timer: 0x33, facing: 0x40, rightTerrain: 0),
            playerX: 0x20,
            playerY: 0x40,
            playerJumpPhase49: 0,
            entropy48: 0,
            frameCounter3C: 3,
            cameraDelta43: 0);
        Require(odd.HorizontalDeltaBeforeCamera == 1 && odd.State.X == 0x51,
            "type0B moves one pixel on odd frame");
        Require(odd.State.DecisionTimer == 0x33,
            "type0B direct terrain route also preserves its timer byte");
        Require(odd.State.ActionState == 0x10,
            "ordinary 0B route remains in $10 without proximity fall");

        var turn = PlatformCommonEntityPreparation.StepOrdinaryMobile(
            Motion(0x10, 0x0A, 0x50, 0x50, phase: 0, ground: 0xE0, timer: 5, facing: 0x40, rightTerrain: 0x80),
            playerX: 0x20,
            playerY: 0x40,
            playerJumpPhase49: 0,
            entropy48: 0,
            frameCounter3C: 3,
            cameraDelta43: 0);
        Require(turn.Decision.Outcome == PlatformEntityDecisionOutcome.TerrainTurned,
            "type0A terrain descriptor $80 forces right-facing turn");
        Require(turn.State.FlagsFacing == 0x00 && turn.State.X == 0x4F,
            "turned type0A immediately walks one pixel left on odd frame");
    }

    private static void CheckRightHitConsumesMotion3SameFrame()
    {
        var attack = ActiveAttack(x: 0x50, y: 0x50, facing: 0x40);
        var frame = StepSingle0A0B(
            type: 0x0A,
            frameCounter: 2,
            attack: attack,
            rightTerrain: 0,
            playerX: 0x30,
            playerY: 0x40);

        Require(frame.SlotA!.Interaction!.HitSequence.Results[0].Result.Outcome == PlatformProjectileHitOutcome.KnockbackOnly,
            "type0A projectile hit takes knockback-only router");
        Require(frame.SlotA.Motion3KnockbackPost!.Value.Advanced,
            "A845 consumes newly written +$03 in same frame");
        Require(frame.SlotA.Motion3KnockbackPost.Value.State.StatePhase == 0x43,
            "right hit writes $44 then same-frame A845 decrements to $43");
        Require(frame.SlotA.Entity.Motion.X == 0x54,
            "right impulse applies +4 X after hit/contact");
        Require(frame.SlotA.Entity.HitPoints == 30,
            "type0A knockback-only route does not subtract HP");
        Require(frame.SlotA.Entity.Motion.ActionState == 0x10,
            "type0A projectile response changes +$03 but preserves action $10");
    }

    private static void CheckPersistedMotion3ContinuesWithoutNewHit()
    {
        var player = BasePlayer(PlatformAttackState.Empty, x: 0x30, y: 0x40);
        var entityA = RuntimeEntity(
            type: 0x0A,
            action: 0x10,
            x: 0x50,
            y: 0x50,
            phase: 0x43,
            ground: 0xE0,
            rightTerrain: 0);
        var entityB = RuntimeEntity(0x05, 0x10, 0x90, 0x50, 0, 0xE0, 0);

        var frame = PlatformTwoCommonEntityCombatSlice.StepNonFatal(
            OpenStage(),
            player,
            PlatformInput.None,
            new PlatformFrameResources(99, 100),
            new PlatformContactPhaseState(5, new ContactDrainState(0, 0)),
            entityA,
            entityB,
            PlatformHitboxParameters.Tall,
            PlatformHitboxParameters.Common,
            seventhSense: 0,
            frameCounter3C: 2,
            entropy48: 0,
            cameraDelta43: 0,
            engineSubstate02: 1);

        Require(frame.SlotA!.Motion3KnockbackPost!.Value.State.StatePhase == 0x42,
            "persisted $43 knockback advances to $42 without a new hit");
        Require(frame.SlotA.Entity.Motion.X == 0x54,
            "persisted right impulse moves another +4 even on zero-walk cadence frame");
    }

    private static void CheckLeftHitMovesNegative()
    {
        var frame = StepSingle0A0B(
            type: 0x0B,
            frameCounter: 2,
            attack: ActiveAttack(0x50, 0x50, facing: 0x00),
            rightTerrain: 0,
            playerX: 0x30,
            playerY: 0x40);

        Require(frame.SlotA!.Motion3KnockbackPost!.Value.State.StatePhase == 0x03,
            "left hit writes $04 then same-frame A845 decrements to $03");
        Require(frame.SlotA.Entity.Motion.X == 0x4C,
            "remaining value below $40 applies -4 X");
        Require(frame.SlotA.Entity.Motion.ActionState == 0x10,
            "left knockback also leaves the live family at $10");
    }

    private static void CheckTerrainBlocksHitImpulse()
    {
        var frame = StepSingle0A0B(
            type: 0x0A,
            frameCounter: 2,
            attack: ActiveAttack(0x50, 0x50, facing: 0x40),
            rightTerrain: 0xE0,
            playerX: 0x30,
            playerY: 0x40);

        var hit = frame.SlotA!.Interaction!.HitSequence.Results[0].Result;
        Require(hit.Outcome == PlatformProjectileHitOutcome.KnockbackOnly && hit.MotionWasTerrainBlocked,
            "$E0-$EF right terrain blocks the +$03 impulse write");
        Require(frame.SlotA.Motion3KnockbackPost!.Value.Advanced == false,
            "blocked impulse leaves nothing for A845 to consume");
        Require(frame.SlotA.Entity.Motion.StatePhase == 0,
            "blocked hit preserves zero motion3");
    }

    private static void CheckProximityFallStillRunsCurrentInteractionAndKnockback()
    {
        var player = BasePlayer(
            PlatformAttackState.Empty with { Slot0 = ActiveAttack(0x50, 0x46, 0x40) },
            x: 0x50,
            y: 0x60);
        var entityA = RuntimeEntity(
            type: 0x0A,
            action: 0x10,
            x: 0x50,
            y: 0x40,
            phase: 0,
            ground: 0xA8,
            rightTerrain: 0);
        var entityB = RuntimeEntity(0x05, 0x10, 0x90, 0x50, 0, 0xE0, 0);

        var frame = PlatformTwoCommonEntityCombatSlice.StepNonFatal(
            OpenStage(),
            player,
            PlatformInput.None,
            new PlatformFrameResources(99, 100),
            new PlatformContactPhaseState(5, new ContactDrainState(0, 0)),
            entityA,
            entityB,
            PlatformHitboxParameters.Tall,
            PlatformHitboxParameters.Common,
            seventhSense: 0,
            frameCounter3C: 2,
            entropy48: 0,
            cameraDelta43: 0,
            engineSubstate02: 1);

        Require(frame.SlotA!.Dispatch.Ordinary!.Value.ProximityFallStarted,
            "type0A ordinary path can start proximity $50");
        Require(frame.SlotA.Interaction is not null,
            "same entry update still reaches projectile/contact interaction after setting $50");
        Require(frame.SlotA.Interaction!.HitSequence.Results[0].Result.Outcome == PlatformProjectileHitOutcome.KnockbackOnly,
            "projectile can hit during the frame that proximity fall starts");
        Require(frame.SlotA.Entity.Motion.ActionState == 0x50,
            "knockback does not erase newly stored $50 action");
        Require(frame.SlotA.Entity.Motion.StatePhase == 0x43 && frame.SlotA.Entity.Motion.X == 0x54,
            "same-frame A845 consumes the hit impulse even though next frame will use fall50");
    }

    private static void CheckFallLandingReturnsBothTypesTo10()
    {
        foreach (var type in new byte[] { 0x0A, 0x0B })
        {
            var fall = PlatformCommonEntityFall50.Step(
                Motion(0x50, type, 0x50, 0x5F, phase: 7, ground: 0xA8, rightTerrain: 0),
                cameraDelta43: 0);

            Require(fall.Outcome == PlatformCommonEntityFall50Outcome.Landed,
                $"type {type:X2} fall reaches shared C491 landing");
            Require(fall.State.ActionState == 0x10 && fall.State.StatePhase == 0,
                $"type {type:X2} landing closes $50 back to $10");
        }
    }

    private static void CheckRemovalIsTerminalRatherThanThirdActiveFamily()
    {
        foreach (var type in new byte[] { 0x0A, 0x0B })
        {
            var entity = RuntimeEntity(type, 0x50, 0x50, 0xB0, 0, 0xE0, 0);
            var cleared = PlatformEntityRemovalA647.Apply(entity, visualSpritePlus1: 0xFD, engineState00: 0x20);
            Require(cleared.VisualSpritePlus1 == 0xFE && cleared.Entity.Motion.ActionState == 0,
                $"type {type:X2} retirement can clear action to terminal $00 while freeing the visual slot");

            var preserved = PlatformEntityRemovalA647.Apply(entity, visualSpritePlus1: 0xFD, engineState00: 0x30);
            Require(preserved.VisualSpritePlus1 == 0xFE && preserved.Entity.Motion.ActionState == 0x50,
                $"type {type:X2} high engine state may preserve old logical action only behind visual $FE retirement");
        }
    }

    private static void CheckInjectedUnreachableFamiliesRemainRejected()
    {
        foreach (var type in new byte[] { 0x0A, 0x0B })
        foreach (var family in new byte[] { 0x30, 0x40, 0x70, 0xD0, 0xE0 })
        {
            var rejected = false;
            try
            {
                PlatformCommonEntityActiveDispatcher.Step(
                    Motion(family, type, 0x50, 0x50, phase: 0, ground: 0xE0, rightTerrain: 0),
                    playerX: 0x20,
                    playerY: 0x40,
                    playerJumpPhase49: 0,
                    entropy48: 0,
                    frameCounter3C: 1,
                    cameraDelta43: 0);
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }

            Require(rejected,
                $"clean-room dispatcher rejects injected unreachable {family:X2} family for type {type:X2}");
        }
    }

    private static PlatformTwoCommonEntityCombatSliceResult StepSingle0A0B(
        byte type,
        byte frameCounter,
        PlatformAttackSlot attack,
        byte rightTerrain,
        byte playerX,
        byte playerY)
    {
        var player = BasePlayer(
            PlatformAttackState.Empty with { Slot0 = attack },
            playerX,
            playerY);
        var entityA = RuntimeEntity(type, 0x10, 0x50, 0x50, 0, 0xE0, rightTerrain);
        var entityB = RuntimeEntity(0x05, 0x10, 0x90, 0x50, 0, 0xE0, 0);

        return PlatformTwoCommonEntityCombatSlice.StepNonFatal(
            OpenStage(),
            player,
            PlatformInput.None,
            new PlatformFrameResources(99, 100),
            new PlatformContactPhaseState(5, new ContactDrainState(0, 0)),
            entityA,
            entityB,
            PlatformHitboxParameters.Tall,
            PlatformHitboxParameters.Common,
            seventhSense: 0,
            frameCounter3C: frameCounter,
            entropy48: 0,
            cameraDelta43: 0,
            engineSubstate02: 1);
    }

    private static PlatformAttackSlot ActiveAttack(byte x, byte y, byte facing) =>
        new(new PlatformAttackObject(y, 0x64, facing, x, 0, 0, 0, 0), 8);

    private static PlatformCommonEntityRuntimeState RuntimeEntity(
        byte type,
        byte action,
        byte x,
        byte y,
        byte phase,
        byte ground,
        byte rightTerrain) =>
        new(
            Motion(action, type, x, y, phase, ground, rightTerrain),
            HitPoints: 30,
            LifeDrainTicks: 2,
            CosmoDrainTicks: 3,
            SeventhSenseRewardBcd: 0x10);

    private static PlatformCommonEntityMotionState Motion(
        byte action,
        byte type,
        byte x,
        byte y,
        byte phase,
        byte ground,
        byte rightTerrain,
        byte timer = 0x44,
        byte facing = 0x40) =>
        new(
            ActionState: action,
            X: x,
            Y: y,
            StatePhase: phase,
            GroundDescriptor: ground,
            DecisionTimer: timer,
            FlagsFacing: facing,
            Type: type,
            TerrainProbeRight: rightTerrain,
            TerrainProbeLeft: 0);

    private static PlatformPlayerActionState BasePlayer(
        PlatformAttackState attack,
        byte x,
        byte y) =>
        new(
            PlatformSaintIndex.Seiya,
            new PlatformHorizontalState(x, 0, 0, 0x40),
            y,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            5,
            0,
            attack);

    private static PlatformStageMap GroundStage()
    {
        var descriptors = new byte[PlatformStagePage.DescriptorCount];
        descriptors[4 * PlatformStagePage.Columns] = 0xA8;
        return new PlatformStageMap(0, [new PlatformStagePage(0, descriptors)]);
    }

    private static PlatformCommonEntityRuntimeState ProducerExisting() =>
        new(
            new PlatformCommonEntityMotionState(
                ActionState: 0,
                X: 0x50,
                Y: 0x50,
                StatePhase: 0x22,
                GroundDescriptor: 0xE0,
                DecisionTimer: 0x11,
                FlagsFacing: 0x40,
                Type: 0x01,
                TerrainProbeRight: 0x88,
                TerrainProbeLeft: 0x99),
            HitPoints: 0x20,
            LifeDrainTicks: 1,
            CosmoDrainTicks: 1,
            SeventhSenseRewardBcd: 0x05);

    private static PlatformStageMap OpenStage() =>
        new(0, [new PlatformStagePage(0, new byte[PlatformStagePage.DescriptorCount])]);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Entity type 0A/0B self-test failed: {label}");
    }
}

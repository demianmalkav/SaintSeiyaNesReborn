using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class HazardAndCommonEntityFrameChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckAuxiliaryConsumesProjectileBeforeCommonEntity();
        CheckAuxiliaryContactSeedsLatchBeforeCommonEntity();
        CheckControlWithoutAuxiliaryLetsCommonEntityReceiveProjectile();
        CheckExceptionalPlayerExitSkipsBothPipelines();
    }

    private static void CheckAuxiliaryConsumesProjectileBeforeCommonEntity()
    {
        var frame = RunFrame(withAuxiliary: true);

        var auxHit = frame.AuxiliaryHazards!.SlotA.ProjectileHits!.Results[0].Result;
        Require(auxHit.Outcome == PlatformProjectileHitOutcome.DropReaction,
            "auxiliary slot A receives the overlapping projectile first");
        Require(auxHit.StandardConsumptionApplied,
            "Hyoga projectile is retired by auxiliary slot A");

        var commonHit = frame.CommonEntities!.SlotA.Interaction!.HitSequence.Results[0].Result;
        Require(commonHit.Outcome == PlatformProjectileHitOutcome.NoOverlap,
            "common entity A sees the projectile already retired by the earlier auxiliary pipeline");
        Require(frame.PlayerAfterLatePhases.State.AttackState.Slot0.Object.Type == 0xFE,
            "retired projectile remains inactive through later A22C");
        Require(frame.FrameCounterAdvanced && frame.FrameCounterAfter3C == 2,
            "composed normal frame increments $3C exactly once");
    }

    private static void CheckAuxiliaryContactSeedsLatchBeforeCommonEntity()
    {
        var frame = RunFrame(withAuxiliary: true);

        Require(frame.AuxiliaryHazards!.SlotA.Contact!.Value.Outcome == PlatformEntityContactOutcome.ContactTriggered,
            "auxiliary contact occurs before its projectile phase");
        Require(frame.AuxiliaryHazards.SlotA.LogicalEntityAfterInteraction.State == 0xE0,
            "later projectile hit can mutate logical auxiliary state without undoing earlier contact");

        var commonContact = frame.CommonEntities!.SlotA.Interaction!.ContactPhase.Contact;
        Require(commonContact.Outcome == PlatformEntityContactOutcome.ContactLatchActive,
            "common entity A sees auxiliary-seeded $76=$20 and cannot trigger a second contact");
        Require(frame.ContactState.HazardLatch76 == 0x20,
            "shared contact latch remains synchronized after auxiliary and common pipelines");
        Require(frame.ContactState.DrainState == new ContactDrainState(LifeTicks: 3, CosmoTicks: 2),
            "common entity cannot overwrite auxiliary drain counters in the same frame");
        Require(frame.PlayerAfterLatePhases.State.Special76 == frame.ContactState.HazardLatch76,
            "single physical $76 view remains synchronized at frame exit");
    }

    private static void CheckControlWithoutAuxiliaryLetsCommonEntityReceiveProjectile()
    {
        var frame = RunFrame(withAuxiliary: false);
        var commonHit = frame.CommonEntities!.SlotA.Interaction!.HitSequence.Results[0].Result;

        Require(commonHit.Outcome == PlatformProjectileHitOutcome.HpSurvived,
            "without an auxiliary slot, the same projectile reaches common entity A");
        Require(commonHit.StandardConsumptionApplied,
            "Hyoga consumes the projectile at common A in the control frame");
    }

    private static void CheckExceptionalPlayerExitSkipsBothPipelines()
    {
        var player = Player() with
        {
            ActionState4D = 0x80,
            Special76 = 0,
            AttackState = Player().AttackState with { ActionState4D = 0x80 },
        };
        var frame = PlatformHazardAndCommonEntityFrameSlice.StepNonFatal(
            Stage(),
            player,
            PlatformInput.None,
            new PlatformFrameResources(99, 100),
            new PlatformContactPhaseState(0, default),
            AuxiliaryState(withAuxiliary: true),
            CommonEntityA(),
            CommonEntityB(),
            PlatformHitboxParameters.Common,
            PlatformHitboxParameters.Common,
            seventhSense: 0,
            frameCounter3C: 1,
            entropy48: 0,
            cameraDelta43: 0,
            engineSubstate02: 1);

        Require(frame.ExitedBeforeLateObjectPipeline,
            "$80/$76=0 reload exits before C2D7 late object classes");
        Require(frame.AuxiliaryHazards is null && frame.CommonEntities is null,
            "exceptional player exit skips both auxiliary and common pipelines");
        Require(!frame.FrameCounterAdvanced && frame.FrameCounterAfter3C == 1,
            "exceptional reload skips normal C402 frame-counter increment");
    }

    private static PlatformHazardAndCommonEntityFrameResult RunFrame(bool withAuxiliary) =>
        PlatformHazardAndCommonEntityFrameSlice.StepNonFatal(
            Stage(),
            Player(),
            PlatformInput.None,
            new PlatformFrameResources(99, 100),
            new PlatformContactPhaseState(0, default),
            AuxiliaryState(withAuxiliary),
            CommonEntityA(),
            CommonEntityB(),
            PlatformHitboxParameters.Common,
            PlatformHitboxParameters.Common,
            seventhSense: 0,
            frameCounter3C: 1,
            entropy48: 0,
            cameraDelta43: 0,
            engineSubstate02: 1);

    private static PlatformPlayerActionState Player()
    {
        var projectile = new PlatformAttackSlot(
            new PlatformAttackObject(
                Y: 0x40,
                Type: 0x64,
                Facing: 0x40,
                X: 0x40,
                Field4: 0,
                Field5: 0,
                Field6: 0,
                AuxiliaryX: 0),
            RangeCounter: 3);

        return new PlatformPlayerActionState(
            PlatformSaintIndex.Hyoga,
            new PlatformHorizontalState(0x40, 0, 0, 0x40),
            PlayerY: 0x25,
            PlayerYHigh41: 0,
            ActionState4D: 0,
            JumpPhase49: 0,
            JumpButtonLatch4A: 0,
            HighJumpSelector038A: 0,
            DropButtonLatch038C: 0,
            Support038D: 0,
            Special76: 0,
            HorizontalAmount43: 0,
            AttackState: PlatformAttackState.Empty with { Slot0 = projectile });
    }

    private static PlatformAuxiliaryHazardSpawnerState AuxiliaryState(bool withAuxiliary)
    {
        var slot = withAuxiliary
            ? PlatformAuxiliaryHazardSlot.Empty with
            {
                Y0 = 0x40,
                SpriteType1 = 0x80,
                Flags2 = 0x40,
                X3 = 0x3E,
                MirrorFlags6 = 0x40,
            }
            : PlatformAuxiliaryHazardSlot.Empty;

        return new PlatformAuxiliaryHazardSpawnerState(
            CurrentKind03B4: 0,
            ActiveKind03B5: 1,
            Cooldown03B6: 0,
            HorizontalStep03A5: 0,
            VerticalStep03A6: 0,
            SlotA: slot,
            SlotB: PlatformAuxiliaryHazardSlot.Empty,
            MetadataA: new PlatformAuxiliaryHazardMetadata(
                Kind09: 1,
                HitPoints0C: 0x1E,
                CosmoDrainTicks0D: 2,
                LifeDrainTicks0E: 3,
                SeventhSenseReward0F: 0x32),
            MetadataB: default);
    }

    private static PlatformCommonEntityRuntimeState CommonEntityA() =>
        new(
            new PlatformCommonEntityMotionState(
                ActionState: 0x10,
                X: 0x3F,
                Y: 0x40,
                StatePhase: 0,
                GroundDescriptor: 0xE0,
                DecisionTimer: 5,
                FlagsFacing: 0x40,
                Type: 0x05,
                TerrainProbeRight: 0,
                TerrainProbeLeft: 0),
            HitPoints: 100,
            LifeDrainTicks: 8,
            CosmoDrainTicks: 9,
            SeventhSenseRewardBcd: 0x10);

    private static PlatformCommonEntityRuntimeState CommonEntityB() =>
        new(
            new PlatformCommonEntityMotionState(
                ActionState: 0x10,
                X: 0x90,
                Y: 0x40,
                StatePhase: 0,
                GroundDescriptor: 0xE0,
                DecisionTimer: 5,
                FlagsFacing: 0x40,
                Type: 0x05,
                TerrainProbeRight: 0,
                TerrainProbeLeft: 0),
            HitPoints: 100,
            LifeDrainTicks: 1,
            CosmoDrainTicks: 1,
            SeventhSenseRewardBcd: 0x10);

    private static PlatformStageMap Stage()
    {
        var descriptors = Enumerable.Repeat((byte)0x90, PlatformStagePage.DescriptorCount).ToArray();
        return new PlatformStageMap(0, [new PlatformStagePage(0, descriptors)]);
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Hazard/common frame self-test failed: {label}");
    }
}

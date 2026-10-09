using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class TwoCommonEntityCombatSliceChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckSlotAProjectileConsumptionAffectsSlotB();
        CheckSlotAContactLatchSuppressesSlotB();
    }

    private static void CheckSlotAProjectileConsumptionAffectsSlotB()
    {
        var active = new PlatformAttackSlot(
            new PlatformAttackObject(0x50, 0x64, 0x40, 0x50, 0, 0, 0, 0),
            3);
        var player = BasePlayer(
            PlatformSaintIndex.Hyoga,
            x: 0x40,
            y: 0x50,
            latch: 5,
            attack: PlatformAttackState.Empty with { Slot0 = active });
        var entityA = RuntimeEntity(hp: 30, lifeTicks: 2, cosmoTicks: 3);
        var entityB = RuntimeEntity(hp: 30, lifeTicks: 4, cosmoTicks: 5);

        var frame = PlatformTwoCommonEntityCombatSlice.StepNonFatal(
            OpenStage(),
            player,
            PlatformInput.None,
            new PlatformFrameResources(99, 50),
            new PlatformContactPhaseState(5, new ContactDrainState(0, 0)),
            entityA,
            entityB,
            PlatformHitboxParameters.Common,
            PlatformHitboxParameters.Common,
            seventhSense: 0,
            frameCounter3C: 1,
            entropy48: 0,
            cameraDelta43: 1,
            engineSubstate02: 1);

        Require(frame.PostPlayerLatch.After76 == 4,
            "$B94B decrements latch before both common entity slots");
        Require(frame.PrePlayer.PlatformDamage == 10,
            "Hyoga Cosmo50 supplies 10 damage for both ordered slots");

        var hitA = frame.SlotA!.Interaction!.HitSequence.Results[0].Result;
        Require(hitA.Outcome == PlatformProjectileHitOutcome.HpSurvived,
            "slot A sees active Hyoga projectile first");
        Require(hitA.StandardConsumptionApplied,
            "slot A standard hit consumes Hyoga projectile");
        Require(frame.SlotA.Entity.HitPoints == 20,
            "slot A takes the 10 damage");

        var hitB = frame.SlotB!.Interaction!.HitSequence.Results[0].Result;
        Require(hitB.Outcome == PlatformProjectileHitOutcome.NoOverlap,
            "slot B receives already-retired attack state from slot A");
        Require(frame.SlotB.Entity.HitPoints == 30,
            "slot B takes no damage from projectile consumed in slot A");

        var finalSlot = frame.PlayerAfterLatePhases.State.AttackState.Slot0;
        Require(finalSlot.Object.Type == 0xFE && finalSlot.Object.Y == 0xF0,
            "retired Hyoga projectile remains inactive through later A22C");
        Require(finalSlot.RangeCounter == 3,
            "retired projectile is not aged after slot A consumption");
        Require(frame.FrameCounterAdvanced && frame.FrameCounterAfter3C == 2,
            "two-slot processing still reaches one shared C402 increment");
    }

    private static void CheckSlotAContactLatchSuppressesSlotB()
    {
        var player = BasePlayer(
            PlatformSaintIndex.Seiya,
            x: 0x50,
            y: 0x50,
            latch: 0,
            attack: PlatformAttackState.Empty);
        var entityA = RuntimeEntity(hp: 30, lifeTicks: 2, cosmoTicks: 3);
        var entityB = RuntimeEntity(hp: 30, lifeTicks: 5, cosmoTicks: 6);

        var frame = PlatformTwoCommonEntityCombatSlice.StepNonFatal(
            OpenStage(),
            player,
            PlatformInput.None,
            new PlatformFrameResources(99, 100),
            new PlatformContactPhaseState(0, new ContactDrainState(0, 0)),
            entityA,
            entityB,
            PlatformHitboxParameters.Common,
            PlatformHitboxParameters.Common,
            seventhSense: 0,
            frameCounter3C: 1,
            entropy48: 0,
            cameraDelta43: 1,
            engineSubstate02: 1);

        Require(frame.SlotA!.Interaction!.ContactPhase.Contact.Triggered,
            "slot A contacts player first");
        Require(frame.SlotA.Interaction.ContactPhase.State.HazardLatch76 == 0x20,
            "slot A seeds shared $76 latch");
        Require(frame.SlotA.Interaction.ContactPhase.State.DrainState == new ContactDrainState(2, 3),
            "slot A seeds its own drain profile");

        Require(frame.SlotB!.Interaction!.ContactPhase.Contact.Outcome == PlatformEntityContactOutcome.ContactLatchActive,
            "slot B sees slot A's same-frame latch and cannot contact again");
        Require(frame.ContactState.HazardLatch76 == 0x20,
            "shared latch remains seeded by slot A");
        Require(frame.ContactState.DrainState == new ContactDrainState(2, 3),
            "suppressed slot B does not overwrite slot A drain counters with 5/6");
        Require(frame.PlayerAfterLatePhases.State.Special76 == 0x20,
            "player view of physical $76 matches shared two-slot contact state");
    }

    private static PlatformCommonEntityRuntimeState RuntimeEntity(
        byte hp,
        byte lifeTicks,
        byte cosmoTicks) =>
        new(
            new PlatformCommonEntityMotionState(
                ActionState: 0x10,
                X: 0x50,
                Y: 0x50,
                StatePhase: 0,
                GroundDescriptor: 0xE0,
                DecisionTimer: 5,
                FlagsFacing: 0x40,
                Type: 0x05,
                TerrainProbeRight: 0,
                TerrainProbeLeft: 0),
            HitPoints: hp,
            LifeDrainTicks: lifeTicks,
            CosmoDrainTicks: cosmoTicks,
            SeventhSenseRewardBcd: 0x10);

    private static PlatformPlayerActionState BasePlayer(
        PlatformSaintIndex saint,
        byte x,
        byte y,
        byte latch,
        PlatformAttackState attack) =>
        new(
            saint,
            new PlatformHorizontalState(x, 0, 0, 0x40),
            y,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            latch,
            0,
            attack);

    private static PlatformStageMap OpenStage() =>
        new(0, [new PlatformStagePage(0, new byte[PlatformStagePage.DescriptorCount])]);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Two common entity slice self-test failed: {label}");
    }
}

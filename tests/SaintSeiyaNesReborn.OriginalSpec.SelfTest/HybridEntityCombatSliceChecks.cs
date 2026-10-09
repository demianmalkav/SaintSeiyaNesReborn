using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class HybridEntityCombatSliceChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckCommonAProjectileConsumptionCarriesIntoSpecialB();
        CheckSpecialAContactLatchSuppressesCommonB();
        CheckGlobal039AThreadsAcrossTwoSpecialSlots();
        CheckVisualFreeInactiveSlotSkipsBeforeRouteValidation();
    }

    private static void CheckCommonAProjectileConsumptionCarriesIntoSpecialB()
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

        var frame = Step(
            player,
            new PlatformContactPhaseState(5, default),
            PlatformHybridEntitySlotState.Common(CommonEntity(hp: 30, life: 2, cosmo: 3), 0xFD),
            PlatformHybridEntitySlotState.Special08090C(
                SpecialEntity(type: 0x08, action: 0x00, phase: 0, x: 0x50, y: 0x50, hp: 30),
                0xFD,
                control04: 1),
            seventhSense: 0,
            global039A: 0x20,
            frame3C: 1,
            entropy48: 0,
            cameraDelta43: 0);

        Require(frame.SlotA?.Route == PlatformHybridEntitySlotRoute.Common,
            "slot A routes through common runtime");
        Require(frame.SlotB?.Route == PlatformHybridEntitySlotRoute.Special08090C,
            "slot B routes through special 08/09/0C runtime");

        var hitA = frame.SlotA!.Common!.Interaction!.HitSequence.Results[0].Result;
        Require(hitA.Outcome == PlatformProjectileHitOutcome.HpSurvived && hitA.StandardConsumptionApplied,
            "common slot A consumes the overlapping Hyoga projectile");
        Require(frame.SlotA.State.Entity.HitPoints == 20,
            "common slot A receives shared platform damage");

        var hitB = frame.SlotB!.Special!.MainInteraction!.HitSequence.Results[0].Result;
        Require(hitB.Outcome == PlatformProjectileHitOutcome.NoOverlap,
            "special slot B receives the already-retired attack state from slot A");
        Require(frame.SlotB.State.Entity.HitPoints == 30,
            "special slot B is not hit again by the consumed projectile");
        Require(frame.PlayerAfterLatePhases.State.AttackState.Slot0.Object.Type == 0xFE,
            "retired attack state survives the hybrid pipeline and late attack-object phase");
    }

    private static void CheckSpecialAContactLatchSuppressesCommonB()
    {
        var player = BasePlayer(
            PlatformSaintIndex.Seiya,
            x: 0x50,
            y: 0x50,
            latch: 0,
            attack: PlatformAttackState.Empty);

        var frame = Step(
            player,
            new PlatformContactPhaseState(0, default),
            PlatformHybridEntitySlotState.Special08090C(
                SpecialEntity(type: 0x08, action: 0x00, phase: 0, x: 0x50, y: 0x50, hp: 30, life: 2, cosmo: 3),
                0xFD,
                control04: 1),
            PlatformHybridEntitySlotState.Common(CommonEntity(hp: 30, life: 5, cosmo: 6), 0xFD),
            seventhSense: 0,
            global039A: 0,
            frame3C: 1,
            entropy48: 0,
            cameraDelta43: 0);

        Require(frame.SlotA!.Special!.MainInteraction!.ContactPhase.Contact.Triggered,
            "special slot A contacts the player before slot B");
        Require(frame.SlotA.Special.MainInteraction.ContactPhase.State.HazardLatch76 == 0x20,
            "special slot A seeds shared $76");
        Require(frame.SlotA.Special.MainInteraction.ContactPhase.State.DrainState == new ContactDrainState(2, 3),
            "special slot A seeds its own Life/Cosmo drain profile");

        Require(frame.SlotB!.Common!.Interaction!.ContactPhase.Contact.Outcome == PlatformEntityContactOutcome.ContactLatchActive,
            "common slot B sees the special slot A latch in the same frame");
        Require(frame.ContactState.HazardLatch76 == 0x20,
            "hybrid shared contact state preserves slot A latch");
        Require(frame.ContactState.DrainState == new ContactDrainState(2, 3),
            "suppressed common slot B cannot overwrite slot A drain counters");
        Require(frame.PlayerAfterLatePhases.State.Special76 == 0x20,
            "player physical $76 view stays synchronized with hybrid contact state");
    }

    private static void CheckGlobal039AThreadsAcrossTwoSpecialSlots()
    {
        var player = BasePlayer(
            PlatformSaintIndex.Seiya,
            x: 0x10,
            y: 0x20,
            latch: 0,
            attack: PlatformAttackState.Empty);

        var frame = Step(
            player,
            new PlatformContactPhaseState(0, default),
            PlatformHybridEntitySlotState.Special08090C(
                SpecialEntity(type: 0x08, action: 0x00, phase: 0, x: 0x70, y: 0x50),
                0xFD),
            PlatformHybridEntitySlotState.Special08090C(
                SpecialEntity(type: 0x09, action: 0x00, phase: 0, x: 0x80, y: 0x50),
                0xFD),
            seventhSense: 0,
            global039A: 0x7E,
            frame3C: 1,
            entropy48: 0x45,
            cameraDelta43: 0);

        Require(frame.SlotA!.Special!.PreDispatch.Outcome == PlatformSpecialEntityPreDispatchOutcome.CounterAdvanced,
            "slot A advances shared $039A from $7E to $7F");
        Require(frame.SlotA.Special.PreDispatch.GlobalCounter039A == 0x7F,
            "slot A exposes the intermediate $7F global value");

        Require(frame.SlotB!.Special!.PreDispatch.Outcome == PlatformSpecialEntityPreDispatchOutcome.Attack70Started,
            "slot B receives slot A's $7F and reaches the $80 threshold");
        Require(frame.SlotB.Special.PreDispatch.GlobalCounter039A == 0x05,
            "slot B resets $039A to entropy low six bits after the threshold");
        Require(frame.GlobalCounter039A == 0x05,
            "frame result exposes the final ordered shared $039A value");
    }

    private static void CheckVisualFreeInactiveSlotSkipsBeforeRouteValidation()
    {
        var player = BasePlayer(
            PlatformSaintIndex.Seiya,
            x: 0x10,
            y: 0x20,
            latch: 0,
            attack: PlatformAttackState.Empty);
        var unsupportedIfRouted = new PlatformCommonEntityRuntimeState(
            new PlatformCommonEntityMotionState(0x00, 0, 0, 0, 0, 0, 0, 0, 0, 0),
            0, 0, 0, 0);

        var frame = Step(
            player,
            new PlatformContactPhaseState(0, default),
            PlatformHybridEntitySlotState.Common(unsupportedIfRouted, 0xFE),
            PlatformHybridEntitySlotState.Common(CommonEntity(hp: 30, life: 1, cosmo: 2), 0xFD),
            seventhSense: 0,
            global039A: 0x22,
            frame3C: 1,
            entropy48: 0,
            cameraDelta43: 0);

        Require(frame.SlotA!.Route == PlatformHybridEntitySlotRoute.Skipped
            && frame.SlotA.Activity.Outcome == PlatformCommonSlotActivityOutcome.VisualFreeInactive,
            "visual-free ordinary slot is skipped before common-route validation");
        Require(frame.SlotA.Common is null && frame.SlotA.Special is null,
            "skipped slot executes no runtime body");
        Require(frame.SlotB!.Route == PlatformHybridEntitySlotRoute.Common,
            "slot B still executes after skipped slot A");
        Require(frame.GlobalCounter039A == 0x22,
            "skipped/common slots leave unrelated special global counter unchanged");
        Require(frame.FrameCounterAdvanced && frame.FrameCounterAfter3C == 2,
            "hybrid pipeline reaches one shared frame-counter increment");
    }

    private static PlatformHybridEntityCombatSliceResult Step(
        PlatformPlayerActionState player,
        PlatformContactPhaseState contact,
        PlatformHybridEntitySlotState slotA,
        PlatformHybridEntitySlotState slotB,
        int seventhSense,
        byte global039A,
        byte frame3C,
        byte entropy48,
        byte cameraDelta43) =>
        PlatformHybridEntityCombatSlice.StepNonFatal(
            OpenStage(),
            player,
            PlatformInput.None,
            new PlatformFrameResources(99, 50),
            contact,
            slotA,
            slotB,
            PlatformHitboxParameters.Common,
            PlatformHitboxParameters.Common,
            seventhSense,
            global039A,
            frame3C,
            entropy48,
            cameraDelta43,
            engineSubstate02: 1,
            engineState00: 0x10,
            alternateParent08_03AB: 0x66);

    private static PlatformCommonEntityRuntimeState CommonEntity(
        byte hp,
        byte life,
        byte cosmo) =>
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
            LifeDrainTicks: life,
            CosmoDrainTicks: cosmo,
            SeventhSenseRewardBcd: 0x10);

    private static PlatformCommonEntityRuntimeState SpecialEntity(
        byte type,
        byte action,
        byte phase,
        byte x,
        byte y,
        byte hp = 30,
        byte life = 2,
        byte cosmo = 3) =>
        new(
            new PlatformCommonEntityMotionState(
                ActionState: action,
                X: x,
                Y: y,
                StatePhase: phase,
                GroundDescriptor: 0xA8,
                DecisionTimer: 0,
                FlagsFacing: 0x40,
                Type: type,
                TerrainProbeRight: 0,
                TerrainProbeLeft: 0),
            HitPoints: hp,
            LifeDrainTicks: life,
            CosmoDrainTicks: cosmo,
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
            throw new InvalidOperationException($"Hybrid entity slice self-test failed: {label}");
    }
}

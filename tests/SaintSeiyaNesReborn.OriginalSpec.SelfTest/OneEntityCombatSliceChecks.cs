using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class OneEntityCombatSliceChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckIntegratedDamageContactAndProjectileMotion();
        CheckLatchOneExpiresBeforeSameFrameContact();
        CheckReloadSkipsLatePipelineAndFrameCounter();
    }

    private static void CheckIntegratedDamageContactAndProjectileMotion()
    {
        var player = BasePlayer(action: 0x00, latch: 0);
        var contact = new PlatformContactPhaseState(
            HazardLatch76: 0,
            DrainState: new ContactDrainState(LifeTicks: 0, CosmoTicks: 1));
        var entity = new PlatformCombatEntity(
            State: 0x10,
            X: 0x45,
            Y: 0x50,
            Type: 0x05,
            HitPoints: 40,
            SeventhSenseRewardBcd: 0x10);

        var frame = PlatformOneEntityCombatSlice.StepNonFatal(
            OpenStage(),
            player,
            PlatformInput.B,
            new PlatformFrameResources(Life: 99, Cosmo: 200),
            contact,
            entity,
            entityLifeDrainTicks: 2,
            entityCosmoDrainTicks: 3,
            PlatformHitboxParameters.Common,
            seventhSense: 100,
            frameCounter3C: 1,
            engineSubstate02: 1);

        Require(frame.PrePlayer.ResourcesAfterDrain.Cosmo == 199,
            "pre-player Cosmo drain applies first");
        Require(frame.PrePlayer.PlatformDamage == 36,
            "post-drain Cosmo 199 produces Seiya damage 36");
        Require(frame.PrePlayer.Player.AttackAttempt?.Outcome == PlatformAttackAttemptOutcome.Created,
            "B creates attack after pre-player resource phase");
        Require(frame.PrePlayer.Player.State.AttackState.Slot0.Object.X == 0x52,
            "projectile exists at birth X before entity phase");
        Require(frame.PrePlayer.Player.State.AttackState.Slot0.RangeCounter == 3,
            "post-drain Cosmo selects lower range bracket");

        Require(frame.Interaction is not null,
            "normal frame reaches common entity interaction");
        Require(frame.Interaction!.HitSequence.Results[0].Result.Outcome == PlatformProjectileHitOutcome.HpSurvived,
            "projectile hits entity before later contact/motion");
        Require(frame.Entity.HitPoints == 4 && frame.Entity.State == 0x40,
            "same-frame damage reduces HP 40->4 and enters reaction state");
        Require(frame.SeventhSense == 100,
            "surviving entity does not award Seventh Sense");

        Require(frame.Interaction.ContactPhase.Contact.Triggered,
            "entity contact follows projectile hit in same A442 interaction");
        Require(frame.ContactState.HazardLatch76 == 0x20,
            "contact seeds shared $76 latch to 32");
        Require(frame.ContactState.DrainState == new ContactDrainState(2, 3),
            "contact seeds next-frame Life/Cosmo drain counters");
        Require(frame.PlayerAfterLatePhases.State.Special76 == 0x20,
            "player view of physical $76 is resynchronized after contact");

        var slot = frame.PlayerAfterLatePhases.State.AttackState.Slot0;
        Require(slot.Object.X == 0x57,
            "surviving Seiya projectile moves +5 only after hit/contact phases");
        Require(slot.Object.Type == 0x65,
            "A22C uses current frameCounter parity before C402 increment");
        Require(slot.RangeCounter == 2,
            "A22C decrements post-hit projectile range 3->2");
        Require(frame.FrameCounterBefore3C == 1 && frame.FrameCounterAfter3C == 2,
            "C402 increments frame counter only after late attack update");
        Require(frame.FrameCounterAdvanced,
            "normal slice reaches frame-counter increment");
    }

    private static void CheckLatchOneExpiresBeforeSameFrameContact()
    {
        var player = BasePlayer(action: 0x00, latch: 1);
        var contact = new PlatformContactPhaseState(1, new ContactDrainState(0, 0));
        var entity = new PlatformCombatEntity(0x10, 0x40, 0x50, 0x05, 40, 0);

        var frame = PlatformOneEntityCombatSlice.StepNonFatal(
            OpenStage(),
            player,
            PlatformInput.None,
            new PlatformFrameResources(99, 100),
            contact,
            entity,
            entityLifeDrainTicks: 1,
            entityCosmoDrainTicks: 1,
            PlatformHitboxParameters.Common,
            seventhSense: 0,
            frameCounter3C: 7,
            engineSubstate02: 1);

        Require(frame.PostPlayerLatch.Before76 == 1 && frame.PostPlayerLatch.After76 == 0,
            "$B94B expires latch 1->0 before A442");
        Require(frame.Interaction!.ContactPhase.Contact.Triggered,
            "same-frame contact sees expired zero latch and may immediately retrigger");
        Require(frame.ContactState.HazardLatch76 == 0x20,
            "retrigger reseeds shared latch to 32 in that same frame");
    }

    private static void CheckReloadSkipsLatePipelineAndFrameCounter()
    {
        var attack = PlatformAttackState.Empty with
        {
            Slot0 = new PlatformAttackSlot(
                new PlatformAttackObject(0x57, 0x64, 0x40, 0x52, 0, 0, 0, 0),
                3),
        };
        var player = BasePlayer(action: 0x80, latch: 0, attack: attack);
        var contact = new PlatformContactPhaseState(0, new ContactDrainState(0, 0));
        var entity = new PlatformCombatEntity(0x10, 0x45, 0x50, 0x05, 40, 0);

        var frame = PlatformOneEntityCombatSlice.StepNonFatal(
            OpenStage(),
            player,
            PlatformInput.None,
            new PlatformFrameResources(99, 100),
            contact,
            entity,
            entityLifeDrainTicks: 2,
            entityCosmoDrainTicks: 3,
            PlatformHitboxParameters.Common,
            seventhSense: 0,
            frameCounter3C: 9,
            engineSubstate02: 1);

        Require(frame.ExitedBeforeLatePipeline,
            "$80/$76=0 reload exits before A442/A22C");
        Require(frame.Interaction is null,
            "reload has no common entity interaction");
        Require(frame.PlayerAfterLatePhases.State.AttackState == attack,
            "reload leaves existing projectile untouched because A22C is skipped");
        Require(!frame.FrameCounterAdvanced && frame.FrameCounterAfter3C == 9,
            "reload jump also skips later C402 frame-counter increment");
    }

    private static PlatformPlayerActionState BasePlayer(
        byte action,
        byte latch,
        PlatformAttackState? attack = null) =>
        new(
            PlatformSaintIndex.Seiya,
            new PlatformHorizontalState(0x40, 0, 0, 0x40),
            0x50,
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
            throw new InvalidOperationException($"One-entity combat slice self-test failed: {label}");
    }
}

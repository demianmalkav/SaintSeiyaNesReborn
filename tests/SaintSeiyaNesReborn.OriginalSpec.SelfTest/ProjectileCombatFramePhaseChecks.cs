using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class ProjectileCombatFramePhaseChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckNewSeiyaProjectileHitsAtBirthThenMoves();
        CheckConsumedHyogaProjectileNeverGetsSameFrameMotion();
        CheckReloadSkipsBothLatePhases();
    }

    private static void CheckNewSeiyaProjectileHitsAtBirthThenMoves()
    {
        var stage = OpenStage();
        var state = BaseState(PlatformSaintIndex.Seiya, action: 0x00, x: 0x40, y: 0x50);
        state = PlatformAttackFramePhases.AdvanceBusyBeforePlayer(state);

        var player = PlatformPlayerActionDispatcher.Step(
            stage,
            state,
            PlatformInput.B,
            frameCounter3C: 0,
            cosmo: 100);

        Require(player.AttackAttempt?.Outcome == PlatformAttackAttemptOutcome.Created,
            "B creates Seiya projectile during player phase");
        Require(player.State.AttackState.Slot0.Object.X == 0x52,
            "new projectile begins at BBCA birth X");
        Require(player.State.AttackState.Slot0.Object.Y == 0x57,
            "new projectile begins at BBCA birth Y");
        Require(player.State.AttackState.Slot0.RangeCounter == 3,
            "Seiya Cosmo100 birth range is 3 before A22C");

        // Common hitbox high-X is entityX+13. entityX=$45 makes $52 exactly
        // the inclusive high edge: the birth coordinate hits, but $57 after a
        // hypothetical early +5 movement would miss. This locks A442/9915
        // before A22C rather than merely observing that both happen in-frame.
        var entity = new PlatformCombatEntity(
            State: 0x10,
            X: 0x45,
            Y: 0x50,
            Type: 0x05,
            HitPoints: 30,
            SeventhSenseRewardBcd: 0);

        var frame = PlatformProjectileCombatFramePhases.ResolveHitsThenAdvanceAttackObjects(
            player,
            entity,
            PlatformHitboxParameters.Common,
            platformDamage: 10,
            seventhSense: 0,
            engineSubstate02: 1,
            frameCounter3C: 0);

        Require(frame.HitSequence is not null,
            "normal platform frame reaches entity hit phase");
        Require(frame.HitSequence!.Results[0].Result.Outcome == PlatformProjectileHitOutcome.HpSurvived,
            "slot0 collides at its birth coordinate before motion");
        Require(frame.Entity.HitPoints == 20 && frame.Entity.State == 0x40,
            "birth-coordinate hit applies HP damage and reaction");
        Require(frame.PlayerAfterHitPhase.State.AttackState.Slot0.Object.X == 0x57,
            "surviving Seiya projectile moves +5 only after collision resolution");
        Require(frame.PlayerAfterHitPhase.State.AttackState.Slot0.RangeCounter == 2,
            "A22C decrements range only after the hit phase");
    }

    private static void CheckConsumedHyogaProjectileNeverGetsSameFrameMotion()
    {
        var stage = OpenStage();
        var state = BaseState(PlatformSaintIndex.Hyoga, action: 0x00, x: 0x40, y: 0x50);
        state = PlatformAttackFramePhases.AdvanceBusyBeforePlayer(state);

        var player = PlatformPlayerActionDispatcher.Step(
            stage,
            state,
            PlatformInput.B,
            frameCounter3C: 1,
            cosmo: 100);

        Require(player.State.AttackState.Slot0.Object.X == 0x52,
            "Hyoga projectile shares birth X");
        Require(player.State.AttackState.Slot0.RangeCounter == 4,
            "Hyoga Cosmo100 birth range is 4");

        var entity = new PlatformCombatEntity(
            State: 0x10,
            X: 0x45,
            Y: 0x50,
            Type: 0x05,
            HitPoints: 30,
            SeventhSenseRewardBcd: 0);

        var frame = PlatformProjectileCombatFramePhases.ResolveHitsThenAdvanceAttackObjects(
            player,
            entity,
            PlatformHitboxParameters.Common,
            platformDamage: 10,
            seventhSense: 0,
            engineSubstate02: 1,
            frameCounter3C: 1);

        var slot = frame.PlayerAfterHitPhase.State.AttackState.Slot0;
        Require(frame.HitSequence!.Results[0].Result.StandardConsumptionApplied,
            "Hyoga standard hit consumes projectile during entity phase");
        Require(slot.Object.Type == 0xFE && slot.Object.Y == 0xF0,
            "consumed projectile remains retired when later A22C runs");
        Require(slot.RangeCounter == 4,
            "retirement before A22C prevents same-frame range decrement");
    }

    private static void CheckReloadSkipsBothLatePhases()
    {
        var stage = OpenStage();
        var attack = PlatformAttackState.Empty with
        {
            Slot0 = new PlatformAttackSlot(
                new PlatformAttackObject(0x57, 0x64, 0x40, 0x52, 0, 0, 0, 0),
                3),
        };
        var state = BaseState(
            PlatformSaintIndex.Seiya,
            action: 0x80,
            x: 0x40,
            y: 0x50,
            special76: 0,
            attack: attack);

        var player = PlatformPlayerActionDispatcher.Step(
            stage,
            state,
            PlatformInput.None,
            frameCounter3C: 0,
            cosmo: 100);
        Require(player.ExitsNormalPlayerLoop, "$80/$76=0 takes exceptional reload path");

        var entity = new PlatformCombatEntity(0x10, 0x45, 0x50, 0x05, 30, 0);
        var frame = PlatformProjectileCombatFramePhases.ResolveHitsThenAdvanceAttackObjects(
            player,
            entity,
            PlatformHitboxParameters.Common,
            platformDamage: 10,
            seventhSense: 0,
            engineSubstate02: 1,
            frameCounter3C: 0);

        Require(frame.SkippedBecausePlayerLoopExited, "reload skips late normal-frame pipeline");
        Require(frame.HitSequence is null, "reload never reaches A442/9915 hit phase");
        Require(frame.PlayerAfterHitPhase.State.AttackState == attack,
            "reload also skips A22C projectile motion");
        Require(frame.Entity == entity, "reload leaves would-be entity phase untouched");
    }

    private static PlatformPlayerActionState BaseState(
        PlatformSaintIndex saint,
        byte action,
        byte x,
        byte y,
        byte special76 = 0,
        PlatformAttackState? attack = null) =>
        new(
            saint,
            new PlatformHorizontalState(x, 0, 0, 0x40),
            y,
            0,
            action,
            0,
            0,
            0,
            0,
            0,
            special76,
            0,
            attack ?? PlatformAttackState.Empty);

    private static PlatformStageMap OpenStage() =>
        new(0, [new PlatformStagePage(0, new byte[PlatformStagePage.DescriptorCount])]);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Projectile combat frame phase self-test failed: {label}");
    }
}

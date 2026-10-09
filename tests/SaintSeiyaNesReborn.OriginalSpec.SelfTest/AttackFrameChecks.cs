using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class AttackFrameChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckCreateHitThenMove();
        CheckBusyBeforeInput();
        CheckRangeOneHitsBeforeRetirement();
        CheckShunSecondSegmentAppearsAfterHits();
    }

    private static void CheckCreateHitThenMove()
    {
        // playerX $3E -> right-facing origin X $50; playerY $39 -> origin Y $40.
        // Reduced target at X $4A has high X bound exactly $50. If the generic
        // projectile moved its +5 first it would be outside; original hits first.
        var target = new PlatformHitTarget(
            new PlatformCombatEntity(0x10, 0x4A, 0x40, 5, 20, 0),
            PlatformHitboxParameters.Reduced);

        var frame = PlatformAttackFrame.Step(
            PlatformAttackState.Empty,
            PlatformSaintIndex.Seiya,
            PlatformInput.B,
            playerY: 0x39,
            playerX: 0x3E,
            facing42: 0x40,
            frameStartAction4E: 0,
            jumpPhase49: 0,
            frameCounter3C: 0,
            cosmo: 100,
            engineSubstate01: 0x20,
            engineSubstate02: 1,
            seventhSense: 0,
            orderedTargets: [target]);

        Require(frame.Attempt.Outcome == PlatformAttackAttemptOutcome.Created,
            "B creates projectile before entity update");
        Require(frame.PlatformDamage == 19, "damage is computed from current Cosmo before hit phase");
        Require(frame.HitSequences[0].Results[0].Result.Outcome == PlatformProjectileHitOutcome.HpSurvived,
            "newly created projectile hits target at creation origin in same frame");
        Require(frame.Targets[0].Entity.HitPoints == 1,
            "target receives Seiya Cosmo100 damage 19 before projectile movement");
        Require(frame.AttackState.Slot0.Object.X == 0x55 && frame.AttackState.Slot0.RangeCounter == 2,
            "only after hits does generic updater decrement range 3->2 and move X $50->$55");
    }

    private static void CheckBusyBeforeInput()
    {
        var fireAsSevenExpires = PlatformAttackFrame.Step(
            PlatformAttackState.Empty with { Busy4B = 7 },
            PlatformSaintIndex.Seiya,
            PlatformInput.B,
            0x40, 0x30, 0x40, 0, 0, 0,
            cosmo: 100,
            engineSubstate01: 0x20,
            engineSubstate02: 1,
            seventhSense: 0,
            orderedTargets: []);
        Require(fireAsSevenExpires.Attempt.Outcome == PlatformAttackAttemptOutcome.Created,
            "$4B 7 advances to 0 before B creation check");
        Require(fireAsSevenExpires.AttackState.Busy4B == 1,
            "successful fire replaces expired busy with 1");

        var stillBusy = PlatformAttackFrame.Step(
            PlatformAttackState.Empty with { Busy4B = 6 },
            PlatformSaintIndex.Seiya,
            PlatformInput.B,
            0x40, 0x30, 0x40, 0, 0, 0,
            100, 0x20, 1, 0, []);
        Require(stillBusy.Attempt.Outcome == PlatformAttackAttemptOutcome.Busy,
            "$4B 6 advances to 7 and still blocks attack this frame");
        Require(stillBusy.AttackState.Busy4B == 7 && stillBusy.AttackState.BButtonLatch4C == 1,
            "busy rejection consumes B latch after pre-input counter advance");

        var release = PlatformAttackFrame.Step(
            PlatformAttackState.Empty with { Busy4B = 3, BButtonLatch4C = 1 },
            PlatformSaintIndex.Seiya,
            PlatformInput.None,
            0x40, 0x30, 0x40, 0, 0, 0,
            100, 0x20, 1, 0, []);
        Require(release.AttackState.Busy4B == 4 && release.AttackState.BButtonLatch4C == 0,
            "busy advances first but B release still clears independent latch later in frame");
    }

    private static void CheckRangeOneHitsBeforeRetirement()
    {
        var existing = PlatformAttackState.Empty with
        {
            Slot0 = new PlatformAttackSlot(
                new PlatformAttackObject(0x40, 0x64, 0x40, 0x50, 0, 0, 0, 0),
                RangeCounter: 1),
        };
        var target = new PlatformHitTarget(
            new PlatformCombatEntity(0x10, 0x50, 0x40, 0x0E, 30, 0),
            PlatformHitboxParameters.Common);

        var frame = PlatformAttackFrame.Step(
            existing,
            PlatformSaintIndex.Seiya,
            PlatformInput.None,
            0x40, 0x30, 0x40, 0, 0, 0,
            100, 0x20, 1, 0, [target]);

        Require(frame.HitSequences[0].Results[0].Result.Outcome == PlatformProjectileHitOutcome.AbsorbedNoHp,
            "range1 projectile still participates in collision before lifetime update");
        Require(frame.AttackState.Slot0.Object.Type == 0xFE && frame.AttackState.Slot0.RangeCounter == 0,
            "A22C later decrements 1->0 and retires it without movement");
    }

    private static void CheckShunSecondSegmentAppearsAfterHits()
    {
        var target = new PlatformHitTarget(
            new PlatformCombatEntity(0x10, 0x50, 0x40, 0x0E, 30, 0),
            PlatformHitboxParameters.Common);

        var first = PlatformAttackFrame.Step(
            PlatformAttackState.Empty,
            PlatformSaintIndex.Shun,
            PlatformInput.B,
            playerY: 0x39,
            playerX: 0x3E,
            facing42: 0x40,
            frameStartAction4E: 0,
            jumpPhase49: 0,
            frameCounter3C: 0,
            cosmo: 0,
            engineSubstate01: 0x20,
            engineSubstate02: 1,
            seventhSense: 0,
            orderedTargets: [target]);

        Require(first.Attempt.Outcome == PlatformAttackAttemptOutcome.Created,
            "Shun creates slot0 $54");
        Require(first.HitSequences[0].Results[0].Result.Outcome == PlatformProjectileHitOutcome.AbsorbedNoHp,
            "Shun slot0 can hit on creation frame");
        Require(first.HitSequences[0].Results[1].Result.Outcome == PlatformProjectileHitOutcome.NoOverlap,
            "slot1 is still inactive during creation-frame entity hit phase");
        Require(first.AttackState.Slot0.RangeCounter == 0 && first.AttackState.ShunExtension0391 == 10,
            "post-hit A311 decrements Shun range1 and extends to 10");
        Require(first.AttackState.Slot1.Object.Type == 0x55,
            "only post-hit A311 synthesizes second chain segment $55");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException($"AttackFrame self-test failed: {message}");
    }
}

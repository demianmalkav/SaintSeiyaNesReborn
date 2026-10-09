using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class SpecialEntityActive08090CChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckImmediatePredispatchSpawnExistsBeforeAa70();
        CheckLate70SpawnOccursAfterAa70();
        CheckControl04Cadence();
        CheckSurvivingHitAdvances40SameFrame();
        CheckLethalHitCanAdvanceD0SameFrame();
        CheckType0CBob();
        CheckFrameStart50SkipsInteractionAndBob();
        CheckAttachedHazardCanContactWhenParentMisses();
    }

    private static void CheckImmediatePredispatchSpawnExistsBeforeAa70()
    {
        var result = RunFrame(
            State(0x09, 0x00, control04: 0, phase03: 1, global039A: 0x7F, x: 0x50, y: 0x50),
            frame: 1, playerX: 0x10, playerY: 0x20);

        Require(result.PreDispatch.Outcome == PlatformSpecialEntityPreDispatchOutcome.SecondarySpawnTriggered,
            "threshold + nonzero +03 takes immediate A908 path");
        Require(result.ImmediateSpawn?.Spawned == true && result.ImmediateSpawn.Value.State.Raw2D == 0xA1,
            "type09 A908 object exists before active interaction");
        Require(result.AttachedContact is not null,
            "same update reaches AA70 after immediate A908 spawn");
        Require(result.State.Control.Control04 == 2 && result.Control04Advanced,
            "pre-dispatch +04=1 is advanced to 2 later in the same update");
    }

    private static void CheckLate70SpawnOccursAfterAa70()
    {
        var result = RunFrame(
            State(0x09, 0x77, control04: 1, phase03: 0, global039A: 0, x: 0x70, y: 0x50),
            frame: 1, playerX: 0x10, playerY: 0x20);

        Require(result.AttachedContact is PlatformEntityAttachedHazardContactResult contact
            && contact.State.Raw2D == 0xFE,
            "AA70 sees the pre-late-phase attached state");
        Require(result.LateAttack70.Advanced
            && result.LateAttack70.State.ActionState == 0x78
            && result.LateAttack70.CallsSecondarySpawnRoutine,
            "odd $3C advances type09 $77->$78 and requests A908");
        Require(result.LateSpawn?.Spawned == true && result.State.AttachedHazard.Raw2D == 0xA1,
            "late A908 object appears only after current AA70 evaluation");
    }

    private static void CheckControl04Cadence()
    {
        var zero = RunFrame(
            State(0x08, 0x00, 0, 0, 0, 0x70, 0x50),
            frame: 1, playerX: 0x10, playerY: 0x20);
        Require(zero.State.Control.Control04 == 0 && !zero.Control04Advanced,
            "$A738 leaves +04=0 untouched");

        var wrap = RunFrame(
            State(0x08, 0x00, 0x0B, 0, 0, 0x70, 0x50),
            frame: 1, playerX: 0x10, playerY: 0x20);
        Require(wrap.State.Control.Control04 == 0 && wrap.Control04Advanced,
            "$0B increments and wraps to zero at $0C");
    }

    private static void CheckSurvivingHitAdvances40SameFrame()
    {
        var result = RunFrame(
            State(0x08, 0x00, 1, 0, 0, 0x60, 0x50, hp: 10),
            Projectile(0x60, 0x60),
            damage: 4,
            frame: 1,
            playerX: 0x10,
            playerY: 0x20);

        Require(result.MainInteraction is not null
            && result.MainInteraction.HitSequence.Results.Any(x => x.Result.Outcome == PlatformProjectileHitOutcome.HpSurvived),
            "tall special hitbox resolves a surviving projectile hit");
        Require(result.Reaction40Advanced && result.State.Control.Entity.Motion.ActionState == 0x41,
            "hit-created $40 advances to $41 in the same update");
        Require(result.State.Control.Entity.HitPoints == 6,
            "special HP subtraction persists through post path");
    }

    private static void CheckLethalHitCanAdvanceD0SameFrame()
    {
        var result = RunFrame(
            State(0x08, 0x00, 1, 0, 0, 0x60, 0x50, hp: 3, ground: 0xA8),
            Projectile(0x60, 0x60),
            damage: 5,
            frame: 0,
            playerX: 0x10,
            playerY: 0x20);

        Require(result.MainInteraction is not null
            && result.MainInteraction.HitSequence.Results.Any(x => x.Result.Outcome == PlatformProjectileHitOutcome.HpKilled),
            "overlapping projectile takes special HP kill path");
        Require(result.DeathD0Advanced && result.State.Control.Entity.Motion.ActionState == 0xD1,
            "cadence-aligned lethal hit writes $D0 then advances to $D1 same update");
        Require(result.State.Control.Entity.HitPoints == 3,
            "original kill path does not zero stored HP");
    }

    private static void CheckType0CBob()
    {
        var result = RunFrame(
            State(0x0C, 0x00, 1, 0, 0, 0x70, 0x50),
            frame: 0, playerX: 0x10, playerY: 0x20);

        Require(result.Type0CBobApplied && result.Type0CBobDelta == 1,
            "type0C uses first +1 bob sample on aligned $3C");
        Require(result.State.Control.Entity.Motion.Y == 0x51,
            "type0C bob mutates Y after interaction");
    }

    private static void CheckFrameStart50SkipsInteractionAndBob()
    {
        var result = RunFrame(
            State(0x0C, 0x50, 0, 0, 0, 0x70, 0x60, ground: 0xA8),
            frame: 0, playerX: 0x10, playerY: 0x20);

        Require(result.Outcome == PlatformSpecialEntityActive08090COutcome.SkippedInteraction,
            "frame-start $50 takes dedicated A57E path");
        Require(result.MainInteraction is null && result.AttachedContact is null && !result.Type0CBobApplied,
            "$50 jumps to A886, bypassing interaction and A7D0 bob");
        Require(result.State.Control.Entity.Motion.ActionState == 0x00
            && result.State.Control.Entity.Motion.Y == 0x60,
            "C491 landing snaps type0C and returns action $00");
    }

    private static void CheckAttachedHazardCanContactWhenParentMisses()
    {
        var state = State(
            0x09, 0x00, 1, 0, 0, 0x80, 0x60,
            lifeDrain: 5, cosmoDrain: 2) with
        {
            AttachedHazard = PlatformEntityAttachedHazardState.Empty with
            {
                Raw2C = 0x20,
                Raw2D = 0xA1,
                Raw2F = 0x20,
            },
        };

        var result = RunFrame(state, frame: 1, playerX: 0x20, playerY: 0x20);

        Require(result.MainInteraction is not null && !result.MainInteraction.ContactPhase.Contact.Triggered,
            "parent tall geometry can miss while attached subobject overlaps");
        Require(result.AttachedContact?.Triggered == true,
            "AA70 triggers after parent-contact miss");
        Require(result.ContactState.HazardLatch76 == 0x20
            && result.ContactState.DrainState.LifeTicks == 5
            && result.ContactState.DrainState.CosmoTicks == 2,
            "attached contact seeds corrected parent Life/Cosmo drain values");
        Require(result.State.AttachedHazard.Raw2D == 0xFE,
            "successful AA70 contact deactivates attached object");
    }

    private static PlatformSpecialEntityActive08090CResult RunFrame(
        PlatformSpecialEntityActive08090CState state,
        byte frame,
        byte playerX,
        byte playerY) =>
        RunFrame(state, PlatformAttackState.Empty, 4, frame, playerX, playerY);

    private static PlatformSpecialEntityActive08090CResult RunFrame(
        PlatformSpecialEntityActive08090CState state,
        PlatformAttackState attacks,
        int damage,
        byte frame,
        byte playerX,
        byte playerY) =>
        PlatformSpecialEntityActive08090C.Step(
            state,
            attacks,
            PlatformSaintIndex.Seiya,
            damage,
            seventhSense: 0,
            engineSubstate02: 0,
            contactState: new PlatformContactPhaseState(0, default),
            frameStartAction4E: 0,
            playerX,
            playerY,
            entropy48: 0,
            frameCounter3C: frame,
            cameraDelta43: 0,
            engineState00: 0x10,
            alternateParent08_03AB: 0x66);

    private static PlatformSpecialEntityActive08090CState State(
        byte type,
        byte action,
        byte control04,
        byte phase03,
        byte global039A,
        byte x,
        byte y,
        byte hp = 20,
        byte ground = 0xA8,
        byte lifeDrain = 5,
        byte cosmoDrain = 2) =>
        new(
            new PlatformSpecialEntityControlState(
                new PlatformCommonEntityRuntimeState(
                    new PlatformCommonEntityMotionState(
                        action, x, y, phase03, ground, 0, 0, type, 0, 0),
                    hp,
                    lifeDrain,
                    cosmoDrain,
                    0x04),
                control04),
            PlatformEntityAttachedHazardState.Empty,
            ParentOffset08: 0,
            GlobalCounter039A: global039A);

    private static PlatformAttackState Projectile(byte x, byte y) =>
        PlatformAttackState.Empty with
        {
            Slot0 = new PlatformAttackSlot(
                new PlatformAttackObject(y, 0x64, 0x40, x, 0, 0, 0, 0),
                RangeCounter: 3),
        };

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Special 08/09/0C active self-test failed: {label}");
    }
}

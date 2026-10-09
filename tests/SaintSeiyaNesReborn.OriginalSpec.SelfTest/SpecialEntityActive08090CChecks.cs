using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class SpecialEntityActive08090CChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckImmediatePredispatchSpawnExistsBeforeAa70();
        CheckLate70SpawnOccursAfterAa70();
        CheckControl04ZeroStaysZeroAndNonzeroWraps();
        CheckSurvivingProjectileHitAdvances40SameFrame();
        CheckLethalProjectileHitCanAdvanceD0SameFrame();
        CheckType0CBobRunsOnPostInteractionPath();
        CheckFrameStart50SkipsInteractionAndBob();
        CheckExistingAttachedHazardCanSeedContactWhenParentMisses();
    }

    private static void CheckImmediatePredispatchSpawnExistsBeforeAa70()
    {
        var state = State(
            type: 0x09,
            action: 0x00,
            control04: 0,
            phase03: 1,
            global039A: 0x7F,
            x: 0x50,
            y: 0x50);

        var result = Step(state, frameCounter: 1, playerX: 0x10, playerY: 0x20);

        Require(result.PreDispatch.Outcome == PlatformSpecialEntityPreDispatchOutcome.SecondarySpawnTriggered,
            "threshold + nonzero +03 takes immediate A908 path");
        Require(result.ImmediateSpawn?.Spawned == true
            && result.ImmediateSpawn.Value.State.Raw2D == 0xA1,
            "type09 A908 object exists before active interaction");
        Require(result.AttachedContact is not null,
            "same update reaches AA70 after immediate A908 spawn");
        Require(result.State.Control.Control04 == 2 && result.Control04Advanced,
            "pre-dispatch sets +04=1 and later A738 advances it to 2 in same update");
    }

    private static void CheckLate70SpawnOccursAfterAa70()
    {
        var state = State(
            type: 0x09,
            action: 0x77,
            control04: 1,
            phase03: 0,
            global039A: 0,
            x: 0x70,
            y: 0x50);

        var result = Step(state, frameCounter: 1, playerX: 0x10, playerY: 0x20);

        Require(result.AttachedContact is PlatformEntityAttachedHazardContactResult contact
            && contact.State.Raw2D == 0xFE,
            "AA70 sees the pre-late-phase attached state before midpoint spawn");
        Require(result.LateAttack70.Advanced
            && result.LateAttack70.State.ActionState == 0x78
            && result.LateAttack70.CallsSecondarySpawnRoutine,
            "odd $3C advances type09 $77->$78 and requests A908");
        Require(result.LateSpawn?.Spawned == true
            && result.State.AttachedHazard.Raw2D == 0xA1,
            "late A908 spawn appears only after current AA70 evaluation");
    }

    private static void CheckControl04ZeroStaysZeroAndNonzeroWraps()
    {
        var zero = Step(
            State(0x08, 0x00, control04: 0, phase03: 0, global039A: 0, x: 0x70, y: 0x50),
            frameCounter: 1,
            playerX: 0x10,
            playerY: 0x20);
        Require(zero.State.Control.Control04 == 0 && !zero.Control04Advanced,
            "$A738 leaves +04=0 untouched");

        var wrap = Step(
            State(0x08, 0x00, control04: 0x0B, phase03: 0, global039A: 0, x: 0x70, y: 0x50),
            frameCounter: 1,
            playerX: 0x10,
            playerY: 0x20);
        Require(wrap.State.Control.Control04 == 0 && wrap.Control04Advanced,
            "nonzero +04 increments and $0B wraps to zero at $0C");
    }

    private static void CheckSurvivingProjectileHitAdvances40SameFrame()
    {
        var state = State(
            type: 0x08,
            action: 0x00,
            control04: 1,
            phase03: 0,
            global039A: 0,
            x: 0x60,
            y: 0x50,
            hp: 10);
        var attacks = Projectile(x: 0x60, y: 0x60);

        var result = Step(
            state,
            attacks,
            platformDamage: 4,
            frameCounter: 1,
            playerX: 0x10,
            playerY: 0x20);

        Require(result.MainInteraction?.HitSequence.Hits.Count > 0,
            "overlapping projectile reaches special entity through tall hitbox");
        Require(result.Reaction40Advanced
            && result.State.Control.Entity.Motion.ActionState == 0x41,
            "surviving hit writes $40 and A79E advances it to $41 in same update");
        Require(result.State.Control.Entity.HitPoints == 6,
            "special HP subtraction persists through post path");
    }

    private static void CheckLethalProjectileHitCanAdvanceD0SameFrame()
    {
        var state = State(
            type: 0x08,
            action: 0x00,
            control04: 1,
            phase03: 0,
            global039A: 0,
            x: 0x60,
            y: 0x50,
            hp: 3,
            ground: 0xA8);

        var result = Step(
            state,
            Projectile(0x60, 0x60),
            platformDamage: 5,
            frameCounter: 0,
            playerX: 0x10,
            playerY: 0x20);

        Require(result.DeathD0Advanced
            && result.State.Control.Entity.Motion.ActionState == 0xD1,
            "cadence-aligned lethal hit writes $D0 then advances to $D1 same update");
        Require(result.State.Control.Entity.HitPoints == 3,
            "original kill path does not zero stored HP");
    }

    private static void CheckType0CBobRunsOnPostInteractionPath()
    {
        var state = State(
            type: 0x0C,
            action: 0x00,
            control04: 1,
            phase03: 0,
            global039A: 0,
            x: 0x70,
            y: 0x50);

        var result = Step(state, frameCounter: 0, playerX: 0x10, playerY: 0x20);

        Require(result.Type0CBobApplied && result.Type0CBobDelta == 1,
            "type0C uses first +1 bob sample when ($3C&7)==0 and phase index 0");
        Require(result.State.Control.Entity.Motion.Y == 0x51,
            "type0C bob mutates parent Y after interaction");
    }

    private static void CheckFrameStart50SkipsInteractionAndBob()
    {
        var state = State(
            type: 0x0C,
            action: 0x50,
            control04: 0,
            phase03: 0,
            global039A: 0,
            x: 0x70,
            y: 0x60,
            ground: 0xA8);

        var result = Step(state, frameCounter: 0, playerX: 0x10, playerY: 0x20);

        Require(result.Outcome == PlatformSpecialEntityActive08090COutcome.SkippedInteraction,
            "frame-start $50 takes dedicated A57E path");
        Require(result.MainInteraction is null && result.AttachedContact is null,
            "$50 jumps directly to A886 and skips both interaction calls");
        Require(!result.Type0CBobApplied,
            "$50 path bypasses type0C A7D0 bob on current update");
        Require(result.State.Control.Entity.Motion.ActionState == 0x00
            && result.State.Control.Entity.Motion.Y == 0x60,
            "C491 landing snaps type0C and returns it to action $00");
    }

    private static void CheckExistingAttachedHazardCanSeedContactWhenParentMisses()
    {
        var attached = PlatformEntityAttachedHazardState.Empty with
        {
            Raw2C = 0x20,
            Raw2D = 0xA1,
            Raw2F = 0x20,
        };
        var state = State(
            type: 0x09,
            action: 0x00,
            control04: 1,
            phase03: 0,
            global039A: 0,
            x: 0x80,
            y: 0x60,
            lifeDrain: 5,
            cosmoDrain: 2) with
        {
            AttachedHazard = attached,
        };

        var result = Step(state, frameCounter: 1, playerX: 0x20, playerY: 0x20);

        Require(result.MainInteraction is not null
            && !result.MainInteraction.ContactPhase.Contact.Triggered,
            "parent geometry can miss while attached subobject overlaps");
        Require(result.AttachedContact?.Triggered == true,
            "AA70 then triggers attached-hazard contact");
        Require(result.ContactState.HazardLatch76 == 0x20
            && result.ContactState.DrainState.LifeTicks == 5
            && result.ContactState.DrainState.CosmoTicks == 2,
            "attached contact seeds corrected parent Life/Cosmo drains");
        Require(result.State.AttachedHazard.Raw2D == 0xFE,
            "successful AA70 contact deactivates attached object");
    }

    private static PlatformSpecialEntityActive08090CResult Step(
        PlatformSpecialEntityActive08090CState state,
        byte frameCounter,
        byte playerX,
        byte playerY) =>
        Step(state, PlatformAttackState.Empty, 4, frameCounter, playerX, playerY);

    private static PlatformSpecialEntityActive08090CResult Step(
        PlatformSpecialEntityActive08090CState state,
        PlatformAttackState attacks,
        int platformDamage,
        byte frameCounter,
        byte playerX,
        byte playerY) =>
        PlatformSpecialEntityActive08090C.Step(
            state,
            attacks,
            PlatformSaintIndex.Seiya,
            platformDamage,
            seventhSense: 0,
            engineSubstate02: 0,
            contactState: new PlatformContactPhaseState(0, default),
            frameStartAction4E: 0,
            playerX,
            playerY,
            entropy48: 0,
            frameCounter3C: frameCounter,
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
                        ActionState: action,
                        X: x,
                        Y: y,
                        StatePhase: phase03,
                        GroundDescriptor: ground,
                        DecisionTimer: 0,
                        FlagsFacing: 0,
                        Type: type,
                        TerrainProbeRight: 0,
                        TerrainProbeLeft: 0),
                    HitPoints: hp,
                    LifeDrainTicks: lifeDrain,
                    CosmoDrainTicks: cosmoDrain,
                    SeventhSenseRewardBcd: 0x04),
                control04),
            PlatformEntityAttachedHazardState.Empty,
            ParentOffset08: 0,
            GlobalCounter039A: global039A);

    private static PlatformAttackState Projectile(byte x, byte y)
    {
        var slot = new PlatformAttackSlot(
            new PlatformAttackObject(
                Y: y,
                Type: 0x64,
                Facing: 0x40,
                X: x,
                Field4: 0,
                Field5: 0,
                Field6: 0,
                AuxiliaryX: 0),
            RangeCounter: 3);
        return PlatformAttackState.Empty with { Slot0 = slot };
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Special 08/09/0C active self-test failed: {label}");
    }
}

using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class Multisprite9B93NormalUpdateChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckContactUsesPreMovePosition();
        CheckProjectileUsesPostMovePosition();
        CheckFlag04SkipsDefaultProjectileCheck();
        CheckSubstate0CForcesProjectileCheck();
        CheckFlag08TransitionHalvesDrains();
        CheckAnimationTableGroup();
        CheckVerticalBandClearsWholeClass();
    }

    private static void CheckContactUsesPreMovePosition()
    {
        var state = State(mode81: 0, x: 0x40, y: 0x40, flags: 0x06);
        var step = PlatformMultisprite9B93NormalUpdate.Step(
            state,
            PlatformAttackState.Empty,
            new PlatformContactPhaseState(0, default),
            PlatformSaintIndex.Seiya,
            platformDamage: 10,
            seventhSense: 0,
            frameCounter3C: 1,
            cameraDelta43: 0,
            playerX3F: 0x4C,
            playerY40: 0x40,
            frameStartPlayerAction4E: 0,
            engineSubstate02: 1);

        Require(step.Contact.Outcome == PlatformEntityContactOutcome.ContactTriggered,
            "contact tests original X=$40 before mode81=0 shifts object left");
        Require(step.State.Visual.Part0.X == 0x3E,
            "normal motion then shifts part0 left by two");
        Require(!step.ProjectileCheckRequested,
            "flag04 suppresses default projectile path in this fixture");
    }

    private static void CheckProjectileUsesPostMovePosition()
    {
        var state = State(mode81: 0, x: 0x40, y: 0x40, flags: 0x02);
        var projectile = new PlatformAttackSlot(
            new PlatformAttackObject(0x40, 0x64, 0x40, 0x3A, 0, 0, 0, 0),
            3);
        var attacks = PlatformAttackState.Empty with { Slot0 = projectile };

        var step = PlatformMultisprite9B93NormalUpdate.Step(
            state,
            attacks,
            new PlatformContactPhaseState(1, default),
            PlatformSaintIndex.Seiya,
            platformDamage: 10,
            seventhSense: 0,
            frameCounter3C: 1,
            cameraDelta43: 0,
            playerX3F: 0x70,
            playerY40: 0x70,
            frameStartPlayerAction4E: 0,
            engineSubstate02: 1);

        var hit = step.ProjectileHits!.Results[0].Result;
        Require(step.State.Visual.Part0.X == 0x3E,
            "visual object moves before projectile test");
        Require(hit.Outcome == PlatformProjectileHitOutcome.HpSurvived,
            "projectile at X=$3A hits only after logical X syncs to moved $3E");
        Require(step.State.Logical.Action00 == 0x40,
            "ordinary type5 hit mutation is threaded back into logical state");
    }

    private static void CheckFlag04SkipsDefaultProjectileCheck()
    {
        var state = State(mode81: 0, x: 0x40, y: 0x40, flags: 0x06);
        var attacks = PlatformAttackState.Empty with
        {
            Slot0 = new PlatformAttackSlot(new PlatformAttackObject(0x40, 0x64, 0x40, 0x40, 0, 0, 0, 0), 3),
        };

        var step = PlatformMultisprite9B93NormalUpdate.Step(
            state,
            attacks,
            new PlatformContactPhaseState(1, default),
            PlatformSaintIndex.Hyoga,
            20,
            0,
            1,
            0,
            0x70,
            0x70,
            0,
            1);

        Require(!step.ProjectileCheckRequested && step.ProjectileHits is null,
            "default branch skips 9915 when visual flag04 is set");
        Require(step.AttackState.Slot0.Object.Type == 0x64,
            "skipped projectile path leaves attack active");
    }

    private static void CheckSubstate0CForcesProjectileCheck()
    {
        var state = State(mode81: 0, x: 0x40, y: 0x40, flags: 0x06) with
        {
            Logical = State(0, 0x40, 0x40, 0x06).Logical with { Type09 = 5 },
        };
        var attacks = PlatformAttackState.Empty with
        {
            Slot0 = new PlatformAttackSlot(new PlatformAttackObject(0x40, 0x64, 0x40, 0x3E, 0, 0, 0, 0), 3),
        };

        var step = PlatformMultisprite9B93NormalUpdate.Step(
            state,
            attacks,
            new PlatformContactPhaseState(1, default),
            PlatformSaintIndex.Hyoga,
            20,
            0,
            1,
            0,
            0x70,
            0x70,
            0,
            0x0C);

        Require(step.ProjectileCheckRequested && step.ProjectileHits is not null,
            "$0C forces projectile path regardless of flag04");
        Require(step.AttackState.Slot0.Object.Type == 0xFE,
            "Hyoga projectile is consumed by the forced hit path");
    }

    private static void CheckFlag08TransitionHalvesDrains()
    {
        var state = State(mode81: 3, x: 0x40, y: 0x40, flags: 0x02) with
        {
            Logical = State(3, 0x40, 0x40, 0x02).Logical with
            {
                Field05 = 0x80,
                CosmoDrain0D = 5,
                LifeDrain0E = 3,
            },
        };

        var step = PlatformMultisprite9B93NormalUpdate.Step(
            state,
            PlatformAttackState.Empty,
            new PlatformContactPhaseState(1, default),
            PlatformSaintIndex.Seiya,
            10,
            0,
            1,
            0,
            0x70,
            0x40,
            0,
            1);

        Require(step.Outcome == PlatformMultisprite9B93NormalOutcome.TransitionedToFlag08,
            "mode81 player-relative object arms flag08 when threshold gates pass");
        Require((step.State.Visual.Part0.Flags & 0x08) != 0,
            "transition persists flag08 through animation low-bit preservation");
        Require(step.State.Logical.Phase03 == 0,
            "transition resets logical phase");
        Require(step.State.Logical.CosmoDrain0D == 2 && step.State.Logical.LifeDrain0E == 1,
            "transition halves both drain counters using LSR semantics");
        Require(step.SoundId == 0x2C,
            "flag04-clear transition requests sound 2C");
    }

    private static void CheckAnimationTableGroup()
    {
        var state = State(mode81: 0, x: 0x50, y: 0x40, flags: 0x02);
        var step = PlatformMultisprite9B93NormalUpdate.Step(
            state,
            PlatformAttackState.Empty,
            new PlatformContactPhaseState(1, default),
            PlatformSaintIndex.Seiya,
            10,
            0,
            frameCounter3C: 0x08,
            cameraDelta43: 0,
            playerX3F: 0x70,
            playerY40: 0x70,
            frameStartPlayerAction4E: 0,
            engineSubstate02: 1);

        Require(step.State.Visual.Part0.Sprite == 0xB6
            && step.State.Visual.Part1.Sprite == 0xB7
            && step.State.Visual.Part2.Sprite == 0xB4
            && step.State.Visual.Part3.Sprite == 0xB5,
            "$3C&18=$08 selects second normal animation group");
        Require((step.State.Visual.Part0.Flags & 0xC0) == 0x80,
            "animation table replaces high flag bits while preserving low bits");
    }

    private static void CheckVerticalBandClearsWholeClass()
    {
        var state = State(mode81: 3, x: 0x40, y: 0xA9, flags: 0x02) with
        {
            Logical = State(3, 0x40, 0xA9, 0x02).Logical with { Phase03 = 0x3F },
        };
        var step = PlatformMultisprite9B93NormalUpdate.Step(
            state,
            PlatformAttackState.Empty,
            new PlatformContactPhaseState(1, default),
            PlatformSaintIndex.Seiya,
            10,
            0,
            1,
            0,
            0x70,
            0x70,
            0,
            1);

        Require(step.Outcome == PlatformMultisprite9B93NormalOutcome.RemovedVerticalBand,
            "part entering B0-BF triggers whole-class cleanup");
        Require(step.State.Visual.AllEmpty && step.State.Logical.Phase03 == 0,
            "cleanup writes F0/FE to all parts and clears phase");
        Require(step.ProjectileHits is null,
            "vertical-band cleanup returns before later projectile path");
    }

    private static PlatformMultisprite9B93RuntimeState State(byte mode81, byte x, byte y, byte flags)
    {
        var visual = new PlatformMultisprite9B93VisualState(
            new PlatformMultisprite9B93Part(y, 0xB4, flags, x),
            new PlatformMultisprite9B93Part(y, 0xB5, flags, unchecked((byte)(x + 8))),
            new PlatformMultisprite9B93Part(unchecked((byte)(y + 8)), 0xB6, flags, x),
            new PlatformMultisprite9B93Part(unchecked((byte)(y + 8)), 0xB7, flags, unchecked((byte)(x + 8))));
        var logical = new PlatformMultisprite9B93LogicalState(
            Action00: 0,
            X01: x,
            Y02: y,
            Phase03: 0,
            Field05: 0,
            Type09: 5,
            Profile0C: 100,
            CosmoDrain0D: 4,
            LifeDrain0E: 2,
            SeventhSenseReward0F: 0x10);
        return new PlatformMultisprite9B93RuntimeState(visual, logical, mode81);
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"9B93 normal-update self-test failed: {label}");
    }
}

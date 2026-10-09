using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class Multisprite9B93Flag08Bit04Checks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckDefaultBranchContactsWithoutProjectile();
        Check0CProjectileRunsBeforeContactResync();
        CheckCurveNormalizationAndSound();
        CheckHorizontalPartRetirement();
        CheckVerticalBoundaryClearsAll();
    }

    private static void CheckDefaultBranchContactsWithoutProjectile()
    {
        var state = State(phase: 0, x: 0x40, y: 0x40);
        var attacks = AttackAt(0x40, 0x40);
        var step = PlatformMultisprite9B93Flag08Bit04.Step(
            state, attacks, new PlatformContactPhaseState(0, default),
            PlatformSaintIndex.Hyoga, 20, 0, 1, 0,
            playerX3F: 0x40, playerY40: 0x40,
            frameStartPlayerAction4E: 0, engineSubstate02: 1);

        Require(step.Contact.Outcome == PlatformEntityContactOutcome.ContactTriggered,
            "flag08+04 default branch reaches contact");
        Require(step.ProjectileHits is null,
            "default flag08+04 branch does not test projectile collision");
        Require(step.AttackState.Slot0.Object.Type == 0x64,
            "overlapping projectile remains active when 9915 is not called");
    }

    private static void Check0CProjectileRunsBeforeContactResync()
    {
        var baseState = State(phase: 0, x: 0x40, y: 0x40) with
        {
            Logical = State(0, 0x40, 0x40).Logical with
            {
                Type09 = 1,
                Profile0C = 0x1E,
            },
        };
        var step = PlatformMultisprite9B93Flag08Bit04.Step(
            baseState,
            AttackAt(0x40, 0x40),
            new PlatformContactPhaseState(0, default),
            PlatformSaintIndex.Hyoga,
            20,
            0,
            1,
            0,
            playerX3F: 0x40,
            playerY40: 0x40,
            frameStartPlayerAction4E: 0,
            engineSubstate02: 0x0C);

        Require(step.ProjectileHits is not null
            && step.ProjectileHits.Results[0].Result.Outcome == PlatformProjectileHitOutcome.DropReaction,
            "$0C performs projectile routing before contact");
        Require(step.State.Logical.Action00 == 0xE0,
            "projectile action mutation survives later position re-sync");
        Require(step.State.Logical.Y02 == 0x40,
            "later 9CD5 position sync overwrites type<5 hit Y+6 before contact");
        Require(step.Contact.Outcome == PlatformEntityContactOutcome.ContactTriggered,
            "contact still uses re-synced visual position after the hit");
        Require(step.AttackState.Slot0.Object.Type == 0xFE,
            "Hyoga projectile is consumed in the pre-contact $0C hit");
    }

    private static void CheckCurveNormalizationAndSound()
    {
        var state = State(phase: 0x3F, x: 0x50, y: 0x40) with
        {
            Logical = State(0x3F, 0x50, 0x40).Logical with { Field05 = 0x80 },
        };
        var step = PlatformMultisprite9B93Flag08Bit04.Step(
            state, PlatformAttackState.Empty, new PlatformContactPhaseState(1, default),
            PlatformSaintIndex.Seiya, 10, 0, frameCounter3C: 1, cameraDelta43: 0,
            playerX3F: 0x70, playerY40: 0x70, frameStartPlayerAction4E: 0, engineSubstate02: 1);

        Require(step.CurveIndex == 0x2F && step.CurveByte == 0xFB,
            "phase 3F normalizes to curve index 2F");
        Require(step.State.Logical.Phase03 == 0x40,
            "stored logical phase increments original 3F to 40 rather than normalized index");
        Require(step.State.Visual.Part0.Y == 0x45,
            "subtracting raw FB curve byte moves screen Y down by five");
        Require(step.SoundId == 0x2B,
            "curve index 2F plus field05>=80 requests sound 2B");
    }

    private static void CheckHorizontalPartRetirement()
    {
        var visual = State(phase: 0, x: 0x05, y: 0x40).Visual with
        {
            Part1 = PlatformMultisprite9B93Part.Empty,
            Part2 = PlatformMultisprite9B93Part.Empty,
            Part3 = PlatformMultisprite9B93Part.Empty,
        };
        var state = State(0, 0x05, 0x40) with { Visual = visual };
        var step = PlatformMultisprite9B93Flag08Bit04.Step(
            state, PlatformAttackState.Empty, new PlatformContactPhaseState(1, default),
            PlatformSaintIndex.Seiya, 10, 0, 1, 0,
            0x70, 0x70, 0, engineSubstate02: 0x10);

        Require(step.Outcome == PlatformMultisprite9B93Flag08Bit04Outcome.AllPartsRetired,
            "$10 part0 X=5 becomes 3 and retires the only active part");
        Require(step.State.Visual.AllEmpty && step.State.Logical.Phase03 == 0,
            "all-parts check clears logical phase after individual retirement");
    }

    private static void CheckVerticalBoundaryClearsAll()
    {
        var state = State(phase: 0, x: 0x40, y: 0xB0);
        var step = PlatformMultisprite9B93Flag08Bit04.Step(
            state, PlatformAttackState.Empty, new PlatformContactPhaseState(1, default),
            PlatformSaintIndex.Seiya, 10, 0, 1, 0,
            0x70, 0x70, 0, 1);

        Require(step.Outcome == PlatformMultisprite9B93Flag08Bit04Outcome.RemovedVerticalBoundary,
            "post-curve Y >= B0 clears the whole class");
        Require(step.State.Visual.AllEmpty && step.State.Logical.Phase03 == 0,
            "vertical cleanup clears all parts and phase");
    }

    private static PlatformMultisprite9B93RuntimeState State(byte phase, byte x, byte y)
    {
        const byte flags = 0x0E; // base02 + bit04 + bit08
        var visual = new PlatformMultisprite9B93VisualState(
            new PlatformMultisprite9B93Part(y, 0xB4, flags, x),
            new PlatformMultisprite9B93Part(y, 0xB5, flags, unchecked((byte)(x + 8))),
            new PlatformMultisprite9B93Part(unchecked((byte)(y + 8)), 0xB6, flags, x),
            new PlatformMultisprite9B93Part(unchecked((byte)(y + 8)), 0xB7, flags, unchecked((byte)(x + 8))));
        var logical = new PlatformMultisprite9B93LogicalState(
            0, x, y, phase, 0, 5, 100, 4, 2, 0x10);
        return new PlatformMultisprite9B93RuntimeState(visual, logical, Mode81: 3);
    }

    private static PlatformAttackState AttackAt(byte x, byte y) =>
        PlatformAttackState.Empty with
        {
            Slot0 = new PlatformAttackSlot(
                new PlatformAttackObject(y, 0x64, 0x40, x, 0, 0, 0, 0),
                3),
        };

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"9B93 flag08+04 self-test failed: {label}");
    }
}

using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class Multisprite9B93Substate0DChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckTurnRightAndThreePartFormation();
        CheckVerticalGateCanFreezeY();
        CheckFrameBit3AnimationGroup();
        CheckContactPrecedesProjectileHit();
        CheckHorizontalBoundaryClearsAll();
        CheckD0AnimatesFirstThreeOnly();
        CheckDfToE0CompletesDeath();
    }

    private static void CheckTurnRightAndThreePartFormation()
    {
        var state = State(action: 0, x: 0x40, y: 0x50, flags: 0x02);
        var step = PlatformMultisprite9B93Substate0D.Step(
            state,
            PlatformAttackState.Empty,
            new PlatformContactPhaseState(1, default),
            PlatformSaintIndex.Seiya,
            platformDamage: 10,
            seventhSense: 0,
            frameCounter3C: 1,
            cameraDelta43: 0,
            playerX3F: 0x70,
            playerY40: 0x60,
            frameStartPlayerAction4E: 0);

        Require(step.Outcome == PlatformMultisprite9B93Substate0DOutcome.Active,
            "$0D active route remains live");
        Require(step.State.Visual.Part0.Y == 0x51,
            "Y-16 < playerY permits parity-1 vertical step");
        Require(step.State.Visual.Part0.Flags == 0x42,
            "X+32 below playerX turns part0 to flag42");
        Require(step.State.Visual.Part0.X == 0x42,
            "flag40 direction moves part0 +2");
        Require(step.State.Visual.Part1.X == 0x3A && step.State.Visual.Part2.X == 0x32,
            "right-facing formation places parts1/2 at X-8/X-16");
        Require(step.State.Visual.Part1.Y == 0x51 && step.State.Visual.Part2.Y == 0x51,
            "formation copies resulting part0 Y to parts1/2");
        Require(step.State.Visual.Part1.Flags == 0x42 && step.State.Visual.Part2.Flags == 0x42,
            "formation copies facing flags to parts1/2");
        Require(step.State.Visual.Part3 == state.Visual.Part3,
            "$0D active route leaves fourth visual record untouched");
        Require(step.SpriteBaseUsed == 0x8C
            && step.State.Visual.Part0.Sprite == 0x8C
            && step.State.Visual.Part1.Sprite == 0x8D
            && step.State.Visual.Part2.Sprite == 0x8E,
            "frame bit3 clear selects 8C/8D/8E sprites");
    }

    private static void CheckVerticalGateCanFreezeY()
    {
        var state = State(action: 0, x: 0x80, y: 0x50, flags: 0x02);
        var step = PlatformMultisprite9B93Substate0D.Step(
            state,
            PlatformAttackState.Empty,
            new PlatformContactPhaseState(1, default),
            PlatformSaintIndex.Seiya,
            10,
            0,
            frameCounter3C: 1,
            cameraDelta43: 0,
            playerX3F: 0x80,
            playerY40: 0x30,
            frameStartPlayerAction4E: 0);

        Require(step.State.Visual.Part0.Y == 0x50,
            "byte(Y-16)>=playerY bypasses parity vertical increment");
        Require(step.State.Visual.Part1.Y == 0x50 && step.State.Visual.Part2.Y == 0x50,
            "frozen original Y feeds the three-part formation");
    }

    private static void CheckFrameBit3AnimationGroup()
    {
        var step = PlatformMultisprite9B93Substate0D.Step(
            State(0, 0x60, 0x50, 0x02),
            PlatformAttackState.Empty,
            new PlatformContactPhaseState(1, default),
            PlatformSaintIndex.Seiya,
            10,
            0,
            frameCounter3C: 0x08,
            cameraDelta43: 0,
            playerX3F: 0x60,
            playerY40: 0x30,
            frameStartPlayerAction4E: 0);

        Require(step.SpriteBaseUsed == 0xD1
            && step.State.Visual.Part0.Sprite == 0xD1
            && step.State.Visual.Part1.Sprite == 0xD2
            && step.State.Visual.Part2.Sprite == 0xD3,
            "frame bit3 set selects D1/D2/D3 animation group");
    }

    private static void CheckContactPrecedesProjectileHit()
    {
        var state = State(action: 0, x: 0x40, y: 0x50, flags: 0x02) with
        {
            Logical = State(0, 0x40, 0x50, 0x02).Logical with
            {
                Type09 = 1,
                Profile0C = 0x1E,
                CosmoDrain0D = 4,
                LifeDrain0E = 3,
            },
        };
        var attack = PlatformAttackState.Empty with
        {
            Slot0 = new PlatformAttackSlot(
                new PlatformAttackObject(0x51, 0x64, 0x40, 0x42, 0, 0, 0, 0),
                3),
        };

        var step = PlatformMultisprite9B93Substate0D.Step(
            state,
            attack,
            new PlatformContactPhaseState(0, default),
            PlatformSaintIndex.Hyoga,
            platformDamage: 20,
            seventhSense: 0,
            frameCounter3C: 1,
            cameraDelta43: 0,
            playerX3F: 0x42,
            playerY40: 0x51,
            frameStartPlayerAction4E: 0);

        Require(step.Contact!.Value.Outcome == PlatformEntityContactOutcome.ContactTriggered,
            "$0D wide-box contact triggers before projectile routing");
        Require(step.ContactState.HazardLatch76 == 0x20
            && step.ContactState.DrainState == new ContactDrainState(3, 4),
            "contact seeds D/E drain profile before later hit");
        Require(step.ProjectileHits!.Results[0].Result.Outcome == PlatformProjectileHitOutcome.DropReaction,
            "later type1 projectile hit takes E0/Y+6 route");
        Require(step.State.Logical.Action00 == 0xE0 && step.State.Logical.Y02 == 0x57,
            "post-contact projectile mutation persists; no later visual position resync overwrites Y+6");
        Require(step.AttackState.Slot0.Object.Type == 0xFE,
            "Hyoga projectile is consumed after contact");
    }

    private static void CheckHorizontalBoundaryClearsAll()
    {
        var step = PlatformMultisprite9B93Substate0D.Step(
            State(0, 0x03, 0x50, 0x02),
            PlatformAttackState.Empty,
            new PlatformContactPhaseState(1, default),
            PlatformSaintIndex.Seiya,
            10,
            0,
            frameCounter3C: 0,
            cameraDelta43: 0,
            playerX3F: 0x20,
            playerY40: 0x30,
            frameStartPlayerAction4E: 0);

        Require(step.Outcome == PlatformMultisprite9B93Substate0DOutcome.RemovedHorizontalBoundary,
            "part0 post-move X below4 triggers A01F cleanup");
        Require(step.State.Visual.AllEmpty && step.State.Logical.Phase03 == 0,
            "horizontal removal clears all four records and logical phase");
        Require(step.Contact is null && step.ProjectileHits is null,
            "boundary cleanup returns before interaction phase");
    }

    private static void CheckD0AnimatesFirstThreeOnly()
    {
        var state = State(0xD0, 0x50, 0x50, 0x02);
        var originalPart3 = state.Visual.Part3;
        var step = PlatformMultisprite9B93Substate0D.Step(
            state,
            PlatformAttackState.Empty,
            new PlatformContactPhaseState(7, new ContactDrainState(2, 3)),
            PlatformSaintIndex.Seiya,
            10,
            99,
            1,
            5,
            0x40,
            0x40,
            0);

        Require(step.Outcome == PlatformMultisprite9B93Substate0DOutcome.DeathAnimating,
            "D0 enters dedicated death animation path");
        Require(step.State.Logical.Action00 == 0xD1,
            "death action increments one state per frame");
        Require(step.State.Visual.Part0.Sprite == 0x6A
            && step.State.Visual.Part1.Sprite == 0x6A
            && step.State.Visual.Part2.Sprite == 0x6A,
            "death animation writes sprite6A to first three parts");
        Require(step.State.Visual.Part3 == originalPart3,
            "fourth visual record remains untouched during D0-DF animation");
        Require(step.Contact is null && step.ProjectileHits is null,
            "D0 route performs no contact or projectile collision");
        Require(step.AttackState == PlatformAttackState.Empty && step.SeventhSense == 99,
            "death animation leaves attack/sense state unchanged");
    }

    private static void CheckDfToE0CompletesDeath()
    {
        var state = State(0xDF, 0x50, 0x50, 0x02) with
        {
            Logical = State(0xDF, 0x50, 0x50, 0x02).Logical with { Phase03 = 9 },
        };
        var step = PlatformMultisprite9B93Substate0D.Step(
            state,
            PlatformAttackState.Empty,
            new PlatformContactPhaseState(1, default),
            PlatformSaintIndex.Seiya,
            10,
            0,
            0,
            0,
            0,
            0,
            0);

        Require(step.Outcome == PlatformMultisprite9B93Substate0DOutcome.DeathCompleted,
            "DF increments to E0 and performs terminal cleanup");
        Require(step.State.Logical.Action00 == 0xE0 && step.State.Logical.Phase03 == 0,
            "terminal death keeps E0 action and clears phase");
        Require(step.State.Visual.AllEmpty,
            "terminal death finally clears all four visual records");
    }

    private static PlatformMultisprite9B93RuntimeState State(byte action, byte x, byte y, byte flags)
    {
        var visual = new PlatformMultisprite9B93VisualState(
            new PlatformMultisprite9B93Part(y, 0xF6, flags, x),
            new PlatformMultisprite9B93Part(y, 0xF7, flags, unchecked((byte)(x + 8))),
            new PlatformMultisprite9B93Part(unchecked((byte)(y + 8)), 0xFA, flags, x),
            new PlatformMultisprite9B93Part(unchecked((byte)(y + 8)), 0xFB, flags, unchecked((byte)(x + 8))));
        var logical = new PlatformMultisprite9B93LogicalState(
            Action00: action,
            X01: x,
            Y02: y,
            Phase03: 0,
            Field05: 0,
            Type09: 5,
            Profile0C: 100,
            CosmoDrain0D: 4,
            LifeDrain0E: 2,
            SeventhSenseReward0F: 0x10);
        return new PlatformMultisprite9B93RuntimeState(visual, logical, Mode81: 0);
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"9B93 substate0D self-test failed: {label}");
    }
}

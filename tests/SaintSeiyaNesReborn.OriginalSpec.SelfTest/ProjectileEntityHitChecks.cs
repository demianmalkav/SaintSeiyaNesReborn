using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class ProjectileEntityHitChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        Require(PlatformProjectileEntityHit.IsAcceptedAttackSignature(Attack(0x64).Object), "$64 generic signature accepted");
        Require(PlatformProjectileEntityHit.IsAcceptedAttackSignature(Attack(0x65).Object), "$65 generic animation signature accepted");
        Require(PlatformProjectileEntityHit.IsAcceptedAttackSignature(Attack(0x54).Object), "$54 Shun signature accepted");
        Require(PlatformProjectileEntityHit.IsAcceptedAttackSignature(Attack(0x55).Object), "$55 Shun near-segment signature accepted");
        Require(!PlatformProjectileEntityHit.IsAcceptedAttackSignature(Attack(0xFE).Object), "$FE inactive signature rejected");

        var entity = Entity(type: 0, hp: 30, reward: 0x32);
        var hitbox = PlatformProjectileHitboxParameters.CommonEntity;
        Require(PlatformProjectileEntityHit.Overlaps(Attack(0x64).Object, entity.Motion, hitbox), "common projectile point overlaps common entity window");

        // Common parameters at entity $50/$50 yield low X/Y $4C, with strict
        // lower and inclusive upper bounds ($5C X, $6E Y).
        Require(!PlatformProjectileEntityHit.Overlaps(Attack(0x64, x: 0x4C, y: 0x50).Object, entity.Motion, hitbox), "projectile lower X bound is strict");
        Require(PlatformProjectileEntityHit.Overlaps(Attack(0x64, x: 0x5C, y: 0x50).Object, entity.Motion, hitbox), "projectile upper X bound is inclusive");
        Require(!PlatformProjectileEntityHit.Overlaps(Attack(0x64, x: 0x50, y: 0x4C).Object, entity.Motion, hitbox), "projectile lower Y bound is strict");
        Require(PlatformProjectileEntityHit.Overlaps(Attack(0x64, x: 0x50, y: 0x6E).Object, entity.Motion, hitbox), "projectile upper Y bound is inclusive");

        var seiyaHit = PlatformProjectileEntityHit.ResolveSingle(
            PlatformAttackSlotId.Slot0,
            Attack(0x64, facing: 0x40),
            entity,
            hitbox,
            PlatformSaintIndex.Seiya,
            attackDamage72: 19,
            currentSeventhSense: 100,
            engineSubstate02: 1);
        Require(seiyaHit.Outcome == PlatformProjectileHitOutcome.HitPointsReduced, "ordinary type0 survives 19 damage from 30 HP");
        Require(seiyaHit.Entity.HitPoints == 11, "ordinary HP subtraction stores remaining HP");
        Require(seiyaHit.Entity.Motion.ActionState == 0x40, "surviving ordinary type enters reaction family $40");
        Require(seiyaHit.Entity.Motion.StatePhase == 0x48, "right-facing hit seeds phase $40+8");
        Require(!seiyaHit.AttackRetired, "Seiya hit object is not retired by $9A27");
        Require(seiyaHit.AttackSlot.Object.Type == 0x64, "Seiya projectile remains active after hit helper");
        Require(seiyaHit.PrimarySoundId == 0x28, "overlap requests primary hit sound $28");
        Require(seiyaHit.SeventhSense == 100, "nonlethal hit does not change Seventh Sense");

        var hyogaHit = PlatformProjectileEntityHit.ResolveSingle(
            PlatformAttackSlotId.Slot0,
            Attack(0x64, facing: 0x40),
            entity,
            hitbox,
            PlatformSaintIndex.Hyoga,
            attackDamage72: 21,
            currentSeventhSense: 100,
            engineSubstate02: 1);
        Require(hyogaHit.Outcome == PlatformProjectileHitOutcome.HitPointsReduced, "Hyoga ordinary hit resolves HP path");
        Require(hyogaHit.Entity.HitPoints == 9, "Hyoga damage leaves nine HP in fixture");
        Require(hyogaHit.AttackRetired, "Hyoga current hit object is retired by $9A27");
        Require(hyogaHit.AttackSlot.Object.Y == 0xF0 && hyogaHit.AttackSlot.Object.Type == 0xFE, "hit retirement writes only inactive Y/type markers");
        Require(hyogaHit.AttackSlot.RangeCounter == 9, "hit retirement leaves range counter untouched");

        var kill = PlatformProjectileEntityHit.ResolveSingle(
            PlatformAttackSlotId.Slot0,
            Attack(0x64),
            entity,
            hitbox,
            PlatformSaintIndex.Seiya,
            attackDamage72: 30,
            currentSeventhSense: 100,
            engineSubstate02: 1);
        Require(kill.Outcome == PlatformProjectileHitOutcome.EntityKilled, "HP equal to attack damage takes kill branch");
        Require(kill.Entity.Motion.ActionState == 0xD0, "ordinary killed entity enters $D0");
        Require(kill.Entity.HitPoints == 30, "kill branch does not store zero HP back to record");
        Require(kill.SeventhSense == 132, "packed BCD $32 reward adds 32 Seventh Sense");

        var killRewardSuppressed = PlatformProjectileEntityHit.ResolveSingle(
            PlatformAttackSlotId.Slot0,
            Attack(0x64),
            entity,
            hitbox,
            PlatformSaintIndex.Seiya,
            attackDamage72: 30,
            currentSeventhSense: 100,
            engineSubstate02: 0);
        Require(killRewardSuppressed.SeventhSense == 100, "substate02 zero suppresses D1E0 reward");

        // Type $0D bypasses $9A27 entirely, even for Hyoga/Shiryu, and uses $A0
        // instead of $D0 on lethal HP resolution.
        var type0D = Entity(type: 0x0D, hp: 10, reward: 0x05);
        var type0DKill = PlatformProjectileEntityHit.ResolveSingle(
            PlatformAttackSlotId.Slot0,
            Attack(0x64),
            type0D,
            hitbox,
            PlatformSaintIndex.Hyoga,
            attackDamage72: 10,
            currentSeventhSense: 0,
            engineSubstate02: 1);
        Require(type0DKill.Outcome == PlatformProjectileHitOutcome.EntityKilled, "type0D still uses HP path");
        Require(type0DKill.Entity.Motion.ActionState == 0xA0, "type0D lethal hit enters $A0");
        Require(!type0DKill.AttackRetired, "type0D bypasses Hyoga/Shiryu hit retirement helper");
        Require(type0DKill.SecondarySoundId is null, "type0D bypasses type-reaction sound lookup");

        // Types 1..4 use non-HP $E0 reaction after optional projectile retirement.
        var type1 = Entity(type: 1, hp: 30, reward: 0x32);
        var type1Hit = PlatformProjectileEntityHit.ResolveSingle(
            PlatformAttackSlotId.Slot0,
            Attack(0x64),
            type1,
            hitbox,
            PlatformSaintIndex.Shiryu,
            attackDamage72: 200,
            currentSeventhSense: 0,
            engineSubstate02: 1);
        Require(type1Hit.Outcome == PlatformProjectileHitOutcome.ReactionOnly, "type1 does not use ordinary HP subtraction in this collision branch");
        Require(type1Hit.Entity.HitPoints == 30, "type1 reaction preserves HP");
        Require(type1Hit.Entity.Motion.Y == 0x56 && type1Hit.Entity.Motion.ActionState == 0xE0, "type1 reaction adds six Y and enters $E0");
        Require(type1Hit.AttackRetired, "Shiryu projectile is retired before type1 reaction");
        Require(type1Hit.SecondarySoundId == 0x28, "type1 reads reaction sound from $C0D3");

        // $0A/$0B branch uses phase magnitude 4, no HP/action rewrite.
        var type0A = Entity(type: 0x0A, hp: 77, reward: 0x00);
        var type0AHit = PlatformProjectileEntityHit.ResolveSingle(
            PlatformAttackSlotId.Slot0,
            Attack(0x64, facing: 0x00),
            type0A,
            hitbox,
            PlatformSaintIndex.Hyoga,
            attackDamage72: 200,
            currentSeventhSense: 0,
            engineSubstate02: 1);
        Require(type0AHit.Outcome == PlatformProjectileHitOutcome.ReactionOnly, "type0A uses special reaction-only branch");
        Require(type0AHit.Entity.HitPoints == 77 && type0AHit.Entity.Motion.ActionState == 0x10, "type0A special branch preserves HP/action");
        Require(type0AHit.Entity.Motion.StatePhase == 0x04, "left-facing type0A hit seeds phase four");
        Require(type0AHit.AttackRetired, "Hyoga type0A hit still calls $9A27");
        Require(type0AHit.SecondarySoundId is null, "type0A branch bypasses reaction-sound table");

        // Terrain E0-EF on the projectile-facing side blocks only the phase write.
        var phaseBlockedEntity = Entity(type: 0, hp: 30, reward: 0x00, rightProbe: 0xE4) with
        {
            Motion = Entity(type: 0, hp: 30, reward: 0x00, rightProbe: 0xE4).Motion with { StatePhase = 0x12 },
        };
        var phaseBlocked = PlatformProjectileEntityHit.ResolveSingle(
            PlatformAttackSlotId.Slot0,
            Attack(0x64, facing: 0x40),
            phaseBlockedEntity,
            hitbox,
            PlatformSaintIndex.Seiya,
            attackDamage72: 1,
            currentSeventhSense: 0,
            engineSubstate02: 1);
        Require(phaseBlocked.Entity.Motion.ActionState == 0x40, "terrain phase block still enters action $40");
        Require(phaseBlocked.Entity.Motion.StatePhase == 0x12, "$E0-$EF facing probe preserves existing phase");

        // Type $0E consumes the collision/retirement/sounds but returns before HP.
        var type0E = Entity(type: 0x0E, hp: 44, reward: 0x00);
        var type0EHit = PlatformProjectileEntityHit.ResolveSingle(
            PlatformAttackSlotId.Slot0,
            Attack(0x64),
            type0E,
            hitbox,
            PlatformSaintIndex.Hyoga,
            attackDamage72: 200,
            currentSeventhSense: 0,
            engineSubstate02: 1);
        Require(type0EHit.Outcome == PlatformProjectileHitOutcome.IgnoredAfterCollision, "type0E returns before HP/reaction");
        Require(type0EHit.Entity.HitPoints == 44, "type0E preserves HP");
        Require(type0EHit.AttackRetired, "type0E still retires Hyoga hit object first");
        Require(type0EHit.PrimarySoundId == 0x28 && type0EHit.SecondarySoundId == 0x28, "type0E still requests collision and type sounds");

        // $9915 visits all three slots in order. Shun's $54/$55 segments are not
        // retired, so two overlapping segments can both reach the kill/reward path
        // in the same call. The original kill branch leaves HP unchanged, making
        // this sequential behavior observable.
        var shunAttacks = PlatformAttackState.Empty with
        {
            Slot0 = Attack(0x54),
            Slot1 = Attack(0x55),
        };
        var doubleKillEntity = Entity(type: 0, hp: 20, reward: 0x10);
        var batch = PlatformProjectileEntityHit.ResolveAll(
            shunAttacks,
            doubleKillEntity,
            hitbox,
            PlatformSaintIndex.Shun,
            attackDamage72: 25,
            currentSeventhSense: 0,
            engineSubstate02: 1);
        Require(batch.Events[0].Outcome == PlatformProjectileHitOutcome.EntityKilled, "slot0 Shun segment kills first");
        Require(batch.Events[1].Outcome == PlatformProjectileHitOutcome.EntityKilled, "slot1 is still checked after slot0 kill");
        Require(batch.Events[2].Outcome == PlatformProjectileHitOutcome.InactiveAttack, "slot2 inactive signature is skipped");
        Require(batch.SeventhSense == 20, "two same-call kill branches each add packed BCD 10 reward");
        Require(batch.AttackState.Slot0.Object.Type == 0x54 && batch.AttackState.Slot1.Object.Type == 0x55, "Shun hit segments remain active through $9A27");
    }

    private static PlatformAttackSlot Attack(
        byte type,
        byte x = 0x50,
        byte y = 0x50,
        byte facing = 0x40) =>
        new(
            new PlatformAttackObject(
                Y: y,
                Type: type,
                Facing: facing,
                X: x,
                Field4: 0,
                Field5: 0,
                Field6: 0,
                AuxiliaryX: 0),
            RangeCounter: 9);

    private static PlatformEntityCombatState Entity(
        byte type,
        byte hp,
        byte reward,
        byte rightProbe = 0,
        byte leftProbe = 0) =>
        new(
            new PlatformCommonEntityMotionState(
                ActionState: 0x10,
                X: 0x50,
                Y: 0x50,
                StatePhase: 0,
                GroundDescriptor: 0,
                DecisionTimer: 40,
                FlagsFacing: 0x40,
                Type: type,
                TerrainProbeRight: rightProbe,
                TerrainProbeLeft: leftProbe),
            HitPoints: hp,
            SeventhSenseRewardPackedBcd: reward);

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Projectile/entity hit self-test failed: {label}");
    }
}

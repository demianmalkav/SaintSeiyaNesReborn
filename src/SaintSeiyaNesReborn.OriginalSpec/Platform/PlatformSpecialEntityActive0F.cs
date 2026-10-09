namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformSpecialEntityActive0FOutcome
{
    Active,
    SkippedInteraction,
    RemovedBeforeInteraction,
    RemovedByDeathCompletion,
}

public readonly record struct PlatformSpecialEntityActive0FState(
    PlatformCommonEntityRuntimeState Entity,
    PlatformEntityAttachedHazardState AttachedHazard,
    byte ParentOffset08);

public sealed record PlatformSpecialEntityActive0FResult(
    PlatformSpecialEntityActive0FState State,
    PlatformSpecialEntityActive0FOutcome Outcome,
    PlatformAttackState AttackState,
    PlatformContactPhaseState ContactState,
    int SeventhSense,
    PlatformCommonEntityInteractionResult? MainInteraction,
    PlatformCommonEntityAttack70PostResult LateAttack70,
    PlatformEntityAttachedHazardSpawnResult? LateSpawn,
    bool Reaction40Advanced,
    bool DeathD0Advanced);

/// <summary>
/// Clean-room active update for primary entity type $0F.
///
/// Bank-3 dispatch jumps directly from $A495 to $A74C. Type $0F therefore does
/// not enter the common $A55E preparation dispatcher and does not use either
/// special pre-dispatch. Its dedicated path always applies +2 Y, horizontal
/// +/-2 according to facing, camera correction and its own removal thresholds;
/// then, unless the action family is $40/$D0, it executes $9915 followed by
/// $98BA with square 8/8/6/6 geometry.
///
/// The path deliberately does not execute $AA70 or the $A738 +$04 cadence.
/// After interaction it rejoins $A79E. The $A7FD type-$0F shortcut bypasses the
/// normal ($3C & 3) death gate, so $D0-$DF advances on every update.
/// </summary>
public static class PlatformSpecialEntityActive0F
{
    private static readonly PlatformContactHitboxParameters ContactBox =
        new(0x08, 0x08, 0x06, 0x06);

    public static PlatformSpecialEntityActive0FResult Step(
        PlatformSpecialEntityActive0FState state,
        PlatformAttackState attacks,
        PlatformSaintIndex saint,
        int platformDamage,
        int seventhSense,
        byte engineSubstate02,
        PlatformContactPhaseState contactState,
        byte frameStartAction4E,
        byte playerX,
        byte playerY,
        byte frameCounter3C,
        byte cameraDelta43,
        byte engineState00,
        byte alternateParent08_03AB)
    {
        if (state.Entity.Motion.Type != 0x0F)
            throw new ArgumentOutOfRangeException(nameof(state), state.Entity.Motion.Type, "Type-$0F runtime accepts only entity type $0F.");

        var motion = state.Entity.Motion;

        // $A74C-$A759: vertical motion is unconditional on action family.
        motion = motion with { Y = unchecked((byte)(motion.Y + 2)) };
        state = state with { Entity = state.Entity with { Motion = motion } };
        if (motion.Y >= 0xA0)
        {
            return Finish(
                state,
                PlatformSpecialEntityActive0FOutcome.RemovedBeforeInteraction,
                attacks,
                contactState,
                seventhSense,
                interaction: null,
                NoLate70(motion),
                lateSpawn: null,
                reactionAdvanced: false,
                deathAdvanced: false);
        }

        // $A75C-$A77D: facing bit set subtracts $FE (equivalent to +2), while
        // facing bit clear subtracts +2. Camera delta is then subtracted.
        var horizontalDelta = (motion.FlagsFacing & PlatformCommonEntityMotion.FacingRightBit) != 0
            ? 2
            : -2;
        motion = motion with
        {
            X = unchecked((byte)(motion.X + horizontalDelta - cameraDelta43)),
        };
        state = state with { Entity = state.Entity with { Motion = motion } };
        if (motion.X < 0x04)
        {
            return Finish(
                state,
                PlatformSpecialEntityActive0FOutcome.RemovedBeforeInteraction,
                attacks,
                contactState,
                seventhSense,
                interaction: null,
                NoLate70(motion),
                lateSpawn: null,
                reactionAdvanced: false,
                deathAdvanced: false);
        }

        var family = (byte)(motion.ActionState & 0xF0);
        var skipInteraction = family is 0x40 or 0xD0;
        PlatformCommonEntityInteractionResult? interaction = null;

        if (!skipInteraction)
        {
            // $A78E-$A79B installs 8/8/6/6, then calls $9915 and $98BA.
            var hits = PlatformProjectileHitSequence.ResolveThreeSlots(
                attacks,
                state.Entity.ToCombatEntity(),
                saint,
                PlatformHitboxParameters.Square,
                platformDamage,
                seventhSense,
                engineSubstate02);

            var contact = PlatformEntityContact.Evaluate(
                frameStartAction4E,
                playerX,
                playerY,
                hits.Entity.X,
                hits.Entity.Y,
                contactState.HazardLatch76,
                state.Entity.LifeDrainTicks,
                state.Entity.CosmoDrainTicks,
                ContactBox);

            var contactAfter = contact.Triggered
                ? new PlatformContactPhaseState(contact.HazardLatch76, contact.DrainState)
                : contactState;
            var contactPhase = new PlatformContactAfterEntityResult(contactAfter, contact);

            interaction = new PlatformCommonEntityInteractionResult(
                hits.AttackState,
                hits.Entity,
                hits.SeventhSense,
                hits,
                contactPhase);

            attacks = hits.AttackState;
            seventhSense = hits.SeventhSense;
            contactState = contactAfter;
            state = state with { Entity = state.Entity.WithCombatEntity(hits.Entity) };
        }

        // $A79E-$A7C4: type >= $08 finishes $4F at action $00.
        var postMotion = state.Entity.Motion;
        var reactionAdvanced = false;
        if ((postMotion.ActionState & 0xF0) == 0x40)
        {
            var next = unchecked((byte)(postMotion.ActionState + 1));
            postMotion = postMotion with
            {
                ActionState = next >= 0x50 ? (byte)0x00 : next,
            };
            reactionAdvanced = true;
        }

        // $A7D0 loads type $0F into A, then $A7FD branches directly to $A807.
        // Consequently the normal ($3C & 3)==0 gate is skipped for this type.
        var deathAdvanced = false;
        var removedByDeath = false;
        if ((postMotion.ActionState & 0xF0) == 0xD0)
        {
            if (postMotion.GroundDescriptor < 0x80 || postMotion.GroundDescriptor >= 0xF0)
            {
                postMotion = postMotion with
                {
                    Y = unchecked((byte)(postMotion.Y + 2)),
                };
            }

            var next = unchecked((byte)(postMotion.ActionState + 1));
            deathAdvanced = true;
            if (next >= 0xE0)
            {
                postMotion = postMotion with { ActionState = 0 };
                removedByDeath = true;
            }
            else
            {
                postMotion = postMotion with { ActionState = next };
            }
        }

        state = state with { Entity = state.Entity with { Motion = postMotion } };
        if (removedByDeath)
        {
            return Finish(
                state,
                PlatformSpecialEntityActive0FOutcome.RemovedByDeathCompletion,
                attacks,
                contactState,
                seventhSense,
                interaction,
                NoLate70(postMotion),
                lateSpawn: null,
                reactionAdvanced,
                deathAdvanced);
        }

        // $A886 is genuinely shared. At $78 type $0F still calls $A908 and
        // requests sound $2A, but its fixed spawn template is zero, so no child
        // object is created. Terminal $7F advances to $10.
        var late70 = PlatformCommonEntityAttack70.AdvanceAfterInteraction(
            postMotion,
            frameCounter3C);
        state = state with { Entity = state.Entity with { Motion = late70.State } };

        PlatformEntityAttachedHazardSpawnResult? lateSpawn = null;
        if (late70.CallsSecondarySpawnRoutine)
        {
            var spawned = PlatformEntityAttachedHazard.TrySpawn(
                state.AttachedHazard,
                state.Entity.Motion,
                engineState00,
                alternateParent08_03AB);
            lateSpawn = spawned;
            state = state with
            {
                AttachedHazard = spawned.State,
                ParentOffset08 = spawned.ParentOffset08Value ?? state.ParentOffset08,
            };
        }

        return Finish(
            state,
            skipInteraction
                ? PlatformSpecialEntityActive0FOutcome.SkippedInteraction
                : PlatformSpecialEntityActive0FOutcome.Active,
            attacks,
            contactState,
            seventhSense,
            interaction,
            late70,
            lateSpawn,
            reactionAdvanced,
            deathAdvanced);
    }

    private static PlatformCommonEntityAttack70PostResult NoLate70(
        PlatformCommonEntityMotionState state) =>
        new(
            state,
            Advanced: false,
            CallsSecondarySpawnRoutine: false,
            SpawnTemplate: default,
            SoundId: null,
            CompletedFamily: false);

    private static PlatformSpecialEntityActive0FResult Finish(
        PlatformSpecialEntityActive0FState state,
        PlatformSpecialEntityActive0FOutcome outcome,
        PlatformAttackState attacks,
        PlatformContactPhaseState contactState,
        int seventhSense,
        PlatformCommonEntityInteractionResult? interaction,
        PlatformCommonEntityAttack70PostResult late70,
        PlatformEntityAttachedHazardSpawnResult? lateSpawn,
        bool reactionAdvanced,
        bool deathAdvanced) =>
        new(
            state,
            outcome,
            attacks,
            contactState,
            seventhSense,
            interaction,
            late70,
            lateSpawn,
            reactionAdvanced,
            deathAdvanced);
}

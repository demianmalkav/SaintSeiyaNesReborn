namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformSpecialEntityActive0D0EOutcome
{
    Active,
    SkippedInteraction,
    RemovedBeforeInteraction,
    RemovedByDeathCompletion,
    RemovedByType0DA0Completion,
}

public readonly record struct PlatformSpecialEntityActive0D0EState(
    PlatformCommonEntityRuntimeState Entity,
    byte Control04,
    PlatformEntityAttachedHazardState AttachedHazard,
    byte ParentOffset08);

public sealed record PlatformSpecialEntityActive0D0EResult(
    PlatformSpecialEntityActive0D0EState State,
    PlatformSpecialEntityActive0D0EOutcome Outcome,
    PlatformAttackState AttackState,
    PlatformContactPhaseState ContactState,
    int SeventhSense,
    PlatformEntityJumpStepResult? Jump30,
    PlatformCommonEntityInteractionResult? MainInteraction,
    PlatformEntityAttachedHazardContactResult? AttachedContact,
    PlatformCommonEntityAttack70PostResult LateAttack70,
    PlatformEntityAttachedHazardSpawnResult? LateSpawn,
    bool Control04Advanced,
    bool Reaction40Advanced,
    bool DeathD0Advanced,
    bool Type0DA0Advanced);

/// <summary>
/// Clean-room active update for scheduled platform entity types $0D/$0E.
///
/// The bank-3 dispatcher sends these types directly from $A48B/$A48F through
/// $A4BF to the shared $A55E path. They therefore MUST NOT execute the
/// $08/$09/$0C $A4A7-$A55B pre-dispatch that gates on +$04 and mutates $039A.
///
/// Their later path still shares substantial code with other entities:
/// $50/$E0 fall/removal, optional $30 jump vertical phase, camera-relative
/// movement/removal, $9915/$98BA interaction, $AA70 attached-hazard contact,
/// +$04 post-interaction cadence, >=$08 $40 reaction completion to $00, common
/// $D0 death cadence and the type-aware late $70 helper.
///
/// Type $0D additionally owns the confirmed $A86B-$A885 $A0-$AF progression:
/// increment each update and remove through $A647 when it reaches $B0.
/// </summary>
public static class PlatformSpecialEntityActive0D0E
{
    public static PlatformSpecialEntityActive0D0EResult Step(
        PlatformSpecialEntityActive0D0EState state,
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
        ValidateType(state.Entity.Motion.Type);

        var motion = state.Entity.Motion;
        var family = (byte)(motion.ActionState & 0xF0);

        // $A55E: $50/$E0 branch directly to $A57E then jumps to $A886.
        if (family is 0x50 or 0xE0)
        {
            var fall = StepFallOrDrop(motion, cameraDelta43);
            state = state with { Entity = state.Entity with { Motion = fall.State } };

            if (fall.Removed)
            {
                return Finish(
                    state,
                    PlatformSpecialEntityActive0D0EOutcome.RemovedBeforeInteraction,
                    attacks,
                    contactState,
                    seventhSense,
                    jump30: null,
                    interaction: null,
                    attachedContact: null,
                    NoLate70(fall.State),
                    lateSpawn: null,
                    control04Advanced: false,
                    reactionAdvanced: false,
                    deathAdvanced: false,
                    type0DA0Advanced: false);
            }

            var late = PlatformCommonEntityAttack70.AdvanceAfterInteraction(
                state.Entity.Motion,
                frameCounter3C);
            state = state with { Entity = state.Entity with { Motion = late.State } };

            return Finish(
                state,
                PlatformSpecialEntityActive0D0EOutcome.SkippedInteraction,
                attacks,
                contactState,
                seventhSense,
                jump30: null,
                interaction: null,
                attachedContact: null,
                late,
                lateSpawn: null,
                control04Advanced: false,
                reactionAdvanced: false,
                deathAdvanced: false,
                type0DA0Advanced: false);
        }

        // $A574/$A5BB: family $30 runs fixed-bank $C5E6 before the shared
        // horizontal/camera path. Landing may replace $3x before movement is
        // selected, so family is re-read afterwards just like the ROM.
        PlatformEntityJumpStepResult? jump30 = null;
        if (family == 0x30)
        {
            jump30 = PlatformCommonEntityMotion.StepJumpVertical(motion);
            motion = jump30.Value.State;
        }

        family = (byte)(motion.ActionState & 0xF0);
        var horizontalDelta = HorizontalDeltaAfterA5BE(
            motion,
            family,
            frameCounter3C);
        motion = motion with
        {
            X = unchecked((byte)(motion.X + horizontalDelta - cameraDelta43)),
        };
        state = state with { Entity = state.Entity with { Motion = motion } };

        if (motion.X >= 0xF8 || motion.Y is >= 0xB0 and < 0xC0)
        {
            return Finish(
                state,
                PlatformSpecialEntityActive0D0EOutcome.RemovedBeforeInteraction,
                attacks,
                contactState,
                seventhSense,
                jump30,
                interaction: null,
                attachedContact: null,
                NoLate70(motion),
                lateSpawn: null,
                control04Advanced: false,
                reactionAdvanced: false,
                deathAdvanced: false,
                type0DA0Advanced: false);
        }

        // $A6B6 CMP #$0D / BCS $A700: both $0D/$0E bypass the phase-zero
        // proximity-fall branch completely.
        family = (byte)(motion.ActionState & 0xF0);
        var skipInteraction = family is 0xE0 or 0xD0 or 0xA0 or 0x40;

        PlatformCommonEntityInteractionResult? interaction = null;
        PlatformEntityAttachedHazardContactResult? attachedContact = null;
        var control04Advanced = false;

        if (!skipInteraction)
        {
            // $A719+: shared tall 16/8/14/4 projectile/contact geometry.
            interaction = PlatformCommonEntityInteractionPhases.ResolveProjectileHitThenContact(
                attacks,
                state.Entity.ToCombatEntity(),
                saint,
                PlatformHitboxParameters.Tall,
                platformDamage,
                seventhSense,
                engineSubstate02,
                contactState,
                frameStartAction4E,
                playerX,
                playerY,
                state.Entity.LifeDrainTicks,
                state.Entity.CosmoDrainTicks);

            attacks = interaction.AttackState;
            seventhSense = interaction.SeventhSense;
            contactState = interaction.ContactPhase.State;
            state = state with { Entity = state.Entity.WithCombatEntity(interaction.Entity) };

            // $A729-$A735: shared attached-record contact follows main contact.
            var childContact = PlatformEntityAttachedHazard.EvaluateContact(
                state.AttachedHazard,
                frameStartAction4E,
                playerX,
                playerY,
                contactState.HazardLatch76,
                state.Entity.LifeDrainTicks,
                state.Entity.CosmoDrainTicks);
            attachedContact = childContact;
            state = state with { AttachedHazard = childContact.State };
            if (childContact.Triggered)
            {
                contactState = new PlatformContactPhaseState(
                    childContact.HazardLatch76,
                    childContact.DrainState);
            }

            // $A738-$A746 is shared even though these types bypass the earlier
            // $A4A7 +$04 pre-dispatch. Spawn initializes +$04 to zero; if some
            // other confirmed path makes it nonzero, this cadence still applies.
            if (state.Control04 != 0)
            {
                var next04 = unchecked((byte)(state.Control04 + 1));
                if (next04 >= 0x0C)
                    next04 = 0;
                state = state with { Control04 = next04 };
                control04Advanced = true;
            }
        }

        // $A79E+ sees any $9915 state mutation in the same update.
        var post = StepPostInteraction(state.Entity.Motion, frameCounter3C);
        state = state with { Entity = state.Entity with { Motion = post.State } };

        if (post.RemovedByDeathCompletion)
        {
            return Finish(
                state,
                PlatformSpecialEntityActive0D0EOutcome.RemovedByDeathCompletion,
                attacks,
                contactState,
                seventhSense,
                jump30,
                interaction,
                attachedContact,
                NoLate70(post.State),
                lateSpawn: null,
                control04Advanced,
                post.ReactionAdvanced,
                post.DeathAdvanced,
                post.Type0DA0Advanced);
        }

        if (post.RemovedByType0DA0Completion)
        {
            return Finish(
                state,
                PlatformSpecialEntityActive0D0EOutcome.RemovedByType0DA0Completion,
                attacks,
                contactState,
                seventhSense,
                jump30,
                interaction,
                attachedContact,
                NoLate70(post.State),
                lateSpawn: null,
                control04Advanced,
                post.ReactionAdvanced,
                post.DeathAdvanced,
                post.Type0DA0Advanced);
        }

        // Shared $A886-$A8DB helper is already type-aware. For $0D/$0E it
        // completes $70 at $10. At midpoint $78 it calls A908/play $2A, but the
        // fixed template table contains no secondary object for these types.
        var late70 = PlatformCommonEntityAttack70.AdvanceAfterInteraction(
            post.State,
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
                ? PlatformSpecialEntityActive0D0EOutcome.SkippedInteraction
                : PlatformSpecialEntityActive0D0EOutcome.Active,
            attacks,
            contactState,
            seventhSense,
            jump30,
            interaction,
            attachedContact,
            late70,
            lateSpawn,
            control04Advanced,
            post.ReactionAdvanced,
            post.DeathAdvanced,
            post.Type0DA0Advanced);
    }

    private static int HorizontalDeltaAfterA5BE(
        PlatformCommonEntityMotionState state,
        byte family,
        byte frameCounter3C)
    {
        if (family == 0x30)
            return PlatformCommonEntityMotion.JumpHorizontalDelta(
                state.ActionState,
                state.Type,
                frameCounter3C);

        // $A5D9-$A5F3: these families take the camera-only path. $0D/$0E
        // ordinary preparation at $A970 is a no-op, so e.g. action $10 (which
        // can result from completing $70) reaches the facing-walk path below.
        if (family is 0x00 or 0x70 or 0xD0 or 0x40 or 0xA0)
            return 0;

        var step = PlatformCommonEntityMotion.HorizontalStep(
            state.Type,
            frameCounter3C);
        return PlatformCommonEntityMotion.FacingRight(state.FlagsFacing)
            ? step
            : -step;
    }

    private readonly record struct FallOrDropResult(
        PlatformCommonEntityMotionState State,
        bool Removed);

    private static FallOrDropResult StepFallOrDrop(
        PlatformCommonEntityMotionState state,
        byte cameraDelta43)
    {
        var oldFamily = (byte)(state.ActionState & 0xF0);
        state = state with
        {
            X = unchecked((byte)(state.X - cameraDelta43)),
            Y = unchecked((byte)(state.Y + 3)),
        };

        if (state.Y >= 0xB0)
            return new(state, Removed: true);

        if (oldFamily == 0x50)
        {
            var landing = PlatformCommonEntityLanding.Resolve(state);
            state = landing.State;
        }

        return new(state, Removed: false);
    }

    private readonly record struct PostResult(
        PlatformCommonEntityMotionState State,
        bool ReactionAdvanced,
        bool DeathAdvanced,
        bool Type0DA0Advanced,
        bool RemovedByDeathCompletion,
        bool RemovedByType0DA0Completion);

    private static PostResult StepPostInteraction(
        PlatformCommonEntityMotionState state,
        byte frameCounter3C)
    {
        var reactionAdvanced = false;
        if ((state.ActionState & 0xF0) == 0x40)
        {
            var next = unchecked((byte)(state.ActionState + 1));
            state = state with
            {
                // $A7AE-$A7C1: every type >=$08 returns completed $40 to $00.
                ActionState = next >= 0x50 ? (byte)0x00 : next,
            };
            reactionAdvanced = true;
        }

        var deathAdvanced = false;
        var deathRemoved = false;
        if ((state.ActionState & 0xF0) == 0xD0 && (frameCounter3C & 0x03) == 0)
        {
            if (state.GroundDescriptor < 0x80 || state.GroundDescriptor >= 0xF0)
            {
                state = state with
                {
                    Y = unchecked((byte)(state.Y + 2)),
                };
            }

            var next = unchecked((byte)(state.ActionState + 1));
            deathAdvanced = true;
            if (next >= 0xE0)
            {
                state = state with { ActionState = 0 };
                deathRemoved = true;
            }
            else
            {
                state = state with { ActionState = next };
            }
        }

        var a0Advanced = false;
        var a0Removed = false;
        if (!deathRemoved
            && state.Type == 0x0D
            && (state.ActionState & 0xF0) == 0xA0)
        {
            var next = unchecked((byte)(state.ActionState + 1));
            a0Advanced = true;
            if (next >= 0xB0)
            {
                state = state with { ActionState = next };
                a0Removed = true;
            }
            else
            {
                state = state with { ActionState = next };
            }
        }

        return new(
            state,
            reactionAdvanced,
            deathAdvanced,
            a0Advanced,
            deathRemoved,
            a0Removed);
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

    private static PlatformSpecialEntityActive0D0EResult Finish(
        PlatformSpecialEntityActive0D0EState state,
        PlatformSpecialEntityActive0D0EOutcome outcome,
        PlatformAttackState attacks,
        PlatformContactPhaseState contactState,
        int seventhSense,
        PlatformEntityJumpStepResult? jump30,
        PlatformCommonEntityInteractionResult? interaction,
        PlatformEntityAttachedHazardContactResult? attachedContact,
        PlatformCommonEntityAttack70PostResult late70,
        PlatformEntityAttachedHazardSpawnResult? lateSpawn,
        bool control04Advanced,
        bool reactionAdvanced,
        bool deathAdvanced,
        bool type0DA0Advanced) =>
        new(
            state,
            outcome,
            attacks,
            contactState,
            seventhSense,
            jump30,
            interaction,
            attachedContact,
            late70,
            lateSpawn,
            control04Advanced,
            reactionAdvanced,
            deathAdvanced,
            type0DA0Advanced);

    private static void ValidateType(byte type)
    {
        if (type is not (0x0D or 0x0E))
            throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                "$0D/$0E active runtime accepts only scheduled types $0D and $0E.");
    }
}

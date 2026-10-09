namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformSpecialEntityActive08090COutcome
{
    Active,
    SkippedInteraction,
    RemovedBeforeInteraction,
    RemovedByDeathCompletion,
}

public readonly record struct PlatformSpecialEntityActive08090CState(
    PlatformSpecialEntityControlState Control,
    PlatformEntityAttachedHazardState AttachedHazard,
    byte ParentOffset08,
    byte GlobalCounter039A);

public sealed record PlatformSpecialEntityActive08090CResult(
    PlatformSpecialEntityActive08090CState State,
    PlatformSpecialEntityActive08090COutcome Outcome,
    PlatformAttackState AttackState,
    PlatformContactPhaseState ContactState,
    int SeventhSense,
    PlatformSpecialEntityPreDispatchResult PreDispatch,
    PlatformEntityAttachedHazardSpawnResult? ImmediateSpawn,
    PlatformCommonEntityInteractionResult? MainInteraction,
    PlatformEntityAttachedHazardContactResult? AttachedContact,
    PlatformCommonEntityAttack70PostResult LateAttack70,
    PlatformEntityAttachedHazardSpawnResult? LateSpawn,
    bool Control04Advanced,
    bool Type0CBobApplied,
    int Type0CBobDelta,
    bool Reaction40Advanced,
    bool DeathD0Advanced);

/// <summary>
/// Clean-room active update for scheduled special platform entity types
/// $08/$09/$0C, composing the confirmed pre-dispatch, shared $A55E path,
/// $9915/$98BA interaction, attached-hazard $AA70 contact, +$04 cadence and the
/// late $40/$D0/$70 phases.
///
/// This deliberately remains separate from the common $00-$07 dispatcher:
/// these special types do not use common chase/jump AI, return $40 reactions to
/// action $00, type $0C has a frame-driven vertical bob, and their attached
/// hazard has its own collision rules.
/// </summary>
public static class PlatformSpecialEntityActive08090C
{
    private static readonly sbyte[] Type0CBob = [1, 1, -1, -1];

    public static PlatformSpecialEntityActive08090CResult Step(
        PlatformSpecialEntityActive08090CState state,
        PlatformAttackState attacks,
        PlatformSaintIndex saint,
        int platformDamage,
        int seventhSense,
        byte engineSubstate02,
        PlatformContactPhaseState contactState,
        byte frameStartAction4E,
        byte playerX,
        byte playerY,
        byte entropy48,
        byte frameCounter3C,
        byte cameraDelta43,
        byte engineState00,
        byte alternateParent08_03AB)
    {
        ValidateType(state.Control.Entity.Motion.Type);

        // $A478-$A55E. Even the immediate $A908 path falls through into $A55E
        // after resetting $039A; it does not end the entity update.
        var pre = PlatformSpecialEntityPreDispatch08090C.Step(
            state.Control,
            playerX,
            state.GlobalCounter039A,
            entropy48);
        state = state with
        {
            Control = pre.State,
            GlobalCounter039A = pre.GlobalCounter039A,
        };

        PlatformEntityAttachedHazardSpawnResult? immediateSpawn = null;
        if (pre.CallsSecondarySpawnRoutine)
        {
            var spawn = PlatformEntityAttachedHazard.TrySpawn(
                state.AttachedHazard,
                state.Control.Entity.Motion,
                engineState00,
                alternateParent08_03AB);
            immediateSpawn = spawn;
            state = state with
            {
                AttachedHazard = spawn.State,
                ParentOffset08 = spawn.ParentOffset08Value ?? state.ParentOffset08,
            };
        }

        var motion = state.Control.Entity.Motion;
        var family = (byte)(motion.ActionState & 0xF0);

        // $50/$E0 take the dedicated $A57E path and JMP directly to $A886.
        // They do not execute $9915/$98BA/$AA70, +$04 cadence, type0C bob, or
        // $40/$D0 post-path logic on this update.
        if (family is 0x50 or 0xE0)
        {
            var fall = StepFallOrDrop(motion, cameraDelta43);
            state = state with
            {
                Control = state.Control with
                {
                    Entity = state.Control.Entity with { Motion = fall.State },
                },
            };

            if (fall.Removed)
            {
                return Finish(
                    state,
                    PlatformSpecialEntityActive08090COutcome.RemovedBeforeInteraction,
                    attacks,
                    contactState,
                    seventhSense,
                    pre,
                    immediateSpawn,
                    null,
                    null,
                    NoLate70(fall.State),
                    null,
                    control04Advanced: false,
                    bobApplied: false,
                    bobDelta: 0,
                    reactionAdvanced: false,
                    deathAdvanced: false);
            }

            var late = PlatformCommonEntityAttack70.AdvanceAfterInteraction(
                state.Control.Entity.Motion,
                frameCounter3C);
            state = state with
            {
                Control = state.Control with
                {
                    Entity = state.Control.Entity with { Motion = late.State },
                },
            };

            return Finish(
                state,
                PlatformSpecialEntityActive08090COutcome.SkippedInteraction,
                attacks,
                contactState,
                seventhSense,
                pre,
                immediateSpawn,
                null,
                null,
                late,
                null,
                control04Advanced: false,
                bobApplied: false,
                bobDelta: 0,
                reactionAdvanced: false,
                deathAdvanced: false);
        }

        // Shared $A5BE-$A700 path. Special types do not receive common facing
        // walk/chase motion here; for the supported families screen X only
        // receives the camera correction before removal/proximity checks.
        motion = motion with { X = unchecked((byte)(motion.X - cameraDelta43)) };
        state = state with
        {
            Control = state.Control with
            {
                Entity = state.Control.Entity with { Motion = motion },
            },
        };

        if (motion.X >= 0xF8 || motion.Y is >= 0xB0 and < 0xC0)
        {
            return Finish(
                state,
                PlatformSpecialEntityActive08090COutcome.RemovedBeforeInteraction,
                attacks,
                contactState,
                seventhSense,
                pre,
                immediateSpawn,
                null,
                null,
                NoLate70(motion),
                null,
                control04Advanced: false,
                bobApplied: false,
                bobDelta: 0,
                reactionAdvanced: false,
                deathAdvanced: false);
        }

        // Only type $0C can reach the shared phase-zero proximity fall branch;
        // $08/$09 are explicitly excluded at $A6AA/$A6B0.
        if (motion.Type == 0x0C && ShouldStartProximityFall(motion, playerX, playerY))
        {
            motion = motion with
            {
                ActionState = 0x50,
                Y = unchecked((byte)(motion.Y + 6)),
            };
            state = state with
            {
                Control = state.Control with
                {
                    Entity = state.Control.Entity with { Motion = motion },
                },
            };
        }

        family = (byte)(motion.ActionState & 0xF0);
        var skipInteraction = family is 0x40 or 0xD0 or 0xA0;

        PlatformCommonEntityInteractionResult? interaction = null;
        PlatformEntityAttachedHazardContactResult? attachedContact = null;
        var control04Advanced = false;

        if (!skipInteraction)
        {
            interaction = PlatformCommonEntityInteractionPhases.ResolveProjectileHitThenContact(
                attacks,
                state.Control.Entity.ToCombatEntity(),
                saint,
                PlatformHitboxParameters.Tall,
                platformDamage,
                seventhSense,
                engineSubstate02,
                contactState,
                frameStartAction4E,
                playerX,
                playerY,
                state.Control.Entity.LifeDrainTicks,
                state.Control.Entity.CosmoDrainTicks);

            attacks = interaction.AttackState;
            seventhSense = interaction.SeventhSense;
            contactState = interaction.ContactPhase.State;
            state = state with
            {
                Control = state.Control with
                {
                    Entity = state.Control.Entity.WithCombatEntity(interaction.Entity),
                },
            };

            // $A729-$A735 installs 4/4/2/2 then calls $AA70. Main entity contact
            // ran first, so an already-seeded $76 prevents double contact here.
            var childContact = PlatformEntityAttachedHazard.EvaluateContact(
                state.AttachedHazard,
                frameStartAction4E,
                playerX,
                playerY,
                contactState.HazardLatch76,
                state.Control.Entity.LifeDrainTicks,
                state.Control.Entity.CosmoDrainTicks);
            attachedContact = childContact;
            state = state with { AttachedHazard = childContact.State };
            if (childContact.Triggered)
            {
                contactState = new PlatformContactPhaseState(
                    childContact.HazardLatch76,
                    childContact.DrainState);
            }

            // $A738-$A746: zero remains zero; nonzero increments and wraps to
            // zero when reaching $0C.
            if (state.Control.Control04 != 0)
            {
                var next04 = unchecked((byte)(state.Control.Control04 + 1));
                if (next04 >= 0x0C)
                    next04 = 0;
                state = state with
                {
                    Control = state.Control with { Control04 = next04 },
                };
                control04Advanced = true;
            }
        }

        // $A79E+ sees mutations created by $9915 in this same update.
        var post = StepPostInteraction(
            state.Control.Entity.Motion,
            frameCounter3C);
        state = state with
        {
            Control = state.Control with
            {
                Entity = state.Control.Entity with { Motion = post.State },
            },
        };

        if (post.RemovedByDeathCompletion)
        {
            return Finish(
                state,
                PlatformSpecialEntityActive08090COutcome.RemovedByDeathCompletion,
                attacks,
                contactState,
                seventhSense,
                pre,
                immediateSpawn,
                interaction,
                attachedContact,
                NoLate70(post.State),
                null,
                control04Advanced,
                post.BobApplied,
                post.BobDelta,
                post.ReactionAdvanced,
                post.DeathAdvanced);
        }

        // $A886 runs after all preceding post-path work. A hit that changed $70
        // to $40/$D0 therefore suppresses late attack progression automatically.
        var late70 = PlatformCommonEntityAttack70.AdvanceAfterInteraction(
            post.State,
            frameCounter3C);
        state = state with
        {
            Control = state.Control with
            {
                Entity = state.Control.Entity with { Motion = late70.State },
            },
        };

        PlatformEntityAttachedHazardSpawnResult? lateSpawn = null;
        if (late70.CallsSecondarySpawnRoutine)
        {
            // This spawn occurs after AA70 for the current update, so the newly
            // created attached hazard cannot contact the player until a later
            // entity update.
            var spawned = PlatformEntityAttachedHazard.TrySpawn(
                state.AttachedHazard,
                state.Control.Entity.Motion,
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
                ? PlatformSpecialEntityActive08090COutcome.SkippedInteraction
                : PlatformSpecialEntityActive08090COutcome.Active,
            attacks,
            contactState,
            seventhSense,
            pre,
            immediateSpawn,
            interaction,
            attachedContact,
            late70,
            lateSpawn,
            control04Advanced,
            post.BobApplied,
            post.BobDelta,
            post.ReactionAdvanced,
            post.DeathAdvanced);
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

        // E0 deliberately skips C491 landing.
        return new(state, Removed: false);
    }

    private readonly record struct PostResult(
        PlatformCommonEntityMotionState State,
        bool BobApplied,
        int BobDelta,
        bool ReactionAdvanced,
        bool DeathAdvanced,
        bool RemovedByDeathCompletion);

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
                ActionState = next >= 0x50 ? (byte)0x00 : next,
            };
            reactionAdvanced = true;
        }

        var bobApplied = false;
        var bobDelta = 0;
        if (state.Type == 0x0C && (frameCounter3C & 0x07) == 0)
        {
            var index = (frameCounter3C >> 3) & 0x03;
            bobDelta = Type0CBob[index];
            state = state with
            {
                Y = unchecked((byte)(state.Y + bobDelta)),
            };
            bobApplied = true;
        }

        var deathAdvanced = false;
        var removed = false;
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
                removed = true;
            }
            else
            {
                state = state with { ActionState = next };
            }
        }

        return new(
            state,
            bobApplied,
            bobDelta,
            reactionAdvanced,
            deathAdvanced,
            removed);
    }

    private static bool ShouldStartProximityFall(
        PlatformCommonEntityMotionState state,
        byte playerX,
        byte playerY)
    {
        if (state.StatePhase != 0)
            return false;
        if (state.GroundDescriptor is >= 0xE0 and < 0xF0)
            return false;
        if ((state.ActionState & 0xF0) is 0x40 or 0xD0)
            return false;
        if (playerY >= 0x81)
            return false;

        var playerRow = (byte)(playerY & 0xF0);
        if (playerRow <= state.Y)
            return false;
        if (state.X < 0x21 || state.X >= 0xC0)
            return false;

        if (state.X >= playerX)
        {
            var left = unchecked((byte)(state.X - 0x20));
            return left < playerX;
        }

        if (playerX < 0x20)
            return false;
        return unchecked((byte)(state.X + 0x20)) >= playerX;
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

    private static PlatformSpecialEntityActive08090CResult Finish(
        PlatformSpecialEntityActive08090CState state,
        PlatformSpecialEntityActive08090COutcome outcome,
        PlatformAttackState attacks,
        PlatformContactPhaseState contactState,
        int seventhSense,
        PlatformSpecialEntityPreDispatchResult pre,
        PlatformEntityAttachedHazardSpawnResult? immediateSpawn,
        PlatformCommonEntityInteractionResult? interaction,
        PlatformEntityAttachedHazardContactResult? attachedContact,
        PlatformCommonEntityAttack70PostResult late70,
        PlatformEntityAttachedHazardSpawnResult? lateSpawn,
        bool control04Advanced,
        bool bobApplied,
        int bobDelta,
        bool reactionAdvanced,
        bool deathAdvanced) =>
        new(
            state,
            outcome,
            attacks,
            contactState,
            seventhSense,
            pre,
            immediateSpawn,
            interaction,
            attachedContact,
            late70,
            lateSpawn,
            control04Advanced,
            bobApplied,
            bobDelta,
            reactionAdvanced,
            deathAdvanced);

    private static void ValidateType(byte type)
    {
        if (type is not (0x08 or 0x09 or 0x0C))
            throw new ArgumentOutOfRangeException(nameof(type), type, "Special active runtime covers only types $08/$09/$0C.");
    }
}

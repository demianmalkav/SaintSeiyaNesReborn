using SaintSeiyaNesReborn.OriginalSpec;

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public sealed record PlatformAuxiliaryHazardSlotFrameResult(
    PlatformAuxiliaryHazardUpdateResult Update,
    PlatformEntityContactResult? Contact,
    PlatformProjectileHitSequenceResult? ProjectileHits,
    PlatformCombatEntity LogicalEntityAfterInteraction,
    PlatformAttackState AttackState,
    PlatformContactPhaseState ContactState,
    int SeventhSense);

public sealed record PlatformAuxiliaryHazardPairFrameResult(
    PlatformAuxiliaryHazardSpawnerResult Spawn,
    PlatformAuxiliaryHazardSlotFrameResult SlotA,
    PlatformAuxiliaryHazardSlotFrameResult SlotB,
    PlatformAuxiliaryHazardSpawnerState State,
    PlatformAttackState AttackState,
    PlatformContactPhaseState ContactState,
    int SeventhSense);

/// <summary>
/// Collision/interactions for the independent $07B0/$07B8 auxiliary hazard
/// records. The original order differs from common enemies:
///
/// $976A motion -> $9887/$98BA hazard-to-player contact ->
/// $9896/$9915 player-projectile-to-hazard (ordinary families only).
///
/// The $F4/$F5 homing family reaches contact but deliberately skips $9915.
/// </summary>
public static class PlatformAuxiliaryHazardInteractions
{
    public static readonly PlatformHitboxParameters ProjectileHitbox = new(0x04, 0x04, 0x03, 0x03);

    public static PlatformAuxiliaryHazardSlotFrameResult StepSlot(
        PlatformAuxiliaryHazardSlot slot,
        PlatformAuxiliaryHazardMetadata metadata,
        sbyte horizontalStep03A5,
        sbyte verticalStep03A6,
        byte frameCounter3C,
        byte cameraDelta43,
        byte playerX3F,
        byte playerY40,
        byte frameStartPlayerAction4E,
        PlatformAttackState attacks,
        PlatformContactPhaseState contactState,
        PlatformSaintIndex saint,
        int platformDamage,
        int seventhSense,
        byte engineSubstate02)
    {
        var update = PlatformAuxiliaryHazardUpdater.Step(
            slot,
            horizontalStep03A5,
            verticalStep03A6,
            frameCounter3C,
            cameraDelta43,
            playerX3F,
            playerY40,
            frameStartPlayerAction4E);

        var logical = new PlatformCombatEntity(
            State: 0,
            X: update.Slot.X3,
            Y: update.Slot.Y0,
            Type: metadata.Kind09,
            HitPoints: metadata.HitPoints0C,
            SeventhSenseRewardBcd: metadata.SeventhSenseReward0F);

        PlatformEntityContactResult? contact = null;
        if (update.RequestsPlayerContactCheck)
        {
            var evaluated = PlatformEntityContact.Evaluate(
                frameStartPlayerAction4E,
                playerX3F,
                playerY40,
                logical.X,
                logical.Y,
                contactState.HazardLatch76,
                metadata.LifeDrainTicks0E,
                metadata.CosmoDrainTicks0D,
                PlatformContactHitboxParameters.AuxiliaryHazard);
            contact = evaluated;

            if (evaluated.Triggered)
            {
                contactState = new PlatformContactPhaseState(
                    evaluated.HazardLatch76,
                    evaluated.DrainState);
            }
        }

        PlatformProjectileHitSequenceResult? hits = null;
        if (update.RequestsProjectileHitCheck)
        {
            hits = PlatformProjectileHitSequence.ResolveThreeSlots(
                attacks,
                logical,
                saint,
                ProjectileHitbox,
                platformDamage,
                seventhSense,
                engineSubstate02);
            attacks = hits.AttackState;
            logical = hits.Entity;
            seventhSense = hits.SeventhSense;
        }

        return new PlatformAuxiliaryHazardSlotFrameResult(
            update,
            contact,
            hits,
            logical,
            attacks,
            contactState,
            seventhSense);
    }

    /// <summary>
    /// Composes the complete confirmed $96B4 -> $9761 two-slot order. Slot A
    /// executes before B and threads the physical shared velocities, player
    /// attack objects and contact latch/drain state into the second slot.
    /// </summary>
    public static PlatformAuxiliaryHazardPairFrameResult StepPair(
        PlatformAuxiliaryHazardSpawnerState state,
        byte entropy48,
        byte frameCounter3C,
        byte cameraDelta43,
        byte playerX3F,
        byte playerY40,
        byte frameStartPlayerAction4E,
        PlatformAttackState attacks,
        PlatformContactPhaseState contactState,
        PlatformSaintIndex saint,
        int platformDamage,
        int seventhSense,
        byte engineSubstate02)
    {
        var spawn = PlatformAuxiliaryHazardSpawner.Step(state, entropy48, playerY40);
        state = spawn.State;

        var slotA = StepSlot(
            state.SlotA,
            state.MetadataA,
            state.HorizontalStep03A5,
            state.VerticalStep03A6,
            frameCounter3C,
            cameraDelta43,
            playerX3F,
            playerY40,
            frameStartPlayerAction4E,
            attacks,
            contactState,
            saint,
            platformDamage,
            seventhSense,
            engineSubstate02);

        attacks = slotA.AttackState;
        contactState = slotA.ContactState;
        seventhSense = slotA.SeventhSense;
        state = state with
        {
            SlotA = slotA.Update.Slot,
            HorizontalStep03A5 = slotA.Update.HorizontalStep03A5,
            VerticalStep03A6 = slotA.Update.VerticalStep03A6,
        };

        var slotB = StepSlot(
            state.SlotB,
            state.MetadataB,
            state.HorizontalStep03A5,
            state.VerticalStep03A6,
            frameCounter3C,
            cameraDelta43,
            playerX3F,
            playerY40,
            frameStartPlayerAction4E,
            attacks,
            contactState,
            saint,
            platformDamage,
            seventhSense,
            engineSubstate02);

        attacks = slotB.AttackState;
        contactState = slotB.ContactState;
        seventhSense = slotB.SeventhSense;
        state = state with
        {
            SlotB = slotB.Update.Slot,
            HorizontalStep03A5 = slotB.Update.HorizontalStep03A5,
            VerticalStep03A6 = slotB.Update.VerticalStep03A6,
        };

        return new PlatformAuxiliaryHazardPairFrameResult(
            spawn,
            slotA,
            slotB,
            state,
            attacks,
            contactState,
            seventhSense);
    }
}

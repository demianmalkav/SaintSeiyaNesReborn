using SaintSeiyaNesReborn.OriginalSpec;

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public sealed record PlatformAuxiliaryInteractionResult(
    PlatformAuxiliarySlotState Slot,
    PlatformAttackState AttackState,
    PlatformContactPhaseState ContactState,
    int SeventhSense,
    PlatformEntityContactResult? Contact,
    PlatformProjectileHitSequenceResult HitSequence,
    bool ContactSuppressedByPlayer80);

/// <summary>
/// Interaction tail reached by an active $976A slot after movement. The original
/// auxiliary ordering differs from common $A442 entities: $9887 performs
/// entity->player contact first, then $9896/$9915 tests player projectiles.
/// </summary>
public static class PlatformAuxiliaryInteraction
{
    public static PlatformAuxiliaryInteractionResult Resolve(
        PlatformAuxiliarySlotState slot,
        PlatformAttackState attacks,
        PlatformContactPhaseState contactState,
        PlatformSaintIndex saint,
        byte frameStartAction4E,
        byte playerX3F,
        byte playerY40,
        int platformDamage,
        int seventhSense,
        byte engineSubstate02)
    {
        if (slot.Visual.IsInactive)
            throw new InvalidOperationException("Inactive auxiliary slot cannot enter $9887/$9896 interaction.");

        // $989C copies object Y/X into the logical target before each collision
        // call. PlatformAuxiliaryMotion already performs that synchronization.
        var logic = slot.Logic;
        var contactSuppressed = PlatformActionState.Family(frameStartAction4E)
            == (byte)PlatformActionFamily.Damage80;

        PlatformEntityContactResult? contact = null;
        if (!contactSuppressed)
        {
            contact = PlatformEntityContact.Evaluate(
                frameStartAction4E,
                playerX3F,
                playerY40,
                logic.X,
                logic.Y,
                contactState.HazardLatch76,
                logic.LifeDrainTicks,
                logic.CosmoDrainTicks,
                PlatformHitboxParameters.Auxiliary);

            if (contact.Value.Triggered)
            {
                contactState = new PlatformContactPhaseState(
                    contact.Value.HazardLatch76,
                    contact.Value.DrainState);
            }
        }

        // $9896 runs regardless of the player-$80 suppression above.
        var hits = PlatformProjectileHitSequence.ResolveThreeSlots(
            attacks,
            logic.ToCombatEntity(),
            saint,
            PlatformHitboxParameters.Auxiliary,
            platformDamage,
            seventhSense,
            engineSubstate02);

        slot = slot with { Logic = logic.WithCombatEntity(hits.Entity) };
        return new PlatformAuxiliaryInteractionResult(
            slot,
            hits.AttackState,
            contactState,
            hits.SeventhSense,
            contact,
            hits,
            contactSuppressed);
    }
}

public sealed record PlatformAuxiliarySlotFrameResult(
    PlatformAuxiliaryMotionResult Motion,
    PlatformAuxiliaryInteractionResult? Interaction);

public sealed record PlatformAuxiliarySlotsUpdateResult(
    PlatformAuxiliarySlotState SlotA,
    PlatformAuxiliarySlotState SlotB,
    PlatformAuxiliarySharedState Shared,
    PlatformAttackState AttackState,
    PlatformContactPhaseState ContactState,
    int SeventhSense,
    PlatformAuxiliarySlotFrameResult SlotAFrame,
    PlatformAuxiliarySlotFrameResult SlotBFrame);

public sealed record PlatformAuxiliarySpawnAndUpdateResult(
    PlatformAuxiliarySpawnerResult Spawn,
    PlatformAuxiliarySlotsUpdateResult Update);

/// <summary>
/// Clean-room composition of the fixed frame order `$96B4 -> $9761`.
///
/// Slot A ($07B0/$03DA) is always processed before slot B ($07B8/$03EA).
/// Homing direction globals, player attacks, contact latch/drain state and
/// Seventh Sense therefore flow from A into B exactly as shared RAM does.
/// </summary>
public static class PlatformAuxiliarySlotsPipeline
{
    public static PlatformAuxiliarySpawnAndUpdateResult SpawnThenUpdate(
        PlatformAuxiliarySlotState slotA,
        PlatformAuxiliarySlotState slotB,
        PlatformAuxiliarySharedState shared,
        PlatformAuxiliarySpawnStats stats,
        PlatformAttackState attacks,
        PlatformContactPhaseState contactState,
        PlatformSaintIndex saint,
        byte frameStartAction4E,
        byte playerX3F,
        byte playerY40,
        int platformDamage,
        int seventhSense,
        byte frameCounter3C,
        byte cameraDelta43,
        byte entropy48,
        byte engineSubstate02)
    {
        var spawn = PlatformAuxiliarySpawner.Step(
            slotA,
            slotB,
            shared,
            stats,
            playerY40,
            entropy48);

        var update = Update(
            spawn.SlotA,
            spawn.SlotB,
            spawn.Shared,
            attacks,
            contactState,
            saint,
            frameStartAction4E,
            playerX3F,
            playerY40,
            platformDamage,
            seventhSense,
            frameCounter3C,
            cameraDelta43,
            engineSubstate02);

        return new PlatformAuxiliarySpawnAndUpdateResult(spawn, update);
    }

    public static PlatformAuxiliarySlotsUpdateResult Update(
        PlatformAuxiliarySlotState slotA,
        PlatformAuxiliarySlotState slotB,
        PlatformAuxiliarySharedState shared,
        PlatformAttackState attacks,
        PlatformContactPhaseState contactState,
        PlatformSaintIndex saint,
        byte frameStartAction4E,
        byte playerX3F,
        byte playerY40,
        int platformDamage,
        int seventhSense,
        byte frameCounter3C,
        byte cameraDelta43,
        byte engineSubstate02)
    {
        var a = ProcessSlot(
            slotA,
            shared,
            attacks,
            contactState,
            saint,
            frameStartAction4E,
            playerX3F,
            playerY40,
            platformDamage,
            seventhSense,
            frameCounter3C,
            cameraDelta43,
            engineSubstate02);

        var b = ProcessSlot(
            slotB,
            a.Motion.Shared,
            a.AttackState,
            a.ContactState,
            saint,
            frameStartAction4E,
            playerX3F,
            playerY40,
            platformDamage,
            a.SeventhSense,
            frameCounter3C,
            cameraDelta43,
            engineSubstate02);

        return new PlatformAuxiliarySlotsUpdateResult(
            a.Slot,
            b.Slot,
            b.Motion.Shared,
            b.AttackState,
            b.ContactState,
            b.SeventhSense,
            new PlatformAuxiliarySlotFrameResult(a.Motion, a.Interaction),
            new PlatformAuxiliarySlotFrameResult(b.Motion, b.Interaction));
    }

    private sealed record SlotCarry(
        PlatformAuxiliarySlotState Slot,
        PlatformAuxiliaryMotionResult Motion,
        PlatformAuxiliaryInteractionResult? Interaction,
        PlatformAttackState AttackState,
        PlatformContactPhaseState ContactState,
        int SeventhSense);

    private static SlotCarry ProcessSlot(
        PlatformAuxiliarySlotState slot,
        PlatformAuxiliarySharedState shared,
        PlatformAttackState attacks,
        PlatformContactPhaseState contactState,
        PlatformSaintIndex saint,
        byte frameStartAction4E,
        byte playerX3F,
        byte playerY40,
        int platformDamage,
        int seventhSense,
        byte frameCounter3C,
        byte cameraDelta43,
        byte engineSubstate02)
    {
        var motion = PlatformAuxiliaryMotion.Step(
            slot,
            shared,
            playerX3F,
            playerY40,
            frameCounter3C,
            cameraDelta43);
        slot = motion.Slot;

        if (!motion.ReadyForInteraction)
        {
            return new SlotCarry(
                slot,
                motion,
                null,
                attacks,
                contactState,
                seventhSense);
        }

        var interaction = PlatformAuxiliaryInteraction.Resolve(
            slot,
            attacks,
            contactState,
            saint,
            frameStartAction4E,
            playerX3F,
            playerY40,
            platformDamage,
            seventhSense,
            engineSubstate02);

        return new SlotCarry(
            interaction.Slot,
            motion,
            interaction,
            interaction.AttackState,
            interaction.ContactState,
            interaction.SeventhSense);
    }
}

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformCommonEdgeSpawnOutcome
{
    SpawnGateMismatch,
    ActionFamilyBlocked,
    SlotOccupied,
    ScheduledTypeExcluded,
    CooldownDecremented,
    GroundSearchRejected,
    Spawned,
    SecondSlotDisabled,
}

public readonly record struct PlatformCommonEdgeSpawnSlotResult(
    PlatformCommonEdgeSpawnOutcome Outcome,
    PlatformCommonEntityRuntimeState Entity,
    byte VisualSprite,
    byte Cooldown03B8,
    int GroundSamples,
    byte? AcceptedGroundDescriptor)
{
    public bool Spawned => Outcome == PlatformCommonEdgeSpawnOutcome.Spawned;
}

public readonly record struct PlatformCommonEdgeSpawnPairResult(
    PlatformCommonEdgeSpawnSlotResult SlotA,
    PlatformCommonEdgeSpawnSlotResult? SlotB,
    byte Cooldown03B8,
    bool PairProcessed);

/// <summary>
/// Clean-room semantic reduction of bank-0 $B6D0-$B7FC.
///
/// This producer complements the table-driven bank-1 spawner. It excludes
/// low-nibble types $08/$09/$0C/$0D/$0E, uses a shared $03B8 cooldown, and can
/// attempt slot B after slot A when bit 7 of $58 is set.
/// </summary>
public static class PlatformCommonEdgeSpawner
{
    public const byte FreeVisualSprite = 0xFE;
    public const byte SpawnVisualSprite = 0xFD;
    public const byte CooldownSeed = 0x30;

    public static bool IsTableDrivenExcludedType(byte engine58) =>
        (engine58 & 0x0F) is 0x08 or 0x09 or 0x0C or 0x0D or 0x0E;

    public static PlatformCommonEdgeSpawnPairResult StepPair(
        PlatformStageMap stage,
        int scrollX,
        byte playerX,
        byte cameraDelta43,
        byte entropy48,
        byte engine58,
        byte spawnGate03B7,
        byte cooldown03B8,
        PlatformSpecialSpawnProfile profile,
        PlatformCommonEntityRuntimeState entityA,
        byte visualSpriteA,
        PlatformCommonEntityRuntimeState entityB,
        byte visualSpriteB)
    {
        if (spawnGate03B7 != 0 && spawnGate03B7 != engine58)
        {
            var blocked = new PlatformCommonEdgeSpawnSlotResult(
                PlatformCommonEdgeSpawnOutcome.SpawnGateMismatch,
                entityA,
                visualSpriteA,
                cooldown03B8,
                0,
                null);
            return new(blocked, null, cooldown03B8, PairProcessed: false);
        }

        var a = StepSlot(
            stage,
            scrollX,
            playerX,
            cameraDelta43,
            entropy48,
            engine58,
            cooldown03B8,
            profile,
            entityA,
            visualSpriteA);
        cooldown03B8 = a.Cooldown03B8;

        if ((engine58 & 0x80) == 0)
        {
            return new(a, null, cooldown03B8, PairProcessed: true);
        }

        var b = StepSlot(
            stage,
            scrollX,
            playerX,
            cameraDelta43,
            entropy48,
            engine58,
            cooldown03B8,
            profile,
            entityB,
            visualSpriteB);

        return new(a, b, b.Cooldown03B8, PairProcessed: true);
    }

    public static PlatformCommonEdgeSpawnSlotResult StepSlot(
        PlatformStageMap stage,
        int scrollX,
        byte playerX,
        byte cameraDelta43,
        byte entropy48,
        byte engine58,
        byte cooldown03B8,
        PlatformSpecialSpawnProfile profile,
        PlatformCommonEntityRuntimeState existingEntity,
        byte existingVisualSprite)
    {
        var family = existingEntity.Motion.ActionState & 0xF0;
        if (family is 0xD0 or 0x40)
        {
            return NoSpawn(
                PlatformCommonEdgeSpawnOutcome.ActionFamilyBlocked,
                existingEntity,
                existingVisualSprite,
                cooldown03B8);
        }

        if (existingVisualSprite != FreeVisualSprite)
        {
            return NoSpawn(
                PlatformCommonEdgeSpawnOutcome.SlotOccupied,
                existingEntity,
                existingVisualSprite,
                cooldown03B8);
        }

        if (IsTableDrivenExcludedType(engine58))
        {
            return NoSpawn(
                PlatformCommonEdgeSpawnOutcome.ScheduledTypeExcluded,
                existingEntity,
                existingVisualSprite,
                cooldown03B8);
        }

        if (cooldown03B8 != 0)
        {
            return new PlatformCommonEdgeSpawnSlotResult(
                PlatformCommonEdgeSpawnOutcome.CooldownDecremented,
                existingEntity,
                existingVisualSprite,
                unchecked((byte)(cooldown03B8 - 1)),
                0,
                null);
        }

        cooldown03B8 = CooldownSeed;
        var type = (byte)(engine58 & 0x0F);
        var motion = existingEntity.Motion;
        var groundSamples = 0;
        byte? acceptedGround = null;

        if (type == 0x0F)
        {
            if ((entropy48 & 0x08) == 0)
            {
                motion = motion with
                {
                    X = 0x00,
                    Y = 0x00,
                    FlagsFacing = 0x41,
                };
            }
            else
            {
                motion = motion with
                {
                    X = unchecked((byte)(playerX + 0x78)),
                    Y = 0x00,
                    FlagsFacing = 0x01,
                };
            }
        }
        else
        {
            var spawnRight = cameraDelta43 != 0 || (entropy48 & 0x08) != 0;
            motion = motion with
            {
                X = spawnRight ? (byte)0xF8 : (byte)0x00,
                Y = 0x20,
                FlagsFacing = spawnRight ? (byte)0x01 : (byte)0x41,
            };

            while (true)
            {
                groundSamples++;
                var descriptor = stage.DescriptorAt(scrollX + motion.X + 8, motion.Y + 0x20) ?? (byte)0x00;
                motion = motion with { GroundDescriptor = descriptor };

                if (descriptor >= 0xA8)
                {
                    acceptedGround = descriptor;
                    break;
                }

                var candidateY = unchecked((byte)(motion.Y + 0x10));
                if (candidateY >= 0x81)
                {
                    var partial = existingEntity with { Motion = motion };
                    return new PlatformCommonEdgeSpawnSlotResult(
                        PlatformCommonEdgeSpawnOutcome.GroundSearchRejected,
                        partial,
                        existingVisualSprite,
                        cooldown03B8,
                        groundSamples,
                        null);
                }

                motion = motion with { Y = candidateY };
            }
        }

        motion = motion with
        {
            ActionState = 0x10,
            StatePhase = 0x00,
            // Offset +$04 is also zeroed in the original but is not currently
            // represented independently in PlatformCommonEntityMotionState.
            DecisionTimer = (byte)((entropy48 & 0x3F) + 0x1F),
            Type = type,
        };

        var entity = existingEntity with
        {
            Motion = motion,
            HitPoints = profile.HitPoints0C,
            LifeDrainTicks = profile.LifeDrainTicks0D,
            CosmoDrainTicks = profile.CosmoDrainTicks0E,
            SeventhSenseRewardBcd = profile.SeventhSenseRewardBcd0F,
        };

        return new PlatformCommonEdgeSpawnSlotResult(
            PlatformCommonEdgeSpawnOutcome.Spawned,
            entity,
            SpawnVisualSprite,
            cooldown03B8,
            groundSamples,
            acceptedGround);
    }

    private static PlatformCommonEdgeSpawnSlotResult NoSpawn(
        PlatformCommonEdgeSpawnOutcome outcome,
        PlatformCommonEntityRuntimeState entity,
        byte visualSprite,
        byte cooldown) =>
        new(outcome, entity, visualSprite, cooldown, 0, null);
}

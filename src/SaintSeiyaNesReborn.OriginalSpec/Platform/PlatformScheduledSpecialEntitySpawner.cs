namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformSpecialSpawnEntry(
    byte CameraLowAligned,
    byte CameraHigh,
    byte SpawnY,
    byte Reserved03 = 0);

public readonly record struct PlatformSpecialSpawnProfile(
    byte HitPoints0C,
    byte LifeDrainTicks0D,
    byte CosmoDrainTicks0E,
    byte SeventhSenseRewardBcd0F);

public enum PlatformScheduledSpecialSpawnOutcome
{
    UnsupportedType,
    SlotOccupied,
    NoMatchingTrigger,
    DuplicateLowTrigger,
    Spawned,
}

public readonly record struct PlatformScheduledSpecialSpawnResult(
    PlatformScheduledSpecialSpawnOutcome Outcome,
    PlatformCommonEntityRuntimeState Entity,
    byte VisualSprite,
    byte LastTriggerLow03A2,
    PlatformSpecialSpawnEntry? Entry)
{
    public bool Spawned => Outcome == PlatformScheduledSpecialSpawnOutcome.Spawned;
}

public readonly record struct PlatformScheduledSpecialSpawnPairResult(
    PlatformScheduledSpecialSpawnResult SlotA,
    PlatformScheduledSpecialSpawnResult SlotB,
    byte LastTriggerLow03A2);

/// <summary>
/// Clean-room semantic reduction of PRG-bank-1 $8925-$89D1.
///
/// This is the table-driven producer that can populate either common logical
/// record ($03BA or $03CA) for special types selected by the low nibble of $58:
/// $08, $09, $0C, $0D and $0E. The caller supplies the already-extracted
/// substate schedule so ROM-owned table bytes do not need to live in source.
///
/// The original checks visual sprite byte +1 for $FE before scanning the
/// schedule. Trigger matching uses $45 as camera high and ($44 & $F8) as camera
/// low. $03A2 remembers only the trigger LOW byte, so two entries sharing that
/// low byte are considered duplicates even if their high bytes differ.
///
/// Slot A is attempted before slot B. A successful A spawn therefore updates
/// $03A2 before B scans the same schedule, which normally makes B reject that
/// trigger as a duplicate in the same frame. If A is occupied, B can take it.
/// </summary>
public static class PlatformScheduledSpecialEntitySpawner
{
    public const byte FreeVisualSprite = 0xFE;
    public const byte SpawnVisualSprite = 0xFD;
    public const byte SpawnX = 0xF8;

    public static bool IsSupportedType(byte engine58) =>
        (engine58 & 0x0F) is 0x08 or 0x09 or 0x0C or 0x0D or 0x0E;

    public static PlatformScheduledSpecialSpawnPairResult TrySpawnPair(
        PlatformCommonEntityRuntimeState entityA,
        byte visualSpriteA,
        PlatformCommonEntityRuntimeState entityB,
        byte visualSpriteB,
        byte engine58,
        byte cameraLow44,
        byte cameraHigh45,
        byte lastTriggerLow03A2,
        PlatformSpecialSpawnProfile profile,
        IReadOnlyList<PlatformSpecialSpawnEntry> schedule)
    {
        var a = TrySpawn(
            entityA,
            visualSpriteA,
            engine58,
            cameraLow44,
            cameraHigh45,
            lastTriggerLow03A2,
            profile,
            schedule);

        var b = TrySpawn(
            entityB,
            visualSpriteB,
            engine58,
            cameraLow44,
            cameraHigh45,
            a.LastTriggerLow03A2,
            profile,
            schedule);

        return new PlatformScheduledSpecialSpawnPairResult(a, b, b.LastTriggerLow03A2);
    }

    public static PlatformScheduledSpecialSpawnResult TrySpawn(
        PlatformCommonEntityRuntimeState existingEntity,
        byte existingVisualSprite,
        byte engine58,
        byte cameraLow44,
        byte cameraHigh45,
        byte lastTriggerLow03A2,
        PlatformSpecialSpawnProfile profile,
        IReadOnlyList<PlatformSpecialSpawnEntry> schedule)
    {
        if (!IsSupportedType(engine58))
        {
            return NoSpawn(
                PlatformScheduledSpecialSpawnOutcome.UnsupportedType,
                existingEntity,
                existingVisualSprite,
                lastTriggerLow03A2);
        }

        if (existingVisualSprite != FreeVisualSprite)
        {
            return NoSpawn(
                PlatformScheduledSpecialSpawnOutcome.SlotOccupied,
                existingEntity,
                existingVisualSprite,
                lastTriggerLow03A2);
        }

        var alignedLow = (byte)(cameraLow44 & 0xF8);
        PlatformSpecialSpawnEntry? matched = null;
        foreach (var entry in schedule)
        {
            if (entry.CameraHigh == cameraHigh45 && entry.CameraLowAligned == alignedLow)
            {
                matched = entry;
                break;
            }
        }

        if (!matched.HasValue)
        {
            return NoSpawn(
                PlatformScheduledSpecialSpawnOutcome.NoMatchingTrigger,
                existingEntity,
                existingVisualSprite,
                lastTriggerLow03A2);
        }

        var trigger = matched.Value;
        if (trigger.CameraLowAligned == lastTriggerLow03A2)
        {
            return new PlatformScheduledSpecialSpawnResult(
                PlatformScheduledSpecialSpawnOutcome.DuplicateLowTrigger,
                existingEntity,
                existingVisualSprite,
                lastTriggerLow03A2,
                trigger);
        }

        var type = (byte)(engine58 & 0x0F);
        var motion = existingEntity.Motion with
        {
            ActionState = 0x00,
            X = SpawnX,
            Y = trigger.SpawnY,
            StatePhase = 0x00,
            GroundDescriptor = 0x00,
            // Offset +$06 is deliberately preserved: $898B-$89D1 never writes
            // it. The same is true for terrain probes +$0A/+0B.
            FlagsFacing = 0x01,
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

        return new PlatformScheduledSpecialSpawnResult(
            PlatformScheduledSpecialSpawnOutcome.Spawned,
            entity,
            SpawnVisualSprite,
            trigger.CameraLowAligned,
            trigger);
    }

    private static PlatformScheduledSpecialSpawnResult NoSpawn(
        PlatformScheduledSpecialSpawnOutcome outcome,
        PlatformCommonEntityRuntimeState entity,
        byte visualSprite,
        byte lastTriggerLow) =>
        new(outcome, entity, visualSprite, lastTriggerLow, null);
}

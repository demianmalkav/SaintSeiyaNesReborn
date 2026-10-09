namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformAuxiliaryHazardProfile(
    byte Kind,
    byte InitialSpriteType,
    byte HitPoints,
    byte CosmoDrainTicks,
    byte LifeDrainTicks,
    byte SeventhSenseRewardBcd)
{
    /// <summary>
    /// Bank-1 $9C92 profile table plus fixed-bank $C169 initial sprite table.
    /// Only kinds 0..4 are emitted by the stage hazard selector at $9BB9+.
    /// </summary>
    public static PlatformAuxiliaryHazardProfile ForKind(byte kind) => kind switch
    {
        0 => new(0, 0xFE, 0x00, 0x00, 0x00, 0x00),
        1 => new(1, 0x80, 0x1E, 0x02, 0x02, 0x32),
        2 => new(2, 0x83, 0x1E, 0x04, 0x01, 0x32),
        3 => new(3, 0xF4, 0x00, 0x05, 0x02, 0x00),
        4 => new(4, 0xDA, 0x00, 0x04, 0x03, 0x00),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Auxiliary hazard kind must be 0..4."),
    };
}

/// <summary>
/// The eight-byte auxiliary visual/motion record at $07B0 or $07B8.
/// Offset names intentionally mirror the ROM layout; mirror fields are maintained
/// because $976A updates them independently for the F4/F5 homing family.
/// </summary>
public readonly record struct PlatformAuxiliaryHazardSlot(
    byte Y0,
    byte SpriteType1,
    byte Flags2,
    byte X3,
    byte MirrorY4,
    byte MirrorSpriteType5,
    byte MirrorFlags6,
    byte MirrorX7)
{
    public static PlatformAuxiliaryHazardSlot Empty =>
        new(0xF0, 0xFE, 0, 0, 0xFE, 0xFE, 0, 0);

    public bool IsEmpty => SpriteType1 == 0xFE;
}

/// <summary>
/// Combat metadata written to $03DA/$03EA offsets +$09 and +$0C..+$0F by $96CE.
/// Position is copied into +$01/+$02 immediately before collision checks, so it is
/// deliberately derived from the slot rather than persisted here.
/// </summary>
public readonly record struct PlatformAuxiliaryHazardMetadata(
    byte Kind09,
    byte HitPoints0C,
    byte CosmoDrainTicks0D,
    byte LifeDrainTicks0E,
    byte SeventhSenseReward0F)
{
    public static PlatformAuxiliaryHazardMetadata FromProfile(PlatformAuxiliaryHazardProfile profile) =>
        new(
            profile.Kind,
            profile.HitPoints,
            profile.CosmoDrainTicks,
            profile.LifeDrainTicks,
            profile.SeventhSenseRewardBcd);
}

public enum PlatformAuxiliaryHazardSpawnAttemptOutcome
{
    NotEligible,
    Occupied,
    CooldownDecremented,
    RejectedSpawnY,
    Spawned,
}

public readonly record struct PlatformAuxiliaryHazardSpawnerState(
    byte CurrentKind03B4,
    byte ActiveKind03B5,
    byte Cooldown03B6,
    sbyte HorizontalStep03A5,
    sbyte VerticalStep03A6,
    PlatformAuxiliaryHazardSlot SlotA,
    PlatformAuxiliaryHazardSlot SlotB,
    PlatformAuxiliaryHazardMetadata MetadataA,
    PlatformAuxiliaryHazardMetadata MetadataB);

public readonly record struct PlatformAuxiliaryHazardSpawnerResult(
    PlatformAuxiliaryHazardSpawnerState State,
    PlatformAuxiliaryHazardSpawnAttemptOutcome SlotAOutcome,
    PlatformAuxiliaryHazardSpawnAttemptOutcome SlotBOutcome);

/// <summary>
/// Bank-3 $96B4/$96CE auxiliary hazard spawner.
///
/// The two slot attempts are sequential and share $03B6. Kind 3 intentionally
/// attempts only slot A. The cooldown seed is 32..63 because the immediately
/// preceding CMP #$FE leaves carry set and $96E4 uses ADC #$1F without CLC.
/// </summary>
public static class PlatformAuxiliaryHazardSpawner
{
    public static PlatformAuxiliaryHazardSpawnerResult Step(
        PlatformAuxiliaryHazardSpawnerState state,
        byte entropy48,
        byte playerY40)
    {
        if (state.CurrentKind03B4 == 0 || state.CurrentKind03B4 != state.ActiveKind03B5)
        {
            return new(
                state,
                PlatformAuxiliaryHazardSpawnAttemptOutcome.NotEligible,
                PlatformAuxiliaryHazardSpawnAttemptOutcome.NotEligible);
        }

        if (state.ActiveKind03B5 is > 4)
            throw new InvalidOperationException($"ROM hazard selector produced unsupported kind ${state.ActiveKind03B5:X2}.");

        var first = TrySlot(
            state.SlotA,
            state.MetadataA,
            state.Cooldown03B6,
            state.HorizontalStep03A5,
            state.VerticalStep03A6,
            state.ActiveKind03B5,
            entropy48,
            playerY40);

        state = state with
        {
            SlotA = first.Slot,
            MetadataA = first.Metadata,
            Cooldown03B6 = first.Cooldown,
            HorizontalStep03A5 = first.HorizontalStep,
            VerticalStep03A6 = first.VerticalStep,
        };

        if (state.ActiveKind03B5 == 3)
        {
            return new(
                state,
                first.Outcome,
                PlatformAuxiliaryHazardSpawnAttemptOutcome.NotEligible);
        }

        var second = TrySlot(
            state.SlotB,
            state.MetadataB,
            state.Cooldown03B6,
            state.HorizontalStep03A5,
            state.VerticalStep03A6,
            state.ActiveKind03B5,
            entropy48,
            playerY40);

        state = state with
        {
            SlotB = second.Slot,
            MetadataB = second.Metadata,
            Cooldown03B6 = second.Cooldown,
            HorizontalStep03A5 = second.HorizontalStep,
            VerticalStep03A6 = second.VerticalStep,
        };

        return new(state, first.Outcome, second.Outcome);
    }

    private readonly record struct SlotAttempt(
        PlatformAuxiliaryHazardSlot Slot,
        PlatformAuxiliaryHazardMetadata Metadata,
        byte Cooldown,
        sbyte HorizontalStep,
        sbyte VerticalStep,
        PlatformAuxiliaryHazardSpawnAttemptOutcome Outcome);

    private static SlotAttempt TrySlot(
        PlatformAuxiliaryHazardSlot slot,
        PlatformAuxiliaryHazardMetadata metadata,
        byte cooldown,
        sbyte horizontalStep,
        sbyte verticalStep,
        byte kind,
        byte entropy48,
        byte playerY40)
    {
        if (!slot.IsEmpty)
        {
            return new(slot, metadata, cooldown, horizontalStep, verticalStep,
                PlatformAuxiliaryHazardSpawnAttemptOutcome.Occupied);
        }

        if (cooldown != 0)
        {
            return new(slot, metadata, unchecked((byte)(cooldown - 1)), horizontalStep, verticalStep,
                PlatformAuxiliaryHazardSpawnAttemptOutcome.CooldownDecremented);
        }

        // $96D0 CMP #$FE establishes carry=1. $96E0 AND #$1F does not alter C,
        // therefore ADC #$1F yields ($48 & $1F) + $20.
        cooldown = unchecked((byte)((entropy48 & 0x1F) + 0x20));

        var fromRight = kind == 3 || (entropy48 & 0x08) != 0;
        horizontalStep = fromRight ? (sbyte)-1 : (sbyte)1;
        verticalStep = 0;
        var x = fromRight ? (byte)0xFF : (byte)0x02;
        var flags = fromRight ? (byte)0x02 : (byte)0x42;

        // These fields are written before the Y-range rejection at $972D-$972F.
        slot = slot with
        {
            X3 = x,
            Flags2 = flags,
            MirrorFlags6 = flags,
        };

        var yBase = playerY40 != 0 && playerY40 < 0x87
            ? playerY40
            : (byte)0x50;
        var y = unchecked((byte)(yBase - (entropy48 & 0x3F)));
        if (y >= 0xA0)
        {
            return new(slot, metadata, cooldown, horizontalStep, verticalStep,
                PlatformAuxiliaryHazardSpawnAttemptOutcome.RejectedSpawnY);
        }

        var profile = PlatformAuxiliaryHazardProfile.ForKind(kind);
        slot = slot with
        {
            Y0 = y,
            SpriteType1 = profile.InitialSpriteType,
        };
        metadata = PlatformAuxiliaryHazardMetadata.FromProfile(profile);

        return new(slot, metadata, cooldown, horizontalStep, verticalStep,
            PlatformAuxiliaryHazardSpawnAttemptOutcome.Spawned);
    }
}

public enum PlatformAuxiliaryHazardUpdateOutcome
{
    Empty,
    Active,
    RemovedHorizontalBoundary,
    RemovedHomingVerticalBoundary,
}

public readonly record struct PlatformAuxiliaryHazardUpdateResult(
    PlatformAuxiliaryHazardSlot Slot,
    sbyte HorizontalStep03A5,
    sbyte VerticalStep03A6,
    PlatformAuxiliaryHazardUpdateOutcome Outcome,
    bool RequestsPlayerContactCheck,
    bool RequestsProjectileHitCheck)
{
    public bool Removed => Outcome is
        PlatformAuxiliaryHazardUpdateOutcome.RemovedHorizontalBoundary or
        PlatformAuxiliaryHazardUpdateOutcome.RemovedHomingVerticalBoundary;
}

/// <summary>
/// Bank-3 $976A-$9899 updater for one $07B0/$07B8 auxiliary hazard slot.
/// Collision itself remains delegated to the already reconstructed $98BA/$9915
/// semantics; this class exposes exactly which checks the original requests.
/// </summary>
public static class PlatformAuxiliaryHazardUpdater
{
    public static PlatformAuxiliaryHazardUpdateResult Step(
        PlatformAuxiliaryHazardSlot slot,
        sbyte horizontalStep03A5,
        sbyte verticalStep03A6,
        byte frameCounter3C,
        byte cameraDelta43,
        byte playerX3F,
        byte playerY40,
        byte frameStartPlayerAction4E)
    {
        if (slot.IsEmpty)
            return Result(slot, horizontalStep03A5, verticalStep03A6, PlatformAuxiliaryHazardUpdateOutcome.Empty, false, false);

        if (slot.SpriteType1 is 0xF4 or 0xF5)
        {
            return StepHoming(
                slot,
                horizontalStep03A5,
                verticalStep03A6,
                frameCounter3C,
                cameraDelta43,
                playerX3F,
                playerY40,
                frameStartPlayerAction4E);
        }

        if ((frameCounter3C & 0x03) == 0)
            slot = slot with { SpriteType1 = NextOrdinaryAnimation(slot.SpriteType1) };

        var xDelta = (slot.Flags2 & 0x40) != 0 ? 2 : -2;
        slot = slot with
        {
            X3 = unchecked((byte)(slot.X3 + xDelta - cameraDelta43)),
        };

        if ((slot.X3 & 0xFE) < 0x02)
        {
            slot = Remove(slot);
            return Result(slot, horizontalStep03A5, verticalStep03A6,
                PlatformAuxiliaryHazardUpdateOutcome.RemovedHorizontalBoundary, false, false);
        }

        return Result(
            slot,
            horizontalStep03A5,
            verticalStep03A6,
            PlatformAuxiliaryHazardUpdateOutcome.Active,
            PlayerCanBeContacted(frameStartPlayerAction4E),
            requestsProjectileHitCheck: true);
    }

    private static PlatformAuxiliaryHazardUpdateResult StepHoming(
        PlatformAuxiliaryHazardSlot slot,
        sbyte horizontalStep,
        sbyte verticalStep,
        byte frameCounter3C,
        byte cameraDelta43,
        byte playerX,
        byte playerY,
        byte frameStartPlayerAction4E)
    {
        if ((frameCounter3C & 0x03) == 0)
        {
            var next = slot.SpriteType1 == 0xF4 ? (byte)0xF5 : (byte)0xF4;
            slot = slot with
            {
                SpriteType1 = next,
                MirrorSpriteType5 = unchecked((byte)(next - 2)),
            };
        }

        // Equality zeroing occurs every update before the 32-frame steering refresh.
        if (slot.X3 == playerX)
            horizontalStep = 0;
        if (slot.Y0 == playerY)
            verticalStep = 0;

        if ((frameCounter3C & 0x1F) == 0)
        {
            horizontalStep = CompareAxis(slot.X3, playerX);
            verticalStep = CompareAxis(slot.Y0, playerY);
        }

        if ((frameCounter3C & 0x01) == 0)
        {
            var nextY = unchecked((byte)(slot.Y0 + verticalStep));
            var mirrorY = unchecked((byte)(nextY - 8));
            slot = slot with
            {
                Y0 = nextY,
                MirrorY4 = mirrorY,
            };

            if (mirrorY >= 0xA0)
            {
                slot = Remove(slot);
                return Result(slot, horizontalStep, verticalStep,
                    PlatformAuxiliaryHazardUpdateOutcome.RemovedHomingVerticalBoundary, false, false);
            }

            var nextX = unchecked((byte)(slot.X3 + horizontalStep));
            slot = slot with
            {
                X3 = nextX,
                MirrorX7 = nextX,
            };
        }

        var cameraX = unchecked((byte)(slot.X3 - cameraDelta43));
        slot = slot with
        {
            X3 = cameraX,
            MirrorX7 = cameraX,
        };

        if ((cameraX & 0xFE) < 0x02)
        {
            slot = Remove(slot);
            return Result(slot, horizontalStep, verticalStep,
                PlatformAuxiliaryHazardUpdateOutcome.RemovedHorizontalBoundary, false, false);
        }

        return Result(
            slot,
            horizontalStep,
            verticalStep,
            PlatformAuxiliaryHazardUpdateOutcome.Active,
            PlayerCanBeContacted(frameStartPlayerAction4E),
            requestsProjectileHitCheck: false);
    }

    private static bool PlayerCanBeContacted(byte frameStartPlayerAction4E) =>
        (frameStartPlayerAction4E & 0xF0) != 0x80;

    private static sbyte CompareAxis(byte current, byte target) =>
        current == target ? (sbyte)0 : current < target ? (sbyte)1 : (sbyte)-1;

    private static byte NextOrdinaryAnimation(byte current) => current switch
    {
        0x80 => 0x81,
        0x81 => 0x82,
        0x82 => 0x80,
        0x83 => 0x84,
        0x84 => 0x85,
        0x85 => 0x86,
        0x86 => 0x83,
        0xDA => 0xDE,
        0xDE => 0xDF,
        0xDF => 0xDA,
        _ => throw new InvalidOperationException($"Unsupported auxiliary hazard sprite type ${current:X2}."),
    };

    private static PlatformAuxiliaryHazardSlot Remove(PlatformAuxiliaryHazardSlot slot) =>
        slot with
        {
            Y0 = 0xF0,
            SpriteType1 = 0xFE,
            MirrorY4 = 0xFE,
            MirrorSpriteType5 = 0xFE,
        };

    private static PlatformAuxiliaryHazardUpdateResult Result(
        PlatformAuxiliaryHazardSlot slot,
        sbyte horizontal,
        sbyte vertical,
        PlatformAuxiliaryHazardUpdateOutcome outcome,
        bool requestsPlayerContactCheck,
        bool requestsProjectileHitCheck) =>
        new(slot, horizontal, vertical, outcome, requestsPlayerContactCheck, requestsProjectileHitCheck);
}

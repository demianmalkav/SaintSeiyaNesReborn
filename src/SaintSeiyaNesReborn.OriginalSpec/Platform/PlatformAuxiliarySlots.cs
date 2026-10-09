namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformAuxiliaryVisualState(
    byte Y,
    byte VisualType,
    byte Flags2,
    byte X,
    byte Aux4,
    byte Aux5,
    byte Flags6,
    byte XMirror7)
{
    public bool IsInactive => VisualType == 0xFE;

    public static PlatformAuxiliaryVisualState Inactive =>
        new(0xF0, 0xFE, 0, 0, 0xFE, 0xFE, 0, 0);
}

public readonly record struct PlatformAuxiliaryLogicState(
    byte State,
    byte X,
    byte Y,
    byte Motion3,
    byte Type,
    byte RightTerrain0A,
    byte LeftTerrain0B,
    byte SeventhSenseRewardBcd,
    byte LifeDrainTicks,
    byte CosmoDrainTicks,
    byte HitPoints)
{
    public PlatformCombatEntity ToCombatEntity() => new(
        State,
        X,
        Y,
        Type,
        HitPoints,
        SeventhSenseRewardBcd,
        Motion3,
        RightTerrain0A,
        LeftTerrain0B);

    public PlatformAuxiliaryLogicState WithCombatEntity(PlatformCombatEntity entity) => this with
    {
        State = entity.State,
        X = entity.X,
        Y = entity.Y,
        Motion3 = entity.Motion3,
        Type = entity.Type,
        RightTerrain0A = entity.RightTerrain0A,
        LeftTerrain0B = entity.LeftTerrain0B,
        SeventhSenseRewardBcd = entity.SeventhSenseRewardBcd,
        HitPoints = entity.HitPoints,
    };
}

public readonly record struct PlatformAuxiliarySlotState(
    PlatformAuxiliaryVisualState Visual,
    PlatformAuxiliaryLogicState Logic)
{
    public static PlatformAuxiliarySlotState Inactive =>
        new(PlatformAuxiliaryVisualState.Inactive, default);
}

public readonly record struct PlatformAuxiliarySharedState(
    byte CurrentPatternB4,
    byte LatchedPatternB5,
    byte SpawnTimerB6,
    sbyte ChaseDxA5,
    sbyte ChaseDyA6);

public readonly record struct PlatformAuxiliarySpawnStats(
    byte SeventhSenseRewardBcd,
    byte LifeDrainTicks,
    byte CosmoDrainTicks,
    byte HitPoints);

public enum PlatformAuxiliarySpawnOutcome
{
    NotEnabled,
    PatternMismatch,
    Occupied,
    TimerDecremented,
    SpawnAbortedVertical,
    Spawned,
    SkippedSecondSlotForPattern3,
}

public readonly record struct PlatformAuxiliarySlotSpawnResult(
    PlatformAuxiliarySlotState Slot,
    PlatformAuxiliarySharedState Shared,
    PlatformAuxiliarySpawnOutcome Outcome);

public readonly record struct PlatformAuxiliarySpawnerResult(
    PlatformAuxiliarySlotState SlotA,
    PlatformAuxiliarySlotState SlotB,
    PlatformAuxiliarySharedState Shared,
    PlatformAuxiliarySpawnOutcome SlotAOutcome,
    PlatformAuxiliarySpawnOutcome SlotBOutcome);

public static class PlatformAuxiliarySpawner
{
    /// <summary>
    /// Exact fixed-bank table at $C169 indexed by $03B5. The active platform
    /// map path feeds only the low three bits, so indices $00-$07 are promoted.
    /// </summary>
    public static byte VisualTypeForPattern(byte pattern) => pattern switch
    {
        0x00 => 0xFE,
        0x01 => 0x80,
        0x02 => 0x83,
        0x03 => 0xF4,
        0x04 => 0xDA,
        0x05 => 0x81,
        0x06 => 0x82,
        0x07 => 0x80,
        _ => throw new ArgumentOutOfRangeException(nameof(pattern), pattern, "Auxiliary pattern is the low three-bit platform pattern $00-$07."),
    };

    /// <summary>
    /// Semantic reduction of bank-3 $96B4. Slot A ($07B0/$03DA) is attempted
    /// first; pattern $03 deliberately suppresses the slot-B attempt, otherwise
    /// slot B ($07B8/$03EA) runs against the shared timer/state left by A.
    /// </summary>
    public static PlatformAuxiliarySpawnerResult Step(
        PlatformAuxiliarySlotState slotA,
        PlatformAuxiliarySlotState slotB,
        PlatformAuxiliarySharedState shared,
        PlatformAuxiliarySpawnStats stats,
        byte playerY40,
        byte entropy48)
    {
        if (shared.CurrentPatternB4 == 0)
        {
            return new(slotA, slotB, shared,
                PlatformAuxiliarySpawnOutcome.NotEnabled,
                PlatformAuxiliarySpawnOutcome.NotEnabled);
        }

        if (shared.CurrentPatternB4 != shared.LatchedPatternB5)
        {
            return new(slotA, slotB, shared,
                PlatformAuxiliarySpawnOutcome.PatternMismatch,
                PlatformAuxiliarySpawnOutcome.PatternMismatch);
        }

        var first = TrySlot(slotA, shared, stats, playerY40, entropy48);
        slotA = first.Slot;
        shared = first.Shared;

        if (shared.LatchedPatternB5 == 0x03)
        {
            return new(slotA, slotB, shared,
                first.Outcome,
                PlatformAuxiliarySpawnOutcome.SkippedSecondSlotForPattern3);
        }

        var second = TrySlot(slotB, shared, stats, playerY40, entropy48);
        return new(slotA, second.Slot, second.Shared, first.Outcome, second.Outcome);
    }

    private static PlatformAuxiliarySlotSpawnResult TrySlot(
        PlatformAuxiliarySlotState slot,
        PlatformAuxiliarySharedState shared,
        PlatformAuxiliarySpawnStats stats,
        byte playerY40,
        byte entropy48)
    {
        // $96D0 CMP #$FE -> non-inactive slots return before touching $03B6.
        if (!slot.Visual.IsInactive)
            return new(slot, shared, PlatformAuxiliarySpawnOutcome.Occupied);

        if (shared.SpawnTimerB6 != 0)
        {
            shared = shared with { SpawnTimerB6 = unchecked((byte)(shared.SpawnTimerB6 - 1)) };
            return new(slot, shared, PlatformAuxiliarySpawnOutcome.TimerDecremented);
        }

        // The successful CMP #$FE leaves carry set. LDA/AND preserve carry, so
        // ADC #$1F at $96E4 actually computes (entropy&$1F)+$20: 32..63.
        shared = shared with
        {
            SpawnTimerB6 = (byte)((entropy48 & 0x1F) + 0x20),
            ChaseDyA6 = 0,
        };

        var fromLeft = shared.CurrentPatternB4 != 0x03 && (entropy48 & 0x08) == 0;
        var visual = slot.Visual;
        if (fromLeft)
        {
            shared = shared with { ChaseDxA5 = 1 };
            visual = visual with { X = 0x02, Flags2 = 0x42, Flags6 = 0x42 };
        }
        else
        {
            shared = shared with { ChaseDxA5 = -1 };
            visual = visual with { X = 0xFF, Flags2 = 0x02, Flags6 = 0x02 };
        }

        var yBase = playerY40 == 0 || playerY40 >= 0x87 ? (byte)0x50 : playerY40;
        var y = unchecked((byte)(yBase - (entropy48 & 0x3F)));
        if (y >= 0xA0)
        {
            // The original has already reseeded timer/directions and written the
            // side/facing bytes, but it leaves type $FE so the slot stays free.
            return new(slot with { Visual = visual }, shared, PlatformAuxiliarySpawnOutcome.SpawnAbortedVertical);
        }

        visual = visual with
        {
            Y = y,
            VisualType = VisualTypeForPattern(shared.LatchedPatternB5),
        };

        var logic = slot.Logic with
        {
            Type = (byte)(shared.LatchedPatternB5 & 0x0F),
            SeventhSenseRewardBcd = stats.SeventhSenseRewardBcd,
            LifeDrainTicks = stats.LifeDrainTicks,
            CosmoDrainTicks = stats.CosmoDrainTicks,
            HitPoints = stats.HitPoints,
        };

        return new(
            new PlatformAuxiliarySlotState(visual, logic),
            shared,
            PlatformAuxiliarySpawnOutcome.Spawned);
    }
}

public enum PlatformAuxiliaryMotionOutcome
{
    Inactive,
    Active,
    DeactivatedVertical,
    DeactivatedHorizontal,
}

public readonly record struct PlatformAuxiliaryMotionResult(
    PlatformAuxiliarySlotState Slot,
    PlatformAuxiliarySharedState Shared,
    PlatformAuxiliaryMotionOutcome Outcome,
    bool ReadyForInteraction,
    bool AnimationAdvanced,
    bool PositionAdvanced);

/// <summary>
/// Exact movement/animation portion of bank-3 $9761/$976A for one auxiliary
/// slot. Collision/contact is deliberately a later phase because $9887/$9896
/// have their own ordering and shared player-attack/contact side effects.
/// </summary>
public static class PlatformAuxiliaryMotion
{
    public static PlatformAuxiliaryMotionResult Step(
        PlatformAuxiliarySlotState slot,
        PlatformAuxiliarySharedState shared,
        byte playerX3F,
        byte playerY40,
        byte frameCounter3C,
        byte cameraDelta43)
    {
        if (slot.Visual.IsInactive)
            return new(slot, shared, PlatformAuxiliaryMotionOutcome.Inactive, false, false, false);

        var visual = slot.Visual;
        var animationAdvanced = false;
        var positionAdvanced = false;

        if (visual.VisualType is 0xF4 or 0xF5)
        {
            if ((frameCounter3C & 0x03) == 0)
            {
                var nextType = visual.VisualType == 0xF4 ? (byte)0xF5 : (byte)0xF4;
                visual = visual with
                {
                    VisualType = nextType,
                    Aux5 = unchecked((byte)(nextType - 2)),
                };
                animationAdvanced = true;
            }

            if (visual.X == playerX3F)
                shared = shared with { ChaseDxA5 = 0 };
            if (visual.Y == playerY40)
                shared = shared with { ChaseDyA6 = 0 };

            if ((frameCounter3C & 0x1F) == 0)
            {
                shared = shared with
                {
                    ChaseDxA5 = DirectionToward(visual.X, playerX3F),
                    ChaseDyA6 = DirectionToward(visual.Y, playerY40),
                };
            }

            // BIT #$01 against $3C: movement occurs only when bit0 is clear.
            if ((frameCounter3C & 0x01) == 0)
            {
                var y = AddSigned(visual.Y, shared.ChaseDyA6);
                var aux4 = unchecked((byte)(y - 8));
                visual = visual with { Y = y, Aux4 = aux4 };
                positionAdvanced = true;

                if (aux4 >= 0xA0)
                {
                    return Deactivate(slot with { Visual = visual }, shared,
                        PlatformAuxiliaryMotionOutcome.DeactivatedVertical,
                        animationAdvanced,
                        positionAdvanced);
                }

                var x = AddSigned(visual.X, shared.ChaseDxA5);
                visual = visual with { X = x, XMirror7 = x };
            }
        }
        else
        {
            if ((frameCounter3C & 0x03) == 0)
            {
                visual = visual with { VisualType = NextVisualType(visual.VisualType) };
                animationAdvanced = true;
            }

            var dx = (visual.Flags2 & 0x40) != 0 ? 2 : -2;
            visual = visual with { X = unchecked((byte)(visual.X + dx)) };
            positionAdvanced = true;
        }

        var screenX = unchecked((byte)(visual.X - cameraDelta43));
        visual = visual with { X = screenX, XMirror7 = screenX };
        slot = slot with { Visual = visual };

        if ((screenX & 0xFE) < 0x02)
        {
            return Deactivate(slot, shared,
                PlatformAuxiliaryMotionOutcome.DeactivatedHorizontal,
                animationAdvanced,
                positionAdvanced);
        }

        // $989C copies current object coordinates into the logical collision
        // record before both contact and projectile-hit paths.
        slot = slot with
        {
            Logic = slot.Logic with
            {
                X = screenX,
                Y = visual.Y,
            },
        };

        return new(slot, shared, PlatformAuxiliaryMotionOutcome.Active, true, animationAdvanced, positionAdvanced);
    }

    private static PlatformAuxiliaryMotionResult Deactivate(
        PlatformAuxiliarySlotState slot,
        PlatformAuxiliarySharedState shared,
        PlatformAuxiliaryMotionOutcome outcome,
        bool animationAdvanced,
        bool positionAdvanced)
    {
        var visual = slot.Visual with
        {
            Y = 0xF0,
            VisualType = 0xFE,
            Aux4 = 0xFE,
            Aux5 = 0xFE,
        };
        return new(slot with { Visual = visual }, shared, outcome, false, animationAdvanced, positionAdvanced);
    }

    private static byte NextVisualType(byte type) => type switch
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
        _ => throw new InvalidOperationException($"Auxiliary visual type ${type:X2} is outside the closed $976A animation families."),
    };

    private static sbyte DirectionToward(byte current, byte target) =>
        current == target ? (sbyte)0 : current < target ? (sbyte)1 : (sbyte)-1;

    private static byte AddSigned(byte value, sbyte delta) =>
        unchecked((byte)(value + delta));
}

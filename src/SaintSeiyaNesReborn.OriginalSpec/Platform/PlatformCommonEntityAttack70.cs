namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformCommonEntityAttack70PreparationOutcome
{
    ReadyForInteraction,
    RemovedHorizontal,
    RemovedVerticalBand,
}

public readonly record struct PlatformCommonEntityAttack70PreparationResult(
    PlatformCommonEntityMotionState State,
    PlatformEntityDecisionResult Decision,
    PlatformEntityJumpStepResult? JumpStep,
    PlatformCommonEntityAttack70PreparationOutcome Outcome,
    bool ProximityFallStarted,
    int HorizontalDeltaBeforeCamera,
    byte CameraDelta43);

public readonly record struct PlatformSecondarySpawnTemplate(
    byte ObjectType,
    sbyte VerticalOffset)
{
    /// <summary>
    /// Functional reduction of fixed-bank tables $C0E3 and $C0EF consumed by
    /// bank-3 $A908. A zero object type means the original spawn helper returns
    /// without creating a secondary object.
    /// </summary>
    public static PlatformSecondarySpawnTemplate ForEntityType(byte entityType) => entityType switch
    {
        0x05 => new(0xB2, 5),
        0x06 => new(0xB0, 2),
        0x07 => new(0x00, 0),
        0x08 => new(0xD2, 4),
        0x09 => new(0xA1, -2),
        0x0A => new(0x00, 0),
        0x0B => new(0x00, 0),
        0x0C => new(0x8F, 5),
        0x0D => new(0x00, 0),
        0x0E => new(0x00, 0),
        0x0F => new(0x00, 0),
        _ => new(0x00, 0),
    };

    public bool CanCreateObject => ObjectType != 0;
}

public readonly record struct PlatformCommonEntityAttack70PostResult(
    PlatformCommonEntityMotionState State,
    bool Advanced,
    bool CallsSecondarySpawnRoutine,
    PlatformSecondarySpawnTemplate SpawnTemplate,
    byte? SoundId,
    bool CompletedFamily);

/// <summary>
/// Exact semantic split of the entity $70 attack/activation family.
///
/// The original does not treat $70 as one atomic routine. Before ordinary
/// projectile/contact interaction, $A55E-$A700 still runs decision logic,
/// optional jump conversion, camera-relative movement/removal and the proximity
/// fall gate. Only later, after $9915/$98BA, $A886-$A8DB advances the $70 state
/// on odd $3C frames and may call $A908 to create a secondary object.
///
/// Keeping those phases separate is required because an interaction can mutate
/// the entity to $40/$D0; in that case the later $70 progression must not run.
/// </summary>
public static class PlatformCommonEntityAttack70
{
    public const byte MidpointSpawnSoundId = 0x2A;
    public const byte TerminalSpawnSoundId = 0x2D;

    public static PlatformCommonEntityAttack70PreparationResult PrepareCommon(
        PlatformCommonEntityMotionState state,
        byte playerX,
        byte playerY,
        byte playerJumpPhase49,
        byte entropy48,
        byte frameCounter3C,
        byte cameraDelta43)
    {
        if (state.Type >= 0x08)
            throw new ArgumentOutOfRangeException(nameof(state), state.Type, "Common $70 preparation covers types $00-$07.");
        if ((state.ActionState & 0xF0) != 0x70)
            throw new InvalidOperationException($"Attack70 preparation requires entry family $70, got ${state.ActionState:X2}.");

        // $A578 -> $A970 still executes while the entity is in $70. For common
        // types an expired timer may therefore turn the entity or even replace
        // $70 with a $31/$32 jump before the rest of this update.
        var decision = PlatformCommonEntityDecision.Step(
            state,
            playerX,
            playerY,
            playerJumpPhase49,
            entropy48);
        state = decision.State;

        PlatformEntityJumpStepResult? jump = null;
        if ((state.ActionState & 0xF0) == 0x30)
        {
            jump = PlatformCommonEntityMotion.StepJumpVertical(state);
            state = jump.Value.State;
        }

        var horizontalDelta = 0;
        if ((state.ActionState & 0xF0) == 0x30)
        {
            horizontalDelta = PlatformCommonEntityMotion.JumpHorizontalDelta(
                state.ActionState,
                state.Type,
                frameCounter3C);
        }
        // Family $70 itself deliberately has no ordinary facing walk at
        // $A5D9-$A5F3: screen X is carried unchanged into the shared $43 camera
        // subtraction below.

        state = state with
        {
            X = unchecked((byte)(state.X + horizontalDelta - cameraDelta43)),
        };

        if (state.X >= 0xF8)
        {
            return new(
                state,
                decision,
                jump,
                PlatformCommonEntityAttack70PreparationOutcome.RemovedHorizontal,
                ProximityFallStarted: false,
                horizontalDelta,
                cameraDelta43);
        }

        if (state.Y is >= 0xB0 and < 0xC0)
        {
            return new(
                state,
                decision,
                jump,
                PlatformCommonEntityAttack70PreparationOutcome.RemovedVerticalBand,
                ProximityFallStarted: false,
                horizontalDelta,
                cameraDelta43);
        }

        var proximityFall = false;
        if (state.StatePhase == 0
            && state.GroundDescriptor is not (>= 0xE0 and < 0xF0)
            && (state.ActionState & 0xF0) is not (0x40 or 0xD0)
            && playerY < 0x81)
        {
            var playerRow = (byte)(playerY & 0xF0);
            if (playerRow > state.Y
                && state.X is >= 0x21 and < 0xC0
                && WithinOriginalHorizontalProximity(state.X, playerX))
            {
                state = state with
                {
                    ActionState = 0x50,
                    Y = unchecked((byte)(state.Y + 6)),
                };
                proximityFall = true;
            }
        }

        return new(
            state,
            decision,
            jump,
            PlatformCommonEntityAttack70PreparationOutcome.ReadyForInteraction,
            proximityFall,
            horizontalDelta,
            cameraDelta43);
    }

    /// <summary>
    /// Late $A886-$A8DB portion. This must be called after entity interaction,
    /// with the entity state as mutated by $9915. If that interaction replaced
    /// $70 with another family, this method is a no-op.
    /// </summary>
    public static PlatformCommonEntityAttack70PostResult AdvanceAfterInteraction(
        PlatformCommonEntityMotionState state,
        byte frameCounter3C)
    {
        if ((state.ActionState & 0xF0) != 0x70)
            return NoAdvance(state);

        // $A893-$A897: only odd $3C frames advance the family.
        if ((frameCounter3C & 1) == 0)
            return NoAdvance(state);

        var next = unchecked((byte)(state.ActionState + 1));
        var spawn = false;
        byte? sound = null;
        var completed = false;

        if (next >= 0x80)
        {
            completed = true;
            if (state.Type is 0x08 or 0x09 or 0x0C)
            {
                // $A89E-$A8BB: these special types call A908 at terminal time,
                // play $2D, and return to action $00.
                spawn = true;
                sound = TerminalSpawnSoundId;
                next = 0x00;
            }
            else
            {
                next = 0x10;
            }
        }

        state = state with { ActionState = next };

        // $A8C4-$A8D9: reaching $78 calls A908 for every type except $08/$0C
        // and plays sound $2A. Type $09 is intentionally not excluded.
        if (!completed && next == 0x78 && state.Type is not (0x08 or 0x0C))
        {
            spawn = true;
            sound = MidpointSpawnSoundId;
        }

        var template = spawn
            ? PlatformSecondarySpawnTemplate.ForEntityType(state.Type)
            : default;

        return new(
            state,
            Advanced: true,
            CallsSecondarySpawnRoutine: spawn,
            SpawnTemplate: template,
            SoundId: sound,
            CompletedFamily: completed);
    }

    private static PlatformCommonEntityAttack70PostResult NoAdvance(PlatformCommonEntityMotionState state) =>
        new(state, false, false, default, null, false);

    private static bool WithinOriginalHorizontalProximity(byte entityX, byte playerX)
    {
        if (entityX >= playerX)
        {
            var left = unchecked((byte)(entityX - 0x20));
            return left < playerX;
        }

        if (playerX < 0x20)
            return false;
        var right = unchecked((byte)(entityX + 0x20));
        return right >= playerX;
    }
}

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformSpecialEntityControlState(
    PlatformCommonEntityRuntimeState Entity,
    byte Control04);

public enum PlatformSpecialEntityPreDispatchOutcome
{
    BypassedControl04,
    BypassedSpecialActionFamily,
    CounterAdvanced,
    Attack70Started,
    SecondarySpawnTriggered,
}

public readonly record struct PlatformSpecialEntityPreDispatchResult(
    PlatformSpecialEntityControlState State,
    PlatformSpecialEntityPreDispatchOutcome Outcome,
    byte GlobalCounter039A,
    bool FacingChanged,
    bool CallsSecondarySpawnRoutine,
    PlatformSecondarySpawnTemplate SpawnTemplate,
    byte? SoundId)
{
    public bool EnteredAttack70 => Outcome == PlatformSpecialEntityPreDispatchOutcome.Attack70Started;
}

/// <summary>
/// Exact type-$08/$09/$0C pre-dispatch shared path through bank-3
/// $A478-$A55E, before the active-family dispatcher at $A55E.
///
/// Scheduled-special spawns begin with action $00 and +$04=0. These types first
/// pass through this control layer: unless +$04 or a special cleanup family
/// bypasses it, they face the player and advance global counter $039A. At the
/// threshold they either enter $70 (when +$03 is zero) or set +$04=1 and call
/// $A908 immediately (when +$03 is nonzero).
/// </summary>
public static class PlatformSpecialEntityPreDispatch08090C
{
    public const byte TriggerThreshold = 0x80;
    public const byte ImmediateSpawnSoundId = 0x29;

    public static PlatformSpecialEntityPreDispatchResult Step(
        PlatformSpecialEntityControlState state,
        byte playerX,
        byte globalCounter039A,
        byte entropy48)
    {
        var type = state.Entity.Motion.Type;
        if (type is not (0x08 or 0x09 or 0x0C))
            throw new ArgumentOutOfRangeException(nameof(state), type, "This pre-dispatch covers only types $08/$09/$0C.");

        if (state.Control04 != 0)
        {
            return Result(
                state,
                PlatformSpecialEntityPreDispatchOutcome.BypassedControl04,
                globalCounter039A,
                facingChanged: false);
        }

        var family = state.Entity.Motion.ActionState & 0xF0;
        if (family is 0x40 or 0xD0 or 0xE0)
        {
            return Result(
                state,
                PlatformSpecialEntityPreDispatchOutcome.BypassedSpecialActionFamily,
                globalCounter039A,
                facingChanged: false);
        }

        var motion = state.Entity.Motion;
        var flags = motion.FlagsFacing;
        var shouldFaceRight = motion.X < playerX;
        var currentlyFacesRight = (flags & PlatformCommonEntityMotion.FacingRightBit) != 0;
        var facingChanged = shouldFaceRight != currentlyFacesRight;
        if (facingChanged)
            flags ^= PlatformCommonEntityMotion.FacingRightBit;

        motion = motion with { FlagsFacing = flags };
        state = state with { Entity = state.Entity with { Motion = motion } };

        var nextCounter = unchecked((byte)(globalCounter039A + 1));
        if (nextCounter < TriggerThreshold)
        {
            return Result(
                state,
                PlatformSpecialEntityPreDispatchOutcome.CounterAdvanced,
                nextCounter,
                facingChanged);
        }

        var resetCounter = (byte)(entropy48 & 0x3F);
        if (motion.StatePhase == 0)
        {
            motion = motion with { ActionState = 0x70 };
            state = state with { Entity = state.Entity with { Motion = motion } };
            return Result(
                state,
                PlatformSpecialEntityPreDispatchOutcome.Attack70Started,
                resetCounter,
                facingChanged);
        }

        state = state with { Control04 = 1 };
        var template = PlatformSecondarySpawnTemplate.ForEntityType(type);
        return new(
            state,
            PlatformSpecialEntityPreDispatchOutcome.SecondarySpawnTriggered,
            resetCounter,
            facingChanged,
            CallsSecondarySpawnRoutine: true,
            SpawnTemplate: template,
            SoundId: ImmediateSpawnSoundId);
    }

    private static PlatformSpecialEntityPreDispatchResult Result(
        PlatformSpecialEntityControlState state,
        PlatformSpecialEntityPreDispatchOutcome outcome,
        byte counter,
        bool facingChanged) =>
        new(
            state,
            outcome,
            counter,
            facingChanged,
            CallsSecondarySpawnRoutine: false,
            SpawnTemplate: default,
            SoundId: null);
}

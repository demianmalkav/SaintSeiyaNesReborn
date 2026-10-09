namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformPrimaryEncounterLatchState(
    byte ActiveEngine58,
    PlatformPrimaryEncounterSpawnConfig? ActiveConfig)
{
    public static PlatformPrimaryEncounterLatchState Empty => new(0, null);
}

public enum PlatformPrimaryEncounterAcceptanceOutcome
{
    Unchanged,
    DeferredUnsafe,
    AcceptedZero,
    AcceptedNonzero,
}

public readonly record struct PlatformPrimaryEncounterAcceptanceResult(
    PlatformPrimaryEncounterAcceptanceOutcome Outcome,
    byte StagedDescriptor03B7,
    PlatformPrimaryEncounterLatchState State,
    bool ContinueToSecondarySchedule9BB9,
    bool RecomputedProfile)
{
    public bool Accepted => Outcome is
        PlatformPrimaryEncounterAcceptanceOutcome.AcceptedZero or
        PlatformPrimaryEncounterAcceptanceOutcome.AcceptedNonzero;
}

/// <summary>
/// Clean-room semantic reduction of bank-1 $996C-$9A2D.
///
/// The current page descriptor is staged in $03B7 and compared with active $58.
/// A changed descriptor is accepted only when both common slots are visually
/// free and neither logical record is in family $40 or $D0. Otherwise the old
/// $58/profile remains active for this frame and control jumps to $9BB9.
///
/// A safely accepted nonzero descriptor rebuilds the tier/profile and returns
/// directly at $9A2D. A safely accepted zero descriptor clears $58 but follows
/// the $9BB9 continuation without rebuilding profile bytes.
/// </summary>
public static class PlatformPrimaryEncounterAcceptance
{
    public const byte FreeVisualSprite = 0xFE;

    public static PlatformPrimaryEncounterAcceptanceResult Step(
        PlatformPrimaryEncounter stagedEncounter,
        PlatformPrimaryEncounterLatchState current,
        byte visualSpriteA,
        byte actionA,
        byte visualSpriteB,
        byte actionB)
    {
        ValidateState(current);

        var staged = stagedEncounter.Raw;
        if (staged == current.ActiveEngine58)
        {
            return new(
                PlatformPrimaryEncounterAcceptanceOutcome.Unchanged,
                staged,
                current,
                ContinueToSecondarySchedule9BB9: true,
                RecomputedProfile: false);
        }

        if (!SlotSafe(visualSpriteA, actionA) || !SlotSafe(visualSpriteB, actionB))
        {
            return new(
                PlatformPrimaryEncounterAcceptanceOutcome.DeferredUnsafe,
                staged,
                current,
                ContinueToSecondarySchedule9BB9: true,
                RecomputedProfile: false);
        }

        if (staged == 0)
        {
            var cleared = PlatformPrimaryEncounterLatchState.Empty;
            return new(
                PlatformPrimaryEncounterAcceptanceOutcome.AcceptedZero,
                staged,
                cleared,
                ContinueToSecondarySchedule9BB9: true,
                RecomputedProfile: false);
        }

        var config = PlatformPrimaryEncounterSpawnConfig.FromEncounter(stagedEncounter);
        var accepted = new PlatformPrimaryEncounterLatchState(staged, config);
        return new(
            PlatformPrimaryEncounterAcceptanceOutcome.AcceptedNonzero,
            staged,
            accepted,
            ContinueToSecondarySchedule9BB9: false,
            RecomputedProfile: true);
    }

    public static bool SlotSafe(byte visualSprite, byte actionState)
    {
        if (visualSprite != FreeVisualSprite)
            return false;

        var family = actionState & 0xF0;
        return family is not (0x40 or 0xD0);
    }

    private static void ValidateState(PlatformPrimaryEncounterLatchState state)
    {
        if (state.ActiveConfig is PlatformPrimaryEncounterSpawnConfig config
            && config.Engine58 != state.ActiveEngine58)
        {
            throw new InvalidOperationException(
                $"Active encounter config ${config.Engine58:X2} does not match active $58 ${state.ActiveEngine58:X2}.");
        }

        if (state.ActiveEngine58 == 0 && state.ActiveConfig is not null)
        {
            throw new InvalidOperationException(
                "Active $58 is zero but a nonzero primary encounter config is still attached.");
        }
    }
}

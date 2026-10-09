using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class PrimaryEncounterAcceptanceChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckUnchangedDescriptorContinuesSecondarySchedule();
        CheckOccupiedVisualSlotDefersChange();
        CheckReactionOrDeathFamilyDefersChange();
        CheckSafeNonzeroAcceptsAndRecomputesProfile();
        CheckSafeZeroClearsActiveEncounterWithoutProfileRefresh();
    }

    private static void CheckUnchangedDescriptorContinuesSecondarySchedule()
    {
        var active = Active(Encounter(0x86, 15, 1, 1, 4));
        var result = PlatformPrimaryEncounterAcceptance.Step(
            Encounter(0x86, 15, 1, 1, 4),
            active,
            visualSpriteA: 0x80,
            actionA: 0xD0,
            visualSpriteB: 0x80,
            actionB: 0x40);

        Require(result.Outcome == PlatformPrimaryEncounterAcceptanceOutcome.Unchanged,
            "equal staged descriptor bypasses safety checks");
        Require(result.State.Equals(active), "unchanged descriptor preserves active config");
        Require(result.ContinueToSecondarySchedule9BB9,
            "equal descriptor follows JMP $9A2E -> $9BB9");
    }

    private static void CheckOccupiedVisualSlotDefersChange()
    {
        var active = Active(Encounter(0x85, 10, 1, 1, 2));
        var result = PlatformPrimaryEncounterAcceptance.Step(
            Encounter(0x96, 30, 2, 2, 6),
            active,
            visualSpriteA: 0x80,
            actionA: 0x10,
            visualSpriteB: 0xFE,
            actionB: 0x10);

        Require(result.Outcome == PlatformPrimaryEncounterAcceptanceOutcome.DeferredUnsafe,
            "occupied slot A defers changed descriptor");
        Require(result.State.Equals(active), "deferred change preserves old $58/profile");
        Require(result.StagedDescriptor03B7 == 0x96,
            "$03B7 still reflects current page descriptor while $58 stays old");
        Require(result.ContinueToSecondarySchedule9BB9,
            "deferred descriptor follows $9BB9 continuation");
    }

    private static void CheckReactionOrDeathFamilyDefersChange()
    {
        var active = Active(Encounter(0x85, 10, 1, 1, 2));

        var reaction = PlatformPrimaryEncounterAcceptance.Step(
            Encounter(0x96, 30, 2, 2, 6),
            active,
            0xFE,
            0x40,
            0xFE,
            0x10);
        Require(reaction.Outcome == PlatformPrimaryEncounterAcceptanceOutcome.DeferredUnsafe,
            "family $40 blocks descriptor replacement even with free visual slot");

        var death = PlatformPrimaryEncounterAcceptance.Step(
            Encounter(0x96, 30, 2, 2, 6),
            active,
            0xFE,
            0x10,
            0xFE,
            0xD5);
        Require(death.Outcome == PlatformPrimaryEncounterAcceptanceOutcome.DeferredUnsafe,
            "family $D0 blocks descriptor replacement even with free visual slot");
    }

    private static void CheckSafeNonzeroAcceptsAndRecomputesProfile()
    {
        var result = PlatformPrimaryEncounterAcceptance.Step(
            Encounter(0xB6, 120, 8, 4, 13),
            PlatformPrimaryEncounterLatchState.Empty,
            0xFE,
            0x10,
            0xFE,
            0x50);

        Require(result.Outcome == PlatformPrimaryEncounterAcceptanceOutcome.AcceptedNonzero,
            "free safe slots accept changed nonzero descriptor");
        Require(result.State.ActiveEngine58 == 0xB6,
            "accepted descriptor becomes active $58");
        Require(result.State.ActiveConfig?.Profile.HitPoints0C == 120
            && result.State.ActiveConfig?.Profile.LifeDrainTicks0D == 4
            && result.State.ActiveConfig?.Profile.CosmoDrainTicks0E == 8
            && result.State.ActiveConfig?.Profile.SeventhSenseRewardBcd0F == 0x13,
            "acceptance rebuilds exact tier profile");
        Require(result.RecomputedProfile,
            "nonzero accept marks profile recomputation");
        Require(!result.ContinueToSecondarySchedule9BB9,
            "nonzero accepted path returns at $9A2D instead of entering $9BB9");
    }

    private static void CheckSafeZeroClearsActiveEncounterWithoutProfileRefresh()
    {
        var active = Active(Encounter(0x85, 10, 1, 1, 2));
        var zero = new PlatformPrimaryEncounter(0, 0, 0, false, false, null);

        var result = PlatformPrimaryEncounterAcceptance.Step(
            zero,
            active,
            0xFE,
            0x10,
            0xFE,
            0x00);

        Require(result.Outcome == PlatformPrimaryEncounterAcceptanceOutcome.AcceptedZero,
            "safe zero descriptor clears active encounter");
        Require(result.State.ActiveEngine58 == 0 && result.State.ActiveConfig is null,
            "zero accept clears semantic active config");
        Require(!result.RecomputedProfile,
            "zero accept does not rebuild $03AE/$03AD/$03AC/$03AF");
        Require(result.ContinueToSecondarySchedule9BB9,
            "zero accept branches back through $9986 -> $9BB9");
    }

    private static PlatformPrimaryEncounterLatchState Active(PlatformPrimaryEncounter encounter)
    {
        var config = PlatformPrimaryEncounterSpawnConfig.FromEncounter(encounter);
        return new PlatformPrimaryEncounterLatchState(encounter.Raw, config);
    }

    private static PlatformPrimaryEncounter Encounter(
        byte raw,
        int hp,
        byte cosmo,
        byte life,
        int ss) =>
        new(
            Raw: raw,
            TypeId: (byte)(raw & 0x0F),
            Tier: (byte)((raw >> 4) & 0x03),
            SecondCommonSlotEnabled: (raw & 0x80) != 0,
            Bit6Unknown: (raw & 0x40) != 0,
            Stats: new PlatformEncounterStats(hp, cosmo, life, ss));

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Primary encounter acceptance self-test failed: {label}");
    }
}

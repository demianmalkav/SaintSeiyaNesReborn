namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformNormalWarmReloadDisposition
{
    CommitStableState,
    ReenterInteractiveSelector
}

public readonly record struct PlatformNormalWarmReloadInput(
    byte Terminal0670,
    byte Progression067D,
    byte CanonicalSaint0533,
    byte ReloadField050E,
    byte StoryPhase06CE,
    byte Flags0673,
    byte Flags06CC,
    byte ProgressionCode06CD);

public readonly record struct PlatformNormalWarmReloadResult(
    PlatformNormalWarmReloadDisposition Disposition,
    byte? EngineState00,
    byte? EngineMirror01,
    byte? EngineSubstate03,
    byte Terminal0670,
    byte Progression067D,
    byte CanonicalSaint0533,
    byte ReloadField050E,
    byte StoryPhase06CE,
    byte Flags0673,
    byte Flags06CC,
    byte ProgressionCode06CD,
    byte? Selector068F);

/// <summary>
/// Semantic reduction of the normal warm-reload path after the interactive
/// $E35A/$E35D loop has been released.
///
/// Entry is restricted to terminal values proved reachable from the principal
/// platform/battle progression. The selector-wide value $04 is deliberately
/// rejected here: its only interactive writer is the $050E=$0D Talk context,
/// while the principal $067D->$050E table never maps to $0D.
///
/// The fixed-bank post-loop collapses to three stable engine destinations:
/// $00, $10 and $90. A phase-local Saga $FF with $06CE!=0 is not a stable
/// destination: it returns through $E28F/$E2DD to $E327 and seeds the selector
/// again for the next phase.
/// </summary>
public static class PlatformNormalWarmReloadDestination
{
    private static readonly byte[] ProgressionTo050E =
    {
        0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x0F, 0x06,
        0x10, 0x07, 0x08, 0x09, 0x0C, 0x0A, 0x00
    };

    private static readonly byte[] ProgressionCodeE50B =
    {
        0x00, 0x00, 0x00, 0x02, 0x02, 0x02, 0x02, 0x00,
        0x00, 0x00, 0x08, 0x0A, 0x0A, 0x0E, 0x00
    };

    private static readonly byte[] CanonicalToInternalSaint =
    {
        0x00, 0x02, 0x01, 0x03, 0x04
    };

    public static bool IsPrincipalInteractiveRelease(byte value) =>
        value is 0x01 or 0x02 or 0xDD or 0xFE or 0xFF;

    public static PlatformNormalWarmReloadResult ResolvePrincipal(
        PlatformNormalWarmReloadInput input)
    {
        ValidateInput(input);

        return input.Terminal0670 switch
        {
            0x01 => ResolveProgressionAdvance(input, forcedSaintZero: false),
            0x02 => ResolveRelease02(input),
            0xDD => ResolveReleaseDd(input),
            0xFE => ResolveProgressionAdvance(input, forcedSaintZero: true),
            0xFF => ResolveReleaseFf(input),
            _ => throw new InvalidOperationException(
                $"${input.Terminal0670:X2} is not a principal interactive warm-reload release.")
        };
    }

    private static PlatformNormalWarmReloadResult ResolveRelease02(
        PlatformNormalWarmReloadInput input)
    {
        RequireStoryPhaseZero(input);

        var stage050E = StageForProgression(input.Progression067D);
        return Commit(
            engineState: 0x00,
            terminal0670: 0x02,
            progression067D: input.Progression067D,
            canonicalSaint0533: input.CanonicalSaint0533,
            reloadField050E: stage050E,
            storyPhase06CE: 0x00,
            flags0673: input.Flags0673,
            flags06CC: input.Flags06CC,
            progressionCode06CD: input.ProgressionCode06CD,
            selector068F: null);
    }

    private static PlatformNormalWarmReloadResult ResolveReleaseDd(
        PlatformNormalWarmReloadInput input)
    {
        RequireStoryPhaseZero(input);

        // $E417 forces $0673=$3F, then $E187 stores $068F=$DD and commits A=$90.
        // No $06CD writer is crossed on this branch.
        return Commit(
            engineState: 0x90,
            terminal0670: 0xDD,
            progression067D: input.Progression067D,
            canonicalSaint0533: input.CanonicalSaint0533,
            reloadField050E: input.ReloadField050E,
            storyPhase06CE: 0x00,
            flags0673: 0x3F,
            flags06CC: input.Flags06CC,
            progressionCode06CD: input.ProgressionCode06CD,
            selector068F: 0xDD);
    }

    private static PlatformNormalWarmReloadResult ResolveReleaseFf(
        PlatformNormalWarmReloadInput input)
    {
        // $F2E4/$970A does not change $0670 in normal $04=$00 mode.
        // Nonzero $06CE exists only in the stage-$0A Saga phase family.
        if (input.StoryPhase06CE != 0)
        {
            if (input.ReloadField050E != 0x0A)
            {
                throw new InvalidOperationException(
                    "Nonzero $06CE is only reachable in the stage-$0A multi-phase Saga family.");
            }

            // Canonical $FFDE=$00 keeps $E9=0 in $EEA1. $E2DD sees $0670=$FF,
            // falls through its stage-specific checks, and reaches $E327 where
            // the interactive selector is seeded again. $06CD is not rewritten.
            return new PlatformNormalWarmReloadResult(
                Disposition: PlatformNormalWarmReloadDisposition.ReenterInteractiveSelector,
                EngineState00: null,
                EngineMirror01: null,
                EngineSubstate03: null,
                Terminal0670: 0x00,
                Progression067D: input.Progression067D,
                CanonicalSaint0533: input.CanonicalSaint0533,
                ReloadField050E: input.ReloadField050E,
                StoryPhase06CE: input.StoryPhase06CE,
                Flags0673: input.Flags0673,
                Flags06CC: input.Flags06CC,
                ProgressionCode06CD: input.ProgressionCode06CD,
                Selector068F: null);
        }

        // $E17B-$E185 is the completion mask gate. $02/$03 bypass it, but
        // a normal $FF reaches it unchanged.
        if (((input.Flags0673 | input.Flags06CC) & 0x0F) == 0x0F)
        {
            return Commit(
                engineState: 0x90,
                terminal0670: 0xFF,
                progression067D: input.Progression067D,
                canonicalSaint0533: input.CanonicalSaint0533,
                reloadField050E: input.ReloadField050E,
                storyPhase06CE: 0x00,
                flags0673: input.Flags0673,
                flags06CC: input.Flags06CC,
                progressionCode06CD: input.ProgressionCode06CD,
                selector068F: 0xDD);
        }

        var stage050E = StageForProgression(input.Progression067D);
        var canonicalSaint = input.CanonicalSaint0533;
        var updated06CC = SaintMask(canonicalSaint);

        // $E214 normalizes the special $0F reload to $0D for every character
        // except canonical index 3.
        if (stage050E == 0x0F && canonicalSaint != 0x03)
            stage050E = 0x0D;

        return Commit(
            engineState: 0x00,
            terminal0670: 0xFF,
            progression067D: input.Progression067D,
            canonicalSaint0533: canonicalSaint,
            reloadField050E: stage050E,
            storyPhase06CE: 0x00,
            flags0673: input.Flags0673,
            flags06CC: updated06CC,
            progressionCode06CD: input.ProgressionCode06CD,
            selector068F: 0x00);
    }

    private static PlatformNormalWarmReloadResult ResolveProgressionAdvance(
        PlatformNormalWarmReloadInput input,
        bool forcedSaintZero)
    {
        RequireStoryPhaseZero(input);

        var canonicalSaint = forcedSaintZero
            ? (byte)0x00
            : input.CanonicalSaint0533 == 0x04
                ? (byte)0x00
                : input.CanonicalSaint0533;

        if (input.Progression067D >= 0x0E)
        {
            throw new InvalidOperationException(
                "Progression-advance releases require $067D below the terminal mapped index $0E.");
        }

        var progression = (byte)(input.Progression067D + 1);
        byte progressionCode;
        byte flags0673;

        // $E3BB-$E3DA has a dedicated transition when the increment reaches $0C.
        // BIT $FFC0 is effectively a test of old $0673 bit 0 because $FFC0=$01.
        if (progression == 0x0C)
        {
            if ((input.Flags0673 & 0x01) != 0)
            {
                canonicalSaint = 0x02;
                progressionCode = 0x0B;
            }
            else
            {
                canonicalSaint = 0x00;
                progressionCode = 0x0E;
            }

            flags0673 = (byte)(progressionCode | 0x30);
        }
        else
        {
            progressionCode = ProgressionCodeFor(progression);
            flags0673 = (byte)(progressionCode | 0x30);
        }

        var stage050E = StageForProgression(progression);

        // $E1DD-$E1EF: after advancement into $0D/$0E the reload commits state
        // zero and rewrites $0670 to $05. Other advances commit state $10.
        var terminalTransition = progression is 0x0D or 0x0E;
        var engineState = terminalTransition ? (byte)0x00 : (byte)0x10;
        var terminal0670 = terminalTransition ? (byte)0x05 : (byte)0x01;

        return Commit(
            engineState: engineState,
            terminal0670: terminal0670,
            progression067D: progression,
            canonicalSaint0533: canonicalSaint,
            reloadField050E: stage050E,
            storyPhase06CE: 0x00,
            flags0673: flags0673,
            flags06CC: 0x00,
            progressionCode06CD: progressionCode,
            selector068F: null);
    }

    private static PlatformNormalWarmReloadResult Commit(
        byte engineState,
        byte terminal0670,
        byte progression067D,
        byte canonicalSaint0533,
        byte reloadField050E,
        byte storyPhase06CE,
        byte flags0673,
        byte flags06CC,
        byte progressionCode06CD,
        byte? selector068F)
    {
        return new PlatformNormalWarmReloadResult(
            Disposition: PlatformNormalWarmReloadDisposition.CommitStableState,
            EngineState00: engineState,
            EngineMirror01: engineState,
            EngineSubstate03: InternalSaintFor(canonicalSaint0533),
            Terminal0670: terminal0670,
            Progression067D: progression067D,
            CanonicalSaint0533: canonicalSaint0533,
            ReloadField050E: reloadField050E,
            StoryPhase06CE: storyPhase06CE,
            Flags0673: flags0673,
            Flags06CC: flags06CC,
            ProgressionCode06CD: progressionCode06CD,
            Selector068F: selector068F);
    }

    private static byte StageForProgression(byte progression067D)
    {
        if (progression067D >= ProgressionTo050E.Length)
            throw new InvalidOperationException($"No confirmed $F016 mapping for $067D=${progression067D:X2}.");

        return ProgressionTo050E[progression067D];
    }

    private static byte ProgressionCodeFor(byte progression067D)
    {
        if (progression067D >= ProgressionCodeE50B.Length)
            throw new InvalidOperationException($"No confirmed $E50B mapping for $067D=${progression067D:X2}.");

        return ProgressionCodeE50B[progression067D];
    }

    private static byte InternalSaintFor(byte canonicalSaint0533)
    {
        if (canonicalSaint0533 >= CanonicalToInternalSaint.Length)
            throw new InvalidOperationException($"$0533 must be a canonical Saint index 0-4; got ${canonicalSaint0533:X2}.");

        return CanonicalToInternalSaint[canonicalSaint0533];
    }

    private static byte SaintMask(byte canonicalSaint0533)
    {
        if (canonicalSaint0533 > 4)
            throw new InvalidOperationException($"$0533 must be a canonical Saint index 0-4; got ${canonicalSaint0533:X2}.");

        return (byte)(1 << canonicalSaint0533);
    }

    private static void RequireStoryPhaseZero(PlatformNormalWarmReloadInput input)
    {
        if (input.StoryPhase06CE != 0)
        {
            throw new InvalidOperationException(
                $"Release ${input.Terminal0670:X2} is not reachable with nonzero $06CE in the principal post-loop graph.");
        }
    }

    private static void ValidateInput(PlatformNormalWarmReloadInput input)
    {
        if (!IsPrincipalInteractiveRelease(input.Terminal0670))
        {
            throw new InvalidOperationException(
                $"${input.Terminal0670:X2} is outside the principal post-interactive release set {01,02,DD,FE,FF}.");
        }

        _ = InternalSaintFor(input.CanonicalSaint0533);
        _ = StageForProgression(input.Progression067D);
    }
}

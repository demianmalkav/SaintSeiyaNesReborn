using SaintSeiyaNesReborn.OriginalSpec;

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformResourceFailureCause
{
    None,
    Life,
    Cosmo,
    LifeAndCosmo
}

public readonly record struct PlatformResourceFailureFrameResult(
    ContactDrainState DrainCounters,
    int Life,
    int Cosmo,
    PlatformResourceFailureCause Cause,
    byte? EngineState00,
    byte? EngineMirror01,
    byte? FailureTimer4D);

public readonly record struct PlatformFailureMainFrameResult(
    byte FailureTimer4D,
    byte FailureMirror4E,
    byte Field40,
    bool ExitToReloadFf,
    byte? ReloadMode04,
    byte? EngineState00,
    byte? EngineMirror01);

public readonly record struct PlatformFailureReloadInput(
    PlatformSaintIndex Saint,
    byte Progression067D,
    byte ReloadField050E,
    byte StoryPhase06CE,
    byte Field06B8,
    byte Flags0673,
    byte Flags06CC,
    byte ProgressionCode06CD);

public readonly record struct PlatformFailureReloadResult(
    byte UpdatedFlags0673,
    byte Progression067D,
    byte CanonicalSaint0533,
    byte Mode04AfterPrelude,
    byte? SavedFailureStage067E,
    bool SwitchedToCanonicalSeiya,
    PlatformNormalWarmReloadResult Destination);

/// <summary>
/// Semantic reduction of the fatal active-platform resource path:
///
/// bank 1 $927A/$930A -> $92E3 state $60
/// fixed $C364-$C3AB failure frame
/// $C389 -> $C2BA mode $04=$FF -> $E100
/// bank 5 $970A failure prelude
/// existing normal reload destination machinery.
///
/// The model intentionally excludes PPU/audio work. It preserves only resource,
/// timer and persistent control fields that select later logical transitions.
/// </summary>
public static class PlatformResourceFailureTransition
{
    public const byte FailureState = 0x60;
    public const byte InitialFailureTimer = 0xD0;
    public const byte FailureExitThreshold = 0xE0;

    /// <summary>
    /// Runs the two ordered resource consumers exactly as bank 1 $8000 does:
    /// Life first, then Cosmo even if Life already committed state $60.
    /// Semantic resource values are ordinary integers; the original packed-BCD
    /// borrow machinery is reduced to the equivalent underflow boundaries.
    /// </summary>
    public static PlatformResourceFailureFrameResult ApplyResourceFrame(
        PlatformSaintIndex saint,
        byte engineSubstate02,
        byte frameCounter3C,
        ContactDrainState drainCounters,
        int life,
        int cosmo)
    {
        ValidateResource(life, nameof(life));
        ValidateResource(cosmo, nameof(cosmo));

        var lifeTicks = drainCounters.LifeTicks;
        var cosmoTicks = drainCounters.CosmoTicks;
        var lifeFatal = false;
        var cosmoFatal = false;

        var periodicLife = engineSubstate02 == 0x10
            && ContactDrain.IsPeriodicEnvironmentDrainFrame(saint, frameCounter3C);

        var consumeLife = periodicLife || lifeTicks != 0;
        if (consumeLife)
        {
            if (!periodicLife)
                lifeTicks--;

            if (life < 2)
            {
                life = 0;
                lifeFatal = true;
            }
            else
            {
                life -= 2;
            }
        }

        // $930A is called unconditionally after $927A by bank-1 $8000.
        if (cosmoTicks != 0)
        {
            cosmoTicks--;
            if (cosmo == 0)
            {
                cosmoFatal = true;
            }
            else
            {
                cosmo -= 1;
            }
        }

        var cause = (lifeFatal, cosmoFatal) switch
        {
            (false, false) => PlatformResourceFailureCause.None,
            (true, false) => PlatformResourceFailureCause.Life,
            (false, true) => PlatformResourceFailureCause.Cosmo,
            _ => PlatformResourceFailureCause.LifeAndCosmo
        };

        var fatal = cause != PlatformResourceFailureCause.None;
        return new PlatformResourceFailureFrameResult(
            new ContactDrainState(lifeTicks, cosmoTicks),
            life,
            cosmo,
            cause,
            fatal ? FailureState : null,
            fatal ? FailureState : null,
            fatal ? InitialFailureTimer : null);
    }

    /// <summary>
    /// Logical part of fixed $C364-$C3AB. State $60 never increments into
    /// $61-$6F: only $4D/$4E advance. On every frame where ($3C & $0F)==0,
    /// the failure timer advances by one. Reaching $E0 branches before storing
    /// the incremented value and immediately enters $C2BA with A=$FF.
    /// </summary>
    public static PlatformFailureMainFrameResult AdvanceFailureFrame(
        byte frameCounter3C,
        byte failureTimer4D,
        byte failureMirror4E,
        byte field4F,
        byte field40)
    {
        if (failureTimer4D < InitialFailureTimer || failureTimer4D >= FailureExitThreshold)
        {
            throw new ArgumentOutOfRangeException(
                nameof(failureTimer4D),
                failureTimer4D,
                "Reachable state-$60 failure timer must be in $D0-$DF.");
        }

        // $C364-$C37A: presentation-side vertical/animation field. It survives
        // this boundary but does not select the engine destination.
        if ((field4F >= 0xF0 || field4F < 0x80)
            && (frameCounter3C & 0x01) == 0
            && field40 < 0xA0)
        {
            field40++;
        }

        var next = failureTimer4D;
        if ((frameCounter3C & 0x0F) == 0)
        {
            next++;
            if (next >= FailureExitThreshold)
            {
                // $C389 loads #$FF and jumps $C2BA before $C38E stores X.
                return new PlatformFailureMainFrameResult(
                    FailureTimer4D: failureTimer4D,
                    FailureMirror4E: failureMirror4E,
                    Field40: field40,
                    ExitToReloadFf: true,
                    ReloadMode04: 0xFF,
                    EngineState00: 0x3D,
                    EngineMirror01: 0x3D);
            }
        }

        // $C38E/$C390 synchronize both fields on every non-terminal frame.
        return new PlatformFailureMainFrameResult(
            FailureTimer4D: next,
            FailureMirror4E: next,
            Field40: field40,
            ExitToReloadFf: false,
            ReloadMode04: null,
            EngineState00: FailureState,
            EngineMirror01: FailureState);
    }

    /// <summary>
    /// No reachable transition inside this family writes an engine state other
    /// than $60 before the terminal $3D reload handoff.
    /// </summary>
    public static bool IsReachableFailureFamilyState(byte state00) => state00 == FailureState;

    /// <summary>
    /// Reduces bank-5 $970A as entered specifically from reload mode $04=$FF,
    /// then composes with the already-promoted terminal-$FF destination model.
    ///
    /// The prelude marks the current canonical Saint in $0673. Stage $05 stores
    /// $067E and clears $04. Stage $0A with $06B8!=0 redirects progression to
    /// $067D=$02 and switches the active canonical Saint to Seiya. All branches
    /// return with $0670=$FF.
    /// </summary>
    public static PlatformFailureReloadResult ResolveReloadFf(
        PlatformFailureReloadInput input)
    {
        var canonicalSaint = (byte)SaintIndexMap.ToCanonical(input.Saint);
        var saintMask = (byte)(1 << canonicalSaint);
        var flags0673 = (byte)(input.Flags0673 | saintMask);
        var progression067D = input.Progression067D;
        var mode04 = (byte)0xFF;
        byte? savedFailureStage067E = null;
        var switchedToSeiya = false;

        if (input.ReloadField050E == 0x05)
        {
            // $97B8-$97DA: stage-local failure presentation records the stage,
            // then $97D6 clears mode $04 before returning.
            savedFailureStage067E = input.ReloadField050E;
            mode04 = 0x00;
        }
        else if (input.ReloadField050E == 0x0A && input.Field06B8 != 0)
        {
            // $979C-$97AE: redirect the special stage-$0A branch to progression
            // index 2 and swap the active working record to canonical Seiya.
            progression067D = 0x02;
            canonicalSaint = 0x00;
            switchedToSeiya = true;
        }

        var destination = PlatformNormalWarmReloadDestination.ResolvePrincipal(
            new PlatformNormalWarmReloadInput(
                Terminal0670: 0xFF,
                Progression067D: progression067D,
                CanonicalSaint0533: canonicalSaint,
                ReloadField050E: input.ReloadField050E,
                StoryPhase06CE: input.StoryPhase06CE,
                Flags0673: flags0673,
                Flags06CC: input.Flags06CC,
                ProgressionCode06CD: input.ProgressionCode06CD));

        return new PlatformFailureReloadResult(
            UpdatedFlags0673: flags0673,
            Progression067D: progression067D,
            CanonicalSaint0533: canonicalSaint,
            Mode04AfterPrelude: mode04,
            SavedFailureStage067E: savedFailureStage067E,
            SwitchedToCanonicalSeiya: switchedToSeiya,
            Destination: destination);
    }

    private static void ValidateResource(int value, string parameterName)
    {
        if (value is < 0 or > 999)
            throw new ArgumentOutOfRangeException(parameterName, value, "Resource must be in 0..999.");
    }
}

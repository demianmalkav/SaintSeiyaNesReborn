namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformWarmReloadMenuCase : byte
{
    Idle = 0,
    UpperLeft = 1,
    LowerLeft = 2,
    UpperRight = 3,
    LowerRight = 4,
    Initialize = 5,
}

public enum PlatformWarmReloadRoute
{
    Idle,
    Initialize,
    Stage0InterludeF238,
    CommonActionF813,
    StageNarrative9C91,
    StageNarrative9C91ThenConditionalCommon,
    Case3PresentationF261,
    Case3SharedProgressionF180,
    Case3Stage10Progression,
    PassiveCleanupF1C6,
    PassiveCase4,
}

public enum PlatformWarmReloadStageDispatcher
{
    None,
    A361,
    A381,
    NineC91,
}

public readonly record struct PlatformWarmReloadSelectorState(
    byte Horizontal0585,
    byte Vertical0586);

public readonly record struct PlatformWarmReloadDispatchResult(
    PlatformWarmReloadMenuCase MenuCase,
    PlatformWarmReloadRoute Route,
    PlatformWarmReloadStageDispatcher CommonDispatcher,
    byte? DeterministicTerminal0670);

public readonly record struct PlatformWarmReloadTerminalSource(
    ushort SourceAddress,
    ushort PhysicalWriterAddress,
    byte Terminal0670);

/// <summary>
/// Semantic reduction of the interactive warm-reload control layer around
/// fixed-bank $E327-$E35D, $F025 and bank-5 $A20B/$A275.
///
/// This model intentionally stops at the stage-specific action bodies. It closes
/// controller-to-menu selection, the six $F025 cases, the exact routing into
/// $9C91/$F813 and the static terminal source sites of the common $A361/$A381
/// dispatchers. Conditions internal to those stage bodies are a separate boundary.
/// </summary>
public static class PlatformWarmReloadInteractive
{
    public const byte PrincipalStageMax050E = 0x0B;

    public const byte InputA = 0x01;
    public const byte InputUp = 0x10;
    public const byte InputDown = 0x20;
    public const byte InputLeft = 0x40;
    public const byte InputRight = 0x80;

    public const ushort CommonTerminalWriterACAA = 0xACAA;

    public static PlatformWarmReloadSelectorState ApplyDirectionalInput(
        PlatformWarmReloadSelectorState state,
        byte inputMask,
        bool inputEnabled = true)
    {
        ValidateSelectors(state);

        if (!inputEnabled)
            return state;

        // $A20B-$A23E is priority ordered: right, left, down, up.
        if ((inputMask & InputRight) != 0)
            return state with { Horizontal0585 = 0x02 };
        if ((inputMask & InputLeft) != 0)
            return state with { Horizontal0585 = 0x00 };
        if ((inputMask & InputDown) != 0)
            return state with { Vertical0586 = 0x01 };
        if ((inputMask & InputUp) != 0)
            return state with { Vertical0586 = 0x00 };

        return state;
    }

    public static PlatformWarmReloadMenuCase ResolveConfirmedCase(
        PlatformWarmReloadSelectorState state,
        byte inputMask)
    {
        ValidateSelectors(state);

        // $A275 returns without changing $0584 unless the A-button mask at $FFC0 is set.
        if ((inputMask & InputA) == 0)
            return PlatformWarmReloadMenuCase.Idle;

        // $A28B-$A295: $0584 = $0585 + $0586 + 1.
        return (PlatformWarmReloadMenuCase)(state.Horizontal0585 + state.Vertical0586 + 1);
    }

    public static PlatformWarmReloadStageDispatcher ResolveCommonActionDispatcher(
        byte stage050E,
        byte flag067C)
    {
        ValidatePrincipalStage(stage050E);

        // $F813 routes the stage-2 / $067C==0 special through $A361.
        // All other principal-stage common actions eventually reach $F936 -> $A381.
        return stage050E == 0x02 && flag067C == 0x00
            ? PlatformWarmReloadStageDispatcher.A361
            : PlatformWarmReloadStageDispatcher.A381;
    }

    public static PlatformWarmReloadDispatchResult ResolveDispatch(
        byte stage050E,
        PlatformWarmReloadMenuCase menuCase,
        byte flag067C,
        byte flag06B8)
    {
        ValidatePrincipalStage(stage050E);

        return menuCase switch
        {
            PlatformWarmReloadMenuCase.Idle => Result(menuCase, PlatformWarmReloadRoute.Idle),
            PlatformWarmReloadMenuCase.Initialize => Result(menuCase, PlatformWarmReloadRoute.Initialize),
            PlatformWarmReloadMenuCase.UpperLeft => ResolveCase1(stage050E, flag067C),
            PlatformWarmReloadMenuCase.LowerLeft => ResolveCase2(stage050E, flag067C),
            PlatformWarmReloadMenuCase.UpperRight => ResolveCase3(stage050E, flag067C, flag06B8),
            PlatformWarmReloadMenuCase.LowerRight => stage050E == 0x00
                ? Result(menuCase, PlatformWarmReloadRoute.Stage0InterludeF238)
                : Result(menuCase, PlatformWarmReloadRoute.PassiveCase4),
            _ => throw new InvalidOperationException($"Warm reload $F025 case ${(byte)menuCase:X2} is outside the confirmed 0-5 table."),
        };
    }

    public static IReadOnlyList<PlatformWarmReloadTerminalSource> GetPotentialTerminalSources(
        PlatformWarmReloadStageDispatcher dispatcher,
        byte stage050E)
    {
        ValidatePrincipalStage(stage050E);

        return dispatcher switch
        {
            PlatformWarmReloadStageDispatcher.A361 => A361TerminalSources(stage050E),
            PlatformWarmReloadStageDispatcher.A381 => A381TerminalSources(stage050E),
            PlatformWarmReloadStageDispatcher.NineC91 => NineC91TerminalSources(stage050E),
            _ => Array.Empty<PlatformWarmReloadTerminalSource>(),
        };
    }

    private static PlatformWarmReloadDispatchResult ResolveCase1(byte stage050E, byte flag067C)
    {
        if (stage050E == 0x00)
            return Result(PlatformWarmReloadMenuCase.UpperLeft, PlatformWarmReloadRoute.Stage0InterludeF238);

        // $F063-$F077 has a dedicated stage-3 path. With $067C==0, $9C91's
        // stage-3 handler reaches $9DC5 and deterministically stores $0670=$02.
        if (stage050E == 0x03 && flag067C == 0x00)
        {
            return Result(
                PlatformWarmReloadMenuCase.UpperLeft,
                PlatformWarmReloadRoute.StageNarrative9C91,
                PlatformWarmReloadStageDispatcher.NineC91,
                deterministicTerminal0670: 0x02);
        }

        var dispatcher = ResolveCommonActionDispatcher(stage050E, flag067C);
        return Result(
            PlatformWarmReloadMenuCase.UpperLeft,
            PlatformWarmReloadRoute.CommonActionF813,
            dispatcher);
    }

    private static PlatformWarmReloadDispatchResult ResolveCase2(byte stage050E, byte flag067C)
    {
        // $F0B1 always enters $9C91 first. If that body leaves $0670 zero but
        // raises $DC, the same invocation falls through $F0A5 -> $F813.
        // Closing the exact per-stage conditions for that second leg is the next boundary.
        var deterministicTerminal = stage050E == 0x03 && flag067C == 0x00
            ? (byte?)0x02
            : null;

        return Result(
            PlatformWarmReloadMenuCase.LowerLeft,
            PlatformWarmReloadRoute.StageNarrative9C91ThenConditionalCommon,
            PlatformWarmReloadStageDispatcher.NineC91,
            deterministicTerminal);
    }

    private static PlatformWarmReloadDispatchResult ResolveCase3(
        byte stage050E,
        byte flag067C,
        byte flag06B8)
    {
        if (stage050E == 0x00)
            return Result(PlatformWarmReloadMenuCase.UpperRight, PlatformWarmReloadRoute.Stage0InterludeF238);

        if (stage050E is 0x02 or 0x03 or 0x05 or 0x09)
            return Result(PlatformWarmReloadMenuCase.UpperRight, PlatformWarmReloadRoute.Case3PresentationF261);

        if (stage050E == 0x07)
        {
            return Result(
                PlatformWarmReloadMenuCase.UpperRight,
                PlatformWarmReloadRoute.CommonActionF813,
                ResolveCommonActionDispatcher(stage050E, flag067C));
        }

        if (stage050E == 0x08)
        {
            if (flag06B8 != 0x00)
                return Result(PlatformWarmReloadMenuCase.UpperRight, PlatformWarmReloadRoute.PassiveCleanupF1C6);

            return Result(
                PlatformWarmReloadMenuCase.UpperRight,
                PlatformWarmReloadRoute.CommonActionF813,
                ResolveCommonActionDispatcher(stage050E, flag067C));
        }

        if (stage050E == 0x0A)
            return Result(PlatformWarmReloadMenuCase.UpperRight, PlatformWarmReloadRoute.Case3Stage10Progression);

        // Principal stages 1, 4, 6 and 11 reach the shared $F180 flag/update path.
        return Result(PlatformWarmReloadMenuCase.UpperRight, PlatformWarmReloadRoute.Case3SharedProgressionF180);
    }

    private static PlatformWarmReloadDispatchResult Result(
        PlatformWarmReloadMenuCase menuCase,
        PlatformWarmReloadRoute route,
        PlatformWarmReloadStageDispatcher dispatcher = PlatformWarmReloadStageDispatcher.None,
        byte? deterministicTerminal0670 = null) =>
        new(menuCase, route, dispatcher, deterministicTerminal0670);

    private static IReadOnlyList<PlatformWarmReloadTerminalSource> NineC91TerminalSources(byte stage050E) =>
        stage050E switch
        {
            0x00 => new[] { Direct(0x9D20, 0x01) },
            0x03 => new[] { Direct(0x9DC5, 0x02) },
            _ => Array.Empty<PlatformWarmReloadTerminalSource>(),
        };

    private static IReadOnlyList<PlatformWarmReloadTerminalSource> A361TerminalSources(byte stage050E) =>
        stage050E switch
        {
            0x01 => new[] { Common(0xA3DE, 0x01) },
            0x02 => new[] { Common(0xA46C, 0x02), Common(0xA4A1, 0x01) },
            0x03 => new[] { Common(0xA52C, 0x01) },
            0x04 => new[] { Common(0xA61E, 0x01), Common(0xA633, 0x01) },
            0x05 => new[] { Common(0xA685, 0x01), Common(0xA6EB, 0x02), Common(0xA781, 0xFE) },
            0x06 => new[] { Common(0xA837, 0x01) },
            0x07 => new[] { Common(0xA8D3, 0xFE) },
            0x08 => new[] { Common(0xA957, 0xFE), Common(0xA9CE, 0xFE) },
            0x09 => new[] { Common(0xAAE7, 0xFE) },
            0x0A => new[] { Common(0xAC00, 0x01) },
            _ => Array.Empty<PlatformWarmReloadTerminalSource>(),
        };

    private static IReadOnlyList<PlatformWarmReloadTerminalSource> A381TerminalSources(byte stage050E) =>
        stage050E switch
        {
            0x01 => new[] { Common(0xA43B, 0xFF) },
            0x02 => new[] { Common(0xA4F4, 0xFF) },
            0x03 => new[] { Common(0xA596, 0xFF) },
            0x04 => new[] { Common(0xA65C, 0xFF) },
            0x05 => new[] { Common(0xA7E0, 0xFF), Common(0xA7FA, 0xFF) },
            0x06 => new[] { Common(0xA866, 0xFF) },
            0x07 => new[] { Common(0xA8F7, 0xFF) },
            0x08 => new[] { Common(0xAA01, 0xFF), Common(0xAA51, 0xFE) },
            0x09 => new[] { Common(0xAB13, 0xFF) },
            0x0A => new[] { Common(0xAC38, 0xFF), Common(0xAC71, 0xFF) },
            _ => Array.Empty<PlatformWarmReloadTerminalSource>(),
        };

    private static PlatformWarmReloadTerminalSource Direct(ushort writer, byte code) =>
        new(writer, writer, code);

    private static PlatformWarmReloadTerminalSource Common(ushort source, byte code) =>
        new(source, CommonTerminalWriterACAA, code);

    private static void ValidateSelectors(PlatformWarmReloadSelectorState state)
    {
        if (state.Horizontal0585 is not (0x00 or 0x02))
            throw new InvalidOperationException($"Warm reload horizontal selector $0585 must be $00 or $02; got ${state.Horizontal0585:X2}.");
        if (state.Vertical0586 is not (0x00 or 0x01))
            throw new InvalidOperationException($"Warm reload vertical selector $0586 must be $00 or $01; got ${state.Vertical0586:X2}.");
    }

    private static void ValidatePrincipalStage(byte stage050E)
    {
        if (stage050E > PrincipalStageMax050E)
            throw new InvalidOperationException($"Warm reload principal-stage model accepts $050E=$00-$0B; got ${stage050E:X2}.");
    }
}

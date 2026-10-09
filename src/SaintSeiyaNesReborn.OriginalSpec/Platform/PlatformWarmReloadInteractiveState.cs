namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformWarmReloadDirection
{
    Up,
    Down,
    Left,
    Right
}

public enum PlatformWarmReloadRoute
{
    IdleCase0,
    InitializeCase5,
    CommonNonterminal,
    TalkDispatcher9C91,
    BronzeActionRoundF813,
    DirectLoopRelease
}

public readonly record struct PlatformWarmReloadSelectorState(
    byte PendingCase0584,
    byte Horizontal0585,
    byte Vertical0586,
    byte CommandDb);

public readonly record struct PlatformWarmReloadDispatchResult(
    PlatformWarmReloadSelectorState State,
    byte DispatchedCase,
    PlatformWarmReloadRoute Route,
    byte? DirectRelease0670);

/// <summary>
/// Semantic reduction of the normal warm-reload interactive loop around
/// fixed-bank $E327-$E35D, dispatcher $F025 and bank-5 input helpers
/// $A20B/$A275.
///
/// The ROM implements a 2x2 selector with two compact coordinate fields:
/// $0585 is 0 for left and 2 for right; $0586 is 0 for up and 1 for down.
/// NMI-side $A20B updates those fields only while $DB == 0. Main-side $A275
/// converts a confirmed coordinate into $0584 = $0585 + $0586 + 1, producing
/// cases 1..4. $F025 always clears $0584 before dispatching the selected body.
///
/// Cases are:
///   0 -> idle/unlock ($DB=0)
///   1 -> body $F057 ($DB=2)
///   2 -> Talk body $F0B1 ($DB=3, dispatcher $9C91)
///   3 -> body $F0D3 ($DB=4)
///   4 -> body $F041 ($DB=1)
///   5 -> initial body $F1D9; resets $0585/$0586 but preserves $DB
///
/// This model stops at the already-isolated battle/story dispatch boundaries.
/// It does not emulate drawing, audio, controller-register plumbing or the
/// complete Bronze/Gold combat round. It does preserve direct loop-release
/// writes and the complete set of $0670 values reachable from the interactive
/// subgraph after downstream stage handlers return.
/// </summary>
public static class PlatformWarmReloadInteractiveState
{
    public const byte InitialCase = 0x05;
    public const byte InitialCommandDb = 0xFF;

    public const ushort DispatcherF025 = 0xF025;
    public const ushort NmiDirectionHandlerA20B = 0xA20B;
    public const ushort MainConfirmHandlerA275 = 0xA275;
    public const ushort TalkDispatcher9C91 = 0x9C91;
    public const ushort BronzeActionRoundF813 = 0xF813;
    public const ushort DownstreamReleaseSinkACAA = 0xACAA;

    private static readonly IReadOnlyList<byte> KnownReleaseValues =
        Array.AsReadOnly(new byte[] { 0x01, 0x02, 0x04, 0xDD, 0xFE, 0xFF });

    /// <summary>
    /// Exact selector state after $E327-$E347 has cleared $0584-$0586 and
    /// seeded case 5. $E119 has already set $DB=$FF on this reload pass.
    /// </summary>
    public static PlatformWarmReloadSelectorState SeedNormalWarmReload() =>
        new(
            PendingCase0584: InitialCase,
            Horizontal0585: 0x00,
            Vertical0586: 0x00,
            CommandDb: InitialCommandDb);

    /// <summary>
    /// NMI-side $A20B. Directional changes are ignored after a command body
    /// has made $DB nonzero. Standard NES masks at $FFC4-$FFC7 map to
    /// Up/Down/Left/Right respectively.
    /// </summary>
    public static PlatformWarmReloadSelectorState ApplyDirection(
        PlatformWarmReloadSelectorState state,
        PlatformWarmReloadDirection direction)
    {
        if (state.CommandDb != 0)
            return state;

        return direction switch
        {
            PlatformWarmReloadDirection.Up => state with { Vertical0586 = 0x00 },
            PlatformWarmReloadDirection.Down => state with { Vertical0586 = 0x01 },
            PlatformWarmReloadDirection.Left => state with { Horizontal0585 = 0x00 },
            PlatformWarmReloadDirection.Right => state with { Horizontal0585 = 0x02 },
            _ => throw new ArgumentOutOfRangeException(nameof(direction))
        };
    }

    /// <summary>
    /// Main-side $A275. Without confirm, $0584 remains unchanged. On confirm,
    /// the two selector coordinates produce one of the four real command cases.
    /// </summary>
    public static PlatformWarmReloadSelectorState ApplyConfirm(
        PlatformWarmReloadSelectorState state,
        bool confirmed)
    {
        if (!confirmed)
            return state;

        ValidateCoordinates(state.Horizontal0585, state.Vertical0586);

        var selectedCase = (byte)(state.Horizontal0585 + state.Vertical0586 + 1);
        return state with { PendingCase0584 = selectedCase };
    }

    /// <summary>
    /// Executes only the state-visible semantics of $F025 and classifies the
    /// selected body. Stage-specific display work remains outside this model.
    /// </summary>
    public static PlatformWarmReloadDispatchResult DispatchF025(
        PlatformWarmReloadSelectorState state,
        byte stage050E,
        byte field067C = 0,
        byte field06B8 = 0,
        byte talkProgress066F = 0)
    {
        var selectedCase = state.PendingCase0584;
        var next = state with { PendingCase0584 = 0x00 };

        switch (selectedCase)
        {
            case 0:
                next = next with { CommandDb = 0x00 };
                return new(next, selectedCase, PlatformWarmReloadRoute.IdleCase0, null);

            case 1:
            {
                next = next with { CommandDb = 0x02 };
                var release = ResolveCase1DirectRelease(stage050E, field067C);
                if (release.HasValue)
                    return new(next, selectedCase, PlatformWarmReloadRoute.DirectLoopRelease, release);

                var route = stage050E is 0x00 or 0x0D
                    ? PlatformWarmReloadRoute.CommonNonterminal
                    : PlatformWarmReloadRoute.BronzeActionRoundF813;
                return new(next, selectedCase, route, null);
            }

            case 2:
            {
                next = next with { CommandDb = 0x03 };
                var release = ResolveTalkDirectRelease(stage050E, field067C, talkProgress066F);
                if (release.HasValue)
                    return new(next, selectedCase, PlatformWarmReloadRoute.DirectLoopRelease, release);

                return new(next, selectedCase, PlatformWarmReloadRoute.TalkDispatcher9C91, null);
            }

            case 3:
            {
                next = next with { CommandDb = 0x04 };
                var route = ResolveCase3Route(stage050E, field06B8);
                if (route == PlatformWarmReloadRoute.InitializeCase5)
                {
                    next = next with
                    {
                        Horizontal0585 = 0x00,
                        Vertical0586 = 0x00
                    };
                }

                return new(next, selectedCase, route, null);
            }

            case 4:
                next = next with { CommandDb = 0x01 };
                return new(next, selectedCase, PlatformWarmReloadRoute.CommonNonterminal, null);

            case 5:
                next = next with
                {
                    Horizontal0585 = 0x00,
                    Vertical0586 = 0x00
                };
                return new(next, selectedCase, PlatformWarmReloadRoute.InitializeCase5, null);

            default:
                throw new InvalidOperationException(
                    $"$F025 has exactly six reachable selector cases (0-5); got ${selectedCase:X2}.");
        }
    }

    /// <summary>
    /// Values proved reachable at $E35A from this interactive subgraph.
    /// 01/02 can be produced directly or by the post-action sink; 04 is the
    /// stage-$0D Talk exit; DD/FE/FF come from stage post-action scripts through
    /// the common bank-5 sink at $ACAA. Value 03 is intentionally absent.
    /// </summary>
    public static IReadOnlyList<byte> LoopReleaseValues => KnownReleaseValues;

    public static bool IsLoopReleaseValue(byte value) =>
        value is 0x01 or 0x02 or 0x04 or 0xDD or 0xFE or 0xFF;

    private static byte? ResolveCase1DirectRelease(byte stage050E, byte field067C)
    {
        // $F08B-$F0A4: stage $0C writes $0670=$01 directly.
        if (stage050E == 0x0C)
            return 0x01;

        // $F063-$F077 enters the stage-3 Talk handler only when $067C=0;
        // $9D96-$9DCA then writes $0670=$02 and unwinds the case body.
        if (stage050E == 0x03 && field067C == 0)
            return 0x02;

        return null;
    }

    private static byte? ResolveTalkDirectRelease(
        byte stage050E,
        byte field067C,
        byte talkProgress066F)
    {
        // Stage 0: first Talk phase raises $066F; the next phase reaches
        // $9D20 and releases with $0670=$01.
        if (stage050E == 0x00 && talkProgress066F != 0)
            return 0x01;

        // Stage 3: $9D96 takes the immediate transition while $067C=0.
        if (stage050E == 0x03 && field067C == 0)
            return 0x02;

        // Stage $0D maps to $A1C5, which writes $0670=$04 at $A1CC.
        if (stage050E == 0x0D)
            return 0x04;

        return null;
    }

    private static PlatformWarmReloadRoute ResolveCase3Route(byte stage050E, byte field06B8)
    {
        // $F10D: stage $0D jumps to the case-5 body ($F1D9). It resets the
        // selector coordinates but preserves the case-3 $DB=$04 latch.
        if (stage050E == 0x0D)
            return PlatformWarmReloadRoute.InitializeCase5;

        // $F10D-$F140: stages 7 and 8 can intentionally fall into $F0A5,
        // which starts the Bronze action round. Stage 8 suppresses it when
        // $06B8 is already nonzero.
        if (stage050E == 0x07)
            return PlatformWarmReloadRoute.BronzeActionRoundF813;
        if (stage050E == 0x08 && field06B8 == 0)
            return PlatformWarmReloadRoute.BronzeActionRoundF813;

        return PlatformWarmReloadRoute.CommonNonterminal;
    }

    private static void ValidateCoordinates(byte horizontal0585, byte vertical0586)
    {
        if (horizontal0585 is not (0x00 or 0x02))
        {
            throw new InvalidOperationException(
                $"$0585 selector coordinate must be $00 (left) or $02 (right); got ${horizontal0585:X2}.");
        }

        if (vertical0586 is not (0x00 or 0x01))
        {
            throw new InvalidOperationException(
                $"$0586 selector coordinate must be $00 (up) or $01 (down); got ${vertical0586:X2}.");
        }
    }
}

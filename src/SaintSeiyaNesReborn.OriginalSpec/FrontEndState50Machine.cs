namespace SaintSeiyaNesReborn.OriginalSpec;

public enum FrontEnd50Disposition
{
    Stay,
    AdvanceIntro,
    EnterReady,
    EnterSelectionCommit,
    EnterAttractHandoff,
    EnterPasswordEntry,
    EnterPasswordAccepted,
    ExitToAttractState30,
    ExitToState10,
    ExitToRestoredState10
}

public enum FrontEnd50EntryKind
{
    ColdReset,
    AttractCompletion,
    AttractStartEscape
}

public readonly record struct FrontEnd50Result(
    FrontEnd50Disposition Disposition,
    byte EngineState00,
    byte EngineMirror01,
    byte ModalStep0200,
    byte MainLane0201,
    byte Branch0202,
    bool LoadsChrExecutableOverlay = false,
    bool InitializesPasswordWorkspace = false,
    bool RestoresDecodedProgression = false);

/// <summary>
/// Semantic reduction of exact global engine state $50.
///
/// State $50 is the original front-end/title modal shell. Its finite control state
/// lives primarily in $0200, with $0201 selecting the main-thread lane and $0202
/// holding the one-frame branch choice sampled in ready state $05.
///
/// Confirmed topology:
///
/// cold/reset or completed attract -> $50:$00 -> $01 -> $02 -> $03 -> $04 -> $05
/// Start during $00-$04 -----------------------------------------------> $05
/// scripted event from $05 -> $06 -> paired engine $30 -> attract/presentation
/// Start from $05 -> $07
///   $0202=0 -> paired engine $10
///   $0202=1 -> CHR31 executable overlay -> password entry $09
/// valid password $09 -> $08 -> paired engine $10 with decoded progression restore
///
/// The $30-$4D family previously promoted as a generic scene/battle scaffold is
/// therefore specifically the front-end attract/presentation loop. Its structural
/// transition graph remains valid; only its semantic classification changes.
/// </summary>
public static class FrontEndState50Machine
{
    public const byte GlobalState50 = 0x50;
    public const byte AttractExitState30 = 0x30;
    public const byte NormalExitState10 = 0x10;

    public const byte StartNewPressMask = 0x08;
    public const byte UpNewPressMask = 0x10;
    public const byte DownNewPressMask = 0x20;

    public const byte ReadyStep = 0x05;
    public const byte AttractHandoffStep = 0x06;
    public const byte SelectionCommitStep = 0x07;
    public const byte PasswordAcceptedStep = 0x08;
    public const byte PasswordEntryStep = 0x09;

    // Bank-0 $8000/$8013 select MMC1 CHR bank 31, copy $3C0 bytes from PPU
    // pattern memory to RAM $0440-$07FF, and JMP $0440. The copied bytes are
    // executable 6502 code. This is an intentional CHR-backed RAM overlay system.
    public const byte ExecutableOverlayChrBank = 0x1F;
    public const ushort ExecutableOverlayRamStart = 0x0440;
    public const ushort ExecutableOverlayLength = 0x03C0;
    public const ushort FrontEndOverlayPpuSource = 0x1000;
    public const ushort PasswordOverlayPpuSource = 0x1400;

    private static readonly byte[] ReachableSteps = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

    public static IReadOnlyList<byte> ReachableModalSteps => ReachableSteps;

    public static bool IsReachableModalStep(byte modalStep0200) => modalStep0200 <= PasswordEntryStep;

    public static FrontEnd50Result Enter(FrontEnd50EntryKind entryKind) => entryKind switch
    {
        FrontEnd50EntryKind.ColdReset => BaseEntry(modalStep0200: 0x00),
        FrontEnd50EntryKind.AttractCompletion => BaseEntry(modalStep0200: 0x00),
        FrontEnd50EntryKind.AttractStartEscape => BaseEntry(modalStep0200: ReadyStep),
        _ => throw new ArgumentOutOfRangeException(nameof(entryKind), entryKind, null)
    };

    private static FrontEnd50Result BaseEntry(byte modalStep0200) => new(
        FrontEnd50Disposition.Stay,
        EngineState00: GlobalState50,
        EngineMirror01: GlobalState50,
        ModalStep0200: modalStep0200,
        MainLane0201: 0x00,
        Branch0202: 0x00,
        LoadsChrExecutableOverlay: true);

    /// <summary>
    /// NMI step $00. Bank-0 $8078 dispatches to $8088. Once $0204 >= 2,
    /// candidate ($0207+1) >= $F0 promotes $0200 to $01. The common $8857 tail
    /// then applies a newly-pressed Start redirect after the local transition.
    /// </summary>
    public static FrontEnd50Result StepIntro0(byte frameHigh0204, byte scroll0207, bool startNewPress)
    {
        var next = (byte)0x00;
        if (frameHigh0204 >= 0x02 && (byte)(scroll0207 + 1) >= 0xF0)
            next = 0x01;

        return ApplyCommonStartRedirect(
            next,
            branch0202: 0,
            startNewPress,
            next == 0x01 ? FrontEnd50Disposition.AdvanceIntro : FrontEnd50Disposition.Stay);
    }

    /// <summary>$01->$02 when the observed high frame counter $0204 reaches 3.</summary>
    public static FrontEnd50Result StepIntro1(byte frameHigh0204, bool startNewPress)
    {
        var next = frameHigh0204 >= 0x03 ? (byte)0x02 : (byte)0x01;
        return ApplyCommonStartRedirect(
            next,
            branch0202: 0,
            startNewPress,
            next == 0x02 ? FrontEnd50Disposition.AdvanceIntro : FrontEnd50Disposition.Stay);
    }

    /// <summary>
    /// $02->$03 when the local position/counter step has reached its proven
    /// terminal pair ($0207==0 and $0208==$FD after the bank-0 update).
    /// </summary>
    public static FrontEnd50Result StepIntro2(bool terminalPositionAfterStep, bool startNewPress)
    {
        var next = terminalPositionAfterStep ? (byte)0x03 : (byte)0x02;
        return ApplyCommonStartRedirect(
            next,
            branch0202: 0,
            startNewPress,
            terminalPositionAfterStep ? FrontEnd50Disposition.AdvanceIntro : FrontEnd50Disposition.Stay);
    }

    /// <summary>$03->$04 once low frame counter $0203 reaches $80.</summary>
    public static FrontEnd50Result StepIntro3(byte frameLow0203, bool startNewPress)
    {
        var next = frameLow0203 >= 0x80 ? (byte)0x04 : (byte)0x03;
        return ApplyCommonStartRedirect(
            next,
            branch0202: 0,
            startNewPress,
            next == 0x04 ? FrontEnd50Disposition.AdvanceIntro : FrontEnd50Disposition.Stay);
    }

    /// <summary>
    /// The first observing NMI of $04 runs $8515 and, because the entry produced
    /// $0204=0, advances locally to ready step $05. The common Start redirect runs
    /// afterwards, so a Start edge on that same NMI maps the new $05 to $07.
    /// </summary>
    public static FrontEnd50Result StepIntro4(bool startNewPress) => ApplyCommonStartRedirect(
        ReadyStep,
        branch0202: 0,
        startNewPress,
        FrontEnd50Disposition.EnterReady);

    /// <summary>
    /// Ready state $05 samples new Up/Down edges through bank-0 $881D. $0202 is
    /// rewritten every observing NMI: Up wins and selects 0; Down selects 1;
    /// no directional edge also selects 0. It is therefore not a persistent menu
    /// cursor. A scripted object event $04EF promotes local state to $06. The
    /// common Start redirect executes afterwards: ordinary $05+Start becomes $07,
    /// while simultaneous scripted $06+Start is redirected back to $05.
    /// </summary>
    public static FrontEnd50Result StepReady5(
        bool upNewPress,
        bool downNewPress,
        bool startNewPress,
        bool scriptedAttractEvent)
    {
        var branch = upNewPress ? (byte)0x00 : downNewPress ? (byte)0x01 : (byte)0x00;
        var localStep = scriptedAttractEvent ? AttractHandoffStep : ReadyStep;
        var localDisposition = scriptedAttractEvent
            ? FrontEnd50Disposition.EnterAttractHandoff
            : FrontEnd50Disposition.Stay;

        return ApplyCommonStartRedirect(localStep, branch, startNewPress, localDisposition);
    }

    /// <summary>
    /// NMI states $06/$07/$08 set $0201=1 and jump back to the main-thread lane.
    /// $06 commits paired global $30. $07 chooses between normal paired $10 and
    /// password entry according to the current $0202. $08 commits paired $10 via
    /// $C1ED so the decoded password fields staged at $0100/$0101 are restored
    /// into persistent progression before the already-promoted $10->$11 bootstrap.
    /// </summary>
    public static FrontEnd50Result CommitFromNmi(byte modalStep0200, byte branch0202 = 0)
    {
        if (branch0202 > 1)
            throw new ArgumentOutOfRangeException(nameof(branch0202), branch0202, "$0202 is reachable only as 0 or 1.");

        return modalStep0200 switch
        {
            AttractHandoffStep => new FrontEnd50Result(
                FrontEnd50Disposition.ExitToAttractState30,
                EngineState00: AttractExitState30,
                EngineMirror01: AttractExitState30,
                ModalStep0200: AttractHandoffStep,
                MainLane0201: 0x01,
                Branch0202: branch0202),

            SelectionCommitStep when branch0202 == 0 => new FrontEnd50Result(
                FrontEnd50Disposition.ExitToState10,
                EngineState00: NormalExitState10,
                EngineMirror01: NormalExitState10,
                ModalStep0200: SelectionCommitStep,
                MainLane0201: 0x01,
                Branch0202: 0x00),

            SelectionCommitStep => new FrontEnd50Result(
                FrontEnd50Disposition.EnterPasswordEntry,
                EngineState00: GlobalState50,
                EngineMirror01: GlobalState50,
                ModalStep0200: PasswordEntryStep,
                MainLane0201: 0x01,
                Branch0202: 0x01,
                LoadsChrExecutableOverlay: true,
                InitializesPasswordWorkspace: true),

            PasswordAcceptedStep => new FrontEnd50Result(
                FrontEnd50Disposition.ExitToRestoredState10,
                EngineState00: NormalExitState10,
                EngineMirror01: NormalExitState10,
                ModalStep0200: PasswordAcceptedStep,
                MainLane0201: 0x01,
                Branch0202: branch0202,
                RestoresDecodedProgression: true),

            _ => throw new ArgumentOutOfRangeException(nameof(modalStep0200), modalStep0200,
                "Commit lane is reachable only from modal states $06, $07, and $08.")
        };
    }

    /// <summary>
    /// State $09 is the password-entry UI. Bank-0 $B0AB/$B240 drives the grid and
    /// invokes the already-documented decoder $AEB5. Valid decode returns A=0 and
    /// the UI writes $0200=$08; invalid decode remains in $09.
    /// </summary>
    public static FrontEnd50Result StepPassword9(bool decoderAccepted) => new(
        decoderAccepted ? FrontEnd50Disposition.EnterPasswordAccepted : FrontEnd50Disposition.Stay,
        EngineState00: GlobalState50,
        EngineMirror01: GlobalState50,
        ModalStep0200: decoderAccepted ? PasswordAcceptedStep : PasswordEntryStep,
        MainLane0201: 0x01,
        Branch0202: 0x01,
        RestoresDecodedProgression: decoderAccepted);

    public static byte ResolveStartRedirect(byte modalStep0200)
    {
        if (!IsReachableModalStep(modalStep0200))
            throw new ArgumentOutOfRangeException(nameof(modalStep0200));

        return modalStep0200 switch
        {
            0x00 or 0x01 or 0x02 or 0x03 or 0x04 => ReadyStep,
            0x05 => SelectionCommitStep,
            0x06 => ReadyStep,
            0x07 => SelectionCommitStep,
            0x08 or 0x09 => ReadyStep,
            _ => throw new UnreachableException()
        };
    }

    private static FrontEnd50Result ApplyCommonStartRedirect(
        byte localStep,
        byte branch0202,
        bool startNewPress,
        FrontEnd50Disposition localDisposition)
    {
        if (!startNewPress)
        {
            return new FrontEnd50Result(
                localDisposition,
                GlobalState50,
                GlobalState50,
                localStep,
                MainLane0201: 0x00,
                branch0202);
        }

        var redirected = ResolveStartRedirect(localStep);
        var disposition = redirected switch
        {
            ReadyStep => FrontEnd50Disposition.EnterReady,
            SelectionCommitStep => FrontEnd50Disposition.EnterSelectionCommit,
            _ => FrontEnd50Disposition.Stay
        };

        return new FrontEnd50Result(
            disposition,
            GlobalState50,
            GlobalState50,
            redirected,
            MainLane0201: 0x00,
            branch0202);
    }
}

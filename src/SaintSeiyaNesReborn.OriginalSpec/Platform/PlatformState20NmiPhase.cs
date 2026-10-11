namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformState20NmiOperationKind
{
    MarkMapperInterrupted,
    ResetMmc1Serial,
    ReadPpuStatus,
    WriteOamAddress,
    OamDma,
    TemporaryPrgBank,
    InvokeBank1VisualRefresh9915,
    WritePpuControl,
    WritePpuAddress,
    WritePpuData,
    RebuildPlatformAudioCb6A,
    ResetAudioAndLoad63,
    WritePpuMask,
    WritePpuScroll,
    WaitSpriteZeroHitClear,
    RestorePersistentPrgBank,
    ReturnFromInterrupt,
}

public readonly record struct PlatformState20NmiOperation(
    PlatformState20NmiOperationKind Kind,
    ushort Address = 0,
    byte Value = 0);

/// <summary>
/// Persistent RAM/mirror values directly owned or consumed by the canonical
/// platform NMI branch ($00=$20).
/// </summary>
public readonly record struct PlatformState20NmiState(
    byte CameraLow44,
    byte CameraPage45,
    byte ScrollY46,
    byte PpuCtrlMirror77,
    byte PpuMaskMirror78,
    byte PersistentPrgBank3B,
    byte StreamLatch03A3,
    byte PauseDebounce05,
    byte PauseFlag0386,
    byte ControllerHeld3D);

public sealed record PlatformState20NmiResult(
    PlatformState20NmiState State,
    byte[] OamSnapshot,
    IReadOnlyList<PlatformState20NmiOperation> Operations,
    bool InvokedBank1VisualRefresh9915,
    bool PauseToggled,
    bool CanonicalDebugTailReachable);

/// <summary>
/// Clean-room semantic model of the fixed-bank NMI path:
/// $D269 prologue -> $00=$20 dispatch -> $D7F2 -> $D988 -> $D367 epilogue.
///
/// This intentionally models the presentation boundary only. Main-thread player,
/// entity and attack simulation remains owned by the existing platform frame model.
/// </summary>
public static class PlatformState20NmiPhase
{
    public const byte OamDmaPage = 0x07;
    public const int OamBytes = 0x100;
    public const int TileColumnBytes = 0x16;
    public const int AttributeColumnBytes = 0x08;
    public const byte CanonicalDebugGateFfde = 0x00;

    public static PlatformState20NmiResult Step(
        PlatformState20NmiState state,
        ReadOnlySpan<byte> oamShadow0700,
        ReadOnlySpan<byte> tileColumn0362,
        ReadOnlySpan<byte> attributeColumn0378)
    {
        if (oamShadow0700.Length != OamBytes)
            throw new ArgumentException("OAM shadow must contain exactly $100 bytes.", nameof(oamShadow0700));
        if (tileColumn0362.Length != TileColumnBytes)
            throw new ArgumentException("Platform streamed tile column must contain exactly $16 bytes.", nameof(tileColumn0362));
        if (attributeColumn0378.Length != AttributeColumnBytes)
            throw new ArgumentException("Platform attribute column must contain exactly $08 bytes.", nameof(attributeColumn0378));

        var operations = new List<PlatformState20NmiOperation>(96)
        {
            new(PlatformState20NmiOperationKind.MarkMapperInterrupted, Value: 0x01),
            new(PlatformState20NmiOperationKind.ResetMmc1Serial, Address: 0xFFFF),
            new(PlatformState20NmiOperationKind.ReadPpuStatus, Address: 0x2002),
            new(PlatformState20NmiOperationKind.WriteOamAddress, Address: 0x2003, Value: 0x00),
            new(PlatformState20NmiOperationKind.OamDma, Address: 0x4014, Value: OamDmaPage),
        };

        // DMA occurs before any state-$20-specific work. Copying the supplied page
        // here makes the main-thread/NMI ownership boundary explicit and testable.
        var oamSnapshot = oamShadow0700.ToArray();

        var streamLatch = state.StreamLatch03A3;
        var invoked9915 = false;

        // $D7F2-$D843: tile streamer / bank-1 visual refresh arbitration.
        if ((state.CameraLow44 & 0x06) != 0)
        {
            streamLatch = 0x00;
            Invoke9915(operations);
            invoked9915 = true;
        }
        else if (streamLatch != 0)
        {
            Invoke9915(operations);
            invoked9915 = true;
        }
        else
        {
            streamLatch = 0xFF;

            // Temporary PPUCTRL write enables increment-by-32 for a vertical
            // nametable column. The RAM mirror $77 itself is not changed here.
            operations.Add(new(
                PlatformState20NmiOperationKind.WritePpuControl,
                Address: 0x2000,
                Value: (byte)(state.PpuCtrlMirror77 | 0x04)));

            var nametableHigh = (byte)(0x20 + (((state.CameraPage45 + 1) & 0x01) << 2));
            var nametableLow = (byte)(state.CameraLow44 >> 3);
            operations.Add(new(PlatformState20NmiOperationKind.WritePpuAddress, 0x2006, nametableHigh));
            operations.Add(new(PlatformState20NmiOperationKind.WritePpuAddress, 0x2006, nametableLow));
            foreach (var value in tileColumn0362)
                operations.Add(new(PlatformState20NmiOperationKind.WritePpuData, 0x2007, value));

            // $D844 only updates the attribute column when scroll low is aligned
            // to a 32-pixel boundary. It first restores PPUCTRL from the mirror.
            if ((state.CameraLow44 & 0x1E) == 0)
            {
                operations.Add(new(
                    PlatformState20NmiOperationKind.WritePpuControl,
                    Address: 0x2000,
                    Value: state.PpuCtrlMirror77));

                var attributeHigh = (byte)(0x23 + (((state.CameraPage45 + 1) & 0x01) << 2));
                var attributeLow = (byte)(0xC0 + (state.CameraLow44 >> 5));

                for (var i = 0; i < AttributeColumnBytes; i++)
                {
                    operations.Add(new(PlatformState20NmiOperationKind.WritePpuAddress, 0x2006, attributeHigh));
                    operations.Add(new(PlatformState20NmiOperationKind.WritePpuAddress, 0x2006, (byte)(attributeLow + i * 8)));
                    operations.Add(new(PlatformState20NmiOperationKind.WritePpuData, 0x2007, attributeColumn0378[i]));
                }
            }
        }

        // $D988-$D9B9: Start-edge pause toggle. $0386 is consumed by the main
        // thread at $C302-$C305 to skip the normal platform simulation while set.
        var pauseDebounce = state.PauseDebounce05;
        var pauseFlag = state.PauseFlag0386;
        var pauseToggled = false;

        if ((state.ControllerHeld3D & 0x10) == 0)
        {
            pauseDebounce = 0x00;
        }
        else if (pauseDebounce == 0)
        {
            pauseDebounce = 0xFF;
            pauseToggled = true;
            operations.Add(new(PlatformState20NmiOperationKind.RebuildPlatformAudioCb6A));

            if (pauseFlag == 0)
            {
                // Entering pause resets the audio slot system and loads cue $63.
                operations.Add(new(PlatformState20NmiOperationKind.ResetAudioAndLoad63, Value: 0x63));
                pauseFlag = 0xFF;
            }
            else
            {
                pauseFlag = 0x00;
            }
        }

        // The canonical byte at $FFDE is zero, so $D9BA+ is dead in this ROM.
        // Do not reproduce the dormant debug/stat mutation tail as gameplay.
        const bool canonicalDebugTailReachable = CanonicalDebugGateFfde != 0;

        // $D367-$D39D common presentation commit.
        var ppuCtrlMirror = (byte)((state.PpuCtrlMirror77 & 0xFE) | (state.CameraPage45 & 0x01));
        operations.Add(new(PlatformState20NmiOperationKind.WritePpuControl, 0x2000, ppuCtrlMirror));
        operations.Add(new(PlatformState20NmiOperationKind.WritePpuMask, 0x2001, state.PpuMaskMirror78));
        operations.Add(new(PlatformState20NmiOperationKind.WritePpuScroll, 0x2005, state.CameraLow44));
        operations.Add(new(PlatformState20NmiOperationKind.WritePpuScroll, 0x2005, state.ScrollY46));
        operations.Add(new(PlatformState20NmiOperationKind.WaitSpriteZeroHitClear, 0x2002, 0x40));
        operations.Add(new(
            PlatformState20NmiOperationKind.RestorePersistentPrgBank,
            Address: 0xE000,
            Value: state.PersistentPrgBank3B));
        operations.Add(new(PlatformState20NmiOperationKind.ReturnFromInterrupt));

        var updatedState = state with
        {
            PpuCtrlMirror77 = ppuCtrlMirror,
            StreamLatch03A3 = streamLatch,
            PauseDebounce05 = pauseDebounce,
            PauseFlag0386 = pauseFlag,
        };

        return new(
            updatedState,
            oamSnapshot,
            operations,
            invoked9915,
            pauseToggled,
            canonicalDebugTailReachable);
    }

    private static void Invoke9915(List<PlatformState20NmiOperation> operations)
    {
        // $C0B4 is the raw MMC1 PRG writer: these temporary switches do not alter
        // persistent bank mirror $3B, which the NMI epilogue restores separately.
        operations.Add(new(PlatformState20NmiOperationKind.TemporaryPrgBank, Address: 0xE000, Value: 0x01));
        operations.Add(new(PlatformState20NmiOperationKind.InvokeBank1VisualRefresh9915, Address: 0x9915));
        operations.Add(new(PlatformState20NmiOperationKind.TemporaryPrgBank, Address: 0xE000, Value: 0x03));
    }
}

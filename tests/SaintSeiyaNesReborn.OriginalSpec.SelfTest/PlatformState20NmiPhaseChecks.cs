using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class PlatformState20NmiPhaseChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckOamSnapshotPrecedesState20Work();
        CheckTileAndAttributeStreaming();
        CheckRefreshBranchAndLatch();
        CheckPauseToggleAndDebounce();
        CheckCommonPpuCommitAndBankRestore();
        CheckCanonicalDebugTailIsDead();
    }

    private static void CheckOamSnapshotPrecedesState20Work()
    {
        var oam = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
        var result = Step(DefaultState(), oam);
        oam[0] = 0xEE;

        Equal((byte)0x00, result.OamSnapshot[0], "NMI DMA snapshots supplied $0700 page");
        Equal(PlatformState20NmiOperationKind.WriteOamAddress, result.Operations[3].Kind, "OAM address before DMA");
        Equal(PlatformState20NmiOperationKind.OamDma, result.Operations[4].Kind, "OAM DMA order");
        Equal((byte)0x07, result.Operations[4].Value, "OAM DMA source page");
    }

    private static void CheckTileAndAttributeStreaming()
    {
        var state = DefaultState() with { CameraLow44 = 0x00, CameraPage45 = 0x00, StreamLatch03A3 = 0 };
        var tiles = Enumerable.Range(0, 0x16).Select(i => (byte)(0x40 + i)).ToArray();
        var attrs = Enumerable.Range(0, 8).Select(i => (byte)(0x80 + i)).ToArray();
        var result = PlatformState20NmiPhase.Step(state, new byte[256], tiles, attrs);

        Equal((byte)0xFF, result.State.StreamLatch03A3, "stream latch set after column upload");
        True(!result.InvokedBank1VisualRefresh9915, "column upload does not call $9915");
        True(result.Operations.Any(o => o.Kind == PlatformState20NmiOperationKind.WritePpuControl && o.Value == (byte)(state.PpuCtrlMirror77 | 0x04)), "temporary vertical PPU increment");

        var dataWrites = result.Operations.Where(o => o.Kind == PlatformState20NmiOperationKind.WritePpuData).ToArray();
        Equal(0x16 + 8, dataWrites.Length, "tile plus attribute PPU data writes");
        Equal((byte)0x40, dataWrites[0].Value, "first streamed tile");
        Equal((byte)0x55, dataWrites[0x15].Value, "last streamed tile");
        Equal((byte)0x80, dataWrites[0x16].Value, "first attribute byte");
        Equal((byte)0x87, dataWrites[^1].Value, "last attribute byte");
    }

    private static void CheckRefreshBranchAndLatch()
    {
        var unaligned = Step(DefaultState() with { CameraLow44 = 0x02, StreamLatch03A3 = 0xFF });
        Equal((byte)0x00, unaligned.State.StreamLatch03A3, "unaligned scroll clears stream latch");
        True(unaligned.InvokedBank1VisualRefresh9915, "unaligned scroll invokes $9915");

        var alignedLatched = Step(DefaultState() with { CameraLow44 = 0x08, StreamLatch03A3 = 0xFF });
        Equal((byte)0xFF, alignedLatched.State.StreamLatch03A3, "aligned latched refresh preserves latch");
        var banks = alignedLatched.Operations.Where(o => o.Kind == PlatformState20NmiOperationKind.TemporaryPrgBank).Select(o => o.Value).ToArray();
        Equal(2, banks.Length, "refresh has two temporary PRG switches");
        Equal((byte)0x01, banks[0], "refresh enters PRG bank1");
        Equal((byte)0x03, banks[1], "refresh returns to PRG bank3");
    }

    private static void CheckPauseToggleAndDebounce()
    {
        var press = Step(DefaultState() with { ControllerHeld3D = 0x10, PauseDebounce05 = 0, PauseFlag0386 = 0 });
        True(press.PauseToggled, "fresh Start press toggles pause");
        Equal((byte)0xFF, press.State.PauseDebounce05, "Start debounce latched");
        Equal((byte)0xFF, press.State.PauseFlag0386, "pause flag set");
        True(press.Operations.Any(o => o.Kind == PlatformState20NmiOperationKind.ResetAudioAndLoad63 && o.Value == 0x63), "pause entry resets audio and loads cue $63");

        var held = Step(press.State with { ControllerHeld3D = 0x10 });
        True(!held.PauseToggled, "held Start does not retrigger");
        Equal((byte)0xFF, held.State.PauseFlag0386, "held Start preserves pause");

        var release = Step(held.State with { ControllerHeld3D = 0x00 });
        Equal((byte)0x00, release.State.PauseDebounce05, "Start release rearms debounce");

        var unpause = Step(release.State with { ControllerHeld3D = 0x10 });
        True(unpause.PauseToggled, "second Start edge toggles again");
        Equal((byte)0x00, unpause.State.PauseFlag0386, "second edge clears pause");
        True(!unpause.Operations.Any(o => o.Kind == PlatformState20NmiOperationKind.ResetAudioAndLoad63), "unpause does not run pause-entry reset");
    }

    private static void CheckCommonPpuCommitAndBankRestore()
    {
        var state = DefaultState() with
        {
            CameraLow44 = 0xA8,
            CameraPage45 = 0x03,
            ScrollY46 = 0x18,
            PpuCtrlMirror77 = 0x90,
            PpuMaskMirror78 = 0x1E,
            PersistentPrgBank3B = 0x02,
        };
        var result = Step(state);

        Equal((byte)0x91, result.State.PpuCtrlMirror77, "epilogue mirrors camera page bit0 into PPUCTRL bit0");
        var tail = result.Operations.TakeLast(7).ToArray();
        Equal(PlatformState20NmiOperationKind.WritePpuControl, tail[0].Kind, "final PPUCTRL commit");
        Equal((byte)0x91, tail[0].Value, "final PPUCTRL value");
        Equal(PlatformState20NmiOperationKind.WritePpuMask, tail[1].Kind, "PPUMASK commit");
        Equal((byte)0x1E, tail[1].Value, "PPUMASK value");
        Equal((byte)0xA8, tail[2].Value, "horizontal scroll commit");
        Equal((byte)0x18, tail[3].Value, "vertical scroll commit");
        Equal(PlatformState20NmiOperationKind.WaitSpriteZeroHitClear, tail[4].Kind, "status wait");
        Equal((byte)0x40, tail[4].Value, "waits on PPUSTATUS sprite-zero-hit bit");
        Equal(PlatformState20NmiOperationKind.RestorePersistentPrgBank, tail[5].Kind, "persistent bank restore");
        Equal((byte)0x02, tail[5].Value, "restored PRG bank");
        Equal(PlatformState20NmiOperationKind.ReturnFromInterrupt, tail[6].Kind, "RTI boundary");
    }

    private static void CheckCanonicalDebugTailIsDead()
    {
        Equal((byte)0x00, PlatformState20NmiPhase.CanonicalDebugGateFfde, "canonical $FFDE gate byte");
        True(!Step(DefaultState()).CanonicalDebugTailReachable, "$D9BA+ debug/stat tail unreachable in canonical ROM");
    }

    private static PlatformState20NmiResult Step(PlatformState20NmiState state) =>
        Step(state, new byte[256]);

    private static PlatformState20NmiResult Step(PlatformState20NmiState state, byte[] oam) =>
        PlatformState20NmiPhase.Step(state, oam, new byte[0x16], new byte[8]);

    private static PlatformState20NmiState DefaultState() => new(
        CameraLow44: 0x02,
        CameraPage45: 0,
        ScrollY46: 0,
        PpuCtrlMirror77: 0x90,
        PpuMaskMirror78: 0x1E,
        PersistentPrgBank3B: 0x03,
        StreamLatch03A3: 0,
        PauseDebounce05: 0,
        PauseFlag0386: 0,
        ControllerHeld3D: 0);

    private static void Equal<T>(T expected, T actual, string name) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"{name}: expected {expected}, got {actual}");
    }

    private static void True(bool value, string name)
    {
        if (!value)
            throw new InvalidOperationException($"{name}: expected true");
    }
}

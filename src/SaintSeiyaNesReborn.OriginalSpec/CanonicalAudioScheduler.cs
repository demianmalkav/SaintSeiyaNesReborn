namespace SaintSeiyaNesReborn.OriginalSpec;

public enum CanonicalAudioChannel : byte
{
    Pulse1 = 0,
    Pulse2 = 1,
    Triangle = 2,
    Noise = 3,
}

public enum CanonicalAudioVoiceAction : byte
{
    None = 0,
    StartOrRetune = 1,
    Stop = 2,
    ControlAndTimerLow = 3,
}

public readonly record struct CanonicalAudioCueDescriptor(
    byte SlotOffset,
    byte Field1,
    byte StreamPointerLow,
    byte StreamPointerHigh)
{
    public ushort StreamPointer => (ushort)(StreamPointerLow | (StreamPointerHigh << 8));
}

public readonly record struct CanonicalAudioSlot(
    byte Lifecycle,
    byte Field1,
    byte StreamPointerLow,
    byte StreamPointerHigh)
{
    public bool Active => Lifecycle != 0xFF;
    public CanonicalAudioChannel Channel => (CanonicalAudioChannel)(Field1 & 0x03);
    public ushort StreamPointer => (ushort)(StreamPointerLow | (StreamPointerHigh << 8));

    public static CanonicalAudioSlot Inactive => new(0xFF, 0, 0, 0);
}

/// <summary>
/// Semantic voice operation supplied after stream decoding. The original note/envelope
/// payload remains external to this model; this contract owns scheduling and APU routing.
/// </summary>
public readonly record struct CanonicalAudioVoiceFrame(
    CanonicalAudioVoiceAction Action,
    byte Control,
    byte Auxiliary,
    byte TimerLow,
    byte TimerHigh);

public readonly record struct CanonicalApuWrite(ushort Address, byte Value);

public sealed class CanonicalAudioSchedulerState
{
    public CanonicalAudioSlot[] Slots { get; }
    public byte EnableShadow04F0 { get; }
    public byte VisualResetLatch04EF { get; }
    public byte Phase04EE { get; }

    public CanonicalAudioSchedulerState(
        CanonicalAudioSlot[] slots,
        byte enableShadow04F0,
        byte visualResetLatch04EF,
        byte phase04EE)
    {
        if (slots.Length != CanonicalAudioScheduler.SlotCount)
            throw new ArgumentException($"Expected {CanonicalAudioScheduler.SlotCount} audio slots.", nameof(slots));
        Slots = (CanonicalAudioSlot[])slots.Clone();
        EnableShadow04F0 = enableShadow04F0;
        VisualResetLatch04EF = visualResetLatch04EF;
        Phase04EE = phase04EE;
    }
}

public readonly record struct CanonicalAudioFrameResult(
    CanonicalAudioSchedulerState State,
    IReadOnlyList<CanonicalApuWrite> ApuWrites,
    byte ClaimedChannelMask);

/// <summary>
/// Clean-room architecture contract for fixed $DB9C/$DBB6 and bank-0 $8B50+.
/// It deliberately accepts decoded voice-frame operations instead of embedding or
/// reproducing the original song/SFX byte streams.
/// </summary>
public static class CanonicalAudioScheduler
{
    public const int SlotCount = 8;
    public const int SlotStride = 0x15;
    public const int SlotSpan = SlotCount * SlotStride; // $A8

    private static readonly byte[] EnableBits = [0x01, 0x02, 0x04, 0x08];
    private static readonly byte[] ClearMasks = [0x0E, 0x0D, 0x0B, 0x07];
    private static readonly ushort[] ApuBase = [0x4000, 0x4004, 0x4008, 0x400C];

    public static CanonicalAudioSchedulerState ResetDB9C() =>
        new(Enumerable.Repeat(CanonicalAudioSlot.Inactive, SlotCount).ToArray(), 0, 0, 0);

    public static CanonicalApuWrite ResetApuWriteDB9E() => new(0x4015, 0x00);

    public static CanonicalAudioSchedulerState LoadCueDBB6(
        CanonicalAudioSchedulerState state,
        byte cueId,
        ReadOnlySpan<CanonicalAudioCueDescriptor> descriptors)
    {
        if (cueId >= descriptors.Length)
            throw new ArgumentOutOfRangeException(nameof(cueId));
        return LoadDescriptorDBB6(state, descriptors[cueId]);
    }

    public static CanonicalAudioSchedulerState LoadDescriptorDBB6(
        CanonicalAudioSchedulerState state,
        CanonicalAudioCueDescriptor descriptor)
    {
        if (descriptor.SlotOffset >= SlotSpan || descriptor.SlotOffset % SlotStride != 0)
            throw new ArgumentOutOfRangeException(nameof(descriptor), "Descriptor byte0 must select one $15-byte slot offset in $00..$93.");

        var slots = (CanonicalAudioSlot[])state.Slots.Clone();
        var slotIndex = descriptor.SlotOffset / SlotStride;
        var previous = slots[slotIndex];
        var shadow = state.EnableShadow04F0;

        // $DBD7-$DBEA: replacing an active slot clears the old slot's channel bit
        // from the software $4015 shadow. DBB6 itself does not write $4015.
        if (previous.Active)
            shadow &= ClearMasks[(int)previous.Channel];

        slots[slotIndex] = new CanonicalAudioSlot(
            Lifecycle: 0x00,
            Field1: descriptor.Field1,
            StreamPointerLow: descriptor.StreamPointerLow,
            StreamPointerHigh: descriptor.StreamPointerHigh);

        return new(slots, shadow, state.VisualResetLatch04EF, state.Phase04EE);
    }

    /// <summary>
    /// Models stream command $A5 at $8D33-$8D3C. The fixed-bank consumer is visual,
    /// not APU state: nonzero $04EF causes $8904/$8AF1 work and is then cleared.
    /// </summary>
    public static CanonicalAudioSchedulerState RequestVisualReset04EF(
        CanonicalAudioSchedulerState state,
        byte value) =>
        new(state.Slots, state.EnableShadow04F0, value, state.Phase04EE);

    public static (CanonicalAudioSchedulerState State, bool Requested) ConsumeVisualReset04EF(
        CanonicalAudioSchedulerState state)
    {
        var requested = state.VisualResetLatch04EF != 0;
        return (new(state.Slots, state.EnableShadow04F0, 0, state.Phase04EE), requested);
    }

    /// <summary>
    /// Executes the scheduler/arbitration/APU-routing part of one $8B50 frame.
    /// decodedFrames is indexed by scheduler slot. Stream bytecode parsing and original
    /// payload bytes remain outside this boundary.
    /// </summary>
    public static CanonicalAudioFrameResult StepDecodedFrame8B50(
        CanonicalAudioSchedulerState state,
        IReadOnlyList<CanonicalAudioVoiceFrame?> decodedFrames)
    {
        if (decodedFrames.Count != SlotCount)
            throw new ArgumentException($"Expected {SlotCount} decoded slot frames.", nameof(decodedFrames));

        var slots = (CanonicalAudioSlot[])state.Slots.Clone();
        var writes = new List<CanonicalApuWrite>();
        byte claimed = 0;
        var shadow = state.EnableShadow04F0;
        var phase = unchecked((byte)(state.Phase04EE + 1));

        for (var i = 0; i < SlotCount; i++)
        {
            var slot = slots[i];
            if (!slot.Active)
                continue;

            var channel = (int)slot.Channel;
            var bit = EnableBits[channel];

            // $8E0D drops later work when an earlier slot has already claimed this channel.
            if ((claimed & bit) != 0)
                continue;

            if (slot.Lifecycle == 0)
                slots[i] = slot = slot with { Lifecycle = 2 };

            var frame = decodedFrames[i];
            if (frame is not null)
            {
                var operation = frame.Value;
                var apu = ApuBase[channel];
                switch (operation.Action)
                {
                    case CanonicalAudioVoiceAction.StartOrRetune:
                        shadow |= bit;
                        writes.Add(new(0x4015, shadow));
                        writes.Add(new(apu, operation.Control));
                        writes.Add(new((ushort)(apu + 1), operation.Auxiliary));
                        writes.Add(new((ushort)(apu + 2), operation.TimerLow));
                        writes.Add(new((ushort)(apu + 3), operation.TimerHigh));
                        break;

                    case CanonicalAudioVoiceAction.Stop:
                        shadow &= ClearMasks[channel];
                        writes.Add(new(0x4015, shadow));
                        break;

                    case CanonicalAudioVoiceAction.ControlAndTimerLow:
                        writes.Add(new(apu, operation.Control));
                        writes.Add(new((ushort)(apu + 2), operation.TimerLow));
                        break;

                    case CanonicalAudioVoiceAction.None:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(decodedFrames));
                }
            }

            claimed |= bit;
        }

        return new(
            new CanonicalAudioSchedulerState(slots, shadow, state.VisualResetLatch04EF, phase),
            writes,
            claimed);
    }

    public static byte ChannelEnableBit(CanonicalAudioChannel channel) => EnableBits[(int)channel];
    public static byte ChannelClearMask(CanonicalAudioChannel channel) => ClearMasks[(int)channel];
    public static ushort ChannelApuBase(CanonicalAudioChannel channel) => ApuBase[(int)channel];
}

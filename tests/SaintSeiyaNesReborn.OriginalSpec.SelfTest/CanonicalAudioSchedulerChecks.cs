using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;

internal static class CanonicalAudioSchedulerChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckResetAndDescriptorLoad();
        CheckPreemptionShadow();
        CheckFramePriorityAndApuRouting();
        CheckVisualResetLatch();
    }

    private static void CheckResetAndDescriptorLoad()
    {
        var state = CanonicalAudioScheduler.ResetDB9C();
        Equal((byte)0, state.EnableShadow04F0, "DB9C clears 04F0");
        Equal((byte)0, state.VisualResetLatch04EF, "DB9C clears 04EF");
        Equal(new CanonicalApuWrite(0x4015, 0), CanonicalAudioScheduler.ResetApuWriteDB9E(), "DB9C disables APU channels");
        True(state.Slots.All(x => x.Lifecycle == 0xFF), "DB9C marks all eight slots inactive");

        var descriptors = new[]
        {
            new CanonicalAudioCueDescriptor(0x2A, 0x82, 0x34, 0x12),
        };
        state = CanonicalAudioScheduler.LoadCueDBB6(state, 0, descriptors);
        Equal((byte)0, state.Slots[2].Lifecycle, "DBB6 marks selected slot newly loaded");
        Equal(CanonicalAudioChannel.Triangle, state.Slots[2].Channel, "descriptor low two bits select triangle");
        Equal((ushort)0x1234, state.Slots[2].StreamPointer, "descriptor bytes 2/3 become stream pointer");
    }

    private static void CheckPreemptionShadow()
    {
        var slots = Enumerable.Repeat(CanonicalAudioSlot.Inactive, CanonicalAudioScheduler.SlotCount).ToArray();
        slots[1] = new CanonicalAudioSlot(2, 0x01, 0, 0); // active pulse2 owner
        var state = new CanonicalAudioSchedulerState(slots, 0x0F, 0, 0);

        state = CanonicalAudioScheduler.LoadDescriptorDBB6(
            state,
            new CanonicalAudioCueDescriptor(0x15, 0x03, 0x78, 0x56));

        Equal((byte)0x0D, state.EnableShadow04F0, "preemption clears old pulse2 bit from 4015 shadow");
        Equal(CanonicalAudioChannel.Noise, state.Slots[1].Channel, "replacement descriptor installs new channel owner");
        Equal((byte)0, state.Slots[1].Lifecycle, "replacement lifecycle restarts at zero");
    }

    private static void CheckFramePriorityAndApuRouting()
    {
        var slots = Enumerable.Repeat(CanonicalAudioSlot.Inactive, CanonicalAudioScheduler.SlotCount).ToArray();
        slots[0] = new CanonicalAudioSlot(0, 0x00, 0, 0); // pulse1, earlier slot
        slots[4] = new CanonicalAudioSlot(2, 0x00, 0, 0); // pulse1, later competing slot
        slots[2] = new CanonicalAudioSlot(2, 0x82, 0, 0); // triangle
        var state = new CanonicalAudioSchedulerState(slots, 0, 0, 0xFF);

        CanonicalAudioVoiceFrame?[] frames = new CanonicalAudioVoiceFrame?[CanonicalAudioScheduler.SlotCount];
        frames[0] = new(CanonicalAudioVoiceAction.StartOrRetune, 0x31, 0x08, 0x44, 0x02);
        frames[4] = new(CanonicalAudioVoiceAction.StartOrRetune, 0x99, 0x99, 0x99, 0x99);
        frames[2] = new(CanonicalAudioVoiceAction.StartOrRetune, 0x80, 0x00, 0x55, 0x03);

        var result = CanonicalAudioScheduler.StepDecodedFrame8B50(state, frames);

        Equal((byte)0, result.State.Phase04EE, "8B50 increments 04EE modulo 256");
        Equal((byte)2, result.State.Slots[0].Lifecycle, "new slot enters initialized lifecycle state");
        Equal((byte)0x05, result.State.EnableShadow04F0, "pulse1 and triangle enabled in 4015 shadow");
        Equal((byte)0x05, result.ClaimedChannelMask, "first slot per channel owns frame");

        var expected = new[]
        {
            new CanonicalApuWrite(0x4015, 0x01),
            new CanonicalApuWrite(0x4000, 0x31),
            new CanonicalApuWrite(0x4001, 0x08),
            new CanonicalApuWrite(0x4002, 0x44),
            new CanonicalApuWrite(0x4003, 0x02),
            new CanonicalApuWrite(0x4015, 0x05),
            new CanonicalApuWrite(0x4008, 0x80),
            new CanonicalApuWrite(0x4009, 0x00),
            new CanonicalApuWrite(0x400A, 0x55),
            new CanonicalApuWrite(0x400B, 0x03),
        };
        Equal(expected.Length, result.ApuWrites.Count, "semantic APU write count");
        for (var i = 0; i < expected.Length; i++)
            Equal(expected[i], result.ApuWrites[i], $"semantic APU write {i}");

        True(!result.ApuWrites.Any(x => x.Value == 0x99), "later competing pulse1 slot is suppressed");
    }

    private static void CheckVisualResetLatch()
    {
        var state = CanonicalAudioScheduler.RequestVisualReset04EF(CanonicalAudioScheduler.ResetDB9C(), 0x7F);
        Equal((byte)0x7F, state.VisualResetLatch04EF, "A5-style stream request owns 04EF");
        var consumed = CanonicalAudioScheduler.ConsumeVisualReset04EF(state);
        True(consumed.Requested, "fixed dispatcher observes nonzero 04EF");
        Equal((byte)0, consumed.State.VisualResetLatch04EF, "fixed dispatcher clears 04EF after visual work");
    }

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

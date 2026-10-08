namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformActionFamily : byte
{
    Neutral = 0x00,
    Locomotion = 0x10,
    Crouch = 0x20,
    Jump = 0x30,
    Special40 = 0x40,
    FallOrDrop = 0x50,
    DamageOrHazard = 0x80,
    Death = 0xD0,
}

public static class PlatformActionState
{
    public static byte Family(byte state) => (byte)(state & 0xF0);

    public static int JumpDirectionalBits(byte state)
    {
        if (Family(state) != (byte)PlatformActionFamily.Jump)
            throw new ArgumentException("State is not in jump family $30-$3F.", nameof(state));
        return state & 0x03;
    }
}

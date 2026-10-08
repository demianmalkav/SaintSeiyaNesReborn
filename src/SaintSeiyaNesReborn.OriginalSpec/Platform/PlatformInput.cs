namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

[Flags]
public enum PlatformInput : byte
{
    None = 0x00,
    Right = 0x01,
    Left = 0x02,
    Down = 0x04,
    Up = 0x08,
    Start = 0x10,
    Select = 0x20,
    B = 0x40,
    A = 0x80,
}

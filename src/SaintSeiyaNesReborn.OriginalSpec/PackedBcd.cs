namespace SaintSeiyaNesReborn.OriginalSpec;

public static class PackedBcd
{
    public static int DecodeByte(byte value)
    {
        var tens = (value >> 4) & 0x0F;
        var ones = value & 0x0F;
        if (tens > 9 || ones > 9)
            throw new ArgumentOutOfRangeException(nameof(value), value, "Byte is not valid packed BCD.");
        return tens * 10 + ones;
    }

    public static byte EncodeByte(int value)
    {
        if ((uint)value > 99)
            throw new ArgumentOutOfRangeException(nameof(value), value, "Value must be in 0..99.");
        return (byte)(((value / 10) << 4) | (value % 10));
    }

    public static int DecodeFourDigits(byte lowTwoDigits, byte highTwoDigits) =>
        DecodeByte(highTwoDigits) * 100 + DecodeByte(lowTwoDigits);

    public static (byte LowTwoDigits, byte HighTwoDigits) EncodeFourDigits(int value)
    {
        if ((uint)value > 9999)
            throw new ArgumentOutOfRangeException(nameof(value), value, "Value must be in 0..9999.");
        return (EncodeByte(value % 100), EncodeByte(value / 100));
    }
}

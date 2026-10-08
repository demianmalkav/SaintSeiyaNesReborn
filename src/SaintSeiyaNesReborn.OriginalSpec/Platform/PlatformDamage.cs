using SaintSeiyaNesReborn.OriginalSpec;

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public static class PlatformDamage
{
    // ROM bank 1 $8611, indexed in internal platform order.
    private static readonly byte[] Coefficients = [19, 25, 21, 17, 15];

    public static int Coefficient(PlatformSaintIndex saint)
    {
        var index = (int)saint;
        if ((uint)index >= Coefficients.Length)
            throw new ArgumentOutOfRangeException(nameof(saint));
        return Coefficients[index];
    }

    /// <summary>
    /// Reproduces the effective arithmetic of bank 1 $8616+.
    /// At three-digit Cosmo values the original ignores the ones digit.
    /// </summary>
    public static int FromCosmo(PlatformSaintIndex saint, int cosmo)
    {
        if ((uint)cosmo > 999)
            throw new ArgumentOutOfRangeException(nameof(cosmo), cosmo, "Cosmo must be in 0..999.");

        var k = Coefficient(saint);

        if (cosmo < 100)
            return k * cosmo / 100;

        var hundreds = cosmo / 100;
        var tens = (cosmo / 10) % 10;
        return k * hundreds + (k * tens / 10);
    }
}

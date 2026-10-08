from __future__ import annotations

from dataclasses import dataclass
from enum import IntEnum


class Saint(IntEnum):
    """Internal platform-engine order used by RAM index $03."""

    SEIYA = 0
    SHUN = 1
    HYOGA = 2
    SHIRYU = 3
    IKKI = 4


# Reconstructed from PRG bank 1 table $8611.
DAMAGE_BASE = (19, 25, 21, 17, 15)

# Reconstructed from PRG bank 3 table $BCAE. Rows are Cosmo-hundreds
# brackets 0-1, 2-3, 4-5, 6-7, 8-9. Columns are internal Saints 0..3.
# Ikki (slot 4) bypasses this table and always receives 60.
PROJECTILE_RANGE_TABLE = (
    (3, 1, 4, 6),
    (6, 3, 10, 10),
    (12, 8, 16, 14),
    (24, 12, 22, 18),
    (48, 16, 28, 22),
)


def platform_attack_damage(saint: Saint, cosmo: int) -> int:
    """Return the exact integer value written to original RAM $72.

    The >=100 branch intentionally ignores the ones digit because the
    original BCD-digit algorithm truncates before that digit contributes.
    """

    if not 0 <= cosmo <= 999:
        raise ValueError("Cosmo must be in range 0..999")

    base = DAMAGE_BASE[int(saint)]
    hundreds = cosmo // 100
    tens = (cosmo // 10) % 10
    ones = cosmo % 10

    if hundreds:
        return base * hundreds + (base * tens) // 10

    return (base * tens + (base * ones) // 10) // 10


def projectile_range_parameter(saint: Saint, cosmo: int) -> int:
    """Original attack range/lifetime seed for the selected Saint/Cosmo."""

    if not 0 <= cosmo <= 999:
        raise ValueError("Cosmo must be in range 0..999")

    if saint is Saint.IKKI:
        return 60

    bracket = (cosmo // 100) // 2
    return PROJECTILE_RANGE_TABLE[bracket][int(saint)]


def bcd2_to_int(value: int) -> int:
    """Decode one packed-BCD byte containing 00..99."""

    high = (value >> 4) & 0x0F
    low = value & 0x0F
    if high > 9 or low > 9:
        raise ValueError(f"invalid packed-BCD byte: 0x{value:02X}")
    return high * 10 + low


def add_seventh_sense_reward(
    current: int, reward_bcd: int, *, mode_active: bool = True
) -> int:
    """Mirror fixed routine $D1E0 at semantic level.

    The original ignores this reward path when high-level mode $02 is zero
    and saturates the four-digit Seventh Sense value at 9999.
    """

    if not 0 <= current <= 9999:
        raise ValueError("Seventh Sense must be in range 0..9999")
    if not mode_active:
        return current
    return min(9999, current + bcd2_to_int(reward_bcd))


@dataclass(frozen=True)
class ContactDrainResult:
    life: int
    cosmo: int
    frames: int
    failed: bool


def resolve_contact_drain(
    life: int, cosmo: int, life_ticks: int, cosmo_ticks: int
) -> ContactDrainResult:
    """Reference model for ordinary entity-contact resource drain.

    On each platform update while the counters are nonzero, the original:
    - consumes one Life tick and subtracts 2 Life;
    - consumes one Cosmo tick and subtracts 1 Cosmo.

    Exhausting either resource enters the original failure transition.
    The separate ordinary-hit invulnerability timer is 32 update frames and
    is intentionally not folded into this drain function.
    """

    if min(life, cosmo, life_ticks, cosmo_ticks) < 0:
        raise ValueError("values must be non-negative")

    frames = 0
    while life_ticks or cosmo_ticks:
        frames += 1

        if life_ticks:
            life_ticks -= 1
            life -= 2
            if life <= 0:
                return ContactDrainResult(0, max(cosmo, 0), frames, True)

        if cosmo_ticks:
            cosmo_ticks -= 1
            cosmo -= 1
            if cosmo <= 0:
                return ContactDrainResult(max(life, 0), 0, frames, True)

    return ContactDrainResult(life, cosmo, frames, False)


ORDINARY_HIT_INVULNERABILITY_FRAMES = 32

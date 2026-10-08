"""Clean-room ORIGINAL SPEC for Kanketsu Hen battle resources.

The original stores Life/Cosmo as three decimal digits split across two bytes:
- low byte: packed BCD tens/ones (00..99)
- high byte: one decimal hundreds digit (0..9)

Each resource also has an exclusive hundreds boundary. Boundary 1 means the
largest legal value is 99; boundary 5 means 499; boundary 10 would mean 999,
although the stored boundary itself is a nibble-sized game field.
"""

from __future__ import annotations

from dataclasses import dataclass


class BcdError(ValueError):
    pass


def packed_bcd_to_int(value: int) -> int:
    hi = (value >> 4) & 0x0F
    lo = value & 0x0F
    if hi > 9 or lo > 9:
        raise BcdError(f"invalid packed BCD byte 0x{value:02X}")
    return hi * 10 + lo


def int_to_packed_bcd(value: int) -> int:
    if not 0 <= value <= 99:
        raise BcdError("two-digit BCD value must be 0..99")
    return ((value // 10) << 4) | (value % 10)


@dataclass(frozen=True)
class ThreeDigitResource:
    low_bcd: int
    hundreds: int
    hundreds_boundary: int

    def __post_init__(self) -> None:
        packed_bcd_to_int(self.low_bcd)
        if not 0 <= self.hundreds <= 9:
            raise BcdError("hundreds digit must be 0..9")
        if not 1 <= self.hundreds_boundary <= 10:
            raise BcdError("hundreds boundary must be 1..10")
        if self.value > self.maximum:
            raise BcdError("resource exceeds cap")

    @property
    def value(self) -> int:
        return self.hundreds * 100 + packed_bcd_to_int(self.low_bcd)

    @property
    def maximum(self) -> int:
        return self.hundreds_boundary * 100 - 1

    @classmethod
    def from_int(cls, value: int, hundreds_boundary: int) -> "ThreeDigitResource":
        maximum = hundreds_boundary * 100 - 1
        if not 0 <= value <= maximum:
            raise BcdError(f"value must be 0..{maximum}")
        return cls(int_to_packed_bcd(value % 100), value // 100, hundreds_boundary)

    def increment(self) -> tuple["ThreeDigitResource", bool]:
        """Mirror the ROM one-unit increment. Returns (new_value, blocked_at_cap)."""
        if self.value >= self.maximum:
            return self, True
        return ThreeDigitResource.from_int(self.value + 1, self.hundreds_boundary), False

    def decrement(self) -> tuple["ThreeDigitResource", bool]:
        """Mirror the ROM one-unit decrement. Returns (new_value, blocked_at_zero)."""
        if self.value == 0:
            return self, True
        return ThreeDigitResource.from_int(self.value - 1, self.hundreds_boundary), False


@dataclass(frozen=True)
class BattleResources:
    life: ThreeDigitResource
    cosmo: ThreeDigitResource


def unpack_cap_byte(cap_byte: int) -> tuple[int, int]:
    """Return (life_boundary, cosmo_boundary) from one persistent Saint cap byte."""
    life = (cap_byte >> 4) & 0x0F
    cosmo = cap_byte & 0x0F
    if life == 0 or cosmo == 0:
        raise BcdError("zero resource boundary is not a normal playable cap")
    return life, cosmo


def load_battle_resources(record: tuple[int, int, int, int, int]) -> BattleResources:
    """Convert one persistent five-byte Saint record to active battle resources.

    Record layout:
      [Life low BCD, Life hundreds, Cosmo low BCD, Cosmo hundreds, cap byte]

    This mirrors bank 1 `$AB4E+`, which loads Life to `$05CE/$05CF` and
    Cosmo to `$05BC/$05BD`.
    """
    if len(record) != 5:
        raise ValueError("record must contain five bytes")
    life_low, life_h, cosmo_low, cosmo_h, caps = record
    life_cap, cosmo_cap = unpack_cap_byte(caps)
    return BattleResources(
        life=ThreeDigitResource(life_low, life_h, life_cap),
        cosmo=ThreeDigitResource(cosmo_low, cosmo_h, cosmo_cap),
    )


def seventh_sense_increment(value: int) -> tuple[int, bool]:
    if not 0 <= value <= 9999:
        raise ValueError("Seventh Sense must be 0..9999")
    if value == 9999:
        return 9999, True
    return value + 1, False


def seventh_sense_decrement(value: int) -> tuple[int, bool]:
    if not 0 <= value <= 9999:
        raise ValueError("Seventh Sense must be 0..9999")
    if value == 0:
        return 0, True
    return value - 1, False

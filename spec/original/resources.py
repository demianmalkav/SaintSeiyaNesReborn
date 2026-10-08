from __future__ import annotations

from dataclasses import dataclass
from enum import Enum


class Resource(Enum):
    LIFE = "life"
    COSMO = "cosmo"


SEVENTH_SENSE_MAX = 9999


def resource_max_from_boundary(boundary: int) -> int:
    """Convert original exclusive hundreds boundary to semantic maximum.

    The allocation routines reject an increment when the resulting hundreds
    digit is >= boundary. Normal game values therefore obey:
        max = boundary * 100 - 1
    Boundary zero is exposed conservatively as zero.
    """

    if not 0 <= boundary <= 9:
        raise ValueError("boundary must fit one decimal nibble (0..9)")
    return max(0, boundary * 100 - 1)


def unpack_cap_byte(cap_byte: int) -> tuple[int, int]:
    """Return (life_boundary, cosmo_boundary) from one persistent cap byte."""

    if not 0 <= cap_byte <= 0x99:
        raise ValueError("cap byte must be in range 0x00..0x99")
    life_boundary = (cap_byte >> 4) & 0x0F
    cosmo_boundary = cap_byte & 0x0F
    if life_boundary > 9 or cosmo_boundary > 9:
        raise ValueError("cap byte nibbles must be decimal")
    return life_boundary, cosmo_boundary


def pack_cap_byte(life_boundary: int, cosmo_boundary: int) -> int:
    if not 0 <= life_boundary <= 9 or not 0 <= cosmo_boundary <= 9:
        raise ValueError("cap boundaries must be decimal nibbles")
    return (life_boundary << 4) | cosmo_boundary


@dataclass(frozen=True)
class ResourceState:
    life: int
    cosmo: int
    seventh_sense: int
    cap_byte: int

    @property
    def life_boundary(self) -> int:
        return unpack_cap_byte(self.cap_byte)[0]

    @property
    def cosmo_boundary(self) -> int:
        return unpack_cap_byte(self.cap_byte)[1]

    @property
    def life_max(self) -> int:
        return resource_max_from_boundary(self.life_boundary)

    @property
    def cosmo_max(self) -> int:
        return resource_max_from_boundary(self.cosmo_boundary)


def spend_seventh_sense_for_resource(
    state: ResourceState, resource: Resource
) -> ResourceState:
    """Original 1 Seventh Sense -> 1 Life/Cosmo allocation rule.

    If Seventh Sense is zero or the target resource is already at its cap,
    the original menu leaves the state unchanged.
    """

    if state.seventh_sense <= 0:
        return state

    if resource is Resource.LIFE:
        if state.life >= state.life_max:
            return state
        return ResourceState(
            life=state.life + 1,
            cosmo=state.cosmo,
            seventh_sense=state.seventh_sense - 1,
            cap_byte=state.cap_byte,
        )

    if state.cosmo >= state.cosmo_max:
        return state
    return ResourceState(
        life=state.life,
        cosmo=state.cosmo + 1,
        seventh_sense=state.seventh_sense - 1,
        cap_byte=state.cap_byte,
    )


def sacrifice_resource_for_seventh_sense(
    state: ResourceState, resource: Resource
) -> ResourceState:
    """Original 1 Life/Cosmo -> 1 Seventh Sense reverse exchange rule."""

    if state.seventh_sense >= SEVENTH_SENSE_MAX:
        return state

    if resource is Resource.LIFE:
        if state.life <= 0:
            return state
        return ResourceState(
            life=state.life - 1,
            cosmo=state.cosmo,
            seventh_sense=state.seventh_sense + 1,
            cap_byte=state.cap_byte,
        )

    if state.cosmo <= 0:
        return state
    return ResourceState(
        life=state.life,
        cosmo=state.cosmo - 1,
        seventh_sense=state.seventh_sense + 1,
        cap_byte=state.cap_byte,
    )


def bcd2_to_int(value: int) -> int:
    high = (value >> 4) & 0x0F
    low = value & 0x0F
    if high > 9 or low > 9:
        raise ValueError(f"invalid packed-BCD byte: 0x{value:02X}")
    return high * 10 + low


def add_enemy_seventh_sense_reward(
    current: int, reward_bcd: int, *, mode_active: bool = True
) -> int:
    """Semantic model of fixed $D1E0: add 00..99 Seventh Sense."""

    if not 0 <= current <= SEVENTH_SENSE_MAX:
        raise ValueError("Seventh Sense out of range")
    if not mode_active:
        return current
    return min(SEVENTH_SENSE_MAX, current + bcd2_to_int(reward_bcd))


def add_scripted_seventh_sense_hundreds(current: int, reward_bcd: int) -> int:
    """Semantic model of fixed $F31E: add reward_bcd * 100.

    $F31E adds its packed-BCD argument to the upper two decimal digits of the
    four-digit Seventh Sense counter, so 0x02 -> +200 and 0x10 -> +1000.
    """

    if not 0 <= current <= SEVENTH_SENSE_MAX:
        raise ValueError("Seventh Sense out of range")
    return min(SEVENTH_SENSE_MAX, current + bcd2_to_int(reward_bcd) * 100)

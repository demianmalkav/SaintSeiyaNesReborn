"""Clean-room behavioral predicates for Kanketsu Hen platform tile descriptors.

These functions are semantic reductions of the 6502 comparisons used by the
platform player. They intentionally avoid assigning visual/design names such
as "slope" until descriptor-to-metatile identity is proven.
"""
from __future__ import annotations


def _in(v: int, lo: int, hi: int) -> bool:
    return lo <= v < hi


def blocks_ground_right(desc: int) -> bool:
    """Ordinary rightward body-side collision ($50/$51)."""
    desc &= 0xFF
    return _in(desc, 0x80, 0x88) or _in(desc, 0xE0, 0xF0)


def blocks_ground_left_lower(desc: int) -> bool:
    """Ordinary leftward lower-side collision ($53)."""
    desc &= 0xFF
    return (
        _in(desc, 0x78, 0x80)
        or _in(desc, 0x88, 0x90)
        or _in(desc, 0xE0, 0xF0)
    )


def blocks_ground_left_upper(desc: int) -> bool:
    """Ordinary leftward upper-side collision ($54)."""
    desc &= 0xFF
    return _in(desc, 0x88, 0x90) or _in(desc, 0xE0, 0xF0)


def blocks_air_right_main(desc: int) -> bool:
    """Directional-jump right lower/late-floor tests ($50/$52)."""
    return blocks_ground_right(desc)


def blocks_air_right_upper(desc: int) -> bool:
    """Directional-jump right upper-side test ($51)."""
    desc &= 0xFF
    return _in(desc, 0xE0, 0xF0)


def blocks_air_left_main(desc: int) -> bool:
    """Directional-jump left lower/late-floor tests ($53/$55)."""
    desc &= 0xFF
    return _in(desc, 0x88, 0x90) or _in(desc, 0xE0, 0xF0)


def blocks_air_left_upper(desc: int) -> bool:
    """Directional-jump left upper-side test ($54)."""
    desc &= 0xFF
    return _in(desc, 0xE0, 0xF0)


def blocks_ceiling(desc: int) -> bool:
    """Head probe $56 while rising."""
    desc &= 0xFF
    return _in(desc, 0xE0, 0xF0)


def bottom_left_push_right(desc: int) -> bool:
    """Fall/landing correction from $55: move player right by one pixel."""
    desc &= 0xFF
    return _in(desc, 0x88, 0x90) or _in(desc, 0xE0, 0xF0)


def bottom_right_push_left(desc: int) -> bool:
    """Fall/landing correction from $52: move player left by one pixel."""
    desc &= 0xFF
    return _in(desc, 0x80, 0x88) or _in(desc, 0xE0, 0xF0)


def center_floor_snap(desc: int, player_y: int, dynamic_ff_y: int | None = None) -> int | None:
    """Return the ordinary center-floor Y snap, or None for no landing.

    This models bank-3 $B8CD-$B90D for player_y < $86. The special lower-screen
    $F8/$F9 path is deliberately excluded because it also mutates hazard/event
    state and is not an ordinary landing.
    """
    desc &= 0xFF
    player_y &= 0xFF
    if desc == 0xFF:
        return None if dynamic_ff_y is None else dynamic_ff_y & 0xFF
    if desc < 0x80:
        return None
    low = player_y & 0x0F
    if desc >= 0xF0:
        if low < 0x08:
            return None
        return (player_y & 0xF0) | 0x08
    if low >= 0x06:
        return None
    return player_y & 0xF0


def behavioral_family(desc: int) -> str:
    """Neutral family label based only on observed collision behavior."""
    desc &= 0xFF
    if _in(desc, 0xE0, 0xF0):
        return "full_side_and_ceiling_block"
    if desc >= 0xF0:
        return "upper_range_floor_or_special"
    if _in(desc, 0x90, 0xE0):
        return "floor_support_without_side_block"
    if _in(desc, 0x88, 0x90):
        return "left_block_and_left_bottom_correction"
    if _in(desc, 0x80, 0x88):
        return "right_block_and_right_bottom_correction"
    if _in(desc, 0x78, 0x80):
        return "ground_left_lower_block_only"
    return "non_support_or_other"

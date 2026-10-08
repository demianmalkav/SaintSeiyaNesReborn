"""Gold-Saint counterattack dodge window reconstructed from `$9183+` / `$F936+`.

The defense sequence records a signed-like 8-bit horizontal offset in zero-page
`$AF` and a last dodge direction in `$0661`. The attack exposes one blocked/
danger direction in `$0662`, derived from `$0660 & 1`.

If the player's final movement is too short, `$0661` is cleared and the hit is
forced. If a valid dodge direction is the same as `$0662`, damage is also
applied. A valid opposite direction bypasses the damage routine entirely.
"""

from __future__ import annotations

from dataclasses import dataclass
from enum import IntEnum
from typing import Iterable


class DodgeDirection(IntEnum):
    NONE = 0x00
    LEFT = 0x01
    RIGHT = 0xFF


@dataclass(frozen=True)
class DodgeResult:
    final_offset: int
    final_direction: DodgeDirection
    valid_movement: bool
    evaded: bool


def blocked_direction_from_parity(parity_bit: int) -> DodgeDirection:
    """Mirror `$F995-$F99E`: even -> RIGHT/FF, odd -> LEFT/01."""
    if parity_bit not in (0, 1):
        raise ValueError("parity_bit must be 0 or 1")
    return DodgeDirection.LEFT if parity_bit else DodgeDirection.RIGHT


def simulate_dodge(
    inputs: Iterable[DodgeDirection | int],
    *,
    blocked_direction: DodgeDirection | int,
    initial_offset: int = 0,
) -> DodgeResult:
    """Simulate the 28-iteration dodge input window.

    Each input entry represents one polling iteration. RIGHT moves on-screen
    sprites +2 and subtracts 2 from `$AF`; LEFT moves sprites -2 and adds 2.
    The real routine accepts at most 28 iterations.
    """
    blocked = DodgeDirection(blocked_direction)
    if blocked is DodgeDirection.NONE:
        raise ValueError("blocked_direction must be LEFT or RIGHT")
    if not 0 <= initial_offset <= 0xFF:
        raise ValueError("initial_offset must fit in one byte")

    offset = initial_offset
    last = DodgeDirection.NONE
    sequence = list(inputs)
    if len(sequence) > 28:
        raise ValueError("dodge window contains at most 28 polling iterations")

    for raw in sequence:
        direction = DodgeDirection(raw)
        if direction is DodgeDirection.RIGHT:
            offset = (offset - 2) & 0xFF
            last = DodgeDirection.RIGHT
        elif direction is DodgeDirection.LEFT:
            offset = (offset + 2) & 0xFF
            last = DodgeDirection.LEFT
        elif direction is not DodgeDirection.NONE:
            raise ValueError(direction)

    # `$91C0+` validates net displacement according to the last direction.
    valid = False
    if last is DodgeDirection.LEFT:
        valid = offset >= 0x10
    elif last is DodgeDirection.RIGHT:
        valid = offset < 0xF0

    if not valid:
        last = DodgeDirection.NONE

    evaded = valid and last != blocked
    return DodgeResult(offset, last, valid, evaded)


def update_dodge_counters(successful: int, failed: int, result: DodgeResult) -> tuple[int, int]:
    """Mirror `$F9C6-$F9D6`: success -> `$0678`, otherwise -> `$0677`."""
    if successful < 0 or failed < 0:
        raise ValueError("counters cannot be negative")
    if result.evaded:
        return successful + 1, failed
    return successful, failed + 1

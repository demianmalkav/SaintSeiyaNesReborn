"""Executable ORIGINAL SPEC for Kanketsu Hen platform movement.

This module is clean-room behavioral documentation derived from the verified
Japanese ROM. It intentionally uses engine Saint indices (0..4) where a
character identity is not independently proven by the ROM.
"""

from __future__ import annotations

from dataclasses import dataclass
from enum import Enum


class JumpKind(str, Enum):
    STANDING = "standing"
    HIGH = "high"
    DIRECTIONAL = "directional"


# Player action-state families reconstructed from bank 3.
STATE_IDLE = 0x00
STATE_WALK_FAMILY = 0x10
STATE_CROUCH = 0x20
STATE_JUMP = 0x30
STATE_JUMP_RIGHT = 0x31
STATE_JUMP_LEFT = 0x32
STATE_JUMP_BOTH = 0x33
STATE_DROP = 0x50
STATE_DAMAGE_FAMILY = 0x80


# Signed vertical displacement tables. Positive means upward movement in the
# original screen coordinate system (the 6502 routine subtracts the value from Y).
STANDING_JUMP = (
    8, 8, 7, 7, 6, 5, 4, 3, 3, 2, 2, 1, 1, 1, 0, 0,
    0, 0, -1, -1, -1, -1, -2, -2, -2, -2, -2, -3, -3, -3, -3, -3,
)

HIGH_JUMPS = (
    (9,8,7,7,6,6,5,5,4,4,4,4,4,3,3,3,3,3,2,2,2,2,2,1,1,1,1,1,0,0,0,0,0,0,-1,-1,-1,-1,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-3,-3,-3,-3,-3,-3,-3,-3,-3,-3),
    (9,8,7,7,6,6,5,5,4,4,4,3,3,3,2,2,2,2,2,1,1,1,1,0,0,0,0,0,0,-1,-1,-1,-1,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-3,-3,-3,-3,-3),
    (9,7,6,6,5,5,4,4,4,4,3,3,3,2,2,2,1,1,0,0,0,0,0,0,-1,-1,-1,-1,-2,-2,-2,-2,-3,-3,-3,-3,-3,-3,-3,-3),
    (9,7,6,6,5,5,4,4,4,4,3,3,3,2,2,2,1,1,0,0,0,0,0,0,-1,-1,-1,-1,-2,-2,-2,-2,-3,-3,-3,-3,-3,-3,-3,-3),
    (9,8,7,7,6,6,5,5,4,4,4,3,3,3,2,2,2,2,2,1,1,1,1,0,0,0,0,0,0,-1,-1,-1,-1,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-2,-3,-3,-3,-3,-3),
)

DIRECTIONAL_JUMPS = (
    (4,3,2,2,2,2,2,2,2,2,2,2,1,1,1,1,1,1,1,1,1,1,1,0,1,0,0,0,0,0,0,-1,-1,-1,-1,-2,-2,-2,-2,-2,-2,-2,-2,-3,-3,-3,-3,-3,-3,-3,-3,-3,-3,-3),
    (4,3,2,2,2,2,2,2,2,2,2,2,1,1,1,1,1,1,0,0,0,0,0,0,-1,-1,-1,-1,-2,-2,-2,-2,-2,-2,-3,-3,-3,-3,-3,-3),
    (4,3,2,2,2,2,2,2,2,2,2,2,1,1,1,1,1,1,0,1,0,0,0,0,0,0,-1,-1,-1,-1,-2,-2,-2,-2,-2,-2,-3,-3,-3,-3,-3,-3,-3,-3),
    (4,3,2,2,2,2,2,2,2,2,2,2,1,1,1,1,1,1,0,1,0,0,0,0,0,0,-1,-1,-1,-1,-2,-2,-2,-2,-2,-2,-3,-3,-3,-3,-3,-3,-3,-3),
    (4,3,2,2,2,2,2,2,2,2,2,2,1,1,1,1,1,1,1,1,1,1,1,0,1,0,0,0,0,0,0,-1,-1,-1,-1,-2,-2,-2,-2,-2,-2,-2,-2,-3,-3,-3,-3,-3,-3,-3,-3,-3,-3,-3),
)


@dataclass(frozen=True)
class ProbePoint:
    x: int
    y: int


@dataclass(frozen=True)
class CollisionProbes:
    head_center: ProbePoint
    upper_left: ProbePoint
    upper_right: ProbePoint
    lower_left: ProbePoint
    lower_right: ProbePoint
    ground_left: ProbePoint
    ground_center: ProbePoint
    ground_right: ProbePoint


def movement_increments(engine_saint_index: int, frame_parity: int) -> tuple[int, int, int]:
    """Return the original `$0387/$0388/$0389` movement increments.

    `frame_parity` is the low bit used by the original engine and must be 0 or 1.
    """
    if engine_saint_index not in range(5):
        raise ValueError("engine_saint_index must be 0..4")
    if frame_parity not in (0, 1):
        raise ValueError("frame_parity must be 0 or 1")

    alternating = frame_parity + 1  # 1,2
    if engine_saint_index == 1:
        return alternating, 2, 1
    return 1, alternating, frame_parity


def select_jump_curve(engine_saint_index: int, kind: JumpKind) -> tuple[int, ...]:
    if engine_saint_index not in range(5):
        raise ValueError("engine_saint_index must be 0..4")
    if kind is JumpKind.STANDING:
        return STANDING_JUMP
    if kind is JumpKind.HIGH:
        return HIGH_JUMPS[engine_saint_index]
    if kind is JumpKind.DIRECTIONAL:
        return DIRECTIONAL_JUMPS[engine_saint_index]
    raise ValueError(kind)


def jump_positions(start_y: int, engine_saint_index: int, kind: JumpKind) -> tuple[int, ...]:
    """Return Y after every table-controlled jump frame.

    NES screen Y increases downward, so positive table entries are subtracted.
    """
    y = start_y
    out: list[int] = []
    for delta in select_jump_curve(engine_saint_index, kind):
        y -= delta
        out.append(y)
    return tuple(out)


def maximum_rise(engine_saint_index: int, kind: JumpKind) -> int:
    positions = jump_positions(0, engine_saint_index, kind)
    return -min(positions)


def collision_probe_points(player_x: int, player_y: int, scroll_x: int, *, special_y_88: bool | None = None) -> CollisionProbes:
    """Reconstruct the eight map-sample points used by the platform engine.

    Coordinates are expressed in world-space pixels before conversion to the
    game's 16-pixel map-cell addressing. The lower-side row uses a +16 offset,
    except at player Y `$88`, where the ROM uses +24.
    """
    world_x = scroll_x + player_x
    aligned_y = (player_y + 8) & 0xF0
    if special_y_88 is None:
        special_y_88 = (player_y & 0xFF) == 0x88
    lower_offset = 24 if special_y_88 else 16

    return CollisionProbes(
        head_center=ProbePoint(world_x + 8, aligned_y),
        upper_left=ProbePoint(world_x + 0, aligned_y),
        upper_right=ProbePoint(world_x + 16, aligned_y),
        lower_left=ProbePoint(world_x + 0, aligned_y + lower_offset),
        lower_right=ProbePoint(world_x + 16, aligned_y + lower_offset),
        ground_left=ProbePoint(world_x - 8, player_y + 32),
        ground_center=ProbePoint(world_x + 8, player_y + 32),
        ground_right=ProbePoint(world_x + 24, player_y + 32),
    )

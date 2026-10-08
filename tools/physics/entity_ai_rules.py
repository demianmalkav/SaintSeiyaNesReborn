"""Clean-room common entity AI primitives for Kanketsu Hen platform mode.

No ROM payload is embedded. Constants and predicates are semantic reductions of
reverse-engineered 6502 behavior documented in PLATFORM_ENTITY_AI.md.
"""
from __future__ import annotations


FACING_RIGHT = 0x40


def horizontal_step(entity_type: int, frame_counter: int) -> int:
    """Return the common horizontal pixel step for one platform update."""
    entity_type &= 0xFF
    frame_counter &= 0xFF
    if (entity_type & 0xFE) == 0x0A:
        return frame_counter & 1
    return 2 if (frame_counter & 3) == 0 else 1


def reseed_decision_timer(entropy_byte: int) -> int:
    """Mirror $AA64: common mobile-entity decision interval, 31..94."""
    return (entropy_byte & 0x3F) + 0x1F


def facing_right(flags: int) -> bool:
    return bool(flags & FACING_RIGHT)


def toggle_facing(flags: int) -> int:
    return (flags ^ FACING_RIGHT) & 0xFF


def terrain_forces_turn(entity_type: int, flags: int, descriptor: int) -> bool:
    """Mirror the terrain-facing decision at bank 3 $AA1F+.

    Offset $0A is the right-facing probe and $0B the left-facing probe. The
    caller is responsible for supplying the descriptor from the active side.
    """
    entity_type &= 0xFF
    descriptor &= 0xFF

    # Type 7 has an explicit early-out for $E4+.
    if entity_type == 0x07 and descriptor >= 0xE4:
        return False

    if descriptor >= 0xF0:
        return False

    if facing_right(flags):
        return (0x80 <= descriptor < 0x88) or (0xE0 <= descriptor < 0xF0)

    return (0x88 <= descriptor < 0x90) or (0xE0 <= descriptor < 0xF0)


def state30_horizontal_delta(action_state: int, entity_type: int, frame_counter: int) -> int:
    """Horizontal delta for the confirmed directional jump states.

    $31 moves +X/right, $32 moves -X/left. Other $3x values have no confirmed
    common horizontal displacement in this path.
    """
    step = horizontal_step(entity_type, frame_counter)
    if action_state == 0x31:
        return step
    if action_state == 0x32:
        return -step
    return 0


# Signed displacement sequence consumed by the common entity jump before the
# fixed +3 px fall phase. Positive source values move upward because the ROM
# subtracts them from screen Y.
ENTITY_JUMP_SOURCE = (
    8, 8, 7, 7, 6, 5, 4, 3, 3, 2, 2, 1, 1, 1,
    0, 0, 0, 0, -1, -1, -1, -1, -2, -2, -2, -2, -2, -3, -3, -3,
)


def entity_jump_screen_deltas() -> tuple[int, ...]:
    """Per-update screen-Y deltas for the table-controlled common jump phase."""
    return tuple(-v for v in ENTITY_JUMP_SOURCE)


def jump_profile_metrics() -> dict[str, int]:
    y = 0
    minimum = 0
    apex_index = 0
    for i, dy in enumerate(entity_jump_screen_deltas(), start=1):
        y += dy
        if y < minimum:
            minimum = y
            apex_index = i
    return {
        "table_updates": len(ENTITY_JUMP_SOURCE),
        "max_ascent_px": -minimum,
        "apex_update": apex_index,
        "net_y_after_table": y,
        "post_table_fall_px_per_update": 3,
        "decision_timer_min": reseed_decision_timer(0),
        "decision_timer_max": reseed_decision_timer(0xFF),
    }


def _self_test() -> None:
    assert horizontal_step(1, 0) == 2
    assert horizontal_step(1, 1) == 1
    assert horizontal_step(0x0A, 0) == 0
    assert horizontal_step(0x0A, 1) == 1
    assert reseed_decision_timer(0) == 31
    assert reseed_decision_timer(0x3F) == 94
    assert terrain_forces_turn(1, 0x00, 0x88)
    assert terrain_forces_turn(1, 0x40, 0x80)
    assert terrain_forces_turn(1, 0x40, 0xE0)
    assert not terrain_forces_turn(1, 0x40, 0x90)
    assert not terrain_forces_turn(7, 0x40, 0xE4)
    metrics = jump_profile_metrics()
    assert metrics["max_ascent_px"] == 58
    assert metrics["decision_timer_min"] == 31
    assert metrics["decision_timer_max"] == 94
    print(metrics)


if __name__ == "__main__":
    _self_test()

#!/usr/bin/env python3
"""Executable clean-room reference model for Kanketsu Hen platform stages.

Consumes JSON produced locally by export_platform_stage_spec.py --include-grids.
The repository contains no extracted stage data. This module provides semantic
queries used by future parity tests and the native REBORN implementation.
"""
from __future__ import annotations

import argparse
import json
from dataclasses import dataclass
from pathlib import Path

CELL = 16
PAGE_W = 256
GRID_ROWS = 11
GRID_COLS = 16


@dataclass(frozen=True)
class ProbeSet:
    upper_left: int | None
    upper_center: int | None
    upper_right: int | None
    lower_left: int | None
    lower_right: int | None
    bottom_left: int | None
    bottom_center: int | None
    bottom_right: int | None


def _between(v: int | None, lo: int, hi: int) -> bool:
    return v is not None and lo <= v < hi


def blocks_right(desc: int | None, airborne_upper: bool = False) -> bool:
    if desc is None:
        return False
    if airborne_upper:
        return 0xE0 <= desc < 0xF0
    return (0x80 <= desc < 0x88) or (0xE0 <= desc < 0xF0)


def blocks_left(desc: int | None, upper: bool = False, airborne: bool = False) -> bool:
    if desc is None:
        return False
    if upper or airborne:
        return (0x88 <= desc < 0x90 and not airborne) or (0xE0 <= desc < 0xF0)
    return (
        (0x78 <= desc < 0x80)
        or (0x88 <= desc < 0x90)
        or (0xE0 <= desc < 0xF0)
    )


def blocks_ceiling(desc: int | None) -> bool:
    return desc is not None and 0xE0 <= desc < 0xF0


def center_floor_snap(desc: int | None, player_y: int) -> int | None:
    """Ordinary player landing snap for Y < $86; specials excluded."""
    if desc is None or desc == 0xFF or desc < 0x80:
        return None
    low = player_y & 0x0F
    if desc >= 0xF0:
        return ((player_y & 0xF0) | 0x08) if low >= 8 else None
    return (player_y & 0xF0) if low < 6 else None


class StageView:
    def __init__(self, spec: dict, substate: int):
        entries = {int(s["substate"]): s for s in spec["substates"]}
        if substate not in entries:
            raise KeyError(f"substate {substate:02X} not present")
        self.substate = entries[substate]
        self.pages = self.substate["pages"]
        if any("metatile_rows_11x16" not in p for p in self.pages):
            raise ValueError(
                "stage spec has no grids; regenerate with export_platform_stage_spec.py --include-grids"
            )

    @property
    def width_px(self) -> int:
        return len(self.pages) * PAGE_W

    def descriptor(self, world_x: int, world_y: int) -> int | None:
        if world_x < 0 or world_y < 0:
            return None
        page_index = world_x // PAGE_W
        if page_index >= len(self.pages):
            return None
        col = (world_x % PAGE_W) // CELL
        row = world_y // CELL
        if not (0 <= row < GRID_ROWS and 0 <= col < GRID_COLS):
            return None
        return int(self.pages[page_index]["metatile_rows_11x16"][row][col])

    def page_metadata_at_world_x(self, world_x: int) -> dict | None:
        if world_x < 0:
            return None
        page = world_x // PAGE_W
        return self.pages[page] if page < len(self.pages) else None

    def probes(self, player_x: int, player_y: int, scroll_x: int) -> ProbeSet:
        """Reproduce the eight platform environment sample positions.

        player_x is screen-local; world X = scroll_x + player_x.
        """
        x = scroll_x + player_x
        t = (player_y + 8) & 0xF0
        lower_offset = 24 if player_y == 0x88 else 16
        return ProbeSet(
            upper_left=self.descriptor(x + 0, t),
            upper_center=self.descriptor(x + 8, t),
            upper_right=self.descriptor(x + 16, t),
            lower_left=self.descriptor(x + 0, t + lower_offset),
            lower_right=self.descriptor(x + 16, t + lower_offset),
            bottom_left=self.descriptor(x - 8, player_y + 32),
            bottom_center=self.descriptor(x + 8, player_y + 32),
            bottom_right=self.descriptor(x + 24, player_y + 32),
        )

    def can_move_right(self, probes: ProbeSet, airborne: bool = False) -> bool:
        if airborne:
            return not (
                blocks_right(probes.lower_right)
                or blocks_right(probes.upper_right, airborne_upper=True)
            )
        return not (
            blocks_right(probes.lower_right)
            or blocks_right(probes.upper_right)
        )

    def can_move_left(self, probes: ProbeSet, airborne: bool = False) -> bool:
        if airborne:
            return not (
                blocks_left(probes.lower_left, airborne=True)
                or blocks_left(probes.upper_left, airborne=True)
            )
        return not (
            blocks_left(probes.lower_left)
            or blocks_left(probes.upper_left, upper=True)
        )

    def encounter(self, world_x: int) -> dict | None:
        page = self.page_metadata_at_world_x(world_x)
        if page is None:
            return None
        return {
            "page_index": page["page_index"],
            "primary": page["primary_encounter"],
            "secondary": page["secondary_archetype"],
        }


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("spec", type=Path)
    ap.add_argument("--substate", type=lambda s: int(s, 0), default=0)
    ap.add_argument("--player-x", type=int, default=0x70)
    ap.add_argument("--player-y", type=int, default=0x70)
    ap.add_argument("--scroll-x", type=int, default=0)
    args = ap.parse_args()

    spec = json.loads(args.spec.read_text(encoding="utf-8"))
    stage = StageView(spec, args.substate)
    probes = stage.probes(args.player_x, args.player_y, args.scroll_x)
    world_x = args.player_x + args.scroll_x
    print(f"substate=${args.substate:02X} width={stage.width_px}px world_x={world_x}")
    print("encounter:", json.dumps(stage.encounter(world_x), ensure_ascii=False))
    print("probes:", probes)
    print("can_move_left:", stage.can_move_left(probes))
    print("can_move_right:", stage.can_move_right(probes))
    print("floor_snap:", center_floor_snap(probes.bottom_center, args.player_y))


if __name__ == "__main__":
    main()

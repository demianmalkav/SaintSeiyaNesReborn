#!/usr/bin/env python3
"""Extract Kanketsu Hen platform page composition from a user-owned ROM.

No ROM payload is embedded. The tool verifies the canonical core CRC32 and
emits structural JSON. By default it reports page reuse only; --include-grid
adds the 16x11 metatile-index grids for local analysis.
"""
from __future__ import annotations

import argparse
import json
import zlib
from pathlib import Path

CANONICAL_CORE_CRC32 = 0x9561798D
PRG_BANK_SIZE = 0x4000
MAIN_STAGE_COUNT = 12
MAIN_METATILE_BASE = 0x8000
MAIN_PAGE0_ATTR = 0x8400
MAIN_PAGE0_GRID = 0x8440
PAGE_BLOCK_SIZE = 0xF0
PAGE_ATTR_SIZE = 0x40
PAGE_GRID_SIZE = 0xB0
GRID_COLUMNS = 16
GRID_ROWS = 11


def load_prg(path: Path, force: bool = False) -> list[bytes]:
    rom = path.read_bytes()
    if len(rom) < 16 or rom[:4] != b"NES\x1a":
        raise SystemExit("not an iNES ROM")
    trainer = 512 if rom[6] & 0x04 else 0
    core = rom[16 + trainer :]
    crc = zlib.crc32(core) & 0xFFFFFFFF
    if crc != CANONICAL_CORE_CRC32 and not force:
        raise SystemExit(
            f"unexpected core CRC32 {crc:08X}; expected {CANONICAL_CORE_CRC32:08X}"
        )
    start = 16 + trainer
    prg = rom[start : start + rom[4] * PRG_BANK_SIZE]
    return [prg[i : i + PRG_BANK_SIZE] for i in range(0, len(prg), PRG_BANK_SIZE)]


def read_cpu(banks: list[bytes], bank: int, addr: int, size: int) -> bytes:
    base = 0xC000 if bank == 7 else 0x8000
    if not base <= addr < base + 0x4000:
        raise ValueError(f"address ${addr:04X} outside bank-{bank} CPU window")
    off = addr - base
    return banks[bank][off : off + size]


def u16(raw: bytes) -> int:
    return raw[0] | (raw[1] << 8)


def pointer_table_for_substate(banks: list[bytes], substate: int) -> int:
    return u16(read_cpu(banks, 7, 0xCFFA + substate * 2, 2))


def main_stage_page_count(substate: int) -> int:
    # Fixed pointer-list boundaries prove growth from 6 through 16 pages;
    # the last two main substates both use 16 pages.
    if not 0 <= substate < MAIN_STAGE_COUNT:
        raise ValueError("main-stage page-count rule only applies to substates 0..11")
    return min(6 + substate, 16)


def page_pool_id(grid_pointer: int) -> int | None:
    delta = grid_pointer - MAIN_PAGE0_GRID
    if delta < 0 or delta % PAGE_BLOCK_SIZE:
        return None
    return delta // PAGE_BLOCK_SIZE


def grid_columns(raw: bytes) -> list[list[int]]:
    if len(raw) != PAGE_GRID_SIZE:
        raise ValueError("platform page grid must contain 176 bytes")
    return [
        list(raw[col * GRID_ROWS : (col + 1) * GRID_ROWS])
        for col in range(GRID_COLUMNS)
    ]


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("rom", type=Path)
    ap.add_argument("-o", "--output", type=Path)
    ap.add_argument("--include-grid", action="store_true")
    ap.add_argument("--include-metatiles", action="store_true")
    ap.add_argument("--force", action="store_true")
    args = ap.parse_args()

    banks = load_prg(args.rom, args.force)
    stages = []
    for substate in range(MAIN_STAGE_COUNT):
        list_addr = pointer_table_for_substate(banks, substate)
        page_count = main_stage_page_count(substate)
        pages = []
        for page_index in range(page_count):
            grid_ptr = u16(read_cpu(banks, 7, list_addr + page_index * 2, 2))
            raw_grid = read_cpu(banks, 2, grid_ptr, PAGE_GRID_SIZE)
            page = {
                "page_index": page_index,
                "grid_pointer": f"0x{grid_ptr:04X}",
                "attribute_pointer": f"0x{grid_ptr - PAGE_ATTR_SIZE:04X}",
                "pool_page_id": page_pool_id(grid_ptr),
            }
            if args.include_grid:
                page["grid_column_major_16x11"] = grid_columns(raw_grid)
            pages.append(page)
        stages.append(
            {
                "platform_substate_02": f"0x{substate:02X}",
                "story_progress_normal": substate,
                "page_pointer_list": f"0x{list_addr:04X}",
                "page_count": page_count,
                "pages": pages,
            }
        )

    payload: dict[str, object] = {
        "format": "SaintSeiyaNesReborn.PlatformMapComposition.v1",
        "source_core_crc32": f"{CANONICAL_CORE_CRC32:08X}",
        "layout": {
            "metatile_definition_base": f"0x{MAIN_METATILE_BASE:04X}",
            "metatile_definition_count": 256,
            "bytes_per_metatile_definition": 4,
            "page_block_size": PAGE_BLOCK_SIZE,
            "attribute_bytes_per_page": PAGE_ATTR_SIZE,
            "grid_bytes_per_page": PAGE_GRID_SIZE,
            "grid_columns": GRID_COLUMNS,
            "grid_rows": GRID_ROWS,
            "grid_storage": "column-major",
            "first_page_attribute_pointer": f"0x{MAIN_PAGE0_ATTR:04X}",
            "first_page_grid_pointer": f"0x{MAIN_PAGE0_GRID:04X}",
        },
        "main_stages": stages,
    }

    if args.include_metatiles:
        defs = read_cpu(banks, 2, MAIN_METATILE_BASE, 256 * 4)
        payload["metatile_definitions"] = [
            list(defs[i * 4 : i * 4 + 4]) for i in range(256)
        ]

    text = json.dumps(payload, indent=2, ensure_ascii=False) + "\n"
    if args.output:
        args.output.write_text(text, encoding="utf-8")
    else:
        print(text, end="")


if __name__ == "__main__":
    main()

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
PLATFORM_SUBSTATE_COUNT = 18  # $00-$11
MAIN_STAGE_COUNT = 12
PAGE_POINTER_TABLE = 0xCFFA
SPECIAL_METATILE_BASE_TABLE = 0xD1D4
PAGE_POINTER_LIST_END = 0xD19A
PAGE_BLOCK_SIZE = 0xF0
PAGE_ATTR_SIZE = 0x40
PAGE_GRID_SIZE = 0xB0
METATILE_TABLE_SIZE = 0x400
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


def all_pointer_list_addresses(banks: list[bytes]) -> list[int]:
    return [
        u16(read_cpu(banks, 7, PAGE_POINTER_TABLE + i * 2, 2))
        for i in range(PLATFORM_SUBSTATE_COUNT)
    ]


def page_count_from_list_boundaries(banks: list[bytes], list_addr: int) -> int:
    # All 18 page-pointer lists occupy the fixed-bank interval ending exactly
    # where routine $D19A begins. Sorting their starts recovers each list length.
    starts = sorted(all_pointer_list_addresses(banks))
    boundaries = starts + [PAGE_POINTER_LIST_END]
    idx = starts.index(list_addr)
    length = boundaries[idx + 1] - list_addr
    if length <= 0 or length & 1:
        raise ValueError(f"invalid page-pointer-list boundary at ${list_addr:04X}")
    return length // 2


def data_bank_for_substate(substate: int) -> int:
    # Fixed $CED3 selects bank 3 for $10, bank 1 for $11, bank 2 otherwise.
    if substate == 0x10:
        return 3
    if substate == 0x11:
        return 1
    return 2


def metatile_base_for_substate(banks: list[bytes], substate: int) -> int:
    # Fixed $D1B6 uses $8000 for $00-$0B and a six-entry table for $0C-$11.
    if substate < 0x0C:
        return 0x8000
    return u16(
        read_cpu(
            banks,
            7,
            SPECIAL_METATILE_BASE_TABLE + (substate - 0x0C) * 2,
            2,
        )
    )


def page_pool_id(grid_pointer: int, metatile_base: int) -> int | None:
    first_grid = metatile_base + METATILE_TABLE_SIZE + PAGE_ATTR_SIZE
    delta = grid_pointer - first_grid
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
    pointer_lists = all_pointer_list_addresses(banks)
    substates = []

    for substate in range(PLATFORM_SUBSTATE_COUNT):
        bank = data_bank_for_substate(substate)
        metatile_base = metatile_base_for_substate(banks, substate)
        list_addr = pointer_lists[substate]
        page_count = page_count_from_list_boundaries(banks, list_addr)
        pages = []
        for page_index in range(page_count):
            grid_ptr = u16(read_cpu(banks, 7, list_addr + page_index * 2, 2))
            raw_grid = read_cpu(banks, bank, grid_ptr, PAGE_GRID_SIZE)
            page = {
                "page_index": page_index,
                "grid_pointer": f"0x{grid_ptr:04X}",
                "attribute_pointer": f"0x{grid_ptr - PAGE_ATTR_SIZE:04X}",
                "pool_page_id": page_pool_id(grid_ptr, metatile_base),
            }
            if args.include_grid:
                page["grid_column_major_16x11"] = grid_columns(raw_grid)
            pages.append(page)

        entry: dict[str, object] = {
            "platform_substate_02": f"0x{substate:02X}",
            "data_prg_bank": bank,
            "metatile_definition_base": f"0x{metatile_base:04X}",
            "page_pointer_list": f"0x{list_addr:04X}",
            "page_count": page_count,
            "pages": pages,
        }
        if substate < MAIN_STAGE_COUNT:
            entry["normal_story_progress"] = substate

        if args.include_metatiles:
            defs = read_cpu(banks, bank, metatile_base, 256 * 4)
            entry["metatile_definitions"] = [
                list(defs[i * 4 : i * 4 + 4]) for i in range(256)
            ]
        substates.append(entry)

    payload = {
        "format": "SaintSeiyaNesReborn.PlatformMapComposition.v2",
        "source_core_crc32": f"{CANONICAL_CORE_CRC32:08X}",
        "layout": {
            "metatile_definition_count": 256,
            "bytes_per_metatile_definition": 4,
            "page_block_size": PAGE_BLOCK_SIZE,
            "attribute_bytes_per_page": PAGE_ATTR_SIZE,
            "grid_bytes_per_page": PAGE_GRID_SIZE,
            "grid_columns": GRID_COLUMNS,
            "grid_rows": GRID_ROWS,
            "grid_storage": "column-major",
            "first_grid_offset_from_metatile_base": METATILE_TABLE_SIZE + PAGE_ATTR_SIZE,
        },
        "platform_substates": substates,
    }

    text = json.dumps(payload, indent=2, ensure_ascii=False) + "\n"
    if args.output:
        args.output.write_text(text, encoding="utf-8")
    else:
        print(text, end="")


if __name__ == "__main__":
    main()

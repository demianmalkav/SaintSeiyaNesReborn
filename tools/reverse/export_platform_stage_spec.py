#!/usr/bin/env python3
"""Export a unified Kanketsu Hen platform-stage specification from a user ROM.

The generated JSON is a private/local analysis artifact. This tool contains no
ROM payload. It combines geometry, CHR-bank selection, primary encounter
configuration and secondary hazard/object schedule page-by-page.
"""
from __future__ import annotations

import argparse
import json
import zlib
from pathlib import Path

CANONICAL_CORE_CRC32 = 0x9561798D
PRG_BANK_SIZE = 0x4000
SUBSTATE_COUNT = 18
PAGE_LIST_TABLE = 0xCFFA
PAGE_LIST_END = 0xD19A
METATILE_BASE_TABLE = 0xD1D4
PRIMARY_PTR_TABLE = 0x9AE5
PRIMARY_CONFIG = 0x9A35
SECONDARY_PTR_TABLE = 0x9C24
SECONDARY_ARCHETYPES = 0x9C92
CHR0_TABLE = 0xCACF
CHR1_TABLE = 0xCABD
PAGE_GRID_SIZE = 0xB0
PAGE_ATTR_SIZE = 0x40
PAGE_BLOCK_SIZE = 0xF0
GRID_COLS = 16
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


def read(banks: list[bytes], bank: int, addr: int, n: int) -> bytes:
    base = 0xC000 if bank == 7 else 0x8000
    off = addr - base
    if off < 0 or off + n > 0x4000:
        raise ValueError(f"${addr:04X}+{n} outside bank {bank}")
    return banks[bank][off : off + n]


def u16(raw: bytes) -> int:
    return raw[0] | (raw[1] << 8)


def u16_table(banks: list[bytes], bank: int, addr: int, count: int) -> list[int]:
    raw = read(banks, bank, addr, count * 2)
    return [u16(raw[i * 2 : i * 2 + 2]) for i in range(count)]


def data_bank(substate: int) -> int:
    if substate == 0x10:
        return 3
    if substate == 0x11:
        return 1
    return 2


def metatile_base(banks: list[bytes], substate: int) -> int:
    if substate < 0x0C:
        return 0x8000
    return u16(read(banks, 7, METATILE_BASE_TABLE + (substate - 0x0C) * 2, 2))


def grid_to_rows(raw: bytes) -> list[list[int]]:
    # ROM storage is column-major; JSON rows are friendlier for simulation.
    return [
        [raw[col * GRID_ROWS + row] for col in range(GRID_COLS)]
        for row in range(GRID_ROWS)
    ]


def bcd(value: int) -> int:
    return ((value >> 4) & 0xF) * 10 + (value & 0xF)


def primary_config(banks: list[bytes], type_id: int, tier: int) -> dict | None:
    if not 5 <= type_id <= 0x0F:
        return None
    off = (type_id - 5) * 16 + tier * 4
    hp, cosmo, life, reward = read(banks, 1, PRIMARY_CONFIG + off, 4)
    return {
        "hp": hp,
        "cosmo_drain_ticks": cosmo,
        "life_drain_ticks": life,
        "seventh_sense_reward": bcd(reward),
    }


def decode_primary(banks: list[bytes], raw: int) -> dict:
    type_id = raw & 0x0F
    tier = (raw >> 4) & 3
    return {
        "raw": raw,
        "type": type_id,
        "tier": tier,
        "second_common_slot_enabled": bool(raw & 0x80),
        "bit6_unknown": bool(raw & 0x40),
        "config": primary_config(banks, type_id, tier),
    }


def secondary_archetype(banks: list[bytes], archetype: int) -> dict | None:
    if archetype == 0:
        return None
    hp, cosmo, life, reward = read(banks, 1, SECONDARY_ARCHETYPES + archetype * 4, 4)
    return {
        "id": archetype,
        "hp": hp,
        "cosmo_drain_ticks": cosmo,
        "life_drain_ticks": life,
        "seventh_sense_reward": bcd(reward),
    }


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("rom", type=Path)
    ap.add_argument("-o", "--output", type=Path, required=True)
    ap.add_argument("--include-grids", action="store_true")
    ap.add_argument("--force", action="store_true")
    args = ap.parse_args()

    banks = load_prg(args.rom, args.force)
    page_lists = u16_table(banks, 7, PAGE_LIST_TABLE, SUBSTATE_COUNT)
    sorted_starts = sorted(page_lists)
    boundaries = sorted_starts + [PAGE_LIST_END]
    page_counts = {
        start: (boundaries[i + 1] - start) // 2
        for i, start in enumerate(sorted_starts)
    }
    primary_ptrs = u16_table(banks, 1, PRIMARY_PTR_TABLE, SUBSTATE_COUNT)
    secondary_ptrs = u16_table(banks, 1, SECONDARY_PTR_TABLE, SUBSTATE_COUNT)
    chr0 = list(read(banks, 7, CHR0_TABLE, SUBSTATE_COUNT))
    chr1 = list(read(banks, 7, CHR1_TABLE, SUBSTATE_COUNT))

    substates = []
    for s in range(SUBSTATE_COUNT):
        bank = data_bank(s)
        base = metatile_base(banks, s)
        first_grid = base + 0x440
        count = page_counts[page_lists[s]]
        pages = []
        for page_index in range(count):
            grid_ptr = u16(read(banks, 7, page_lists[s] + page_index * 2, 2))
            grid = read(banks, bank, grid_ptr, PAGE_GRID_SIZE)
            primary_raw = read(banks, 1, primary_ptrs[s] + page_index, 1)[0]
            secondary_raw = read(banks, 1, secondary_ptrs[s] + page_index, 1)[0] & 0x07
            delta = grid_ptr - first_grid
            page = {
                "page_index": page_index,
                "grid_pointer": grid_ptr,
                "attribute_pointer": grid_ptr - PAGE_ATTR_SIZE,
                "pool_page_id": delta // PAGE_BLOCK_SIZE if delta >= 0 and delta % PAGE_BLOCK_SIZE == 0 else None,
                "primary_encounter": decode_primary(banks, primary_raw),
                "secondary_archetype": secondary_archetype(banks, secondary_raw),
            }
            if args.include_grids:
                page["metatile_rows_11x16"] = grid_to_rows(grid)
            pages.append(page)

        substates.append(
            {
                "substate": s,
                "data_prg_bank": bank,
                "metatile_definition_base": base,
                "chr0_sprite_bank_4k": chr0[s],
                "chr1_background_bank_4k": chr1[s],
                "page_count": count,
                "pages": pages,
            }
        )

    payload = {
        "format": "SaintSeiyaNesReborn.PlatformStageSpec.v1",
        "source_core_crc32": f"{CANONICAL_CORE_CRC32:08X}",
        "cell_size_px": 16,
        "page_size_cells": [16, 11],
        "page_size_px": [256, 176],
        "substates": substates,
    }
    args.output.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"wrote {args.output}")


if __name__ == "__main__":
    main()

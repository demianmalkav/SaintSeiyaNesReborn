#!/usr/bin/env python3
"""Audit Kanketsu Hen platform sprite-definition resources from a user-owned ROM.

The tool embeds no original graphics. It verifies the canonical ROM core CRC32,
reads the proven platform CHR0 selector, enumerates the shared bank-3 metasprite
pointer families for primary entity types $05-$0F, validates each definition,
and inventories separately selected attached/$9B93 visual resources.

The JSON output contains addresses, counts and selector metadata only; no CHR or
ROM payload bytes are emitted.
"""
from __future__ import annotations

import argparse
import json
import zlib
from pathlib import Path
from typing import Any

CANONICAL_CORE_CRC32 = 0x9561798D
PRG_BANK_SIZE = 0x4000
CHR4K_SIZE = 0x1000

PRIMARY_TYPES = tuple(range(0x05, 0x10))
POINTER_TABLES = (
    0xB671,
    0xB699,
    0xB6C1,
    0xB6E9,
    0xB711,
    0xB739,
    0xB761,
    0xB789,
    0xB7B1,
    0xB7D9,
    0xB801,
)
DIRECT_BLANK_DEFINITION = 0xB647


def load_rom(path: Path, force: bool = False) -> tuple[list[bytes], list[bytes]]:
    rom = path.read_bytes()
    if len(rom) < 16 or rom[:4] != b"NES\x1a":
        raise SystemExit("not an iNES ROM")

    trainer = 512 if rom[6] & 0x04 else 0
    core = rom[16 + trainer :]
    crc = zlib.crc32(core) & 0xFFFFFFFF
    if crc != CANONICAL_CORE_CRC32 and not force:
        raise SystemExit(
            f"unexpected core CRC32 {crc:08X}; expected {CANONICAL_CORE_CRC32:08X}; "
            "use --force only for intentional revision comparison"
        )

    prg_count = rom[4]
    chr8_count = rom[5]
    start = 16 + trainer
    prg = rom[start : start + prg_count * PRG_BANK_SIZE]
    chr_data = rom[
        start + prg_count * PRG_BANK_SIZE :
        start + prg_count * PRG_BANK_SIZE + chr8_count * 0x2000
    ]
    prg_banks = [prg[i : i + PRG_BANK_SIZE] for i in range(0, len(prg), PRG_BANK_SIZE)]
    chr4k = [chr_data[i : i + CHR4K_SIZE] for i in range(0, len(chr_data), CHR4K_SIZE)]
    return prg_banks, chr4k


def switched(prg: list[bytes], bank: int, cpu_addr: int, n: int) -> bytes:
    if not 0x8000 <= cpu_addr <= 0xBFFF:
        raise ValueError(f"switch-bank CPU address outside $8000-$BFFF: ${cpu_addr:04X}")
    raw = prg[bank][cpu_addr - 0x8000 : cpu_addr - 0x8000 + n]
    if len(raw) != n:
        raise ValueError(f"read crosses PRG bank {bank} at ${cpu_addr:04X}")
    return raw


def fixed(prg: list[bytes], cpu_addr: int, n: int) -> bytes:
    if not 0xC000 <= cpu_addr <= 0xFFFF:
        raise ValueError(f"fixed-bank CPU address outside $C000-$FFFF: ${cpu_addr:04X}")
    raw = prg[7][cpu_addr - 0xC000 : cpu_addr - 0xC000 + n]
    if len(raw) != n:
        raise ValueError(f"read crosses fixed PRG bank at ${cpu_addr:04X}")
    return raw


def u16le(raw: bytes) -> int:
    if len(raw) != 2:
        raise ValueError("u16le requires exactly two bytes")
    return raw[0] | (raw[1] << 8)


def parse_metasprite(prg: list[bytes], address: int) -> dict[str, Any]:
    if not 0x8000 <= address <= 0xBFFF:
        raise ValueError(f"metasprite pointer outside bank-3 window: ${address:04X}")

    # Parsing is bounded by the physical switch-bank window; no cross-bank reads.
    available = 0xC000 - address
    raw = switched(prg, 3, address, available)
    if not raw:
        raise ValueError(f"empty metasprite definition at ${address:04X}")

    count = raw[0]
    if count == 0:
        raise ValueError(f"zero-sprite definition at ${address:04X}")

    p = 1
    override_count = 0
    tiles: list[int] = []
    for index in range(count):
        if p >= len(raw):
            raise ValueError(f"truncated metasprite ${address:04X} before entry {index}")
        if raw[p] == 0xFF:
            if p + 1 >= len(raw):
                raise ValueError(f"truncated attribute override in ${address:04X}")
            override_count += 1
            p += 2
        if p + 2 >= len(raw):
            raise ValueError(f"truncated tile/Y/X record in ${address:04X}")
        tile = raw[p]
        # Every tile byte must resolve wholly inside one selected 4 KiB CHR bank.
        if tile * 16 + 16 > CHR4K_SIZE:
            raise ValueError(f"tile ${tile:02X} escapes 4 KiB CHR bank")
        tiles.append(tile)
        p += 3

    return {
        "address": f"{address:04X}",
        "sprite_count": count,
        "encoded_length": p,
        "attribute_override_count": override_count,
        "tile_min": min(tiles),
        "tile_max": max(tiles),
    }


def visual_capacity(entity_type: int) -> int:
    return 12 if entity_type == 0x0D else 11


def audit(prg: list[bytes], chr4k: list[bytes]) -> dict[str, Any]:
    chr0_by_substate = list(fixed(prg, 0xCACF, 18))
    active_chr0 = sorted(set(chr0_by_substate))
    for bank in active_chr0:
        if bank >= len(chr4k):
            raise ValueError(f"selected CHR0 bank {bank} absent from ROM")

    pointer_families: dict[str, Any] = {}
    max_count_by_type = {entity_type: 0 for entity_type in PRIMARY_TYPES}
    distinct_definitions: set[int] = set()

    for table in POINTER_TABLES:
        entries: dict[str, Any] = {}
        for entity_type in PRIMARY_TYPES:
            pointer_address = table + entity_type * 2
            definition = u16le(switched(prg, 3, pointer_address, 2))
            parsed = parse_metasprite(prg, definition)
            capacity = visual_capacity(entity_type)
            if parsed["sprite_count"] > capacity:
                raise ValueError(
                    f"type ${entity_type:02X} definition ${definition:04X} has "
                    f"{parsed['sprite_count']} sprites; capacity is {capacity}"
                )
            max_count_by_type[entity_type] = max(max_count_by_type[entity_type], parsed["sprite_count"])
            distinct_definitions.add(definition)
            entries[f"{entity_type:02X}"] = {
                "pointer_entry": f"{pointer_address:04X}",
                **parsed,
            }
        pointer_families[f"{table:04X}"] = entries

    direct_blank = parse_metasprite(prg, DIRECT_BLANK_DEFINITION)
    if direct_blank["sprite_count"] != 11:
        raise ValueError("direct $B647 blank/flash definition must own eleven records")
    blank_raw = switched(prg, 3, DIRECT_BLANK_DEFINITION, direct_blank["encoded_length"])
    p = 1
    blank_tiles: list[int] = []
    for _ in range(blank_raw[0]):
        if blank_raw[p] == 0xFF:
            p += 2
        blank_tiles.append(blank_raw[p])
        p += 3
    if any(tile != 0xFE for tile in blank_tiles):
        raise ValueError("direct $B647 definition is no longer the expected all-$FE blank frame")

    # $A908 separately creates one attached visual at primary visual +$2C.
    # $C0E3 supplies sprite/tile; $C0EF supplies the vertical offset added to
    # logical +$02. Horizontal position is fixed +/-9 by facing in code.
    attached_tiles = list(fixed(prg, 0xC0E3, 11))
    attached_y_offsets = list(fixed(prg, 0xC0EF, 11))
    attached: list[dict[str, Any]] = []
    for i, entity_type in enumerate(PRIMARY_TYPES):
        raw_offset = attached_y_offsets[i]
        signed_offset = raw_offset - 256 if raw_offset >= 128 else raw_offset
        attached.append(
            {
                "entity_type": f"{entity_type:02X}",
                "tile": attached_tiles[i],
                "y_offset": signed_offset,
                "creates_visual": attached_tiles[i] != 0,
            }
        )

    # Independent $9B93 multisprite bootstrap visual resources (bank 1).
    selector_sprite_bases = list(switched(prg, 1, 0x9B65, 7))
    selector_global_03a9 = list(switched(prg, 1, 0x9B6C, 7))
    profile_raw = list(switched(prg, 1, 0x9B73, 7 * 4))
    selector_profiles = [profile_raw[i : i + 4] for i in range(0, len(profile_raw), 4)]
    dedicated_0d_profile = list(switched(prg, 1, 0x9B8F, 4))

    if max_count_by_type[0x0D] != 12:
        raise ValueError("type $0D must retain its proven twelve-record reachable definition")
    if any(max_count_by_type[t] > 11 for t in PRIMARY_TYPES if t != 0x0D):
        raise ValueError("only type $0D may exceed the nominal eleven-record primary visual block")

    return {
        "chr0": {
            "by_substate_00_11": chr0_by_substate,
            "active_4k_banks": active_chr0,
        },
        "shared_compositor": {
            "selector_entry": "B987",
            "pointer_tables": pointer_families,
            "direct_blank_B647": direct_blank,
            "distinct_primary_definitions": len(distinct_definitions),
            "max_sprite_count_by_type": {
                f"{entity_type:02X}": max_count_by_type[entity_type]
                for entity_type in PRIMARY_TYPES
            },
            "visual_capacity_by_type": {
                f"{entity_type:02X}": visual_capacity(entity_type)
                for entity_type in PRIMARY_TYPES
            },
        },
        "attached_A908": {
            "tile_table": "C0E3",
            "vertical_offset_table": "C0EF",
            "entries": attached,
        },
        "multisprite_9B93": {
            "selector_sprite_bases_9B65": selector_sprite_bases,
            "selector_global_03A9_9B6C": selector_global_03a9,
            "selector_profiles_9B73": selector_profiles,
            "dedicated_substate_0D_profile_9B8F": dedicated_0d_profile,
            "dedicated_substate_0D_tile": 0x8C,
        },
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("rom", type=Path)
    parser.add_argument("-o", "--output", type=Path)
    parser.add_argument("--force", action="store_true")
    args = parser.parse_args()

    prg, chr4k = load_rom(args.rom, args.force)
    result = audit(prg, chr4k)
    payload = json.dumps(result, indent=2, sort_keys=True)

    if args.output:
        args.output.write_text(payload + "\n", encoding="utf-8")
        print(f"wrote {args.output}")
    else:
        print(payload)


if __name__ == "__main__":
    main()

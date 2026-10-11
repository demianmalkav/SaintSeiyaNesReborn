#!/usr/bin/env python3
"""Audit platform visual-refresh pointer ownership without exporting palette bytes.

Reads a user-owned canonical ROM. Output contains only addresses, selector metadata
and CHR bank numbers; original palette payloads are never emitted.
"""
from __future__ import annotations

import argparse
import json
import zlib
from pathlib import Path

CANONICAL_CORE_CRC32 = 0x9561798D
PRG_BANK_SIZE = 0x4000
BANK = 1

SPECIAL_DESCRIPTOR_DEFAULT = 0x9960
SPECIAL_DESCRIPTOR_SUBSTATE10 = 0x9963
DYNAMIC_CHR0_TABLE = 0x9966
OUTER_TABLE_9F46 = 0x9F46
SECONDARY_TABLE_9F66 = 0x9F66
AUX_TABLE_9C7F = 0x9C7F
PENDING_TABLE_9CC6 = 0x9CC6
BACKGROUND_A022 = 0xA022
BACKGROUND_A02B = 0xA02B


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
    prg_count = rom[4]
    start = 16 + trainer
    prg = rom[start : start + prg_count * PRG_BANK_SIZE]
    return [
        prg[i : i + PRG_BANK_SIZE]
        for i in range(0, len(prg), PRG_BANK_SIZE)
    ]


def switched(prg: list[bytes], address: int, n: int) -> bytes:
    if not 0x8000 <= address <= 0xBFFF:
        raise ValueError(f"bank-1 address outside switchable window: {address:04X}")
    start = address - 0x8000
    raw = prg[BANK][start : start + n]
    if len(raw) != n:
        raise ValueError(f"read crosses bank-1 window at {address:04X}")
    return raw


def u16(prg: list[bytes], address: int) -> int:
    raw = switched(prg, address, 2)
    return raw[0] | (raw[1] << 8)


def descriptor(prg: list[bytes], address: int, n: int = 3) -> dict:
    switched(prg, address, n)
    return {"address": f"{address:04X}", "byte_count": n}


def audit(prg: list[bytes]) -> dict:
    dynamic_chr0 = list(switched(prg, DYNAMIC_CHR0_TABLE, 6))

    players = {}
    for saint in range(5):
        ptr = u16(prg, OUTER_TABLE_9F46 + saint * 2)
        players[f"{saint:02X}"] = descriptor(prg, ptr)

    primary = {}
    for entity_type in range(0x05, 0x10):
        pointer_list = u16(prg, OUTER_TABLE_9F46 + entity_type * 2)
        variants = [u16(prg, pointer_list + i * 2) for i in range(4)]
        secondary = u16(prg, SECONDARY_TABLE_9F66 + entity_type * 2)
        for ptr in variants:
            descriptor(prg, ptr)
        descriptor(prg, secondary)
        primary[f"{entity_type:02X}"] = {
            "pointer_list": f"{pointer_list:04X}",
            "variant_descriptors": [f"{ptr:04X}" for ptr in variants],
            "secondary_descriptor": f"{secondary:04X}",
        }

    auxiliary = {}
    for profile in range(1, 5):
        ptr = u16(prg, AUX_TABLE_9C7F + profile * 2)
        auxiliary[str(profile)] = descriptor(prg, ptr)

    pending = {}
    for selector in range(1, 6):
        ptr = u16(prg, PENDING_TABLE_9CC6 + (selector - 1) * 2)
        pending[str(selector)] = descriptor(prg, ptr)

    descriptor(prg, SPECIAL_DESCRIPTOR_DEFAULT)
    descriptor(prg, SPECIAL_DESCRIPTOR_SUBSTATE10)
    descriptor(prg, BACKGROUND_A022, 9)
    descriptor(prg, BACKGROUND_A02B, 9)

    return {
        "special_one_shot": {
            "descriptor_default": f"{SPECIAL_DESCRIPTOR_DEFAULT:04X}",
            "descriptor_substate_10": f"{SPECIAL_DESCRIPTOR_SUBSTATE10:04X}",
            "dynamic_chr0_by_substate_0C_11": dynamic_chr0,
        },
        "player_descriptors": players,
        "primary_profile_descriptors": primary,
        "auxiliary_profile_descriptors": auxiliary,
        "pending_03A9_descriptors": pending,
        "background_0D": {
            "source_low_half": f"{BACKGROUND_A02B:04X}",
            "source_high_half": f"{BACKGROUND_A022:04X}",
            "source_byte_count": 9,
            "target_palette_bytes": 16,
        },
        "transfer_format": {
            "sprite_palette_start": "3F10",
            "descriptor_byte_count": 3,
            "subpalette_count": 4,
            "prefix": "0F",
            "ppu_address_reset_sequence": ["3F", "00", "00", "00"],
        },
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("rom", type=Path)
    parser.add_argument("--force", action="store_true")
    args = parser.parse_args()
    print(json.dumps(audit(load_prg(args.rom, args.force)), indent=2))


if __name__ == "__main__":
    main()

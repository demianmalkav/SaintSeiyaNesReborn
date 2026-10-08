#!/usr/bin/env python3
"""Extract primary/common platform encounter descriptors from Kanketsu Hen.

No ROM payload is embedded. The tool validates the canonical core CRC32 and
emits the per-page encounter descriptor, decoded type/tier/slot-enable flags,
and the four-byte combat-resource configuration selected by that descriptor.
"""
from __future__ import annotations

import argparse
import json
import zlib
from pathlib import Path

CANONICAL_CORE_CRC32 = 0x9561798D
PRG_BANK_SIZE = 0x4000
SUBSTATE_COUNT = 18
COMMON_SCHEDULE_POINTERS = 0x9AE5
COMMON_CONFIG_TABLE = 0x9A35
COMMON_CONFIG_FIRST_TYPE = 0x05
TIER_AUX_TABLE = 0x9A31
PAGE_COUNTS = (6,7,8,9,10,11,12,13,14,15,16,16,12,12,12,3,12,2)
GENERIC_SPAWN_REJECT_TYPES = {0x08,0x09,0x0C,0x0D,0x0E}


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


def read_bank(banks: list[bytes], bank: int, addr: int, size: int) -> bytes:
    base = 0xC000 if bank == 7 else 0x8000
    off = addr - base
    return banks[bank][off : off + size]


def u16(raw: bytes) -> int:
    return raw[0] | (raw[1] << 8)


def bcd_to_int(value: int) -> int:
    return ((value >> 4) & 0xF) * 10 + (value & 0xF)


def config_for(banks: list[bytes], type_id: int, tier: int) -> dict[str, int | str] | None:
    if not (COMMON_CONFIG_FIRST_TYPE <= type_id <= 0x0F):
        return None
    offset = (type_id - COMMON_CONFIG_FIRST_TYPE) * 16 + tier * 4
    hp, cosmo_ticks, life_ticks, reward_bcd = read_bank(
        banks, 1, COMMON_CONFIG_TABLE + offset, 4
    )
    return {
        "hp": hp,
        "cosmo_drain_ticks": cosmo_ticks,
        "life_drain_ticks": life_ticks,
        "seventh_sense_reward": bcd_to_int(reward_bcd),
        "reward_packed_bcd": f"0x{reward_bcd:02X}",
    }


def decode_descriptor(banks: list[bytes], value: int) -> dict[str, object]:
    type_id = value & 0x0F
    tier = (value >> 4) & 0x03
    return {
        "raw": f"0x{value:02X}",
        "type": type_id,
        "tier": tier,
        "second_common_slot_enabled": bool(value & 0x80),
        "bit6": bool(value & 0x40),
        "tier_aux_03AB": read_bank(banks, 1, TIER_AUX_TABLE + tier, 1)[0],
        "generic_edge_spawner_accepts_type": (
            value != 0 and type_id not in GENERIC_SPAWN_REJECT_TYPES
        ),
        "config": config_for(banks, type_id, tier),
    }


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("rom", type=Path)
    ap.add_argument("-o", "--output", type=Path)
    ap.add_argument("--force", action="store_true")
    args = ap.parse_args()

    banks = load_prg(args.rom, args.force)
    pointers = [
        u16(read_bank(banks, 1, COMMON_SCHEDULE_POINTERS + i * 2, 2))
        for i in range(SUBSTATE_COUNT)
    ]

    substates = []
    for substate, (pointer, count) in enumerate(zip(pointers, PAGE_COUNTS)):
        raw = list(read_bank(banks, 1, pointer, count))
        substates.append(
            {
                "platform_substate_02": f"0x{substate:02X}",
                "schedule_pointer": f"0x{pointer:04X}",
                "page_count": count,
                "pages": [
                    {"page_index": i, **decode_descriptor(banks, value)}
                    for i, value in enumerate(raw)
                ],
            }
        )

    payload = {
        "format": "SaintSeiyaNesReborn.PrimaryEncounterSchedule.v1",
        "source_core_crc32": f"{CANONICAL_CORE_CRC32:08X}",
        "descriptor_layout": {
            "bits_0_3": "type",
            "bits_4_5": "tier (stats + sprite-palette selection)",
            "bit_6": "unknown/special; not consumed by generic edge spawner",
            "bit_7": "enable common slot B in addition to slot A (subject to cooldown)",
        },
        "generic_spawn_cooldown_updates": 0x30,
        "generic_spawn_slots": ["$03BA", "$03CA"],
        "substates": substates,
    }

    text = json.dumps(payload, ensure_ascii=False, indent=2) + "\n"
    if args.output:
        args.output.write_text(text, encoding="utf-8")
    else:
        print(text, end="")


if __name__ == "__main__":
    main()

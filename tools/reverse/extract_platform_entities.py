#!/usr/bin/env python3
"""Extract neutral platform entity archetypes/schedules from the canonical ROM.

The script contains no ROM payload. It reads a user-owned iNES image and emits
JSON describing the reverse-engineered spawn schedule and five numeric entity
archetypes. Visual graphics remain in the ROM and are not exported.
"""

from __future__ import annotations

import argparse
import json
import zlib
from pathlib import Path

CANONICAL_CORE_CRC32 = 0x9561798D
PRG_BANK_SIZE = 0x4000


def load_banks(path: Path, force: bool = False) -> list[bytes]:
    rom = path.read_bytes()
    if len(rom) < 16 or rom[:4] != b"NES\x1a":
        raise SystemExit("not an iNES ROM")
    trainer = 512 if rom[6] & 0x04 else 0
    core = rom[16 + trainer :]
    crc = zlib.crc32(core) & 0xFFFFFFFF
    if crc != CANONICAL_CORE_CRC32 and not force:
        raise SystemExit(
            f"unexpected core CRC32 {crc:08X}; expected {CANONICAL_CORE_CRC32:08X}. "
            "Use --force only for intentional revision comparison."
        )
    prg_count = rom[4]
    start = 16 + trainer
    prg = rom[start : start + prg_count * PRG_BANK_SIZE]
    return [prg[i : i + PRG_BANK_SIZE] for i in range(0, len(prg), PRG_BANK_SIZE)]


def switched(banks: list[bytes], bank: int, cpu_addr: int, n: int) -> bytes:
    if not 0x8000 <= cpu_addr < 0xC000:
        raise ValueError(f"not a switched PRG address: ${cpu_addr:04X}")
    return banks[bank][cpu_addr - 0x8000 : cpu_addr - 0x8000 + n]


def fixed(banks: list[bytes], cpu_addr: int, n: int) -> bytes:
    if not 0xC000 <= cpu_addr <= 0xFFFF:
        raise ValueError(f"not a fixed PRG address: ${cpu_addr:04X}")
    return banks[7][cpu_addr - 0xC000 : cpu_addr - 0xC000 + n]


def u16le(raw: bytes) -> list[int]:
    return [raw[i] | (raw[i + 1] << 8) for i in range(0, len(raw), 2)]


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("rom", type=Path)
    ap.add_argument("-o", "--output", type=Path)
    ap.add_argument("--force", action="store_true")
    args = ap.parse_args()

    banks = load_banks(args.rom, args.force)

    # Bank 1 $9C92-$9CA5: five 4-byte archetypes.
    raw_archetypes = switched(banks, 1, 0x9C92, 5 * 4)
    visual_types = fixed(banks, 0xC169, 5)
    archetypes = []
    for entity_id in range(5):
        hp, cosmo_ticks, life_ticks, reward_bcd = raw_archetypes[entity_id * 4 : entity_id * 4 + 4]
        reward = ((reward_bcd >> 4) & 0xF) * 10 + (reward_bcd & 0xF)
        archetypes.append(
            {
                "id": entity_id,
                "hp": hp,
                "cosmo_drain_ticks": cosmo_ticks,
                "life_drain_ticks": life_ticks,
                "seventh_sense_reward": reward,
                "reward_packed_bcd": f"0x{reward_bcd:02X}",
                "visual_object_type": f"0x{visual_types[entity_id]:02X}",
            }
        )

    # Bank 1 $9C24-$9C47: 18 little-endian schedule pointers, indexed by $02.
    pointers = u16le(switched(banks, 1, 0x9C24, 18 * 2))
    unique_starts = sorted({p for p in pointers if 0x9C48 <= p < 0x9C7F})
    boundaries = unique_starts + [0x9C7F]
    schedule_by_pointer: dict[int, list[int]] = {}
    for i, start in enumerate(unique_starts):
        end = next(b for b in boundaries if b > start)
        schedule_by_pointer[start] = list(switched(banks, 1, start, end - start))

    schedules = []
    for substate, pointer in enumerate(pointers):
        values = schedule_by_pointer.get(pointer)
        schedules.append(
            {
                "engine_substate_02": f"0x{substate:02X}",
                "pointer": f"0x{pointer:04X}",
                "page_archetypes": values if values is not None else [],
            }
        )

    payload = {
        "format": "SaintSeiyaNesReborn.PlatformEntitySchedule.v1",
        "source_core_crc32": f"{CANONICAL_CORE_CRC32:08X}",
        "notes": {
            "page_index_ram": "$45",
            "substate_ram": "$02",
            "archetype_table": "PRG bank 1 $9C92-$9CA5",
            "schedule_pointer_table": "PRG bank 1 $9C24-$9C47",
            "visual_type_table": "fixed bank $C169",
        },
        "archetypes": archetypes,
        "schedules": schedules,
    }

    text = json.dumps(payload, ensure_ascii=False, indent=2) + "\n"
    if args.output:
        args.output.write_text(text, encoding="utf-8")
    else:
        print(text, end="")


if __name__ == "__main__":
    main()

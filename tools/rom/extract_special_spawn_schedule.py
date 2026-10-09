#!/usr/bin/env python3
"""Extract bank-1 $8925 special-entity spawn schedules from Kanketsu Hen.

The script emits derived JSON only. It does not modify the ROM and does not
embed schedule bytes in the public source tree.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path

INES_HEADER = 16
PRG_BANK_SIZE = 0x4000
BANK1_INDEX = 1
CPU_BASE = 0x8000
POINTER_TABLE_CPU = 0x89D2
SUBSTATE_COUNT = 18
ENTRY_SIZE = 4
TERMINATOR = 0xFF


def bank1(rom: bytes) -> bytes:
    if len(rom) < INES_HEADER or rom[:4] != b"NES\x1a":
        raise ValueError("Input is not an iNES ROM")
    prg_banks = rom[4]
    if prg_banks <= BANK1_INDEX:
        raise ValueError("ROM does not contain PRG bank 1")
    start = INES_HEADER + BANK1_INDEX * PRG_BANK_SIZE
    end = start + PRG_BANK_SIZE
    return rom[start:end]


def read_u16_le(data: bytes, offset: int) -> int:
    return data[offset] | (data[offset + 1] << 8)


def cpu_to_bank_offset(address: int) -> int:
    if not (0x8000 <= address <= 0xBFFF):
        raise ValueError(f"Bank-1 pointer outside $8000-$BFFF: ${address:04X}")
    return address - CPU_BASE


def extract(rom: bytes) -> dict:
    data = bank1(rom)
    table_offset = cpu_to_bank_offset(POINTER_TABLE_CPU)
    schedules = []

    for substate in range(SUBSTATE_COUNT):
        pointer = read_u16_le(data, table_offset + substate * 2)
        cursor = cpu_to_bank_offset(pointer)
        entries = []

        while True:
            low = data[cursor]
            if low == TERMINATOR:
                break
            if cursor + ENTRY_SIZE > len(data):
                raise ValueError(f"Schedule for substate {substate:02X} overruns bank 1")

            camera_low, camera_high, spawn_y, reserved = data[cursor:cursor + ENTRY_SIZE]
            entries.append({
                "camera_low_aligned": camera_low,
                "camera_high": camera_high,
                "spawn_y": spawn_y,
                "reserved_03": reserved,
            })
            cursor += ENTRY_SIZE

        schedules.append({
            "substate": substate,
            "pointer_cpu": pointer,
            "entries": entries,
        })

    return {
        "source": "PRG bank 1 $8925-$89D1",
        "pointer_table_cpu": POINTER_TABLE_CPU,
        "entry_size": ENTRY_SIZE,
        "substates": schedules,
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("rom", type=Path)
    parser.add_argument("-o", "--output", type=Path)
    args = parser.parse_args()

    result = extract(args.rom.read_bytes())
    text = json.dumps(result, ensure_ascii=False, indent=2) + "\n"
    if args.output:
        args.output.write_text(text, encoding="utf-8")
    else:
        print(text, end="")


if __name__ == "__main__":
    main()

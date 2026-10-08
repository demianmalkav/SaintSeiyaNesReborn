#!/usr/bin/env python3
"""Extract platform-physics evidence from the canonical Kanketsu Hen ROM.

This tool contains no ROM bytes. It reads a user-supplied iNES image and
reports values at reverse-engineered locations so findings remain reproducible.
"""

from __future__ import annotations

import argparse
import hashlib
import struct
import zlib
from pathlib import Path

CANONICAL_CORE_CRC32 = 0x9561798D
PRG_BANK_SIZE = 0x4000


def s8(value: int) -> int:
    return value - 0x100 if value & 0x80 else value


def load_rom(path: Path, force: bool = False) -> tuple[bytes, list[bytes]]:
    rom = path.read_bytes()
    if len(rom) < 16 or rom[:4] != b"NES\x1a":
        raise SystemExit("not an iNES ROM")
    prg_banks = rom[4]
    trainer = 512 if rom[6] & 0x04 else 0
    prg_start = 16 + trainer
    prg_size = prg_banks * PRG_BANK_SIZE
    core = rom[16 + trainer :]
    crc = zlib.crc32(core) & 0xFFFFFFFF
    if crc != CANONICAL_CORE_CRC32 and not force:
        raise SystemExit(
            f"unexpected ROM core CRC32 {crc:08X}; expected {CANONICAL_CORE_CRC32:08X}. "
            "Use --force only for intentional revision comparison."
        )
    prg = rom[prg_start : prg_start + prg_size]
    banks = [prg[i : i + PRG_BANK_SIZE] for i in range(0, len(prg), PRG_BANK_SIZE)]
    if len(banks) != 8:
        raise SystemExit(f"expected 8 PRG banks, got {len(banks)}")
    return rom, banks


def read_window(banks: list[bytes], addr: int, n: int, switched_bank: int = 3) -> bytes:
    if 0x8000 <= addr < 0xC000:
        bank = banks[switched_bank]
        off = addr - 0x8000
    elif 0xC000 <= addr <= 0xFFFF:
        bank = banks[7]
        off = addr - 0xC000
    else:
        raise ValueError(f"CPU address outside PRG window: ${addr:04X}")
    return bank[off : off + n]


def u16le(raw: bytes) -> int:
    return struct.unpack("<H", raw)[0]


def pointer_table(banks: list[bytes], addr: int, count: int) -> list[int]:
    raw = read_window(banks, addr, count * 2)
    return [u16le(raw[i : i + 2]) for i in range(0, len(raw), 2)]


def curve_stats(banks: list[bytes], ptr: int, duration: int) -> dict[str, int]:
    curve = [s8(x) for x in read_window(banks, ptr, duration)]
    y = 0
    apex_y = 0
    apex_frame = 0
    for frame, delta in enumerate(curve, 1):
        # Original routine subtracts positive entries from player Y and adds
        # the magnitude of negative entries, so screen-space delta is -delta.
        y -= delta
        if y < apex_y:
            apex_y = y
            apex_frame = frame
    return {
        "duration": duration,
        "max_rise_px": -apex_y,
        "apex_frame": apex_frame,
        "net_y_px": y,
    }


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("rom", type=Path)
    ap.add_argument("--force", action="store_true")
    args = ap.parse_args()

    rom, banks = load_rom(args.rom, args.force)
    print("ROM SHA1:", hashlib.sha1(rom).hexdigest().upper())
    print("core CRC32:", f"{zlib.crc32(rom[16:]) & 0xFFFFFFFF:08X}")

    standing_duration = 0x20
    standing_ptr = 0xBF49
    high_durations = list(read_window(banks, 0xBCF0, 5))
    forward_durations = list(read_window(banks, 0xBCF5, 5))
    high_ptrs = pointer_table(banks, 0xBFD7, 5)
    forward_ptrs = pointer_table(banks, 0xBFE1, 5)

    print("\nStanding vertical jump")
    print(f"  ptr=${standing_ptr:04X}", curve_stats(banks, standing_ptr, standing_duration))

    print("\nUp+A high-jump profiles by engine Saint index")
    for index, (duration, ptr) in enumerate(zip(high_durations, high_ptrs)):
        print(f"  {index}: ptr=${ptr:04X} {curve_stats(banks, ptr, duration)}")

    print("\nDirectional jump profiles by engine Saint index")
    for index, (duration, ptr) in enumerate(zip(forward_durations, forward_ptrs)):
        print(f"  {index}: ptr=${ptr:04X} {curve_stats(banks, ptr, duration)}")

    ranges = list(read_window(banks, 0xBCAE, 20))
    print("\nCosmo projectile range/lifetime table")
    print("  rows are Cosmo hundreds brackets 0-1, 2-3, 4-5, 6-7, 8-9")
    print("  columns are engine Saint indices 0..3; index 4 uses fixed 60")
    for row in range(5):
        values = ranges[row * 4 : row * 4 + 4]
        print(f"  bracket {row * 2}-{row * 2 + 1}: {values}")
    print("  index 4: 60")

    print("\nCollision probe geometry (world X = scroll + player X)")
    print("  $56: center/head      x+8,  align16(y+8)")
    print("  $54: left/upper side  x+0,  align16(y+8)")
    print("  $51: right/upper side x+16, align16(y+8)")
    print("  $53: left/lower side  x+0,  align16(y+8)+16 (special +24 at y=$88)")
    print("  $50: right/lower side x+16, align16(y+8)+16 (special +24 at y=$88)")
    print("  $55: ground left      x-8,  y+32")
    print("  $4F: ground center    x+8,  y+32")
    print("  $52: ground right     x+24, y+32")


if __name__ == "__main__":
    main()

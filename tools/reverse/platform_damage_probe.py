#!/usr/bin/env python3
"""Extract and evaluate platform attack damage from the canonical ROM.

No ROM bytes are embedded. The tool reads a user-supplied iNES image,
verifies the known core CRC32 by default, extracts the five coefficient bytes
from bank 1, and evaluates the reconstructed packed-decimal damage rule.
"""

from __future__ import annotations

import argparse
import zlib
from pathlib import Path

CANONICAL_CORE_CRC32 = 0x9561798D
PRG_BANK_SIZE = 0x4000
INTERNAL_NAMES = ("Seiya", "Shun", "Hyoga", "Shiryu", "Ikki")


def load_prg(path: Path, force: bool = False) -> list[bytes]:
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
    prg_start = 16 + trainer
    prg = rom[prg_start : prg_start + prg_count * PRG_BANK_SIZE]
    return [prg[i : i + PRG_BANK_SIZE] for i in range(0, len(prg), PRG_BANK_SIZE)]


def read_cpu(banks: list[bytes], bank: int, addr: int, n: int) -> bytes:
    if not 0x8000 <= addr < 0xC000:
        raise ValueError("this helper expects a switched-bank CPU address")
    return banks[bank][addr - 0x8000 : addr - 0x8000 + n]


def damage_from_cosmo(cosmo: int, coefficient: int) -> int:
    """Reproduce the effective arithmetic of bank 1 $8616+.

    Below 100 the full two decimal digits contribute. At 100+ the original
    code discards the ones digit and works from hundreds/tens only.
    """
    if not 0 <= cosmo <= 999:
        raise ValueError("Cosmo must be in 0..999")
    if cosmo < 100:
        return (coefficient * cosmo) // 100
    hundreds = cosmo // 100
    tens = (cosmo // 10) % 10
    return coefficient * hundreds + (coefficient * tens) // 10


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("rom", type=Path)
    ap.add_argument("--cosmo", type=int, default=None, help="show one Cosmo value instead of standard fixtures")
    ap.add_argument("--force", action="store_true")
    args = ap.parse_args()

    banks = load_prg(args.rom, args.force)
    coefficients = list(read_cpu(banks, 1, 0x8611, 5))

    print("Internal Saint order:", ", ".join(INTERNAL_NAMES))
    print("Coefficients:", coefficients)

    values = [args.cosmo] if args.cosmo is not None else [50, 99, 100, 109, 110, 499, 999]
    print("\nCosmo -> platform damage")
    print("Cosmo  " + "  ".join(f"{name:>7}" for name in INTERNAL_NAMES))
    for cosmo in values:
        row = [damage_from_cosmo(cosmo, k) for k in coefficients]
        print(f"{cosmo:>5}  " + "  ".join(f"{v:>7}" for v in row))

    print("\nEnemy collision facts")
    print("  logical entity HP: record +0x0C")
    print("  Seventh Sense reward: record +0x0F")
    print("  computed damage byte: zero-page $72")
    print("  projectile collision entry: bank 3 $9915")


if __name__ == "__main__":
    main()

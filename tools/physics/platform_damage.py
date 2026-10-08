#!/usr/bin/env python3
"""Reproduce the original platform attack-damage calculation.

The canonical ROM stores one base coefficient per internal Saint at PRG bank 1
$8611. This tool validates the ROM hash, extracts those coefficients and applies
the clean-room formula reconstructed from $8616-$86CA.

Internal Saint order: Seiya, Shun, Hyoga, Shiryu, Ikki.
"""

from __future__ import annotations

import argparse
import hashlib
from pathlib import Path

CANONICAL_SHA1 = "F871D9B3DAFDDCDAD5F2ACD71044292E5169064E"
SAINTS = ("Seiya", "Shun", "Hyoga", "Shiryu", "Ikki")


def platform_damage(base: int, cosmo: int) -> int:
    """Exact integer result written to RAM $72 by the original routine.

    Cosmo is interpreted as a three-digit decimal value (0..999).
    Below 100, all two digits contribute. At 100+, the ones digit is
    deliberately ignored by the original decimal-digit algorithm.
    """
    if not 0 <= cosmo <= 999:
        raise ValueError("Cosmo must be in range 0..999")

    hundreds = cosmo // 100
    tens = (cosmo // 10) % 10
    ones = cosmo % 10

    if hundreds:
        return base * hundreds + (base * tens) // 10

    # Equivalent to floor(base * cosmo / 100), written this way to mirror
    # the original two-stage decimal-digit/truncation process.
    return (base * tens + (base * ones) // 10) // 10


def read_coefficients(rom: Path) -> list[int]:
    data = rom.read_bytes()
    if hashlib.sha1(data).hexdigest().upper() != CANONICAL_SHA1:
        raise SystemExit("ROM SHA-1 does not match the canonical Japanese revision")
    if data[:4] != b"NES\x1A":
        raise SystemExit("not an iNES ROM")

    prg_size = data[4] * 0x4000
    prg = data[16 : 16 + prg_size]
    offset = 1 * 0x4000 + (0x8611 - 0x8000)
    return list(prg[offset : offset + 5])


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("rom", type=Path)
    ap.add_argument(
        "--cosmo",
        type=int,
        nargs="*",
        default=[0, 10, 50, 99, 100, 199, 500, 999],
        help="Cosmo values to tabulate (default: representative values)",
    )
    args = ap.parse_args()

    bases = read_coefficients(args.rom)
    print("base coefficients:", dict(zip(SAINTS, bases)))
    print("Cosmo  " + " ".join(f"{name:>6}" for name in SAINTS))
    for cosmo in args.cosmo:
        values = [platform_damage(base, cosmo) for base in bases]
        print(f"{cosmo:>5}  " + " ".join(f"{value:>6}" for value in values))


if __name__ == "__main__":
    main()

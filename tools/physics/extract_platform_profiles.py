#!/usr/bin/env python3
"""Extract table-driven platform movement/attack profiles from the canonical ROM.

The script validates the canonical Japanese ROM by SHA-1, then reads only the
small tables already identified through static reverse engineering. It does
not redistribute ROM data.
"""

from __future__ import annotations

import argparse
import hashlib
from pathlib import Path

CANONICAL_SHA1 = "F871D9B3DAFDDCDAD5F2ACD71044292E5169064E"


def s8(value: int) -> int:
    return value - 256 if value >= 0x80 else value


def main() -> None:
    ap = argparse.ArgumentParser(
        description="Extract Kanketsu Hen platform jump and projectile profiles"
    )
    ap.add_argument("rom", type=Path)
    args = ap.parse_args()

    data = args.rom.read_bytes()
    if hashlib.sha1(data).hexdigest().upper() != CANONICAL_SHA1:
        raise SystemExit("ROM SHA-1 does not match the canonical Japanese revision")
    if data[:4] != b"NES\x1A":
        raise SystemExit("not an iNES ROM")

    prg = data[16 : 16 + data[4] * 0x4000]

    def read(bank: int, cpu_addr: int, size: int) -> bytes:
        base = 0xC000 if bank == 7 else 0x8000
        offset = bank * 0x4000 + (cpu_addr - base)
        return prg[offset : offset + size]

    def profile(bank: int, addr: int, duration: int) -> tuple[list[int], int]:
        # $BCD3 increments the phase counter before indexing, so table indices
        # 0..duration-3 are consumed. At/after duration the engine falls at +3/frame.
        deltas = [s8(v) for v in read(bank, addr, duration - 2)]
        y = 0
        minimum = 0
        for delta in deltas:
            y -= delta
            minimum = min(minimum, y)
        return deltas, -minimum

    normal_duration = 0x20
    normal_pointer = (3, 0xBF49)
    high_durations = list(read(3, 0xBCF0, 5))
    forward_durations = list(read(3, 0xBCF5, 5))

    raw_high = read(3, 0xBFD7, 10)
    raw_forward = read(3, 0xBFE1, 10)
    high_addresses = [raw_high[i] | (raw_high[i + 1] << 8) for i in range(0, 10, 2)]
    forward_addresses = [
        raw_forward[i] | (raw_forward[i + 1] << 8) for i in range(0, 10, 2)
    ]

    def bank_for(addr: int) -> int:
        return 7 if addr >= 0xC000 else 3

    _, normal_height = profile(*normal_pointer, normal_duration)
    print(
        f"normal vertical: duration={normal_duration}, "
        f"max_ascent={normal_height}px, terminal_fall=3px/frame"
    )

    print("\nslot  high(dur,height)  forward(dur,height)")
    for slot in range(5):
        high_duration = high_durations[slot]
        forward_duration = forward_durations[slot]
        _, high_height = profile(
            bank_for(high_addresses[slot]), high_addresses[slot], high_duration
        )
        _, forward_height = profile(
            bank_for(forward_addresses[slot]),
            forward_addresses[slot],
            forward_duration,
        )
        print(
            f"{slot:>4}  {high_duration:>3},{high_height:>3}px"
            f"         {forward_duration:>3},{forward_height:>3}px"
        )

    # For slots 0..3 the attack lifetime/range parameter is indexed by
    # floor(Cosmo-hundreds / 2). Slot 4 uses an immediate constant.
    table = list(read(3, 0xBCAE, 20))
    special = read(3, 0xBC90, 2)
    slot4 = special[1] if len(special) == 2 and special[0] == 0xA9 else None

    print("\nprojectile lifetime/range parameter by Cosmo hundreds bracket")
    print("hundreds  slot0 slot1 slot2 slot3 slot4")
    for bracket in range(5):
        row = table[bracket * 4 : bracket * 4 + 4]
        label = f"{bracket * 2}-{bracket * 2 + 1}"
        extra = f"{slot4:>5}" if slot4 is not None else "    ?"
        print(f"{label:>8}  " + " ".join(f"{v:>5}" for v in row) + " " + extra)


if __name__ == "__main__":
    main()

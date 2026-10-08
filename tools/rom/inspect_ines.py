#!/usr/bin/env python3
"""Inspect an iNES ROM without modifying it.

Prints header geometry and hashes for the complete file, payload, PRG and CHR.
This tool intentionally does not redistribute ROM contents.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import zlib
from pathlib import Path


def hashes(data: bytes) -> dict[str, str]:
    return {
        "crc32": f"{zlib.crc32(data) & 0xFFFFFFFF:08X}",
        "md5": hashlib.md5(data).hexdigest().upper(),
        "sha1": hashlib.sha1(data).hexdigest().upper(),
        "sha256": hashlib.sha256(data).hexdigest().upper(),
    }


def inspect(path: Path) -> dict:
    blob = path.read_bytes()
    if len(blob) < 16 or blob[:4] != b"NES\x1A":
        raise SystemExit("Not an iNES/NES 2.0 file")

    h = blob[:16]
    prg_size = h[4] * 16 * 1024
    chr_size = h[5] * 8 * 1024
    trainer_size = 512 if h[6] & 0x04 else 0
    data_start = 16 + trainer_size
    prg = blob[data_start : data_start + prg_size]
    chr_ = blob[data_start + prg_size : data_start + prg_size + chr_size]
    payload = blob[16:]
    mapper = (h[6] >> 4) | (h[7] & 0xF0)

    return {
        "path": str(path),
        "file_size": len(blob),
        "header_hex": h.hex().upper(),
        "format": "NES 2.0" if (h[7] & 0x0C) == 0x08 else "iNES",
        "mapper": mapper,
        "prg_bytes": len(prg),
        "chr_bytes": len(chr_),
        "trainer": bool(h[6] & 0x04),
        "battery": bool(h[6] & 0x02),
        "header_mirroring": "vertical" if h[6] & 0x01 else "horizontal",
        "hashes": {
            "complete_file": hashes(blob),
            "header_removed": hashes(payload),
            "prg": hashes(prg),
            "chr": hashes(chr_),
        },
    }


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("rom", type=Path)
    args = ap.parse_args()
    print(json.dumps(inspect(args.rom), indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()

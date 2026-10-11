#!/usr/bin/env python3
"""Audit the canonical $E0AC random/phase source without exporting ROM payloads."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

EXPECTED_SHA1 = "F871D9B3DAFDDCDAD5F2ACD71044292E5169064E"
PRG_BANK_SIZE = 0x4000
SOURCE_START = 0x94F0
SOURCE_BYTES = 0x100


def load(path: Path):
    rom = path.read_bytes()
    sha1 = hashlib.sha1(rom).hexdigest().upper()
    if sha1 != EXPECTED_SHA1:
        raise SystemExit(f"unexpected ROM SHA-1 {sha1}; expected {EXPECTED_SHA1}")
    if rom[:4] != b"NES\x1a" or rom[4] != 8:
        raise SystemExit("unexpected iNES/PRG layout")
    prg = rom[16 : 16 + 8 * PRG_BANK_SIZE]
    return rom, [prg[i * PRG_BANK_SIZE : (i + 1) * PRG_BANK_SIZE] for i in range(8)]


def fixed(banks, address: int, n: int) -> bytes:
    return banks[7][address - 0xC000 : address - 0xC000 + n]


def switched(banks, bank: int, address: int, n: int) -> bytes:
    return banks[bank][address - 0x8000 : address - 0x8000 + n]


def find_all(blob: bytes, needle: bytes):
    return [i for i in range(len(blob) - len(needle) + 1) if blob[i : i + len(needle)] == needle]


def audit(banks):
    updater = fixed(banks, 0xE0AC, 0x11)
    expected_updater = bytes.fromhex("AE6006 BDF094 18 6D5F06 8D5F06 EE6006 60")
    if updater != expected_updater:
        raise AssertionError("$E0AC updater bytes changed")

    caller_hits = []
    needle = bytes.fromhex("20ACE0")
    for bank, data in enumerate(banks):
        for offset in find_all(data, needle):
            cpu = (0xC000 if bank == 7 else 0x8000) + offset
            caller_hits.append([bank, f"{cpu:04X}"])
    if caller_hits != [[7, "E09C"]]:
        raise AssertionError(f"unexpected $E0AC caller set: {caller_hits}")

    table_hashes = {}
    for bank in range(7):
        window = switched(banks, bank, SOURCE_START, SOURCE_BYTES)
        table_hashes[str(bank)] = hashlib.sha256(window).hexdigest()
    if len(set(table_hashes.values())) != 7:
        raise AssertionError("physical $94F0 windows are unexpectedly identical")

    direct_065f = []
    direct_0660 = []
    for bank, data in enumerate(banks):
        base = 0xC000 if bank == 7 else 0x8000
        for target, bucket in [(0x065F, direct_065f), (0x0660, direct_0660)]:
            word = bytes([target & 0xFF, target >> 8])
            for offset in find_all(data, word):
                bucket.append([bank, f"{base + offset - 1:04X}"])

    return {
        "caller_jsr_e0ac": caller_hits,
        "source_window": {
            "cpu_start": "94F0",
            "byte_count": SOURCE_BYTES,
            "sha256_by_switchable_prg_bank": table_hashes,
            "all_seven_windows_distinct": True,
        },
        "known_bank_resolution": {
            "0641": "0 if 068F==8F else 6",
            "0526": 6,
            "0538": 5,
            "057D": 6,
            "priority": ["0641", "0526", "0538", "057D"],
            "mapper_busy": "preserve incoming visible bank",
        },
        "reset_seed_owners": {
            "C13D": [0, 0],
            "AD4A": [1, 1],
            "959D": [0, 0],
            "AF0D": [0, 0],
            "B38A_aliases": {"0648+17": "065F", "0648+18": "0660"},
        },
        "consumer_addresses": {
            "065F": ["E33D", "EC18", "EC20", "EC2B", "F65D", "F665", "FAC9", "FAD7", "bank6:913E", "bank6:92E2"],
            "0660": ["F995"],
        },
        "raw_word_hits_note": {
            "065F_count": len(direct_065f),
            "0660_count": len(direct_0660),
            "bank2_8158": "data false positive for raw 5F 06 bytes",
        },
    }


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("rom", type=Path)
    args = ap.parse_args()
    _, banks = load(args.rom)
    print(json.dumps(audit(banks), indent=2))


if __name__ == "__main__":
    main()

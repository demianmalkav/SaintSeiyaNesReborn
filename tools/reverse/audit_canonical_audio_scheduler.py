#!/usr/bin/env python3
"""Audit the canonical DB9C/DBB6 audio scheduler without exporting audio payloads."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

EXPECTED_SHA1 = "F871D9B3DAFDDCDAD5F2ACD71044292E5169064E"
PRG_BANK_SIZE = 0x4000

RANGE_HASHES = {
    "fixed_dispatch_D9A0_DB7A": (7, 0xD9A0, 0xDB7B, "67c17c74e9c91ae6fee82628ec5008d5ea8eca4a05e00ba5f717854e6473bc00"),
    "fixed_reset_loader_DB9C_DC09": (7, 0xDB9C, 0xDC0A, "8e02e81ae8aa91030d6cc5cef1833eddc974d76e264d86abd26ecc74e542db16"),
    "bank0_scheduler_8B50_8F11": (0, 0x8B50, 0x8F12, "3db8088adcdf9f76e0f8eff6997a67cae0c377eaebb47c3583009f758404667c"),
}


def load(path: Path):
    rom = path.read_bytes()
    sha1 = hashlib.sha1(rom).hexdigest().upper()
    if sha1 != EXPECTED_SHA1:
        raise SystemExit(f"unexpected ROM SHA-1 {sha1}; expected {EXPECTED_SHA1}")
    if rom[:4] != b"NES\x1a" or rom[4] != 8:
        raise SystemExit("unexpected iNES/PRG layout")
    prg = rom[16 : 16 + 8 * PRG_BANK_SIZE]
    return [prg[i * PRG_BANK_SIZE : (i + 1) * PRG_BANK_SIZE] for i in range(8)]


def cpu_slice(banks, bank: int, start: int, end: int) -> bytes:
    base = 0xC000 if bank == 7 else 0x8000
    return banks[bank][start - base : end - base]


def find_all(blob: bytes, needle: bytes):
    start = 0
    while True:
        pos = blob.find(needle, start)
        if pos < 0:
            return
        yield pos
        start = pos + 1


def audit(banks):
    for name, (bank, start, end, expected) in RANGE_HASHES.items():
        actual = hashlib.sha256(cpu_slice(banks, bank, start, end)).hexdigest()
        if actual != expected:
            raise AssertionError(f"{name} changed: {actual}")

    fixed = banks[7]
    bank0 = banks[0]

    # DB9C: $4015=0, $04F0=0, $04EF=0, then +0 of eight $15-byte records becomes $FF.
    reset_prefix = cpu_slice(banks, 7, 0xDB9C, 0xDBAA)
    if reset_prefix != bytes.fromhex("A0008C15408CF0048CEF04A9FF994004"):
        raise AssertionError("DB9C reset prefix changed")

    # DBB6 loader: cue id -> DC0E+4*A descriptor; byte0=slot offset, bytes1..3 -> slot +1..+3,
    # slot +0 -> 0. Existing owner clears its old channel bit from the $4015 shadow.
    if cpu_slice(banks, 7, 0xDBD7, 0xDBF0) != bytes.fromhex(
        "B10EAABD4004C9FFF00FBD41042903A8B90ADC2DF0048DF004A001"
    ):
        raise AssertionError("DBB6 preemption path changed")
    if cpu_slice(banks, 7, 0xDBF0, 0xDC09) != bytes.fromhex(
        "B10E9D4104C8B10E9D4204C8B10E9D4304A9009D400468A868AA60"
    ):
        raise AssertionError("DBB6 descriptor copy path changed")

    enable_bits = list(cpu_slice(banks, 0, 0x8F42, 0x8F46))
    clear_masks = list(cpu_slice(banks, 0, 0x8F46, 0x8F4A))
    traits = list(cpu_slice(banks, 0, 0x8F4A, 0x8F4E))
    loader_clear_masks = list(cpu_slice(banks, 7, 0xDC0A, 0xDC0E))
    if enable_bits != [1, 2, 4, 8]:
        raise AssertionError(f"unexpected channel enable bits {enable_bits}")
    if clear_masks != [0x0E, 0x0D, 0x0B, 0x07] or loader_clear_masks != clear_masks:
        raise AssertionError("channel clear-mask table mismatch")
    if traits != [0x00, 0x01, 0x82, 0x43]:
        raise AssertionError(f"unexpected channel trait bytes {traits}")

    # Per-frame scheduler is called in the fixed-bank frame service and arbitrates eight records.
    if bytes.fromhex("20508B") not in fixed:
        raise AssertionError("fixed-bank JSR $8B50 missing")
    if cpu_slice(banks, 0, 0x8B50, 0x8B6D) != bytes.fromhex(
        "A9008DE804EEEE04AABD410429038DE904A80A0A8DEB04B94A8F8DEA04"
    ):
        raise AssertionError("8B50 per-frame scheduler prologue changed")

    # Exact $4015 ownership inside the scheduler: note/start sets a bit, stop clears a bit.
    if cpu_slice(banks, 0, 0x8DB0, 0x8DBF) != bytes.fromhex("200D8EB9428F0DF0048DF0048D1540"):
        raise AssertionError("scheduler channel-enable path changed")
    if cpu_slice(banks, 0, 0x8DFD, 0x8E0D) != bytes.fromhex("200D8EB9468F2DF0048D15408DF00460"):
        raise AssertionError("scheduler channel-disable path changed")

    # Direct absolute/indexed APU writers owned by the scheduler. Indexed bases cover
    # pulse1/pulse2/triangle/noise via Y={0,4,8,12}.
    apu_writers = {
        "4015_reset": "DB9E",
        "4015_enable": "8DBC",
        "4015_disable": "8E06",
        "4000_plus_channel_base_control": ["8E4D", "8E9E"],
        "4001_plus_channel_base_aux": "8DCD",
        "4002_plus_channel_base_timer_low": ["8DD1", "8F0E"],
        "4003_plus_channel_base_timer_high": "8DEA",
    }

    # $04EF is not the $4015 shadow. Stream command $A5 writes it at $8D39; fixed dispatcher
    # consumes non-zero by invoking visual-buffer clear routines $8904/$8AF1, then clears it.
    if cpu_slice(banks, 0, 0x8D33, 0x8D3D) != bytes.fromhex("C9A5D009B10C8DEF04C8"):
        raise AssertionError("04EF stream-command owner changed")
    if cpu_slice(banks, 7, 0xDB41, 0xDB52) != bytes.fromhex("ADEF04F01320048920F18AA9008DEF04"):
        raise AssertionError("04EF fixed consumer changed")

    # Enumerate executable JSR $DBB6 callsites and literal cue IDs without copying descriptor payloads.
    cue_calls = []
    for bank, data in enumerate(banks):
        base = 0xC000 if bank == 7 else 0x8000
        for pos in find_all(data, bytes.fromhex("20B6DB")):
            literal = data[pos - 1] if pos >= 2 and data[pos - 2] == 0xA9 else None
            cue_calls.append({
                "bank": bank,
                "cpu": f"{base + pos:04X}",
                "literal_cue": None if literal is None else f"{literal:02X}",
            })

    return {
        "record_layout": {
            "base": "0440",
            "stride": 0x15,
            "count": 8,
            "lifecycle_plus0": {"FF": "inactive", "00": "newly loaded; initialized on next 8B50 pass"},
            "descriptor_copy": {"plus1": "descriptor byte1", "plus2": "stream pointer low", "plus3": "stream pointer high"},
        },
        "channel_contract": {
            "selector": "slot[+1] & 3",
            "apu_base_offsets": [0, 4, 8, 12],
            "enable_bits_8F42": enable_bits,
            "clear_masks_8F46_and_DC0A": clear_masks,
            "traits_8F4A": traits,
            "priority": "first active slot for a channel in ascending slot order owns that channel for the frame",
        },
        "state_owners": {
            "04F0": "$4015 software shadow; loader clears preempted old-channel bit; scheduler sets/clears and writes $4015",
            "04EF": "stream-command visual-reset latch; fixed dispatcher consumes it via $8904/$8AF1 and clears it",
        },
        "apu_writers": apu_writers,
        "cue_call_count": len(cue_calls),
        "cue_calls": cue_calls,
        "range_hashes": {k: v[3] for k, v in RANGE_HASHES.items()},
    }


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("rom", type=Path)
    args = ap.parse_args()
    print(json.dumps(audit(load(args.rom)), indent=2))


if __name__ == "__main__":
    main()

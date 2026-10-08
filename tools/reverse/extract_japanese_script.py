#!/usr/bin/env python3
"""Extract the indexed Japanese dialogue corpus from a user-owned canonical ROM.

This tool contains no original dialogue bytes. It reconstructs the original
message pointer table, reads message streams from the two CHR banks used as
text storage, decodes the visible font tile IDs to Unicode Japanese, and
records simple immediate message callsites.

Output is derived copyrighted game text and should remain a private/local
analysis artifact unless the user explicitly decides otherwise.
"""

from __future__ import annotations

import argparse
import hashlib
import json
from collections import Counter
from pathlib import Path
import unicodedata

CANONICAL_SHA1 = "F871D9B3DAFDDCDAD5F2ACD71044292E5169064E"
PRG_BANK_SIZE = 0x4000
CHR_4K_SIZE = 0x1000
MESSAGE_COUNT = 251
POINTER_TABLE_BANK = 6
POINTER_TABLE_CPU = 0xA47B
TEXT_CHR_BANKS_4K = (0x15, 0x17)
MESSAGE_ROUTINES = {
    0xE7B3: ("slot_066A", 0xFF),
    0xE7B7: ("slot_066A", 0x00),
    0xE7C3: ("slot_066B", 0xFF),
    0xE7C7: ("slot_066B", 0x00),
}

HIRAGANA = dict(zip(
    range(0x04, 0x32),
    "あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをん",
))
HIRAGANA.update(dict(zip(range(0x32, 0x3B), "ぁぃぅぇぉっゃゅょ")))

KATAKANA = dict(zip(
    range(0x44, 0x72),
    "アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲン",
))
KATAKANA.update(dict(zip(range(0x72, 0x7B), "ァィゥェォッャュョ")))

VISIBLE = {
    0x01: " ",
    0x3D: "、",  # not used by the 251-message corpus; visual font classification
    0x3E: "ー",
    0x3F: "?",
    0x40: "!",
    0x41: "・",
    0x42: ":",  # not used by the corpus
    0x43: "/",  # not used by the corpus
    0x7B: "「",  # font tile exists but is unused by the corpus
    0x7C: "」",
    0x7D: "(",
    0x7E: ")",
    0x7F: "%",
}
VISIBLE.update({0x80 + i: str(i) for i in range(10)})
VISIBLE.update({0x8A + i: chr(ord("A") + i) for i in range(26)})


def _apply_combining_mark(text: str, combining_mark: str) -> str:
    if not text:
        return combining_mark
    return text[:-1] + unicodedata.normalize("NFC", text[-1] + combining_mark)


def decode_message(raw: bytes) -> str:
    out = ""
    for value in raw:
        if value == 0xFF:
            break
        if value == 0xA4:
            out += "\n"
            continue
        if value == 0x3B:  # dakuten overlay
            out = _apply_combining_mark(out, "\u3099")
            continue
        if value == 0x3C:  # handakuten overlay
            out = _apply_combining_mark(out, "\u309A")
            continue
        if value in HIRAGANA:
            out += HIRAGANA[value]
        elif value in KATAKANA:
            out += KATAKANA[value]
        elif value in VISIBLE:
            out += VISIBLE[value]
        else:
            out += f"<TILE_{value:02X}>"
    return out


def _read_u16_le(data: bytes, offset: int) -> int:
    return data[offset] | (data[offset + 1] << 8)


def _load_rom(path: Path) -> tuple[bytes, bytes]:
    rom = path.read_bytes()
    sha1 = hashlib.sha1(rom).hexdigest().upper()
    if sha1 != CANONICAL_SHA1:
        raise SystemExit(f"ROM SHA-1 {sha1} does not match canonical {CANONICAL_SHA1}")
    if len(rom) < 16 or rom[:4] != b"NES\x1A":
        raise SystemExit("not an iNES ROM")

    trainer = 512 if rom[6] & 0x04 else 0
    prg_size = rom[4] * PRG_BANK_SIZE
    chr_size = rom[5] * 0x2000
    start = 16 + trainer
    return rom[start : start + prg_size], rom[start + prg_size : start + prg_size + chr_size]


def _prg_cpu_offset(bank: int, cpu_addr: int) -> int:
    base = 0xC000 if bank == 7 else 0x8000
    if not base <= cpu_addr < base + PRG_BANK_SIZE:
        raise ValueError(f"CPU ${cpu_addr:04X} is outside PRG bank {bank} window")
    return bank * PRG_BANK_SIZE + cpu_addr - base


def _message_pointers(prg: bytes) -> list[int]:
    table = _prg_cpu_offset(POINTER_TABLE_BANK, POINTER_TABLE_CPU)
    values = [_read_u16_le(prg, table + i * 2) for i in range(MESSAGE_COUNT)]
    if values != sorted(values):
        raise SystemExit("message pointer table is unexpectedly non-monotonic")
    if values[-1] >= CHR_4K_SIZE * len(TEXT_CHR_BANKS_4K):
        raise SystemExit("message pointer exceeds reconstructed text storage")
    return values


def _text_blob(chr_data: bytes) -> bytes:
    chunks = []
    for bank in TEXT_CHR_BANKS_4K:
        start = bank * CHR_4K_SIZE
        chunks.append(chr_data[start : start + CHR_4K_SIZE])
    return b"".join(chunks)


def _extract_message(blob: bytes, pointer: int) -> bytes:
    end = blob.find(b"\xFF", pointer)
    if end < 0:
        raise SystemExit(f"message at text offset ${pointer:04X} has no FF terminator")
    return blob[pointer : end + 1]


def _find_immediate_callsites(prg: bytes) -> list[dict[str, object]]:
    result: list[dict[str, object]] = []
    for bank in range(8):
        base = 0xC000 if bank == 7 else 0x8000
        bank_data = prg[bank * PRG_BANK_SIZE : (bank + 1) * PRG_BANK_SIZE]
        for offset in range(0, len(bank_data) - 4):
            # LDA #message_id ; JSR $E7Bx
            if bank_data[offset] != 0xA9 or bank_data[offset + 2] != 0x20:
                continue
            target = bank_data[offset + 3] | (bank_data[offset + 4] << 8)
            if target not in MESSAGE_ROUTINES:
                continue
            message_id = bank_data[offset + 1]
            if message_id >= MESSAGE_COUNT:
                continue
            slot, side_flag = MESSAGE_ROUTINES[target]
            result.append({
                "message_id": message_id,
                "prg_bank": bank,
                "cpu_address": f"0x{base + offset:04X}",
                "routine": f"0x{target:04X}",
                "message_slot": slot,
                "side_flag_0672": f"0x{side_flag:02X}",
            })
    return result


def main() -> None:
    parser = argparse.ArgumentParser(description="Extract Kanketsu Hen Japanese dialogue by message ID")
    parser.add_argument("rom", type=Path)
    parser.add_argument("-o", "--output", type=Path)
    args = parser.parse_args()

    prg, chr_data = _load_rom(args.rom)
    pointers = _message_pointers(prg)
    blob = _text_blob(chr_data)
    callsites = _find_immediate_callsites(prg)
    calls_by_id: dict[int, list[dict[str, object]]] = {i: [] for i in range(MESSAGE_COUNT)}
    for call in callsites:
        calls_by_id[int(call["message_id"])].append(call)

    messages = []
    unknown_tiles = Counter()
    for message_id, pointer in enumerate(pointers):
        raw = _extract_message(blob, pointer)
        text = decode_message(raw)
        for token in text.split("<TILE_")[1:]:
            unknown_tiles[token[:2]] += 1
        messages.append({
            "id": message_id,
            "stable_id": f"MSG_{message_id:03d}",
            "text_storage_offset": f"0x{pointer:04X}",
            "byte_length_including_ff": len(raw),
            "japanese": text,
            "raw_hex": raw.hex(" ").upper(),
            "immediate_callsites": calls_by_id[message_id],
        })

    payload = {
        "format": "SaintSeiyaNesReborn.JapaneseScript.v1",
        "source_rom_sha1": CANONICAL_SHA1,
        "message_count": MESSAGE_COUNT,
        "pointer_table": {"prg_bank": 6, "cpu_address": "0xA47B"},
        "text_storage_chr4k_banks": [f"0x{x:02X}" for x in TEXT_CHR_BANKS_4K],
        "terminator": "0xFF",
        "newline": "0xA4",
        "immediate_callsite_count": len(callsites),
        "unknown_visible_tiles": dict(sorted(unknown_tiles.items())),
        "messages": messages,
    }

    rendered = json.dumps(payload, ensure_ascii=False, indent=2) + "\n"
    if args.output:
        args.output.write_text(rendered, encoding="utf-8")
    else:
        print(rendered, end="")


if __name__ == "__main__":
    main()

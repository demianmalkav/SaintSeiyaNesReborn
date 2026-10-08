#!/usr/bin/env python3
"""Render Kanketsu Hen platform entity metasprites from a user-owned ROM.

No original graphics or ROM bytes are embedded. The tool verifies the canonical
ROM core CRC32, reads the proven platform CHR-bank table, parses the common
metasprite definitions, and writes a PNG contact sheet for reverse-engineering
reference.

Requires Pillow.
"""
from __future__ import annotations

import argparse
import zlib
from pathlib import Path

from PIL import Image, ImageDraw

CANONICAL_CORE_CRC32 = 0x9561798D
PRG_BANK_SIZE = 0x4000
CHR4K_SIZE = 0x1000


def load_rom(path: Path, force: bool = False) -> tuple[list[bytes], list[bytes]]:
    rom = path.read_bytes()
    if len(rom) < 16 or rom[:4] != b"NES\x1a":
        raise SystemExit("not an iNES ROM")
    trainer = 512 if rom[6] & 0x04 else 0
    core = rom[16 + trainer :]
    crc = zlib.crc32(core) & 0xFFFFFFFF
    if crc != CANONICAL_CORE_CRC32 and not force:
        raise SystemExit(
            f"unexpected core CRC32 {crc:08X}; expected {CANONICAL_CORE_CRC32:08X}; "
            "use --force only for intentional revision comparison"
        )
    prg_count = rom[4]
    chr8_count = rom[5]
    start = 16 + trainer
    prg = rom[start : start + prg_count * PRG_BANK_SIZE]
    chr_data = rom[
        start + prg_count * PRG_BANK_SIZE :
        start + prg_count * PRG_BANK_SIZE + chr8_count * 0x2000
    ]
    prg_banks = [
        prg[i : i + PRG_BANK_SIZE]
        for i in range(0, len(prg), PRG_BANK_SIZE)
    ]
    chr4k = [
        chr_data[i : i + CHR4K_SIZE]
        for i in range(0, len(chr_data), CHR4K_SIZE)
    ]
    return prg_banks, chr4k


def switched(prg: list[bytes], bank: int, cpu_addr: int, n: int) -> bytes:
    return prg[bank][cpu_addr - 0x8000 : cpu_addr - 0x8000 + n]


def fixed(prg: list[bytes], cpu_addr: int, n: int) -> bytes:
    return prg[7][cpu_addr - 0xC000 : cpu_addr - 0xC000 + n]


def u16le(raw: bytes) -> int:
    return raw[0] | (raw[1] << 8)


def signed8(v: int) -> int:
    return v - 256 if v >= 128 else v


def parse_metasprite(prg: list[bytes], addr: int) -> list[tuple[int, int, int, int]]:
    """Return (tile, y_offset, x_offset, attribute_or) entries."""
    raw = switched(prg, 3, addr, 0x100)
    count = raw[0]
    p = 1
    entries: list[tuple[int, int, int, int]] = []
    for _ in range(count):
        attr_or = 0
        if raw[p] == 0xFF:
            attr_or = raw[p + 1]
            p += 2
        tile = raw[p]
        y_off = signed8(raw[p + 1])
        x_off = signed8(raw[p + 2])
        p += 3
        entries.append((tile, y_off, x_off, attr_or))
    return entries


def decode_tile(chr_bank: bytes, tile: int, scale: int) -> Image.Image:
    raw = chr_bank[tile * 16 : tile * 16 + 16]
    if len(raw) != 16:
        raise ValueError(f"tile {tile:02X} outside 4 KiB CHR bank")
    img = Image.new("L", (8, 8), 0)
    px = img.load()
    levels = (0, 85, 170, 255)
    for y in range(8):
        p0, p1 = raw[y], raw[y + 8]
        for x in range(8):
            bit = 7 - x
            value = ((p0 >> bit) & 1) | (((p1 >> bit) & 1) << 1)
            px[x, y] = levels[value]
    if scale != 1:
        img = img.resize((8 * scale, 8 * scale), Image.Resampling.NEAREST)
    return img


def render_metasprite(
    chr_bank: bytes,
    entries: list[tuple[int, int, int, int]],
    scale: int,
) -> Image.Image:
    min_x = min(x for _, _, x, _ in entries)
    min_y = min(y for _, y, _, _ in entries)
    max_x = max(x + 8 for _, _, x, _ in entries)
    max_y = max(y + 8 for _, y, _, _ in entries)
    pad = 2
    img = Image.new(
        "L",
        ((max_x - min_x + 2 * pad) * scale, (max_y - min_y + 2 * pad) * scale),
        0,
    )
    for tile, y, x, attr in entries:
        t = decode_tile(chr_bank, tile, scale)
        # Attribute bit 6 is horizontal flip and bit 7 vertical flip on NES OAM.
        if attr & 0x40:
            t = t.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
        if attr & 0x80:
            t = t.transpose(Image.Transpose.FLIP_TOP_BOTTOM)
        img.paste(t, ((x - min_x + pad) * scale, (y - min_y + pad) * scale))
    return img


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("rom", type=Path)
    ap.add_argument("-o", "--output", type=Path, default=Path("platform_entities.png"))
    ap.add_argument("--scale", type=int, default=4)
    ap.add_argument("--force", action="store_true")
    args = ap.parse_args()

    prg, chr4k = load_rom(args.rom, args.force)

    # Fixed $CACF[$02] proves which CHR0 4 KiB banks platform mode selects.
    chr0_by_substate = list(fixed(prg, 0xCACF, 18))
    active_chr0 = sorted(set(chr0_by_substate))

    # The common renderer selects among these three unique animation pointer
    # tables; $B699 is used by two animation indices in the original.
    animation_pointer_tables = [0xB671, 0xB699, 0xB6C1]

    cells: list[tuple[str, Image.Image]] = []
    for chr_index in active_chr0:
        for anim_no, pointer_table in enumerate(animation_pointer_tables):
            for entity_type in range(1, 5):
                ptr = u16le(switched(prg, 3, pointer_table + entity_type * 2, 2))
                meta = parse_metasprite(prg, ptr)
                image = render_metasprite(chr4k[chr_index], meta, args.scale)
                cells.append((f"CHR{chr_index} A{anim_no} T{entity_type}", image))

    cols = 4
    cell_w = max(im.width for _, im in cells) + 24
    cell_h = max(im.height for _, im in cells) + 36
    rows = (len(cells) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * cell_w, rows * cell_h), "white")
    draw = ImageDraw.Draw(sheet)
    for i, (label, image) in enumerate(cells):
        col, row = i % cols, i // cols
        ox, oy = col * cell_w, row * cell_h
        draw.text((ox + 4, oy + 4), label, fill="black")
        rgb = image.convert("RGB")
        x = ox + (cell_w - rgb.width) // 2
        y = oy + 24
        sheet.paste(rgb, (x, y))

    sheet.save(args.output)
    print("platform substate -> CHR0:", chr0_by_substate)
    print("active CHR0 banks:", active_chr0)
    print("wrote", args.output)


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""Render Kanketsu Hen platform sprite resources from a user-owned ROM.

No original graphics or ROM bytes are embedded. The shared bank-3 compositor is
used both by the player (indices $00-$04) and by primary platform entities
(types $05-$0F). This tool reconstructs either domain against the proven CHR0
banks selected by platform substate and can also render the separately selected
$A908/$9B93 direct sprite resources.

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

DYNAMIC_PLAYER_TABLES = [0xB671, 0xB699, 0xB6C1]
ALL_SHARED_TABLES = [
    0xB671,
    0xB699,
    0xB6C1,
    0xB6E9,
    0xB711,
    0xB739,
    0xB761,
    0xB789,
    0xB7B1,
    0xB7D9,
    0xB801,
]
DIRECT_BLANK_DEFINITION = 0xB647


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
        if attr & 0x40:
            t = t.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
        if attr & 0x80:
            t = t.transpose(Image.Transpose.FLIP_TOP_BOTTOM)
        img.paste(t, ((x - min_x + pad) * scale, (y - min_y + pad) * scale))
    return img


def render_direct_group(chr_bank: bytes, tiles: list[int], scale: int, columns: int = 2) -> Image.Image:
    rows = (len(tiles) + columns - 1) // columns
    img = Image.new("L", (columns * 8 * scale, rows * 8 * scale), 0)
    for i, tile in enumerate(tiles):
        part = decode_tile(chr_bank, tile, scale)
        x = (i % columns) * 8 * scale
        y = (i // columns) * 8 * scale
        img.paste(part, (x, y))
    return img


def add_shared_cells(
    cells: list[tuple[str, Image.Image]],
    prg: list[bytes],
    chr_bank: bytes,
    chr_index: int,
    tables: list[int],
    indices: range,
    scale: int,
) -> None:
    for pointer_table in tables:
        for index in indices:
            ptr = u16le(switched(prg, 3, pointer_table + index * 2, 2))
            meta = parse_metasprite(prg, ptr)
            image = render_metasprite(chr_bank, meta, scale)
            cells.append((f"CHR{chr_index} ${pointer_table:04X} I{index:02X} ${ptr:04X}", image))


def add_special_cells(
    cells: list[tuple[str, Image.Image]],
    prg: list[bytes],
    chr_bank: bytes,
    chr_index: int,
    scale: int,
) -> None:
    # Direct blank/flash definition selected at $B97C.
    cells.append((
        f"CHR{chr_index} direct $B647",
        render_metasprite(chr_bank, parse_metasprite(prg, DIRECT_BLANK_DEFINITION), scale),
    ))

    # $A908 attached primary visual: nonzero $C0E3 entries create one tile.
    attached_tiles = list(fixed(prg, 0xC0E3, 11))
    for offset, tile in enumerate(attached_tiles):
        if tile == 0:
            continue
        entity_type = 0x05 + offset
        cells.append((
            f"CHR{chr_index} A908 T{entity_type:02X} tile{tile:02X}",
            render_direct_group(chr_bank, [tile], scale, columns=1),
        ))

    # Independent $9B93 ordinary bootstrap resources. Selector zero is no-spawn.
    sprite_bases = list(switched(prg, 1, 0x9B65, 7))
    for selector, base in enumerate(sprite_bases[1:], start=1):
        cells.append((
            f"CHR{chr_index} 9B93 S{selector} 2x2",
            render_direct_group(chr_bank, [base, base + 1, base + 2, base + 3], scale),
        ))
        # Substate $0C skips two sprite values before the second row.
        cells.append((
            f"CHR{chr_index} 9B93 S{selector} sub0C",
            render_direct_group(chr_bank, [base, base + 1, base + 4, base + 5], scale),
        ))

    # Dedicated substate-$0D bootstrap owns one direct sprite tile $8C.
    cells.append((
        f"CHR{chr_index} 9B93 sub0D tile8C",
        render_direct_group(chr_bank, [0x8C], scale, columns=1),
    ))


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("rom", type=Path)
    ap.add_argument("-o", "--output", type=Path, default=Path("platform_visual_resources.png"))
    ap.add_argument("--scale", type=int, default=4)
    ap.add_argument(
        "--domain",
        choices=("player", "primary", "all"),
        default="primary",
        help="shared compositor indices to render; primary is types $05-$0F",
    )
    ap.add_argument("--include-special", action="store_true",
                    help="also render direct $B647, $A908 and $9B93 resources")
    ap.add_argument("--force", action="store_true")
    args = ap.parse_args()

    prg, chr4k = load_rom(args.rom, args.force)

    chr0_by_substate = list(fixed(prg, 0xCACF, 18))
    active_chr0 = sorted(set(chr0_by_substate))

    if args.domain == "player":
        tables = DYNAMIC_PLAYER_TABLES
        indices = range(0x00, 0x05)
    elif args.domain == "primary":
        tables = ALL_SHARED_TABLES
        indices = range(0x05, 0x10)
    else:
        tables = ALL_SHARED_TABLES
        indices = range(0x00, 0x10)

    cells: list[tuple[str, Image.Image]] = []
    for chr_index in active_chr0:
        add_shared_cells(cells, prg, chr4k[chr_index], chr_index, tables, indices, args.scale)
        if args.include_special:
            add_special_cells(cells, prg, chr4k[chr_index], chr_index, args.scale)

    if not cells:
        raise SystemExit("no visual resources selected")

    cols = 4
    cell_w = max(im.width for _, im in cells) + 32
    cell_h = max(im.height for _, im in cells) + 40
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
    print("domain:", args.domain, "shared tables:", [f"${table:04X}" for table in tables])
    print("special resources:", args.include_special)
    print("wrote", args.output)


if __name__ == "__main__":
    main()

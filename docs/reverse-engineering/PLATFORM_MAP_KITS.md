# Platform map kits by substate

This document extends `PLATFORM_MAP_FORMAT.md` to all platform substates `$00-$11`.

The same loader protocol is reused everywhere, but special substates can select a different PRG bank and a different 256-entry metatile-definition table.

## Generic kit layout

For every mapped platform kit:

- metatile definitions occupy `0x400` bytes starting at `metatile_base`;
- first page attributes begin at `metatile_base + 0x400`;
- first page grid begins at `metatile_base + 0x440`;
- additional reusable pages advance by `0xF0` bytes;
- each page remains `0x40` attribute bytes + `0xB0` descriptor bytes.

Thus the special areas are not separate engines. They are alternate data kits plugged into the same renderer/collision streamer.

## Exact substate kits

| `$02` | PRG bank | metatile base | pages | pool-page sequence |
|---:|---:|---:|---:|---|
| `$00` | 2 | `$8000` | 6 | `4,18,11,4,5,0` |
| `$01` | 2 | `$8000` | 7 | `4,16,10,6,9,16,0` |
| `$02` | 2 | `$8000` | 8 | `4,10,6,3,17,14,9,0` |
| `$03` | 2 | `$8000` | 9 | `4,18,6,2,3,19,9,16,0` |
| `$04` | 2 | `$8000` | 10 | `4,6,3,17,3,2,8,19,9,0` |
| `$05` | 2 | `$8000` | 11 | `4,18,6,3,1,2,3,7,1,9,0` |
| `$06` | 2 | `$8000` | 12 | `4,5,11,6,7,3,19,14,9,10,11,0` |
| `$07` | 2 | `$8000` | 13 | `4,18,6,1,3,2,7,12,13,14,9,5,0` |
| `$08` | 2 | `$8000` | 14 | `4,16,11,10,6,12,13,7,3,14,15,9,5,0` |
| `$09` | 2 | `$8000` | 15 | `4,6,12,13,3,14,7,15,14,9,16,10,5,4,0` |
| `$0A` | 2 | `$8000` | 16 | `4,16,11,6,19,14,3,2,3,12,13,17,9,18,5,0` |
| `$0B` | 2 | `$8000` | 16 | `4,16,6,12,13,17,19,14,8,7,3,17,19,9,5,0` |
| `$0C` | 2 | `$96C0` | 12 | `0,1,2,3,4,5,6,0,3,7,8,9` |
| `$0D` | 2 | `$A420` | 12 | `0,1,2,3,4,5,6,6,7,8,8,9` |
| `$0E` | 2 | `$B180` | 12 | `0,1,2,3,4,5,4,5,6,7,7,8` |
| `$0F` | 2 | `$8000` | 3 | `9,5,0` |
| `$10` | 3 | `$8001` | 12 | `0,1,2,3,4,5,1,6,7,3,8,9` |
| `$11` | 1 | `$8019` | 2 | `0,1` |

The page counts are derived from the actual fixed-bank page-pointer-list boundaries, not guessed from stage length.

## Special substates are event-selected

Main story progress normally maps values 0..11 directly to substates `$00-$0B`.

Bank-5 event code separately forces:

- `$02=$0C` at `$9DBF-$9DC1`;
- `$02=$0E` at `$A468-$A46A`;
- `$02=$0D` at `$A6E7-$A6E9`.

They are therefore transient/special-area maps, not ordinary sequential Houses.

Later main story progress is nonlinear:

- progress 12 -> `$02=$10`;
- progress 13 -> `$02=$0F`;
- progress 14 -> `$02=$11`.

External walkthrough/TAS order makes progress 12 strongly consistent with the post-Pisces Palace Roses section, progress 13 with the short final approach/Saga sequence, and progress 14 with the ending/Athena-statue segment. These narrative labels remain separate from the mechanically confirmed numeric mapping.

## Background and sprite CHR split

Platform initialization sets PPUCTRL shadow `$77=$90`.

NES PPUCTRL `$90` means:

- background pattern table = `$1000` -> MMC1 CHR bank 1;
- sprite pattern table = `$0000` -> MMC1 CHR bank 0;
- NMI enabled.

This confirms the earlier metasprite observations:

- CHR0 tables documented in `PLATFORM_CHR_MAP.md` are the sprite graphics;
- CHR1 tables are the platform background/metatile graphics.

For normal substates `$00-$0B`, CHR1 is bank 15. The four-byte metatile definitions in bank 2 can therefore be rendered unambiguously against CHR4K bank 15 for visual classification.

## Collision-family visual linkage

Because the collision descriptor is also the metatile index, the behavior families `$78/$80/$88/$90/$E0/$F0` can now be inspected directly in the corresponding background CHR.

Initial grayscale reconstruction shows mirrored diagonal/edge-like graphics in the `$78/$80/$88` families, providing visual support for the earlier slope/corner inference. `$E0-$EF` contains the strongly blocking terrain family. Exact art-semantic names should still be assigned per used metatile rather than globally from one contact sheet.

## Tool support

`tools/reverse/extract_platform_maps.py` v2 now handles all 18 map substates and determines automatically:

- PRG data bank;
- metatile base;
- page-list length;
- reusable pool page IDs;
- optional 16×11 grids;
- optional 256-entry metatile definitions.

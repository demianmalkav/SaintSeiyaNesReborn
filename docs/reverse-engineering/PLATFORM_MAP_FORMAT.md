# Platform map format and page composition

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: main-stage page layout, metatile definition format, collision-grid representation and scrolling-buffer behavior are statically reconstructed.

## Architectural overview

The platform renderer and the collision system consume the same logical metatile map.

The original does **not** infer collision from graphics. Instead:

1. a page contains metatile descriptor/index bytes;
2. each descriptor is copied into the RAM collision window;
3. the same descriptor indexes a four-byte 2×2 tile definition for rendering;
4. the player and entity collision samplers later read the descriptor byte back from RAM.

This separation is a strong architectural precedent for REBORN: visual art and collision semantics should remain distinct even when they share one stage-design record.

## Main bank-2 layout

For the normal main-sequence platform substates `$00-$0B`, PRG bank 2 is selected and contains a compact reusable level kit.

### `$8000-$83FF` — metatile definitions

Exactly `0x400` bytes are available before the first page block:

- 256 definitions;
- 4 bytes per definition.

The loader multiplies a descriptor by four and indexes this table.

Because PPUCTRL uses vertical increment while each column is expanded, the four bytes behave as the 2×2 8-pixel-tile composition of one 16×16 metatile.

### Reusable page blocks from `$8400`

Each page block is exactly `0xF0` (240) bytes:

- first `0x40` = 64 bytes of NES nametable attribute data;
- next `0xB0` = 176 metatile descriptors.

First block:

- attributes `$8400-$843F`
- grid `$8440-$84EF`

Second block begins at `$84F0`, with grid at `$8530`, and so on.

The normal page pool contains twenty reusable blocks through the block whose grid begins at `$9610`.

## Logical page geometry

The 176 descriptor bytes form:

- **16 columns**
- **11 rows**
- **column-major storage**

Index formula:

`index = column * 11 + row`

This matches the collision sampler at bank 0 `$B595/$B5C6`, which derives the same `column*11 + row` address from world X/Y.

At 16×16 pixels per metatile, one logical page spans:

- 256 px horizontally
- 176 px of active platform geometry vertically.

The rest of the NES frame is available to HUD/UI/non-collision presentation.

## Initial page rendering — fixed `$CCED-$CDBE`

Initial platform setup:

1. `$D19A` resolves the stage/substate page-pointer list;
2. page 0 grid pointer becomes `$1E/$1F`;
3. `$D1B6` selects the metatile-definition base `$22/$23`;
4. `$CD00+` copies all 176 descriptors into RAM at `$0202+`;
5. each descriptor is multiplied by four and expanded to PPU tile bytes;
6. the 64 bytes immediately before the grid pointer are copied to PPU attribute memory.

The routine intentionally sets PPU increment mode to 32 while expanding a page. Consequently it draws one vertical 16-pixel metatile column at a time, matching the column-major logical map.

## Double-buffered collision window

Collision queries choose between two 176-byte RAM page buffers:

- `$0202-$02B1`
- `$02B2-$0361`

The active page is selected from horizontal page/camera parity (`$45 & 1`).

This explains the address math already documented in `COLLISION_PROBES.md`.

As scrolling crosses the level, one logical page can remain active while the other is filled with incoming columns.

## Incremental streaming — fixed `$CEEA+`

The scrolling path does not rebuild a whole page every frame.

It:

1. resolves a later page pointer using `$D19A` and page index derived from `$45`;
2. computes the source column from horizontal scroll `$44`;
3. copies **11 descriptor bytes** — exactly one metatile column — into the appropriate collision buffer;
4. expands that same column through the metatile-definition table;
5. updates the corresponding nametable/attribute support data.

Thus map streaming, rendering and collision remain synchronized at a one-column granularity.

## Page-pointer lists

Fixed table `$CFFA` contains one pointer per platform substate to a page-pointer list.

For normal substates `$00-$0B`, those lists are in fixed ROM around `$D088-$D199` and point into the reusable page pool in PRG bank 2.

Main page counts are:

| `$02` | pages |
|---:|---:|
| `$00` | 6 |
| `$01` | 7 |
| `$02` | 8 |
| `$03` | 9 |
| `$04` | 10 |
| `$05` | 11 |
| `$06` | 12 |
| `$07` | 13 |
| `$08` | 14 |
| `$09` | 15 |
| `$0A` | 16 |
| `$0B` | 16 |

An earlier provisional reading of 17 pages for `$0B` was rejected: the next word after the sixteenth pointer is already executable code/data outside the page-pointer domain.

## Exact main-stage page reuse

Pool page IDs below are zero-based from grid pointer `$8440`, with each next pool block adding `$F0`.

| `$02` | reusable page sequence |
|---:|---|
| `$00` | `4,18,11,4,5,0` |
| `$01` | `4,16,10,6,9,16,0` |
| `$02` | `4,10,6,3,17,14,9,0` |
| `$03` | `4,18,6,2,3,19,9,16,0` |
| `$04` | `4,6,3,17,3,2,8,19,9,0` |
| `$05` | `4,18,6,3,1,2,3,7,1,9,0` |
| `$06` | `4,5,11,6,7,3,19,14,9,10,11,0` |
| `$07` | `4,18,6,1,3,2,7,12,13,14,9,5,0` |
| `$08` | `4,16,11,10,6,12,13,7,3,14,15,9,5,0` |
| `$09` | `4,6,12,13,3,14,7,15,14,9,16,10,5,4,0` |
| `$0A` | `4,16,11,6,19,14,3,2,3,12,13,17,9,18,5,0` |
| `$0B` | `4,16,6,12,13,17,19,14,8,7,3,17,19,9,5,0` |

This is a major piece of the original level-design DNA: the game builds increasingly long approaches by recombining a comparatively small library of authored page chunks rather than storing twelve entirely separate levels.

## Collision descriptor = metatile index

The descriptor copied to `$0202/$02B2` is the same byte used to choose the 2×2 visual metatile definition.

Therefore descriptor ranges documented in `COLLISION_BEHAVIOR.md` simultaneously identify families in the visual metatile table.

This gives us a direct path to resolve the remaining semantic names:

`descriptor behavior -> metatile definition -> CHR graphics -> visual surface identity`.

No pixel-based collision inference is required.

## Special substates

`$D1B6` changes the metatile-definition base for `$02 >= $0C`, and `$CED3` can map a different PRG bank for substates `$10/$11`.

Thus special/event areas are allowed to use their own page libraries/metatile kits while preserving the same high-level streaming protocol.

These special banks/bases will be documented separately as their stage identities are resolved.

## Reproducible extractor

`tools/reverse/extract_platform_maps.py` validates the canonical ROM and emits:

- main stage page counts;
- fixed page-pointer-list addresses;
- page grid pointers;
- reusable pool IDs;
- optionally all 16×11 descriptor grids;
- optionally all 256 four-byte metatile definitions.

Generated map JSON is derived game data and should remain a local/private analysis artifact rather than being committed to the public repository.

## REBORN consequence

A native stage representation can preserve this compositional idea while removing 8-bit constraints:

- `Stage` = ordered list of reusable chunks;
- `Chunk` = logical collision/environment cells + visual layer references + spawn/event metadata;
- original 16×11 pages can be imported as compatibility chunks;
- modern chunks can be larger, multi-layered and visually richer without changing the semantic model.

This is a better foundation than recreating each NES screen as a bitmap.

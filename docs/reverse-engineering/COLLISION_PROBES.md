# Platform collision probes

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: probe-generation geometry is statically reconstructed. Tile-class semantics remain partially open.

## Generator

Bank 0 `$B3E2+` generates the eight environment descriptors stored at `$4F-$56`.

Helper `$B584` builds horizontal world position from:

`world_x = scroll_x + player_x`

using `$44/$45` plus `$3F`.

Helpers `$B595/$B5C6` convert pixel coordinates into the 16×16 tilemap address and return the tile/class byte at that location.

Therefore `$4F-$56` are not abstract collision flags: they are actual tile samples at fixed positions around the player.

## Probe geometry

Let:

- `X = horizontal world position` from `$44/$45 + $3F`;
- `Y = player vertical coordinate $40`;
- `T = (Y + 8) & $F0`, i.e. the 16-pixel-aligned row around the upper/body probes.

The probes are:

| RAM | X sample | Y sample | Working spatial role |
|---:|---:|---:|---|
| `$54` | `X + 0` | `T` | upper-left |
| `$56` | `X + 8` | `T` | upper-center |
| `$51` | `X + 16` | `T` | upper-right |
| `$53` | `X + 0` | `T + 16` normally | lower-left side |
| `$50` | `X + 16` | `T + 16` normally | lower-right side |
| `$55` | `X - 8` | `Y + 32` | bottom-left / ledge probe |
| `$4F` | `X + 8` | `Y + 32` | bottom-center / floor probe |
| `$52` | `X + 24` | `Y + 32` | bottom-right / ledge probe |

When `$40 == $88`, the lower-side offset used for `$50/$53` is `$18` rather than `$10`, producing `T + 24`. This is an explicit special case in the original.

## Shape implied by the probes

The layout forms a three-by-three-like collision envelope with the middle-left/middle-right layer and no exact center-body tile:

```text
      upper row
  $54   $56   $51

  $53         $50

  $55   $4F   $52
      bottom row
```

The bottom side probes extend eight pixels beyond the nominal left/right body edges (`X-8`, `X+24`). This explains why they are useful for ledge, slope or floor-edge decisions rather than just solid-body overlap.

## Consumption evidence

Examples from bank 3:

- ordinary right movement inspects `$50/$51`;
- ordinary left movement inspects `$53/$54`;
- vertical/fall logic inspects `$4F`, `$52` and `$55`;
- landing code uses `$4F` as the central floor descriptor and snaps `$40` to tile-aligned heights for accepted tile classes;
- special values `$F8/$F9` at the floor probe are treated separately in the lower-screen/fall logic.

This gives functional roles to the geometry without yet naming every tile-class byte.

## Remaining tile-class work

The engine compares probe values against ranges/values including:

- `$78`
- `$80-$87`
- `$88-$8F`
- `$90+`
- `$E0+`
- `$F0+`
- `$F8/$F9`
- `$FF`

The next task is to connect these class values back to metatile definitions and stage data so that ORIGINAL SPEC can say not only *where* the player probes, but what each class means: solid wall, one-way platform, hazard, edge/slope, scripted trigger, etc.

## REBORN consequence

A native implementation does not need to emulate the tile-address arithmetic, but it should preserve the semantic probe envelope if parity is desired. The clean representation is a player collision shape plus eight named environment queries derived from these original offsets.

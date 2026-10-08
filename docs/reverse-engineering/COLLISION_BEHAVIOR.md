# Platform collision descriptor behavior

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: exact branch behavior reconstructed statically. Visual/metatile names remain intentionally neutral until the descriptor families are tied back to stage art.

## Collision buffer layout

The sampler does not read arbitrary ROM addresses directly while moving the player. Bank 0 `$B595-$B5D7` addresses a RAM tile/descriptor buffer.

Horizontal world position is quantized to 16 px columns. The address math is:

`address = page_base + column * 11 + row`

with two 16-column pages:

- page 0 base `$0202`
- page 1 base `$02B2`

Each page therefore represents a **16 × 11** column-major descriptor grid. Together they form the two-page working collision window used by the eight probes around the player.

This is important for REBORN: the original collision system is already logically separated from rendering. Movement consumes a compact stage-descriptor grid rather than testing sprite pixels.

## Behavioral descriptor families

The 6502 code repeatedly classifies descriptors by ranges. The following names describe only proven behavior, not yet visual meaning.

| Range | Proven behavior summary |
|---|---|
| `< $78` | generally non-support/non-blocking in ordinary side tests |
| `$78-$7F` | blocks ordinary **left lower-side** movement only |
| `$80-$87` | blocks movement to the **right**; bottom-right probe pushes player left |
| `$88-$8F` | blocks movement to the **left**; bottom-left probe pushes player right |
| `$90-$DF` | supports center-floor landing but does not block ordinary lateral movement |
| `$E0-$EF` | strongest solid family: blocks both lateral directions and rising head/ceiling probe |
| `$F0+` | laterally passable in the ordinary path, but participates in a distinct center-floor snap at half-tile height; `$F8/$F9/$FF` have extra special cases |

These ranges are implementation-level behavior and should be safe to reproduce even before their art/design semantics are named.

## Ordinary horizontal movement

### Moving right

Lower-right `$50` and, in the relevant stage family, upper-right `$51` block only for:

- `$80-$87`
- `$E0-$EF`

Everything else passes the ordinary right-side test.

### Moving left

Lower-left `$53` blocks for:

- `$78-$7F`
- `$88-$8F`
- `$E0-$EF`

Upper-left `$54` blocks for:

- `$88-$8F`
- `$E0-$EF`

The asymmetry is deliberate and is strong evidence that the descriptor byte carries surface geometry, not just a boolean `solid` flag.

## Airborne horizontal movement

Directional-jump collision is slightly less restrictive than grounded movement.

### Airborne right

- main/lower and late-floor tests (`$50/$52`) block `$80-$87` and `$E0-$EF`;
- upper-side `$51` blocks only `$E0-$EF`.

### Airborne left

- main/lower and late-floor tests (`$53/$55`) block `$88-$8F` and `$E0-$EF`;
- upper-side `$54` blocks only `$E0-$EF`.

The grounded-only `$78-$7F` left-lower rule disappears in this airborne path.

## Ceiling collision

While rising, head probe `$56` stops the upward phase only for:

`$E0-$EF`

The engine resets jump phase and enters the downward/fall family `$50`.

Thus `$E0-$EF` is proven to be a full body/ceiling-solid family.

## Bottom-side correction

During falling/landing the wide bottom probes perform one-pixel horizontal corrections:

- `$55` bottom-left causes `player_x += 1` for `$88-$8F` or `$E0-$EF`;
- `$52` bottom-right causes `player_x -= 1` for `$80-$87` or `$E0-$EF`.

This makes the `$80-$87` and `$88-$8F` families mirror each other geometrically.

A slope/corner interpretation is plausible, but remains `INFERRED` until metatile art is tied to the descriptors.

## Center-floor landing and snap heights

Center probe `$4F` performs the actual ordinary floor landing.

For player Y below `$86`:

### Descriptor `< $80`

No landing.

### Descriptor `$80-$EF`

Landing occurs only while the player's low Y nibble is `< 6`.

On landing:

`player_y = player_y & $F0`

So this family snaps to the 16-pixel row boundary.

### Descriptor `$F0-$FE`

Landing occurs only while the player's low Y nibble is `>= 8`.

On landing:

`player_y = (player_y & $F0) | $08`

This is a distinct **8-pixel-offset floor height**.

### Descriptor `$FF`

`$FF` bypasses the normal snap calculation and uses dynamic value `$039B` as the Y target.

This is proven dynamic/special floor behavior; exact stage semantics remain open.

## `$F8/$F9` lower-screen special case

When the player is already in the lower vertical region (`Y >= $86`), center-floor `$F8` and `$F9` enter a special path that forces Y to `$88` and also interacts with `$76`.

This is not ordinary floor support and should remain a separate hazard/event behavior in REBORN until its stage meaning is fully classified.

## Clean-room executable rules

`tools/physics/collision_rules.py` implements these predicates without ROM bytes. It includes:

- grounded right/left blocking;
- airborne right/left blocking;
- ceiling blocking;
- bottom-side correction;
- center-floor snap behavior;
- neutral family labels based only on observed semantics.

The module is intended as a parity primitive for the future native movement implementation.

## Current interpretation boundary

`CONFIRMED`:

- descriptor ranges and branch outcomes;
- asymmetric side blocking;
- floor support thresholds;
- full-solid behavior of `$E0-$EF`;
- half-row Y snap behavior of `$F0+`;
- two-page 16×11 column-major RAM collision buffer.

`INFERRED`:

- `$80-$87` / `$88-$8F` are opposite slope/corner families;
- `$90-$DF` are one-way/top-only floor-like metatiles;
- `$F0+` visually represent half-height surfaces or a related geometry family.

The inferred names should not be promoted until the stage/metatile renderer is joined to this collision map.

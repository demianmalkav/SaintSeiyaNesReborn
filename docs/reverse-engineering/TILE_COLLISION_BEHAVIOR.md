# Tile collision descriptor behavior

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: behavioral classification from static movement code. The numeric descriptor ranges below are proven to produce the listed movement decisions, but their visual/narrative tile identities are intentionally left unnamed until correlated with map graphics or runtime observation.

## Why this is not a binary solid/empty map

Bank 0 samples eight map bytes around the player and bank 3 interprets them differently depending on:

- which probe produced the byte;
- travel direction;
- airborne phase;
- player Y alignment within the 16-pixel tile row;
- current substage (`$02`) in a few side-collision paths.

Thus descriptors encode directional/shape behavior rather than a single `solid` flag.

## Floor-center support (`$4F`)

During downward/fall resolution:

- values `< $80` do not produce ordinary floor landing;
- values `$80-$EF` can act as floor support, with landing snap occurring when the player's low Y nibble is near the upper part of the tile row;
- values `$F0+` also support landing but use a different vertical snap threshold/offset;
- `$FF` has a dedicated path that uses another stored vertical value;
- `$F8/$F9` have a special path when the player is sufficiently low on screen (`y >= $86`): player Y is forced to `$88` and `$76` is used as a one-shot/timer latch.

Therefore `$F0+` is not simply 'non-solid'. It is a special high descriptor family with different vertical geometry.

## Head probe (`$56`)

During upward jump resolution:

- descriptors `$E0-$EF` terminate the ascent and force transition into the `$50` downward/fall family;
- values `< $E0` and `$F0+` do not take that specific ceiling-stop branch.

This makes `$E0-$EF` a confirmed **ceiling-obstructing family** for the upper-center probe.

## Right-side movement (`$50/$51`)

For the lower-right probe `$50` in ordinary grounded movement, the code blocks the normal rightward displacement for:

- `$80-$87`;
- `$E0-$EF`.

It permits/continues through other ranges including:

- `< $80`;
- `$88-$DF`;
- `$F0+`.

In substages `$0C-$0E`, the upper-right probe `$51` is additionally checked. Its `$80-$87` and `$E0-$EF` ranges can block rightward motion under that mode-specific path.

The fact that `$88-$DF` may permit side passage while still participating in floor support is direct evidence of one-way/slope/shape-style collision encoding.

## Left-side movement (`$53/$54`)

The lower-left probe `$53` uses a different set of blocking bands in ordinary grounded movement:

- `$78-$7F` blocks;
- `$88-$8F` blocks;
- `$E0-$EF` blocks.

Ranges such as `$80-$87`, `$90-$DF`, `$F0+`, and `< $78` follow the non-blocking branch in this particular leftward test.

In substages `$0C-$0E`, upper-left `$54` adds another mode-specific obstruction check centered on `$88-$8F` and `$E0-$EF`.

The asymmetric right/left thresholds strongly suggest descriptors include edge or surface geometry rather than merely material type.

## Airborne lateral resolution

Jump/fall handlers test the same side and floor probes with phase-dependent rules.

Examples:

- when moving right in air, lower-right `$50` ranges `$80-$87` and `$E0-$EF` can force a return to jump family `$30`, preventing penetration;
- corresponding right-upper `$51` and right-floor `$52` checks become active at later jump phases;
- leftward air movement mirrors this through `$53/$54/$55` but retains the same asymmetric range families observed on the ground.

This means the collision profile is sampled as the player travels through a tile, not only at final landing.

## Conservative descriptor taxonomy

The following names are deliberately behavioral, not visual:

| Descriptor range | Proven behavior examples |
|---|---|
| `< $78` | generally passable in ordinary side tests; `<$80` lacks ordinary floor support |
| `$78-$7F` | left-side obstruction band |
| `$80-$87` | right-side obstruction band; floor-support-capable |
| `$88-$8F` | left-side / selected airborne obstruction band; floor-support-capable |
| `$90-$DF` | floor-support-capable; generally less restrictive in side tests shown so far |
| `$E0-$EF` | strong obstruction family: side blocking and ceiling stop |
| `$F0-$F7` | special floor-support/geometry family; often bypasses ordinary side-block test |
| `$F8-$F9` | special floor interaction with dedicated Y/latch behavior |
| `$FA-$FE` | high special family; exact identities unresolved |
| `$FF` | dedicated vertical/floor branch using stored fallback Y state |

Do **not** translate this table into names such as `ladder`, `spikes`, `stairs`, `platform` or `wall` without a map/graphic/runtime correlation.

## REBORN implication

The original map format effectively exposes a collision descriptor, not merely a binary tile. For faithful import we should model an explicit collision-profile enum/record whose behavior can be reproduced per probe/direction. Later REBORN maps can use modern polygons while preserving a compatibility decoder for the original descriptor rules.

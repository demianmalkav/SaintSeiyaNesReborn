# Platform CHR bank selection and entity metasprites

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: static mapping confirmed from fixed-bank MMC1 callsites. Visual identity labels remain separate from the proven bank linkage.

## Platform CHR setup

Fixed routine around `$CB04` uses high-level platform substate `$02` as a direct index into two 18-byte tables.

- table `$CACF[$02]` -> MMC1 CHR bank 0 via `$C078`;
- table `$CABD[$02]` -> shadow `$75` and MMC1 CHR bank 1 via `$C096`.

MMC1 is in 4 KiB CHR mode, so these are 4 KiB bank numbers, not the original iNES 8 KiB units.

## Exact table

| `$02` | CHR 0 | CHR 1 |
|---:|---:|---:|
| `$00` | 25 | 15 |
| `$01` | 25 | 15 |
| `$02` | 25 | 15 |
| `$03` | 25 | 15 |
| `$04` | 25 | 15 |
| `$05` | 25 | 15 |
| `$06` | 25 | 15 |
| `$07` | 25 | 15 |
| `$08` | 25 | 15 |
| `$09` | 25 | 15 |
| `$0A` | 25 | 15 |
| `$0B` | 25 | 15 |
| `$0C` | 29 | 19 |
| `$0D` | 29 | 19 |
| `$0E` | 27 | 17 |
| `$0F` | 25 | 15 |
| `$10` | 25 | 15 |
| `$11` | 25 | 30 |

This strongly groups platform substates `$00-$0B/$0F-$10`, `$0C-$0D`, `$0E`, and `$11` into different visual-resource sets.

## Why this closes the metasprite ambiguity

Entity rendering in bank 3 selects metasprite definitions through pointer tables around `$B669/$B671/$B699/$B6C1`.

Before the CHR selection table was found, those definitions could be combined with any of 32 possible 4 KiB CHR banks and many accidental patterns appeared plausible.

Using the actual platform CHR0 banks from `$CACF` — 25, 27 and 29 — the type-1 through type-4 definitions consistently assemble into coherent multi-tile humanoid figures across their animation variants.

Therefore the following chain is now statically grounded:

`platform substate $02 -> CHR bank -> metasprite pointer table -> entity type -> coherent sprite`

This is enough to trust our metasprite parser and to use it for visual cross-identification, while still keeping character/enemy names separate until the art is unambiguous.

## Base entity render definitions

For one common animation table (`$B699`), entity types 1–4 resolve to metasprite definitions:

- type 1 -> `$ACC4`, 7 hardware sprites;
- type 2 -> `$AD0F`, 6 hardware sprites;
- type 3 -> `$AD54`, 6 hardware sprites;
- type 4 -> `$AD9C`, 7 hardware sprites.

Other animation phases use sibling definitions selected from `$B671/$B6C1`.

The definitions contain tile index, Y offset and X offset triples, with optional attribute overrides marked by `$FF`.

## Original hardware implication

A single ordinary entity can consume 6–7 of the NES's 64 hardware sprites, before counting the player, projectiles, UI and other objects. This helps explain why the engine uses tightly bounded entity/object slots and aggressive OAM reuse.

REBORN should preserve the animation/pose semantics, not the original sprite-count scarcity.

## Reproducible renderer

`tools/reverse/render_platform_entities.py` reads a user-provided canonical ROM and reconstructs entity types 1–4 against the actual platform CHR0 banks 25/27/29.

The script contains no original graphics; all tile bytes are read from the supplied ROM at runtime. Generated PNGs are reference artifacts and should stay outside the public repository.

## Remaining visual work

1. join `$02` to named scenario/House states;
2. compare the reconstructed type-1..4 figures with in-game captures to assign visual identities;
3. decode type `$05-$0F` metasprite definitions and special-object sprite tables;
4. map CHR1 usage to backgrounds/UI versus sprite pattern-table selection in each state;
5. preserve only semantic descriptions and clean-room tools in GitHub, not extracted original art.

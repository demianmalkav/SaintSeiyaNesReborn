# Platform CHR bank selection and shared metasprite resources

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: **static platform CHR routing, dynamic `$9915` CHR0 overrides and shared player/primary-entity metasprite resources mechanically closed.** Visual identity labels remain separate from the proven resource linkage.

## Platform CHR setup

Fixed routine around `$CB04` uses high-level platform substate `$02` as a direct index into two 18-byte tables.

- table `$CACF[$02]` -> MMC1 CHR bank 0 via `$C078`;
- table `$CABD[$02]` -> shadow `$75` and MMC1 CHR bank 1 via `$C096`.

MMC1 is in 4 KiB CHR mode, so these are 4 KiB bank numbers, not the original iNES 8 KiB units.

## Static initialization table

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

This is the platform-entry/static resource selection. It groups substates `$00-$0B/$0F-$10`, `$0C-$0D`, `$0E`, and `$11` into distinct initial visual-resource sets.

## Dynamic CHR0 override from bank-1 `$9915`

The static `$CACF` table is not the final word for every frame. The state-`$20` NMI can call bank-1 `$9915`; its one-shot special refresh route selects a six-byte CHR0 table at `$9966` after `$03A4` is armed for substates `$0C-$11`.

| `$02` | static `$CACF` CHR0 | `$9915/$9966` CHR0 |
|---:|---:|---:|
| `$0C` | `$1D` | `$1D` |
| `$0D` | `$1D` | `$1D` |
| `$0E` | `$1B` | `$1B` |
| `$0F` | `$19` | `$00` |
| `$10` | `$19` | `$19` |
| `$11` | `$19` | `$00` |

The zero entries are canonical ROM bytes. They are not absent mappings: `$0F` and `$11` intentionally switch CHR0 to bank 0 when the one-shot refresh fires.

The `$9915` write is a raw MMC1 CHR0 serial transaction (`INC $FFFF` followed by five writes in the `$A000-$BFFF` register window). It is a transient presentation/resource mutation and does not modify the static initialization table or persistent PRG-bank mirror `$3B`.

`PLATFORM_VISUAL_REFRESH_9915.md` closes the arming gates, palette descriptor transfer and this dynamic CHR0 ownership.

## CHR0 versus CHR1

Platform initialization sets PPUCTRL shadow `$77=$90`.

On the NES this selects:

```text
sprite pattern table     = $0000 -> MMC1 CHR0
background pattern table = $1000 -> MMC1 CHR1
NMI enabled
```

Therefore the `$CACF` values and later `$9915/$9966` overrides are sprite-resource banks consumed by platform OAM/metasprite code, while `$CABD` supplies background/metatile graphics.

## Corrected ownership of the bank-3 metasprite tables

Earlier notes described the pointer tables around `$B669/$B671/$B699/$B6C1` as though their low entries were ordinary entity types. Direct caller tracing corrects that wording.

`$B987` is a **shared player/primary-entity compositor**:

- player call `$B94B+` puts internal Saint index `$03` in `$26`, so table indices `$00-$04` are Seiya/Shun/Hyoga/Shiryu/Ikki;
- primary-entity call `$A8DC-$A907` puts logical entity type `+$09` in `$26`, so table indices `$05-$0F` are the canonical primary entity-type domain.

This is why the same 16-entry pointer tables legitimately contain both namespaces.

## Complete shared pointer family

The compositor can select eleven 16-entry bank-3 pointer tables:

```text
$B671 $B699 $B6C1 $B6E9 $B711 $B739
$B761 $B789 $B7B1 $B7D9 $B801
```

The dynamic four-phase table at `$B669` maps:

```text
0 -> $B699
1 -> $B671
2 -> $B699
3 -> $B6C1
```

A direct hidden/flash definition also exists at `$B647`.

`PLATFORM_VISUAL_RESOURCE_DEFINITIONS.md` freezes the exact `$B987-$BACF` selector rules, primary type `$05-$0F` record inventory, dynamic action mutation and special direct resources.

## Metasprite format and verified primary capacity

A selected definition is:

```text
count
[count × (optional $FF attribute-OR, tile, signed Y, signed X)]
```

All 121 combinations of:

```text
11 pointer-table families × 11 primary types $05-$0F
```

are parsed by the ROM-fed auditor and satisfy bank/pointer/tile/record invariants.

Maximum reachable hardware-sprite counts are:

```text
05 10   06 10   07 10   08 11   09 9   0A 6
0B 7    0C 7    0D 12   0E 4    0F 4
```

Type `$0D` is the sole 12-record exception; this matches the independent `$A647` lifecycle evidence that explicitly retires one extra visual record at `+$2C/+2D` only for `$0D`.

## Separately selected direct resources

The mechanical visual inventory also includes:

- `$B647`: direct 11-record all-`$FE` hidden/flash definition;
- `$A908`: attached single-sprite resource selected by type through fixed `$C0E3/$C0EF` tile/vertical-offset tables;
- independent `$9B93` multisprite bootstrap resources at `$9B65/$9B6C/$9B73/$9B8F`, including dedicated substate-`$0D` tile `$8C`.

These do not need to be misrepresented as ordinary entries in the shared `$B987` pointer tables.

## Reproducible tools

`tools/reverse/audit_platform_visual_resources.py` reads a user-provided canonical ROM and audits the complete primary pointer domain plus direct special-resource metadata. It emits addresses/counts/selector metadata only; no original graphics are written into the repository.

`tools/reverse/audit_platform_visual_refresh.py` independently audits the bank-1 palette-pointer domains and `$9966` dynamic CHR0 selectors without emitting original palette bytes.

`tools/reverse/render_platform_entities.py` distinguishes:

```text
--domain player   shared indices $00-$04
--domain primary  shared indices $05-$0F
--domain all      shared indices $00-$0F
--include-special direct $B647 / $A908 / $9B93 resources
```

All pixel data are read from the supplied ROM at runtime. Generated PNG contact sheets remain private/reference artifacts and must not be committed.

## Remaining visual work outside this closure

1. optional screenshot/capture correlation for visual/narrative names;
2. platform HUD/name-table refresh downstream of `$9915` at `$9D69+`;
3. presentation paths for non-platform global NMI states, if required by final ORIGINAL SPEC audit;
4. preserve only semantic descriptions and clean-room tools in GitHub, never extracted original art or palette payloads.

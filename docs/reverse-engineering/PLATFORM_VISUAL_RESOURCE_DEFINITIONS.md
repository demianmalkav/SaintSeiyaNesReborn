# Platform visual-resource definitions — shared metasprite compositor and special objects

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: **CONFIRMED by direct canonical-ROM flow, complete ROM-fed pointer/record audit, and executable clean-room selector fixtures.** Visual/narrative identity names remain a separate confidence layer.

## Scope

This checkpoint closes the mechanical platform sprite-resource definition layer. It does not attempt to reproduce the NES PPU/NMI scheduler or assign character names from screenshots.

The important correction to the earlier working model is that bank-3 `$B987` is not an entity-only renderer. It is a **shared player/primary-entity metasprite compositor**.

## Shared ownership of `$B987`

### Player call

The player renderer at `$B94B+` prepares the compositor with:

```text
$28 = $4E      frame-start/latched player action
$27 = $4B      player busy/control state
$26 = $03      internal Saint index 0..4
$29 = $4F      sampled descriptor
$34 = $42      facing/flags
$35 = $3F      X
$36 = $40      Y
JSR $B987
```

Therefore table entries `$00-$04` are the five internal Saint slots:

```text
0 Seiya
1 Shun
2 Hyoga
3 Shiryu
4 Ikki
```

### Primary-entity call

The primary slot renderer at `$A8DC-$A907` feeds the same compositor from the active logical entity:

```text
$27 = logical +$04
$29 = logical +$05
$26 = logical +$09   numeric entity type
$34 = logical +$07
$35 = logical +$01   X
$36 = logical +$02   Y
JSR $B987
$28 -> logical +$00  action may be rewritten by the dynamic selector
```

The already-reconstructed encounter tables prove the canonical primary type domain `$05-$0F`. Thus one 16-entry pointer table can serve both namespaces without conflating their semantics:

```text
$00-$04 = player/Saint indices
$05-$0F = primary entity types
```

## Definition pointer families

The shared compositor can select these eleven 16-entry bank-3 pointer tables:

```text
$B671
$B699
$B6C1
$B6E9
$B711
$B739
$B761
$B789
$B7B1
$B7D9
$B801
```

The dynamic four-phase selector at `$B669` contains:

```text
phase 0 -> $B699
phase 1 -> $B671
phase 2 -> $B699
phase 3 -> $B6C1
```

A direct definition at `$B647` is selected for the hidden half of several flashing/reaction families instead of indexing a pointer table.

## Exact primary selector `$B987-$BACF`

Inputs below use logical-field names from the primary call:

```text
type    = +$09 = $26
action  = +$00 = $28
control = +$04 = $27
ground  = +$05 = $29
frame   = $3C
global animation gate = $03B9
```

### Families `$80/$40/$E0`

```text
(frame & $04) == 0 -> direct $B647
(frame & $04) != 0 -> table $B7D9
```

### Family `$70`

Type `$0C`:

```text
(frame & $10) == 0 -> $B711
(frame & $10) != 0 -> $B761
```

Types `$08/$09`:

```text
action < $78  -> $B761
action >= $78 -> $B711
```

Other primary types:

```text
action < $78  -> $B6E9
action >= $78 -> $B711
```

### Family `$A0`

```text
$A0-$A3 -> $B6E9
$A4-$A7 -> frame bit $10 clear $B711, set $B761
$A8-$AB -> $B739
$AC-$AF -> $B761
```

### Family `$D0`

For `$D0-$D3`:

```text
frame bit $04 clear -> direct $B647
frame bit $04 set   -> $B7D9
```

For `$D4-$DF`:

```text
frame bit $04 clear -> direct $B647
frame bit $04 set and ($29 < $80 or $29 >= $F0) -> $B7D9
frame bit $04 set and $80 <= $29 < $F0           -> $B801
```

### Nonzero logical `+$04`

Once the early `$40/$70/$80/$A0/$D0/$E0` cases have been excluded, nonzero control routes by family:

```text
$20 -> $B761
$30 -> $B7B1
other -> $B711
```

### Zero logical `+$04`

Family `$00`:

```text
type $0C -> frame bit $10 toggles $B6E9/$B739
type $0F -> frame bit $02 toggles $B6E9/$B739
type $0E -> dynamic $B669 phase selector
other    -> $B6E9
```

Other supported families:

```text
$20 -> $B739
$30/$50 -> $B789
$10 -> dynamic $B669 phase selector
```

Other family/control-zero combinations reach the original `$BA58` self-loop. The clean-room selector reports these combinations as unsupported instead of fabricating a resource.

## Dynamic entity selector mutates action

For primary types `$05-$0F`, `$BA62+` uses `$03B9` as a gate, not as the phase number:

```text
if $03B9 == 0:
    action = (action + 1) & $13
    write action back to $28
else:
    action unchanged

phase = action & 3
phase -> $B699/$B671/$B699/$B6C1
```

Because `$A901-$A905` copies final `$28` back to logical `+$00`, the renderer itself can advance the entity action byte on this path. `PlatformMetaspriteResourceSelector` exposes both the selected table and that write.

## Metasprite record format

`$BAD3+` consumes a selected definition as:

```text
byte 0 = hardware-sprite count
repeat count times:
    optional $FF, attribute-OR byte
    tile byte
    signed Y offset
    signed X offset
```

The attribute override is ORed with base facing/attribute state. Each emitted record becomes one NES OAM `(Y,tile,attributes,X)` entry.

The routine seeds `$2B=$0B`, giving the ordinary primary visual block a nominal eleven-record cleanup budget.

## Complete `$05-$0F` ROM audit

`audit_platform_visual_resources.py` parses all:

```text
11 pointer-table families × 11 primary types = 121 pointer selections
```

Every pointer resolves inside switch bank 3, every encoded definition terminates inside that bank, and every tile byte resolves inside one 4 KiB CHR bank.

Maximum reachable hardware-sprite count by type:

| type | max sprites |
|---:|---:|
| `$05` | 10 |
| `$06` | 10 |
| `$07` | 10 |
| `$08` | 11 |
| `$09` | 9 |
| `$0A` | 6 |
| `$0B` | 7 |
| `$0C` | 7 |
| `$0D` | **12** |
| `$0E` | 4 |
| `$0F` | 4 |

### Type `$0D` is the proven twelve-record exception

The maximum occurs at table `$B761`, whose type-`$0D` entry resolves to definition `$B324` with 12 hardware sprites.

This independently matches the already-closed removal helper `$A647`:

```text
ordinary primary removal -> retire 11 records +$00..+$28
type $0D                 -> additionally retire +$2C/+2D
```

The resource and lifecycle evidence therefore agree exactly. The clean-room capacity invariant is 11 records for `$05-$0C/$0E-$0F`, 12 for `$0D`.

## Direct `$B647` definition

`$B647` contains 11 records using tile `$FE` with zero offsets. It is the direct hidden/blank definition selected by the flash families above and does not pass through a 16-entry pointer table.

## CHR0 binding

Platform setup already proves that sprites use PPU pattern table `$0000`, backed by MMC1 CHR0. Fixed `$CACF[$02]` selects:

```text
$00-$0B -> CHR0 25
$0C-$0D -> CHR0 29
$0E     -> CHR0 27
$0F-$10 -> CHR0 25
$11     -> CHR0 25
```

Thus all shared definitions are mechanically renderable against the exact CHR0 bank selected by the current platform substate. The same definition pointer can deliberately produce different visual pixels when a special substate selects a different CHR0 bank.

## Separately selected attached visual `$A908`

`$A908` does not use the shared metasprite pointer tables. It indexes two fixed 11-byte tables with `(type - $05)`:

```text
$C0E3 -> direct sprite/tile
$C0EF -> signed vertical offset added to logical Y
```

Exact entries:

| type | tile | Y offset | creates visual |
|---:|---:|---:|---|
| `$05` | `$B2` | `+5` | yes |
| `$06` | `$B0` | `+2` | yes |
| `$07` | `$00` | `0` | no |
| `$08` | `$D2` | `+4` | yes |
| `$09` | `$A1` | `-2` | yes |
| `$0A` | `$00` | `0` | no |
| `$0B` | `$00` | `0` | no |
| `$0C` | `$8F` | `+5` | yes |
| `$0D` | `$00` | `0` | no |
| `$0E` | `$00` | `0` | no |
| `$0F` | `$00` | `0` | no |

When nonzero, the visual record is at primary visual `+$2C`; X is derived from logical X by `+9/-9` according to facing, and attributes include the facing bit plus `$03`.

This corrects an earlier provisional interpretation of `$C0EF`: it is a **vertical/Y offset table**, not an X-offset table.

## Independent `$9B93` multisprite visuals

The already-closed `$9B93` object class is independent of the primary slots and uses its own direct resource tables in **switchable PRG bank 3**:

```text
$9B65 -> selector sprite bases
$9B6C -> global $03A9 values
$9B73 -> selector logical profiles
$9B8F -> dedicated substate-$0D profile
```

Ordinary nonzero selectors build a 2×2 visual block from their sprite base. Substate `$0C` skips two sprite values before the second row; substate `$10` writes only part0. Dedicated substate `$0D` bypasses the ordinary tables and creates one direct tile `$8C` with its established side/facing placement.

`render_platform_entities.py --include-special` can reconstruct these direct resources from the supplied ROM without storing extracted art in GitHub.

## Clean-room artifacts

- `PlatformMetaspriteResourceSelector` reproduces `$B987-$BACF` resource selection for primary types `$05-$0F`, including dynamic action mutation and the type-`$0D` capacity exception.
- `PlatformMetaspriteResourceSelectorChecks` freezes discriminating branch fixtures.
- `tools/reverse/audit_platform_visual_resources.py` audits all 121 primary pointer selections plus `$B647`, `$A908` and `$9B93` metadata from a user-owned canonical ROM.
- `tools/reverse/render_platform_entities.py` now distinguishes player versus primary domains, renders every shared primary pointer family, and optionally reconstructs direct special resources.

No CHR bytes, PNGs or other original art are committed.

## Boundary after this checkpoint

Mechanically closed here:

```text
platform substate -> CHR0
shared player/entity selector -> definition pointer
primary type $05-$0F -> all shared pointer families
direct blank $B647
attached $A908 direct visual
independent $9B93 direct bootstrap visual resources
metasprite record parsing / OAM-count capacity
```

Still separate future work:

- screenshot-based visual/narrative naming;
- palette semantics beyond the already-proven selector plumbing;
- complete NMI/PPU/OAM transfer scheduling;
- complete late-object main-thread integration;
- audio and RNG global closure.

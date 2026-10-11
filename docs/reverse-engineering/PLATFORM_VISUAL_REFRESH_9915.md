# Platform bank-1 visual refresh — `$9915/$9EEF`

Status: **confirmed against the canonical Japanese ROM and modeled as clean-room selector/transfer semantics**.

This checkpoint composes with the already-closed platform state-`$20` NMI. Fixed-bank `$D7F2` temporarily maps PRG bank 1 and calls `$9915`; this document closes palette/CHR work reachable from that entrypoint without absorbing main-thread entity simulation or the later HUD writer at `$9D69`.

## Entry split — `$9915`

The first two gates are:

```text
$9915  LDA $07C0
$9918  CMP #$FE
$991A  BEQ $996C

$991C  LDA $03A4
$991F  CMP #$FF
$9921  BNE $996C
```

A failed gate does **not** return. It transfers to the general visual-profile path at `$996C`.

`$07C0` is the Y byte of the first of four OAM records built by the special-platform visual owner at `$9130-$91D4`. `$FE` is the hidden/inactive sentinel. The gate therefore prevents the one-shot palette/CHR switch until that visual exists in OAM shadow.

`$03A4` is a one-shot refresh latch:

- platform reset `$98F1` writes `0`;
- its only canonical setter is `$9167-$9169`, which writes `$FF`;
- successful `$9915` special refresh executes `DEC $03A4`, producing `$FE`;
- `$9162+` only arms when the latch is zero, so `$FE` cannot re-arm until the next platform reset.

### Canonical arming domain

The setter starts at `$9130` and only accepts substates `$0C-$11`.

All six use camera-page threshold `$45==$0A`. The `$44` low-scroll thresholds from `$9207` are:

```text
$0C -> $A0
$0D -> $E0
$0E -> $D0
$0F -> $D0
$10 -> $D0
$11 -> $A5
```

There is one explicit exclusion:

```text
$02 == $10
$03 == 1 (internal Shun)
-> do not arm $03A4
```

This proves why `$9966[$02-$0C]` is only reached with a six-entry domain rather than being a general `$00-$11` CHR table.

## One-shot special palette/CHR route — `$9923-$995F`

When `$07C0 != $FE` and `$03A4 == $FF`, `$9915` overwrites the fourth palette pointer `$0398/$0399`:

```text
$02 != $10 -> $0398/$0399 = $9960
$02 == $10 -> $0398/$0399 = $9963
```

Both sources are three-byte palette descriptors. The routine then consumes the latch `$FF->$FE`, calls `$9EEF`, and afterwards performs a raw MMC1 CHR0 serial write.

### Dynamic CHR0 table — `$9966`

| `$02` | static CHR0 from `$CACF` | `$9915` dynamic CHR0 |
|---:|---:|---:|
| `$0C` | `$1D` | `$1D` |
| `$0D` | `$1D` | `$1D` |
| `$0E` | `$1B` | `$1B` |
| `$0F` | `$19` | `$00` |
| `$10` | `$19` | `$19` |
| `$11` | `$19` | `$00` |

The zero entries are canonical bytes, not missing data. `$0F` and `$11` therefore have intentional **transient CHR0 overrides to bank 0** when this one-shot refresh fires.

The write sequence is `INC $FFFF` followed by five writes to `$BFFF`. This is a raw CHR0 mapper transaction; it does not change persistent PRG-bank owner `$3B` or the fixed `$CACF` initialization table.

## Sprite palette transfer — `$9EEF/$9F29`

`$9EEF` temporarily writes `$2000=0/$2001=0`, then sets PPU address `$3F10`.

It consumes four pointer pairs:

```text
$0392/$0393
$0394/$0395
$0396/$0397
$0398/$0399
```

Each `$9F29` call writes exactly `$0F` followed by three source bytes. Four calls therefore fill exactly 16 bytes at `$3F10-$3F1F`.

Fixed initialization and the fallback managers establish ownership:

- `$0392/$0393`: current player/Saint palette descriptor;
- `$0394/$0395`: primary visual-profile palette;
- `$0396/$0397`: secondary/auxiliary profile palette;
- `$0398/$0399`: secondary primary-profile descriptor, or the special `$9960/$9963` override.

The same outer table `$9F46` is dual-use. Indices `$00-$04` resolve directly to player descriptors; primary type nibble `$05-$0F` resolves to a four-variant pointer list.

For primary profile `$03B7`:

```text
type    = $03B7 & $0F
variant = ($03B7 & $30) >> 4
outer pointer entry = $9F46 + type*2
inner pointer offset = variant*2
secondary entry = $9F66 + type*2
```

The ROM-fed auditor resolves these pointers but deliberately does not emit palette bytes.

## General fallback — `$996C+`

If the special top gate is not taken, `$9915` enters the general visual-profile manager.

### Primary profile change

`$996C+` obtains current-page profile `$03B7` from the substate-specific pointer table at `$9AE5`, indexed by camera page `$45`.

If it differs from remembered `$58`, palette replacement waits until `$0749==$FE`, `$077D==$FE`, and high nibbles of `$03BA/$03CA` are neither `$40` nor `$D0`.

A nonzero accepted profile writes `$58=$03B7`, resolves `$0394/$0395` through `$9F46`, resolves `$0398/$0399` through `$9F66`, calls `$9EEF`, and updates related visual-profile parameter bytes `$03AB/$03AC-$03AF`.

### Secondary profile change

`$9BB9+` derives `$03B4` from current page and keeps bits `0-2`. A changed nonzero profile waits for `$07B1==$FE` and `$07B9==$FE`, then writes `$03B5`, selects `$0396/$0397` through `$9C7F`, calls `$9EEF`, and updates `$03B0-$03B3`. Canonical nonzero palette-selector domain is `$01-$04`.

### Pending `$03A9` refresh

If no secondary change is ready, `$9CA6+` checks `$03A9`. For values `$01-$05` it selects `$0396/$0397` through `$9CC6 + ($03A9-1)*2`, calls `$9EEF`, clears `$03A9=0`, and returns.

If `$03A9==0`, control continues to the special `$0D` background-palette owner or to `$9D69`.

## Substate `$0D` background palette animation — `$9CE2-$9D58`

This path is reachable only for `$02==$0D` and camera pages `$45=$02-$04`. Frame bit `$3C&$08` and latch `$03A7` suppress redundant writes.

Low half (`bit3=0`) with `$03A7=0` sets `$03A7=$FF` and uses source `$A02B`.

High half (`bit3=1`) with `$03A7!=0` clears `$03A7=0` and uses source `$A022`.

Each source is nine bytes. The transfer writes three background subpalettes as `$0F + 3 source bytes`, then appends fixed fourth subpalette `$0F $10 $11 $16`, for exactly 16 bytes at `$3F00-$3F0F`.

If no palette edge is due, control delegates to `$9D69`, the HUD/name-table refresh owner. `$9D69` is intentionally outside this palette/CHR checkpoint.

## `$9D58` tail

Both palette families use `$9D58` after a transfer. Exact `$2006` write sequence is:

```text
$3F $00 $00 $00
```

The first pair addresses `$3F00`; the second leaves the PPU address at `$0000`. The enclosing NMI later restores `$77/$78` and scroll through the already-closed `$D367+` epilogue.

## Clean-room implementation

`PlatformVisualRefresh9915` models the `$03A4` arming owner and Shun exception, top route split, one-shot descriptor/CHR override, `$3F10` transfer format, `$9D58` reset, general selector formulas and the `$0D` background-palette alternation.

`tools/reverse/audit_platform_visual_refresh.py` verifies pointer domains from a user-supplied canonical ROM while outputting only addresses/counts/CHR selectors. It never emits original palette bytes.

## Scope boundary

Closed here:

```text
state20 NMI chooses $9915
 -> special one-shot palette/CHR route OR general palette-profile route
 -> sprite/background palette transfer
 -> dynamic CHR0 when applicable
 -> $9D58 address normalization
 -> return to enclosing NMI
```

Explicitly outside this checkpoint: `$9D69+` HUD/name-table writing, main-thread entity simulation, full audio, RNG, non-platform NMI states, and original palette payload bytes.

# `$9B93` multisprite class — bootstrap

Status: **CONFIRMED by static ROM flow** for `$9B93-$9CAB`. The active updater beginning at `$9CAC` remains a separate target.

## Identity and storage

This object class is independent from both previously reconstructed systems:

- common entity A/B logical records `$03BA/$03CA` with presentation `$0748/$077C`;
- auxiliary hazard slots `$07B0/$07B8` with metadata `$03DA/$03EA`.

`$9B93` instead initializes:

```text
visual pointer $12/$13 = $07E0
logical pointer $16/$17 = $03FB
```

The visual block is four contiguous four-byte records:

```text
+0 Y
+1 sprite/tile selector
+2 flags
+3 X
```

so the four parts occupy `$07E0-$07EF`.

## Empty/active gate

The routine inspects visual `+1,+5,+9,+13`. If any sprite byte is not `$FE`, bootstrap is skipped and control transfers to the active updater at `$9CAC`.

Initialization therefore occurs only while all four parts are empty.

## Selector source

There are two selector paths.

### Immediate main path

When:

```text
$02 < $0C
$74 == 0
```

the stage-derived selector is ignored.

The selector becomes:

```text
($02 & $FE) == $08 ? 4 : 3
```

Thus substates `$08/$09` force selector 4; the other substates in this immediate range force selector 3.

This path later sets `$81=3` and does not wait for `$03FA`.

### Table-driven/timed path

Otherwise the routine uses the selector obtained from the substate/page pointer table rooted at `$9A84`.

A zero selector does not create the class.

For a nonzero selector, `$81=0`. If `$03FA != 0`, the routine decrements `$03FA` once and returns without populating the visual block. If `$03FA==0`, initialization proceeds and resets `$03FA=$80`.

## Selector tables

`$9B65` supplies the base sprite value, `$9B6C` supplies global `$03A9`, and `$9B73` contains four profile bytes copied to logical offsets `+$0C..+$0F`.

| selector | sprite base | `$03A9` | `+$0C` | `+$0D` | `+$0E` | `+$0F` |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 0 | `$00` | `$00` | `$00` | `$00` | `$00` | `$00` |
| 1 | `$B4` | `$01` | `$14` | `$00` | `$02` | `$02` |
| 2 | `$B4` | `$04` | `$28` | `$00` | `$06` | `$04` |
| 3 | `$B4` | `$01` | `$14` | `$00` | `$02` | `$02` |
| 4 | `$B4` | `$04` | `$28` | `$00` | `$06` | `$04` |
| 5 | `$F6` | `$01` | `$1E` | `$0A` | `$03` | `$01` |
| 6 | `$DA` | `$03` | `$28` | `$0A` | `$05` | `$04` |

The table ends before `$9B8F`; bytes from `$9B8F` belong to a later special substate path and are not selector 7.

## Flags and entry X

Base flags are `$02`.

Selectors 3 and 4 keep `$02`; all other nonzero selectors OR in `$04`, producing `$06`.

X origin depends on `$81`:

```text
$81 != 0 : player X `$3F`
$81 == 0 : fixed edge X `$F7`
```

Within a row, the second part uses `X+8`.

## Normal 2x2 visual construction

For ordinary substates, bootstrap creates:

```text
part0: Y=$F8, sprite=base+0, X=xBase
part1: Y=$F8, sprite=base+1, X=xBase+8
part2: Y=$00, sprite=base+2, X=xBase
part3: Y=$00, sprite=base+3, X=xBase+8
```

This corresponds exactly to the two nested two-part loops at `$9C53-$9CA4`.

Substate `$0C` adds two extra sprite increments between rows, so the second row becomes `base+4/base+5`.

At the end of the normal path the routine clears logical offset `+$00` to zero. Logical offset `+$03` has already been reset to zero before visual construction.

## Substate `$10` early return

`$02==$10` branches to `$9CAB` immediately after writing the first four-byte visual record.

Therefore this path:

- writes only part0;
- has already reset logical `+$03`;
- has already copied the profile and reset `$03FA=$80`;
- returns **before** the later logical `+$00=0` epilogue.

The clean model records that distinction explicitly rather than assuming the remaining three visual parts or action clear occurred.

## Clean-room boundary

`PlatformMultisprite9B93Bootstrap` models only selector resolution, cooldown gating, profile copying and visual bootstrap.

The `$9CAC+` active updater contains multiple branches driven by visual flag bits, logical `$D0/$E0` families, engine substates `$0C/$0D/$10`, projectile/contact calls and four-part motion. Those paths must be promoted separately before this class is inserted into the composed late platform frame.

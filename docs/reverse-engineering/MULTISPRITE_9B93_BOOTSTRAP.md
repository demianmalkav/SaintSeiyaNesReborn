# `$9B93` multisprite class — bootstrap

Status: **CONFIRMED by static ROM flow** for `$9B93-$9CAB` plus the dedicated substate-`$0D` initializer at `$A0E4-$A129`.

## Identity and storage

This object class is independent from both common entity A/B (`$03BA/$03CA` + `$0748/$077C`) and auxiliary hazard slots (`$03DA/$03EA` + `$07B0/$07B8`).

`$9B93` initializes:

```text
visual pointer $12/$13 = $07E0
logical pointer $16/$17 = $03FB
```

The visual block is four contiguous four-byte records `(Y, sprite, flags, X)` at `$07E0-$07EF`.

## Empty/active gate

The routine inspects visual sprite bytes `+1,+5,+9,+13`.

- if any is not `$FE`, control transfers to the active updater at `$9CAC`;
- only an all-empty block continues through bootstrap.

This distinction matters for composition: a newly initialized object returns from bootstrap and does **not** execute `$9CAC+` again in the same call/frame.

## Selector source

### Immediate main path

When `$02 < $0C` and `$74 == 0`, the stage selector is ignored:

```text
($02 & $FE) == $08 ? selector 4 : selector 3
```

This path later sets `$81=3` and bypasses the `$03FA` wait.

### Table-driven/timed path

Otherwise the stage-derived selector is used.

- selector zero jumps to `$9CAC`; because the visual block is empty, that updater immediately returns and does not write `$81` or `$03A9`;
- nonzero selector writes `$81=0` before the cooldown gate;
- if `$03FA != 0`, `$03FA` decrements and the routine jumps to `$9CAC` before phase/profile/`$03A9` initialization;
- when the wait expires, `$03FA` becomes `$80` and bootstrap continues.

## Ordinary selector tables

For non-`$0D` initialization, `$9B65` supplies sprite base, `$9B6C` supplies global `$03A9`, and `$9B73` supplies logical offsets `+$0C..+$0F`.

| selector | sprite base | `$03A9` | `+$0C` | `+$0D` | `+$0E` | `+$0F` |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 0 | `$00` | `$00` | `$00` | `$00` | `$00` | `$00` |
| 1 | `$B4` | `$01` | `$14` | `$00` | `$02` | `$02` |
| 2 | `$B4` | `$04` | `$28` | `$00` | `$06` | `$04` |
| 3 | `$B4` | `$01` | `$14` | `$00` | `$02` | `$02` |
| 4 | `$B4` | `$04` | `$28` | `$00` | `$06` | `$04` |
| 5 | `$F6` | `$01` | `$1E` | `$0A` | `$03` | `$01` |
| 6 | `$DA` | `$03` | `$28` | `$0A` | `$05` | `$04` |

The table ends before `$9B8F`.

## Ordinary visual construction

Base flags are `$02`; selectors other than 3/4 OR in `$04`, producing `$06`.

X origin is:

```text
$81 != 0 : player X `$3F`
$81 == 0 : fixed edge X `$F7`
```

Normal initialization builds a 2×2 block. Substate `$0C` skips two sprite values before row 2. Substate `$10` writes only part0 and returns before the ordinary `action +$00 = 0` epilogue.

## Dedicated substate `$0D` initializer

This is the important exception.

After a nonzero selector passes the timed cooldown gate, `$9C03` clears logical phase `+$03`, then:

```text
$9C0A  LDA $02
$9C0C  CMP #$0D
$9C0E  BNE ordinary-profile path
$9C10  JMP $A0E4
```

`$A0E4` does **not** use the selector's ordinary sprite/profile tables and does **not** write `$03A9`.

It creates only visual part0:

```text
Y      = $20
sprite = $8C

if ($48 & $08) == 0:
    flags = $02
    X     = $EF
else:
    flags = $42
    X     = $11
```

Parts1-3 remain empty.

It clears logical action `+$00` and copies the four dedicated raw bytes at `$9B8F-$9B92`:

```text
+$0C = $1E
+$0D = $05
+$0E = $05
+$0F = $01
```

This corrects the earlier clean-room assumption that substate `$0D` used selector-5's ordinary `$F6` 2×2 initialization.

## Clean-room representation

`PlatformMultisprite9B93Bootstrap` now exposes whether `$03A9` was actually written. This is required by a persistent wrapper because several bootstrap outcomes return before that global write:

- existing-active transfer;
- selector zero;
- cooldown decrement;
- dedicated substate `$0D` initialization.

Only ordinary completed selector initialization writes `$03A9`.

No ROM payload is embedded in the model.

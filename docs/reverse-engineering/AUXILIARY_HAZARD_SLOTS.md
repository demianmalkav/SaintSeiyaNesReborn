# Auxiliary hazard slots `$07B0/$07B8`

Status: **CONFIRMED by static ROM flow** for the spawn/motion/animation/removal behavior promoted in `PlatformAuxiliaryHazards.cs`.

## Important subsystem separation

Two mechanisms previously risked being grouped under the loose label “secondary object”:

1. `$A908` writes an embedded child/sprite record inside the current common entity's `$0748/$077C` presentation block (offsets `+$2C..+$32`). It is reached from the `$70` family and remains tied to that parent entity's record.
2. `$96B4/$9761` operate two independent eight-byte auxiliary slots at **`$07B0` and `$07B8`**, with logical/combat metadata at **`$03DA` and `$03EA`**.

They are not the same storage or updater. This document covers only the second mechanism.

## Frame position

The bank-3 platform pipeline reaches these routines before the two common entity records:

```text
...
$96B4   stage-driven auxiliary spawn attempt
$9761   update $07B0 then $07B8
$A442   common entity A/B pipeline
$A22C   player attack-object late update
...
```

`$9761` selects the records through:

```text
slot A: $12 = $07B0, $16 = $03DA
slot B: $12 = $07B8, $16 = $03EA
```

## Stage selector and profiles

Bank 1 `$9BB9+` derives `$03B4` from the current stage/page data (`& 7`). When the value changes and both auxiliary slots are free, it becomes `$03B5` and selects four profile bytes from `$9C92`.

Fixed `$C169` supplies the initial sprite/behavior byte.

| kind `$03B5` | initial sprite | `+$0C` | `+$0D` Cosmo ticks | `+$0E` Life ticks | `+$0F` reward |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 0 | `$FE` | 0 | 0 | 0 | 0 |
| 1 | `$80` | `$1E` | 2 | 2 | `$32` |
| 2 | `$83` | `$1E` | 4 | 1 | `$32` |
| 3 | `$F4` | 0 | 5 | 2 | 0 |
| 4 | `$DA` | 0 | 4 | 3 | 0 |

`+$0C` is the same logical-record byte used as HP by the ordinary entity damage path, although kinds 1–4 are routed specially by `$9915` and should not be assumed to consume it identically in every hit branch.

## `$96B4/$96CE` spawning

A spawn pass runs only when:

- `$03B4 != 0`, and
- `$03B4 == $03B5`.

Slot A is attempted first. Kind 3 returns after slot A; kinds 1, 2 and 4 then attempt slot B.

An occupied slot (`slot[1] != $FE`) returns before touching the shared cooldown `$03B6`.

For an empty slot:

- nonzero `$03B6` is decremented once and the attempt ends;
- zero `$03B6` is reseeded from `$48`.

### Carry quirk in the cooldown seed

`$96D0 CMP #$FE` proves the slot is empty and leaves carry set. The later sequence is:

```text
LDA $48
AND #$1F
ADC #$1F
STA $03B6
```

There is no `CLC`, so the seed is:

```text
($48 & $1F) + $20
```

or **32–63**, not 31–62.

For non-kind-3 profiles, if slot B is empty, its immediately following attempt can decrement that freshly seeded value in the same frame. Thus a successful slot-A spawn commonly leaves 31–62 after the complete `$96B4` pass.

### Spawn side and partial writes

Kind 3 always uses the right-side branch. Other kinds use `$48 & $08`:

- left: `X=$02`, flags `$42`, `$03A5=+1`;
- right: `X=$FF`, flags `$02`, `$03A5=-1`.

`$03A6` is reset to zero.

These X/facing writes occur **before** Y validation. Spawn Y is:

```text
baseY = ($40 != 0 && $40 < $87) ? $40 : $50
Y = byte(baseY - ($48 & $3F))
```

If the wrapped result is `>= $A0`, the spawn is rejected while the slot still retains those earlier X/facing writes and the new cooldown.

## `$976A` updater: ordinary auxiliary families

For sprite types other than `$F4/$F5`, animation advances when `($3C & 3)==0`.

Confirmed cycles:

```text
$80 -> $81 -> $82 -> $80
$83 -> $84 -> $85 -> $86 -> $83
$DA -> $DE -> $DF -> $DA
```

Horizontal motion then applies:

```text
flags bit $40 clear: X -= 2
flags bit $40 set:   X += 2
X -= $43
```

The object is removed when `(X & $FE) < 2`. Removal writes:

```text
slot[0] = $F0
slot[1] = $FE
slot[4] = $FE
slot[5] = $FE
```

If still active, `$989C` copies its position into logical metadata and installs the reduced auxiliary hitbox parameters `$79=$04, $7A=$04, $7B=$03, $7C=$03`.

The ordinary path then requests both:

- `$98BA` entity/hazard -> player contact (unless player frame-start family `$80`), and
- `$9915` player projectile -> auxiliary object processing.

## `$F4/$F5` homing family

Kind 3 alternates `$F4/$F5` every four frames, while mirror sprite byte `slot[5]` receives `newType-2` (`$F3/$F2`).

The shared velocities are `$03A5/$03A6`.

Every frame, exact coordinate equality first zeroes the corresponding velocity. Every 32 frames (`$3C & $1F == 0`) both axes are refreshed to `-1/0/+1` toward the player.

Actual X/Y motion occurs only on even `$3C`:

```text
Y += $03A6
mirrorY = Y - 8
X += $03A5
mirrorX = X
```

If `mirrorY >= $A0`, the slot is removed before the X step. Camera subtraction then applies every frame:

```text
X -= $43
mirrorX = X
```

The same `(X & $FE) < 2` removal gate follows.

Unlike ordinary auxiliary families, `$F4/$F5` calls the player-contact path but **does not** continue into `$9915`; player projectiles are therefore not tested against this family through `$976A`.

## Clean-room implementation boundary

`PlatformAuxiliaryHazardSpawner` now models `$96B4/$96CE`.

`PlatformAuxiliaryHazardUpdater` models the motion/animation/removal portion of `$976A` and exposes whether the original requests `$98BA` and/or `$9915` afterward.

Actual composition of those collision calls into the full two-slot frame is deliberately left for the next step so that the physical `$76/$7F/$80`, attack objects, Seventh Sense and auxiliary metadata can be threaded without duplicating the already-promoted collision semantics.

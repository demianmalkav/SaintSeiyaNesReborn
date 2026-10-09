# `$9B93` multisprite class — special substate `$0D`

Status: **CONFIRMED by static ROM flow** for `$A12A-$A22B`.

Substate `$0D` bypasses the normal `$9D05` / flag-`$08` dispatch and enters a dedicated updater. It uses the first three visual records as an articulated horizontal formation while leaving the fourth visual record untouched during ordinary updates and during the `$D0-$DF` death animation.

## Entry dispatch

At `$A12A`, logical action is checked only for family `$D0`:

```text
(action & $F0) == $D0 -> dedicated death path $A20D
otherwise             -> active formation path
```

Notably, this test is not a generic `$D0/$E0` check like the non-`$0D` dispatcher.

## Active vertical gate

Part0 Y is preserved in the 6502 X register. The routine computes:

```text
byte(part0Y - $10)
```

If that unsigned value is **below** player Y, part0 Y advances by `($3C & 1)` pixels and the result becomes the formation Y. Otherwise Y is left unchanged.

Parts1 and 2 later receive the same formation Y.

## Facing decisions

Part0 flags can be replaced with `$42` or `$02` using literal unsigned byte comparisons:

```text
if X < $0C:
    flags = $42
else:
    t = byte(X + $20)
    if t < playerX:
        flags = $42
    else if t >= $E0:
        flags = $02
    else:
        preserve current flags
```

The wrap of `X+$20` is intentional.

## Three-part horizontal formation

After facing is settled:

- flag bit `$40` clear: part0 `X -= 2`, then `X -= $43`; part1 is `X+8`, part2 `X+16`;
- flag bit `$40` set: part0 `X += 2`, then `X -= $43`; part1 is `X-8`, part2 `X-16`.

Parts1/2 receive part0's formation Y and flags. Part3 is not modified.

If post-movement part0 X is `<4`, control jumps to the whole-class cleanup used elsewhere (`$A01F`) before collision handling.

## Three-sprite animation

Frame bit `$08` selects one of two three-sprite sequences:

```text
($3C & 8) == 0 : $8C, $8D, $8E
($3C & 8) != 0 : $D1, $D2, $D3
```

Only parts0-2 receive these sprite values. Part3 remains untouched.

## Wide collision geometry and order

Helper `$A1E5` synchronizes part0 Y/X into the logical record and prepares:

```text
$79 = $04
$7A = $0C
$7B = $03
$7C = $0A
```

The active order is:

```text
formation / animation
 -> contact $98BA
 -> synchronize part0 again
 -> projectile hit $9915
```

Thus contact occurs **before** projectile collision. If a later projectile hit mutates logical Y (for example a type `<5` `Y+6/$E0` response), there is no subsequent position re-sync in this frame to erase it.

The same `(4,12,3,10)` geometry is used by contact and projectile collision.

## Dedicated `$D0-$DF` death path

When logical action begins the frame in family `$D0`, `$A20D` runs instead of the active formation path:

```text
action += 1
if action >= $E0:
    clear whole class / phase
else:
    sprite(part0) = $6A
    sprite(part1) = $6A
    sprite(part2) = $6A
```

There is no contact, projectile hit, X/Y movement or camera subtraction in this death route.

Part3 is still untouched during `$D0-$DF`. Only the terminal `$DF -> $E0` cleanup clears all four visual records.

## Clean-room representation

`PlatformMultisprite9B93Substate0D` models both the active three-part formation and its dedicated `$D0` death lifecycle. It intentionally preserves the inactive fourth visual record instead of normalizing this special substate into the four-part behavior used elsewhere.

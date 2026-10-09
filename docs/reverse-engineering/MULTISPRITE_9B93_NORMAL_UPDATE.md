# `$9B93` multisprite class — normal active path

Status: **CONFIRMED by static ROM flow** for the flag-`$08`-clear, non-`$D0/$E0`, non-substate-`$0D` path through `$9CAC-$9DCC` and animation `$9DCD-$9E2C`.

## Entry boundary

This path applies only when:

- at least one `$07E0-$07EF` visual part is active;
- `$02 != $0D`;
- part0 flags bit `$08` is clear;
- logical `$03FB+0` is not in family `$D0` or `$E0`.

The alternate flag-`$08` path at `$9ED1`, logical `$D0/$E0` path at `$A06E`, and special `$0D` path at `$A12A` remain separate targets.

## Confirmed order

The normal path is:

```text
sync part0 position -> logical +1/+2
contact ($98BA)
logical phase +1
move visual parts
optional projectile hit ($9915)
optional flag-$08 transition
animation table update
```

The order is significant: contact sees the pre-movement position, while a projectile hit—when enabled—sees the post-movement position.

## Collision geometry

`$9CD5/$9CFC` installs:

- ordinary substates: `(79,7A,7B,7C) = (8,8,5,5)`;
- substate `$10`: `(4,4,2,2)`.

The same geometry persists into the later `$9915` call.

## Movement

Logical `+$03` increments once. The vertical delta is `phase >> 3`.

Each processed visual part applies:

```text
Y += phase >> 3
if $81 == 0: X -= 2
X -= $43
```

Entering Y band `$B0-$BF` clears the whole visual class to `Y=$F0, sprite=$FE` and clears logical phase.

Substate `$10` processes only part0; other supported substates process all four parts.

## Projectile gate

A later `$9915` check is requested when:

- `$02 == $0C`, or
- `$02 == $10`, or
- part0 flags bit `$04` is clear.

Otherwise the normal path skips projectile collision for this frame.

Projectile mutations are threaded back into logical action/X/Y/type/profile/reward fields before the later transition gate.

## Flag `$08` transition

The path can set visual flag `$08` only when `$81 != 0` and all of these hold after visual movement:

```text
part0 Y >= player Y
$30 <= part0 Y < $F0
logical +$05 >= $80
```

Transition effects:

```text
part0 flags |= $08
logical phase = 0
logical +$0D >>= 1
logical +$0E >>= 1
sound = $2B if flag $04 set, else $2C
```

The next frame therefore enters the separate flag-`$08` updater.

## Animation tables

`$3C & $18` selects one of four eight-byte groups.

- normal path: table `$9E61`;
- substate `$0C`: table `$9E41`;
- substate `$10`: table `$9E81` and only part0 is updated.

Each active part receives a sprite byte plus high flag bits from the table; existing low six flag bits are preserved.

## Clean-room implementation

`PlatformMultisprite9B93NormalUpdate` composes contact, movement, optional projectile hit, transition and animation for this closed route. Unsupported active routes throw rather than being approximated by analogy.

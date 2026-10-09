# `$9B93` multisprite class — flag `$08` + flag `$04`

Status: **CONFIRMED by static ROM flow** for `$9ED1-$9F74` when part0 flags contain both `$08` and `$04` and logical action is not `$D0/$E0`.

## Dispatch boundary

The active updater reaches `$9ED1` when part0 flag `$08` is set. Before branching on flag `$04`, logical action families `$D0/$E0` divert to the already modeled `$A06E` terminal path.

This document covers only flag `$04` **set**. The flag `$04`-clear route at `$9F79` uses a different curve and horizontal scheme and remains separate.

## Interaction order

For most substates:

```text
sync visual part0 -> logical X/Y
contact ($98BA)
phase/curve work
animation
visual movement
```

Substate `$0C` inserts a projectile phase first:

```text
sync part0 -> logical X/Y
projectile hit ($9915)
sync part0 -> logical X/Y again
contact ($98BA)
...
```

The second sync is observable: a type `<5` projectile response may temporarily add six to logical Y, then `$9CD5` overwrites logical X/Y from the still-unmoved visual part before contact. Action/type/HP mutations are not overwritten by that position sync.

## Geometry

As in the normal route, collision geometry comes from `$9CD5`:

- ordinary substates: `(8,8,5,5)`;
- substate `$10`: `(4,4,2,2)`.

## Phase index and `$9EA1` curve

The curve index begins from logical phase `+$03` **before** phase increment.

If phase is `<$30`, the index is unchanged. Otherwise:

```text
index = (phase & $0F) | $20
```

Thus e.g. `$3F` reads curve index `$2F`.

The original stored phase is then incremented directly; it is not replaced by the normalized index.

`$9EA1` contains 48 raw signed/two-complement displacement bytes. Visual motion performs raw 8-bit `Y -= curveByte`.

At curve index `$1F` or `$2F`, logical field `+$05 >= $80` requests sound `$2B`.

## Animation before movement

The shared `$9DCD` animation table update occurs before this route moves the visual parts. Table selection remains:

- `$9E41` for substate `$0C`;
- `$9E81` for `$10`;
- `$9E61` otherwise.

## Visual movement and retirement

For each processed part:

```text
Y -= curveByte
if Y >= $B0: clear whole class
X -= 2
X -= $43
if X < 4: retire this part only (Y=$F0, sprite=$FE)
```

Substate `$10` processes only part0. Other substates process four parts.

After movement, if all sprite bytes are `$FE`, the common `$A00F/$A01F` cleanup clears logical phase.

## Clean-room representation

`PlatformMultisprite9B93Flag08Bit04` models this closed branch, including optional `$0C` projectile routing, contact state, raw curve bytes, animation, individual X retirement and whole-class cleanup. The flag `$04`-clear branch is intentionally not approximated here.

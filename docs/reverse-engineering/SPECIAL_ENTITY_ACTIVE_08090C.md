# Special entity active runtime: types `$08/$09/$0C`

Status: **CONFIRMED by direct static flow and composed clean-room fixtures**.

These scheduled special types share the pre-dispatch at `$A478-$A55E`, but their active behavior after `$A55E` differs materially from common `$00-$07` entities. They therefore remain a dedicated runtime rather than being forced through the common dispatcher.

## Full composed order

For an active special slot:

```text
pre-dispatch $A478-$A55E
  -> possible immediate $A908 + sound $29
  -> shared $A55E preparation / camera path
  -> if eligible: $9915 projectile hit
  -> $98BA parent contact
  -> $AA70 attached-hazard contact
  -> +$04 nonzero cadence (1..11 -> 0)
  -> $A79E special $40 reaction
  -> type0C $A7D0 vertical bob
  -> $A7FB death cadence
  -> late $A886 $70 progression
  -> possible late $A908 spawn
```

The order is observable and must not be flattened.

## Immediate versus late `$A908`

The threshold path in the pre-dispatch does **not** end the entity update. If `+$03 != 0`, it sets `+$04=1`, calls `$A908`, plays `$29`, resets `$039A`, and then falls through to `$A55E`.

Therefore the newly created attached hazard already exists when `$AA70` runs later in the same update.

By contrast, `$A908` requested by late `$A886` occurs after `$AA70`; that object cannot contact the player until a later update.

## Movement / preparation

Types `$08/$09/$0C` do not use common chase/jump decisions at `$A970`. For the supported ordinary/`$70` path, screen X receives only the shared camera correction before removal/proximity tests.

`$08/$09` are explicitly excluded from the phase-zero proximity fall branch. `$0C` can enter `$50` there and still completes the current interaction/post path before `$50` becomes the next-frame entry family.

Frame-start `$50/$E0` instead take the dedicated `$A57E` path and jump directly to `$A886`, skipping parent interaction, `$AA70`, +$04 cadence and the type0C bob for that update.

## Main interaction

The special parent uses the tall hitbox:

```text
$79=$10  $7A=$08  $7B=$0E  $7C=$04
```

Projectile resolution `$9915` occurs before parent contact `$98BA`. Attached contact `$AA70` comes after both using the reduced 4/4/2/2 geometry.

This gives the parent contact priority over the child through the shared `$76` latch: if `$98BA` already seeded `$76=$20`, `$AA70` cannot trigger in the same update.

## `+$04` cadence

After `$AA70`:

```text
+$04 == 0   -> unchanged
+$04 != 0   -> increment
result >=12 -> 0
```

This turns the immediate-spawn seed `1` into an 11-update cooldown back to zero.

## Special `$40` reaction

A special entity entering `$40` increments the action byte on the same post path:

```text
$40 -> $41 -> ... -> $4F -> $00
```

Unlike common types, `$08/$09/$0C` do not consume the common `+$03` knockback helper on this path.

A projectile that writes `$40` during `$9915` can therefore end that same update at `$41`.

## Type `$0C` bob

After `$40` handling and before death handling, type `$0C` applies a vertical sample only when:

```text
($3C & 7) == 0
```

Index:

```text
($3C >> 3) & 3
```

Table:

```text
+1, +1, -1, -1
```

Frame-start `$50/$E0` bypass this bob because those paths jump directly to `$A886`.

## `$D0` death

After the optional type0C bob, `$D0` advances when `($3C & 3)==0`, using the already-confirmed descriptor-dependent `+2 Y` rule. Reaching `$E0` clears action and removes the record. A kill created during `$9915` can advance from `$D0` to `$D1` on the kill frame when cadence aligns.

## Late `$70`

`PlatformCommonEntityAttack70.AdvanceAfterInteraction(...)` is reused because `$A886` itself already contains the special type rules:

- type `$08/$0C` suppress midpoint spawn at `$78`;
- special terminal `$08/$09/$0C` returns to `$00` and requests `$A908`/sound `$2D`.

Any late `$A908` is materialized through `PlatformEntityAttachedHazard` only after the current `$AA70` call.

## Scope boundary

This runtime closes the logical entity/update path for `$08/$09/$0C` but does not yet place it inside the higher two-slot/frame scheduler. The next integration step is to route visually active special slots through this compositor while common slots continue through the existing common dispatcher, sharing attacks, `$76/$7F/$80`, Seventh Sense, `$039A`, and attached-hazard state in original slot order.

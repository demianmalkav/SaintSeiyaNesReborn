# Platform projectile frame order — hit before motion

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: executable clean-room composition of the confirmed ordering boundary between common-entity projectile collision and the later player-attack updater.

## Confirmed late-frame order

Within the active platform frame, fixed `$C2D7` maps bank 3 and calls the object classes in this order:

1. `$9B93` — special/event object path;
2. `$96B4` — secondary spawn/allocation;
3. `$9761` — secondary-object update;
4. `$A442` — common movable-entity update;
5. `$A22C` — player attack/projectile update.

The common entity path reaches `$9915/$992A` to test player attacks. Therefore projectile-vs-entity collision happens **before** `$A22C` moves or ages those player attack objects.

## Consequence for a newly created projectile

`$AAE4` can call `$BBCA` and create an attack earlier in the same frame. The new attack is therefore visible to `$A442/$9915` immediately at its birth coordinates.

Only after that collision pass does `$A22C`:

- decrement the projectile range/lifetime counter;
- update `$64/$65` animation parity from the still-current `$3C`;
- move a generic projectile by 5 pixels;
- update Shun's extend/retract chain geometry.

Thus the observable order is:

`BBCA create -> A442/9915 hit-test at birth position -> A22C move/age`

not:

`BBCA create -> move -> hit-test`.

## Consumption consequence

The hit helper `$9A27` retires standard Hyoga/Shiryu projectiles on applicable hits before `$A22C` runs. A projectile retired during `$A442` is no longer a `$64/$65` object when `$A22C` sees it, so it receives no same-frame movement or range decrement.

Seiya/Shun/Ikki do not receive that standard immediate retirement in `$9A27`, so their surviving attack objects can continue into the later `$A22C` phase.

## `$80` reload exception

When frame-start action is family `$80` and `$76==0`, `$AAE4` takes the exceptional reload/reinitialization path and does not return to the ordinary platform frame. Neither `$A442/$9915` nor `$A22C` is reached.

## Executable composition

`PlatformProjectileCombatFramePhases.ResolveHitsThenAdvanceAttackObjects` composes the already-promoted canonical components without duplicating them:

- `PlatformProjectileHitSequence` for `$9915` slot order and hit resolution;
- `PlatformAttackFramePhases.UpdateObjectsAfterPlayer` for the later `$A22C` phase.

The self-test suite includes a discriminating edge fixture: a newly created Seiya projectile begins at `X=$52`, and the entity hitbox is placed so `$52` is exactly its inclusive upper bound while the later moved position `$57` would be outside. The hit must therefore occur before the +5 movement.

No ROM payload is embedded in the model.

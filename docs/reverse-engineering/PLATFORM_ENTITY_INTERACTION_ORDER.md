# Common entity interaction order — projectile hit before player contact

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: static order confirmed directly in the canonical ROM and promoted to executable clean-room composition.

## Call order inside the common entity paths

Two common bank-3 paths contain the same consecutive calls:

- `$A723: JSR $9915`
- `$A726: JSR $98BA`

and later:

- `$A798: JSR $9915`
- `$A79B: JSR $98BA`

Therefore, for these ordinary common-entity update paths, the order is:

`player projectile -> entity resolution`

followed by:

`entity -> player contact resolution`.

The second call is not evaluated against a frame-start copy of the entity. It reads the live record after `$9915` has had the opportunity to mutate it.

## Same-frame mutation consequence

Projectile hit routing can alter entity fields before contact. One clear example is the observed type-1 reaction path, which applies:

- `entity Y += 6`
- `entity state = $E0`.

Because `$98BA` follows immediately, its asymmetric contact window uses that updated Y coordinate.

The executable fixture places the player so that contact would succeed against the pre-hit entity position (`Y=$50`) but fails after the projectile reaction moves the same entity to `Y=$56`. This distinguishes the confirmed order from a hypothetical contact-first implementation.

## Resource consequence

If the post-hit contact still succeeds, `$98BA` seeds/overwrites:

- `$7F` Life-drain ticks;
- `$80` Cosmo-drain ticks;
- `$76 = $20` ordinary contact latch.

Those new drain counters are still too late to affect the current frame's pre-player drain phase; their first resource loss remains on the next frame.

## Executable composition

`PlatformCommonEntityInteractionPhases.ResolveProjectileHitThenContact` composes the existing canonical implementations:

1. `PlatformProjectileHitSequence.ResolveThreeSlots`;
2. `PlatformContactFramePhases.ApplyOrdinaryEntityContact` using the resulting entity X/Y.

The helper intentionally does not claim the full `$A442` AI/render pipeline. It fixes only the interaction-order boundary proven by the ROM.

No ROM payload is embedded in the model.

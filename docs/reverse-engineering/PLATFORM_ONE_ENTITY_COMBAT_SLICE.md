# First end-to-end common combat slice

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: executable clean-room composition of the confirmed combat ordering for one common entity already positioned at the interaction point of `$A442`.

This is intentionally **not yet the entire platform frame**. Special/event objects, secondary spawned objects, preceding common-entity AI/movement/render preparation, NMI/OAM work and unrelated mode logic remain outside this slice.

## Composed order

For a non-fatal ordinary active-platform frame, the slice now executes these already-promoted components in ROM order:

1. pre-player attack-busy advancement (`$926C` semantics);
2. pending Life/Cosmo drain (`$927A/$930A`);
3. semantic resource subtraction;
4. platform damage derivation from post-drain Cosmo (`$C52F -> $8616`);
5. player/action dispatcher `$AAE4`, including B/projectile creation through `$BBCA` using the same post-drain Cosmo;
6. post-player `$76` decrement in `$B94B`;
7. common-entity projectile hit sequence `$9915/$992A`;
8. ordinary entity-to-player contact `$98BA`, observing any entity mutation from step 7;
9. player attack/projectile update `$A22C` using the still-current `$3C`;
10. fixed `$C402` frame-counter increment.

## Physical `$76` aliasing

The original has one physical RAM byte `$76`. Earlier clean-room components expose that byte through both player state and contact-phase state because they model different engine responsibilities.

`PlatformOneEntityCombatSlice` therefore requires those views to agree at frame entry, then explicitly resynchronizes them after:

- player/hazard mutations;
- the `$B94B` decrement;
- a possible `$98BA` contact reseed to `$20`.

This prevents impossible clean-room states that could never exist in the NES RAM image.

## Integrated fixture

The main self-test deliberately crosses several boundaries in one frame:

- Seiya enters with Cosmo 200 and one pending Cosmo-drain tick;
- pre-player drain makes Cosmo 199;
- damage becomes 36 rather than 38;
- B creates the projectile with range seed 3 rather than 6;
- the projectile collides at its birth coordinate before `$A22C`;
- a 40-HP entity is reduced to HP 4 and enters `$40` reaction;
- the same entity then contacts the player and seeds `$76=$20`, `$7F=2`, `$80=3`;
- the surviving Seiya projectile finally moves +5 and its range becomes 2;
- animation parity uses old `$3C=1` (`$65` projectile tile/state);
- only after those late phases does `$3C` become 2.

## Last invulnerability count can expire before contact

Another fixture starts the frame with `$76=1`.

`$AAE4` runs while the byte is still 1, then `$B94B` decrements it to zero before `$A442` reaches `$98BA`. Consequently a valid entity contact later in that same frame can immediately trigger and reseed `$76=$20`.

This is an important timing property for exact parity and should not be replaced by a generic "decrement timer at end of frame" implementation.

## `$80` reload exception

If frame-start action is family `$80` with `$76==0`, `$AAE4` takes the exceptional reload/reinitialization path. The normal call chain is abandoned, so this slice correctly skips:

- common-entity interaction;
- `$A22C` projectile update;
- the later `$C402` frame-counter increment.

## Architectural consequence

This is the first point where player control, resource economy, attack creation, entity combat, contact damage scheduling, projectile motion and frame timing execute together under one deterministic clean-room contract.

The next logical expansion is to prepend the common entity's own `$A442` motion/decision preparation, then add the special/secondary object classes that run before it.

No ROM payload is embedded in the model.

# Two common entity slots — ordered shared-state processing

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: executable clean-room composition for the two ordinary common movable-entity records processed by `$A442`.

## Original slot topology

The common movable-entity updater processes two 16-byte records:

1. slot A: `$03BA-$03C9`;
2. slot B: `$03CA-$03D9`.

They are not independent parallel simulations. Slot B runs after slot A and observes mutable global/player state left by slot A.

## Shared state threaded A -> B

`PlatformTwoCommonEntityCombatSlice` explicitly carries these values from the first slot into the second:

- player attack/projectile slots;
- shared contact/hazard latch `$76`;
- pending Life drain `$7F`;
- pending Cosmo drain `$80`;
- Seventh Sense reward total.

Each entity keeps its own runtime record, but these systems are common to the frame.

## Projectile-consumption consequence

The first self-test uses Hyoga with one active `$64` projectile overlapping both entities.

Slot A resolves first. The standard `$9A27` helper retires Hyoga's projectile on the hit.

Slot B then receives the already-mutated player attack state. Its slot-0 collision sees `$FE/$F0`, so the second entity cannot be damaged by that projectile in the same frame.

This differs materially from evaluating both entities against a snapshot of attacks taken before `$A442`.

## Contact-latch consequence

The second self-test begins with `$76=0` and no active projectile.

Both entities overlap the player. Slot A reaches `$98BA` first and writes:

- `$76=$20`;
- its Life-drain count into `$7F`;
- its Cosmo-drain count into `$80`.

Slot B then reaches the same contact gate with `$76` already nonzero. It is rejected as `ContactLatchActive` and cannot overwrite A's drain profile.

Thus ordinary same-frame double contact is serialized by slot order and the shared latch.

## Late-frame work still runs once

After both common entity slots finish, `$A22C` updates player attacks once, then fixed `$C402` increments `$3C` once. The two-slot slice therefore does not accidentally age attacks or frame timing once per entity.

## Scope

This composition currently uses the ordinary mobile preparation route for both records (types `$00-$07`, entry family `$10`). Special-state paths and types `$08+` will be added as separate dispatch branches rather than forced through the ordinary route.

No ROM payload is embedded in the model.

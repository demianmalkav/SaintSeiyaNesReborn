# Character index mapping

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: **CONFIRMED** mapping between the platform engine's internal Saint index and the five playable Bronze Saints.

## Two index spaces

The engine uses two related five-value selectors:

- `$03` — internal active-Saint index used by platform stats/movement/attack code;
- `$0533` — canonical character selector used by higher-level progression, palette and scenario code.

Fixed-bank table `$E505` contains:

`[0, 2, 1, 3, 4]`

It is an involution: applying it twice restores the original value.

The engine uses it in both directions:

- `$E121-$E126`: `$03 -> table[$03] -> $0533`;
- `$E245-$E24B`: `$0533 -> table[$0533] -> $03`.

Therefore internal slots 1 and 2 are deliberately swapped relative to the canonical selector space.

## Canonical selector identity from palette pipeline

Bank 5 `$AF9D+` computes `selector * 16` from `$0533` and copies a 16-byte palette block from `$B11A+` into `$0616-$0625`.

Fixed `$E99F+` then writes those 16 bytes directly to PPU palette RAM beginning at `$3F00`.

The five selector palettes have distinctive character color signatures:

- selector 0: silver/white plus strong red/orange accents;
- selector 1: silver/white and cool/light-blue tones;
- selector 2: magenta/pink plus green/cyan tones;
- selector 3: dominant green/cyan tones;
- selector 4: blue/white plus red/orange tones.

These correspond directly to the five playable Cloth/undersuit color identities:

| Canonical selector `$0533` | Character |
|---:|---|
| 0 | Seiya / Pegasus |
| 1 | Hyoga / Cygnus |
| 2 | Shun / Andromeda |
| 3 | Shiryu / Dragon |
| 4 | Ikki / Phoenix |

## Internal platform index `$03`

Applying `$E505 = [0,2,1,3,4]` gives:

| Internal index `$03` | Canonical selector | Character |
|---:|---:|---|
| 0 | 0 | Seiya |
| 1 | 2 | Shun |
| 2 | 1 | Hyoga |
| 3 | 3 | Shiryu |
| 4 | 4 | Ikki |

## Independent behavioral corroboration

The mapping is also consistent with independently reconstructed platform behavior:

### Internal 1 — Shun

The B-attack path treats index 1 specially:

- initializes projectile/attack type `$54`;
- seeds extension state `$0391 = 5`;
- update routine `$A311+` extends the attack by `+5`, then retracts by `-5` after the range counter expires;
- it uses a second OAM/projectile slot during the extension.

This is the unique returning/extend-retract attack profile expected for Andromeda's chain.

### Internal 4 — Ikki

Slot 4 uniquely initializes to Life `499` and Cosmo `499`, while the other four initialize to `99/99`. It also bypasses the ordinary Cosmo-range table and receives a fixed platform attack range/lifetime parameter of `60`.

### Internal 0 — Seiya

Slot 0 has the tallest A+Up jump profile (about 103 px maximum ascent) and the strongest growth in ordinary projectile range as Cosmo hundreds increase.

## Snapshot/password ordering

The persistent snapshot order now becomes understandable. `$951F` stores the five internal slots into snapshot records in this order:

`internal [0,2,1,3,4]`

which is exactly canonical selector order:

`[Seiya, Hyoga, Shun, Shiryu, Ikki]`.

The password serializes only the first four canonical records:

`Seiya, Hyoga, Shun, Shiryu`

and excludes the fifth record:

`Ikki`.

This is direct architectural evidence for why the fifth active slot is absent from the password format.

## ORIGINAL SPEC consequence

All future tables indexed by `$03` must use internal platform order:

`[Seiya, Shun, Hyoga, Shiryu, Ikki]`

All tables indexed by `$0533` use canonical/high-level order:

`[Seiya, Hyoga, Shun, Shiryu, Ikki]`.

Confusing these two spaces silently swaps Shun and Hyoga, so code and documentation should always state which index domain is being used.

# Platform attacks — projectile slots and Saint-specific behavior

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: static reconstruction. Object allocation, generic projectile motion, Shun-like extend/retract behavior and Cosmo-range tables are directly established by code. Character identity labels are only promoted where a mechanic/stat combination is unique enough to cross-identify safely.

## Attack entry

Bank 3 `$BBCA+` is the platform B-button attack path.

- `$3D & $40` is B.
- `$4C` is a B-button latch: one press creates at most one attack until B is released.
- `$4B` is an attack busy/cooldown counter. Bank 1 `$926C` advances non-zero values and wraps at 8.
- a successful attack sets `$4B = 1`.
- attack origin Y comes from `$BCC2`: `player_y + 7`, plus another 8 pixels while the frame-start action snapshot `$4E` is crouch family `$20`.
- facing is copied from `$42`.
- initial X is approximately `player_x + $12` when facing right or `player_x - 9` when facing left.

This gives REBORN a clean semantic separation: input latch, cooldown, projectile allocation, origin, facing and per-Saint behavior are independent concepts even though the NES implementation stores them compactly.

## Projectile/object slots

The attack code allocates objects from three 8-byte records rooted at:

- `$0730`
- `$0738`
- `$0740`

The byte at offset `+1` acts as a type/active marker; `$FE` is used as an inactive marker in this subsystem.

Allocation policy depends on `$03` (engine Saint index):

- indices `0,1,2`: primary slot `$0730` only;
- index `4`: can use `$0738`, then fall back to `$0730`;
- index `3`: can use `$0740`, then `$0738`, then `$0730`.

Thus index 3 can sustain three concurrent ordinary attack objects and index 4 two. This is a concrete implementation of character-specific attack cadence/capacity rather than just cosmetic animation.

## Generic projectile

For every Saint except engine index 1, creation writes projectile type `$64`.

Updater `$A250+` recognizes `$64/$65` as a moving projectile family. In the generic path:

- the projectile moves by 3 pixels per update along facing;
- a per-object lifetime/range counter is decremented;
- when the counter expires or the projectile leaves the valid horizontal region, the object is retired with `$FE/$F0` markers.

The animation tile/type toggles with frame parity for several projectile families.

## Engine index 1 — extend/retract two-part attack

Engine index 1 is structurally unique.

Creation at `$BC40+` writes type `$54` instead of `$64`, initializes `$0391 = 5`, and uses two object records. Bank 3 `$A311+` then performs a two-phase motion:

1. **extension** — while range counter `$038E` is non-zero, decrement it and add 5 to `$0391` each update;
2. **retraction** — after the outward counter reaches zero, subtract 5 from `$0391` each update;
3. both object positions are rebuilt symmetrically around the player using that extension distance;
4. when the extension distance becomes negative, both attack objects are retired.

The two visible attack parts therefore move away from the player and subsequently return toward the player. This is not generic projectile behavior.

That mechanic uniquely matches **Shun's chain attack**, and external gameplay documentation independently describes Shun's Cosmo attack as returning. Engine Saint index **1 is therefore identified as Shun with high confidence**.

## Cosmo-dependent range/lifetime table

For indices 0–3, `$BC73+` reads the hundreds digit of the current Saint's Cosmo, groups it into five brackets, combines the bracket with Saint index, and indexes the 20-byte table at `$BCAE`.

Rows are Cosmo-hundreds brackets; columns are engine indices 0..3:

| Cosmo hundreds | idx 0 | idx 1 | idx 2 | idx 3 |
|---|---:|---:|---:|---:|
| 0–1 | 3 | 1 | 4 | 6 |
| 2–3 | 6 | 3 | 10 | 10 |
| 4–5 | 12 | 8 | 16 | 14 |
| 6–7 | 24 | 12 | 22 | 18 |
| 8–9 | 48 | 16 | 28 | 22 |

Engine index 4 bypasses the table and uses fixed value `60`.

The exact real-world distance represented by one lifetime unit depends on projectile type/update cadence, but the relative design is already explicit.

## Character-index reconstruction

Current identity evidence:

| Engine index | Identity | Evidence status |
|---:|---|---|
| 0 | Seiya | strong cross-validation: uniquely largest high jump (103 px) and strongest Cosmo range growth (up to 48), matching documented Seiya traits |
| 1 | Shun | high confidence: unique extend/retract two-part attack mechanically identifies the chain; also has higher jump profile |
| 2 | Hyoga | strong inference by remaining trait profile: enhanced projectile range without multi-slot rapid-fire allocation |
| 3 | Shiryu | strong inference: three simultaneous ordinary projectile slots implement the strongest rapid-fire capacity |
| 4 | Ikki | high confidence: unique new-game 499 Life / 499 Cosmo record plus two-slot attack capacity and fixed long range 60 |

The index-2 / index-3 distinction should remain `INFERRED` until one more ROM-internal identity anchor (portrait/name/character-specific graphics or technique data) is tied to the same indices.

## Why this matters for REBORN

The original already gives the Bronze Saints different *mechanical identities*:

- projectile range responds differently to Cosmo;
- concurrent attack capacity differs;
- Shun has a genuinely different two-part returning weapon model;
- attack origin changes while crouched;
- projectile motion is stateful and directional.

A modern implementation should preserve those semantic differences while replacing OAM-slot scarcity and coarse 3/5-pixel increments with richer hitboxes, animation and effects.

## Next targets

1. tie indices 2/3 conclusively to Hyoga/Shiryu using ROM-internal graphics/text/technique evidence;
2. identify hitbox dimensions and enemy-hit resolution for `$64/$54` families;
3. map projectile damage and Cosmo consumption, if any, in platform mode;
4. determine whether concurrent slots alter cooldown or only permit another B press after the global 8-count busy interval;
5. produce parity tests for projectile creation, movement, retraction and retirement.

# Platform combat — projectile hit detection and damage

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: attack power, projectile-slot selection, point-vs-expanded-entity overlap and ordinary enemy HP subtraction are statically reconstructed. Enemy-type special cases and reward semantics still need fuller classification.

## Damage value `$72`

Fixed gameplay path `$C52F` maps PRG bank 1 and calls `$8616` once during active platform processing before bank-3 entity/combat updates.

Bank 1 `$8616-$86CA` computes the value stored at `$72`. Bank 3 `$99BA+` later subtracts exactly `$72` from an enemy record's HP field.

Therefore:

`$72 = platform attack damage/power for the current Saint and current Cosmo`

## Per-Saint base coefficients

Bank 1 table `$8611` contains, in internal Saint order:

| Internal index | Saint | Base coefficient |
|---:|---|---:|
| 0 | Seiya | 19 |
| 1 | Shun | 25 |
| 2 | Hyoga | 21 |
| 3 | Shiryu | 17 |
| 4 | Ikki | 15 |

This means Shun has the largest raw platform-damage coefficient at equal Cosmo; Ikki the smallest. Range/lifetime is a separate character/Cosmo-dependent system documented in `PLATFORM_PLAYER.md`.

## Exact damage formula

Let current Cosmo be the three decimal digits:

`Cosmo = 100*H + 10*T + O`

and `B` be the Saint's coefficient above.

The original routine performs repeated BCD-digit multiplication and deliberately truncates at intermediate decimal places.

### Cosmo below 100 (`H = 0`)

The exact result is:

`damage = floor(B * Cosmo / 100)`

### Cosmo 100 or above (`H > 0`)

The ones digit is not used at all:

`damage = B*H + floor(B*T/10)`

Equivalent form:

`damage = floor(B * floor(Cosmo/10) / 10)`

This creates visible plateaus: e.g. Cosmo 990..999 all produce the same damage.

Representative values:

| Cosmo | Seiya | Shun | Hyoga | Shiryu | Ikki |
|---:|---:|---:|---:|---:|---:|
| 50 | 9 | 12 | 10 | 8 | 7 |
| 99 | 18 | 24 | 20 | 16 | 14 |
| 100 | 19 | 25 | 21 | 17 | 15 |
| 199 | 36 | 47 | 39 | 32 | 28 |
| 500 | 95 | 125 | 105 | 85 | 75 |
| 999 | 188 | 247 | 207 | 168 | 148 |

A reproducible implementation that reads the coefficients from a user-supplied canonical ROM lives at `tools/physics/platform_damage.py`.

## Attack slots

Bank 3 `$9915` checks up to three attack/projectile sprite slots through helpers:

- `$9A3F` -> OAM-like record `$0730`, counter/state `$038E`
- `$9A4C` -> record `$0738`, counter/state `$038F`
- `$9A59` -> record `$0740`, counter/state `$0390`

The attack record uses NES OAM-compatible coordinate positions:

- offset `0` = sprite Y
- offset `3` = sprite X

The hit routine accepts active attack tiles/signatures whose offset-1 value, after clearing bit 0, matches `$64` or `$54`.

## Enemy/entity record coordinates

The entity pointer is held in `$16/$17`.

The collision routine and coordinate-copy helper `$9CD5` establish:

- entity offset `0` = high-level state/status family
- entity offset `1` = X
- entity offset `2` = Y
- entity offset `9` = entity/enemy type/class
- entity offset `$0C` = HP for the ordinary damaging path
- entity offset `$0F` = value consumed on death/reward processing

Other offsets remain under classification.

## Generic projectile-vs-entity overlap — `$9915/$992A`

`$9915` receives three extent parameters in A/X/Y and uses a fourth preloaded value in `$79`:

- `$79` = vertical/entity-origin offset
- `$7A` = horizontal/entity-origin offset
- `$7B` = vertical extent
- `$7C` = horizontal extent

For each active attack slot, `$992A` performs two interval tests using the attack sprite's point coordinate against an expanded entity rectangle.

### Vertical axis

Using `entity_y = record[2]` and `attack_y = projectile[0]`:

`low_y = entity_y + $79 - $7B - 6`

`high_y = entity_y + $79 + $7B`

Hit requires:

`low_y < attack_y <= high_y`

(up to exact unsigned-boundary behavior of the 6502 comparisons).

### Horizontal axis

Using `entity_x = record[1]` and `attack_x = projectile[3]`:

`low_x = entity_x + $7A - $7C - 8`

`high_x = entity_x + $7A + $7C`

Hit requires attack X to lie within that interval as well.

The constants `6` and `8` compensate for sprite/origin conventions rather than representing general entity half-widths.

Different entity update paths call `$9915` with different extents. Examples include:

- `$79=16`, A=`8`, X=`14`, Y=`4`
- `$79=8`, A=`8`, X=`6`, Y=`6`
- generic entity path from `$9CD5`: normally `$79=8`, A=`8`, X=`5`, Y=`5`; a special mode uses `$79=4`, A=`4`, X=`2`, Y=`2`.

Thus hitboxes are data/parameter driven at the call site rather than one universal rectangle.

## Ordinary damage application — `$99BA+`

After a valid collision survives enemy-type special handling:

1. read enemy HP at record offset `$0C`;
2. subtract current platform damage `$72`;
3. if result remains positive, store remaining HP;
4. transition the entity to a hit/reaction state (commonly state-family `$40`);
5. if subtraction reaches zero or underflows, use record offset `$0F` in death/reward processing;
6. transition to a death/removal state, usually `$D0`, with an `$A0` special case for type `$0D`.

This establishes a complete high-level semantic chain:

`Cosmo + Saint coefficient -> $72 damage -> projectile overlap -> enemy HP -= $72 -> reaction/death`

## Character-specific projectile consumption

Helper `$9A27` conditionally retires the attacking sprite/object after certain hits.

It leaves the attack object untouched for internal indices:

- 0 Seiya
- 1 Shun
- 4 Ikki

For:

- 2 Hyoga
- 3 Shiryu

it writes hidden/inactive values (`Y=$F0`, tile/state `$FE`) to the current attack record.

This does **not** yet prove a general design statement such as "piercing attacks" for the other three; it only proves the original helper's consumption behavior at this collision point.

## Enemy type special cases

Entity type/class is read from record offset 9. The hit routine contains explicit branches for at least `$0A`, `$0B`, `$0D`, and other ranges before reaching ordinary HP damage.

These special cases likely correspond to enemies/objects with distinct response rules. They remain to be mapped to named stage entities before assigning narrative labels.

## ORIGINAL SPEC consequences

Platform combat is substantially more systemic than a fixed `1 damage per hit` model:

- attack damage scales with current Cosmo;
- each Saint has a distinct damage coefficient;
- attack reach/lifetime separately scales by Cosmo and Saint;
- entity hitboxes are parameterized by caller/type;
- entities maintain explicit HP and hit/death state transitions;
- some Saints' attack objects are consumed differently on collision.

For REBORN, damage and reach should therefore remain separate data-driven dimensions even if animation, hitboxes and feel are modernized.

## Next work

1. map enemy record offsets 3-15 more completely;
2. classify type IDs and their special hit responses;
3. identify the meaning of death/reward byte at offset `$0F`;
4. trace enemy-to-player damage and invulnerability timer `$76`;
5. convert platform damage/range/hitbox rules into clean-room parity tests.

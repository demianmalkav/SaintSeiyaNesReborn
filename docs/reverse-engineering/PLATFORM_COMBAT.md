# Platform combat — bidirectional damage and hit detection

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: player attack power, projectile-vs-entity overlap, enemy HP subtraction, entity-vs-player overlap, Life/Cosmo drain and the post-hit invulnerability timer are statically reconstructed. Enemy-type naming/reward semantics remain open.

## Player attack damage value `$72`

Fixed gameplay path `$C52F` maps PRG bank 1 and calls `$8616` once during active platform processing before bank-3 entity/combat updates.

Bank 1 `$8616-$86CA` computes `$72`. Bank 3 `$99BA+` subtracts exactly `$72` from an enemy record's HP field.

Therefore:

`$72 = platform_attack_damage`

## Per-Saint base coefficients

Bank 1 table `$8611`, in internal order `[Seiya, Shun, Hyoga, Shiryu, Ikki]`:

| Saint | Base |
|---|---:|
| Seiya | 19 |
| Shun | 25 |
| Hyoga | 21 |
| Shiryu | 17 |
| Ikki | 15 |

Shun has the largest raw platform-damage coefficient at equal Cosmo; Ikki the smallest. Attack range/lifetime is a separate system.

## Exact player damage formula

Let:

`Cosmo = 100*H + 10*T + O`

and `B` be the Saint coefficient.

### Cosmo below 100

`damage = floor(B * Cosmo / 100)`

### Cosmo 100 or above

The ones digit is deliberately ignored:

`damage = B*H + floor(B*T/10)`

Equivalent:

`damage = floor(B * floor(Cosmo/10) / 10)`

Thus 990..999 are one damage plateau.

Representative values:

| Cosmo | Seiya | Shun | Hyoga | Shiryu | Ikki |
|---:|---:|---:|---:|---:|---:|
| 50 | 9 | 12 | 10 | 8 | 7 |
| 99 | 18 | 24 | 20 | 16 | 14 |
| 100 | 19 | 25 | 21 | 17 | 15 |
| 199 | 36 | 47 | 39 | 32 | 28 |
| 500 | 95 | 125 | 105 | 85 | 75 |
| 999 | 188 | 247 | 207 | 168 | 148 |

Reproducible calculator: `tools/physics/platform_damage.py`.

## Player attack slots

Bank 3 `$9915` checks up to three OAM-like attack records:

- `$0730`, associated counter/state `$038E`
- `$0738`, associated counter/state `$038F`
- `$0740`, associated counter/state `$0390`

OAM-compatible coordinates:

- attack offset `0` = Y
- attack offset `3` = X

The routine accepts active attack tile/signature values whose offset-1 byte, after clearing bit 0, matches `$64` or `$54`.

## Platform enemy/entity record — confirmed fields

Entity pointer: `$16/$17`.

Static consumers establish:

| Offset | Meaning |
|---:|---|
| `0` | high-level entity state/status family |
| `1` | X coordinate |
| `2` | Y coordinate |
| `9` | entity/enemy type/class |
| `$0C` | HP for ordinary damaging entities |
| `$0D` | Cosmo-drain tick count inflicted on player |
| `$0E` | Life-drain tick count inflicted on player |
| `$0F` | death/reward/event value consumed on kill |

Other fields remain under classification.

## Projectile-vs-entity overlap — `$9915/$992A`

`$9915` receives A/X/Y extent parameters and uses a fourth preloaded value in `$79`:

- `$79` = vertical origin offset
- `$7A` = horizontal origin offset
- `$7B` = vertical extent
- `$7C` = horizontal extent

For each active attack slot, `$992A` tests the projectile coordinate as a point against an expanded entity rectangle.

Using `entity_y = record[2]`, `attack_y = projectile[0]`:

`low_y = entity_y + $79 - $7B - 6`

`high_y = entity_y + $79 + $7B`

Using `entity_x = record[1]`, `attack_x = projectile[3]`:

`low_x = entity_x + $7A - $7C - 8`

`high_x = entity_x + $7A + $7C`

Both axes must pass the unsigned 6502 comparisons.

Call sites use different extent sets, so enemy hitboxes are parameterized rather than globally fixed. Observed examples include:

- `$79=16`, A=`8`, X=`14`, Y=`4`
- `$79=8`, A=`8`, X=`6`, Y=`6`
- generic path: `$79=8`, A=`8`, X=`5`, Y=`5`
- reduced special path: `$79=4`, A=`4`, X=`2`, Y=`2`

## Enemy HP damage — `$99BA+`

After collision/type-specific gates:

1. read enemy HP from offset `$0C`;
2. subtract `$72`;
3. if positive, store remaining HP and enter a hit/reaction state (commonly family `$40`);
4. if zero/underflow, pass offset `$0F` to death/reward processing;
5. transition to death/removal state, usually `$D0`, with an `$A0` case for type `$0D`.

High-level chain:

`Saint + Cosmo -> $72 -> projectile overlap -> enemy HP -= $72 -> reaction/death`

## Character-specific attack retirement

Helper `$9A27` does not forcibly retire the current attack record for:

- Seiya (internal 0)
- Shun (internal 1)
- Ikki (internal 4)

For:

- Hyoga (internal 2)
- Shiryu (internal 3)

it hides/deactivates the current attack record (`Y=$F0`, tile/state `$FE`).

Do not overinterpret this as a universal "piercing" rule yet; it is the exact behavior of this collision helper.

## Entity-to-player overlap — `$98BA`

The opposite direction is a separate collision routine. It rejects hits when, among other gates:

- the player is in one excluded action family;
- player Y is too low/outside the active region;
- invulnerability timer `$76` is nonzero.

It compares the player's origin (`$3F/$40`) against a rectangle built from entity X/Y and the call-site extent parameters `$79-$7C`.

Using `entity_y = record[2]`, `player_y = $40`:

`low_y = entity_y + $79 - $7B - 30`

`high_y = entity_y + $79 + $7B`

Using `entity_x = record[1]`, `player_x = $3F`:

`low_x = entity_x + $7A - $7C - 12`

`high_x = entity_x + $7A + $7C`

The larger fixed subtractions (`30` vertical, `12` horizontal) account for the player's body/origin convention.

## What an enemy hit actually stores

When entity-to-player overlap succeeds and `$76 == 0`:

- entity offset `$0E` -> `$7F`
- entity offset `$0D` -> `$80`
- `$76 = $20` (32 decimal)
- hit sound/effect `$26` is triggered

These two copied bytes are **damage-duration counters**, not direct damage amounts.

### `$7F` — pending Life-drain ticks

Bank 1 `$927A/$9292+` runs during platform updates.

While `$7F > 0`:

1. decrement `$7F` by one;
2. subtract **2 Life** from the current Saint's packed-decimal `$59-$62` value.

Therefore an ordinary enemy hit configured with `record[0x0E] = N` inflicts nominally:

`2*N Life`

unless another state transition/death interrupts the drain.

### `$80` — pending Cosmo-drain ticks

Bank 1 `$930A+`:

While `$80 > 0`:

1. decrement `$80` by one;
2. subtract **1 Cosmo** from current Saint `$63-$6C`.

So `record[0x0D] = N` inflicts nominally:

`N Cosmo`

## `$76` — 32-frame post-hit invulnerability/flashing timer

A successful entity hit sets:

`$76 = $20` = 32 frames/update ticks.

Bank 3 `$B94B+` decrements `$76` once per platform update. While nonzero:

- `$98BA` refuses new entity-to-player hits;
- rendering logic conditionally takes an alternate sprite path based on frame-state bit `$3C & 4`, producing the characteristic hit flashing/blinking behavior.

This promotes `$76` from a generic timer to:

`player_hit_invulnerability_timer`

with an original duration of 32 platform update frames.

There are separate hazard/fall paths that also manipulate `$76` (including special `$F8/$F9` floor classes), so "32 after ordinary entity hit" should not be generalized to every possible damage/hazard event.

## Life/Cosmo exhaustion

The Life decrement path zeroes the current Life record and moves the engine into a fail/death transition when subtraction crosses below zero.

The Cosmo decrement path likewise zeroes Cosmo on underflow and enters the same major transition path.

Thus in this platform mode **both Life depletion and Cosmo depletion can trigger the failure transition** in the original implementation.

## Enemy type special cases

Type/class is read from entity offset 9. Attack handling has explicit branches for at least `$0A`, `$0B`, `$0D` and other ranges before ordinary HP subtraction.

These IDs remain numeric until stage/entity tables identify them reliably.

## ORIGINAL SPEC consequences

Platform combat is data-driven on both sides:

- player damage depends on Saint + current Cosmo;
- player attack range depends separately on Saint + Cosmo;
- enemies have explicit HP;
- hitboxes are call-site parameterized;
- enemy contact carries independent Life-damage and Cosmo-damage tick counts;
- successful contact grants 32 frames of ordinary-hit invulnerability/flashing;
- Life and Cosmo are both survival resources in platform mode.

REBORN can modernize animation and collision shapes while preserving these relationships as tunable data rather than flattening them into one generic damage number.

## Next work

1. map enemy record offsets 3-8 and 10-11;
2. identify type IDs and special response rules;
3. identify offset `$0F` death/reward semantics;
4. trace environmental hazard damage separately from enemy contact;
5. convert damage/range/hitbox/drain behavior into clean-room parity test vectors.

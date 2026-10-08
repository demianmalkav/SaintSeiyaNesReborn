# Platform combat — bidirectional damage and hit detection

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: player attack power, projectile-vs-entity overlap, enemy HP subtraction, entity-vs-player overlap, Life/Cosmo drain, post-hit invulnerability and the ordinary enemy Seventh-Sense reward path are statically reconstructed. Enemy type names and several record fields remain open.

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

Let `Cosmo = 100*H + 10*T + O` and `B` be the Saint coefficient.

For Cosmo below 100:

`damage = floor(B * Cosmo / 100)`

For Cosmo 100 or above, the ones digit is deliberately ignored:

`damage = B*H + floor(B*T/10)`

Equivalent:

`damage = floor(B * floor(Cosmo/10) / 10)`

Thus 990..999 are one damage plateau.

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

- `$0730`, counter/state `$038E`
- `$0738`, counter/state `$038F`
- `$0740`, counter/state `$0390`

OAM-compatible coordinates:

- attack offset `0` = Y
- attack offset `3` = X

The hit routine accepts active attack tile/signature values whose offset-1 byte, after clearing bit 0, matches `$64` or `$54`.

## Platform enemy/entity record — confirmed fields

Entity pointer: `$16/$17`.

| Offset | Meaning |
|---:|---|
| `0` | high-level entity state/status family |
| `1` | X coordinate |
| `2` | Y coordinate |
| `9` | entity/enemy type/class |
| `$0C` | HP for ordinary damaging entities |
| `$0D` | Cosmo-drain tick count inflicted on player |
| `$0E` | Life-drain tick count inflicted on player |
| `$0F` | packed-BCD Seventh Sense reward on kill |

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

Call sites use different extent sets, so enemy hitboxes are parameterized. Observed examples include `(16,8,14,4)`, `(8,8,6,6)`, the generic `(8,8,5,5)`, and a reduced `(4,4,2,2)` form for `$79,A,X,Y`.

## Enemy HP damage — `$99BA+`

After collision/type-specific gates:

1. read enemy HP from offset `$0C`;
2. subtract `$72`;
3. if positive, store remaining HP and enter a hit/reaction state (commonly family `$40`);
4. if zero/underflow, load offset `$0F` and call fixed `$D1E0`;
5. transition to death/removal state, usually `$D0`, with an `$A0` case for type `$0D`.

High-level chain:

`Saint + Cosmo -> $72 -> projectile overlap -> enemy HP -= $72 -> reaction/death`

## Seventh Sense reward — enemy offset `$0F`

The kill path loads entity record offset `$0F` into A and calls fixed routine `$D1E0`.

`$D1E0`:

- does nothing when high-level mode `$02 == 0`;
- otherwise treats A as a two-digit packed-BCD amount;
- adds it digit-by-digit into Seventh Sense `$05AA/$05AB`;
- propagates decimal carries across all four digits;
- saturates at `9999` on overflow.

Therefore offset `$0F` is not a generic score byte. In the ordinary platform kill path it is:

`seventh_sense_reward_bcd`

Example semantics: a record value `$15` represents +15 Seventh Sense, subject to the mode gate and 9999 clamp.

This closes another resource loop in ORIGINAL SPEC:

`kill enemies -> gain Seventh Sense`

## Character-specific attack retirement

Helper `$9A27` does not forcibly retire the current attack record for Seiya, Shun or Ikki. For Hyoga and Shiryu it hides/deactivates the current attack record (`Y=$F0`, tile/state `$FE`).

Do not overinterpret this as a universal "piercing" rule yet; it is the exact behavior of this collision helper.

## Entity-to-player overlap — `$98BA`

The opposite direction is a separate collision routine. It rejects hits when, among other gates, player state excludes contact, player Y is outside the active region, or invulnerability timer `$76` is nonzero.

Using `entity_y = record[2]`, `player_y = $40`:

`low_y = entity_y + $79 - $7B - 30`

`high_y = entity_y + $79 + $7B`

Using `entity_x = record[1]`, `player_x = $3F`:

`low_x = entity_x + $7A - $7C - 12`

`high_x = entity_x + $7A + $7C`

The larger fixed subtractions account for the player's body/origin convention.

## What an enemy hit stores

When contact succeeds and `$76 == 0`:

- enemy offset `$0E` -> `$7F`
- enemy offset `$0D` -> `$80`
- `$76 = $20` (32)
- hit sound/effect `$26` is triggered

The copied bytes are **damage-duration counters**, not direct damage amounts.

### `$7F` — pending Life-drain ticks

Bank 1 `$927A/$9292+`:

While `$7F > 0`:

1. decrement `$7F`;
2. subtract **2 Life** from current Saint.

Nominal contact Life damage from record value `N` is `2*N`.

### `$80` — pending Cosmo-drain ticks

Bank 1 `$930A+`:

While `$80 > 0`:

1. decrement `$80`;
2. subtract **1 Cosmo** from current Saint.

Nominal contact Cosmo damage from record value `N` is `N`.

## `$76` — 32-frame post-hit invulnerability/flashing timer

Ordinary entity contact sets `$76 = $20`.

Bank 3 `$B94B+` decrements it once per platform update. While nonzero:

- `$98BA` rejects further ordinary entity hits;
- rendering conditionally takes an alternate path based on `$3C & 4`, producing hit blinking/flashing.

Working/final semantic name:

`player_hit_invulnerability_timer`

Ordinary contact duration: **32 platform update frames**.

Separate hazard/fall paths also manipulate `$76`, so 32 must not be generalized to every hazard.

## Life/Cosmo exhaustion

The Life decrement path zeroes Life and enters the major fail/death transition on underflow. The Cosmo decrement path likewise zeroes Cosmo and enters the same transition.

Thus both Life depletion and Cosmo depletion can cause platform-mode failure.

## Enemy type special cases

Type/class comes from entity offset 9. Attack handling contains explicit branches for `$0A`, `$0B`, `$0D` and other ranges before ordinary HP subtraction. These IDs remain numeric until stage/entity tables identify them reliably.

## ORIGINAL SPEC consequences

Platform combat is data-driven on both sides:

- player damage depends on Saint + current Cosmo;
- player attack range depends separately on Saint + Cosmo;
- enemies have explicit HP;
- hitboxes are call-site parameterized;
- enemy contact carries independent Life/Cosmo drain durations;
- ordinary contact grants 32 frames of invulnerability/flashing;
- killed enemies can award packed-BCD Seventh Sense;
- Life and Cosmo are both survival resources in platform mode.

REBORN can modernize animation and collision shapes while preserving these relationships as tunable data.

## Next work

1. map enemy record offsets 3-8 and 10-11;
2. identify numeric type IDs and special response rules;
3. trace environmental hazard damage separately from enemy contact;
4. determine how Seventh Sense is spent/consumed outside this reward path;
5. convert damage/range/hitbox/drain/reward behavior into clean-room parity tests.

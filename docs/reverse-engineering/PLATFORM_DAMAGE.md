# Platform attack collision and damage

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: static reconstruction with arithmetic checked independently. The projectile-vs-entity overlap test, enemy HP field, Seventh Sense reward field and current-Cosmo damage computation are tied directly to code.

## Damage is recomputed from current Cosmo

During the active platform frame, fixed `$C52F` maps PRG bank 1 and calls `$8616`. That routine computes byte `$72`, which is later subtracted from enemy HP by bank 3 `$99BA+`.

The Saint-specific coefficients are stored at bank 1 `$8611` in **internal platform order**:

`[Seiya, Shun, Hyoga, Shiryu, Ikki]`

| Saint | Coefficient |
|---|---:|
| Seiya | 19 |
| Shun | 25 |
| Hyoga | 21 |
| Shiryu | 17 |
| Ikki | 15 |

The routine operates directly on the packed-decimal Cosmo digits in `$63-$6C`.

### Effective formula

For Cosmo below 100, the arithmetic is equivalent to:

`damage = floor(coefficient * Cosmo / 100)`

For Cosmo 100 or above, the original routine deliberately ignores the ones digit before the final calculation:

`damage = coefficient * hundreds + floor(coefficient * tens / 10)`

Equivalently:

`damage = floor(coefficient * floor(Cosmo / 10) / 10)`

This means attack damage becomes quantized in 10-Cosmo steps once the value has three digits. It is a real implementation quirk and should be preserved in ORIGINAL SPEC rather than silently replaced by a perfectly linear formula.

Examples:

| Cosmo | Seiya | Shun | Hyoga | Shiryu | Ikki |
|---:|---:|---:|---:|---:|---:|
| 50 | 9 | 12 | 10 | 8 | 7 |
| 99 | 18 | 24 | 20 | 16 | 14 |
| 100 | 19 | 25 | 21 | 17 | 15 |
| 499 | 93 | 122 | 102 | 83 | 73 |
| 999 | 188 | 247 | 207 | 168 | 148 |

The high per-hit coefficient for Shun coexists with his very different returning-chain attack geometry, so raw damage coefficient alone is not a complete measure of character strength.

## Enemy logical record fields

The projectile collision path uses a logical enemy/entity record through pointer `$16`.

Fields now established in that path:

- offset `+01`: entity X anchor;
- offset `+02`: entity Y anchor;
- offset `+09`: entity/type class used for special hit/death behavior;
- offset `+0C`: **enemy HP**;
- offset `+0D/+0E`: contact-damage/timer parameters copied into player-side `$80/$7F` during player contact;
- offset `+0F`: reward value passed to `$D1E0` when HP reaches zero.

`$D1E0` adds that reward to packed-decimal `$05AA/$05AB`, proving offset `+0F` is a **Seventh Sense reward amount** (or a packed amount directly feeding Seventh Sense progression).

## Projectile-vs-enemy collision entry

Bank 3 `$9915` receives three hitbox parameters in registers:

- A -> `$7A` = anchor offset `a`;
- X -> `$7B` = vertical radius parameter `v`;
- Y -> `$7C` = horizontal radius parameter `h`.

It then tests all three player attack slots (`$0730`, `$0738`, `$0740`) via pointer helpers `$9A3F/$9A4C/$9A59`.

Only projectile type families `$64/$65` and `$54/$55` participate in this overlap path.

Projectile records use:

- offset `+00`: projectile Y anchor;
- offset `+01`: projectile type/active marker;
- offset `+02`: facing/attributes;
- offset `+03`: projectile X anchor.

## Logical hit rectangle

For an entity at `(enemy_x, enemy_y)` with parameters `(a, v, h)`, the projectile point must satisfy the ROM's asymmetric anchor bounds:

Vertical:

`enemy_y + a - v - 6 < projectile_y <= enemy_y + a + v`

Horizontal:

`enemy_x + a - h - 8 < projectile_x <= enemy_x + a + h`

The asymmetry comes from sprite/object anchor conventions rather than from a centered modern rectangle.

A common setup built by `$989C` uses:

- `a = 4`
- `v = 3`
- `h = 3`

which produces an approximately 15×13-pixel logical target region around the entity anchor. Other enemy update paths call `$9915` with larger parameter sets such as `(8,14,4)` or `(8,6,6)`, proving hitbox size is entity-specific.

The important architectural point is that **visual sprite dimensions and collision dimensions are separate**.

## Hit resolution

On overlap:

1. sound/effect `$28` is requested;
2. entity type at `+09` selects special-case behavior;
3. ordinary damage path subtracts `$72` from enemy HP at `+0C`;
4. if HP remains above zero, HP is stored back and the entity can be moved into a hit/stun state family;
5. if damage reduces HP to zero or below, offset `+0F` is passed to `$D1E0` to add Seventh Sense reward, then the entity enters a death/destruction family (`$D0` or `$A0` depending on type).

## Projectile persistence after a hit

Helper `$9A27` introduces another Saint-specific difference:

- internal indices 0 (Seiya), 1 (Shun) and 4 (Ikki) do **not** automatically retire the projectile in this helper;
- internal indices 2 (Hyoga) and 3 (Shiryu) mark the current projectile inactive (`Y=$F0`, type=`$FE`) on the relevant hit path.

This must be interpreted alongside entity-type special cases before calling the first group universally 'piercing', but it is already a concrete per-Saint hit-response distinction.

## Player contact collision

The neighboring `$98BA+` path performs enemy/entity vs player overlap using the same entity anchors and parameter system. On valid contact, it:

- copies entity `+0E` to `$7F`;
- copies entity `+0D` to `$80`;
- sets `$76 = $20` (the previously identified invulnerability/contact timer);
- triggers sound/effect `$26`.

This ties `$76` directly to post-contact invulnerability/hit handling and promotes its role beyond the earlier generic timer hypothesis.

## ORIGINAL SPEC / REBORN consequence

A native implementation should model separately:

- rendered sprite size;
- entity logical anchor;
- entity hurtbox parameters;
- projectile anchor;
- projectile family;
- current-Cosmo damage calculation;
- Saint-specific projectile retirement behavior;
- Seventh Sense reward.

That lets REBORN enlarge and richly animate characters without accidentally changing the combat geometry inherited from the 1988 design.

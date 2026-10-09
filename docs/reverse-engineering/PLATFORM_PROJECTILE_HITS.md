# Platform projectile hits and ordinary entity HP resolution

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: projectile point-vs-entity overlap (`$9915/$992A`), standard Saint-specific attack consumption (`$9A27`), and the ordinary HP subtraction/death/reward path (`$99BA+`) are represented in clean-room executable code. Special entity-type behaviors `$0A/$0B/$0E` and recoil fields remain separate.

## Accepted attack records

`$992A` reads attack record offset `+1`, clears bit 0 and accepts only:

- `$64/$65` generic projectile family;
- `$54/$55` Shun chain family.

Thus both visible parity frames of a generic projectile and both Shun chain segments participate in the same overlap gate.

Inactive `$FE` records do not.

## Attack is a point; entity is the expanded rectangle

The collision routine uses:

- attack Y = attack record offset `0`;
- attack X = attack record offset `3`.

It does not construct a projectile rectangle in this routine. Instead, caller-provided parameters expand an entity origin into the acceptance rectangle.

Parameters:

- `$79` = vertical origin offset;
- `$7A` = horizontal origin offset;
- `$7B` = vertical extent;
- `$7C` = horizontal extent.

With entity record coordinates `entity_y=offset2`, `entity_x=offset1`:

```text
y_center = entity_y + $79
low_y    = y_center - $7B - 6
high_y   = y_center + $7B

x_center = entity_x + $7A
low_x    = x_center - $7C - 8
high_x   = x_center + $7C
```

All arithmetic is 8-bit and wraps before the unsigned comparisons.

A hit requires:

```text
low_y < attack_y <= high_y
low_x < attack_x <= high_x
```

The low bounds are therefore **exclusive** and the high bounds **inclusive**. This asymmetry is a literal consequence of the original `CMP/BCS` and `CMP/BCC` sequence and is preserved in ORIGINAL SPEC rather than normalized to a modern rectangle convention.

## Confirmed call-site presets

Four parameter sets are now identified:

| use | `$79` | `$7A` | `$7B` | `$7C` |
|---|---:|---:|---:|---:|
| common entity path | 8 | 8 | 5 | 5 |
| reduced special path | 4 | 4 | 2 | 2 |
| tall path | 16 | 8 | 14 | 4 |
| square/larger path | 8 | 8 | 6 | 6 |

The names describe geometry only; they do not yet assign narrative enemy identities to every caller.

`PlatformHitboxParameters` exposes these presets as semantic fixtures.

## Multiple attack slots

`$9915` checks the three attack slots in order:

1. `$0730`;
2. `$0738`;
3. `$0740`.

The third helper call falls directly through into `$992A`, so all three are evaluated.

This means concurrent Shiryu/Ikki projectiles can each independently participate in the entity hit routine in the same entity update. Full same-frame multi-hit consequences remain to be modeled only after every type-specific branch is composed.

## Hit sound and type routing

Once overlap succeeds, sound `$28` is triggered before entity-type routing.

Entity type is record offset `9`.

The ordinary HP path at `$99BA` is reached by these known types:

- `$00`;
- `$05-$09`;
- `$0C`;
- `$0D`;
- `$0F`.

Types `$0A/$0B` use a special response path. Type `$0E` can accept the geometric hit but bypass ordinary HP subtraction in the branch reconstructed so far. Types `$01-$04` enter a different state/Y response rather than the ordinary HP path.

Therefore overlap and HP damage must remain separate concepts.

## Standard projectile consumption helper `$9A27`

Many — but not all — post-overlap type routes call `$9A27`.

That helper checks the active Saint index:

- Seiya 0: attack remains active;
- Shun 1: attack remains active;
- Hyoga 2: retire current attack record;
- Shiryu 3: retire current attack record;
- Ikki 4: attack remains active.

For Hyoga/Shiryu it writes:

- attack Y = `$F0`;
- attack type = `$FE`.

It does not modify the separate lifetime byte.

This exact helper behavior should not yet be renamed globally as `piercing`, because some enemy types bypass `$9A27` before reaching damage. ORIGINAL SPEC exposes it as `ApplyStandardHitConsumption` instead.

## Ordinary HP subtraction `$99BA+`

Enemy HP is entity offset `$0C`. Platform attack damage is already reconstructed as `$72` / `PlatformDamage`.

The operation is ordinary unsigned subtraction:

`remaining = HP - platform_attack_damage`.

### Remaining HP positive

Store the remaining HP and enter reaction state family `$40`.

For types below `$08`, additional recoil/motion work follows through `$99F9`; that layer is intentionally not folded into the HP primitive yet.

### Zero or borrow

The entity is killed.

Before death state is written, entity offset `$0F` is passed to fixed `$D1E0` as packed-BCD Seventh Sense reward.

- mode/substate `$02==0`: reward suppressed;
- otherwise add reward with decimal carry;
- saturate at 9999.

Death state:

- type `$0D`: `$A0`;
- other ordinary-HP types: `$D0`.

The HP byte itself is not required to be rewritten to zero in this death path; state drives removal/death behavior.

## Clean-room implementation

`PlatformProjectileHit` provides three deliberately separate primitives:

1. `Overlaps(...)` — exact 8-bit point-vs-expanded-entity geometry;
2. `ApplyStandardHitConsumption(...)` — `$9A27` Saint-specific object retirement;
3. `ApplyOrdinaryHpDamage(...)` — `$99BA` HP/reaction/death/Seventh-Sense path.

Keeping these separate prevents type-specific routing from being accidentally flattened into a universal modern hit rule.

## REBORN consequence

The remake can eventually use richer collision shapes, but compatibility data should retain:

- original per-entity hitbox preset;
- original projectile/chain hit eligibility;
- Saint-specific consume-on-hit behavior;
- original damage coefficient/Cosmo relationship;
- reward and death-state semantics.

A modernized hitbox can then be layered as a REBORN choice rather than silently changing ORIGINAL SPEC.

## Next work

1. reconstruct `$0A/$0B/$0E` and `$01-$04` special projectile-hit responses;
2. model type<8 recoil state/offset changes at `$99F9`;
3. compose attack-object update and entity-hit order within one platform frame;
4. determine same-frame behavior when several attack slots overlap one entity;
5. integrate entity hit resolution into the first end-to-end native stage simulation.

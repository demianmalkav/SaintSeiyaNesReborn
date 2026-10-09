# Platform projectile hit type routing

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: branch routing from successful `$992A` overlap through `$9971-$9A27` is statically reconstructed for observed entity types through `$1F`. This document extends `PLATFORM_PROJECTILE_HITS.md`, which isolates geometry and the ordinary HP primitive.

## Why routing must be separate from overlap

A successful geometric overlap does **not** imply HP damage.

Every accepted overlap first triggers sound `$28`, then entity offset `9` is classified. Depending on type, the result can be:

- ordinary HP subtraction;
- directional knockback without HP;
- Y/state reaction without HP;
- accepted/absorbed hit with no HP/state change;
- a direct HP path that bypasses ordinary projectile consumption.

Flattening these into `if overlap: hp -= damage` would materially change the game.

## Sound routine register quirk

For most nonzero types, the router does:

```text
X = entity_type
A = table[$C0D3 + X]
JSR $DBB6
CMP ...
```

At first glance the compares appear to use the sound byte. They do not.

`$DBB6` saves X by `TXA/PHA`, and on return restores it with `PLA/TAX`. The final `PLA` leaves the saved X value in A. Therefore A after `$DBB6` again equals the original entity type.

This proves the post-sound comparisons remain type routing.

## Observed `$C0D3` table bytes

The promoted static range `$00-$1F` is:

```text
00:2E 01:28 02:28 03:28 04:28 05:28 06:28 07:31
08:28 09:28 0A:28 0B:28 0C:28 0D:35 0E:28 0F:28
10:B2 11:B0 12:00 13:D2 14:A1 15:00 16:00 17:8F
18:00 19:00 1A:00 1B:5C 1C:05 1D:02 1E:00 1F:04
```

Not every value is used by this exact hit path because several types branch before the table lookup. The table is nevertheless preserved as observed data needed by the fallthrough family.

## Routing order

After overlap sound `$28`:

### Types `$0A/$0B`

Direct branch to `$99F4`:

1. call standard consumption helper `$9A27`;
2. set impulse magnitude 4;
3. use attack facing and the entity's directional terrain descriptor;
4. if terrain is not `$E0-$EF`, write directional motion field offset `3`;
5. no HP subtraction.

Motion encoding:

- left-facing attack -> `$04`;
- right-facing attack -> `$44`.

Terrain values `<$E0` or `>=$F0` allow the write. Only `$E0-$EF` blocks it.

### Type `$0D`

Direct branch to `$99BA` ordinary HP subtraction.

Crucially, it bypasses:

- helper `$9A27`;
- type-indexed secondary sound lookup;
- survivor recoil path.

Therefore a Hyoga/Shiryu attack that hits type `$0D` is **not** consumed by `$9A27` in this route.

If killed, type `$0D` uses death state `$A0` rather than `$D0`.

### Remaining types: helper `$9A27`

All remaining overlapping types first run the standard Saint-specific consumption helper:

- Seiya/Shun/Ikki attack survives helper;
- Hyoga/Shiryu attack record is marked inactive.

Then routing continues.

### Type `$00`

Immediate ordinary HP path without type-indexed secondary sound.

If it survives, because type `<$08`, it also receives terrain-gated recoil magnitude 8.

### Types `$05-$09`

Type-indexed sound, then ordinary HP path.

- `$05-$07`: survivor recoil magnitude 8;
- `$08-$09`: no `$99F9` recoil after surviving HP subtraction.

Type `$07` has distinct secondary sound `$31`; the others in this range use `$28`.

### Types `$0C/$0F`

Type-indexed sound `$28`, then ordinary HP path. No type<8 survivor recoil.

### Type `$0E`

Type-indexed sound `$28`, then return.

No ordinary HP subtraction and no entity state/Y change in this branch. The attack may already have been consumed for Hyoga/Shiryu by `$9A27`.

Working semantic name in ORIGINAL SPEC: `AbsorbedNoHp`.

### Types `$01-$04`

After type-indexed sound, fall through to `$99AA`:

- entity Y += 6;
- entity state = `$E0`;
- no ordinary HP subtraction.

### High fallthrough family

Types not caught by the special comparisons likewise fall through to `$99AA`. The currently promoted type-sound table is known through `$1F`.

Thus `$10-$1F` can be represented mechanically where encountered, without inventing design identities.

## Survivor recoil for ordinary HP types below `$08`

If `$99BA` leaves positive HP and entity type `<$08`:

1. entity state becomes `$40`;
2. recoil amount is 8;
3. attack facing chooses direction;
4. terrain offset `$0A`/`$0B` gates the motion write.

Motion field offset `3` becomes:

- left: `$08`;
- right: `$48`.

Only terrain descriptor `$E0-$EF` blocks the motion write. The entity remains in state `$40` even when recoil movement is blocked.

Types `$08+` that survive the ordinary HP path get state `$40` without this amount-8 motion write.

## Clean-room router

`PlatformProjectileHitRouter.Resolve(...)` composes:

1. exact overlap gate;
2. primary hit sound;
3. entity-type branch;
4. standard projectile consumption when called by that branch;
5. optional type sound;
6. HP/reaction/special response;
7. survivor recoil where applicable;
8. Seventh Sense reward/death state through the existing HP primitive.

Result data deliberately reports:

- updated attack slot;
- updated entity;
- routing outcome;
- Seventh Sense;
- primary/secondary sound IDs;
- whether `$9A27` consumed the attack;
- whether terrain blocked a motion write.

This provides enough observability for frame-level parity tests without coupling the semantic layer to an audio or rendering engine.

## Remaining questions

1. assign design identities to `$01-$04`, `$0A/$0B`, `$0D/$0E` from spawn/art/event evidence;
2. determine the semantic meaning of entity offset 3 beyond its proven directional hit-motion writes;
3. compose all three attack slots in exact per-entity same-frame order;
4. determine whether a first slot kill can still permit later slots to re-enter type logic during the same `$9915` call;
5. merge player simulation, attacks, entity updates and hit resolution into one executable platform frame.

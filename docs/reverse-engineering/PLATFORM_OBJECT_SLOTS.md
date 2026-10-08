# Platform object slots and secondary spawn system

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: slot topology and secondary-spawn mechanics are statically established. Visual/design names for several spawned archetypes remain intentionally provisional.

## Frame-level platform object pipeline

In active platform mode, fixed-bank processing maps PRG bank 3 and calls a sequence including:

1. `$9B93` — special/event object path;
2. `$96B4` — secondary-object spawn/allocation;
3. `$9761` — secondary-object update;
4. `$A442` — common movable-entity update;
5. `$A22C` — projectile/attack update.

This proves that the engine has multiple object classes rather than one homogeneous enemy array.

## Common movable entities

`$A442` explicitly processes two 16-byte records:

| Record | OAM block | Role |
|---|---|---|
| `$03BA-$03C9` | `$0748+` | common movable entity slot A |
| `$03CA-$03D9` | `$077C+` | common movable entity slot B |

These records use the field layout documented in `PLATFORM_ENTITY_AI.md`: state, X/Y, jump/animation phases, facing, terrain probes, type, HP, contact drains and Seventh Sense reward.

The fact that only two records are iterated here is an original hardware/engine capacity decision, not a general REBORN design constraint.

## Secondary spawned objects

Helpers `$9692/$96A3` select two additional logical records:

| Record | OAM block |
|---|---|
| `$03DA+` | `$07B0+` |
| `$03EA+` | `$07B8+` |

`$96B4` attempts to allocate/update these records, and `$9761` advances their on-screen object state.

They are driven by a separate page/substate spawn schedule and must **not** be conflated with the two common movable enemy slots above.

### Spawn timing

Global `$03B6` is used as a spawn delay/countdown. When it expires, it is reseeded from:

`($48 & $1F) + $1F`

which yields roughly **31..62 update units** before the next spawn attempt.

### Spawn coordinates

The secondary spawner derives a vertical coordinate from player Y minus a pseudo-random amount based on `$48`. It rejects results outside the active vertical region.

Horizontal side/direction is selected through the spawned OAM record and auxiliary state, producing objects that can approach/cross the screen independently of the common-goon AI.

### Spawned archetype data

When a secondary object is created, its numeric archetype index selects:

- an initial visual tile from fixed table `$C169`;
- logical type in record offset `$09`;
- HP/resource-drain/reward values copied into offsets `$0C-$0F` from staging bytes `$03B2/$03B1/$03B0/$03B3`.

The five 4-byte archetype records extracted by `tools/reverse/extract_platform_entities.py` therefore describe this **secondary spawn system**, not necessarily the two common humanoid enemy slots.

Current extracted archetypes:

| ID | HP | Cosmo drain ticks | Life drain ticks | Seventh Sense reward | initial visual tile |
|---:|---:|---:|---:|---:|---:|
| 0 | 0 | 0 | 0 | 0 | `$FE` |
| 1 | 30 | 2 | 2 | 32 | `$80` |
| 2 | 30 | 4 | 1 | 32 | `$83` |
| 3 | 0 | 5 | 2 | 0 | `$F4` |
| 4 | 0 | 4 | 3 | 0 | `$DA` |

The zero-HP entries must not automatically be interpreted as harmless: secondary-object code has type-specific behavior and can still damage the player through contact/hazard paths.

## Spawn schedule

Bank 1 `$9C24-$9C47` contains 18 schedule pointers indexed by platform substate `$02`. Each resulting byte sequence selects which secondary archetype is used as the camera/page progresses.

Important nonzero schedules found so far:

- `$02=$0C`: selected pages spawn archetype 3;
- `$02=$0E`: pages alternate archetypes 1 and 2;
- `$02=$10`: all twelve page entries are archetype 4.

Most ordinary main-sequence substates use the zero/default schedule.

## `$02` is a platform substate, not simply a House number

Fixed `$E4D7` normally derives `$02` from story progress `$067D` through table `$E4E0`.

Main mapping starts:

`progress 0..11 -> substate $00..$0B`

but later progress values map nonlinearly:

- progress 12 -> `$10`
- progress 13 -> `$0F`
- progress 14 -> `$11`
- later entries continue `$12,$13,$14`.

In addition, bank-5 event code directly writes special platform substates:

- `$9DBF`: `$02 = $0C`
- `$A468`: `$02 = $0E`
- `$A6E7`: `$02 = $0D`

Therefore `$0C-$0E` are explicit event/special-area modes rather than ordinary sequential House identifiers.

This is a key architectural distinction for REBORN: stage identity, story progress and transient special-area mode must be separate fields.

## Post-Pisces / Palace Roses inference

When story progress increments to 12, fixed `$E4D7` maps it to platform substate `$10`. The secondary schedule for `$10` is archetype 4 on every page.

External walkthrough/TAS evidence independently identifies the playable section immediately after Aphrodite/Pisces and before Saga as the Palace/Temple approach filled with damaging roses.

Combined with archetype 4's full-area schedule, zero reward and animated tile family beginning at `$DA`, the current leading interpretation is:

`secondary archetype 4 = Palace Roses hazard`

Status: **high-confidence INFERRED**, not yet promoted to CONFIRMED solely from static ROM code because the numeric object has not yet been tied to a named in-game sprite/text event by an internal label or runtime trace.

## Special/event object slot

`$9B93` uses state around `$03FA/$03FB` and OAM around `$07E0`. `$03FA` behaves as a spawn/event timer, and this path is structurally separate from both common entities and the two secondary-object records.

Working designation: `special_event_object`.

Its exact roles are still being partitioned by platform substate/type.

## REBORN consequence

The original object topology is best modeled semantically as:

- common enemies (capacity 2 on NES);
- secondary/hazard spawns (capacity 2 on NES);
- special/event object (capacity 1-ish path);
- player attack/projectile slots;
- player;
- OAM/UI/render objects.

The native remake can remove the capacity limits while preserving the separate behavior classes, spawn schedules and event semantics.

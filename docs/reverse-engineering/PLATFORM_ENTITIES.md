# Platform entities — archetypes and spawn scheduling

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: static reconstruction of the ordinary platform spawn path. Numeric archetypes and scheduling are confirmed; visual/narrative names for archetypes 1–4 remain deliberately unresolved.

## Two structures: visual object and logical entity

The platform engine keeps a small visual/object record and a separate logical entity record.

The spawn path writes both:

- visual/object pointer `$12/$13` receives screen position, object type and attributes;
- logical entity pointer `$16/$17` receives type/class plus combat/contact fields.

This separation explains why sprite dimensions and logical hitboxes need not match.

## Spawn selector

Bank 1 `$9BB9+` selects a page-schedule table from `$02` (engine substage/state) through pointer table `$9C24`.

It then uses `$45` as the index into that schedule:

`archetype = schedule_for_substate[$45] & 7`

The current archetype is cached in `$03B4/$03B5`. A spawn/change path proceeds only when the scheduled value changes and relevant object slots are free.

A scheduled archetype of `0` produces no ordinary entity spawn through this path.

## Substate -> schedule pointer

The pointer table at bank 1 `$9C24` is meaningful particularly for substates `$0C-$11`:

| `$02` | Schedule |
|---:|---:|
| `$0C` | `$9C58` |
| `$0D` | `$9C48` |
| `$0E` | `$9C64` |
| `$0F` | `$9C70` |
| `$10` | `$9C73` |
| `$11` | `$9C48` |

Many earlier substates also point to `$9C48`, an all-zero schedule in the stored range used here.

### Observed schedule payloads

The next pointer boundary determines the compact stored length:

- `$9C48` (16 entries): all `0`;
- `$9C58` (12 entries): `0,0,0,3,3,3,0,0,3,0,0,0`;
- `$9C64` (12 entries): `1,1,1,1,2,2,2,1,1,1,2,2`;
- `$9C70` (3 entries): `0,0,0`;
- `$9C73` (12 entries): `4,4,4,4,4,4,4,4,4,4,4,4`.

These are engine archetype IDs, not yet enemy names.

## Archetype combat/contact table — bank 1 `$9C92`

Each archetype occupies exactly four bytes. `$9C92` ends at `$9CA5`; code resumes at `$9CA6`, proving five entries (`0..4`).

Bank 1 loads each record into staging RAM and bank 3 `$973E+` copies them into logical entity offsets:

1. byte 0 -> entity `+0C` = HP;
2. byte 1 -> entity `+0D` = Cosmo-drain ticks inflicted on player;
3. byte 2 -> entity `+0E` = Life-drain ticks inflicted on player;
4. byte 3 -> entity `+0F` = packed-BCD Seventh Sense reward on death.

Table:

| Archetype | HP | Cosmo ticks | Life ticks | Seventh Sense reward |
|---:|---:|---:|---:|---:|
| 0 | 0 | 0 | 0 | 0 |
| 1 | 30 | 2 | 2 | 32 |
| 2 | 30 | 4 | 1 | 32 |
| 3 | 0 | 5 | 2 | 0 |
| 4 | 0 | 4 | 3 | 0 |

Reward bytes for archetypes 1/2 are `$32`, i.e. packed-decimal 32.

Archetypes 1/2 are ordinary damageable enemy-like entities in this table. Archetypes 3/4 have zero HP and no death reward but significant contact drain, so they are **hazard/contact archetypes** at the logical level. Their visual identity must still be correlated before assigning game-world names.

## Visual/object type mapping

During ordinary spawn, bank 3 `$9735+` indexes fixed table `$C169` with the current archetype. Relevant values are:

| Archetype | visual/object type byte |
|---:|---:|
| 0 | `$FE` |
| 1 | `$80` |
| 2 | `$83` |
| 3 | `$F4` |
| 4 | `$DA` |

`$FE` is widely used as inactive/unused in the object system. The other values select type-specific update/render paths.

## Archetype resource pointers

Bank 1 also indexes pointer table `$9C7F` by archetype. Its five entries are:

- type 0 -> `$9C89`
- type 1 -> `$9C89`
- type 2 -> `$9C89`
- type 3 -> `$9C8C`
- type 4 -> `$9C8F`

The three pointed 3-byte sequences are:

- `$9C89`: `$20,$26,$0F`
- `$9C8C`: `$20,$26,$16`
- `$9C8F`: `$29,$16,$06`

Their exact semantic role (animation/resource/script parameters) remains under analysis, but the pointer selection is confirmed.

## Entity creation handoff

The bank-1 spawn planner stages:

- archetype in `$03B4/$03B5`;
- HP in `$03B2`;
- Cosmo ticks in `$03B1`;
- Life ticks in `$03B0`;
- reward in `$03B3`.

Bank 3 `$96E0-$9760` then allocates/materializes the object. `$973E+` writes archetype/class and combat/contact fields into the logical entity record.

This is the first complete bridge from **stage page -> archetype -> stats -> live entity**.

## REBORN consequence

The native data model can cleanly separate:

- spawn schedule (`stage/substate`, page -> archetype);
- archetype definition (HP, resource drain, reward);
- visual presentation;
- logical collision profile;
- behavior controller.

That is substantially cleaner than reproducing the original staging RAM while preserving every gameplay-relevant value.

## Next targets

1. correlate archetype 1–4 visual identities through CHR/object update paths;
2. determine exact page/world coordinate represented by `$45`;
3. extract spawn positioning/randomization in `$96E0+` (`$48`, `$03B6` and player Y influence);
4. map entity type byte at logical `+09` against these archetypes and later transformed states;
5. build a ROM-driven extractor that emits neutral JSON entity schedules without redistributing copyrighted graphics.

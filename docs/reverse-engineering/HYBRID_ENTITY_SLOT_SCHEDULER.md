# Hybrid entity slot scheduler — common + special `$08/$09/$0C`

Status: **clean-room composition of confirmed slot gate, common runtime, special runtime and primary `$A647` retirement ordering**.

The platform engine owns two logical movable-entity records and processes them in strict A -> B order inside the `$A442` family. Earlier work closed the reusable common per-slot runtime and the dedicated active runtime for scheduled special types `$08/$09/$0C`. This composition places both behind the same slot gate without flattening their distinct semantics.

## Ordered frame slice

After the existing pre-player resource phase and post-player `$76` latch update:

```text
slot A activity gate
  -> common runtime OR special 08/09/0C runtime OR skip
  -> if runtime enters confirmed removal path: $A647 retirement
  -> carry shared state
slot B activity gate
  -> common runtime OR special 08/09/0C runtime OR skip
  -> if runtime enters confirmed removal path: $A647 retirement
  -> carry shared state
late attack-object update
shared $3C increment
```

A slot with visual `+1 == $FE` normally skips. Logical `$40` and `$D0` families retain the confirmed activity-gate exceptions and can still run cleanup after visual retirement.

## Route selection

Once a slot is admitted by the activity gate:

- types `$08/$09/$0C` route through `PlatformSpecialEntityActive08090C`;
- currently promoted common types route through `PlatformCommonEntitySlotRuntime`;
- unsupported active types remain explicit failures rather than being coerced into the wrong runtime.

The gate runs **before** route validation. This matters because a visually free ordinary slot can contain a logically inert record that would not be valid input to an active common runtime, yet the original simply skips it.

## State carried A -> B

The following values are threaded from the completed slot A update into slot B:

```text
attack-object state
$76 contact latch
$7F/$80 pending Life/Cosmo drain state
Seventh Sense accumulator
global $039A special-entity trigger counter
```

This preserves same-frame causality. Examples covered by fixtures:

1. a common slot A consumes a projectile, so special slot B receives the retired attack state;
2. a special slot A seeds `$76` and its drain profile, so common slot B cannot overwrite them in the same frame;
3. two special slots share `$039A` in A -> B order, so A can advance `$7E -> $7F` and B can immediately observe `$7F`, reach the `$80` threshold, and reset the counter.

## Special per-slot state

The hybrid slot wrapper retains the special-only values needed by `$08/$09/$0C` without forcing them into the generic common entity record:

- logical `+$04` control byte;
- attached `$A908/$AA70` hazard record;
- parent logical `+$08` side effect.

Common slots carry these fields inertly.

## Primary visual retirement `$A647`

The minimum visual lifecycle required for deterministic cross-frame primary-slot composition is now promoted.

When an already-modeled common or `$08/$09/$0C` path reaches the original shared removal helper `$A647`, the hybrid scheduler applies `PlatformEntityRemovalA647` after the logical runtime result:

- tracked primary visual `+1` becomes `$FE`;
- engine `$00 < $30` clears logical action `+$00`;
- engine `$00 >= $30` preserves the logical action;
- the helper's exact eleven-base-record retirement and type-`$0D` extra-record behavior are recorded as evidence metadata.

This directly explains the `$A459` exception: a visual-free `$40/$D0` logical family can be a legitimate post-removal state when `$00 >= $30`.

The full renderer-owned sprite array is still not mirrored in the hybrid state. The runtime intentionally tracks the occupancy byte required by slot gating and spawning, while the raw `$F0/$FE` writes across the eleven records are documented separately in `PRIMARY_ENTITY_REMOVAL_A647.md`.

## Cross-frame closure

The result state of one hybrid frame can now be supplied directly to the next frame without manually repairing the primary visual marker.

Fixtures cover both threshold branches and a two-frame path where:

1. frame N retires the visual slot while preserving `$40` at engine `$30`;
2. frame N+1 sees `visual=$FE + logical=$40`;
3. `$A459` admits the slot;
4. reaction cleanup advances once camera correction brings it back into the active range.

A separate fixture passes an `$A647`-freed slot directly to the confirmed scheduled producer and verifies `$FE -> $FD` reuse.

## Remaining visual-state boundary

This does **not** promote arbitrary renderer animation writes or every visual record in the engine. The current closure is narrower and evidence-driven:

- primary slot occupancy/free transition is owned;
- primary logical clear threshold is owned;
- attached `$A908/$AA70` visual state remains independently modeled;
- other entity classes retain their own visual-lifecycle models or open gaps.

## Next integration boundary

With primary retirement no longer requiring manual repair, the next useful composition is the persistent producer -> hybrid scheduler bridge: feed `PlatformPrimaryEncounterRefreshAndSpawn` / `PlatformPageEncounterSpawnState` outputs into the hybrid A -> B runtime and carry the resulting slot state forward across frames.

That bridge must preserve the already-confirmed timing boundaries between encounter acceptance, producer execution, entity updates and any NMI-owned rendering work. It should not collapse those phases merely because they now share a state record.

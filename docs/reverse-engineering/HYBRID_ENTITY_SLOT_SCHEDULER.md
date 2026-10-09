# Hybrid entity slot scheduler — common + special `$08/$09/$0C`

Status: **clean-room composition of already-confirmed slot gate, common runtime and special runtime ordering**.

The platform engine owns two logical movable-entity records and processes them in strict A -> B order inside the `$A442` family. Earlier work closed the reusable common per-slot runtime and the dedicated active runtime for scheduled special types `$08/$09/$0C`. This composition places both behind the same slot gate without flattening their distinct semantics.

## Ordered frame slice

After the existing pre-player resource phase and post-player `$76` latch update:

```text
slot A activity gate
  -> common runtime OR special 08/09/0C runtime OR skip
  -> carry shared state
slot B activity gate
  -> common runtime OR special 08/09/0C runtime OR skip
  -> carry shared state
late attack-object update
shared $3C increment
```

A slot with visual `+1 == $FE` normally skips. Logical `$40` and `$D0` families retain the previously confirmed activity-gate exceptions and can still run cleanup even after visual retirement.

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

## Visual-state boundary

`VisualSpritePlus1` is used as the activity-gate input but is intentionally carried unchanged by this slice. The exact renderer-side writes that retire or replace the visual record have not yet been promoted into the composed frame runtime. Inventing those writes here would make the scheduler appear more complete than the current evidence supports.

Therefore this class closes **logical A -> B scheduling and shared combat state propagation**, not the complete visual lifecycle.

## Next integration boundary

The next useful composition is to bridge the already-closed producer state (`PlatformPrimaryEncounterRefreshAndSpawn` / `PlatformPageEncounterSpawnState`) into this hybrid logical scheduler while keeping visual occupancy and special-only per-slot state explicit. That bridge should not infer visual retirement; it should either receive the renderer-owned visual state or first promote the exact visual lifecycle writes required for deterministic multi-frame composition.

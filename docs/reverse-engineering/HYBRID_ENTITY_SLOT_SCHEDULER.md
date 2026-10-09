# Hybrid entity slot scheduler — common and scheduled-special routes

Status: **clean-room composition of confirmed slot gate, promoted per-type runtimes, shared state carry and primary `$A647` retirement ordering**.

The platform engine owns two primary logical movable-entity records and processes them in strict A -> B order inside the `$A442` family. The hybrid scheduler preserves that order while routing each admitted record into the semantic runtime proven for its type.

## Ordered frame slice

After the existing pre-player resource phase and post-player `$76` latch update:

```text
slot A activity gate
  -> common OR $08/$09/$0C OR $0D/$0E runtime OR skip
  -> confirmed $A647 retirement when required
  -> carry shared state
slot B activity gate
  -> common OR $08/$09/$0C OR $0D/$0E runtime OR skip
  -> confirmed $A647 retirement when required
  -> carry shared state
late attack-object update
shared $3C increment
```

A slot with visual `+1 == $FE` normally skips. Logical `$40` and `$D0` families retain the confirmed activity-gate exceptions and can continue cleanup after visual retirement.

## Route selection

Once the common activity gate admits a slot:

- types `$08/$09/$0C` route through `PlatformSpecialEntityActive08090C`;
- types `$0D/$0E` route through `PlatformSpecialEntityActive0D0E`;
- the promoted common type set routes through `PlatformCommonEntitySlotRuntime`;
- unsupported active types remain explicit failures rather than being coerced into a semantically wrong runtime.

The `$0D/$0E` route is deliberately separate. Direct ROM dispatch sends those types from `$A48B/$A48F` to `$A4BF -> $A55E`, bypassing the `$08/$09/$0C` `$A4A7-$A55B` pre-dispatch. They therefore share later entity processing but do not inherit its global `$039A` control behavior.

The gate runs **before** route validation. A visually free ordinary slot can therefore contain inert logical bytes that would be invalid input to an active runtime and still skip exactly as the original does.

## State carried A -> B

The scheduler threads the completed slot A state into slot B in original order:

```text
attack-object state
$76 contact latch
$7F/$80 pending Life/Cosmo drain state
Seventh Sense accumulator
global $039A special-entity trigger counter
```

The `$08/$09/$0C` runtime can mutate `$039A`; the `$0D/$0E` runtime cannot and passes it through unchanged. This distinction is part of the route contract rather than a property of the generic scheduler.

Regression coverage includes:

1. common slot A projectile consumption affecting special slot B;
2. special slot A contact seeding `$76/$7F/$80` before common slot B;
3. two `$08/$09/$0C` slots observing shared `$039A` in A -> B order;
4. `$0D/$0E` frames preserving `$039A` while still carrying attack/contact/Seventh-Sense mutations.

## Rich per-slot state

`PlatformHybridEntitySlotState` carries:

- primary logical entity record;
- tracked primary visual occupancy byte `+1`;
- logical `+$04` control byte;
- attached `$A908/$AA70` hazard record;
- parent logical `+$08` side effect.

This richer shape allows scheduled producers and entity runtimes to share one persistent state without reconstructing special-only fields between frames.

## Primary visual retirement `$A647`

When a promoted route reaches a confirmed removal path, the scheduler composes `PlatformEntityRemovalA647` after the logical runtime result:

- tracked primary visual `+1` becomes `$FE`;
- engine `$00 < $30` clears logical action `+$00`;
- engine `$00 >= $30` preserves logical action;
- eleven base visual records are retired by the original helper;
- type `$0D` receives the confirmed extra `+$2C/+2D` visual retirement.

The type-$0D` `$A0-$AF` terminal path now reaches this same lifecycle through its dedicated runtime.

The full renderer-owned sprite array is still not mirrored in hybrid state. The scheduler tracks the evidence-backed occupancy/lifecycle state required for gating, removal and producer reuse.

## Cross-frame closure

Hybrid output can feed the following frame directly. Proven paths include:

- common removal -> next-frame free-slot skip;
- visual-free `$40` cleanup continuation when engine `$00 >= $30`;
- removal -> later producer reuse `$FE -> $FD`;
- scheduled `$0D` `$AF -> $B0 -> $A647` -> next-frame free-slot skip.

The persistent frame bridge already composes NMI encounter acceptance, split producers, the platform exit gate and this hybrid A -> B scheduler. See `PERSISTENT_PRIMARY_ENTITY_FRAME.md`.

## Remaining boundaries

- Type `$0F` follows its own `$A74C` dispatcher route and is not yet part of the hybrid active runtime.
- Other unsupported active families remain explicit rather than approximated.
- Arbitrary renderer animation/tile/Y/attribute/X writes remain outside this logical/occupancy composition unless independently promoted from evidence.

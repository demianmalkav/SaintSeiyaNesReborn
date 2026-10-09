# Primary encounter refresh → acceptance → producer composition

Status: **CONFIRMED by composition of already closed static ROM paths**.

The earlier clean-room layers intentionally modeled three pieces separately:

1. fixed-bank refresh gate `$D7F2` plus bank-1 `$9915` entry gating;
2. safe acceptance latch `$996C-$9A2D` for staged `$03B7` versus active `$58`;
3. common/scheduled primary-encounter producers.

This document closes the semantic gap between them.

## The important distinction

The page selected by camera high byte `$45` is **not necessarily the encounter currently active in `$58`**.

When the camera reaches a page with a new primary encounter descriptor, `$996C` stages that descriptor in `$03B7`. If either common entity slot is unsafe for replacement, acceptance is deferred:

```text
current page descriptor = NEW
$03B7                  = NEW
$58/profile            = OLD
```

The producers must therefore consume the profile attached to accepted `$58`, not simply rebuild their configuration from the current page every frame.

This distinction is now explicit in `PlatformPageEncounterSpawnPhase.StepAccepted(...)`.

## Composed clean-room state

`PlatformPrimaryEncounterRefreshAndSpawnState` keeps the three persistent views separate:

- `EncounterLatch`: active `$58` plus its resolved spawn profile;
- `StagedDescriptor03B7`: newest descriptor staged by `$996C`;
- `SpawnState`: the two common records/visual markers plus producer timers/latches.

They may legitimately disagree while a page change is pending.

## Per-update composition

`PlatformPrimaryEncounterRefreshAndSpawn.Step(...)` performs:

```text
PlatformPrimaryEncounterRefreshGate
  ↓ when $996C is actually invoked
PlatformPrimaryEncounterAcceptance
  ↓ accepted ActiveConfig (old config survives deferral)
PlatformPageEncounterSpawnPhase.StepAccepted
```

If the refresh gate does not invoke `$996C`, both active `$58/profile` and staged `$03B7` remain unchanged for this composition.

If `$996C` is invoked, the current page descriptor is staged even when acceptance is deferred.

## Deferred replacement behavior

A discriminating fixture uses:

```text
old accepted $58 = $86   (generic common producer)
new page          = $A8   (scheduled type $08)
slot A family     = $40   (unsafe replacement)
```

Result:

```text
$03B7 = $A8
$58   = $86
producer route = generic B6D0 using config $86
```

Because generic `$B6D0` also compares staged `$03B7` against active `$58`, that producer then reports `SpawnGateMismatch`. This preserves both pieces of original state instead of collapsing the frame into either the old or new page semantics.

## Safe replacement behavior

With both common slots safe, the same change accepts `$A8` immediately:

```text
$03B7 = $A8
$58   = $A8
active profile = newly resolved type08 profile
producer route = scheduled $8925
```

The scheduled producer can therefore consume the newly accepted encounter configuration in the same composed phase.

## Refresh suppression

The fixed/bank-1 refresh gate can suppress `$996C` entirely. In that case the model does **not** silently stage the current page descriptor. The prior latch and prior `$03B7` survive, and producers continue from the prior accepted configuration.

This is distinct from `DeferredUnsafe`, where `$996C` did run and `$03B7` was updated.

## Accepted zero

A safely accepted zero page descriptor produces:

```text
$03B7 = $00
$58   = $00
ActiveConfig = null
```

`StepAccepted(...)` then suppresses both primary producer routes while preserving unrelated producer state.

## Containment

Valid game flow keeps `$45` inside the current stage page table. The clean-room composition explicitly contains malformed/out-of-range page inputs rather than reading unrelated memory and inventing an encounter descriptor.

## Consequence for REBORN

The modern runtime should retain this separation even if the presentation layer eventually hides it:

```text
visible page / camera context
staged encounter descriptor
accepted active encounter
spawn producer state
```

Keeping those concepts distinct is necessary for frame parity at page boundaries and gives REBORN a clean place to modernize encounter streaming later without corrupting ORIGINAL SPEC behavior.

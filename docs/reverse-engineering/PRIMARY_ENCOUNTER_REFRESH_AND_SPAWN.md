# Primary encounter NMI → main-thread producer bridge

Status: **CONFIRMED by composition of already closed static ROM paths**.

The platform encounter runtime is split across two execution contexts that must remain separate in the clean-room model:

1. NMI-side refresh/acceptance (`$D269 -> $D7F2 -> bank-1 $9915/$996C`);
2. main-thread producer order (bank-0 `$B6D0`, then bank-1 `$8927`).

`PlatformPrimaryEncounterRefreshAndSpawn` is a state bridge between those contexts, not a new unordered monolithic frame step.

## Persistent state carried across the boundary

`PlatformPrimaryEncounterRefreshAndSpawnState` keeps three independent views:

- `EncounterLatch`: active `$58` plus the profile already accepted for it;
- `StagedDescriptor03B7`: latest page descriptor actually staged by `$996C`;
- `SpawnState`: logical/visual common-slot state plus `$03B8` cooldown and `$03A2` scheduled trigger latch.

They can legitimately disagree during a deferred transition.

## NMI phase

`StepNmi(...)` delegates to `PlatformNmiPrimaryEncounterRefreshPhase`.

If the refresh gate reaches `$996C`, the current page descriptor is staged. Safe replacement updates active `$58/profile`; unsafe replacement keeps the old active encounter while still updating staged `$03B7`.

If the refresh gate suppresses `$996C`, the bridge deliberately preserves the previous staged `$03B7`. It does not infer or copy the visible page descriptor.

Example deferred transition:

```text
visible page descriptor = $A8
old active $58          = $86
slot A family           = $40

NMI result:
$03B7 = $A8
$58   = $86
```

## Main-thread producer phase

`StepMainThread(...)` delegates to `PlatformLatchedCommonProducerPhase` using exactly the state produced by the prior NMI boundary.

Confirmed order remains:

```text
bank 0 $B6D0
  ↓
bank 1 $8927
  ↓
platform damage
  ↓
player $AAE4
```

The bridge does not collapse the two producer routes into an either/or router.

### Generic `$B6D0`

Receives both active `$58/profile` and staged `$03B7`.

Therefore a deferred page change can block the generic producer through the original mismatch gate:

```text
$03B7 != 0 && $03B7 != $58
```

### Scheduled `$8927`

Consumes active `$58/profile` but has no invented `$03B7 == $58` requirement.

Consequently an older special encounter can remain scheduled-active while a newer page descriptor is staged but still unsafe to accept.

## Discriminating composed cases

The bridge self-tests cover:

1. **Deferred common/special change**: NMI stages the new descriptor while old `$58` survives; main-thread `$B6D0` sees the mismatch.
2. **Safe special acceptance**: NMI accepts `$A8`; main-thread `$B6D0` excludes the scheduled type normally and later `$8927` spawns it.
3. **Refresh suppression**: NMI does not touch active or staged encounter state; the previous main-thread producer continues.
4. **Deferred old special**: `$B6D0` is mismatch-blocked while `$8927` can still evaluate the old active special encounter.
5. **Accepted zero**: NMI clears active `$58/profile`; the later main-thread producer phase is a contained no-op.

## Why two methods instead of one

`StepNmi(...)` and `StepMainThread(...)` are intentionally separate. A higher-level scheduler can place the NMI boundary at the correct point relative to main-thread execution without losing which state existed in which context.

This matters for frame parity and prevents a subtle class of bugs where the clean-room remake lets the current page descriptor become active too early simply because both operations were placed in one convenient method.

## Consequence for REBORN

The modern runtime should preserve these logical layers even if its eventual renderer and encounter streaming are redesigned:

```text
visible page / camera context
staged page encounter ($03B7 semantics)
accepted active encounter ($58/profile semantics)
producer state
NMI boundary
main-thread boundary
```

That separation gives ORIGINAL SPEC deterministic parity and gives REBORN a safe place to replace the NES scheduling machinery later without changing game rules accidentally.

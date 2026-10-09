# Persistent primary-entity frame bridge

Status: **clean-room composition of already-confirmed NMI encounter state, primary producers, hybrid entity scheduling and primary removal occupancy**.

This layer closes the state hand-off that previously required translating `PlatformPageEncounterSpawnState` into `PlatformHybridEntitySlotState` outside the runtime.

It does **not** claim to be the complete platform frame. In particular, fixed-bank platform exit evaluation still lies between the early common producer and later bank-1/player work. The composed main-thread method is therefore named `StepMainThreadNonExit`: it represents the already-confirmed normal path after that diversion is known not to end the frame.

## Persistent state

`PlatformPersistentPrimaryEntityFrameState` owns:

```text
accepted encounter latch ($58 + profile)
staged encounter descriptor $03B7
primary hybrid slot A
primary hybrid slot B
common-producer cooldown $03B8
scheduled-trigger low latch $03A2
Seventh Sense accumulator
global special counter $039A
frame counter $3C
```

Each hybrid slot already carries:

```text
primary logical entity record
tracked primary visual occupancy byte (+1)
logical special-control byte +$04
attached $A908/$AA70 hazard record
logical parent byte +$08
```

Thus producer output and entity output now land in the same persistent slot representation.

## NMI remains a distinct boundary

`StepNmi(...)` delegates to `PlatformPrimaryEncounterRefreshAndSpawn.StepNmi(...)` and updates only the encounter latch / staged descriptor fields owned by the NMI-side acceptance phase.

It does not execute producers or entity simulation.

This preserves the existing distinction:

```text
NMI: visible page -> staged $03B7 / accepted $58+profile

later main thread: accepted/staged state -> producers -> simulation
```

A fixture verifies that a newly accepted special encounter leaves the primary slots free at the NMI return and is consumed only by the later main-thread call.

## Normal main-thread composition

For a platform frame already known to continue past the earlier exit diversion, `StepMainThreadNonExit(...)` composes:

```text
$B6D0 generic primary producer
    ->
$8927 scheduled-special producer
    ->
pre-player resource phases
    ->
player/action processing
    ->
hybrid primary entity A
    ->
hybrid primary entity B
    ->
late attack-object phase
    ->
shared $3C advance
```

The first two stages remain delegated to `PlatformPrimaryEncounterRefreshAndSpawn.StepMainThread(...)`; the later simulation remains delegated to `PlatformHybridEntityCombatSlice.StepNonFatal(...)`. The bridge owns only persistent state reconciliation between those closed components.

## Producer write-map reconciliation

`PlatformPageEncounterSpawnState` contains the primary logical records and primary visual occupancy, but it predates the independently modeled special-only fields `+$04`, `+$08` and attached-hazard state.

The bridge therefore reconciles successful replacement using direct producer write evidence instead of zeroing the richer slot wholesale.

### Generic producer `$B6D0`

The canonical ROM write block at bank-0 `$B7BB+` performs, among the already-modeled spawn writes:

```text
logical +$00 = $10
logical +$03 = $00
logical +$04 = $00
logical +$06 = decision timer
logical +$09 = type
logical +$0C..+$0F = profile
visual  +$01 = $FD
```

It does not write logical `+$08`.

### Scheduled producer `$8927`

The canonical ROM write block at bank-1 `$898B-$89D1` performs:

```text
logical +$00 = $00
logical +$01 = $F8
visual  +$01 = $FD
logical +$02 = scheduled Y
logical +$03 = $00
logical +$04 = $00
logical +$07 = $01
logical +$09 = type
logical +$0C..+$0F = profile
```

The increment sequence skips logical `+$08`; it is not overwritten by this producer.

### Resulting bridge rule

On successful spawn by either producer:

- primary `Entity` and visual occupancy come from the producer result;
- `SpecialControl04` is reset to `$00`;
- `ParentOffset08` is preserved;
- `AttachedHazard` is preserved.

The last rule is ownership-based rather than guessed cleanup: the attached `$A908/$AA70` record is a separate raw visual/hazard record and neither primary producer write block owns it. Its own spawn/contact/deactivation routines remain responsible for its mutations.

When no spawn occurs, richer special-only fields are preserved while any producer mutations that did occur to the primary entity record (for example a partial common ground-search result) still propagate through the final producer state.

## Producer-before-entity consequence

On the normal active path, producer mutation is visible to later player/entity processing in the same composed main-thread call.

The scheduled producer creates its entity at `X=$F8`; when the current camera correction is nonzero the later special runtime can observe the newly created record at the corrected in-range X. Fixtures use such a confirmed non-exit scrolling case rather than inventing an unconditional one-frame defer.

If player/action processing exits before the entity pipeline, producer output still persists because those producers ran earlier. The bridge therefore falls back to the post-producer slots when `PlatformHybridEntityCombatSlice` returns no slot results.

## Multi-frame closure

Fixtures cover:

1. NMI acceptance followed by deferred main-thread producer consumption;
2. scheduled-special replacement resetting `+$04` while preserving `+$08` and the external attached-hazard record;
3. generic replacement applying the same `+$04` reset / `+$08` preservation rule;
4. frame N `$A647` retirement feeding frame N+1 `$B6D0` slot reuse directly, with no manual `visual=$FE` repair.

The last case closes the current persistent loop:

```text
producer spawn
 -> hybrid entity simulation
 -> A647 removal/occupancy retirement
 -> persistent state
 -> later producer slot reuse
```

## Open boundaries

- The fixed-bank platform-exit gate is not folded into this bridge. A later full-frame scheduler must preserve its exact position between early `$B6D0` and later bank-1/player work.
- Scheduled types `$0D/$0E` can be produced by `$8927`, but the current hybrid active runtime only promotes special `$08/$09/$0C`; those families remain explicit future integration work.
- Full renderer-owned sprite animation state remains outside this persistent logical/occupancy layer except for already-promoted lifecycle writes.

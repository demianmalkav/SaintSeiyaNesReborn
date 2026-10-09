# Persistent primary-entity frame bridge

Status: **clean-room composition of confirmed NMI encounter state, split primary producers, platform-exit gate, hybrid entity scheduling and primary removal occupancy**.

This layer owns the persistent hand-off that previously required translating `PlatformPageEncounterSpawnState` into `PlatformHybridEntitySlotState` outside the runtime.

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

Each hybrid slot carries the primary logical record, tracked primary visual occupancy `+1`, special-control `+$04`, attached `$A908/$AA70` hazard state and parent byte `+$08`.

## NMI remains a distinct boundary

`StepNmi(...)` delegates to `PlatformPrimaryEncounterRefreshAndSpawn.StepNmi(...)` and updates only the NMI-side encounter latch / staged descriptor state.

It does not execute producers or entity simulation:

```text
NMI: visible page -> staged $03B7 / accepted $58+profile

later main thread: accepted/staged state -> producer/exit/simulation path
```

## Exact main-thread split around the exit gate

Direct static inspection of the canonical Japanese ROM confirms the call sequence in the fixed bank around `$C300`:

```text
$C30A  JSR $B6D0   ; generic common producer
$C30D  LDA #$01
$C30F  JSR $C035   ; select bank 1
$C312  JSR $969D   ; platform exit gate
...
$C319  JSR $8000   ; continuing path only
```

Bank-1 `$8000` then contains:

```text
$8000  JSR $9211
$8003  JSR $926C
$8006  JSR $8712
$8009  JSR $8841
$800C  JSR $8927   ; scheduled-special producer
...
```

Therefore the producer order is not simply `$B6D0 -> $8927` with an exit check elsewhere. The evidence-backed boundary is:

```text
$B6D0 generic producer
    ->
$969D exit gate
    -> if accepted: engine transition, no $8000/$8927 path
    -> if rejected: bank-1 $8000, including $8927, then later player/entities
```

`PlatformLatchedCommonProducerPhase` now exposes this with `StepCommon(...)` and `StepScheduled(...)`. Its legacy `Step(...)` remains the composition for callers already known to be on the non-exit path.

## Full persistent main-thread API

`PlatformPersistentPrimaryEntityFrame.StepMainThread(...)` now executes:

```text
$B6D0 generic primary producer
    ->
PlatformExitGate.Evaluate(...)
    -> accepted: return State3DReload or State70Special
    -> rejected: $8927 scheduled producer
                 -> pre-player resources
                 -> player/action processing
                 -> hybrid primary entity A
                 -> hybrid primary entity B
                 -> late attack-object phase
                 -> shared $3C advance
```

The result type distinguishes:

- `Continued`
- `State3DReload`
- `State70Special`

An accepted exit preserves every state mutation already produced before `$969D`, especially `$B6D0` slot/cooldown changes, but deliberately leaves the scheduled trigger latch and `$3C` untouched because `$8927` and later frame phases did not execute.

The semantic runtime surfaces the transition kind only. The original normal `$3D` path also snapshots Saint state, changes `$00/$01`, disables PPU/NMI, resets the stack and enters `$E100`; the `$11` path enters distinct `$70` plumbing. Those NES engine-transition mechanics remain outside this persistent logical/entity state rather than being faked here.

`StepMainThreadNonExit(...)` remains as a compatibility entry point. It uses the same split core but skips exit-gate evaluation, allowing regression comparison with work that had already established a non-exit frame externally.

## Exit predicates reused, not duplicated

The bridge delegates directly to `PlatformExitGate` for:

- `$49 == 0` jump-phase requirement;
- common `$00-$0B` gate at `X >= $D0`, `Y == $40`;
- special `$0C-$11` coordinate table;
- the `$10` Shun rejection;
- normal `$3D` versus `$11` special `$70` transition distinction.

No second copy of those predicates exists in the scheduler.

## Producer write-map reconciliation

`PlatformPageEncounterSpawnState` contains the primary logical records and primary visual occupancy, but the richer hybrid slot also owns `+$04`, `+$08` and attached-hazard state.

Direct producer write evidence remains the reconciliation rule.

### Generic producer `$B6D0`

The canonical ROM write block at bank-0 `$B7BB+` writes, among the modeled fields:

```text
logical +$00 = $10
logical +$03 = $00
logical +$04 = $00
logical +$06 = decision timer
logical +$09 = type
logical +$0C..+$0F = profile
visual  +$01 = $FD
```

It skips logical `+$08`.

### Scheduled producer `$8927`

The bank-1 `$898B-$89D1` write block writes:

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

It also skips logical `+$08`.

### Resulting bridge rule

On successful spawn by either producer:

- primary `Entity` and visual occupancy come from the producer result;
- `SpecialControl04` resets to `$00`;
- `ParentOffset08` is preserved;
- `AttachedHazard` is preserved because neither primary producer owns the separate `$A908/$AA70` record.

## Multi-frame and exit-path fixtures

Regression fixtures now cover:

1. NMI acceptance followed by deferred main-thread producer consumption;
2. scheduled-special and generic replacement semantics for `+$04`, `+$08` and attached hazard;
3. frame N `$A647` retirement feeding later producer reuse without manual visual repair;
4. a normal `$00-$0B` exit after an early `$B6D0` spawn, proving that the spawn persists while `$8927`/player/entities/`$3C` do not execute;
5. the `$10` Shun exception continuing into `$8927` and later frame simulation;
6. the `$11` gate returning the distinct `$70` transition and suppressing later work;
7. non-exit equivalence between the full entry point and `StepMainThreadNonExit(...)`.

## Remaining boundaries

- Scheduled types `$0D/$0E` can be produced by `$8927`, but the current hybrid active runtime only promotes special `$08/$09/$0C`; those families remain explicit future integration work.
- Full renderer-owned sprite animation state remains outside this persistent logical/occupancy layer except for already-promoted lifecycle writes.
- The semantic `$3D/$70` result does not yet model higher-level native destination/state-machine behavior after leaving the platform area; that belongs above the entity-frame layer.

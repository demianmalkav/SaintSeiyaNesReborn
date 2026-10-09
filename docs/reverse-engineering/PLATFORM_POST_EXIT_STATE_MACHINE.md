# Platform post-exit engine state machine

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: **CONFIRMED by static ROM control flow** for the immediate logical handoff after accepted platform exits and for the special `$70->$80` state sequence described here.

This document begins where `PLATFORM_EXIT_GATES.md` ends. The exit-gate document answers **when** platform mode accepts a completion condition. This document answers **what logical engine-state transition happens immediately afterward**.

The clean-room model is `PlatformPostExitStateMachine`.

## Scope boundary

Included because they affect logical engine progression:

- engine state `$00`;
- NMI/main mirror `$01`;
- substate `$03` where the special sequence clears it;
- mode byte `$04` on the normal reload entry;
- `$26/$27` special-sequence scratch/countdown fields;
- `$57` transition timer;
- confirmed player fields `$3F/$40/$42/$4D/$4E` required by the `$70-$75` sequence.

Intentionally not emulated here:

- PPU register writes and rendering enable/disable;
- stack reset mechanics;
- sound helper internals;
- text tile upload/cursor pacing inside the `$73` NMI stream;
- the stage/narrative destination ultimately selected by the broad `$E100` reload routine.

Those mechanisms remain evidence for control flow without being reproduced as fake logical state.

## Normal platform exit: `$3D` reload

Bank 1 `$96CB-$96E8` executes after a normal platform gate succeeds:

```text
$96CB  LDA #$00
$96CD  STA $04
$96CF  JSR $951F       ; snapshot all five Saint records
$96D2  LDA #$3D
$96D4  STA $00
$96D6  STA $01
...
$96E8  JMP $E100
```

The omitted instructions disable rendering/NMI, call the transition/sound helper and reset the stack. They are real NES mechanics but do not add another semantic state transition before `$E100`.

The immediate clean-room result is therefore:

```text
$04 = $00
snapshot Saints requested
$00 = $3D
$01 = $3D
handoff = $E100 reload
```

The fixed main dispatcher independently confirms the handoff:

```text
$C21E  LDA $00
$C220  STA $01
$C222  CMP #$3D
$C224  BNE ...
$C226  JMP $E100
```

The NMI dispatcher likewise detects mirror state `$01=$3D` and jumps to `$E000`.

### Why `$E100` is not flattened here

`$E100` is a broad engine reload path with many conditionals over persistent globals. It eventually selects later state/substate values, but that destination is not a single universal consequence of `State3DReload`.

Therefore `PlatformPostExitStateMachine` exposes the confirmed `$3D -> $E100` handoff and deliberately stops before stage/narrative destination selection. That destination is a separate reverse-engineering problem.

## Special substate `$11`: entry into `$70`

The `$11` platform exit uses a distinct path at `$96FD-$9713`:

```text
$96FD  LDA #$70
$96FF  STA $00
$9701  LDA #$00
$9703  STA $26
$9705  STA $27
$9707  LDA #$C0
$9709  STA $57
...
$9713  RTS
```

Important distinction: this path does **not** write `$01=$70` itself. The global main loop later mirrors `$00->$01` at `$C21E-$C220`.

Immediate semantic entry:

```text
$00 = $70
$26 = $00
$27 = $00
$57 = $C0
$01 unchanged until main-thread mirror
```

No `$951F` Saint snapshot belongs to this branch.

## Ownership split: main thread vs NMI

The `$70-$75` transition is not a single synchronous routine. Progression is intentionally split across the global main dispatcher and NMI dispatcher.

The important ordering rule is that `$01` is generally the NMI-visible mirror of the state last copied by the main thread. Several transitions increment only `$00`, leaving `$01` on the previous state until the next main iteration.

That asymmetry is preserved in the clean-room model because it determines which NMI handler runs between transitions.

## State `$70`: NMI-owned countdown

Global NMI dispatch recognizes `$00=$70` and calls fixed `$D3BF`.

At `$D3BF`:

- odd `$3C`: countdown is skipped;
- even `$3C`: `$57` decrements;
- when `$57` becomes zero, helper `$D73B` runs;
- `$D73B` ends with `INC $00`, producing `$70->$71`;
- the caller then seeds `$57=$80` and confirmed player setup fields.

The relevant final logical values after completion are:

```text
$00 = $71
$01 = $70      ; still old NMI mirror
$57 = $80
$3F = $00
$40 = $80
$42 = $40
```

The next main iteration mirrors `$00->$01`, allowing the sequence to proceed with state `$71` coherently.

## State `$71`: main-thread countdown

Fixed handler `$C538` owns `$71`:

```text
DEC $57
BNE continue
INC $00        ; $71 -> $72
```

At handler entry, the global dispatcher has already copied `$00->$01`. Therefore the zero-crossing frame ends logically as:

```text
$00 = $72
$01 = $71
$57 = $00
```

## State `$72`: scripted horizontal movement

The `$72` branch of `$C538`:

1. clears `$03`;
2. clears `$57`;
3. increments player X `$3F` once per main update;
4. continues while `$3F < $65`;
5. at `$3F >= $65`, writes `$4D=$20` and `$4E=$20`;
6. writes `$57=$03`;
7. increments `$00`, producing `$73`.

Thus the threshold frame ends:

```text
$00 = $73
$01 = $72
$03 = $00
$3F = $65 or higher
$4D = $20
$4E = $20
$57 = $03
```

## State `$73`: NMI text-stream gate

Main-thread `$C538` has no advancing body for state `$73`; it effectively waits.

NMI dispatch recognizes `$73` and calls bank-1 `$8C19`, which owns the text/tile stream. Exact PPU/cursor pacing is outside the semantic model. The important logical terminal is the `$FF` script terminator at `$8DDB`:

```text
INC $00
INC $01        ; $73 -> $74 on both
LDA #$80
STA $57
STA $26
STA $27
```

The model therefore represents `$73` as an explicit NMI-owned wait with one external semantic input: **has the confirmed text stream reached its `$FF` terminator?**

Before terminator: state remains `$73`.

At terminator:

```text
$00 = $74
$01 = $74
$57 = $80
$26 = $80
$27 = $80
```

This avoids pretending that text renderer internals are part of the gameplay state machine while preserving the real transition condition.

## State `$74`: main-thread countdown

`$C538` decrements `$57`. On zero it increments both state bytes:

```text
$00 = $75
$01 = $75
$57 = $00
```

Unlike `$71`, the `$74` completion explicitly advances both state bytes in the handler.

## State `$75`: delay and handoff to `$80`

The `$75` branch increments `$57` each main update. Rendering branches change as the counter crosses lower thresholds, but the logical state remains `$75` until the incremented timer reaches `$60`.

At `$57 >= $60`:

```text
$00 = $80
$57 = $20
RTS
```

Because the global main loop mirrored `$00->$01` before entering `$C538`, that transition frame leaves:

```text
$00 = $80
$01 = $75
$57 = $20
```

The destination semantics of state `$80` are a separate boundary and are **not** inferred here.

## Complete promoted special sequence

The logical sequence now closed is:

```text
accepted substate-$11 platform exit
  -> $70  ($57=$C0)
  -> NMI even-frame countdown
  -> $71  ($57=$80)
  -> main countdown
  -> $72  (scripted X movement)
  -> $73  (NMI text stream)
  -> $74  ($57=$80)
  -> main countdown
  -> $75  (main delay)
  -> $80  ($57=$20)
```

Thread ownership matters:

```text
$70 : NMI
$71 : main
$72 : main
$73 : NMI text terminator
$74 : main
$75 : main
```

## Clean-room representation

`PlatformPostExitStateMachine` provides:

- `ApplyAcceptedExit(...)` for the immediate `$3D` vs `$70` seed;
- `MainDispatchesToReloadE100(...)` and `NmiDispatchesToReloadE000(...)` for the confirmed normal reload boundary;
- `StepSpecialNmi(...)` for promoted NMI-owned `$70` and `$73` transitions;
- `StepSpecialMain(...)` for main-owned `$70-$75` dispatch behavior.

The class deliberately rejects unsupported states instead of extrapolating beyond the evidence. The next layer may investigate the generic `$E100` destination selector and/or the state `$80` destination reached by the special sequence, but neither is part of this checkpoint.

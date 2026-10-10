# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

Technical subsystem documents remain authoritative for evidence and semantics. This file records the accepted checkpoint, open boundaries and one executable `NEXT`.

## CURRENT

- Phase: `ORIGINAL SPEC / engine state family $60-$6F`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#111` — reachable engine-state family `$11-$14` from promoted reload `$10`.
- Merge commit: `7fa4d5c4ffc06c843bbb7e78a5e9fb3d1bfa1f2f`
- Exact final PR head: `eb981a1c4703477278ab1a4bfeada6a98b994586`
- Verification gate on that exact head:
  - `ORIGINAL SPEC tests` run `#292`: `SUCCESS`
  - `Original Spec` run `#484`: `SUCCESS`
  - build, OriginalSpec self-test and password compatibility fixture: `SUCCESS`
- PR `#109` closed the fixed-bank global `$00/$01` bootstrap/main/NMI dispatcher map.
- PR `#107` closed special-normal platform exits `$02=$0C-$10`.
- PR `#104` closed principal normal warm-reload destinations.
- PR `#102` closed the interactive `$F025 <-> $A275` warm-reload selector.
- PRs `#94/#96/#98` remain authoritative for the `$70-$89` narrative and `$04=$8F` reload path.
- Workflow hardening checkpoint: PR `#74` remains authoritative for continuation/anti-loop semantics.

## DONE

### Complete platform exit/reload boundary

The platform-local exit family `$02=$00-$11` remains closed and must not be reopened without contradictory ROM evidence or fixture failure.

Promoted normal reload destinations include stable engine states `$00/$10/$90`, plus explicit selector reentry where already documented.

### Global `$00/$01` dispatcher partition — PR #109

The structural bootstrap/main/NMI partition is closed by `EngineStateDispatcherMap` and `ENGINE_STATE_DISPATCHER.md`.

Key bootstrap successors remain:

```text
reload $00 -> $20
reload $10 -> $11
reload $90 -> $91
```

Do not reopen this top-level partition unless a family trace demonstrates an omitted logical branch.

### Engine family `$11-$14` — PR #111

The lower family entered from promoted reload `$10` is now closed end-to-end.

Entry:

```text
stable reload $10
 -> $C180 short bootstrap
 -> $C458 stages durable snapshot
 -> $D442 INC $00
 -> dispatch-ready $00/$01=$11
```

`$D442` additionally establishes:

```text
$06 = min($02,$0C)
$03AA = 0
$58 = $BE
```

The common reload commit left `$05=1`. State `$11` uses it as a Start-release latch: a held Start is ignored until one Start-released frame clears `$05=0`.

Exact logical input masks in `$3D`:

```text
Up    = $08
Down  = $04
Start = $10
```

When `$02!=0`, the reachable two-row cursor is:

```text
upper = $58=$BE
lower = $58=$CE
```

State `$11` has two terminal choices after the Start latch is cleared.

Upper/default route, or any Start with `$02=0`:

```text
$11
 -> $C2B8 $04=0
 -> $CA94 / bank-1 $951F refresh Saint snapshot
 -> $00/$01=$3D
 -> $E100
```

This exits to the already-promoted normal reload machinery.

Lower route `$58=$CE`:

```text
$11
 -> bank-0 $AE18
 -> generate password display stream at $0600 from staged $0110+ durable state
 -> $00/$01=$12
 -> $14=$08, $15=$23
```

`$AE18` is the existing password encoder/output builder documented by `PASSWORD_SYSTEM.md`; it terminates the generated `$0600` stream with `$FF`.

State `$12` is one-NMI transitional:

```text
$D2A2 JSR $D543   ; presentation only
$D2A5 INC $00
$D2A7 INC $01

$12 -> $13
```

State `$13` is text-driven, not timer-driven:

```text
NMI $D42D
 -> map bank 1
 -> A=$0D
 -> JSR $8D5A
 -> dynamic source pointer $0600
```

When the interpreter reaches generated terminator `$FF`:

```text
$8DDB INC $00
$8DDD INC $01
$57=$80
$26=$80
$27=$80

$13 -> $14
```

State `$14` is an **absorbing normal-engine terminal state**:

- main routes it through low generic bank-1 `$9363`, which has no `$14` writer;
- main clears `$05=0`;
- NMI matches no dedicated `$14` route and falls through `$D367`;
- no normal family-local writer advances `$00/$01` from `$14`.

Therefore the password-output branch ends as:

```text
$12 -> $13 -> $14 -> $14 -> ...
```

Escape requires reset/external restart rather than another engine-state transition. This disproves the earlier working assumption that `$14` necessarily had to lead to a state outside the family.

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/EngineState11To14Machine.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/EngineState11To14MachineChecks.cs`
- `docs/reverse-engineering/ENGINE_STATE_FAMILY_11_14.md`
- PR `#111`

Do not reopen `$11-$14` absent contradictory ROM evidence or fixture failure.

## EVIDENCE

### Why `$60-$6F` is selected next

During the `$11-$14` trace, a stronger directly reachable open boundary was confirmed from the already-promoted active platform state `$20`.

Fixed main state `$20` executes bank 1:

```text
$C319 JSR $8000
```

Bank-1 `$8000` invokes both resource consumers before ordinary player action. The already-promoted `PLATFORM_PRE_PLAYER_RESOURCE_ORDER.md` explicitly bounded its executable composition to the **non-fatal** path and left Life/Cosmo exhaustion as a separate transition boundary.

Life drain `$927A+` subtracts a two-point tick from the active Saint. Fatal underflow reaches:

```text
$92DD clear active Life pair
$92E3 LDA #$60
$92E5 STA $00
$92E7 STA $01
```

Cosmo drain `$930A+` subtracts a one-point tick. Fatal underflow reaches:

```text
$9357 clear active Cosmo pair
$935F JMP $92E3
```

Thus `$60` is not merely present in the global dispatcher: it is directly reachable from the already-closed state `$20` platform frame through a known resource-exhaustion condition.

Fixed state-$20` code also checks the transition immediately after bank-1 work:

```text
$C31C LDA $01
$C31E CMP #$60
$C320 BEQ $C339
```

so ordinary attack/player processing is skipped once exhaustion commits `$60`.

### Structural `$60` family anchors already bounded

Main high-nibble `$60` enters `$C364+`.

The visible terminal gate is:

```text
$C37C LDX $4D
...
$C384 INX
$C385 CPX #$E0
$C387 BCC $C38E
$C389 LDA #$FF
$C38B JMP $C2BA
```

`$C2BA` stores A into `$04`, refreshes the Saint snapshot, commits `$00/$01=$3D`, and jumps `$E100`. Therefore state `$60` appears to terminate through **reload mode `$04=$FF`**, a mode intentionally left outside the earlier normal `$04=0` and narrative `$04=8F` checkpoints.

NMI high-nibble `$60` maps bank 1 and calls `$9D69`; the bounded body is presentation/resource display logic and has not yet shown a global-state writer.

This is sufficient to choose the family as the next checkpoint, but not sufficient to promote its final destination: `$04=$FF -> $E100` remains unclosed.

### Other unresolved direct families remain deferred

The high `$91-$99` chain remains reachable from stable reload `$90` and is still a valid later boundary. It is not selected now because `$60` closes an explicit fatal-path hole inside the already-promoted normal platform frame and connects directly to a previously excluded reload mode `$FF`.

## OPEN

1. Prove whether `$60` is the only reachable member of dispatcher range `$60-$6F`, or whether any executable writer advances/sets `$61-$6F`.
2. Close the exact fatal-Life and fatal-Cosmo entry conditions from `$927A/$930A`, including `$7F/$80`, active-Saint resource pair mutation and the special `$02=$10` periodic-Life gate only insofar as they affect entry to `$60`.
3. Reduce main `$C364-$C3AB` to logical state/fade/timer semantics and identify the exact condition that reaches `$C389`.
4. Confirm that NMI `$D2F0 -> bank-1 $9D69` is presentation-only with respect to global progression/state.
5. Trace the resulting `$04=$FF`, `$00/$01=$3D`, `$E100` path through its first stable engine destination. This is a new reload mode and must not be conflated with `$04=0` or `$04=8F`.
6. Record only persistent fields that materially survive or select the post-failure result, especially active-Saint Life/Cosmo, `$4D/$4E`, `$40`, `$04`, `$03/$0533`, `$067D/$050E`, and any resource/progression restoration.
7. `$91-$99`, `$30-$4F`, renderer, RNG, audio and broader boss progression remain outside this checkpoint.

## NEXT

**Close the resource-exhaustion engine family `$60-$6F`, from fatal platform Life/Cosmo drain through reload mode `$04=$FF` to its first stable destination.**

Completion criterion:

> Starting from active platform state `$20`, prove every reachable fatal resource entry into the `$60` dispatcher family, determine the reachable `$60-$6F` state set, reduce main/NMI cooperation to logical effects, and trace the family’s `$04=$FF -> $3D -> $E100` exit to the first stable engine state without modeling unrelated PPU/audio bodies.

Required sequence:

1. close bank-1 `$927A-$9309` Life exhaustion and `$930A-$935F` Cosmo exhaustion against existing resource semantics;
2. prove whether any writer can produce `$61-$6F` from the reachable `$60` entry;
3. trace main `$C364-$C3AB` and NMI `$D2F0/$9D69` only far enough to identify logical timers/fields and all `$00/$01/$04` writes;
4. prove the exact `$C389 -> $C2BA` exit condition and persistent state carried into `$E100`;
5. reconstruct reload mode `$04=$FF` to the first stable destination, reusing existing reload primitives where valid but not assuming equivalence to `$04=0/$8F`;
6. implement one semantic failure-transition model plus discriminating fixtures after the graph is closed;
7. document the family and run both verification workflows.

## BLOCKERS

- None. The canonical ROM, global dispatcher map, active-platform resource model and fatal-entry addresses are available.

## RECOVERY CONTRACT

A new session must be able to resume this project without chat history.

Recovery order:

1. read this file from `main`;
2. reconcile `CURRENT` with newer merged Git history if any exists;
3. inspect `ENGINE_STATE_DISPATCHER.md`, `ENGINE_STATE_FAMILY_11_14.md`, `PLATFORM_PRE_PLAYER_RESOURCE_ORDER.md` and only the code/docs/tests required by the active `$60-$6F` boundary;
4. use `docs/REVERSE_ENGINEERING_STATUS.md` only as a global navigation/maturity map;
5. use `docs/WORK_PROTOCOL.md` for execution rules;
6. when private assets are required, use the private Drive `PRIVATE_WORKSPACE_MANIFEST — Saint Seiya Reborn`; private evidence is indexed under `04_REVERSE_ENGINEERING/EVIDENCE_INDEX`;
7. Drive never overrides this file and never owns a separate `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `engine-state-family-60-6f-resource-exhaustion`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, new evidence, an evidence map, a discarded hypothesis, or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.

## CONTINUE SEMANTICS

When the user says `continúa` or `next` with no narrower instruction:

1. read this file first;
2. reconcile it with current `main` if repository history is newer;
3. execute the single `NEXT` in FAST mode until material progress or a real blocker;
4. run the smallest relevant VERIFY gate;
5. checkpoint only after verification;
6. update `DONE / EVIDENCE / OPEN / NEXT / BLOCKERS / ANTI-LOOP` before ending the cycle.

# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa`**.

Technical subsystem documents remain authoritative for their own evidence and semantics. This file answers only: what checkpoint is accepted, what remains open, and what exact action must run next.

## CURRENT

- Phase: `ORIGINAL SPEC / persistent platform frame + entity-family promotion`
- State: `READY_FOR_NEXT`
- Last verified checkpoint: PR `#81` — scheduled primary entity types `$0D/$0E` promoted from their real bank-3 entry path and integrated into the persistent hybrid scheduler.
- Merge commit: `be773e7bfce81701511ebcb7229fa8eae9bf6293`
- Verification gate on exact final PR head `f654dcd2226decba231dcbe88d5712e7e3a4498c`:
  - `ORIGINAL SPEC tests` run `#229`: `SUCCESS`
  - `Original Spec` run `#411`: `SUCCESS`
- Workflow hardening checkpoint: PR `#74` remains authoritative for continuation/anti-loop semantics.
- Last material result:
  - `$0D/$0E` are now routed through a dedicated runtime from `$A48B/$A48F -> $A4BF -> $A55E`, without inheriting the `$08/$09/$0C` `+$04/$039A` pre-dispatch;
  - shared `$50/$E0`, optional `$30`, movement/removal, `$9915/$98BA`, `$AA70`, post-interaction `+$04`, `$40`, `$D0` and type-aware `$70` semantics are composed where the ROM actually shares them;
  - type `$0D` additionally models its `$A0-$AF` progression and `$A647` terminal retirement including the extra type-$0D visual record;
  - actual `$8927` spawn output for both `$0D` and `$0E` reaches the persistent/hybrid frame successfully;
  - shared attack/contact state and persistent slot state continue correctly into subsequent frames.

## DONE

The following boundaries are closed unless contradictory evidence appears.

### Hybrid A -> B scheduler

- activity gate precedes semantic route selection;
- common admitted slots use `PlatformCommonEntitySlotRuntime`;
- `$08/$09/$0C` use `PlatformSpecialEntityActive08090C`;
- `$0D/$0E` use `PlatformSpecialEntityActive0D0E`;
- attack state, `$76/$7F/$80`, Seventh Sense and `$039A` carry A -> B in-order.

### Primary removal / occupancy lifecycle

- confirmed removal paths compose exact `$A647` primary retirement;
- tracked visual occupancy becomes `$FE`;
- logical `+$00` clear obeys engine `$00 < $30`;
- type `$0D` receives the confirmed extra `+$2C/+$2D` visual retirement;
- frame N retirement can feed frame N+1 gating and later producer reuse directly.

### Persistent producer -> hybrid state bridge

- NMI acceptance remains separate from main-thread producers;
- encounter latch / `$03B7`, richer slots, `$03B8`, `$03A2`, Seventh Sense, `$039A` and `$3C` persist together;
- both promoted producers reset logical `+$04`, skip `+$08`, and do not own the external attached `$A908/$AA70` hazard record;
- producer output feeds the hybrid runtime without manual slot reconstruction.

### Main-thread exit split

Direct ROM order remains closed as:

```text
$C30A JSR $B6D0
$C30F bank-1 select
$C312 JSR $969D
if continuing: $C319 JSR $8000
bank-1 $800C JSR $8927
```

Accepted exits preserve completed `$B6D0` mutations and suppress `$8927`, later player/entities and `$3C` advancement.

### Scheduled `$0D/$0E` active route

- direct dispatcher entry bypasses `$A4A7-$A55B`;
- global `$039A` is not mutated merely by `$0D/$0E` activity;
- ordinary interaction reaches `$9915 -> $98BA -> $AA70` in that order;
- `$AA70` still applies its own geometry before the `$76` latch test;
- post-interaction `+$04` cadence remains shared at `$A738-$A747`;
- type `$0D` `$A0-$AF` and terminal removal are closed;
- type `$0E` terminal `$70` returns to `$10` through the shared type-aware late helper.

VERIFY note for PR #81: the first `Original Spec` run failed only because the ordering fixture supplied `AttachedHazardState.Empty` (`Y=$F0`) and therefore `$AA70` rejected on geometry before reaching the latch check. Direct ROM flow confirmed `$AA70` follows `$98BA`; the fixture was corrected to use an overlapping attached record. Runtime semantics were not changed by the fix, and the final head passed both workflows.

Do **not** reopen these boundaries merely to re-check them. Reopen only for a failing fixture, contradictory ROM evidence, or a newly promoted side effect crossing one of them.

## EVIDENCE

Primary artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformPersistentPrimaryEntityFrame.cs`
- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformHybridEntityCombatSlice.cs`
- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformSpecialEntityActive08090C.cs`
- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformSpecialEntityActive0D0E.cs`
- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformEntityRemovalA647.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/SpecialEntityActive0D0EChecks.cs`
- `docs/reverse-engineering/SPECIAL_ENTITY_ACTIVE_0D0E.md`
- merged PRs `#73` through `#81` relevant to the persistent composition chain.

Direct canonical-ROM anchors for the latest boundary include `$A478-$A4BF`, `$A55E+`, `$A700-$A749`, `$A79E+`, `$A86B-$A885`, `$A886+` and `$A647`.

## OPEN

1. Type `$0F` remains outside the hybrid scheduler and has a distinct direct dispatcher jump at `$A495 -> $A74C`; it must not be forced through common or `$0D/$0E` semantics.
2. The `$0F` path has already-visible special rules that are not yet promoted: unconditional `Y += 2`, facing-dependent horizontal motion, removal at `Y >= $A0` or `X < $04`, dedicated projectile/contact geometry, and a type-specific `$D0` cadence shortcut.
3. Full renderer-owned animation/tile/Y/attribute/X state remains outside this logical/occupancy runtime except for evidence-backed lifecycle writes already promoted.
4. Higher-level native destination/state-machine behavior after semantic `$3D/$70` exit remains above this entity-frame layer.

## NEXT

**Promote type `$0F` as a dedicated active runtime from its real `$A495 -> $A74C` path and integrate it into the persistent hybrid scheduler.**

Completion criterion:

> A type `$0F` produced by the generic edge producer can enter the persistent/hybrid frame, execute its direct `$A74C` movement / interaction / reaction / death path with the correct geometry and cadence, retire through `$A647` when required, and persist or free the slot correctly on the following frame without entering the common dispatcher or any scheduled-special pre-dispatch.

Required sequence:

1. close direct control flow from `$A495 -> $A74C` through `$A79E/$A7FB/$A886` for type `$0F`;
2. model exact `$A74C` movement: `Y += 2`, facing-dependent horizontal ±2 plus camera correction, and removal thresholds;
3. model the dedicated `$9915/$98BA` parameter set installed at `$A78E-$A79B` and prove whether `$AA70` / `+$04` are absent on this route;
4. model type `$0F` `$D0` cadence, including the `$A7FD CMP #$0F` shortcut that bypasses the normal `($3C & 3)` gate;
5. determine which late `$A886` `$70` semantics are genuinely shared and reuse only those primitives;
6. create a dedicated hybrid route/runtime rather than widening common or `$0D/$0E` code;
7. add fixtures beginning from actual generic `$B6D0` type-`$0F` output, with at least one interaction case and one removal/next-frame case;
8. run both verification workflows before checkpointing.

## BLOCKERS

- None for the first reverse-engineering pass. The canonical ROM is available privately and the direct `$0F` entry jump is already identified.

## ANTI-LOOP

- `last_next_signature`: `type0f-direct-a74c-runtime`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

Rules:

- A cycle counts as progress only if it produces code, a test/fixture, new evidence, an evidence map/table, a discarded hypothesis, or an explicit evidence-backed architectural decision.
- If two consecutive cycles finish with the same blocker, same `NEXT`, and no new evidence, a third identical attempt is forbidden.
- On anti-loop trigger: change technique/source or roll back to the last verified checkpoint.
- Closed phases are not re-entered merely because uncertainty remains elsewhere.

## CONTINUE SEMANTICS

When the user says `continúa` with no narrower instruction:

1. read this file first;
2. verify that the latest checkpoint still matches current `main` or reconcile newer merged work;
3. execute `NEXT` in FAST mode until a material result or real blocker;
4. run the smallest relevant VERIFY gate;
5. checkpoint only after verification;
6. update `DONE / EVIDENCE / OPEN / NEXT / BLOCKERS / ANTI-LOOP` before ending the cycle.

If repository history is newer than this file, repository history wins temporarily: reconstruct the real checkpoint, update this file, and only then continue.

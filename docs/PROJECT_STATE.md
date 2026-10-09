# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa`**.

Technical subsystem documents remain authoritative for their own evidence and semantics. This file answers only: what checkpoint is accepted, what remains open, and what exact action must run next.

## CURRENT

- Phase: `ORIGINAL SPEC / persistent platform frame + late-object composition`
- State: `READY_FOR_NEXT`
- Last verified checkpoint: PR `#83` — primary entity type `$0F` promoted from direct bank-3 `$A495 -> $A74C` flow and integrated into the persistent hybrid scheduler.
- Merge commit: `e21255b1e81d846566204086ea0ae19568f0eff8`
- Verification gate on exact final PR head `a7fae20bc1bc927fd4b8d4eafec5eea9916941a2`:
  - `ORIGINAL SPEC tests` run `37926512133`: `SUCCESS`
  - `Original Spec` run `37926512096`: `SUCCESS`
- Workflow hardening checkpoint: PR `#74` remains authoritative for continuation/anti-loop semantics.
- Last material result:
  - type `$0F` now has a dedicated hybrid route rather than entering common or scheduled-special semantics;
  - direct `$A74C` movement/removal, square `8/8/6/6` projectile/contact geometry, no `$AA70`/`+$04` cadence, same-frame `$40` progression, per-update `$D0-$DF` cadence, shared type-aware `$70`, and `$A647` retirement are executable and regression-tested;
  - actual generic `$B6D0` type-`$0F` output reaches that route in the same main-thread frame;
  - primary A/B type coverage is now closed for `$00-$09`, `$0A/$0B` promoted `$10/$50`, `$0C-$0F`, subject only to a reachability audit for whether `$0A/$0B` can ever enter any other action family.

## DONE

The following boundaries are closed unless contradictory evidence appears.

### Hybrid A -> B primary scheduler

- activity gate precedes semantic route selection;
- common admitted slots use `PlatformCommonEntitySlotRuntime`;
- `$08/$09/$0C` use `PlatformSpecialEntityActive08090C`;
- `$0D/$0E` use `PlatformSpecialEntityActive0D0E`;
- `$0F` uses `PlatformSpecialEntityActive0F`;
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
- post-interaction `+$04`, `$40`, `$D0`, type `$0D` `$A0-$AF`, and type-aware `$70` behavior are closed.

### Direct type `$0F` active route

- `$A495` jumps directly to `$A74C`;
- every admitted update applies `Y += 2`, facing-dependent horizontal `±2`, and camera correction before interaction;
- removal thresholds are `Y >= $A0` or `X < $04`;
- interaction uses `$9915 -> $98BA` with `8/8/6/6` geometry and does not execute `$AA70` or `$A738`;
- `$40-$4F` uses the shared >=`$08` progression to `$00`;
- `$A7FD` type-`$0F` shortcut bypasses the normal death cadence gate, so `$D0-$DF` advances every update;
- late `$A886` behavior is shared only where type-aware rules prove it: midpoint `$78` request with zero `$0F` child template and terminal `$7F -> $10`;
- movement/death removal composes `$A647` and persists correctly into the next frame.

Do **not** reopen these boundaries merely to re-check them. Reopen only for a failing fixture, contradictory ROM evidence, or a newly promoted side effect crossing one of them.

## EVIDENCE

Primary artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformPersistentPrimaryEntityFrame.cs`
- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformHybridEntityCombatSlice.cs`
- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformSpecialEntityActive08090C.cs`
- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformSpecialEntityActive0D0E.cs`
- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformSpecialEntityActive0F.cs`
- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformEntityRemovalA647.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/SpecialEntityActive0FChecks.cs`
- `docs/reverse-engineering/SPECIAL_ENTITY_ACTIVE_0F.md`
- merged PRs `#73` through `#83` relevant to the persistent primary composition chain.

Direct canonical-ROM anchors for the latest closed boundary include `$A491-$A495`, `$A74C-$A79E`, `$A7FB-$A839`, `$A886+` and `$A647`.

## OPEN

1. `$9B93` multisprite is already reconstructed in isolated bootstrap/normal/flag/death/substate-`$0D` components but still lacks one authoritative dispatcher/runtime that reproduces the real `$9B93 -> $9CAC` branch ordering and persistent bootstrap state.
2. The persistent main-thread frame still omits the earlier late-object order `$9B93 -> auxiliary hazards $96B4/$9761 -> primary A/B $A442 -> attack-object $A22C`.
3. Types `$0A/$0B` are promoted only for `$10/$50`; static evidence strongly suggests this is the complete reachable action closure, but that reachability claim should be documented explicitly before primary-family coverage is declared globally complete.
4. Full renderer-owned animation/tile/Y/attribute/X state remains outside the logical/occupancy runtime except for evidence-backed lifecycle writes already promoted.
5. Higher-level native destination/state-machine behavior after semantic `$3D/$70` exit remains above this entity-frame layer.

## NEXT

**Compose the already-closed `$9B93` branches into one persistent top-level multisprite runtime/dispatcher.**

Direct ROM dispatch evidence already established for this next block:

```text
$9CAC  active visual scan / bootstrap boundary
$9CBB  if $02 == $0D -> JMP $A12A
otherwise read part0 flags
flag $08 clear -> normal route at $9D05
flag $08 set   -> JMP $9ED1
normal/flag08 routes each divert logical $D0/$E0 -> $A06E before ordinary work
```

A newly initialized bootstrap returns at `$9CAB`; it does **not** run the active updater again in that same call/frame.

Completion criterion:

> One persistent `$9B93` state can be stepped once per frame from empty/cooldown/bootstrap or any already-active promoted branch, route through exactly one ROM-valid branch, carry attacks/contact/Seventh Sense and persistent `$03FA/$03A9/$81` state correctly, and feed its resulting state directly into the next frame.

Required sequence:

1. define persistent `$9B93` state carrying runtime visual/logical state, `$03FA` cooldown, global `$03A9`, and mode `$81`;
2. run `PlatformMultisprite9B93Bootstrap` first and distinguish `ExistingActive` from initialization/cooldown/no-spawn outcomes;
3. when already active, dispatch exactly:
   - substate `$0D` -> `PlatformMultisprite9B93Substate0D`;
   - otherwise logical `$D0/$E0` -> `PlatformMultisprite9B93DeathDrop`;
   - otherwise flag `$08` clear -> `PlatformMultisprite9B93NormalUpdate`;
   - flag `$08` set + `$04` set -> `PlatformMultisprite9B93Flag08Bit04`;
   - flag `$08` set + `$04` clear -> `PlatformMultisprite9B93Flag08Clear04`;
4. preserve unwritten persistent fields during bootstrap rather than reconstructing the logical record from scratch;
5. add fixtures proving newly initialized bootstrap does not also active-update, route precedence for `$0D` and `$D0/$E0`, both flag-`$08` branches, and attack/contact state carry;
6. run both verification workflows before checkpointing.

After this dispatcher is closed, the next composition target is the actual late-object frame order: `$9B93 -> auxiliary A/B -> hybrid primary A/B -> $A22C`.

## BLOCKERS

- None. All currently identified `$9B93` active branches are already represented by isolated clean-room primitives, and the canonical ROM is available privately to verify their dispatch boundaries.

## ANTI-LOOP

- `last_next_signature`: `multisprite-9b93-top-level-dispatch`
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

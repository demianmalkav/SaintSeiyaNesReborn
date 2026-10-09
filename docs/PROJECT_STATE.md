# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa`**.

Technical subsystem documents remain authoritative for their own evidence and semantics. This file answers only: what checkpoint is accepted, what remains open, and what exact action must run next.

## CURRENT

- Phase: `ORIGINAL SPEC / persistent platform frame + late-object composition`
- State: `READY_FOR_NEXT`
- Last verified checkpoint: PR `#86` — `$9B93` multisprite branches composed into one persistent top-level runtime/dispatcher.
- Merge commit: `00334008a4230761ef772fa67faf9c54df8cf189`
- Verification gate on exact final PR head `1008557fbbb4eb22fa0e1d4ab1547b339a773e3f`:
  - `ORIGINAL SPEC tests` run `#239`: `SUCCESS`
  - `Original Spec` run `#424`: `SUCCESS`
- Prerequisite correction: PR `#85` fixed substate `$0D` bootstrap from direct `$9C0F -> $A0E4` ROM flow; both workflows were green (`#237/#420`) before merge.
- Workflow hardening checkpoint: PR `#74` remains authoritative for continuation/anti-loop semantics.

## DONE

Closed unless contradictory evidence appears:

### Persistent primary scheduler

- generic `$B6D0` producer, exit split, scheduled `$8927` producer, player pipeline, hybrid primary A -> B, late attack objects and `$3C` persistence are executable;
- primary routes are explicit for common families, `$08/$09/$0C`, `$0D/$0E`, and direct type `$0F`;
- exact `$A647` removal/visual occupancy lifecycle is composed.

### `$9B93` bootstrap correction

Direct ROM flow proved that engine substate `$0D` does not use ordinary selector-profile initialization after the timed gate.

`$9C0F -> $A0E4` instead:

- creates only part0 as `Y=$20`, sprite `$8C`;
- selects `(flags=$02,X=$EF)` or `(flags=$42,X=$11)` from `$48&8`;
- clears logical action;
- loads dedicated raw profile `$1E,$05,$05,$01` from `$9B8F-$9B92`;
- does not write `$03A9`.

Bootstrap now exposes whether `$03A9` was actually written so persistent composition preserves it on early-return paths.

### Persistent `$9B93` top-level runtime

Every step begins at the bootstrap/active gate. New initialization returns at `$9CAB` and does not run an active branch in the same call.

For existing active visuals, direct dispatcher precedence is closed as:

```text
if $02 == $0D:
    dedicated $A12A route
else if logical family == $D0/$E0:
    $A06E death/drop
else if part0 flag $08 clear:
    normal $9D05 route
else if part0 flag $04 set:
    $9ED1 flag08/bit04 route
else:
    $9F79 flag08/clear04 route
```

Persistent state carries:

- full visual/logical runtime state;
- mode `$81`;
- cooldown `$03FA`;
- global `$03A9`;
- branch-specific attack state, `$76/$7F/$80`, and Seventh Sense outputs.

Fixtures cover no same-frame active update after bootstrap, cooldown/selector-zero preservation, `$0D` precedence, `$D0/$E0` precedence, all three flag routes, and real projectile carry through the normal branch.

### Primary type `$0F`

Direct `$A495 -> $A74C` route, movement/removal, `8/8/6/6` interaction, no `$AA70/+$04`, per-update `$D0-$DF`, late `$70`, and `$A647` persistence remain closed from PR `#83`.

Do **not** reopen these boundaries merely to re-check them. Reopen only for failing fixtures, contradictory ROM evidence, or a newly promoted side effect crossing the boundary.

## EVIDENCE

Primary artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformPersistentPrimaryEntityFrame.cs`
- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformHybridEntityCombatSlice.cs`
- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformMultisprite9B93Bootstrap.cs`
- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformMultisprite9B93Runtime.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/Multisprite9B93BootstrapChecks.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/Multisprite9B93RuntimeChecks.cs`
- `docs/reverse-engineering/MULTISPRITE_9B93_BOOTSTRAP.md`
- `docs/reverse-engineering/MULTISPRITE_9B93_RUNTIME.md`
- merged PRs `#83`, `#85`, `#86` for the latest promoted boundaries.

Direct canonical-ROM anchors include `$9B93-$9CAB`, `$9CBB-$9CD3`, `$9D05+`, `$9ED1+`, `$9F79+`, `$A06E+`, `$A0E4-$A129`, and `$A12A-$A22B`.

## OPEN

1. The persistent main-thread frame still omits the complete late-object order between the player/post-latch phase and primary A/B:

```text
$9B93 multisprite
 -> $96B4 auxiliary spawn
 -> $9761 auxiliary A
 -> $9761 auxiliary B
 -> $A442 primary A
 -> $A442 primary B
 -> $A22C attack-object update
```

2. `PlatformHybridEntityCombatSlice` currently owns pre-player/player/post-latch **and** primary A/B **and** `$A22C`, so the primary A -> B body must be extracted as a reusable after-player primitive before inserting earlier object classes without duplicating player or late attack phases.
3. Types `$0A/$0B` are promoted only for `$10/$50`; static evidence strongly suggests this is their complete reachable action closure, but that reachability claim still needs an explicit evidence note before declaring all primary type/action state space globally closed.
4. Full renderer-owned animation/tile/Y/attribute/X state remains outside logical runtimes except where exact lifecycle writes have been promoted.
5. Higher-level native destination/state-machine behavior after semantic `$3D/$70` exit remains above this entity-frame layer.

## NEXT

**Compose the complete promoted late-object order into one persistent main-thread frame.**

Completion criterion:

> A continuing platform frame can run the existing producer/exit/player phases, then `$9B93`, auxiliary A/B, primary A/B, one `$A22C` attack update, and one `$3C` increment with one shared attack/contact/Seventh-Sense state, while all persistent object states feed directly into the next frame.

Required sequence:

1. extract the already-closed primary slot A -> B body from `PlatformHybridEntityCombatSlice` into a reusable after-player pair primitive; refactor the existing hybrid slice to delegate to it with no semantic change;
2. define a richer persistent frame state containing current primary state plus persistent `$9B93` and `PlatformAuxiliaryHazardSpawnerState`;
3. preserve producer/exit ordering from `PlatformPersistentPrimaryEntityFrame`;
4. on the continuing path execute pre-player/player/post-latch once;
5. thread attack/contact/Seventh Sense through:
   - `PlatformMultisprite9B93Runtime`;
   - `PlatformAuxiliaryHazardInteractions.StepPair`;
   - extracted primary A -> B primitive;
   - `PlatformAttackFramePhases.UpdateObjectsAfterPlayer` exactly once;
6. increment `$3C` exactly once only on the normal path;
7. keep exceptional player early-exit semantics: producers persist, but `$9B93`, auxiliaries, primary slots, `$A22C`, and normal `$3C` advance are skipped;
8. add discriminating order fixtures:
   - projectile consumed by `$9B93` is invisible to auxiliary/primary classes;
   - projectile consumed by auxiliary A is invisible to primary A/B;
   - `$9B93` or auxiliary contact seeds `$76/$7F/$80` and suppresses later contact overwrite;
   - newly spawned auxiliary hazards update in the same frame;
   - `$A22C` runs once after all classes;
9. run both verification workflows before checkpointing.

## BLOCKERS

- None. All required late-object class primitives are now individually promoted.

## ANTI-LOOP

- `last_next_signature`: `persistent-complete-late-object-frame`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

Rules:

- A cycle counts as progress only if it produces code, a fixture, new evidence, an evidence map/table, a discarded hypothesis, or an evidence-backed architectural decision.
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

# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

## CURRENT

- Phase: `ORIGINAL SPEC / integral closure audit`
- State: `READY_FOR_NEXT`
- Closing checkpoint in PR `#167` — canonical runtime text-content integration for `MSG_000..MSG_250`.
- Exact code head verified before documentation-only state synchronization: `1fe8ba3c352f02ba345b4861fc5ee7650011df15`.
- Verification on that exact head:
  - `Original Spec` #630: `SUCCESS`
  - `ORIGINAL SPEC tests` #426: `SUCCESS`
  - build/self-test/password compatibility: `SUCCESS`
- Canonical ROM identity remains: size `262160`, SHA-1 `F871D9B3DAFDDCDAD5F2ACD71044292E5169064E`, MD5 `3B0F17C2B6EFC928B3D3FE9B1A389680`, SHA-256 `6917B31D7343A9A17170E833BACDBC3B1EBA3E02D11C51C0D44DBE436C9AD43A`, CRC32 `F8D258A3`.
- Global presentation, platform frame/rendering, battle/event coverage, canonical RNG, audio scheduler architecture and runtime text/content integration are frozen after their verified checkpoints.

## DONE

### Canonical runtime text-content integration — PR #167

The bounded runtime localization boundary around fixed `$E7B3/$E7B7/$E7C3/$E7C7`, `$066A/$066B/$0672` and stable `MSG_000..MSG_250` identity is closed without versioning original or translated dialogue payloads.

#### Canonical request tuple

The four entrypoints are frozen as:

```text
$E7B3 -> message id in $066A ; $0672=$FF ; dispatch $EC6D
$E7B7 -> message id in $066A ; $0672=$00 ; dispatch $EC6D
$E7C3 -> message id in $066B ; $0672=$FF ; dispatch $ECB8
$E7C7 -> message id in $066B ; $0672=$00 ; dispatch $ECB8
```

The runtime request identity is therefore:

```text
(message id, canonical slot $066A/$066B, raw $0672 variant)
```

`$0672` remains deliberately unnamed beyond raw request/presentation metadata. Current evidence does not justify interpreting it as speaker side, portrait side, player/opponent, or any narrower semantic concept.

#### Stable localization identity

Canonical identity remains immutable:

```text
numeric ids  0..250
stable ids   MSG_000..MSG_250
count        251 exactly
```

Descriptive `speaker`, `scene` and `semantic_alias` metadata may improve as context is confirmed, but cannot replace or renumber the stable ID.

#### External/private catalog contract

`CanonicalRuntimeLocalization` accepts the existing external CSV schema and enforces:

```text
exactly 251 entries
ids exactly 0..250
no duplicate ids
stable_id must match MSG_xxx
Japanese source must exist for every entry
JP / ES selection is explicit
missing ES may fall back to JP only under explicit Japanese fallback policy
fallback never changes message id
slot and $0672 metadata propagate unchanged
```

Full JP/ES payloads remain private. The public repository contains only schema/engine semantics, synthetic fixtures and descriptive traceability fields.

#### Public artifacts

- `src/SaintSeiyaNesReborn.OriginalSpec/CanonicalRuntimeLocalization.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/CanonicalRuntimeLocalizationChecks.cs`
- `docs/reverse-engineering/CANONICAL_RUNTIME_TEXT_CONTENT.md`
- reconciled `docs/reverse-engineering/TEXT_ENGINE.md`
- reconciled `docs/LOCALIZATION.md`
- PR #167

Fixtures use generated `JP_SYNTH_xxx` / `ES_SYNTH_xxx` strings only and prove entrypoint mapping, deterministic selection/fallback, slot/variant propagation, exact catalog coverage, duplicate/stable-ID rejection, external CSV quoting behavior and absence of embedded dialogue resources.

Do not reopen runtime text-content integration without contradictory canonical evidence, a changed catalog invariant or a failing fixture.

## EVIDENCE FOR NEXT

All material bounded ORIGINAL SPEC subsystems now have executable or mechanically documented closure points:

- ROM / boot / mapper / bank architecture;
- global `$00/$01` state namespace and interruption contracts;
- front-end/title/password;
- platform frame, exits/reload, object layers and presentation;
- maps / CHR / visual resources / HUD;
- battle/event dispatch, resources, damage, dodge, techniques and stage contexts;
- canonical RNG `$E0AC/$065F/$0660`;
- canonical audio scheduler `$DB9C/$DBB6/$0440+`;
- Japanese message extraction/codec and runtime localization boundary `MSG_000..MSG_250`.

The next boundary is not another subsystem reconstruction. It is an **integral closure audit**: prove that the accumulated ORIGINAL SPEC has no material unowned gameplay/runtime surfaces, contradictory contracts, stale open items or missing regression links before REBORN implementation is unfrozen.

## OPEN

1. Build a subsystem inventory from the authoritative docs/code/tests and classify each as `CLOSED`, `INTENTIONALLY_OUT_OF_SCOPE`, or `MATERIAL_GAP`.
2. Reconcile stale `TODO`, `OPEN`, `NEXT`, uncertainty markers and historical status text against the latest closed checkpoints; do not reopen a subsystem merely because an old document still contains historical language.
3. Audit executable ownership for the major runtime surfaces already modeled: state dispatch, NMI/main-thread presentation, platform objects/exits, battle/events, RNG, audio scheduler and text requests.
4. Cross-check clean-room contracts against their canonical evidence documents and fixtures; identify contradictions or duplicated semantics.
5. Run the complete existing ORIGINAL SPEC regression suite and record exact verified head/workflows.
6. Produce one integral closure report with any residual `MATERIAL_GAP` items ranked by impact and evidence. Zero material gaps is the criterion for freezing ORIGINAL SPEC and unblocking REBORN.
7. Stop after the closure audit. If gaps exist, NEXT must be the highest-impact bounded gap. If none exist, NEXT may transition to REBORN architecture/implementation planning.

## NEXT

**Perform the integral ORIGINAL SPEC closure audit: inventory every reconstructed runtime/gameplay subsystem, reconcile stale open markers against verified checkpoints, cross-check evidence ↔ clean-room contracts ↔ fixtures, run the complete regression suite, and determine whether any material unowned or contradictory behavior remains before REBORN is unfrozen.**

Completion criterion:

> Every material ORIGINAL SPEC subsystem must have an explicit owner/status and traceable evidence path. Historical TODOs must be either resolved by a later checkpoint, explicitly out of scope, or promoted to a concrete material gap. All clean-room contracts and regression fixtures must be green on one exact head. The audit must end with either (a) a finite ranked list of material gaps and a single bounded NEXT, or (b) zero material gaps and an evidence-backed declaration that ORIGINAL SPEC is closed enough to begin REBORN implementation.

## BLOCKERS

- None. All known prerequisite subsystem checkpoints, public clean-room contracts, regression fixtures and private canonical ROM/localization sources are available.

## RECOVERY CONTRACT

1. Refresh `main`, then read this file before executing `NEXT`.
2. Freeze PR #167 runtime text-content integration, PR #165 audio scheduler, PR #163 RNG and all earlier closed gates unless new contradictory evidence appears.
3. The audit may discover a gap, but discovery alone does not authorize broad rework: promote only evidence-backed material gaps to a new bounded checkpoint.
4. Treat historical `TODO`/`OPEN` prose as suspect until reconciled against newer checkpoints and tests.
5. Preserve content separation: ROM/audio/dialogue payloads remain private; public GitHub stores semantic results, tests and evidence summaries.
6. Do not begin REBORN implementation inside the integral audit checkpoint.
7. Drive remains private evidence/content storage only and never owns an independent `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `original-spec-integral-closure-audit`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, evidence, a discarded hypothesis or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.

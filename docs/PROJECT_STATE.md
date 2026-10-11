# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

## CURRENT

- Phase: `REBORN / architecture planning`
- State: `READY_FOR_NEXT`
- Integral ORIGINAL SPEC closure checkpoint: PR `#169` — zero-material-gap audit and baseline freeze.
- Exact audit regression head before documentation-only closure synchronization: `4f4f107dc874a60ad53a116f0fe3e7d12003c87f`.
- Verification on that exact head:
  - `Original Spec` #635: `SUCCESS`
  - `ORIGINAL SPEC tests` #432: `SUCCESS`
  - build/self-test/password compatibility: `SUCCESS`
- Audit verdict: `MATERIAL_GAP = 0`.
- `src/` currently contains only `SaintSeiyaNesReborn.OriginalSpec`; no REBORN implementation project has been created yet.
- ORIGINAL SPEC is now a frozen semantic baseline/oracle. Reopen it only for contradictory canonical evidence, a failing frozen fixture, or a material original dependency discovered during REBORN.

## DONE

### Integral ORIGINAL SPEC closure audit — PR #169

The accumulated reconstruction was audited across all material runtime/gameplay surfaces. `docs/reverse-engineering/ORIGINAL_SPEC_CLOSURE_AUDIT.md` owns the integral inventory.

Material surfaces classified `CLOSED` include:

- canonical ROM identity, boot, MMC1, vectors and bank architecture;
- global `$00/$01` state namespace, dispatch and reachability;
- front-end/title/attract/password and modal transitions;
- platform control, motion, attacks, frame progression, objects and hazards;
- platform maps, exits, warm reload and narrative progression;
- CHR/metasprites/palette/HUD/NMI/global presentation;
- battle/event dispatch, resources, damage, dodge, techniques and stable stage contexts;
- canonical RNG `$E0AC/$065F/$0660`;
- audio scheduler `$DB9C/$DBB6/$0440+` and APU ownership;
- Japanese message indexing/extraction/codec;
- runtime message request/localization contract for `MSG_000..MSG_250`.

Explicit `INTENTIONALLY_OUT_OF_SCOPE` items are not material gaps:

- exact original music/SFX payload reconstruction;
- full JP/ES dialogue payloads in the public repository;
- instruction-for-instruction or cycle-accurate emulation parity;
- dedicated battle context for `$050E=$0B`, proven structural/transient;
- REBORN-specific design, presentation and expansion decisions.

The stale README statement that warm reload was still the active frontier was reconciled as documentation debt, not contradictory runtime evidence.

The audit report is now a trigger for `.github/workflows/original-spec.yml`, so future edits to the closure claim run the complete C# OriginalSpec build/self-test/password gate as well as the Python clean-room parity workflow.

## ORIGINAL SPEC FREEZE CONTRACT

1. ORIGINAL SPEC remains authoritative for proven 1988 semantics.
2. REBORN may deliberately deviate, but deviations must be recorded as REBORN design decisions and must not rewrite ORIGINAL SPEC evidence.
3. Existing frozen fixtures/tables/state transitions are oracle material.
4. If new canonical evidence invalidates an oracle, use the `ORACLE CHANGE` process from `docs/VERIFY.md` and repair ORIGINAL SPEC in a bounded checkpoint before propagating the change.
5. Copyrighted ROM/audio/dialogue payloads remain private; public code consumes semantic contracts or external/private content.

## OPEN

There are no known material ORIGINAL SPEC gaps blocking implementation.

The open problem is now architectural: create a modern REBORN layer that consumes the frozen specification without coupling application/game code to NES addresses, bank switching, PPU tile encoding or private copyrighted payloads.

The first REBORN checkpoint must decide and document:

1. project/runtime structure under `src/` and `tests/`;
2. dependency direction between `OriginalSpec`, REBORN domain/gameplay, presentation/platform adapters and content/localization;
3. which ORIGINAL SPEC concepts cross the boundary as semantic DTOs/interfaces versus which remain evidence-only;
4. deterministic update/input/time model suitable for parity fixtures;
5. save/progression state ownership and versioning boundary;
6. localization/content injection boundary using the existing external-catalog contract;
7. rendering/audio adapter boundaries that avoid NES hardware leakage into domain logic;
8. the first bounded vertical slice and its parity acceptance criteria.

Do not start broad gameplay porting before these boundaries are frozen.

## NEXT

**Design and checkpoint the initial REBORN architecture: define the modern project/module boundaries, dependency rules, deterministic runtime loop, semantic bridge from ORIGINAL SPEC, content/localization injection, presentation/audio adapters, save-state boundary, and one minimal vertical slice whose acceptance tests prove that REBORN can consume frozen ORIGINAL SPEC behavior without importing NES hardware details into gameplay logic.**

Completion criterion:

> The repository must contain a documented and testable REBORN architecture with explicit dependency direction and a minimal compileable skeleton. ORIGINAL SPEC must remain independently buildable and unchanged semantically. The first vertical slice must be narrowly defined with parity-preservation tests or fixtures at the semantic boundary. No large-scale gameplay migration is allowed in this checkpoint.

## BLOCKERS

- None known.

## RECOVERY CONTRACT

1. Refresh `main`, then read this file before executing `NEXT`.
2. Treat `docs/reverse-engineering/ORIGINAL_SPEC_CLOSURE_AUDIT.md` as the integral closure inventory, not as a new work queue.
3. Freeze PR #169 audit result and all prior ORIGINAL SPEC gates unless evidence satisfies the freeze contract above.
4. Keep ORIGINAL SPEC and REBORN as separate layers; REBORN consumes the specification but never becomes evidence for the original game.
5. Prefer semantic interfaces over exposing raw NES RAM addresses/banks/registers to REBORN domain code.
6. Drive remains private evidence/content storage only and never owns an independent `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `reborn-initial-architecture-boundary`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, evidence, a discarded hypothesis or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.

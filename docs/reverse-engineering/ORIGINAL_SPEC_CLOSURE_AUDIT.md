# ORIGINAL SPEC integral closure audit

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Canonical ROM identity:

- size `262160` bytes;
- SHA-1 `F871D9B3DAFDDCDAD5F2ACD71044292E5169064E`;
- MD5 `3B0F17C2B6EFC928B3D3FE9B1A389680`;
- SHA-256 `6917B31D7343A9A17170E833BACDBC3B1EBA3E02D11C51C0D44DBE436C9AD43A`;
- CRC32 `F8D258A3`.

## Audit question

Is the accumulated ORIGINAL SPEC complete enough that REBORN can begin without relying on unowned material gameplay/runtime behavior from the 1988 game?

The audit classifies every material surface as:

- `CLOSED` — owned by canonical evidence plus executable/semantic contract and/or regression fixture;
- `INTENTIONALLY_OUT_OF_SCOPE` — not required to own gameplay/runtime semantics for REBORN entry and explicitly excluded;
- `MATERIAL_GAP` — behavior that can materially change game progression, control, combat, presentation contract, deterministic state, localization identity or integration and lacks an adequate owner.

## Integral inventory

| Surface | Status | Primary ownership/evidence |
|---|---|---|
| Canonical ROM identity, boot, vectors, MMC1 and bank mapping | `CLOSED` | `CANONICAL_ROM.md`, `BOOT_AND_MAPPER.md`, `BANK_MAP.md` |
| Global `$00/$01` state namespace and dispatch/reachability | `CLOSED` | state-dispatch/reachability documents, `EngineStateDispatcherMap`, `EngineStateReachability`, state-machine fixtures |
| Front end, title/attract/password and modal transitions | `CLOSED` | front-end/password contracts and compatibility fixture |
| Platform player control and frame progression | `CLOSED` | player/airborne/attack/motion contracts and self-test fixtures |
| Platform object layers, hazards and interactions | `CLOSED` | auxiliary/primary/late-object evidence and fixtures |
| Platform maps, exits, reload and narrative transitions | `CLOSED` | map/exit/warm-reload/narrative evidence and executable contracts |
| CHR, metasprites, palette/HUD and global presentation ownership | `CLOSED` | visual-resource, state-$20 NMI, HUD and global NMI-coverage checkpoints |
| Battle/event dispatch | `CLOSED` | `BATTLE_EVENT_DISPATCH.md` and executable dispatch fixtures |
| Boss resources, damage, dodge and techniques | `CLOSED` | dedicated evidence documents/contracts and regression fixtures |
| Stable battle contexts `$00-$0A` | `CLOSED` | per-stage contexts plus `BATTLE_STAGE_CONTEXT_COVERAGE.md` |
| `$050E=$0B` | `INTENTIONALLY_OUT_OF_SCOPE` | proved structural/transient; no canonical stable battle and no dedicated context required |
| final-special `$0C` bridge | `CLOSED` | separately owned final-special/event checkpoint |
| Canonical RNG `$E0AC/$065F/$0660` | `CLOSED` | PR #163, `CanonicalRandomSourceE0AC`, consumer/seed/reset evidence and fixtures |
| Audio scheduling/arbitration/APU ownership | `CLOSED` | PR #165, `CanonicalAudioScheduler`, `$DB9C/$DBB6/$8B50+` evidence and fixtures |
| Original note/envelope/instrument/song/SFX payload reconstruction | `INTENTIONALLY_OUT_OF_SCOPE` | scheduler architecture is owned; copyrighted payload/content parity is not required for REBORN architecture entry |
| Japanese message indexing/extraction/codec | `CLOSED` | `TEXT_ENGINE.md`, canonical extractor and 251-message pointer/storage proof |
| Runtime text request semantics and JP/ES localization boundary | `CLOSED` | PR #167, `CanonicalRuntimeLocalization`, `MSG_000..MSG_250` invariants and fixtures |
| Full JP/ES dialogue payload | `INTENTIONALLY_OUT_OF_SCOPE` for public repository | private external catalog by design; identity/schema/runtime contract is closed |
| Instruction-for-instruction CPU emulation parity | `INTENTIONALLY_OUT_OF_SCOPE` | project target is semantic reconstruction, not a cycle-accurate emulator |
| REBORN-specific architecture, UI, assets and expansions | `INTENTIONALLY_OUT_OF_SCOPE` for ORIGINAL SPEC | belongs to the next project phase and must not rewrite ORIGINAL SPEC evidence |

## Historical-marker reconciliation

The audit searched current `main` for active `TODO`, `OPEN`, `pending`, `uncertain` and `Next work` markers. No indexed active marker promoted a new material gameplay/runtime gap.

One stale orientation statement was found in the repository README: it still described normal warm reload as the active frontier. That statement predates later closure checkpoints and is documentation debt, not contradictory runtime evidence. The audit checkpoint updates the README to point to the closed ORIGINAL SPEC state and REBORN transition.

Historical subsystem documents may retain local narrative about what was "next" at the time they were written. Such prose is non-authoritative unless it is also present in current `docs/PROJECT_STATE.md` and survives reconciliation against later checkpoints.

## Ownership cross-check

The major runtime surfaces required by the audit all have explicit owners:

```text
state selection / dispatch        global state map + state-machine contracts
NMI / presentation               global NMI coverage + state-$20/HUD/visual contracts
platform frame                    player + motion + attacks + object-layer contracts
platform exits / reload           exits + warm reload + narrative progression contracts
battle / event                    dispatch + resources + stage context contracts
randomness                        CanonicalRandomSourceE0AC
sound architecture                CanonicalAudioScheduler
message requests / localization   CanonicalRuntimeLocalization
```

No two public clean-room contracts were found to claim contradictory ownership of the same canonical RAM byte or dispatcher boundary. Shared bytes that cross subsystems are explicitly modeled at their handoff boundary rather than silently duplicated.

## Regression gate

The repository's complete existing public ORIGINAL SPEC regression surface is:

1. `.github/workflows/original-spec.yml`
   - build `SaintSeiyaNesReborn.OriginalSpec`;
   - run the complete C# self-test project;
   - run the password compatibility fixture.
2. `.github/workflows/original-spec-tests.yml`
   - discover and run all `tests/test_*_spec.py` clean-room parity tests.

This audit adds its report path to the `Original Spec` workflow trigger so an audit checkpoint cannot be promoted without running both regression families on the same PR head.

Final run numbers/head are recorded in `docs/PROJECT_STATE.md` after CI completes.

## Residual risk vs material gap

Residual uncertainty remains possible at the level of names, exact audiovisual content, unused data, implementation details that do not alter proven semantics, or optional future fidelity work. Those are not `MATERIAL_GAP` by themselves.

A closed subsystem may be reopened only by one of:

- contradictory canonical ROM/runtime evidence;
- a failing frozen fixture;
- a REBORN requirement that reveals an unmodeled original semantic dependency.

Such discovery becomes a new bounded ORIGINAL SPEC checkpoint; it does not invalidate this audit retroactively.

## Verdict

`MATERIAL_GAP = 0`.

The known gameplay/runtime surfaces required to start a modern reconstruction have explicit ownership and traceable evidence. The remaining excluded areas are deliberate content/fidelity scope decisions rather than missing game-logic ownership.

Therefore ORIGINAL SPEC is **closed enough to freeze as the semantic baseline and unblock REBORN architecture/implementation planning**, subject to the regression gate on the exact audit PR head.

The first REBORN checkpoint must define architecture boundaries and parity-preservation policy before implementing broad gameplay changes. It must consume ORIGINAL SPEC as an oracle rather than mutate it.

# Verification contract

## Purpose

This document standardizes promotion decisions for both `ORIGINAL SPEC` and later `REBORN` work. It does not define what to work on next; `docs/PROJECT_STATE.md` owns that.

## Verdict vocabulary

Each required gate is reported as:

- `PASS` — executed and matched the expected invariant.
- `FAIL` — executed and contradicted the expected invariant.
- `BLOCKED` — a prerequisite prevented execution.
- `NOT_APPLICABLE` — the change cannot affect the gate and the exemption is justified.
- `NOT_RUN` — relevant but not executed; promotion is not allowed.

A checkpoint can be promoted only when every gate required by the current `PROJECT_STATE.md` is `PASS` or explicitly justified `NOT_APPLICABLE`.

## Baseline recovery

Before verification:

1. refresh `main`;
2. read `docs/PROJECT_STATE.md`;
3. identify the last verified checkpoint and affected subsystem;
4. verify the canonical ROM before ROM-fed tooling is used;
5. preserve the ORIGINAL SPEC / REBORN boundary;
6. execute the smallest gate set capable of detecting damage to the changed surface plus any gate explicitly required by the current state.

## Verification layers

### 1. Input identity

For ROM-dependent work, verify the expected canonical ROM/hash and reject an unexpected input before interpreting derived results.

### 2. Build/static gate

Use repository build/self-tests, parsers and structural invariants relevant to the subsystem. Existing GitHub workflows include:

- `.github/workflows/original-spec-tests.yml`
- `.github/workflows/original-spec.yml`

A successful build does not establish semantic parity.

### 3. ORIGINAL SPEC semantic gate

Prefer discriminating fixtures of the form:

```text
known state + inputs/conditions -> expected state/observable result
```

Depending on the subsystem, this can include:

- RAM/state transitions;
- bank/pointer reachability;
- password compatibility;
- event/battle dispatch;
- deterministic parser/renderer output;
- bounded OAM/metasprite composition;
- exact formulas or table selection proven from the ROM.

Do not require instruction-for-instruction matching when the project has deliberately closed a subsystem at semantic parity, but do not replace an unresolved original behavior with a plausible implementation.

### 4. REBORN regression gate

A REBORN change must preserve every ORIGINAL SPEC invariant that the design explicitly chose to retain. Deliberate deviations require a recorded design decision and new tests for the replacement behavior; they do not rewrite ORIGINAL SPEC evidence.

### 5. Evidence/reproducibility gate

A promoted claim must record enough evidence to reproduce it from a known state, including the relevant ROM identity, code/commit, fixtures or trace conditions, and the exact scope/limitations of the conclusion.

## Oracle protection

Treat these as Oracle material when they define expected behavior rather than implementation:

- canonical state-transition fixtures;
- frozen formulas/tables proven from ROM evidence;
- expected password vectors;
- pointer/bank invariants;
- parser/render expected outputs;
- coverage matrices and closed-gate assertions.

Never modify Oracle material merely because a candidate fails.

If new ROM/runtime evidence contradicts an Oracle expectation, mark the change `ORACLE CHANGE` and document:

1. contradictory evidence;
2. why the old expectation is invalid;
3. replacement invariant;
4. affected closed gates;
5. regression consequences.

Prefer a dedicated commit for the Oracle correction before changing implementation to satisfy the new expectation.

## Handoff format

Use an explicit gate report, for example:

```text
INPUT        PASS
BUILD        PASS
SEMANTIC     PASS
REGRESSION   PASS
EVIDENCE     PASS
VERDICT      PASS
```

Use `FAIL`, `BLOCKED`, `NOT_APPLICABLE` or `NOT_RUN` rather than “looks correct”. The verdict is subordinate to the current `PROJECT_STATE.md` completion criterion.

# AGENTS.md — SaintSeiyaNesReborn bootstrap contract

## Purpose

A fresh session must be able to recover the project without chat history, preserve the separation between `ORIGINAL SPEC` and `REBORN`, execute one authoritative `NEXT`, and verify the result before promotion.

## Authority hierarchy

1. `docs/PROJECT_STATE.md` — single operational source of truth for current checkpoint, open frontier and `NEXT`.
2. `docs/WORK_PROTOCOL.md` — permanent research/development method.
3. `docs/VERIFY.md` — verification verdicts, minimum promotion gates and Oracle-protection rules.
4. `docs/RECOVERY_MANIFEST.json` — machine-readable bootstrap pointers only; it intentionally does not duplicate dynamic milestone state.
5. subsystem/reverse-engineering documents — evidence and local technical history.
6. Drive private manifest/evidence index — private ROM/evidence inventory, never an independent `NEXT`.
7. chat history — non-authoritative context.

## Required fresh-context sequence

1. Start from a refreshed `main` and read `docs/RECOVERY_MANIFEST.json`.
2. Read `docs/PROJECT_STATE.md` and reconcile it with newer merged history if necessary.
3. Read the relevant sections of `docs/WORK_PROTOCOL.md`.
4. Read `docs/VERIFY.md` before changing or promoting a verified surface.
5. Load only the subsystem documents named by the current state.
6. Verify the canonical ROM identity before any ROM-dependent extraction, trace, render or fixture generation.
7. Execute only the current `NEXT` until material progress, a verified closure or a concrete blocker is produced.

## Non-negotiable rules

- Never commit ROMs, complete PRG/CHR dumps, protected manuals or extracted original graphics/audio as distributable repository assets.
- `ORIGINAL SPEC` describes what the original does; `REBORN` design decisions never rewrite original evidence.
- Do not promote names, meanings or models beyond their evidence level.
- Closed gates reopen only for contradictory evidence or a failing regression.
- Maximum two repeats of the same no-progress strategy without new evidence.
- A green build is not enough: use the applicable behavioral/semantic gates in `docs/VERIFY.md`.
- Expected fixtures, canonical state transitions and other Oracle material must not be edited merely to make a candidate pass.

## Handoff discipline

Every material checkpoint should leave `DONE / EVIDENCE / OPEN / NEXT / BLOCKERS / ANTI-LOOP` coherent in `docs/PROJECT_STATE.md` and report explicit verification verdicts. Do not duplicate the live milestone in this file.

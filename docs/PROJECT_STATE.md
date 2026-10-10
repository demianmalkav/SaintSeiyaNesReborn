# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

Technical subsystem documents remain authoritative for detailed evidence and semantics. This file records the accepted checkpoint, open boundaries and one executable `NEXT`.

## CURRENT

- Phase: `ORIGINAL SPEC / global engine-state reachability closure`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#119` — exact global engine state `$50` front-end/title modal shell, executable CHR-to-RAM overlays, password bridge, and semantic correction of `$30-$4D` as the front-end attract/presentation loop.
- Merge commit: `8e63d2467a878eaef24e94d07b3240e7f16b9e37`
- Exact final PR head: `a93e175db2098b658606b0cdde69bc9a94e78554`
- Verification gate on that exact head:
  - `ORIGINAL SPEC tests` run `#308`: `SUCCESS`
  - `Original Spec` run `#502`: `SUCCESS`
  - build: `SUCCESS`, 0 errors
  - OriginalSpec self-test: `SUCCESS`
  - password compatibility fixture: `SUCCESS`
- PR `#117` remains structurally authoritative for the `$30-$4D` transition graph, but its broad `scene/battle` semantic label is superseded by PR #119: that graph is the front-end attract/presentation loop.
- PR `#115` closed `$91-$99`.
- PR `#113` closed fatal resource state `$60` and reload `$04=$FF`.
- PR `#111` closed `$11-$14`.
- PR `#109` closed the top-level fixed-bank bootstrap/main/NMI dispatcher partition.
- PRs `#94/#96/#98/#102/#104/#107` remain authoritative for platform exits, narrative reloads, selector and normal reload destinations.

## DONE

### Global machine families already closed

The following reachable engine-state regions now have promoted semantic models and fixtures:

```text
$00 bootstrap -> platform $20
$10 bootstrap -> $11-$14
$20 platform/gameplay
$30,$31-$38,$40-$4D front-end attract/presentation loop
$50 front-end/title modal shell
$60 fatal platform resource failure
$70-$89 promoted narrative/post-exit paths
$90 bootstrap -> $91-$99
$3D reload bridge
```

Known bootstrap/reload successors remain:

```text
reload $00 -> $20
reload $10 -> $11
reload $90 -> $91
```

### Front-end/title modal shell `$50` — PR #119

Cold reset and completed attract playback enter paired global `$50` at `$0200=$00`.

Reachable modal set is exactly:

```text
$0200 = $00-$09
```

Normal front-end progression:

```text
$50:$00 -> $01 -> $02 -> $03 -> $04 -> $05
```

Newly pressed Start redirects intro substates `$00-$04` to ready step `$05` through bank-0 `$8857`.

Ready step `$05` rewrites `$0202` every observing NMI from newly pressed directional edges:

```text
Up/default -> $0202=0
Down       -> $0202=1
```

`$0202` is therefore a one-sample branch selector, not a persistent cursor.

The complete control split is:

```text
$05 --scripted front-end event--> $06
$06 --$0201=1/commit-----------> paired global $30
                                     |
                                     v
                              $31 ... $4D
                                     |
                                     +----> $50:$00

$05 --Start--> $07
                |
                +-- $0202=0 --> paired global $10
                |
                +-- $0202=1 --> password entry $09
                                     |
                                invalid -> $09
                                valid   -> $08
                                           |
                                           +--> restored paired global $10
```

Start during `$30-$4D` writes paired `$50` and enters `$0200=$05`, returning directly to the ready front-end state.

### Semantic correction of PR #117

PR #117's transition evidence remains confirmed:

```text
$30 -> $31 -> $32 -> $33 -> $34 -> $35 -> $36 -> $37 -> $38
 -> $40 -> $41 -> ... -> $4D -> $50
```

Reachable set remains exactly:

```text
$30, $31-$38, $40-$4D
```

and `$39-$3F/$4E-$4F` remain without canonical producers.

PR #119 proves this is the **front-end attract/presentation loop**, not the ordinary boss-battle scaffold. `ENGINE_STATE_50_FRONTEND.md` supersedes only the old semantic label, not the verified writers/thresholds.

### Executable CHR-to-RAM overlays — PR #119

Bank-0 `$8000/$8013` expose a previously hidden original-engine mechanism:

1. select MMC1 4 KiB CHR bank `$1F` (31);
2. read CHR pattern bytes through `$2006/$2007`;
3. copy `$03C0` bytes into CPU RAM `$0440-$07FF`;
4. `JMP $0440` and execute the copied 6502 code;
5. overlay `RTS` returns to the original fixed-bank caller.

Two confirmed sources:

```text
PPU $1000-$13BF -> front-end initialization overlay
PPU $1400-$17BF -> password-entry overlay
```

The password overlay explicitly writes `$0200=$09`, closing the previously missing producer of the password editor state.

This means CHR ROM is not graphics-only storage in the original; at least one bank also stores executable overlays.

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/FrontEndState50Machine.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/FrontEndState50MachineChecks.cs`
- `docs/reverse-engineering/ENGINE_STATE_50_FRONTEND.md`
- PR `#119`

Do not reopen `$50` or the `$30-$4D` structural graph absent contradictory ROM evidence or fixture failure.

## EVIDENCE FOR NEXT

### Why global reachability closure is selected now

The major functional top-level families are now understood. The remaining uncertainty in the **global engine state machine itself** is no longer a large known subsystem; it is the set of numeric ranges recognized by the dispatcher for which reachability was never proved or disproved.

PR #109 deliberately left these structural ranges as unknown where no producer had yet been established, including examples such as:

```text
$01-$0F outside proved bootstrap/use contexts
$15-$1F
$21-$2F
$51-$5F
unused members of other high-nibble families
$9A+ and other common-tail values
```

Several ranges have since been partially or completely eliminated by later checkpoints:

- `$39-$3F` and `$4E-$4F`: no canonical producer after PR #117/#119;
- `$61-$6F`: no reachable producer from the fatal resource graph; only `$60` is used there;
- `$50-$5F`: exact `$50` is now closed, while `$51-$5F` still require an explicit global reachability verdict rather than assumption.

A fresh raw writer audit against the canonical ROM found the expected executable immediate writers already owned by promoted families (`$12/$20/$3D/$40/$50/$60/$70/$80/$10`). Apparent extra `INC/DEC $00` hits at bank-5 `$9FF9` and bank-6 `$A489` resolve as inline-table/data false positives, demonstrating why the next pass must be **executable-control-flow based**, not byte-pattern based.

The newly discovered CHR-backed RAM overlays also change the audit rule: a complete producer census must include code loaded from CHR into RAM, not only the eight PRG banks.

Closing this census will let us say that the top-level engine state machine is fully enumerated before moving into renderer/RNG/audio/boss-context completeness.

## OPEN

1. Enumerate every executable writer to global `$00/$01` across fixed PRG, switchable PRG, and confirmed CHR-backed RAM overlays.
2. Reconcile every writer with the already-promoted families and reject inline data/table false positives.
3. For each remaining dispatcher-recognized numeric state/range, classify it as:
   - `REACHABLE`;
   - `BOOTSTRAP/TRANSIENT`;
   - `STRUCTURAL BUT UNREACHABLE`;
   - or `OPEN` only if an executable producer remains genuinely unresolved.
4. Explicitly close residual ranges such as `$15-$1F`, `$21-$2F`, `$51-$5F`, and high common-tail values rather than inferring deadness from absence in gameplay traces.
5. Determine whether any executable CHR overlay beyond the two state-$50 overlays writes `$00/$01` or creates a previously invisible global state.
6. Produce one global reachability artifact/fixture set that composes all promoted families without duplicating their internal semantics.
7. Only after the global state namespace is closed, select the next ORIGINAL SPEC subsystem using the integral-assimilation criterion: boss-context completeness, renderer/CHR, RNG, audio, or text runtime/localization.

## NEXT

**Close the global engine-state reachability census: prove every remaining `$00/$01` state value recognized by the dispatcher reachable, transient, or unreachable, including executable CHR/RAM overlays.**

Completion criterion:

> Starting from the verified dispatcher map and all promoted state-family checkpoints, enumerate every executable producer/advancer of global `$00/$01`, include PRG and executable CHR-to-RAM code, reject data/inline-table false positives, and leave no dispatcher-recognized numeric range with ambiguous reachability unless a concrete unresolved executable writer is documented.

Required sequence:

1. build an exhaustive candidate writer index for `STA/STX/STY/INC/DEC $00/$01` plus indirect/overlay equivalents;
2. prove executable reachability of each candidate from known roots and mark data/inline tables false;
3. map each real writer into the existing promoted family graph;
4. classify all residual state values/ranges, with explicit fixtures for representative dead gaps;
5. scan confirmed executable CHR/RAM overlays for global-state writers and inventory any additional overlay entrypoints found;
6. implement a compact `EngineStateReachability` artifact + discriminating fixtures;
7. document the final top-level state namespace and run both verification workflows.

## BLOCKERS

- None. Canonical ROM, global dispatcher map, promoted family models, exact state `$50`, and CHR-overlay mechanism are available.

## RECOVERY CONTRACT

A new session must be able to resume without chat history.

Recovery order:

1. read this file from `main`;
2. reconcile `CURRENT` with newer merged Git history if any exists;
3. inspect `ENGINE_STATE_DISPATCHER.md` and `ENGINE_STATE_50_FRONTEND.md`;
4. use existing family-specific documents only to classify already-owned writers, not to reopen their internals;
5. scan canonical ROM/overlays only for global-state reachability evidence required by this boundary;
6. use `docs/REVERSE_ENGINEERING_STATUS.md` as navigation only;
7. use `docs/WORK_PROTOCOL.md` for execution rules;
8. use Drive only to locate private ROM/evidence; Drive never owns a separate `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `global-engine-state-reachability-census`
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

# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

## CURRENT

- Phase: `ORIGINAL SPEC / global pseudo-random source — $E0AC / $065F-$0660`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#161` — exhaustive global NMI presentation coverage.
- Merge commit: `96fee8a49b81dfc85818dd7dd1603f0cfdc3af4f`
- Exact final PR head: `6158aef4700c3c986516528c9410bf8950eae5d2`
- Verification on that exact head:
  - `ORIGINAL SPEC tests` #412: `SUCCESS`
  - `Original Spec` #617: `SUCCESS`
  - build/self-test/password compatibility: `SUCCESS`
- Canonical ROM reverified before analysis: size `262160`, SHA-1 `F871D9B3DAFDDCDAD5F2ACD71044292E5169064E`, MD5 `3B0F17C2B6EFC928B3D3FE9B1A389680`, SHA-256 `6917B31D7343A9A17170E833BACDBC3B1EBA3E02D11C51C0D44DBE436C9AD43A`, CRC32 `F8D258A3`.
- Global engine-state namespace, platform presentation chain, front-end/modal/attract presentation, narrative text families, high `$91-$99` presentation and global NMI coverage are frozen.

## DONE

### Global NMI presentation coverage — PR #161

The fixed-bank NMI dispatcher rooted at `$D269` is now exhaustively composed with the already-closed global `$00/$01` reachability census.

Canonical namespace:

```text
produced global-state values : 59
no-producer byte values      : 197
canonical NMI route classes  : 16
unclassified produced states : 0
material presentation gaps   : 0
```

The two mirror-first routes remain semantically prior to live `$00`:

```text
$01=$50 -> $DABC  front-end/title modal NMI
$01=$3D -> $E000  cooperative reload NMI
```

All remaining routes use live `$00`.

#### Oracle correction found during the audit

Direct canonical ROM flow is:

```text
$D297 LDA $00
$D299 BNE $D29E
$D29B JMP $D382
```

Therefore live state `$00=$00` does **not** enter ordinary common epilogue at `$D367`. It enters at `$D382`, skipping `$D367-$D381` (the ordinary `$77->$2000` / `$78->$2001` commit) while retaining scroll/status wait, persistent `$3B` restoration and RTI.

This correction was isolated in commit:

```text
7f90041d4416021a07b7dd9017a729ec48a352af
```

before the new coverage manifest was added. Do not restore the older `$00->$D367` expectation without contradictory ROM evidence.

#### Canonical presentation ownership

```text
$00                    -> $D382 tail entry
$10/$30/$90            -> common $D367 tail
$3D                    -> mirror $E000 reload
$11/$14                -> common tail
$12                    -> $D543 -> $13
$13                    -> $D42D generated-$0600 stream
$20                    -> $D7F2 / $D988 platform NMI
$31-$33/$35-$38        -> common tail
$34                    -> $D73B
$40-$4D                -> bank1 $8C19 attract text/presentation
$50                    -> mirror $DABC modal dispatcher
$60                    -> bank1 $9D69 HUD/status
$70                    -> $D3BF (terminal setup reaches $D73B)
$71-$72/$74-$75        -> common tail
$73                    -> bank1 $8C19 post-exit text
$80-$89                -> bank1 $8C19 narrative text
$91                    -> $D42D
$92/$97/$99            -> common tail
$93                    -> $D55E
$94-$96                -> $D571 -> $D55E
$98                    -> $D53D / $D511 -> $99
```

The shared `$8C19` renderer is already bounded by three closed contexts: front-end attract `$40-$4D`, post-exit `$73`, and narrative `$80-$89`. Original text/tile payloads remain private/content-layer data rather than a missing state/presentation contract.

Mapper ownership is also frozen:

```text
$40-$4D / $73 / $80-$89:
  dispatcher raw bank1 -> $8C19 -> raw bank3

$60:
  dispatcher raw bank1 -> $9D69
  no immediate bank3 write
  common $D393-$D395 restores persistent $3B

$D42D / $20 / $DABC / $E000:
  handler-owned mapper transactions
```

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/NmiPresentationCoverage.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/NmiPresentationCoverageChecks.cs`
- `docs/reverse-engineering/NMI_PRESENTATION_COVERAGE.md`
- corrected `EngineStateDispatcherMap` + fixture
- PR #161

The global semantic presentation surface is therefore closed. Do not create another renderer checkpoint merely from structurally dispatchable but canonically unreachable byte values.

## EVIDENCE FOR NEXT

With presentation closed, the next bounded global subsystem is the game's pseudo-random/phase source.

Direct canonical fixed-bank evidence exposes the updater at `$E0AC`:

```text
$E0AC LDX $0660
$E0AF LDA $94F0,X
$E0B2 CLC
$E0B3 ADC $065F
$E0B6 STA $065F
$E0B9 INC $0660
$E0BC RTS
```

At minimum this proves the byte recurrence:

```text
$065F' = ($065F + visible_prg[$94F0 + $0660]) & $FF
$0660' = ($0660 + 1) & $FF
```

The unresolved qualifier is important: `$94F0` lies in the switchable `$8000-$BFFF` PRG window. The audit must prove which PRG bank is visible at every canonical `$E0AC` invocation before treating `$94F0-$95EF` as one fixed RNG table.

Existing consumer reconnaissance already proves the source is gameplay-relevant:

```text
$E33D  LDA $065F ; AND #$07 -> $05AC
$EC18/$EC20/$EC2B  LDA $065F ; AND #$01 / #$03
$F65D/$F665        LDA $065F ; AND #$01 / #$03 -> $0531 path
$FAC9/$FAD7        LDA $065F ; AND #$0F ; threshold comparisons -> $06BC
bank6 $913E/$92E2  direct $065F consumers

$F995  LDA $0660 ; AND #$01 -> dodge danger-direction parity
```

`BOSS_DODGE.md` had already left open what drives `$0660`; this updater supplies that missing source relation.

A raw direct-writer pass found no other direct writer of `$0660` beyond `INC $0660` at `$E0B9`, and no other direct writer of `$065F` beyond `$E0B6`; initialization/reset ownership still needs executable proof rather than assumption.

## OPEN

1. Re-disassemble `$E0AC` in its full caller/NMI context and enumerate every canonical invocation.
2. Prove the PRG bank visible at `$94F0,X` for each invocation; determine whether the source bytes are one stable 256-byte table or bank-context-dependent ROM data.
3. Prove initialization/reset ownership for `$065F/$0660`, natural byte wrap behavior, and whether either field is seeded indirectly.
4. Enumerate executable consumers of `$065F/$0660` and reject raw data/operand false positives.
5. Classify each real consumer by gameplay owner (battle selection, dodge parity, stage/event choice, etc.) without reopening the already-closed downstream mechanics.
6. Distinguish this source from deterministic frame counters such as `$3C`; do not label unrelated counters as RNG.
7. Promote a clean-room `CanonicalRandomSourceE0AC` (or equivalently scoped model) with deterministic sequence/bank-context fixtures and consumer range/parity invariants.
8. Stop after the pseudo-random source/callsite contract. Do not absorb the audio driver or redesign probability distributions.

## NEXT

**Close the canonical pseudo-random/phase source rooted at fixed `$E0AC`: prove the complete `$065F/$0660` recurrence and initialization, resolve the switchable-PRG ownership of `$94F0,X` at every reachable update, inventory every executable consumer and its mask/range semantics, and promote a deterministic clean-room source/callsite contract with fixtures without reopening the already-closed battle/platform behaviors that consume it.**

Completion criterion:

> Given a canonical RNG state `$065F/$0660` and the PRG-bank context visible to `$E0AC`, the model must deterministically reproduce the next source state, byte wrapping and every reachable consumer's derived selector/parity range. Every executable writer/update and consumer of `$065F/$0660` must be classified, with raw false positives rejected and zero unresolved bank-context assumptions. Downstream battle/platform semantics remain composed rather than duplicated.

## BLOCKERS

- None. Canonical ROM, mapper model, global NMI/presentation closure, battle consumers and clean-room test harness are available.

## RECOVERY CONTRACT

1. Refresh `main`, then read this file before executing `NEXT`.
2. Freeze PR #161 global NMI coverage, PR #159 HUD, PR #156 palette/CHR, PR #154 state-`$20` NMI, PR #151 visual resources and PR #149 battle-stage closure.
3. Preserve Oracle correction `$00 NMI -> $D382` from commit `7f90041d...`.
4. Treat `$94F0` as bank-sensitive until callsite evidence proves otherwise.
5. Do not infer randomness from a masked counter merely because it looks random; classify updater and consumers mechanically.
6. Do not commit ROM, extracted copyrighted tables/payloads, traces or captures unless private evidence is materially required.
7. Drive is private evidence storage only and never owns an independent `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `canonical-random-source-e0ac-065f-0660`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, evidence, a discarded hypothesis or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.

# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

## CURRENT

- Phase: `ORIGINAL SPEC / audio scheduler architecture — $DB9C/$DBB6/$0440+`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#163` — canonical pseudo-random/phase source `$E0AC`, `$065F/$0660`.
- Merge commit: `b89a5fb7d86e7b7b322cca1ea13a416ed9ca050f`
- Exact final PR head: `cb003c01db8370d1195d1e8a527dfad4a40f68da`
- Verification on that exact head:
  - `ORIGINAL SPEC tests` #416: `SUCCESS`
  - `Original Spec` #620: `SUCCESS`
  - build/self-test/password compatibility: `SUCCESS`
- Canonical ROM reverified before analysis: size `262160`, SHA-1 `F871D9B3DAFDDCDAD5F2ACD71044292E5169064E`, MD5 `3B0F17C2B6EFC928B3D3FE9B1A389680`, SHA-256 `6917B31D7343A9A17170E833BACDBC3B1EBA3E02D11C51C0D44DBE436C9AD43A`, CRC32 `F8D258A3`.
- Global presentation, platform frame/rendering, battle/event coverage and canonical RNG source are frozen.

## DONE

### Canonical pseudo-random / phase source — PR #163

The source rooted at fixed `$E0AC` is now mechanically closed.

#### Invocation and recurrence

There is one executable caller:

```text
$E09C JSR $E0AC
```

inside mirror-state `$01=$3D -> $E000` NMI service. The update runs only when:

```text
$9C != 0
($9D | $9E | $A0) == 0
```

The exact recurrence is:

```text
source = visible_prg[$94F0 + $0660]
$065F' = ($065F + source) & $FF
$0660' = ($0660 + 1) & $FF
```

`$0660` is therefore an update counter modulo 256, not a generic frame counter.

#### Bank-sensitive `$94F0` ownership

`$94F0-$95EF` lies in switchable `$8000-$BFFF`. `$E0AC` runs before `$E0BD` restores persistent battle/reload bank `$0639`.

When mapper service is available (`$063E!=$04` and `$063F!=$04`), the last serviced queue owns the visible bank at `$E09C`:

```text
$0641 != 0 -> bank 0 when $068F==$8F, else bank 6
$0526 != 0 -> bank 6
$0538 != 0 -> bank 5
$057D != 0 -> bank 6
```

Later queue wins. If mapper service is blocked, the update consumes the bank already committed/visible on NMI entry.

The seven physical switchable-bank `$94F0-$95EF` windows have distinct hashes; a single fixed RNG table would therefore be wrong.

#### Reset / seed ownership

Canonical state owners now frozen:

```text
cold RESET $C13D+                  -> (00,00)
bank0 $AD4A clear + $0648 fill    -> (01,01)
bank1 $959D page clear            -> (00,00)
bank0 $AF0D page clear            -> (00,00)
```

`$B38A STA $0648,Y` also has two canonical alias offsets:

```text
Y=$17 -> $065F
Y=$18 -> $0660
```

so indirect bank-0 seeding is real and is not discarded as a false positive.

#### Consumer contract

Executable `$065F` consumers are frozen at:

```text
$E33D             mask $07
$EC18/$EC20       mask $01, second half +2
$EC2B             mask $03
$F65D/$F665       mask $01 / $03
$FAC9/$FAD7       mask $0F with thresholds $05 / $06
bank6 $913E       mask $01
bank6 $92E2       mask $03
```

`$0660` has one downstream executable consumer outside the updater:

```text
$F995: even -> $FF, odd -> $01
```

which closes the previously open parity source used by the already-closed dodge direction path.

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/CanonicalRandomSourceE0AC.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/CanonicalRandomSourceE0ACChecks.cs`
- `tools/reverse/audit_canonical_random_source.py`
- `docs/reverse-engineering/CANONICAL_RANDOM_SOURCE_E0AC.md`
- PR #163

No original `$94F0` table payload is versioned.

Do not reopen the RNG source without contradictory canonical-ROM evidence or a failing fixture.

## EVIDENCE FOR NEXT

The next bounded global subsystem is audio. Start with architecture, not full song/SFX reconstruction.

Direct fixed-bank evidence:

```text
$DB9C:
  $4015=0
  $04F0=0
  $04EF=0
  eight records at $0440 + $15*N, N=0..7
  record +0 initialized to $FF

$DBB6:
  input A selects descriptor at $DC0E + 4*A
  descriptor byte0 selects a $15-byte slot offset
  descriptor bytes1-3 load slot +1/+2/+3
  slot +0 becomes 0 (active)
  existing slot ownership can update $04F0 through mask table $DC0A
```

This proves an eight-slot scheduler/voice-command layer, but the per-frame updater, exact slot-field meanings, `$04EF/$04F0` ownership and final APU register routing remain unclosed.

Known callsites already depend on this boundary (for example pause cue `$63` through `$DBB6`), so scheduler semantics should be closed before attempting full music data or soundtrack reproduction.

## OPEN

1. Re-disassemble `$DB9C/$DBB6` and every executable caller; freeze cue/descriptor reachability without exporting original music/SFX payloads.
2. Identify the routine(s) that iterate `$0440 + $15*N` and classify the eight slots, lifecycle byte at `+0`, descriptor fields `+1/+2/+3`, and any shared fields used during playback.
3. Prove `$04EF/$04F0` semantics and how slot replacement/preemption modifies them.
4. Enumerate all direct APU writers `$4000-$4015` and assign each to initialization, per-frame synthesis, DMC, or unrelated hardware setup.
5. Prove the scheduler-to-APU ownership chain and update cadence; distinguish music versus SFX only where callsite/table evidence supports it.
6. Promote a clean-room `CanonicalAudioScheduler` (or equivalently scoped model) with reset/load/preemption/update fixtures.
7. Stop after scheduler/voice/APU ownership. Do not yet reproduce full note streams, instrument envelopes, original music data or audio assets.

## NEXT

**Close the canonical audio scheduler architecture rooted at `$DB9C/$DBB6` and the eight `$0440+$15*N` records: prove cue-descriptor loading, slot lifecycle/preemption and `$04EF/$04F0` ownership, identify the per-frame slot updater and its APU `$4000-$4015` write ownership, and promote an executable clean-room scheduler contract with fixtures without absorbing full song/SFX payload reconstruction.**

Completion criterion:

> Given reset state, a cue ID accepted by `$DBB6`, current eight-slot state and one scheduler update, the model must deterministically reproduce slot selection/initialization, lifecycle/preemption state and the semantic APU-write operations owned by the scheduler. Every executable owner of `$0440-$04EF/$04F0` and `$4000-$4015` relevant to this scheduler boundary must be classified, while original note/music/SFX payload bytes remain outside GitHub.

## BLOCKERS

- None. Canonical ROM, NMI architecture, cue callsites, clean-room harness and mapper/audio-adjacent fixed routines are available.

## RECOVERY CONTRACT

1. Refresh `main`, then read this file before executing `NEXT`.
2. Freeze PR #163 RNG, PR #161 global NMI coverage, PR #159 HUD, PR #156 palette/CHR, PR #154 state-$20 NMI, PR #151 visual resources and PR #149 battle-stage closure.
3. Treat `$0440` as an eight-slot audio scheduler boundary because `$DB9C/$DBB6` prove record structure and `$4015` ownership; do not infer field names beyond evidence.
4. Preserve original audio content separation: semantic scheduler/state belongs in GitHub; copyrighted note/instrument payloads do not.
5. Do not merge scheduler architecture with full soundtrack reconstruction in one checkpoint.
6. Drive remains private evidence storage only and never owns an independent `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `audio-scheduler-db9c-dbb6-0440`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, evidence, a discarded hypothesis or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.

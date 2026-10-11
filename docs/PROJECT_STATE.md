# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

## CURRENT

- Phase: `ORIGINAL SPEC / platform visual-refresh palette-CHR closure — $9915/$9EEF`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#154` — canonical platform `$00=$20` NMI presentation boundary.
- Merge commit: `52886987465b9455304f1fd735733650891dfa27`
- Exact final PR head: `fe272f74366eb15b8510a4a1d1032db580f7518a`
- Verification on that exact head:
  - `ORIGINAL SPEC tests` #393: `SUCCESS`
  - `Original Spec` #601: `SUCCESS`
  - build/self-test/password compatibility: `SUCCESS`
- Canonical ROM reverified before analysis: size `262160`, SHA-1 `F871D9B3DAFDDCDAD5F2ACD71044292E5169064E`, SHA-256 `6917B31D7343A9A17170E833BACDBC3B1EBA3E02D11C51C0D44DBE436C9AD43A`.
- Battle/event coverage, map kits, CHR routing, mechanical sprite definitions and platform state-$20 NMI boundary are frozen.

## DONE

### Platform `$00=$20` NMI presentation — PR #154

Canonical fixed-bank flow:

```text
$C000 -> $D269 NMI
$D2BA JSR $D7F2
$D2BD JSR $D988
$D2C0 JMP $D367
```

#### Prologue / OAM boundary

`$D269+` pushes A/X/Y, writes `$3A=1`, resets the MMC1 serial latch, reads `$2002`, writes `$2003=0`, then `$4014=$07`.

Therefore the current NMI DMA snapshot is exactly the already-built `$0700-$07FF` OAM shadow **before** any state-$20-specific work. Main-thread platform simulation remains a separate phase and was not duplicated inside NMI.

#### `$D7F2` reclassified: platform background streamer

Two routes are now frozen.

Refresh route:

```text
($44 & $06) != 0:
  $03A3=0
  raw PRG bank 1
  JSR $9915
  raw PRG bank 3

($44 & $06)==0 and $03A3!=0:
  same bank1 $9915 refresh
```

New-column route:

```text
($44 & $06)==0 and $03A3==0:
  $03A3=$FF
  $2000 = $77 | $04        ; temporary increment-by-32
  address high = $20 + ((($45+1)&1)<<2)
  address low  = $44 >> 3
  $0362-$0377 -> $2007     ; exactly 22 tile bytes
```

`$D844` adds the eight-byte attribute column only when `($44 & $1E)==0`:

```text
$2000 = $77
attribute high = $23 + ((($45+1)&1)<<2)
attribute low0 = $C0 + ($44>>5)
$0378-$037F -> low0 + 8*N, N=0..7
```

The bank switches around `$9915` use raw `$C0B4` and do not steal persistent `$3B` ownership.

#### `$D988` reclassified: NMI-owned platform pause toggle

Start bit `$10` in `$3D` is edge-gated by `$05`.

```text
Start released -> $05=0
fresh Start edge -> $05=$FF; JSR $CB6A; toggle $0386
held Start with $05!=0 -> no retrigger
```

Main-thread `$C302-$C305` proves `$0386` is the pause gate: nonzero skips the normal platform simulation.

Entering pause from `$0386=0` additionally executes `$DB9C` and loads audio cue `$63` through `$DBB6`, then stores `$0386=$FF`. Leaving pause stores `$0386=0` without the pause-entry reset/cue path.

Canonical fixed byte:

```text
$FFDE=$00
```

therefore `$D9BA+` resource/debug mutation code is unreachable normal gameplay and is not promoted.

#### Common `$D367+` presentation commit

```text
$77 = ($77 & $FE) | ($45 & 1)
$2000 = $77
$2001 = $78
$2005 = $44
$2005 = $46
```

The status loop uses A=`$40` and waits while PPUSTATUS bit 6 is set, i.e. until sprite-zero-hit clears.

Finally `$3B` is passed to raw `$C0B4`, restoring the persistent PRG bank, then Y/X/A are restored and `RTI` executes.

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformState20NmiPhase.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/PlatformState20NmiPhaseChecks.cs`
- `docs/reverse-engineering/PLATFORM_STATE20_NMI.md`
- PR #154

Do not reopen this state-$20 boundary without contradictory ROM evidence or a failing fixture.

## EVIDENCE FOR NEXT

The contiguous unresolved platform-presentation owner exposed by the completed NMI work is bank-1 `$9915` and its palette/CHR helper chain.

Direct canonical-ROM evidence already established during PR #154:

```text
$9915:
  tests $07C0 and $03A4
  chooses pointer state using platform substate $02
  calls $9EEF
  then performs a raw MMC1 CHR0 serial write at $BFFF

$9EEF:
  writes $2000=0 / $2001=0
  targets palette address $3F10
  consumes pointer pairs $0392-$0399 through $9F29
  then calls $9D58
```

The exact semantic ownership of `$0392-$0399`, `$03A4`, `$07C0`, the palette transfer record format in `$9F29+`, the `$9D58` tail and the selected CHR0 values must be proven before declaring platform palette/frame presentation globally closed.

This is narrower and more contiguous than jumping to unrelated RNG/audio work.

## OPEN

1. Re-disassemble canonical bank-1 `$9915-$9960`, `$9EEF+`, `$9F29+` and `$9D58` far enough to close their reachable call graph.
2. Classify the gates `$07C0/$03A4/$02` and distinguish palette-refresh ownership from CHR0 animation/bank ownership.
3. Decode the four pointer pairs `$0392-$0399` and exact palette bytes/lengths transferred through `$9F29` without embedding extracted palette payloads in source.
4. Freeze the raw MMC1 CHR0 serial write selected by `$9966+` for reachable platform substates, including whether it duplicates or transiently overrides the already-closed static CHR map.
5. Model a clean-room `PlatformVisualRefresh9915` (or equivalently scoped phase) with discriminating fixtures for gate behavior, palette transfer descriptors, CHR0 selection and restoration/ownership.
6. Reconcile `PLATFORM_CHR_MAP.md` and NMI documentation if the dynamic `$9915` path refines the static CHR interpretation.
7. Stop after the platform palette/CHR refresh path. Do not absorb the full audio engine, RNG or every non-platform NMI state.

## NEXT

**Close the platform bank-1 visual-refresh path rooted at `$9915`: prove its `$07C0/$03A4/$02` gates, decode `$9EEF/$9F29/$9D58` palette-transfer ownership, freeze the dynamic CHR0 serial-write selection at `$9966+`, and promote an executable clean-room phase with fixtures that composes with the already-closed state-$20 NMI without reopening main-thread rendering.**

Completion criterion:

> Given the platform NMI's decision to invoke `$9915`, the model must deterministically state whether refresh work occurs, what palette-transfer descriptors are consumed, which PPU palette region is committed, which CHR0 bank value is selected for each reachable platform substate, and what persistent/transient state is mutated. The result must be supported by canonical ROM addresses and executable fixtures, with extracted original palette/CHR data remaining outside GitHub.

## BLOCKERS

- None. Canonical ROM, state-$20 NMI callsite, static CHR map, mapper helpers and clean-room test harness are available.

## RECOVERY CONTRACT

1. Read this file from `main` and reconcile it with newer merged history before executing `NEXT`.
2. Freeze PR #154 state-$20 NMI, PR #151 visual-resource closure and PR #149 battle-stage closure.
3. Treat direct canonical-ROM bytes as authority over historical labels.
4. Keep `$9915` refresh semantics separate from main-thread entity simulation and from full audio semantics.
5. Do not commit ROM, extracted CHR, palette dumps or gameplay captures.
6. Drive remains private ROM/evidence storage and never owns an independent `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `platform-visual-refresh-9915-palette-chr0`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, evidence, a discarded hypothesis or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.

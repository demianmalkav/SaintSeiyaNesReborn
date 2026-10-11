# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

## CURRENT

- Phase: `ORIGINAL SPEC / runtime text-content integration`
- State: `READY_FOR_NEXT`
- Closing checkpoint in this PR: `#165` — canonical audio scheduler `$DB9C/$DBB6/$0440+`.
- Pre-state-update verified PR head: `10482ab54cefaca61adbeaf14102176a3e87f5b6`.
- Verification on that exact head:
  - `Original Spec` #624: `SUCCESS`
  - `ORIGINAL SPEC tests` #420: `SUCCESS`
- Canonical ROM reverified before analysis: size `262160`, SHA-1 `F871D9B3DAFDDCDAD5F2ACD71044292E5169064E`, MD5 `3B0F17C2B6EFC928B3D3FE9B1A389680`, SHA-256 `6917B31D7343A9A17170E833BACDBC3B1EBA3E02D11C51C0D44DBE436C9AD43A`, CRC32 `F8D258A3`.
- Global presentation, platform frame/rendering, battle/event coverage, canonical RNG and audio scheduler architecture are frozen after their verified checkpoints.

## DONE

### Canonical audio scheduler architecture — PR #165

The bounded scheduler/voice/APU ownership layer rooted at fixed `$DB9C/$DBB6` and bank-0 `$8B50+` is mechanically closed without versioning original song/SFX payloads.

#### Reset and slot layout

`$DB9C` proves eight records at:

```text
$0440 + $15*N, N=0..7
```

Reset semantics:

```text
$4015 = 00
$04F0 = 00
$04EF = 00
slot[+0] = FF for all eight records
```

Therefore `+0 == $FF` is inactive.

#### Cue loading and preemption

`$DBB6` maps input cue ID `A` to a four-byte descriptor at `$DC0E + 4*A`:

```text
descriptor[0] -> slot offset
descriptor[1] -> slot +1
descriptor[2] -> slot +2
descriptor[3] -> slot +3
slot +0       -> 00
```

The low two bits of slot `+1` select channel class `0..3` -> pulse1, pulse2, triangle, noise.

If the selected record was already active, the old record channel selects mask `$0E/$0D/$0B/$07` at `$DC0A`; that mask clears the old channel bit from `$04F0` before the new descriptor is installed.

#### `$04F0` and `$04EF`

`$04F0` is the software shadow of the low four `$4015` channel-enable bits.

Owners are frozen at:

```text
$DB9C          reset shadow
$DBB6          clear old owner on record replacement
$8DB0-$8DBC    OR channel bit, store shadow, write $4015
$8DFD-$8E09    AND clear mask, store shadow, write $4015
```

`$04EF` is not APU state. Bank-0 stream command `$A5` stores an operand to `$04EF`; fixed dispatcher `$DB40+` consumes nonzero `$04EF` by invoking visual-buffer routines `$8904/$8AF1` and then clearing the latch.

#### Per-frame scheduler and arbitration

Bank-0 `$8B50+`:

```text
$04E8 = 0           ; per-frame claimed-channel mask
INC $04EE           ; modulo-256 phase/update byte
iterate 8 records in ascending slot order
channel = slot[+1] & 3
APU base offset = 0,4,8,12
```

`$8E0D` suppresses a later slot when an earlier active slot already claimed the same channel. Therefore the first active slot in ascending record order owns that channel for the frame.

A newly loaded record (`+0==0`) enters the initialization path before normal stream processing.

#### APU ownership

Scheduler-relevant executable writers are frozen as:

```text
$DB9E                 $4015 reset
$8DBC                 $4015 enable-shadow write
$8E06                 $4015 disable-shadow write
$8E4D / $8E9E         $4000 + channel base
$8DCD                 $4001 + channel base
$8DD1 / $8F0E         $4002 + channel base
$8DEA                 $4003 + channel base
```

with channel bases `0,4,8,12`. Cold RESET `$C12C+` separately initializes `$4010/$4015/$4017`; no scheduler-owned DMC `$4010-$4013` path was found.

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/CanonicalAudioScheduler.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/CanonicalAudioSchedulerChecks.cs`
- `tools/reverse/audit_canonical_audio_scheduler.py`
- `docs/reverse-engineering/CANONICAL_AUDIO_SCHEDULER_DB9C_DBB6.md`
- PR #165

The clean-room contract accepts decoded semantic voice-frame operations; original note streams, instruments, envelopes, music and SFX payload bytes remain outside GitHub.

Do not reopen audio scheduler architecture without contradictory canonical-ROM evidence or a failing fixture.

## EVIDENCE FOR NEXT

The remaining material prerequisite before an integral ORIGINAL SPEC audit is runtime text/content integration, not soundtrack payload reconstruction.

Already-confirmed text architecture:

```text
message IDs                   0..250 (251 total)
fixed entrypoints             $E7B3/$E7B7 -> $066A
                              $E7C3/$E7C7 -> $066B
variant/side byte             $0672
pointer table                 bank6 $A47B, 251 words
physical message storage      CHR4K $15 || CHR4K $17
terminator                    $FF
space                         $01
line break                    $A4
dakuten / handakuten          $3B / $3C
```

`tools/reverse/extract_japanese_script.py` already reconstructs the private 251-message Japanese source corpus from the canonical ROM. `docs/LOCALIZATION.md` fixes stable public IDs `MSG_000..MSG_250`, and `tools/localization/validate_catalog.py` validates a private localization catalog without embedding copyrighted script content in the repository.

The missing boundary is runtime-facing integration: a content-independent executable contract that resolves stable message IDs through language selection and presentation metadata while preserving the original two-slot/variant semantics needed by canonical callsites. Full Japanese/Spanish dialogue payloads remain private.

## OPEN

1. Reconcile `TEXT_ENGINE.md`, `LOCALIZATION.md`, extractor/catalog validator and all existing message-context metadata; do not duplicate already-closed extraction work.
2. Freeze the runtime message-request contract around `$066A/$066B/$0672` and the four `$E7Bx` entrypoints, including slot/variant semantics only where evidence supports naming them.
3. Classify message-producing callsites by runtime context sufficiently to bridge canonical `MSG_000..MSG_250` IDs to stable semantic metadata without embedding source dialogue.
4. Promote a clean-room runtime localization interface that accepts an external/private catalog, supports at least `JP` and `ES`, preserves stable IDs, and cleanly separates dialogue payload from game logic.
5. Add fixtures proving deterministic message selection/fallback, slot/variant propagation, catalog coverage `0..250`, and absence of hard-coded copyrighted dialogue in the public contract.
6. Stop after runtime text/content integration. Do not begin REBORN implementation or integral ORIGINAL SPEC closure in the same checkpoint.

## NEXT

**Close runtime text/content integration around canonical `MSG_000..MSG_250`: prove the request semantics of `$E7B3/$E7B7/$E7C3/$E7C7` through `$066A/$066B/$0672`, classify the runtime metadata needed by canonical callsites, and promote a clean-room JP/ES localization contract that loads private external catalogs while keeping all original/translated dialogue payloads out of GitHub.**

Completion criterion:

> Given a canonical message request (stable `MSG_xxx`, message slot and `$0672` variant), selected language and a valid external catalog, the public clean-room contract must deterministically resolve the requested localized entry and propagate the canonical request metadata. It must reject malformed/incomplete catalogs, preserve exact ID coverage `0..250`, provide an explicit fallback policy, and contain no copyrighted dialogue payload. Existing extraction/codec behavior must remain regression-green.

## BLOCKERS

- None. Canonical ROM, text engine reconstruction, 251-message extractor, stable IDs, private-catalog validator and battle-context metadata already exist.

## RECOVERY CONTRACT

1. Refresh `main`, then read this file before executing `NEXT`.
2. Freeze PR #165 audio scheduler architecture and all earlier closed gates unless new contradictory evidence appears.
3. Treat the 251 message IDs as immutable localization identity; semantic aliases may improve, numeric IDs may not change.
4. Keep full JP/ES script content private. Public GitHub receives only engine semantics, schemas/contracts, validators, synthetic fixtures and non-copyrighted metadata.
5. Do not couple localization runtime closure to REBORN UI implementation.
6. Drive remains private evidence/content storage only and never owns an independent `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `runtime-text-content-msg000-250-e7bx`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, evidence, a discarded hypothesis or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.

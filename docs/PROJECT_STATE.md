# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

## CURRENT

- Phase: `ORIGINAL SPEC / platform HUD-status NMI writer — $9D69+`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#156` — bank-1 platform palette/CHR visual-refresh closure rooted at `$9915`.
- Merge commit: `a6f421289fcaf8e2e000282307edb3a7223f95e1`
- Exact final PR head: `a76b084b5e8fc3999931ec618157bcd1a4aca800`
- Verification on that exact head:
  - `ORIGINAL SPEC tests` #401: `SUCCESS`
  - `Original Spec` #609: `SUCCESS`
  - build/self-test/password compatibility: `SUCCESS`
- Canonical ROM reverified before analysis: size `262160`, SHA-1 `F871D9B3DAFDDCDAD5F2ACD71044292E5169064E`, SHA-256 `6917B31D7343A9A17170E833BACDBC3B1EBA3E02D11C51C0D44DBE436C9AD43A`.
- Platform main-thread frame, map kits, static/dynamic CHR routing, metasprite resources, state-`$20` NMI, and palette/CHR refresh are frozen.

## DONE

### Platform visual refresh `$9915/$9EEF` — PR #156

The state-`$20` NMI's bank-1 refresh call is now mechanically closed through its palette/CHR-owned paths.

#### Top gate and one-shot owner

Exact `$9915` gate:

```text
$07C0 == $FE  -> general fallback $996C
$03A4 != $FF  -> general fallback $996C
otherwise      -> one-shot special palette/CHR branch
```

A gate miss does **not** return and is not a no-op.

`$03A4` is a one-shot latch. Platform init clears it to zero. The sole canonical setter at `$9130-$9169` can arm `$FF` only for substates `$0C-$11` on camera page `$45=$0A` after the substate-specific `$44` threshold:

```text
0C A0
0D E0
0E D0
0F D0
10 D0
11 A5
```

Special exclusion:

```text
$02=$10 and internal Saint $03=1 (Shun) -> do not arm
```

Successful special refresh executes `DEC $03A4`, producing `$FE`; the setter requires zero, so it cannot rearm before the next platform reset.

`$07C0` is the first Y byte of the special visual's OAM block and `$FE` is the inactive/hidden sentinel.

#### Sprite palette transfer

`$9EEF` disables presentation registers temporarily, addresses `$3F10`, and consumes four descriptor pointers:

```text
$0392/$0393
$0394/$0395
$0396/$0397
$0398/$0399
```

Each `$9F29` call writes:

```text
$0F + exactly 3 descriptor bytes
```

Four descriptors therefore fill exactly `$3F10-$3F1F`.

Ownership is now separated mechanically:

- `$0392/93`: current player/Saint palette descriptor;
- `$0394/95`: primary visual-profile descriptor;
- `$0396/97`: secondary/auxiliary profile descriptor;
- `$0398/99`: secondary primary-profile descriptor or special one-shot override.

The special branch forces:

```text
$02 != $10 -> $0398/99 = $9960
$02 == $10 -> $0398/99 = $9963
```

`$9D58` normalizes PPUADDR with exact write sequence:

```text
$3F $00 $00 $00
```

#### Dynamic CHR0 override

After the one-shot palette transfer, `$9966[$02-$0C]` selects raw MMC1 CHR0:

```text
$02 : 0C 0D 0E 0F 10 11
CHR0: 1D 1D 1B 00 19 00
```

The `$0F/$11` zero values are canonical intentional bank-0 overrides, not missing entries. Static `$CACF` remains the platform-entry initializer; `$9966` is a later transient presentation mutation.

#### General fallback `$996C+`

The same entrypoint also owns three general palette managers:

1. primary profile `$03B7`: resolves `$0394/95` via `$9F46` four-variant lists and `$0398/99` via `$9F66`;
2. secondary profile `$03B4/$03B5`: resolves `$0396/97` through `$9C7F`;
3. pending `$03A9` selector values `$01-$05`: resolves `$0396/97` through `$9CC6`, then clears `$03A9`.

These paths all use the same `$9EEF` sprite-palette transfer.

#### Substate `$0D` background-palette animation

For `$02=$0D`, camera pages `$02-$04`, frame bit `$3C&$08` and latch `$03A7` alternate two nine-byte sources:

```text
low half  -> $A02B, latch 0 -> FF
high half -> $A022, latch FF -> 0
```

The first three background subpalettes are `$0F + 3 source bytes`; the fourth is fixed `$0F $10 $11 $16`, giving exactly `$3F00-$3F0F`.

If no palette work is due, control reaches `$9D69`.

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformVisualRefresh9915.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/PlatformVisualRefresh9915Checks.cs`
- `tools/reverse/audit_platform_visual_refresh.py`
- `tests/test_platform_visual_refresh_spec.py`
- `docs/reverse-engineering/PLATFORM_VISUAL_REFRESH_9915.md`
- reconciled `PLATFORM_CHR_MAP.md`
- reconciled `PLATFORM_STATE20_NMI.md`
- PR #156

No original palette payloads, CHR bytes, OAM dumps or ROM bytes are committed.

Do not reopen `$9915/$9EEF` without contradictory canonical-ROM evidence or a failing fixture.

## EVIDENCE FOR NEXT

The next contiguous unresolved presentation owner is bank-1 `$9D69+`, reached when `$9915` has no palette mutation to perform.

Direct canonical-ROM reconnaissance already establishes a bounded four-phase writer controlled by `$73`:

```text
$9D69:
  $2000=0
  $73 = ($73 + 1) & 3
  dispatch phase 0/1/2/3
```

Preliminary phase boundaries:

### Phase 0 — `$73=0`

Writes digit tiles derived from the active Saint resource arrays to explicit HUD addresses:

```text
$22F0
$2330
```

and, when `$02!=0`, writes the four-digit Seventh Sense value around `$236F` from `$05AA/$05AB`.

Helpers `$9EB8/$9EC4` split packed BCD nibbles and emit digit tile IDs `#$80 + nibble`.

### Phase 1 — `$73=1`

Uses `$22F4`, current Saint cap/resource nibbles and helpers `$9E73/$9E77/$9E84` to emit a bounded bar/gauge representation.

### Phase 2 — `$73=2`

Mirrors the phase-1 structure at `$2334` for the other packed resource/cap nibble.

### Phase 3 — `$73=3`

For `$02!=0`, writes a Seventh-Sense gauge around `$2374`; helper `$9E84` maps value bands to tile family `$A7/$B1-$B6/$BF`.

The directly adjacent helpers are bounded through `$9EED`:

```text
$9E73/$9E77/$9E80 -> repeated fill-tile writers
$9E84             -> threshold-to-gauge-tile classifier
$9EB8/$9EC4       -> packed-BCD digit emitters
$9ECD/$9ED8/$9EE3 -> fixed PPUADDR setters
```

No existing repository artifact closes `$9D69` as an executable phase. This is therefore a real contiguous gap rather than duplicate work.

## OPEN

1. Re-disassemble `$9D69-$9EED` from the canonical ROM and freeze all four `$73` phases, exact PPU addresses, RAM inputs and return paths.
2. Assign resource semantics only where existing RAM-map evidence supports them; preserve raw names for uncertain gauge fields rather than guessing UI labels.
3. Prove the `$73` cadence and whether every `$9915` fallback invocation advances it even when a phase performs no visible write.
4. Decode `$9E73/$9E77/$9E80/$9E84/$9EB8/$9EC4` as deterministic tile-generation primitives, including threshold boundaries and zero behavior.
5. Model a clean-room `PlatformHudRefresh9D69` (or equivalently scoped writer) that emits semantic PPU-address/data operations without embedding original nametable assets.
6. Add discriminating fixtures for packed-BCD digit rendering, resource-cap bars, Seventh Sense gating, phase cadence and tile-threshold boundaries.
7. Compose the writer as the downstream handoff from `PlatformVisualRefresh9915`/state-`$20` NMI without duplicating palette or main-thread simulation.
8. Stop after `$9D69-$9EED`. Do not absorb unrelated text renderer, full audio or RNG.

## NEXT

**Close the platform HUD/status presentation writer rooted at bank-1 `$9D69`: prove its four-phase `$73` cadence, exact resource/Seventh-Sense PPU writes and helper tile-generation rules through `$9EED`, then promote a clean-room executable phase with fixtures and compose it as the downstream no-palette-work handoff from the already-closed `$9915` path.**

Completion criterion:

> Given the active Saint/resource state, platform substate and incoming `$73` phase counter, the model must deterministically produce the same phase advance, PPU target addresses and semantic tile stream as canonical `$9D69-$9EED` for every reachable phase, including packed-BCD digits, bar/gauge fill behavior, Seventh Sense gating and no-write branches. The implementation must contain no extracted nametable/graphics payloads and must leave `$9915`, main-thread simulation, audio and RNG frozen.

## BLOCKERS

- None. Canonical ROM, RAM-map resource semantics, state-`$20` NMI, `$9915` palette/CHR path and clean-room test harness are available.

## RECOVERY CONTRACT

1. Read this file from `main` and reconcile it with newer merged history before executing `NEXT`.
2. Freeze PR #156 palette/CHR refresh, PR #154 state-`$20` NMI, PR #151 visual resources and PR #149 battle-stage closure.
3. Treat canonical ROM bytes as authority over UI labels; do not infer semantic names from tile appearance alone.
4. Preserve the phase boundary: `$9D69` is presentation/HUD work, not main-thread gameplay simulation.
5. Do not commit ROM, extracted CHR, palette payloads, nametable dumps or gameplay captures.
6. Drive remains private ROM/evidence storage and never owns an independent `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `platform-hud-status-9d69-four-phase-writer`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, evidence, a discarded hypothesis or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.

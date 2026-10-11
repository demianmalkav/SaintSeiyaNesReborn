# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

## CURRENT

- Phase: `ORIGINAL SPEC / global NMI presentation coverage audit`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#159` — platform HUD/status writer `$9D69-$9EED`.
- Merge commit: `0be81b3b2be10d47a20364d98b10c76f16b834f8`
- Exact final PR head: `134f7796bd1f57ae6eb961e4bcef83a013b662ee`
- Verification on that exact head:
  - `ORIGINAL SPEC tests` #408: `SUCCESS`
  - `Original Spec` #613: `SUCCESS`
  - build/self-test/password compatibility: `SUCCESS`
- Canonical ROM reverified before analysis: size `262160`, SHA-1 `F871D9B3DAFDDCDAD5F2ACD71044292E5169064E`, SHA-256 `6917B31D7343A9A17170E833BACDBC3B1EBA3E02D11C51C0D44DBE436C9AD43A`.
- Platform main-thread frame, map kits, static/dynamic CHR routing, metasprite resources, state-`$20` NMI, palette/CHR refresh and platform HUD/status presentation are frozen.

## DONE

### Platform HUD/status writer `$9D69-$9EED` — PR #159

The downstream no-palette-work presentation owner reached from `$9915` is mechanically closed.

#### Four-phase cadence

Every invocation executes:

```text
$2000 = 0
$73 = ($73 + 1) & 3
```

The **new** `$73` value selects the phase. Cadence advances even when the selected phase later emits no HUD data.

```text
phase 0 -> numeric Cosmo/Life + optional Seventh Sense digits
phase 1 -> Cosmo cap/current gauge
phase 2 -> Life cap/current gauge
phase 3 -> optional Seventh Sense gauge
```

#### Phase 0 digits

Exact PPU targets:

```text
Cosmo          $22F0
Life           $2330
Seventh Sense  $236F
```

Current Saint resources use the already-closed RAM model:

```text
Life  $59-$62
Cosmo $63-$6C
caps  $6D-$71
Seventh Sense $05AA/$05AB
```

Digit mapping is exactly:

```text
tile = $80 + nibble
```

`$9EB8` emits high then low packed-BCD nibble; `$9EC4` emits the low nibble only. The `$236F` address is set unconditionally, but `$02=0` suppresses Seventh Sense digit bytes.

#### Phase 1 / 2 resource gauges

Fixed targets:

```text
Cosmo $22F4
Life  $2334
```

The relevant cap nibble determines total width and is first cleared with `$A7`. Completed hundreds overwrite from the start with `$BF`; one partial segment follows from the packed low-two-digit byte.

Exact `$9E84` classifier:

```text
00-04 -> A7
05-24 -> B1
25-36 -> B2
37-49 -> B3
50-61 -> B4
62-74 -> B5
75-86 -> B6
87-99 -> BF
```

Threshold bytes are `$05/$25/$37/$50/$62/$75/$87`.

#### Phase 3 Seventh Sense gauge

`$02=0` suppresses the entire gauge write after cadence advance.

For `$02!=0`, `$2374` is first cleared with ten `$A7` tiles, then reset to `$2374`. One `$BE` is emitted per thousands digit (`high nibble of $05AB`). The partial fraction intentionally ignores the ones digit:

```text
fraction = ((low nibble $05AB) << 4) | (high nibble $05AA)
```

The high nibble of `$05AA` is also written to scratch `$39`.

Partial tile transform:

```text
A7 -> A7
B1 -> B8
B2 -> B9
B3 -> BA
B4 -> BB
B5 -> BC
B6 -> BD
BF -> BE
```

`$BE` is therefore the full Seventh Sense segment tile.

#### Bounded helpers

```text
$9E73 -> repeat $A7 Y times
$9E77 -> repeat $BF Y times
$9E80 -> repeat $BE Y times
$9E84 -> gauge threshold classifier
$9EB8/$9EC4 -> packed-BCD digit emitters
$9ECD -> PPUADDR $22F4
$9ED8 -> PPUADDR $2334
$9EE3 -> PPUADDR $2374
```

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformHudRefresh9D69.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/PlatformHudRefresh9D69Checks.cs`
- `docs/reverse-engineering/PLATFORM_HUD_REFRESH_9D69.md`
- PR #159

No ROM bytes, extracted nametable/CHR payloads or gameplay captures were committed.

Do not reopen `$9D69-$9EED` without contradictory canonical-ROM evidence or a failing fixture.

## EVIDENCE FOR NEXT

The contiguous platform presentation chain is now closed end-to-end at the semantic level:

```text
platform main-thread OAM shadow
 -> state-$20 NMI OAM DMA / background streamer / pause
 -> $9915 palette + dynamic CHR refresh
 -> $9D69 HUD/status writer when no palette work owns the slot
 -> common $D367+ control/mask/scroll commit
 -> persistent mapper restore / RTI
```

The remaining renderer/NMI uncertainty is **global coverage**, not a known platform-local routine. The fixed NMI entry `$D269+` dispatches additional `$00/$01` engine states outside platform state `$20`; many of those modes are already semantically closed elsewhere (front-end, transitions, battle/ending), but there is no executable coverage inventory proving which NMI presentation branches are already represented and which remain materially unresolved.

This audit must happen before jumping to RNG/audio so ORIGINAL SPEC does not leave an unclassified presentation gap.

## OPEN

1. Re-disassemble fixed-bank NMI dispatcher `$D269+` far enough to enumerate every canonically reachable global `$00/$01` presentation branch and its direct subroutine owners.
2. Cross-reference each branch against the already-closed global-state namespace and existing front-end/platform/battle/ending specifications.
3. Produce a coverage matrix classifying each branch as:
   - closed by an existing semantic model;
   - common/structural epilogue only;
   - materially unresolved presentation owner.
4. Add an executable `NmiPresentationCoverage` (or equivalently scoped manifest) with invariants that every known reachable global state has an evidence-backed classification and no platform state reopens closed `$D7F2/$D988/$9915/$9D69` work.
5. For any unresolved branch, trace only far enough to define one bounded next checkpoint with exact entry, owner and closure criterion; do not absorb all branches into the audit.
6. Reconcile global reverse-engineering status documentation with the coverage result.
7. Stop after the coverage audit. RNG, audio and runtime localization remain separate later checkpoints.

## NEXT

**Close the global NMI presentation coverage audit: enumerate the fixed-bank `$D269+` dispatch across the already-closed global `$00/$01` namespace, map every canonically reachable presentation branch to its existing semantic owner or mark it materially unresolved, and promote an executable coverage manifest whose zero-gap result determines the next bounded renderer checkpoint (if any) before ORIGINAL SPEC moves to RNG/audio.**

Completion criterion:

> Starting from the canonical NMI entry and the already-closed global state namespace, every reachable NMI presentation branch must have a deterministic evidence-backed classification with entry address, temporary PRG-bank ownership where applicable, and owning specification/model. The audit must report zero **unclassified** reachable branches. Existing closed platform, front-end, battle and ending semantics must not be duplicated. If one or more materially unresolved branches remain, the audit must choose exactly one bounded branch as the next executable checkpoint rather than implementing all of them at once.

## BLOCKERS

- None. Canonical ROM, global `$00/$01` namespace, fixed NMI entry, platform presentation chain and clean-room test harness are available.

## RECOVERY CONTRACT

1. Read this file from `main` and reconcile it with newer merged history before executing `NEXT`.
2. Freeze PR #159 HUD writer, PR #156 palette/CHR refresh, PR #154 state-`$20` NMI, PR #151 visual resources and PR #149 battle-stage closure.
3. Treat canonical ROM bytes and live Git history as authority over historical labels or chat context.
4. Audit coverage before adding new renderer behavior; classification is not permission to reopen already-closed branches.
5. Do not commit ROM, extracted CHR/palette/nametable payloads, OAM dumps or gameplay captures.
6. Drive remains private ROM/evidence storage and never owns an independent `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `global-nmi-presentation-coverage-audit`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, evidence, a discarded hypothesis or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.

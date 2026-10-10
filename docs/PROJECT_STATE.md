# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

## CURRENT

- Phase: `ORIGINAL SPEC / platform visual-resource closure — metasprites $05-$0F + special objects`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#149` — complete canonical stage `$07` Capricorn / Shura context and forced-Seiya Aquarius handoff.
- Merge commit: `28ba0a71c943d0f2ee943e8b60158a15c9d3efd5`
- Exact final PR head: `eb55de62e008d1dbb92b16a6432759a22b32b7fb`
- Verification on that exact head:
  - `ORIGINAL SPEC tests` #384: `SUCCESS`
  - `Original Spec` #593: `SUCCESS`
  - build/self-test/password compatibility: `SUCCESS`
- Canonical `$050E=$00-$0B` battle/event denominator now has **zero material stage-local gaps**.
- Dedicated contexts are closed at `$00-$0A`; `$0B` remains structural/transient with no canonical stable battle; final-special `$0C` remains separately closed.
- Do not invent another House/boss gap. ORIGINAL SPEC now returns to unresolved global subsystems.

## DONE

### Stage `$07` Capricorn / Shura — PR #149

Canonical entry from the frozen Scorpio bridge:

```text
$067D=$09
$050E=$07
$06CD=$00
$0673=$30
reachable Saints = Seiya / Hyoga / Shun / Shiryu
Ikki excluded
```

Exact stage owners remain `$9ACF/$9ED6/$A86B/$A8D8`, with generic parity Gold slots `0/1`.

#### Fresh initialization is Shiryu-only

The outer battle-entry owner `$9770-$97B8` checks the fixed designated-Saint table before dispatching the stage initializer:

```text
$F36F[$07]=$03 = Shiryu
```

Therefore fresh Seiya/Hyoga/Shun entries bypass `$9ACF`. Fresh Shiryu dispatches it.

Ordinary Shiryu `$9ACF`:

```text
temporary presentation $0F
$0672=0
$058A:1->2
$0696:1->2
+600 Seventh Sense
shared $9C3D -> release $03 / $068E=1
```

Inbound `$0670=$FE` returns before reward/growth.

#### Talk / battle branches

Talk `$9ED6`:

```text
first $066F=0:
  $AE + per-Saint $43/$43/$43/$AF
  INC $066F
  no forced Gold
repeat $066F!=0:
  repeat-form dialogue
  INC $DC
  INC $066F
  fixed caller forces Gold
```

Post-Bronze `$A86B`:

```text
$EB=$00 + $06BC!=0 -> continue
$EB=$00 + $06BC=0  -> message $8B
$EB=$01 + Shiryu   -> continue; no $0690
$EB=$01 + other    -> $0690=$FF
$EB=$FF             -> $06B1=$FF / +800 Seventh Sense / release $FE
```

Fixed `$FAB9+` consumes `$0690` materially:

```text
$0690!=0 -> force $06BC=0
$0690==0 -> generic battle subsystem retains $06BC ownership
```

Post-Gold `$A8D8`:

```text
$EA=$00 -> continue
$EA=$01 -> repeatable $40/$91 feedback
$EA=$FF -> release $FF
```

#### Defeat retry cannot farm the initializer reward

Defeat `$FF` keeps progress `$09`, so fixed `$E4D7` selects principal platform substate `$02=$09`. The common accepted gate is `X >= $D0 / Y=$40 / jump=0`, followed by normal `$E100` warm reload.

`$ED57/$A973` clears encounter-local state including `$066F/$0670/$068E/$0690`. The warm continuation then reaches `$E2DD->$E33D` directly. It **does not** call `$970A/$97DB/$9ACF` again.

Consequences:

```text
$058A preserved at 2
$0696 preserved at 2
$0672 preserved
no second +600
no duplicate technique increment
```

#### Victory `$FE` exact successor

Fixed `$E3ED-$E414` saves the winning Saint record, forces `$0533=$00` Seiya, rewrites `$FE->$01`, and joins ordinary story progression:

```text
$067D:09->0A
$050E=$08
$06CD=$08
$0673=$38
active Saint = Seiya
```

This is the exact already-closed Aquarius/final-Camus story boundary. Aquarius internals were not reopened.

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/CapricornStage07Context.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/CapricornStage07ContextChecks.cs`
- `docs/reverse-engineering/BOSS_CONTEXT_STAGE_07_CAPRICORN.md`
- promoted battle-stage coverage and fixtures
- updated battle coverage/dispatch docs
- PR #149

Frozen coverage result:

```text
closed dedicated : 00 01 02 03 04 05 06 07 08 09 0A
material missing : NONE
structural only  : 0B
separate closed  : 0C
```

Do not reopen the battle-stage denominator without contradictory ROM evidence or a failing fixture.

## EVIDENCE FOR NEXT

The next unresolved global area with the strongest existing groundwork is the **platform visual-resource layer**. This is deliberately narrower than “write the renderer”.

### Already closed/proven visual foundations

`PLATFORM_CHR_MAP.md` proves fixed `$CB04` CHR routing from platform substate `$02`:

```text
$CACF[$02] -> MMC1 CHR bank 0
$CABD[$02] -> MMC1 CHR bank 1 / shadow $75
```

MMC1 is in 4 KiB CHR mode. Proven platform groups include:

```text
$00-$0B : CHR0=25 / CHR1=15
$0C-$0D : CHR0=29 / CHR1=19
$0E     : CHR0=27 / CHR1=17
$0F-$10 : CHR0=25 / CHR1=15
$11     : CHR0=25 / CHR1=30
```

`PLATFORM_MAP_KITS.md` already proves PPUCTRL shadow `$77=$90` in platform mode:

```text
sprite pattern table     = $0000 -> CHR0
background pattern table = $1000 -> CHR1
```

Thus CHR0 is the platform sprite resource bank and CHR1 is the background/metatile resource bank.

The map-kit layer for all substates `$00-$11` is already closed, including PRG data bank, metatile base, page counts and pool-page sequences. Do not rediscover that mapping.

### Existing metasprite foundation

Bank 3 entity rendering already selects metasprite definitions through pointer-table families around:

```text
$B669 / $B671 / $B699 / $B6C1
```

Using the proven CHR0 banks `25/27/29`, entity types `$01-$04` have already been parsed into coherent multi-tile figures. One common animation table resolves:

```text
type $01 -> $ACC4, 7 hardware sprites
type $02 -> $AD0F, 6 hardware sprites
type $03 -> $AD54, 6 hardware sprites
type $04 -> $AD9C, 7 hardware sprites
```

The format is established as hardware-sprite records containing tile/Y/X triples with optional `$FF` attribute overrides.

`tools/reverse/render_platform_entities.py` is a clean-room ROM-fed renderer for types `$01-$04`; it embeds no original graphics and verifies the canonical ROM core CRC before extracting CHR/metasprite data.

### Existing runtime/object foundation

The persistent `$9B93` multisprite runtime is already closed independently, including bootstrap/normal/flag/death/substate-`$0D` routing and shared state carry. Its remaining boundary is composition into the complete late-object order, not rediscovery of those branch internals.

### Actual visual gap

`PLATFORM_CHR_MAP.md` explicitly leaves unresolved:

1. metasprite definitions for entity types `$05-$0F`;
2. special-object sprite tables outside the ordinary type `$01-$0F` family;
3. complete semantic inventory linking each decoded definition to its pointer-table/animation selector and CHR0 group;
4. an executable invariant that every reachable platform sprite definition resolves within the selected 4 KiB CHR0 bank and produces a bounded OAM composition.

Visual identity names from screenshots are **not** required to close this mechanical resource layer. Names may remain numeric/semantic until capture evidence is unambiguous.

## OPEN

1. Trace bank-3 entity renderer selector flow far enough to enumerate the complete reachable ordinary entity-type domain `$01-$0F` and the exact pointer-table/animation families that select definitions.
2. Decode and model metasprite definitions for types `$05-$0F` using the already-established count / optional `$FF` attribute / tile-Y-X record format; reject any type or animation family that static reachability proves unused rather than inventing data.
3. Enumerate special-object sprite-definition tables reachable from platform mode that are not owned by the ordinary entity-type table; keep already-closed `$9B93` behavior separate from its visual definition data.
4. Bind each reachable definition to the proven CHR0 bank set selected by `$02` (`25/27/29`, plus any other bank only if ROM evidence establishes it).
5. Extend the ROM-fed clean-room visual tool so it can audit/render all reachable ordinary entity definitions and special-object definitions without embedding ROM graphics.
6. Add executable semantic fixtures/invariants for pointer validity, sprite-count bounds, tile-index validity inside a 4 KiB bank, optional attribute overrides, and deterministic definition selection.
7. Produce one focused visual-resource document distinguishing mechanical closure from optional visual-name identification.
8. Stop after resource-definition closure. Do not yet absorb the complete NMI/PPU frame scheduler, palette system, or late-object execution order unless required by contradictory evidence.

## NEXT

**Close the platform visual-resource definition layer: extend the proven CHR0/metasprite chain from ordinary entity types `$01-$04` to every canonically reachable ordinary type `$05-$0F`, enumerate reachable special-object sprite-definition tables, bind each definition to its exact bank-3 selector/pointer family and proven platform CHR0 bank, and promote the ROM-fed renderer/auditor plus fixtures so every reachable definition is mechanically reproducible without committing original graphics.**

Completion criterion:

> Starting from the frozen `$02 -> CHR0/CHR1` selection and existing types `$01-$04` parser, produce a complete executable inventory of all canonically reachable platform sprite definitions (ordinary `$01-$0F` plus separately selected special objects), their pointer/animation ownership, record parsing and selected CHR0 resource set. Every reachable definition must satisfy deterministic pointer, record-length, tile-range and hardware-sprite-count invariants and be renderable from a user-supplied canonical ROM. Numeric identities are sufficient; screenshot-based character naming is optional and must not block closure.

## BLOCKERS

- None. Canonical ROM, platform CHR routing, map kits, ordinary metasprite parser, ROM-fed renderer tool and major platform object runtimes are already available.

## RECOVERY CONTRACT

1. Read this file from `main` and reconcile it with newer merged history before executing `NEXT`.
2. Freeze PR #149 and the complete `$050E=$00-$0B` battle-stage coverage. Do not reopen a House/boss context merely because the active work moved to visuals.
3. Reuse `PLATFORM_CHR_MAP.md`, `PLATFORM_MAP_KITS.md` and `render_platform_entities.py`; do not rediscover CHR0/CHR1 roles or platform map-kit tables.
4. Treat visual identity labels as a separate confidence layer from mechanically proven pointers/tiles/metasprites.
5. Do not commit extracted CHR art or generated PNGs; only clean-room code, semantic tables/tests/docs belong in GitHub.
6. Drive remains private ROM/evidence storage only; it never owns an independent `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `platform-visual-resource-metasprites-05-0f-special-objects`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, evidence, a discarded hypothesis or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.

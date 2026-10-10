# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

## CURRENT

- Phase: `ORIGINAL SPEC / stage $00 Mu repair context`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#139` — complete `$050E=$00-$0B` battle-stage context coverage audit.
- Merge commit: `2ed957e779277be7441fec2732e1ba0c919e4903`
- Exact final PR head: `d25433da6f4a178922c2a6dbe06eac048ec7cca5`
- Verification on that exact head:
  - `ORIGINAL SPEC tests` #349: `SUCCESS`
  - `Original Spec` #548: `SUCCESS`
  - build/self-test/password compatibility: `SUCCESS`
- PR #137 closes the post-Saga ending tail and true `$BD2F` hard terminal.
- Dedicated battle contexts already closed: #123 Taurus `$01`, #125 Leo `$04`, #127 Virgo `$05`, #129 Aquarius `$08`, #131 Pisces `$09`, #135 Saga `$0A`; #133 closes final-special bridge `$0C`.
- PR #121 closes the complete global `$00/$01` engine namespace.

## DONE

### Complete battle-stage context coverage audit — PR #139

The canonical stage-indexed battle/event namespace `$050E=$00-$0B` is now fully classified. The audit does not duplicate generic damage, resource, dodge or technique arithmetic.

Exact dispatcher families are locked in executable fixtures:

```text
init          $97DB / table $97E1
Talk          $9C95 / table $9C9B
post-Bronze   $A361 / table $A367
post-Gold     $A381 / table $A387
```

Fixed story-progress table `$F016` is promoted exactly for `$067D=$00-$0E`:

```text
00->00  01->01  02->02  03->03  04->04
05->05  06->0F  07->06  08->10  09->07
0A->08  0B->09  0C->0C  0D->0A  0E->00
```

Canonical ordinary battle provenance is therefore:

```text
stage 00 <- progress 00
stage 01 <- progress 01
stage 02 <- progress 02
stage 03 <- progress 03
stage 04 <- progress 04
stage 05 <- progress 05
stage 06 <- progress 07
stage 07 <- progress 09
stage 08 <- progress 0A
stage 09 <- progress 0B
stage 0A <- progress 0D
stage 0B <- none
```

Coverage result:

```text
closed dedicated contexts : 01,04,05,08,09,0A
material uncovered        : 00,02,03,06,07
structural/no battle       : 0B
separate closed bridge     : 0C
```

Key negative result for `$0B`:

- no `$F016` entry selects stable stage `$0B`;
- its two relevant immediate `$0B` uses call temporary presentation loader `$F2ED`;
- raw init pointer `$A960` lands inside the real instruction beginning at `$A95F`, so table membership is not executable reachability;
- no dedicated `$0B` battle context is required.

Canonical reachable opponent technique slots are also locked:

```text
00 none
01 0,1
02 0,1
03 0,1
04 0,1
05 0,1,2
06 0,1
07 0,1
08 0,1,2
09 0,1,2
0A 0,1,2,3
0B none
```

The audit proves the **first material uncovered context is stage `$00`**, not Gemini.

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/BattleStageContextCoverage.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/BattleStageContextCoverageChecks.cs`
- `docs/reverse-engineering/BATTLE_STAGE_CONTEXT_COVERAGE.md`
- updated `docs/reverse-engineering/BATTLE_EVENT_DISPATCH.md`
- PR #139

Do not reopen this coverage classification without contradictory ROM evidence or a failing fixture.

## EVIDENCE FOR NEXT

### Stage `$00` is a material special context

Canonical seed is story progress `$067D=$00`, which fixed `$F016` maps to `$050E=$00`.

Common battle setup `$A973+` clears the local state used here, including:

```text
$066F = 0
$0670 = 0
```

Stage init `$97F7` is `RTS`, but the context is not empty. Fixed command handling specializes stage zero:

```text
Attack              $F057+ -> $F238
Resource allocation $F041+ -> $F238
Escape              $F0D3+ -> $F238
Talk                $F0B1 -> $9C95 -> $9CB7
```

Thus Attack, allocation and Escape are blocked before ordinary Bronze/Gold battle processing. The canonical material machine is Talk `$9CB7`:

```text
$066F == 0
  -> first presentation
  -> $066F = 1
  -> return

$066F != 0
  -> second presentation
  -> $0670 = $01
  -> return
```

Release `$01` joins the already-known fixed owner `$E399+/$E3B3`, increments story progress `$067D:00->01`, and fixed `$F016[01]=$01` selects Taurus next.

Because Attack is blocked, stage `$00` cannot canonically reach generic Bronze damage, `$A361`, Gold selection/dodge/damage or `$A381`. Structural `$A3A1` entries therefore do not make it an ordinary battle.

## OPEN

1. Model the exact canonical stage-`$00` runtime seed from common `$A973+`, including only persistent/local fields that affect reachable control.
2. Encode the command topology proving Attack, resource allocation and Escape terminate in the stage-zero blocked owner while Talk remains reachable.
3. Close Talk `$9CB7` across `$066F=0 -> 1` and the repeated/second Talk `$0670=$01` terminal.
4. Resolve whether the first/second presentation branches vary materially by active Saint; preserve message/presentation selectors only when they affect behavior or localization ownership.
5. Compose release `$01` through fixed `$E399/$E3B3` to exact successor `$067D=$01/$050E=$01` without reopening Taurus internals.
6. Prove no generic Bronze/post-Bronze/Gold/post-Gold arithmetic is reachable from canonical `$00` command flow.
7. Implement a dedicated executable `MuStage00Context`, discriminating fixtures and one focused context document.
8. Stop once every canonical stage-`$00` command has a known result and the only progression terminal joins the existing Taurus boundary.

## NEXT

**Close canonical stage `$00` Mu / pre-battle repair from common battle reset `$A973+` and story seed `$067D=$00/$050E=$00` through the Talk-only `$9CB7` machine, second-Talk release `$01`, and fixed progression handoff to `$067D=$01/$050E=$01` Taurus, proving blocked Attack/resource/Escape surfaces and excluding generic Gold arithmetic.**

Completion criterion:

> Starting from the exact common battle-reset seed, produce an executable special-context model that proves the reachable command topology, first Talk `$066F:0->1`, second/repeated Talk release `$0670=$01`, and the fixed owner chain to story progress `$01` / Taurus stage `$01`. Attack, resource allocation and Escape must be shown to bypass ordinary battle arithmetic, and no structural post-action or Gold-selector entry may be promoted without reachable control flow.

## BLOCKERS

- None. Canonical ROM, common battle reset, stage-zero command gates, Talk handler, release owner, `$F016` story map and Taurus successor are all known.

## RECOVERY CONTRACT

1. Read this file from `main`.
2. Reconcile it with newer merged Git history if present.
3. Treat PR #139 and `BATTLE_STAGE_CONTEXT_COVERAGE.md` as the frozen coverage denominator.
4. Inspect only stage `$00` owners needed for the special context: `$A973+`, `$F041+`, `$F057+`, `$F0B1+`, `$F0D3+`, `$F238+`, `$9CB7+`, and the fixed release/progression owner `$E399/$E3B3`.
5. Reuse the global release/story machinery and Taurus boundary; do not reopen generic damage/resources/dodge or Taurus `$01`.
6. Treat post-Saga progress `$0E -> numeric stage $00` as a separate already-closed platform path, not a Mu re-entry.
7. Drive remains private ROM/evidence storage only; it never owns an independent `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `special-context-stage-00-mu-repair`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, evidence, a discarded hypothesis or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.

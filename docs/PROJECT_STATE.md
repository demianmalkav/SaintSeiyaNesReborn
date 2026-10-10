# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

## CURRENT

- Phase: `ORIGINAL SPEC / final-special stage $0C`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#131` — complete stage `$050E=$09` Pisces/Aphrodite context, Shun progression, deterministic Gold escalation and exact `$FE` successor.
- Merge commit: `002ecd86361ccc6acca028fbafda5e16df41b3a6`
- Exact final PR head: `92d428cb30ced7f6c6a9f403571aafb4f8cd7378`
- Verification on that exact head:
  - `ORIGINAL SPEC tests` #333: `SUCCESS`
  - `Original Spec` #529: `SUCCESS`
  - build/self-test/password compatibility: `SUCCESS`
- PR #129 closes Aquarius/Camus stage `$08`; PR #127 closes Virgo/Shaka `$05`; PR #125 closes Leo/Aioria `$04`; PR #123 closes Taurus/Aldebaran `$01`.
- PR #121 closes the complete global `$00/$01` namespace.

## DONE

### Pisces/Aphrodite stage `$09` — PR #131

Closed reachable stage-local/control paths:

```text
initialization       $9B5C  (RTS / no local body)
Talk                 $9F99
post-Bronze action   $AA57
post-Gold response   $AAF0
Gold selector        bank6 $90D5-$90EA
```

Promoted semantics:

- canonical predecessor Aquarius advances `$067D: $0A->$0B`; the story-stage table maps `$0B -> $050E=$09`;
- story descriptor `$E50B[$0B]=$0A` produces pre-entry `$0673=$3A`; the generic Saint-bit gate proves only Seiya and Shun are selectable in canonical Pisces;
- stage-9 initialization pointer `$9B5C` is a bare `RTS`; no Aphrodite-specific init behavior is invented;
- Talk uses total Gold dodge history `$0677+$0678`; below two attempts it is dialogue-only and does not force a Gold response;
- at two or more attempts, first Shun Talk with `$066F==0` runs `$A1FF` with `#$10`, grants +1000 Seventh Sense through already-closed `$F31E`, increments `$066F` and does not force the immediate Gold response;
- all other post-threshold Talk paths raise transient `$DC` and force the Gold response;
- every Bronze action increments both `$064D` and dedicated Pisces action counter `$EF`;
- because only Seiya/Shun are reachable, `$AA57`'s raw `$0533!=0` branch is exactly the Shun route;
- Shun technique growth is equality-triggered: `$EF==2` increments `$0589/$0696` from 2->3 (Nebula Stream), `$EF==5` increments 3->4 (Nebula Storm); a Seiya action on either exact count misses that increment rather than deferring it;
- technique growth executes before opponent-condition handling, so a same-action Aphrodite defeat still receives the Shun increment first;
- `$EB=$01` first-low branch installs `$064D=$80`; this is both a one-time dialogue latch and an immediate jump to the highest reachable Aphrodite attack tier;
- `$EB=$FF` grants +1200 Seventh Sense (`#$12 -> $F31E`) and releases `$FE`;
- Gold attack selection is deterministic: `$064D 0..2 -> slot0`, `3..5 -> slot1`, `>=6 -> slot2`; structural slot3 is unreachable; reachable coefficients are `22/32`, `34/22`, `29/29` Cosmo/Life;
- post-Gold `$EA=$01` is nonterminal feedback; `$EA=$FF` releases ordinary defeat `$FF`;
- generic `$FE` ownership zeroes the active winner Life/Cosmo mirrors before fixed story progression;
- Pisces `$FE` advances `$067D: $0B->$0C`; the story-stage table maps `$0C -> stage $0C`, not directly to Saga;
- the special `$0C` handoff has two proven variants from the persisted `$0673` bitset: Shun-Pisces route selects Seiya with `$06CD=$0E/$0673=$3E`; Seiya-Pisces route selects Shun with `$06CD=$0B/$0673=$3B`.

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/PiscesStage09Context.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/PiscesStage09ContextChecks.cs`
- `docs/reverse-engineering/BOSS_CONTEXT_STAGE_09_PISCES.md`
- promoted Shun progression in `docs/reverse-engineering/BATTLE_TECHNIQUES.md`
- PR #131

Do not reopen Pisces without contradictory ROM evidence or a failing fixture.

## EVIDENCE FOR NEXT

### Why special/final stage `$0C` is next

Stage `$0C` is the proven canonical successor of Pisces, not an optional detour:

```text
Pisces victory release $FE
$067D: $0B -> $0C
story-stage table $F016[$0C] = $0C
```

Two exact entry variants are already proven by the Pisces closure:

```text
Shun won Pisces  -> stage $0C with active Seiya, $06CD=$0E, $0673=$3E
Seiya won Pisces -> stage $0C with active Shun,  $06CD=$0B, $0673=$3B
```

Known static anchors requiring composition rather than inference:

- stage-indexed Talk dispatcher uses `$A1AD` for index `$0C`;
- `$A1AD` has Seiya/Shun dialogue paths and a one-time `$066F==0` branch that loads `#$10`, calls `$A1FF` and increments `$066F`; by the already-closed resource helper this implies a +1000 Seventh Sense event, but its exact entry/command ownership still must be joined;
- stage-indexed post-Bronze and post-Gold dispatchers both point stage `$0C` to shared `$A3A1` (`RTS`), so this is not a normal boss context and must not be forced into the Taurus/Leo/Virgo/Aquarius/Pisces template;
- the stage-`$0C` initialization-dispatch slot is nonstandard and currently points into a context that was previously left unclassified; prove whether it is executable dispatch, data, or deliberately bypassed before modeling it;
- fixed command/state handling contains explicit stage-`$0C` branches outside the normal boss dispatchers, including a path around `$F08B+` that performs presentation/action helpers and writes release `$0670=$01`;
- another fixed stage-`$0C` branch around `$F14A+` emits message `$D4`, reinforcing that ownership is distributed through fixed special-state code rather than ordinary boss handlers;
- the fixed story-stage table maps subsequent progress `$0D -> stage $0A` (Saga), so the next closure must prove the exact `$0C -> $0D` transition instead of assuming a direct jump.

## OPEN

1. Classify stage `$0C` entry ownership and the nonstandard initialization-dispatch slot; prove which code actually executes on each Seiya/Shun entry variant.
2. Close `$A1AD` Talk end-to-end, including active-Saint dialogue, `$066F`, `$A1FF`, the +1000 Seventh Sense event and whether/when Talk advances the special context.
3. Trace all fixed stage-`$0C` command/state branches, especially `$F08B+` release `$01` and `$F14A+`, and identify their high-level command meanings from callers/state.
4. Prove whether any battle arithmetic, dodge, post-Bronze or post-Gold machinery is actually reachable; do not infer it merely because `$050E` is a stage index.
5. Resolve the complete release namespace reachable inside `$0C` and join each token to an already-closed owner or a newly isolated special owner.
6. Prove the exact mutation that advances `$067D: $0C->$0D`, then join `$0D` to story stage `$0A` Saga through the fixed table.
7. Implement an executable special-stage context plus discriminating fixtures and evidence documentation only after the reachable graph is classified.
8. Stop only when both Pisces-derived entry variants have a proven route through `$0C` to the Saga boundary or to an explicitly identified terminal owner.

## NEXT

**Close the complete special/final story stage `$0C` end-to-end, including its Seiya/Shun entry variants, `$A1AD` Talk/reward behavior, nonstandard command/initialization ownership, release paths and the exact transition into `$067D=$0D -> stage $0A` Saga.**

Completion criterion:

> Starting from both proven Pisces `$FE` successor states at `$067D/$050E=$0C`, produce an evidence-backed executable graph for every reachable special-stage path, classify the nonstandard init slot and fixed stage-`$0C` branches, resolve `$066F`/Talk and release ownership, and prove the exact handoff to `$067D=$0D -> $050E=$0A` without importing ordinary boss semantics that are not reachable.

## BLOCKERS

- None. Canonical ROM, both exact stage-`$0C` entry states, fixed progression tables and the relevant dispatcher/fixed-bank anchors are available.

## RECOVERY CONTRACT

1. Read this file from `main`.
2. Reconcile it with newer merged Git history if present.
3. Read `BOSS_CONTEXT_STAGE_09_PISCES.md` only for the exact `$0C` predecessor states and release handoff.
4. Inspect `$A1AD`, stage-`$0C` dispatcher entries and fixed stage-`$0C` branches around `$F08B+/$F14A+`; follow callers before assigning semantics.
5. Reuse `RESOURCE_ECONOMY.md`, release/progression specs and global state-machine contracts instead of reopening them.
6. Do not treat stage `$0C` as a boss battle unless reachability proves battle paths.
7. Drive is private ROM/evidence storage only; it never owns a separate `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `final-special-stage-0c`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, evidence, a discarded hypothesis or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.

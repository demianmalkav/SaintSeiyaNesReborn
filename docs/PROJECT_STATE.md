# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

## CURRENT

- Phase: `ORIGINAL SPEC / boss context stage $09 Pisces`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#129` — complete stage `$050E=$08` Aquarius/Camus two-encounter context, Hyoga technique progression and Gold-slot reachability.
- Merge commit: `d23388bebaaee4f4dc893c35531188208b9f4944`
- Exact final PR head: `f9ef70eab80baee247d519a85b3a184491f7a76e`
- Verification on that exact head:
  - `ORIGINAL SPEC tests` #329: `SUCCESS`
  - `Original Spec` #525: `SUCCESS`
  - build/self-test/password compatibility: `SUCCESS`
- PR #127 closes Virgo/Shaka stage `$05`; PR #125 closes Leo/Aioria stage `$04`; PR #123 closes Taurus/Aldebaran stage `$01`.
- PR #121 closes the complete global `$00/$01` namespace.

## DONE

### Aquarius/Camus stage `$08` — PR #129

Closed stage-local handlers:

```text
initialization       $9B14
Talk                 $9F00
post-Bronze action   $A8FC
post-Gold response   $A9D3
Gold selector        bank6 $90A8+
```

Promoted semantics:

- stage `$08` owns two distinct Camus encounters rather than one monolithic battle;
- first Camus is reached by the fixed special-resume path from stage `$02` with active Hyoga: the engine redirects `$050E` to `$08`, writes `$06B8=$0A`, arms `$0690=$FF` and preserves the special-resume phase by skipping the ordinary reset;
- first-Camus Talk is a three-step `$066F` script; before `$066F>=3`, a Bronze action is consumed by a deliberate stack unwind, while the next Bronze action after the third Talk runs the scripted Aurora Execution/Freezing Coffin sequence and releases `$FE`;
- actual Hyoga defeat during first Camus is also converted to scripted `$FE`, not generic defeat `$FF`;
- fixed `$FE` ownership forces Seiya and advances story progress `$067D: $02->$03`, whose next ordinary stage is `$03` Cancer;
- final Camus is the ordinary story context `$067D=$0A -> $050E=$08` with `$06B8=0`;
- `$06E1` survives the ordinary battle reset and controls the one-time no-dodge Talk branch; `$06B8` does not survive that reset and therefore remains the first-encounter redirection latch;
- final-Camus initialization increments Hyoga persistent/active technique counts `$0588/$0696` from 2 to 3, exposing contiguous slot 2 / Aurora Thunder Attack;
- after at least one Gold dodge attempt, the first Hyoga Talk with `$066F=0` clears `$0690` and increments `$0588/$0696` from 3 to 4, exposing contiguous slot 3 / Aurora Execution;
- final-Camus post-Bronze uses generic opponent condition but terminates only on `$EB=$FF`, releasing `$FE`; `$EB=$00/$01` remain nonterminal;
- final-Camus post-Gold uses ordinary defeat only for `$EA=$FF`; `$EA=$01` is feedback-only and nonterminal;
- Gold selector reachability is exact: first Camus (`$06B8!=0`) forces slot2; final Camus selects slot1 while total dodge attempts `$0677+$0678 < 2` and slot0 once the total reaches 2; structural slot3 is unreachable;
- reachable stage-8 coefficient profiles are `30/20` (slots 0/2) and `21/31` (slot1), Cosmo/Life;
- final-Camus `$FE` forces Seiya and advances `$067D: $0A->$0B`; the story-stage table maps `$0B` to stage `$09` Pisces.

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/AquariusStage08Context.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/AquariusStage08ContextChecks.cs`
- `docs/reverse-engineering/BOSS_CONTEXT_STAGE_08_AQUARIUS.md`
- promoted Hyoga unlock semantics in `docs/reverse-engineering/BATTLE_TECHNIQUES.md`
- PR #129

Do not reopen Aquarius without contradictory ROM evidence or a failing fixture.

## EVIDENCE FOR NEXT

### Why stage `$09` Pisces/Aphrodite is next

Pisces is the direct canonical successor of final Camus: Aquarius `$FE` advances `$067D` from `$0A` to `$0B`, and the fixed story-stage table maps `$0B` to `$050E=$09`.

Stage handlers:

```text
initialization       $9B5C  (RTS / no stage-local init body)
Talk                 $9F99
post-Bronze action   $AA57
post-Gold response   $AAF0
Gold selector        bank6 $90CE-$90EA, stage branch $90D5+
```

Known ROM anchors already isolated:

- `$9F99` uses total dodge history `$0677+$0678`; after at least two attempts it has a Shun-specific branch (`$0533==2`) gated by `$066F==0` and helper `$A1FF`, while later/other branches can force a Gold response;
- `$AA57` increments both stage-local `$064D` and turn/event counter `$EF` on every Bronze action;
- the same post-Bronze handler increments Shun technique counts `$0589/$0696` when `$EF` reaches 2 and 5, but exact reachable active-Saint conditions and composition with the stage flow still need to be proven;
- `$AA57` converts the first low-opponent condition into a one-time `$064D=$80` event and uses `$FE` on actual opponent defeat;
- bank6 stage-9 selector is deterministic from `$064D`: `<3 -> slot0`, `3..5 -> slot1`, `>=6 -> slot2`; structural slot3 is not selected by this branch;
- stage-9 coefficient row is `22/32`, `34/22`, `29/29`, `29/29` (Cosmo/Life);
- `$AAF0` is comparatively simple: `$EA=$01` gives low-player feedback, `$EA=$FF` releases generic defeat `$FF`;
- generic `$FE` progression from the canonical Pisces entry `$067D=$0B` advances to `$0C`; the fixed story-stage table maps `$0C` to special stage `$0C`, so that successor boundary must be joined rather than guessed.

## OPEN

1. Prove the complete `$9F99` Talk graph, including dodge-history thresholds, active-Saint branches, `$066F`, `$DC` and the exact semantics of helper `$A1FF`.
2. Close `$AA57` end-to-end: `$064D`, `$EF`, opponent-condition branches, the `$064D=$80` low-opponent latch and terminal `$FE`.
3. Resolve the two `$EF==2/$05` Shun technique-growth events against the contiguous `$0589/$0696` technique model, including exact reachability conditions and whether non-Shun active states can encounter those writes.
4. Close `$AAF0` post-Gold behavior and compose it with generic `$EA` classification without duplicating damage/dodge arithmetic.
5. Prove stage-9 Gold-slot reachability from `$064D`, including interaction with the `$80` latch and structural slot3 reachability.
6. Trace Pisces `$FE` through fixed progression into `$067D=$0C / stage $0C` and identify the already-known or newly-required successor owner.
7. Implement one complete stage-9 executable context, discriminating fixtures and evidence documentation.
8. Stop only when every reachable Pisces path has a known successor or an explicitly identified next subsystem boundary.

## NEXT

**Close the complete stage `$09` Pisces/Aphrodite context end-to-end, including Shun technique-growth thresholds, dodge/Talk state, deterministic Gold-attack escalation and the `$FE` handoff into story stage `$0C`.**

Completion criterion:

> Starting from canonical Pisces entry `$067D=$0B -> $050E=$09`, produce an evidence-backed executable graph covering every reachable `$9F99/$AA57/$AAF0` branch, resolve `$064D/$EF/$066F/$0677/$0678`, compose the two `$0589/$0696` growth events with the existing technique model, prove reachable `$0680` Gold slots from the stage-9 selector, and join `$FE/$FF` terminals to their fixed owners without duplicating generic battle arithmetic.

## BLOCKERS

- None. Canonical ROM, the Aquarius predecessor, boss primitives and the stage-9 handlers are available.

## RECOVERY CONTRACT

1. Read this file from `main`.
2. Reconcile it with newer merged Git history if present.
3. Read `BOSS_CONTEXT_STAGE_08_AQUARIUS.md` only as the immediate predecessor/handoff reference; reuse earlier boss contexts only for structural patterns.
4. Inspect `$9F99/$AA57/$AAF0`, helper `$A1FF`, bank6 `$90CE-$90EA`, and fixed story progression around `$F016` / `$E3F7+`.
5. Reuse `BATTLE_TECHNIQUES.md`, `BOSS_BATTLE_DAMAGE.md`, `BOSS_DODGE.md`, `BOSS_BATTLE_RESOURCES.md` and generic release ownership instead of reopening them.
6. Drive is private ROM/evidence storage only; it never owns a separate `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `boss-context-stage-09-pisces`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, evidence, a discarded hypothesis or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.

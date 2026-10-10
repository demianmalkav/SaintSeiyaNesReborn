# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

## CURRENT

- Phase: `ORIGINAL SPEC / boss context stage $08 Aquarius`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#127` — complete stage `$050E=$05` Virgo/Shaka encounter + Ikki substitution/platform-handoff machine.
- Merge commit: `b209b2106a6ea48b15fe69a71976d51e34d641be`
- Exact final PR head: `2007ea765f28e245567fca238c9a2bf2d987816f`
- Verification on that exact head:
  - `ORIGINAL SPEC tests` #324: `SUCCESS`
  - `Original Spec` #520: `SUCCESS`
  - build/self-test/password compatibility: `SUCCESS`
- PR #125 closes Leo/Aioria stage `$04`; PR #123 closes Taurus/Aldebaran stage `$01`.
- PR #121 closes the complete global `$00/$01` namespace.

## DONE

### Virgo/Shaka stage `$05` — PR #127

Closed stage-local handlers:

```text
initialization       $9A28
Talk                 $9E1B
post-Bronze action   $A661
post-Gold response   $A7B3
Gold selector        bank6 $9074+
```

Promoted semantics:

- active Ikki + `$0683==0` writes `$0673=$3F` and releases `$DD` into the already-closed reload `$90` / high-presentation owner;
- non-Ikki re-entry with `$0673=$3F` performs the long Ikki substitution, writes active Saint `$0533=4`, arms `$0690=$FF`, changes marker to `$2F`, and exits shared intro with `$0670=$03`;
- active Ikki + `$0683!=0` returns active Saint to Seiya and releases `$FE`, which fixed handling owns as special stage-completion/progression;
- Virgo Talk is active-Saint/phase driven: non-Ikki always forces a Gold response; Ikki with `$067C==0` does not; Ikki with `$067C!=0` does;
- the first Ikki post-Bronze action while `$067C==0` always writes platform substate `$02=$0D` and release `$0670=$02`;
- platform `$0D` is a real playable detour; its known gate (`X >= $B4`, `Y==$30`, jump phase 0) exits through paired global `$3D` reload;
- fixed special resume for release `$02` skips ordinary scratch reset and increments `$067C`, marking the post-detour Ikki phase;
- post-detour Ikki clears `$0690`, unlocking ordinary Bronze hits;
- first healthy post-detour phase can run the one-time `$06E0` event; later landed hits use the hit-feedback path;
- first non-healthy Shaka condition (`$EB=$01` or `$FF`) with Ikki is intercepted before ordinary victory: `$064D++` and `$0683++` create the scripted threshold phase;
- the next Ikki post-Bronze pass with `$0683!=0` releases `$FE`, completing the phase rather than using generic `$01` victory;
- Ikki post-Gold uses a special defeat rule: `$EA=$FF` always defeats, and `$EA=$01` also defeats once `$0683!=0`;
- stage-5 Gold slots: non-Ikki reaches only slots 0/1 through `$065F&1`; Ikki forces slot2; slot3 is unreachable;
- reachable stage-5 coefficient profiles are `42/28`, `24/36`, and Ikki-forced `37/22` (Cosmo/Life).

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/VirgoStage05Context.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/VirgoStage05ContextChecks.cs`
- `docs/reverse-engineering/BOSS_CONTEXT_STAGE_05_VIRGO.md`
- PR #127

Do not reopen Virgo without contradictory ROM evidence or a failing fixture.

## EVIDENCE FOR NEXT

### Why stage `$08` Aquarius/Camus is next

Aquarius is selected by semantic novelty. It combines battle progression with Hyoga-specific technique growth and an explicit Gold-selector special case driven by `$06B8` plus dodge history.

Stage handlers:

```text
initialization       $9B14
Talk                 $9F00
post-Bronze action   $A8FC
post-Gold response   $A9D3
Gold selector        bank6 $90A8+
```

Known anchors already promoted elsewhere:

- Hyoga has two progression unlock writers, including `$9B53/$9B56` and `$9F47/$9F4A`, that increment persistent technique count `$0588` and active menu count `$0696`;
- bank6 selector has an Aquarius-specific branch: when `$06B8!=0`, force Gold slot2; otherwise total dodge attempts `$0677+$0678` selects slot0/1 before falling through;
- stage-8 coefficient row is `30/20`, `21/31`, `30/20`, `21/31`; reachability must prove which of these four structural slots are actually selectable in each phase;
- the stage contains redirection/progression state around `$06B8`, `$06E1`, `$066F`, dodge counters and Hyoga identity that is not present in Taurus/Leo/Virgo.

## OPEN

1. Close `$9B14` initialization, including `$067D/$06B8` gating, active-Saint conditions and the first Hyoga technique-unlock writer.
2. Close `$9F00` Talk, including `$06B8`, `$06E1`, `$066F`, Hyoga-specific progression and the second technique unlock.
3. Close `$A8FC` post-Bronze and `$A9D3` post-Gold end-to-end, including any stage redirection and terminal releases.
4. Resolve the exact lifecycle/persistence of `$06B8/$06E1` and how they partition first/final Camus/Aquarius phases.
5. Trace stage-8 Gold selector reachability from `$06B8` and `$0677+$0678`; classify structural but unreachable slots.
6. Compose the two Hyoga technique unlocks with the already-closed contiguous technique-count model rather than duplicating it.
7. Implement one complete stage-8 context + discriminating fixtures and documentation.
8. Stop only when every reachable Aquarius path has a known successor or already-closed subsystem owner.

## NEXT

**Close the complete stage `$08` Aquarius/Camus context end-to-end, including Hyoga technique unlocks, phase/redirection state, Talk/post-action branches and Gold-slot reachability.**

Completion criterion:

> Starting from `$050E=$08`, produce an evidence-backed executable graph covering every reachable `$9B14/$9F00/$A8FC/$A9D3` branch, resolve `$06B8/$06E1/$066F/$0677/$0678` and the two `$0588/$0696` unlock events, prove reachable `$0680` Gold slots, and join every terminal/nonterminal release to an already-closed owner without duplicating generic damage/dodge/resource arithmetic.

## BLOCKERS

- None. Canonical ROM, Taurus/Leo/Virgo templates, battle primitives and stage-8 handlers are available.

## RECOVERY CONTRACT

1. Read this file from `main`.
2. Reconcile it with newer merged Git history if present.
3. Read `BOSS_CONTEXT_STAGE_05_VIRGO.md` plus the prior boss templates only as composition references.
4. Inspect `$9B14/$9F00/$A8FC/$A9D3` and bank6 `$90A8+`.
5. Reuse `BATTLE_TECHNIQUES.md`, `BOSS_BATTLE_DAMAGE.md`, `BOSS_DODGE.md` and release/reload specs instead of reopening them.
6. Drive is private ROM/evidence storage only; it never owns a separate `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `boss-context-stage-08-aquarius`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, evidence, a discarded hypothesis or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.

# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

Technical subsystem documents own detailed evidence and semantics. This file records the accepted checkpoint, open boundaries and one executable `NEXT`.

## CURRENT

- Phase: `ORIGINAL SPEC / boss context stage $05 Virgo`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#125` — complete stage `$050E=$04` Leo/Aioria boss context.
- Merge commit: `b77d863ae8e26769f4f4235eb2ca1771bf03643e`
- Exact final PR head: `5744aa1d7673c0ff0777a85e71a2a56fa2370112`
- Verification gate on that exact head:
  - `ORIGINAL SPEC tests` run `#320`: `SUCCESS`
  - `Original Spec` run `#516`: `SUCCESS`
  - build, OriginalSpec self-test and password compatibility fixture: `SUCCESS`
- PR `#123` closed Taurus/Aldebaran stage `$01`, the first complete boss-context template.
- PR `#121` closed the complete global `$00/$01` namespace at exactly 59 produced values / 197 structural-but-unreachable values.
- PR `#119` closed exact front-end/title state `$50` and the executable CHR31 -> RAM overlay mechanism.
- PRs `#109/#111/#113/#115/#117` and the promoted platform/reload checkpoints remain closed unless contradictory evidence appears.

## DONE

### Taurus/Aldebaran stage `$01` — PR #123

First complete boss context. Init/Talk/post-Bronze/post-Gold, weakening persistence, command-loop composition and terminal `$0670=$01/$FF` releases are closed. Use `BOSS_CONTEXT_STAGE_01_TAURUS.md` as the baseline composition template; do not reopen it without new executable evidence or a failing fixture.

### Leo/Aioria stage `$04` — PR #125

Stage-specific handlers are closed end-to-end:

```text
initialization       $989D
Talk                 $9DD8
post-Bronze action   $A5B3
post-Gold response   $A63E
```

Promoted Leo semantics:

- fixed stage-4 battle entry re-arms scripted Bronze blocker `$0690=$FF` after common `$A973` reset;
- nonzero `$0690` is a real invulnerability/hit gate: fixed `$FAB9+` forces generic Bronze hit token `$06BC=0`;
- the stage intro is gated by `$068E==0` and active Saint Seiya (`$F36F[4]=$00`); a non-Seiya route can enter the ordinary loop without executing `$989D`;
- `$989D` writes `$ED=1` and executes two unconditional `INC $0681`, so a normal first Seiya intro changes weakening `0 -> 2`; the generic damage layer interprets tier `>=2` as quarter Gold damage;
- `$ED` selects between two victory presentation branches, but both converge on terminal `$0670=$01`; `$ED!=0` follows an executed Seiya intro, while an intro-skipped route can retain `$ED=0`;
- Talk topology is not Taurus-like: pre `$066F=0` increments conversation and transient `$DC`, forcing a Gold response; pre `$066F=1` is the unique non-forcing branch; pre `$066F>=2` resumes `INC $066F` + forced Gold responses;
- second Talk uses a Shun-specific text branch and, only when `$F1==0`, calls `$A1F4` to clear `$0690`, unlocking normal Bronze hits;
- `$F1` is not cleared by ordinary battle-runtime reset. It is incremented by `$A5B3` whenever low-condition Aioria is processed with a non-Seiya active, and survives ordinary defeat/re-entry until a broader zero-page reset;
- low-condition Aioria + Seiya leaves `$0690/$F1` unchanged; low-condition + non-Seiya writes `$0690=$FF` and increments `$F1`, re-locking Bronze hits;
- `$A63E` gives the familiar one-time low-player `$064D` event and terminal defeat `$0670=$FF`;
- stage-4 Gold selection has one canonical writer: `$0680=$065F&1`, so only slots `0/1` are reachable. Slots `2/3` exist in the coefficient table but are unreachable for Leo;
- reachable Leo raw coefficient profiles are slot0 `48/32` and slot1 `32/48` (Cosmo/Life), then composed with generic weakening/dodge/damage;
- ordinary defeat/re-entry clears local scratch and re-arms `$0690`, while preserving `$0681/$ED/$F1`.

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/LeoStage04Context.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/LeoStage04ContextChecks.cs`
- `docs/reverse-engineering/BOSS_CONTEXT_STAGE_04_LEO.md`
- PR `#125`

Do not reopen Leo without contradictory ROM evidence or a failing fixture.

## EVIDENCE FOR NEXT

### Why stage `$05` Virgo/Shaka is selected

Virgo is the next context by **semantic novelty**, not numeric order. It introduces a character-substitution / special-handoff structure that Taurus and Leo do not contain, and the generic Gold selector has an explicit stage-5 + Ikki branch.

Stage `$05` handlers are:

```text
initialization       $9A28
Talk                 $9E1B
post-Bronze action   $A661
post-Gold response   $A7B3
```

Preliminary canonical ROM anchors already established for the next pass:

### `$9A28` initialization / Ikki handoff

- active Saint `$0533==4` (Ikki) is treated specially;
- if Ikki and `$0683==0`, handler writes `$0673=$3F` and release `$0670=$DD`;
- if Ikki and `$0683!=0`, it switches active Saint to index `0` and emits release `$0670=$FE`;
- for non-Ikki, `$0673==$3F` enters a long transition that ultimately writes active Saint `$0533=4` (Ikki), sets `$0690=$FF`, and enters shared flow through `$9C56`;
- this is a real substitution/handoff machine and must be composed with the already-promoted reload/platform release semantics rather than flattened as presentation.

### `$9E1B` Talk

- non-Ikki uses character-indexed dialogue and ends with transient `$DC++`, therefore forcing the generic Gold response;
- Ikki + `$067C==0` takes a distinct dialogue path and returns **without** `$DC`;
- Ikki + `$067C!=0` takes another dialogue path and does raise `$DC`, forcing the Gold response;
- unlike Taurus/Leo, Talk behavior is therefore primarily selected by active Saint and `$067C`, not a simple `$066F` progression.

### `$A661` post-Bronze

- non-Ikki follows ordinary opponent classifier `$EB`: defeated -> victory `$0670=$01`; healthy/low branches use `$064E` presentation/event behavior;
- Ikki takes a separate large state machine keyed by `$067C`, `$0683`, `$064D`, `$06E0`, `$06BC` and `$0690`;
- Ikki + `$067C==0` reaches a special release `$0670=$02` while writing platform substate `$02=$0D`;
- later Ikki paths can increment `$0683`, clear `$0690`, or emit release `$0670=$FE` after a substantial transition;
- these releases must be joined to already-known handoff/reload semantics before the context can be called closed.

### `$A7B3` post-Gold

- non-Ikki uses the familiar classifier: `$EA=$FF` -> defeat `$0670=$FF`, with one-time low-player `$064D` event;
- Ikki uses a distinct branch: `$EA=$FF` defeats normally, and when `$0683!=0`, `$EA=$01` is also promoted to the defeat release;
- this special low-condition rule must be proved in the complete Ikki phase graph.

### Gold attack selector `$0680`

Bank-6 `$9074+` contains an explicit special case:

```text
if $050E == $05 and $0533 == $04:
    $0680 = $02
else:
    fall through later selector logic
```

Thus Shaka with active Ikki can force structural Gold slot `2`; this is the first currently selected context where the stage/character pair changes reachable Gold-slot topology directly.

## OPEN

1. Close `$9A28` as a real stage-5 substitution/handoff machine, including exact meaning/reachability of `$0673=$3F`, `$0683`, release `$DD`, release `$FE`, active-Saint swaps and shared `$9C56` return.
2. Close `$9E1B` Talk for non-Ikki vs Ikki and both `$067C` branches, including which paths force a Gold response through transient `$DC`.
3. Close `$A661` post-Bronze separately for non-Ikki and Ikki; resolve `$067C/$0683/$064D/$06E0/$06BC/$0690` and releases `$01/$02/$FE`.
4. Close `$A7B3` post-Gold, including the Ikki-specific `$EA==01 && $0683!=0` defeat rule.
5. Join special release `$0670=$02` + `$02=$0D`, `$DD`, and `$FE` to the already-promoted platform/reload/selector graph instead of treating them as terminal black boxes.
6. Trace stage-5 `$0680` selection end-to-end: prove Ikki-forced slot2 and determine reachable non-Ikki slots/profiles.
7. Implement one complete Virgo/Shaka context model with discriminating fixtures and a structural encounter/handoff graph.
8. Stop only when every reachable stage-5 path has a known successor or an already-closed subsystem owner.

## NEXT

**Close the complete stage `$05` Virgo/Shaka context, including the Ikki substitution/special-handoff machine, Talk branches, post-action state machines and Gold-slot reachability.**

Completion criterion:

> Starting from stage `$050E=$05`, produce an evidence-backed executable graph covering every reachable non-Ikki and Ikki path through `$9A28/$9E1B/$A661/$A7B3`, resolve `$067C/$0683/$064D/$06E0/$0690` and releases `$01/$02/$DD/$FE/$FF`, join each nonterminal release to its already-promoted platform/reload successor, and prove reachable `$0680` Gold slots without duplicating generic damage/dodge arithmetic.

Required sequence:

1. close initialization/substitution `$9A28` and all active-Saint / `$0683/$0673` branches;
2. trace each special release (`$DD`, `$FE`, and `$02` with `$02=$0D`) into its verified successor;
3. close Talk `$9E1B` and `$067C`-dependent forced-counterattack behavior;
4. close post-Bronze `$A661` in non-Ikki and Ikki phases;
5. close post-Gold `$A7B3`, including the Ikki low-condition special defeat;
6. close stage-5 Gold selector reachability, especially forced Ikki slot2;
7. implement the smallest complete Virgo context + discriminating fixtures and documentation;
8. run both verification workflows and checkpoint only on exact green head.

## BLOCKERS

- None. Canonical ROM, Taurus/Leo context templates, generic battle primitives and the stage-5 handlers are available.

## RECOVERY CONTRACT

A new session must be able to resume without chat history.

Recovery order:

1. read this file from `main`;
2. reconcile `CURRENT` with newer merged Git history if any exists;
3. inspect `BOSS_CONTEXT_STAGE_01_TAURUS.md` and `BOSS_CONTEXT_STAGE_04_LEO.md` as composition templates;
4. inspect `BATTLE_EVENT_DISPATCH.md`, `BOSS_BATTLE_RESOURCES.md`, `BOSS_BATTLE_DAMAGE.md`, `BOSS_DODGE.md` and `BATTLE_TECHNIQUES.md`;
5. inspect bank-5 Virgo handlers `$9A28/$9E1B/$A661/$A7B3` and bank-6 selector `$9074+`;
6. inspect already-promoted platform/reload release semantics when joining `$DD/$FE/$02` handoffs;
7. use `docs/REVERSE_ENGINEERING_STATUS.md` as navigation only;
8. use Drive only for private ROM/evidence; Drive never owns a separate `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `boss-context-stage-05-virgo`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, new evidence, an evidence map, a discarded hypothesis, or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.

## CONTINUE SEMANTICS

When the user says `continúa` or `next` with no narrower instruction:

1. read this file first;
2. reconcile it with current `main` if repository history is newer;
3. execute the single `NEXT` in FAST mode until material progress or a real blocker;
4. run the smallest relevant VERIFY gate;
5. checkpoint only after verification;
6. update `DONE / EVIDENCE / OPEN / NEXT / BLOCKERS / ANTI-LOOP` before ending the cycle.

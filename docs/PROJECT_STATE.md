# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

Technical subsystem documents own detailed evidence and semantics. This file records the accepted checkpoint, open boundaries and one executable `NEXT`.

## CURRENT

- Phase: `ORIGINAL SPEC / boss context stage $04 Leo`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#123` — complete stage `$050E=$01` Taurus/Aldebaran boss context.
- Merge commit: `446b54395ce378119e59fcc12d7a7e8174fe5b3b`
- Exact final PR head: `ff0e0064a5a3d194bf623e51d9b785a91cefe742`
- Verification gate on that exact head:
  - `ORIGINAL SPEC tests` run `#316`: `SUCCESS`
  - `Original Spec` run `#510`: `SUCCESS`
  - build, OriginalSpec self-test and password compatibility fixture: `SUCCESS`
- PR `#121` closed the complete global `$00/$01` namespace at exactly 59 produced values / 197 structural-but-unreachable values.
- PR `#119` closed exact front-end/title state `$50` and the executable CHR31 -> RAM overlay mechanism.
- PR `#117` remains structurally authoritative for `$30-$4D`, semantically superseded by #119 as the front-end attract/presentation loop.
- PRs `#109/#111/#113/#115` own the global dispatcher, `$11-$14`, fatal `$60`, and `$91-$99`.
- PRs `#94/#96/#98/#102/#104/#107` own platform exits, narrative chains, selector and reload destinations.

## DONE

### Global engine-state namespace — PR #121

All 256 possible `$00` values are classified. Canonical execution produces exactly 59 values:

```text
transient: $00 $10 $30 $3D $90
reachable: $11-$14, $20, $31-$38, $40-$4D, $50, $60,
           $70-$75, $80-$89, $91-$99
```

The other 197 values have no canonical executable producer. PRG direct/indirect writers and the two confirmed CHR31 -> RAM overlays were audited. Do not reopen this namespace without contradictory executable evidence or a failing fixture.

### Taurus/Aldebaran stage `$01` — PR #123

Stage-specific handlers are closed end-to-end:

```text
initialization       $97F8
Talk                 $9D2C
post-Bronze action   $A3A2
post-Gold response   $A415
```

Promoted Taurus semantics:

- common `$A973` battle-runtime reset clears `$066F/$064D/$064E/$DD/...` but deliberately preserves weakening `$0681`;
- `$97F8/$9C3D` performs the Taurus intro, restores real stage `$01`, sets `$068E=1`, emits transient `$0670=$03`, then fixed entry flow clears `$0670` for the command loop;
- Talk progression is exactly `$066F: 0 -> 1 -> 2`;
- the second Talk changes `$0681:0->1` once; an already-nonzero weakening tier is not incremented;
- third and later Talks leave `$066F=2`, raise transient `$DC`, and the fixed caller immediately consumes `$DC` by forcing a Gold counterattack;
- `$A3A2` terminates victory when `$EB=$FF` via `$0670=$01`;
- first surviving low-opponent condition (`$EB=$01`, `$DD!=5`) latches `$DD=5` and increments feedback `$064E` before the generic `$06BC` hit-token test;
- otherwise `$06BC=0` increments `$064E`; landed hits with no first-low event make no Taurus-local state change;
- `$064E` is feedback/event state in Taurus, not an AI/victory gate;
- `$A415` terminates defeat when `$EA=$FF` via `$0670=$FF`;
- first `$EA=$01` while `$064D=0` performs a one-time event and latches `$064D=1`;
- weakening `$0681` survives defeat/re-entry while ephemeral encounter counters reset; normal victory progression later clears it when leaving the encounter.

Turn composition is now explicit:

```text
Bronze action -> generic hit/damage -> Taurus $A3A2
  -> victory, or
  -> generic Gold counterattack/dodge/damage -> Taurus $A415
     -> defeat/event/continue

Talk #3+ -> forced generic Gold counterattack -> Taurus $A415
```

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/TaurusStage01Context.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/TaurusStage01ContextChecks.cs`
- `docs/reverse-engineering/BOSS_CONTEXT_STAGE_01_TAURUS.md`
- PR `#123`

Do not reopen Taurus without contradictory ROM evidence or a failing fixture.

## EVIDENCE FOR NEXT

### Why stage `$04` Leo/Aioria is selected

Taurus established the first complete encounter-context template. Leo is the next useful checkpoint because it introduces stage-local semantics not present in Taurus instead of merely repeating the same pattern.

Stage `$04` handlers are:

```text
initialization       $989D
Talk                 $9DD8
post-Bronze action   $A5B3
post-Gold response   $A63E
```

Preliminary ROM anchors for the next pass:

- `$989D` seeds `$ED=1`, performs a multi-phase intro/event setup, manipulates progression/presentation flags, and contains two distinct `INC $0681` sites around `$996A` and `$9A05`; their exact reachable conditions and semantic phases must be proved rather than collapsed into a generic weakening rule;
- `$9DD8` differs materially from Taurus: first Talk (`$066F=0`) advances conversation and raises transient `$DC`, therefore forcing a Gold counterattack; the exactly-1 branch performs character-dependent dialogue and can call `$A1F4` when `$F1==0`, then advances to 2; later Talks return to the forcing branch and continue advancing `$066F`;
- `$A5B3` classifies opponent condition through `$ACD6`; `$EB=$FF` reaches victory `$0670=$01`, with an `$ED`-dependent presentation branch that must be resolved for reachability;
- for surviving low-condition Aioria (`$EB=$01`), `$A5B3` distinguishes active Saint `$0533`: Seiya returns without the non-Seiya effect, while non-Seiya writes `$0690=$FF` and increments `$F1`;
- `$0690` is already known structurally as a scripted player-hit block, but its exact Leo lifecycle and relationship to `$F1` must be closed in context;
- `$A63E` follows the familiar post-Gold classifier shape: `$EA=$FF` -> defeat `$0670=$FF`; first low-player condition can latch a one-time `$064D` event;
- unlike Taurus, stage `$04` has materially different Gold attack coefficient pairs by `$0680` slot:

```text
slot 0: 48/32
slot 1: 32/48
slot 2: 48/32
slot 3: 32/48
```

Therefore Leo context closure must trace enough of `$0680` selection/forcing to determine which numeric damage profile is reachable under each stage-local branch. Generic damage arithmetic remains owned by `BOSS_BATTLE_DAMAGE.md`.

## OPEN

1. Trace `$989D` completely and prove the reachable lifecycle of `$ED` and both `$0681` increments.
2. Close `$9DD8` Talk reachability, including `$066F`, transient `$DC`, `$F1`, active-Saint branches and helper `$A1F4`.
3. Trace `$A5B3` for all reachable `$EB/$0533/$ED/$F1/$0690` combinations and terminal victory.
4. Trace `$A63E` for all reachable `$EA/$064D` branches and terminal defeat.
5. Resolve the Leo-local lifecycle/meaning of `$F1` and `$0690`, including whether they persist/reset within encounter retries.
6. Trace `$0680` selection/forcing sufficiently to map reachable Leo Gold attack profiles; compose existing coefficient/damage code rather than reimplement it.
7. Produce a complete stage-4 encounter graph and executable clean-room context with discriminating fixtures.
8. Stop terminal paths at `$0670=$01/$FF`, which remain owned by the already-promoted reload/progression machinery.

## NEXT

**Close the complete stage `$04` Leo/Aioria boss context end-to-end, composing `$989D/$9DD8/$A5B3/$A63E` with the already-promoted generic battle mechanics.**

Completion criterion:

> Starting from stage `$050E=$04` initialization, produce an evidence-backed executable encounter graph covering every reachable Leo Talk/event phase, `$ED/$F1/$0690/$0681` lifecycle, Bronze and Gold post-action branches, reachable `$0680` attack profiles, and terminal victory/defeat release through `$0670`, without duplicating already-closed resource, damage or dodge arithmetic.

Required sequence:

1. close `$989D` initialization/event phases and both `$0681` writers;
2. close `$9DD8` Talk phases, forced-counterattack behavior and `$A1F4/$F1` effects;
3. close `$A5B3` post-Bronze branches including Seiya vs non-Seiya low-condition behavior, `$0690`, `$F1`, `$ED` and victory;
4. close `$A63E` post-Gold low-player/defeat branches;
5. trace reachable `$0680` selection/forcing and compose the existing stage-4 coefficient pairs;
6. implement the smallest complete Leo context + discriminating fixtures;
7. document the full encounter graph and retry/persistence rules;
8. run both verification workflows and checkpoint only on exact green head.

## BLOCKERS

- None. Canonical ROM, Taurus context template, generic battle primitives and stage-4 handlers are available.

## RECOVERY CONTRACT

A new session must be able to resume without chat history.

Recovery order:

1. read this file from `main`;
2. reconcile `CURRENT` with newer merged Git history if any exists;
3. inspect `BOSS_CONTEXT_STAGE_01_TAURUS.md` as the context-composition template;
4. inspect `BATTLE_EVENT_DISPATCH.md`, `BOSS_BATTLE_RESOURCES.md`, `BOSS_BATTLE_DAMAGE.md`, `BOSS_DODGE.md` and `BATTLE_TECHNIQUES.md`;
5. inspect bank-5 Leo handlers `$989D`, `$9DD8`, `$A5B3`, `$A63E` and only the helpers that gate stage-local progression;
6. reuse promoted generic battle specifications rather than reopening formulas;
7. use `docs/REVERSE_ENGINEERING_STATUS.md` as navigation only;
8. use Drive only for private ROM/evidence; Drive never owns a separate `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `boss-context-stage-04-leo`
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

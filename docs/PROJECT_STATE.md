# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

Technical subsystem documents own detailed evidence and semantics. This file records the accepted checkpoint, open boundaries and one executable `NEXT`.

## CURRENT

- Phase: `ORIGINAL SPEC / boss context stage $01 Taurus`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#121` — complete global engine-state `$00/$01` reachability namespace.
- Merge commit: `ab6859986b8d62b05943b2a011e6257a5933f308`
- Exact final PR head: `ce56bb74663748edb71ff6fe15477575bd497114`
- Verification gate on that exact head:
  - `ORIGINAL SPEC tests` run `#312`: `SUCCESS`
  - `Original Spec` run `#506`: `SUCCESS`
  - build, OriginalSpec self-test and password compatibility fixture: `SUCCESS`
- PR `#119` closed exact front-end/title state `$50` and the executable CHR31 -> RAM overlay mechanism.
- PR `#117` remains structurally authoritative for `$30-$4D`, semantically superseded by #119 as the front-end attract/presentation loop.
- PRs `#109/#111/#113/#115` own the global dispatcher, `$11-$14`, fatal `$60`, and `$91-$99`.
- PRs `#94/#96/#98/#102/#104/#107` remain authoritative for platform exits, narrative chains, selector and reload destinations.

## DONE

### Global engine-state namespace — PR #121

Every possible byte value of live global state `$00` is now classified. Canonical execution produces exactly **59 values**: five bootstrap/handoff transients plus 54 ordinary reachable states.

Bootstrap/handoff transients:

```text
$00, $10, $30, $3D, $90
```

Ordinary reachable states:

```text
$11-$14
$20
$31-$38
$40-$4D
$50
$60
$70-$75
$80-$89
$91-$99
```

The remaining **197 values are structural-but-unreachable**. Important closed gaps include:

```text
$01-$0F
$15-$1F
$21-$2F
$39-$3C / $3E-$3F
$4E-$4F
$51-$5F
$61-$6F
$76-$7F
$8A-$8F
$9A-$FF
```

`$3D` is deliberately excluded from the `$39-$3F` dead-gap statement because it is independently reachable as the reload bridge.

Binary audit against the canonical ROM:

- 188 raw direct `$00/$01` writer byte-pattern candidates;
- 54 true direct writer instructions after executable/dynamic-path reconciliation;
- 134 rejected data/operand/inline-table false positives;
- no reachable `DEC $00`;
- 241 reachable indirect stores audited with no `$0000/$0001` destination;
- CHR31 front-end/password overlays separately disassembled (77/73 reachable instructions), neither writing global `$00/$01`;
- helper `$8023` has exactly two PRG callsites and no third promoted executable-overlay source.

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/EngineStateReachability.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/EngineStateReachabilityChecks.cs`
- `docs/reverse-engineering/ENGINE_STATE_REACHABILITY.md`
- PR `#121`

Do not reopen the top-level engine-state namespace without a concrete missing executable producer, contradictory ROM/trace evidence, or failing fixture.

### Major gameplay foundations already promoted

Platform movement, jumping, attacks, collision, entities/hazards, resource drain/failure, platform exits/reload and narrative handoffs are HIGH maturity. Boss battle primitives already promoted include:

- active/persistent Life and Cosmo resources plus condition classifiers (`$EA/$EB`);
- complete Bronze/Gold numeric damage pipeline;
- Bronze technique slots/availability/unlocks;
- Gold counterattack dodge window and success/failure counters;
- stage-indexed initialization, Talk, post-Bronze and post-Gold dispatch tables.

The remaining boss gap is principally **context composition**: prove each stage's event machine end-to-end instead of retaining isolated mechanics and individual handlers.

## EVIDENCE FOR NEXT

### Why stage `$01` Taurus is selected

`BATTLE_EVENT_DISPATCH.md` explicitly identifies Taurus/Aldebaran as the simplest complete per-stage pattern. Stage `$01` has four concrete bank-5 handlers:

```text
initialization       $97F8
Talk/interaction     $9D2C
post-Bronze action   $A3A2
post-Gold response   $A415
```

The surrounding generic battle pipeline is already promoted, making this a bounded composition problem rather than a new combat-engine excavation.

Direct ROM anchors already established for the next pass:

- `$97F8` clears stage-local `$064D`, runs common battle setup, initializes display/event fields and enters the shared encounter flow;
- `$9D2C` is keyed by conversation counter `$066F`; the first two Talk phases advance it, and the second phase increments weakening tier `$0681` exactly once when it is zero;
- `$A3A2` calls the opponent condition classifier `$ACD6`; opponent defeated (`$EB=$FF`) terminates through `$ACAA` with release/result `$0670=$01`; surviving branches update stage-local turn/event state including `$064E`;
- `$A415` calls the player condition classifier `$AD55`; player defeated (`$EA=$FF`) terminates through `$ACAA` with `$0670=$FF`; when player condition becomes `$01` and `$064D==0`, a one-time scripted event runs and increments `$064D`;
- stage `$01` opponent damage coefficients are identical in all four structural Gold-technique slots (`19/29`), so attack-slot identity cannot alter raw numeric damage in this stage;
- Talk weakening `$0681=1` therefore halves the already-promoted Gold raw damage pipeline.

This stage is the correct first template for defining what “complete boss context” means before applying the same method to Leo, Virgo, Aquarius, Pisces and Saga.

## OPEN

1. Close Taurus stage `$01` from initialization through every Talk phase, Bronze attack result, Gold response and terminal win/loss release.
2. Prove how `$064D`, `$064E`, `$066F`, `$0681`, `$EA`, `$EB`, `$06BC`, `$DD` and `$0670` cooperate, distinguishing event counters from generic battle state.
3. Compose—not duplicate—the promoted damage, technique, dodge and resource specifications.
4. Identify all reachable Taurus turn/event paths and reject structurally present but unreachable branches.
5. Resolve the exact effect of `$A3A2` branches around `$EB`, `$DD`, `$06BC`, and `$064E`, and the one-time `$A415` low-condition event.
6. Prove both terminal outcomes through `$ACAA`: victory `$0670=$01` and player defeat `$0670=$FF`, then stop at the already-promoted reload/selector boundary.
7. Implement one executable stage-1 encounter-context machine with discriminating fixtures and documentation.
8. After Taurus becomes the template, continue remaining boss contexts stage-by-stage before renderer/RNG/audio unless new evidence requires a dependency first.

## NEXT

**Close the complete stage `$01` Taurus/Aldebaran boss context end-to-end, composing the four stage-specific event handlers with the already-promoted generic battle mechanics.**

Completion criterion:

> Starting from stage `$050E=$01` initialization, produce an evidence-backed executable encounter graph that covers every reachable Taurus Talk/event phase, Bronze and Gold post-action branch, scripted weakening/condition event, and terminal victory/defeat release through `$0670`, without reimplementing already-closed numeric damage, resource, technique or dodge internals.

Required sequence:

1. trace `$97F8` initialization and normalize the Taurus-local starting counters/flags;
2. close `$9D2C` Talk phases and exact `$066F/$0681` effects;
3. trace `$A3A2` after Bronze action for all reachable `$EB/$DD/$06BC` combinations and `$064E` updates;
4. trace `$A415` after Gold response for all reachable `$EA/$064D` branches;
5. connect terminal `$ACAA` releases to already-promoted reload semantics and stop there;
6. compose stage-1 context with existing battle damage/resources/techniques/dodge models;
7. implement the smallest executable Taurus context + discriminating fixtures;
8. document and run both verification workflows.

## BLOCKERS

- None. Canonical ROM, stage dispatch tables and generic boss mechanics are available.

## RECOVERY CONTRACT

A new session must be able to resume without chat history.

Recovery order:

1. read this file from `main`;
2. reconcile `CURRENT` with newer merged Git history if any exists;
3. inspect `BATTLE_EVENT_DISPATCH.md`, `BOSS_BATTLE_RESOURCES.md`, `BOSS_BATTLE_DAMAGE.md`, `BOSS_DODGE.md` and `BATTLE_TECHNIQUES.md`;
4. inspect bank-5 Taurus handlers `$97F8`, `$9D2C`, `$A3A2`, `$A415` plus helpers only where they gate stage-local progression;
5. reuse promoted generic battle specifications rather than reopening formulas;
6. use `docs/REVERSE_ENGINEERING_STATUS.md` as navigation only;
7. use `docs/WORK_PROTOCOL.md` for execution rules;
8. use Drive only to locate private ROM/evidence; Drive never owns a separate `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `boss-context-stage-01-taurus`
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

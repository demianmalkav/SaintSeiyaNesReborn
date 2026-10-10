# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

## CURRENT

- Phase: `ORIGINAL SPEC / final boss stage $0A Saga`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#133` — complete final-special story stage `$067D/$050E=$0C`, including both Pisces-derived entry variants, command ownership, rose-clearing action and exact Saga handoff.
- Merge commit: `140f7eadf62e3a05d66fbe5448aa542005ed4d76`
- Exact final PR head: `d08f8a3a59c945409d113978d6bccce96bffa987`
- Verification on that exact head:
  - `ORIGINAL SPEC tests` #337: `SUCCESS`
  - `Original Spec` #533: `SUCCESS`
  - build/self-test/password compatibility: `SUCCESS`
- PR #131 closes Pisces/Aphrodite `$09`; PR #129 closes Aquarius/Camus `$08`; PR #127 closes Virgo/Shaka `$05`; PR #125 closes Leo/Aioria `$04`; PR #123 closes Taurus/Aldebaran `$01`.
- PR #121 closes the complete global `$00/$01` namespace.

## DONE

### Final-special story stage `$0C` — PR #133

Closed reachable control surface:

```text
entry ownership       fixed story progression + common reset
initialization         canonical path bypasses bank-5 init dispatcher
Talk                   bank5 $A1AD
Attack                 fixed $F08B+ + bank1 $ABF4+ + fixed $FF9F + bank0 $B900
Escape                 fixed $F14A+
resource allocation    suppressed at fixed $F041+
story advance          release $01 -> fixed $E399/$E3B3+
```

Promoted semantics:

- Pisces `$FE` creates exactly two stage-`$0C` entries: Shun-Pisces victory -> active Seiya with `$06CD=$0E/$0673=$3E`; Seiya-Pisces victory -> active Shun with `$06CD=$0B/$0673=$3B`;
- the apparent stage-12 init pointer is a table overrun decoding raw `$8D00`, but canonical `$0C` entry provably returns before `$97DB` because mandatory-Saint table `$F36F[$0C]=$FF` cannot match active Seiya/Shun;
- `$A1AD` contains no active-Saint branch: both entries display `$D3/$D5`; first Talk with `$066F=0` calls `$A1FF` with `#$10`, grants +1000 Seventh Sense through fixed `$F31E`, increments `$066F`, and emits no `$DC` or release; repeated Talk has no further reward;
- Escape is intercepted by message `$D4` and cannot advance;
- the resource-allocation command is suppressed/redraw-only at stage `$0C` and never enters `$FB89`;
- once Attack is chosen, final-special technique selection cannot be cancelled through the ordinary back path;
- bank-1 action code sets `$0632=$02`, clears `$06BC`, still presents the selected Bronze technique, calls special `$FF9F`, and explicitly skips generic opponent damage;
- `$FF9F -> bank0 $B900` temporarily writes `$050E=$12`, runs the rose-clearing effect until `$06C1=$40`, writes `$0632=$1A`, then restores `$050E=$0C`;
- fixed stage gates make post-Bronze, Gold technique selection/dodge/damage and post-Gold dispatchers unreachable for canonical stage `$0C`;
- the special Attack finishes by emitting release `$0670=$01`;
- fixed release `$01` ownership advances `$067D: $0C->$0D`, then ordinary descriptor lookup gives `$06CD=$0E/$0673=$3E` and story-stage table `$F016[$0D]=$0A` selects Saga;
- active Saint identity survives that handoff: the Saga boundary is reachable with active Seiya **or** active Shun.

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/FinalSpecialStage0CContext.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/FinalSpecialStage0CContextChecks.cs`
- `docs/reverse-engineering/FINAL_SPECIAL_STAGE_0C.md`
- corrected stage-`$0C` reachability in `docs/reverse-engineering/BATTLE_EVENT_DISPATCH.md`
- PR #133

Do not reopen final-special stage `$0C` without contradictory ROM evidence or a failing fixture.

## EVIDENCE FOR NEXT

### Why Saga stage `$0A` is next

The final-special closure proves the direct canonical boundary:

```text
rose-clear Attack -> release $01
$067D: $0C -> $0D
$E50B[$0D] = $0E
$06CD = $0E
$0673 = $3E
$F016[$0D] = $0A
```

Two active-Saint ingress variants remain reachable at the same Saga boundary:

```text
active Seiya -> $067D=$0D / $050E=$0A / $06CD=$0E / $0673=$3E
active Shun  -> $067D=$0D / $050E=$0A / $06CD=$0E / $0673=$3E
```

Saga is explicitly a phase machine driven by `$06CE`, not one monolithic boss handler.

Stage handlers and phase dispatch:

```text
initialization       $9B5D -> $06CE {0:$9B69, 1:$9B9E, 2:$9C2C}
Talk                 $9FF4 -> $06CE {0:$A000, 1:$A0E0, 2:$A115}
post-Bronze action   $AB18 -> $06CE {0:$AB24, 1:$AB62, 2:$AB6F}
post-Gold response   $AC05 -> $06CE {0:$AC11, 1:$AC3E, 2:$AC76}
Gold selector        bank6 $90EC+ -> $06CE {0:$9104, 1:$911D, 2:$9135}
```

Known ROM anchors already isolated:

- init phase `$9B69` performs a record handoff, forces active Ikki (`$0533=4`), increments `$06CE`, clears `$06CF/$06D0/$066F`, and arms scripted Bronze block `$0690=$FF`;
- init phase `$9B9E` contains the long scripted transition back toward Seiya, then increments `$06CE` again and clears `$06CF/$06D0/$066F/$064D/$0681`;
- init phase `$9C2C` emits release `$DD`, writes `$0673=$3F` and resets `$06CE=0`;
- phase-0 Talk `$A000` is driven by `$066F`, total dodge history helper `$A1EC`, `$06CF` and writes `$06D0`; phase-1 Talk `$A0E0` has a one-shot `$066F` branch using `$A1F4`; phase-2 Talk `$A115` is a larger scripted presentation/state path and increments `$066F`;
- post-Bronze phases 0/1 consume the generic player-condition classifier and can set `$06D0=$FF`; phase 2 instead consumes opponent condition, uses `$064D` as a first-low latch and has a distinct terminal path;
- post-Gold phases 0/1 have phase-specific defeat scripts; phase 2 distinguishes `$EA=$01` feedback from `$EA=$FF`, whose terminal path resets `$06CE` and releases `$DD`;
- Saga Gold selection is also phase-specific: phase 0 chooses slot0 unless `$06D0==$FF`, then slot1; phase 1 uses `$06D0` plus `$0649` to choose among slot0/slot2 and falls into phase-2 slot3 when `$06D0==$FF`; phase 2 forces slot3;
- stage-10 coefficient row is `35/23`, `30/30`, `60/60`, `60/60` (Cosmo/Life); actual phase reachability and names must be proven rather than inferred from the table;
- fixed `$F497` writes Seiya technique count `$0587/$0696=3` and grants +1000 Seventh Sense when its late-event gate fires; `BATTLE_TECHNIQUES.md` associates this with the late Saga sequence, but its exact Saga phase/reachability still needs to be joined to the stage machine;
- `$06CE/$06CF/$06D0`, `$066F`, `$064D`, `$0649`, `$0690`, `$0681`, `$06D4` and `$068F` are therefore the principal unresolved phase/progression state around the final boss.

## OPEN

1. Prove the exact canonical `$06CE` seed on both Seiya/Shun Saga ingress states, including any common reset or predecessor persistence that determines the first reachable init phase.
2. Close all reachable `$9B5D` initialization branches and prove the exact active-Saint lifecycle, especially the forced Ikki phase and the transition back to Seiya.
3. Close all three Talk handlers `$A000/$A0E0/$A115`, resolving `$066F/$06CF/$06D0`, dodge-history dependencies and every presentation/state mutation.
4. Close all three post-Bronze handlers `$AB24/$AB62/$AB6F` and all three post-Gold handlers `$AC11/$AC3E/$AC76`, joining their `$EA/$EB` conditions to existing generic classifiers without duplicating damage arithmetic.
5. Prove phase-specific Gold-slot reachability from `$9104/$911D/$9135`, including `$06D0/$0649`, and classify structural-but-unreachable slots/profiles per phase.
6. Join fixed `$F497` Seiya Rolling Crash/+1000 event to its exact reachable Saga state, resolving `$068F/$06D4` ownership instead of keeping it as a detached late-game writer.
7. Resolve every Saga release (`$FF`, `$DD`, `$01` and any other reachable token) through its fixed owner, including phase resets and any reload/re-entry cycles.
8. Prove the final terminal/progression boundary after the last Saga phase rather than assuming the ending from narrative order.
9. Implement one executable multi-phase Saga context, discriminating fixtures and evidence documentation only after the reachable graph is closed.
10. Stop only when both proven ingress variants have known successors through every reachable `$06CE` phase and all terminal/nonterminal paths join already-closed owners or an explicitly isolated final boundary.

## NEXT

**Close the complete stage `$0A` Saga final-boss machine end-to-end across all reachable `$06CE` phases, including active-Saint substitution/return, Talk and post-action state, phase-specific Gold selection, Seiya's late technique unlock, releases/re-entry cycles and the true final terminal boundary.**

Completion criterion:

> Starting from both proven `$067D=$0D -> $050E=$0A` ingress states (active Seiya and active Shun), produce an evidence-backed executable graph for every reachable `$06CE` phase through `$9B5D/$9FF4/$AB18/$AC05` and bank6 `$90EC+`, resolve `$06CF/$06D0/$066F/$064D/$0649/$0690/$0681` plus the `$F497` Seiya unlock gate, prove reachable `$0680` Gold slots per phase, and join every release/reload/terminal path to its fixed owner without reopening generic damage, dodge or resource arithmetic.

## BLOCKERS

- None. Canonical ROM, both exact Saga ingress variants, the final-special predecessor, generic boss primitives, phase dispatchers and fixed release/progression owners are available.

## RECOVERY CONTRACT

1. Read this file from `main`.
2. Reconcile it with newer merged Git history if present.
3. Read `FINAL_SPECIAL_STAGE_0C.md` only for the exact Saga predecessor/handoff states.
4. Inspect `$9B5D`, `$9FF4`, `$AB18`, `$AC05`, bank6 `$90EC+`, and fixed `$F497`; follow `$06CE` reachability before assigning phase semantics.
5. Reuse `BATTLE_TECHNIQUES.md`, `BOSS_BATTLE_DAMAGE.md`, `BOSS_DODGE.md`, `BOSS_BATTLE_RESOURCES.md`, `RESOURCE_ECONOMY.md` and generic release ownership instead of reopening them.
6. Treat the active Seiya/Shun ingress distinction as live until ROM evidence proves convergence; do not silently assume Seiya-only entry.
7. Drive is private ROM/evidence storage only; it never owns a separate `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `boss-context-stage-0a-saga-multiphase`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, evidence, a discarded hypothesis or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.
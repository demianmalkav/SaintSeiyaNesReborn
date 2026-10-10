# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

## CURRENT

- Phase: `ORIGINAL SPEC / battle-stage context coverage audit`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#137` — complete post-Saga ending tail from the exact Saga victory boundary to the original hard terminal.
- Merge commit: `f7592b7f586f81c6130082dbd5cd0b54f8ba8ba7`
- Exact final PR head: `049d8bec0c6da51ef8ba0a91ec5508e621db8051`
- Verification on that exact head:
  - `ORIGINAL SPEC tests` #345: `SUCCESS`
  - `Original Spec` #544: `SUCCESS`
  - build/self-test/password compatibility: `SUCCESS`
- PR #135 closes Saga `$0A`; PR #133 closes final-special `$0C`; PR #131 closes Pisces `$09`; PR #129 closes Aquarius `$08`; PR #127 closes Virgo `$05`; PR #125 closes Leo `$04`; PR #123 closes Taurus `$01`.
- PR #121 closes the complete global `$00/$01` namespace.

## DONE

### Post-Saga ending tail — PR #137

Closed the complete canonical successor chain after Saga victory:

```text
Saga phase-2 victory
 -> release $01
 -> $067D: $0D -> $0E
 -> $050E=$00 / $06CD=$00 / $0673=$30
 -> release rewritten to $05
 -> bootstrap $00->$20
 -> progress $0E maps to platform substate $11
 -> accepted gate: X >= $D0 / Y == $50 / jump phase 0
 -> $70->$71->$72->$73->$74->$75
 -> $80->$81->$82->$83->$84->$85->$86->$87->$88->$89
 -> $04=$8F / $00=$01=$3D / JMP $E100
 -> $068F=$8F
 -> $F381 ending branch
 -> $06CD/$0673/$06CC = $20/$20/$21
 -> PRG bank 0
 -> JMP $BC39
 -> presentation streams 0..9
 -> $BD2F: JMP $BD2F
```

Promoted semantics and correction:

- the first post-Saga bootstrap `$00->$20` is real and enters the final two-page platform substate `$11` as Seiya;
- the `$11` exit is the already-closed special `$70` path, not the ordinary `$3D` reload, and does not snapshot Saints;
- the existing `$70-$75` and `$80-$89` machines compose without new inferred transitions;
- state `$89` produces the exact `$8F` reload entry `$04=$8F/$00=$01=$3D/$03=0`;
- previous bounded analysis had incorrectly assumed `$F381` returned to `$E214`, allowing a second common state-zero commit/bootstrap;
- deeper control flow proves the opposite: with `$068F=$8F`, `$F381` writes `$06CD=$20`, `$0673=$20`, `$06CC=$21`, selects PRG bank 0 and tail-jumps at `$F3BC` to `$BC39`;
- because that is `JMP`, not `JSR`, `$F381` never returns to `$E214`: `$050E` normalization, `$00/$01=0`, `$C180` and a supposed second `$00->$20` bootstrap are unreachable on the ending path;
- bank-0 `$BC39-$BD2F` drives exactly ten final presentation streams in order, using pointers `$BDF2/$BE14/$BE48/$BE81/$BEB1/$BEE9/$BEFD/$BF3B/$BF71/$BF87`;
- after stream 9 completes (`$0641=0`), `$BD2F` executes `JMP $BD2F` permanently;
- there is no software edge from that terminal back to gameplay, reload, title/front-end or another engine state; leaving it requires external reset/power semantics.

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/PostSagaEndingTail.cs`
- corrected `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformNarrative8FReload.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/PostSagaEndingTailChecks.cs`
- corrected `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/Narrative8FReloadChecks.cs`
- `docs/reverse-engineering/POST_SAGA_ENDING_TAIL.md`
- corrected `docs/reverse-engineering/PLATFORM_RELOAD_MODE_8F.md`
- PR #137

Do not reopen the post-Saga ending chain or the corrected `$8F` diversion without contradictory ROM evidence or a failing fixture.

## EVIDENCE FOR NEXT

### Why battle-stage context coverage is now the next boundary

The canonical main story control path is now proven through the final hard terminal, and the major platform/global/boss primitives are already executable. Before moving to renderer, RNG, audio or REBORN, the stage-local battle surface must be checked for omitted material contexts.

`BATTLE_EVENT_DISPATCH.md` proves stage-indexed handler families across `$050E=$00-$0B`:

```text
init          stages 00..0B at $97DB
Talk          stages 00..0B at $9C95
post-Bronze   stages 00..0B at $A361
post-Gold     stages 00..0B at $A381
```

ROM-backed identities currently recorded there are:

```text
$00  Mu / pre-battle repair context
$01  Taurus — Aldebaran
$02  Gemini / first Camus branch
$03  Cancer — Death Mask
$04  Leo — Aioria
$05  Virgo — Shaka
$06  Scorpio — Milo
$07  Capricorn — Shura
$08  Aquarius — Camus
$09  Pisces — Aphrodite
$0A  Pope/Saga
$0B  special/final context
```

Dedicated executable context checkpoints currently exist for:

```text
$01 Taurus
$04 Leo
$05 Virgo
$08 Aquarius
$09 Pisces
$0A Saga
$0C final-special bridge (non-boss exception)
```

Therefore `$00/$02/$03/$06/$07/$0B` are not yet classified at the same context granularity. Table membership alone is insufficient: some may be generic/no-boss/special presentation contexts, while others may contain material stage-local progression, dialogue, releases, technique growth or alternate branches that still need dedicated executable contexts.

The next cycle must establish that classification before choosing any one omitted boss by narrative intuition. Generic battle damage/resources/dodge/techniques and fixed release/reload owners are already closed and must be reused.

## OPEN

1. Enumerate exact init/Talk/post-Bronze/post-Gold and Gold-selector ownership for every `$050E=$00-$0B` stage, cross-referencing the existing dedicated contexts.
2. Prove canonical reachability/provenance for the currently unclassified `$00/$02/$03/$06/$07/$0B` surfaces, including the story-progress values that select them.
3. Classify each stage as one of: `dedicated-context closed`, `generic-only/no material stage context`, `special/non-boss context`, or `material context missing`.
4. For every stage classified generic/special, provide evidence that no unmodeled stage-local release/progression or persistent mutation is being skipped.
5. For every material gap, identify its exact entry state, local RAM/counters, Talk/action handlers, reachable Gold slots, release tokens and successor ownership without reopening generic arithmetic.
6. Produce one coverage matrix/spec with discriminating fixtures so future work cannot silently skip a stage or reopen already-closed ones.
7. Set the next authoritative boundary to the **first material uncovered stage** in canonical progression order; if the audit proves no material stage gap remains, advance instead to the next system-level audit.
8. Do not start renderer/RNG/audio or REBORN until this coverage audit has eliminated stage-context ambiguity.

## NEXT

**Close the complete battle-stage context coverage audit across `$050E=$00-$0B`: prove canonical reachability and stage-local ownership for every index, classify all currently omitted `$00/$02/$03/$06/$07/$0B` contexts as generic/special versus materially uncovered, encode the classification in an executable coverage matrix with fixtures, and promote the first real uncovered stage as the next boundary.**

Completion criterion:

> Starting from the four confirmed stage-indexed event dispatcher families and the already-closed dedicated contexts, every `$050E=$00-$0B` value must have a documented and testable coverage classification with evidence for its canonical reachability and stage-local semantic surface. No stage may remain merely “not yet inspected.” If one or more material contexts are missing, the audit must identify the earliest canonical one and give its exact handler/release/progression entry contract as the next actionable checkpoint.

## BLOCKERS

- None. Canonical ROM, event dispatcher tables, existing dedicated boss contexts, generic boss primitives, story/reload machinery and the complete canonical ending path are available.

## RECOVERY CONTRACT

1. Read this file from `main`.
2. Reconcile it with newer merged Git history if present.
3. Treat PR #137 and `POST_SAGA_ENDING_TAIL.md` as frozen unless contradictory ROM evidence appears.
4. Start the coverage audit from `BATTLE_EVENT_DISPATCH.md`; enumerate numeric stages before assigning narrative semantics.
5. Reuse `BOSS_BATTLE_DAMAGE.md`, `BOSS_BATTLE_RESOURCES.md`, `BOSS_DODGE.md`, `BATTLE_TECHNIQUES.md`, `RESOURCE_ECONOMY.md`, fixed release ownership and existing stage contexts; do not reopen generic mechanics.
6. Follow canonical story-progress/reload provenance to prove reachability for `$00/$02/$03/$06/$07/$0B` before modeling handlers.
7. Keep stage `$0C` outside the ordinary `$00-$0B` audit except as a known non-boss bridge already closed by #133.
8. Drive is private ROM/evidence storage only; it never owns an independent `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `battle-stage-context-coverage-audit-00-0b`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, evidence, a discarded hypothesis or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.

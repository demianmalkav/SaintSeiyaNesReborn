# Battle narrative dispatchers — per-stage event state machines

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: the four stage-indexed dispatcher families are statically confirmed, the canonical `$050E=$00-$0B` namespace has now been coverage-audited end-to-end, and the major promoted contexts retain executable reachability fixtures.

See `BATTLE_STAGE_CONTEXT_COVERAGE.md` for the complete numeric coverage matrix and the proof that `$00` is the first material uncovered context while `$0B` has no canonical stable battle entry.

## Stage-indexed architecture

PRG bank 5 repeatedly uses the same pattern:

`LDA $050E -> JSR $E698 -> inline pointer table`

`$E698` is the shared indirect dispatcher. Four important tables are isolated below.

## 1. `$97DB` — battle/stage initialization dispatcher

Pointer table at `$97E1`:

| Stage | Handler |
|---:|---:|
| 0 | `$97F7` |
| 1 | `$97F8` |
| 2 | `$981F` |
| 3 | `$9851` |
| 4 | `$989D` |
| 5 | `$9A28` |
| 6 | `$9ACE` |
| 7 | `$9ACF` |
| 8 | `$9B14` |
| 9 | `$9B5C` |
| 10 | `$9B5D` |
| 11 | raw pointer `$A960`; no canonical stable battle entry |
| 12 | table overrun -> raw `$8D00`; unreachable on canonical final-special entry |

Stages `$00-$0A` establish stage-local counters, dialogue/setup state and transitions before/when entering an encounter. Stage `$0B` is different: canonical story table `$F016` never selects it as a stable battle stage, and pointer `$A960` lands inside the real instruction beginning at `$A95F` (`AD 6F 06`, `LDA $066F`). The two immediate `$0B` uses found in the relevant event code (`$9B31` and `$A16D`) call temporary presentation loader `$F2ED`; they do not enter the stage dispatcher as `$0B`.

Saga stage `$0A` is itself phase-dispatched: `$9B5D` uses `$06CE` to select `$9B69/$9B9E/$9C2C`. Canonical initial story ingress does not call `$9B5D`; release `$FF` re-entry calls it later to create phase 1 (Ikki) and phase 2 (Seiya). Structural init phase 2 `$9C2C` is unreachable because final phase has no `$FF` terminal. See `BOSS_CONTEXT_STAGE_0A_SAGA.md`.

Stage `$0C` is a separate proven exception. Index 12 reads beyond the intended pointer table and would decode bytes `$97F9/$97FA` as `$8D00`, which is data/table context rather than a valid final-special initializer. Canonical stage-`$0C` entry never reaches `$97DB`: mandatory-Saint table `$F36F[$0C]=$FF` cannot match either reachable active Saint (Seiya `0` or Shun `2`).

## 2. `$9C95` — Talk / interaction dispatcher

Pointer table at `$9C9B`:

| Stage | Handler |
|---:|---:|
| 0 | `$9CB7` |
| 1 | `$9D2C` |
| 2 | `$9D81` |
| 3 | `$9D96` |
| 4 | `$9DD8` |
| 5 | `$9E1B` |
| 6 | `$9E51` |
| 7 | `$9ED6` |
| 8 | `$9F00` |
| 9 | `$9F99` |
| 10 | `$9FF4` |
| 11 | `$9FF4` structural alias only; no canonical stable `$0B` battle |
| 12 | `$A1AD` |

Stage `$00` Talk `$9CB7` is now proven to be material: common battle reset seeds `$066F=0`; first Talk sets `$066F=1`; the later Talk branch writes release `$0670=$01`. Because fixed command owners block attack, resource allocation and escape when `$050E==0`, this Talk path is the canonical progression surface for the Mu/pre-battle repair context.

Saga `$9FF4` dispatches `$06CE` to `$A000/$A0E0/$A115`. Phase 0 uses the scripted-miss history and `$06CF/$06D0`; phase 1 first Talk clears `$0690`; phase 2 first Talk opens the prerequisite state for the later support event. None of these Talk handlers raises transient `$DC`.

Stage `$0C` Talk `$A1AD` has no active-Saint branch: both exact Seiya/Shun entry variants display messages `$D3/$D5`; the first Talk calls `$A1FF` with `#$10`, grants +1000 Seventh Sense through fixed `$F31E`, increments `$066F`, and returns without `$DC` or a release.

## 3. `$A361` — post-Bronze-action dispatcher

Fixed-bank callers include `$F83F` in a special battle branch and `$F932` after the player action/attack flow.

Pointer table at `$A367`:

| Stage | Handler |
|---:|---:|
| 0 | `$A3A1` |
| 1 | `$A3A2` |
| 2 | `$A444` |
| 3 | `$A50F` |
| 4 | `$A5B3` |
| 5 | `$A661` |
| 6 | `$A7FF` |
| 7 | `$A86B` |
| 8 | `$A8FC` |
| 9 | `$AA57` |
| 10 | `$AB18` |
| 11 | `$A3A1` structural only |
| 12 | `$A3A1` structural only; unreachable from canonical `$0C` Attack |

`$A3A1` is `RTS`.

For stage `$00`, that `RTS` is not evidence of a generic battle: fixed command-1 handling diverts `$050E==0` to `$F238`, so the post-Bronze dispatcher is canonically unreachable there.

Saga `$AB18` phase-dispatches to `$AB24/$AB62/$AB6F`. Phase 0 can four-`PLA` unwind the first scripted miss; phases 0/1 consume player condition; only phase 2 consumes opponent condition and owns final victory `$01`.

For final-special stage `$0C`, fixed `$F925+` returns for stage indices `>= $0B`, so command-1 Attack never calls `$A361`.

## 4. `$A381` — post-Gold-response dispatcher

Fixed `$FA86` invokes this after Gold-Saint attack selection, dodge resolution, damage (if any) and resource refresh.

Pointer table at `$A387`:

| Stage | Handler |
|---:|---:|
| 0 | `$A3A1` |
| 1 | `$A415` |
| 2 | `$A4CC` |
| 3 | `$A560` |
| 4 | `$A63E` |
| 5 | `$A7B3` |
| 6 | `$A847` |
| 7 | `$A8D8` |
| 8 | `$A9D3` |
| 9 | `$AAF0` |
| 10 | `$AC05` |
| 11 | `$A3A1` structural only |
| 12 | `$A3A1` structural only; unreachable at canonical `$0C` |

Stage `$00` cannot reach a Gold response because its attack path is blocked before Bronze attack processing. Stage `$0B` has no canonical stable battle entry.

Saga `$AC05` phase-dispatches to `$AC11/$AC3E/$AC76`. Phases 0/1 convert either low or defeated player condition into release `$FF` re-entry. Final phase keeps low condition nonterminal and converts only true defeat into `$DD` after resetting `$06CE`.

Final-special stage `$0C` never reaches Gold response: fixed `$F936+` returns for stage indices `>= $0B` before Gold technique selection, dodge, damage or `$A381`.

## Canonical story-stage provenance

Fixed `$F016` maps `$067D=$00-$0E` as:

```text
00->00  01->01  02->02  03->03  04->04
05->05  06->0F  07->06  08->10  09->07
0A->08  0B->09  0C->0C  0D->0A  0E->00
```

Therefore canonical ordinary `$00-$0B` battle entries occur at:

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

Progress `$0E` reuses numeric `$00` only inside the already-closed post-Saga platform tail; it does not re-enter Mu.

## Gold-selector coverage

Bank 6 `$9074-$9146` proves canonical reachable opponent technique slots:

| Stage | Reachable `$0680` slots |
|---:|---|
| `$00` | none; attack path blocked |
| `$01` | `0,1` |
| `$02` | `0,1` |
| `$03` | `0,1` |
| `$04` | `0,1` |
| `$05` | `0,1,2` |
| `$06` | `0,1` via dodge-history branch |
| `$07` | `0,1` |
| `$08` | `0,1,2` |
| `$09` | `0,1,2` |
| `$0A` | `0,1,2,3` across Saga phases |
| `$0B` | none canonically |

Stages without a dedicated selector branch fall through `$913E` and use `$065F & 1`. Equal coefficient entries in the damage table do not make structural slots reachable.

## Coverage classification

| Stage | Battle context | Coverage status |
|---:|---|---|
| `$00` | Mu / pre-battle repair | **material context missing; first next boundary** |
| `$01` | Taurus — Aldebaran | dedicated context closed |
| `$02` | Gemini / first Camus branch | **material context missing** |
| `$03` | Cancer — Death Mask | **material context missing** |
| `$04` | Leo — Aioria | dedicated context closed |
| `$05` | Virgo — Shaka | dedicated context closed |
| `$06` | Scorpio — Milo | **material context missing** |
| `$07` | Capricorn — Shura | **material context missing** |
| `$08` | Aquarius — Camus | dedicated context closed |
| `$09` | Pisces — Aphrodite | dedicated context closed |
| `$0A` | Pope/Saga | dedicated context closed |
| `$0B` | transient/structural presentation index | no canonical stable battle; no dedicated context required |
| `$0C` | final-special rose bridge | separately closed non-boss exception |

The uncovered `$02/$03/$06/$07` handlers all contain persistent counters, release ownership or stage-specific Talk/post-action branching; they are not generic-only placeholders. They remain queued behind `$00` in canonical progression order.

## Architectural consequence

A normal Gold Saint encounter composes at least:

1. stage initialization dispatcher;
2. command selection;
3. Talk/interaction dispatcher when chosen;
4. Bronze technique selection;
5. Bronze attack hit gate (`$06BC`);
6. Bronze damage application;
7. post-Bronze stage handler;
8. Gold technique selection (`$0680`);
9. dodge window and Gold damage;
10. post-Gold stage handler;
11. dialogue/reward/phase transitions;
12. next turn or battle termination.

Table presence alone does not prove reachability. Stage `$0B` is the clearest current example: it has structural entries in the tables, but no canonical stable story entry, and its raw init pointer is not an intended instruction boundary.

### Saga `$0A` phase summary

```text
phase0 inherited Seiya/Shun
  scripted $0690 block
  first action -> $0678 + outer unwind
  post-miss Talk -> $06CF/$06D0
  low/dead after Gold -> $FF

$FF -> init0 -> phase1 Ikki
  first Talk clears $0690
  Gold selector uses $0649/$06D0
  low/dead after Gold -> $FF

$FF -> init1 -> phase2 Seiya +1000 Seventh Sense
  final Talk -> enables one-shot support gate
  support -> $068F=$55 / $06D4 rewards / Seiya technique count3
  opponent defeat -> $01 victory
  Seiya defeat -> $DD
```

### Stage `$0C` exception

```text
Attack
  -> special rose effect
  -> release $01
  -> $067D $0C->$0D
  -> stage $0A Saga

Talk
  -> $A1AD shared dialogue
  -> first-use +1000 Seventh Sense / $066F++

Escape
  -> message $D4

Resource allocation
  -> suppressed/redraw-only
```

No generic opponent damage, post-Bronze, Gold response/dodge/damage or post-Gold dispatcher is reachable in stage `$0C`.

## Frequently used per-battle state

- `$EA` — player coarse condition (`FF` defeated, `00` above threshold, `01` alive/below threshold);
- `$EB` — opponent coarse condition with the same encoding;
- `$064D/$064E` — stage-local event/phase counters;
- `$066F` — conversation/progression counter;
- `$0677/$0678` — dodge/event history bytes, with stage-specific reuse;
- `$0681` — Gold attack weakening tier;
- `$0690` — scripted player-hit block;
- `$06CE-$06D0` — multi-phase story/battle state;
- `$06BC` — current Bronze attack hit token.

## Promoted contexts and next gap

Dedicated executable contexts are closed for `$01/$04/$05/$08/$09/$0A`; final-special `$0C` is closed separately. Coverage audit `BattleStageContextCoverage` proves material gaps at `$00/$02/$03/$06/$07` and no canonical stable battle at `$0B`.

The next checkpoint is stage `$00`: close the Talk-only Mu/pre-battle repair machine from common reset through second-Talk release `$01` and fixed progression to Taurus, without invoking generic Gold arithmetic.

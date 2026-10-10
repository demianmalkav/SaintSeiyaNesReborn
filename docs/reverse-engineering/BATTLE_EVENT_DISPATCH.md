# Battle narrative dispatchers — per-stage event state machines

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: the four stage-indexed dispatcher families are statically confirmed, the canonical `$050E=$00-$0B` namespace has been coverage-audited end-to-end, and stage `$00` Mu is now a closed dedicated special context. The first remaining material gap is `$02`.

See `BATTLE_STAGE_CONTEXT_COVERAGE.md` for the complete numeric coverage matrix and `BOSS_CONTEXT_STAGE_00_MU.md` for the exact Talk-only Mu control graph.

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

Stages `$00-$0A` establish or join stage-local state before/when entering an encounter. Mu `$00` has a no-op initializer (`$97F7 RTS`) but material command/Talk state elsewhere. Stage `$0B` is different: canonical story table `$F016` never selects it as a stable battle stage, and pointer `$A960` lands inside the real instruction beginning at `$A95F` (`AD 6F 06`, `LDA $066F`). The two immediate `$0B` uses found in relevant event code (`$9B31` and `$A16D`) call temporary presentation loader `$F2ED`; they do not enter battle dispatch as `$0B`.

Saga stage `$0A` is phase-dispatched: `$9B5D` uses `$06CE` to select `$9B69/$9B9E/$9C2C`. Canonical initial story ingress does not call `$9B5D`; release `$FF` re-entry later creates phase 1 (Ikki) and phase 2 (Seiya). Structural init phase 2 `$9C2C` is unreachable because final phase has no `$FF` terminal. See `BOSS_CONTEXT_STAGE_0A_SAGA.md`.

Stage `$0C` is a separate proven exception. Index 12 reads beyond the intended pointer table and would decode bytes `$97F9/$97FA` as `$8D00`, which is data/table context rather than a valid final-special initializer. Canonical stage-`$0C` entry never reaches `$97DB`.

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

Stage `$00` Talk `$9CB7` is fully closed by `MuStage00Context`:

```text
$066F==0
  common selectors $32/$33
  Saint selector $9D24 = 35 35 36 34
  $066F=1

$066F!=0
  common selectors $37/$38
  Saint selector $9D28 = 3B 3B 11 3B
  $0670=$01
```

The tables cover the four reachable initial Saints Seiya/Hyoga/Shun/Shiryu; Ikki is excluded by initial story marker `$0673=$30`. Active Saint changes only text selection, not control.

Saga `$9FF4` dispatches `$06CE` to `$A000/$A0E0/$A115`. Phase 0 uses scripted-miss history and `$06CF/$06D0`; phase 1 first Talk clears `$0690`; phase 2 first Talk opens the prerequisite state for the later support event.

Stage `$0C` Talk `$A1AD` has no active-Saint control branch: both exact Seiya/Shun variants display messages `$D3/$D5`; first Talk grants +1000 Seventh Sense through `$A1FF/$F31E`, increments `$066F`, and returns without a release.

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

For stage `$00`, fixed Attack owner `$F057` checks `$050E` and jumps directly to `$F238`; therefore `$A361` is canonically unreachable. The structural `$A3A1` pointer must not be promoted into fake battle semantics.

Saga `$AB18` phase-dispatches to `$AB24/$AB62/$AB6F`. Only phase 2 owns final opponent-defeat release `$01`.

For final-special stage `$0C`, fixed `$F925+` returns for stage indices `>= $0B`, so command-1 Attack never calls `$A361`.

## 4. `$A381` — post-Gold-response dispatcher

Fixed `$FA86` invokes this after Gold-Saint attack selection, dodge resolution, damage and resource refresh.

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

Stage `$00` cannot reach a Gold response because Resource Allocation/Attack/Escape are diverted before the ordinary battle pipeline and Talk never forces a Gold path. Stage `$0B` has no canonical stable battle entry.

Saga `$AC05` phase-dispatches to `$AC11/$AC3E/$AC76`; final phase converts only true player defeat into `$DD` after resetting `$06CE`.

Final-special stage `$0C` never reaches Gold response.

## Stage `$00` fixed command ownership

The stage-zero context is special before either post-action dispatcher can matter:

```text
Resource Allocation  $F041 -> if $050E==0: JMP $F238
Attack               $F057 -> if $050E==0: JMP $F238
Talk                 $F0B1 -> $9C95 -> $9CB7
Escape               $F0D3 -> if $050E==0: JMP $F238
```

Blocked owner `$F238` uses `$06BB`:

```text
$06BB==0   first blocked presentation, then INC -> 1
$06BB!=0   add selector $48, then INC
all        selector $39, redraw, return to command loop
```

Global RAM clear seeds `$06BB=0`; common reset `$A973` does not clear it. No blocked command writes `$0670`.

## Canonical story-stage provenance

Fixed `$F016` maps `$067D=$00-$0E` as:

```text
00->00  01->01  02->02  03->03  04->04
05->05  06->0F  07->06  08->10  09->07
0A->08  0B->09  0C->0C  0D->0A  0E->00
```

Therefore canonical ordinary `$00-$0B` entries occur at:

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

Mu's second Talk release `$01` joins fixed `$E399/$E3B3`:

```text
$067D: 00 -> 01
$06CD = 00
$0673 = 30
$F016[01] = 01
```

so the exact successor is Taurus stage `$01`, preserving the reachable active Saint.

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

Equal coefficient entries in the damage table do not make structural slots reachable.

## Coverage classification

| Stage | Battle context | Coverage status |
|---:|---|---|
| `$00` | Mu / pre-battle repair | dedicated special context closed |
| `$01` | Taurus — Aldebaran | dedicated context closed |
| `$02` | Gemini / first Camus branch | **material context missing; next boundary** |
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

The uncovered `$02/$03/$06/$07` handlers all contain persistent counters, release ownership or stage-specific Talk/post-action branching; they are not generic-only placeholders.

## Architectural consequence

A normal Gold Saint encounter composes stage init, command selection, Talk when chosen, Bronze selection/hit/damage, post-Bronze, Gold selection/dodge/damage and post-Gold. Table presence alone does not prove reachability: Mu `$00` and structural `$0B` are explicit counterexamples.

### Stage `$0C` exception

```text
Attack -> special rose effect -> release $01 -> progress $0D -> Saga $0A
Talk   -> shared dialogue -> first-use +1000 Seventh Sense / $066F++
Escape -> message $D4
Resource allocation -> suppressed/redraw-only
```

No generic opponent damage, post-Bronze, Gold response/dodge/damage or post-Gold dispatcher is reachable in stage `$0C`.

## Promoted contexts and next gap

Dedicated executable contexts are now closed for `$00/$01/$04/$05/$08/$09/$0A`; final-special `$0C` is closed separately. Coverage audit `BattleStageContextCoverage` leaves material gaps at `$02/$03/$06/$07` and no canonical stable battle at `$0B`.

The next checkpoint is stage `$02`: close Gemini / first Camus branch from its exact progress `$067D=$02` entry through its special `$067C` detour, Talk, post-action terminals and successor ownership without reopening generic arithmetic.

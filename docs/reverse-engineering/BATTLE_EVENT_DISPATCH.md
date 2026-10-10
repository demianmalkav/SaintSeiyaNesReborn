# Battle narrative dispatchers — per-stage event state machines

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: four stage-indexed dispatcher families are confirmed statically. The major canonical late-game contexts through Saga `$0A` and final-special `$0C` are now promoted with reachability, not merely table membership.

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
| 11 | `$A960` |
| 12 | table overrun -> raw `$8D00`; unreachable on canonical final-special entry |

These routines establish stage-local counters, dialogue/setup state and transitions before/when entering an encounter.

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
| 11 | `$9FF4` |
| 12 | `$A1AD` |

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
| 11 | `$A3A1` |
| 12 | `$A3A1` (structural only; unreachable from canonical `$0C` Attack) |

`$A3A1` is `RTS`.

Saga `$AB18` phase-dispatches to `$AB24/$AB62/$AB6F`. Phase 0 can four-PLA unwind the first scripted miss; phases 0/1 consume player condition; only phase 2 consumes opponent condition and owns final victory `$01`.

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
| 11 | `$A3A1` |
| 12 | `$A3A1` (structural only; unreachable at canonical `$0C`) |

Saga `$AC05` phase-dispatches to `$AC11/$AC3E/$AC76`. Phases 0/1 convert either low or defeated player condition into release `$FF` re-entry. Final phase keeps low condition nonterminal and converts only true defeat into `$DD` after resetting `$06CE`.

Final-special stage `$0C` never reaches Gold response: fixed `$F936+` returns for stage indices `>= $0B` before Gold technique selection, dodge, damage or `$A381`.

## Stage identity

Opponent initialization records and independently published boss stats match exactly for ordinary battle stages:

| Stage | Battle context |
|---:|---|
| 0 | Mu / pre-battle repair context |
| 1 | Taurus — Aldebaran |
| 2 | Gemini / first Camus branch |
| 3 | Cancer — Death Mask |
| 4 | Leo — Aioria |
| 5 | Virgo — Shaka |
| 6 | Scorpio — Milo |
| 7 | Capricorn — Shura |
| 8 | Aquarius — Camus |
| 9 | Pisces — Aphrodite |
| 10 | Pope/Saga |
| 11 | special/final context |
| 12 | final-special rose-clearing bridge to Saga; not a boss battle |

The numeric stage index remains ROM-canonical. Saga `$0A` is internally tied to the three-phase `$06CE` machine; stage `$0C` is tied to its fixed command flow and exact Pisces/Saga progression handoffs.

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

Saga demonstrates a second layer of composition: each of those stage-local surfaces can itself dispatch a synchronized narrative phase byte (`$06CE`). Table presence alone still does not prove reachability: Saga init phase 2 is structurally present but unreachable, while final-special stage `$0C` bypasses ordinary boss surfaces altogether.

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

## Closed/promoted contexts

1. Taurus/Aldebaran `$01`;
2. Leo/Aioria `$04`;
3. Virgo/Shaka `$05`;
4. Aquarius/Camus `$08`;
5. Pisces/Aphrodite `$09`;
6. final-special bridge `$0C`;
7. Saga final machine `$0A`.

Saga's exact post-victory boundary is story progress `$0E`, stage `$00`, release `$05`, engine bootstrap `$00->$20`; its final defeat boundary is `$DD` followed by bootstrap `$90->$91`. Further ownership belongs to the already-separated global engine/story subsystems, not to the Saga boss dispatcher.

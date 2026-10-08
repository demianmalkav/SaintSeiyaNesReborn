# Battle narrative dispatchers — per-stage event state machines

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: four stage-indexed dispatcher families are confirmed statically. Individual handler semantics are being promoted stage by stage.

## Stage-indexed architecture

PRG bank 5 repeatedly uses the same pattern:

`LDA $050E -> JSR $E698 -> inline pointer table`

`$E698` is the shared indirect dispatcher. Four important tables are now isolated.

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
| 12 | special pointer/data context; still being classified |

These routines establish stage-local counters, dialogue/setup state and transitions before/when entering the encounter.

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

Stage 1 proves the role particularly clearly: its handler advances a conversation counter and, on the second conversation phase, increments the Gold-Saint attack weakening tier `$0681`.

## 3. `$A361` — post-Bronze-action dispatcher

Fixed-bank callers:

- `$F83F` in a special battle branch;
- `$F932` after the player action/attack flow.

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
| 12 | `$A3A1` |

`$A3A1` is `RTS`.

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
| 12 | `$A3A1` |

## Stage identity

Opponent initialization records and independently published boss stats match exactly, giving the following externally corroborated mapping:

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
| 11–12 | special/final contexts |

The numeric stage index remains ROM-canonical; names are secondary labels until text/portrait data is internally tied to those indices.

## Architectural consequence

A Gold Saint encounter is not one monolithic state machine. It composes at least:

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

This is the semantic architecture to preserve before REBORN expands presentation or mechanics.

## Frequently used per-battle state

- `$EA` — player coarse condition (`FF` defeated, `00` above threshold, `01` alive/below threshold);
- `$EB` — opponent coarse condition with the same encoding;
- `$064D/$064E` — stage-local event/phase counters;
- `$066F` — conversation/progression counter in several stage Talk handlers;
- `$0677/$0678` — failed/successful Gold-attack dodge counters;
- `$0681` — Gold attack weakening tier;
- `$0690` — scripted player-hit block;
- `$06CE-$06D0` — multi-phase story/battle state used heavily in Gemini/Saga-related paths;
- `$06BC` — current Bronze attack hit token.

These locations are structurally reusable but some counter semantics remain stage-specific.

## Next stage-by-stage work

1. Taurus/Aldebaran as the simplest complete pattern;
2. Leo/Aioria to combine scripted weakening and multi-attack opponent AI;
3. Virgo/Shaka for Ikki substitution/special platform detour;
4. Aquarius/Camus for technique unlocks and stage redirection;
5. Pisces/Aphrodite for deterministic attack escalation;
6. Saga for the three-phase final state machine.

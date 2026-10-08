# Battle narrative dispatchers — per-stage event state machines

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: dispatcher topology and call timing are confirmed statically. Individual handler semantics are being promoted stage by stage.

## Two distinct per-stage dispatchers

PRG bank 5 contains two neighboring indirect-dispatch tables indexed by `$050E`.

### `$A361` — post-Bronze-action dispatcher

Fixed-bank callers:

- `$F83F` in a special battle branch;
- `$F932` after the player action/attack flow.

The table starts at `$A367` and contains:

| Stage `$050E` | Handler |
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

`$A3A1` is an `RTS`, so stage 0 and special trailing contexts have no ordinary post-Bronze battle script here.

### `$A381` — post-Gold-response dispatcher

Fixed `$FA86` invokes this after the Gold Saint attack/dodge/damage phase and resource display refresh.

The table starts at `$A387`:

| Stage `$050E` | Handler |
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

The opponent initialization records and independently published boss stats match exactly, giving the following externally corroborated mapping:

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
| 11–12 | special/final contexts, ordinary stage handlers disabled |

The numeric stage index remains the ROM-canonical identity; names are secondary labels until the text/portrait engine is tied internally.

## Architectural consequence

A Gold Saint battle is not one monolithic state machine. At minimum it composes:

1. command/technique selection;
2. Bronze attack hit gate (`$06BC`);
3. Bronze damage application;
4. **post-Bronze per-stage handler** (`$A361` table);
5. Gold technique selection (`$0680`);
6. dodge window and Gold damage;
7. **post-Gold per-stage handler** (`$A381` table);
8. dialogue/reward/phase transitions;
9. next turn or battle termination.

This is the structure the native remake should preserve semantically before any REBORN redesign.

## Frequently used per-battle state

The stage handlers repeatedly use:

- `$EA` — player coarse condition (`FF` defeated, `00` above threshold, `01` alive/below threshold);
- `$EB` — opponent coarse condition with the same encoding;
- `$064D/$064E` — stage-local event/phase counters;
- `$0677/$0678` — failed/successful Gold-attack dodge counters;
- `$0681` — Gold attack weakening tier;
- `$0690` — scripted player-hit block;
- `$06CE-$06D0` — multi-phase story/battle state used heavily in Gemini/Saga-related paths;
- `$06BC` — current Bronze attack hit token.

The exact meaning of `$064D/$064E` varies by handler and should not be globally renamed beyond `battle_event_counter_a/b` yet.

## Next stage-by-stage work

1. Taurus/Aldebaran as the simplest complete pattern;
2. Leo/Aioria to combine scripted weakening and multi-attack opponent AI;
3. Virgo/Shaka for Ikki substitution/special platform detour;
4. Aquarius/Camus for technique unlocks and stage redirection;
5. Pisces/Aphrodite for deterministic attack escalation;
6. Saga for the three-phase final state machine.

# Gold Saint battle damage — complete numeric pipeline

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: static reconstruction of the complete numeric damage pipeline for Bronze Saint attacks and Gold Saint attacks. Technique-selection conditions and scripted invulnerability are separate systems.

## Core formula

Both sides use the same basic scaling rule for each resource independently:

`drain = max(3, floor(attacker_cosmo * coefficient / 100))`

Each technique has two coefficients:

- one for Cosmo drain;
- one for Life drain.

The ROM converts the attacker's packed-decimal Cosmo to binary, multiplies it by the selected 8-bit coefficient, reduces the product to the equivalent percent result and clamps each pre-mitigation drain to a minimum of 3.

## Bronze Saint -> opponent

Bank 1 `$BC64+` uses `$065B` as player attack id and reads coefficient pairs from `$BCD4`.

Calculated counters:

- `$06E2/$06E3` — pending opponent Cosmo drain;
- `$06E4/$06E5` — pending opponent Life drain.

Bank 1 `$AD52+` consumes those counters one point at a time:

- `$DE4E` decrements opponent Cosmo by one packed-decimal unit;
- `$DEF5` decrements opponent Life by one packed-decimal unit.

The counters are binary work values; the resources themselves remain packed BCD.

## Player attack-id space

`$ACA1+` constructs the attack id as:

`attack_id = canonical_character_index * 4 + technique_slot`

Canonical character order is:

`[Seiya, Hyoga, Shun, Shiryu, Ikki]`

Therefore the table has 20 structural slots. Active coefficient pairs are:

| ID | Character | Cosmo coeff | Life coeff | Technique identity |
|---:|---|---:|---:|---|
| 0 | Seiya | 26 | 18 | Pegasus Ryusei Ken / Meteoro de Pegaso |
| 1 | Seiya | 18 | 26 | Pegasus Suisei Ken / Cometa de Pegaso |
| 2 | Seiya | 25 | 25 | Pegasus Rolling Crash / Choque Giratorio de Pegaso |
| 3 | Seiya | 25 | 25 | duplicate/internal slot; exact accessibility pending |
| 4 | Hyoga | 16 | 24 | Diamond Dust / Polvo de Diamante |
| 5 | Hyoga | 24 | 16 | Freezing Ring / Anillo de Congelación |
| 6 | Hyoga | 24 | 24 | Aurora Thunder Attack |
| 7 | Hyoga | 30 | 30 | Aurora Execution / Ejecución de la Aurora |
| 8 | Shun | 19 | 19 | Nebula Chain / Cadena de la Nebulosa |
| 9 | Shun | 25 | 17 | Thunder Wave / Onda del Trueno |
| 10 | Shun | 39 | 26 | Nebula Stream / Corriente de la Nebulosa |
| 11 | Shun | 35 | 35 | Nebula Storm / Tormenta de la Nebulosa |
| 12 | Shiryu | 21 | 21 | Rozan Shoryuha / Ascenso del Dragón |
| 13 | Shiryu | 40 | 40 | Rozan Koryuha / Dragón Superior |
| 14 | Shiryu | 0 | 0 | unused slot |
| 15 | Shiryu | 0 | 0 | unused slot |
| 16 | Ikki | 30 | 20 | Phoenix Genma Ken / Ilusión del Fénix |
| 17 | Ikki | 20 | 30 | Houyoku Tensho / Vuelo del Fénix |
| 18 | Ikki | 0 | 0 | unused slot |
| 19 | Ikki | 0 | 0 | unused slot |

The character/technique names above are externally corroborated by published technique tables whose Life/Cosmo factors match the ROM coefficient pairs exactly. IDs and coefficients are ROM-confirmed; the remaining Seiya duplicate slot stays unresolved until an internal text/selection anchor is traced.

## Opponent -> player

Bank 1 `$BCFC+` selects the opponent coefficient pair with:

`(stage_index - 1) * 8 + technique_index * 2`

where `$0680` is the opponent technique index `0..3`.

The same work counters `$06E2-$06E5` receive the calculated drains.

The fixed-bank battle path at `$FA08+` then consumes them:

- `$FA29` repeatedly calls `$FD26`, decrementing active player Cosmo one BCD point per loop;
- `$FA4D` repeatedly calls `$FC2E`, decrementing active player Life one BCD point per loop;
- `$FA69/$FA6C` refresh the Life/Cosmo presentation state after the drain finishes.

This proves that the two sides share the same counter representation but use different resource decrement helpers.

## Gold Saint coefficient rows

Stages 1..10 each expose four structural technique slots:

| Stage | slot 0 | slot 1 | slot 2 | slot 3 |
|---:|---|---|---|---|
| 1 | 19/29 | 19/29 | 19/29 | 19/29 |
| 2 | 26/18 | 26/18 | 26/18 | 26/18 |
| 3 | 28/18 | 28/18 | 28/18 | 28/18 |
| 4 | 48/32 | 32/48 | 48/32 | 32/48 |
| 5 | 42/28 | 24/36 | 37/22 | 37/22 |
| 6 | 29/19 | 20/30 | 29/19 | 20/30 |
| 7 | 26/26 | 26/26 | 26/26 | 26/26 |
| 8 | 30/20 | 21/31 | 30/20 | 21/31 |
| 9 | 22/32 | 34/22 | 29/29 | 29/29 |
| 10 | 35/23 | 30/30 | 60/60 | 60/60 |

Each cell is `Cosmo coefficient / Life coefficient`.

## `$0681` — opponent attack weakening tier

After calculating the opponent's raw drain, `$BD2F+` applies `$0681`:

- `0` — full damage;
- `1` — divide both drains by 2;
- `>=2` — divide both drains by 4.

The shift occurs after the minimum-3 clamp. Therefore a raw minimum drain of 3 becomes 1 at tier 1 and 0 at tier 2+.

Story/event code increments `$0681` in specific battle scripts. External boss documentation independently matches the exact results: weakened Great Horn is half-strength, while weakened Aioria attacks are quarter-strength. This strongly establishes `$0681` as a scripted **opponent attack weakening level**, not a generic player-defense/parry variable.

## Stage identity cross-validation

The opponent Life/Cosmo records and published boss stats line up exactly, providing strong external corroboration for stage indices:

0 Mu, 1 Aldebaran, 2 Gemini empty armor/Camus first encounter, 3 Death Mask, 4 Aioria, 5 Shaka, 6 Milo, 7 Shura, 8 Camus final, 9 Aphrodite, 10 Saga.

The numeric stage index remains ROM-canonical; names are considered externally corroborated until tied to an internal text/portrait table.

## Executable specification

`spec/original/battle_damage.py` implements:

- all 20 player technique slots;
- player technique id construction;
- both coefficient tables;
- the percent damage rule and minimum drain;
- opponent attack weakening tiers.

Parity tests are in `tests/test_battle_damage_spec.py`.

## Remaining work

1. identify the exact battle-menu availability flags for every Bronze technique;
2. resolve Seiya's duplicated fourth coefficient slot;
3. trace `$0680` selection to map each Gold-Saint technique slot to its named technique;
4. separate scripted invulnerability/vulnerability from raw numeric damage;
5. reconstruct the turn/event state machine surrounding attack selection and dialogue.

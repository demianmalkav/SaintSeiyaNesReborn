# Bronze Saint techniques — slots, availability and unlocks

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: technique-slot topology, initial counts, menu availability and the main progression unlock writes are statically confirmed. Names are attached where published Life/Cosmo factors exactly match the ROM coefficient pairs; final JP→ES wording still belongs to the localization pass.

## Canonical character order

Battle/UI character index `$0533` uses:

`0 Seiya, 1 Hyoga, 2 Shun, 3 Shiryu, 4 Ikki`

Player attack ids are:

`attack_id = character_index * 4 + technique_slot`

This creates four structural slots per character, whether or not all four are playable.

## Availability counters

`$0587-$058B` stores one selectable-technique count per canonical character:

- `$0587` Seiya
- `$0588` Hyoga
- `$0589` Shun
- `$058A` Shiryu
- `$058B` Ikki

Initialization at bank 1 `$A184` is:

`[2, 2, 2, 1, 2]`

When a character is selected, fixed-bank code around `$F519+` copies that character's count into `$0696`. The technique-selection UI in bank 5 uses `$0696 - 1` as the last selectable index.

Therefore availability is contiguous: if count is `N`, slots `0..N-1` are selectable.

## Maximum playable slots

| Character | Initial | Maximum | Attack IDs |
|---|---:|---:|---|
| Seiya | 2 | 3 | 0,1,2 |
| Hyoga | 2 | 4 | 4,5,6,7 |
| Shun | 2 | 4 | 8,9,10,11 |
| Shiryu | 1 | 2 | 12,13 |
| Ikki | 2 | 2 | 16,17 |

The structurally present IDs 14,15,18,19 are not exposed by normal technique counts.

## Technique mapping

The ROM coefficient pairs line up exactly with independently documented Life/Cosmo technique factors:

| ID | Character | Technique | ROM Cosmo/Life coefficients |
|---:|---|---|---|
| 0 | Seiya | Pegasus Ryusei Ken / Meteoro de Pegaso | 26 / 18 |
| 1 | Seiya | Pegasus Suisei Ken / Cometa de Pegaso | 18 / 26 |
| 2 | Seiya | Pegasus Rolling Crash / Choque Giratorio de Pegaso | 25 / 25 |
| 4 | Hyoga | Diamond Dust / Polvo de Diamante | 16 / 24 |
| 5 | Hyoga | Freezing Ring / Anillo | 24 / 16 |
| 6 | Hyoga | Aurora Thunder Attack | 24 / 24 |
| 7 | Hyoga | Aurora Execution | 30 / 30 |
| 8 | Shun | Nebula Chain | 19 / 19 |
| 9 | Shun | Thunder Wave | 25 / 17 |
| 10 | Shun | Nebula Stream | 39 / 26 |
| 11 | Shun | Nebula Storm | 35 / 35 |
| 12 | Shiryu | Rozan Shoryuha | 21 / 21 |
| 13 | Shiryu | Rozan Koryuha | 40 / 40 |
| 16 | Ikki | Phoenix Genma Ken | 30 / 20 |
| 17 | Ikki | Houyoku Tensho | 20 / 30 |

Table notation is `Cosmo drain coefficient / Life drain coefficient`. Published guides usually print Life first, then Cosmo, so the columns appear reversed relative to the ROM pair order.

## Progression unlocks

### Seiya

Seiya starts with count 2. Fixed `$F497` writes `3` directly to `$0587`, and the same event grants a large +1000 Seventh Sense reward. This corresponds to the late Saga sequence in which Seiya gains Pegasus Rolling Crash.

Final count: 3.

### Hyoga

Hyoga starts with count 2.

Two distinct event paths increment `$0588` and the active menu count `$0696`:

- bank 5 `$9B53/$9B56`;
- bank 5 `$9F47/$9F4A`.

This produces count 3 then count 4, matching the two later techniques associated with the Aquarius progression.

Final count: 4.

### Shun

Shun starts with count 2.

Bank 5 `$AA57+` maintains an event/turn counter. When it reaches 2 and again when it reaches 5, `$AA6E/$AA71` increments `$0589` and `$0696`.

Thus the same battle script exposes slot 2 and later slot 3 in sequence: Nebula Stream and Nebula Storm.

Final count: 4.

### Shiryu

Shiryu starts with count 1.

Bank 5 `$9B06/$9B09` increments `$058A` and `$0696` in the same event path that grants +600 Seventh Sense. This matches the Capricorn/Shura progression and unlocks the second technique.

Final count: 2.

### Ikki

Ikki starts with and remains at count 2. No normal progression write expands `$058B`.

Final count: 2.

## Password persistence implication

The password serializes `$0587-$058A` — technique counts for Seiya, Hyoga, Shun and Shiryu — but excludes `$058B`, consistent with Ikki being special/transient in the broader persistence model and always having his fixed two-technique set when available.

## Executable specification

`spec/original/battle_techniques.py` models:

- canonical character order;
- initial and maximum technique counts;
- attack-id construction;
- contiguous menu availability;
- named active slots;
- saturating single-technique unlocks.

Tests live in `tests/test_battle_techniques_spec.py`.

## Remaining work

1. tie technique names to original Japanese text ids once the text engine is decoded;
2. identify the exact two Hyoga unlock events internally by stage/event state;
3. formalize progression events as state-machine transitions, not only direct RAM writes;
4. trace opponent `$0680` selection and scripted technique forcing.

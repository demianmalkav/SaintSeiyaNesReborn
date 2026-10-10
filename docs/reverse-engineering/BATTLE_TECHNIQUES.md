# Bronze Saint techniques — slots, availability and unlocks

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: technique-slot topology, initial counts, menu availability and the main progression unlock writes are statically confirmed. Names are attached where published Life/Cosmo factors exactly match the ROM coefficient pairs; final JP→ES wording remains owned by the localization pass.

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

### Hyoga — Aquarius sequence closed

Hyoga starts with count 2. The two increment writers are tied to exact stage-8 events by `BOSS_CONTEXT_STAGE_08_AQUARIUS.md`.

#### Unlock 1: final Camus initialization

Bank 5 `$9B14` is the stage-8 initialization handler. Its `$067D==$02` branch restores Seiya and does not unlock anything. The other branch — canonically final Camus at `$067D=$0A` — ends with:

```text
$9B53 INC $0588
$9B56 INC $0696
```

This changes Hyoga's persistent/active counts `2 -> 3`, exposing contiguous slot 2 / attack id 6: **Aurora Thunder Attack**. The handler then exits through shared release `$03`.

#### Unlock 2: first post-dodge Hyoga Talk

Stage-8 Talk `$9F00` first requires the ordinary/final phase `$06B8==0` and a nonzero total dodge history:

```text
$0677 + $0678 > 0
```

When `$066F==0`, the handler increments the Talk progression counter. It then tests the active Saint:

```text
$0533 == 1  ; Hyoga
```

Only Hyoga reaches `$A1F4`, which clears the scripted Bronze-hit block `$0690`, followed by:

```text
$9F47 INC $0588
$9F4A INC $0696
```

With canonical prior count 3, this produces `3 -> 4`, exposing contiguous slot 3 / attack id 7: **Aurora Execution**.

A non-Hyoga active Saint still advances `$066F` on this first post-dodge Talk but does not clear `$0690` and does not receive the Hyoga unlock.

Final count: 4.

### Shun — Pisces sequence closed

Shun starts with count 2. `BOSS_CONTEXT_STAGE_09_PISCES.md` ties both writers to exact reachable stage-9 conditions.

Canonical story progress `$067D=$0B` produces `$0673=$3A`. The generic Saint-selection gate rejects set Saint bits, so only Seiya (`$0533=0`) and Shun (`$0533=2`) are selectable in Pisces. This proves that the raw `$AA57` test `$0533!=0` is Shun-specific inside the reachable stage-9 state space.

`$AA57` increments zero-page `$EF` once per **Bronze action** and then tests exact equality:

```text
INC $EF
LDA $0533
BEQ skip_growth          ; Seiya
LDA $EF
CMP #$02
BEQ grow
CMP #$05
BEQ grow
...
grow:
INC $0589
INC $0696
```

Canonical Shun path:

```text
start                 $0589/$0696 = 2/2
second Bronze action  $EF=2 -> 3/3 -> slot2 / attack id 10 / Nebula Stream
fifth Bronze action   $EF=5 -> 4/4 -> slot3 / attack id 11 / Nebula Storm
```

The checks are equality-triggered, not `>=`. A Seiya route therefore misses an increment if Seiya is active when `$EF` becomes 2 or 5; the missed threshold is not replayed later by merely changing Saint.

Growth occurs before `$AA57` consumes the opponent condition `$EB`, so a Shun action that reaches `$EF==2` or `$05` and defeats Aphrodite still increments the technique counts before the `$FE` victory release.

The separate first Shun Talk after at least two Gold dodge attempts grants +1000 Seventh Sense through `$A1FF/$F31E`; it does **not** alter technique count.

Final canonical count after both Shun thresholds: 4.

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

Stage-local composition and transition fixtures now include:

- `src/SaintSeiyaNesReborn.OriginalSpec/AquariusStage08Context.cs`;
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/AquariusStage08ContextChecks.cs`;
- `src/SaintSeiyaNesReborn.OriginalSpec/PiscesStage09Context.cs`;
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/PiscesStage09ContextChecks.cs`.

## Remaining work

1. complete final localization review for technique naming/wording in the JP→ES corpus;
2. close late Seiya/Saga technique progression;
3. keep opponent `$0680` reachability stage-local rather than inferring unused structural slots from coefficient tables.

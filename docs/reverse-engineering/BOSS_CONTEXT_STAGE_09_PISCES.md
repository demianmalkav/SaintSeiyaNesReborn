# Boss context stage `$09` — Pisces / Aphrodite

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: **CONFIRMED static reconstruction** of the complete reachable stage-local Pisces/Aphrodite control graph. Generic battle arithmetic, Bronze/Gold damage and dodge execution remain owned by their already-closed subsystems.

Canonical complete-file SHA-256:

`6917B31D7343A9A17170E833BACDBC3B1EBA3E02D11C51C0D44DBE436C9AD43A`

The static pass used the verified 262,160-byte ROM. The private JP→ES localization corpus is used only as narrative corroboration; control semantics come from ROM code.

## Closed boundary

Canonical story entry:

```text
$067D = $0B
$050E = $09
```

Stage-local and directly composed helpers:

| Role | PRG bank | CPU address | ROM file offset |
|---|---:|---:|---:|
| initialization | 5 | `$9B5C` | `$15B6C` |
| Talk | 5 | `$9F99` | `$15FA9` |
| dodge-history sum | 5 | `$A1EC` | `$161FC` |
| scripted large-reward/presentation helper | 5 | `$A1FF` | `$1620F` |
| post-Bronze action | 5 | `$AA57` | `$16A67` |
| post-Gold response | 5 | `$AAF0` | `$16B00` |
| generic release owner | 5 | `$ACAA` | `$16CBA` |
| Gold selector stage-9 branch | 6 | `$90D5-$90EA` | `$190E5+` |
| fixed story advance core | fixed | `$E3B3+` | `$1E3C3+` |
| fixed `$FE` owner | fixed | `$E3F7+` | `$1E407+` |
| story descriptor table | fixed | `$E50B` | `$1E51B` |
| story-stage table | fixed | `$F016` | `$1F026` |
| generic Saint-availability gate | fixed | `$F6FE+` | `$1F70E+` |
| Saint bit table | fixed | `$FFC0` | `$1FFD0` |

## Canonical predecessor and stage entry

Aquarius/Camus stage `$08` is the direct predecessor. Its final `$FE` path advances:

```text
$067D: $0A -> $0B
```

The fixed story-stage table at `$F016` contains:

```text
$F016[$0B] = $09
```

so progress `$0B` selects Pisces.

The same fixed progression path loads the story descriptor from `$E50B`:

```text
$E50B[$0B] = $0A
$06CD = $0A
$0673 = $0A | $30 = $3A
```

This `$0673=$3A` value is crucial to the reachable Pisces roster.

## Reachable roster — why `$0533!=0` really means Shun

Fixed Saint-selection code at `$F6FE+` uses one bit per canonical battle Saint from `$FFC0`:

```text
Seiya  $01
Hyoga  $02
Shun   $04
Shiryu $08
Ikki   $10
```

For a candidate Saint `X`, the selector performs the equivalent of:

```text
if (($0673 & saint_bit[X]) != 0)
    reject candidate;
```

At canonical Pisces entry:

```text
$0673 = $3A = %00111010
```

Therefore:

| Saint | bit | `$3A & bit` | reachable at Pisces entry |
|---|---:|---:|---|
| Seiya | `$01` | 0 | yes |
| Hyoga | `$02` | nonzero | no |
| Shun | `$04` | 0 | yes |
| Shiryu | `$08` | nonzero | no |
| Ikki | `$10` | nonzero | no |

Only **Seiya and Shun** are reachable.

This resolves an apparent ambiguity in `$AA57`: that handler tests only whether `$0533` is zero before applying the two Shun technique increments. In raw code the condition is `active Saint != Seiya`; in the reachable stage-9 state space, the only nonzero active Saint is Shun. Therefore the two writers are genuinely Shun-specific without requiring an explicit `CMP #$02` at `$AA57`.

The common battle-entry path `$9780-$9789` ORs the chosen active Saint's bit into `$0673`. Thus:

```text
Seiya entry -> $0673=$3B
Shun entry  -> $0673=$3E
```

That difference is later consumed by the special post-Pisces story handoff.

## Initialization `$9B5C`

Stage `$09` has no stage-local initialization body:

```text
$9B5C  RTS
```

All meaningful initial state comes from generic battle setup plus story progress `$067D=$0B` / roster `$0673=$3A`.

This is intentionally represented as a no-op in `PiscesStage09Context.ApplyInitialization` rather than inventing an Aphrodite-specific initializer.

## Talk `$9F99`

Talk begins with shared helper `$A1EC`:

```text
A = (byte)($0678 + $0677)
```

where the already-closed dodge subsystem defines:

- `$0677` — failed Gold-attack dodge attempts;
- `$0678` — successful Gold-attack dodges.

The sum is byte arithmetic and the branch threshold is `2`.

### Fewer than two dodge attempts

When total dodge history is `< 2`, Talk is dialogue-only and returns without raising transient `$DC`. Therefore it does **not** directly force an Aphrodite counterattack.

For active Seiya (`$0533==0`):

```text
$C4 -> $3E
```

Current localization anchors:

- `$C4` / `MSG_196`: Aphrodite mocks what the Bronze Saints can do;
- `$3E` / `MSG_062`: Seiya's defiant reply.

For the only other reachable Saint, Shun (`$0533==2`):

```text
$C8 -> $C4
```

`$C8` / `MSG_200` is Shun's declaration that he will avenge Daidalos.

### Two or more dodge attempts

At total dodge history `>=2`, `$9F99` first emits active-Saint-specific dialogue tables.

Reachable pairs are:

| active Saint | Aphrodite line | Bronze line |
|---|---:|---:|
| Seiya | `$C5` | `$B8` |
| Shun | `$C9` | `$CB` |

The Shun lines correspond to Aphrodite dismissing the revenge attempt and Shun resolving to fight to the end.

After the dialogue:

```text
if ($066F == 0 && $0533 == Shun)
    special Shun branch
else
    $DC++
```

`$DC++` is the existing transient signal that forces the Gold response.

### First post-threshold Shun Talk

Only Shun with `$066F==0` reaches:

```text
JSR $DF92
LDA #$10
JSR $A1FF
INC $066F
RTS
```

`$A1FF` immediately calls fixed `$F31E`. `RESOURCE_ECONOMY.md` already proves that `$F31E` interprets its packed-BCD input as a large Seventh Sense reward in hundreds. Therefore:

```text
#$10 -> +1000 Seventh Sense
```

The rest of `$A1FF` performs the existing battle presentation/refresh sequence. The material stage-local semantic effect is the **+1000 Seventh Sense scripted reward**.

This first Shun branch:

- increments `$066F: 0 -> 1`;
- grants +1000 Seventh Sense;
- does **not** increment `$DC`;
- therefore does **not** force the immediate Gold response.

Any later Shun Talk at `>=2` attempts sees `$066F!=0` and forces the Gold response. Seiya never receives this `$066F/+1000` branch.

## `$EF` — Pisces Bronze-action counter

`$EF` is a zero-page counter used by `$AA57` as the exact Bronze-action threshold source. A static PRG cross-reference finds its material stage-9 uses as:

```text
$AA5A  INC $EF
$AA61  LDA $EF
```

The game's reset sequence clears RAM `$0000-$06FF`, so the dedicated byte begins from zero before its Pisces use. It is not a Gold-response or Talk counter: only Bronze-action execution advances it.

## Post-Bronze `$AA57`

The handler starts, before opponent classification, with:

```text
INC $064D
INC $EF
```

So every Bronze action advances two distinct Pisces counters:

- `$064D` — Gold-attack escalation / low-opponent latch;
- `$EF` — exact Shun technique-growth action count.

### Shun technique-growth thresholds

The ROM then executes:

```text
LDA $0533
BEQ skip_growth_checks       ; Seiya
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

Because only Seiya and Shun are reachable, the raw nonzero test is exactly the Shun route.

Canonical Shun sequence:

```text
start $0589/$0696 = 2/2
$EF becomes 2 -> 3/3 -> exposes slot2 / attack id 10 / Nebula Stream
$EF becomes 5 -> 4/4 -> exposes slot3 / attack id 11 / Nebula Storm
```

The equality checks matter. They are not `>=` thresholds. If Seiya is the active Saint when `$EF` becomes 2 or 5, that increment is skipped and is not replayed later merely because Shun becomes active. ORIGINAL SPEC preserves that exact behavior.

The growth executes **before** opponent-condition handling. Therefore a Shun Bronze action that both reaches `$EF==2` or `$05` and defeats Aphrodite receives the technique increment before the victory release.

## Opponent condition after a Bronze action

After counter/growth handling, `$AA57` calls the already-closed generic opponent classifier `$ACD6`, producing `$EB`.

### `$EB=$00` — healthy Aphrodite

Return immediately. No additional stage-local event.

### `$EB=$01` — low/non-healthy Aphrodite

The handler tests the already-incremented `$064D`:

```text
if ($064D < $80) {
    dialogue $CD;
    $064D = $80;
}
return;
```

`$CD` / `MSG_205` is Aphrodite's “who will die first” challenge.

This makes `$064D=$80` a one-time low-opponent latch. Later Bronze actions increment it to `$81`, `$82`, etc.; as long as the high-bit range is retained, the first-low dialogue does not replay.

Because the Gold selector compares `$064D` numerically against 3 and 6, `$80` also immediately promotes Aphrodite to the highest reachable attack slot.

### `$EB=$FF` — Aphrodite defeated

The long victory presentation includes `$CC` / `MSG_204` (“Bloody Rose!”) and `$CF` / `MSG_207` (“Magnificent, but you too will perish”). The semantically material tail is:

```text
$06B1 = $FF       ; presentation/effect flag owned by existing visual code
LDA #$12
JSR $F31E
LDA #$FE
JMP $ACAA
```

`#$12` through fixed `$F31E` is a **+1200 Seventh Sense** reward.

The terminal token is `$FE`, not ordinary victory `$01`.

## Gold attack selector — deterministic escalation

Bank 6 `$90D5-$90EA` handles stage `$09` directly from `$064D`:

```text
if ($064D >= 6)      slot = 2
else if ($064D >= 3) slot = 1
else                 slot = 0
```

Therefore reachable progression is:

| `$064D` | Gold slot | Cosmo/Life coefficients |
|---:|---:|---|
| `0..2` | 0 | `22 / 32` |
| `3..5` | 1 | `34 / 22` |
| `>=6` | 2 | `29 / 29` |
| `$80+` low latch | 2 | `29 / 29` |

The stage-9 coefficient table contains a structural slot3 (`29/29`), but the canonical selector has no branch that writes slot3. **Slot3 is unreachable.**

This is deterministic escalation; there is no stage-9 RNG in the slot choice.

## Interaction between Talk and escalation

Talk does not increment `$064D` or `$EF`.

Consequences:

- repeated pre-threshold Talk does not advance Aphrodite's attack tier;
- a Talk that forces `$DC++` causes a Gold response using the current `$064D` tier;
- only Bronze actions move the normal slot sequence 0 -> 1 -> 2;
- the first `$EB=$01` event can jump the selector directly to slot2 through `$064D=$80`.

## Post-Gold `$AAF0`

`$AAF0` invokes the already-closed player-condition classifier `$AD4D` and consumes `$EA`.

### `$EA=$00`

Return. Battle continues.

### `$EA=$01`

Run low-player feedback (`$3D` followed by `$91`) and return.

There is no one-time stage latch here: the branch is repeatable while the generic classifier continues to return `$01`.

### `$EA=$FF`

```text
LDA #$FF
JMP $ACAA
```

This is ordinary generic defeat release `$FF`.

## `$FE` ownership and the winner's resource zeroing

Generic release owner `$ACAA` treats `$FE` specially before the outer fixed story progression resumes:

```text
$05BC/$05BD = 0   ; active Cosmo
$05CE/$05CF = 0   ; active Life
...
```

The winner's active battle resources are therefore zeroed by the existing generic `$FE` owner. Pisces does not need a duplicate resource rule in its stage-local model; the stage merely emits `$FE` and joins that owner.

This behavior is also narratively consistent with the Bloody Rose victory sequence, but the resource-zero conclusion is ROM-derived.

## Pisces `$FE` -> story progress `$0C` -> special stage `$0C`

Fixed `$E3F7+` recognizes `$FE`, performs the existing Saint-record handoff/force-Seiya sequence and transfers into `$E3B3` story progression.

From canonical Pisces progress:

```text
$067D: $0B -> $0C
```

Progress `$0C` has a special branch before the ordinary `$E50B` table path. It tests Seiya's story-roster bit (`$01`) in the pre-existing `$0673`.

### Pisces entered with Shun

Common battle entry changed `$0673` from `$3A` to `$3E`; Seiya's bit remains clear.

The `$0C` special branch selects:

```text
active Saint $0533 = 0      ; Seiya
$06CD = $0E
$0673 = $0E | $30 = $3E
```

### Pisces entered with Seiya

Common battle entry changed `$0673` from `$3A` to `$3B`; Seiya's bit is set.

The `$0C` special branch selects:

```text
active Saint $0533 = 2      ; Shun
$06CD = $0B
$0673 = $0B | $30 = $3B
```

The fixed story-stage table then gives:

```text
$F016[$0C] = $0C
```

So Pisces does not jump directly to Saga stage `$0A`. Its proven successor is the **special/final stage `$0C` boundary**. The localization corpus already labels the known `$A1AD` Talk context there as `FINAL_SPECIAL`; that successor is intentionally left for the next closure rather than guessed inside Pisces.

## Executable specification

`src/SaintSeiyaNesReborn.OriginalSpec/PiscesStage09Context.cs` models only stage-owned composition:

- canonical Seiya/Shun reachability from `$0673=$3A`;
- no-op `$9B5C` initialization;
- Talk dodge threshold and Shun `$066F/+1000` branch;
- `$064D/$EF` Bronze-action progression;
- exact `$EF==2/$05` Shun technique increments;
- first-low `$064D=$80` latch;
- `$EB` / `$EA` stage-local decisions;
- deterministic Gold slots 0/1/2 and coefficients;
- victory +1200 Seventh Sense delta;
- `$FE/$FF` release ownership;
- both exact `$0C` successor variants.

Discriminating fixtures live in:

`tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/PiscesStage09ContextChecks.cs`

They specifically distinguish:

- Seiya vs Shun roster paths;
- `<2` vs `>=2` dodge-history Talk;
- first Shun post-threshold Talk vs repeated Talk;
- Shun `$EF==2` and `$EF==5` growth vs Seiya misses;
- `$064D` slot boundaries 2/3/5/6 and `$80`;
- technique growth occurring before same-action victory;
- low-opponent latch vs defeat;
- `$EA=$01` feedback vs `$EA=$FF` defeat;
- Shun-victory -> Seiya stage `$0C` and Seiya-victory -> Shun stage `$0C`.

## Closed conclusions

1. Stage `$09` has no local init body; `$9B5C` is `RTS`.
2. Canonical story state leaves only Seiya and Shun reachable.
3. Talk before two Gold dodge attempts is free dialogue; at two attempts the first Shun Talk grants +1000 Seventh Sense and advances `$066F`; all other post-threshold Talk forces the Gold response.
4. `$EF` counts Bronze actions only.
5. Shun's technique count increments exactly when `$EF` becomes 2 and 5, provided Shun is active on those exact actions.
6. `$064D` increments every Bronze action and selects Aphrodite slots 0 -> 1 -> 2 at thresholds 3 and 6.
7. First `$EB=$01` sets `$064D=$80`, immediately forcing slot2 and preventing normal first-low replay.
8. Aphrodite defeat grants +1200 Seventh Sense and releases `$FE`.
9. `$EA=$01` is nonterminal feedback; `$EA=$FF` is generic `$FF` defeat.
10. Structural Gold slot3 is unreachable.
11. Pisces `$FE` joins the generic resource-zeroing stage-advance owner and advances to special stage `$0C`, with the exact next active Saint determined by Seiya's `$0673` bit.

Do not reopen Pisces without contradictory ROM evidence or a failing fixture.

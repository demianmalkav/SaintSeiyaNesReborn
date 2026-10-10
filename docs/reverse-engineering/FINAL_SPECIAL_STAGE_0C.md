# Final-special story stage `$0C`

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: **CONFIRMED static reconstruction** of the complete reachable final-special stage `$067D/$050E=$0C` graph between Pisces/Aphrodite and Saga.

Canonical complete-file SHA-256:

`6917B31D7343A9A17170E833BACDBC3B1EBA3E02D11C51C0D44DBE436C9AD43A`

This context is **not** a normal Gold-Saint boss battle. Its reachable behavior is owned by fixed command/action code plus bank-5 Talk `$A1AD`. Ordinary boss initialization, opponent damage, post-Bronze, Gold dodge/damage and post-Gold response are all bypassed.

## Closed boundary

Pisces victory release `$FE` advances story progress to `$0C` and produces one of two exact states:

```text
Shun won Pisces
  -> $067D=$0C
  -> $050E=$0C
  -> active Saint $0533=0 (Seiya)
  -> $06CD=$0E
  -> $0673=$3E

Seiya won Pisces
  -> $067D=$0C
  -> $050E=$0C
  -> active Saint $0533=2 (Shun)
  -> $06CD=$0B
  -> $0673=$3B
```

The generic reset before command ownership clears the ordinary transient battle state, including `$DC`, `$066F`, `$0670`, `$0677/$0678`, `$064D/$064E`, `$067C`, `$068A`, `$068E`, `$0690`, `$06B8` and `$DD`. Canonical `$0C` therefore starts with `$DC=0`, `$066F=0` and release `$0670=0`.

## Relevant ROM anchors

| Role | Bank | CPU address |
|---|---:|---:|
| stage initialization dispatcher | 5 | `$97DB` |
| final-special Talk | 5 | `$A1AD` |
| scripted reward/presentation helper | 5 | `$A1FF` |
| top-level command dispatcher | fixed | `$F025+` |
| Attack final-special branch | fixed | `$F08B+` |
| Escape final-special branch | fixed | `$F14A+` |
| technique-selection final-special cancel gate | bank 5/fixed composition | `$A349+` / `$F873+` |
| Bronze action final-special special case | 1 | `$ABF4+` |
| special rose-clearing call | fixed | `$FF9F` |
| rose-clearing effect | 0 | `$B900+` |
| post-Bronze stage gate | fixed | `$F925+` |
| Gold response/dodge stage gate | fixed | `$F936+` |
| release `$01` fixed owner | fixed | `$E399+` |
| story progression core | fixed | `$E3B3+` |
| story descriptor table | fixed | `$E50B` |
| story-stage table | fixed | `$F016` |

## 1. The stage-`$0C` initialization slot is malformed but unreachable

The bank-5 initialization dispatcher at `$97DB` indexes the pointer table beginning at `$97E1` with `$050E`.

The intentional entries cover stages `0..11`. If stage `$0C` were dispatched through that table, index 12 reads past its end and interprets bytes `$97F9/$97FA` as:

```text
$00 $8D -> pointer $8D00
```

`$8D00` lies in bank-5 table/data context rather than being a valid final-special initializer. It must **not** be promoted as executable stage logic.

Canonical reachability explains why this is safe. Before `$97DB`, the common entry routine consults the fixed mandatory-Saint table at `$F36F`. Its stage-`$0C` entry is:

```text
$F36F[$0C] = $FF
```

The only reachable active Saints here are Seiya (`0`) and Shun (`2`). Neither can equal `$FF`, so the entry path saves the selected Saint/roster state and returns before the initialization dispatcher is called.

Conclusion:

> The stage-12 initialization pointer is an out-of-range structural artifact. It is provably unreachable for both canonical `$0C` entries.

## 2. Command topology at `$0C`

The ordinary four-command battle menu still supplies command indices `1..4`, but fixed code specializes their ownership:

| Command | Meaning | Stage `$0C` behavior |
|---:|---|---|
| 1 | Attack | special rose-clearing action; only advancing command |
| 2 | Talk | `$A1AD`; fixed dialogue, one-time +1000 reward |
| 3 | Escape | intercepted; message `$D4`; no release |
| 4 | resource allocation | suppressed/redraw-only; `$FB89` is not entered |

This stage therefore reuses the command surface without reusing the ordinary boss response machine.

## 3. Talk `$A1AD`: same graph for Seiya and Shun

The complete handler is:

```text
$A1AD  LDA #$D3
$A1AF  JSR $E7B7
$A1B2  LDA #$D5
$A1B4  JSR $E7C7
$A1B7  LDA $066F
$A1BA  BNE $A1C4
$A1BC  LDA #$10
$A1BE  JSR $A1FF
$A1C1  INC $066F
$A1C4  RTS
```

There is no `$0533` read or active-Saint branch. This corrects the earlier provisional description that suggested separate Seiya/Shun Talk paths.

The private localization corpus corroborates the scene identities:

- `$D3` / `MSG_211`: Marin tells Seiya to use his fist to clear the roses;
- `$D5` / `MSG_213`: Seiya reflects that Marin seemed like his sister.

Control semantics remain ROM-derived.

### First Talk

With canonical `$066F=0`:

```text
#$10 -> $A1FF -> fixed $F31E
INC $066F
```

The already-closed resource specification proves `#$10 -> +1000 Seventh Sense`.

Material effects:

- messages `$D3`, `$D5`;
- +1000 Seventh Sense;
- `$066F: 0 -> 1`;
- `$DC` remains zero;
- `$0670` remains zero.

### Repeat Talk

With `$066F!=0`, the same two messages are displayed but `$A1FF` is skipped.

No additional Seventh Sense is granted and no release is produced.

Therefore Talk cannot leave stage `$0C`.

## 4. Escape is explicitly blocked

Fixed command-3 handling around `$F14A+` checks:

```text
CMP #$0C
BNE ordinary escape handling
LDA #$D4
...
```

`$D4` / `MSG_212` says that the protagonist must face the Patriarch alone. The command then returns to the command flow without setting `$0670`.

Escape is therefore a narrative refusal, not an alternate exit.

## 5. Resource allocation is suppressed

Command 4 normally enters the Life/Cosmo/Seventh-Sense allocation UI through fixed `$FB89`.

At `$F041+`, stage `$0C` satisfies the `>= $0C` gate and skips `$FB89`, taking only the redraw/return path.

Thus the final-special context does not allow the normal resource-allocation subsystem despite retaining the visible four-command command surface.

## 6. Attack becomes a rose-clearing special action

Command 1 reaches the fixed stage check around `$F08B`:

```text
CMP #$0C
BNE ordinary path
JSR $E754
LDX #$50
LDY #$2C
JSR $E719
JSR $E747
JSR $F813
LDA #$01
STA $0670
RTS
```

The action always ends by writing release `$0670=$01`.

### Technique selection cannot be cancelled

The normal technique selector is still shown. In the final-special branch, the cancel/back case at bank-5 `$A349+` detects stage `$0C` and loops back into technique selection instead of returning the normal cancel value.

Once Attack is chosen, the player must choose a technique; the command cannot be backed out through the ordinary battle cancellation route.

### Ordinary resource presentation is bypassed

Fixed `$F8DF+` checks stage `$0C` and jumps directly to the action continuation, bypassing the ordinary resource presentation block at `$F8E9-$F8F6`.

### Bank-1 action special case

Bank 1 `$ABF4+` contains explicit stage-`$0C` behavior:

```text
$0632 = $02
$06BC = $00
```

The chosen Bronze technique/attack identity is still constructed and presented. But the special branch calls fixed `$FF9F`, and the later stage check explicitly skips generic opponent damage `$AD52`.

Consequences:

- chosen technique still determines the visual attack presentation;
- `$06BC` is deliberately cleared;
- no ordinary opponent Life/Cosmo damage is applied.

## 7. `$FF9F -> bank0 $B900`: the rose-clearing effect

Fixed `$FF9F` temporarily maps bank 0 and calls `$B900`.

`$B900` starts by writing:

```text
$050E = $12
```

This is a **transient effect/presentation stage value**, not a story-progress transition.

The routine initializes the special sprite/OAM effect and iterates until:

```text
$06C1 == $40
```

So the effect runs for 64 progression ticks/iterations.

Its terminal block writes:

```text
$0632 = $1A
$050E = $0C
RTS
```

The canonical stage index is therefore restored before fixed `$F08B+` emits release `$01`.

The visible effect aligns with Marin's instruction to blow away the roses, but the `$12 -> $0C` lifecycle and 64-step loop are established directly from code.

## 8. No ordinary boss response is reachable

Several independent fixed gates agree:

### No post-Bronze dispatcher

At `$F925+`:

```text
LDA $050E
CMP #$0B
BCC dispatch_post_bronze
RTS
```

Stage `$0C` returns before `$A361`. The nominal stage-12 pointer `$A3A1` (`RTS`) is therefore structurally present but never reached by this attack path.

### No Gold attack/dodge/damage

At `$F936+`:

```text
LDA $050E
CMP #$0B
BCC ordinary_gold_response
RTS
```

Stage `$0C` never enters Gold technique selection, dodge polling or Gold damage.

### No post-Gold dispatcher

Because the Gold response path is never entered, stage-12 post-Gold `$A3A1` is likewise unreachable.

### Reachability summary

```text
stage-$0C init dispatcher       NO
ordinary opponent damage       NO
post-Bronze dispatcher          NO
Gold response / dodge / damage NO
post-Gold dispatcher            NO
resource allocation             NO
```

This is why `$0C` must not inherit Taurus/Leo/Virgo/Aquarius/Pisces boss semantics.

## 9. Release `$01` is the exact `$0C -> $0D` owner

The top-level command loop consumes `$0670` after command execution.

For release `$01`, fixed `$E399+` joins the ordinary story progression core at `$E3B3`:

```text
INC $067D
```

Starting from final-special progress `$0C`:

```text
$067D: $0C -> $0D
```

After the increment, the special `$067D==$0C` branch no longer applies. The ordinary descriptor table is used:

```text
$E50B[$0D] = $0E
$06CD = $0E
$0673 = $0E | $30 = $3E
```

The fixed story-stage table then gives:

```text
$F016[$0D] = $0A
```

So release `$01` proves the exact handoff:

```text
final-special stage $0C
    -> story progress $0D
    -> descriptor $06CD=$0E
    -> roster $0673=$3E
    -> stage $0A Saga
```

The fixed progression path only force-switches active Ikki before this branch. Ikki is impossible in the two reachable `$0C` entries, so the active Saint is preserved:

```text
Seiya $0C entry -> Saga boundary with active Seiya
Shun  $0C entry -> Saga boundary with active Shun
```

Both variants otherwise converge on `$067D=$0D`, `$06CD=$0E`, `$0673=$3E`, `$050E=$0A`.

## Executable specification

`src/SaintSeiyaNesReborn.OriginalSpec/FinalSpecialStage0CContext.cs` models:

- both exact Pisces-derived entry identities;
- unreachable malformed init-table slot (`$8D00` structural overrun);
- shared `$A1AD` Talk and one-time +1000 reward;
- Escape `$D4` refusal;
- suppressed resource-allocation command;
- forced technique selection after Attack is chosen;
- temporary `$050E=$12` rose effect and restoration to `$0C`;
- explicit exclusion of generic opponent damage/post-Bronze/Gold/post-Gold;
- release `$01` ownership;
- exact `$0D -> stage $0A` Saga boundary while preserving the active Saint.

Discriminating fixtures live in:

`tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/FinalSpecialStage0CContextChecks.cs`

## Closed conclusions

1. Both Pisces-derived variants enter one shared final-special control graph.
2. The apparent stage-12 init pointer `$8D00` is an unreachable table overrun, not executable final-special logic.
3. `$A1AD` is identical for Seiya and Shun; first Talk grants +1000 Seventh Sense and increments `$066F`, repeat Talk does not reward again.
4. Talk never sets `$DC` and never releases the stage.
5. Escape is blocked by message `$D4`; it cannot progress.
6. Resource allocation is suppressed at stage `$0C`.
7. Attack is the only advancing command; after Attack is chosen, technique selection cannot be cancelled.
8. The selected technique is presented, but generic opponent damage is explicitly skipped.
9. `$B900` temporarily sets `$050E=$12`, runs the rose-clearing effect to `$06C1=$40`, writes `$0632=$1A`, then restores `$050E=$0C`.
10. Stage `$0C` never reaches post-Bronze, Gold dodge/damage or post-Gold dispatchers.
11. The special attack emits release `$01`.
12. Release `$01` advances `$067D $0C->$0D`; descriptor `$0E` produces `$0673=$3E`; story table selects stage `$0A` Saga.
13. Seiya/Shun active identity survives the handoff, so Saga must accept both proven entry variants.

Do not reopen final-special stage `$0C` without contradictory ROM evidence or a failing fixture.
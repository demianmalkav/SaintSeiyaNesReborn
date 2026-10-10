# Battle narrative dispatchers — per-stage event state machines

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: all four stage-indexed dispatcher families are statically confirmed and the canonical `$050E=$00-$0B` namespace is coverage-audited end-to-end. Dedicated contexts are closed for `$00/$01/$02/$03/$04/$05/$06/$08/$09/$0A`; **Capricorn `$07` is the only remaining material gap**. `$0B` is structural/transient. Final-special `$0C` is closed separately.

See `BATTLE_STAGE_CONTEXT_COVERAGE.md` for the denominator and `BOSS_CONTEXT_STAGE_06_SCORPIO.md` for the Scorpio composite and its progress-`$08` bridge.

## Stage-indexed architecture

PRG bank 5 repeatedly uses:

```text
LDA $050E
JSR $E698
<inline pointer table>
```

`$E698` is the shared indirect dispatcher.

## 1. Initialization `$97DB` / table `$97E1`

| Stage | Handler |
|---:|---:|
| `$00` | `$97F7` |
| `$01` | `$97F8` |
| `$02` | `$981F` |
| `$03` | `$9851` |
| `$04` | `$989D` |
| `$05` | `$9A28` |
| `$06` | `$9ACE` |
| `$07` | `$9ACF` |
| `$08` | `$9B14` |
| `$09` | `$9B5C` |
| `$0A` | `$9B5D` |
| `$0B` | raw `$A960`; structural only |

Scorpio `$9ACE` is exactly `RTS`; there is no stage-specific intro reward, presentation or release handoff.

Stage `$0B` has no stable canonical provenance: `$F016` never returns `$0B`, and `$A960` points into the real instruction beginning at `$A95F`.

## 2. Talk `$9C95` / table `$9C9B`

| Stage | Handler |
|---:|---:|
| `$00` | `$9CB7` |
| `$01` | `$9D2C` |
| `$02` | `$9D81` |
| `$03` | `$9D96` |
| `$04` | `$9DD8` |
| `$05` | `$9E1B` |
| `$06` | `$9E51` |
| `$07` | `$9ED6` |
| `$08` | `$9F00` |
| `$09` | `$9F99` |
| `$0A` | `$9FF4` |
| `$0B` | `$9FF4` structural alias |
| `$0C` | `$A1AD` separate final-special handler |

### Scorpio `$9E51`

Helper `$A1EC` computes the 8-bit sum `$0678+$0677`.

With total below two:

```text
Hyoga ($0533=$01):
  messages $84/$85
  if $068A==0:
      INC $068A
      #$03 -> $A1FF -> +300 Seventh Sense
  else:
      no second reward

Seiya/Shun/Shiryu:
  messages $F8/$3E
  no reward/state mutation
```

With total at least two and `$066F==0`:

```text
per-Saint message table $9ED2 = 86 86 87 86
message $A3
INC $066F
#$02 -> $A1FF -> +200 Seventh Sense
```

With total at least two and `$066F!=0`:

```text
repeat per-Saint message + $A3
Hyoga -> return
Seiya/Shun/Shiryu -> INC $DC -> fixed caller forces Gold response
```

`$068A` and `$066F` are independent reward gates.

## 3. Post-Bronze `$A361` / table `$A367`

| Stage | Handler |
|---:|---:|
| `$00` | `$A3A1` |
| `$01` | `$A3A2` |
| `$02` | `$A444` |
| `$03` | `$A50F` |
| `$04` | `$A5B3` |
| `$05` | `$A661` |
| `$06` | `$A7FF` |
| `$07` | `$A86B` |
| `$08` | `$A8FC` |
| `$09` | `$AA57` |
| `$0A` | `$AB18` |
| `$0B` | `$A3A1` structural |

### Scorpio `$A7FF`

After shared helpers `$ADC4/$ACD6`:

```text
$EB=$FF -> victory presentation -> release $01
otherwise:
    $06BC!=0 -> message $A6 -> continue
    $06BC==0 -> continue without Scorpio-local feedback
```

Only `$EB=$FF` is terminal. Scorpio does not distinguish `$EB=$00` from `$EB=$01` for a separate local terminal.

## 4. Post-Gold `$A381` / table `$A387`

| Stage | Handler |
|---:|---:|
| `$00` | `$A3A1` |
| `$01` | `$A415` |
| `$02` | `$A4CC` |
| `$03` | `$A560` |
| `$04` | `$A63E` |
| `$05` | `$A7B3` |
| `$06` | `$A847` |
| `$07` | `$A8D8` |
| `$08` | `$A9D3` |
| `$09` | `$AAF0` |
| `$0A` | `$AC05` |
| `$0B` | `$A3A1` structural |

### Scorpio `$A847`

After shared classifier `$AD4D`:

```text
$EA=$00 -> return/continue
$EA=$01 -> messages $A4/$91 -> continue
$EA=$FF -> release $FF defeat
```

The low-player feedback is repeatable; no Scorpio one-shot latch is consulted.

## 5. Scorpio Gold selector `$908C+`

Bank 6 owns a dedicated branch before the generic parity fallthrough:

```text
908C LDA $050E
908F CMP #$06
9091 BNE $90A8
9093 LDA $0677
9096 CLC
9097 ADC $0678
909A CMP #$02
909C BCC $90A3
909E LDA #$00
90A0 JMP $9143
90A3 LDA #$01
90A5 JMP $9143
```

Therefore:

```text
8-bit dodge sum < 2  -> slot 1
8-bit dodge sum >= 2 -> slot 0
```

Only slots `0,1` are reachable.

## 6. Scorpio retry owner

Generic `$FF` defeat leaves story progress `$07`. On normal re-entry common `$A973` clears, among other fields:

```text
$066F $0670 $0677 $0678 $067C $068A $068E $0690 $06B8
```

This resets the Scorpio dodge selector and rearms both one-time Talk rewards.

## 7. Scorpio successor ownership

Scorpio victory release `$01` does not hand directly to Capricorn.

First fixed progression:

```text
$067D:07->08
$F016[08]=$10
$E50B[08]=$00
$050E=$10
$06CD=$00
$0673=$30
```

Fixed `$E4D7` maps progress `$08` to principal platform substate:

```text
$02=$08
```

The already-closed common principal platform gate for `$02=$08` requires:

```text
X >= $D0
Y == $40
jump phase == 0
```

and exits through normal `State3DReload` (`$3D/$E100`).

After reload, fixed `$E2DD` sees story-stage `$050E=$10`, synthesizes release `$01`, and fixed progression reaches:

```text
$067D:08->09
$F016[09]=$07
$E50B[09]=$00
$050E=$07
$06CD=$00
$0673=$30
```

Active Seiya/Hyoga/Shun/Shiryu is preserved throughout.

Namespace rule: `$050E=$10` is a story-stage value. It is not special-normal platform substate `$02=$10`; this bridge uses `$02=$08`.

## 8. Canonical story-stage provenance

```text
00->00  01->01  02->02  03->03  04->04
05->05  06->0F  07->06  08->10  09->07
0A->08  0B->09  0C->0C  0D->0A  0E->00
```

Canonical ordinary battle entries:

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

## 9. Gold-slot coverage

| Stage | Reachable slots |
|---:|---|
| `$00` | none |
| `$01` | `0,1` |
| `$02` | `0,1` |
| `$03` | `0,1` |
| `$04` | `0,1` |
| `$05` | `0,1,2` |
| `$06` | `0,1` |
| `$07` | `0,1` |
| `$08` | `0,1,2` |
| `$09` | `0,1,2` |
| `$0A` | `0,1,2,3` |
| `$0B` | none |

## Coverage classification

```text
closed dedicated : 00 01 02 03 04 05 06 08 09 0A
material missing : 07
structural only  : 0B
separate closed  : 0C
```

The next stage-local checkpoint is **Capricorn / Shura `$07`**, owned by `$9ACF/$9ED6/$A86B/$A8D8`. Scorpio's checkpoint terminates at the exact progress `$09` / stage `$07` boundary and does not reopen those internals.

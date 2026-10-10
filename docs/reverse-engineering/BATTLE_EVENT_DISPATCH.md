# Battle narrative dispatchers — per-stage event state machines

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: all four stage-indexed dispatcher families are statically confirmed and every material canonical `$050E=$00-$0A` battle context is now closed by an executable stage model. `$0B` remains structural/transient with no canonical stable battle; final-special `$0C` is closed separately.

See `BATTLE_STAGE_CONTEXT_COVERAGE.md` for the denominator, `BOSS_CONTEXT_STAGE_06_SCORPIO.md` for the Scorpio bridge, and `BOSS_CONTEXT_STAGE_07_CAPRICORN.md` for the final material stage-local closure.

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

### Capricorn initializer dispatch is gated before `$97DB`

The outer battle-entry owner at `$9770-$97B8` first tests `$068E`, then compares the active Saint against `$F36F[$050E]`.

Exact designated-Saint table prefix:

```text
$F36F: FF 00 02 03 00 FF FF 03 01 FF FF FF FF
```

Therefore:

```text
stage $07 -> designated Saint $03 -> Shiryu
```

Fresh Seiya/Hyoga/Shun entries bypass `$9ACF`. Fresh Shiryu reaches it.

`$9ACF` itself begins with an independent `$0670==$FE` early return. The ordinary Shiryu path clears `$0672`, increments `$058A/$0696`, grants +600 Seventh Sense and exits through shared `$9C3D` release `$03`.

Stage `$0B` has no stable canonical provenance: `$F016` never returns `$0B`, and `$A960` points inside the real instruction beginning at `$A95F`.

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

### Capricorn `$9ED6`

```text
message $AE
if $066F==0:
    per-Saint table $43/$43/$43/$AF
    INC $066F
    return
else:
    same semantic per-Saint table through repeat display helper
    INC $DC
    INC $066F
    return
```

The first Talk does not force Gold. Every repeated Talk raises transient `$DC`; the fixed caller converts that into the Gold-response path. `$066F` keeps incrementing as an 8-bit counter.

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

### Capricorn `$A86B`

After shared classifier `$ACD6`:

```text
$EB=$00:
  $06BC!=0 -> continue
  $06BC==0 -> message $8B -> continue

$EB=$01:
  active Shiryu -> continue, no $0690 mutation
  Seiya/Hyoga/Shun -> $0690=$FF -> continue

$EB=$FF:
  scripted victory
  $06B1=$FF
  #$08 -> $F31E -> +800 Seventh Sense
  release $FE
```

Fixed `$FAB9-$FAE5` makes `$0690` executable state:

```text
$0690!=0 -> force $06BC=0
$0690==0 -> preserve generic hit-token ownership
```

Thus the stage context composes the override and does not duplicate generic hit arithmetic.

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

### Capricorn `$A8D8`

After shared player classifier `$AD4D`:

```text
$EA=$00 -> continue
$EA=$01 -> repeatable messages $40/$91 -> continue
$EA=$FF -> release $FF defeat
```

No Capricorn one-shot low-player latch exists.

## 5. Gold selection

Canonical reachable slots remain:

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

Capricorn falls through generic parity at `$913E`:

```text
slot = $065F & 1
```

## 6. Capricorn `$FF` retry ownership

Defeat `$FF` leaves progress `$09`. Fixed `$E4D7` maps that progress to principal platform substate `$02=$09`.

Accepted principal-platform exit remains the shared gate:

```text
X >= $D0
Y == $40
jump phase == 0
-> State3DReload / $E100
```

Warm reload reaches `$ED57`, which invokes `$A973` for inherited `$FF` and clears encounter-local state including `$066F/$0670/$068E/$0690`.

The decisive continuation is:

```text
$E2DD sees stage $07, not story-stage $10
$0670 is now 0
-> $E2E7 BEQ $E33D
-> direct command-loop resume
```

No call to `$970A/$97DB/$9ACF` occurs on this retry. Therefore the Shiryu +600 reward and `$058A/$0696` increments are not replayed; technique counts and `$0672` survive the reset unchanged.

## 7. Capricorn successor ownership

Scripted victory release `$FE` is handled by fixed `$E3ED-$E414`:

```text
save winning Saint record
force $0533=$00 (Seiya)
rewrite $0670=$01
join ordinary story increment
```

From progress `$09`:

```text
$067D:09->0A
$F016[$0A]=$08
$E50B[$0A]=$08
$050E=$08
$06CD=$08
$0673=$38
active Saint = Seiya
```

This is the exact already-closed Aquarius/final-Camus story boundary. Capricorn closure stops there and does not reopen stage `$08` internals.

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

## Coverage classification after Capricorn closure

```text
closed dedicated : 00 01 02 03 04 05 06 07 08 09 0A
material missing : NONE
structural only  : 0B
separate closed  : 0C
```

The stage-indexed boss/event dispatcher layer has no remaining material `$00-$0B` gap. Subsequent ORIGINAL SPEC work must move to unresolved global subsystems rather than extend this denominator.

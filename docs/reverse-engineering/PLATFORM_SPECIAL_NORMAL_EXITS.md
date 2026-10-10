# Special normal platform exits `$02=$0C-$10`

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: **CONFIRMED statically** for reachability/provenance, physical exit gates and the warm-reload handoff through the already-closed interactive/destination machinery.

Scope: platform substates `$0C-$10`. Substate `$11` is explicitly excluded: it is the already-closed special `$70->$89->$8F` narrative chain.

## Common physical exit body

All five special-normal substates still use the ordinary bank-1 exit body at `$96CB`.

`$969D-$9713` first rejects nonzero jump phase `$49`. Substate `$10` additionally rejects internal Saint index `1` (Shun). The coordinate table at `$9714` supplies the special endpoints:

| substate | min player X | exact player Y | exit kind |
|---:|---:|---:|---|
| `$0C` | `$88` | `$20` | normal `$3D` reload |
| `$0D` | `$B4` | `$30` | normal `$3D` reload |
| `$0E` | `$B4` | `$80` | normal `$3D` reload |
| `$0F` | `$B4` | `$40` | normal `$3D` reload |
| `$10` | `$B4` | `$70` | normal `$3D` reload; Shun rejected |

On acceptance, every one executes:

```text
$04 = $00
JSR $951F        ; snapshot all five Saints
$00 = $3D
$01 = $3D
...
JMP $E100
```

Therefore the distinguishing semantics are not in the gate itself. They are the persistent values carried into `$E100` by the routines that created each substate.

## Provenance of the five substates

### `$0C` — stage-3 Talk phase

Bank 5 `$9D96+`:

```text
$9D96  LDA $067C
       BNE ...
...
$9DBF  LDA #$0C
$9DC1  STA $02
$9DC3  LDA #$02
$9DC5  STA $0670
```

This path belongs to reload stage `$050E=$03`, uniquely progression `$067D=$03`, and is taken only while `$067C=0`.

Entry profile:

```text
$02   = $0C
$067D = $03
$050E = $03
$0670 = $02
$067C = $00 at creation
$06CD = $02
```

### `$0D` — stage-5 special action

Bank 5 stage-5 `$A361` handler, `$A6A8+`:

```text
$A6A8  LDA $067C
       BNE ...
...
$A6E7  LDA #$0D
$A6E9  STA $02
$A6EB  LDA #$02
$A6ED  JMP $ACAA     ; common $0670 writer
```

Profile:

```text
$02   = $0D
$067D = $05
$050E = $05
$0670 = $02
$067C = $00 at creation
$06CD = $02
```

### `$0E` — stage-2 special action

Bank 5 stage-2 `$A361` handler, `$A444+`:

```text
$A444  LDA $067C
       BNE ...
...
$A468  LDA #$0E
$A46A  STA $02
$A46C  LDA #$02
$A46E  JMP $ACAA
```

Profile:

```text
$02   = $0E
$067D = $02
$050E = $02
$0670 = $02
$067C = $00 at creation
$06CD = $00
```

### `$10` and `$0F` — late progression map

Fixed `$E4D7` indexes `$E4E0[$067D]` and stores it to `$02` at `$E4DD`.

Relevant table entries are:

```text
new $067D=$0C -> table byte $E4EC = $10
new $067D=$0D -> table byte $E4ED = $0F
new $067D=$0E -> table byte $E4EE = $11
```

The `$0E->$11` entry is the already-closed narrative branch. The two normal cases are:

```text
substate $10:
  $067D=$0C
  $050E=$0C
  inherited $0670=$01
  $06CD=$0E

substate $0F:
  $067D=$0D
  $050E=$0A
  inherited $0670=$05
  $06CD=$0E
```

PR #104 already proved the special progression transition into `$067D=$0C`: old `$0673` bit 0 selects either canonical Seiya (`$0533=0`, `$0673=$3E`) or canonical Shun (`$0533=2`, `$0673=$3B`). No other Saint can normally create substate `$10`.

## Persistent values survive active platform mode

The special substate frame paths do not overwrite the reload-critical progression selectors before an accepted gate. Bank-1 audits of the `$0C-$10` active helpers show no ordinary frame writer for `$067D`, `$050E`, `$06CE` or `$06CC`, and no active-frame `$0670` writer other than the reload reset helper outside platform play.

The engine-state entry also preserves the inherited terminal code:

- state `$00` enters the generic platform initializer and reaches active state `$20` without clearing `$0670`;
- state `$10` uses transitional `$D442`, increments `$00` to `$11`, then the generic initializer reaches `$20`; this path also does not clear `$0670`.

Thus the `$02/$05/$01` values above are real inputs to the later `$E100` reload.

## `$0C/$0D/$0E`: one-shot phase platforms

At `$ED57`, incoming `$0670=$02/$03` takes the dedicated branch:

```text
$ED8F  INC $067C
```

It deliberately skips reset helper `$A973`. This converts the creation-time `$067C=0` into `$067C=1` before the selector is reopened.

All three then reach `$E327`, where `$0670` and `$0584-$0586` are cleared and case 5 is seeded. The selector state is exactly the one already modeled in PR #102.

Therefore:

```text
$0C exit -> progression $03, stage $03, $067C=1 -> selector
$0D exit -> progression $05, stage $05, $067C=1 -> selector
$0E exit -> progression $02, stage $02, $067C=1 -> selector
```

This explains the function of the special platform: the action that created it is guarded by `$067C=0`; leaving the platform increments that phase field so the same creation branch cannot simply repeat.

### `$0E` Hyoga exception

`$ED57` contains one character-specific branch after incrementing `$067C`:

```text
if $050E == $02 and canonical $0533 == $01 (Hyoga):
    $06B8 = $0A
    $050E = $08
    $0690 = $FF
```

So the selector entry differs only for Hyoga:

```text
ordinary Saint: $050E=$02, $06B8=0
Hyoga:          $050E=$08, $06B8=$0A
```

Progression remains `$067D=$02`. The downstream selector and post-loop destination still compose with PRs #102/#104; for example release `$02` recomputes `$050E` from progression, while `$DD` preserves the temporary `$08` field on its direct `$90` commit.

## `$0F`: reset then Saga selector

Substate `$0F` reaches `$E100` with inherited `$0670=$05`.

It is neither `$01` nor `$02/$03`, so `$ED57` calls `$A973`. That reset clears, among other fields:

```text
$0670 = 0
$067C = 0
$06B8 = 0
```

while leaving progression `$067D=$0D` and stage `$050E=$0A` intact. Execution reaches `$E327` and seeds the standard selector.

Stable handoff:

```text
substate $0F
 -> normal $3D/$E100 reload
 -> $A973 reset
 -> selector seed
    progression $0D
    stage $0A
    $067C=0
    $06B8=0
```

From that point the existing stage-`$0A` Saga logic applies, including PR #104's `$FF + nonzero $06CE` selector reentry.

## `$10`: direct state-zero commit

Substate `$10` is structurally different.

Its progression entry retains `$0670=$01`. At `$E26A`, this is recognized **before** `$ED57`:

```text
CMP #$01
BNE ...
LDA #$05
STA $0670
JMP $E1F8
```

Therefore `$10` never reaches the interactive selector on exit.

Reachability further narrows the accepted path:

1. progression `$0B->$0C` can create only Seiya or Shun;
2. the physical substate-`$10` exit gate explicitly rejects internal Saint index 1 = Shun;
3. consequently an accepted normal `$10` exit is **Seiya-only**.

For that exact path, PR #104 supplied the progression-$0C fields and `$E1F8-$E254` supplies the final commit:

```text
$0670 = $05
$067D = $0C
$0533 = $00        ; Seiya
$050E = $0C
$0673 = $3E
$06CD = $0E
$06CC = $01        ; refreshed Seiya bit at $E200-$E206
$068F = $00

$00/$01 = $00
$03 = $00          ; internal Seiya
$05 = $01
JMP $C180
```

Shun can enter substate `$10` but cannot leave through this gate. Hyoga/Shiryu/Ikki cannot reach this substate through the confirmed normal progression provenance.

## End-to-end summary

| substate | provenance | inherited `$0670` | accepted-exit result |
|---:|---|---:|---|
| `$0C` | stage 3 Talk | `$02` | `$067C:0->1`, reenter selector at stage `$03` |
| `$0D` | stage 5 action | `$02` | `$067C:0->1`, reenter selector at stage `$05` |
| `$0E` | stage 2 action | `$02` | `$067C:0->1`, selector stage `$02`; Hyoga temporarily `$08/$06B8=$0A` |
| `$0F` | progression `$0D` | `$05` | `$A973` reset, reenter selector at Saga stage `$0A` |
| `$10` | progression `$0C` | `$01` | accepted only for Seiya; direct stable state `$00`, no selector |

The normal platform-exit family is therefore closed across `$02=$00-$10`; `$11` remains the separate already-closed `$70-$89` narrative path.

## Clean-room implementation

`PlatformSpecialNormalExitPipeline` encodes:

- exact special-substate provenance and inherited reload fields;
- composition with `PlatformExitGate`;
- phase increment/reset behavior before selector entry;
- `$0E` Hyoga remap;
- `$0F` Saga selector handoff;
- `$10` provenance restriction and direct stable state-$00` commit;
- selector seed reuse from `PlatformWarmReloadInteractiveState` and stable-result shape compatible with `PlatformNormalWarmReloadDestination`.

Self-tests discriminate all five substates, gate failures, the Seiya/Shun `$10` split, Hyoga's `$0E` exception and composition with the already-verified #104 destination model.

# Boss/event context stage `$02` — Gemini + redirected first Camus

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: **CONFIRMED static reconstruction** of the complete reachable stage-local `$050E=$02` control graph, including its mandatory platform detour and the Hyoga redirect into the already-closed first-Camus phase of stage `$08`.

Canonical complete-file SHA-256:

`6917B31D7343A9A17170E833BACDBC3B1EBA3E02D11C51C0D44DBE436C9AD43A`

Generic damage/resource/dodge arithmetic, the `$0E` platform gate/reload machinery and first-Camus internals are composed from already-closed specifications rather than duplicated here.

## Closed boundary

Canonical story entry:

```text
$067D = $02
$050E = $02
```

Stage-local and directly composed owners:

| Role | PRG bank | CPU address | ROM file offset |
|---|---:|---:|---:|
| initialization | 5 | `$981F` | `$1582F` |
| shared intro tail | 5 | `$9C3D` | `$15C4D` |
| Talk | 5 | `$9D81` | `$15D91` |
| post-Bronze | 5 | `$A444` | `$16454` |
| post-Gold | 5 | `$A4CC` | `$164DC` |
| common battle reset | 1 | `$A973` | `$06983` |
| special reload/resume owner | fixed | `$ED57` | `$1ED67` |
| ordinary release-`$01` owner | fixed | `$E399/$E3B3` | `$1E3A9/$1E3C3` |
| story descriptor table | fixed | `$E50B` | `$1E51B` |
| story-stage table | fixed | `$F016` | `$1F026` |

## Canonical seed and roster

Fixed tables give:

```text
$F016[$02] = $02
$E50B[$02] = $00
```

so the story descriptor before battle-Saint selection is:

```text
$06CD = $00
$0673 = $00 | $30 = $30
```

Fixed Saint bits at `$FFC0` are:

```text
Seiya  $01
Hyoga  $02
Shun   $04
Shiryu $08
Ikki   $10
```

Therefore `$0673=$30` permits canonical battle Saints 0..3 and excludes Ikki. The reachable stage-2 entry roster is exactly:

```text
Seiya / Hyoga / Shun / Shiryu
```

Common reset `$A973` clears the stage-local fields that matter here:

```text
$066F = 0
$0670 = 0
$067C = 0
$068E = 0
$0690 = 0
$06B8 = 0
```

## Initialization `$981F`

The initializer is stage-specific and material.

Relevant control sequence:

```text
$981F JSR $9C6D
...
$9827 LDA #$11
$9829 JSR $F2ED        ; temporary presentation index
...
$9849 LDA #$03
$984B JSR $F31E        ; +300 Seventh Sense
$984E JMP $9C3D
```

Shared `$9C3D` later writes:

```text
$0670 = $03
$068E = $01
$050E = saved real stage ($02)
```

Thus canonical initialization semantics are:

```text
stage stays $02
+300 Seventh Sense
intro flag $068E=1
internal intro handoff $0670=$03
```

Release `$03` is consumed by the established interactive reload path and does **not** invoke `$A973`; the command loop begins with `$0670=0`.

## Talk `$9D81`

Exact control:

```text
$9D81 INC $DC
$9D83 message $45
$9D88 LDA $066F
$9D8B BNE return
$9D8D INC $066F
$9D90 message $43
$9D95 RTS
```

Consequences:

- every Talk increments transient `$DC` before any branch;
- the fixed caller therefore forces a Gold response after **every** Talk;
- only the first Talk mutates persistent stage-local state: `$066F:0->1`;
- repeated Talk leaves `$066F` nonzero and still forces the counterattack.

This matters across the platform detour: release `$02` special resume skips `$A973`, so an earlier `$066F=1` survives the ordinary non-Hyoga return from platform `$0E`.

## Post-Bronze `$A444`: phase zero intercepts before classification

The first instruction is the phase test:

```text
$A444 LDA $067C
$A447 BNE $A471
```

Only `$A471+` calls the generic opponent classifier `$ACD6`.

Therefore with canonical initial `$067C=0`, the first Bronze action is intercepted **before** opponent condition `$EB` is computed/consumed. Even a hypothetical action whose damage would otherwise defeat the opponent cannot take the ordinary victory branch yet.

Phase-zero tail:

```text
$A468 LDA #$0E
$A46A STA $02
$A46C LDA #$02
$A46E JMP $ACAA
```

Canonical result:

```text
platform substate $02 = $0E
release $0670 = $02
$067C remains 0 until reload
```

This is a mandatory first-Bronze detour.

## Platform `$0E` composition

The already-closed `PlatformSpecialNormalExitPipeline` owns the physical exit gate:

```text
X >= $B4
Y == $80
jump phase == 0
```

Accepted exit performs the normal `$3D/$E100` warm reload. Fixed `$ED57` sees incoming release `$02` and takes the special branch:

```text
$ED8F INC $067C
```

Crucially, this branch **does not call `$A973`**.

Therefore:

```text
$067C: 0 -> 1
$066F survives
```

The active-Saint split occurs immediately afterward.

### Seiya / Shun / Shiryu

For every reachable non-Hyoga Saint:

```text
$050E remains $02
$06B8 remains $00
$067C = 1
```

The command loop resumes ordinary stage `$02` with preserved Talk progress.

### Hyoga

Fixed `$ED99+` recognizes canonical Hyoga (`$0533=1`) and writes:

```text
$050E = $08
$06B8 = $0A
$0690 = $FF
$067D remains $02
$067C = 1
```

This is the already-closed **redirected first Camus** boundary represented by:

```text
AquariusStage08Context.EnterRedirectedFirstCamus(...)
```

`GeminiStage02Context` composes into that existing machine; it does not reimplement stage `$08`.

## Ordinary post-detour stage `$02`

Once `$067C!=0`, `$A444` calls the generic opponent classifier `$ACD6` and consumes `$EB`.

### `$EB=$FF` — opponent defeated

The stage-specific victory presentation ends with:

```text
LDA #$01
JMP $ACAA
```

so ordinary stage-2 victory emits release `$01`.

### `$EB=$01` — opponent low

Stage-local low-opponent feedback is emitted and the battle continues.

### `$EB=$00`

The handler checks generic Bronze hit token `$06BC`:

- hit: return/continue;
- miss/no-hit: stage-specific feedback, then continue.

No additional persistent stage-local counter is advanced on these branches.

## Gold response `$A4CC`

`$A4CC` calls the already-closed player-condition classifier `$AD4D` and consumes `$EA`:

```text
$EA=$00 -> healthy-player feedback, continue
$EA=$01 -> low-player feedback, continue
$EA=$FF -> release $FF
```

Stage `$02` has no special Gold selector branch. Bank 6 falls through to the generic parity selector:

```text
slot = $065F & 1
```

so canonical Gold slots are exactly `0,1`.

## Defeat / retry semantics

Generic stage-2 defeat is release `$FF` from `$A4CC`.

Fixed release handling does not advance `$067D`; story remains progress `$02`. On the subsequent normal stage re-entry, `$ED57` sees a release other than `$02/$03` and calls common reset `$A973`.

Therefore retry clears:

```text
$067C = 0
$066F = 0
$0670 = 0
$068E = 0
$06B8 = 0
```

and stage initialization is eligible to run again.

The important stage-local result is:

> **a generic `$FF` retry rearms the mandatory platform `$0E` detour.**

The next first Bronze action again encounters `$067C=0` and exits to `$0E`.

## Ordinary victory -> Cancer

Release `$01` joins fixed `$E399/$E3B3`.

For canonical ordinary stage-2 continuations, Hyoga is absent because his `$0E` exit redirects to first Camus. Reachable ordinary winners are Seiya/Shun/Shiryu; none triggers the special Ikki substitution at `$E3A0`.

Fixed progression gives:

```text
$067D: $02 -> $03
$F016[$03] = $03
$E50B[$03] = $02
$06CD = $02
$0673 = $02 | $30 = $32
```

Exact successor boundary:

```text
$067D = $03
$050E = $03
$06CD = $02
$0673 = $32
active Saint preserved
```

Stage `$03` Cancer/Death Mask remains outside this checkpoint.

## Redirected first Camus -> Cancer

Hyoga's platform exit composes into `AquariusStage08Context` with:

```text
story progress $02
phase $067C=1
$06B8=$0A
$0690=$FF
active Hyoga
```

The already-closed first-Camus machine has two canonical `$FE` terminals:

1. after the three-step Talk script, the next Bronze action triggers the scripted freezing sequence and release `$FE`;
2. actual Hyoga defeat during the first-Camus Gold response is converted into the same scripted `$FE` progression rather than generic `$FF` defeat.

Existing fixed `$FE` ownership forces Seiya, converts to ordinary story advancement and reaches:

```text
$067D: $02 -> $03
$050E = $03
active Saint = Seiya
```

Adding the same fixed progress-`$03` descriptor yields:

```text
$06CD = $02
$0673 = $32
```

Thus **both canonical first-Camus terminals converge on the same Cancer story boundary** as ordinary Gemini victory, with the expected distinction that `$FE` forces Seiya.

## Executable specification

`src/SaintSeiyaNesReborn.OriginalSpec/GeminiStage02Context.cs` models only stage-owned/composed semantics:

- exact reachable entry roster from `$0673=$30`;
- `$981F` intro handoff and +300 Seventh Sense reward contract;
- Talk `$9D81` and unconditional forced Gold response;
- phase-zero first-Bronze interception before opponent classification;
- composition with `PlatformSpecialNormalExitPipeline` for substate `$0E`;
- ordinary resume preserving `$066F`;
- Hyoga redirect composed through `AquariusStage08Context.EnterRedirectedFirstCamus`;
- ordinary phase-1 post-Bronze outcomes and release `$01`;
- post-Gold `$EA` outcomes and release `$FF`;
- retry reset that rearms phase zero;
- ordinary and first-Camus Cancer boundaries.

`tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/GeminiStage02ContextChecks.cs` discriminates:

- all four reachable entry Saints and Ikki exclusion;
- intro reward/handoff;
- first/repeat Talk;
- phase-zero detour even with hypothetical `$EB=$FF`;
- rejected and accepted `$0E` physical gates;
- Talk-state preservation on ordinary resume;
- Hyoga redirect fields and first-Camus phase identity;
- ordinary hit/miss/low/victory branches;
- healthy/low/defeat Gold responses;
- `$FF` retry rearming the detour;
- both first-Camus `$FE` routes to Cancer;
- generic parity Gold slots `0,1`.

## Closure

Canonical stage `$02` is now closed as a **composite context**, not as an isolated Gemini handler set:

```text
progress 02 / stage 02
 -> init +300 / release 03
 -> command loop
 -> first Bronze while phase0
 -> platform 0E / release 02
 -> accepted exit / phase1
    -> Seiya/Shun/Shiryu: ordinary stage 02
       -> victory release 01 -> Cancer
       -> defeat release FF -> retry resets phase0 -> detour repeats
    -> Hyoga: stage 08 / first Camus
       -> scripted FE terminal -> force Seiya -> Cancer
```

Next uncovered canonical stage context is `$03` Cancer / Death Mask.

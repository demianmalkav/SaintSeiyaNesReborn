# Boss context — stage `$03` Cancer / Death Mask

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Canonical ROM identity used for this checkpoint:

- size: `262160` bytes;
- SHA-1: `F871D9B3DAFDDCDAD5F2ACD71044292E5169064E`;
- MD5: `3B0F17C2B6EFC928B3D3FE9B1A389680`;
- SHA-256: `6917B31D7343A9A17170E833BACDBC3B1EBA3E02D11C51C0D44DBE436C9AD43A`;
- CRC32: `F8D258A3`.

Status: canonical stage `$050E=$03` is closed as a dedicated executable context in `CancerStage03Context`.

This document owns only Cancer-specific control. Generic resource arithmetic, Bronze/Gold damage, opponent/player classifiers, dodge resolution and the already-closed platform `$0C` mechanics remain in their existing specifications.

## 1. Canonical story seed and roster

Cancer is entered from story progress `$067D=$03`:

```text
$067D = $03
$F016[$03] = $03 -> $050E=$03
$E50B[$03] = $02 -> $06CD=$02
$0673 = $32
```

The fixed Saint selector at this story marker permits exactly:

```text
Seiya  $0533=0  reachable
Hyoga  $0533=1  blocked
Shun   $0533=2  reachable
Shiryu $0533=3  reachable
Ikki   $0533=4  blocked
```

Normal battle reset `$A973+` clears the Cancer control fields that matter here:

```text
$064D = 0    ; player-low first-use latch
$0670 = 0    ; release
$067C = 0    ; Cancer phase
$068E = 0    ; intro latch
```

`$064A` is not treated as persistent Cancer state. It is scratch used by the shared classifier/presentation paths and can be rewritten by generic combat code.

## 2. Initialization `$9851`

Exact bank-5 control skeleton:

```text
$9851  JSR $9C6D
$9854  LDA #$03
$9856  STA $0514
$9859  LDA #$0E
$985B  JSR $F2ED          ; temporary presentation stage $0E
...
$9888  LDA #$5B
$988A  JSR $E7B7
...
$9890  LDA #$91
$9892  JSR $E7C7
$9895  LDA #$04
$9897  JSR $F31E          ; +400 Seventh Sense
$989A  JMP $9C3D
```

Shared `$9C3D` owns the intro handoff:

```text
$0670 = $03
$068E = 1
$050E = $067E             ; restores real stage $03
```

The executable model therefore exposes:

```text
temporary presentation = $0E
Seventh Sense reward    = +400
intro release           = $03
intro latch $068E       = 1
```

The fixed reload path consumes the internal `$03` before ordinary command selection.

## 3. Talk `$9D96`: two phases

Cancer Talk is controlled entirely by `$067C`.

### Phase zero — create platform `$0C`

Exact branch:

```text
$9D96  LDA $067C
$9D99  BNE $9DCB
$9D9B  LDA #$56
$9D9D  JSR $E7B3
...
$9DAD  LDA #$69
$9DAF  JSR $E7B3
...
$9DBA  LDA #$08
$9DBC  JSR $E726
$9DBF  LDA #$0C
$9DC1  STA $02
$9DC3  LDA #$02
$9DC5  STA $0670
$9DC8  PLA
$9DC9  PLA
$9DCA  RTS
```

The two `PLA` instructions deliberately unwind the ordinary Talk caller. This path does **not** increment transient `$DC`; it exits directly through release `$02` to platform substate `$0C`.

Semantic result:

```text
$02   = $0C
$0670 = $02
$067C remains 0 until special resume
caller unwind = yes
forced Gold response = no
```

### Phase one — forced Gold response

After `$067C!=0`:

```text
$9DCB  LDA #$58
$9DCD  JSR $E7C7
$9DD0  LDA #$57
$9DD2  JSR $E7B3
$9DD5  INC $DC
$9DD7  RTS
```

The fixed command owner sees nonzero `$DC`, clears it and routes directly to the Gold-response path. No persistent Cancer-local Talk counter exists.

## 4. Platform `$0C` composition

Cancer reuses the already-closed `PlatformSpecialNormalExitPipeline`; no platform mechanics are duplicated here.

Confirmed substate profile:

```text
substate $02            = $0C
origin                  = stage-3 Talk $9D96/$9DC1
story progress          = $03
reload stage            = $03
inherited release       = $02
accepted X              >= $88
required Y              = $20
required jump phase     = 0
```

On an accepted normal `$3D/$E100` reload, fixed `$ED57/$ED8F` consumes release `$02` and increments:

```text
$067C: 0 -> 1
```

Because release `$02` takes the special-resume path, common reset `$A973` is skipped. Therefore the active Cancer Saint and stage-local `$064D/$068E` values survive this transition.

There is no Cancer character redirect analogous to Gemini/Hyoga first Camus: Seiya, Shun or Shiryu return to stage `$03` with `$067C=1`.

## 5. Important reachability result: the detour is optional

Unlike Gemini stage `$02`, Cancer post-Bronze handler `$A50F` does **not** inspect `$067C`.

Therefore ordinary Attack/Bronze action is reachable while `$067C=0`, before Talk ever creates platform `$0C`. A true opponent defeat at that time reaches victory release `$01` immediately.

Canonical Cancer therefore has at least two success families:

```text
A. phase-zero direct victory
   entry -> ordinary Bronze battle -> $EB=$FF -> release $01 -> Leo

B. Talk/platform route
   entry -> Talk -> platform $0C -> $067C=1 -> ordinary battle -> release $01 -> Leo
```

The platform transition is narrative/phase state, not a prerequisite for defeating Death Mask in the ROM control graph.

## 6. Post-Bronze `$A50F`

Exact stage-local control:

```text
$A50F  JSR $ACD6         ; shared opponent classifier
$A512  LDA $EB
$A514  CMP #$FF
$A516  BNE $A531
...
$A52C  LDA #$01
$A52E  JMP $ACAA         ; victory release $01

$A531  LDA $EB
$A533  CMP #$01
$A535  BNE $A548
$A537  INC $064A
...
$A542  LDA #$8D
$A544  JSR $E7B3
$A547  RTS

$A548  LDA $06BC
$A54B  BNE $A547         ; hit -> continue
$A54D  JSR $DFBA
...
$A55A  LDA #$3D
$A55C  JSR $E7B3         ; miss feedback
$A55F  RTS
```

Semantic branches:

```text
$EB=$FF             -> release $01 victory
$EB=$01             -> INC $064A, low-opponent feedback
$EB=$00 + hit       -> continue
$EB=$00 + no hit    -> miss feedback
```

`$064A` is intentionally modeled as classifier scratch supplied to the stage-local handler. Cancer performs exactly one `INC $064A` on every `$EB=$01` execution; there is no test that turns it into a one-shot Cancer latch.

## 7. Post-Gold `$A560`

Exact stage-local control:

```text
$A560  JSR $DFB1
$A563  JSR $AD4D         ; shared player classifier
$A566  LDA $EA
$A568  CMP #$FF
$A56A  BEQ $A596
$A56C  CMP #$01
$A56E  BNE $A59B
$A570  LDA $064D
$A573  BNE $A59B
...
$A582  LDA #$8B
$A584  STA $066A
...
$A58D  LDA #$91
$A58F  JSR $E7C7
$A592  INC $064D
$A595  RTS

$A596  LDA #$FF
$A598  JMP $ACAA         ; defeat release $FF

$A59B  LDA #$8E
$A59D  JSR $E7C7
...
$A5AD  LDA #$40
$A5AF  JSR $E7B7
$A5B2  RTS
```

Semantic branches:

```text
$EA=$FF                    -> release $FF defeat
$EA=$01 and $064D=0        -> first-low presentation; $064D becomes 1
$EA=$01 and $064D!=0       -> common/repeat feedback
$EA=$00                    -> common/healthy feedback
```

Unlike `$064A`, `$064D` is a true one-time stage-local latch until a normal reset.

## 8. Gold selector

Stage `$03` has no dedicated branch in bank-6 selector `$9074-$9146`. It falls through the generic parity selector:

```text
slot $0680 = $065F & 1
```

Only slots `0/1` are canonically reachable. Coefficients/damage remain owned by the generic Gold battle specification.

## 9. Defeat and retry

Generic defeat uses release `$FF` and does not advance `$067D`.

On subsequent normal stage re-entry, fixed `$ED57` does not take the release-`$02/$03` special-resume branch and therefore calls common reset `$A973`:

```text
$067C = 0
$064D = 0
$0670 = 0
$068E = 0
```

Retry therefore starts again in Cancer phase zero. Talk can create platform `$0C` again.

No claim is made that `$A973` clears `$064A`; it does not. `$064A` is excluded from persistent Cancer state because shared classifier/combat code owns its scratch lifecycle.

## 10. Victory and exact Leo boundary

Cancer victory release `$01` joins the already-established fixed owner `$E399/$E3B3`:

```text
$067D: $03 -> $04
$F016[$04] = $04
$E50B[$04] = $02
$050E = $04
$06CD = $02
$0673 = $32
```

Cancer's reachable roster excludes Ikki, so the release owner preserves the active Seiya/Shun/Shiryu.

Both phase-zero direct victory and post-platform phase-one victory terminate at exactly the same boundary:

```text
story progress  $067D = $04
stage           $050E = $04
story descriptor$06CD = $02
story marker    $0673 = $32
active Saint    preserved (0,2,3)
```

Leo `$04` internals remain owned by the already-closed `LeoStage04Context` / PR #125 and are not reopened here.

## 11. Executable artifact and discriminating fixtures

`CancerStage03Context` encodes:

- exact canonical roster/seed;
- +400 intro handoff;
- both Talk phases;
- platform `$0C` composition through the existing pipeline;
- optional rather than mandatory detour semantics;
- post-Bronze branches including repeatable `$064A++`;
- post-Gold first-low `$064D` latch and defeat `$FF`;
- parity Gold slots `0/1`;
- retry phase reset;
- exact Leo boundary.

`CancerStage03ContextChecks` discriminates the important competing interpretations:

- Hyoga/Ikki are rejected while Seiya/Shun/Shiryu are accepted;
- platform gate rejects wrong X/Y/jump and accepts the exact boundary;
- release `$02` preserves `$064D/$068E` because `$A973` is skipped;
- phase-one Talk forces Gold while phase-zero Talk does not;
- phase-zero direct victory is legal;
- `$064A` low branch can increment repeatedly;
- `$064D` first-low presentation is one-shot;
- defeat resets Cancer to phase zero;
- both victory families converge on the frozen Leo boundary.

## 12. Closure

Stage `$03` is complete when the executable fixtures pass on the exact technical head and the global coverage matrix promotes `$03` to `DedicatedContextClosed`.

After promotion, the remaining material battle-stage gaps are only:

```text
$06 Scorpio / Milo
$07 Capricorn / Shura
```

Stage `$0B` remains structural/transient and final-special `$0C` remains separately closed.

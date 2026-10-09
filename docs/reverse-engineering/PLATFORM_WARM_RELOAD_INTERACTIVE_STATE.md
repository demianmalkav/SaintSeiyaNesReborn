# Platform warm reload interactive state

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: **CONFIRMED statically** for the normal warm-reload interactive boundary around fixed-bank `$E327-$E35D`, dispatcher `$F025`, NMI-side selector `$A20B`, main-side confirm handler `$A275`, Talk dispatcher `$9C91`, and the terminal `$0670` writes reachable from this subgraph.

This document intentionally stops at already isolated battle/story dispatch boundaries. It does not emulate PPU drawing, audio, raw controller polling, or the full Bronze/Gold combat round.

## Entry from normal warm reload

After the generic normal reload has reached `$E327`, the fixed bank initializes the selector fields and then enters a cooperative main/NMI loop:

```text
$E327  LDA #$00
$E329  STA $0670
$E32C  STA $0584
$E32F  STA $0585
$E332  STA $0586
...
$E345  LDA #$05
$E347  STA $0584

$E34C  LDA #$05
$E34E  JSR $E589    ; select PRG bank 5
$E351  JSR $A275    ; confirm -> pending case
$E354  JSR $F025    ; dispatch pending case
$E357  JSR $FF11
$E35A  LDA $0670
$E35D  BEQ $E34C
```

Earlier in the same `$E100` pass, `$E117-$E119` has written `$DB=$FF`.

Therefore the exact first selector state at `$E347` is:

```text
$0584 = $05
$0585 = $00
$0586 = $00
$DB   = $FF
$0670 = $00
```

## `$F025`: six dispatcher cases

`$F025` reads `$0584`, clears it, and dispatches through `$E698`:

| `$0584` | Handler | State-visible role |
|---:|---:|---|
| `0` | `$F03C` | idle/unlock; writes `$DB=0` |
| `1` | `$F057` | command 1; writes `$DB=2` |
| `2` | `$F0B1` | Talk command; writes `$DB=3`, invokes `$9C91` |
| `3` | `$F0D3` | command 3; writes `$DB=4` |
| `4` | `$F041` | command 4; writes `$DB=1` |
| `5` | `$F1D9` | initial/interstitial setup; resets `$0585/$0586` |

`$F025` itself performs:

```text
LDA $0584
LDX #$00
STX $0584
JSR $E698
```

so every selected body begins with pending case `$0584=0`.

Case 5 does **not** write `$DB`. On the initial reload pass it therefore preserves `$DB=$FF`. The following loop iteration, absent confirmation, dispatches case 0, which writes `$DB=0`. That unlocks NMI-side directional input.

## `$A20B`: NMI-side 2×2 selector

The NMI path switches to bank 5 and calls `$A20B`. Its first gate is:

```text
LDA $DB
BEQ active_input
RTS
```

Thus direction changes are accepted only while `$DB=0`.

The relevant standard NES controller masks at `$FFC4-$FFC7` are:

| Direction | Mask | Effect |
|---|---:|---|
| Up | `$10` | `$0586=0` |
| Down | `$20` | `$0586=1` |
| Left | `$40` | `$0585=0` |
| Right | `$80` | `$0585=2` |

So the selector is a compact 2×2 coordinate system:

```text
horizontal $0585 ∈ {0,2}
vertical   $0586 ∈ {0,1}
```

## `$A275`: confirmation to command case

Main-side `$A275` checks the confirm input and, when accepted, computes:

```text
$0584 = $0585 + $0586 + 1
```

This proves that cases 1–4 are the four real choices:

| Coordinates | Resulting case |
|---|---:|
| left + up (`0+0`) | `1` |
| left + down (`0+1`) | `2` |
| right + up (`2+0`) | `3` |
| right + down (`2+1`) | `4` |

Case 5 is initialization, not a fifth user choice. Case 0 is the idle/unlocked state.

## Command latch `$DB`

Once `$F025` dispatches a real choice, the selected handler makes `$DB` nonzero:

```text
case 1 -> $DB=2
case 2 -> $DB=3
case 3 -> $DB=4
case 4 -> $DB=1
```

That immediately freezes `$A20B` directional input. The selected body can then repeat over subsequent `$E34C-$E35D` iterations until its own stage/event logic produces progression.

## Case 1 — `$F057`

Case 1 is mostly a gateway into the normal Bronze action round at `$F813`, but two stage-dependent paths can release the warm-reload loop directly.

| Condition | Result |
|---|---|
| `$050E=$0C` | fixed `$F0A1` writes `$0670=1` |
| `$050E=3` and `$067C=0` | stage-3 interaction path reaches `$9D96...$9DC5`, writes `$0670=2` |
| other ordinary cases | enters `$F813` or nonterminal common work |

The stage-3 direct path is the same bank-5 stage handler also reachable from Talk; it unwinds without returning to the normal case body after setting the transition.

## Case 2 — Talk / `$9C91`

Case 2 writes `$DB=3`, performs its setup and calls bank-5 `$9C91`.

`$9C91` dispatches by `$050E` through the confirmed Talk table:

| `$050E` | Handler |
|---:|---:|
| `0` | `$9CB7` |
| `1` | `$9D2C` |
| `2` | `$9D81` |
| `3` | `$9D96` |
| `4` | `$9DD8` |
| `5` | `$9E1B` |
| `6` | `$9E51` |
| `7` | `$9ED6` |
| `8` | `$9F00` |
| `9` | `$9F99` |
| `A` | `$9FF4` |
| `B` | `$9FF4` |
| `C` | `$A1AD` |
| `D` | `$A1C5` |

Direct loop-release effects inside this dispatcher are:

| Condition | Writer | `$0670` |
|---|---:|---:|
| stage `0`, later Talk phase (`$066F!=0`) | `$9D20` | `1` |
| stage `3`, `$067C=0` | `$9DC5` | `2` |
| stage `$0D` | `$A1CC` | `4` |

For stage 0, the first Talk phase raises its progression counter instead of releasing immediately; a later phase reaches the `$0670=1` writer.

## Case 3 — `$F0D3`

Case 3 writes `$DB=4` and contains several stage-specific detours.

The state-machine-relevant paths are:

| Condition | Result |
|---|---|
| `$050E=$0D` | jumps to `$F1D9`; resets `$0585/$0586`, preserves `$DB=4` |
| `$050E=7` | advances local phase and enters Bronze action round `$F813` |
| `$050E=8`, `$06B8=0` | enters `$F813` |
| `$050E=8`, `$06B8!=0` | remains nonterminal/common path |
| other ordinary stages | presentation/story-specific nonterminal path unless descendants later release |

The `$0D -> $F1D9` path is important: it reuses the initialization body but is **not** equivalent to resetting the complete selector state because `$DB` remains 4.

## Case 4 — `$F041`

Case 4 writes `$DB=1`. Within the bounded selector layer it is nonterminal; stage/event descendants may continue into already-isolated battle/story machinery.

## Bronze/Gold action boundary — `$F813`

Several command paths enter `$F813`, which is a larger battle round rather than part of the selector itself.

The relevant architecture is already isolated elsewhere:

```text
Bronze action/technique flow
 -> bank-5 $A361 post-Bronze stage dispatcher
 -> Gold response flow
 -> bank-5 $A381 post-Gold stage dispatcher
```

Those stage handlers can converge on bank-5 `$ACAA`, whose semantic role for this boundary is simply:

```text
STA $0670
```

The values proved reachable at that sink from the interactive action path are:

```text
$01, $02, $DD, $FE, $FF
```

This document does not duplicate the internal combat state machines. It treats `$F813` as a delegated semantic boundary whose relevant output is whether a stage/event handler returns with a terminal `$0670` value.

## Exact warm-loop release set

A global writer audit of `$0670`, intersected with callgraph reachability from `$F025/$A275`, gives the complete set of values that can make `$E35A` leave its `$0670==0` loop:

```text
{ $01, $02, $04, $DD, $FE, $FF }
```

Sources:

- `$01`: `$F0A1`, `$9D20`, or downstream `$ACAA`;
- `$02`: `$9DC5` or downstream `$ACAA`;
- `$04`: Talk stage `$0D` at `$A1CC`;
- `$DD`: downstream stage script through `$ACAA`;
- `$FE`: downstream stage script through `$ACAA`;
- `$FF`: downstream stage script through `$ACAA`.

`$0670=$03` is **not** part of this subgraph. Its writer at bank-5 `$9C58` belongs to a different initialization/event family and is not reachable from the `$F025/$A275` interactive selector boundary.

## `$067D -> $050E` principal progression mapping

The fixed table at `$F016` begins:

```text
$067D: 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D
$050E: 00 01 02 03 04 05 0F 06 10 07 08 09 0C 0A
```

Therefore `$050E` must not be treated as numerically identical to the progression index.

The `$050E=$0F` and `$050E=$10` cases are intercepted earlier in `$E100` and assign progression before this interactive loop. They are consequently outside the ordinary principal selector subgraph being modeled here.

## Semantic implementation

Executable reduction:

- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformWarmReloadInteractiveState.cs`

Discriminating fixtures:

- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/WarmReloadInteractiveStateChecks.cs`

The model preserves:

- exact initial selector state;
- NMI directional gate and 2×2 coordinate encoding;
- confirm-to-case arithmetic;
- `$F025` pending-case clear;
- exact `$DB` command latches;
- direct Talk/case terminal writes;
- delegated `$F813` battle boundary;
- exact reachable `$0670` release set.

It deliberately excludes:

- tile/PPU drawing;
- sound effects;
- raw controller-register polling;
- full battle calculations already modeled elsewhere;
- unrelated reload modes.

## Consequence for the next boundary

The interactive portion of the normal warm reload is no longer an unknown selector. Once `$E35A` sees one of the six proved nonzero release values, control leaves the loop and resumes the fixed-bank reload logic after `$E35D`.

The next reverse-engineering boundary is therefore **not** another menu pass. It is the post-loop `$E35F+` handling that maps the terminal `$0670` outcome, stage/profile fields and active-Saint mapping into the final normal reload destination committed at `$E22C-$E254`.

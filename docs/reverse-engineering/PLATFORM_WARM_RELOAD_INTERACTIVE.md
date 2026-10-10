# Platform warm-reload interactive control

Status: `CONFIRMED` for the controller/menu control layer; downstream stage-body terminal conditions remain the next boundary.

Scope: normal warm reload after a platform exit, principal `$050E=$00-$0B`, specifically fixed-bank `$E327-$E35D`, `$F025`, bank-5 `$A20B/$A275`, and the statically reachable terminal source sites in `$9C91`, `$A361` and `$A381`.

This document does **not** model PPU drawing, dialogue rendering, audio waits or the internal semantic conditions of every stage script.

## Entry

The normal reload reaches `$E327` with the warm state reset. The relevant initialization is:

```text
$E327  LDA #$00
$E329  STA $0670
$E32C  STA $0584
$E32F  STA $0585
$E332  STA $0586
...
$E345  LDA #$05
$E347  STA $0584
```

The loop is:

```text
$E34C  bank 5
$E351  JSR $A275
$E354  JSR $F025
$E357  JSR $FF11
$E35A  LDA $0670
$E35D  BEQ $E34C
```

Canonical ROM byte `$FFDE=$00`, so `$FF11` cannot release this loop in the canonical revision. A nonzero `$0670` must come from an action body.

## `$F025` is a six-way inline dispatcher

`$F025` reads `$0584`, immediately clears it to zero, then uses `$E698` to dispatch through the inline pointer table:

| `$0584` | target | semantic role |
|---:|---:|---|
| `0` | `$F03C` | idle / enable menu-direction sampling through `$DB=0` |
| `1` | `$F057` | upper-left action |
| `2` | `$F0B1` | lower-left / `$9C91` stage action |
| `3` | `$F0D3` | upper-right stage-specific action |
| `4` | `$F041` | lower-right passive/presentation action |
| `5` | `$F1D9` | initialization/interstitial setup |

Because `$F025` clears `$0584` before executing a case, the following iteration naturally returns to case `0` unless `$A275` writes a new selected case.

## Directional selector state: `$A20B`

Bank-5 `$A20B` is called from the periodic fixed-bank path at `$E0CF`. It updates the menu selectors only while `$DB==0`, which is exactly what case `0` establishes.

The controller masks live in fixed-bank `$FFC0-$FFC7`:

```text
$FFC0 = $01  A
$FFC4 = $10  Up
$FFC5 = $20  Down
$FFC6 = $40  Left
$FFC7 = $80  Right
```

`$A20B` checks directions in strict priority order:

```text
Right -> $0585=$02
Left  -> $0585=$00
Down  -> $0586=$01
Up    -> $0586=$00
```

A horizontal match branches directly to the cursor-update tail, so simultaneous horizontal+vertical input changes only the horizontal selector on that invocation. This priority is preserved in the semantic model.

Therefore the only selector states produced by this routine are:

```text
$0585 in {$00,$02}
$0586 in {$00,$01}
```

## Confirmation: `$A275`

`$A275` samples input and returns without changing `$0584` unless the A-button mask `$FFC0=$01` is active.

On confirmation:

```text
$A28B  LDA $0585
$A28E  CLC
$A28F  ADC $0586
$A292  STA $0584
$A295  INC $0584
```

So:

| `$0585` | `$0586` | confirmed case |
|---:|---:|---:|
| `0` | `0` | `1` |
| `0` | `1` | `2` |
| `2` | `0` | `3` |
| `2` | `1` | `4` |

This is a 2×2 menu grid. No fifth interactive case exists; case `5` is seeded only by reload initialization.

## Principal-stage routing

### Case 1 — `$F057`

For principal stages:

- `$050E=0`: routes to `$F238` interstitial/presentation work.
- `$050E=3` and `$067C=0`: calls `$9C91`; its stage-3 body reaches `$9DC5`, storing `$0670=$02` and stack-unwinding out of the action.
- all other principal cases: reach common `$F813`.

`$F813` selects the stage dispatcher as follows:

```text
$050E=2 and $067C=0 -> bank 5 $A361
otherwise            -> $F936 -> bank 5 $A381
```

### Case 2 — `$F0B1`

Always calls `$9C91` first.

After `$9C91`:

- if `$0670!=0`, the action is terminal;
- otherwise, if `$DC!=0`, it falls through `$F0A5 -> $F813`, entering the common `$A361/$A381` action layer;
- otherwise it returns to the menu loop after presentation cleanup.

The exact per-stage conditions that make `$9C91` raise `$DC` are intentionally the next boundary.

Direct terminal writers inside `$9C91` for principal stages are:

```text
stage 0: $9D20 -> $0670=$01
stage 3: $9DC5 -> $0670=$02
```

Stage 3 with `$067C=0` deterministically takes the second writer.

### Case 3 — `$F0D3`

Principal-stage routing is now bounded:

```text
stage 0          -> $F238 interlude
stage 2,3,5,9    -> $F261 presentation branch
stage 7          -> raises $DC -> $F813 common action
stage 8, $06B8=0 -> raises $DC -> $F813 common action
stage 8, $06B8!=0-> passive cleanup $F1C6
stage 10         -> dedicated $06CE/$06D0 progression branch
stage 1,4,6,11   -> shared flag/progression branch beginning $F180
```

No direct `$0670` write exists in the case-3 body itself for principal stages. Terminal changes can only occur after the branches that reach `$F813`.

### Case 4 — `$F041`

- stage `0`: `$F238` interstitial.
- principal stages `1-$0B`: presentation/wait via `$FB89` and cleanup; no direct `$0670` write.

### Case 5 — `$F1D9`

Initialization only. It prepares the interstitial/menu state and eventually clears `$0585/$0586` at `$F21D/$F220`. It is not reachable through `$A275` confirmation.

## Static terminal-source audit of common dispatchers

The following source sites load a terminal code immediately before jumping to the common physical writer `$ACAA`, which performs `STA $0670`.

### `$A361` stage dispatcher

| stage `$050E` | source site(s) | possible terminal code(s) |
|---:|---|---|
| `0` | none | — |
| `1` | `$A3DE` | `$01` |
| `2` | `$A46C`, `$A4A1` | `$02`, `$01` |
| `3` | `$A52C` | `$01` |
| `4` | `$A61E`, `$A633` | `$01` |
| `5` | `$A685`, `$A6EB`, `$A781` | `$01`, `$02`, `$FE` |
| `6` | `$A837` | `$01` |
| `7` | `$A8D3` | `$FE` |
| `8` | `$A957`, `$A9CE` | `$FE` |
| `9` | `$AAE7` | `$FE` |
| `10` | `$AC00` | `$01` |
| `11` | none | — |

### `$A381` stage dispatcher

| stage `$050E` | source site(s) | possible terminal code(s) |
|---:|---|---|
| `0` | none | — |
| `1` | `$A43B` | `$FF` |
| `2` | `$A4F4` | `$FF` |
| `3` | `$A596` | `$FF` |
| `4` | `$A65C` | `$FF` |
| `5` | `$A7E0`, `$A7FA` | `$FF` |
| `6` | `$A866` | `$FF` |
| `7` | `$A8F7` | `$FF` |
| `8` | `$AA01`, `$AA51` | `$FF`, `$FE` |
| `9` | `$AB13` | `$FF` |
| `10` | `$AC38`, `$AC71` | `$FF` |
| `11` | none | — |

These tables are a static reachability inventory, not yet a proof that every listed source is reachable from every warm-reload entry state. The physical common writer is `$ACAA`.

## What is closed by this checkpoint

The interactive control structure is no longer ambiguous:

```text
reload seeds case 5
 -> initialization
 -> case 0 idle
 -> $A20B moves a 2x2 selector while $DB=0
 -> A-button at $A275 converts selectors to case 1-4
 -> $F025 dispatches the selected action and clears next case back to 0
 -> action either returns to idle or writes a nonzero $0670
 -> $E35A leaves the loop only on nonzero $0670
```

All six `$F025` cases, the selector arithmetic, input priority, principal-stage top-level routing, common `$F813` dispatcher choice, direct `$9C91` terminal writers and static `$A361/$A381` terminal source sites are now represented in code/tests.

## Remaining boundary

What remains unresolved is narrower than the original problem:

1. for each principal stage, determine the internal flag/counter conditions that make `$9C91` raise `$DC` and therefore invoke the second-leg `$F813` action;
2. prove which listed `$A361/$A381` terminal source sites are actually reachable from the warm-reload entry state and under which persistent flags;
3. once those terminal outcomes are exact, resume `$E35A+` and map each nonzero `$0670` code to the final normal reload engine-state commit.

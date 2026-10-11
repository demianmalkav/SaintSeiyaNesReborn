# Global NMI presentation coverage — `$D269+`

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: **CONFIRMED / ZERO MATERIAL PRESENTATION GAPS** across the complete canonically produced global `$00/$01` namespace.

This checkpoint is a coverage audit, not another renderer implementation. It composes the already-closed 59-value global-state reachability census with the fixed-bank NMI dispatcher rooted at `$D269`, then assigns every canonically reachable NMI presentation route to an existing semantic owner.

Result:

```text
canonical produced global states : 59
structural/no-producer values    : 197
produced NMI states unclassified : 0
material NMI presentation gaps   : 0
```

No new renderer family is required before ORIGINAL SPEC can leave presentation work and proceed to the next global subsystem.

## 1. Universal prologue

Every NMI enters at `$D269` and first performs the already-proven common work:

```text
save A/X/Y
$3A=1
reset MMC1 serial latch
read $2002
$2003=0
$4014=$07            ; DMA from $0700-$07FF
```

Thus every branch in this audit inherits the same pre-dispatch OAM snapshot boundary.

## 2. Mirror-first priority is part of coverage

NMI does not immediately dispatch on live `$00`.

```text
$D282 LDA $01
$D284 CMP #$50 -> $DABC
...
$D290 CMP #$3D -> $E000
...
$D297 LDA $00
```

Therefore two latched/mirror routes preempt every live-state case:

| mirror `$01` | target | owner |
|---:|---:|---|
| `$50` | `$DABC` | `ENGINE_STATE_50_FRONTEND.md` / `FrontEndState50Machine` |
| `$3D` | `$E000` | reload bridge / `PLATFORM_POST_EXIT_STATE_MACHINE.md` and promoted reload machinery |

Canonical producers of `$50` and `$3D` write the mirror as well, so these are the canonical presentation owners for those global states.

## 3. Oracle correction discovered by this audit

The older `EngineStateDispatcherMap` grouped live `$00=$00` with the ordinary common NMI tail at `$D367`. Direct canonical ROM disassembly contradicts that exact target:

```text
$D297 LDA $00
$D299 BNE $D29E
$D29B JMP $D382
```

State `$00` therefore enters at `$D382`, skipping `$D367-$D381`.

The skipped segment is the ordinary final PPUCTRL/PPUMASK commit:

```text
$D367-$D37A  derive/write $77 -> $2000
$D37D-$D37F  $78 -> $2001
```

State `$00` begins instead at:

```text
$D382 LDX $44
$D384 STX $2005
$D387 LDX $46
$D389 STX $2005
...
$D393 LDA $3B
$D395 JSR $C0B4
...
RTI
```

So `$00` still performs scroll/status wait, persistent PRG restoration and interrupt return, but does not claim the ordinary `$D367` entry. This is a precision correction to an existing Oracle, not a newly discovered gameplay mode or renderer gap.

The correction was committed separately before the coverage implementation.

## 4. Exhaustive canonical route matrix

The global reachability census proves exactly 59 produced byte values. The table below classifies all of them by top-level NMI presentation owner.

| canonical state(s) | NMI route/body | semantic owner | coverage |
|---|---|---|---|
| `$00` | `$D382` tail entry | bootstrap/reload + common NMI infrastructure | CLOSED |
| `$10`, `$30`, `$90` | ordinary `$D367` tail | bootstrap/handoff transients | CLOSED |
| `$3D` | mirror `$01=$3D -> $E000` | cooperative reload | CLOSED |
| `$11`, `$14` | `$D367` tail | low/password family | CLOSED |
| `$12` | `$D543`, then `$00/$01->$13` | `ENGINE_STATE_FAMILY_11_14.md` | CLOSED |
| `$13` | `$D42D` | generated `$0600` text stream | CLOSED |
| `$20` | `$D7F2`, `$D988`, then common commit | platform state-20 presentation chain | CLOSED |
| `$31-$33`, `$35-$38` | `$D367` tail | front-end attract/presentation | CLOSED |
| `$34` | `$D73B` | front-end attract transitional setup | CLOSED |
| `$40-$4D` | bank1 `$8C19` | front-end attract text/presentation chain | CLOSED |
| `$50` | mirror `$01=$50 -> $DABC` | front-end/title modal shell | CLOSED |
| `$60` | bank1 `$9D69` | fatal-resource HUD/status presentation | CLOSED |
| `$70` | `$D3BF` (and terminal `$D73B`) | special post-exit narrative | CLOSED |
| `$71-$72`, `$74-$75` | `$D367` tail | main-owned post-exit states | CLOSED |
| `$73` | bank1 `$8C19` | special post-exit text gate | CLOSED |
| `$80-$89` | bank1 `$8C19` | narrative text family | CLOSED |
| `$91` | `$D42D` | generated `$0600` high-family text | CLOSED |
| `$92`, `$97`, `$99` | `$D367` tail | main-owned / absorbing high-family states | CLOSED |
| `$93` | `$D55E` | high presentation transition | CLOSED |
| `$94-$96` | `$D571 -> $D55E` | high presentation transition | CLOSED |
| `$98` | `$D53D`, `$D511`, then increment | high presentation transition | CLOSED |

No produced state remains in an `UNKNOWN` or `UNCLASSIFIED` category.

## 5. Shared bank-1 `$8C19` is not an unresolved renderer family

`$8C19` is deliberately shared by three already-closed contexts:

```text
$40-$4D   front-end attract/presentation
$73       special post-exit text gate
$80-$89   narrative text chain
```

Its semantic contract is already bounded in the family documents:

- local `$57/$26` pacing before script consumption;
- state-specific pointer selection;
- `$FF` termination through `$8DDB`;
- paired `$00/$01` state advancement where applicable;
- final family-specific handoffs (`$4D->$50`, `$89->$3D`).

The project intentionally does not commit original text/tile payloads. Their absence is not a behavioral gap: payload extraction/localization is a separate private/runtime content pipeline, while the state and presentation ownership required by ORIGINAL SPEC is already deterministic.

## 6. Mapper ownership across NMI routes

The audit also freezes temporary PRG ownership so the route matrix cannot hide bank-state assumptions.

### Dispatcher bank1 -> bank3

For `$40-$4D`, `$73`, and `$80-$89`:

```text
A=1 -> raw $C0B4
JSR $8C19
A=3 -> raw $C0B4
```

The dispatcher explicitly returns to bank 3 before entering the common tail.

### Dispatcher bank1 until common persistent restore

For `$60`:

```text
A=1 -> raw $C0B4
JSR $9D69
```

There is no immediate raw bank-3 write. Control falls through the remaining dispatcher tests and ultimately reaches common `$D393-$D395`, which restores persistent bank `$3B` through `$C0B4`.

### Handler-managed mapper work

`$D42D`, state `$20`, front-end `$DABC`, and reload `$E000` own their internal bank transactions. Their contracts are already closed by their respective subsystem specifications and are not duplicated here.

## 7. Structural NMI routes that do not create work

The dispatcher contains routes for arbitrary byte values that canonical execution never produces. Examples include:

```text
$61-$6F -> structural $6x / $9D69 route
$8A-$8F -> structural $8x / $8C19 route
```

`ENGINE_STATE_REACHABILITY.md` proves these values have no canonical producer. The executable coverage manifest therefore classifies all 197 such values as:

```text
STRUCTURAL_UNREACHABLE
```

not as renderer gaps.

This is critical to avoid creating fictional work merely because a comparison tree accepts a numeric range.

## 8. Clean-room executable manifest

`NmiPresentationCoverage` composes rather than duplicates closed models.

It exposes:

- canonical classification for all 256 live state bytes;
- exact 59 produced-state coverage;
- mirror-first `$50/$3D` priority;
- dispatcher route and target address;
- semantic body addresses;
- owning family/specification;
- temporary PRG-bank contract;
- structural-unreachable distinction;
- explicit material-gap count.

`NmiPresentationCoverageChecks` asserts:

```text
59 produced states classified
197 structural/unreachable states excluded from gap count
16 distinct canonical NMI route classes
0 unclassified produced states
0 material presentation gaps
```

It also locks the `$00->$D382` Oracle correction, mirror priority, shared `$8C19` ownership and `$60` bank-restoration asymmetry.

## 9. Closed presentation boundary

With this audit, the ORIGINAL SPEC presentation surface is globally closed at the semantic level required by the project:

```text
canonical main/state producers
 -> OAM/shadow state
 -> universal NMI DMA
 -> mirror-first / live-state presentation dispatch
 -> family-specific closed presentation owner when present
 -> common tail / scroll/status / mapper restoration
 -> RTI
```

This does not claim cycle-accurate PPU emulation or version original copyrighted tile/text payloads. Neither is required to identify or reproduce the game's semantic presentation routing.

## 10. Next bounded subsystem

Because the audit found **zero material renderer/NMI gaps**, no further presentation branch is selected.

The next global ORIGINAL SPEC boundary should move to **RNG**, before full audio, because RNG is smaller, mechanically enumerable and cross-cuts already-promoted platform/battle behavior without requiring the much broader sound-driver reconstruction.

The next checkpoint should inventory every canonical RNG state variable, update routine and reachable callsite, distinguish deterministic counters from true pseudorandom evolution, then promote a clean-room generator/callsite contract with fixtures. Audio remains separate and later.

# Boss context stage `$0A` — Saga final machine

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: **CONFIRMED static reconstruction** of the complete reachable Saga final-boss control graph at story progress `$067D=$0D`, including all reachable `$06CE` phases, re-entry ownership, final support/technique event and victory/defeat boundaries.

Canonical complete-file SHA-256:

`6917B31D7343A9A17170E833BACDBC3B1EBA3E02D11C51C0D44DBE436C9AD43A`

This document composes already-closed generic battle primitives. It does not duplicate Bronze/Gold numeric damage, player-hit probability, dodge arithmetic, resource storage or generic condition classifiers.

## Canonical predecessor and phase seed

Final-special stage `$0C` proves two exact Saga ingress variants:

```text
$067D=$0D
$050E=$0A
$06CD=$0E
$0673=$3E
active Saint = Seiya ($0533=0) OR Shun ($0533=2)
```

The active Saint distinction is real at the Saga boundary, but both variants share `$06CE=0`.

Why `$06CE` is exactly zero:

- bank 0 `$AD4A-$AD54` clears the entire `$0600-$06FF` page during global initialization (`STA $0600,X` for all X);
- the only direct `$06CE` writers outside Saga handlers are not on the canonical pre-Saga story route;
- common battle reset `$ED57->$A973` deliberately does **not** clear `$06CE`;
- therefore the untouched phase byte reaches Saga as the globally initialized zero.

A second distinction matters: initial story entry does **not** call Saga init `$9B5D`. The only fixed caller that invokes stage init through `$970A` in this release/re-entry path is `$F2E4`, reached from `$E3ED` for release `$FF`. Initial `$0C->$0D` story release is `$01`, so first Saga combat begins directly in phase 0 with the inherited Seiya/Shun.

Before that first command cycle, `$ED57->$A973` clears the ordinary battle transients (`$066F`, `$0670`, `$0677/$0678`, `$064D`, etc.) while preserving `$06CE`. Its explicit stage-`$0A`, phase-0 branch then writes:

```text
$0690 = $FF
```

The already-closed player-hit gate proves `$0690!=0` forces `$06BC=0`. Phase 0 therefore begins unable to connect Bronze damage.

## Phase architecture

Saga uses `$06CE` as a three-way dispatcher across every stage-owned family:

| Surface | Dispatcher | phase 0 | phase 1 | phase 2 |
|---|---:|---:|---:|---:|
| init | `$9B5D` | `$9B69` | `$9B9E` | `$9C2C` |
| Talk | `$9FF4` | `$A000` | `$A0E0` | `$A115` |
| post-Bronze | `$AB18` | `$AB24` | `$AB62` | `$AB6F` |
| post-Gold | `$AC05` | `$AC11` | `$AC3E` | `$AC76` |
| Gold selector | bank6 `$90EC+` | `$9104` | `$911D` | `$9135` |

Reachable lifecycle:

```text
story ingress
  phase 0: inherited Seiya or Shun
       |
       | release $FF
       v
  $E3ED->$F2E4->$970A->$9B69
  phase 1: forced Ikki
       |
       | release $FF
       v
  $E3ED->$F2E4->$970A->$9B9E
  phase 2: forced Seiya
       |
       +-- opponent defeat -> release $01 -> post-Saga story boundary
       |
       `-- Seiya defeat -> release $DD -> special defeat/recovery boundary
```

The structural init phase 2 `$9C2C` is unreachable in this canonical graph: stage init is invoked by `$FF` re-entry, while phase 2 has no reachable `$FF` terminal. Its victory is `$01`; its terminal player defeat is `$DD`.

## Phase 0 — inherited Seiya/Shun

### Scripted hit block and first Bronze action

Initial `$0690=$FF`, so generic hit gating forces `$06BC=0`.

Post-Bronze `$AB24` first checks `$06D0`. Canonical phase 0 starts at zero. When `$06D0==0`:

```text
INC $0678
PLA
PLA
PLA
PLA
RTS
```

The four-stack unwind deliberately exits the outer action chain. No Gold response follows that action.

Although `$0678` is used elsewhere by Gold-dodge tracking, Saga phase 0 repurposes the same byte as the event history consumed by `$A1EC` in Talk.

### Talk `$A000`

With `$066F=0`, `$A1EC` returns byte sum `$0677+$0678`.

Before the scripted miss (`sum==0`): dialogue only; no phase state changes.

After at least one miss (`sum>0`):

```text
INC $066F
if $06CF==0:
    ... scripted presentation ...
    INC $06CF
    INC $06D0
```

Canonical first post-miss Talk therefore reaches:

```text
$066F=1
$06CF=1
$06D0=1
```

Subsequent Talk is repeat dialogue only.

Talk never increments transient `$DC`; it does not itself force a Gold response.

### Later Bronze actions

Once `$06D0!=0`, `$AB24` consumes generic player condition `$EA` rather than opponent condition:

```text
$EA=0    -> keep $06D0
$EA=1/FF -> $06D0=$FF
```

The action then returns normally into the Gold-response path.

Because `$0690` remains blocked throughout phase 0, these player attacks still cannot apply ordinary Bronze damage to Saga.

### Gold selector — phase 0

`$9104` uses `$06D0`:

```text
$06D0 != $FF -> slot0 -> 35/23 Cosmo/Life
$06D0 == $FF -> slot1 -> 30/30
```

### Post-Gold — phase 0

`$AC11` consumes player condition `$EA`:

```text
$EA=0    -> continue phase0
$EA=1/FF -> scripted event -> release $FF
```

Low and fully defeated player states deliberately share the same phase-transition release.

## `$FF` owner: phase 0 -> phase 1

Release `$FF` reaches fixed `$E3ED`, which calls `$F2E4->$970A` before outer release handling continues. With `$06CE=0`, Saga init `$9B69` executes:

```text
save outgoing inherited-Saint record
load Ikki record
$0533 = 4
$0673 &= $2F
INC $06CE             ; 0 -> 1
$06CF = 0
$06D0 = 0
$066F = 0
$0690 = $FF
```

The init increments `$06CE` before fixed `$E168`. Because `$06CE!=0`, `$E168` jumps directly into the continuation path and bypasses common reset `$ED57`. Thus these explicit init writes, especially `$0690=$FF`, survive into phase 1.

Both original ingress variants have now converged on active Ikki.

## Phase 1 — Ikki

### Talk `$A0E0`

Phase 1 starts with `$0690=$FF`, so ordinary Bronze connections are still script-blocked.

First Talk (`$066F==0`) displays the phase dialogue and calls `$A1F4`. That already-closed helper clears:

```text
$0690 = 0
```

Then `$066F` increments. Repeat Talk is dialogue-only.

This makes Ikki's first Talk mechanically important: it is the only reachable Saga phase-1 writer that removes the story-level player-hit block.

If the player never uses that Talk, phase 1 can still end by taking Gold damage, but `$0690=$FF` is carried through the next init and remains set in phase 2. The final Seiya phase can then no longer connect normal Bronze attacks. This reachable trap/softlock-like route is preserved by the executable specification rather than silently repaired.

### Post-Bronze `$AB62`

The handler consumes only player condition `$EA`:

```text
$EA=0    -> continue
$EA=1/FF -> $06D0=$FF
```

Opponent condition is not used here. Saga can therefore have its resources reduced by Ikki without a phase-1 victory terminal; the phase ends through player state and `$FF`, not through `$EB`.

### `$0649` — current Bronze technique slot

Fixed attack path `$F8B3-$F8B8` stores:

```text
$0649 = $E3 + $E4
```

This is the selected Bronze technique slot consumed by phase-1 Gold selection.

### Gold selector — phase 1

`$911D` resolves in this order:

```text
$06D0 == $FF -> slot3 -> 60/60
else $0649 == 0 -> slot2 -> 60/60
else              slot0 -> 35/23
```

Structural slot1 is unreachable in phase 1.

### Post-Gold — phase 1

`$AC3E` again consumes player condition:

```text
$EA=0    -> continue phase1
$EA=1/FF -> scripted transition -> release $FF
```

## `$FF` owner: phase 1 -> phase 2

The same fixed `$E3ED->$F2E4->$970A` re-entry now dispatches `$9B9E` because `$06CE=1`.

Important control effects:

- saves/loads the relevant records and forces active Seiya (`$0533=0`);
- reloads Seiya's active technique count from `$0587`;
- runs a nested `250 * 4` loop calling fixed `$FDE0` exactly 1000 times;
- `$FDE0` is already confirmed as +1 Seventh Sense, so this sequence grants nominal **+1000 Seventh Sense**, subject to the global cap;
- `$0673 &= $3E`;
- `$06CE: 1 -> 2`;
- clears `$06CF/$06D0/$066F/$064D/$0681`.

Crucially, `$9B9E` does **not** write `$0690`. The common `$ED57` reset is again bypassed because `$06CE` was incremented before `$E168`. Therefore the phase-1 hit-block value is preserved:

```text
Ikki Talk used    -> $0690=0  -> Seiya final phase hittable
Ikki Talk skipped -> $0690=FF -> Seiya final phase remains script-blocked
```

## Phase 2 — final Seiya

Canonical active Saint is now Seiya regardless of original stage-`$0C` ingress identity.

### Talk `$A115`

First Talk (`$066F==0`) runs the long final scripted presentation, temporarily invokes stage `$0B` through `$F2ED`, restores the saved `$050E`, then increments `$066F`.

Repeat Talk is dialogue-only. It does not clear `$0690` and does not emit a release.

### Final Escape becomes the support command

Fixed command-3 branch `$F153+` specializes Saga.

Phases 0/1:

```text
message $E1
no release
```

Phase 2 first checks `$06D0`.

If `$06D0!=0`, the command returns/redraws and cannot open the special support overlay.

If `$06D0==0`, it unconditionally does:

```text
INC $06CF
INC $06D0
```

Then it tests `$066F`.

If final Talk has **not** happened (`$066F==0`), the command returns without support selection. Because `$06D0` is now nonzero and there is no reachable phase-2 writer that restores it to zero, using Escape too early permanently consumes the support gate for that run.

If final Talk has happened (`$066F!=0`):

```text
$06D4 = 0
$068F = $55
JSR $F381
```

This opens the one final support-selection overlay.

### `$06D4` support-reward bitset and exact Seiya unlock

Inside `$068F=$55` overlay, fixed `$F477-$F49A` obtains a selected support bit from table `$F786`:

```text
01, 10, 08, 02, 20, 04
```

For a bit not already present in `$06D4`:

```text
$06D4 |= support_bit
LDA #$0A
JSR $F31E          ; +1000 Seventh Sense
LDA #$03
STA $0587
STA $0696
```

Therefore each newly confirmed support bit in that overlay can grant +1000 Seventh Sense; repeat confirmation of the same bit cannot. The first new confirmation is the exact progression event that changes Seiya technique count `2 -> 3`, exposing contiguous slot 2 / attack id 2: **Pegasus Rolling Crash**.

The technique write is idempotent after the first reward: later unique support confirmations keep count at 3.

### Post-Bronze `$AB6F`

Only phase 2 consumes opponent condition `$EB`.

`$EB=0`: continue.

First `$EB=1` while `$064D==0` runs the one-time low-opponent event and increments `$064D`. Further `$EB=1` does not replay it.

`$EB=FF`: final victory sequence, then:

```text
$06CE = 0
release $01
```

For nonterminal paths, a nonzero already-computed `$06BC` can trigger phase-specific hit feedback, but numeric hit/damage remains owned by the generic specifications.

### Gold selector — phase 2

`$9135` always chooses:

```text
slot3 -> 60/60 Cosmo/Life
```

### Post-Gold `$AC76`

```text
$EA=0  -> continue
$EA=1  -> low-player feedback only
$EA=FF -> scripted final defeat
          $06CE=0
          release $DD
```

Unlike phases 0/1, low condition is not terminal in phase 2.

## Reachable Saga Gold slots

| Phase | Condition | Reachable slot | Cosmo/Life |
|---:|---|---:|---:|
| 0 | `$06D0 != FF` | 0 | 35/23 |
| 0 | `$06D0 == FF` | 1 | 30/30 |
| 1 | healthy and `$0649!=0` | 0 | 35/23 |
| 1 | healthy and `$0649==0` | 2 | 60/60 |
| 1 | `$06D0==FF` | 3 | 60/60 |
| 2 | all reachable states | 3 | 60/60 |

Thus all four structural stage-10 slots are reachable somewhere in Saga, but no single phase exposes all four.

## Structural init phase 2 `$9C2C` is unreachable

The handler itself would:

```text
release $DD
$0673=$3F
$06CE=0
```

However init is reached through the `$FF` re-entry owner. Phase 2 never emits `$FF` on any closed terminal path:

- opponent defeat -> `$01`;
- player defeat -> `$DD`;
- low player -> nonterminal;
- Talk/Escape/support -> no `$FF`.

Therefore `$9C2C` is structural code, not a reachable canonical transition.

## Final victory boundary

Phase-2 opponent defeat emits `$01` after resetting `$06CE=0`.

Fixed story owner `$E399/$E3B3+` advances:

```text
$067D: $0D -> $0E
$E50B[$0E] = $00 -> $06CD=$00
$0673 = $30
$F016[$0E] = $00 -> $050E=$00
```

Fixed `$E1CF` has an explicit progress `$0D/$0E` branch. At `$0E`, it rewrites:

```text
$0670 = $05
```

and commits engine bootstrap state `$00` through `$E22C`.

The already-closed global dispatcher proves:

```text
bootstrap $00 -> immediate logical successor $20
```

So the exact terminal boundary of **Saga ownership** is:

```text
active Seiya
$067D=$0E
$050E=$00
$06CD=$00
$0673=$30
$0670=$05
engine bootstrap $00 -> state $20
```

This document does not invent what later state `$20` presentation means; ownership has already transferred out of the Saga boss machine.

## Final defeat boundary

Phase-2 player defeat emits `$DD` after resetting `$06CE=0`.

Fixed `$E417+` writes:

```text
$0673=$3F
$068F=$DD
```

then runs the dedicated `$F381` defeat overlay and eventually commits bootstrap engine state `$90`.

The closed global dispatcher proves:

```text
bootstrap $90 -> state $91
```

Story progress remains `$0D`. This is a defeat/recovery subsystem boundary, not a Saga phase re-entry.

## Executable specification

`src/SaintSeiyaNesReborn.OriginalSpec/SagaStage0AContext.cs` models only Saga-owned composition:

- both exact Seiya/Shun phase-0 ingress variants;
- initial common reset and scripted hit block;
- all three Talk handlers;
- all three post-Bronze handlers;
- all three post-Gold handlers;
- phase-specific Gold slot selection and exact coefficients;
- both `$FF` phase re-entry inits and active-Saint lifecycle;
- +1000 Seventh Sense on Ikki->Seiya transition;
- final Escape/support gate;
- `$06D4/$068F=$55` support reward and Seiya Rolling Crash unlock;
- structural/unreachable init phase 2;
- `$01` victory and `$DD` defeat terminal boundaries.

Discriminating fixtures:

`tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/SagaStage0AContextChecks.cs`

They explicitly distinguish the two ingress Saints, phase-0 unwind, every reachable Gold slot, Ikki Talk used/skipped, support gate correct/missed ordering, repeated/new support bits, first-low Saga latch, final victory and final defeat.

## Closed conclusions

1. Saga story ingress begins in `$06CE=0` with inherited Seiya or Shun; init `$9B5D` does not run on that initial handoff.
2. Initial common reset clears battle transients and arms `$0690=$FF`, making phase 0 intentionally unhittable.
3. The first phase-0 Bronze action increments `$0678` and unwinds; first post-miss Talk advances `$066F/$06CF/$06D0`.
4. Phase-0 Gold slots are 0/1 by `$06D0`; low or dead player after Gold response releases `$FF`.
5. First `$FF` executes init `$9B69`, saves the inherited Saint and forces blocked Ikki phase `$06CE=1`.
6. Ikki's first Talk is the exact `$0690` clear event; skipping it can carry the block into the final Seiya phase.
7. Phase-1 Gold slots are 0/2/3 by `$0649` and `$06D0`; low/dead Gold result releases `$FF`.
8. Second `$FF` executes `$9B9E`, forces Seiya, advances `$06CE=2`, clears phase latches and grants +1000 Seventh Sense.
9. Final Talk must precede the first final-phase Escape to open the one-shot `$068F=$55` support overlay; reversing the order consumes the gate without support.
10. New support bits in `$06D4` each grant +1000 and set Seiya technique count to 3; the first is the exact Rolling Crash unlock.
11. Final phase always uses Gold slot3; first `$EB=1` latches `$064D`, `$EB=FF` wins with `$01`, `$EA=1` is nonterminal and `$EA=FF` loses with `$DD`.
12. Structural init phase 2 `$9C2C` is unreachable because final phase never emits `$FF`.
13. Saga victory exits at progress `$0E`, release `$05`, stage `$00`, engine bootstrap `$00->$20`; final defeat exits through `$DD` overlay and bootstrap `$90->$91`.

Do not reopen Saga stage `$0A` without contradictory ROM evidence or a failing fixture.

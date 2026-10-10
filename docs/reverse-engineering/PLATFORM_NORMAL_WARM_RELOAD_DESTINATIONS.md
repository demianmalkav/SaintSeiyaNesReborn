# Normal warm-reload post-loop destinations

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: **CONFIRMED statically** for the principal normal warm-reload path after the interactive `$E35A/$E35D` loop and through common commit `$E22C-$E254`.

This document consumes the closed selector result from `PLATFORM_WARM_RELOAD_INTERACTIVE_STATE.md`. It does not reopen controller/menu semantics or reproduce battle internals.

## Scope

The closed interactive selector can emit:

```text
$0670 ∈ { $01, $02, $04, $DD, $FE, $FF }
```

For **principal progression** the `$04` case is not reachable. Its interactive writer is the Talk handler for `$050E=$0D`, while the principal progression map at `$F016` is:

```text
$067D: 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E
$050E: 00 01 02 03 04 05 0F 06 10 07 08 09 0C 0A 00
```

No principal entry maps to `$0D`. Therefore this bounded post-loop model accepts:

```text
{ $01, $02, $DD, $FE, $FF }
```

## Fixed-bank post-loop dispatcher

When `$E35A` observes a nonzero release value, execution falls through to `$E35F`:

```text
$E35F  CMP #$02  -> $E168
$E366  CMP #$04  -> $E41F
$E36D  CMP #$DD  -> $E417
$E374  CMP #$FE  -> $E3ED
$E378  CMP #$FF  -> $E3ED
        otherwise -> ordinary progression body $E37C
```

The principal cases then reduce as follows.

## `$0670=$01` — ordinary progression advance

The ordinary path performs stage/character cleanup and, if the active canonical Saint `$0533` is Ikki (`4`), changes it to Seiya (`0`) before progression advances.

Then:

```text
INC $067D
STA $06CC = 0
```

For most new progression indices:

```text
$06CD = $E50B[$067D]
$0673 = $06CD | $30
```

The relevant `$E50B` progression-code table is:

```text
index: 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E
value: 00 00 00 02 02 02 02 00 00 00 08 0A 0A 0E 00
```

### Special new `$067D=$0C`

`$E3BB-$E3DA` replaces the table value with a branch based on old `$0673` bit 0 (`BIT $FFC0`, where `$FFC0=$01`):

| old `$0673 & 1` | final `$0533` | `$06CD` | `$0673` |
|---:|---:|---:|---:|
| `0` | `0` | `$0E` | `$3E` |
| `1` | `2` | `$0B` | `$3B` |

So `$06CD` cannot be reconstructed from `$067D` alone at index `$0C`.

### Stable destination after the advance

After the new `$067D` is mapped through `$F016`:

- new `$067D != $0D/$0E` -> A=`$10` enters `$E22C` -> stable engine state `$10`;
- new `$067D=$0D` or `$0E` -> `$0670=$05`, A=`$00` enters `$E22C` -> stable engine state `$00`.

Thus an ordinary progression advance normally returns to engine state `$10`, except the late `$0D/$0E` transitions which return to `$00`.

## `$0670=$FE` — forced-Saint progression advance

`$FE` first calls `$F2E4`, then remains `$FE` in normal mode `$04=$00`.

At `$E402-$E411` it:

```text
uses current $0533 for cleanup
sets $0533=0
sets $0670=1
```

and jumps into the same progression-advance body used by `$01`.

Therefore `$FE` has the same `$067D/$06CD/$0673` destination rules as `$01`, but canonical Saint `$0533` is forced to zero before the shared body. The special new `$067D=$0C` branch can still subsequently select canonical Saint `0` or `2` from old `$0673` bit 0.

## `$0670=$02` — state-zero commit without progression advance

`$02` jumps directly to `$E168` and explicitly bypasses the completion-mask test:

```text
$E170 LDA $0670
$E173 CMP #$02
$E175 BEQ $E19C
```

Then:

```text
$050E = $F016[$067D]
```

and `$E1A8-$E1F2` joins `$E227`, which loads A=`$00` for the common commit.

Effects:

- `$067D` unchanged;
- `$0533` unchanged;
- `$06CD` unchanged;
- `$0673/$06CC` unchanged by this bounded path;
- stable `$00/$01=$00`.

## `$0670=$DD` — forced state `$90`

`$DD` enters `$E417`:

```text
$0673 = $3F
JMP $E187
```

`$E187` then:

```text
$068F = $DD
...
LDA #$90
JMP $E22C
```

Therefore `$DD` deterministically commits engine state `$90`, preserves progression `$067D`, canonical Saint `$0533`, `$06CD` and `$06CC`, and forces `$0673=$3F`.

## `$0670=$FF` — three semantic outcomes

`$FF` first passes through `$F2E4 -> bank-5 $970A`. In normal mode `$04=$00`, that helper does not rewrite `$0670`.

### 1. Nonzero Saga phase `$06CE` -> selector reentry

Writer audit shows nonzero `$06CE` is created only by the stage-`$0A` multi-phase Saga family. Its direct writers are the phase increments around bank-5 `$9B8A/$9C17`; other direct writers clear it.

At `$E168`, any nonzero `$06CE` takes `$E28F` before the completion-mask test.

For canonical ROM byte `$FFDE=$00`, `$EEA1` does not set `$E9`; stage `$050E=$0A` returns through `$E2DD`, and `$0670=$FF` eventually reaches `$E327`.

`$E327` reseeds:

```text
$0670=0
$0584=0 -> later 5
$0585=0
$0586=0
```

This is **not** a stable `$E22C` destination. It is deliberate reentry into the already modeled interactive selector for the next Saga phase. `$067D`, `$0533`, `$06CE` and `$06CD` remain phase-local inputs.

### 2. `$06CE=0` and completion mask full -> state `$90`

For `$FF` with `$06CE=0`, `$E17B-$E185` tests:

```text
(($0673 | $06CC) & $0F) == $0F
```

When true, execution joins `$E187` directly:

```text
$068F=$DD
A=$90
-> $E22C
```

Unlike the explicit `$DD` path, this entry does **not** pass through `$E417`, so it does not force `$0673=$3F` first.

### 3. `$06CE=0` and completion mask not full -> state `$00`

Otherwise:

```text
$050E = $F016[$067D]
$068F = 0
$06CC = $FFC0[$0533]
```

For canonical Saint indices 0–4, `$FFC0[$0533]` is the one-hot mask:

```text
01, 02, 04, 08, 10
```

Then `$E214` applies one special normalization:

```text
if $050E == $0F and $0533 != 3:
    $050E = $0D
```

Finally A=`$00` reaches the common commit.

`$067D`, `$0533` and `$06CD` do not advance on this branch.

## Final `$03` mapping at common commit

All stable destinations (`$00`, `$10`, `$90`) eventually join `$E22C-$E254`.

The relevant final writes are:

```text
$E241 STA $01
$E243 STA $00
$E245 LDX $0533
$E248 LDA $E505,X
$E24B STA $03
```

For canonical Saint indices:

```text
$0533: 0 1 2 3 4
$03:   0 2 1 3 4
```

This is the inverse of the platform-entry mapping and restores the internal Saint index used by platform mode.

## Stable destination summary

For principal post-interactive warm reloads:

| Release / condition | Progression effect | Stable result |
|---|---|---|
| `$01` | increment `$067D`; rebuild progression fields | `$10`, except new `$067D=$0D/$0E` -> `$00` |
| `$FE` | force `$0533=0`, normalize to `$01`, increment | same rule as `$01` |
| `$02` | no increment | `$00` |
| `$DD` | no increment; `$0673=$3F` | `$90` |
| `$FF`, `$06CE!=0` | no global increment | reenter selector; no stable commit |
| `$FF`, `$06CE=0`, low completion mask full | no increment | `$90` |
| `$FF`, `$06CE=0`, mask incomplete | no increment; refresh `$06CC` Saint bit | `$00` |

Therefore the **complete stable engine-state set is exactly**:

```text
{ $00, $10, $90 }
```

plus the non-stable Saga phase reentry to `$E327`.

## Executable specification

Model:

- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformNormalWarmReloadDestination.cs`

Fixtures:

- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/NormalWarmReloadDestinationChecks.cs`

The model carries `$06CD` explicitly as input because non-advancing branches cross no `$06CD` writer. This is required for correctness at `$067D=$0C`, where `$06CD` can be `$0B` or `$0E` and cannot be inferred from `$E50B[$067D]`.

## Boundary after this closure

Once this model is verified, the principal normal platform reload is closed from accepted platform exit through:

```text
$3D/$E100
 -> warm reload setup
 -> interactive $F025/$A275
 -> terminal $0670
 -> post-loop routing
 -> common $E22C commit
 -> stable $00/$10/$90 (or explicit Saga phase reentry)
```

Remaining work should move to a different global engine/reload family or another still-open subsystem rather than reopening this chain without contradictory evidence.

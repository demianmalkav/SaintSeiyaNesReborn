# Boss context stage `$08` — Aquarius / Camus

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: **CONFIRMED static reconstruction** of the complete stage-local Aquarius/Camus control graph. Generic resource arithmetic, Bronze damage, Gold damage, dodge accounting and technique-menu arithmetic remain owned by their already-closed subsystems.

Canonical complete-file SHA-256:

`6917B31D7343A9A17170E833BACDBC3B1EBA3E02D11C51C0D44DBE436C9AD43A`

The static pass used the verified 262,160-byte ROM and the private JP→ES localization corpus only as a narrative cross-check. The control semantics below come from ROM code.

## Closed boundary

Stage index:

`$050E = $08`

Stage-local handlers:

| Role | PRG bank | CPU address | ROM file offset |
|---|---:|---:|---:|
| initialization | 5 | `$9B14` | `$15B24` |
| Talk | 5 | `$9F00` | `$15F10` |
| post-Bronze action | 5 | `$A8FC` | `$1690C` |
| post-Gold response | 5 | `$A9D3` | `$169E3` |
| Gold selector branch | 6 | `$90A8+` | `$190B8` |
| dodge-history sum helper | 5 | `$A1EC` | `$161FC` |
| Hyoga second-unlock helper | 5 | `$A1F4` | `$16204` |
| generic stage release owner | 5 | `$ACAA` | `$16CBA` |
| fixed special resume / redirect | fixed | `$ED57+` | `$1ED67` |
| fixed stage-8 setup marker | fixed | `$F1C6+` | `$1F1D6` |
| story-progress stage table | fixed | `$F016` | `$1F026` |
| ordinary battle reset | 1 | `$A973` | `$06983` |

## The central result: stage `$08` is two different Camus encounters

The same numeric stage is used for two semantically distinct contexts.

### 1. Redirected first Camus encounter

The ordinary story table at fixed `$F016` maps:

```text
$067D=$02 -> stage $02
```

So the first Camus encounter does **not** arise from ordinary story-stage selection.

Fixed special-resume code `$ED8F-$EDAF` intercepts release `$02` or `$03`. When the current stage is `$02` and the active battle Saint `$0533` is Hyoga (`$01`), it:

```text
$050E = $08
$06B8 = $0A
$0690 = $FF
$067C++
```

and, crucially, skips the ordinary reset at bank 1 `$A973`.

Therefore:

> `$06B8!=0` is the authoritative marker for the redirected first Camus encounter.

The canonical entry value is `$06B8=$0A`.

The predecessor is already-owned stage `$02`: its first post-Bronze branch `$A444+` emits release `$02` after installing platform substate `$02=$0E`. The fixed resume owns the actual redirection. Aquarius does not duplicate the stage-2 logic.

### 2. Final Aquarius/Camus encounter

The fixed story-stage table maps:

```text
$067D=$0A -> stage $08
```

This is the ordinary final Aquarius encounter.

The normal battle reset clears `$06B8`, so its canonical phase marker is:

```text
$067D = $0A
$06B8 = $00
```

The same ordinary reset clears `$0690`, then returns the current stage index. Fixed `$ED72-$ED8C` sees stage `$08` and immediately re-arms:

```text
$0690 = $FF
```

That block is later cleared by the second Hyoga unlock event.

## `$06B8` lifecycle

`$06B8` has one explicit stage-8-special writer in the fixed resume:

```text
$EDA5  LDA #$0A
$EDA7  STA $06B8
```

The ordinary battle reset at bank 1 clears it:

```text
$A995  STA $06B8
```

The special release `$02/$03` resume deliberately skips that reset, allowing the redirected first Camus state to survive.

Once the first encounter terminates through `$FE`, the normal unwind/reset path clears `$06B8` before fixed progression ownership converts `$FE` into story advance.

This is why `$06B8` is a phase/redirection latch, not a general Aquarius counter.

## `$06E1` lifecycle

The fixed stage-8 setup path at `$F1C6+` performs:

```text
LDA $050E
CMP #$08
BNE ...
LDA #$01
STA $06E1
```

Bank 1 `$A973` does **not** clear `$06E1`.

Inside stage-8 Talk, `$06E1` is read only when:

```text
$06B8 == 0
($0677 + $0678) == 0
```

If `$06E1==0`, one dialogue branch runs and `$06E1` is incremented. If it is already nonzero, Talk forces the Gold response.

Therefore `$06E1` is a persistent stage-8 prelude/previous-contact latch across ordinary battle resets. It can bridge the earlier redirected Camus event and the later final encounter. A fresh state with `$06E1=0` still has a defined one-time no-dodge Talk branch.

## Initialization `$9B14`

The handler starts with:

```text
LDA $067D
CMP #$02
BNE final/introduction branch
```

### `$067D==$02`

The handler calls fixed `$FB9F` with:

```text
X = 0
Y = current $0533
```

`$FB9F` wraps bank-1 `$AB10`, which saves the outgoing active battle resource record and loads the selected persistent record. The handler then writes:

```text
$0533 = 0
```

So this branch restores Seiya and returns without a technique unlock or release.

### `$067D!=$02`

The long presentation path ends with:

```text
$9B53 INC $0588
$9B56 INC $0696
JMP $9C3D
```

Shared `$9C3D` exits through release `$03`.

In the canonical final Camus context `$067D=$0A`, Hyoga arrives with technique count 2, so this event produces:

```text
persistent Hyoga count $0588: 2 -> 3
active menu count       $0696: 2 -> 3
```

Because technique availability is contiguous, this exposes Hyoga slot 2 / attack id 6: **Aurora Thunder Attack**.

The associated localization line `MSG_190` (`$BE`) reads in the current JP→ES draft:

> “Hyoga, tú deberías poder alcanzar el cero absoluto.”

This is supporting narrative evidence; the unlock itself is ROM-confirmed by the two `INC` instructions.

## Talk `$9F00`

The handler first partitions on `$06B8`.

### First Camus: `$06B8!=0`

Talk is driven entirely by `$066F`:

| inbound `$066F` | branch | outbound |
|---:|---|---:|
| 0 | first Camus/Hyoga exchange | 1 |
| 1 | second exchange | 2 |
| >=2 | final exchange | `+1` |

These paths return without incrementing transient `$DC`; therefore they do not directly force the normal Gold response.

Narrative anchors in the current localization corpus match the control flow:

- `$52` / `MSG_082`: Camus challenges Hyoga;
- `$53` / `MSG_083`: Hyoga says he cannot raise his fist against him;
- `$87` / `MSG_135`: Hyoga asks about the Patriarch's evil;
- `$5A` / `MSG_090`: Camus asks whether he still does not understand;
- `$B6` / `MSG_182`: Camus ends the argument.

The third Talk raises `$066F` to 3, which is the threshold consumed by the special post-Bronze handler.

### Final Camus: `$06B8==0`

Helper `$A1EC` returns:

```text
(byte)($0678 + $0677)
```

where `$0678` is successful Gold-attack dodges and `$0677` failed dodge attempts.

#### Total dodge attempts = 0

If `$06E1==0`:

- a one-time dialogue runs;
- `$06E1++`;
- no forced Gold response.

If `$06E1!=0`:

- generic stage-8 Talk response runs;
- transient `$DC++`;
- a Gold response is forced.

#### Total dodge attempts > 0

If `$066F!=0`:

- generic response;
- `$DC++`;
- Gold response forced.

If `$066F==0`:

- dialogue runs;
- `$066F++`;
- no forced Gold response.

Then active Saint is tested:

```text
LDA $0533
CMP #$01
BNE return
JSR $A1F4
INC $0588
INC $0696
```

Only Hyoga receives the second unlock.

`$A1F4` performs:

```text
LDA #$73
JSR $C00A
LDA #$00
STA $0690
RTS
```

So the event is not merely menu progression: it explicitly removes the scripted Bronze-hit block.

With canonical counts after initialization:

```text
$0588: 3 -> 4
$0696: 3 -> 4
$0690: $FF -> $00
```

This exposes Hyoga slot 3 / attack id 7: **Aurora Execution**.

The dialogue immediately before the branch uses `$BF` / `MSG_191`:

> “Para derrotarme, no tienes otra opción que despertar al cero absoluto.”

## Post-Bronze `$A8FC`

Again `$06B8` is the top-level partition.

### First Camus: `$06B8!=0`

No generic opponent-condition classifier is consulted.

The handler checks:

```text
$066F < 3
```

If true, it performs four `PLA` instructions and returns. This deliberately unwinds the enclosing post-action chain. Semantically, a Bronze action attempted before the three Talk steps is consumed without entering the ordinary completion path.

Once `$066F>=3`, the next Bronze action runs the scripted sequence that includes:

- `$8B` / `MSG_139` taunt;
- `$73` / `MSG_115` “Aurora Execution!!”;
- `$54` / `MSG_084` “Perdóname, Hyoga. Descansa dentro del Ataúd de Hielo.”;
- `$74` / `MSG_116` “Freezing Coffin!!”.

It then writes release `$FE`.

The termination does not depend on Camus's generic Life/Cosmo condition. The encounter is a scripted narrative loss/freeze gate.

### Final Camus: `$06B8==0`

Generic opponent classifier `$ACD6` supplies `$EB`.

- `$EB=$00` -> continue;
- `$EB=$01` -> continue;
- `$EB=$FF` -> final victory presentation and release `$FE`.

The victory sequence uses `MSG_192..194`, which describe Hyoga reaching absolute zero and saying farewell to Camus.

Thus final Camus has no special low-opponent threshold: only actual defeated state is terminal.

## Post-Gold `$A9D3`

Generic player classifier `$AD4D` supplies `$EA`.

### First Camus: `$06B8!=0`

- `$EA=$00` or `$01` -> continue;
- `$EA=$FF` -> scripted freezing sequence and release `$FE`.

This is important:

> Losing all Life in the first Camus encounter is **not** generic defeat `$FF`.

It is converted into the same story-progress `$FE` endpoint as the three-Talk scripted path.

### Final Camus: `$06B8==0`

- `$EA=$00` -> continue;
- `$EA=$01` -> nonterminal low-player feedback;
- `$EA=$FF` -> generic defeat release `$FF`.

## Gold attack selector `$90A8+`

Exact stage-8 branch:

```text
if $06B8 != 0:
    $0680 = 2
else:
    attempts = (byte)($0677 + $0678)
    if attempts < 2:
        $0680 = 1
    else:
        $0680 = 0
```

Stage-8 coefficient row from the already-closed damage table:

| structural slot | Cosmo/Life |
|---:|---:|
| 0 | 30 / 20 |
| 1 | 21 / 31 |
| 2 | 30 / 20 |
| 3 | 21 / 31 |

Reachability is therefore:

| Context | Condition | reachable slot | profile |
|---|---|---:|---:|
| first Camus | `$06B8!=0` | 2 | 30/20 |
| final Camus | dodge attempts 0–1 | 1 | 21/31 |
| final Camus | dodge attempts >=2 | 0 | 30/20 |
| any canonical stage-8 path | — | 3 | **unreachable** |

Slot 3 is structural data only.

## Release ownership and exact successors

Bank-5 `$ACAA` stores the release code. For `$FE` it also clears the active Life/Cosmo mirrors before delegating to the fixed outer flow.

Fixed `$E3F7-$E414` owns `$FE`:

1. save outgoing active Saint resources;
2. load Seiya (`X=0`);
3. set `$0533=0`;
4. convert to ordinary progression;
5. increment `$067D`;
6. resolve the next stage through `$F016`.

This closes both stage-8 terminal paths.

### First Camus successor

```text
$067D: $02 -> $03
$0533: -> Seiya
$F016[$03] = $03
```

Successor: stage `$03` (Cancer / Death Mask).

### Final Camus successor

```text
$067D: $0A -> $0B
$0533: -> Seiya
$F016[$0B] = $09
```

Successor: stage `$09` (Pisces / Aphrodite).

Final Camus `$FF` remains owned by the already-closed generic defeat path.

## Executable specification

Implemented in:

- `src/SaintSeiyaNesReborn.OriginalSpec/AquariusStage08Context.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/AquariusStage08ContextChecks.cs`

The fixtures discriminate:

- stage-2/Hyoga special resume -> stage8 + `$06B8=$0A`;
- `$067D==$02` initialization restore-to-Seiya branch;
- canonical final `$067D=$0A` initialization and 2->3 Hyoga unlock;
- the three first-Camus Talk steps and `$066F>=3` gate;
- first-Camus defeat converted to `$FE`;
- `$06E1` one-time no-dodge Talk behavior;
- first post-dodge Hyoga Talk 3->4 unlock + `$0690` clear;
- non-Hyoga no-unlock branch;
- final post-Bronze `$EB` outcomes;
- final post-Gold `$EA` outcomes;
- exact Gold slots 2/1/0 and unreachable slot3;
- `$FE` successors 2->3/stage3 and `$0A->$0B`/stage9.

## Frozen conclusions

Do not reopen without contradictory ROM evidence or a failing fixture:

1. `$06B8!=0` is the redirected first-Camus phase; canonical value is `$0A`.
2. First Camus enters from stage2 + active Hyoga through fixed special resume.
3. First Camus is narrative/scripted: three Talk steps gate the Bronze-action freeze ending; actual Hyoga defeat reaches the same `$FE`.
4. `$06B8==0` + story progress `$0A` is canonical final Camus.
5. `$06E1` is not reset by the ordinary battle reset and controls the one-time no-dodge Talk branch.
6. Final initialization increments Hyoga technique counts 2->3.
7. First post-dodge Hyoga Talk increments 3->4 and clears `$0690`.
8. First Camus forces Gold slot2; final Camus uses slot1 before two total dodge attempts and slot0 afterward; slot3 is unreachable.
9. First `$FE` advances progress 2->3 and stage3; final `$FE` advances `$0A->$0B` and stage9; both force Seiya.
10. Generic damage, dodge and resource arithmetic are composed, not duplicated here.

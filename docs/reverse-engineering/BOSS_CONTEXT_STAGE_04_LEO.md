# Boss context stage `$04` — Leo / Aioria

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: **CONFIRMED by static ROM control flow** for the stage-local encounter machine from stage-4 entry through Talk, scripted Bronze-hit blocking/unblocking, post-Bronze/post-Gold decisions, Gold-technique slot reachability and terminal `$0670=$01/$FF` releases. Generic resource arithmetic, damage scaling and dodge mechanics remain owned by their existing promoted specifications.

## Scope

Stage `$050E=$04` uses:

```text
initialization       $989D
Talk                 $9DD8
post-Bronze action   $A5B3
post-Gold response   $A63E
```

This checkpoint composes those handlers with the fixed battle loop and closes the stage-local fields that materially change control:

```text
$066F  Talk progression
$064D  one-time low-player event latch
$0681  Gold attack weakening tier
$0690  scripted Bronze hit blocker
$ED    Leo intro/victory presentation selector
$F1    persistent Leo history counter
$0680  Gold technique slot selected by generic bank-6 logic
$0670  encounter release/result
$068E  intro/setup-done latch
```

## 1. Fixed battle entry: Leo starts with scripted Bronze blocking

Fixed `$ED57+` clears `$0690`, calls common bank-1 battle reset `$A973`, then classifies stages `2/4/8/10` specially.

For stage 4:

```text
$A973 clears $066F/$0670/$0677/$0678/$064D/$064E/$067C/
              $068A/$068E/$0690/$06B8/$DD

then fixed $ED72-$ED89 detects $050E=$04 and writes:

$0690 = $FF
```

So every ordinary Leo battle-runtime entry reaches the encounter with the scripted Bronze-hit blocker armed.

`$A973` does **not** clear:

```text
$0681
$ED
$F1
```

Those three therefore survive an ordinary defeat/re-entry unless a broader bootstrap/reset owns them.

### What `$0690` actually does

Fixed `$FAB9-$FAE2` reads `$0690` before generic Bronze damage resolution. If `$0690!=0`, the path forces:

```text
$06BC = 0
```

`$06BC` is the already-promoted Bronze hit token. A zero token prevents the normal opponent-damage path.

Therefore `$0690` is not just a story flag: in Leo it is a real **scripted invulnerability / Bronze-hit blocking gate**.

## 2. When the Leo intro runs

The generic stage-intro gate in bank 5 checks:

```text
$068E == 0
active Saint == $F36F[$050E]
```

Fixed table `$F36F[4]` is:

```text
$00 = Seiya
```

Thus `$989D` runs when the stage-4 intro is pending and Seiya is active.

A non-Seiya active route can pass through ordinary battle entry without dispatching `$989D`; in that case `$068E` remains zero and inbound `$ED/$0681` remain unchanged until a later state satisfies the Seiya intro gate.

This distinction matters because `$989D` changes both `$ED` and `$0681`.

## 3. Leo initialization `$989D`

At entry:

```text
LDA #$01
STA $ED
```

so any route that actually executes the Leo intro leaves `$ED=1`.

The handler also temporarily clears bit `$20` of `$0673`, runs multiple presentation/event phases, optionally skips two visual/dialogue blocks according to `$0673 & $08` and `$0673 & $04`, later restores `$0673|=$20`, and eventually ends through shared `$9C3D`.

Those `$0673` branches affect presentation but do not bypass either Leo weakening write.

### Two unconditional weakening increments

The handler executes:

```text
$996A: INC $0681
...
$9A05: INC $0681
```

Both lie on the common control path. The conditional presentation branches at `$98E1-$992F` do not skip them.

Therefore:

```text
Leo intro executed:
$0681 -> $0681 + 2   (8-bit wrap preserved)
```

On an ordinary first stage-4 entry after prior progression has cleared `$0681`, this yields:

```text
$0681: 0 -> 2
```

The already-promoted Gold damage pipeline interprets:

```text
0    full raw drain
1    half raw drain
>=2  quarter raw drain
```

so the Seiya-triggered Leo intro immediately places Aioria in the **quarter-damage** weakening tier.

The clean-room model intentionally preserves byte wrap; the original does not saturate `$0681`.

### Intro handoff

Shared `$9C3D` closes the intro with:

```text
$0670 = $03
$068E = $01
$050E = saved real stage ($04)
```

Fixed entry flow later clears the transient `$0670=$03` before the command loop.

## 4. `$ED` and the two victory presentations

`$A5B3` tests `$ED` only after Aioria has been classified defeated.

Both branches end with:

```text
LDA #$01
JMP $ACAA
```

so both produce the same terminal result:

```text
$0670 = $01
```

but they use different presentation sequences:

```text
$ED==0  -> $A616 branch
$ED!=0  -> $A623 branch
```

Reachability is not equivalent to “one live / one dead”:

- if `$989D` has run, `$ED=1`, so the `$ED!=0` presentation is forced;
- if the stage-4 intro was skipped because a non-Seiya was active while `$068E=0`, inbound `$ED` can remain zero and the alternate `$ED==0` victory presentation remains structurally reachable.

The executable model therefore preserves both victory branches while keeping their shared release semantics unified.

## 5. Talk state machine `$9DD8`

`$066F` is the stage conversation counter. Common runtime reset seeds it to zero.

Leo differs materially from Taurus.

### First Talk — pre `$066F=0`

The handler takes `$9DDF`:

```text
presentation
INC $DC
INC $066F
```

Result:

```text
$066F: 0 -> 1
$DC transiently nonzero
$0690 remains $FF
```

The fixed Talk caller detects `$DC` and immediately forces the Gold counterattack path. Thus **the first Leo Talk costs a counterattack and does not unlock Bronze damage**.

### Second Talk — pre `$066F=1`

This is the unique special/non-forcing branch.

It selects a character-dependent text id from `$9E17,X`:

```text
Seiya  0 -> $86
Hyoga  1 -> $86
Shun   2 -> $87
Shiryu 3 -> $86
```

Ikki index 4 is not part of this four-entry stage table and is not included in the canonical Leo roster model.

The handler then checks `$F1`:

```text
if $F1 == 0:
    JSR $A1F4
```

`$A1F4` performs presentation/event `$73` and writes:

```text
$0690 = 0
```

Then:

```text
INC $066F   ; 1 -> 2
```

No `$DC` is raised on this branch, so no forced Gold counterattack follows.

Canonical initial unlock sequence:

```text
entry:       $0690=FF
Talk #1:     still FF, forced Gold response
Talk #2:     if $F1=0 -> $0690=00
```

This is the stage-local transition from scripted invulnerability to ordinary Bronze hit resolution.

### Third and later Talk — pre `$066F>=2`

The code returns to `$9DDF`, so every such Talk:

```text
INC $066F
INC $DC
```

and therefore forces a Gold counterattack.

Unlike Taurus, Leo does **not** clamp `$066F` at 2. It remains an 8-bit incrementing conversation byte.

Canonical shape:

```text
0 --Talk/force Gold--> 1
1 --Talk/unlock branch--> 2
2 --Talk/force Gold--> 3
3 --Talk/force Gold--> 4
...
```

## 6. `$F1`: persistent Leo history, not a per-attempt latch

Within executable Leo code:

```text
$9E0C  reads $F1
$A5CB  INC $F1
```

No ordinary battle-runtime reset writes `$F1`.

The broad bank-1 reset `$959D` zeros:

```text
$0500-$06FF
zero page $90-$FF
```

and therefore clears `$F1`. Its fixed caller is global bootstrap `$C206` on the `$10` bootstrap path.

So `$F1` is cleared by the broader bootstrap/new-runtime reset, but **persists across ordinary Leo defeat/re-entry**.

This persistence matters because the second Talk calls `$A1F4` only when `$F1==0`.

## 7. Post-Bronze handler `$A5B3`

The handler begins with generic opponent classifier `$ACD6`, then common presentation helper `$ADC4`.

### `$EB=$00` — healthy Aioria

The handler emits its healthy-result presentation and returns. No Leo-local control field changes.

### `$EB=$01` — low-condition Aioria

The handler tests active Saint `$0533`.

#### Seiya (`$0533=0`)

Immediate return:

```text
$0690 unchanged
$F1 unchanged
```

So a low-condition Aioria remains normally hittable if the scripted block had already been cleared and Seiya is active.

#### Non-Seiya (`$0533!=0`)

The handler executes:

```text
$0690 = $FF
INC $F1
RTS
```

Thus the low-opponent non-Seiya branch **re-arms scripted Bronze invulnerability** and records the event in persistent `$F1`.

The handler does not inspect `$06BC`; if control reaches it with `$EB=$01` and a non-Seiya active, `$F1` increments each time that branch executes. `$F1` is therefore an 8-bit counter, not merely a boolean.

### `$EB=$FF` — victory

The handler runs one of the two `$ED`-selected victory presentations described above, then releases:

```text
$0670 = $01
```

The terminal unwind happens through shared `$ACAA`; no Gold response occurs after that victory release.

## 8. Post-Gold handler `$A63E`

`$A63E` calls the generic player classifier through `$AD4D`, producing `$EA`.

### `$EA=$FF`

Immediate terminal defeat:

```text
$0670 = $FF
```

via shared `$ACAA`.

### First `$EA=$01` while `$064D==0`

The handler runs a one-time Leo presentation/event and then:

```text
INC $064D
```

so stable Leo values are:

```text
$064D = 0 -> 1
```

### `$EA=$00`, or later `$EA=$01`

No further Leo-local state change; control returns to the command loop.

This composes with the generic dodge result exactly as in Taurus: the post-Gold classifier can observe a player who was already low even when the current Gold attack was successfully dodged and dealt zero damage.

## 9. Gold attack slot reachability `$0680`

There is one canonical writer to `$0680` in bank 6:

```text
$9143: STA $0680
```

The selector has special branches for stages 5, 6, 8, 9 and 10. Stage 4 matches none of them and falls through to:

```text
LDA $065F
AND #$01
STA $0680
```

Therefore Leo can select exactly:

```text
$0680 = 0   when $065F is even
$0680 = 1   when $065F is odd
```

Structural slots 2 and 3 exist in the coefficient table but are **unreachable for canonical stage-4 selection**.

`$065F` is updated by fixed NMI helper `$E0AC` using the phase index `$0660` and a bank-6 delta table, so this choice is phase/timing-parity driven rather than a simple “alternate every turn” counter.

The already-promoted stage-4 coefficient row maps the two reachable slots to:

```text
slot 0 -> Cosmo/Life coefficients 48/32
slot 1 -> Cosmo/Life coefficients 32/48
```

The Leo context model returns only the reachable slot/profile. Numeric drain remains owned by `BOSS_BATTLE_DAMAGE.md`.

When the Seiya intro has raised `$0681` from 0 to 2, whichever raw profile is selected is then quartered by the generic weakening layer.

## 10. Retry/persistence behavior

On ordinary defeat/re-entry:

```text
$066F -> 0
$064D -> 0
$068E -> 0
$0690 -> FF  (stage-4 fixed entry re-arms it)

$0681 preserved
$ED   preserved
$F1   preserved
```

Whether `$989D` runs again is a separate generic intro-gate decision:

```text
$068E==0 AND active Saint==Seiya
```

If a prior low-condition non-Seiya branch made `$F1!=0`, then an ordinary retry still begins with `$0690=FF`; the second Talk will no longer call `$A1F4`, because `$F1` is nonzero. The clean-room fixture preserves this exact ROM consequence rather than silently normalizing it.

This is a strict original-game control property. It should not be “fixed” inside ORIGINAL SPEC; REBORN may later decide deliberately whether to retain or redesign it.

## 11. Complete Leo encounter graph

```text
battle runtime entry
  |
  | A973 clears local scratch
  | fixed stage-4 gate sets $0690=FF
  v
intro gate: $068E==0 && active Saint==Seiya ?
  |
  +-- yes -> $989D
  |            $ED=1
  |            $0681 += 2
  |            transient $0670=3 / $068E=1
  |            -> command loop
  |
  +-- no -------------------------------------> command loop

COMMAND LOOP
  |
  +-- Talk with $066F=0
  |      -> $066F=1
  |      -> keep $0690=FF
  |      -> forced Gold counterattack --------+
  |                                            |
  +-- Talk with $066F=1                        |
  |      -> character-specific dialogue        |
  |      -> if $F1=0: $0690=0                  |
  |      -> $066F=2                            |
  |      -> no forced Gold response            |
  |                                            |
  +-- Talk with $066F>=2                       |
  |      -> $066F++                            |
  |      -> forced Gold counterattack --------+
  |                                            |
  +-- Bronze action                            |
         |                                     |
         | generic hit gate:                   |
         |   if $0690!=0 -> $06BC=0            |
         v                                     |
      generic Bronze damage if hit             |
         |                                     |
         v                                     |
      Leo $A5B3                                |
         |                                     |
         +-- $EB=FF -> ED-selected presentation|
         |             -> $0670=01 VICTORY     |
         |                                     |
         +-- $EB=0 -> continue ----------------+
         |                                     |
         +-- $EB=1 + Seiya -> continue --------+
         |                                     |
         +-- $EB=1 + non-Seiya                 |
               -> $0690=FF                     |
               -> $F1++ -----------------------+
                                               |
                          generic Gold response <+
                             |
                             | $0680=$065F&1
                             | dodge / generic damage
                             v
                          Leo $A63E
                             |
                             +-- $EA=FF -> $0670=FF DEFEAT
                             |
                             +-- first $EA=1 -> $064D=1
                             |
                             +-- otherwise -> command loop
```

## 12. Composition boundaries

Leo consumes semantic results from already-promoted generic systems:

- `$EA/$EB` condition classifiers — `BOSS_BATTLE_RESOURCES.md`;
- Bronze and Gold numeric drain — `BOSS_BATTLE_DAMAGE.md`;
- `$06BC` Bronze hit token and generic scripted-block consumer;
- Bronze technique availability — `BATTLE_TECHNIQUES.md`;
- Gold dodge/no-damage result — `BOSS_DODGE.md`.

This stage context does not duplicate their arithmetic.

## 13. Executable specification

Model:

- `src/SaintSeiyaNesReborn.OriginalSpec/LeoStage04Context.cs`

Fixtures:

- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/LeoStage04ContextChecks.cs`

Fixtures discriminate:

- fixed stage-4 `$0690=FF` entry;
- Seiya-only intro dispatch;
- `$ED=1` and both unconditional `$0681` increments;
- intro-skipped `$ED=0/$0681` route;
- Talk 1/2/3+ topology and forced-counterattack behavior;
- Shun-specific second-Talk dialogue branch;
- `$F1==0` unlock vs `$F1!=0` no-unlock;
- healthy/low/defeated opponent branches;
- Seiya vs non-Seiya low-opponent behavior;
- `$F1` persistence and `$0690` re-arm over retry;
- both `$ED` victory presentation branches converging on `$0670=1`;
- one-time `$064D` low-player event and defeat `$0670=FF`;
- exact Gold-slot reachability `{0,1}` from `$065F&1`.

## Closure boundary

This checkpoint stops exactly at:

```text
victory -> $0670=$01
defeat  -> $0670=$FF
```

The existing progression/reload machinery owns what follows. Renderer/audio details of the individual dialogue and victory sequences remain outside this encounter-control checkpoint.

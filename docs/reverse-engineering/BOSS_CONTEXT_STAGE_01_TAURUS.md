# Boss context stage `$01` — Taurus / Aldebaran

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: **CONFIRMED by static ROM control flow** for the complete stage-local event machine from Taurus initialization through Talk phases, post-Bronze result handling, post-Gold result handling and terminal `$0670=$01/$FF` releases. Generic damage/resource/technique/dodge mechanics are composed from their existing promoted specifications and are not reimplemented here.

## Scope

Stage `$050E=$01` uses the four handlers already identified by the bank-5 dispatch tables:

```text
initialization       $97F8
Talk                 $9D2C
post-Bronze action   $A3A2
post-Gold response   $A415
```

This document closes how those handlers cooperate with the fixed battle loop. It intentionally stops when the stage writes a terminal release to `$0670`; warm reload/progression after those releases is already owned by the platform/reload checkpoints.

## 1. Common battle reset before Taurus-local logic

Fixed `$ED57+` reaches bank-1 `$A973` on a normal battle-runtime entry. `$A973` clears:

```text
$DC
$066F
$0670
$0677/$0678
$064D/$064E
$067C
$068A
$068E
$0690
$06B8
$DD
```

and returns the current stage index through `$BDCA`.

Important exception: **`$0681` is not cleared by `$A973`**.

Therefore the Taurus weakening tier is not a per-attempt scratch field. The first/new-progression setup can initialize it to zero, but a defeated/re-entered Taurus encounter can retain the weakening obtained from Talk.

On ordinary progression after a victory, fixed `$E37C-$E38E` calls bank-1 `$A75C`; that path clears `$0681` at `$A777` while advancing away from the completed encounter. Defeat `$0670=$FF` uses the separate `$E3ED` path and does not perform that progression reset before the retry/reload decision.

This yields the canonical retry property:

```text
Talk weakening obtained -> player loses -> Taurus re-entry
                           |
                           +--> $066F/$064D/$064E/$DD reset
                           +--> $0681 preserved
```

## 2. Taurus stage intro `$97F8`

The surrounding stage-entry dispatcher `$97B8` first saves the real stage index in `$067E` and dispatches by `$050E`.

For stage 1, `$97F8` begins with:

```text
LDA #$00
STA $064D
JSR $9C6D
```

It then performs presentation setup. One notable implementation detail is that it temporarily calls `$F2ED` with A=`$0C`, so `$050E` is temporarily stage `$0C` while shared display/event assets are configured.

The handler ends through shared `$9C3D`, which performs the logical handoff:

```text
$0670 = $03
$068E = $01
$050E = $067E   ; restore real stage, therefore $01
```

Fixed flow later clears `$0670` at `$E327` before entering the interactive command loop.

So the Taurus-specific intro boundary is:

```text
stage $01
 -> temporary presentation stage $0C
 -> restore stage $01
 -> $068E=1
 -> transient $0670=3
 -> fixed entry flow
 -> active command loop with $0670=0
```

`$068E` is therefore an intro/setup-done latch used by the surrounding battle-entry logic; it is reset on the next full `$A973` battle-runtime reset.

## 3. Fixed turn composition

The important fixed-bank call graph is:

```text
normal attack turn
  -> Bronze action / hit selection / opponent damage
  -> $F932 JSR $A361
       -> stage 1 $A3A2
  -> if no terminal victory:
       $F936 Gold counterattack
       -> generic technique selection
       -> dodge window
       -> Gold damage if not dodged
       -> $FA86 JSR $A381
            -> stage 1 $A415

Talk command
  -> $F0BD JSR $9C91
       -> stage 1 $9D2C
  -> if Talk set transient $DC:
       $F0C6-$F0CA -> counterattack path $F0A5/$F813
       -> $F813 clears $DC
       -> $F936 Gold counterattack
       -> stage 1 $A415
```

Therefore repeated Talk does not perform a Bronze action. It directly spends the command on a Gold counterattack.

If `$A3A2` emits victory `$0670=$01`, shared `$ACAA` unwinds the active battle call stack; the Gold response half of that turn is not executed.

## 4. Talk state machine `$9D2C`

`$066F` is the Taurus conversation counter. Common battle reset seeds it to zero.

### First Talk — `$066F=0`

The handler presents its first conversation pair and executes:

```text
INC $066F
```

Result:

```text
$066F: 0 -> 1
$0681 unchanged
$DC unchanged
```

No forced Gold counterattack follows.

### Second Talk — `$066F=1`

The handler presents the second conversation pair and increments:

```text
$066F: 1 -> 2
```

Then it inspects `$0681`.

If `$0681==0`:

```text
presentation/event $73
INC $0681
```

so the canonical first activation is:

```text
$0681: 0 -> 1
```

If `$0681` is already nonzero, it is left unchanged. The handler does **not** stack another weakening tier.

The already-promoted Gold damage specification owns the numeric consequence: tier 1 halves both raw Gold drain counters after their ordinary calculation. Stage 1's four structural Gold-technique coefficient slots are all the same (`19/29` Cosmo/Life), so technique-slot identity does not alter raw Taurus damage; `$0681=1` is the stage-local modifier that matters numerically.

### Third and later Talk — `$066F>=2`

The handler no longer increments `$066F`. It presents the repeated response and executes:

```text
INC $DC
```

The fixed Talk caller immediately checks `$DC`, branches to `$F813`, and `$F813` clears `$DC` before running `$F936`.

Thus canonical Talk reachability is exactly:

```text
$066F = 0 -> 1 -> 2
                  |
                  +-- all later Talks remain 2 and force Gold counterattack
```

At stable command boundaries `$DC` remains zero; its nonzero value is only a transient signal between `$9D2C` and `$F813`.

## 5. Post-Bronze handler `$A3A2`

The handler begins by calling opponent classifier `$ACD6`, which writes `$EB`:

```text
$EB=$FF   opponent Life is zero (defeated)
$EB=$00   opponent Life and Cosmo both strictly above stage threshold
$EB=$01   opponent alive but one/both resources do not clear threshold
```

Stage 1 uses threshold `$05` from `$AD42`.

### `$EB=$FF` — victory

The handler runs the Taurus victory presentation and ends:

```text
LDA #$01
JMP $ACAA
```

`$ACAA` writes:

```text
$0670 = $01
```

and unwinds the current battle stack. This is the stage-local terminal victory boundary.

### Surviving Aldebaran

For `$EB!=FF`, `$ADC4` performs common presentation setup without changing the encounter decision fields.

The next branch distinguishes the first low-condition event.

#### `$EB=$01` and `$DD!=5`

Common battle reset initializes `$DD=0`. On the first Taurus post-Bronze result where Aldebaran is in low condition, `$A3A2` performs:

```text
$DD = $05
character-dependent presentation
INC $064E
```

and returns.

This branch occurs before the `$06BC` hit-token test, so the first low-condition event wins priority regardless of whether the current Bronze hit token was zero or nonzero.

Within the Taurus handler, `$DD` has no other writer. Therefore `$DD=5` is a one-way stage-local presentation latch until the next common battle reset.

#### Otherwise, `$06BC==0`

`$06BC` is the already-promoted generic Bronze hit token. The generic damage consumer applies opponent damage only when it is nonzero.

If no first-low event was taken and `$06BC==0`, Taurus emits its no-hit feedback and executes:

```text
INC $064E
```

#### Otherwise, landed hit

If Aldebaran survived, no first-low event is pending, and `$06BC!=0`, `$A3A2` returns without another Taurus-local state change.

### Role of `$064E`

For stage 1, `$064E` is initialized to zero and written only by the two feedback/event branches above. No Taurus handler reads it back to gate damage, AI, Talk, victory or defeat.

So in the Taurus context it is an **observable feedback/event counter**, not a control latch. It can increment on:

- first entry into opponent low condition;
- subsequent no-hit Bronze results.

## 6. Gold counterattack and post-Gold `$A415`

The generic fixed path `$F936+` owns Gold attack selection, the dodge window, damage calculation/consumption and resource refresh. This stage context does not duplicate those mechanics.

After the generic Gold response, `$FA86` dispatches stage 1 to `$A415`.

`$A415` calls player classifier `$AD4D`, producing `$EA` with the same three-state encoding as `$EB`.

### `$EA=$FF` — defeat

The handler immediately executes:

```text
LDA #$FF
JMP $ACAA
```

which writes:

```text
$0670 = $FF
```

and unwinds the current battle stack. This is the stage-local terminal defeat boundary.

### `$EA=$01` and `$064D==0` — one-time low-player event

The handler performs its scripted presentation and then:

```text
INC $064D
```

No resource mutation or damage multiplier is introduced by this handler itself.

Because later low-condition passes require `$064D==0`, canonical Taurus values are:

```text
$064D = 0 -> 1
```

with no repeated event during the same encounter runtime.

### `$EA=$00`, or later `$EA=$01`

No Taurus-local control state changes. The battle returns to the command loop.

A successful dodge can still be followed by this classifier: if the player was already in low condition before the counterattack, the one-time `$064D` event can occur even though that particular Gold attack dealt zero damage.

## 7. Complete Taurus encounter graph

```text
common battle reset $A973
  |
  | clears $066F/$064D/$064E/$DD/... but preserves $0681
  v
stage-1 intro $97F8
  -> transient $0670=3, $068E=1
  -> fixed entry consumes handoff
  -> active $0670=0

COMMAND LOOP
  |
  +-- Talk #1 ------------------------> $066F=1 -----------------------> loop
  |
  +-- Talk #2 ------------------------> $066F=2
  |                                      if $0681=0 -> $0681=1 ------> loop
  |
  +-- Talk #3+ -> transient $DC ------> forced Gold counterattack -----+
  |                                                                      |
  +-- Bronze attack/action                                                |
         |                                                                |
         v                                                                |
      generic Bronze hit/damage                                           |
         |                                                                |
         v                                                                |
      Taurus $A3A2                                                        |
         |                                                                |
         +-- $EB=FF -> $0670=01 VICTORY                                  |
         |                                                                |
         +-- first $EB=1 -> $DD=5, $064E++                               |
         |                                                                |
         +-- otherwise $06BC=0 -> $064E++                                |
         |                                                                |
         +-- otherwise continue                                           |
         |                                                                |
         +------------------> generic Gold counterattack <----------------+
                                   |
                                   +-- dodge success -> no damage
                                   +-- otherwise generic Gold damage
                                   |
                                   v
                              Taurus $A415
                                   |
                                   +-- $EA=FF -> $0670=FF DEFEAT
                                   |
                                   +-- first $EA=1 -> $064D=1
                                   |
                                   +-- otherwise -> loop
```

## 8. Reachability conclusions

Canonical Taurus-local bounded fields at stable command boundaries are:

```text
$066F  {0,1,2}
$064D  {0,1}
$DD    {0,5}
$DC    0 stable; transient nonzero only for repeated Talk -> forced counterattack
$0670  0 active; 1 victory; FF defeat; 3 stage-intro handoff
```

`$064E` is an incrementing feedback counter and is not a control discriminator in stage 1.

For a normal first Taurus encounter `$0681` is 0 or 1. The code deliberately preserves any nonzero inbound value and does not increment it on the second Talk, so the clean-room model preserves that exact behavior instead of hard-clamping arbitrary input.

## 9. Composition boundaries

The Taurus model deliberately consumes semantic results from already-closed generic systems:

- `$EB` from `BOSS_BATTLE_RESOURCES.md` opponent condition classifier;
- `$EA` from the player condition classifier;
- `$06BC` from the generic Bronze hit gate;
- Bronze damage from `BOSS_BATTLE_DAMAGE.md`;
- Gold raw damage and `$0681` weakening from `BOSS_BATTLE_DAMAGE.md`;
- Bronze technique availability from `BATTLE_TECHNIQUES.md`;
- Gold dodge/no-damage result from `BOSS_DODGE.md`.

It does not reproduce any of those formulas.

## 10. Executable specification

Model:

- `src/SaintSeiyaNesReborn.OriginalSpec/TaurusStage01Context.cs`

Fixtures:

- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/TaurusStage01ContextChecks.cs`

The fixtures discriminate:

- intro handoff and command-loop normalization;
- first/second/repeated Talk;
- one-time weakening and weakening persistence over defeat/retry;
- healthy hit, no-hit feedback, first opponent-low event and victory;
- healthy/low/defeated player post-Gold branches;
- one-time `$064D` event;
- terminal release guards.

## Closure boundary

This checkpoint stops exactly at:

```text
victory -> $0670=$01
defeat  -> $0670=$FF
```

The already-promoted reload/progression machinery owns what happens after those releases. No renderer/audio internals are required to close Taurus stage control.

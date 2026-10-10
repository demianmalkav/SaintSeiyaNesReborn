# Virgo/Shaka stage `$05` — complete encounter and handoff context

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: stage-local control reconstruction for `$050E=$05`, composing the already-promoted generic battle/resource/damage/dodge systems with the stage-specific handlers `$9A28/$9E1B/$A661/$A7B3` and the fixed release/reload dispatcher.

## Scope

This document owns only Virgo-specific control semantics:

- Ikki substitution and entry guards;
- special platform detour;
- stage-local Talk behavior;
- post-Bronze/post-Gold stage scripts;
- stage-specific Gold-slot reachability;
- release ownership and handoff targets.

Generic damage arithmetic, resource decrement, dodge timing and technique-selection mechanics remain owned by their existing ORIGINAL SPEC documents.

## 1. Initialization `$9A28`

The handler begins by testing canonical active Saint `$0533`.

### Active Ikki (`$0533=$04`)

If `$0683==0`:

```text
$0673 = $3F
$0670 = $DD
RTS
```

This is not ordinary encounter victory/defeat. Fixed release handling routes `$DD` through `$E417`, preserves `$0673=$3F`, then enters the already-closed stable reload `$90` / engine family `$91-$99` high-presentation path.

If `$0683!=0`:

```text
$0533 = $00   ; Seiya
$0670 = $FE
RTS
```

`$ACAA` handles `$FE` specially by clearing active Life/Cosmo before unwinding. Fixed release handling later snapshots/switches back to Seiya and advances stage progression. Thus `$FE` is the completion handoff for the Ikki phase rather than a generic defeat.

### Active Saint is not Ikki

If `$0673!=$3F`, `$9A28` simply returns and ordinary Virgo combat proceeds.

If `$0673==$3F`, the long transition path executes. The control-relevant writes are:

```text
$0673 = $2F
...
$0533 = $04   ; Ikki becomes active
$0690 = $FF   ; scripted Bronze-hit block armed
...
JMP $9C56
```

Shared `$9C56` then writes:

```text
$0670 = $03
$068E = $01
```

This is the actual Ikki substitution/intro handoff.

## 2. Talk `$9E1B`

Talk topology is selected by active Saint and `$067C`.

### Non-Ikki

The handler selects character-indexed dialogue and ends at:

```text
INC $DC
RTS
```

The fixed caller consumes nonzero `$DC` by forcing the generic Gold response. Therefore every non-Ikki Virgo Talk consumes the player's command and gives Shaka a counterattack.

### Ikki before special platform detour (`$067C=0`)

This path uses a distinct dialogue sequence and returns directly without incrementing `$DC`.

Therefore it is the unique Virgo Talk branch that does **not** force Shaka's Gold response.

### Ikki after the detour (`$067C!=0`)

The alternate Ikki dialogue path reaches the common `$DC++` tail. It therefore forces a Gold response.

## 3. Post-Bronze `$A661`

`$A661` first calls the already-promoted opponent condition classifier `$ACD6`, producing `$EB` in `{00,01,FF}`.

### Non-Ikki branch

- `$EB=$FF` -> presentation -> `$0670=$01` ordinary victory.
- `$EB=$01` and `$064E==0` -> one-time event/feedback and `$064E++`.
- `$EB=$01` and `$064E!=0` -> no second increment.
- `$EB=$00` -> ordinary feedback/continue path.

No Virgo-specific Gold-slot or resource arithmetic is introduced here.

### Ikki, phase `$067C=0`

This branch ignores ordinary victory semantics and performs a special transition after the first Ikki Bronze action:

```text
$064B = 0
$064D = 0
$02   = $0D
$0670 = $02
```

Fixed engine handling treats release `$02` as a handoff to platform mode rather than terminal battle resolution. Platform substate `$0D` is already closed by `PLATFORM_EXIT_GATES.md`:

```text
substate $0D
player X >= $B4
player Y == $30
jump phase == 0
 -> paired global $3D reload
```

Thus Virgo contains a real playable platform detour in the middle of the boss context.

When battle runtime later resumes with release `$02`, fixed `$ED57` skips the ordinary `$A973` scratch reset and executes:

```text
INC $067C
```

This is the phase boundary that turns the later Ikki branch on.

## 4. Ikki after platform detour (`$067C!=0`)

At entry `$A6F0` first clears:

```text
$0690 = 0
```

This removes the scripted Bronze-hit block armed during the substitution sequence and allows ordinary Bronze hits again.

### Shaka still healthy (`$EB=$00`)

If `$0683==0`, control reaches `$A786`.

- first pass with `$06E0==0`: one-time stage event, then `$06E0++`;
- later landed hits (`$06BC!=0`): hit-feedback branch;
- otherwise continue.

`$06E0` is not cleared by ordinary `$A973`, so this is durable within the current run rather than per-turn scratch.

### Shaka becomes non-healthy for the first time

At `$A6F5+`, any `$EB!=0` (both low `$01` and defeated `$FF`) with `$064D==0` is intercepted before ordinary victory:

```text
$064D++
$0683++
```

The routine then performs scripted presentation and deliberately unwinds the current action path.

This is a major semantic difference from non-Ikki combat: **Ikki reducing Shaka to low/zero condition does not immediately produce `$0670=$01` victory.** It becomes a scripted phase transition marked by `$0683`.

### Subsequent Ikki post-Bronze after `$0683!=0`

The next reachable post-Bronze pass reaches `$A729+`, performs the long completion sequence and exits through:

```text
$0670 = $FE
```

That handoff returns active control to Seiya and advances progression through the already-known fixed release machinery.

## 5. Post-Gold `$A7B3`

### Non-Ikki

The branch follows the familiar boss pattern:

- `$EA=$FF` -> `$0670=$FF` defeat;
- first `$EA=$01` with `$064D==0` -> one-time low-player event and `$064D++`;
- otherwise continue.

### Ikki

Ikki uses a distinct rule:

- `$EA=$FF` -> `$0670=$FF` defeat;
- if `$0683!=0`, then `$EA=$01` is also promoted to `$0670=$FF` defeat;
- otherwise continue.

Therefore once the scripted `$0683` threshold phase has begun, an Ikki who merely falls into the generic low-condition state is treated as defeated by the Virgo script.

## 6. Gold attack selector `$0680`

Bank-6 `$9074+` has an explicit Virgo/Ikki override:

```text
if $050E==$05 and $0533==$04:
    $0680 = $02
else:
    ...
    $0680 = $065F & 1
```

Thus reachable stage-5 slots are:

```text
non-Ikki: slot 0 or slot 1
Ikki:     forced slot 2
slot 3:   structurally present but unreachable
```

Composing with the already-promoted stage-5 coefficient row:

| Reachable context | `$0680` | Cosmo coeff | Life coeff |
|---|---:|---:|---:|
| non-Ikki parity 0 | 0 | 42 | 28 |
| non-Ikki parity 1 | 1 | 24 | 36 |
| Ikki | 2 | 37 | 22 |

The generic damage formula and dodge gate are not duplicated here.

## 7. Release ownership

The complete Virgo-specific release map is:

```text
$01 -> generic boss victory/progression
$02 + $02=$0D -> platform substate $0D -> platform gate -> global $3D reload
$03 -> shared intro/substitution handoff, then command loop
$DD -> fixed $E417 -> $0673=$3F -> stable reload $90 -> closed $91-$99 high family
$FE -> special completion handoff; clear active resources, force Seiya, advance progression
$FF -> generic defeat/re-entry path
```

No Virgo-local release is left as an unowned black box.

## 8. Structural encounter graph

```text
ordinary non-Ikki Virgo battle
  |
  +-- Talk -------------------------> forced Gold response
  |
  +-- Bronze action -> $A661
         |-- Shaka defeated --------> $01 victory
         |-- low/healthy -----------> continue

special Ikki entry while $0683=0
  -> $DD / marker $3F / high presentation owner

non-Ikki re-entry with marker $3F
  -> substitute Ikki
  -> arm $0690
  -> $03 intro handoff
  -> Ikki command loop, $067C=0

first Ikki Bronze action
  -> $02 + platform substate $0D
  -> playable platform detour
  -> $3D reload
  -> fixed special resume increments $067C

Ikki command loop, $067C!=0
  -> clear $0690
  -> healthy Shaka: one-time $06E0 event / ordinary hits
  -> Shaka low or zero first time: $064D++, $0683++
  -> next post-Bronze with $0683!=0: $FE
  -> force Seiya + advance stage

Gold responses:
  non-Ikki: ordinary low-event / defeat
  Ikki: defeat at $EA=$FF; also at $EA=$01 once $0683!=0
```

## Executable artifact

- `src/SaintSeiyaNesReborn.OriginalSpec/VirgoStage05Context.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/VirgoStage05ContextChecks.cs`

The model intentionally exposes release ownership and the platform-detour phase boundary instead of flattening Virgo into a single boss-turn loop.

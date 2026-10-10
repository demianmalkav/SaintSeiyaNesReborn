# Stage `$00` context — Mu / pre-battle repair

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: **CONFIRMED static reconstruction** of the complete reachable stage-local `$050E=$00` control graph. This context is special/non-ordinary: it has no reachable Bronze attack, Gold response, damage, dodge or post-action arithmetic.

Canonical complete-file identity used for this pass:

- size `262160` bytes;
- SHA-1 `F871D9B3DAFDDCDAD5F2ACD71044292E5169064E`;
- MD5 `3B0F17C2B6EFC928B3D3FE9B1A389680`;
- SHA-256 `6917B31D7343A9A17170E833BACDBC3B1EBA3E02D11C51C0D44DBE436C9AD43A`;
- CRC32 `F8D258A3`.

## Closed boundary

Canonical story seed:

```text
$067D = $00
$050E = $00
```

Fixed `$F016[$00]=$00` owns the story-progress to stage mapping. Progress `$0E` also contains numeric `$00`, but that is the already-closed post-Saga platform tail and must not be treated as a Mu re-entry.

Relevant owners:

| role | PRG bank | CPU | file offset |
|---|---:|---:|---:|
| global `$0500-$06FF` clear | 0 | `$AD4A` | `$02D5A` |
| new-game story/Saint initializer | 1 | `$A100` | `$06110` |
| common battle reset | 1 | `$A973` | `$06983` |
| common battle Saint commit | 5 | `$9780` | `$15790` |
| stage init | 5 | `$97F7` | `$15807` |
| Talk handler | 5 | `$9CB7` | `$15CC7` |
| first-Talk Saint text table | 5 | `$9D24` | `$15D34` |
| second-Talk Saint text table | 5 | `$9D28` | `$15D38` |
| resource-allocation gate | fixed | `$F041` | `$1F051` |
| Attack gate | fixed | `$F057` | `$1F067` |
| Talk command owner | fixed | `$F0B1` | `$1F0C1` |
| Escape gate | fixed | `$F0D3` | `$1F0E3` |
| blocked-command owner | fixed | `$F238` | `$1F248` |
| release `$01` owner | fixed | `$E399` | `$1E3A9` |
| story increment owner | fixed | `$E3B3` | `$1E3C3` |
| story-stage table | fixed | `$F016` | `$1F026` |
| Saint-availability gate | fixed | `$F6FE` | `$1F70E` |

## Canonical initial state

The global clear at bank 0 `$AD4A-$AD54` writes zero across `$0500-$06FF`. Therefore before the first Mu entry:

```text
$06BB = $00
$06CD = $00
$066F = $00
$0670 = $00
```

Bank-1 initializer `$A100+` derives the initial story availability mask:

```text
LDA $06CD       ; 00
ORA #$30
STA $0673       ; 30
```

It then selects Seiya (`$0533=0`) as the initial active Saint.

The fixed selector at `$F6FE+` uses Saint bits from `$FFC0`:

```text
Seiya  01
Hyoga  02
Shun   04
Shiryu 08
Ikki   10
```

and rejects a candidate whose bit is already set in `$0673`. With `$0673=$30`, Seiya/Hyoga/Shun/Shiryu are structurally available while Ikki is rejected. This explains why the two Mu Saint-message tables are exactly four bytes long.

When the chosen Saint enters the battle/event context, bank-5 `$9780-$9789` ORs that Saint bit into `$0673`. That commit does not alter the stage-local Mu machine described below.

## Common reset ownership

The actual common battle reset is bank 1 `$A973+`:

```text
A973  LDA #$00
A975  STA $DC
A977  STA $066F
A97A  STA $0670
A97D  STA $0677
A980  STA $0678
A983  STA $064D
A986  STA $064E
A989  STA $067C
A98C  STA $068A
A98F  STA $068E
A992  STA $0690
A995  STA $06B8
A998  STA $DD
```

Material Mu consequence:

- `$066F` starts at `0`;
- `$0670` starts at `0`;
- `$06BB` is **not** cleared here.

Because the global RAM clear seeded `$06BB=0` and the only direct material accesses found are `$F238 LDA $06BB` and `$F25C INC $06BB`, `$06BB` is an accumulating blocked-command counter/latch during this stage.

Stage init `$97F7` itself is exactly `RTS`; there is no additional stage-local initializer.

## Command topology

The fixed battle command dispatcher routes semantic commands to four owners. Stage zero is explicitly special-cased in three of them.

### Resource allocation — `$F041+`

```text
LDA #$01
STA $DB
LDA $050E
BNE ...
JMP $F238
```

At `$050E=0`, allocation never reaches its ordinary resource helper.

### Attack — `$F057+`

```text
LDA #$02
STA $DB
LDA $050E
BNE ...
JMP $F238
```

At `$050E=0`, Attack never reaches `$F813`, Bronze technique/damage, `$A361`, Gold selection, dodge, Gold damage or `$A381`.

### Talk — `$F0B1+`

Talk does **not** test stage zero out. It prepares the ordinary interaction presentation, selects bank 5 and calls `$9C91/$9C95`, whose stage table dispatches `$050E=0` to `$9CB7`.

### Escape — `$F0D3+`

```text
LDA #$04
STA $DB
LDA $050E
BNE ...
JMP $F238
```

At `$050E=0`, Escape shares the same blocked owner as allocation and Attack.

Therefore the only stage-local progression surface is Talk.

## `$F238` blocked-command machine and `$06BB`

`$F238` first tests `$06BB`:

```text
LDA $06BB
BEQ first_blocked

; repeated blocked attempt
... message $48 ...

first_blocked:
...
INC $06BB
LDA #$39
STA $066A
... presentation/redraw ...
return to command loop
```

Exact stage-local semantics:

```text
before command   branch                 after command
$06BB=00         first blocked          $06BB=01
$06BB!=00        repeated; adds $48     $06BB++ (8-bit)
```

All blocked variants end with shared message selector `$39`. A repeated branch adds selector `$48` before it. `$06BB` is incremented after the branch predicate, so `$FF` takes the repeated branch and then wraps to `$00`, exactly as an 8-bit `INC`.

The three blocked commands do not write `$0670` and do not enter any ordinary battle arithmetic.

## Talk `$9CB7` — complete state machine

`$9CB7` has one control predicate:

```text
LDA $066F
BNE second_talk
```

### First Talk — `$066F==0`

The handler performs presentation work with common selectors `$32/$33`, invokes graphics-only helper `$AEDA`, then selects one Saint-specific message from `$9D24`:

```text
$9D24: 35 35 36 34
```

Canonical mapping:

| `$0533` | Saint | first-Talk selector |
|---:|---|---:|
| `0` | Seiya | `$35` |
| `1` | Hyoga | `$35` |
| `2` | Shun | `$36` |
| `3` | Shiryu | `$34` |

The persistent tail is only:

```text
LDA #$01
STA $066F
RTS
```

So first Talk produces:

```text
$066F: 00 -> 01
$0670: 00
$06BB: unchanged
```

The `$AEDA` helper is presentation-only for this context: it installs graphics/presentation work counters, redraws and restores the previous PRG bank. It does not mutate Mu story progress, release, resource totals or battle damage state.

### Second/repeated Talk — `$066F!=0`

The second branch uses common selectors `$37/$38` and Saint table `$9D28`:

```text
$9D28: 3B 3B 11 3B
```

| `$0533` | Saint | second-Talk selector |
|---:|---|---:|
| `0` | Seiya | `$3B` |
| `1` | Hyoga | `$3B` |
| `2` | Shun | `$11` |
| `3` | Shiryu | `$3B` |

The terminal tail is:

```text
LDA #$01
STA $0670
RTS
```

It does not increment `$066F` again. Canonical state therefore ends as:

```text
$066F = 01
$0670 = 01
```

Any structurally nonzero `$066F` value selects the same second branch; canonical execution reaches it with exactly `1`.

## Release `$01` -> Taurus

Fixed `$E399+` consumes `$0670=$01`. Its only active-Saint substitution checks for Ikki (`$0533=4`). Ikki is unreachable in canonical Mu, so Seiya/Hyoga/Shun/Shiryu are preserved.

Fixed `$E3B3` then advances:

```text
INC $067D       ; 00 -> 01
$06CC = 00
```

For progress `$01`:

```text
$E50B[$01] = $00
$06CD = $00
$0673 = $00 | $30 = $30
$F016[$01] = $01
```

The exact successor boundary is therefore:

```text
$067D = $01
$050E = $01
$06CD = $00
$0673 = $30       ; before the next battle-entry Saint bit is committed
active Saint = preserved 0..3
```

That joins the already-closed `TaurusStage01Context`; Taurus internals are not reopened here.

## Negative reachability proof

Stage `$00` has structural post-Bronze and post-Gold table entries at shared `$A3A1` (`RTS`), but they are unreachable under canonical stage-zero command control.

There is no canonical path from any Mu command to:

- Bronze technique selection;
- Bronze damage;
- `$A361` post-Bronze dispatch;
- Gold technique selector `$0680`;
- dodge processing;
- Gold damage;
- `$A381` post-Gold dispatch.

This is why stage `$00` owns no reachable Gold slot despite the wider battle engine containing structural data around the same numeric namespace.

## Executable specification

`src/SaintSeiyaNesReborn.OriginalSpec/MuStage00Context.cs` models:

- canonical global/new-game/reset seed;
- reachable Saint roster `0..3` and Ikki exclusion;
- `$06BB` first/repeated blocked-command behavior;
- shared blocked command ownership for allocation/Attack/Escape;
- both `$9CB7` Talk branches;
- exact `$9D24/$9D28` Saint message selectors;
- release `$01` composition to the Taurus boundary;
- explicit negative Bronze/Gold reachability.

`tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/MuStage00ContextChecks.cs` contains discriminating fixtures for all four Saints, all canonical semantic commands, `$06BB` repeated/wrap behavior, both Talk states, terminal guards and the exact `$067D=$01/$050E=$01` successor.

## Closure

Stage `$00` is now a closed dedicated **special context**, not a boss fight and not a generic empty slot.

The remaining material uncovered stage contexts from the frozen `$00-$0B` audit are:

```text
$02 Gemini / first Camus branch
$03 Cancer / Death Mask
$06 Scorpio / Milo
$07 Capricorn / Shura
```

The next canonical gap is `$02`.

# Engine state `$50` — front-end/title modal shell

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: **CONFIRMED** structural/semantic control-flow model for exact global engine state `$50`, including all reachable `$0200` substates, `$0201/$0202` control roles, executable CHR-to-RAM overlays, attract-loop handoff, normal start path and password-entry/acceptance path.

## Semantic correction to PR #117

PR #117 correctly closed the **structure** of global states `$30-$4D`, but classified that structure too broadly as a generic scene/battle scaffold. State `$50` supplies the missing context and changes the semantic classification.

The proven topology is a **front-end/title modal shell plus attract/presentation loop**:

```text
cold RESET
  -> $50:$00 -> $01 -> $02 -> $03 -> $04 -> $05
                                  Start from $00-$04 -> $05

$50:$05 --scripted event--> $06 -> global $30 -> $31 ... $4D -> $50:$00
$50:$05 --Start-----------> $07
                               branch $0202=0 -> global $10
                               branch $0202=1 -> password $09
                                                    valid -> $08 -> global $10

Start during global $30-$4D -> $50:$05
```

Therefore the transition graph promoted by `SceneBattleEngineStateGraph` remains structurally valid, including its exact `$30/$31-$38/$40-$4D` reachability. The semantic label `scene/battle` is superseded by this document: those states are the front-end attract/presentation sequence, not the ordinary boss-battle state machine documented elsewhere.

The historical class/file names from PR #117 are retained temporarily to avoid rewriting a verified checkpoint merely for symbol churn. New work should use the semantic terminology **front-end attract/presentation**.

## 1. Global state `$50` producers

### Cold reset / restart

Fixed reset clears RAM pages and reaches:

```text
$C14B  LDA #$50
$C14D  STA $00
$C14F  STA $01
$C151  JMP $DA13
```

`$DA13` initializes `$0200=0`, proving cold boot enters paired global `$50` at modal substate `$00`.

### Completion of the attract/presentation loop

PR #117 proved `$4D` terminal NMI uses bank-1 `$8D41` to write paired `$50`. Fixed NMI then recognizes live `$50`, reaches `$D2E9`, and jumps through `$C14B`, which restarts the modal shell at `$0200=0`.

Thus completed attract playback returns to the same front-end intro entry as cold reset.

### Start during attract/presentation

Every active `$3x/$4x` main frame polls Start first. The proven writer is:

```text
$C34F  LDA #$50
$C351  STA $00
$C353  STA $01
$C355  LDA #$05
$C357  JMP $DA15
```

Unlike `$C14B`, this enters `$DA15` with A=`$05`, so Start during attract playback returns directly to modal ready substate `$05` instead of replaying substates `$00-$04`.

No additional reachable paired `$50` producer changes the modal graph.

## 2. Main-thread lanes: `$0201`

State `$50` main begins:

```text
$DA13  LDA #$00
$DA15  STA $0200
$DA18  JSR $DB9C
$DA1B  LDA #$00
$DA1D  STA $0201
...
$DA33  LDA $0201
$DA36  JSR $8960
        .word $DA3D
        .word $DA5C
```

`$0201` is therefore a two-way main-thread lane selector:

- `$0201=0` -> `$DA3D`: initialization/display lane, then main thread deliberately parks while NMI owns modal progression;
- `$0201=1` -> `$DA5C`: commit/exit lane, entered from NMI substates `$06/$07/$08`.

NMI handlers `$DB7C` and `$DB84` set `$0201=1` and jump back into `$DA25`, allowing main execution to leave the parked lane.

## 3. NMI dispatcher: complete reachable `$0200` set

Mirror state `$01=$50` short-circuits ordinary NMI dispatch:

```text
$D282  LDA $01
$D284  CMP #$50
$D286  BNE ...
$D288  JMP $DABC
```

`$DABC` dispatches on `$0200` through an inline table:

| `$0200` | NMI handler | role |
|---:|---:|---|
| `$00` | `$DAEB` -> bank0 `$8088` | intro/presentation phase 0 |
| `$01` | `$DAEB` -> bank0 `$80D4` | intro/presentation phase 1 |
| `$02` | `$DAEB` -> bank0 `$8131` | intro/presentation phase 2 |
| `$03` | `$DAEB` -> bank0 `$84E8` | intro/presentation phase 3 |
| `$04` | `$DB04` | final intro phase -> ready |
| `$05` | `$DB27` | ready/input + scripted attract trigger |
| `$06` | `$DB7C` | commit attract-loop handoff |
| `$07` | `$DB7C` | commit selected start/password branch |
| `$08` | `$DB84` | commit accepted password |
| `$09` | `$DB8C` | password-entry UI |

All ten values `$00-$09` have reachable producers. No `$0200 >= $0A` entry is present in the finite dispatcher.

## 4. Intro chain `$00->$01->$02->$03->$04->$05`

States `$00-$03` share `$DAEB`, which clears `$0202`, performs presentation work, dispatches bank-0 logic keyed by `($0200 & 3)`, then reaches the common Start helper `$8857`.

### `$00 -> $01`

Bank-0 `$8088` waits until `$0204>=2`; then the transition occurs when the byte-sized candidate `$0207+1` reaches at least `$F0`.

### `$01 -> $02`

Bank-0 `$80D4` advances when observed `$0204>=3`.

### `$02 -> $03`

Bank-0 `$8131` performs the presentation position update. The transition is taken when the post-update pair reaches the proven terminal condition `$0207=0`, `$0208=$FD`.

### `$03 -> $04`

Bank-0 `$84E8` advances once low frame counter `$0203>= $80`.

### `$04 -> $05`

Handler `$DB04` initializes the final front-end phase. Entry through `$887B` has reset `$0204`, so the first observing NMI reaches `$8515` and promotes modal state to `$05`.

This yields a closed normal intro chain:

```text
$00 -> $01 -> $02 -> $03 -> $04 -> $05
```

## 5. Start is an edge-triggered redirect, not a held-button state

Bank-0 `$8B1F` serially reads controller 1 into `$020A` and computes newly pressed edges into `$020B`:

```text
(new XOR old) & new -> $020B
```

After the routine's ROR packing, the relevant bits are:

- `$08` Start;
- `$10` Up;
- `$20` Down.

Common helper `$8857` tests **new Start** (`$020B & $08`), then maps current `$0200` through table `$8871`:

```text
$00-$04 -> $05
$05     -> $07
$06     -> $05
$07     -> $07
$08-$09 -> $05
```

Reachable handlers `$00-$05` pass through this common helper. Consequences:

- Start during intro `$00-$04` skips directly to ready state `$05`;
- Start in ready `$05` commits state `$07`;
- if the scripted attract event changes `$05->$06` on the same NMI as a Start edge, the common helper runs afterwards and maps `$06->$05`, cancelling that attract launch;
- in `$04`, the local `$04->$05` transition happens before the helper, so a Start edge on that same NMI maps the newly-created `$05` onward to `$07`.

## 6. `$0202` is a one-sample branch selector

Ready handler `$DB27` calls bank-0 `$881D` before committing any route.

`$881D` rewrites `$0202` on **every** observing ready-state NMI:

- new Up edge -> `$0202=0`;
- otherwise new Down edge -> `$0202=1`;
- otherwise -> `$0202=0`.

Up has priority if both are present.

Therefore `$0202` is not a persistent menu cursor. Branch 1 is live only for the input sample in which Down is newly pressed; to commit it through Start, the branch selection and Start edge must coexist in the relevant ready-state sample.

This distinction is preserved in `FrontEndState50Machine` fixtures.

## 7. Scripted attract handoff `$05->$06->$30`

State `$05` also watches `$04EF`, which is produced by the front-end object interpreter. When nonzero:

```text
clear $04EF
$0200 = $06
```

NMI state `$06` executes `$DB7C`:

```text
$0201 = 1
JMP $DA25
```

Main commit lane `$DA5C` recognizes `$0200=$06` and writes paired global `$30` at `$DA9D`.

PR #117 then proves:

```text
paired $30
 -> fixed bootstrap increments live $00
 -> main sync
 -> dispatch-ready $31
 -> ...
 -> $4D
 -> paired $50
 -> $0200=$00
```

This closes `$30-$4D` as the **front-end attract/presentation loop**.

## 8. Selection commit `$05 + Start -> $07`

Start in ready step `$05` changes `$0200` to `$07`. NMI `$07` uses the same `$DB7C` commit switch as `$06`, setting `$0201=1`.

Main `$DA5C` then branches on `$0202`.

### `$0202=0` — normal start path

The zero branch reaches `$DA90`, initializes the low-family work fields, and writes paired global `$10`.

This composes with the already-promoted `$10->$11-$14` family.

### `$0202=1` — password entry path

The nonzero branch calls bank-0 `$8013`, then `$AD4A` to initialize password work memory. `$8013` is not an ordinary display routine: it loads and executes a CHR-backed RAM overlay, whose entry explicitly writes `$0200=$09`.

Thus:

```text
$50:$07, $0202=1
 -> executable password overlay
 -> $50:$09
```

## 9. Executable code stored in CHR ROM

This checkpoint closes a previously hidden architectural mechanism.

Bank-0 `$8000` and `$8013` use helper `$8023` to:

1. select MMC1 4 KiB CHR bank `$1F` (31);
2. read pattern memory through `$2006/$2007`;
3. copy exactly `$03C0` bytes into CPU RAM `$0440-$07FF`;
4. return to the loader;
5. `JMP $0440` and execute the copied bytes as 6502 code.

The original fixed-bank `JSR` remains on the stack, so the RAM overlay eventually `RTS` directly back to its fixed-bank caller.

### Front-end overlay

`$8000` copies PPU `$1000-$13BF` from CHR bank 31.

The resulting RAM code at `$0440` initializes the front-end nametable/display structures and returns. It does not change global engine state.

### Password overlay

`$8013` copies PPU `$1400-$17BF` from the same CHR bank.

Its RAM entry at `$0440` initializes the password-entry screen and terminates with:

```text
LDA #$09
STA $0200
RTS
```

This is the definitive producer of password modal substate `$09`.

This finding changes the architectural model: some original executable logic is physically stored in CHR ROM, transferred via the PPU port, and run from CPU RAM. Any future clean-room reconstruction of the original must treat CHR not as graphics-only storage.

## 10. Password editor `$09`, validation, and accepted state `$08`

State `$09` NMI `$DB8C` calls bank-0 `$B0AB`, which reaches the already-documented manual password entry machinery around `$B240/$B271+`.

Existing `PASSWORD_SYSTEM.md` owns the password semantics:

- `$06EC` grid column;
- `$06ED` grid row;
- `$06EE` password position;
- accepted symbols at `$06AC+position` / `$0140+position`;
- decoder `$AEB5` validates checksum/obfuscation and restores staged persistent state.

Decoder `$AEB5` returns A=`$00` on success and A=`$FF` on failure.

The password UI success path writes:

```text
$0200 = $08
```

Invalid input stays in `$09` and drives its feedback state without changing the global engine state.

Thus:

```text
$09 --invalid--> $09
$09 --valid----> $08
```

## 11. Accepted password `$08 -> global $10` with restoration

NMI state `$08` sets `$0201=1`, entering main commit lane. `$DAA4+` writes paired `$10` and jumps through `$C1ED` rather than the ordinary zero-branch setup.

`$C1ED` restores the decoded staged progression fields:

```text
$0100 -> $067D
$0101 -> $06CD
```

before the existing global bootstrap resumes and enters the already-promoted `$10->$11` family.

Therefore state `$08` is specifically the **accepted-password handoff**.

## 12. Complete reachable graph

```text
RESET / completed attract
          |
          v
$50:$00 -> $01 -> $02 -> $03 -> $04 -> $05
   \          Start at any intro phase ---------/

$05 --scripted object event--> $06 --commit--> global $30
 ^                                             |
 |                                             v
 |                                      $31 ... $4D
 |                                             |
 +---------------- completed attract <---------+

$05 --Start--> $07
                |
                +-- $0202=0 --> global $10 --> known $11-$14
                |
                +-- $0202=1 --> CHR/RAM password overlay --> $09
                                                              |
                                                  invalid -----+
                                                              |
                                                  valid -> $08
                                                            |
                                                            +--> restored global $10
```

Start during `$30-$4D` writes paired `$50` and enters `$0200=$05`, returning directly to the ready front-end state.

## 13. Control-field summary

| field | confirmed role |
|---|---|
| `$00/$01` | global engine state; both remain `$50` throughout modal UI except commit exits |
| `$0200` | modal substate `$00-$09` |
| `$0201` | main lane: 0 initialization/parked-NMI mode, 1 commit/exit mode |
| `$0202` | ready-state one-sample branch selector: 0 normal start, 1 password entry |
| `$020A` | current controller packed bits |
| `$020B` | newly pressed controller edges |
| `$0203/$0204` | low/high frame counters used by front-end intro timing |
| `$04EF` | scripted front-end object event that promotes `$05->$06` |

## 14. Executable artifact

`FrontEndState50Machine` promotes:

- all real state-$50 entry modes;
- complete reachable `$0200=$00-$09` set;
- exact intro chain and Start skip semantics;
- one-sample `$0202` selection behavior;
- scripted attract `$05->$06->$30` handoff;
- `$05+Start->$07`, branch-zero global `$10`, branch-one password `$09`;
- password invalid stay / valid `$09->$08`;
- accepted-password `$08->$10` restored-progress path;
- CHR31 -> RAM `$0440-$07FF` executable-overlay architecture.

Rendering details, individual attract text content and audio remain separate subsystems. The control graph no longer depends on them.

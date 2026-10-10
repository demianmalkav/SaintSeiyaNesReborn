# Global engine-state dispatcher (`$00/$01`)

Status: **CONFIRMED structural partition** for the fixed-bank top-level dispatchers in the canonical Japanese ROM.

Scope: logical `$00/$01` state routing only. PPU/OAM, renderer, controller plumbing, audio and bank-switch implementation details are intentionally excluded unless they alter engine-state transitions.

## Why this boundary exists

The platform/reload work closed stable reload destinations `$00`, `$10` and `$90`, but those values are not all ordinary gameplay states. The fixed-bank bootstrap at `$C180` immediately interprets them before the ordinary frame loop begins. Therefore a global state map is required to understand where the already-promoted reload machinery actually hands control next.

## `$C180` bootstrap

Relevant fixed-bank control flow:

```text
C190  LDA $00
C192  CMP #$10
C194  BEQ C20E
C196  CMP #$90
C198  BEQ C211
...
C1B3  LDA #$20
C1B5  STA $00
C1B7  STA $01
...
C20E  JSR C458
C211  JSR D442
```

`$D442` begins:

```text
D442  INC $00
```

Therefore the promoted reload outcomes feed the global engine as follows:

| reload commit `$00` | bootstrap path | immediate logical successor |
|---:|---|---:|
| `$00` | full bootstrap | `$20` |
| `$10` | short bootstrap through `$D442` | `$11` |
| `$90` | short bootstrap through `$D442` | `$91` |

This is the first global result of the dispatcher audit: `$10` and `$90` are bootstrap entry states, not long-lived ordinary frame states.

## Main-loop mirror semantics

The ordinary frame dispatcher begins after bootstrap at `$C21E`:

```text
C21E  LDA $00
C220  STA $01
C222  CMP #$3D
C224  BNE C229
C226  JMP $E100
```

So main copies live engine state `$00` into frame mirror `$01` before dispatching. `$3D` is then consumed immediately by the promoted reload entry `$E100`.

The mirror matters because NMI does **not** use `$01` uniformly. NMI first checks two latched values from `$01`, then reloads live `$00` for all other logical dispatch.

## Main dispatcher partition

The fixed-bank main dispatcher is structurally equivalent to the following table.

| `$00` state/range | logical main route | classification at this checkpoint |
|---|---|---|
| `$00` | common frame tail `$C3FC` | promoted bootstrap/reload state |
| `$01-$10`, `$12-$14` | bank-1 generic `$9363` | unresolved/global-low family |
| `$11` | dedicated `$C246` body | **reachable unresolved** |
| `$15-$1F` | common tail only | static presence; reachability not proved |
| `$20` | platform body `$C2F9` | promoted |
| `$21-$2F` | common tail only | static presence; reachability not proved |
| `$30-$4F` | shared scene family `$C346` / `$C659` | reachable but not globally promoted |
| `$50-$5F` | common main tail | NMI-owned/special family where reachable |
| `$60-$6F` | shared family `$C364` | unresolved |
| `$70-$7F` | `$C538` family | promoted narrative/post-exit coverage for known reachable states |
| `$80-$8F` | common main tail | NMI-owned for the known narrative sequence |
| `$90` | common loop if seen there, but confirmed as `$C180` bootstrap input | promoted reload bootstrap state |
| `$91` | bank-1 `$9363` | **reachable unresolved** |
| `$92` | dedicated `$C3C3` | unresolved |
| `$93-$98` | bank-1 `$9363` | unresolved |
| `$99+` | common tail at this dispatcher level | reachability/context dependent |
| `$3D` | direct `$E100` reload | promoted |

Important discriminator: the `< $15` test occurs before the high-nibble family logic, and `$11` is carved out as a dedicated case. State `$15` is therefore **not** part of the bank-1 low-state route.

## NMI dispatcher partition

NMI starts at `$D269`.

The first two logical gates use **mirror `$01`**:

```text
D282  LDA $01
D284  CMP #$50
D288  JMP $DABC       ; if equal
...
D290  CMP #$3D
D294  JMP $E000       ; if equal
```

Only after those gates does NMI reload live state:

```text
D297  LDA $00
```

All remaining state cases are selected from live `$00`.

| selector | state/range | NMI route/effect | top-level ownership |
|---|---:|---|---|
| mirror `$01` | `$50` | `$DABC` | NMI-latched |
| mirror `$01` | `$3D` | `$E000` | cooperative reload |
| live `$00` | `$00` | common restore/tail | no independent NMI body |
| live `$00` | `$12` | `$D543`, then `INC $00` + `INC $01` | NMI advances to `$13` |
| live `$00` | `$13` | `$D42D` | cooperative low family |
| live `$00` | `$20` | `$D7F2`, `$D988` | cooperative platform |
| live `$00` | `$34` | `$D73B` | dedicated scene NMI case |
| live `$00` | `$40-$4F` | bank-1 `$8C19` path | cooperative scene family |
| live `$00` | `$60-$6F` | bank-1 `$9D69` path | cooperative family |
| live `$00` | `$70` | `$D3BF` | narrative NMI |
| live `$00` | `$73` | bank-1 `$8C19` path | narrative special case |
| live `$00` | `$80-$8F` | bank-1 `$8C19` path | NMI-owned narrative family |
| live `$00` | `$91` | `$D42D` plus `$07FC=$F0` | cooperative high family |
| live `$00` | `$93` | `$D55E`; routine increments `$00` | `$93->$94` |
| live `$00` | `$94-$96` | `$D571->$D55E`; increment `$00` | `$94->$95->$96->$97` |
| live `$00` | `$98` | `$D53D/$D511`, then `INC $00` | `$98->$99` |
| live `$00` | other | common NMI tail | no top-level special body |

### Why `$50/$3D` are special

The ordering is observable and semantically relevant. If `$01=$50`, NMI takes `$DABC` even if live `$00` has already changed. Likewise `$01=$3D` forces `$E000`. For ordinary states, NMI ignores the mirror after those checks and dispatches on current `$00`.

This is why ORIGINAL SPEC represents the NMI resolver with both `mirror01` and `live00` rather than pretending `$01` is merely a duplicate byte.

## Confirmed writers into unresolved families

This audit indexed state writers only far enough to determine the next reachable boundary. Data-table false positives were rejected; the entries below are executable writers on confirmed control paths.

### Directly from promoted reload destinations

```text
promoted reload $10
 -> $C180 short path
 -> $D442 INC $00
 -> $11

promoted reload $90
 -> $C180 short path
 -> $D442 INC $00
 -> $91
```

Thus `$11` and `$91` are the first unresolved states proved reachable **directly** from an already-closed checkpoint.

### Low `$11-$14` family writers already visible

- `$11`: `$D442 INC $00` from promoted bootstrap state `$10`.
- `$12`: `$C298 LDA #$12 / $C29A STA $00 / $C29C STA $01` inside the dedicated `$11` main body.
- `$13`: NMI `$D2A5 INC $00 / $D2A7 INC $01` when live state is `$12`.
- `$14`: a generic text/state increment writer exists at bank-1 `$8DDB` (`INC $00`, `INC $01`), but its exact reachability from `$13` is deliberately left to the next family-specific checkpoint rather than inferred here.

### High `$91-$99` family writers already visible

- `$91`: `$D442 INC $00` from promoted bootstrap state `$90`.
- `$92`: generic bank-1 text terminator writer `$8DDB` can increment the paired state bytes; exact `$91->$92` call-path reachability remains to be promoted.
- `$93`: main `$C3E8 INC $00` from dedicated state `$92`.
- `$94-$97`: NMI `$D55E/$D56E` increments states `$93-$96`.
- `$98`: bank-1 `$9381 INC $00` is gated specifically by state `$97` after its local counter reaches the terminal value.
- `$99`: NMI `$D365 INC $00` from `$98`.

These writers make the high family a bounded future target, but it is not selected before the lower direct successor `$11` family.

## Other confirmed family-entry writers

Useful global anchors found while auditing `$00` writers:

- `$40`: fixed `$C9A9-$C9AB`.
- `$50`: fixed `$C14B-$C14F`, fixed `$C34F-$C353`, and bank-1 `$8D41-$8D45`.
- `$60`: bank-1 `$92E3-$92E7`.
- `$70`: bank-1 `$96FD-$9701` (already tied to the promoted special platform exit path).
- `$80`: fixed `$C5C3-$C5C5` (already inside the promoted `$70->$80` narrative transition).
- `$3D`: normal exit/reload writers already promoted in the platform/reload checkpoints.

The dispatcher recognizes additional static ranges, especially `$15-$1F`, `$21-$2F` and parts of `$50/$90+`, for which reachability is not established merely by the comparison tree. They remain `STATIC / REACHABILITY UNKNOWN`, not invented game modes.

## Semantic ownership summary

The top-level engine is cooperative rather than a single switch statement:

- **main-owned at top level:** `$11`, `$92`, generic low-state logic and several family bodies;
- **NMI-owned at top level:** mirror `$50`, most `$80-$8F` work, several high-family transitions;
- **cooperative:** `$3D`, `$12/$13`, `$20`, `$40-$4F`, `$60-$6F`, `$70/$73`, `$91`, `$93-$98`;
- **bootstrap-owned:** promoted reload destinations `$00/$10/$90` when `$E100` returns through `$C180`.

## Promoted executable artifact

`EngineStateDispatcherMap` reproduces only this top-level partition:

- `$C180` bootstrap successor selection;
- main-loop state routing;
- NMI mirror-first/live-second routing;
- immediate NMI state increments that are explicit in the dispatcher bodies.

It intentionally does not emulate the target routines.

## Selected next boundary

The next family is:

```text
engine states $11-$14
```

Reason:

1. promoted reload result `$10` reaches `$11` directly and unconditionally through `$C180->$D442`;
2. `$11` has a dedicated main body at `$C246`;
3. that body contains a confirmed writer to `$12`;
4. NMI explicitly advances `$12->$13`;
5. `$14` has a candidate paired-state increment writer whose exact call-path must be proved;
6. the family is small enough to close without opening renderer/audio internals.

The parallel `$90->$91` high family is also directly reachable, but is deferred until `$11-$14` is closed. This is a prioritization decision based on control-flow order and bounded scope, not a claim that `$91` is unreachable.

## Evidence status

- Main dispatcher partition: **CONFIRMED**.
- NMI mirror/live partition: **CONFIRMED**.
- `$00->$20`, `$10->$11`, `$90->$91` bootstrap outcomes: **CONFIRMED**.
- `$11->$12` writer existence: **CONFIRMED**.
- `$12->$13` NMI transition: **CONFIRMED**.
- `$13->$14` reachability through bank-1 `$8DDB`: **OPEN**.
- Meaning/UX of `$11-$14` and `$91-$99`: **OPEN** until family-specific checkpoints.

# Battle-stage context coverage — `$050E=$00-$0B`

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Canonical ROM identity used for this audit:

- size: `262160` bytes;
- SHA-1: `F871D9B3DAFDDCDAD5F2ACD71044292E5169064E`;
- MD5: `3B0F17C2B6EFC928B3D3FE9B1A389680`;
- SHA-256: `6917B31D7343A9A17170E833BACDBC3B1EBA3E02D11C51C0D44DBE436C9AD43A`;
- CRC32: `F8D258A3`.

Status: the numeric battle-stage namespace `$00-$0B` is now fully classified for canonical reachability and stage-local ownership. This is a coverage audit: it does not duplicate generic damage, resource, dodge or technique arithmetic.

## 1. Canonical story provenance

Fixed-bank table `$F016` maps story progress `$067D=$00-$0E` to the current numeric stage/presentation index:

| `$067D` | `$F016[X]` | interpretation for this audit |
|---:|---:|---|
| `$00` | `$00` | Mu / pre-battle repair context |
| `$01` | `$01` | Taurus |
| `$02` | `$02` | Gemini / first Camus branch |
| `$03` | `$03` | Cancer |
| `$04` | `$04` | Leo |
| `$05` | `$05` | Virgo |
| `$06` | `$0F` | non-ordinary story context |
| `$07` | `$06` | Scorpio |
| `$08` | `$10` | non-ordinary story context |
| `$09` | `$07` | Capricorn |
| `$0A` | `$08` | Aquarius |
| `$0B` | `$09` | Pisces |
| `$0C` | `$0C` | final-special rose bridge; already closed separately |
| `$0D` | `$0A` | Saga |
| `$0E` | `$00` | post-Saga platform tail; **not** a Mu battle re-entry |

The important negative result is that **no story-progress value maps to stable stage `$0B`**.

## 2. Dispatcher coverage matrix

The four bank-5 dispatcher families are exact ROM tables:

- initialization: `$97DB` / pointer table `$97E1`;
- Talk: `$9C95` / pointer table `$9C9B`;
- post-Bronze: `$A361` / pointer table `$A367`;
- post-Gold: `$A381` / pointer table `$A387`.

The Gold selector is bank 6 `$9074-$9146`; stages without a dedicated branch fall through `$913E` and choose `slot = $065F & 1`.

| stage | canonical battle progress | init | Talk | post-Bronze | post-Gold | canonical Gold slots | coverage |
|---:|---:|---:|---:|---:|---:|---|---|
| `$00` | `$00` | `$97F7` | `$9CB7` | `$A3A1` | `$A3A1` | none | **material context missing** |
| `$01` | `$01` | `$97F8` | `$9D2C` | `$A3A2` | `$A415` | `0,1` | dedicated context closed |
| `$02` | `$02` | `$981F` | `$9D81` | `$A444` | `$A4CC` | `0,1` | **material context missing** |
| `$03` | `$03` | `$9851` | `$9D96` | `$A50F` | `$A560` | `0,1` | **material context missing** |
| `$04` | `$04` | `$989D` | `$9DD8` | `$A5B3` | `$A63E` | `0,1` | dedicated context closed |
| `$05` | `$05` | `$9A28` | `$9E1B` | `$A661` | `$A7B3` | `0,1,2` | dedicated context closed |
| `$06` | `$07` | `$9ACE` | `$9E51` | `$A7FF` | `$A847` | `0,1` | **material context missing** |
| `$07` | `$09` | `$9ACF` | `$9ED6` | `$A86B` | `$A8D8` | `0,1` | **material context missing** |
| `$08` | `$0A` | `$9B14` | `$9F00` | `$A8FC` | `$A9D3` | `0,1,2` | dedicated context closed |
| `$09` | `$0B` | `$9B5C` | `$9F99` | `$AA57` | `$AAF0` | `0,1,2` | dedicated context closed |
| `$0A` | `$0D` | `$9B5D` | `$9FF4` | `$AB18` | `$AC05` | `0,1,2,3` | dedicated context closed |
| `$0B` | none | `$A960`* | `$9FF4` | `$A3A1` | `$A3A1` | none | structural/transient; no canonical battle |

`*` `$A960` is not an executable intended initializer: the real instruction begins at `$A95F` as `AD 6F 06` (`LDA $066F`), so `$A960` points into the middle of that instruction. Canonical control never dispatches it.

The existing dedicated executable contexts are therefore exactly stages `$01/$04/$05/$08/$09/$0A`. Stage `$0C` is a separately closed non-boss bridge and is deliberately outside this `$00-$0B` ordinary namespace audit.

## 3. Why stage `$0B` is not a missing boss/context

Two independent facts eliminate `$0B` as a canonical stable battle entry.

First, `$F016` contains no `$0B` value. The main story progression can therefore never select `$050E=$0B` as its stable battle stage.

Second, the only immediate `$0B` assignments found in the relevant battle/event code are:

```text
$9B31  LDA #$0B
$9B33  JSR $F2ED

$A16D  LDA #$0B
$A16F  JSR $F2ED
```

`$F2ED` is the temporary presentation-stage loader: it stores A to `$050E`, performs graphics/presentation setup and returns. These uses do not invoke `$97DB/$9C95/$A361/$A381` as stage `$0B` battle dispatch.

The first use belongs to the Aquarius initialization presentation. The later script saves/restores the real stage around another temporary `$0B` presentation. This explains why table membership exists without canonical stage reachability.

The malformed structural init pointer reinforces the conclusion:

```text
$A95F: AD 6F 06    LDA $066F
        ^
$A960 is byte 2 of that instruction
```

Therefore no dedicated `$0B` battle context is required.

## 4. Material gaps proven by handler behavior

### `$00` — Mu / pre-battle repair: first missing context

This is not an ordinary Gold battle, but it is materially stateful and cannot be classified as generic/no-op.

Common battle setup `$A973+` clears `$066F` and `$0670`. Stage init `$97F7` itself is just `RTS`.

The fixed command owners explicitly block the non-Talk surfaces when `$050E==0`:

- attack path `$F057+` -> `$F238`;
- resource allocation `$F041+` -> `$F238`;
- escape path `$F0D3+` -> `$F238`.

Talk remains reachable through `$F0B1 -> $9C91/$9C95 -> $9CB7`.

`$9CB7` is a two-state machine:

```text
$066F == 0
  -> first repair/conversation presentation
  -> $066F = 1
  -> return

$066F != 0
  -> second presentation
  -> $0670 = $01
  -> return
```

Because attack is blocked, `$A3A1` post-Bronze/post-Gold and the structural Gold selector are not canonically reached. Release `$01` joins fixed owner `$E399+`, increments `$067D:00->01`, and `$F016[01]=$01` hands control to Taurus.

This is the **earliest material uncovered stage context** and therefore the next actionable checkpoint.

### `$02` — Gemini / first Camus branch

Material evidence:

- init `$981F` performs stage-specific presentation/reward setup and exits through the shared intro handoff;
- Talk `$9D81` mutates `$066F` and transient `$DC`;
- post-Bronze `$A444` has a distinct `$067C==0` branch that writes engine substate `$02=$0E` and emits release `$02`, while its ordinary branch can emit victory `$01`;
- post-Gold `$A4CC` owns low-player feedback and defeat `$FF`;
- canonical Gold selection falls through to slots `0/1` (its coefficient row happens to duplicate all structural slots, but reachability is still only `0/1`).

This is material and remains to be promoted after `$00`.

### `$03` — Cancer / Death Mask

Material evidence:

- init `$9851` is stage-specific;
- Talk `$9D96` branches on `$067C`; the zero branch writes `$02=$0C`, emits release `$02` and performs a caller unwind, while the other branch raises transient `$DC`;
- post-Bronze `$A50F` owns opponent-low `$064A` and victory `$01`;
- post-Gold `$A560` owns first player-low `$064D` and defeat `$FF`;
- Gold selection is canonical slots `0/1`.

### `$06` — Scorpio / Milo

Material evidence:

- init `$9ACE` is `RTS`, but the context is not generic because the remaining handlers are stage-specific;
- Talk `$9E51` consumes dodge history through `$A1EC`, branches on active Saint, mutates `$068A/$066F`, invokes stage reward logic and can raise transient `$DC`;
- post-Bronze `$A7FF` owns victory `$01` and stage feedback;
- post-Gold `$A847` owns low-player response and defeat `$FF`;
- Gold selector `$908C+` chooses slot `0` after at least two accumulated dodge events, otherwise slot `1`.

### `$07` — Capricorn / Shura

Material evidence:

- init `$9ACF` is a true progression event: unless re-entered with `$0670=$FE`, it performs the stage intro, increments Shiryu technique counts `$058A/$0696`, grants the associated progression reward and exits through the intro handoff;
- Talk `$9ED6` advances `$066F` and later raises transient `$DC`;
- post-Bronze `$A86B` owns `$0690` behavior and the special opponent-defeat sequence ending in release `$FE`;
- fixed release owner `$E3ED+` converts that `$FE` path into the normal progression increment through `$E3B3`;
- post-Gold `$A8D8` owns defeat `$FF`;
- Gold selection is slots `0/1`.

## 5. Gold-slot reachability result

Canonical reachable `$0680` masks are:

```text
stage 00  none
stage 01  0,1
stage 02  0,1
stage 03  0,1
stage 04  0,1
stage 05  0,1,2
stage 06  0,1
stage 07  0,1
stage 08  0,1,2
stage 09  0,1,2
stage 0A  0,1,2,3
stage 0B  none
```

This separates structural coefficient-table slots from actually selectable techniques. In particular, equal coefficient values do not prove slot reachability.

## 6. Executable coverage artifact

`src/SaintSeiyaNesReborn.OriginalSpec/BattleStageContextCoverage.cs` records:

- the four exact dispatcher addresses per stage;
- the full `$F016` progress map;
- canonical battle-entry provenance;
- coverage classification;
- Gold-selector ownership and reachable-slot mask;
- the exact `$00` first-gap contract;
- the structural `$0B` evidence anchors.

`tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/BattleStageContextCoverageChecks.cs` locks those invariants so future work cannot silently skip a stage, turn structural table membership into false reachability, or reopen already-closed contexts.

## 7. Coverage conclusion / next boundary

Audit result:

```text
closed dedicated contexts : 01,04,05,08,09,0A
material uncovered        : 00,02,03,06,07
structural/no battle       : 0B
separate closed bridge     : 0C
```

The next canonical checkpoint is therefore **stage `$00` Mu / pre-battle repair**, not Gemini.

Completion target for that next checkpoint:

> Starting from common reset `$A973+` with canonical story progress `$067D=$00/$050E=$00`, model the command topology that blocks attack/resource/escape, close Talk `$9CB7` across `$066F=0->1`, prove the second-Talk `$0670=$01` release through `$E399/$E3B3` to `$067D=$01/$050E=$01`, and encode the resulting special non-boss context with fixtures without invoking generic Gold damage or selector arithmetic.

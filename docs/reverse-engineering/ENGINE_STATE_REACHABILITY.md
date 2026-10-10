# Global engine-state reachability census

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: **CONFIRMED** complete reachability classification for global engine state `$00` and its mirror `$01` across the canonical PRG control graph and the confirmed CHR-backed executable overlays.

This checkpoint composes the already-promoted engine families. It does not reinterpret their internal gameplay semantics. Its purpose is narrower and global: answer, for every possible byte value, whether the original can actually produce it as global engine state.

## Result

The complete canonical namespace contains **59 produced values** when bootstrap/handoff transients are counted. The other **197 byte values have no executable producer**.

Classification:

| value/range | verdict | owner/reason |
|---|---|---|
| `$00` | bootstrap/handoff transient | stable reload commit, consumed by full bootstrap -> `$20` |
| `$01-$0F` | unreachable | no producer |
| `$10` | bootstrap/handoff transient | front-end/reload commit, short bootstrap -> `$11` |
| `$11-$14` | reachable | closed low/password family |
| `$15-$1F` | unreachable | dispatcher common tail only; no producer after absorbing `$14` |
| `$20` | reachable | platform engine |
| `$21-$2F` | unreachable | no platform/global producer |
| `$30` | bootstrap/handoff transient | front-end attract commit, immediately promoted to `$31` |
| `$31-$38` | reachable | front-end attract/presentation |
| `$39-$3C` | unreachable | `$38` writes `$40` directly |
| `$3D` | bootstrap/handoff transient | real reload bridge to `$E100` |
| `$3E-$3F` | unreachable | no producer |
| `$40-$4D` | reachable | front-end attract text/presentation chain |
| `$4E-$4F` | unreachable | `$4D` writes `$50` directly |
| `$50` | reachable | front-end/title modal shell |
| `$51-$5F` | unreachable | exact `$50` modal graph never increments global state |
| `$60` | reachable | fatal Life/Cosmo resource state |
| `$61-$6F` | unreachable | failure graph produces exact `$60` only and never advances it |
| `$70-$75` | reachable | special post-platform narrative chain |
| `$76-$7F` | unreachable | `$75` writes `$80` directly |
| `$80-$89` | reachable | narrative text chain |
| `$8A-$8F` | unreachable | `$89` exits to `$3D` before another text-state increment |
| `$90` | bootstrap/handoff transient | warm-reload commit, short bootstrap -> `$91` |
| `$91-$99` | reachable | closed high presentation family; `$99` absorbing |
| `$9A-$FF` | unreachable | no producer after absorbing `$99` |

The apparent conflict around `$3D` is now resolved explicitly: PR #117/#119 proved `$39-$3F` are absent from the **front-end attract path**, but `$3D` is independently and repeatedly produced as the global reload bridge. Thus only `$39-$3C/$3E-$3F` are globally dead.

## Canonical binary verification

The private canonical ROM was re-read for this checkpoint and matched all expected hashes:

```text
SHA-1   F871D9B3DAFDDCDAD5F2ACD71044292E5169064E
MD5     3B0F17C2B6EFC928B3D3FE9B1A389680
SHA-256 6917B31D7343A9A17170E833BACDBC3B1EBA3E02D11C51C0D44DBE436C9AD43A
CRC32   F8D258A3
```

The executable-control-flow pass used the same bank-entry seeds already established by `BANK_MAP.md`, augmented with the newly-confirmed bank-0 front-end overlay loader entries `$8000/$8013` from PR #119.

Reachable instruction counts in that conservative recursive graph were:

```text
bank 0: 1021
bank 1: 1894
bank 2: 0 seeded executable instructions
bank 3: 3501
bank 4: 48
bank 5: 392
bank 6: 382
bank 7: 4562
```

Bank 0 is larger than the earlier first-pass count because `$8000/$8013/$8023` are now proven executable rather than unseeded.

### Raw direct-writer audit

Scanning all eight PRG banks for direct 6502 writes/mutations of zero-page `$00/$01` (`STA/STX/STY/INC/DEC`, zero-page or absolute encoding) produced **188 raw byte-pattern candidates**.

Of these:

- 49 lie on the conservative seeded executable graph;
- 5 additional direct writers are independently proven executable through dynamic/inline-routed fixed-bank paths that conservative recursion does not follow cleanly: `$D5DA`, `$DA9D`, `$DA9F`, `$DAAF`, `$DAB1`;
- the remaining **134 raw hits are data, inline parameters, opcode operands, or otherwise non-executable in the canonical graph**.

Representative rejected false positives:

- bank 1 `$97C6` (`STA $01`) lies inside a structured table;
- bank 1 `$A68A` (`DEC $01`) is table data;
- bank 3 `$9419/$943A` are data blocks;
- bank 5 apparent `$9FF9 INC $00` is the high operand byte of the preceding `JSR $E698` crossing a raw pattern boundary;
- bank 6 `$A489 DEC $00` is inside a word/table sequence.

Therefore a byte search alone materially overstates the writer surface.

## Real direct state seeds

The executable constant/global seed writers are fully owned by promoted paths:

| address | target/effect | owner |
|---|---|---|
| bank1 `$8D41` | paired `$50` | terminal front-end attract text path |
| bank1 `$8D4E` | paired `$3D` | narrative/text reload conversion |
| bank1 `$92E3` | paired `$60` | fatal Life/Cosmo underflow |
| bank1 `$96D2` | paired `$3D` | normal accepted platform exit |
| bank1 `$96FD` | live `$70` | special platform exit |
| bank3 `$AAF3` | paired `$00`, then `$C180` | confirmed gameplay reset/bootstrap route |
| fixed `$C14B` | paired `$50` | reset / completed attract front-end entry |
| fixed `$C1B3` | paired `$20` | full bootstrap platform entry |
| fixed `$C298` | paired `$12` | low/password family branch |
| fixed `$C2BF` | paired `$3D` | reload handoff |
| fixed `$C34F` | paired `$50` | Start escape from attract loop |
| fixed `$C5C3` | live `$80` | `$75` narrative handoff |
| fixed `$C9A9` | live `$40` | attract `$38` direct skip over `$39-$3F` |
| fixed `$DA03` | paired `$3D` | reload handoff |
| fixed `$DA9B/$DA9D` | paired `$10` via Y | normal front-end start |
| fixed `$DAAD` | paired `$10` | accepted-password restored start |
| fixed `$E241/$E243` | paired A | closed reload resolver restricts A to `$00/$10/$90` |

No executable constant seed writes any other global-state value.

## Real advancers

The remaining produced values come from guarded increments already owned by promoted state families:

- bank1 `$8DDB/$8DDD`: paired text terminator increment, reachable only on the closed edges `$13->$14`, `$40->$41...$4C->$4D`, `$73->$74`, `$80->$81...$88->$89`, and `$91->$92`; guards intercept `$4D` and `$89` instead of permitting further increments;
- bank1 `$9381`: `$97->$98`;
- fixed `$C3E8`: `$92->$93`;
- fixed `$C542`: `$71->$72`;
- fixed `$C572`: `$72->$73`;
- fixed `$C57D/$C57F`: `$74->$75`;
- fixed `$C6B0`: `$31->$32`;
- fixed `$C6DD`: `$32->$33`;
- fixed `$C70D`: `$33->$34`;
- fixed `$C8B7`: `$35->$36`;
- fixed `$C940`: `$36->$37`;
- fixed `$C984`: `$37->$38`;
- fixed `$D2A5/$D2A7`: `$12->$13`;
- fixed `$D365`: `$98->$99`;
- fixed `$D442`: bootstrap `$10->$11` or `$90->$91` only;
- fixed `$D56E`: `$93->$94->$95->$96->$97` under the already-proven NMI gates;
- fixed `$D78B`: confirmed `$34->$35` and `$70->$71` call contexts;
- fixed `$D5DA`: transient `$30->$31`.

There is **no reachable `DEC $00`** and no unbounded generic increment that can walk into the residual numeric gaps.

## Mirror-only writes do not create states

Several legitimate writes update only `$01`, notably ordinary main synchronization `$C220 STA $01` and platform-specific mirror maintenance. They cannot create a live global state because `$01` is the mirror/latch, not the source of ordinary main dispatch.

The two exceptional NMI mirror selectors (`$01=$50` and `$01=$3D`) are already owned by the front-end and reload models and originate from paired/live paths above.

## Indirect-store audit

The seeded executable PRG graph contains 241 indirect `STA/STX/STY` sites. Their zero-page pointer bases are limited to:

```text
$10, $12, $14, $16, $18, $1C, $24
```

Counts by base are:

```text
$10:   1
$12: 109
$14:   8
$16: 100
$18:  19
$1C:   1
$24:   3
```

These are established copy-buffer, stream, entity/object and renderer pointer domains. None of the promoted call paths resolves one of these indirect stores to CPU addresses `$0000/$0001`.

The potentially most relevant `$10/$11` site is bank0 `$8053 STA ($10),Y`: the same loader explicitly resets the destination to `$0400` before the copy loop, so it writes the executable overlay buffer `$0440-$07FF`, never global state.

No indirect global-state producer was found.

## CHR-backed executable overlay audit

PR #119 proved the only direct PRG transfers into executable RAM `$0400-$07FF` are:

```text
bank0 $8010 JMP $0440
bank0 $8020 JMP $0440
```

Both are fed by helper `$8023`. A whole-PRG callsite scan found exactly two `JSR $8023` sites:

```text
bank0 $800D
bank0 $801D
```

Thus the promoted loader has exactly two executable overlay sources:

```text
CHR31 / PPU $1000-$13BF -> RAM $0440-$07FF  (front-end)
CHR31 / PPU $1400-$17BF -> RAM $0440-$07FF  (password)
```

Recursive disassembly from RAM `$0440` reaches:

```text
front-end overlay: 77 instructions
password overlay:  73 instructions
```

Neither overlay contains an executable write, increment, decrement or shift of `$00/$01`. The password overlay writes modal `$0200=$09`, as promoted by PR #119, but leaves global `$00/$01=$50`.

No third direct `JMP/JSR` from the seeded PRG graph enters `$0400-$07FF`.

## Exact produced set

Bootstrap/handoff transients:

```text
$00, $10, $30, $3D, $90
```

Ordinary reachable states:

```text
$11-$14
$20
$31-$38
$40-$4D
$50
$60
$70-$75
$80-$89
$91-$99
```

Total: **59**.

Everything else in `$00-$FF` is `STRUCTURAL BUT UNREACHABLE` for canonical execution.

## Executable artifact

`EngineStateReachability` provides an exhaustive `Classify(byte)` over all 256 values, semantic ownership for every produced value, and the two confirmed executable-overlay audit records.

`EngineStateReachabilityChecks` verifies:

- the exact 59-value produced set and 197-value complement;
- exact transient set `$00/$10/$30/$3D/$90`;
- representative holes in every residual range;
- the globally reachable `$3D` exception inside the front-end `$39-$3F` hole;
- narrative boundaries `$75->$80` and `$89->$3D`;
- absorbing `$99` and dead `$9A+`;
- structural dispatcher routes for dead values do not imply reachability;
- both confirmed CHR/RAM overlays are non-writers of global state.

## Consequence

The global top-level engine-state namespace is now closed. Future ORIGINAL SPEC work must not invent additional `$00/$01` modes merely because the dispatcher has a default/common-tail path for arbitrary bytes. Reopening this result requires a concrete executable producer missing from this audit, a contradictory trace, or a failing fixture.

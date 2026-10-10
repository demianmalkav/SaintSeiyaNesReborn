# Scene/battle engine-state graph `$30-$4F`

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: **CONFIRMED** structural/global control-flow model for the reachable `$30-$4F` scene/battle family. Existing battle-resource, damage, dodge, technique and stage-event documents remain authoritative for mechanics executed inside the scene; this document owns only the global `$00/$01` state graph.

## Summary

The dispatcher range `$30-$4F` is much sparser than its numeric width suggests.

Canonical reachable set:

```text
$30,
$31,$32,$33,$34,$35,$36,$37,$38,
$40,$41,$42,$43,$44,$45,$46,$47,$48,$49,$4A,$4B,$4C,$4D
```

No canonical producer was found for:

```text
$39-$3F
$4E-$4F
```

The reachable graph is:

```text
state $50 resume result $0200=$06
 -> paired $30
 -> bootstrap increments live $00
 -> dispatch-ready $31

$31 -> $32 -> $33 -> $34
                 NMI  |
                      v
                     $35 -> $36 -> $37 -> $38
                                          |
                                          +-- direct write --> $40

$40 -> $41 -> $42 -> $43 -> $44 -> $45 -> $46
 -> $47 -> $48 -> $49 -> $4A -> $4B -> $4C -> $4D
 -> paired $50
```

Every ordinary `$3x/$4x` main frame polls Start first. Start bit `$10` preempts the local state body and writes paired `$00/$01=$50`.

## 1. Real entry producer: `$50 -> $30 -> $31`

The scene family is not entered by a direct fixed `LDA #$31 / STA $00` writer.

State-$50 main path `$DA5C+` begins with:

```text
$DA5C  LDY #$30
$DA5E  LDA $0200
$DA61  CMP #$06
$DA63  BEQ $DA9D
...
$DA9D  STY $00
$DA9F  STY $01
$DAA1  JMP $C1D0
```

Therefore `$0200=$06` is the canonical scene-resume result that writes paired `$30/$30`.

`$C1D0` rejects only mirror `$01=$10`; for `$30` it reaches:

```text
$C1E2 JSR $D5DA
```

and `$D5DA` begins:

```text
$D5DA INC $00
```

so live state becomes `$31` while mirror remains `$30` momentarily. The same bootstrap continues into ordinary main `$C21E`, where:

```text
$C220 STA $01
```

synchronizes the mirror before dispatch. Thus:

- `$30` is **reachable**, but only as a bootstrap/transitional state;
- the first ordinary dispatch-ready state is paired `$31/$31`.

`$D5DA` also seeds the long scene presentation fields used by subsequent states, including `$3F=$F8`, `$03BB=$F8`, `$03CC=$80`, `$4D/$4E=$10`, etc. Only fields that later gate global state are modeled in the executable graph.

## 2. Shared main router and Start handoff

Fixed main dispatch recognizes both high nibbles:

```text
$C33C AND #$F0
$C33E CMP #$30
$C340 BEQ $C346
$C342 CMP #$40
$C344 BNE ...
```

At `$C346`, controller input is read. Start is bit `$10` in `$3D`:

```text
$C349 LDA #$10
$C34B BIT $3D
$C34D BEQ $C35A
$C34F LDA #$50
$C351 STA $00
$C353 STA $01
$C355 LDA #$05
$C357 JMP $DA15
```

So any active `$3x/$4x` state can hand off to the exact state `$50` before its local `$C659` logic runs.

This document records the handoff but does not yet promote the complete `$50` modal subsystem.

## 3. Early chain `$31->$34`

### `$31 -> $32`

`$C659` detects state `$31`. `$C668` decrements `$3F`; the only global writer is:

```text
$C6A1 LDA $3F
$C6A3 BNE ...
...
$C6B0 INC $00
```

Therefore `$31` advances to `$32` exactly when post-decrement `$3F==0`.

Only live `$00` increments. `$01` remains `$31` until the next main `$C220` sync.

### `$32 -> $33`

State `$32` executes the first scene-object step at `$C6BE+`. After `$C710`, field 1 of the pointed scene object is read:

```text
$C6D6 LDA ($16),Y
$C6D8 BNE ...
$C6DA JSR $A647
$C6DD INC $00
```

A zero post-step field advances live state to `$33`.

### `$33 -> $34`

State `$33` reuses the second scene-object step at `$C6EE+`:

```text
$C706 LDA ($16),Y
$C708 BNE $C70F
$C70A JSR $A647
$C70D INC $00
```

Again, zero post-step field 1 advances only live `$00`, now to `$34`.

The `$32->$33` and `$33->$34` bodies are adjacent and can chain within one main invocation if both terminal conditions happen to be satisfied. The structural graph remains sequential.

## 4. `$34` is one-NMI transitional

NMI dispatcher has an exact special case:

```text
$D2C3 CMP #$34
$D2C5 BNE ...
$D2C7 JSR $D73B
```

`$D73B` performs scene setup/presentation and seeds:

```text
$03BC=$80
$40=$80
$03BB=$F8
$3F=$F8
$03CC=$00
$03CB=$20
$03D1=$43
```

then:

```text
$D78B INC $00
```

So the first observing NMI advances live `$34->$35`; mirror is untouched until the next main sync.

## 5. `$35 -> $36`

State `$35` runs the presentation path `$C7A8->$C812+`. Its state writer is:

```text
$C8B0 LDA $03BB
$C8B3 CMP #$65
$C8B5 BNE ...
$C8B7 INC $00
```

Therefore global advance occurs exactly when the observed `$03BB` value is `$65`.

The surrounding routines update sprites/scroll/effects and compose with battle presentation, but they do not create another global-state edge.

## 6. `$36 -> $37`

State `$36` is selected at `$C8D4`. It decrements `$3F`:

```text
$C8F4 DEC $3F
```

and later tests the post-decrement value:

```text
$C934 CMP #$44
$C936 BNE ...
...
$C940 INC $00
$C942 LDA #$10
$C944 STA $57
```

Thus post-decrement `$3F=$44` advances live `$36->$37` and seeds `$57=$10`.

## 7. `$37 -> $38`

State `$37` first drains `$57`:

```text
$C961 LDA $57
$C963 BEQ $C96E
$C965 DEC $57
```

Once `$57==0`, it checks `$03CC` before incrementing it:

```text
$C96E LDA $03CC
$C971 CMP #$88
$C973 BCS $C984
$C975 INC $03CC
...
$C984 INC $00
```

Consequences:

- `$03CC=$87` becomes `$88` but remains state `$37` for that frame;
- a later zero-timer frame entering with `$03CC >= $88` advances live `$00` to `$38`.

The transition also seeds:

```text
$03CA=$00
$42=$40
$4D/$4E=$10
```

## 8. `$38` writes `$40` directly

State `$38` increments `$3F` every main frame:

```text
$C9A1 INC $3F
$C9A3 LDA $3F
$C9A5 CMP #$60
$C9A7 BCC ...
```

At threshold:

```text
$C9A9 LDA #$40
$C9AB STA $00
$C9AD LDA #$20
$C9AF STA $4D
$C9B1 STA $4E
$C9B3 LDA #$03
$C9B5 STA $57
```

This proves two facts:

1. `$38` does **not** increment into `$39`; it writes `$40` directly.
2. `$39-$3F` have no producer in the canonical graph.

Only live `$00` is written, so mirror remains `$38` until the next main sync.

## 9. NMI text chain `$40-$4C`

NMI recognizes the entire `$4x` high nibble:

```text
$D2CD AND #$F0
$D2CF CMP #$40
$D2D1 BNE ...
$D2D3 map bank 1
$D2D8 JSR $8C19
```

Bank-1 `$8C19` owns the text/presentation countdown. It first drains `$57`; when `$57` is zero it drains `$26`. Once local delays allow text consumption, states below `$4D` select text pointers through `$9100` indexed by `state-$40`.

Confirmed pointer entries:

| State | source pointer |
|---:|---:|
| `$40` | `$8DF3` |
| `$41` | `$8E1A` |
| `$42` | `$8E35` |
| `$43` | `$8E60` |
| `$44` | `$8E89` |
| `$45` | `$8EB5` |
| `$46` | `$8EE2` |
| `$47` | `$8EF6` |
| `$48` | `$8F19` |
| `$49` | `$8F39` |
| `$4A` | `$8F52` |
| `$4B` | `$8F7B` |
| `$4C` | `$8F8D` |

The shared text interpreter reaches:

```text
$8D93 CMP #$FF
$8D95 BEQ $8DDB
...
$8DDB INC $00
$8DDD INC $01
$8DDF LDA #$80
$8DE1 STA $57
$8DE5 STA $26
$8DE8 STA $27   ; X=0 here
```

Thus each terminal `$FF` advances **both** state bytes:

```text
$40->$41->$42->$43->$44->$45->$46->$47->$48->$49->$4A->$4B->$4C->$4D
```

and seeds `$57=$80`, `$26=$80`, `$27=0` for the next state.

## 10. `$4D -> $50`; `$4E/$4F` unreachable

When `$57==0` and decrementing `$26` reaches zero, `$8D2F` classifies the live state before text-pointer selection:

```text
$8D3D CMP #$4D
$8D3F BCC $8D57
$8D41 LDA #$50
$8D43 STA $00
$8D45 STA $01
```

Therefore canonical `$4D` never consumes the nominal table entry at `$9100+0x1A` (which is `$0600` in another shared-text use). It exits directly to paired `$50` after the local delay expires.

This also proves canonical `$4D` cannot advance to `$4E`; with no other executable producer found, `$4E/$4F` are statically dispatchable but unreachable in the closed scene graph.

## 11. Reachability classification

| State/range | classification | reason |
|---|---|---|
| `$30` | reachable transitional | written by `$DA9D`, immediately bootstrapped by `$D5DA` |
| `$31-$33` | reachable main-owned | counter/object terminal gates |
| `$34` | reachable NMI-transitional | `$D2C7->$D73B->$D78B` |
| `$35-$38` | reachable main-owned | presentation counters/timers |
| `$39-$3F` | structurally routed, unreachable | `$38` writes `$40` directly; no producer found |
| `$40-$4C` | reachable cooperative NMI/text | bank-1 `$8C19`, `$FF->$8DDB` |
| `$4D` | reachable terminal handoff state | local NMI delay -> paired `$50` |
| `$4E-$4F` | structurally routed, unreachable | `$4D` exits to `$50`; no producer found |
| `$50` | next-family handoff | Start preemption and `$4D` completion both enter it |

## 12. Composition with existing battle specifications

This graph does **not** replace existing battle documents. In particular:

- `BATTLE_EVENT_DISPATCH.md` owns stage-indexed event handlers;
- `BOSS_BATTLE_RESOURCES.md` owns battle resource semantics;
- `BOSS_BATTLE_DAMAGE.md` owns battle damage;
- `BOSS_DODGE.md` owns dodge behavior;
- `BATTLE_TECHNIQUES.md` owns technique selection/effects.

The new result is the outer engine-state scaffold that hosts or surrounds those mechanics.

## 13. Executable artifact

`SceneBattleEngineStateGraph` promotes:

- exact reachable state set;
- `$50/$0200=$06 -> transient $30 -> dispatch-ready $31`;
- Start preemption to paired `$50`;
- conditions for `$31->$32->$33->$34`;
- NMI `$34->$35`;
- exact `$35->$36`, `$36->$37`, `$37->$38`, `$38->$40` gates;
- paired text advances `$40-$4C`;
- delayed `$4D->$50` handoff;
- explicit exclusion of `$39-$3F/$4E-$4F` from canonical reachability.

Renderer/PPU/audio details remain deliberately outside this structural checkpoint.

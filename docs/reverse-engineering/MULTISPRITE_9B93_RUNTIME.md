# `$9B93` multisprite — persistent top-level dispatcher

Status: **CONFIRMED by direct canonical-ROM dispatch flow and clean-room composition of already-promoted branches**.

The independent multisprite class rooted at visual `$07E0` / logical `$03FB` is now represented by one persistent runtime. This compositor does not invent a new behavior layer; it reproduces the actual routing between the previously isolated bootstrap, normal, flag, terminal and substate-`$0D` primitives.

## One call begins at `$9B93`

Every update first enters the bootstrap/active gate.

If all four visual sprite bytes are `$FE`, bootstrap may:

- return through selector-zero behavior;
- decrement timed `$03FA`;
- initialize an ordinary object;
- initialize the dedicated substate `$0D` object.

A newly initialized object reaches RTS at `$9CAB`. It **does not** immediately execute `$9CAC+` in the same call.

If any visual sprite is active, the early gate transfers into `$9CAC` and the active dispatcher runs.

## Active dispatcher order

Direct bytes establish this precedence:

```text
$9CBB  LDA $02
$9CBD  CMP #$0D
$9CBF  BNE ...
$9CC1  JMP $A12A
```

So substate `$0D` wins before the generic flag dispatcher.

For all other substates, part0 flags are tested:

```text
flag $08 clear -> normal path near $9D05
flag $08 set   -> JMP $9ED1
```

Both non-`$0D` dispatch families test logical action before ordinary branch work:

```text
(action & $F0) == $D0/$E0 -> $A06E
```

Thus the semantic routing is:

```text
if substate == $0D:
    PlatformMultisprite9B93Substate0D
else if action family is $D0 or $E0:
    PlatformMultisprite9B93DeathDrop
else if part0 flag $08 is clear:
    PlatformMultisprite9B93NormalUpdate
else if part0 flag $04 is set:
    PlatformMultisprite9B93Flag08Bit04
else:
    PlatformMultisprite9B93Flag08Clear04
```

The ordering is significant. For example, substate `$0D` with logical `$D0` must use its dedicated `$A20D` death path rather than `$A06E`, and a non-`$0D` `$D0` record must reach `$A06E` even if its visual flags still contain `$08/$04`.

## Persistent state

`PlatformMultisprite9B93PersistentState` carries:

- full visual/logical runtime state;
- mode `$81`;
- timed spawn cooldown `$03FA`;
- global profile/control byte `$03A9`.

Bootstrap writes are merged into the existing logical record rather than constructing a new record from defaults. This preserves fields that the ROM does not write during initialization, including logical X/Y, field `+$05`, and type `+$09`.

The bootstrap's explicit `Global03A9WasWritten` signal prevents early-return paths from accidentally zeroing the persistent global.

### Bootstrap state mutation rules

- `ExistingActive`: persistent bootstrap fields are untouched before active routing.
- selector zero: no persistent field changes.
- timed cooldown: `$03FA` decrements and `$81=0` persists; logical state and `$03A9` remain unchanged.
- ordinary initialization: visual/profile/phase/mode/cooldown are committed; action is cleared only when the exact bootstrap path reaches the action-clear epilogue; `$03A9` is updated.
- dedicated `$0D` initialization: dedicated part0/profile writes are committed, `$81=0`, `$03FA=$80`, but `$03A9` is preserved because `$A0E4` bypasses that write.

## Shared-state carry

The top-level result always exposes the selected branch's final:

- player attack-object state;
- `$76/$7F/$80` contact/drain state;
- Seventh Sense accumulator.

Routes without projectile/contact work preserve those values unchanged. The flag-`$08`/clear-`$04` route carries its post-movement contact state while leaving attack objects and Seventh Sense untouched, matching its lack of `$9915`.

## Scope boundary

This closes `$9B93` as an independently step-able persistent class. It does **not yet** place the class into the complete main-thread late-object order.

The next composition boundary is:

```text
$9B93 multisprite
 -> auxiliary hazard A/B ($96B4/$9761 family)
 -> primary entity A/B ($A442)
 -> attack-object late update ($A22C)
```

That integration must preserve the same attack/contact/Seventh-Sense carry already exposed by this runtime.

No ROM payload is committed.

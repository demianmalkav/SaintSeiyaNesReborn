# RAM map — current static pass

Target: verified Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM documented in `CANONICAL_ROM.md`.

This file distinguishes what the original code itself proves from semantic names still awaiting runtime confirmation.

## CONFIRMED structures

### `$0059-$0062` — five 2-byte numeric slots

The code indexes this range as `2 * $03`, so it is structurally an array of five two-byte values. Arithmetic routines treat the two bytes as decimal digits packed into nibbles and perform explicit decimal correction after subtraction.

External cheat documentation identifies these values as the five Saints' Cosmo values. Static code strongly agrees with that interpretation.

Provisional semantic name: `saint_cosmo_bcd[5]`.

Evidence anchors:

- bank 1 `$9299+` selects `2 * $03` and subtracts two units from the `$59/$5A` pair with decimal-nibble correction;
- bank 1 `$951F` snapshots all five pairs into `$058C-$05A4`;
- bank 1 `$9720` restores them;
- UI code in bank 1 reads the same array for display.

### `$0063-$006C` — five 2-byte numeric slots

Same five-character / two-byte layout as `$0059-$0062`. The game performs packed-decimal arithmetic and converts the current character's value for gameplay use.

External cheat documentation identifies these values as Life/Energy. Static behavior supports that interpretation.

Provisional semantic name: `saint_life_bcd[5]`.

Evidence anchors:

- bank 1 `$8616` uses `2 * $03` and reads `$63/$64` for the selected Saint;
- `$9311+` decrements the pair with manual BCD correction;
- `$951F` snapshots all five values;
- `$9720` restores them.

### `$006D-$0071` — five one-byte per-Saint values

`$951F/$9720` snapshot and restore one byte per Saint alongside Cosmo and Life. Display code consumes their nibbles as quantities when constructing the stat UI, so this is not merely an arbitrary auxiliary byte.

Exact meaning is still unresolved.

Name remains `saint_unknown_6d[5]`.

### `$0076` — timer-like gameplay state

Initialized to zero by bank 1 `$98BA`. Collision/gameplay paths set it to `$20` and other code tests it while processing interactions.

Community cheats call this invulnerability/flashing state. Static behavior proves that it is a countdown/cooldown-like state associated with interaction handling; exact visible semantics still need runtime confirmation.

Provisional semantic name: `invulnerability_timer`.

### `$020A` — controller held-state byte

Bank 0 `$8B1F` strobes `$4016`, shifts eight controller reads into `$020A`, and retains the resulting current-button bitfield.

Provisional semantic name: `menu_input_held`.

### `$020B` — controller newly-pressed byte

The same routine computes `(current XOR previous) AND current` and stores it in `$020B`.

Provisional semantic name: `menu_input_pressed`.

### `$0203-$0204` — monotonically incremented 16-bit counter

Incremented once at the end of the bank-0 controller polling routine, with carry from `$0203` to `$0204`.

Likely a frame/update counter, but runtime cadence should be confirmed before assigning the final name.

### `$3D/$3E` — platform controller held state

Fixed-bank `$C4E4` reads controllers 1 and 2 into these bytes. For `$3D` the bit layout is:

- `$80` A
- `$40` B
- `$20` Select
- `$10` Start
- `$08` Up
- `$04` Down
- `$02` Left
- `$01` Right

`$3D` is consumed directly by bank-3 platform movement and attack logic.

### `$03` — current Saint index

Multiple routines use `$03`, double it, and index the five-entry Cosmo/Life arrays. Fixed-bank table `$E505` also assigns values including `0,2,1,3,4` according to game state.

Structurally, valid values `0..4` select one of five active Saint slots.

Provisional semantic name: `current_saint_index`.

The exact identity/order of all five indices is still being mapped; do not attach character names yet.

### `$058C-$05A4` — 25-byte Saint-stat snapshot

Bank 1 `$951F` copies all five Saints' active stat records into five 5-byte snapshot records, and `$9720` restores them.

The copy order is not simply linear in zero page:

- snapshot slot 0 `$058C-$0590` <- `$59,$5A,$63,$64,$6D`
- slot 1 `$0591-$0595` <- `$5D,$5E,$67,$68,$6F`
- slot 2 `$0596-$059A` <- `$5B,$5C,$65,$66,$6E`
- slot 3 `$059B-$059F` <- `$5F,$60,$69,$6A,$70`
- slot 4 `$05A0-$05A4` <- `$61,$62,$6B,$6C,$71`

This is a persistent/snapshot representation distinct from the active zero-page arrays.

**Password consequence:** only snapshot slots 0-3 (`$058C-$059F`) are serialized into the 31-character password. Slot 4 (`$05A0-$05A4`) is deliberately excluded. This proves the game distinguishes four password-persistent Saint records from a fifth active but non-persistent slot. The fifth character's identity remains intentionally unnamed until the selection/progression mapping proves it.

### `$05AA-$05AB` — Seventh Sense packed-decimal value

The fixed bank extracts four decimal nibbles from these two bytes and converts them into display tiles. `$F31E` performs packed-decimal addition and clamps at `$99` where appropriate.

The public 9999 password decodes through the reconstructed password algorithm to `$05AA=$99`, `$05AB=$99`, independently validating the community identification as Seventh Sense.

Provisional semantic name: `seventh_sense_bcd`.

### `$0700-$07FF` — OAM shadow page

NMI writes `$07` to `$4014`, causing sprite DMA from page `$0700`. Multiple routines also write sprite-related values throughout this page.

## Mapper/NMI coordination

### `$3A`

NMI writes `1` here before resetting the MMC1 shift register. Interrupt-safe MMC1 writers clear/test it and retry a five-write serial transaction if an NMI occurred during the transaction.

Provisional name: `mmc1_write_interrupted`.

### `$3B`

Used by the protected PRG-bank writer and reloaded by NMI before returning. This tracks the PRG bank that should be restored after NMI performs temporary bank switches.

Provisional name: `persistent_prg_bank`.

### `$0639/$063A/$063E/$063F`

A second mapper-coordination path around `$E589/$E5B7` uses these bytes to coordinate bank requests and critical sections with an alternate NMI path.

Provisional roles:

- `$0639`: requested/persistent bank for synchronized path;
- `$063A`: transient bank value;
- `$063E/$063F`: mapper-write critical-section markers.

## High-level state candidates

### `$00/$01`

RESET initializes both to `$50`. NMI dispatches large branches of engine behavior from these values, including exact states and state families (`$20`, `$40`, `$60`, `$70`, `$80`, `$90` ranges).

They are confirmed as high-level engine/state-machine bytes; their division of responsibility remains unknown.

### `$0200/$0201`

The main initializer at `$DA13+` feeds these bytes into bank-0 inline jump-table dispatchers. They are confirmed scene/substate dispatcher indices; exact scene names remain to be assigned.

### `$050E`

Banks 4 and 5 use this heavily as an index into scenario-dependent tables. Strong candidate for current scenario/temple identifier.

Status: `INFERRED`.

### `$0533`

Fixed-bank code indexes table `$E505` with `$0533` and writes the result to current Saint index `$03`. Therefore `$0533` is a character/progression selector that influences which Saint becomes active.

Status: structure confirmed; semantic label pending.

### `$067D`

Incremented when a progression/event sequence completes, compared against `$0C`, serialized in the password pipeline, and restored when continuing.

This is confirmed persistent progression state. Exact numbering-to-temple mapping is still being reconstructed.

Provisional name: `story_progress_index`.

### `$06CD`

Derived from progression tables, serialized/restored alongside `$067D`, and used to construct later event state.

Confirmed persistent progression descriptor; exact semantic meaning pending.

## Password serialization — CONFIRMED

The earlier serialization hypothesis is now proven end-to-end. Fixed-bank `$C458` stages durable state, bank 0 packs it, generates 31 six-bit symbols with XOR checksum and a 10-symbol XOR-`$3F` obfuscation window, and the inverse decoder restores the state.

Staging:

- `$058C-$059F` -> `$0110-$0123` (four 5-byte Saint records)
- `$05AA-$05AC` -> `$0130-$0132`
- `$067D` -> `$0133`
- `$06CD` -> `$0134`
- `$0587-$058A` -> `$0135-$0138`

Detailed format is documented in `PASSWORD_SYSTEM.md`, with a clean-room decoder in `tools/password/password_codec.py`.

The public 999 password is a validated fixture: the clean-room decoder produces Life 999, Cosmo 999 for all four persistent Saint records and Seventh Sense 9999.

## Platform/player state

See `PLATFORM_PLAYER.md` for the first movement skeleton. Important current addresses:

- `$3F`: player horizontal coordinate;
- `$40`: principal vertical coordinate/motion component;
- `$42`: facing/direction;
- `$44/$45`: horizontal scroll/camera pair;
- `$4D/$4E`: action/animation state family;
- `$4F-$56`: collision/environment sample block (spatial meanings pending);
- `$0387-$0389`: movement increment candidates.

## External battle-mode candidates not yet promoted

Community cheats identify `$05BC/$05BD` as battle Cosmo and `$05CE/$05CF` as battle Life, with `$05BE` / `$05D0` related maxima. These have not yet been statically tied to sufficient callers and remain `INFERRED` pending tracing.

## Next tests

1. Resolve current-Saint indices 0..4 to character identities without assumption.
2. Assign spatial meaning to `$4F-$56` collision samples.
3. Enumerate `$4D/$4E` action states.
4. Trace `$067D/$06CD/$050E/$0533` across the Twelve Houses progression.
5. Map `$05BC-$05D0` battle structures and reconcile them with the five active Saint slots.
6. Add dynamic watches/breakpoints when a debugger-capable emulator becomes available in the workflow.

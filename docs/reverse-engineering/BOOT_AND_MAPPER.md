# Boot and MMC1 — initial static analysis

Status: static analysis against the verified Japanese ROM. Labels below are promoted only where the code path is unambiguous.

## Vectors

The vectors are in the final 16 KiB PRG bank:

- NMI vector: `$C000`
- RESET vector: `$C100`
- IRQ/BRK vector: `$C003`

`$C000` is a trampoline to the NMI body at `$D269`. `$C003` is an `RTI`.

## RESET sequence

At `$C100`, the game:

1. executes `SEI` and resets MMC1 serial state with an RMW write affecting `$FFFF`;
2. disables decimal mode;
3. disables PPU rendering/NMI and DMC;
4. initializes the stack to `$01FF`;
5. waits across PPU status/VBlank transitions;
6. calls the MMC1 control-register helper with `$1E`;
7. disables APU channels and configures frame counter;
8. clears RAM `$0000-$06FF`;
9. initializes state bytes `$00/$01` to `$50`;
10. transfers control to the main initializer at `$DA13`.

Page `$07xx` is not part of this clearing loop. NMI later proves that the page is used as the OAM shadow buffer because `$4014` is loaded with `$07` every frame/update where the NMI path runs.

## MMC1 control state

RESET writes `$1E` to the MMC1 control register.

Interpreting standard MMC1 control bits:

- mirroring = vertical (`10b`);
- PRG banking mode = mode 3: switch 16 KiB at `$8000-$BFFF`, fix last bank at `$C000-$FFFF`;
- CHR banking mode = 4 KiB banks.

Therefore the iNES header mirroring bit is not the final runtime mirroring configuration.

## Confirmed serial-write helpers in fixed bank

The fixed bank contains compact 5-bit MMC1 serial writers:

- `$C05A`: MMC1 control register (`$8000-$9FFF`)
- `$C078`: CHR bank 0 register (`$A000-$BFFF`; implementation writes inside that window)
- `$C096`: CHR bank 1 register (`$C000-$DFFF`)
- `$C0B4`: PRG bank register (`$E000-$FFFF`)

There are also protected serial writers, most importantly `$C035` for the PRG bank. They are not redundant copies: they solve the MMC1/NMI race.

## MMC1/NMI race handling

MMC1 needs five serial writes to complete a register update. An NMI in the middle of those writes would corrupt the transaction unless the program coordinates with the interrupt handler.

The game does the following:

- a protected mapper writer clears `$3A` before beginning;
- it resets the MMC1 shift register with `INC $FFFF`;
- it emits the five serial bits;
- NMI sets `$3A = 1` and itself resets the MMC1 shift register with `INC $FFFF`;
- after the five writes, the protected writer checks `$3A` and retries if NMI interrupted the transaction.

`INC $FFFF` is intentional: because `$FFFF` lies in ROM, the read-modify-write bus cycle supplies a value whose high bit resets the MMC1 serial latch.

`$3B` stores the persistent PRG bank. NMI is free to use raw temporary bank changes and restores `$3B` before returning.

Current provisional names:

- `$3A` -> `mmc1_write_interrupted`
- `$3B` -> `persistent_prg_bank`

This distinction gives us two categories of bank change:

1. **raw/temporary switches**, normally used inside a controlled sequence or NMI;
2. **persistent/protected switches**, which survive NMI and become the bank restored at interrupt exit.

## NMI

`$C000` jumps to `$D269`.

The NMI body:

- saves A/X/Y;
- writes `1` to `$3A`;
- resets the MMC1 serial latch;
- reads PPU status;
- resets OAM address and performs OAM DMA from `$0700-$07FF`;
- dispatches behavior based heavily on state bytes `$00/$01`;
- temporarily maps PRG bank 1 or 3 for several state handlers;
- maintains PPU control/mask from RAM mirrors `$77/$78`;
- writes scroll-related values from `$44/$46`;
- restores the persistent PRG bank from `$3B`;
- restores Y/X/A and returns with `RTI`.

This confirms that `$00/$01` are high-level engine/state-machine values rather than ordinary gameplay stats.

## Banked entry points already reached from fixed code

Static flow following has produced definite cross-bank entry anchors.

### PRG bank 0

Known entries include `$8960`, `$8B04`, `$8B1F`, `$8B50`, `$AE18`, `$B3E2`.

`$8960` is a compact jump-table dispatcher: it converts A into a word index, consumes the caller return address to locate an inline pointer table, then jumps indirectly through zero page.

`$8B1F` is the controller reader and stores held/newly-pressed state at `$020A/$020B`.

### PRG bank 1

Known entries include `$8000`, `$8616`, `$8841`, `$8C19`, `$8D5A`, `$924D`, `$9363`, `$951F`, `$95FB`, `$969D`, `$9720`, `$98BA`, `$9915`, `$9D69`.

This bank already exposes significant player-stat and gameplay logic.

### PRG bank 3

Known entries include `$96B4`, `$9761`, `$9B93`, `$A22C`, `$A442`, `$AAE4`, `$B94B`.

### PRG bank 2

The fixed bank contains at least one sequence that calls bank 1 and then performs a persistent switch back to bank 2 before returning. This proves bank 2 is a normal long-lived gameplay bank in at least one mode, but its entry graph is not yet mapped.

Banks 4-6 likewise remain unmapped; absence from the present recursive seed set must not be interpreted as absence of executable code.

## Main initializer

At `$DA13` the fixed bank:

- clears `$0200`;
- resets a structured subsystem through `$DB9C`;
- clears `$0201`;
- maps PRG bank 0;
- resets the stack;
- disables PPU rendering through bank-0 code;
- polls the controller;
- clears the newly-pressed byte;
- feeds `$0201` to the bank-0 jump-table dispatcher.

`$0200/$0201` are therefore additional high-level dispatcher indices/state values. Exact semantics remain under investigation.

## Structured records at `$0440+`

`$DB9C` initializes records spaced `$15` bytes apart beginning at `$0440`, while `$DBB6` selects and initializes one of those records from fixed-bank tables. Bank-0 update code also iterates fields at `$0440+X`, `$0441+X`, etc.

This is a real record/slot system, but whether the slots represent entities, sound channels, effects, or a mixed scheduler has not yet been proven. It remains intentionally unnamed.

## Next verification targets

- map bank 2 and discover seeds for banks 4-6;
- assign semantics to the `$0440` record system;
- correlate `$00/$01/$0200/$0201` with title, transition, platforming and boss modes;
- trace persistent bank `$3B` through those modes;
- map CHR-bank values by mode;
- begin dynamic confirmation once a debugger-capable emulator is available in the workflow.

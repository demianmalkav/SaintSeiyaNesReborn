# Boot and MMC1 — initial static analysis

Status: early static analysis against the verified Japanese ROM. Labels below are promoted only where the code path is unambiguous.

## Vectors

The vectors are in the final 16 KiB PRG bank:

- NMI vector: `$C000`
- RESET vector: `$C100`
- IRQ/BRK vector: `$C003`

`$C000` is a trampoline to the NMI body at `$D269`. `$C003` is an `RTI`.

## RESET sequence

At `$C100`, the game:

1. executes `SEI` and resets MMC1 serial state with a write affecting `$FFFF`;
2. disables decimal mode;
3. disables PPU rendering/NMI and DMC;
4. initializes the stack to `$01FF`;
5. waits across PPU status/VBlank transitions;
6. calls the MMC1 control-register helper with `$1E`;
7. disables APU channels and configures frame counter;
8. clears RAM `$0000-$06FF`;
9. initializes state bytes `$00/$01` to `$50`;
10. transfers control to the main initializer at `$DA13`.

The RAM clear stopping before page `$07xx` is noteworthy because page 7 is then free to hold persistent engine state that survives this particular clearing loop or is managed separately.

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
- `$C078`: CHR bank 0 register (`$A000-$BFFF`; implementation writes `$BFFF`)
- `$C096`: CHR bank 1 register (`$C000-$DFFF`)
- `$C0B4`: PRG bank register (`$E000-$FFFF`)

Additional routines at `$C010` and `$C035` also perform serial MMC1 writes while preserving state and appear to be used from interrupt-sensitive paths; their exact semantic distinction remains under analysis.

## NMI

`$C000` jumps to `$D269`.

The NMI body:

- saves A/X/Y;
- marks an interrupt/bank-write coordination flag at `$3A`;
- performs OAM DMA from page `$07` (`$4014 <- $07`);
- dispatches behavior based heavily on state bytes `$00/$01`;
- temporarily switches PRG bank 1 or 3 for several state handlers;
- updates PPU control from `$77`;
- later restores registers and returns with `RTI`.

This is strong evidence that `$00/$01` are high-level engine/state-machine values rather than ordinary gameplay stats.

## Next verification targets

- enumerate every callsite to the MMC1 helpers and assign bank semantics;
- identify all values written to PRG/CHR registers during title, character select, platforming and boss combat;
- trace `$00/$01`, `$3A/$3B`, `$77` and `$041B` dynamically;
- map entry points in switchable banks reached from fixed-bank trampolines.

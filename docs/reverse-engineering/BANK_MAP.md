# PRG bank map — first static role pass

Target geometry: 8 × 16 KiB PRG banks. MMC1 mode fixes bank 7 at `$C000-$FFFF` and switches one 16 KiB bank at `$8000-$BFFF`.

This is a role map, not a claim that each bank contains only one kind of content.

| Bank | Static status | Current role evidence |
|---|---|---|
| 0 | executable confirmed | generic helpers, controller input, object/record updater, jump-table dispatcher |
| 1 | executable confirmed | active Saint stats, BCD Life/Cosmo arithmetic, gameplay/event helpers, stat snapshot/restore |
| 2 | data-dominant; executable entry not found | begins with dense table-like data; selected persistently by gameplay setup; likely map/resource data |
| 3 | executable confirmed, mixed | many gameplay routines reached from fixed bank; frequent temporary mapping |
| 4 | mixed, executable confirmed | scenario/temple-dependent table expansion and event/resource setup |
| 5 | executable/data confirmed | progression/event logic and large scenario-dependent tables |
| 6 | executable/data confirmed | heavy PPU/nametable/sprite transfer logic and display/resource processing |
| 7 | fixed executable/data confirmed | RESET/NMI/IRQ, mapper coordination, global dispatch, progression, common rendering/event infrastructure |

## Bank 0 anchors

Known callable anchors include `$8960`, `$8B04`, `$8B1F`, `$8B50`, `$AE18`, `$B3E2`.

Notable functions:

- `$8960`: inline jump-table dispatcher using the caller's return address;
- `$8B1F`: controller polling, producing held/newly-pressed bytes;
- `$8B50+`: iterates the `$0440+` structured record area.

## Bank 1 anchors

Known anchors include `$8000`, `$8616`, `$8841`, `$8C19`, `$8D5A`, `$924D`, `$9363`, `$951F`, `$95FB`, `$969D`, `$9720`, `$98BA`, `$9915`, `$9D69`.

Notable discoveries:

- `$59-$62` and `$63-$6C` are five-entry two-byte packed-decimal arrays;
- `$951F/$9720` snapshot/restore the five Saints' active numeric state;
- `$98BA` clears a large group of gameplay temporaries including `$76`.

## Bank 2

A persistent mapper switch to bank 2 is performed after gameplay initialization, but the bank begins with long table-like byte sequences and contains very few plausible control-flow signatures compared with the code-heavy banks.

Current conclusion: **data bank is the leading hypothesis**. Do not invent code entrypoints merely because the bank is persistently mapped. Fixed-bank and other-bank code can read resource data from `$8000-$BFFF` while bank 2 is selected.

Status remains `INFERRED` until every bank-2 use site is catalogued.

## Bank 3

Known anchors include `$96B4`, `$9761`, `$9B93`, `$A22C`, `$A442`, `$AAE4`, `$B94B`.

The fixed engine maps bank 3 repeatedly around gameplay processing. It is clearly code-bearing and appears to participate in active side-scrolling/gameplay logic.

## Bank 4

Two fixed-bank call sequences currently provide reliable entrypoints:

- bank 4 `$8020`;
- bank 4 `$8200`.

`$8020` uses `$050E` as an index, scales it by 16, and copies scenario-specific table data into `$05E0-$05F2`-area buffers.

`$8200` immediately enters an event/data-selection path keyed by `$050E` and `$0514`.

This strongly supports a scenario/temple resource role.

## Bank 5

Confirmed entrypoints include `$9330`, `$942E`, `$94B1`, `$970A`, `$A20B`, `$A275`, `$A2B0`, `$AF2F`, `$AF9D`.

Observed roles include:

- copying scenario-indexed table blocks into `$053B+` and `$055C+`;
- story/progression tests involving `$050E`, `$0533`, `$0670`, `$0673`, `$067D`;
- calls back into fixed event/display infrastructure.

Bank 5 is therefore strongly associated with scenario/event configuration and progression, although it also contains data.

## Bank 6

Confirmed entrypoints include `$8000`, `$8E0F`, `$8F42`, `$925F`, `$A68A`.

The `$8E0F+` region is particularly revealing:

- reads PPU status;
- writes PPUCTRL;
- streams bytes through `$2006/$2007`;
- constructs OAM-shadow records at `$0704+`;
- advances nametable addresses by `$20` per row.

Bank 6 therefore contains substantial rendering/resource-transfer machinery.

## Second PRG switching system

Besides the compact MMC1 helpers near `$C000`, the fixed bank has another synchronized PRG-selection path around `$E589/$E5B7`.

### `$E589`

- records requested bank in `$0639`;
- requests/awaits a synchronization point through `$0679`;
- marks a mapper-write critical section through `$063E`;
- performs the serial PRG-register update;
- clears the critical-section marker.

### `$E5B7`

- stores a transient bank value in `$063A`;
- marks `$063F` as a critical-section indicator;
- performs the same serial PRG update;
- clears the marker.

The alternate NMI path at `$E000+` checks those critical-section bytes before doing its own temporary bank work. This is a second, more elaborate NMI/main-thread coordination mechanism in addition to `$3A/$3B`.

It is used to enter banks 4, 5 and 6, explaining why those banks were invisible to the initial call graph seeded only from the `$C035/$C0B4` path.

## Static reachability after known seeds

Current recursive-disassembly counts are useful only as coverage indicators:

- bank 0: ~977 reachable instructions;
- bank 1: ~1,894;
- bank 2: 0 seeded executable instructions;
- bank 3: ~3,501;
- bank 4: ~48 from current seeds;
- bank 5: ~392;
- bank 6: ~382;
- bank 7 fixed: ~4,562.

These are not total instruction counts. Inline data and return-address-driven dispatchers can defeat a conventional recursive disassembler, while unseeded routines remain invisible.

## Important disassembly hazard

The engine repeatedly uses routines that consume the caller's return address and treat bytes immediately following a `JSR` as inline parameters/tables. A naïve linear or recursive disassembler can therefore decode inline data as 6502 instructions.

Every suspicious fall-through after such helper calls must be treated as data until control flow proves otherwise.

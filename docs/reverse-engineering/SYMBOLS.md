# Working symbol map

This is the current clean-room naming layer for the canonical Japanese ROM. Names marked `CONFIRMED` describe behavior proven by static code; `PROVISIONAL` names are useful working labels whose exact semantics may still change.

## Fixed PRG bank ($C000-$FFFF)

| Address | Symbol | Status | Meaning |
|---:|---|---|---|
| `$C000` | `nmi_vector_trampoline` | CONFIRMED | jumps to NMI body `$D269` |
| `$C003` | `irq_rti` | CONFIRMED | IRQ/BRK handler immediately returns |
| `$C035` | `mmc1_set_prg_protected` | CONFIRMED | interrupt-safe persistent PRG-bank serial writer |
| `$C05A` | `mmc1_set_control_raw` | CONFIRMED | MMC1 control serial writer |
| `$C078` | `mmc1_set_chr0_raw` | CONFIRMED | MMC1 CHR0 serial writer |
| `$C096` | `mmc1_set_chr1_raw` | CONFIRMED | MMC1 CHR1 serial writer |
| `$C0B4` | `mmc1_set_prg_raw` | CONFIRMED | raw PRG-bank writer |
| `$C100` | `reset` | CONFIRMED | reset/bootstrap entry |
| `$C458` | `password_stage_state` | CONFIRMED | stages persistent state for password encoder |
| `$C4E4` | `read_platform_controllers` | CONFIRMED | reads controller 1/2 into `$3D/$3E` |
| `$D269` | `nmi_main` | CONFIRMED | OAM DMA, state dispatch, PPU update, temporary banking |
| `$DA13` | `main_initializer` | CONFIRMED | global init and top-level dispatcher setup |
| `$E505` | `current_saint_by_selector_table` | PROVISIONAL | table used with `$0533` to assign `$03` |
| `$E589` | `mmc1_set_prg_synchronized` | PROVISIONAL | second synchronized bank-selection path |
| `$E5B7` | `mmc1_set_prg_transient_sync` | PROVISIONAL | transient synchronized PRG switch |

## PRG bank 0 ($8000-$BFFF when selected)

| Address | Symbol | Status | Meaning |
|---:|---|---|---|
| `$8960` | `dispatch_inline_pointer_table` | CONFIRMED | consumes caller return address and dispatches through inline word table |
| `$8B1F` | `read_menu_controller` | CONFIRMED | held + newly-pressed controller state in `$020A/$020B` |
| `$AE18` | `password_encode_sixbit` | CONFIRMED | converts staged payload into six-bit password symbols |
| `$AEB5` | `password_decode_validate` | CONFIRMED | checksum/deobfuscation/decoding entry |
| `$AF19` | `password_pack_saint_records` | CONFIRMED | packs four five-byte Saint records into four bytes each |
| `$AF89` | `password_restore_payload` | CONFIRMED | unpacks decoded payload back into persistent state |
| `$B03A` | `password_pack_byte` | CONFIRMED | splits one payload byte into upper-two/lower-six representation |
| `$B04C` | `password_unpack_bits` | CONFIRMED | reconstructs bytes from six-bit representation |
| `$B271` | `password_grid_input` | CONFIRMED | handles 10×7 kana-grid selection and 31-symbol buffer |

## PRG bank 1

| Address | Symbol | Status | Meaning |
|---:|---|---|---|
| `$8616` | `draw_current_saint_life` | PROVISIONAL | formats selected Saint's `$63/$64` packed-decimal value for UI |
| `$9211` | `derive_movement_increments` | PROVISIONAL | writes `$0387-$0389` according to Saint/state |
| `$9299` | `consume_current_saint_cosmo` | PROVISIONAL | subtracts packed-decimal amount from `$59/$5A` indexed by `$03` |
| `$9311` | `consume_current_saint_life` | PROVISIONAL | packed-decimal decrement on `$63/$64` indexed by `$03` |
| `$951F` | `snapshot_all_saint_stats` | CONFIRMED | active zero-page arrays -> `$058C-$05A4` |
| `$9720` | `restore_all_saint_stats` | CONFIRMED | `$058C-$05A4` -> active zero-page arrays |
| `$98BA` | `clear_platform_temporaries` | PROVISIONAL | clears many gameplay temporary fields including `$76` |

## PRG bank 3

| Address | Symbol | Status | Meaning |
|---:|---|---|---|
| `$AB3F` | `platform_horizontal_move` | PROVISIONAL | right/left movement, collision and camera handoff |
| `$B87D` | `platform_vertical_phase` | PROVISIONAL | one vertical-motion/collision phase |
| `$B94B` | `platform_sync_player_record` | PROVISIONAL | copies player/action state into working collision/render fields |
| `$BB76` | `platform_jump_input` | PROVISIONAL | A jump, direction modifier, Up high-jump branch |
| `$BBCA` | `platform_attack_input` | PROVISIONAL | B attack transition path |

## RAM symbols

| Address | Symbol | Status |
|---:|---|---|
| `$00/$01` | `engine_state_a/b` | PROVISIONAL |
| `$03` | `current_saint_index` | CONFIRMED structurally |
| `$3A` | `mmc1_write_interrupted` | CONFIRMED |
| `$3B` | `persistent_prg_bank` | CONFIRMED |
| `$3D` | `platform_input_p1` | CONFIRMED |
| `$3E` | `platform_input_p2` | CONFIRMED |
| `$3F` | `player_x` | CONFIRMED |
| `$40` | `player_y_component` | PROVISIONAL |
| `$42` | `player_facing` | PROVISIONAL |
| `$44/$45` | `scroll_x_low/high` | PROVISIONAL |
| `$4D/$4E` | `player_action_state/current_previous` | PROVISIONAL |
| `$59-$62` | `saint_cosmo_bcd[5]` | PROVISIONAL semantic / CONFIRMED structure |
| `$63-$6C` | `saint_life_bcd[5]` | PROVISIONAL semantic / CONFIRMED structure |
| `$6D-$71` | `saint_aux_stat[5]` | UNKNOWN semantic |
| `$76` | `invulnerability_timer` | PROVISIONAL |
| `$020A` | `menu_input_held` | CONFIRMED |
| `$020B` | `menu_input_pressed` | CONFIRMED |
| `$0387-$0389` | `movement_delta_*` | PROVISIONAL |
| `$058C-$05A4` | `saint_stat_snapshot[5][5]` | CONFIRMED structure |
| `$05AA/$05AB` | `seventh_sense_bcd` | CONFIRMED semantic with external fixture |
| `$0639/$063A` | `sync_prg_bank_requested/transient` | PROVISIONAL |
| `$063E/$063F` | `sync_prg_critical_flags` | PROVISIONAL |
| `$067D` | `story_progress_index` | PROVISIONAL semantic / CONFIRMED persistent |
| `$06CD` | `story_progress_descriptor` | PROVISIONAL |
| `$06EC/$06ED` | `password_cursor_col/row` | CONFIRMED |
| `$06EE` | `password_position` | CONFIRMED |
| `$0700-$07FF` | `oam_shadow` | CONFIRMED |

## Naming rule

A symbol may be useful before its English name is perfect. When subsequent evidence changes a semantic interpretation, rename it rather than bending the evidence around the old label. `ORIGINAL SPEC` takes precedence over naming continuity.

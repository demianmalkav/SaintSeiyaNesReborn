# Working symbol map

This is the current clean-room naming layer for the canonical Japanese ROM. `CONFIRMED` names describe behavior directly established by static code or a reproducible compatibility fixture; `PROVISIONAL` names remain useful working labels.

## Fixed PRG bank (`$C000-$FFFF`)

| Address | Symbol | Status | Meaning |
|---:|---|---|---|
| `$C000` | `nmi_vector_trampoline` | CONFIRMED | jumps to NMI body `$D269` |
| `$C003` | `irq_rti` | CONFIRMED | IRQ/BRK immediately returns |
| `$C035` | `mmc1_set_prg_protected` | CONFIRMED | interrupt-safe persistent PRG writer |
| `$C05A` | `mmc1_set_control_raw` | CONFIRMED | MMC1 control serial writer |
| `$C078` | `mmc1_set_chr0_raw` | CONFIRMED | CHR0 serial writer |
| `$C096` | `mmc1_set_chr1_raw` | CONFIRMED | CHR1 serial writer |
| `$C0B4` | `mmc1_set_prg_raw` | CONFIRMED | raw/temporary PRG bank writer |
| `$C100` | `reset` | CONFIRMED | reset/bootstrap entry |
| `$C458` | `password_stage_state` | CONFIRMED | persistent state -> password staging buffer |
| `$C4E4` | `read_platform_controllers` | CONFIRMED | controller 1/2 -> `$3D/$3E` |
| `$D269` | `nmi_main` | CONFIRMED | OAM DMA, PPU update, state dispatch, temporary banking |
| `$DA13` | `main_initializer` | CONFIRMED | global init / top-level dispatch setup |
| `$E505` | `current_saint_by_selector_table` | PROVISIONAL | `$0533`-indexed table that assigns `$03` |
| `$E589` | `mmc1_set_prg_synchronized` | PROVISIONAL | synchronized persistent PRG path |
| `$E5B7` | `mmc1_set_prg_transient_sync` | PROVISIONAL | synchronized transient PRG switch |

## PRG bank 0

| Address | Symbol | Status | Meaning |
|---:|---|---|---|
| `$8960` | `dispatch_inline_pointer_table` | CONFIRMED | caller-relative inline word-table dispatcher |
| `$8B1F` | `read_menu_controller` | CONFIRMED | held/newly-pressed input in `$020A/$020B` |
| `$AE18` | `password_encode_sixbit` | CONFIRMED | payload -> six-bit password representation |
| `$AEB5` | `password_decode_validate` | CONFIRMED | checksum/deobfuscation/decode entry |
| `$AF19` | `password_pack_saint_records` | CONFIRMED | four five-byte records -> four bytes each |
| `$AF89` | `password_restore_payload` | CONFIRMED | decoded payload -> persistent state |
| `$B03A` | `password_pack_byte` | CONFIRMED | upper-two/lower-six split |
| `$B04C` | `password_unpack_bits` | CONFIRMED | reconstructs payload bytes |
| `$B271` | `password_grid_input` | CONFIRMED | 10×7 grid / 31-symbol entry buffer |

## PRG bank 1 — Saint stats / movement support

| Address | Symbol | Status | Meaning |
|---:|---|---|---|
| `$8616` | `draw_current_saint_cosmo` | PROVISIONAL | formats selected Saint `$63/$64` value for UI |
| `$9211` | `derive_movement_increments` | PROVISIONAL | derives `$0387-$0389` from Saint/frame state |
| `$9299` | `consume_current_saint_life` | PROVISIONAL | packed-decimal decrement in `$59/$5A` family |
| `$9311` | `consume_current_saint_cosmo` | PROVISIONAL | packed-decimal decrement in `$63/$64` family |
| `$951F` | `snapshot_all_saint_stats` | CONFIRMED | active arrays -> `$058C-$05A4` |
| `$9720` | `restore_all_saint_stats` | CONFIRMED | snapshot -> active arrays |
| `$98BA` | `clear_platform_temporaries` | PROVISIONAL | clears gameplay temporary state including `$76` |

## PRG bank 3 — platform player/attacks

| Address | Symbol | Status | Meaning |
|---:|---|---|---|
| `$A311` | `update_attack_projectile` | PROVISIONAL | advances projectile and decrements range/lifetime counter |
| `$AB3F` | `platform_horizontal_move` | PROVISIONAL | horizontal movement, collision and camera handoff |
| `$B87D` | `platform_vertical_phase` | PROVISIONAL | fall/vertical collision phase |
| `$B94B` | `platform_sync_player_record` | PROVISIONAL | player/action -> collision/render working fields |
| `$BB76` | `platform_jump_input` | PROVISIONAL | A jump; horizontal low-bit state; Up high-jump modifier |
| `$BBCA` | `platform_attack_input` | PROVISIONAL | B attack path |
| `$BCD3` | `platform_jump_curve_step` | CONFIRMED behavior | chooses/consumes table-driven jump profile |
| `$BCAE` | `projectile_range_by_cosmo_table` | CONFIRMED table role | slots 0-3 range/lifetime by Cosmo-hundreds bracket |
| `$BCF0` | `high_jump_duration_by_saint` | CONFIRMED table role | 5 high-jump durations |
| `$BCF5` | `forward_jump_duration_by_saint` | CONFIRMED table role | 5 directional-jump durations |
| `$BFD7` | `high_jump_curve_ptrs` | CONFIRMED table role | per-Saint jump-curve pointers |
| `$BFE1` | `forward_jump_curve_ptrs` | CONFIRMED table role | per-Saint directional-curve pointers |

## RAM symbols

| Address | Symbol | Status |
|---:|---|---|
| `$00/$01` | `engine_state_a/b` | PROVISIONAL |
| `$03` | `current_saint_index` | CONFIRMED structure |
| `$3A` | `mmc1_write_interrupted` | CONFIRMED |
| `$3B` | `persistent_prg_bank` | CONFIRMED |
| `$3D` | `platform_input_p1` | CONFIRMED |
| `$3E` | `platform_input_p2` | CONFIRMED |
| `$3F` | `player_x` | CONFIRMED |
| `$40` | `player_y` | CONFIRMED role / coordinate convention pending |
| `$41` | `player_y_page_or_high` | PROVISIONAL |
| `$42` | `player_facing` | PROVISIONAL |
| `$44/$45` | `scroll_x_low/high` | PROVISIONAL |
| `$49` | `jump_phase` | PROVISIONAL |
| `$4A` | `jump_latch` | PROVISIONAL |
| `$4B/$4C` | `attack_busy_state` | PROVISIONAL |
| `$4D` | `player_action_state` | PROVISIONAL ontology |
| `$4E` | `player_action_latched` | CONFIRMED structural role |
| `$4F-$56` | `collision_samples[8]` | CONFIRMED structure / UNKNOWN geometry |
| `$59-$62` | `saint_life_bcd[5]` | CONFIRMED semantic |
| `$63-$6C` | `saint_cosmo_bcd[5]` | CONFIRMED semantic |
| `$6D-$71` | `saint_aux_stat[5]` | UNKNOWN semantic |
| `$76` | `invulnerability_timer` | PROVISIONAL |
| `$020A` | `menu_input_held` | CONFIRMED |
| `$020B` | `menu_input_pressed` | CONFIRMED |
| `$0387-$0389` | `air_horizontal_delta_*` | PROVISIONAL exact naming |
| `$038A` | `high_jump_modifier` | CONFIRMED behavioral role |
| `$038E-$0390` | `projectile_range_or_lifetime[]` | PROVISIONAL array role; `$038E` directly traced statically |
| `$058C-$05A4` | `saint_stat_snapshot[5][5]` | CONFIRMED structure |
| `$05AA/$05AB` | `seventh_sense_bcd` | CONFIRMED semantic with fixture |
| `$0639/$063A` | `sync_prg_bank_requested/transient` | PROVISIONAL |
| `$063E/$063F` | `sync_prg_critical_flags` | PROVISIONAL |
| `$067D` | `story_progress_index` | CONFIRMED persistent / exact story numbering pending |
| `$06CD` | `story_progress_descriptor` | PROVISIONAL |
| `$06EC/$06ED` | `password_cursor_col/row` | CONFIRMED |
| `$06EE` | `password_position` | CONFIRMED |
| `$0700-$07FF` | `oam_shadow` | CONFIRMED |

## Saint index identity — current evidence

Do not treat this as final yet:

- index 0: strong Seiya candidate — uniquely tallest high jump (103 px) and longest non-special high-Cosmo projectile range;
- index 4: strong Ikki candidate — unique initial 499 Life/499 Cosmo and fixed projectile range/lifetime 60;
- index 3: strong Shun candidate — attack path uses multiple projectile/OAM slots in a pattern compatible with chain behavior;
- indices 1/2 remain to be separated rigorously between Shiryu and Hyoga.

## Naming rule

When new evidence changes semantics, rename symbols immediately. ORIGINAL SPEC takes precedence over naming continuity. The Life/Cosmo swap corrected in October 2026 is the canonical example: `$59` is Life and `$63` is Cosmo; older provisional names must not be propagated.

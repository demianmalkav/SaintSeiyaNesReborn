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
| `$C52F` | `compute_platform_attack_damage_wrapper` | CONFIRMED | maps bank 1 and calls `$8616` |
| `$D1E0` | `add_seventh_sense_reward_bcd` | CONFIRMED | adds A as two-digit BCD to `$05AA/$05AB`, clamps 9999 |
| `$D269` | `nmi_main` | CONFIRMED | OAM DMA, PPU update, state dispatch, temporary banking |
| `$DA13` | `main_initializer` | CONFIRMED | global init / top-level dispatch setup |
| `$E505` | `internal_canonical_saint_index_map` | CONFIRMED | involution `[0,2,1,3,4]`, converts `$03 <-> $0533` |
| `$E589` | `mmc1_set_prg_synchronized` | PROVISIONAL | synchronized persistent PRG path |
| `$E5B7` | `mmc1_set_prg_transient_sync` | PROVISIONAL | synchronized transient PRG switch |
| `$E99F` | `upload_selector_palette` | CONFIRMED | `$0616-$0625` -> PPU `$3F00-$3F0F` |
| `$F31E` | `add_scripted_seventh_sense_hundreds_bcd` | CONFIRMED | adds packed BCD to upper two Seventh-Sense digits; scripted rewards in hundreds |
| `$FBCF` | `increment_active_life` | CONFIRMED | +1 Life if below selected Saint cap |
| `$FC2E` | `decrement_active_life` | CONFIRMED | -1 Life if nonzero |
| `$FCC7` | `increment_active_cosmo` | CONFIRMED | +1 Cosmo if below selected Saint cap |
| `$FD26` | `decrement_active_cosmo` | CONFIRMED | -1 Cosmo if nonzero |
| `$FDE0` | `increment_seventh_sense_one` | CONFIRMED | +1 Seventh Sense, cap 9999 |
| `$FE26` | `decrement_seventh_sense_one` | CONFIRMED | -1 Seventh Sense if nonzero |
| `$FEC6` | `format_seventh_sense_digits` | CONFIRMED | expands `$05AA/$05AB` nibbles for display |

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
| `$B3E2` | `sample_player_collision_tiles` | CONFIRMED | fills `$4F-$56` from eight world-space tile probes |
| `$B584` | `begin_world_x_tile_lookup` | CONFIRMED role | combines scroll X and player X |
| `$B595` | `select_tilemap_column` | CONFIRMED role | converts world X to 16-pixel tile column/address |
| `$B5C6` | `read_tilemap_cell` | CONFIRMED role | adds 16-pixel Y row and returns tile/class byte |

## PRG bank 1 — Saint stats / movement / resource economy

| Address | Symbol | Status | Meaning |
|---:|---|---|---|
| `$8611` | `platform_damage_base_by_internal_saint` | CONFIRMED table role | `[19,25,21,17,15]` = Seiya/Shun/Hyoga/Shiryu/Ikki |
| `$8616` | `compute_platform_attack_damage` | CONFIRMED | current Saint + Cosmo -> `$72` |
| `$9211` | `derive_movement_increments` | PROVISIONAL | derives `$0387-$0389` from Saint/frame state |
| `$927A` | `apply_platform_life_drain` | CONFIRMED behavior | while `$7F>0`, decrement timer and subtract 2 Life/tick |
| `$9299` | `subtract_two_current_saint_life` | CONFIRMED behavior | packed-decimal Life decrement in `$59-$62` |
| `$930A` | `apply_platform_cosmo_drain` | CONFIRMED behavior | while `$80>0`, decrement timer and subtract 1 Cosmo/tick |
| `$9311` | `subtract_one_current_saint_cosmo` | CONFIRMED behavior | packed-decimal Cosmo decrement in `$63-$6C` |
| `$951F` | `snapshot_all_saint_stats` | CONFIRMED | internal order -> canonical snapshot order |
| `$9720` | `restore_all_saint_stats` | CONFIRMED | canonical snapshot -> internal active arrays |
| `$98BA` | `clear_platform_temporaries` | PROVISIONAL | clears gameplay temporary state including `$76/$7F/$80` |
| `$9F29` | `upload_one_palette_triplet` | CONFIRMED | writes universal `$0F` plus three colors to PPU palette |
| `$9F46` | `internal_saint_palette_ptrs` | CONFIRMED table role | five internal-index pointers to 3-color player palettes |
| `$AB10` | `load_store_selected_saint_resources` | CONFIRMED role | moves selected persistent Life/Cosmo/caps to/from `$05BC-$05D0` |
| `$AB4E` | `unpack_selected_saint_cap_byte` | CONFIRMED role | low nibble -> Cosmo boundary `$05BE`; high nibble -> Life boundary `$05D0` |

## PRG bank 3 — platform player/combat

| Address | Symbol | Status | Meaning |
|---:|---|---|---|
| `$98BA` | `entity_hits_player` | CONFIRMED behavior | entity-vs-player overlap; loads drain counters and `$76=32` |
| `$9915` | `player_attacks_entity` | CONFIRMED behavior | checks three attack slots against current entity |
| `$992A` | `test_attack_slot_vs_entity` | CONFIRMED behavior | point-vs-parameterized-rectangle hit test |
| `$99BA` | `apply_attack_damage_to_entity` | CONFIRMED behavior | enemy HP offset `$0C` minus `$72` |
| `$9A27` | `retire_attack_for_hyoga_shiryu` | CONFIRMED behavior | collision helper deactivates attack only for internal 2/3 |
| `$9A3F/$9A4C/$9A59` | `select_attack_slot_0/1/2` | CONFIRMED | OAM records `$0730/$0738/$0740`, counters `$038E-$0390` |
| `$A311` | `update_shun_extend_retract_attack` | CONFIRMED character-specific role | type `$54`, extends then retracts using `$0391` |
| `$AB3F` | `platform_horizontal_move` | PROVISIONAL | horizontal movement, collision and camera handoff |
| `$B87D` | `platform_vertical_phase` | PROVISIONAL | fall/vertical collision phase |
| `$B94B` | `platform_sync_player_record` | PROVISIONAL | player/action -> collision/render working fields; decrements `$76` |
| `$BB76` | `platform_jump_input` | PROVISIONAL | A jump; horizontal low-bit state; Up high-jump modifier |
| `$BBCA` | `platform_attack_input` | CONFIRMED input role | B attack path / character-specific projectile selection |
| `$BCD3` | `platform_jump_curve_step` | CONFIRMED behavior | chooses/consumes table-driven jump profile |
| `$BCAE` | `projectile_range_by_cosmo_table` | CONFIRMED table role | Seiya/Shun/Hyoga/Shiryu range/lifetime by Cosmo bracket |
| `$BCF0` | `high_jump_duration_by_saint` | CONFIRMED table role | internal order `[Seiya,Shun,Hyoga,Shiryu,Ikki]` |
| `$BCF5` | `forward_jump_duration_by_saint` | CONFIRMED table role | internal order `[Seiya,Shun,Hyoga,Shiryu,Ikki]` |
| `$BFD7` | `high_jump_curve_ptrs` | CONFIRMED table role | per-Saint jump-curve pointers |
| `$BFE1` | `forward_jump_curve_ptrs` | CONFIRMED table role | per-Saint directional-curve pointers |

## PRG bank 5

| Address | Symbol | Status | Meaning |
|---:|---|---|---|
| `$AF9D` | `stage_character_palette_by_selector` | CONFIRMED | copies 16-byte `$0533` palette block to `$0616` |
| `$B11A` | `canonical_character_palette_blocks` | CONFIRMED table role | five 16-byte palettes in high-level character order |

## RAM symbols

| Address | Symbol | Status |
|---:|---|---|
| `$00/$01` | `engine_state_a/b` | PROVISIONAL |
| `$03` | `current_saint_internal` | CONFIRMED — `0 Seiya, 1 Shun, 2 Hyoga, 3 Shiryu, 4 Ikki` |
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
| `$4F` | `collision_floor_center` | CONFIRMED geometry |
| `$50` | `collision_lower_right` | CONFIRMED geometry |
| `$51` | `collision_upper_right` | CONFIRMED geometry |
| `$52` | `collision_bottom_right_ledge` | CONFIRMED geometry |
| `$53` | `collision_lower_left` | CONFIRMED geometry |
| `$54` | `collision_upper_left` | CONFIRMED geometry |
| `$55` | `collision_bottom_left_ledge` | CONFIRMED geometry |
| `$56` | `collision_upper_center` | CONFIRMED geometry |
| `$59-$62` | `saint_life_bcd[5]` | CONFIRMED semantic |
| `$63-$6C` | `saint_cosmo_bcd[5]` | CONFIRMED semantic |
| `$6D-$71` | `saint_resource_caps[5]` | CONFIRMED — high nibble Life boundary, low nibble Cosmo boundary |
| `$72` | `platform_attack_damage` | CONFIRMED |
| `$76` | `player_hit_invulnerability_timer` | CONFIRMED ordinary-hit duration 32 |
| `$7F` | `pending_life_drain_ticks` | CONFIRMED — 2 Life/tick |
| `$80` | `pending_cosmo_drain_ticks` | CONFIRMED — 1 Cosmo/tick |
| `$020A` | `menu_input_held` | CONFIRMED |
| `$020B` | `menu_input_pressed` | CONFIRMED |
| `$0387-$0389` | `air_horizontal_delta_*` | PROVISIONAL exact naming |
| `$038A` | `high_jump_modifier` | CONFIRMED behavioral role |
| `$038E-$0390` | `projectile_range_or_lifetime[]` | PROVISIONAL array role; `$038E` directly traced |
| `$0533` | `current_saint_canonical_selector` | CONFIRMED — `0 Seiya, 1 Hyoga, 2 Shun, 3 Shiryu, 4 Ikki` |
| `$058C-$05A4` | `saint_stat_snapshot[5][5]` | CONFIRMED canonical character order |
| `$05AA/$05AB` | `seventh_sense_bcd` | CONFIRMED semantic with fixture |
| `$05BC/$05BD` | `active_cosmo_bcd` | CONFIRMED selected-Saint working value |
| `$05BE` | `active_cosmo_hundreds_boundary` | CONFIRMED exclusive cap boundary |
| `$05CE/$05CF` | `active_life_bcd` | CONFIRMED selected-Saint working value |
| `$05D0` | `active_life_hundreds_boundary` | CONFIRMED exclusive cap boundary |
| `$0616-$0625` | `staged_character_palette` | CONFIRMED |
| `$0639/$063A` | `sync_prg_bank_requested/transient` | PROVISIONAL |
| `$063E/$063F` | `sync_prg_critical_flags` | PROVISIONAL |
| `$067D` | `story_progress_index` | CONFIRMED persistent / exact story numbering pending |
| `$06CD` | `story_progress_descriptor` | PROVISIONAL |
| `$06EC/$06ED` | `password_cursor_col/row` | CONFIRMED |
| `$06EE` | `password_position` | CONFIRMED |
| `$0700-$07FF` | `oam_shadow` | CONFIRMED |

## Platform entity-record fields (pointer `$16/$17`)

| Offset | Symbol/meaning | Status |
|---:|---|---|
| `0` | entity state/status family | CONFIRMED structural role |
| `1` | entity X | CONFIRMED |
| `2` | entity Y | CONFIRMED |
| `9` | entity type/class | CONFIRMED structural role |
| `$0C` | entity HP | CONFIRMED ordinary damage path |
| `$0D` | Cosmo-drain ticks inflicted on player | CONFIRMED |
| `$0E` | Life-drain ticks inflicted on player | CONFIRMED |
| `$0F` | `seventh_sense_reward_bcd` | CONFIRMED — added by `$D1E0`, saturates 9999 |

## Character index domains

Internal platform order (`$03`): `[Seiya, Shun, Hyoga, Shiryu, Ikki]`.

Canonical/high-level selector order (`$0533`): `[Seiya, Hyoga, Shun, Shiryu, Ikki]`.

`$E505` swaps values 1 and 2 when converting between them. See `CHARACTER_INDEX_MAP.md`.

## Naming rule

When new evidence changes semantics, rename symbols immediately. ORIGINAL SPEC takes precedence over naming continuity. The Life/Cosmo correction is the canonical example: `$59` is Life and `$63` is Cosmo; older provisional names must not be propagated.

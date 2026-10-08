"""Gold Saint technique-slot selection reconstructed from PRG bank 6 `$9074+`.

This models only which structural technique slot (`$0680`, 0..3) is selected.
Animation/dialogue ids and scripted vulnerability are separate state machines.

Stage indices follow `$050E`:
0 Mu/non-battle, 1 Aldebaran, 2 Gemini/Camus branch, 3 Death Mask,
4 Aioria, 5 Shaka, 6 Milo, 7 Shura, 8 Camus, 9 Aphrodite, 10 Saga.
The names are externally corroborated by boss stats matching the ROM records.
"""

from __future__ import annotations


def select_boss_technique(
    stage_index: int,
    *,
    parity_bit: int = 0,
    canonical_character_index: int = 0,
    event_counter_a: int = 0,
    event_counter_b: int = 0,
    camus_special_flag: bool = False,
    aphrodite_phase: int = 0,
    saga_phase: int = 0,
    saga_flag_06d0_ff: bool = False,
    player_technique_slot: int = 0,
) -> int:
    """Return the opponent technique slot selected by the original routine.

    Arguments map to the ROM decision inputs:
    - `parity_bit`: `$065F & 1`, default selector for ordinary stages;
    - `canonical_character_index`: `$0533` (Ikki == 4);
    - `event_counter_a/b`: `$0677 + $0678` for Milo/Camus branches;
    - `camus_special_flag`: `$06B8 != 0`;
    - `aphrodite_phase`: `$064D`;
    - `saga_phase`: `$06CE`, valid 0..2 for the observed jump table;
    - `saga_flag_06d0_ff`: `$06D0 == $FF`;
    - `player_technique_slot`: `$0649`.
    """
    if not 0 <= stage_index <= 10:
        raise ValueError("stage_index must be 0..10")
    if parity_bit not in (0, 1):
        raise ValueError("parity_bit must be 0 or 1")
    if not 0 <= canonical_character_index <= 4:
        raise ValueError("canonical_character_index must be 0..4")
    if not 0 <= player_technique_slot <= 3:
        raise ValueError("player_technique_slot must be 0..3")

    # Shaka: when Ikki is the active character, force slot 2.
    if stage_index == 5 and canonical_character_index == 4:
        return 2

    # Milo: before the combined event counters reach 2 use slot 1;
    # afterwards use slot 0.
    if stage_index == 6:
        return 0 if (event_counter_a + event_counter_b) >= 2 else 1

    # Camus: special flag forces slot 2. Otherwise the same two-counter
    # threshold selects slot 1 early and slot 0 later.
    if stage_index == 8:
        if camus_special_flag:
            return 2
        return 0 if (event_counter_a + event_counter_b) >= 2 else 1

    # Aphrodite: deterministic escalation by `$064D`.
    if stage_index == 9:
        if aphrodite_phase >= 6:
            return 2
        if aphrodite_phase >= 3:
            return 1
        return 0

    # Saga: `$06CE` dispatches through three explicit branches.
    if stage_index == 10:
        if saga_phase == 0:
            return 1 if saga_flag_06d0_ff else 0
        if saga_phase == 1:
            if saga_flag_06d0_ff:
                return 3
            return 2 if player_technique_slot == 0 else 0
        if saga_phase == 2:
            return 3
        raise ValueError("observed Saga phase must be 0..2")

    # All remaining stages use `$065F & 1`. Several bosses have duplicate
    # coefficient/visual slots, so the alternating structural slot may still
    # represent the same named technique.
    return parity_bit

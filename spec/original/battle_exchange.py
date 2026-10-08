"""Composable numeric exchange for Kanketsu Hen Gold Saint battles.

This is intentionally narrower than the complete battle state machine. It
composes already-reconstructed ORIGINAL SPEC primitives:

player hit gate -> player technique drain -> defeat check -> opponent technique
drain -> dodge bypass -> resulting resource state.

Dialogue, scripted invulnerability transitions, forced character swaps and
story rewards remain outside this module and are explicit inputs.
"""

from __future__ import annotations

from dataclasses import dataclass

from .battle_damage import opponent_attack_drain, player_attack_drain
from .player_attack_hit import resolve_player_attack_hit


@dataclass(frozen=True)
class CombatantState:
    life: int
    cosmo: int

    def __post_init__(self) -> None:
        if not 0 <= self.life <= 999:
            raise ValueError("life must be 0..999")
        if not 0 <= self.cosmo <= 999:
            raise ValueError("cosmo must be 0..999")

    @property
    def defeated(self) -> bool:
        # The original battle ends when either resource reaches zero.
        return self.life == 0 or self.cosmo == 0

    def drain(self, *, life: int = 0, cosmo: int = 0) -> "CombatantState":
        if life < 0 or cosmo < 0:
            raise ValueError("drain values cannot be negative")
        return CombatantState(
            life=max(0, self.life - life),
            cosmo=max(0, self.cosmo - cosmo),
        )


@dataclass(frozen=True)
class ExchangeResult:
    player: CombatantState
    opponent: CombatantState
    player_attack_connected: bool
    player_life_drain_dealt: int
    player_cosmo_drain_dealt: int
    opponent_countered: bool
    opponent_attack_evaded: bool
    player_life_drain_taken: int
    player_cosmo_drain_taken: int


def resolve_numeric_exchange(
    *,
    player: CombatantState,
    opponent: CombatantState,
    player_attack_id: int,
    player_phase_accumulator: int,
    stage_index: int,
    opponent_technique_index: int,
    scripted_player_hit_block: bool = False,
    player_hit_special_phase_two: bool = False,
    forced_player_no_damage_context: bool = False,
    opponent_attack_weakening_tier: int = 0,
    opponent_attack_evaded: bool = False,
    counterattack_enabled: bool = True,
) -> ExchangeResult:
    """Resolve one numeric attack/counterattack exchange.

    The opponent counterattack is skipped when:
    - the opponent is defeated after the Bronze attack;
    - caller disables it for a story/state-machine reason;
    - otherwise damage is calculated, but a successful dodge bypasses resource
      drain completely.
    """
    hit = resolve_player_attack_hit(
        player_phase_accumulator,
        scripted_block=scripted_player_hit_block,
        special_phase_two=player_hit_special_phase_two,
        forced_no_damage_context=forced_player_no_damage_context,
    )

    dealt_life = dealt_cosmo = 0
    updated_opponent = opponent
    if hit.connects and not opponent.defeated:
        drain = player_attack_drain(player.cosmo, player_attack_id)
        dealt_life = min(updated_opponent.life, drain.life)
        dealt_cosmo = min(updated_opponent.cosmo, drain.cosmo)
        updated_opponent = updated_opponent.drain(life=drain.life, cosmo=drain.cosmo)

    can_counter = counterattack_enabled and not updated_opponent.defeated and not player.defeated
    if not can_counter:
        return ExchangeResult(
            player=player,
            opponent=updated_opponent,
            player_attack_connected=hit.connects,
            player_life_drain_dealt=dealt_life,
            player_cosmo_drain_dealt=dealt_cosmo,
            opponent_countered=False,
            opponent_attack_evaded=False,
            player_life_drain_taken=0,
            player_cosmo_drain_taken=0,
        )

    incoming = opponent_attack_drain(
        updated_opponent.cosmo,
        stage_index,
        opponent_technique_index,
        mitigation_tier=opponent_attack_weakening_tier,
    )

    if opponent_attack_evaded:
        taken_life = taken_cosmo = 0
        updated_player = player
    else:
        taken_life = min(player.life, incoming.life)
        taken_cosmo = min(player.cosmo, incoming.cosmo)
        updated_player = player.drain(life=incoming.life, cosmo=incoming.cosmo)

    return ExchangeResult(
        player=updated_player,
        opponent=updated_opponent,
        player_attack_connected=hit.connects,
        player_life_drain_dealt=dealt_life,
        player_cosmo_drain_dealt=dealt_cosmo,
        opponent_countered=True,
        opponent_attack_evaded=opponent_attack_evaded,
        player_life_drain_taken=taken_life,
        player_cosmo_drain_taken=taken_cosmo,
    )

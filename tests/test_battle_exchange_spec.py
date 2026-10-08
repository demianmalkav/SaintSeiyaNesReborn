import unittest

from spec.original.battle_exchange import CombatantState, resolve_numeric_exchange


class BattleExchangeSpecTests(unittest.TestCase):
    def test_connected_player_attack_drains_both_opponent_resources(self):
        result = resolve_numeric_exchange(
            player=CombatantState(life=200, cosmo=100),
            opponent=CombatantState(life=299, cosmo=299),
            player_attack_id=0,
            player_phase_accumulator=0x05,
            stage_index=2,
            opponent_technique_index=0,
            counterattack_enabled=False,
        )
        self.assertTrue(result.player_attack_connected)
        self.assertEqual(result.opponent, CombatantState(life=281, cosmo=273))
        self.assertEqual((result.player_life_drain_dealt, result.player_cosmo_drain_dealt), (18, 26))

    def test_missed_player_attack_does_no_opponent_drain(self):
        result = resolve_numeric_exchange(
            player=CombatantState(life=200, cosmo=100),
            opponent=CombatantState(life=299, cosmo=299),
            player_attack_id=0,
            player_phase_accumulator=0x04,
            stage_index=2,
            opponent_technique_index=0,
            counterattack_enabled=False,
        )
        self.assertFalse(result.player_attack_connected)
        self.assertEqual(result.opponent, CombatantState(life=299, cosmo=299))

    def test_defeated_opponent_cannot_counter(self):
        result = resolve_numeric_exchange(
            player=CombatantState(life=200, cosmo=999),
            opponent=CombatantState(life=10, cosmo=10),
            player_attack_id=13,
            player_phase_accumulator=0x0F,
            stage_index=7,
            opponent_technique_index=0,
        )
        self.assertTrue(result.opponent.defeated)
        self.assertFalse(result.opponent_countered)

    def test_successful_dodge_keeps_player_resources_unchanged(self):
        result = resolve_numeric_exchange(
            player=CombatantState(life=200, cosmo=200),
            opponent=CombatantState(life=399, cosmo=399),
            player_attack_id=0,
            player_phase_accumulator=0x04,
            stage_index=4,
            opponent_technique_index=0,
            opponent_attack_evaded=True,
        )
        self.assertTrue(result.opponent_countered)
        self.assertTrue(result.opponent_attack_evaded)
        self.assertEqual(result.player, CombatantState(life=200, cosmo=200))

    def test_opponent_attack_uses_post_player_attack_cosmo(self):
        result = resolve_numeric_exchange(
            player=CombatantState(life=500, cosmo=100),
            opponent=CombatantState(life=500, cosmo=500),
            player_attack_id=0,
            player_phase_accumulator=0x05,
            stage_index=4,
            opponent_technique_index=0,
        )
        # Player attack drains opponent Cosmo by 26 -> 474. The Aioria slot-0
        # counter therefore requests floor(474*.48)=227 Cosmo and
        # floor(474*.32)=151 Life from the player. Effective Cosmo consumption
        # is capped by the player's remaining 100 points.
        self.assertEqual(result.opponent.cosmo, 474)
        self.assertEqual(result.player_cosmo_drain_taken, 100)
        self.assertEqual(result.player_life_drain_taken, 151)
        self.assertEqual(result.player, CombatantState(life=349, cosmo=0))

    def test_scripted_weakening_applies_before_player_resource_clamp(self):
        full = resolve_numeric_exchange(
            player=CombatantState(life=500, cosmo=500),
            opponent=CombatantState(life=199, cosmo=299),
            player_attack_id=0,
            player_phase_accumulator=0x04,
            stage_index=1,
            opponent_technique_index=0,
            opponent_attack_weakening_tier=0,
        )
        half = resolve_numeric_exchange(
            player=CombatantState(life=500, cosmo=500),
            opponent=CombatantState(life=199, cosmo=299),
            player_attack_id=0,
            player_phase_accumulator=0x04,
            stage_index=1,
            opponent_technique_index=0,
            opponent_attack_weakening_tier=1,
        )
        self.assertGreater(full.player_life_drain_taken, half.player_life_drain_taken)
        self.assertGreater(full.player_cosmo_drain_taken, half.player_cosmo_drain_taken)


if __name__ == "__main__":
    unittest.main()

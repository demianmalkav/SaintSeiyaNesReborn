"""Bronze Saint battle-technique slots and unlock counts.

Canonical character order in battle UI: [Seiya, Hyoga, Shun, Shiryu, Ikki].
The original stores the number of selectable techniques per character at
`$0587-$058B`. Menu code copies the selected character's count to `$0696`.

Technique names are attached where ROM coefficient pairs match independently
published Life/Cosmo factor tables exactly. The structural ids/counts are ROM
facts; localized display strings remain part of the future JP->ES text pass.
"""

from __future__ import annotations

from dataclasses import dataclass
from enum import IntEnum


class Character(IntEnum):
    SEIYA = 0
    HYOGA = 1
    SHUN = 2
    SHIRYU = 3
    IKKI = 4


INITIAL_TECHNIQUE_COUNTS = (2, 2, 2, 1, 2)
MAX_TECHNIQUE_COUNTS = (3, 4, 4, 2, 2)


@dataclass(frozen=True)
class Technique:
    attack_id: int
    character: Character
    slot: int
    canonical_name: str
    spanish_working_name: str


TECHNIQUES = (
    Technique(0, Character.SEIYA, 0, "Pegasus Ryusei Ken", "Meteoro de Pegaso"),
    Technique(1, Character.SEIYA, 1, "Pegasus Suisei Ken", "Cometa de Pegaso"),
    Technique(2, Character.SEIYA, 2, "Pegasus Rolling Crash", "Choque Giratorio de Pegaso"),
    Technique(4, Character.HYOGA, 0, "Diamond Dust", "Polvo de Diamante"),
    Technique(5, Character.HYOGA, 1, "Freezing Ring", "Anillo de Congelación"),
    Technique(6, Character.HYOGA, 2, "Aurora Thunder Attack", "Ataque de la Aurora"),
    Technique(7, Character.HYOGA, 3, "Aurora Execution", "Ejecución de la Aurora"),
    Technique(8, Character.SHUN, 0, "Nebula Chain", "Cadena de la Nebulosa"),
    Technique(9, Character.SHUN, 1, "Thunder Wave", "Onda del Trueno"),
    Technique(10, Character.SHUN, 2, "Nebula Stream", "Corriente de la Nebulosa"),
    Technique(11, Character.SHUN, 3, "Nebula Storm", "Tormenta de la Nebulosa"),
    Technique(12, Character.SHIRYU, 0, "Rozan Shoryuha", "Ascenso del Dragón"),
    Technique(13, Character.SHIRYU, 1, "Rozan Koryuha", "Dragón Superior"),
    Technique(16, Character.IKKI, 0, "Phoenix Genma Ken", "Ilusión del Fénix"),
    Technique(17, Character.IKKI, 1, "Houyoku Tensho", "Vuelo del Fénix"),
)

_BY_ATTACK_ID = {t.attack_id: t for t in TECHNIQUES}


def attack_id(character: Character | int, slot: int) -> int:
    c = Character(character)
    if not 0 <= slot <= 3:
        raise ValueError("slot must be 0..3")
    return int(c) * 4 + slot


def available_attack_ids(character: Character | int, technique_count: int) -> tuple[int, ...]:
    c = Character(character)
    max_count = MAX_TECHNIQUE_COUNTS[int(c)]
    if not 0 <= technique_count <= max_count:
        raise ValueError(f"technique_count must be 0..{max_count} for {c.name}")
    return tuple(attack_id(c, slot) for slot in range(technique_count))


def initial_attack_ids(character: Character | int) -> tuple[int, ...]:
    c = Character(character)
    return available_attack_ids(c, INITIAL_TECHNIQUE_COUNTS[int(c)])


def technique_for_attack_id(value: int) -> Technique | None:
    return _BY_ATTACK_ID.get(value)


def unlock_one(character: Character | int, current_count: int) -> int:
    c = Character(character)
    maximum = MAX_TECHNIQUE_COUNTS[int(c)]
    if not 0 <= current_count <= maximum:
        raise ValueError("current_count outside character range")
    return min(maximum, current_count + 1)

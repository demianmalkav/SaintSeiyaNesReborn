# Estado global de ingeniería inversa

> **Documento de orientación global, no operativo.**
>
> Este archivo resume madurez y localización de evidencia. **No decide el trabajo siguiente.** La única autoridad de continuación es `docs/PROJECT_STATE.md`, reconciliada con `main` y PR/CI.

## Target primario

**Saint Seiya: Ōgon Densetsu Kanketsu Hen** — Famicom, Japón, 1988.

ROM canónica:

- SHA-1 `F871D9B3DAFDDCDAD5F2ACD71044292E5169064E`
- MD5 `3B0F17C2B6EFC928B3D3FE9B1A389680`
- SHA-256 `6917B31D7343A9A17170E833BACDBC3B1EBA3E02D11C51C0D44DBE436C9AD43A`
- CRC32 iNES `F8D258A3`
- CRC32 sin header `9561798D`
- 128 KiB PRG + 128 KiB CHR
- mapper 1 / MMC1

Fuente: `docs/reverse-engineering/CANONICAL_ROM.md`.

## Convención de madurez

- `HIGH`: especificación semántica sustancial + documentación/fixtures; quedan bordes concretos.
- `MEDIUM`: partes importantes promovidas, superficie incompleta.
- `LOW`: anclas parciales/investigación temprana.

Los niveles `CONFIRMED / INFERRED / UNKNOWN / DISPROVEN` siguen aplicándose dentro de los documentos técnicos.

## Mapa global vigente

| Subsistema | Madurez | Estado resumido | Entradas representativas |
|---|---|---|---|
| ROM / boot / MMC1 / bancos | HIGH | Target, vectores, wrappers y mapa de bancos ampliamente establecidos. | `CANONICAL_ROM.md`, `BOOT_AND_MAPPER.md`, `BANK_MAP.md` |
| Índices de Saints | MEDIUM-HIGH | Canonical/internal mapping promovido y reutilizado. | `CHARACTER_INDEX_MAP.md`, `SaintId.cs` |
| Plataforma: frame normal | HIGH | Orden persistente, entidades, hazards, interacción y timing ampliamente promovidos. | `PERSISTENT_LATE_OBJECT_FRAME.md`, `ENTITY_TYPES_0A_0B.md` |
| Plataforma: movimiento / colisión / combate | HIGH | Movimiento, salto, ataques, probes, daño, resource drain normal y transición fatal ya tienen modelos ejecutables. | `PLATFORM_PLAYER.md`, `PLATFORM_JUMP.md`, `PLATFORM_ATTACKS.md`, `COLLISION_BEHAVIOR.md`, `PLATFORM_PRE_PLAYER_RESOURCE_ORDER.md`, `PLATFORM_RESOURCE_FAILURE_STATE_60.md` |
| Plataforma: salida / reload / narrativa | HIGH | Exits `$02=$00-$11`, selector warm reload, destinos `$00/$10/$90`, narrativa `$70-$89`, reload `$8F` y failure reload `$FF` promovidos. | `PLATFORM_EXIT_GATES.md`, `PLATFORM_WARM_RELOAD_INTERACTIVE_STATE.md`, `PLATFORM_NORMAL_WARM_RELOAD_DESTINATIONS.md`, `PLATFORM_SPECIAL_NORMAL_EXITS.md`, `PLATFORM_NARRATIVE_STATES_80_89.md`, `PLATFORM_RELOAD_MODE_8F.md`, `PLATFORM_RESOURCE_FAILURE_STATE_60.md` |
| Máquina global `$00/$01` | HIGH en partición / MEDIUM-HIGH global | Dispatcher superior, `$11-$14` y `$60` cerrados. Frente operativo: `$91-$99`. | `ENGINE_STATE_DISPATCHER.md`, `ENGINE_STATE_FAMILY_11_14.md`, `PLATFORM_RESOURCE_FAILURE_STATE_60.md`, `PROJECT_STATE.md` |
| Combate de jefes | MEDIUM-HIGH | Recursos, daño, dodge, técnicas, AI y event dispatch promovidos; falta paridad global de todos los contextos. | `BOSS_BATTLE_RESOURCES.md`, `BOSS_BATTLE_DAMAGE.md`, `BOSS_DODGE.md`, `BATTLE_TECHNIQUES.md`, `BATTLE_EVENT_DISPATCH.md` |
| Life/Cosmo/Seventh Sense | HIGH en plataforma / MEDIUM-HIGH global | Drain normal y fatal están cerrados en plataforma; quedan semánticas globales laterales. | `RESOURCE_ECONOMY.md`, `PLATFORM_PRE_PLAYER_RESOURCE_ORDER.md`, `PLATFORM_RESOURCE_FAILURE_STATE_60.md` |
| Texto / localización | MEDIUM | Corpus JP y borrador ES estructurado; falta integración/revisión final. | `TEXT_ENGINE.md`, `docs/LOCALIZATION.md`, corpus privado Drive |
| Password | MEDIUM-HIGH | Codec/fixture y runtime `$11->$12->$13->$14` promovidos. | `PASSWORD_SYSTEM.md`, `ENGINE_STATE_FAMILY_11_14.md`, `tools/password/password_codec.py` |
| Renderer / metasprites / CHR | MEDIUM-LOW | Conocimiento parcial, sin render spec global. | `PLATFORM_CHR_MAP.md`, `BANK_MAP.md` |
| RNG | LOW-MEDIUM | Subsistema global abierto. | frontera futura |
| Audio | LOW | Sin especificación global equivalente a plataforma/combate. | frontera futura |
| REBORN moderno | EARLY | Se prioriza ORIGINAL SPEC antes de expansiones estructurales grandes. | capa futura |

## Frontera operativa actual — snapshot

`PROJECT_STATE.md` sitúa el frente activo en:

```text
ORIGINAL SPEC / engine state family $91-$99
```

El checkpoint previo PR #113 cerró agotamiento fatal Life/Cosmo:

```text
platform $20
 -> underflow Life/Cosmo
 -> $60 / $4D=$D0
 -> sólo estado alcanzable $60
 -> failure timer $D0-$DF
 -> $04=$FF / $3D / $E100
 -> defeated-Saint mask en $0673
 -> stable $00 o $90
    OR selector Saga ya conocido cuando $06CE!=0
```

El siguiente límite es el otro sucesor directo de reload promovido:

```text
reload $90 -> bootstrap -> $91
```

Este snapshot nunca sustituye el `NEXT` de `PROJECT_STATE.md`.

## Checkpoints que no deben reabrirse sin evidencia nueva

- frame persistente y familias de entidades de plataforma;
- movimiento/colisión/combate local ya promovidos;
- exits normales y especiales `$02=$00-$11`;
- `$70->$80` y narrativa `$80-$89`;
- reload `$04=$8F`;
- selector `$F025<->$A275`;
- destinos normal warm reload `$00/$10/$90`;
- dispatcher global main/NMI de PR #109;
- familia `$11-$14` de PR #111;
- fatal resource state `$60` y reload `$04=$FF` de PR #113.

Reapertura requiere fixture fallido, ROM contradictoria o efecto lateral nuevo demostrado.

## Áreas globales abiertas

1. engine family `$91-$99`;
2. familias `$30-$4F` todavía no cerradas end-to-end;
3. todos los contextos de boss battle/progresión aún no promovidos;
4. renderer/metasprites/CHR global;
5. RNG y consumidores;
6. audio / bank-track mapping;
7. integración final de localización española;
8. capa REBORN moderna.

## Recuperación

Una sesión nueva debe leer, en orden:

1. `docs/PROJECT_STATE.md` — checkpoint y único `NEXT`;
2. `docs/reverse-engineering/ENGINE_STATE_DISPATCHER.md`;
3. documentos/código/tests citados por el límite `$91-$99`;
4. `docs/WORK_PROTOCOL.md`;
5. este archivo sólo como mapa lateral;
6. manifiesto privado Drive sólo para localizar ROM/corpus/traces/save states/builds.

Nunca reconstruir `NEXT` desde una lista global de incógnitas.

# Estado global de ingeniería inversa

> **Documento de orientación global, no operativo.**
>
> Este archivo resume la madurez de los subsistemas y sirve para localizar documentación. **No decide qué se hace a continuación.** El único punto de continuación autorizado es `docs/PROJECT_STATE.md`, reconciliado con `main` y con el historial de PR/CI.

## Target primario

**Saint Seiya: Ōgon Densetsu Kanketsu Hen** — Famicom, Japón, 1988.

ROM canónica verificada:

- SHA-1: `F871D9B3DAFDDCDAD5F2ACD71044292E5169064E`
- MD5: `3B0F17C2B6EFC928B3D3FE9B1A389680`
- SHA-256: `6917B31D7343A9A17170E833BACDBC3B1EBA3E02D11C51C0D44DBE436C9AD43A`
- CRC32 del archivo iNES: `F8D258A3`
- CRC32 sin header: `9561798D`
- 128 KiB PRG + 128 KiB CHR
- mapper 1 / MMC1

Fuente detallada: `docs/reverse-engineering/CANONICAL_ROM.md`.

## Cómo leer este estado

Las etiquetas de esta página son de **madurez del subsistema**, no de certeza de cada afirmación individual:

- `HIGH`: existe especificación semántica sustancial, documentación y/o fixtures ejecutables; quedan bordes concretos.
- `MEDIUM`: hay partes importantes promovidas, pero no está cerrada toda la superficie funcional.
- `LOW`: sólo hay anclas parciales o investigación temprana.

Los niveles de evidencia `CONFIRMED / INFERRED / UNKNOWN / DISPROVEN` siguen aplicándose dentro de los documentos técnicos correspondientes.

## Mapa global vigente

| Subsistema | Madurez | Estado resumido | Entradas representativas |
|---|---|---|---|
| ROM / boot / MMC1 / bancos | HIGH | Target, vectores, wrappers MMC1 y mapa de bancos ampliamente establecidos. | `CANONICAL_ROM.md`, `BOOT_AND_MAPPER.md`, `BANK_MAP.md` |
| Índices de Saints / selección interna | MEDIUM-HIGH | Mapeos internos promovidos y reutilizados por plataforma/reload. | `CHARACTER_INDEX_MAP.md` |
| Plataforma: frame normal | HIGH | Orden persistente de frame, spawners, entidades primarias/auxiliares, hazards, interacción y timing ampliamente promovidos. | `PERSISTENT_LATE_OBJECT_FRAME.md`, `COMMON_EDGE_SPAWNER_B6D0.md`, `ENTITY_POST_INTERACTION_TIMING.md`, `ENTITY_TYPES_0A_0B.md` |
| Plataforma: movimiento / colisión / combate local | HIGH | Movimiento, salto, ataques, probes, colisiones, daño y efectos principales ya no son frentes iniciales desconocidos. | `PLATFORM_PLAYER.md`, `PLATFORM_JUMP.md`, `PLATFORM_ATTACKS.md`, `COLLISION_PROBES.md`, `COLLISION_BEHAVIOR.md`, `PLATFORM_COMBAT.md` |
| Plataforma: salida / reload / narrativa | HIGH | Familia normal `$02=$00-$10`, cadena especial `$11->$70-$89`, reload `$8F` y destinos normales `$00/$10/$90` cerrados semánticamente. | `PLATFORM_EXIT_GATES.md`, `PLATFORM_WARM_RELOAD_INTERACTIVE_STATE.md`, `PLATFORM_NORMAL_WARM_RELOAD_DESTINATIONS.md`, `PLATFORM_SPECIAL_NORMAL_EXITS.md`, `PLATFORM_NARRATIVE_STATES_80_89.md`, `PLATFORM_RELOAD_MODE_8F.md` |
| Máquina global `$00/$01` | MEDIUM | Varias familias están cerradas localmente, pero falta el mapa superior main/NMI que las particione y exponga los estados aún no modelados. | `PROJECT_STATE.md` + máquinas locales de plataforma/reload |
| Combate de jefes | MEDIUM-HIGH | Recursos, daño, dodge, técnicas, AI y event dispatch tienen investigación promovida; todavía no se declara paridad global de todos los bosses/contextos. | `BOSS_BATTLE_RESOURCES.md`, `BOSS_BATTLE_DAMAGE.md`, `BOSS_DODGE.md`, `BATTLE_TECHNIQUES.md`, `BATTLE_EVENT_DISPATCH.md` |
| Recursos Life/Cosmo/Seventh Sense | MEDIUM-HIGH | Semántica importante ya está integrada en modelos de plataforma/combate; quedan bordes globales y conversiones/contextos por cerrar. | `RESOURCE_ECONOMY.md` y documentos de plataforma/combate |
| Texto / localización | MEDIUM | Corpus japonés y borrador español estructurado con IDs estables; falta integración final completa y revisión total. | `TEXT_ENGINE.md`, `docs/LOCALIZATION.md`, `docs/localization/GLOSSARY_ES.md`; corpus privado en Drive |
| Password | MEDIUM-HIGH | Codec/documentación y fixture de compatibilidad existen y se verifican en CI; todavía no equivale a producto/UX final. | `PASSWORD_SYSTEM.md`, `tools/password/password_codec.py`, CI `Original Spec` |
| Renderer / metasprites / CHR por escena | MEDIUM-LOW | Hay conocimiento incidental y mapa CHR de plataforma, pero no un atlas/render spec global cerrado. | `PLATFORM_CHR_MAP.md`, `BANK_MAP.md`, documentos multisprite |
| RNG y consumidores | LOW-MEDIUM | No está cerrado como subsistema global. | investigación futura específica |
| Audio / bank-track mapping | LOW | No hay especificación global promovida comparable a plataforma o combate. | frontera futura |
| REBORN moderno | EARLY | La prioridad sigue siendo convertir el original en una especificación reproducible antes de expansiones estructurales grandes. | capa `REBORN` futura |

## Frontera operativa actual — sólo snapshot

A la fecha de este checkpoint (`2026-10-09`), `PROJECT_STATE.md` sitúa el frente activo en:

`ORIGINAL SPEC / global engine state dispatcher`

La familia de exits/reloads de plataforma ya está cerrada. El límite concreto ahora es construir el mapa superior de los dispatchers `$00/$01` de main/NMI, reconciliarlo con las familias locales ya promovidas y seleccionar **una sola familia de estado alcanzable que continúe sin modelar**.

Este párrafo es sólo una fotografía. Si queda desactualizado, **no debe corregirse el trabajo desde aquí**: se lee `PROJECT_STATE.md` y se actualiza este mapa después.

## Checkpoints recientes que no deben reabrirse sin evidencia nueva

- frame persistente normal de plataforma;
- reachability de familias de entidades ya promovidas;
- frontera post-exit inmediata y secuencia `$70->$80`;
- narrativa NMI `$80-$89`;
- reload narrativo `$04=$8F` hasta estado estable `$00/$00`;
- selector warm-reload `$F025 <-> $A275`;
- destinos warm-reload principales `$00/$10/$90`;
- special-normal platform exits `$02=$0C-$10` y su composición con el reload existente.

La reapertura requiere fixture fallido, evidencia contradictoria o un efecto lateral nuevo demostrado que atraviese la frontera cerrada.

## Áreas todavía globalmente abiertas

1. mapa superior completo de la máquina `$00/$01` main/NMI y clasificación de familias de estado;
2. familias globales de estado que ese mapa demuestre alcanzables pero aún no promovidas;
3. cobertura completa de renderer/metasprites/CHR por contexto;
4. RNG y todos sus consumidores;
5. cierre integral de todos los contextos de boss battle y progresión;
6. audio y mapeo de pistas/bancos;
7. integración final de localización española en el runtime/producto;
8. capa REBORN moderna: presentación, expansión y contenido deliberadamente nuevo.

## Evidencia externa secundaria

Se conservan como pistas, no como autoridad automática:

- TASVideos para identificación de revisión, RAM watch y comportamiento observable;
- traducción inglesa previa y hacks derivados como diferenciales técnicos;
- primer **Ōgon Densetsu** de 1987 sólo para genealogía del engine;
- WonderSwan Color Perfect Edition como referencia oficial comparativa/rediseño.

Toda semántica promovida debe cerrarse contra nuestra ROM japonesa canónica o una evidencia reproducible equivalente.

## Recuperación del proyecto

Una reanudación sin memoria previa debe seguir este orden:

1. `docs/PROJECT_STATE.md` — checkpoint y único `NEXT`;
2. documentos/código/tests citados por ese límite;
3. `docs/WORK_PROTOCOL.md` — reglas de ejecución y verificación;
4. este archivo — mapa global, sólo si hace falta contexto lateral;
5. manifiesto privado de Drive — sólo para localizar ROM, corpus, traces, save states o builds no versionados.

Nunca se reconstruye el `NEXT` desde una lista de `UNKNOWN` globales.

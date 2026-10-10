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
| Plataforma: movimiento / colisión / combate local | HIGH | Movimiento, salto, ataques, probes, colisiones, daño y efectos principales ya no son frentes iniciales desconocidos. El borde fatal de recursos se separa ahora como familia global `$60`. | `PLATFORM_PLAYER.md`, `PLATFORM_JUMP.md`, `PLATFORM_ATTACKS.md`, `COLLISION_PROBES.md`, `COLLISION_BEHAVIOR.md`, `PLATFORM_COMBAT.md`, `PLATFORM_PRE_PLAYER_RESOURCE_ORDER.md` |
| Plataforma: salida / reload / narrativa | HIGH | Familia normal `$02=$00-$10`, cadena especial `$11->$70-$89`, reload `$8F` y destinos normales `$00/$10/$90` cerrados semánticamente. | `PLATFORM_EXIT_GATES.md`, `PLATFORM_WARM_RELOAD_INTERACTIVE_STATE.md`, `PLATFORM_NORMAL_WARM_RELOAD_DESTINATIONS.md`, `PLATFORM_SPECIAL_NORMAL_EXITS.md`, `PLATFORM_NARRATIVE_STATES_80_89.md`, `PLATFORM_RELOAD_MODE_8F.md` |
| Máquina global `$00/$01` | MEDIUM-HIGH | Dispatcher global promovido; familia `$11-$14` cerrada incluyendo bifurcación a reload `$3D` y pantalla terminal de password `$14`. El frente activo pasa a `$60-$6F`, entrada fatal desde plataforma. | `ENGINE_STATE_DISPATCHER.md`, `ENGINE_STATE_FAMILY_11_14.md`, `EngineStateDispatcherMap.cs`, `PROJECT_STATE.md` |
| Combate de jefes | MEDIUM-HIGH | Recursos, daño, dodge, técnicas, AI y event dispatch tienen investigación promovida; todavía no se declara paridad global de todos los bosses/contextos. | `BOSS_BATTLE_RESOURCES.md`, `BOSS_BATTLE_DAMAGE.md`, `BOSS_DODGE.md`, `BATTLE_TECHNIQUES.md`, `BATTLE_EVENT_DISPATCH.md` |
| Recursos Life/Cosmo/Seventh Sense | MEDIUM-HIGH | Semántica ordinaria importante ya está integrada; la transición fatal Life/Cosmo hacia engine `$60` es el borde global activo. | `RESOURCE_ECONOMY.md`, `PLATFORM_PRE_PLAYER_RESOURCE_ORDER.md` y documentos de plataforma/combate |
| Texto / localización | MEDIUM | Corpus japonés y borrador español estructurado con IDs estables; falta integración final completa y revisión total. | `TEXT_ENGINE.md`, `docs/LOCALIZATION.md`, `docs/localization/GLOSSARY_ES.md`; corpus privado en Drive |
| Password | MEDIUM-HIGH | Codec/documentación y fixture de compatibilidad existen; además, la rama de output runtime `$11->$12->$13->$14` está ahora promovida. | `PASSWORD_SYSTEM.md`, `ENGINE_STATE_FAMILY_11_14.md`, `tools/password/password_codec.py`, CI `Original Spec` |
| Renderer / metasprites / CHR por escena | MEDIUM-LOW | Hay conocimiento incidental y mapa CHR de plataforma, pero no un atlas/render spec global cerrado. | `PLATFORM_CHR_MAP.md`, `BANK_MAP.md`, documentos multisprite |
| RNG y consumidores | LOW-MEDIUM | No está cerrado como subsistema global. | investigación futura específica |
| Audio / bank-track mapping | LOW | No hay especificación global promovida comparable a plataforma o combate. | frontera futura |
| REBORN moderno | EARLY | La prioridad sigue siendo convertir el original en una especificación reproducible antes de expansiones estructurales grandes. | capa `REBORN` futura |

## Frontera operativa actual — sólo snapshot

A la fecha de este checkpoint (`2026-10-09`), `PROJECT_STATE.md` sitúa el frente activo en:

`ORIGINAL SPEC / engine state family $60-$6F`

El dispatcher global y la familia baja `$11-$14` ya están promovidos. `$14` resultó ser un estado terminal absorbente de la presentación de password, no un puente hacia otra familia. El límite activo es ahora la transición fatal de recursos desde plataforma `$20`: Life o Cosmo agotados escriben `$00/$01=$60`, y el main `$60` parece terminar mediante reload mode `$04=$FF`, todavía no cerrado.

Este párrafo es sólo una fotografía. Si queda desactualizado, **no debe corregirse el trabajo desde aquí**: se lee `PROJECT_STATE.md` y se actualiza este mapa después.

## Checkpoints recientes que no deben reabrirse sin evidencia nueva

- frame persistente normal de plataforma;
- reachability de familias de entidades ya promovidas;
- frontera post-exit inmediata y secuencia `$70->$80`;
- narrativa NMI `$80-$89`;
- reload narrativo `$04=$8F` hasta estado estable `$00/$00`;
- selector warm-reload `$F025 <-> $A275`;
- destinos warm-reload principales `$00/$10/$90`;
- special-normal platform exits `$02=$0C-$10` y su composición con el reload existente;
- partición estructural del dispatcher global `$00/$01` main/NMI promovida por PR #109;
- familia `$11-$14`, incluyendo generación/presentación de password y absorción terminal en `$14`, promovida por PR #111.

La reapertura requiere fixture fallido, evidencia contradictoria o un efecto lateral nuevo demostrado que atraviese la frontera cerrada.

## Áreas todavía globalmente abiertas

1. familia fatal `$60-$6F` y reload mode `$04=$FF`;
2. familia alta `$91-$99` directamente sucesora del reload `$90`;
3. familias globales `$30-$4F` cuya arquitectura está presente pero no promovida de extremo a extremo;
4. cobertura completa de renderer/metasprites/CHR por contexto;
5. RNG y todos sus consumidores;
6. cierre integral de todos los contextos de boss battle y progresión;
7. audio y mapeo de pistas/bancos;
8. integración final de localización española en el runtime/producto;
9. capa REBORN moderna: presentación, expansión y contenido deliberadamente nuevo.

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
2. `docs/reverse-engineering/ENGINE_STATE_DISPATCHER.md` — mapa superior vigente;
3. `docs/reverse-engineering/ENGINE_STATE_FAMILY_11_14.md` — familia baja recién cerrada;
4. documentos/código/tests citados por el límite activo, especialmente `PLATFORM_PRE_PLAYER_RESOURCE_ORDER.md` para `$60`;
5. `docs/WORK_PROTOCOL.md` — reglas de ejecución y verificación;
6. este archivo — mapa global, sólo si hace falta contexto lateral;
7. manifiesto privado de Drive — sólo para localizar ROM, corpus, traces, save states o builds no versionados.

Nunca se reconstruye el `NEXT` desde una lista de `UNKNOWN` globales.

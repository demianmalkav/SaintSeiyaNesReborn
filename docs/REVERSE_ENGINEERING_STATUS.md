# Estado global de ingeniería inversa

> **Documento de orientación global, no operativo.**
>
> Resume madurez y localización de evidencia. **No decide el trabajo siguiente.** La única autoridad de continuación es `docs/PROJECT_STATE.md`, reconciliada con `main` y PR/CI.

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

## Convención de madurez

- `HIGH`: especificación semántica sustancial + documentación/fixtures; quedan bordes concretos.
- `MEDIUM`: partes importantes promovidas, superficie incompleta.
- `LOW`: anclas parciales/investigación temprana.

Los niveles `CONFIRMED / INFERRED / UNKNOWN / DISPROVEN` siguen aplicándose dentro de cada documento técnico.

## Mapa global vigente

| Subsistema | Madurez | Estado resumido | Entradas representativas |
|---|---|---|---|
| ROM / boot / MMC1 / bancos | HIGH | Target, vectores, wrappers y mapa PRG ampliamente establecidos. | `CANONICAL_ROM.md`, `BOOT_AND_MAPPER.md`, `BANK_MAP.md` |
| Máquina global `$00/$01` | HIGH en familias funcionales / MEDIUM-HIGH en namespace total | Dispatcher y principales familias cerrados; frente actual: censo final de reachability de huecos numéricos. | `ENGINE_STATE_DISPATCHER.md`, `PROJECT_STATE.md` |
| Front-end/title `$50` | HIGH | Modal `$0200=$00-$09`, intro, ready, attract handoff, start/password branches y exits `$30/$10` cerrados. | `ENGINE_STATE_50_FRONTEND.md`, `FrontEndState50Machine.cs` |
| Attract/presentation `$30-$4D` | HIGH | Estructura de PR #117 preservada; semántica corregida por #119: loop attract del front-end, no outer battle graph. | `ENGINE_STATE_50_FRONTEND.md`, `SCENE_BATTLE_ENGINE_STATE_30_4F.md` (nombre histórico) |
| Plataforma: frame / entidades / hazards | HIGH | Orden persistente, spawners, entidades, hazards y timing ampliamente promovidos. | `PERSISTENT_LATE_OBJECT_FRAME.md`, `ENTITY_TYPES_0A_0B.md` |
| Plataforma: movimiento / colisión / combate | HIGH | Movimiento, salto, ataques, probes, daño, drain y fatal resource transition modelados. | `PLATFORM_PLAYER.md`, `PLATFORM_JUMP.md`, `PLATFORM_ATTACKS.md`, `COLLISION_BEHAVIOR.md`, `PLATFORM_RESOURCE_FAILURE_STATE_60.md` |
| Plataforma: exits / reload / narrativa | HIGH | `$02=$00-$11`, selector, destinos `$00/$10/$90`, `$70-$89`, reload `$8F/$FF` promovidos. | `PLATFORM_EXIT_GATES.md`, `PLATFORM_NORMAL_WARM_RELOAD_DESTINATIONS.md`, `PLATFORM_NARRATIVE_STATES_80_89.md` |
| Engine `$11-$14` | HIGH | Menú/runtime password output y terminal `$14` cerrados. | `ENGINE_STATE_FAMILY_11_14.md` |
| Engine `$60` | HIGH | Fatal Life/Cosmo -> `$60` -> reload `$FF` -> destinos cerrados. | `PLATFORM_RESOURCE_FAILURE_STATE_60.md` |
| Engine `$91-$99` | HIGH | Familia completa desde reload `$90`; `$99` absorbente. | `ENGINE_STATE_FAMILY_91_99.md` |
| Combate de jefes | MEDIUM-HIGH | Recursos, daño, dodge, técnicas, AI y event dispatch promovidos; faltan contextos end-to-end antes de cierre integral. | `BOSS_BATTLE_RESOURCES.md`, `BOSS_BATTLE_DAMAGE.md`, `BOSS_DODGE.md`, `BATTLE_TECHNIQUES.md`, `BATTLE_EVENT_DISPATCH.md` |
| Life/Cosmo/Seventh Sense | HIGH en plataforma / MEDIUM-HIGH global | Drain normal/fatal cerrados; quedan contextos boss/global específicos. | `RESOURCE_ECONOMY.md`, `PLATFORM_PRE_PLAYER_RESOURCE_ORDER.md` |
| Password | HIGH en codec/runtime principal | Output, entry UI, decoder y puente front-end `$09->$08->$10` ahora unidos. | `PASSWORD_SYSTEM.md`, `ENGINE_STATE_50_FRONTEND.md` |
| Texto / localización | MEDIUM | Corpus JP y borrador ES estructurado; falta runtime final + integración/revisión española. | `TEXT_ENGINE.md`, `docs/LOCALIZATION.md`, corpus privado Drive |
| Renderer / metasprites / CHR | MEDIUM | Plataforma CHR/metasprites parcial; nueva evidencia demuestra CHR bank31 también contiene overlays ejecutables. | `PLATFORM_CHR_MAP.md`, `ENGINE_STATE_50_FRONTEND.md` |
| Executable CHR/RAM overlays | MEDIUM | Dos overlays state-$50 confirmados: CHR31 `$1000/$1400` -> RAM `$0440-$07FF`; falta censo global de otros posibles overlays. | `ENGINE_STATE_50_FRONTEND.md` |
| RNG | LOW-MEDIUM | Subsistema global abierto. | frontera futura |
| Audio | LOW | Sin especificación global equivalente a plataforma/combate. | frontera futura |
| REBORN moderno | EARLY | Se mantiene congelado hasta cerrar ORIGINAL SPEC con el criterio integral acordado. | fase futura |

## Frontera operativa actual — snapshot

`PROJECT_STATE.md` sitúa el frente activo en:

```text
ORIGINAL SPEC / global engine-state reachability closure
```

Último checkpoint técnico: PR #119.

Grafo front-end confirmado:

```text
RESET -> $50:$00->$01->$02->$03->$04->$05
Start intro ---------------------------> $05

$05 -> scripted $06 -> global $30 -> $31...$4D -> $50:$00
$05 -> Start $07 -> branch0 -> global $10
                  -> branch1 -> password $09 -> valid $08 -> restored global $10
```

La clasificación anterior de `$30-$4D` como `scene/battle` queda semánticamente supersedida. Su grafo estructural y sus fixtures siguen siendo válidos.

El frente actual cerrará ahora el namespace global de estados: valores alcanzables, bootstrap/transitorios y huecos estructurales muertos, incluyendo productores en overlays ejecutables CHR/RAM.

## Checkpoints que no deben reabrirse sin evidencia nueva

- plataforma normal, movimiento, colisión, ataques y resource drain ya promovidos;
- exits `$02=$00-$11` y reloads `$8F/$FF`;
- narrativa `$70-$89`;
- selector y destinos `$00/$10/$90`;
- dispatcher global main/NMI de PR #109;
- `$11-$14` de PR #111;
- fatal `$60` de PR #113;
- `$91-$99` de PR #115;
- estructura `$30-$4D` de PR #117;
- semántica front-end/title `$50`, attract loop y password bridge de PR #119.

Reapertura requiere fixture fallido, ROM contradictoria o efecto lateral nuevo demostrado.

## Áreas globales abiertas

1. censo final de reachability de `$00/$01`, incluidos huecos `$15-$1F`, `$21-$2F`, `$51-$5F`, high-tail y overlays CHR/RAM;
2. cobertura end-to-end de todos los contextos de boss battle/progresión;
3. renderer/metasprites/CHR global, incluido inventario completo de overlays ejecutables;
4. RNG y consumidores;
5. audio / bank-track mapping;
6. texto runtime + integración final de localización española;
7. revisión final de campos persistentes/progresión todavía provisionales;
8. REBORN sólo después del cierre integral de ORIGINAL SPEC.

## Hallazgo arquitectónico nuevo: CHR ejecutable

PR #119 demostró que el juego no trata todo CHR como gráficos.

Bank-0 `$8000/$8013` selecciona CHR 4 KiB bank31, lee `$03C0` bytes por el puerto PPU, los copia a RAM `$0440-$07FF` y ejecuta desde `$0440`.

Los dos overlays confirmados hasta ahora son:

```text
CHR31 PPU $1000-$13BF -> front-end init
CHR31 PPU $1400-$17BF -> password-entry init / producer de $0200=$09
```

Esto obliga a que cualquier auditoría de código/global-state completa incluya PRG **y** código ejecutado desde RAM cargado desde CHR.

## Recuperación

Una sesión nueva debe leer, en orden:

1. `docs/PROJECT_STATE.md` — único NEXT;
2. `docs/reverse-engineering/ENGINE_STATE_DISPATCHER.md`;
3. `docs/reverse-engineering/ENGINE_STATE_50_FRONTEND.md`;
4. documentos de familias ya promovidas sólo para reconciliar writers conocidos;
5. `docs/WORK_PROTOCOL.md`;
6. este archivo sólo como mapa lateral;
7. Drive únicamente para ROM/corpus/evidence privado.

Nunca reconstruir `NEXT` desde esta lista global de áreas abiertas.

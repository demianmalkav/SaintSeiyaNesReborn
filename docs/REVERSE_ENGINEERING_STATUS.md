# Estado global de ingeniería inversa

> Documento de orientación global, no operativo. El único `NEXT` autoritativo vive en `docs/PROJECT_STATE.md`.

## Mapa vigente

| Subsistema | Madurez | Estado resumido |
|---|---|---|
| ROM / boot / MMC1 / bancos | **HIGH / CLOSED** | Target, vectores, mapper, PRG mapping y contratos de interrupción establecidos. |
| Máquina global `$00/$01` | **HIGH / CLOSED** | Namespace completo: 59 valores producidos, 197 sin productor. |
| Front-end/title/password | **HIGH / CLOSED** | Modal, attract, codec y overlays ejecutables cerrados. |
| Plataforma / exits / reload / narrativa | **HIGH / CLOSED** | Main path, exits, `$70-$89`, `$8F` y terminal final cerrados. |
| Plataforma: player / frame / objetos / hazards | **HIGH / CLOSED** | Player, motion, attacks, auxiliares, primary A/B y late objects compuestos con fixtures. |
| Boss primitives / stage contexts | **HIGH / CLOSED** | Recursos, daño, dodge, técnicas y contextos materiales `$00-$0A`; `$0B` probado estructural. |
| Platform maps / CHR / metasprites | **HIGH / CLOSED** | Kits `$00-$11`, routing CHR estático/dinámico y visual resources cerrados. |
| Platform / global presentation | **HIGH / CLOSED-SEMANTIC** | State-$20 NMI, palette/CHR, HUD y coverage NMI global con cero gaps materiales. |
| RNG / pseudo-random source `$065F/$0660` | **HIGH / CLOSED** | PR #163: `$E0AC`, bank-sensitive `$94F0,X`, seeds/resets y consumer masks/parity cerrados. |
| Audio scheduler `$DB9C/$DBB6/$0440+` | **HIGH / CLOSED** | PR #165: slots/cues/preemption, `$04F0/$04EF`, arbitration y ownership APU cerrados. |
| Texto/localización | **HIGH / CLOSED-RUNTIME** | PR #167: extracción/codec + request tuple + contrato externo JP/ES `MSG_000..MSG_250`. |
| Auditoría integral ORIGINAL SPEC | **CLOSED / ZERO MATERIAL GAPS** | PR #169: inventario integral, ownership y regresión completa; `MATERIAL_GAP=0`. |
| ORIGINAL SPEC | **FROZEN BASELINE** | Oracle semántico; reabrir sólo por evidencia contradictoria, fixture fallida o dependencia original material no modelada. |
| REBORN architecture | **CLOSED INITIAL BOUNDARY** | PR #171: Core determinista + anti-corruption bridge + ports + save schema + slice de texto sintético. |
| REBORN gameplay / horizontal grounded | **CLOSED** | PR #173: world-space locomotion/facing, perfiles 1/1 y 1/2, bridge y parity fixtures. |
| REBORN gameplay / standing vertical | **CLOSED PENDING MERGE** | PR #175: iniciación + 30-sample standing-jump trajectory, positive-up semantic Y y paridad completa. |
| REBORN gameplay / airborne horizontal | **NEXT** | Standing-jump open-space drift: neutral/left/right + parity cadence, sin collision/landing. |

## ORIGINAL SPEC freeze

`docs/reverse-engineering/ORIGINAL_SPEC_CLOSURE_AUDIT.md` sigue siendo la matriz integral. Resultado congelado:

```text
MATERIAL_GAP = 0
```

Fuera de alcance deliberado: payload musical/SFX exacto, payload íntegro JP/ES en GitHub, paridad cycle-accurate, `$050E=$0B` como batalla dedicada y decisiones propias de REBORN.

## REBORN architecture boundary

`docs/reborn/ARCHITECTURE.md` define la arquitectura inicial:

```text
OriginalSpec --\
               > Reborn.OriginalBridge -> Reborn.Core contracts
Reborn.Core ---/

future host/adapters -------------------> Reborn.Core
```

Reglas congeladas del boundary:

- `Reborn.Core` no referencia `OriginalSpec`;
- CPU/RAM addresses, entrypoints, PRG/CHR banks y PPU/APU mechanics quedan fuera del dominio;
- `Reborn.OriginalBridge` es el único traductor entre contratos canónicos NES-facing y DTOs REBORN;
- contenido/localización entra por puertos externos; los payloads protegidos siguen privados;
- presentación/audio consumen output semántico mediante adapters;
- saves son propiedad de REBORN y versionados, no snapshots de RAM NES.

## Gameplay REBORN congelado

### Grounded horizontal — PR #173

`docs/reborn/PLATFORM_HORIZONTAL_LOCOMOTION.md` congela la proyección del split original `scroll + player_x` a `WorldX` semántico. Esto preserva locomoción/facing/cadencia sin atar el remake a la cámara NES.

### Ordinary standing jump — PR #175

`docs/reborn/PLATFORM_STANDING_JUMP.md` congela la segunda primitive de movimiento.

La curva ordinaria se proyecta a una coordenada vertical semántica positive-up:

```text
samples                 30
first rise              +8 px
peak rise               +58 px
first apex              tick 14
net rise at table end   +35 px
```

`Initiate` consume la primera muestra en el mismo logical tick, preservando el orden observable del original. La fixture recorre las 30 muestras en paralelo con `PlatformJumpInitiation` + `PlatformAirborneVerticalMotion`, verificando delta, acumulado, phase y determinismo. Los cinco Saint indices comparten esta curva standing ordinaria.

El slice corta antes del terminal fixed fall, landing, collision, directional/high jumps, air control, attacks y rendering.

Verification del code head del checkpoint:

```text
head                       731a34ddf0a94aa730375b69b56bb86eea714078
REBORN architecture #26    SUCCESS
ORIGINAL SPEC tests #452   SUCCESS
```

## Frontera operativa actual

La siguiente pieza acotada es **standing-jump airborne horizontal control en espacio libre**. `PlatformAirborneHorizontalMotion` ya está congelado como oracle: para un salto vertical ordinario, el drift horizontal depende de la paridad lógica y mantiene prioridad Right-before-Left. REBORN debe proyectarlo a `WorldX` y componerlo con la trayectoria vertical ya cerrada, sin importar el split player-X/scroll ni collision descriptors al Core.

La evolución visual total del remake sigue fuera de las restricciones NES: aquí se preserva comportamiento, no limitaciones gráficas o de cámara.

El `NEXT` exacto y sus criterios viven exclusivamente en `docs/PROJECT_STATE.md`.

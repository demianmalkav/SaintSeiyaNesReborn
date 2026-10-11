# Estado global de ingeniería inversa

> Documento de orientación global, no operativo. El único `NEXT` autoritativo vive en `docs/PROJECT_STATE.md`.

## Mapa vigente

| Subsistema | Madurez | Estado resumido |
|---|---|---|
| ROM / boot / MMC1 / bancos | **HIGH / CLOSED** | Target, vectores, mapper, PRG mapping y contratos de interrupción establecidos. |
| Máquina global `$00/$01` | **HIGH / CLOSED** | Namespace completo: 59 valores producidos, 197 sin productor. |
| Front-end/title/password | **HIGH / CLOSED** | Modal, attract, codec y overlays ejecutables cerrados. |
| Plataforma / exits / reload / narrativa | **HIGH / CLOSED** | Main path, exits, reload y terminal final cerrados. |
| Plataforma: player / frame / objetos / hazards | **HIGH / CLOSED** | Player, motion, attacks, auxiliares y objetos compuestos con fixtures. |
| Boss primitives / stage contexts | **HIGH / CLOSED** | Recursos, daño, dodge, técnicas y contextos materiales cerrados. |
| Platform maps / CHR / metasprites | **HIGH / CLOSED** | Routing visual y recursos semánticos cerrados. |
| Platform / global presentation | **HIGH / CLOSED-SEMANTIC** | NMI, palette/CHR, HUD y coverage global sin gaps materiales. |
| RNG | **HIGH / CLOSED** | Fuente, seeds/resets y consumidores cerrados. |
| Audio scheduler | **HIGH / CLOSED** | Slots/cues/preemption, arbitration y ownership cerrados. |
| Texto/localización | **HIGH / CLOSED-RUNTIME** | Request tuple + contrato externo JP/ES `MSG_000..MSG_250`. |
| Auditoría integral ORIGINAL SPEC | **CLOSED / ZERO MATERIAL GAPS** | PR #169: `MATERIAL_GAP=0`. |
| ORIGINAL SPEC | **FROZEN BASELINE** | Oracle semántico; reabrir sólo por evidencia contradictoria o fixture fallida. |
| REBORN architecture | **CLOSED INITIAL BOUNDARY** | PR #171: Core determinista + bridge + ports + save schema. |
| REBORN gameplay / horizontal grounded | **CLOSED** | PR #173: world-space locomotion/facing y perfiles de cadencia. |
| REBORN gameplay / standing vertical | **CLOSED** | PR #175: ordinary standing 30-sample trajectory. |
| REBORN gameplay / standing air control | **CLOSED** | PR #177: free-space 0/1 parity drift y composición vertical-first. |
| REBORN gameplay / high standing jump | **CLOSED** | PR #179: takeoff semántico + tres familias high-jump completas. |
| REBORN gameplay / directional jump vertical | **NEXT** | Takeoff identity + perfiles verticales; sin forced horizontal trajectory/collision. |

## ORIGINAL SPEC freeze

`docs/reverse-engineering/ORIGINAL_SPEC_CLOSURE_AUDIT.md` sigue siendo la matriz integral. Resultado congelado:

```text
MATERIAL_GAP = 0
```

ORIGINAL SPEC sigue siendo el oracle. REBORN puede modernizar diseño, presentación, cámara y contenido sin reescribir evidencia histórica.

## REBORN architecture boundary

```text
OriginalSpec --\
               > Reborn.OriginalBridge -> Reborn.Core contracts
Reborn.Core ---/

future host/adapters -------------------> Reborn.Core
```

`Reborn.Core` no referencia OriginalSpec ni conceptos de CPU/RAM/bancos/PPU/APU. La traducción canónica termina en `Reborn.OriginalBridge`.

## Gameplay REBORN congelado

### Grounded horizontal — PR #173

`PLATFORM_HORIZONTAL_LOCOMOTION.md` proyecta el split original `scroll + player_x` a `WorldX` semántico y preserva facing/cadencia sin atar REBORN a la cámara NES.

### Ordinary standing vertical — PR #175

`PLATFORM_STANDING_JUMP.md` congela la curva ordinaria positive-up de 30 muestras y same-frame initiation.

### Ordinary standing air control — PR #177

`PLATFORM_STANDING_JUMP_AIR_CONTROL.md` congela steering aéreo ordinario en espacio libre: 0/1 por paridad, facing de takeoff preservado y orden `vertical -> horizontal -> phase`.

### High standing jump — PR #179

`PLATFORM_HIGH_STANDING_JUMP.md` proyecta el high jump a tres familias semánticas completas:

```text
Seiya           58 ticks   peak +103   apex 28   net +51
Shun / Ikki     48 ticks   peak  +88   apex 23   net +51
Hyoga / Shiryu  38 ticks   peak  +71   apex 18   net +41
```

Todas comienzan con +9 px en el tick de despegue. `RebornHighStandingJumpTakeoffIntent` exige jump + upward intent + takeoff horizontal neutral. `OriginalSpecHighStandingJumpBridge` contiene la selección canónica por Saint/input. La fixture recorre los cinco Saints contra `PlatformJumpInitiation` + `PlatformAirborneVerticalMotion`, valida las secuencias completas y corta antes del terminal fall.

Verification del checkpoint:

```text
code head                  71452b23ec179ce975b6340c58913cf753b07771
final PR head              adff7cc1b107011b478c4afd0f035dd0e0f3d1b4
merge                      d9ed0be3cdc95cbdac766ee488325666fc7e10a3
REBORN architecture #47    SUCCESS
ORIGINAL SPEC tests #466   SUCCESS
```

## Frontera operativa actual

La siguiente pieza acotada es **directional jump takeoff + vertical profiles**. El oracle ya separa Right, Left y both-held en takeoff y demuestra familias verticales Saint-specific. El próximo slice debe preservar identidad de despegue y la curva vertical completa, pero dejar para después la trayectoria horizontal forzada, counter-steer, collision y landing.

La evolución visual total del remake sigue fuera de las restricciones NES: aquí se preserva comportamiento, no limitaciones gráficas o de cámara.

El `NEXT` exacto y sus criterios viven exclusivamente en `docs/PROJECT_STATE.md`.

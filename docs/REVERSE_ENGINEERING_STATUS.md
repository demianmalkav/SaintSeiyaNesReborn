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
| REBORN gameplay | **ACTIVE / FIRST SLICE** | Próximo checkpoint: locomoción horizontal/facing del jugador con paridad semántica acotada. |

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
- `RebornRuntime.Step` usa ticks lógicos deterministas y no wall-clock time;
- contenido/localización entra por puertos externos; los payloads protegidos siguen privados;
- presentación/audio consumen output semántico mediante adapters;
- saves son propiedad de REBORN y versionados, no snapshots de RAM NES.

Primer slice de arquitectura probado: request de texto canónico -> bridge -> `RebornTextRequest` -> runtime determinista -> resolución JP/ES -> `RebornFrameOutput`, usando sólo strings sintéticos.

Verification inicial del código de arquitectura:

```text
head                    6104ae5815b1612834e4008418024b385a77c8f7
REBORN architecture #2  SUCCESS
ORIGINAL SPEC tests #438 SUCCESS
```

## Frontera operativa actual

El siguiente trabajo ya es gameplay REBORN, pero continúa acotado: **locomoción horizontal/facing del jugador de plataforma**.

Debe apoyarse en los contratos/fixtures congelados de player control y horizontal motion, traduciendo sólo la semántica mínima necesaria al Core moderno. Se excluyen del mismo checkpoint collision resolution, hazards, maps/exits, attacks, salto/vertical motion, animation assets y rendering.

El `NEXT` exacto y sus criterios viven exclusivamente en `docs/PROJECT_STATE.md`.

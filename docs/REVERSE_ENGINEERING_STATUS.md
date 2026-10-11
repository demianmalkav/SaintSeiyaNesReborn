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
| Audio scheduler `$DB9C/$DBB6/$0440+` | **HIGH / CLOSED** | PR #165: ocho slots, cue loader/preemption, `$04F0/$04EF`, `$8B50+` arbitration y ownership APU cerrados. |
| Texto/localización | **HIGH / CLOSED-RUNTIME** | PR #167: extracción/codec + request tuple `$066A/$066B/$0672` + contrato externo JP/ES `MSG_000..MSG_250`. |
| Auditoría integral ORIGINAL SPEC | **CLOSED / ZERO MATERIAL GAPS** | PR #169 audit: inventario integral, ownership y regresión completa; `MATERIAL_GAP=0`. |
| ORIGINAL SPEC | **FROZEN BASELINE** | Reabrir sólo por evidencia canónica contradictoria, fixture fallida o dependencia original no modelada descubierta durante REBORN. |
| REBORN | **READY FOR ARCHITECTURE** | Desbloqueado para planificación/arquitectura; todavía no existe implementación REBORN bajo `src/`. |

## Cierre integral

`docs/reverse-engineering/ORIGINAL_SPEC_CLOSURE_AUDIT.md` clasifica cada superficie material como `CLOSED`, `INTENTIONALLY_OUT_OF_SCOPE` o `MATERIAL_GAP`.

Resultado:

```text
MATERIAL_GAP = 0
```

No se detectó comportamiento material de gameplay/runtime sin propietario entre:

- boot/mapper/bancos y máquina global;
- front-end/password;
- plataforma, control, objetos, hazards, exits/reload y narrativa;
- mapas/CHR/metasprites/HUD/NMI/presentación;
- battle/event, recursos, daño, dodge, técnicas y contextos;
- RNG;
- scheduler de audio;
- motor de texto y boundary runtime de localización.

Quedan explícitamente fuera del requisito de cierre:

- payload musical/SFX original exacto;
- payload íntegro de diálogo JP/ES en GitHub;
- paridad instrucción-a-instrucción/cycle-accurate;
- `$050E=$0B` como batalla dedicada, porque está probado como estructural/transitorio;
- decisiones y expansiones propias de REBORN.

Estas exclusiones no son gaps materiales de ORIGINAL SPEC.

## Último checkpoint técnico previo al audit

PR #167 cerró la integración runtime de texto:

```text
E7B3 -> 066A / 0672=FF
E7B7 -> 066A / 0672=00
E7C3 -> 066B / 0672=FF
E7C7 -> 066B / 0672=00
identity  MSG_000..MSG_250
count     251 exactos
runtime   catálogo CSV externo JP/ES
fallback  ES -> JP sólo bajo política explícita
```

`$0672` permanece metadata raw de request/presentación; no recibe una semántica más estrecha sin nueva evidencia.

## Política de congelado

No reabrir por deuda documental histórica ni por deseo de mayor fidelidad cosmética. Un subsistema congelado sólo vuelve a ORIGINAL SPEC ante:

1. evidencia canónica contradictoria;
2. una fixture/oracle existente que falle;
3. una dependencia semántica original material descubierta al construir REBORN.

En ese caso se crea un checkpoint ORIGINAL SPEC acotado y se vuelve a congelar antes de propagar el cambio a REBORN.

## Frontera operativa actual

ORIGINAL SPEC deja de ser la cola principal de trabajo y pasa a funcionar como **oracle/baseline semántico**.

La frontera global siguiente es **REBORN architecture planning**: definir la primera arquitectura moderna consumidora de ORIGINAL SPEC, los límites entre dominio, runtime, presentación y contenido/localización, la estrategia de tests de paridad y el primer vertical slice. Esa planificación debe preceder a una migración amplia de código o gameplay.

El `NEXT` exacto y sus criterios viven exclusivamente en `docs/PROJECT_STATE.md`.

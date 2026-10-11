# Estado global de ingeniería inversa

> Documento de orientación global, no operativo. El único `NEXT` autoritativo vive en `docs/PROJECT_STATE.md`.

## Mapa vigente

| Subsistema | Madurez | Estado resumido |
|---|---|---|
| ROM / boot / MMC1 / bancos | HIGH | Target, vectores, mapper, PRG mapping y contratos de interrupción establecidos. |
| Máquina global `$00/$01` | **HIGH / CLOSED** | Namespace completo: 59 valores producidos, 197 sin productor. |
| Front-end/title/password | **HIGH / CLOSED** | Modal, attract, codec y overlays ejecutables cerrados. |
| Plataforma / exits / reload / narrativa | **HIGH / CLOSED** | Main path, exits, `$70-$89`, `$8F` y terminal final cerrados. |
| Plataforma: main-thread frame / late objects | **HIGH / CLOSED** | Producers, player, auxiliares, primary A/B y late objects compuestos. |
| Boss primitives / stage contexts | **HIGH / CLOSED** | Recursos, daño, dodge, técnicas y contextos materiales `$00-$0A`; `$0B` estructural. |
| Platform maps / CHR / metasprites | **HIGH / CLOSED** | Kits `$00-$11`, routing CHR estático/dinámico y visual resources cerrados. |
| Platform / global presentation | **HIGH / CLOSED-SEMANTIC** | State-$20 NMI, palette/CHR, HUD y coverage NMI global con cero gaps materiales. |
| RNG / pseudo-random source `$065F/$0660` | **HIGH / CLOSED** | PR #163: `$E0AC`, bank-sensitive `$94F0,X`, seeds/resets y consumer masks/parity cerrados. |
| Audio scheduler `$DB9C/$DBB6/$0440+` | **HIGH / CLOSED** | PR #165: ocho slots, cue loader/preemption, `$04F0/$04EF`, `$8B50+` arbitration y ownership APU cerrados. |
| Texto/localización | **HIGH / CLOSED-RUNTIME** | PR #167: extracción/codec + request tuple `$066A/$066B/$0672` + contrato externo JP/ES `MSG_000..MSG_250`. |
| Auditoría integral ORIGINAL SPEC | **ACTIVE** | Inventario, reconciliación de gaps históricos y verificación integral antes de liberar REBORN. |
| REBORN | EARLY / FROZEN | Congelado hasta que la auditoría integral confirme cero gaps materiales o los reduzca a checkpoints acotados. |

## Último checkpoint técnico — PR #167

La integración runtime de texto queda cerrada sin incorporar payloads originales o traducidos al repositorio público.

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

`$0672` permanece como metadata raw de request/presentación; no se le asigna semántica más estrecha sin evidencia canónica adicional.

Verification before state update:

```text
PR    #167
head  1fe8ba3c352f02ba345b4861fc5ee7650011df15
CI    Original Spec #630 SUCCESS
      ORIGINAL SPEC tests #426 SUCCESS
```

Artifacts: `CanonicalRuntimeLocalization.cs`, self-test fixtures, `CANONICAL_RUNTIME_TEXT_CONTENT.md`, `TEXT_ENGINE.md` reconciliado y `LOCALIZATION.md` reconciliado. Los fixtures usan únicamente strings sintéticos; el guion JP/ES sigue privado.

## Frontera operativa actual

La siguiente frontera global es la **auditoría integral de cierre de ORIGINAL SPEC**.

No consiste en reconstruir otro subsistema por defecto. Debe comprobar que todos los bloques materiales ya reconstruidos tienen ownership, evidencia y regresión coherentes; que los `TODO`/`OPEN` históricos no representen gaps reales ya resueltos; y que no haya contradicciones entre documentación, clean-room contracts y fixtures.

Resultado permitido de la auditoría:

```text
A) cero MATERIAL_GAP -> ORIGINAL SPEC puede congelarse y REBORN se desbloquea
B) uno o más MATERIAL_GAP -> ranking finito + un único NEXT acotado al gap de mayor impacto
```

## No reabrir sin evidencia nueva

- global `$00/$01` reachability;
- global NMI presentation — #161;
- battle/event coverage — #149 and stage PRs;
- platform main-thread / exits / narrative;
- maps / CHR / visual definitions — #151/#156;
- state-$20 NMI — #154;
- HUD `$9D69-$9EED` — #159;
- canonical RNG `$E0AC/$065F/$0660` — #163;
- audio scheduler `$DB9C/$DBB6/$0440+` — #165;
- runtime text/content integration `MSG_000..MSG_250` — #167;
- promoted boss damage/resources/dodge/technique boundaries.

## Áreas abiertas

1. auditoría integral final de ORIGINAL SPEC;
2. cualquier gap material que esa auditoría demuestre, no gaps históricos presumidos;
3. full audio content queda opcional/separado salvo que la auditoría demuestre que bloquea comportamiento material;
4. REBORN sólo después del cierre integral.

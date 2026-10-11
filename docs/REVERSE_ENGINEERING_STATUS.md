# Estado global de ingeniería inversa

> Documento de orientación global, no operativo. El único `NEXT` autoritativo vive en `docs/PROJECT_STATE.md`.

## Mapa vigente

| Subsistema | Madurez | Estado resumido |
|---|---|---|
| ROM / boot / MMC1 / bancos | HIGH | Target, vectores, mapper, PRG mapping y contrato de interrupción MMC1 establecidos. |
| Máquina global `$00/$01` | **HIGH / CLOSED** | Namespace completo: 59 valores producidos, 197 sin productor. |
| Front-end/title/password | **HIGH / CLOSED** | Modal, attract, codec y overlays ejecutables cerrados. |
| Plataforma / exits / reload / narrativa | **HIGH / CLOSED** | Main path, common/special exits, `$70-$89`, `$8F` y terminal final cerrados. |
| Plataforma: main-thread frame / late objects | **HIGH / CLOSED** | Producers, player, `$9B93`, auxiliares, primary A/B, `$A22C` y `$3C` compuestos. |
| Boss primitives | HIGH | Recursos, clasificadores, daño, técnicas, dodge y dispatchers cerrados en sus boundaries promovidos. |
| Cobertura stage-local `$00-$0B` | **CLOSED** | Todos los contextos materiales `$00-$0A` cerrados; `$0B` estructural/transitorio; PR #149. |
| Mu / repair `$00` | HIGH | Contexto especial completo, PR #141. |
| Taurus/Aldebaran `$01` | HIGH | Contexto completo, PR #123. |
| Gemini / first Camus `$02` | HIGH | Compuesto `$0E` + split Hyoga/ordinario cerrado, PR #143. |
| Cancer/Death Mask `$03` | HIGH | Contexto completo, PR #145. |
| Leo/Aioria `$04` | HIGH | Contexto completo, PR #125. |
| Virgo/Shaka `$05` | HIGH | Contexto completo, PR #127. |
| Scorpio/Milo `$06` | HIGH | Contexto + bridge progress `$08` / stage `$10` / platform `$08` cerrados, PR #147. |
| Capricorn/Shura `$07` | HIGH | Shiryu-only init, Talk/battle, retry y `$FE`->Aquarius cerrados, PR #149. |
| Aquarius/Camus `$08` | HIGH | Dos encuentros Camus y unlocks Hyoga cerrados, PR #129. |
| Pisces/Aphrodite `$09` | HIGH | Roster, Talk, Shun growth, selector y `$FE->$0C` cerrados, PR #131. |
| Saga `$0A` | HIGH | Máquina `$06CE` completa y terminales cerrados, PR #135. |
| Stage `$0B` | CLOSED-STRUCTURAL | Sin entrada estable canónica; usos transitorios/presentación. |
| Final-special `$0C` | HIGH | Rosas, command ownership y handoff a Saga cerrados, PR #133. |
| Post-Saga / ending tail | HIGH | `$0E->$11->$70-$89->$8F->$BC39->$BD2F` hard terminal, PR #137. |
| Plataforma: mapas/metatiles/CHR routing | **HIGH / CLOSED** | Kits `$00-$11`, CHR0/CHR1 estáticos y overrides CHR0 dinámicos `$9915/$9966` cerrados. |
| Metasprites / recursos visuales platform | **HIGH / CLOSED-MECHANICAL** | Shared player/entity `$B987`, tipos `$05-$0F`, `$B647`, `$A908`, `$9B93`, auditor y renderer ROM-fed cerrados, PR #151. |
| Platform NMI `$00=$20` | **HIGH / CLOSED** | OAM DMA, streamer `$D7F2`, pause `$D988`, epílogo `$D367+` y restauración mapper cerrados, PR #154. |
| Platform palette / CHR refresh `$9915` | **HIGH / CLOSED** | Gates/latches, `$9EEF/$9F29/$9D58`, general profile managers, `$0D` background palette y `$9966` cerrados, PR #156. |
| Platform HUD/status NMI `$9D69+` | **HIGH / CLOSED** | Cuatro fases `$73`, dígitos BCD, gauges Life/Cosmo/Seventh Sense y helpers `$9E73-$9EED` cerrados, PR #159. |
| Renderer / frame composition global | **HIGH / CLOSED-SEMANTIC** | PR #161: 59 estados producidos, 16 clases NMI, cero ramas producidas sin owner y cero gaps materiales. |
| RNG / pseudo-random source `$065F/$0660` | **ACTIVE** | Updater `$E0AC` y varios consumidores confirmados; falta cerrar bank-context de `$94F0,X`, initialization y callsite inventory. |
| Audio | LOW | Abierto; se mantiene fuera del checkpoint RNG. |
| Texto/localización | MEDIUM | Corpus JP + borrador ES; integración runtime final pendiente. |
| Auditoría integral ORIGINAL SPEC | PENDING | Se ejecutará después de cerrar subsistemas globales restantes. |
| REBORN | EARLY | Congelado hasta cierre integral de ORIGINAL SPEC. |

## Último checkpoint técnico — PR #161

La cobertura global del dispatcher NMI `$D269+` quedó cerrada contra el namespace canónico completo.

Resultado:

```text
produced states         59
structural unreachable 197
canonical NMI routes    16
unclassified produced   0
material renderer gaps  0
```

Mirror priority:

```text
$01=$50 -> $DABC
$01=$3D -> $E000
else    -> live $00 dispatch
```

El audit encontró y corrigió un Oracle previo: live `$00=$00` ejecuta `$D29B JMP $D382`, no el entry ordinario `$D367`. La corrección se aisló en `7f90041d4416021a07b7dd9017a729ec48a352af` antes del manifest de cobertura.

La familia compartida bank-1 `$8C19` queda clasificada en sus tres contextos cerrados: attract `$40-$4D`, post-exit `$73`, y narrativa `$80-$89`. El estado `$60` llama bank-1 `$9D69` y conserva ese banco hasta la restauración persistente `$3B` del tail común.

Technical checkpoint:

```text
PR    #161
merge 96fee8a49b81dfc85818dd7dd1603f0cfdc3af4f
head  6158aef4700c3c986516528c9410bf8950eae5d2
CI    #412 SUCCESS / #617 SUCCESS
```

No ROM, texto/tile payload extraído, captura ni evidencia binaria privada fue versionada.

## Frontera operativa actual

El renderer/presentation deja de ser un área abierta. La frontera siguiente es el source pseudo-random/phase de `$065F/$0660`.

Direct ROM reconnaissance:

```text
$E0AC LDX $0660
$E0AF LDA $94F0,X
$E0B3 ADC $065F
$E0B6 STA $065F
$E0B9 INC $0660
```

Hay consumidores confirmados con máscaras `$01/$03/$07/$0F`, y `$F995` usa `$0660&1` para la dirección peligrosa del dodge. La dirección `$94F0` está en la ventana PRG bankeable; no se debe asumir una tabla fija hasta demostrar el banco visible en cada update.

## No reabrir sin evidencia nueva

- global `$00/$01` reachability — cerrado;
- global NMI presentation coverage — #161;
- coverage battle/event `$00-$0B` — #149;
- Mu `$00` — #141;
- Taurus `$01` — #123;
- Gemini / first Camus `$02` — #143;
- Cancer `$03` — #145;
- Leo `$04` — #125;
- Virgo `$05` — #127;
- Scorpio `$06` — #147;
- Capricorn `$07` — #149;
- Aquarius `$08` — #129;
- Pisces `$09` — #131;
- final-special `$0C` — #133;
- Saga `$0A` — #135;
- post-Saga ending/hard terminal — #137;
- front-end/title/password — cerrado;
- `$11-$14`, `$60`, `$91-$99` state families — cerrado;
- platform exits/reload/narrativa — cerrado;
- platform main-thread persistent late-object frame — cerrado;
- map kits `$00-$11` / static+dynamic CHR routing — cerrado;
- platform visual-resource definitions — #151;
- platform state-`$20` NMI — #154;
- platform palette/dynamic CHR refresh `$9915` — #156;
- platform HUD/status `$9D69-$9EED` — #159;
- boss damage/resources/dodge/techniques genéricos — cerrados en sus promoted boundaries.

## Áreas abiertas

1. RNG / pseudo-random source `$E0AC`, `$065F/$0660`, bank context y consumers;
2. audio;
3. texto runtime/localización final;
4. auditoría integral final de ORIGINAL SPEC;
5. REBORN sólo después del cierre integral.

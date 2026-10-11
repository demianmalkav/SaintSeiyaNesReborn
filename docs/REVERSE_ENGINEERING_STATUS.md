# Estado global de ingeniería inversa

> Documento de orientación global, no operativo. El único `NEXT` autoritativo vive en `docs/PROJECT_STATE.md`.

## Mapa vigente

| Subsistema | Madurez | Estado resumido |
|---|---|---|
| ROM / boot / MMC1 / bancos | HIGH | Target, vectores, mapper, PRG mapping y contrato de interrupción MMC1 establecidos. |
| Máquina global `$00/$01` | HIGH | Namespace completo cerrado, PR #121. |
| Front-end/title/password | HIGH | Modal, attract, codec y overlays ejecutables cerrados. |
| Plataforma / exits / reload / narrativa | HIGH | Main path, common/special exits, `$70-$89`, `$8F` y terminal final cerrados. |
| Plataforma: main-thread frame / late objects | HIGH | Producers, player, `$9B93`, auxiliares, primary A/B, `$A22C` y `$3C` compuestos. |
| Boss primitives | HIGH | Recursos, clasificadores, daño, técnicas, dodge y dispatchers. |
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
| Renderer / frame composition global | **ACTIVE-AUDIT** | Cadena platform completa; falta inventario de cobertura NMI de estados globales no-platform antes de declarar cierre de presentación. |
| RNG | LOW-MEDIUM | Abierto. |
| Audio | LOW | Abierto. |
| Texto/localización | MEDIUM | Corpus JP + borrador ES; integración runtime final pendiente. |
| Auditoría integral ORIGINAL SPEC | PENDING | Se ejecutará después de cerrar subsistemas globales restantes. |
| REBORN | EARLY | Congelado hasta cierre integral de ORIGINAL SPEC. |

## Último checkpoint técnico — PR #159

La subfase HUD/status bank-1 `$9D69-$9EED` quedó cerrada.

Resultado principal:

```text
entry:
  $2000=0
  $73=($73+1)&3

phase0:
  $22F0 Cosmo digits
  $2330 Life digits
  $236F Seventh Sense digits if $02!=0
  digit tile = $80+nibble

phase1:
  $22F4 Cosmo gauge
  cap width = low nibble $6D+$03
  full hundreds = $BF

phase2:
  $2334 Life gauge
  cap width = high nibble $6D+$03
  full hundreds = $BF

partial classifier $9E84:
  00-04 A7
  05-24 B1
  25-36 B2
  37-49 B3
  50-61 B4
  62-74 B5
  75-86 B6
  87-99 BF

phase3:
  $2374 Seventh Sense gauge if $02!=0
  ten $A7 clear tiles
  thousands -> $BE
  fraction uses hundreds+tens, ignores ones
  partial family A7/B8-BD/BE
  scratch $39 = tens digit
```

Technical checkpoint:

```text
PR    #159
merge 0be81b3b2be10d47a20364d98b10c76f16b834f8
head  134f7796bd1f57ae6eb961e4bcef83a013b662ee
CI    #408 SUCCESS / #613 SUCCESS
```

No ROM, CHR/palette/nametable payload, OAM dump o captura fue versionado.

## Frontera operativa actual

La cadena de presentación de plataforma está cerrada semánticamente:

```text
main-thread OAM
 -> NMI DMA / map streamer / pause
 -> palette + dynamic CHR `$9915`
 -> HUD/status `$9D69`
 -> common PPU commit `$D367+`
 -> mapper restore / RTI
```

La siguiente frontera no es otra rutina platform conocida, sino la **cobertura global del dispatcher NMI `$D269+`**. Hay que demostrar qué branches de estados globales no-platform ya quedan representados por specs existentes y cuáles, si alguno, siguen siendo materialmente desconocidos.

## No reabrir sin evidencia nueva

- global `$00/$01` — #121;
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
- front-end/title — #119;
- attract — #117/#119;
- `$11-$14`, `$60`, `$91-$99` — #111/#113/#115;
- platform exits/reload/narrativa;
- platform main-thread persistent late-object frame;
- map kits `$00-$11` / static+dynamic CHR routing;
- platform visual-resource definitions — #151;
- platform state-`$20` NMI — #154;
- platform palette/dynamic CHR refresh `$9915` — #156;
- platform HUD/status `$9D69-$9EED` — #159;
- boss damage/resources/dodge/techniques genéricos.

## Áreas abiertas

1. auditoría global de cobertura NMI/presentation no-platform;
2. cualquier branch de presentación material que esa auditoría demuestre realmente abierto;
3. RNG;
4. audio;
5. texto runtime/localización final;
6. auditoría integral final de ORIGINAL SPEC;
7. REBORN sólo después del cierre integral.

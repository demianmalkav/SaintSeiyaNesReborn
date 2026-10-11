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
| Plataforma: mapas/metatiles/CHR routing | HIGH | Kits `$00-$11`, CHR0 sprites / CHR1 background y bancos por substate establecidos. |
| Metasprites / recursos visuales platform | **HIGH / CLOSED-MECHANICAL** | Shared player/entity `$B987`, tipos `$05-$0F`, `$B647`, `$A908`, `$9B93`, auditor y renderer ROM-fed cerrados, PR #151. |
| Platform NMI / PPU presentation boundary | **ACTIVE** | OAM DMA y NMI global conocidos; falta cerrar exactamente la rama `$00=$20`, `$D7F2/$D988` y epílogo `$D367+`. |
| Renderer / frame composition global | MEDIUM-HIGH | Main-thread y recursos mecánicos cerrados; presentación NMI/PPU global todavía incompleta. |
| RNG | LOW-MEDIUM | Abierto. |
| Audio | LOW | Abierto. |
| Texto/localización | MEDIUM | Corpus JP + borrador ES; integración runtime final pendiente. |
| Auditoría integral ORIGINAL SPEC | PENDING | Se ejecutará después de cerrar subsistemas globales restantes. |
| REBORN | EARLY | Congelado hasta cierre integral de ORIGINAL SPEC. |

## Último checkpoint técnico — PR #151

La capa mecánica de recursos visuales platform quedó cerrada.

Resultado principal:

```text
$B987 = compositor compartido
  índice $00-$04 -> Saints internos
  índice $05-$0F -> tipos primary entity

pointer-table families:
  B671 B699 B6C1 B6E9 B711 B739
  B761 B789 B7B1 B7D9 B801

direct hidden/flash:
  B647

canonical audit:
  121/121 primary pointer selections válidos
  51 definiciones primarias distintas

max sprites:
  05=10 06=10 07=10 08=11 09=9  0A=6
  0B=7  0C=7  0D=12 0E=4  0F=4

0D exception:
  B761 -> B324 -> 12 sprites
  coincide con A647 retirando +2C/+2D extra

A908 direct attached visual:
  C0E3 tile
  C0EF vertical/Y offset
  visual sólo 05/06/08/09/0C

9B93 direct bootstrap resources:
  PRG bank 3
  9B65 / 9B6C / 9B73 / 9B8F
```

El selector clean-room reproduce también la mutación de `$28` en el path dinámico `$BA62` cuando `$03B9==0`.

Technical checkpoint:

```text
PR    #151
merge f0e36cf9b3f73988ac9b59b62aa93f9b269f4042
head  fe2d0e16a3ace099be4d580c4878abf86c4ba5d6
CI    #388 SUCCESS / #597 SUCCESS
```

No se versionó ROM, CHR extraído ni PNG generado.

## Frontera operativa actual

La próxima frontera es **platform state `$20` NMI / presentación PPU**.

Ya está congelado:

- vector NMI `$C000 -> $D269`;
- prologue de NMI, contrato `$3A/$3B`, reset serial MMC1;
- OAM shadow page `$0700-$07FF` y DMA `$4014=$07`;
- main-thread platform frame completo hasta late objects y `$3C`;
- CHR0/CHR1 routing y definiciones de sprite;
- epílogo común visible alrededor de `$D367+` con mirrors `$77/$78`, scroll `$44/$46` y restauración de PRG bank `$3B`.

La rama platform `$00=$20` llama:

```text
$D2BA JSR $D7F2
$D2BD JSR $D988
$D2C0 JMP $D367
```

El trabajo inmediato es volver a clasificar `$D7F2/$D988` desde bytes canónicos y cerrar la frontera semántica OAM-DMA/PPU, corrigiendo cualquier etiqueta histórica que contradiga el ROM.

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
- map kits `$00-$11` / CHR0/CHR1 routing;
- platform visual-resource definitions — #151;
- boss damage/resources/dodge/techniques genéricos.

## Áreas abiertas

1. platform state `$20` NMI / OAM DMA / PPU-control-scroll commit;
2. resto de renderer/palette/frame presentation global que quede después de esa frontera;
3. RNG;
4. audio;
5. texto runtime/localización final;
6. auditoría integral final de ORIGINAL SPEC;
7. REBORN sólo después del cierre integral.

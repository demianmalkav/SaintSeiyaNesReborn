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
| Platform HUD/status NMI `$9D69+` | **ACTIVE** | Cuatro fases `$73` y helpers visibles; falta promover writer ejecutable y composición final con `$9915`. |
| Renderer / frame composition global | HIGH-MEDIUM | Main thread, OAM DMA, map streaming, sprites y palette/CHR cerrados; HUD platform aún activo y otros estados globales quedan para auditoría. |
| RNG | LOW-MEDIUM | Abierto. |
| Audio | LOW | Abierto. |
| Texto/localización | MEDIUM | Corpus JP + borrador ES; integración runtime final pendiente. |
| Auditoría integral ORIGINAL SPEC | PENDING | Se ejecutará después de cerrar subsistemas globales restantes. |
| REBORN | EARLY | Congelado hasta cierre integral de ORIGINAL SPEC. |

## Último checkpoint técnico — PR #156

La subfase bank-1 de palette/CHR invocada por el NMI de plataforma quedó cerrada.

Resultado principal:

```text
$9915 top gate:
  $07C0 == FE -> $996C fallback
  $03A4 != FF -> $996C fallback
  else        -> one-shot special refresh

$03A4 arming:
  only substates 0C-11
  camera page $45=0A
  low thresholds A0/E0/D0/D0/D0/A5
  substate10 + internal Shun excluded
  successful refresh FF -> FE, no rearm until reset

sprite palette:
  $3F10-$3F1F
  4 pointer pairs $0392-$0399
  each descriptor = 3 bytes
  transfer = $0F + 3 bytes × 4

special fourth descriptor:
  non-10 -> $9960
  10     -> $9963

dynamic CHR0 $9966:
  0C 1D
  0D 1D
  0E 1B
  0F 00
  10 19
  11 00

substate0D background palette:
  pages02-04
  A02B/A022 alternation by $3C bit3 + $03A7
  exact 16-byte $3F00-$3F0F transfer
```

General fallback `$996C+` also closes primary `$03B7`, secondary `$03B4/$03B5`, and pending `$03A9` palette selectors. `$9D58` normalizes PPUADDR via `$3F,$00,$00,$00`.

Technical checkpoint:

```text
PR    #156
merge a6f421289fcaf8e2e000282307edb3a7223f95e1
head  a76b084b5e8fc3999931ec618157bcd1a4aca800
CI    #401 SUCCESS / #609 SUCCESS
```

An earlier head exposed one self-test namespace-import error after the library itself built successfully; the corrected exact final head passed build, self-test, password fixture and parity.

No ROM, CHR payload, palette payload, OAM dump or generated art was versioned.

## Frontera operativa actual

La próxima frontera contigua es **platform HUD/status writer `$9D69-$9EED`**.

Direct ROM reconnaissance already shows:

```text
$9D69:
  $2000=0
  $73 = ($73 + 1) & 3
  phase0 / phase1 / phase2 / phase3
```

The four phases update small HUD regions instead of redrawing the whole interface every invocation. Confirmed primitives include:

- `$9EB8/$9EC4`: packed-BCD nibble -> digit tile `$80+nibble`;
- `$9E73/$9E77/$9E80`: repeated fill-tile writers;
- `$9E84`: threshold classifier producing `$A7/$B1-$B6/$BF` gauge tiles;
- `$9ECD/$9ED8/$9EE3`: fixed PPU target setters `$22F4/$2334/$2374`.

Phase0 writes active-Saint packed resources at `$22F0/$2330` and, when `$02!=0`, Seventh Sense around `$236F`. Phases1/2 update the two resource/cap gauges; phase3 updates a Seventh-Sense gauge when applicable.

No current code/test artifact closes this writer, so it is the next real presentation gap rather than duplicate work.

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
- map kits `$00-$11` / static CHR routing;
- platform visual-resource definitions — #151;
- platform state-`$20` NMI — #154;
- platform palette/dynamic CHR refresh `$9915` — #156;
- boss damage/resources/dodge/techniques genéricos.

## Áreas abiertas

1. platform HUD/status writer `$9D69-$9EED`;
2. cualquier otro renderer/NMI global detectado por auditoría después de cerrar el HUD de plataforma;
3. RNG;
4. audio;
5. texto runtime/localización final;
6. auditoría integral final de ORIGINAL SPEC;
7. REBORN sólo después del cierre integral.

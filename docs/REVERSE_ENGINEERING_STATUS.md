# Estado global de ingeniería inversa

> Documento de orientación global, no operativo. El único `NEXT` autoritativo vive en `docs/PROJECT_STATE.md`.

## Mapa vigente

| Subsistema | Madurez | Estado resumido |
|---|---|---|
| ROM / boot / MMC1 / bancos | HIGH | Target, vectores, mapper y mapa PRG establecidos. |
| Máquina global `$00/$01` | HIGH | Namespace completo cerrado, PR #121. |
| Front-end/title/password | HIGH | Modal, attract, codec y overlays ejecutables cerrados. |
| Plataforma / exits / reload / narrativa | HIGH | Main path, common/special exits, `$70-$89`, `$8F` y terminal final cerrados. |
| Boss primitives | HIGH | Recursos, clasificadores, daño, técnicas, dodge y dispatchers. |
| Cobertura stage-local `$00-$0B` | **CLOSED** | Todos los contextos materiales `$00-$0A` cerrados; `$0B` estructural/transitorio; PR #149 completa el denominador. |
| Mu / repair `$00` | HIGH | Contexto especial completo, PR #141. |
| Taurus/Aldebaran `$01` | HIGH | Contexto completo, PR #123. |
| Gemini / first Camus `$02` | HIGH | Compuesto `$0E` + split Hyoga/ordinario cerrado, PR #143. |
| Cancer/Death Mask `$03` | HIGH | Contexto completo, PR #145. |
| Leo/Aioria `$04` | HIGH | Contexto completo, PR #125. |
| Virgo/Shaka `$05` | HIGH | Contexto completo, PR #127. |
| Scorpio/Milo `$06` | HIGH | Contexto + bridge progress `$08` / stage `$10` / platform `$08` cerrados, PR #147. |
| Capricorn/Shura `$07` | **HIGH** | Shiryu-only init, Talk/battle, retry y `$FE`->Aquarius cerrados, PR #149. |
| Aquarius/Camus `$08` | HIGH | Dos encuentros Camus y unlocks Hyoga cerrados, PR #129. |
| Pisces/Aphrodite `$09` | HIGH | Roster, Talk, Shun growth, selector y `$FE->$0C` cerrados, PR #131. |
| Saga `$0A` | HIGH | Máquina `$06CE` completa y terminales cerrados, PR #135. |
| Stage `$0B` | CLOSED-STRUCTURAL | Sin entrada estable canónica; usos transitorios/presentación. |
| Final-special `$0C` | HIGH | Rosas, command ownership y handoff a Saga cerrados, PR #133. |
| Post-Saga / ending tail | HIGH | `$0E->$11->$70-$89->$8F->$BC39->$BD2F` hard terminal, PR #137. |
| Plataforma: mapas/metatiles/CHR routing | HIGH | Kits `$00-$11`, CHR0 sprites / CHR1 background y bancos por substate establecidos. |
| Metasprites / recursos visuales platform | ACTIVE | Tipos `$01-$04` y parser/renderer ROM-fed probados; faltan `$05-$0F` + special-object definitions. |
| Renderer / frame composition global | MEDIUM | Varias primitivas/runtime cerradas; composición visual global todavía no promovida. |
| RNG | LOW-MEDIUM | Abierto. |
| Audio | LOW | Abierto. |
| Texto/localización | MEDIUM | Corpus JP + borrador ES; integración runtime final pendiente. |
| Auditoría integral ORIGINAL SPEC | PENDING | Se ejecutará después de cerrar subsistemas globales restantes. |
| REBORN | EARLY | Congelado hasta cierre integral de ORIGINAL SPEC. |

## Último checkpoint técnico — PR #149

Stage `$07` Capricorn / Shura quedó cerrado como último contexto material de la matriz battle/event `$050E=$00-$0B`.

Resultado principal:

```text
seed $067D=$09 / $050E=$07 / $06CD=$00 / $0673=$30
roster Seiya / Hyoga / Shun / Shiryu

fresh init gate:
  $F36F[$07]=$03
  -> sólo Shiryu despacha $9ACF

Shiryu init $9ACF:
  $058A:1->2
  $0696:1->2
  +600 Seventh Sense
  release $03

Talk $9ED6:
  first -> $066F++
  repeats -> $DC++ / $066F++ / forced Gold

post-Bronze $A86B:
  EB00 -> hit/miss $8B
  EB01 -> no $0690 for Shiryu; $0690=FF for others
  EBFF -> $06B1=FF / +800 / release FE

$0690 consumer $FAB9+:
  nonzero -> force $06BC=0

post-Gold $A8D8:
  EA00 continue / EA01 repeatable $40/$91 / EAFF defeat FF

FF retry:
  progress09 -> platform $09 -> E100 -> ED57/A973 -> E33D
  no replay of $970A/$97DB/$9ACF
  no duplicate +600 / no duplicate technique growth

victory FE:
  fixed $E3ED-$E414
  -> save winning record
  -> force Seiya
  -> $067D=$0A / $050E=$08 / $06CD=$08 / $0673=$38
```

Technical checkpoint:

```text
PR    #149
merge 28ba0a71c943d0f2ee943e8b60158a15c9d3efd5
head  eb55de62e008d1dbb92b16a6432759a22b32b7fb
CI    #384 SUCCESS / #593 SUCCESS
```

Cobertura material resultante:

```text
closed dedicated : 00 01 02 03 04 05 06 07 08 09 0A
material missing : NONE
structural only  : 0B
separate closed  : 0C
```

## Frontera operativa actual

La siguiente frontera vuelve a un subsistema global: **recursos visuales de plataforma / metasprites**.

Ya está congelado:

- `$02 -> $CACF/$CABD -> CHR0/CHR1`;
- platform PPUCTRL `$77=$90`: CHR0 = sprites, CHR1 = backgrounds;
- kits de mapas/metatiles para `$02=$00-$11`;
- CHR0 efectivos `25/27/29` para los grupos que ya alimentan el parser de entidades;
- punteros de metasprites bank-3 alrededor de `$B669/$B671/$B699/$B6C1`;
- formato de definición con count + registros tile/Y/X y override de atributo `$FF`;
- tipos `$01-$04` reconstruidos de forma coherente;
- `tools/reverse/render_platform_entities.py` como herramienta ROM-fed sin arte original embebido.

El hueco inmediato es mecánico, no de naming visual: cerrar tipos `$05-$0F`, tablas de sprites de objetos especiales, selector/pointer ownership, vínculo a CHR0 e invariantes ejecutables de definiciones/OAM.

## No reabrir sin evidencia nueva

- global `$00/$01` — #121;
- coverage battle/event `$00-$0B` — cerrado integralmente por #149;
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
- map kits `$00-$11` y CHR0/CHR1 routing;
- boss damage/resources/dodge/techniques genéricos.

## Áreas abiertas

1. platform visual-resource definitions: metasprites `$05-$0F` + special objects + executable inventory;
2. renderer/frame composition global restante;
3. RNG;
4. audio;
5. texto runtime/localización final;
6. auditoría integral final de ORIGINAL SPEC;
7. REBORN sólo después del cierre integral.

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
| Cobertura stage-local `$00-$0B` | HIGH | Sólo Capricorn `$07` permanece materialmente abierto. |
| Mu / repair `$00` | HIGH | Contexto especial completo, PR #141. |
| Taurus/Aldebaran `$01` | HIGH | Contexto completo, PR #123. |
| Gemini / first Camus `$02` | HIGH | Compuesto cerrado, PR #143. |
| Cancer/Death Mask `$03` | HIGH | Contexto + platform `$0C` cerrado, PR #145. |
| Leo/Aioria `$04` | HIGH | Contexto completo, PR #125. |
| Virgo/Shaka `$05` | HIGH | Contexto completo, PR #127. |
| Scorpio/Milo `$06` | HIGH | Talk/rewards, battle, retry y bridge `$08/$10/$08` a Capricorn cerrados, PR #147. |
| Capricorn/Shura `$07` | ACTIVE | Último gap stage-local material. |
| Aquarius/Camus `$08` | HIGH | Dos encuentros Camus y unlocks Hyoga cerrados, PR #129. |
| Pisces/Aphrodite `$09` | HIGH | Contexto completo, PR #131. |
| Saga `$0A` | HIGH | Máquina `$06CE` completa, PR #135. |
| Stage `$0B` | CLOSED-STRUCTURAL | Sin entrada estable canónica. |
| Final-special `$0C` | HIGH | Rosas y handoff a Saga cerrados, PR #133. |
| Post-Saga / ending tail | HIGH | Hard terminal cerrado, PR #137. |
| Renderer / metasprites / CHR | MEDIUM | Spec global pendiente. |
| RNG | LOW-MEDIUM | Abierto. |
| Audio | LOW | Abierto. |
| Texto/localización | MEDIUM | Corpus JP + borrador ES; integración final pendiente. |
| REBORN | EARLY | Congelado hasta cierre integral de ORIGINAL SPEC. |

## Checkpoint técnico reciente — PR #147

Stage `$06` Scorpio / Milo quedó cerrado como contexto ejecutable compuesto con su sucesión obligatoria hasta Capricorn.

Resultados centrales:

- seed `$067D=$07/$050E=$06/$06CD=$00/$0673=$30`, roster Seiya/Hyoga/Shun/Shiryu;
- init `$9ACE=RTS`;
- Talk `$9E51` modela suma de dodge de 8 bits, reward Hyoga +300 con latch `$068A`, milestone +200 con `$066F` y forced Gold sólo en repetición high-history no-Hyoga;
- `$A7FF` cierra victoria `$01` y feedback de hit;
- `$A847` cierra healthy/low/defeat `$FF`;
- selector `$908C+` elige slot1 con dodge total `<2` y slot0 con `>=2`;
- `$FF` retry vía `$A973` rearma `$066F/$0677/$0678/$068A`;
- victoria `$01` lleva a progress `$08/$050E=$10`, fixed `$E4D7` crea principal platform `$02=$08`, common gate `X>=$D0/Y=$40/jump=0` hace normal reload y fixed `$E2DD` sintetiza un segundo release `$01`;
- la cadena termina exactamente en Capricorn `$067D=$09/$050E=$07/$06CD=$00/$0673=$30`, preservando Saint;
- coverage promueve `$06`; `$07` queda como único gap material `$00-$0B`.

```text
merge 62f474bd4e31cd4ca4f40006fd4e9b475bd950b6
head  4148e0b178592126ee2587a4ca83753019580b42
CI    #375 SUCCESS / #582 SUCCESS
```

Artifacts: `ScorpioStage06Context.cs`, `ScorpioStage06ContextChecks.cs`, `BOSS_CONTEXT_STAGE_06_SCORPIO.md`, coverage y dispatch actualizados.

## Frontera operativa actual

```text
ORIGINAL SPEC / stage $07 Capricorn / Shura
```

Contrato preliminar verificado:

```text
seed
  $067D=$09
  $050E=$07
  $06CD=$00
  $0673=$30
  roster Seiya / Hyoga / Shun / Shiryu

init $9ACF
  if $0670==$FE -> RTS
  otherwise temporary $0F
  $0672=0
  $058A++ / $0696++
  +600 Seventh Sense
  shared $9C3D intro/release $03

Talk $9ED6
  first -> message $AE + per-Saint 43/43/43/AF; $066F++
  repeat -> same table; $DC++; $066F++; forced Gold

post-Bronze $A86B
  EB=00 + miss -> $8B feedback
  EB=01 + Shiryu -> continue
  EB=01 + non-Shiryu -> $0690=$FF
  EB=FF -> scripted sequence; $06B1=$FF; +800 Seventh Sense; release $FE

post-Gold $A8D8
  EA=00 continue
  EA=01 repeatable $40/$91 feedback
  EA=FF release $FF

Gold selector
  generic parity slots 0/1
```

Fixed `$FAB9+` consumes nonzero `$0690` by forcing `$06BC=0`; this must be composed into the Capricorn model.

Fixed `$FE` release ownership forces active Seiya, rewrites to `$01`, increments progress and produces the already-closed Aquarius boundary:

```text
$067D=$0A
$050E=$08
$06CD=$08
$0673=$38
active Seiya
```

## No reabrir sin evidencia nueva

- global `$00/$01` — #121;
- coverage audit — #139;
- Mu `$00` — #141;
- Taurus `$01` — #123;
- Gemini / first Camus `$02` — #143;
- Cancer `$03` — #145;
- Leo `$04` — #125;
- Virgo `$05` — #127;
- Scorpio `$06` — #147;
- Aquarius `$08` — #129;
- Pisces `$09` — #131;
- final-special `$0C` — #133;
- Saga `$0A` — #135;
- post-Saga ending — #137;
- front-end/title/attract/overlays and promoted platform/reload/narrative primitives;
- generic boss damage/resources/dodge/technique arithmetic.

## Áreas abiertas

1. Capricorn / Shura `$07` — último contexto battle/event material;
2. renderer/metasprites/CHR global;
3. RNG;
4. audio;
5. texto runtime/localización final;
6. auditoría integral final de ORIGINAL SPEC;
7. REBORN sólo después del cierre integral.

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
| Cobertura stage-local `$00-$0B` | HIGH | Denominador completo PR #139; promovido hasta Cancer `$03`. |
| Mu / repair `$00` | HIGH | Contexto especial completo, PR #141. |
| Taurus/Aldebaran `$01` | HIGH | Contexto completo, PR #123. |
| Gemini / first Camus `$02` | HIGH | Compuesto `$0E` + split Hyoga/ordinario cerrado, PR #143. |
| Cancer/Death Mask `$03` | HIGH | Talk/platform `$0C`, batalla, retry y Leo boundary cerrados, PR #145. |
| Leo/Aioria `$04` | HIGH | Contexto completo, PR #125. |
| Virgo/Shaka `$05` | HIGH | Contexto completo, PR #127. |
| Scorpio/Milo `$06` | ACTIVE | Primer gap restante; incluye bridge progress `$08` / stage `$10` / platform `$08`. |
| Capricorn/Shura `$07` | MEDIUM | Último gap stage-local material tras Scorpio. |
| Aquarius/Camus `$08` | HIGH | Dos encuentros Camus y unlocks Hyoga cerrados, PR #129. |
| Pisces/Aphrodite `$09` | HIGH | Roster, Talk, Shun growth, selector y `$FE->$0C` cerrados, PR #131. |
| Saga `$0A` | HIGH | Máquina `$06CE` completa y terminales cerrados, PR #135. |
| Stage `$0B` | CLOSED-STRUCTURAL | Sin entrada estable canónica; usos transitorios/presentación, PR #139. |
| Final-special `$0C` | HIGH | Rosas, command ownership y handoff a Saga cerrados, PR #133. |
| Post-Saga / ending tail | HIGH | `$0E->$11->$70-$89->$8F->$BC39->$BD2F` hard terminal, PR #137. |
| Renderer / metasprites / CHR | MEDIUM | Spec global pendiente. |
| RNG | LOW-MEDIUM | Abierto. |
| Audio | LOW | Abierto. |
| Texto/localización | MEDIUM | Corpus JP + borrador ES; integración final pendiente. |
| REBORN | EARLY | Congelado hasta cierre integral de ORIGINAL SPEC. |

## Checkpoint técnico reciente — PR #145

Stage `$03` Cancer / Death Mask quedó cerrado como contexto dedicado ejecutable.

Resultados centrales:

- seed `$067D=$03/$050E=$03/$06CD=$02/$0673=$32`; roster exacto Seiya/Shun/Shiryu;
- init `$9851` usa presentación temporal `$0E`, otorga +400 Seventh Sense y entrega release interno `$03` con `$068E=1`;
- Talk `$9D96` con `$067C=0` crea platform `$0C/release $02` y hace doble-`PLA` caller unwind;
- salida `$0C` ya cerrada usa gate `X>=$88/Y=$20/jump=0` e incrementa `$067C:0->1` sin `$A973`;
- Talk con `$067C!=0` hace `$DC++` y fuerza respuesta Gold;
- `$A50F` permite victoria `$01` incluso en phase0, por lo que el detour `$0C` es opcional; `$EB=$01` ejecuta `INC $064A` repetible y `$064A` queda tratado como scratch;
- `$A560` posee first-low `$064D`, feedback repeat/healthy y derrota `$FF`;
- slots Gold `0/1` por parity genérica;
- `$FF` retry vuelve a phase0 y rearma Talk/platform `$0C`;
- victoria directa o post-platform converge en Leo `$067D=$04/$050E=$04/$06CD=$02/$0673=$32` preservando Saint alcanzable;
- coverage promueve `$03` a cerrado y deja únicamente `$06/$07` como gaps stage-local materiales.

```text
merge 079e97cab384c53ef5fddf69f2e1c8eb6f065703
head  4c686f90ca476a47ca2e7d2134f72a52609a9276
CI    #368 SUCCESS / #573 SUCCESS
```

Artifacts principales: `CancerStage03Context.cs`, `CancerStage03ContextChecks.cs`, `BOSS_CONTEXT_STAGE_03_CANCER.md` y actualización de coverage/dispatch global.

## Frontera operativa actual

```text
ORIGINAL SPEC / stage $06 Scorpio + progress-$08 stage-$10 platform bridge
```

Contrato ya delimitado:

```text
seed Scorpio
  $067D=$07
  $050E=$06
  $06CD=$00
  $0673=$30
  roster Seiya / Hyoga / Shun / Shiryu

init $9ACE = RTS

Talk $9E51
  dodge total = $0677+$0678

  total <2 + Hyoga
    -> first $068A=0: $068A++ and +300 Seventh Sense
    -> repeats: no second +300 in same runtime

  total <2 + non-Hyoga
    -> presentation only

  total >=2 + $066F=0
    -> per-Saint text table 86 86 87 86
    -> $066F++
    -> +200 Seventh Sense

  total >=2 + $066F!=0
    -> Hyoga returns without $DC
    -> non-Hyoga $DC++ -> forced Gold

post-Bronze $A7FF
  -> opponent defeat release $01
  -> nonterminal hit feedback $A6

post-Gold $A847
  -> $EA=00 continue
  -> $EA=01 low feedback
  -> $EA=FF defeat $FF

Gold selector $908C+
  total <2  -> slot1
  total >=2 -> slot0
```

`$A973` clears `$066F/$0677/$0678/$068A`, so generic `$FF` retry rearms Scorpio's Talk/reward gates.

### Success bridge after Milo

Scorpio victory does **not** enter Capricorn directly:

```text
Scorpio release $01 at progress $07
  -> progress $08
  -> $F016[08]=$10 / $E50B[08]=$00
  -> story-stage $050E=$10 / marker $30
  -> inherited release $01 calls $E4D7
  -> $E4E0[08]=$08 -> platform substate $02=$08
  -> common platform gate X>=$D0 / Y=$40 / jump=0
  -> normal $3D/$E100 reload
  -> progress $08 still reconstructs $050E=$10
  -> fixed $E2DD detects stage $10 and synthesizes release $01
  -> progress $09
  -> $F016[09]=$07 / $E50B[09]=$00
  -> Capricorn $050E=$07 / $06CD=$00 / $0673=$30
```

The stage value `$050E=$10` and special-normal platform substate `$02=$10` are different namespaces. Scorpio's bridge uses principal platform substate `$02=$08`.

## No reabrir sin evidencia nueva

- global `$00/$01` — #121;
- coverage matrix `$00-$0B` — #139;
- Mu `$00` — #141;
- Taurus `$01` — #123;
- Gemini / first Camus `$02` — #143;
- Cancer `$03` — #145;
- Leo `$04` — #125;
- Virgo `$05` — #127;
- Aquarius `$08` — #129;
- Pisces `$09` — #131;
- final-special `$0C` — #133;
- Saga `$0A` — #135;
- post-Saga ending/hard terminal — #137;
- front-end/title — #119;
- attract — #117/#119;
- `$11-$14`, `$60`, `$91-$99` — #111/#113/#115;
- plataforma/reload/narrativa promovidos;
- boss damage/resources/dodge/techniques genéricos.

## Áreas abiertas

1. contexto compuesto `$06` Scorpio + progress `$08` / stage `$10` / platform `$08` bridge;
2. contexto material `$07` Capricorn / Shura;
3. renderer/metasprites/CHR global;
4. RNG;
5. audio;
6. texto runtime/localización final;
7. auditoría integral final de ORIGINAL SPEC;
8. REBORN sólo después del cierre integral.

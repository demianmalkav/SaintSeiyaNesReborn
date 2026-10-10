# Estado global de ingeniería inversa

> Documento de orientación global, no operativo. El único `NEXT` autoritativo vive en `docs/PROJECT_STATE.md`.

## Mapa vigente

| Subsistema | Madurez | Estado resumido |
|---|---|---|
| ROM / boot / MMC1 / bancos | HIGH | Target, vectores, mapper y mapa PRG establecidos. |
| Máquina global `$00/$01` | HIGH | Namespace completo cerrado, PR #121. |
| Front-end/title/password | HIGH | Modal, attract, codec y overlays ejecutables cerrados. |
| Plataforma / exits / reload / narrativa | HIGH | Main path, special exits, `$70-$89`, `$8F` y terminal final cerrados. |
| Boss primitives | HIGH | Recursos, clasificadores, daño, técnicas, dodge y dispatchers. |
| Cobertura stage-local `$00-$0B` | HIGH | Matriz completa de reachability/ownership, PR #139; promovida hasta `$02`. |
| Mu / repair `$00` | HIGH | Contexto especial completo, PR #141. |
| Taurus/Aldebaran `$01` | HIGH | Contexto completo, PR #123. |
| Gemini / first Camus `$02` | HIGH | Compuesto `$0E` + split Hyoga/ordinario + Cancer boundary cerrado, PR #143. |
| Cancer/Death Mask `$03` | ACTIVE | Primer gap restante: Talk crea platform `$0C`; post-action y retry pendientes. |
| Leo/Aioria `$04` | HIGH | Contexto completo, PR #125. |
| Virgo/Shaka `$05` | HIGH | Contexto completo, PR #127. |
| Scorpio/Milo `$06` | MEDIUM | Gap material confirmado; contexto dedicado pendiente. |
| Capricorn/Shura `$07` | MEDIUM | Gap material confirmado; contexto dedicado pendiente. |
| Aquarius/Camus `$08` | HIGH | Dos encuentros Camus y unlocks Hyoga cerrados, PR #129. |
| Pisces/Aphrodite `$09` | HIGH | Roster, Talk, Shun growth, selector y `$FE->$0C` cerrados, PR #131. |
| Saga `$0A` | HIGH | Máquina `$06CE` completa y terminales cerrados, PR #135. |
| Stage `$0B` | CLOSED-STRUCTURAL | Sin entrada estable canónica; usos `$0B` transitorios/presentación, PR #139. |
| Final-special `$0C` | HIGH | Rosas, command ownership y handoff a Saga cerrados, PR #133. |
| Post-Saga / ending tail | HIGH | `$0E->$11->$70-$89->$8F->$BC39->$BD2F` hard terminal, PR #137. |
| Renderer / metasprites / CHR | MEDIUM | Spec global pendiente. |
| RNG | LOW-MEDIUM | Abierto. |
| Audio | LOW | Abierto. |
| Texto/localización | MEDIUM | Corpus JP + borrador ES; integración final pendiente. |
| REBORN | EARLY | Congelado hasta cierre integral de ORIGINAL SPEC. |

## Checkpoint técnico reciente — PR #143

Stage `$02` Gemini / first-Camus quedó cerrado como contexto compuesto ejecutable.

Resultados centrales:

- progreso `$02` selecciona stage `$02`, descriptor `$00`, marker `$30`; roster alcanzable Seiya/Hyoga/Shun/Shiryu;
- init `$981F` usa presentación temporal `$11`, otorga +300 Seventh Sense y entrega release interno `$03`;
- Talk `$9D81` fuerza respuesta Gold siempre y sólo en primer uso hace `$066F:0->1`;
- `$A444` intercepta el primer Bronze con `$067C=0` antes de clasificar al rival y fuerza platform `$0E/release $02`;
- salida `$0E` ya cerrada incrementa `$067C:0->1` sin `$A973`, preservando `$066F`;
- Seiya/Shun/Shiryu vuelven a stage `$02`; Hyoga redirige a `$050E=$08/$06B8=$0A/$0690=$FF` y compone first Camus existente;
- phase1 `$A444` posee hit/miss/low/victory `$01`; `$A4CC` posee healthy/low/defeat `$FF`;
- `$FF` retry llama luego a `$A973`, limpia `$067C` y rearma el detour `$0E`;
- victoria ordinaria `$01` y las dos terminales `$FE` de first Camus convergen en Cancer `$067D=$03/$050E=$03/$06CD=$02/$0673=$32`;
- coverage promueve `$02` a cerrado y deja gaps `$03/$06/$07`.

```text
merge 8f41ea2afd54e00f72066f07ee493936e92412c9
head  38d3f1abb919f8dc380e06218d6dbe1d4c6cbdea
CI    #359 SUCCESS / #562 SUCCESS
```

Artifacts principales: `GeminiStage02Context.cs`, `GeminiStage02ContextChecks.cs`, `BOSS_CONTEXT_STAGE_02_GEMINI_FIRST_CAMUS.md` y actualización de coverage/dispatch global.

## Frontera operativa actual

```text
ORIGINAL SPEC / stage $03 Cancer + platform $0C detour
```

Contrato ya delimitado:

```text
seed: $067D=$03 / $050E=$03 / $06CD=$02 / $0673=$32
roster: Seiya / Shun / Shiryu

init $9851
  -> temporary presentation $0E
  -> +400 Seventh Sense
  -> release $03 / $068E=1

Talk $9D96, phase $067C=0
  -> $02=$0C
  -> release $02
  -> caller unwind
  -> platform $0C
  -> accepted exit X>=$88 / Y=$20 / jump=0
  -> special resume $067C:0->1

Talk $9D96, phase $067C!=0
  -> dialogue
  -> $DC++ -> forced Gold response

post-Bronze $A50F
  -> victory $01 / repeated low-opponent $064A++ / miss feedback

post-Gold $A560
  -> first-low latch $064D / repeat-health feedback / defeat $FF
```

Generic `$FF` retry clears `$067C` through `$A973`, returning Cancer to phase zero. Victory `$01` advances exactly to already-closed Leo `$067D=$04/$050E=$04/$06CD=$02/$0673=$32`.

## No reabrir sin evidencia nueva

- global `$00/$01` — #121;
- coverage matrix `$00-$0B` — #139;
- Mu `$00` — #141;
- Taurus `$01` — #123;
- Gemini / first Camus `$02` — #143;
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

1. contexto compuesto `$03` Cancer / platform `$0C`;
2. contextos materiales `$06` Scorpio y `$07` Capricorn, en orden canónico;
3. renderer/metasprites/CHR global;
4. RNG;
5. audio;
6. texto runtime/localización final;
7. auditoría integral final de ORIGINAL SPEC;
8. REBORN sólo después del cierre integral.

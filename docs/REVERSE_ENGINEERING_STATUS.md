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
| Cobertura stage-local `$00-$0B` | HIGH | Matriz completa de reachability/ownership, PR #139; actualizada con Mu cerrado. |
| Mu / repair `$00` | HIGH | Contexto especial completo: `$06BB`, Talk, release `$01` y Taurus boundary, PR #141. |
| Taurus/Aldebaran `$01` | HIGH | Contexto completo, PR #123. |
| Gemini / first Camus `$02` | ACTIVE | Próximo gap: detour obligatorio `$0E`, split ordinario/Hyoga y first-Camus compuesto. |
| Cancer/Death Mask `$03` | MEDIUM | Gap material confirmado; contexto dedicado pendiente tras `$02`. |
| Leo/Aioria `$04` | HIGH | Contexto completo, PR #125. |
| Virgo/Shaka `$05` | HIGH | Contexto completo, PR #127. |
| Scorpio/Milo `$06` | MEDIUM | Gap material confirmado; contexto dedicado pendiente. |
| Capricorn/Shura `$07` | MEDIUM | Gap material confirmado; contexto dedicado pendiente. |
| Aquarius/Camus `$08` | HIGH | Dos encuentros Camus y unlocks Hyoga cerrados, PR #129. |
| Pisces/Aphrodite `$09` | HIGH | Roster, Talk, Shun growth, selector y `$FE->$0C` cerrados, PR #131. |
| Saga `$0A` | HIGH | Máquina `$06CE` completa y terminales cerrados, PR #135. |
| Stage `$0B` | CLOSED-STRUCTURAL | Sin entrada estable canónica; usos `$0B` son transitorios/presentación, PR #139. |
| Final-special `$0C` | HIGH | Rosas, command ownership y handoff a Saga cerrados, PR #133. |
| Post-Saga / ending tail | HIGH | `$0E->$11->$70-$89->$8F->$BC39->$BD2F` hard terminal, PR #137. |
| Renderer / metasprites / CHR | MEDIUM | Spec global pendiente. |
| RNG | LOW-MEDIUM | Abierto. |
| Audio | LOW | Abierto. |
| Texto/localización | MEDIUM | Corpus JP + borrador ES; integración final pendiente. |
| REBORN | EARLY | Congelado hasta cierre integral de ORIGINAL SPEC. |

## Checkpoint técnico reciente — PR #141

Stage `$00` Mu / pre-battle repair quedó cerrado como contexto dedicado especial.

Resultados centrales:

- el clear global `$AD4A-$AD54` prueba `$06BB=0` al inicio canónico;
- `$A100+` deriva `$0673=$30`, selecciona Seiya inicialmente y deja disponibles Seiya/Hyoga/Shun/Shiryu, no Ikki;
- reset común `$A973+` limpia `$066F/$0670` pero preserva `$06BB`;
- Resource Allocation, Attack y Escape desvían stage `$00` a `$F238`, que diferencia primer/repetido mediante `$06BB`, incrementa el contador y vuelve al command loop;
- Talk `$9CB7` usa tablas exactas `$9D24=35 35 36 34` y `$9D28=3B 3B 11 3B`; primer Talk hace `$066F=1`, segundo/repetido emite `$0670=$01`;
- `$AEDA` es presentación, no progresión/daño/recursos persistentes de Mu;
- release `$01` -> `$E399/$E3B3` produce `$067D=$01/$050E=$01/$06CD=$00/$0673=$30`, preservando Saint 0..3;
- ninguna orden canónica de Mu alcanza Bronze damage, post-Bronze, Gold selector/dodge/damage o post-Gold;
- la matriz de cobertura promueve `$00` a cerrado y mueve el primer gap a `$02`.

```text
merge 33ac4e3b3ec249b37424f4def9344e96a525654f
head  fe4d5f92cb90aff67a43618c17b5b56a92e9b2de
CI    #353 SUCCESS / #554 SUCCESS
```

Artifacts principales: `MuStage00Context.cs`, `MuStage00ContextChecks.cs`, `BOSS_CONTEXT_STAGE_00_MU.md` y actualización de la matriz/dispatch global.

## Frontera operativa actual

```text
ORIGINAL SPEC / stage $02 Gemini + first-Camus detour
```

Stage `$02` debe cerrarse como un compuesto, no como un boss lineal:

```text
seed: $067D=$02 / $050E=$02
init $981F -> +300 Seventh Sense -> release $03
Talk $9D81 -> $DC++ siempre; primer uso también $066F++

primer post-Bronze con $067C=0
  -> $02=$0E
  -> release $02
  -> platform $0E
  -> accepted exit X>=$B4 / Y=$80 / jump=0
  -> special resume $067C:0->1

ordinary Saint -> reabre stage $02
Hyoga          -> $050E=$08 / $06B8=$0A / $0690=$FF
                  -> first-Camus branch ya cerrado en AquariusStage08Context
```

Tras `$067C!=0`, stage `$02` posee su post-Bronze ordinario con victoria `$01`, slots Gold `0/1` y post-Gold con derrota `$FF`. El checkpoint debe componer todas las rutas canónicas hasta el boundary Cancer `$067D=$03/$050E=$03`, reutilizando plataforma `$0E`, Aquarius first-Camus y aritmética genérica ya cerrados.

## No reabrir sin evidencia nueva

- global `$00/$01` — #121;
- coverage matrix `$00-$0B` — #139;
- Mu `$00` — #141;
- Taurus `$01` — #123;
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

1. contexto compuesto `$02` Gemini / first Camus;
2. contextos materiales `$03` Cancer, `$06` Scorpio y `$07` Capricorn, en orden canónico;
3. renderer/metasprites/CHR global;
4. RNG;
5. audio;
6. texto runtime/localización final;
7. auditoría integral final de ORIGINAL SPEC;
8. REBORN sólo después del cierre integral.

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
| Cobertura stage-local `$00-$0B` | HIGH | Matriz completa de reachability/ownership, PR #139. |
| Mu / repair `$00` | ACTIVE | Primer gap material: Talk-only; cierre dedicado pendiente. |
| Taurus/Aldebaran `$01` | HIGH | Contexto completo, PR #123. |
| Gemini / first Camus `$02` | MEDIUM | Material confirmado por auditoría; contexto dedicado pendiente después de `$00`. |
| Cancer/Death Mask `$03` | MEDIUM | Material confirmado por auditoría; contexto dedicado pendiente. |
| Leo/Aioria `$04` | HIGH | Contexto completo, PR #125. |
| Virgo/Shaka `$05` | HIGH | Contexto completo, PR #127. |
| Scorpio/Milo `$06` | MEDIUM | Material confirmado por auditoría; contexto dedicado pendiente. |
| Capricorn/Shura `$07` | MEDIUM | Material confirmado por auditoría; contexto dedicado pendiente. |
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

## Checkpoint técnico reciente — PR #139

La cobertura stage-local `$050E=$00-$0B` quedó cerrada como denominador ejecutable.

Resultados centrales:

- se fijaron las cuatro familias exactas de dispatch (`init`, `Talk`, `post-Bronze`, `post-Gold`) para cada índice `$00-$0B`;
- se promovió el mapa fijo `$F016` de `$067D=$00-$0E`, eliminando ambigüedad entre índice numérico y entrada narrativa real;
- clasificación final: cerrados `$01/$04/$05/$08/$09/$0A`; gaps materiales `$00/$02/$03/$06/$07`; `$0B` estructural/transitorio sin combate estable; `$0C` bridge separado ya cerrado;
- `$0B` no aparece como salida estable de `$F016`; los usos inmediatos relevantes cargan `$0B` sólo mediante `$F2ED` para presentación y el raw init pointer `$A960` cae dentro de la instrucción que empieza en `$A95F`;
- reachability de Gold slots quedó separada de los slots estructurales de coeficientes;
- `$00` es el primer gap material canónico: Attack/resource/Escape están bloqueados, mientras Talk `$9CB7` mantiene una máquina `$066F 0->1` y en el segundo uso emite release `$01` hacia Taurus.

```text
merge 2ed957e779277be7441fec2732e1ba0c919e4903
head  d25433da6f4a178922c2a6dbe06eac048ec7cca5
CI    #349 SUCCESS / #548 SUCCESS
```

Artifacts principales: `BATTLE_STAGE_CONTEXT_COVERAGE.md`, `BattleStageContextCoverage.cs`, sus fixtures y la actualización de `BATTLE_EVENT_DISPATCH.md`.

## Frontera operativa actual

```text
ORIGINAL SPEC / stage $00 Mu repair context
```

La auditoría ya decidió el orden de trabajo. Stage `$00` es un contexto especial, no un combate Gold ordinario:

```text
seed: $067D=$00 / $050E=$00 / $066F=0 / $0670=0

Attack              -> blocked owner $F238
Resource allocation -> blocked owner $F238
Escape              -> blocked owner $F238
Talk                -> $9CB7

Talk #1: $066F 0->1
Talk #2+: $0670=$01
release $01 -> fixed progression -> $067D=$01 -> $050E=$01 Taurus
```

El siguiente checkpoint debe convertir este contrato en un contexto dedicado ejecutable, incluyendo la prueba negativa de que ninguna superficie Bronze/Gold estructural es alcanzable.

## No reabrir sin evidencia nueva

- global `$00/$01` — #121;
- coverage matrix `$00-$0B` — #139;
- Taurus — #123;
- Leo — #125;
- Virgo — #127;
- Aquarius — #129;
- Pisces — #131;
- final-special `$0C` — #133;
- Saga `$0A` — #135;
- post-Saga ending/hard terminal — #137;
- front-end/title — #119;
- attract — #117/#119;
- `$11-$14`, `$60`, `$91-$99` — #111/#113/#115;
- plataforma/reload/narrativa promovidos;
- boss damage/resources/dodge/techniques genéricos.

## Áreas abiertas

1. contexto dedicado `$00` Mu / pre-battle repair;
2. contextos materiales `$02` Gemini/first Camus, `$03` Cancer, `$06` Scorpio y `$07` Capricorn, en orden canónico tras `$00`;
3. renderer/metasprites/CHR global;
4. RNG;
5. audio;
6. texto runtime/localización final;
7. auditoría integral final de ORIGINAL SPEC;
8. REBORN sólo después del cierre integral.

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
| Taurus/Aldebaran `$01` | HIGH | Contexto completo, PR #123. |
| Leo/Aioria `$04` | HIGH | Contexto completo, PR #125. |
| Virgo/Shaka `$05` | HIGH | Contexto completo, PR #127. |
| Aquarius/Camus `$08` | HIGH | Dos encuentros Camus y unlocks Hyoga cerrados, PR #129. |
| Pisces/Aphrodite `$09` | HIGH | Roster, Talk, Shun growth, selector y `$FE->$0C` cerrados, PR #131. |
| Final-special `$0C` | HIGH | Rosas, command ownership y handoff a Saga cerrados, PR #133. |
| Saga `$0A` | HIGH | Máquina `$06CE` completa y terminales cerrados, PR #135. |
| Post-Saga / ending tail | HIGH | `$0E->$11->$70-$89->$8F->$BC39->$BD2F` hard terminal, PR #137. |
| Cobertura stage-local `$00-$0B` | ACTIVE | Falta clasificar contextos omitidos `$00/$02/$03/$06/$07/$0B`. |
| Renderer / metasprites / CHR | MEDIUM | Spec global pendiente. |
| RNG | LOW-MEDIUM | Abierto. |
| Audio | LOW | Abierto. |
| Texto/localización | MEDIUM | Corpus JP + borrador ES; integración final pendiente. |
| REBORN | EARLY | Congelado hasta cierre integral de ORIGINAL SPEC. |

## Checkpoint técnico reciente — PR #137

La cola post-Saga quedó cerrada hasta el terminal real del original.

Resultados centrales:

- la victoria de Saga produce `$067D=$0E`, `$050E=$00`, `$06CD=$00`, `$0673=$30`, release `$05` y el único bootstrap post-Saga `$00->$20`;
- progreso `$0E` selecciona el platform substate `$11`, cuyo gate final exige X `>= $D0`, Y `$50`, jump phase `0` y entra por el path especial `$70`;
- se reutilizan sin reabrir las máquinas ya promovidas `$70->$75->$80` y `$80->$89`;
- `$89` entrega `$04=$8F/$00=$01=$3D` a `$E100`;
- corrección importante: `$F381` con `$068F=$8F` no retorna a `$E214`; escribe `$06CD/$0673/$06CC=$20/$20/$21`, selecciona PRG bank 0 y hace tail-jump a `$BC39`;
- por ello no existe el segundo `$00->$20` bootstrap inferido anteriormente, no se normaliza `$050E` y `$C180` no vuelve a ejecutarse;
- bank 0 `$BC39-$BD2F` consume diez presentation streams `0..9`;
- al terminar el stream 9, `$BD2F: JMP $BD2F` forma el terminal duro del juego; no existe retorno software a gameplay/title/front-end.

```text
merge f7592b7f586f81c6130082dbd5cd0b54f8ba8ba7
head  049d8bec0c6da51ef8ba0a91ec5508e621db8051
CI    #345 SUCCESS / #544 SUCCESS
```

Artifacts principales: `POST_SAGA_ENDING_TAIL.md`, `PostSagaEndingTail.cs`, fixtures end-to-end y corrección de `PLATFORM_RELOAD_MODE_8F.md` / `PlatformNarrative8FReload.cs`.

## Frontera operativa actual

```text
ORIGINAL SPEC / battle-stage context coverage audit
```

La ruta canónica hasta el final ya tiene un terminal probado. El riesgo estructural restante más inmediato es de **cobertura stage-local**: `BATTLE_EVENT_DISPATCH.md` tiene cuatro familias indexadas para `$050E=$00-$0B`, pero sólo algunos stages poseen un contexto ejecutable dedicado.

Contextos dedicados cerrados:

```text
$01 Taurus
$04 Leo
$05 Virgo
$08 Aquarius
$09 Pisces
$0A Saga
$0C final-special bridge (fuera del rango ordinario, no-boss)
```

Índices ordinarios aún no clasificados al mismo nivel:

```text
$00 Mu / pre-battle repair
$02 Gemini / first Camus branch
$03 Cancer — Death Mask
$06 Scorpio — Milo
$07 Capricorn — Shura
$0B special/final context
```

La próxima tarea no presupone que todos requieran un modelo dedicado: debe probar para cada uno si es genérico/no-boss/special o si contiene progresión/eventos materiales todavía no promovidos.

## No reabrir sin evidencia nueva

- global `$00/$01` — #121;
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

1. auditoría completa de cobertura de contexts `$050E=$00-$0B` y cierre del primer gap material;
2. renderer/metasprites/CHR global;
3. RNG;
4. audio;
5. texto runtime/localización final;
6. campos persistentes/progresión provisionales que la auditoría final todavía marque como incompletos;
7. auditoría integral final de ORIGINAL SPEC;
8. REBORN sólo después del cierre integral.

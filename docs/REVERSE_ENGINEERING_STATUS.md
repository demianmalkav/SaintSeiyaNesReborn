# Estado global de ingeniería inversa

> Documento de orientación global, no operativo. El único `NEXT` autoritativo vive en `docs/PROJECT_STATE.md`.

## Mapa vigente

| Subsistema | Madurez | Estado resumido |
|---|---|---|
| ROM / boot / MMC1 / bancos | HIGH | Target, vectores, mapper y mapa PRG establecidos. |
| Máquina global `$00/$01` | HIGH | PR #121: namespace completo cerrado. |
| Front-end/title/password | HIGH | Modal, attract, codec y overlays ejecutables CHR->RAM cerrados. |
| Plataforma / exits / reload / narrativa | HIGH | Sistemas principales y handoffs ampliamente promovidos; queda la composición final post-Saga. |
| Boss primitives | HIGH | Recursos, clasificadores, daño, técnicas, dodge y dispatchers. |
| Taurus/Aldebaran `$01` | HIGH | Contexto completo, PR #123. |
| Leo/Aioria `$04` | HIGH | Contexto completo, PR #125. |
| Virgo/Shaka `$05` | HIGH | Contexto completo con Ikki + plataforma `$0D` + releases especiales, PR #127. |
| Aquarius/Camus `$08` | HIGH | Dos encuentros Camus, unlocks Hyoga, `$06B8/$06E1`, selector y releases cerrados, PR #129. |
| Pisces/Aphrodite `$09` | HIGH | Roster Seiya/Shun, Talk/dodge, Shun growth, selector 0→1→2 y `$FE->$0C` cerrados, PR #131. |
| Final-special `$0C` | HIGH | Init-overrun inalcanzable, Talk compartido, rosas `$12->$0C`, release `$01` y handoff a Saga cerrados, PR #133. |
| Saga `$0A` | HIGH | Máquina `$06CE` completa, Ikki→Seiya, support/Rolling Crash, slots Gold y terminales cerrados, PR #135. |
| Post-Saga / ending tail | ACTIVE | Victoria Saga `$0E` -> substate `$11` -> `$70-$89` -> reload `$8F`; falta cerrar el destino real post-`$8F`. |
| Renderer / metasprites / CHR | MEDIUM | Spec global pendiente. |
| RNG | LOW-MEDIUM | Abierto. |
| Audio | LOW | Abierto. |
| Texto/localización | MEDIUM | Corpus JP + borrador ES; integración final pendiente. |
| REBORN | EARLY | Congelado hasta cierre integral de ORIGINAL SPEC. |

## Checkpoint técnico reciente — PR #135

Saga `$0A` quedó cerrado end-to-end sobre la ROM canónica verificada.

Resultados centrales:

- ambos ingresos post-`$0C` llegan a `$067D=$0D/$050E=$0A/$0673=$3E` con Seiya o Shun activo y `$06CE=0`;
- el ingreso inicial no ejecuta init Saga: phase0 empieza con el Santo heredado y `$0690=$FF`;
- primer Bronze phase0 produce miss scriptado, incrementa `$0678` y desenrolla la acción; Talk posterior avanza `$066F/$06CF/$06D0`;
- phase0 usa slots Gold 0/1 y termina por `$FF` al quedar el jugador low/dead;
- primer `$FF` ejecuta `$9B69`, guarda al Santo heredado, fuerza Ikki y avanza `$06CE=1`;
- primer Talk de Ikki limpia exactamente `$0690`; omitirlo puede arrastrar el bloqueo hasta Seiya final;
- phase1 usa slots Gold 0/2/3 por `$0649/$06D0` y vuelve a terminar por `$FF`;
- segundo `$FF` ejecuta `$9B9E`, fuerza Seiya, avanza `$06CE=2`, limpia latches de fase y concede +1000 Seventh Sense mediante 1000 llamadas a `$FDE0`;
- phase2 Talk habilita el orden correcto del support gate; Escape demasiado temprano consume el gate sin abrirlo;
- el support overlay `$068F=$55` usa `$06D4` como bitset: cada apoyo nuevo da +1000; el primero fija `$0587/$0696=3`, evento exacto de Pegasus Rolling Crash;
- phase2 usa siempre Gold slot3; primer `$EB=1` latches `$064D`; `$EB=FF` gana por `$01`; `$EA=1` no termina; `$EA=FF` pierde por `$DD`;
- init estructural phase2 `$9C2C` es inalcanzable porque phase2 no emite `$FF`;
- victoria: `$067D 0D->0E`, `$06CD=00`, `$0673=30`, `$050E=00`, release `$05`, bootstrap `$00->$20`;
- derrota final: progreso `$0D`, `$0673=3F`, `$068F=DD`, overlay dedicado, bootstrap `$90->$91`.

```text
merge 36d5929502f8beb535b44ab3e54d3a1d1213b4a9
head  bfc65e9d18848c7dec9d0b17d328ad0cebbbe254
CI    #341 SUCCESS / #538 SUCCESS
```

Artifacts: `BOSS_CONTEXT_STAGE_0A_SAGA.md`, `SagaStage0AContext.cs`, fixtures y actualización de `BATTLE_TECHNIQUES.md` / `BATTLE_EVENT_DISPATCH.md`.

## Frontera operativa actual

```text
ORIGINAL SPEC / post-Saga ending tail
```

La victoria de Saga entrega control a:

```text
$067D=$0E
$050E=$00
$06CD=$00
$0673=$30
$0670=$05
bootstrap $00 -> state $20
```

El mapper de plataforma `$E4D7/$E4E0` asocia progreso `$0E` con substate `$02=$11`. Ese substate es el especial de dos páginas cuyo exit aceptado salta a `$70` en lugar del reload normal `$3D`.

Las piezas siguientes ya están promovidas por separado:

```text
$11 exit -> $70->$71->$72->$73->$74->$75->$80
$80->$81->$82->$83->$84->$85->$86->$87->$88->$89
$89 -> $04=$8F / $3D -> $E100
```

`PLATFORM_RELOAD_MODE_8F.md` cierra el reload sólo hasta el commit estable `$00` y bootstrap `$00->$20`. La ROM confirma además el setup especial `$068F=$8F` en `$F381`, incluyendo `$06CD/$0673=$20` y `$06CC=$21`. Todavía falta demostrar qué posee el state `$20` después de ese retorno y dónde está el terminal/restart verdadero.

## No reabrir sin evidencia nueva

- global `$00/$01` — #121;
- Taurus — #123;
- Leo — #125;
- Virgo — #127;
- Aquarius — #129;
- Pisces — #131;
- final-special `$0C` — #133;
- Saga `$0A` — #135;
- front-end/title — #119;
- attract — #117/#119;
- `$11-$14`, `$60`, `$91-$99` — #111/#113/#115;
- plataforma/reload/narrativa promovidos;
- boss damage/resources/dodge/techniques genéricos.

## Áreas abiertas

1. cola post-Saga `$0E -> $11 -> $70-$89 -> $8F` hasta terminal/restart real;
2. revisión de cobertura de encounters/stages omitidos si la auditoría final lo exige;
3. renderer/metasprites/CHR global;
4. RNG;
5. audio;
6. texto runtime/localización final;
7. campos persistentes/progresión provisionales;
8. auditoría final ORIGINAL SPEC;
9. REBORN sólo después del cierre integral.

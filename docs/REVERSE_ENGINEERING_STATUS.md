# Estado global de ingeniería inversa

> Documento de orientación global, no operativo. El único `NEXT` autoritativo vive en `docs/PROJECT_STATE.md`.

## Mapa vigente

| Subsistema | Madurez | Estado resumido |
|---|---|---|
| ROM / boot / MMC1 / bancos | HIGH | Target, vectores, mapper y mapa PRG establecidos. |
| Máquina global `$00/$01` | HIGH | PR #121: namespace completo cerrado. |
| Front-end/title/password | HIGH | Modal, attract, codec y overlays ejecutables CHR->RAM cerrados. |
| Plataforma / exits / reload / narrativa | HIGH | Sistemas principales y handoffs ampliamente promovidos. |
| Boss primitives | HIGH | Recursos, clasificadores, daño, técnicas, dodge y dispatchers. |
| Taurus/Aldebaran `$01` | HIGH | Contexto completo, PR #123. |
| Leo/Aioria `$04` | HIGH | Contexto completo, PR #125. |
| Virgo/Shaka `$05` | HIGH | Contexto completo con Ikki + plataforma `$0D` + releases especiales, PR #127. |
| Aquarius/Camus `$08` | HIGH | Dos encuentros Camus, unlocks Hyoga, `$06B8/$06E1`, selector y releases cerrados, PR #129. |
| Pisces/Aphrodite `$09` | ACTIVE | Siguiente contexto: Shun progression, `$064D/$EF`, Talk/dodge y selector determinista. |
| Saga/final y stages especiales | MEDIUM | Stage `$0C`, Saga `$0A` y contextos finales aún requieren composición/auditoría. |
| Renderer / metasprites / CHR | MEDIUM | Spec global pendiente. |
| RNG | LOW-MEDIUM | Abierto. |
| Audio | LOW | Abierto. |
| Texto/localización | MEDIUM | Corpus JP + borrador ES; integración final pendiente. |
| REBORN | EARLY | Congelado hasta cierre integral de ORIGINAL SPEC. |

## Checkpoint técnico reciente — PR #129

Aquarius/Camus `$08` quedó cerrado end-to-end sobre la ROM canónica verificada.

Resultados centrales:

- stage `$08` se divide en primer Camus redirigido y Camus final;
- fixed special resume desde stage `$02` + Hyoga redirige a `$08`, fija `$06B8=$0A` y arma `$0690=$FF`;
- primer Camus usa Talk `$066F` en tres pasos; antes del umbral el post-Bronze aborta la cadena, y después del tercer Talk el siguiente Bronze action ejecuta la secuencia de congelamiento y sale por `$FE`;
- derrota real de Hyoga en el primer Camus también se convierte en `$FE`, no `$FF`;
- ese `$FE` fuerza Seiya y avanza `$067D $02->$03`, stage `$03` Cancer;
- Camus final entra por progreso `$0A`, con `$06B8=0`;
- `$06E1` persiste a través del reset ordinario y gobierna el Talk sin dodge previo;
- init `$9B14` hace el unlock Hyoga 2->3 (`$0588/$0696`), exponiendo Aurora Thunder Attack;
- primer Talk post-dodge con Hyoga hace 3->4, limpia `$0690` y expone Aurora Execution;
- post-Bronze final termina sólo con `$EB=$FF` vía `$FE`; post-Gold final derrota sólo con `$EA=$FF` vía `$FF`;
- selector stage8: primer Camus slot2; Camus final slot1 antes de dos intentos de dodge y slot0 desde dos; slot3 inalcanzable;
- `$FE` final fuerza Seiya y avanza `$067D $0A->$0B`, cuyo siguiente stage es `$09` Pisces.

```text
merge d23388bebaaee4f4dc893c35531188208b9f4944
head  f9ef70eab80baee247d519a85b3a184491f7a76e
CI    #329 SUCCESS / #525 SUCCESS
```

Artifacts: `BOSS_CONTEXT_STAGE_08_AQUARIUS.md`, `AquariusStage08Context.cs`, fixtures y actualización de `BATTLE_TECHNIQUES.md`.

## Frontera operativa actual

```text
ORIGINAL SPEC / boss context stage $09 Pisces
```

Pisces/Aphrodite usa:

```text
init        $9B5C  (RTS)
Talk        $9F99
post-Bronze $AA57
post-Gold   $AAF0
selector    bank6 $90CE-$90EA / stage branch $90D5+
```

Los anchors iniciales muestran una máquina distinta a Aquarius: `$AA57` incrementa `$064D/$EF`, los thresholds `$EF==2/$05` escriben `$0589/$0696`, Talk usa dodge history y una rama especial para Shun, y el selector escala slots 0->1->2 determinísticamente desde `$064D`. El próximo cierre debe probar la reachability exacta de esos caminos y unir `$FE` con el stage especial `$0C`.

## No reabrir sin evidencia nueva

- global `$00/$01` — #121;
- Taurus — #123;
- Leo — #125;
- Virgo — #127;
- Aquarius — #129;
- front-end/title — #119;
- attract — #117/#119;
- `$11-$14`, `$60`, `$91-$99` — #111/#113/#115;
- plataforma/reload/narrativa promovidos;
- boss damage/resources/dodge/techniques genéricos.

## Áreas abiertas

1. Pisces/Aphrodite `$09` end-to-end;
2. stage especial `$0C`, Saga `$0A` y contextos finales;
3. revisión de stages de boss omitidos si la auditoría de cobertura los exige;
4. renderer/metasprites/CHR global;
5. RNG;
6. audio;
7. texto runtime/localización final;
8. campos persistentes/progresión provisionales;
9. auditoría final ORIGINAL SPEC;
10. REBORN sólo después del cierre integral.

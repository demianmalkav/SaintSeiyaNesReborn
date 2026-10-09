# Protocolo de trabajo

## 1. Principio rector

El proyecto separa estrictamente **arqueología del original** de **diseño del remake**.

`ORIGINAL SPEC` responde: **¿qué hace el juego original y cómo lo hace?**

`REBORN` responde: **¿qué queremos conservar, reinterpretar, mejorar o expandir?**

Nunca se modifica una descripción de ORIGINAL SPEC para justificar una decisión de REBORN.

## 2. Fuentes de verdad

Orden de autoridad para comportamiento original:

1. ROM japonesa canónica de **Ōgon Densetsu Kanketsu Hen**, identificada por hash.
2. Ejecución reproducible en emulador/debugger.
3. Desensamblado, trazas de CPU/PPU y breakpoints de RAM.
4. Manual oficial y material contemporáneo.
5. Versiones oficiales relacionadas (primer Ōgon Densetsu de 1987 y WonderSwan Perfect Edition) como evidencia comparativa, no como sustituto de la ROM japonesa.
6. TAS, guías, hacks, traducciones y documentación comunitaria como pistas que deben verificarse cuando sea posible.

## 3. Estados de evidencia

Todo hallazgo relevante llevará una etiqueta:

- `CONFIRMED`: verificado directamente.
- `INFERRED`: inferencia fuerte aún no probada.
- `UNKNOWN`: no resuelto.
- `DISPROVEN`: hipótesis refutada.

Cada entrada técnica debe registrar, cuando corresponda:

- fecha;
- ROM/hash usada;
- dirección CPU/offset/banco;
- condición de reproducción;
- breakpoint o traza utilizada;
- resultado observado;
- nivel de confianza;
- relación con otros hallazgos.

## 4. Flujo de ingeniería inversa

Para cada subsistema:

1. Formular una pregunta concreta.
2. Buscar anclas observables conocidas: RAM, texto, gráficos, input, sonido, flags.
3. Instrumentar el original.
4. Capturar evidencia reproducible.
5. Nombrar rutinas y variables de forma provisional.
6. Validar la hipótesis mediante una segunda prueba o contexto diferente.
7. Promover a `CONFIRMED` sólo cuando la evidencia lo justifique.
8. Documentar comportamiento, no sólo direcciones.

Ejemplo: no basta con decir `$05BC = Cosmo`; debe registrarse en qué contexto aparece, cuándo se escribe, qué rutina lo consume, si es el valor canónico o un buffer y cómo se relaciona con otros valores de Cosmo.

## 5. Nomenclatura de símbolos

Hasta conocer el significado real:

- `unk_XXXX` para rutinas desconocidas;
- `ram_XXXX` para RAM sin semántica confirmada;
- `tbl_XXXX` para tablas;
- `flag_XXXX` sólo cuando se haya demostrado que funciona como flag;
- nombres semánticos definitivos sólo después de validación.

Se evita bautizar prematuramente variables porque los nombres erróneos contaminan todo el análisis posterior.

## 6. Reproducibilidad

Todo descubrimiento importante debe poder repetirse desde un estado conocido.

Se conservarán:

- hash de ROM;
- save state o instrucciones para recrear el estado;
- input necesario;
- direcciones vigiladas;
- resultado esperado.

Los save states y capturas binarias no se versionarán en el repositorio público si contienen datos derivados protegidos; se almacenarán en Drive.

## 7. Git y ramas

`main` representa documentación y código estable.

Ramas recomendadas:

- `reverse/<tema>` para investigación técnica;
- `spec/<subsistema>` para formalizar ORIGINAL SPEC;
- `reborn/<feature>` para implementación moderna;
- `tools/<herramienta>` para extractores, parsers o instrumentation;
- `docs/<tema>` para estado operativo o documentación transversal.

Cambios grandes deben integrarse con commits pequeños y descriptivos. Un commit no debería mezclar, por ejemplo, un hallazgo de RAM con una nueva mecánica de REBORN.

Un checkpoint sólo puede promoverse a `main` después de ejecutar el conjunto mínimo de pruebas relevante. Cuando el cambio toca el runtime de ORIGINAL SPEC y existen workflows de repositorio aplicables, el checkpoint requiere CI verde antes de fusionar.

## 8. Qué vive en GitHub

Sí:

- código propio;
- herramientas propias;
- documentación;
- símbolos y mapas de memoria redactados por nosotros;
- tablas reconstruidas cuando sea legal y razonable;
- pruebas automatizadas;
- localización propia;
- assets originales creados para REBORN.

No:

- ROMs;
- dumps completos de PRG/CHR;
- manuales escaneados protegidos;
- música, sprites o gráficos originales extraídos distribuidos como assets del repositorio;
- material binario del juego que no deba publicarse.

## 9. Qué vive en Drive

Drive es el almacén privado de trabajo pesado:

- ROMs aportadas por el usuario;
- hashes y manifiestos;
- save states;
- traces extensos;
- capturas y vídeos de referencia;
- dumps temporales;
- manuales y documentación aportada;
- arte conceptual y referencias visuales;
- builds de prueba grandes.

## 10. Separación ORIGINAL SPEC / REBORN

Cada subsistema se documentará en paralelo:

### ORIGINAL SPEC

- comportamiento;
- datos;
- fórmulas;
- estados;
- timings relevantes;
- bugs y peculiaridades;
- evidencia.

### REBORN

- elementos preservados;
- elementos corregidos;
- elementos expandidos;
- cambios deliberados;
- justificación de diseño.

Un bug original puede documentarse como `CONFIRMED` y al mismo tiempo decidirse que REBORN no lo reproduce.

## 11. Validación de paridad

Antes de expandir un subsistema, se busca una implementación moderna mínima capaz de reproducir resultados observables del original.

Siempre que sea viable se usarán casos de prueba del tipo:

`estado inicial + inputs + RNG/control de condiciones -> estado final esperado`

La paridad no exige conservar limitaciones gráficas de NES, sino la semántica del sistema que decidamos preservar.

## 12. Diseño moderno

No se añade una mecánica nueva directamente sobre una zona del original que todavía no comprendemos si esa mecánica impediría verificar el comportamiento antiguo.

Orden preferido:

`descubrir -> especificar -> replicar -> probar -> expandir`

## 13. Localización

La fuente primaria es el japonés de la ROM japonesa. Español es el idioma final principal. La política detallada está en `docs/LOCALIZATION.md`.

## 14. Criterio de finalización de una investigación

Un subsistema puede considerarse suficientemente comprendido cuando conocemos, según corresponda:

- entradas;
- salidas;
- estado persistente;
- estado temporal;
- transición de estados;
- tablas de datos;
- fórmulas;
- interacción con otros subsistemas;
- condiciones extremas/bugs relevantes;
- procedimiento reproducible de validación.

No es necesario traducir cada instrucción 6502 a pseudocódigo si ya podemos describir y reproducir correctamente su semántica.

## 15. Estado maestro y semántica de `continúa`

`docs/PROJECT_STATE.md` es la única fuente operativa autorizada para decidir qué se ejecuta a continuación. Los documentos técnicos describen conocimiento; `PROJECT_STATE.md` describe el punto de continuación.

Antes de iniciar una continuación:

1. leer `PROJECT_STATE.md`;
2. comprobar que su último checkpoint sigue correspondiendo con `main`;
3. si Git contiene trabajo fusionado más reciente, reconstruir primero el checkpoint real y actualizar el estado;
4. ejecutar únicamente el `NEXT` vigente.

`continúa` no significa volver a explorar el proyecto completo ni repetir la última investigación. Significa ejecutar el `NEXT` vigente hasta obtener un resultado material o un bloqueo real.

## 16. Carriles FAST y VERIFY

El trabajo se divide deliberadamente en dos carriles:

`FAST -> VERIFY -> CHECKPOINT -> NEXT`

### FAST

Se permite avanzar agresivamente mediante desensamblado, hipótesis, composición de código, fixtures y comparación de evidencia. La meta es producir un resultado material, no maximizar certeza en cada micro-paso.

### VERIFY

Se ejecuta después de una unidad lógica de trabajo. Debe ser el conjunto mínimo de pruebas capaz de detectar que el nuevo resultado rompió una propiedad importante: tests, invariantes, build, comparación binaria, traza o reproducción controlada.

No se mezcla revisión global continua dentro de FAST. La robustez se obtiene mediante verificaciones dirigidas y checkpoints reversibles.

## 17. Criterio de progreso material

Una iteración debe producir al menos uno de estos resultados:

- código;
- test o fixture;
- evidencia nueva;
- mapa/tabla/documento técnico nuevo;
- hipótesis descartada con fundamento;
- decisión arquitectónica explícita respaldada por evidencia.

Una iteración que sólo reformula lo ya conocido no cuenta como avance y no justifica otro ciclo idéntico.

Cada bloque termina actualizando en `PROJECT_STATE.md`:

`DONE / EVIDENCE / OPEN / NEXT / BLOCKERS / ANTI-LOOP`.

## 18. Presupuesto de reintentos y regla anti-loop

Una estrategia puede intentarse como máximo dos veces sin nueva evidencia material.

Si dos ciclos consecutivos terminan con el mismo `NEXT`, el mismo bloqueo y esencialmente la misma evidencia, está prohibido repetir la misma técnica una tercera vez. Se debe hacer una de estas tres cosas:

1. cambiar de fuente o técnica;
2. retroceder al último checkpoint verificado y tomar otra vía;
3. declarar el bloqueo de forma explícita y mover el `NEXT` a una investigación que pueda resolverlo.

Una fase cerrada sólo se reabre cuando aparece evidencia contradictoria, un test que falla o un efecto lateral recién demostrado que atraviesa su frontera. La mera incertidumbre en otra parte del sistema no alcanza.

## 19. Checkpoints semánticos

No se crean checkpoints por cantidad de operaciones sino por cambios materiales de estado, por ejemplo:

- formato de datos identificado;
- decoder round-trip verificado;
- rutina o subsistema compuesto con fixtures discriminantes;
- fórmula confirmada;
- integración cerrada con CI verde.

Los commits deben describir el resultado semántico conseguido. El checkpoint registra también la evidencia que lo valida y el próximo límite todavía abierto.

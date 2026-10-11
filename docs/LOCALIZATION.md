# Política de localización JP → ES

## Objetivo

La versión japonesa de **Saint Seiya: Ōgon Densetsu Kanketsu Hen** es la fuente narrativa primaria. **SaintSeiyaNesReborn** tendrá español como idioma principal de presentación.

## Principios

1. Traducir desde japonés siempre que sea posible.
2. Usar traducciones inglesas existentes sólo como referencia secundaria.
3. No heredar automáticamente decisiones del doblaje, manga o juegos posteriores.
4. Preservar intención, carácter y función dramática por encima de restricciones de longitud de NES.
5. Separar texto de lógica de juego.

## Identificadores estables

La ingeniería inversa del motor de texto confirmó 251 mensajes canónicos, numerados `0..250`. Por tanto, la identidad primaria e inmutable de cada entrada es:

`MSG_000` ... `MSG_250`

Sobre ese ID numérico se puede añadir un alias semántico cuando la escena y el hablante estén confirmados, por ejemplo:

`TAURUS_ALDEBARAN_INTRO_001`

El alias semántico es descriptivo y puede mejorarse; `MSG_xxx` no cambia nunca.

Campos actuales del catálogo privado:

- `id`
- `stable_id`
- `jp_original`
- `es_draft`
- `status`
- `speaker`
- `scene`
- `semantic_alias`
- `source_text_offset`

## Metadata canónica de solicitud

La integración runtime conserva además la metadata que proviene de los cuatro entrypoints originales del motor de texto:

```text
$E7B3 -> $066A, $0672=$FF
$E7B7 -> $066A, $0672=$00
$E7C3 -> $066B, $0672=$FF
$E7C7 -> $066B, $0672=$00
```

El contrato moderno preserva por tanto `(MSG_xxx, slot $066A/$066B, variante $0672)` sin inventar una semántica más estrecha para `$0672` que la evidencia disponible no demuestra.

## Estados de traducción

- `RAW`: japonés extraído, sin traducción.
- `DRAFT`: primera traducción directa JP → ES.
- `REVIEWED`: revisada con contexto, hablante y terminología.
- `FINAL`: aprobada para el juego.
- `NEEDS_CONTEXT`: requiere observar o reconstruir mejor la escena antes de decidir.

## Glosario

Las decisiones terminológicas se centralizan en `docs/localization/GLOSSARY_ES.md`.

Como mínimo se fijan globalmente:

- Saint / Caballero;
- Cloth / Armadura;
- Cosmos;
- Sanctuary / Santuario;
- Gold Cloth / Armadura Dorada;
- Bronze Saint / Caballero de Bronce;
- nombres de técnicas;
- nombres propios y transliteraciones.

No se cambia terminología escena por escena.

## Técnicas

Para técnicas con nombre japonés se evalúan tres posibilidades según contexto:

1. nombre tradicional consolidado en español;
2. romanización/nombre internacionalizado;
3. combinación de nombre localizado + original en materiales de apoyo.

La decisión será global y documentada antes de promover las líneas correspondientes a `FINAL`.

## Restricciones tipográficas

REBORN no hereda automáticamente el límite de caracteres ni el ancho de las cajas de texto de NES. La traducción puede ser natural y completa.

Sin embargo, el texto debe conservar ritmo y función de la escena. No se utilizará la mayor capacidad moderna como excusa para sobreescribir diálogos breves.

## Modo de desarrollo

Durante desarrollo debe ser posible consultar el japonés asociado a una línea española. El sistema de localización permite seleccionar `JP` o `ES` sobre el mismo `MSG_xxx` y conserva slot/variante canónicos para que la presentación moderna no mezcle identidad de contenido con lógica de escena.

La política de fallback es explícita:

- `ES + Japanese fallback`: si una entrada española todavía está vacía, se presenta su fuente JP y se marca que hubo fallback;
- `ES + None`: una entrada española vacía es error;
- `JP`: nunca se sintetiza a partir de ES ni cambia de ID.

## Fuente y trazabilidad

Cada texto español debe poder remontarse a una línea japonesa concreta y a su offset/entrada dentro del original. Esto permite distinguir:

- traducción;
- adaptación;
- texto nuevo exclusivo de REBORN.

El texto nuevo nunca debe hacerse pasar por texto presente en el juego de 1988.

## Catálogo privado

El contenido íntegro JP/ES no se versiona en el repositorio público. El catálogo de trabajo se conserva en el workspace privado del proyecto y puede regenerarse desde la ROM japonesa mediante `tools/reverse/extract_japanese_script.py`.

El borrador actual cubre los 251 IDs y debe pasar:

```text
python tools/localization/validate_catalog.py <catalogo.csv>
```

El validador comprueba cobertura exacta `0..250`, IDs estables, estados, presencia de fuente/traducción y trazabilidad de offsets sin conocer ni incorporar el contenido del guion.

## Contrato runtime público

`src/SaintSeiyaNesReborn.OriginalSpec/CanonicalRuntimeLocalization.cs` carga el mismo esquema CSV desde una fuente externa y aplica los invariantes necesarios en runtime:

- exactamente 251 entradas;
- IDs `0..250` sin duplicados;
- `stable_id` coherente con el ID numérico;
- fuente japonesa presente;
- selección JP/ES y fallback determinista;
- propagación de slot, variante, speaker, scene, alias y offset sin convertir metadata descriptiva en identidad primaria.

Los fixtures públicos generan cadenas sintéticas; no incluyen ninguna línea del guion japonés ni de la traducción española. La especificación técnica completa de esta frontera está en `docs/reverse-engineering/CANONICAL_RUNTIME_TEXT_CONTENT.md`.

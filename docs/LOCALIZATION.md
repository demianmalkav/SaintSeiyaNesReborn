# Política de localización JP → ES

## Objetivo

La versión japonesa de **Saint Seiya: Ōgon Densetsu** es la fuente narrativa primaria. **SaintSeiyaNesReborn** tendrá español como idioma principal de presentación.

## Principios

1. Traducir desde japonés siempre que sea posible.
2. Usar traducciones inglesas existentes sólo como referencia secundaria.
3. No heredar automáticamente decisiones del doblaje, manga o juegos posteriores.
4. Preservar intención, carácter y función dramática por encima de restricciones de longitud de NES.
5. Separar texto de lógica de juego.

## Identificadores estables

Cada texto tendrá un ID semántico estable, por ejemplo:

`DIALOGUE_TRAINING_MARIN_001`

Campos previstos:

- `id`
- `jp_original`
- `es_final`
- `speaker`
- `scene`
- `context`
- `source_pointer_or_reference`
- `notes`
- `status`

## Estados de traducción

- `RAW`: japonés extraído, sin traducción.
- `DRAFT`: primera traducción.
- `REVIEWED`: revisada con contexto.
- `FINAL`: aprobada para el juego.
- `NEEDS_CONTEXT`: requiere observar la escena antes de decidir.

## Glosario

Las decisiones terminológicas deben centralizarse. Como mínimo se fijarán:

- Saint / Santo / Caballero
- Cloth / Armadura
- Cosmos
- Sanctuary / Santuario
- Gold Cloth / Armadura de Oro
- Bronze Saint
- nombres de técnicas
- nombres propios y transliteraciones

No se debe cambiar terminología escena por escena.

## Técnicas

Para técnicas con nombre japonés se evaluarán tres posibilidades según contexto:

1. nombre tradicional en español;
2. romanización japonesa;
3. combinación de nombre localizado + nombre original en materiales de apoyo.

La decisión será global y documentada.

## Restricciones tipográficas

REBORN no hereda automáticamente el límite de caracteres ni el ancho de las cajas de texto de NES. La traducción puede ser natural y completa.

Sin embargo, el texto debe conservar ritmo y función de la escena. No se utilizará la mayor capacidad moderna como excusa para sobreescribir diálogos breves.

## Modo de desarrollo

Durante desarrollo debe ser posible consultar el japonés asociado a una línea española. Idealmente el sistema de localización permitirá alternar `JP` y `ES` en builds de depuración.

## Fuente y trazabilidad

Cada texto español debe poder remontarse a una línea japonesa concreta y, cuando sea posible, a su puntero/entrada dentro del original. Esto permite distinguir:

- traducción;
- adaptación;
- texto nuevo exclusivo de REBORN.

El texto nuevo nunca debe hacerse pasar por texto presente en el juego de 1987.

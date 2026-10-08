# Kanketsu Hen — investigación previa a la decompilación

Este documento reúne pistas externas útiles. Ninguna etiqueta semántica de RAM o fórmula pasa a ORIGINAL SPEC sólo por aparecer aquí: debe reproducirse contra la ROM canónica.

## ROM / comunidad de TAS

TASVideos mantiene una entrada específica para el juego y reconoce como good dump la misma revisión que usamos:

- SHA-1 `F871D9B3DAFDDCDAD5F2ACD71044292E5169064E`
- MD5 `3B0F17C2B6EFC928B3D3FE9B1A389680`

También publica un archivo RAM Watch `.wch` específico para Kanketsu Hen.

Fuentes:
- https://tasvideos.org/1074G
- https://tasvideos.org/UserFiles/Game/1074

## Estructura sistémica documentada

El juego alterna dos contextos principales:

1. plataforma / desplazamiento hacia los templos;
2. combate 1 contra 1 de tipo RPG contra personajes importantes.

La selección/configuración de personaje constituye además un tercer contexto de UI/estado relevante.

La guía moderna documenta:

- Cosmo por personaje;
- Life por personaje;
- Seven Senses como recurso compartido;
- conversión 1:1 entre Seven Senses y Life/Cosmo durante configuración;
- un personaje muere si Life o Cosmo llegan a cero;
- cambiar de personaje durante combate cuenta como muerte de ese personaje;
- el Gold Saint conserva el daño acumulado entre personajes;
- los Bronze Saints reviven después de derrotar al Gold Saint;
- tras 30 segundos sin desplazamiento en plataforma empiezan a caer rocas;
- fórmula reportada para técnicas: `daño total = Cosmos × (Daño / 100)`.

Fuente secundaria para reproducir y verificar:
- https://gamefaqs.gamespot.com/nes/562974-saint-seiya-ougon-densetsu-kanketsu-hen/faqs/74512/2-como-jugar

## Personajes y bosses

Valores iniciales reportados para aliados:

- Seiya: Life 99, Cosmo 99
- Shiryu: Life 99, Cosmo 99
- Hyoga: Life 99, Cosmo 99
- Shun: Life 99, Cosmo 99
- Ikki: Life 499, Cosmo 499

La guía también enumera Life/Cosmo/EXP de bosses y técnicas. Estos valores sirven como patrones de búsqueda y validación de tablas de datos.

Fuente:
- https://gamefaqs.gamespot.com/nes/562974-saint-seiya-ougon-densetsu-kanketsu-hen/faqs/74512/3-personajes

## Mecánicas de combate observadas por TAS

La investigación TAS de eien86 describe una ventana temporal para parry de ataques de Gold Saints con probabilidad aproximada del 50%. También señala que Cosmo y HP pueden ajustarse al llegar a un templo y que el password system fue descifrado empíricamente por el autor.

La optimización de Maomao (2026) aporta condiciones observables adicionales:

- Life > 109 evita determinadas secuencias de diálogo de boss;
- elección de técnica afecta no sólo daño sino duración de animación y resolución;
- el estado del boss puede resolverse por evento narrativo sin una muerte convencional (Gemini);
- rutas y glitches dependen de X/Y y posición de cámara;
- daño intencional en un templo puede preparar un estado favorable varios templos después;
- distribución anticipada de Cosmo entre personajes afecta estrategias posteriores.

Fuentes:
- https://tasvideos.org/8397S
- https://tasvideos.org/10683S

Estas observaciones sugieren que ORIGINAL SPEC debe modelar explícitamente diálogo/flags, persistencia entre templos, cámara y battle resolution, no sólo HP/daño.

## RAM anchors externos

GameHacking.org publica cheats para la revisión con CRC de payload `9561798D`. Entre las direcciones reportadas:

- `$0059/$005A`: Cosmo (fuente VisitntX)
- `$0063/$0064`: Energy/Life (fuente VisitntX)
- `$05AA/$05AB`: Seven Senses
- `$05BC`: Cosmo (fuente ReyVGM)
- `$05CF`: Energy/Life (fuente ReyVGM)
- `$0076`: invulnerability/flashing

Fuente:
- https://gamehacking.org/game/30740

La existencia de dos ubicaciones distintas para conceptos similares hace especialmente importante distinguir valores canónicos de copias/buffers y contextos (plataforma vs combate vs configuración).

## Traducciones / romhacks como diferenciales

La traducción inglesa aishsha/Djinn v1.01 documenta explícitamente:

- hacking del juego;
- edición gráfica;
- trabajo sobre codificación Huffman para disponer de mucho más espacio de texto;
- corrección posterior del password mostrado tras game over.

Esto la convierte en un excelente binario diferencial para localizar texto, renderer, password y expansión/relocalización de datos.

Una traducción PT-BR posterior declara estar basada en esa traducción inglesa. No se usará como fuente canónica para español.

Referencias:
- romhacking.net translation 1487 / mirrors de documentación pública
- https://joao13traducoes.com/2018/03/nes-saint-seiya-ougon-densetsu-kanketsu-kanketsu-hen-luis-victor/

## Política de localización

El texto final de REBORN será español, traducido desde la fuente japonesa siempre que sea posible. Inglés, portugués y guías modernas son referencias contextuales y técnicas, no cadena de traducción primaria.

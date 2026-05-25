# Imágenes de la web

Esta carpeta es para los assets visuales del sitio. La CSS ya los busca por nombre exacto;
si dejás un archivo con el nombre correcto, lo levanta automáticamente.

## Imágenes que la CSS ya está buscando

| Archivo            | Dónde aparece                                          | Tamaño sugerido          |
|--------------------|--------------------------------------------------------|--------------------------|
| `hero.jpg`         | Banner de fondo del hero en `/` (con opacidad 18%)     | 1920×800, JPG comprimido |

Sin estos archivos la web sigue funcionando — el hero queda con el gradient gold/dark
que se ve perfecto igual, sólo que sin imagen de fondo.

## De dónde sacar imágenes de MU

- Screenshots in-game del propio cliente (mejor opción — fidelidad visual y propiedad
  del contenido).
- Webzen tiene assets oficiales que se pueden re-usar para sitios privados de MU.
- Fan wikis y comunidades (chequear licencias).

## Tips

- Para el hero, una imagen oscura con buen contraste funciona mejor (la CSS le pone un
  gradient negro encima al 65–90% de opacidad).
- Comprimí los JPGs antes de subir (squoosh.app, tinypng.com) — si la web va a estar
  en la VM con upload limitado, una hero de 200KB es mucho mejor que una de 2MB.

## Más imágenes que podríamos sumar más adelante

- `logo.png` para reemplazar el ⚔ del header
- `class-bk.png`, `class-sm.png`, etc. para la sección de clases de la guía
- `goldens/budge.png`, `goldens/goblin.png`, etc. para la guía de cajas

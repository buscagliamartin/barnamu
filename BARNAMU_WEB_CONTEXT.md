# BarnaMu Web — Frontend & Design Context

> **Objetivo del Agente:** Sos un experto en diseño web, UI/UX y ASP.NET Core Razor Pages. Tu trabajo es EXCLUSIVAMENTE sobre el directorio `C:\MuDev\BarnaMuWeb\`. 
> NO toques la lógica del servidor de juego (OpenMU) ni la base de datos a menos que afecte directamente la vista (UI).

## 1. Stack Tecnológico
- **Framework:** ASP.NET Core Razor Pages (.NET 10).
- **Estilos:** Bootstrap, CSS personalizado (`dmncms/barnamed.css`, `style.css`).
- **Fuentes:** Google Fonts (Cinzel + Exo2), FontAwesome.
- **Ruta de trabajo:** `C:\MuDev\BarnaMuWeb\`

## 2. Estructura del Directorio Web
- `Pages/`: Vistas Razor (`.cshtml`). Archivos clave: Index, Register, Vip, Guide, Rankings, Status, Download, ReportBug.
- `Pages/Shared/_Layout.cshtml`: El template principal (estilo Webzen/BarnaMu, navbar, sidebar con relojes e info del server).
- `wwwroot/`: Archivos estáticos (CSS, imágenes, JS).
- `Content/`: Archivos JSON (`news.json`, `guide.json`) que alimentan las vistas sin necesidad de recompilar.

## 3. Reglas de Diseño
- Mantener la estética "Dark Fantasy" / Mu Online (tonos oscuros, dorados, fuentes serifadas para títulos como Cinzel).
- Todo el trabajo debe ser responsivo (mobile-first donde sea posible usando las clases de Bootstrap).
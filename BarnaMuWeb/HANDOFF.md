# BarnaMu Web — Handoff Resumido

## Stack
- ASP.NET Core Razor Pages, .NET 10, Dapper + Npgsql, BCrypt.Net-Next
- DB: PostgreSQL (OpenMU schema) — esquemas `data`, `config`, `guild`
- Namespace raíz: `BarnaMu.Web`; páginas: `BarnaMu.Web.Pages`

## Regla crítica — directorio de publicación
El servidor corre desde `C:\MuDev\BarnaMuWeb\publish` (pre-compilado).
Editar fuente en `C:\MuDev\BarnaMuWeb\` no tiene efecto hasta republicar:
```
cd C:\MuDev\BarnaMuWeb
dotnet publish -c Release -o .\publish
```
CSS es archivo estático → se puede copiar directo a publish sin republicar.
CSHTML/C# → SIEMPRE requiere `dotnet publish`.

## Estado actual
**TODO el trabajo está hecho en el source. El usuario NUNCA ha publicado desde que empezamos.**
El CSS en publish ya fue sincronizado manualmente, pero el HTML compilado en el DLL
sigue siendo el viejo. Por eso no se ven cambios. Solución: `dotnet publish`.

## Archivos modificados (source)

### CSS
- `wwwroot/css/barnamed.css` — reescrito completo (Dark Fantasy v3.0, 867 líneas)
  - Tokens CSS en `:root` (--barna-bg, --barna-gold, --barna-surface, etc.)
  - Fuentes: Cinzel (display) + Exo 2 (body), cargadas vía Google Fonts en _Layout
  - Glassmorphism en .card y .barna-widget (backdrop-filter: blur)
  - Hero con imagen elf: gradiente left→right (oscuro izq, transparente der)
  - Clases nuevas: .barna-topbar, .barna-nav, .barna-logo, .barna-page-wrapper,
    .barna-main, .barna-sidebar, .barna-widget, .barna-quicklinks__item,
    .barna-info-list, .barna-clock, .stat-grid, .stat-item, .status-indicator,
    .status-glow, .barna-tabs, .barna-tab, .rank-num, .char-name, .val-gold,
    .val-teal, .rank-1, .pill-online, .pill-offline, .btn-gold, .btn-secondary,
    .btn-block, .field, .perk-list, .benefit-list, .vip-price, .badge-vip,
    .dl-version-badge, .sha-code, .grid-2, .page-narrow, .page-medium

### Shared Layout
- `Pages/Shared/_Layout.cshtml` — reescrito completo
  - Inyecta: `@inject IOptions<BarnaMuOptions> BarnaMuOpts`
  - Header fijo: `.barna-topbar` con nav activa por `ViewContext.RouteData.Values["page"]`
  - Layout: `.barna-page-wrapper` → `.barna-main` + `.barna-sidebar`
  - Sidebar widgets: Accesos Rápidos | Servidor | Horario (2 relojes) |
    Próximos Eventos (6: Golden Invasion, Dragon Invasion, Test9, Blood Castle,
    Devil Square, Chaos Castle) | Castle Siege countdown | En línea ahora
  - JS inline: tick de relojes UTC/ARG, nextFire(periodH, anchorH, anchorM) para
    countdowns de eventos, nextSiege() (sábados 18:00 UTC), polling /api/online-count
    cada 30s con animación pulse verde

### Pages (todas: removido <style> local, usan clases de barnamed.css)
- `Index.cshtml` — hero grande, .grid-2, .perk-list, botones .btn-gold / .btn-secondary
- `Rankings.cshtml` — search bar (#rankSearch) + JS filter client-side,
  tabs (.barna-tabs/.barna-tab), tabla (.barna-table), nombres linkeados a /char/{name}
- `Vip.cshtml` — .vip-price, .benefit-list, .badge-vip, .barna-table
- `Status.cshtml` — clases condicionales Razor: `class="status-indicator @statusClass"`
- `Download.cshtml` — .dl-version-badge, .sha-code, .step-list
- `Register.cshtml` — .field, .btn-gold.btn-block, .page-narrow
- `ReportBug.cshtml` — igual a Register
- `Guide.cshtml` — .hero-small, .card

### Backend nuevo (mínimo, aprobado por el usuario)
**`Data/BarnaMuDb.cs`** — agregados:
- `GetOnlinePlayersAsync()`: `SELECT COUNT(*) FROM data."Account" WHERE "State" != 0 AND "IsTemplate" = false`
- `GetCharacterProfileAsync(string name)`: JOIN Character + CharacterClass + Guild + StatAttribute
  - Atributos por GUID: Level=560931AD-..., MasterLevel=70CD8C10-..., Resets=89A891A7-...
  - WHERE `LOWER(c."Name") = LOWER(@Name)`
- Clase `CharacterProfile`: Name, ClassName, CreateDate, Level, MasterLevel, Resets, GuildName?

**`Pages/Api/OnlineCount.cshtml`** + **`.cshtml.cs`** — nuevo
- Ruta: `/api/online-count`
- Namespace: `BarnaMu.Web.Pages.Api`
- Devuelve JSON: `{ "count": N }`

**`Pages/Char.cshtml`** + **`Pages/Char.cshtml.cs`** — nuevo
- Ruta: `/char/{name}` (`@page "{name}"`)
- Namespace: `BarnaMu.Web.Pages`
- Muestra: stat-grid (Resets/Level/MasterLevel) + grid-2 info cards
- `var c = Model.Character!;` (null-forgiving, nullable enable)
- Maneja not-found y error gracefully

## Lo que falta hacer
1. **Correr `dotnet publish -c Release -o .\publish`** desde `C:\MuDev\BarnaMuWeb`
2. Reiniciar el servidor
3. Verificar: home, rankings, /char/NombrePersonaje, sidebar events, /api/online-count

## Pendiente a futuro (usuario dijo "no ahora")
- Sistema de noticias con editor
- Widget de Discord
- Calculadora de Chaos Machine

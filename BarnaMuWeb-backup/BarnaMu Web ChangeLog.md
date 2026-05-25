# BarnaMu Web ChangeLog

## Project Objectives & Overview

- Build a public BarnaMu website for registration, rankings, server status, downloads, VIP information, guides, news, and bug reporting.
- Keep the web project isolated from the game server codebase: frontend and web-facing logic live in `C:\MuDev\BarnaMuWeb\`.
- Use a MU Online / Webzen-inspired dark fantasy presentation while keeping the pages readable, responsive, and practical for players.
- Allow common content updates, such as news and guide sections, without requiring a recompile.

---

## Technology Stack

| Area | Implementation |
|---|---|
| Framework | ASP.NET Core Razor Pages (.NET 10) |
| Styling | Bootstrap + custom CSS |
| Custom CSS | `wwwroot/css/style.css`, `wwwroot/dmncms/barnamed.css` |
| Fonts | Google Fonts: Cinzel + Exo2 |
| Icons | FontAwesome |
| Database access | Npgsql + Dapper |
| Password hashing | BCrypt.Net-Next 4.0.3 |
| Working path | `C:\MuDev\BarnaMuWeb\` |

---

## Web Project Structure

| Path | Purpose |
|---|---|
| `Pages/` | Razor Pages for the public website. |
| `Pages/Shared/_Layout.cshtml` | Main layout: Webzen-style shell, navbar, sidebar, server info, clocks, shared scripts/styles. |
| `wwwroot/` | Static assets: CSS, JavaScript, images, vendor assets. |
| `Content/news.json` | Editable news content rendered on the home page. |
| `Content/guide.json` | Editable guide sections rendered on the guide page. |
| `Data/` | Web database access, options, and ranking models. |
| `Services/` | Web-only services such as server status checks. |

---

## Main Pages

| Page | Purpose |
|---|---|
| `Index` | Home page with news, server identity, rates, and main player-facing highlights. |
| `Register` | Account creation page using the same BCrypt-compatible password format as OpenMU. |
| `Vip` | VIP benefits, price/payment information, rate comparison, and map access details. |
| `Guide` | Player guide rendered from editable JSON content. |
| `Rankings` | Ranking tables for resets, levels, and guilds. |
| `Status` | Live service status and basic server counters. |
| `Download` | Client download page with version/checksum and connection notes. |
| `ReportBug` | Public bug report form stored in the web schema. |

---

## Design Direction

- Visual style follows a dark fantasy MU Online identity: dark backgrounds, gold accents, ornamental headings, and game-like UI framing.
- Typography uses Cinzel for fantasy-style titles and Exo2 for readable interface text.
- Layout is responsive and Bootstrap-based, with mobile-friendly behavior where possible.
- The UI should feel like a server portal, not a generic SaaS site.
- Server information, navigation, rankings, guide content, and player actions must remain easy to scan.

---

## Functional Changes

### Registration

- Added public account registration through `Register`.
- Validates login name, password length, security code, and optional email.
- Uses BCrypt.Net-Next 4.0.3 so accounts created on the website can log into OpenMU directly.
- Writes new accounts into `data."Account"`.
- Applies registration rate limiting.

### Server Status

- Added live status checks for Connect Server and Game Server ports.
- Uses short TCP probes with caching to avoid excessive polling.
- Exposes results on the `Status` page and shared layout areas.

### Rankings

- Added public rankings for characters and guilds.
- Reads account/character/stat/guild data directly from PostgreSQL.
- Uses known OpenMU stat attribute IDs for level, master level, and resets.

### VIP Page

- Added VIP benefits page.
- Mirrors BarnaMu gameplay perks: VIP rates, map access, Chaos Machine bonus, `/offlevel`, vault extension, and other player-facing benefits.
- Reads configurable price/payment/instruction text from `appsettings.json`.

### Guide & News Content

- Added JSON-driven guide and news content.
- `Content/news.json` can be edited to update home page news.
- `Content/guide.json` can be edited to update guide sections without recompiling the site.

### Bug Reports

- Added public bug report form.
- Creates and writes into `web."BugReport"`.
- Stores title, body, reporter, optional email, timestamp, IP, user agent, and status.
- Applies bug report rate limiting.

---

## Configuration

Primary configuration lives in:

```text
appsettings.json
```

Main configurable areas:

| Section | Purpose |
|---|---|
| Connection string | PostgreSQL access. |
| Server name/version/checksum | Public-facing server/client information. |
| Connect/Game server host and ports | Status checks. |
| Rates | Normal and VIP rate display. |
| VIP | Price, payment links, WhatsApp, and purchase instructions. |
| Maps | Map entry level table shown to players. |

---

## Database Touch Summary

### Read

- `data."Account"`
- `data."Character"`
- `data."StatAttribute"`
- `config."AttributeDefinition"`
- `guild."Guild"`
- `guild."GuildMember"`

### Write

- `data."Account"` for registration.
- `web."BugReport"` for public bug reports.

The web project does not modify game server logic. It only reads game data for public display and writes web/account-related records required by the website.

---

## Main Files

| File | Purpose |
|---|---|
| `Program.cs` | Web startup, options, services, rate limiting, Razor Pages setup, web schema initialization. |
| `appsettings.json` | Server info, rates, VIP settings, maps, connection string. |
| `Data/BarnaMuDb.cs` | Database access for registration, rankings, counters, bug reports, and web schema setup. |
| `Data/BarnaMuOptions.cs` | Strongly typed web configuration. |
| `Services/ServerStatusService.cs` | TCP status checks for public server status display. |
| `Pages/Shared/_Layout.cshtml` | Global layout and Webzen-style page shell. |
| `Pages/Index.cshtml` | Home page. |
| `Pages/Register.cshtml` | Account registration UI. |
| `Pages/Vip.cshtml` | VIP information UI. |
| `Pages/Guide.cshtml` | JSON-driven guide UI. |
| `Pages/Rankings.cshtml` | Rankings UI. |
| `Pages/Status.cshtml` | Server status UI. |
| `Pages/Download.cshtml` | Client download UI. |
| `Pages/ReportBug.cshtml` | Bug report UI. |
| `Content/news.json` | Editable news content. |
| `Content/guide.json` | Editable guide content. |

---

## Gameplay & Player Impact

- Players can register accounts from the website and immediately use them in-game.
- Players can check whether the server is online before launching the client.
- Players can view rankings and server progression publicly.
- VIP benefits and map access are documented clearly outside the game.
- New players have a central place for downloads, guides, commands, and server information.
- Bug reports can be submitted without Discord or manual messages.

---

## Deployment Notes

Planned/used public web deployment:

```text
http://*:8081
```

Typical publish command:

```bat
dotnet publish -c Release -o C:\MuDev\BarnaMuWeb\publish
```

Runtime entry:

```bat
BarnaMuWeb.exe
```

The BAT script system can start the published web app through:

```bat
BarnaMu.bat start web
BarnaMu.bat start all
```

---

## Result

- BarnaMu now has a standalone public website separated from OpenMU server internals.
- The site provides the main public-facing tools needed for launch: registration, status, rankings, VIP information, guide content, downloads, news, and bug reports.
- Content can be updated through JSON files and configuration without touching the game server code.


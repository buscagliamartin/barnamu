# BarnaMu — Project Context (Full Handoff)

> **Handoff file between AI sessions.** Read this first at the start of every new chat.
> Update at the end of each session with relevant changes.
>
> **Last updated:** 2026-05-17 (full rebuild after accidental deletion)

---

## 0. How to use this file

- An AI starting fresh: read top to bottom once, then jump as needed.
- Martin: keep section 21 ("New tasks") current.
- After each session that introduces real changes, append to section 19 ("Session log") and bump the date at the top.

---

## 1. Who and what

**Martin Buscaglia** owns BarnaMu — an MU Online Season 6 Episode 3 private server built on top of the open-source **OpenMU** reimplementation (C# / .NET 10 / PostgreSQL).
Goal: a public, stable, slow-medium-rate MU server with VIP, automatic invasions, balanced drops, public web for registration + rankings + guide.

GitHub backup: `https://github.com/buscagliamartin/barnamu`.

---

## 2. Stack and infrastructure

- **Host OS:** Windows 10 inside VMware
- **VM IP:** `192.168.1.52`
- **Public IP / DDNS:** `83.43.3.212` / `barnamu.ddns.net` (No-IP DUC running on VM; renew at noip.com every 30 days)
- **PostgreSQL 16** — DB `openmu`, user `postgres`, password `W3l.c0m3`. Schemas: `config` (game configuration), `data` (player/account/character data), `friend`, `guild`, `web` (created by BarnaMuWeb for bug reports).
- **OpenMU source:** `C:\MuDev\OpenMU\src\` (compiled from source with `Recompilar.bat`).
- **Web project:** `C:\MuDev\BarnaMuWeb\` (standalone ASP.NET Core Razor Pages, .NET 10).
- **Game client:** `C:\MuDev\BarnaMu-Client\` (Season 6 official-style client, `main.exe` patched to connect directly to `barnamu.ddns.net:44405`).
- **Forwarded ports:** 44405 (Connect Server), 55901 (Game Server), 80 (Admin Panel — currently LAN only), 8081 (planned for public web).
- **Auto-start:** Task Scheduler entries `BarnaMu-Server` (game server, runs `AutoRestart.bat` at boot) and `BarnaMu-Backup` (daily DB backup at 04:00).
- **Backup output:** `C:\MuDev\Backups\` (gitignored exception: stays in repo per Martin's request, see Push.bat below).

---

## 3. Top-level `C:\MuDev\` layout

```
C:\MuDev\
├── OpenMU\              ← Server source code (.NET 10, EF Core, PostgreSQL)
│   ├── src\             ← All projects (GameLogic, ConnectServer, Persistence, Web/AdminPanel, etc.)
│   └── .gitignore       ← Comprehensive VS / build artifact ignore
├── BarnaMuWeb\          ← Public website (ASP.NET Core Razor Pages)
│   ├── Pages\           ← Razor pages: Index, Register, Vip, Guide, Rankings, Status, Download, ReportBug
│   ├── Data\            ← BarnaMuDb (Dapper-based), BarnaMuOptions, RankingRow, etc.
│   ├── Services\        ← ServerStatusService (TCP probes)
│   ├── Content\         ← news.json, guide.json (editable without recompile)
│   ├── wwwroot\         ← Static assets (CSS, images, JS)
│   └── appsettings.json ← Connection string, server config, maps table, VIP info
├── BarnaMu-Client\      ← MU Online game client (gitignored)
├── Backups\             ← Daily/weekly DB dumps (tracked in git)
├── Recompilar.bat       ← Compiles OpenMU (dotnet build sln -c Release)
├── AutoRestart.bat      ← Loop that restarts the game server if it crashes
├── StartServer.bat      ← Wrapper that calls AutoRestart.bat
├── Backup.bat           ← Daily + Sunday weekly Postgres dump
├── Push.bat             ← Backup + git add + commit + push to GitHub
├── BARNAMU_CONTEXT.md   ← THIS FILE (the master handoff doc)
├── BARNAMU_SESION_*.md  ← Per-session checkpoints (cajas/drops, VIP, tier items, etc.)
└── BARNAMU_*.sql        ← One-off SQL scripts (drops, tier items, map levels, etc.)
```

---

## 4. Server / OpenMU project map

OpenMU is a big solution. Relevant projects for BarnaMu work:

| Project | Path | Role |
|---|---|---|
| `MUnique.OpenMU.GameLogic` | `OpenMU\src\GameLogic\` | Core game logic — player actions, chat commands, plugins. Most BarnaMu code changes live here. |
| `MUnique.OpenMU.DataModel` | `OpenMU\src\DataModel\` | Entity definitions (`Account.cs`, `Character.cs`, `ItemDefinition.cs`, etc.). Add fields here when extending entities. |
| `MUnique.OpenMU.Persistence` | `OpenMU\src\Persistence\` | Repository contracts and BasicModel (JSON). |
| `MUnique.OpenMU.Persistence.EntityFramework` | `OpenMU\src\Persistence\EntityFramework\` | EF Core implementation + Postgres-specific config + migrations. |
| `MUnique.OpenMU.Persistence.SourceGenerator` | `OpenMU\src\Persistence\SourceGenerator\` | Roslyn generator that produces `BasicModel\*.Generated.cs` and `EntityFramework\Model\*.Generated.cs` from `DataModel\Entities`. **Build it once manually if its `bin/` is empty** (see gotcha below). |
| `MUnique.OpenMU.Persistence.Initialization` | `OpenMU\src\Persistence\Initialization\` | First-run data seeders. Reference for which item is which (Group/Number) and how default DropItemGroups, ChaosMixes, SocketSystem etc. are wired. **Edits here only affect new DBs**, not running ones. |
| `MUnique.OpenMU.ConnectServer` | `OpenMU\src\ConnectServer\` | Login broker on port 44405. Hosts `ClientListener.cs` with `MaxConnectionsPerIp = 2`. |
| `MUnique.OpenMU.GameServer` | `OpenMU\src\GameServer\` | Game server on port 55901. Hosts protocol view plug-ins. |
| `MUnique.OpenMU.Web.AdminPanel` | `OpenMU\src\Web\AdminPanel\` | Blazor admin panel on port 80 (LAN only) — Martin uses this to toggle plug-ins, edit drop groups, etc. |
| `MUnique.OpenMU.Startup` | `OpenMU\src\Startup\` | Compose root that boots all servers in-process. Handles "apply pending migrations" prompt on console at startup. |

---

## 5. Server files we have modified (BarnaMu-specific changes)

| File | What changed |
|---|---|
| `DataModel\Entities\Account.cs` | Added enum value `AccountState.Vip`. Added `VipExpirationDate` (`DateTime?`, UTC) for VIP timer. |
| `DataModel\Entities\Character.cs` | (Historical) `CharacterStatus.Vip` enum value added in earlier sessions. |
| `GameLogic\Player.cs` | Exp rate 30x / VIP 35x; master exp 15x / VIP 20x; `PartyAutoMode` property. |
| `GameLogic\Party.cs` | Party exp + zen distribution with per-member VIP bonus, zen not dropped on floor in parties. |
| `GameLogic\DefaultDropGenerator.cs` | VIP drop bonus +40% applied **per-group chance** (not just total). |
| `GameLogic\AttackableNpcBase.cs` | Zen VIP +30% individual, party-distributed zen, exp normalization. |
| `GameLogic\PartyAutoMode.cs` | **NEW** — enum `Normal / AutoAccept / AutoDecline`. |
| `GameLogic\PlayerActions\Party\PartyRequestAction.cs` | Auto-respond to party invites based on `PartyAutoMode`. |
| `GameLogic\PlayerActions\LoginAction.cs` | At login: if VIP expired offline → revert State to Normal before applying perks. Also keeps the original "vault extended for VIP/GM" block. |
| `GameLogic\PlayerActions\WarpAction.cs` | VIP map-entry level table (M menu). Fixed map-number bug (Kanturu Ruins = 37, Vulcanus = 63). Includes Land of Trials = 250 VIP. |
| `GameLogic\PlayerActions\WarpGateAction.cs` | Same VIP map-entry table for in-map portals. Kept in sync with WarpAction.cs. |
| `GameLogic\PlayerActions\Chat\ChatMessageNormalProcessor.cs` | `[VIP]` / `[GM]` tag moved from sender-name into message body (sender name now matches the character so chat bubble renders). |
| `GameLogic\PlayerActions\Items\ItemCraftAction.cs` | The bare `catch` around `DoMixAsync` now logs the exception (was hiding crafting errors silently). |
| `GameLogic\PlugIns\PartyAutoCommandPlugIn.cs` | **NEW** — `/re auto`, `/re off`, `/re`. |
| `GameLogic\PlugIns\VipExpirationCheckPlugIn.cs` | **NEW** — `IPeriodicTaskPlugIn`, runs every 1 min, demotes online VIPs whose timer ran out. |
| `GameLogic\PlugIns\ChatCommands\SetVipChatCommandPlugIn.cs` | **NEW** — `/setvip <pj> [days]` (GM, default 30). Updates account State + VipExpirationDate + IsVaultExtended, online or offline. |
| `GameLogic\PlugIns\ChatCommands\VipInfoChatCommandPlugIn.cs` | **NEW** — `/vipinfo` (player). Shows days/hours left. |
| `GameLogic\PlugIns\ChatCommands\PKClearChatCommandPlugIn.cs` | Rewritten — `/pkclear` only clears self, charges **100,000 zen × PlayerKillCount**. |
| `GameLogic\PlugIns\ChatCommands\OfflineLevelingChatCommandPlugIn.cs` | `/offlevel` restricted to VIP / GM. |
| `GameLogic\PlugIns\ChatCommands\StartGoldenInvasionChatCommandPlugIn.cs` | **NEW** — `/goldenstart` (GM) forces Golden Invasion via `ForceStart()`. |
| `GameLogic\PlugIns\ChatCommands\StartRedDragonInvasionChatCommandPlugIn.cs` | **NEW** — `/reddragonstart` (GM). |
| `GameLogic\PlugIns\ChatCommands\StartT9BossInvasionChatCommandPlugIn.cs` | **NEW** — `/t9start` (GM) for Selupan / Erohim / Dark Elf. |
| `GameLogic\PlugIns\InvasionEvents\T9BossInvasionPlugIn.cs` | **NEW** — periodic 8h invasion spawning Selupan / Erohim / Dark Elf on their maps with golden announcement. |
| `GameServer\RemoteView\Character\ShowCharacterListPlugIn.cs` (and 075/095/Extended variants) | Tag `[GM]` in the character list (NOT `[VIP]` — that broke login due to name truncation). |
| `GameServer\Networking\ClientListener.cs` | `MaxConnectionsPerIp = 2` (Connect Server only — does not enforce in-game; see Gotchas). |
| `GameLogic\Actions\Items\SimpleItemCraftingHandler.cs` | +10% Chaos Machine success rate for VIP / GM. |
| `Persistence\EntityFramework\Migrations\20260515120000_AddAccountVipExpirationDate.cs` | **NEW** migration — adds `VipExpirationDate` column to `data."Account"`. |
| `Persistence\EntityFramework\Migrations\EntityDataContextModelSnapshot.cs` | Property `VipExpirationDate` added to the Account block (kept in sync with the model). |
| `Directory.Build.props` (`OpenMU\src\`) | `<NoWarn>$(NoWarn);NU1902;NU1903</NoWarn>` — suppress NuGet vulnerability warnings on Dapr/OpenTelemetry transitive deps that were failing the build. |

---

## 6. PostgreSQL schema we touch most

### `data."Account"` (key columns)

| Column | Type | Note |
|---|---|---|
| `Id` | uuid | PK |
| `LoginName` | varchar(10) UNIQUE | Login name |
| `PasswordHash` | text | **BCrypt** (BCrypt.Net-Next 4.0.3). Web registration must use the same library. |
| `SecurityCode` | text | Numeric, used for character deletion confirmation |
| `EMail` | text | Required (empty string accepted) |
| `State` | int | enum AccountState — 0 Normal, 1 Vip, 2 Spectator, 3 GameMaster, 4 GameMasterInvisible, 5 Banned, 6 TemporarilyBanned |
| `VipExpirationDate` | timestamptz NULL | BarnaMu addition — when VIP runs out |
| `IsVaultExtended` | bool | Set to true at login for VIP / GM |
| `IsTemplate` | bool | Read-only template accounts (excluded from web stats) |
| `RegistrationDate` | timestamptz | UTC |
| `LanguageIsoCode` | varchar(3) default 'en' | |
| `ChatBanUntil` | timestamptz NULL | |
| `VaultPassword` | text required | Empty string by default |

### `data."Character"`

| Column | Use |
|---|---|
| `Id` (uuid) | PK |
| `AccountId` (uuid) | FK to Account |
| `Name` (varchar(10)) | Char name |
| `Experience` (bigint) | XP, used for level ranking |
| `MasterExperience` (bigint) | Master XP |
| `PlayerKillCount` (int) | PK count, drives `/pkclear` cost |
| `State` (int) | HeroState — 0 Normal, 1 PK, 2 Murderer |
| `CharacterClassId` (uuid) | FK to `config."CharacterClass"` |
| `CharacterStatus` (int) | enum (Normal / Banned / GM / GMInvisible / Vip) |

### `data."StatAttribute"` (where Level / Resets / etc. actually live)

| Column | Note |
|---|---|
| `Id` (uuid) | PK |
| `CharacterId` (uuid NULL) | Owner — character-scoped |
| `AccountId` (uuid NULL) | Owner — account-scoped (rare) |
| `DefinitionId` (uuid) | FK to `config."AttributeDefinition"`. Stable Guids: Level = `560931AD-0901-4342-B7F4-FD2E2FCC0563`, MasterLevel = `70CD8C10-391A-4C51-9AA4-A854600E3A9F`, Resets = `89A891A7-F9F9-4AB5-AF36-12056E53A5F7`. |
| `Value` (real) | The stat value as float |

### `config."DropItemGroup"` (mob-side drops)

| Column | Note |
|---|---|
| `Chance` (double) | 0.0–1.0 |
| `Description` (text) | Search key (BarnaMu rows use `'BarnaMu Tier %'`) |
| `ItemLevel` (smallint NULL) | The +level of the dropped item |
| `ItemType` (int) | enum **SpecialItemType** — 0 None, **1 Ancient**, 2 Excellent, **3 RandomItem**, 4 SocketItem, 5 Money, 6 Jewel |
| `MinimumMonsterLevel` / `MaximumMonsterLevel` (smallint NULL) | Level gating |
| `MonsterId` (uuid NULL) | Specific monster filter (rarely used; the `MonsterDefinitionDropItemGroup` join table is the real source for golden-style assignments) |
| `GameConfigurationId` (uuid NULL) | The current GameConfiguration |

Linked items: `config."DropItemGroupItemDefinition"` (DropItemGroupId, ItemDefinitionId) — the PossibleItems collection.
Global attachment to maps: `config."GameMapDefinitionDropItemGroup"`.

### `config."ItemDropItemGroup"` (when a player drops/opens a box)

Same shape as DropItemGroup plus:

| Column | Note |
|---|---|
| `ItemDefinitionId` (uuid NULL) | FK to the **box** ItemDefinition (e.g. Box of Luck group=14 number=11). |
| `SourceItemLevel` (byte) | Which level of the box this rule fires for (Box of Luck +1 = Star, +2 = Firecracker, etc.) |
| `MinimumLevel` / `MaximumLevel` (byte) | The +level range of the produced item |
| `DropEffect` (int) | 0 None, 1 Fireworks, 2 ChristmasFireworks, 3 FanfareSound, 4 Swirl |
| `MoneyAmount` (int) | When ItemType=Money |
| `RequiredCharacterLevel` (short) | Locks the whole result if player level is below this |

Linked items: `config."ItemDropItemGroupItemDefinition"`.

### `config."WarpInfo"` (M-menu warps)

| Column | Note |
|---|---|
| `Name` (text) | Canonical name, e.g. `Kanturu_1`, `Karutan_1`, `Vulcanus`, `Crywolf`, `Swamp` |
| `LevelRequirement` (int) | Normal-player gate. VIP override lives in `WarpAction.cs` (hardcoded). |
| `Costs` (int) | Zen cost |
| `GateId` (uuid) | Target gate |

### `config."EnterGate"` (in-map portal gates)

| Column | Note |
|---|---|
| `LevelRequirement` (int) | Same idea as WarpInfo but for walking through a portal |
| `TargetGateId` (uuid) | Where you end up |

### `guild."Guild"` / `guild."GuildMember"`

Used by the rankings page. `Guild.Score` defaults 0 — populated by Castle Siege (not configured yet).

### `web."BugReport"` (created on first BarnaMuWeb startup)

Columns: `Id` bigserial, `Reporter` text, `Email` text NULL, `Title` text, `Body` text, `CreatedAt` timestamptz, `Ip` text NULL, `UserAgent` text NULL, `Status` text default `'new'`.

---

## 7. Map number reference (verified against OpenMU source)

| Map | # | Map | # | Map | # |
|---|---:|---|---:|---|---:|
| Lorencia | 0 | Devil Square | 9 | LandOfTrials | 31 |
| Dungeon | 1 | Blood Castle 1 | 11 | Aida | 32 |
| Devias | 2 | Chaos Castle 1 | 18 | Crywolf Event | 33 |
| Noria | 3 | Atlans | 7 | Crywolf Fortress | 34 |
| Lost Tower | 4 | Tarkan | 8 | Kanturu Ruins | **37** |
| Stadium / Arena | 6 | Icarus | 10 | Kanturu Relics | **38** |
| Elveland (Elbeland) | 51 | Karutan 1 | 80 | Kanturu Event | 39 |
| Swamp of Calmness | 56 | Karutan 2 | 81 | Balgass Barracks | 41 |
| Raklion | 57 | Vulcanus | **63** | Balgass Refuge | 42 |
| Raklion Boss | 58 | | | | |

**Trap:** in the older `WarpAction.cs`, map 37 was labelled `// Vulcanus` and the real Vulcanus (63) was missing. Fixed in this session.

---

## 8. Rates table

| | Normal | VIP |
|---|---|---|
| Experience | 30x | 35x |
| Master Experience | 15x | 20x |
| Zen | 10x | 13x |
| Drop | 5x | 7x |
| Drop excelente | ~3x | ~4x |

Mirrored in `BarnaMuWeb/appsettings.json` → `BarnaMu.Rates`.

---

## 9. VIP system

- Tag **[VIP]** in chat (NOT in character list — client truncates the 10-char name and breaks login). Tag is now prepended to the message body, not the sender name, so the chat bubble matches the character on screen.
- Tag **[GM]** in character list and chat.
- Auto-extended vault on login.
- `/offlevel` exclusive.
- +10% Chaos Machine.
- Early map access (see section 11).
- Party-zen + party-exp individual VIP bonus.
- **VIP timer (BarnaMu addition):**
  - `Account.VipExpirationDate` (UTC).
  - GM assignment: `/setvip <character> [days]` (default 30).
  - Online expiry: `VipExpirationCheckPlugIn` runs every 1 min, demotes in place.
  - Offline expiry: caught in `LoginAction.FinishLoginAsync` before perks are applied.
  - Player query: `/vipinfo`.

In-code VIP check pattern:
```csharp
player.Account?.State == AccountState.Vip
|| player.Account?.State == AccountState.GameMaster
|| player.Account?.State == AccountState.GameMasterInvisible
```

---

## 10. Reset system (per `ResetConfiguration.cs` defaults — adjust in Admin Panel if customized)

| Setting | Default | Note |
|---|---|---|
| RequiredLevel | 400 | |
| LevelAfterReset | 10 | |
| RequiredMoney | 1 × reset count | (Almost free unless overridden) |
| PointsPerReset | 1500 × reset count | Legacy field; tier list takes precedence if populated |
| ResetStats | true | |
| MultiplyRequiredMoneyByResetCount | true | |
| MultiplyPointsByResetCount | true | |
| ReplacePointsPerReset | true | |
| MoveHome | true | |
| LogOut | true | |
| ResetLimit | null (unlimited) | |

Commands: `/reset`, `/resetinfo`, `/getresets`.

---

## 11. Map entry levels (current — 2026-05-17)

| Map | # | Normal | VIP |
|---|---:|---:|---:|
| Lorencia / Noria / Elbeland | 0/3/51 | 10 | — |
| Devias | 2 | 20 | — |
| Dungeon | 1 | 50 | — |
| Atlans | 7 | 70 | — |
| Lost Tower | 4 | 100 | — |
| Tarkan | 8 | 140 | — |
| Icarus | 10 | 170 | — |
| Aida | 32 | 150 | — |
| Kanturu Ruins | 37 | 160 | 130 |
| Karutan 1 / 2 | 80 / 81 | 200 | 160 |
| Kanturu Relics | 38 | 240 | 200 |
| Land of Trials | 31 | 280 | 250 |
| Raklion (La Cleon) | 57 | 280 | 240 |
| Vulcanus | 63 | 300 | 260 |
| Crywolf Fortress | 34 | 320 | 280 |
| Balgass Barracks | 41 | 350 | 300 |
| Balgass Refuge | 42 | 350 | 300 |
| Swamp of Calmness | 56 | 350 | 300 |

VIP values: hardcoded in `WarpAction.cs` and `WarpGateAction.cs` (must keep in sync).
Normal values: in DB (`WarpInfo.LevelRequirement` for M menu, `EnterGate.LevelRequirement` for portals). Mirror table in `BarnaMuWeb/appsettings.json` → `BarnaMu.Maps`.

---

## 12. Tier items A-F (BarnaMu drop system)

Mobs drop the **Box of Luck** ItemDefinition (group 14, number 11) at increasing +level depending on mob level. Each tier is a separate `DropItemGroup` with `Description LIKE 'BarnaMu Tier %'`.

| Tier | Box level | Drop name in client | Mob level min | Drop % per kill |
|---|:-:|---|---:|---:|
| A | +1 | Star of Sacred Birth | 1 | 0.5% |
| B | +2 | Firecracker | 30 | 0.5% |
| C | +3 | Heart of Love | 60 | 0.5% |
| D | +4 | Olive of Love | 90 | 0.5% |
| E | +5 | Silver Medal | 120 | 0.5% |
| F | +6 | Gold Medal | 150 | 0.5% |

Drop side configured by `BARNAMU_TIER_ITEMS_DROPS.sql`. Consume rewards (what the box gives when used) currently follow vanilla OpenMU recipe per level — Martin handled per-tier reward tuning manually.

---

## 13. Goldens / Boss invasions

Three periodic plug-ins, all extending `BaseInvasionPlugIn`:

| Plug-in | Cadence | Duration | Notes |
|---|---|---|---|
| `GoldenInvasionPlugIn` | every 4 hours (00/04/08/12/16/20 UTC) | 5 min | 9 Golden mobs split across Lorencia/Noria/Devias/Atlans/Tarkan. Each Golden drops its specific Box of Kundun (1:1 mapping in `MonsterDefinitionDropItemGroup`). |
| `RedDragonInvasionPlugIn` | every 6 hours (02/08/14/20) | 10 min | Red Dragon (×3) drops Blue Chocolate Box → ancients. 20M HP, 0.75–0.9 resistances. |
| `T9BossInvasionPlugIn` | every 8 hours | 30 min | Selupan (Raklion Boss 58), Erohim (LandOfTrials 31), Dark Elf (Balgass Refuge 42). Drops Pink Chocolate Box (T9 excellent, drop level 133–147). 25M HP, 0.8–0.95 resistances. |

GM commands: `/goldenstart`, `/reddragonstart`, `/t9start` — call `ForceStart()` on the corresponding active plug-in (looked up via `PlugInManager.GetActivePlugInsOf<IPeriodicTaskPlugIn>().OfType<T>().FirstOrDefault()`).

---

## 14. Player commands

```
/addstr N · /addagi N · /addvit N · /addene N · /addcmd N   ← stat points
/reset · /resetinfo · /getresets                            ← resets
/post                                                       ← global message
/pkclear                                                    ← clear own PK (cost 100k × PK count)
/ware · /clearinv                                           ← vault · clear inventory
/move <map>                                                 ← teleport (cost zen)
/offlevel                                                   ← VIP only
/re auto · /re off · /re                                    ← party auto-respond mode
/vipinfo                                                    ← VIP days/hours left
```

## 15. GM commands

```
/item group:X number:Y lvl:15 ex:63 sk:1 lu:1 opt:4 anc:0
/set str/agi/vit/ene/cmd [value] [character]
/setlevel · /setmoney · /getmoney · /setresets · /getstat · /getlevel · /getmasterlevel · /setmasterlevel
/ban · /banacc · /unban · /unacc · /chatban · /dc · /hide · /unhide · /notice
/online · /trace · /track · /move <char> <map>
/startbc · /startds · /startcc
/setvip <char> [days]      ← BarnaMu (default 30 days)
/goldenstart               ← BarnaMu: force Golden invasion now
/reddragonstart            ← BarnaMu: force Red Dragon now
/t9start                   ← BarnaMu: force T9 boss invasion now
```

Admin Panel (LAN-only, port 80): Accounts → State `Vip` / `GameMaster`; Drop group editing (use SQL for the goldens, the join table is hidden in the UI); PlugIns → toggle activation.

---

## 16. Web — BarnaMuWeb

Standalone **ASP.NET Core Razor Pages**, .NET 10. Connects to Postgres directly via **Npgsql + Dapper**. Account creation uses **BCrypt.Net-Next 4.0.3** (same as OpenMU) so accounts created from the web log in to the game with no extra step. Does NOT reference any OpenMU assembly.

### Files

| File | Purpose |
|---|---|
| `BarnaMuWeb.csproj` | net10.0 Razor Pages SDK. Three NuGet deps: `Npgsql`, `Dapper`, `BCrypt.Net-Next`. |
| `Program.cs` | Composition root. Configures `BarnaMuOptions`, `BarnaMuDb`, `ServerStatusService`, rate limiting (5/h registrations, 10/h bug reports), Razor Pages. Calls `BarnaMuDb.EnsureWebSchemaAsync()` at startup. |
| `appsettings.json` | Server config: connection string, ServerName, DiscordInviteUrl, ClientDownloadUrl, ClientVersion, ClientChecksum, ConnectHost/Port, GameServerHost/Ports, **Rates** (mirrors game), **Vip** (price text, PayPal, WhatsApp, instructions), **Maps[]** (mirrors section 11). |
| `Data\BarnaMuOptions.cs` | Strongly-typed config class with nested `RatesOptions`, `VipOptions`, `MapEntry`. |
| `Data\BarnaMuDb.cs` | All DB access: account exists/create, total counts, rankings (by resets / level / guild — uses the stable Guid attribute IDs), bug report insert, `EnsureWebSchemaAsync` (creates `web."BugReport"`). |
| `Services\ServerStatusService.cs` | TCP probes Connect + Game ports with 15s cache, 800ms timeout. |
| `Pages\Index.cshtml(.cs)` | Home — hero, news (from `Content\news.json`), stats, "why play here" feature list. |
| `Pages\Register.cshtml(.cs)` | Account creation form. BCrypt hash + insert into `data."Account"`. Validates name 3–10 alphanumeric, password 6–20, security code 4–10 digits, email optional. Rate-limited. |
| `Pages\Vip.cshtml(.cs)` | VIP info — rates, perks, map access table (driven by `Maps[]`), price + PayPal + WhatsApp instructions. |
| `Pages\Guide.cshtml(.cs)` | Reads `Content\guide.json` (editable without recompile) and renders each section's `bodyHtml` raw. Sections: clases, items-tier, mapas, alas-n3, sockets, quests, comandos. |
| `Pages\Rankings.cshtml(.cs)` | Tabs: Resets / Level / Guilds. Top 50 each. |
| `Pages\Status.cshtml(.cs)` | Live service list + account/character counters. |
| `Pages\Download.cshtml(.cs)` | Client link + checksum (optional) + "how to connect" instructions. |
| `Pages\ReportBug.cshtml(.cs)` | Form → `web."BugReport"`. |
| `Pages\Shared\_Layout.cshtml` | Webzen-style template, BarnaMu skin (navbar, sidebar with Accesos Rápidos / Server info / clocks). Loads bootstrap.min, style.css, dmncms/barnamed.css, FontAwesome, Google Fonts (Cinzel + Exo2). |
| `Content\news.json` | Editable list of news items (date / title / body). |
| `Content\guide.json` | Editable list of guide sections (id / title / bodyHtml). |
| `wwwroot\` | Static CSS / images / JS. |
| `Properties\launchSettings.json` | dev URL `http://localhost:5050`. |
| `README.md` | Deploy + config walkthrough. |

### Database touch summary

- **Read:** `data."Account"`, `data."Character"`, `data."StatAttribute"`, `config."AttributeDefinition"`, `guild."Guild"`, `guild."GuildMember"`.
- **Write:** `data."Account"` (registration) and `web."BugReport"`.

### Deploying the web (port 8081 plan)

1. `dotnet publish -c Release -o C:\MuDev\BarnaMuWeb\publish`
2. Windows Firewall: open inbound TCP 8081.
3. Router: forward 8081 → 192.168.1.52:8081.
4. `WebStart.bat` (not yet created) loops `BarnaMuWeb.exe` with `ASPNETCORE_URLS=http://*:8081`.
5. Task Scheduler `BarnaMu-Web` to autostart.
6. When the Admin Panel is moved to a LAN-only port, repoint the web to 80/443.

---

## 17. Client — BarnaMu-Client

Season 6 Episode 3 official-style client living in `C:\MuDev\BarnaMu-Client\` (excluded from git via `.gitignore`).

- **`main.exe` patched** by Martin to connect directly to `barnamu.ddns.net:44405` (no launcher / no manual server list).
- Players can run the launcher or `main.exe` directly.
- Must be run as Administrator at least once.
- MU Helper enabled (lvl 80 client restriction stands — not configurable server-side).
- **Known client-side limitations** that need a patched main.exe (deferred until server is stable):
  - Right-click equip/unequip — classic client doesn't send an equip packet for right-click.
  - Auto helper pauses on inventory open — built into MU Helper.
  - Mob HP bars not rendered.
  - Item compare tooltip not supported.
- **Known client-side mismatches** the server has to absorb:
  - Seed Master extraction window shows 5 item slots, but OpenMU's recipe only checks 4. Players just leave one slot empty.

---

## 18. Bat scripts (in `C:\MuDev\`)

| Script | What it does |
|---|---|
| `Recompilar.bat` | `cd C:\MuDev\OpenMU\src && dotnet build MUnique.OpenMU.sln --configuration Release`. Stops on error. |
| `AutoRestart.bat` | Loop: `dotnet run -c Release` from the Startup project, restart in 10s on crash. |
| `StartServer.bat` | Wrapper that launches `AutoRestart.bat`. |
| `Backup.bat` | `pg_dump openmu → barnamu_daily.sql`. On Sundays also copies to `barnamu_weekly.sql`. Cleans legacy timestamped backups. No auto-push (Push.bat invokes it). |
| `Push.bat "msg"` | Anchors to `C:\MuDev`, calls Backup.bat, `git add -A`, commits with the message, `git push`. Designed so committing always includes a fresh backup. |

---

## 19. Session log (compact)

Append to the top of this list when a session does real work.

- **2026-05-17** — Map levels overhauled (new table), `WarpAction.cs` / `WarpGateAction.cs` rewritten with correct map numbers (Kanturu Ruins = 37, Vulcanus = 63, Land of Trials = 31 added). 3 GM invasion commands (`/goldenstart`, `/reddragonstart`, `/t9start`). `BARNAMU_MAP_LEVELS.sql` for Normal levels. Build now allows NuGet vuln warnings via `<NoWarn>NU1902;NU1903</NoWarn>` in Directory.Build.props. Push.bat reordered so cd-to-repo happens before Backup.bat invocation. Web reorganized by Martin (DMNCMS / Webzen template).
- **2026-05-16** — `/pkclear` rework (self-only, 100k × PK), chat bubble VIP fix (tag in message body), tier items A-F drop side (BARNAMU_TIER_ITEMS_DROPS.sql, with ItemType=3 fix), Flame of Condor DropsFromMonsters set true (BARNAMU_FIX_FLAME_OF_CONDOR.sql), Backup.bat rotation + Push.bat helper, ItemCraftAction silent-catch logging fix.
- **2026-05-15** — VIP timer feature (Account.VipExpirationDate column + migration, `/setvip`, `/vipinfo`, periodic check plug-in, login expiry check). Web v1 scaffolded (Register, Status, Rankings, Vip, Guide, Download, ReportBug).
- **2026-05-14** — Cajas Kundun / Heaven (6 Box of Luck variants), T9 Box (Pink Chocolate Box), Red Dragon ancients box, Goldens hardened (HP / resistances), invasions (Golden 4h, Red Dragon 6h, T9 8h), spawn counts trimmed.
- **2026-05-09** — Drop balance pass. `NumberOfMaximumItemDrops` 3 → 2. Random item chance 1.0 → 0.5, Jewel 0.125 → 0.03. Event tickets 0.01 → 0.001. Arena spots filled. Bloody Golem dead-drop bug fixed (empty PossibleItems on a Chance=1 group via `MonsterDefinitionDropItemGroup`). VIP drop multiplier moved to per-group chance in `DefaultDropGenerator.SelectRandomGroup`.

Older detail lives in:
- `C:\MuDev\BARNAMU_SESION_CAJAS_DROPS.md`
- `C:\MuDev\BARNAMU_SESION_VIP_WEB.md`
- `C:\MuDev\BARNAMU_SESION_TIER_ITEMS.md`

---

## 20. Critical gotchas (DO NOT FORGET)

1. **`SpecialItemType` enum** (`DataModel\Configuration\DropItemGroup.cs`):
   `0 None · 1 Ancient · 2 Excellent · 3 RandomItem · 4 SocketItem · 5 Money · 6 Jewel`.
   For "drop this specific item" use **3 (RandomItem)** with PossibleItems = the one item. Using 1 by mistake → ancients dropping everywhere (we've burned an evening on this).

2. **Persistence SourceGenerator `--no-build`.** Both `Persistence` and `Persistence.EntityFramework` `.csproj` have a PreBuild step that does `dotnet run -p SourceGenerator --no-build`. If the SourceGenerator's `bin\` was wiped, the whole compile errors out with `MSB3073 exited with code 1`. **Fix once:** `dotnet build OpenMU\src\Persistence\SourceGenerator\MUnique.OpenMU.Persistence.SourceGenerator.csproj -c Release` then rerun `Recompilar.bat`.

3. **NuGet vulnerability warnings (NU1902 / NU1903)** on Dapr / OpenTelemetry transitive deps were escalated to build failures by the NuGet audit DB. Suppressed in `Directory.Build.props` via `<NoWarn>$(NoWarn);NU1902;NU1903</NoWarn>`. Re-evaluate when Dapr packages bump their deps.

4. **`OpenMU.GameLogic` has a global usings file** at `OpenMU\src\GameLogic\GlobalUsings.cs` that imports `MUnique.OpenMU.DataModel.Entities` and `System.ComponentModel.DataAnnotations`. Plus `SharedGlobalUsings.cs` at `OpenMU\src\` imports `System`, `System.Collections.Generic`, `System.Linq`, `System.Text`, `System.Threading.Tasks`. So `Player`, `Account`, `Character`, `CharacterStatus`, `AccountState`, `DateTime`, `Display`, etc. resolve with no explicit using.

5. **Migrations land in two places.** The `Migrations\YYYYMMDDHHMMSS_Name.cs` file gets `[DbContext(typeof(EntityDataContext))]` + `[Migration("...")]` inline (Martin's pattern — no `.Designer.cs`). The `EntityDataContextModelSnapshot.cs` must also receive the new property under the Account block (or whatever entity). Both files were touched for `VipExpirationDate`.

6. **`PlugInManager.GetPlugIn<T>` does not exist.** To grab a specific concrete plug-in:
   ```csharp
   gameContext.PlugInManager
       .GetActivePlugInsOf<IPeriodicTaskPlugIn>()
       .OfType<GoldenInvasionPlugIn>()
       .FirstOrDefault();
   ```
   The plug-in manager is indexed by plug-in **point** interface, not concrete class.

7. **`MonsterDefinitionDropItemGroup` is the source of truth** for which DropItemGroups a mob actually has. The `DropItemGroup.MonsterId` column is just a redundant filter. The Admin Panel only shows one of them — always query both.

8. **MU client 10-char name limit.** Putting `[VIP]` in the character list breaks login because the name gets truncated. Keep tags in chat messages, not in names.

9. **`ItemCraftAction.cs` had a bare `catch` that swallowed every crafting exception silently.** Now it logs. If a crafting "does nothing," look for `Exception while mixing items at NPC ...` in the server console.

10. **`IP limit = 2` is enforced at the Connect Server only**, during login. Once a player crosses over to the Game Server, the Connect-Server connection drops and the slot frees up. To enforce "2 active accounts per IP in-game" the limit must also be checked on the Game Server side — not done yet.

11. **The MU client's NPC dialogs are mostly hardcoded** — the server can't change the Gens Steward "Yes, I wish to join" text or the Seed Master 5-slot UI. When client and server disagree on a recipe, fix the server to match the client (or accept the dead slot, like with Seed Master).

12. **`Backup.bat` and `Push.bat` use absolute paths**, but `Push.bat` must `cd /d C:\MuDev` BEFORE calling `Backup.bat` (because the `call` itself is relative). This bug was fixed but is easy to reintroduce.

---

## 21. New tasks (Martin-editable)

Add anything below. Format: free-form. Section gets read by the next AI on chat start.

```
TASKS:

- (empty — add new requests here)


IDEAS / NICE-TO-HAVE:

- Right-click equip via main.exe patch (client-side)
- Mob HP bar via main.exe patch (client-side)
- Item compare tooltip via main.exe patch (client-side)
- Auto Helper pause-on-inventory tweak (client-side, low priority)
- Gens system full implementation (multi-session work)
- Resets ranking on website
- Castle Siege config
- HTTPS for the web (Cloudflare proxy or win-acme)
- /resetinfo polish similar to /vipinfo
- Vulcanus M-menu shown red — assumed client-side, confirm

KNOWN ISSUES:

- (empty — add new issues here)
```

---

## 22. Maintenance conventions

- At the end of every working session, prepend a one-line entry to section 19.
- Bump the "Last updated" date at the top.
- If a section grows stale (e.g. a fix was reverted), edit the section, don't leave both versions.
- If you split a session's detail into a separate `BARNAMU_SESION_*.md`, reference it in section 19.
- Keep section 21 brief — link out to issue-specific notes if a task has long context.

---

End of file.

# BarnaMu Server ChangeLog

## Objective

Build BarnaMu as a stable public MU Online Season 6 Episode 3 server on top of OpenMU, with slow-medium progression, VIP perks, balanced drops, automated invasions, useful GM/player commands, and a public web ecosystem for registration, rankings, guides, status, downloads, and bug reports.

## Result

The OpenMU server now includes BarnaMu-specific VIP systems, progression rates, party rewards, map access rules, invasion events, drop balancing, account utilities, web integration, and maintenance scripts. The project is backed by PostgreSQL, automated backups, GitHub push helpers, and a standalone ASP.NET Core public website.

## Core Server Changes

### VIP System

- Added `AccountState.Vip`.
- Added `Account.VipExpirationDate` for timed VIP expiration.
- Added VIP expiration migration and updated the EF model snapshot.
- Added `/setvip <character> [days]` GM command.
- Added `/vipinfo` player command.
- Added periodic online VIP expiration check.
- Added login-time offline VIP expiration check.
- Extended vault automatically for VIP and GM accounts.
- Added VIP-only `/offlevel` access.
- Added VIP/GM Chaos Machine success bonus.
- Added VIP-specific map level reductions.
- Added VIP party experience and zen bonus handling.
- Added VIP drop and zen bonuses.
- Moved `[VIP]` chat tag into the message body so character names remain client-safe.

### Rates

- Normal experience: `30x`.
- VIP experience: `35x`.
- Normal master experience: `15x`.
- VIP master experience: `20x`.
- Normal zen: `10x`.
- VIP zen: `13x`.
- Normal drop: `5x`.
- VIP drop: `7x`.
- Common excellent drop: `0.0001` normal, `0.00014` VIP/GM effective.
- Common jewel drop: `0.0005` normal, `0.0007` VIP/GM effective.

### Party System

- Added `PartyAutoMode`.
- Added `/re auto`, `/re off`, and `/re` party invitation response commands.
- Added automatic party invite accept/decline behavior.
- Reworked party experience distribution with per-member VIP bonuses.
- Reworked party zen distribution so zen is distributed directly instead of dropped on the floor.

### Drops And Rewards

- Added VIP drop chance bonus per drop group.
- Added VIP zen bonus per player.
- Rebalanced global item drops.
- Reduced maximum simultaneous item drops.
- Reduced random item, jewel, and event ticket drop rates.
- Fixed Bloody Golem dead-drop issue caused by an empty guaranteed drop group.
- Added Tier A-F drop system using Box of Luck variants.
- Added monster-level-gated tier box drops.
- Reduced Tier A-F box drop chances:
  - Tier A: `0.0002`
  - Tier B: `0.00018`
  - Tier C: `0.00015`
  - Tier D: `0.00012`
  - Tier E: `0.0001`
  - Tier F: `0.00008`
- Reduced common jewel drop chance to `0.0005`.
- Reduced Jewel of Guardian drop chance to `0.00025`.
- Reduced common excellent item drop chance to `0.0001`.
- Reduced second excellent option chance to `15%`.
- Reduced random Luck option chance to `10%`.
- Reduced normal random option chance to `12%`.
- Reduced random skill chance on normal items to `25%`.
- Capped normal dropped item option level with `MaximumItemOptionLevelDrop = 1`.
- Reworked normal dropped item level generation from guaranteed monster-level scaling to weighted `25%` per item level.
- Kept excellent item skill behavior: excellent items that can have skill still always get skill.
- Fixed `SpecialItemType` usage for specific-item drops.
- Added Flame of Condor monster drop support.
- Added Box of Kundun / Heaven reward work.
- Added T9 Box reward work.
- Added Red Dragon ancient reward box.

### Invasions

- Added periodic Golden Invasion.
- Added periodic Red Dragon Invasion.
- Added periodic T9 Boss Invasion.
- Added Selupan, Erohim, and Dark Elf boss invasion rotation.
- Added GM commands:
  - `/goldenstart`
  - `/reddragonstart`
  - `/t9start`
- Hardened invasion bosses with higher HP and resistances.
- Reduced invasion spawn counts for balance.

### Map Access

- Reworked M-menu warp requirements.
- Reworked in-map portal gate requirements.
- Added VIP map-entry level overrides.
- Fixed map number mismatch:
  - Kanturu Ruins = `37`
  - Kanturu Relics = `38`
  - Vulcanus = `63`
  - Land of Trials = `31`
- Added Land of Trials VIP access requirement.
- Kept normal map requirements in database configuration.
- Kept VIP map requirements hardcoded in `WarpAction.cs` and `WarpGateAction.cs`.

### Player Commands

- Reworked `/pkclear`:
  - Self-only.
  - Cost is `100,000 zen * PlayerKillCount`.
- Added `/vipinfo`.
- Added `/re auto`, `/re off`, and `/re`.
- Restricted `/offlevel` to VIP and GM accounts.
- Existing supported command set includes stat adds, reset commands, post, warehouse, clear inventory, and move.

### GM Commands

- Added `/setvip <character> [days]`.
- Added `/goldenstart`.
- Added `/reddragonstart`.
- Added `/t9start`.
- Existing GM command set includes item creation, stat edits, level/money/reset edits, bans, disconnect, hide, notice, trace, tracking, movement, and event starts.

### Character List And Chat

- Added `[GM]` tag to character list views.
- Kept `[VIP]` out of character list to avoid client name truncation.
- Added `[VIP]` and `[GM]` chat tags in message body.
- Fixed chat bubble rendering by keeping the sender name equal to the actual character name.

### Crafting

- Added VIP/GM Chaos Machine success bonus.
- Added exception logging around item crafting mix failures.
- Preserved compatibility with client-side Seed Master UI mismatch.

### Login And Account Handling

- Added VIP expiration handling during login.
- Preserved extended vault login behavior for VIP and GM.
- Added account registration support through the web project using the same BCrypt hashing as OpenMU.

### Cash Shop

- Added in-game cash shop client initialization on player world entry.
- Added Season 6 cash shop script version packet support for product list `512.2012.084`.
- Added Season 6 cash shop banner version packet support for banner list `583.2011.001`.
- Added `0xD2` cash shop packet group handling.
- Added handlers for cash shop open state, W Coin balance, storage list, buy, gift, delete, consume, and event-item list requests.
- Enabled the client X-key cash shop window to open instead of showing the reconnect error.
- Kept W Coin balances, purchases, gifts, and storage actions non-authoritative placeholders pending account currency and cash shop storage implementation.
- Planned future cash shop direction:
  - Keep the X store for W Coin sinks, services, convenience items, VIP time, expansions, reset utilities, event tickets, cosmetics, and marketplace-related tokens.
  - Avoid selling direct power items through the X store to preserve the slow-medium server economy.
  - Evaluate W Coin-based personal stores and/or a website marketplace for player-farmed item trading.
  - Prefer player-to-player item sales where W Coin transfers from buyer to seller, with an optional marketplace tax burned by the server.
  - Require W Coin balances, transaction logs, item escrow, and full audit logging before enabling real-money W Coin purchases or player-market sales.

### Network And Access

- Set Connect Server max connections per IP to `2`.
- Noted that this limit applies only at Connect Server login time, not active Game Server sessions.
- Planned future work: enforce active in-game IP limits at Game Server level.

### Build And Maintenance

- Added NuGet warning suppression for `NU1902` and `NU1903`.
- Documented Persistence SourceGenerator rebuild requirement.
- Added backup and push helper scripts.
- Fixed `Push.bat` path order so it changes to the repository before calling backup.
- Added daily and weekly PostgreSQL backup rotation.
- Added auto-restart server loop script.

## Public Web Project

### Objective

Provide a standalone BarnaMu public website without referencing OpenMU assemblies.

### Result

The web project can register accounts, show server status, display rankings, publish VIP information, host guides, expose download instructions, and collect bug reports.

### Changes

- Added ASP.NET Core Razor Pages project.
- Added Dapper + Npgsql database access.
- Added BCrypt account registration compatible with OpenMU.
- Added registration rate limiting.
- Added bug report rate limiting.
- Added `web."BugReport"` schema creation on startup.
- Added server status TCP probes with short cache.
- Added rankings by resets, level, and guilds.
- Added editable news and guide JSON content.
- Added VIP page with rates, perks, pricing text, and map access table.
- Added download page with client link, version, checksum, and connection notes.

## Database Changes

- Added `data."Account"."VipExpirationDate"`.
- Uses existing `data."Account"."State"` for VIP, GM, bans, and normal accounts.
- Uses `data."Character"."PlayerKillCount"` for `/pkclear` cost.
- Uses `data."StatAttribute"` for level, master level, and resets rankings.
- Uses `config."DropItemGroup"` and `config."ItemDropItemGroup"` for monster and box drops.
- Updated `config."DropItemGroup"` rates for Tier A-F boxes, common jewels, Jewel of Guardian, and common excellent drops.
- Updated `config."ItemOptionDefinition"` rates for Luck, normal random options, and second excellent options.
- Updated `config."GameConfiguration"."MaximumItemOptionLevelDrop"` to `1`.
- Uses `config."WarpInfo"` and `config."EnterGate"` for normal map entry requirements.
- Added `web."BugReport"` for website bug reports.

## Main Server Files

- `DataModel/Entities/Account.cs`
- `DataModel/Entities/Character.cs`
- `GameLogic/Player.cs`
- `GameLogic/Party.cs`
- `GameLogic/DefaultDropGenerator.cs`
- `GameLogic/AttackableNpcBase.cs`
- `GameLogic/PartyAutoMode.cs`
- `Persistence/Initialization/GameConfigurationInitializerBase.cs`
- `Persistence/Initialization/Updates/FixChaosMixesPlugInBase.cs`
- `Persistence/Initialization/Updates/FixItemOptionsAndAttackSpeedPlugInBase.cs`
- `Persistence/Initialization/Version075/Items/Jewelery.cs`
- `Persistence/Initialization/VersionSeasonSix/Items/Pets.cs`
- `Persistence/Initialization/VersionSeasonSix/Items/Wings.cs`
- `GameLogic/PlayerActions/LoginAction.cs`
- `GameLogic/PlayerActions/WarpAction.cs`
- `GameLogic/PlayerActions/WarpGateAction.cs`
- `GameLogic/PlayerActions/Party/PartyRequestAction.cs`
- `GameLogic/PlayerActions/Chat/ChatMessageNormalProcessor.cs`
- `GameLogic/PlayerActions/Items/ItemCraftAction.cs`
- `GameLogic/Actions/Items/SimpleItemCraftingHandler.cs`
- `GameLogic/PlugIns/PartyAutoCommandPlugIn.cs`
- `GameLogic/PlugIns/VipExpirationCheckPlugIn.cs`
- `GameLogic/PlugIns/ChatCommands/SetVipChatCommandPlugIn.cs`
- `GameLogic/PlugIns/ChatCommands/VipInfoChatCommandPlugIn.cs`
- `GameLogic/PlugIns/ChatCommands/PKClearChatCommandPlugIn.cs`
- `GameLogic/PlugIns/ChatCommands/OfflineLevelingChatCommandPlugIn.cs`
- `GameLogic/PlugIns/ChatCommands/StartGoldenInvasionChatCommandPlugIn.cs`
- `GameLogic/PlugIns/ChatCommands/StartRedDragonInvasionChatCommandPlugIn.cs`
- `GameLogic/PlugIns/ChatCommands/StartT9BossInvasionChatCommandPlugIn.cs`
- `GameLogic/PlugIns/InvasionEvents/T9BossInvasionPlugIn.cs`
- `GameServer/RemoteView/Character/ShowCharacterListPlugIn.cs`
- `GameServer/Networking/ClientListener.cs`
- `Persistence/EntityFramework/Migrations/20260515120000_AddAccountVipExpirationDate.cs`
- `Persistence/EntityFramework/Migrations/EntityDataContextModelSnapshot.cs`
- `Directory.Build.props`

## Main Web Files

- `BarnaMuWeb.csproj`
- `Program.cs`
- `appsettings.json`
- `Data/BarnaMuOptions.cs`
- `Data/BarnaMuDb.cs`
- `Services/ServerStatusService.cs`
- `Pages/Index.cshtml`
- `Pages/Register.cshtml`
- `Pages/Vip.cshtml`
- `Pages/Guide.cshtml`
- `Pages/Rankings.cshtml`
- `Pages/Status.cshtml`
- `Pages/Download.cshtml`
- `Pages/ReportBug.cshtml`
- `Pages/Shared/_Layout.cshtml`
- `Content/news.json`
- `Content/guide.json`

## Session Timeline

- `2026-05-22`: Slow-medium drop-quality pass: reduced Tier A-F box rates, common jewel and excellent rates, Jewel of Guardian rate, second excellent option chance, random Luck/option/skill chances, capped normal option level, and weighted normal item level generation.
- `2026-05-17`: Map level overhaul, VIP map access sync, invasion GM commands, NuGet warning suppression, backup/push script fix, web template reorganization.
- `2026-05-16`: `/pkclear` rewrite, VIP chat tag fix, Tier A-F drop side, Flame of Condor drop support, backup rotation, push helper, crafting exception logging.
- `2026-05-15`: VIP timer system, account migration, `/setvip`, `/vipinfo`, periodic expiration check, login expiration check, first public web scaffold.
- `2026-05-14`: Box reward work, T9 box, Red Dragon ancient box, Golden/Red Dragon/T9 invasions, invasion balance pass.
- `2026-05-09`: Drop balance pass, maximum item drop reduction, jewel/event ticket tuning, Arena spot fill, Bloody Golem drop bug fix, VIP per-group drop multiplier.

## Known Follow-Ups

- Enforce two active in-game accounts per IP at Game Server level.
- Configure Castle Siege.
- Add HTTPS for the web.
- Polish `/resetinfo`.
- Add resets ranking improvements.
- Investigate Vulcanus M-menu red display if still client-side.

## Future Plugin Backlog

- Castle Siege.
- Jewels Bank.
- Player WCoin Market / Web Market.
- Offline Personal Store.
- Web Market Browser.
- Guild Bank.
- Boss Token System.
- Drop Audit Logger.
- Event Reward Balancer.
- Item Lock / Protection.
- Jewel Pack / Unpack Commands.
- Party Finder.
- Guild War Rewards.
- PvP Arena / Duel Ladder.
- Reset / Master Reset Rewards.
- Daily / Weekly Quests.
- Anti-Bot / Farm Monitor.
- WCoin Admin Ledger.
- VIP Convenience Only.
- Web Event Calendar.
- Endgame Boss Lockout.
- Auction House.
- Custom Achievements.
- Crafting Safety Preview.

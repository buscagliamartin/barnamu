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

### Item Audit Logging

- Added `ItemAuditLogger`, a central file-based forensic logger for item creation and transfer events.
- Writes to `%BARNAMU_AUDIT_DIR%` (default `C:\MuDev\Logs\ItemAudit`), one log file per UTC day.
- Each entry records timestamp, source, actor (character/account), item description, group, number, level, excellent count, ancient flag, skill flag, socket count, persistent serial, location, and optional context.
- Added source-attributed logging hooks:
  - `MonsterDrop`: actor is the killer.
  - `GmCommand`: actor is the GM who used `/item`.
  - `BoxReward`: actor is the box opener.
  - `Crafting`: actor is the crafter, on Chaos Machine success.
  - `Pickup`: actor is the player who picked the item up.
  - `Trade`: actor is the receiver, sender recorded as context.
- High-volume sources (monster drops, pickups) log only notable items: excellent, ancient, socketed, or level `7` and above.
- Other sources log every item.
- Audit writes never throw, so logging cannot disrupt gameplay.
- Retired the Phase 1 `DropAuditLoggerPlugIn` map catch-all in favor of source-attributed logging.

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
- Rebalanced Golden Dragon, Golden Lizard King, Golden Wheel, and Golden Tantallos around slow-medium party combat.
- Rebalanced Golden Budge Dragon, Golden Goblin, Golden Soldier, Golden Titan, and Golden Vepar into logical low/mid Golden tiers with lower defense-rate walling and higher real attack threat.
- Rebalanced Red Dragon as an Ancient-box bridge boss between Golden Tantallos and T9 bosses:
  - Level `140`
  - HP `250,000,000`
  - Damage `8,000-11,000`
  - Defense `6,000`
  - Attack rate `17,000`
  - Defense rate `3,200`
  - Poison resistance `0.70`
  - Other elemental resistances `0.65`
- Fixed Season 6 Red Dragon initialization so the Season 6 Red Dragon definition is actually used instead of the inherited 0.95d default.
- Synchronized Red Dragon source reward target to `Blue Chocolate Box - Ancient Sets`.
- Rebalanced T9 bosses Dark Elf, Erohim, and Selupan around high HP, lethal damage, lower defense-rate walling, and party kill expectations.
- Corrected Golden Tantallos level to `90`.
- Capped monster poison tick damage at `20,000` to prevent Decay poison from deleting bosses.

### Client Combat

- Fixed Twisting Slash hold behavior so it casts in place without requiring a selected target or forcing repeated micro-movement.
- Added Twisting Slash area-skill settings with `effectRange = 2`.
- Reduced targeted combat skill packet throttle from `300ms` to `150ms`.
- Kept utility/buff targeted skill throttle at `300ms`.
- Kept Nova, Beast Uppercut, and Darkside targeted skills unthrottled.
- Improved MU Helper attack cadence by running attack ticks every `50ms` while keeping full helper work at `250ms`, closer to held right-click behavior for skills like Penetration.

### Buffs And Master Skills

- Safe checkpoint: party buffs and self-buffs are working again after the buff/helper fix pass.
- Fixed elf `Attack Increase Mastery` client dispatch so the mastery buff casts instead of doing nothing.
- Allowed normal buffs and regeneration skills to apply in safe zones instead of only during mini-games.
- Prevented MU Helper from using player/PVP targets for automated attacks.
- Added a mandatory configuration update for active buff master skill target attributes and aggregate types.
- Pending: manual buffs on non-party players still incorrectly require holding `CTRL`.
- Pending: Wizardry Enhance Strengthener/Mastery tooltip and real damage effect must be corrected and verified.
- Checkpoint: manual non-party buff targeting and Wizardry Enhance damage/tooltip fixes are compiled and deployed, pending in-game verification.

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

### Jewel Bank

- Added the first Jewel Bank plugin checkpoint with server-side account balances, client packet handling, MU Helper access, 17 supported item slots, live balance refresh, deposit/withdraw single-item actions, and deposit/withdraw 10-pack actions.
- Added the first client UI checkpoint: dark metal frame, live item icons rendered through the client 3D UI pass, aligned table columns, live counts, and custom plus/minus action buttons.
- Replaced the temporary flat/code-painted Jewel Bank background with a dedicated gothic MU-style client skin asset.
- Removed the client-side black table repainting so the ornate frame, textured grid, row shading, and target visual design render correctly.
- Kept the Jewel Bank technical layer unchanged: server balances/actions, live client counts, 3D item icons, and plus/minus button actions remain dynamic.

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
- Updated live `config."MonsterAttribute"` values for Golden Budge Dragon, Golden Goblin, Golden Soldier, Golden Titan, Golden Vepar, and Red Dragon.
- Updated live Red Dragon `NumberOfMaximumItemDrops` to `2`.
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
- `GameLogic/ItemAuditLogger.cs`
- `GameLogic/PoisonMagicEffect.cs`
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
- `GameLogic/PlayerActions/Items/ItemBoxDroppedPlugIn.cs`
- `GameLogic/PlayerActions/Items/PickupItemAction.cs`
- `GameLogic/Actions/Items/SimpleItemCraftingHandler.cs`
- `GameLogic/PlugIns/PartyAutoCommandPlugIn.cs`
- `GameLogic/PlugIns/VipExpirationCheckPlugIn.cs`
- `GameLogic/PlugIns/ItemTradeAuditPlugIn.cs`
- `GameLogic/PlugIns/ChatCommands/ItemChatCommandPlugIn.cs`
- `GameLogic/PlugIns/ChatCommands/SetVipChatCommandPlugIn.cs`
- `GameLogic/PlugIns/ChatCommands/VipInfoChatCommandPlugIn.cs`
- `GameLogic/PlugIns/ChatCommands/PKClearChatCommandPlugIn.cs`
- `GameLogic/PlugIns/ChatCommands/OfflineLevelingChatCommandPlugIn.cs`
- `GameLogic/PlugIns/ChatCommands/StartGoldenInvasionChatCommandPlugIn.cs`
- `GameLogic/PlugIns/ChatCommands/StartRedDragonInvasionChatCommandPlugIn.cs`
- `GameLogic/PlugIns/ChatCommands/StartT9BossInvasionChatCommandPlugIn.cs`
- `GameLogic/PlugIns/InvasionEvents/T9BossInvasionPlugIn.cs`
- `Persistence/Initialization/VersionSeasonSix/InvasionMobsInitialization.cs`
- `Persistence/Initialization/VersionSeasonSix/Maps/BalgassRefuge.cs`
- `Persistence/Initialization/VersionSeasonSix/Maps/LandOfTrials.cs`
- `Persistence/Initialization/VersionSeasonSix/Maps/RaklionBoss.cs`
- `Persistence/Initialization/VersionSeasonSix/SkillsInitializer.cs`
- `GameServer/RemoteView/Character/ShowCharacterListPlugIn.cs`
- `GameServer/Networking/ClientListener.cs`
- `Persistence/EntityFramework/Migrations/20260515120000_AddAccountVipExpirationDate.cs`
- `Persistence/EntityFramework/Migrations/EntityDataContextModelSnapshot.cs`
- `Directory.Build.props`

## Main Client Files

- `MuMain/src/source/Engine/Object/ZzzInterface.cpp`
- `MuMain/src/source/MUHelper/MuHelper.cpp`
- `MuMain/src/source/MUHelper/MuHelper.h`
- `MuMain/src/source/UI/NewUI/NewUIMuHelper.cpp`
- `MuMain/src/source/UI/NewUI/NewUIMuHelper.h`
- `MuMain/src/source/Platform/Windows/Winmain.cpp`
- `MuMain/src/bin/Data/Interface/barna_jewelbank_back.OZJ`

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

- `2026-05-25`: Jewel Bank plugin/client checkpoint: account-backed jewel/box balances, deposit/withdraw actions, live client balance updates, MU Helper entry point, first gothic table UI pass with 3D item icons, and final dedicated gothic skin asset replacing the flat black prototype background.
- `2026-05-24`: Added the Drop Audit Logger: central `ItemAuditLogger` with source-attributed item logging for monster drops, GM commands, box rewards, crafting, pickups, and trades, written to per-day forensic log files; retired the Phase 1 map-hook plugin.
- `2026-05-23`: Client combat and invasion balance pass: fixed Twisting Slash hold/movement behavior, added Twisting Slash area range, reduced targeted combat skill throttle, improved MU Helper attack ticks, rebalanced low/mid Golden mobs, Golden Tantallos-tier mobs, Red Dragon, and T9 boss combat stats, and capped monster poison ticks.
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
- Audit logger: extend the notable-item filter to also cover jewels and special consumables.
- Audit logger: capture the persistent serial for freshly created items (currently recorded only once the item becomes persistent).

## Future Plugin Backlog

- Castle Siege.
- Jewels Bank.
- Player WCoin Market / Web Market.
- Offline Personal Store.
- Web Market Browser.
- Guild Bank.
- Boss Token System.
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

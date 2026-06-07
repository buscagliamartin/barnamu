# Upstream Migration

## Aligned Upstream Baseline Checkpoint

Date: 2026-06-07

Current clean baseline:

- Clean server: `C:\MuDev\_clean\OpenMU`.
- Clean client: `C:\MuDev\_clean\MuMain`.
- Messy reference server only: `C:\MuDev\OpenMU`.
- Messy reference client only: `C:\MuDev\MuMain`.
- Selected upstream commit: `9a444b6c3610c2c41b138c5f3793e946a8c1c448` (`9a444b6c3`, 2026-06-04, `Merge pull request #787 from nolt/dark-mode`).
- Clean server checkpoint branch: `barna/aligned-upstream-baseline`.
- Clean server checkpoint tag: `aligned-upstream-baseline-20260607`.

The old wrong clean server at `C:\MuDev\_clean_wrong_base_20260607` is retired and must not be used. It was discarded because it was not the selected aligned upstream baseline for the preserved BarnaMu database and led to clean-source/current-DB runtime mismatch work that should not become the migration base.

Database status:

- The current PostgreSQL `openmu` database is preserved and remains the source of truth.
- No DB mutation was applied for this checkpoint.
- Do not add compatibility columns, mark migrations as applied, wipe, restore, drop tables, or drop columns without a fresh explicit approval.

Smoke test result:

- Server built successfully.
- Server started successfully.
- No migration prompt appeared.
- No `ConsumeHandlerClass` runtime error appeared.
- No `ItemSetGroupId` runtime error appeared.
- Ports are correct again: admin/web `5000`, connect `44405`/`44406`, game `55901`/`55902`, chat `55980`.
- Existing client connected.
- Old admin account worked.
- Character entered the game.
- Spots, mobs, and configuration were visible.
- Login and join-map smoke test passed.

Shared migration baseline:

- Official upstream has 44 EF migrations.
- Messy server has the same 44 upstream migrations plus 8 BarnaMu custom migrations.
- All 44 shared migration files matched by SHA-256.

Next BarnaMu custom migration order:

1. `20260515120000_AddAccountVipExpirationDate`
2. `20260524120000_AddJewelBank`
3. `20260525120000_AddItemBankBoxes`
4. `20260525183000_AddWCoinLedger`
5. `20260525203000_AddAuctionHouseListings`
6. `20260526120000_AddDuelLadder`
7. `20260531120000_AddAuctionListingEscrowStorage`
8. `20260601170000_AddAuctionMailboxEntries`

Do not port features until this baseline remains green. Future feature branches should use the clean server/client paths above, use the messy repositories only as references, and keep the current DB as the preserved runtime truth.

Runtime compatibility fix record:

- `codex/fix-itemset-optionsid-model` resolved the clean source vs current DB mismatch for `ItemSetGroup.OptionsId`.
- No DB mutation was applied for that compatibility fix.
- Clean server starts successfully after the source/model alignment.

Account economy foundation record:

- `codex/custom-account-economy-foundation` aligns clean source/model files with the existing BarnaMu account economy schema already present in the preserved `openmu` database.
- Scope is limited to `Account.VipExpirationDate`, `Account.WCoin`, the 17 account `JewelBank...` balances, and `WCoinTransaction` persistence/model mapping.
- Existing custom migration source files were copied for history consistency only: `20260515120000_AddAccountVipExpirationDate`, `20260524120000_AddJewelBank`, `20260525120000_AddItemBankBoxes`, and `20260525183000_AddWCoinLedger`.
- No DB SQL was executed except read-only metadata checks. No schema/data mutation was applied, no migration was generated, and no migration was applied.
- `AccountState.Vip`, VIP runtime behavior, Jewel Bank handlers, Cash Shop/WCoin behavior, Auction, Duel, Mu Helper, client UI, and gameplay logic remain deferred to later isolated branches.
- Verification: DataModel, Persistence, Persistence.EntityFramework, GameServer, and Startup builds passed. Clean Startup reached host started/listeners against the current DB with no migration prompt and no missing column/table error.

## Custom Feature Isolation Audit

Date: 2026-06-05

Full audit: `[docs] PROJECTS/CUSTOM_FEATURE_INVENTORY.md`

Server plugin audit: `[docs] PROJECTS/SERVER_PLUGIN_INVENTORY.md`

Graphify/GitNexus mapping exists for the wrapper, messy server, messy client, web app, and both clean forks. This section is a summary only; no source code was ported, merged, cherry-picked, deleted, or refactored during the audit.

## Project Docs Reconciliation Audit

Date: 2026-06-05

Docs reconciled:

- `[docs] AI_START_HERE.md`, `[docs] DOCUMENTATION_INDEX.md`, and all `[docs] CHANGELOG_*.md` files.
- All feature docs under `[docs] PROJECTS/`.
- Messy server project docs with BarnaMu/custom facts: `[server] BarnaMu Server ChangeLog.md`, `[server] AuctionHouse.md`, and `[server] docs/reports/monster-*.md`.
- Upstream/generated readmes and packet catalogs under `[server] docs/Packets` were treated as supporting protocol references, not as custom feature status sources.

Reconciliation rules:

- The inventories and current source scan win for whether a plugin, service, handler, command, migration, or packet hook exists.
- Project docs and changelogs win for exact feature intent, known runtime caveats, build/test status, and "do not mark complete" requirements.
- Older handoff docs can be stale. When a doc says a feature is planned/backlog but the plugin inventory finds concrete local-only code, treat the code as real and keep the doc statement as historical.
- No feature may be considered migrated or complete from docs alone. Each branch still needs a build plus the relevant live smoke test or screenshot checklist.

### Reconciled Requirements

- Auction House/Mailbox: preserve the server escrow chain `Inventory -> AuctionListing.EscrowStorage -> AuctionMailboxEntry.ItemStorage -> buyer/seller inventory`, active-row deletion on successful buy/cancel, pending-row deletion on claim, and 160-character display label cap. Apply and verify the mailbox migration `20260601170000_AddAuctionMailboxEntries`. The Lorencia Postman mailbox route uses NPC `600` at `(141,140)` and `MailboxNpcTalkPlugIn`; client classification for monster type `600` must be retained. Old null-escrow or broken listings should be recreated or cleaned only as a deliberate DB cleanup, not migrated as valid test data.
- Auction client: the old `[server] AuctionHouse.md` 16-byte packet contract is superseded by the newer additive extended browse request described in `[docs] PROJECTS/AUCTION_HOUSE.md`. Keep the first 80 bytes of listing rows compatible and preserve appended compact item preview bytes. Global `S` hotkey/routing, Create right-click inventory selection, Mailbox Claim, and Claim all remain verification gates.
- Jewel Bank: migrate all 17 supported slots, including Kundun boxes and Blue/Pink Chocolate, and keep `0xBF/0x30` ops `0` query, `1` deposit, `2` withdraw, `3` deposit-all. Preserve the current UI extraction requirement from `NewUIMuHelper.cpp/.h` and the client rendering lesson that flat `RenderColor` fills need texture state forced off before drawing.
- Duel Ladder: resolve the contradiction between old "op 0/op 1 only" notes and the server plugin inventory/changelog. Current server-side docs/source surface include waiting list/challenge ops `2-5`, history op `8`, Hall of Fame op `10`, and ranking challenge op `11`; do not port a client contract that assumes only ops `0` and `1`. Recheck the prior `QueryHallOfFameAsync` build-signature note before using any old GameServer build result. Preserve best-of-three Duel config (`MaximumScore = 2`) via fresh seed and update plugin, and treat the v17/v18 client visuals as screenshot-pending unless current disk proves otherwise.
- Mu Helper/offlevel/PvP: preserve stationary behavior with no helper-driven walking/pathfinding. The settings blob remains 257 bytes and server mode byte offset `29` maps `0 = ATTACK`, `1 = BUFF`, `2 = BASIC ATTACK`. Offlevel stays VIP/GM-only. Do not port the old messy client/server PvP targeting changes blindly: the `0x8000` explicit-PvP marker, server-side masking contract, and temporary PvP diagnostics are historical validation material only until a fresh design and live tests prove they are needed.
- Invasions/bosses/drops: treat periodic task foundation, Golden/Red Dragon/T9 runtime plugins, Blood/Chaos/Devil Square start plugins, map event state remote view, GM force-start commands, monster combat rebalance, and drop/reward tuning as separate migration pieces. Exact boss facts from docs include T9 rotation over Selupan `459`, Erohim `295`, and Dark Elf `412`; Red Dragon `44`; Golden monsters `43`, `53`, `54`, and `78-83`; Golden Tantallos level `90`; Red Dragon `NumberOfMaximumItemDrops = 2`; and monster poison tick cap `20,000`.
- Economy/rates/VIP: keep the slow-medium economy values together with their update plugins: normal/VIP EXP `30x/35x`, master EXP `15x/20x`, Zen `10x/13x`, drop `5x/7x`, common excellent `0.0001` normal and `0.00014` VIP/GM effective, common jewel `0.0005` normal and `0.0007` VIP/GM effective, Jewel of Guardian `0.00025`, global `Stats.MoneyAmountRate = 0.75`, money drop group default `50%`, and party Zen split only across nearby observing EXP recipients.
- VIP: preserve `AccountState.Vip`, `Account.VipExpirationDate`, login-time and periodic expiration, `/setvip`, `/vipinfo`, VIP-only `/offlevel`, extended vault, map reductions in `WarpAction.cs` and `WarpGateAction.cs`, Chaos Machine success bonus, party/zen/drop bonuses, and `[VIP]` chat tag in message body only. Do not put `[VIP]` into character list names.
- WCoin/Cash Shop: packet group `0xD2`, product version `512.2012.084`, and banner version `583.2011.001` exist, but balances, purchases, gifts, storage, delete, and consume flows are placeholder/non-authoritative. Do not enable production WCoin, real-money purchases, or player-market sales before account currency persistence, transaction logs, item escrow/storage, rollback/duplicate prevention, and audit logging are authoritative.
- Public Web: keep it standalone from OpenMU assemblies. It uses ASP.NET Core Razor Pages/.NET 10, Dapper + Npgsql, and BCrypt.Net-Next against PostgreSQL, plus `web."BugReport"`. Source changes for profile/API/redesign are not live until `dotnet publish -c Release -o .\publish` and restart. Web sidebar event timers, rates, DST clock, Castle Siege assumptions, and least-privilege DB user remain migration requirements before public launch.
- Infinite Arrow passive: preserve server-backed permanent passive for qualified Muse Elf/High Elf characters with Marlon level-220 quest reward skill `77`, zero ammunition consumption, and client arrow/Multi Shot/HUD checks. Also port `InfinityArrowSkillOnQuestCompletionPlugIn` from the seed/update layer if the clean fork lacks that eligibility data.
- ItemAuditLogger: preserve non-throwing daily file logging to `%BARNAMU_AUDIT_DIR%` or `C:\MuDev\Logs\ItemAudit`, sources `MonsterDrop`, `GmCommand`, `BoxReward`, `Crafting`, `Pickup`, and `Trade`, and the retired `DropAuditLoggerPlugIn` map-catch-all decision. Extend notable filters for jewels/special consumables and verify fresh persistent serial capture later.
- Scripts/build: use `BarnaMu.bat` workflows for build/start/stop/web publish/backup/push. Web source edits require publish; CSS-only publish-folder copies are not enough for CSHTML/C# changes. Keep script changes out of feature branches unless the feature explicitly needs workflow support.
- Generated/report data: `monster-spawns-season6.md`, `monster-spawns-live-db.md`, and `monster-combat-rebalance-update89.md` are reference outputs. They prove the migration must include spawn/update data validation, but they should not be blindly copied as source.

### Resolved Contradictions

- Duel Ladder is not merely backlog/planned. Older docs say that, but server inventory and changelog entries identify concrete services, request ops, season end, waiting list, match history, and Hall of Fame work. Migration must inventory current disk before deciding client/server parity.
- Auction House is not just the compact old UI handoff. The old handoff remains useful for legacy op meanings and cautionary UX notes, but the newer project/changelog facts add escrow storage, durable mailbox, compact preview bytes, filters, Postman NPC, and row deletion behavior.
- Jewel Bank status is not "first gothic skin asset" anymore. Later docs state the current approved direction is native Season 6 style with flat primitive buttons and 17-slot Deposit-All, while final re-screenshot may still be pending.
- Public Web source status and live runtime status differ. Source can be "done" while the published site is still old until publish/restart is performed.
- WCoin exists as packet/client opening support, not as production economy authority.

### Cleanly Isolated

- Public Web: separate Razor web app. It mainly needs the final DB schema and connection settings.
- Standalone Mailbox client window: own `MailboxWindow.cpp/.h`, but it still depends on Auction mailbox packet parsing.
- Duel Ladder client window: own `DuelLadderWindow.cpp/.h`, with normal registration, hotkey, and packet dispatch touchpoints.
- Custom client send bridge: `PacketFunctions_Custom.*` and `ConnectionManager.ClientToServer.Custom.cs` are good extension points.

### Partially Isolated

- Auction House server domain: mostly own service/handler/view files, but DB/generated files and client UI/parser are central.
- Auction Mailbox server flow: own entity and NPC plugin, but mailbox logic currently lives inside `AuctionHouseService`.
- Jewel Bank server flow: own action/handler/view plugin, but account schema and client UI are central.
- Duel Ladder server flow: many own services/actions/plugins, but Duel lifecycle and PvP rules touch `Player.cs`, `AttackableExtensions.cs`, and GameContext.
- ItemAuditLogger: logger is isolated, but hooks are intentionally scattered through item ownership transitions.
- Economy, VIP, invasions, and drops: many plugin/config pieces, but reward calculation and party Zen/EXP logic touch core gameplay.
- WCoin foundation: account/currency pieces exist, but production cash shop authority is not complete.
- Server periodic events and invasions: Golden, Red Dragon, T9 Boss Invasion, Blood/Chaos/Devil Square start plugins, Happy Hour, and force-start GM commands are local-only and should be grouped deliberately.
- Server seed/update plugins: a large local-only `Persistence/Initialization/Updates` layer controls money/jewel rates, drop groups, spawn fixes, skill fixes, Duel config, reset attributes, mailbox NPC, and Infinite Arrow setup.

### Not Isolated

- Auction House client UI: large `CNewUIAuctionHouse` implementation is embedded in `NewUIMuHelper.cpp/.h`.
- Jewel Bank client UI: `CNewUIJewelBank` is embedded in `NewUIMuHelper.cpp/.h`.
- Mu Helper custom behavior: mixed through original helper UI/runtime, server player state, target loops, and packet handling.
- PvP/CTRL/basic attack fixes: core combat behavior across server and client.
- Reset/stat packet changes: generated packets, `Stats.cs`, `Player.cs`, and `WSclient.cpp`.
- Database model/snapshot changes: EF snapshot and generated mapping files are monolithic.
- Infinite Arrow passive: small, but it touches player setup, ammo consumption, and client arrow checks.

### Protocol Branch Safety Gate

Before applying edits in `codex/migration-protocol-generated-packets`, first produce and show a protocol inventory, then wait for approval before touching generated packet files, packet XML/schema files, or central client receive dispatch.

The inventory must compare messy reference repositories against the clean forks and classify each item as: exists in clean upstream already, local-only custom, modified locally, should migrate now, should wait for a later branch, or should not migrate.

Required inventory scope:

- Server packet XML/schema files, generated packet files, custom packet handlers, remote view packet senders, request/response structs/classes, and subhandler registration.
- Client `PacketFunctions_Custom.cpp`, `PacketFunctions_Custom.h`, `ConnectionManager.ClientToServer.Custom.cs`, `WSclient.cpp`, `WSclient.h`, and packet structs/parsers for Jewel Bank, Auction/Mailbox, Duel Ladder, Mu Helper status, Cash Shop/WCoin, and reset/stat.

Do not copy giant generated files blindly. If generated files must change, prefer reviewed schema changes followed by clean regeneration.

### Protocol Foundation Branch Result

Date: 2026-06-05

Branch: `codex/migration-protocol-generated-packets`

Migrated minimal foundation only:

- Clean client outbound bridge functions in `ConnectionManager.ClientToServer.Custom.cs` and `PacketFunctions_Custom.cpp/.h` for Jewel Bank `0xBF/0x30`, Auction/Mailbox `0xBF/0x31`, extended Auction browse, and Duel Ladder `0xBF/0x32`.
- Clean server generic `0xBF` group dispatcher in `GameServer/MessageHandler/MuHelper/MuHelperGroupHandler.cs`.

Intentionally deferred:

- Generated packet files, packet XML/schema files, `WSclient.cpp`, `WSclient.h`, receive parsing, feature UI, domain handlers, DB entities/migrations, Mu Helper behavior, Cash Shop/WCoin production logic, PvP/basic attack logic, and reset/stat behavior.
- Auction/Jewel/Duel server handlers and remote views remain later feature-branch work. The branch only creates extension points for future protocol-safe calls.

### Account DB Foundation Branch Result

Date: 2026-06-05

Branch: `codex/migration-account-db-foundation`

Commit: `701b3119a2e9afc9f1b8269746287e5ec9bf57bb`

Migrated phase-1 schema/model foundation only:

- `Account.VipExpirationDate`.
- `Account.WCoin`.
- 17 Jewel Bank account balance columns: Bless, Soul, Life, Creation, Guardian, Gemstone, Harmony, Chaos, LowerRefineStone, HigherRefineStone, Kundun1, Kundun2, Kundun3, Kundun4, Kundun5, ChocoBlue, and ChocoPink.
- `Character.MuHelperConfiguration`.
- `Character.DuelRating`.
- `Character.DuelWins`.
- `Character.DuelLosses`.
- `Character.DuelResetBracket`.
- `WCoinTransaction` entity/table with account FK, timestamp, amount, balanceAfter, reason, source, actor, note, and `(AccountId, Timestamp)` index.
- `AuctionListing` entity/table with escrow item/storage relationship, unique listing number, display label max length `160`, required FK/index relationship setup, and auction listing indexes.
- `AuctionMailboxEntry` entity/table with item/storage relationship, display label max length `160`, required FK/index relationship setup, and mailbox lookup indexes.
- EF migration `20260605125201_Phase1AccountDbFoundation`.
- Regenerated BasicModel and EntityFramework persistence model files for the new entities, plus EF snapshot updates produced from the clean model.

Verification results:

- `dotnet build C:\MuDev\_clean\OpenMU\src\DataModel\MUnique.OpenMU.DataModel.csproj -maxcpucount:1` passed.
- `dotnet build C:\MuDev\_clean\OpenMU\src\Persistence\EntityFramework\MUnique.OpenMU.Persistence.EntityFramework.csproj -maxcpucount:1` passed.
- `dotnet build C:\MuDev\_clean\OpenMU\src\Persistence\MUnique.OpenMU.Persistence.csproj -maxcpucount:1` passed.
- `dotnet test C:\MuDev\_clean\OpenMU\tests\MUnique.OpenMU.Persistence.Initialization.Tests\MUnique.OpenMU.Persistence.Initialization.Tests.csproj -maxcpucount:1` passed: 6 passed, 2 skipped.
- `dotnet build C:\MuDev\_clean\OpenMU\src\GameServer\MUnique.OpenMU.GameServer.csproj -maxcpucount:1` passed.

Full solution verification note:

- `dotnet build C:\MuDev\_clean\OpenMU\src\MUnique.OpenMU.sln -maxcpucount:1` was attempted and failed only because of an unrelated existing static web asset conflict around `css/open-iconic/FONT-LICENSE` between web projects. This is not caused by the DB foundation branch.

Intentionally deferred:

- `AccountState.Vip` and `CharacterStatus.Vip`; VIP state must use an explicit numeric/non-breaking strategy in the later VIP branch.
- Duel configuration, Duel areas, `GameConfiguration` Duel config changes, `AddDuelConfigurationPlugIn`, and `SetDuelBestOfThreePlugIn`.
- Reset update plugins and reset/stat behavior.
- Mailbox NPC seed/update plugins.
- Auction, Jewel Bank, and Duel services, handlers, remote views, packet handlers, and client UI.
- VIP behavior, WCoin commands, Cash Shop behavior, production WCoin purchases/gifts/storage/consume flows, and real-money behavior.
- Mu Helper behavior and PvP/basic attack behavior.

### Reset Stat/EXP Compatibility Branch Result

Date: 2026-06-05

Branch: `codex/migration-reset-stat-exp-compat`

Server commit: `f84b247f4de6e755628ab74ddb901dee85d5a97a`

Client commit: `c2f8852f929a435c3eeab83bf5fd7eaa959d6e5b`

Migrated minimal compatibility foundation only:

- Reviewed server-to-client packet XML declarations and regenerated generated packet files for:
  - `CharacterInformationExtended` (`C3 F3 03`, length `92`, 64-bit current/next experience, `InventoryExtensions` at offset `88`, spare byte at offset `89`, `Resets` as `ushort` at offset `90`).
  - `CurrentStatsExtended` (`C1 26 FF`, 32-bit current health, shield, mana, ability, and speed fields).
  - `MaximumStatsExtended` (`C1 26 FE`, 32-bit maximum health, shield, mana, and ability).
  - `BaseStatsExtended` (`C1 F3 32`, 32-bit base strength, agility, vitality, energy, and leadership).
  - `CharacterStatIncreaseResponseExtended` (`C1 F3 06`, 32-bit maximum stat result fields).
  - `MasterStatsUpdateExtended` (`C1 F3 50`, 64-bit master experience and 32-bit maximum stats).
  - `MasterCharacterLevelUpdateExtended` (`C1 F3 51`, 32-bit maximum stats).
- Minimal server remote-view sender plugins for the approved extended stat/join-map/master-stat packets, using existing clean player attributes and interfaces where available.
- A small `IUpdateCharacterBaseStatsPlugIn` interface so later reset/stat code can emit `BaseStatsExtended` without pulling reset gameplay into this branch.
- Client `WSclient.h` static assertions for `PRECEIVE_JOIN_MAP_SERVER_EXTENDED` offsets and size:
  - `InventoryExtensions == 88`.
  - `Resets == 90`.
  - struct size `92`.

Files touched:

- Server packet schema/generated files: `src/Network/Packets/ServerToClient/ServerToClientPackets.xml`, `ServerToClientPackets.cs`, `ServerToClientPacketsRef.cs`, and `ConnectionExtensions.cs`.
- Server remote views/interfaces: `IUpdateCharacterBaseStatsPlugIn.cs`, `UpdateCharacterStatsExtendedPlugIn.cs`, `UpdateCharacterBaseStatsExtendedPlugIn.cs`, `UpdateCurrentHealthExtendedPlugIn.cs`, `UpdateCurrentManaExtendedPlugIn.cs`, `UpdateMaximumHealthExtendedPlugIn.cs`, `UpdateMaximumManaExtendedPlugIn.cs`, `StatIncreaseResultExtendedPlugIn.cs`, `UpdateMasterStatsExtendedPlugIn.cs`, and `UpdateLevelExtendedPlugIn.cs`.
- Client layout guard: `src/source/Network/Server/WSclient.h`.

Verification results:

- `dotnet build C:\MuDev\_clean\OpenMU\src\Network\Packets\MUnique.OpenMU.Network.Packets.csproj -maxcpucount:1` passed and regenerated the packet files. The packet markdown docs helper printed local path warnings; no packet docs changes were kept.
- `dotnet build C:\MuDev\_clean\OpenMU\src\GameServer\MUnique.OpenMU.GameServer.csproj -maxcpucount:1 -p:ci=true` passed. The `ci=true` switch was used because the normal local prebuild path requires the .NET 6 runtime for the persistence source generator, while this machine currently has newer runtimes.
- `cmake --preset windows-x86` passed for the clean client.
- `cmd.exe /c "call C:\Program Files\Microsoft Visual Studio\18\Community\Common7\Tools\VsDevCmd.bat -arch=x86 -host_arch=x86 >nul && cmake --build --preset windows-x86-debug"` passed. A plain shell build failed before source compilation because `windows.h` was not on the include path until `VsDevCmd.bat` loaded the Windows SDK environment.

Runtime smoke tests still required on a live clean server/client pair:

- Login.
- Character selection.
- Join map.
- Stat window.
- EXP gain.
- Level-up.
- Master level-up.
- Stat increase.
- Death/revival EXP.
- Reset counter display.

Intentionally deferred:

- Reset gameplay, `/reset`, reset NPCs, Gatekeeper, reset costs, reset progression, and PointsPerReset.
- `ResetCharacterPointRequest`, reset request handler, reset action, reset feature plugin, reset NPC plugin, reset chat/info commands, Gatekeeper plugin, and PointsPerReset update plugin.
- Broad `Player.cs` or `Stats.cs` changes.
- `WSclient.cpp`, UI, reset UI, Mu Helper, Auction, Jewel Bank, Duel Ladder, PvP/basic attack, WCoin/Cash Shop, DB entities, migrations, and packet handlers.
- Production Cash Shop/WCoin behavior and real-money behavior.

### PvP Targeting / Basic Attack Validation

Date: 2026-06-05

Branch: `codex/validate-pvp-targeting-basic-attack`

Scope: validation and documentation only. No OpenMU or MuMain source code should change on this branch.

Clean client test result:

- Clean sven-n MuMain client tested against the current server did not reproduce the Elf buff -> left-click/basic attack stuck-CTRL bug.
- Decision: keep clean upstream targeting behavior as the baseline and do not port the old messy PvP/basic attack client changes.
- The old messy implementation is useful only as a risk map before `codex/migration-muhelper-core`.

Inventory summary:

| Area | Relevant messy difference | Classification |
| ---- | ------------------------- | -------------- |
| Client `ZzzInterface.cpp/.h` | Adds `g_bMuHelperBasicAttackInProgress`, `CheckAttackCore`, `CheckBasicAttackTarget`, CTRL probing, `0x8000` explicit-PvP marking, and basic-hit diagnostics. | High-risk messy change. Do not port. Rebuild any future basic-attack target split on clean upstream behavior. |
| Client `MuHelper.cpp/.h` | Simulates Basic Attack by mutating/restoring global targeting state such as `SelectedCharacter`, `ActionTarget`, `Attacking`, movement type, and movement skill. | High-risk messy change. Do not port wholesale. Future Basic Attack mode must avoid stale player target state. |
| Client `WSclient.cpp/.h` | Routes player attackers into Mu Helper self-defense/PvP handling, masks explicit-PvP target ids, and mixes unrelated custom packet parsers. | High-risk central dispatch change. Do not port for Mu Helper core unless separately approved and live-tested. |
| Client `NewUIMuHelper.cpp/.h` | Contains Basic/Buff/Skill mode UI mixed with Auction House, Jewel Bank, Mailbox, and other embedded UI work. | Basic Attack mode concept may be needed later; do not copy this file wholesale. Extract narrowly. |
| Server `Player.cs` and `AttackableExtensions.cs` | Adds broad PvP permission changes, friendly target checks, and `isExplicitPvpRequest` plumbing. | Server-side validation idea is safe, but the messy implementation is coupled to the suspect client contract. Do not port as-is. |
| Server hit/skill handlers and actions | Propagate `0x8000` explicit-PvP state through normal hit, targeted skills, area skills, rage skills, and diagnostics. | High-risk protocol/combat behavior. Defer until a fresh server-authoritative design is approved. |

Messy changes that should not be ported now:

- Client `0x8000` explicit-PvP target marking and target-id masking.
- Global-state Basic Attack simulation that touches `SelectedCharacter`, `ActionTarget`, `Attacking`, movement type, or movement skill.
- `WSclient.cpp` Mu Helper PvP/self-defense routing based on received player attacks or skills.
- Temporary visible PvP debug diagnostics in combat handlers.
- Broad `Player.cs`, `AttackableExtensions.cs`, `ZzzInterface.cpp`, `MuHelper.cpp`, or `WSclient.cpp` copies.

Possibly useful later, but only as clean redesign inputs:

- Dedicated Mu Helper `BASIC ATTACK` mode.
- Monster-only Mu Helper target filtering.
- Separation between beneficial buff targets and offensive/basic-attack targets.
- Server-authoritative denial of automated player/PvP damage.
- A separately reviewed explicit PvP signal, if live tests prove clean upstream behavior is insufficient.

Future live validation checklist:

1. Log in with Elf.
2. Log in with another character.
3. Elf buffs the other character.
4. Do not hold CTRL.
5. Left-click/basic attack the other character.
6. Expected: no attack, no PK.
7. Use offensive skill without CTRL.
8. Expected: no attack unless valid PvP context.
9. Hold CTRL intentionally.
10. Expected: PvP only if server/map rules allow it.
11. Enable future Mu Helper Basic Attack mode later.
12. Repeat the same test to make sure Mu Helper does not reintroduce the bug.

Rule for `codex/migration-muhelper-core`: Basic Attack mode must be rebuilt on top of clean upstream targeting behavior, not copied from the messy client. Do not introduce automated player targeting, stale `ActionTarget` reuse, stuck CTRL behavior, or the old `0x8000` protocol contract without a separate approved design and live test result.

### Mu Helper Core Phase 1 Branch Result

Date: 2026-06-05

Branch: `codex/migration-muhelper-core`

Server commit: `e8552ea9c4ea9e1080dd0946096df4c169b7bbfd`

Client commit: `eaf7c4e3deb6261bfa2a21e1422e28210d639c07`

Migrated Phase 1 foundation only:

- Server Mu Helper settings/status/config core files, including `IMuHelperSettings`, `MuHelperMode`, `MuHelperStatus`, parsed `MuHelperSettings`, and `MuHelperSettingsSerializer`.
- Server save/status actions and packet handlers for the existing Mu Helper configuration/status flow behind the `0xBF/0x51` and `0xAE` contracts.
- Server remote-view configuration/status updates using a small manual packet writer because generated packet/XML/schema files were intentionally off-limits in this branch.
- Tiny `Player.cs` integration only for holding the parsed Mu Helper settings/active state and loading saved `Character.MuHelperConfiguration` at world entry.
- Client Mu Helper mode enum/config serialization, with a minimal `ATTACK` / `BUFF` / `BASIC ATTACK` selector in `NewUIMuHelper.cpp`.
- Client stationary helper guardrails: helper-driven walking/pathfinding/chasing and helper pickup movement were removed from the migrated runtime paths.

Mode byte contract:

- The client `PRECEIVE_MUHELPER_DATA` network struct remains a `257`-byte settings blob.
- `ServerMode` is at blob offset `29`; because the config packet body starts after the 4-byte header, this is packet byte index `33`.
- Mode values remain `0 = ATTACK`, `1 = BUFF`, `2 = BASIC ATTACK`; invalid values default to `ATTACK`.
- `ExtraItems` remains at blob offset `65`.
- `WSclient.h` has static assertions for `ServerMode == 29`, `ExtraItems == 65`, and `sizeof(PRECEIVE_MUHELPER_DATA) == 257`.

Files touched:

- Server: `src/GameLogic/Player.cs`, `src/GameLogic/MuHelper/*`, `src/GameLogic/PlayerActions/MuHelper/*`, `src/GameLogic/Views/MuHelper/*`, `src/GameServer/MessageHandler/MuHelper/*`, and `src/GameServer/RemoteView/MuHelper/*`.
- Client: `src/source/MUHelper/MuHelper.cpp`, `src/source/MUHelper/MuHelperData.cpp`, `src/source/MUHelper/MuHelperData.h`, `src/source/Network/Server/WSclient.h`, and `src/source/UI/NewUI/NewUIMuHelper.cpp`.

Verification results:

- `dotnet build C:\MuDev\_clean\OpenMU\src\GameServer\MUnique.OpenMU.GameServer.csproj -maxcpucount:1 -p:ci=true` passed with 0 errors. Existing warning set remains, including the pre-existing `System.Text.Json 6.0.5` vulnerability warning from persistence and StyleCop warnings in unrelated files.
- `cmake --preset windows-x86` had already configured the clean client successfully.
- Plain `cmake --build --preset windows-x86-debug` failed before source validation because the non-developer shell lacked Windows SDK include paths and could not find `windows.h`.
- `cmd.exe /c "call C:\Program Files\Microsoft Visual Studio\18\Community\Common7\Tools\VsDevCmd.bat -arch=x86 -host_arch=x86 && cmake --build --preset windows-x86-debug"` passed.

Safety confirmations:

- No generated packet files, packet XML/schema files, `WSclient.cpp`, `ZzzInterface.cpp`, `ZzzInterface.h`, `AttackableExtensions.cs`, `Stats.cs`, Auction/Jewel/Duel/Mailbox UI, WCoin/Cash Shop, reset gameplay, or VIP/offlevel logic were changed.
- The old messy `0x8000` explicit-PvP target marker/masking contract was not ported.
- The old Basic Attack implementation that mutates global target state such as `SelectedCharacter`, `ActionTarget`, `Attacking`, movement type, or movement skill was not ported.
- Runtime Basic Attack is saved/displayed only in Phase 1 and is deliberately deferred until a safe monster-only implementation is designed and live-tested.

Runtime smoke tests still required on a live clean server/client pair:

- Save/load Mu Helper config.
- Verify mode byte `29` for Attack, Buff, and Basic Attack.
- Attack mode attacks monsters only.
- Buff mode does not attack.
- Basic Attack mode does not target players; in Phase 1 it should not perform runtime basic attacks.
- Helper does not walk, pathfind, regroup, chase, or send helper movement.
- Pickup remains stationary/range-limited.
- Elf buffs another character, no CTRL held, left-click/basic attack must not attack.
- Offensive skill without CTRL must not attack unless valid PvP context.
- CTRL-held PvP must still obey server/map rules.

Intentionally deferred:

- `/offlevel`, VIP/GM checks, `AccountState.Vip`, offline player manager/login handoff, and offline runtime combat.
- Repair/pet/offline combat runtime beyond the existing clean client behavior.
- Safe runtime Basic Attack implementation.
- Server-authoritative automated PvP/player-target denial beyond the Phase 1 monster-only client target filtering.
- Auction/Jewel/Duel/Mailbox UI and feature logic, WCoin/Cash Shop, reset gameplay, generated packet/XML/schema changes, and future Mu Helper live-test fixes.

### Recommended Next Step

Do not begin `/offlevel` or the next feature migration until the Mu Helper Phase 1 branch is reviewed and live smoke-tested. The clean migration foundations are:

0B. Server Plugin Inventory Audit - completed.
0C. Project Docs Reconciliation Audit - completed.
1. `codex/migration-protocol-generated-packets` - completed and merged.
2. `codex/migration-account-db-foundation` - completed and merged.
3. `codex/migration-reset-stat-exp-compat` - completed and merged.
4. `codex/validate-pvp-targeting-basic-attack` - completed as docs-only validation.
5. `codex/migration-muhelper-core` - Phase 1 foundation pushed; pending review and live smoke tests.

After that, migrate Jewel Bank, Auction/Mailbox, Duel Ladder, server periodic events/invasions, economy/VIP/drops, Infinite Arrow, ItemAuditLogger, server admin commands, and Public Web as separate branches. Before porting the client feature UI branches, extract Auction House and Jewel Bank out of `NewUIMuHelper.cpp/.h` or perform that extraction as the first commit of their migration branches.

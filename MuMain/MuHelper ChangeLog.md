# MuHelper Refactor ChangeLog
# Files Root Directory: "C:\MuDev\MuMain\"

## Jewel Bank UI

### Objective

Replace the functional Jewel Bank prototype with a polished MU-style gothic interface while keeping all server-backed actions and live data intact.

### Result

The Jewel Bank window now uses a dedicated client skin asset matching the requested ornate dark-metal design, with the dynamic live item icons, balances, and deposit/withdraw buttons rendered on top.

### Changes

- Added `barna_jewelbank_back.OZJ` as the Jewel Bank background skin under `Data\Interface`.
- Added `IMAGE_JEWEL_BANK_BACK` and loaded it through the existing client texture pipeline.
- Replaced the old flat/code-painted black background with the dedicated skin render.
- Removed table background repainting that was covering the textured frame and grid.
- Preserved all functional Jewel Bank behavior: MU Helper entry, live balances, 17 supported slots, 3D item icons, and deposit/withdraw single or 10-pack actions.

### Main Files

- `src\source\UI\NewUI\NewUIMuHelper.cpp`
- `src\source\UI\NewUI\NewUIMuHelper.h`
- `src\bin\Data\Interface\barna_jewelbank_back.OZJ`

## Infinity Arrow Passive

### Objective

Implement permanent passive Infinity Arrow for Muse Elf / High Elf characters who completed the Level 220 Marlon quest, removing arrow equipment and recast requirements while preserving the existing quest reward structure.

### Result

Qualified Elf characters receive permanent server-backed Infinity Arrow on login. Client skill checks and UI gating now respect that passive state, so bow skills work without equipped arrows while non-Elf characters remain unaffected.

### Changes

- Added reusable client predicate `ElfHasInfinityArrow()`.
- Fixed `g_isCharacterBuff` macro usage by wrapping `Hero->Object` as `(&Hero->Object)`.
- Updated `CheckArrow()` to pass immediately when passive Infinity Arrow is active.
- Updated Multi Shot execution gating to allow `GetEquipedBowType_Skill() == BOWTYPE_NONE` when passive Infinity Arrow is active.
- Updated Multi Shot HUD icon logic so the skill is not grayed out for passive Infinity Arrow users.
- Added OpenMU server-side permanent `InfinityArrowPassiveEffect`.
- Added login-time passive application through `ApplyInfinityArrowPassiveAsync()`.
- Passive applies `AmmunitionConsumptionRate *= 0`, making server ammunition checks pass without equipped arrows.
- Passive survives death and refreshes safely on relogin.
- No SQL/schema changes required; existing quest reward skill `77` remains the source of eligibility.

### Main Files

- `src\source\Engine\Object\ZzzInterface.h`
- `src\source\Engine\Object\ZzzInterface.cpp`
- `src\source\UI\NewUI\HUD\NewUIMainFrameWindow.cpp`
- `src\GameLogic\Player.cs`

### Gameplay Impact

- Basic Attack, Triple Shot, Ice Arrow, Penetration, and Multi Shot work without arrows for qualified Elf characters.
- Server validates the passive through magic effect state, not client-only assumptions.
- Non-Elf characters and characters without the Infinity Arrow quest reward are unchanged.

## MuHelper Refactor

## Objective

Refactor MuHelper into a stationary macro-style helper that behaves like `/attack auto` plus `/pick`, removing movement/pathfinding behavior and simplifying unused UI/config options.

## Result

MuHelper now attacks, buffs, heals, repairs, and picks items without walking. Combat target selection uses the real skill range from game skill data instead of helper menu range values. Loot pickup is stationary and the effective pickup range is server-defined.

## Changes

- Removed helper-driven movement/pathfinding from hunting and looting loops.
- Removed hunting and obtaining range controls from helper UI and internal runtime logic.
- Removed static position/static pickup option from helper UI and config state.
- Removed Original Position and Long Distance Counter Attack options/state.
- Target selection now depends on `gSkillManager.GetSkillDistance()` and wall checks.
- Loot pickup now sends stationary pickup requests; server validates actual pickup distance.
- Fixed Elf buff/attack switching by isolating buff casts and restoring the assigned attack skill.
- Fixed party buff rotation so helper buffs party members reliably.
- Removed CTRL requirement for beneficial manual buffs on allies/party members.
- Added VIP-only Offlevel checkbox that sends `/offlevel` when enabled.
- Fixed PvP counterattack so offensive skill hits trigger self-defense retaliation, not only basic attacks.
- Added filtering so friendly/buff skills do not trigger PvP counterattack.

## Main Files

- `src\source\MUHelper\MuHelper.cpp`
- `src\source\MUHelper\MuHelper.h`
- `src\source\MUHelper\MuHelperData.cpp`
- `src\source\MUHelper\MuHelperData.h`
- `src\source\UI\NewUI\NewUIMuHelper.cpp`
- `src\source\UI\NewUI\NewUIMuHelper.h`
- `src\source\Engine\Object\ZzzInterface.cpp`
- `src\source\Network\Server\WSclient.cpp`

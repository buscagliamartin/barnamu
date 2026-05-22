# MuHelper Refactor ChangeLog
# Files Root Directory: "C:\MuDev\MuMain\"

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

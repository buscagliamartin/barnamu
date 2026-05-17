-- BarnaMu — actualización de niveles de entrada (Normal) por mapa
-- ============================================================================
-- Aplica la tabla nueva acordada:
--
--   Mapa                 | Lvl Normal | Lvl VIP*
--   ---------------------|-----------:|---------:
--   Kanturu Ruins        |        160 |     130
--   Karutan 1 / 2        |        200 |     160
--   Kanturu Relics       |        240 |     200
--   Raklion              |        280 |     240
--   Vulcanus             |        300 |     260
--   Crywolf Fortress     |        320 |     280
--   Barracks of Balgass  |        350 |     300
--   Balgass Refuge       |        350 |     300
--   Swamp of Calmness    |        350 |     300
--
-- * El valor VIP vive HARDCODED en WarpAction.cs / WarpGateAction.cs (server-side),
--   ya fue actualizado en código. Acá sólo tocamos el Normal en DB.
--
-- IDEMPOTENTE: se puede correr varias veces, sólo actualiza si el valor difiere.
-- ============================================================================

BEGIN;

-- 1) M menu (WarpInfo) — busca por Name. Estos son los nombres canónicos
--    de OpenMU; si Martin los renombró en Admin Panel, ajustar el WHERE.
UPDATE config."WarpInfo" SET "LevelRequirement" = 160 WHERE "Name" = 'Kanturu_1';
UPDATE config."WarpInfo" SET "LevelRequirement" = 200 WHERE "Name" = 'Karutan_1';
UPDATE config."WarpInfo" SET "LevelRequirement" = 200 WHERE "Name" = 'Karutan_2';
UPDATE config."WarpInfo" SET "LevelRequirement" = 240 WHERE "Name" = 'Kanturu_2';
UPDATE config."WarpInfo" SET "LevelRequirement" = 280 WHERE "Name" = 'Raklion';
UPDATE config."WarpInfo" SET "LevelRequirement" = 300 WHERE "Name" = 'Vulcanus';
UPDATE config."WarpInfo" SET "LevelRequirement" = 320 WHERE "Name" = 'Crywolf';
UPDATE config."WarpInfo" SET "LevelRequirement" = 350 WHERE "Name" IN ('Swamp', 'Swamp_of_Calmness');
-- Barracks/Refuge usually no son warpables vía M; quedan vía EnterGate.

-- 2) EnterGate (portales in-map) — los normal requirements para entrar caminando.
--    Aplicamos por TargetMap.Number. El JOIN navega EnterGate.TargetGate → ExitGate → Map.
UPDATE config."EnterGate" eg
   SET "LevelRequirement" = 160
  FROM config."ExitGate" target
 WHERE eg."TargetGateId" = target."Id"
   AND target."MapId" IN (SELECT "Id" FROM config."GameMapDefinition" WHERE "Number" = 37);  -- Kanturu Ruins

UPDATE config."EnterGate" eg
   SET "LevelRequirement" = 240
  FROM config."ExitGate" target
 WHERE eg."TargetGateId" = target."Id"
   AND target."MapId" IN (SELECT "Id" FROM config."GameMapDefinition" WHERE "Number" = 38);  -- Kanturu Relics

UPDATE config."EnterGate" eg
   SET "LevelRequirement" = 200
  FROM config."ExitGate" target
 WHERE eg."TargetGateId" = target."Id"
   AND target."MapId" IN (SELECT "Id" FROM config."GameMapDefinition" WHERE "Number" IN (80, 81));  -- Karutan 1/2

UPDATE config."EnterGate" eg
   SET "LevelRequirement" = 280
  FROM config."ExitGate" target
 WHERE eg."TargetGateId" = target."Id"
   AND target."MapId" IN (SELECT "Id" FROM config."GameMapDefinition" WHERE "Number" = 57);  -- Raklion

UPDATE config."EnterGate" eg
   SET "LevelRequirement" = 300
  FROM config."ExitGate" target
 WHERE eg."TargetGateId" = target."Id"
   AND target."MapId" IN (SELECT "Id" FROM config."GameMapDefinition" WHERE "Number" = 63);  -- Vulcanus

UPDATE config."EnterGate" eg
   SET "LevelRequirement" = 320
  FROM config."ExitGate" target
 WHERE eg."TargetGateId" = target."Id"
   AND target."MapId" IN (SELECT "Id" FROM config."GameMapDefinition" WHERE "Number" = 34);  -- Crywolf

UPDATE config."EnterGate" eg
   SET "LevelRequirement" = 350
  FROM config."ExitGate" target
 WHERE eg."TargetGateId" = target."Id"
   AND target."MapId" IN (SELECT "Id" FROM config."GameMapDefinition" WHERE "Number" IN (41, 42, 56));  -- Barracks, Balgass Refuge, Swamp

-- ============================================================================
-- VERIFICACIÓN: deberían aparecer los niveles nuevos para los mapas tocados.
-- ============================================================================
SELECT 'M menu (WarpInfo)' AS source,
       w."Name",
       w."LevelRequirement"
FROM config."WarpInfo" w
WHERE w."Name" IN ('Kanturu_1', 'Kanturu_2', 'Karutan_1', 'Karutan_2',
                   'Raklion', 'Vulcanus', 'Crywolf', 'Swamp', 'Swamp_of_Calmness')
ORDER BY w."LevelRequirement";

SELECT 'EnterGate (portales)' AS source,
       gmd."Number" AS map_number,
       gmd."Name"   AS map_name,
       MIN(eg."LevelRequirement") AS min_required,
       MAX(eg."LevelRequirement") AS max_required,
       COUNT(*)     AS gate_count
FROM config."EnterGate" eg
JOIN config."ExitGate" target ON target."Id" = eg."TargetGateId"
JOIN config."GameMapDefinition" gmd ON gmd."Id" = target."MapId"
WHERE gmd."Number" IN (34, 37, 38, 41, 42, 56, 57, 63, 80, 81)
GROUP BY gmd."Number", gmd."Name"
ORDER BY gmd."Number";

COMMIT;

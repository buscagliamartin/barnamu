-- BarnaMu — Fix Flame of Condor drops
-- ============================================================================
-- In the OpenMU source code, Flame of Condor (Group 13, Number 52) is defined
-- with DropsFromMonsters = true and a DropItemGroup attached to "Barracks of
-- Balgass" (map 41) with Chance 0.001 from any monster on that map (Balram,
-- Death Spirit, Soram).
--
-- Martin reports that in the live DB the item has DropsFromMonsters = false,
-- which prevents it from ever dropping regardless of the DropItemGroup
-- configuration. This script:
--   1) Flips DropsFromMonsters back to true.
--   2) Shows the DropItemGroup wiring so we can verify the global drop config
--      is correct after the fix.
-- ============================================================================

BEGIN;

UPDATE config."ItemDefinition"
   SET "DropsFromMonsters" = true
 WHERE "Group" = 13 AND "Number" = 52;

-- Verification: should print 1 row with DropsFromMonsters = true.
SELECT "Name", "Group", "Number", "DropsFromMonsters", "DropLevel"
FROM config."ItemDefinition"
WHERE "Group" = 13 AND "Number" = 52;

-- Verification: should print at least one DropItemGroup that contains Flame of
-- Condor. By default OpenMU attaches one to Barracks of Balgass (map 41) with
-- Chance 0.001 (~0.1% per kill from Balram / Death Spirit / Soram).
SELECT dig."Description",
       dig."Chance",
       dig."MinimumMonsterLevel",
       dig."MaximumMonsterLevel",
       COUNT(rel."GameMapDefinitionId") AS attached_maps
FROM config."DropItemGroup" dig
JOIN config."DropItemGroupItemDefinition" j ON j."DropItemGroupId" = dig."Id"
JOIN config."ItemDefinition" item ON item."Id" = j."ItemDefinitionId"
LEFT JOIN config."GameMapDefinitionDropItemGroup" rel ON rel."DropItemGroupId" = dig."Id"
WHERE item."Group" = 13 AND item."Number" = 52
GROUP BY dig."Id", dig."Description", dig."Chance", dig."MinimumMonsterLevel", dig."MaximumMonsterLevel"
ORDER BY dig."Description";

COMMIT;

-- If you want the drop chance higher than 0.1% (e.g. 1% or 2%), run:
-- UPDATE config."DropItemGroup" SET "Chance" = 0.01
--  WHERE "Description" = 'Flame of Condor';

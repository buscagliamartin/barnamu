-- BarnaMu — FIX URGENTE para BARNAMU_TIER_ITEMS_DROPS.sql
-- ============================================================================
-- Bug: en el SQL original puse ItemType = 1 pensando que era SpecialItemType.RandomItem,
-- pero el enum es:
--    0 = None
--    1 = Ancient        <-- lo que estaba YO usando por error
--    2 = Excellent
--    3 = RandomItem     <-- lo que CORRESPONDE
--    4 = SocketItem
--    5 = Money
--    6 = Jewel
--
-- Por eso al matar mobs en Devias estaban cayendo items ancient con 0.5% chance
-- desde mobs lvl 20+ (el drop generator tomaba PossibleItems=[BoxOfLuck] y el
-- ItemType=Ancient le decía "transformá este item en ancient", pero como Box of
-- Luck no tiene variante ancient, el generator caía a su pool de ancients y
-- tiraba ancients random de equipamiento).
--
-- Fix: UPDATE las 6 filas a ItemType = 3. No hay que recrear nada.
-- ============================================================================

BEGIN;

-- Mostrar antes del fix (para que veas qué hay)
SELECT "Description", "ItemType" AS itemtype_before
FROM config."DropItemGroup"
WHERE "Description" LIKE 'BarnaMu Tier %'
ORDER BY "ItemLevel";

UPDATE config."DropItemGroup"
   SET "ItemType" = 3  /* SpecialItemType.RandomItem */
 WHERE "Description" LIKE 'BarnaMu Tier %'
   AND "ItemType" = 1;

-- Confirmación: las 6 filas ahora deberían tener ItemType = 3
SELECT "Description",
       "ItemType" AS itemtype_after,
       "Chance",
       "ItemLevel",
       "MinimumMonsterLevel"
FROM config."DropItemGroup"
WHERE "Description" LIKE 'BarnaMu Tier %'
ORDER BY "ItemLevel";

COMMIT;

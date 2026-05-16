-- BarnaMu — Items tier A-F (drop progresivo desde mobs)
-- ============================================================================
-- Crea 6 DropItemGroups globales que dropean el Box of Luck (Group 14,
-- Number 11) a niveles 1-6 según el nivel del mob:
--   +1 = Star of the Sacred Birth  (A) → mobs lvl 1+
--   +2 = Firecracker                (B) → mobs lvl 30+
--   +3 = Heart of Love              (C) → mobs lvl 60+
--   +4 = Olive of Love              (D) → mobs lvl 90+
--   +5 = Silver Medal               (E) → mobs lvl 120+
--   +6 = Gold Medal                 (F) → mobs lvl 150+
--
-- Chance 0.5% por kill por tier. En late-game (mobs lvl 150+) los 6 tiers son
-- eligibles → ~3% chance total por kill de que caiga algún tier. Es lo que
-- pediste, similar al ratio de jewels actuales.
--
-- IDEMPOTENTE: re-correr el script no duplica filas (chequea por Description).
-- Si querés desinstalar, mirá el bloque ROLLBACK al final.
--
-- ¡OJO! Esto sólo configura el DROP. El COMPORTAMIENTO al consumir
-- (qué te tira al hacer doble-click en el Star, Firecracker, etc.) sigue
-- usando la config de vanilla OpenMU por ahora. Eso se reconfigura en una
-- segunda pasada (próxima sesión) — ver BARNAMU_CONTEXT.md → Pendiente.
-- ============================================================================

BEGIN;

-- 1) Crear los 6 DropItemGroups (si no existen ya)
WITH cfg AS (SELECT "Id" FROM config."GameConfiguration" LIMIT 1)
INSERT INTO config."DropItemGroup" (
    "Id", "Chance", "Description", "GameConfigurationId",
    "ItemLevel", "ItemType", "MinimumMonsterLevel"
)
SELECT gen_random_uuid(),
       0.005,
       t.desc_v,
       cfg."Id",
       t.item_lvl,
       3, /* SpecialItemType.RandomItem — OJO: 1=Ancient, 2=Excellent, 3=RandomItem */
       t.min_lvl
FROM cfg, (VALUES
    ('BarnaMu Tier A — Star of Sacred Birth', 1::smallint,   1::smallint),
    ('BarnaMu Tier B — Firecracker',          2::smallint,  30::smallint),
    ('BarnaMu Tier C — Heart of Love',        3::smallint,  60::smallint),
    ('BarnaMu Tier D — Olive of Love',        4::smallint,  90::smallint),
    ('BarnaMu Tier E — Silver Medal',         5::smallint, 120::smallint),
    ('BarnaMu Tier F — Gold Medal',           6::smallint, 150::smallint)
) AS t(desc_v, item_lvl, min_lvl)
WHERE NOT EXISTS (
    SELECT 1 FROM config."DropItemGroup" dig
    WHERE dig."Description" = t.desc_v
);

-- 2) Linkear cada grupo con el ItemDefinition Box of Luck (Group 14, Number 11).
--    Como PossibleItems contiene un solo item, el drop es siempre el Box of Luck,
--    al nivel especificado por DropItemGroup.ItemLevel (que el cliente renderiza
--    con el nombre correspondiente: Star, Firecracker, etc.).
INSERT INTO config."DropItemGroupItemDefinition" ("DropItemGroupId", "ItemDefinitionId")
SELECT dig."Id", item."Id"
FROM config."DropItemGroup" dig
CROSS JOIN config."ItemDefinition" item
WHERE dig."Description" LIKE 'BarnaMu Tier %'
  AND item."Group" = 14
  AND item."Number" = 11
  AND NOT EXISTS (
      SELECT 1 FROM config."DropItemGroupItemDefinition" j
      WHERE j."DropItemGroupId" = dig."Id"
        AND j."ItemDefinitionId" = item."Id"
  );

-- 3) Attach a TODOS los mapas (drop global). Los filtros MinimumMonsterLevel
--    se encargan de que cada tier solo aplique a mobs de su nivel para arriba.
INSERT INTO config."GameMapDefinitionDropItemGroup" ("GameMapDefinitionId", "DropItemGroupId")
SELECT gmd."Id", dig."Id"
FROM config."GameMapDefinition" gmd
CROSS JOIN config."DropItemGroup" dig
WHERE dig."Description" LIKE 'BarnaMu Tier %'
  AND NOT EXISTS (
      SELECT 1 FROM config."GameMapDefinitionDropItemGroup" r
      WHERE r."GameMapDefinitionId" = gmd."Id"
        AND r."DropItemGroupId" = dig."Id"
  );

-- ============================================================================
-- VERIFICACIÓN: deberías ver 6 filas, una por tier, cada una attached a TODOS
-- los mapas (~30+ mapas en Season 6).
-- ============================================================================
SELECT dig."Description",
       dig."Chance",
       dig."ItemLevel"          AS lvl_dropped,
       dig."MinimumMonsterLevel" AS mob_min_lvl,
       COUNT(rel."GameMapDefinitionId") AS maps_attached
FROM config."DropItemGroup" dig
LEFT JOIN config."GameMapDefinitionDropItemGroup" rel
  ON rel."DropItemGroupId" = dig."Id"
WHERE dig."Description" LIKE 'BarnaMu Tier %'
GROUP BY dig."Id", dig."Description", dig."Chance", dig."ItemLevel", dig."MinimumMonsterLevel"
ORDER BY dig."ItemLevel";

COMMIT;


-- ============================================================================
-- ROLLBACK (si querés DESINSTALAR los tier items):
-- ============================================================================
-- BEGIN;
-- DELETE FROM config."GameMapDefinitionDropItemGroup"
--  WHERE "DropItemGroupId" IN (
--    SELECT "Id" FROM config."DropItemGroup" WHERE "Description" LIKE 'BarnaMu Tier %'
--  );
-- DELETE FROM config."DropItemGroupItemDefinition"
--  WHERE "DropItemGroupId" IN (
--    SELECT "Id" FROM config."DropItemGroup" WHERE "Description" LIKE 'BarnaMu Tier %'
--  );
-- DELETE FROM config."DropItemGroup" WHERE "Description" LIKE 'BarnaMu Tier %';
-- COMMIT;

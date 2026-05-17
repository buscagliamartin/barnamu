SELECT DISTINCT
    CAST(ma."Value" AS INTEGER) AS "Nivel",
    md."Designation" AS "Nombre_Mob",
    map."Name" AS "Mapa"
FROM config."MonsterDefinition" md
-- Unimos con los atributos para sacar el Nivel
JOIN config."MonsterAttribute" ma ON ma."MonsterDefinitionId" = md."Id"
JOIN config."AttributeDefinition" ad ON ad."Id" = ma."AttributeDefinitionId"
-- Unimos con los spawns y mapas
JOIN config."MonsterSpawnArea" msa ON msa."MonsterDefinitionId" = md."Id"
JOIN config."GameMapDefinition" map ON map."Id" = msa."GameMapId"
WHERE ad."Designation" = 'Level'  -- Buscamos el atributo nivel
  AND md."ObjectKind" = 0         -- Filtro corregido: Monstruos en tu DB
  AND ma."Value" > 0              -- Solo mobs con nivel real
ORDER BY "Nivel" ASC, map."Name" ASC;
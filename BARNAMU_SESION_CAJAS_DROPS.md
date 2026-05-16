# BarnaMu — Sesión: Cajas, Drops, Eventos y Dureza

> Checkpoint de sesión. Si se corta, retomar desde acá.
> Última actualización: 2026-05-14

## ✅ COMPLETADO

### Sistema de cajas Kundun / Heaven (Goldens)
- 6 cajas en **Box of Luck (Group 14, Number 11)** como `ItemDropItemGroup` tipo Excellent:
  SourceItemLevel 7=Heaven (DropLevel 0-22), 8=K+1 (23-48), 9=K+2 (49-62),
  10=K+3 (63-82), 11=K+4 (83-101), 12=K+5 (102-132).
- Cada caja contiene SOLO items excelente-capable de su rango de DropLevel.
- 9 Golden mobs asignados 1-a-1 a su caja vía `DropItemGroup` (chance=1, ItemLevel=7..12,
  PossibleItems=Box of Luck) Y attachados en `MonsterDefinitionDropItemGroup` (tabla join).
  Budge Dragon→Heaven, Goblin→K1, Soldier→K1, Titan→K2, Vepar→K2, Wheel→K3, Dragon→K3,
  Lizard King→K4, Tantallos→K5. FUNCIONA EN JUEGO.

### Sistema T9 (bosses)
- **Pink Chocolate Box (Group 14, Number 32)** = caja T9. `ItemDropItemGroup`
  "Pink Chocolate Box - T9 Excellent", SourceItemLevel=2, Excellent, items DropLevel 133-147.
- Selupan, Erohim, Dark Elf (Trainee Soldier) la tiran (chance=1, ItemLevel=2). FUNCIONA.
- Defecto visual: cliente renderiza distinto el nombre en piso vs inventario (item.bmd
  client-side). El usuario quería migrar a Red Chocolate Box pero quedó en Pink — funcional OK.

### Ancients
- Red Dragon → Blue Chocolate Box → ancient items. (Configurado por el usuario.)

### Dureza de mobs
- Goldens: HP 150k→15M escalado por tier, resistencias 0.35→0.65.
- Red Dragon: 20M HP, resistencias 0.75-0.9 (tier 7.5/8).
- Bosses T9 (Selupan/Erohim/Dark Elf): 25M HP, resistencias 0.8-0.95.
- Hallazgo clave: Red Dragon "flojo" era por resistencias elementales bajas (9%) vs
  Selupan (99%). Un mago derretía Red Dragon porque no resistía magia.

### Eventos / Invasiones
- Golden Invasion: plugin YA ACTIVO. Cada 4hs (00,04,08,12,16,20), 5min, anuncio en pantalla.
- Red Dragon Invasion: plugin YA ACTIVO. Cada 6hs (02,08,14,20), 10min, anuncio.
- T9 Boss Invasion: plugin NUEVO creado `T9BossInvasionPlugIn.cs` — cada 8hs, 30min,
  spawnea Selupan/Erohim/Dark Elf en sus mapas, con anuncio.
- Spawn counts ajustados para slow-medium en código:
  - Golden Invasion: 140 → 55 total (Budge Dragon 10, Goblin 8, Soldier 8, Titan 6,
    Vepar 6, Lizard King 4, Wheel 5, Tantallos 3, Golden Dragon 5).
  - Red Dragon Invasion: 5 → 3.

## ⏳ PENDIENTE — PRÓXIMOS PASOS INMEDIATOS

1. **Usuario debe recompilar** (`Recompilar.bat`) y reiniciar server — para que tomen:
   - El plugin nuevo `T9BossInvasionPlugIn.cs`
   - Los nuevos spawn counts de Golden/Red Dragon.
2. **Activar el plugin T9** en Admin Panel → PlugIns → "T9 Boss Invasion".
3. **Correr SQL `BARNAMU_FASE4B_FIX_RESIST.sql`** — inserta resistencias faltantes
   (Vepar Fire/Ice/Poison, Erohim+DarkElf Water). Aún sin correr.
4. **Correr SQL `BARNAMU_FASE4C_REMOVE_SPAWNS.sql`** — SOLO después de que el plugin T9
   esté activo. Borra los spawns permanentes de los 3 bosses T9.

## 📋 PENDIENTE — TAREAS FUTURAS

### >>> PRIORIDAD: PREPARAR EL SERVER PARA IR PÚBLICO <<<
El usuario priorizó, en orden:
  1º) **Página web del servidor**
  2º) **VIP con timer** (expira en N días)
  3º) Después: bugs, implementaciones y extras (la lista numerada de abajo)
Próxima sesión: empezar por VIP timer (mejor acotado, server-side, continúa el momentum)
o página web si el usuario prefiere. Definir requisitos primero en cualquier caso.

### Lista completa (orden original del usuario)
1. **VIP con timer** — expira en N días automáticamente. (código + DB)
2. **Config cajas de drop por tier de mapa** — items deben superponerse de mayor a menor
   (si item A cae en Lorencia/Noria/Elvenland, en Devias cae item B + posibilidad de A):
   - a) Star of sacred birth → random item +4 con chance de +luck y/o +option
   - b) Firecracker → random item +6 con chance de +luck y/o +option
   - c) Heart of love → random item +7 con chance de +luck y/o +option
   - d) Olive of love → jewel
   - e) Silver medal → jewel
   - f) Gold medal → jewel
3. **Chat VIP** — usuario común habla → aparece comentario sobre el pj. VIP habla → NO
   aparece sobre el pj, solo en la barra de chat izquierda.
4. **`/pkclear`** — quitar el argumento usuario (que sea solo `/pkclear`) y que cobre zen.
5. **Equipar/desequipar items con click derecho.**
6. Balance de clases (BK, DW, SM, Elf, MG, DL, Summoner, RF).
7. Página web del servidor.
8. Discord del servidor.
9. Cliente final para distribución pública.
10. Sistema de ranking.
11. Castle Siege — configuración.
12. Vulcanus — entender sistema Gens antes de activar.
13. Mapa exclusivo VIP — evaluar opciones.

### Pendientes técnicos sueltos de esta sesión
- Posible `/startinvasion` GM command (no built-in; requiere tocar clases base).

## 🔑 GOTCHAS / DATOS ÚTILES
- **El runtime SOLO lee drop groups vía `MonsterDefinitionDropItemGroup` (tabla join).**
  `DropItemGroup.MonsterId` es solo filtro redundante. NO tocar drop groups de mobs por
  Admin Panel — siempre por SQL.
- `/item` ancient: 8° arg posicional es `anc` (0/1/2). Ej: `/item 9 3 15 63 1 1 7 1`.
  Solo aplica si el item tiene ItemSetGroup ancient configurado en DB.
- Comando `/item`: group number lvl ex sk lu opt anc ancBonuslvl.
- Excellent Option TypeId: `6487C498-58E0-48E5-B409-35D7598313FC`
- GUIDs plugins: Golden `06D18A9E-2919-4C17-9DBC-6E4F7756495C`,
  RedDragon `548A76CC-242C-441C-BC9D-6C22745A2D72`,
  T9Boss (nuevo) `B7E3A1C4-9F22-4D6E-A8B1-3C5D7E9F0A12`.
- Resistencias se guardan como fracción de 255 (1.0 = inmune, 0.5 = recibe mitad).
- Monster numbers: Selupan 459, Erohim 295, Dark Elf 412, Red Dragon 44.
- Map numbers: LandOfTrials 31, BalgassRefuge 42, RaklionBoss 58.
- Scripts SQL de la sesión: C:\MuDev\BARNAMU_CAJAS_*.sql y BARNAMU_EVENTOS/FASE4*.sql
- Plugin nuevo: C:\MuDev\OpenMU\src\GameLogic\PlugIns\InvasionEvents\T9BossInvasionPlugIn.cs

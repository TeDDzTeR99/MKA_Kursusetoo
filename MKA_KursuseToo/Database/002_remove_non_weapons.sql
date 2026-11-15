-- Migration 002: remove entries that are not weapons in Elden Ring
-- (checked against the game's regulation data, patch 1.17)
--   Shard of Alexander  - a talisman
--   Whetstone Knife     - a key item
--   Dex Curved Sword, Banded Sword, Hammer of the Forge, Ladle - do not exist in the game
--   Glintblade Staff, Comet Azur Staff, Dragon Cult Seal, Great Scythe, Crossbow, Reaper Ballista
--     - wrong names for weapons that are already in the table under their real names
--       (Carian Glintblade Staff, Azur's Glintstone Staff, Dragon Communion Seal,
--        Grave Scythe, Light Crossbow, Hand Ballista), so they are duplicates
--
-- Foreign keys are not enforced in this database, so related rows are deleted explicitly.

BEGIN TRANSACTION;

CREATE TEMP TABLE removed_weapons AS
SELECT id FROM weapons
WHERE name IN ('Shard of Alexander', 'Whetstone Knife', 'Dex Curved Sword',
               'Banded Sword', 'Hammer of the Forge', 'Ladle',
               'Glintblade Staff', 'Comet Azur Staff', 'Dragon Cult Seal',
               'Great Scythe', 'Crossbow', 'Reaper Ballista');

DELETE FROM weapon_scaling      WHERE weapon_id IN (SELECT id FROM removed_weapons);
DELETE FROM weapon_requirements WHERE weapon_id IN (SELECT id FROM removed_weapons);
DELETE FROM weapon_damage_types WHERE weapon_id IN (SELECT id FROM removed_weapons);
DELETE FROM weapons             WHERE id        IN (SELECT id FROM removed_weapons);

DROP TABLE removed_weapons;

COMMIT;

-- Migration 007: status effect buildup per affinity and upgrade level
--
-- Before: weapons.passive_effect held text like "Hemorrhage (50)" for the Standard affinity only,
--         so e.g. a Blood Dagger showed no bleed at all.
-- After:  weapon_damage has one buildup column per status effect, per (weapon, affinity, level),
--         filled from the game data by import_from_game_data.py.
--         weapons.passive_effect is renamed to special_effect and keeps only effects that are not
--         status buildup (e.g. "Boosts Glintblade Sorceries (15%)" on staffs and seals).

BEGIN TRANSACTION;

-- 1. Buildup columns (0 = no buildup)
ALTER TABLE weapon_damage ADD COLUMN poison_buildup       INTEGER NOT NULL DEFAULT 0;
ALTER TABLE weapon_damage ADD COLUMN scarlet_rot_buildup  INTEGER NOT NULL DEFAULT 0;
ALTER TABLE weapon_damage ADD COLUMN bleed_buildup        INTEGER NOT NULL DEFAULT 0;
ALTER TABLE weapon_damage ADD COLUMN frost_buildup        INTEGER NOT NULL DEFAULT 0;
ALTER TABLE weapon_damage ADD COLUMN sleep_buildup        INTEGER NOT NULL DEFAULT 0;
ALTER TABLE weapon_damage ADD COLUMN madness_buildup      INTEGER NOT NULL DEFAULT 0;
ALTER TABLE weapon_damage ADD COLUMN death_blight_buildup INTEGER NOT NULL DEFAULT 0;

-- 2. passive_effect -> special_effect (SQLite also updates v_weapons_overview)
ALTER TABLE weapons RENAME COLUMN passive_effect TO special_effect;

-- Status buildup now comes from weapon_damage, so remove it from the text column
UPDATE weapons SET special_effect = NULL
WHERE special_effect LIKE 'Hemorrhage (%)'
   OR special_effect LIKE 'Poison (%)'
   OR special_effect LIKE 'Scarlet Rot (%)'
   OR special_effect LIKE 'Frostbite (%)'
   OR special_effect LIKE 'Sleep (%)'
   OR special_effect LIKE 'Madness (%)'
   OR special_effect LIKE 'Death Blight (%)';

-- These two texts also described a buildup that is now in weapon_damage; keep only the special part
UPDATE weapons SET special_effect = 'Boosts Frenzied Flame Incantations (+20%)'
WHERE name = 'Frenzied Flame Seal';
UPDATE weapons SET special_effect = 'Boosts Crystalian Sorceries (10%)'
WHERE name = 'Rotten Crystal Staff';

COMMIT;

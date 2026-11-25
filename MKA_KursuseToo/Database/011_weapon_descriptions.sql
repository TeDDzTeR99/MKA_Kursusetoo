-- Migration 011: weapon descriptions
--
-- The in-game description of each weapon, filled by import_weapon_descriptions.py
-- from the WeaponName / WeaponCaption text files (matched by name).

BEGIN TRANSACTION;

ALTER TABLE weapons ADD COLUMN description TEXT;

COMMIT;

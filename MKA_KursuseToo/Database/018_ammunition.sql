-- Migration 018: arrows and bolts
--
-- Filled by import_ammunition.py from the game's own data:
--   - EquipParamWeapon (wepType 81 / 83 / 85 / 86 = arrow, great arrow, bolt, greatbolt)
--   - the WeaponName / WeaponCaption text files

BEGIN TRANSACTION;

CREATE TABLE ammunition (
    id                INTEGER PRIMARY KEY,   -- row ID in EquipParamWeapon
    name              TEXT    NOT NULL,
    category          TEXT    NOT NULL,      -- 'arrow', 'great_arrow', 'bolt' or 'greatbolt', via AmmoKind_<key>
    description       TEXT,
    physical_damage   INTEGER NOT NULL,
    magic_damage      INTEGER NOT NULL,
    fire_damage       INTEGER NOT NULL,
    lightning_damage  INTEGER NOT NULL,
    holy_damage       INTEGER NOT NULL,
    max_quantity      INTEGER NOT NULL,      -- how many can be carried in the quiver slot
    sell_value        INTEGER,
    sort_id           INTEGER NOT NULL,
    is_dlc            INTEGER NOT NULL DEFAULT 0
);

COMMIT;

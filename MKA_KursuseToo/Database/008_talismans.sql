-- Migration 008: talismans
--
-- One row per talisman, filled by import_talismans.py from the game's own data:
--   - EquipParamAccessory (weight, sell value, accessory group)
--   - the AccessoryName / AccessoryInfo / AccessoryCaption text files (name, effect, description)
-- Upgraded versions (e.g. "Erdtree's Favor +1") are separate talismans in the game, so they are separate rows.
-- Where to find each talisman is not in the game data and is left out for now.

BEGIN TRANSACTION;

CREATE TABLE talismans (
    id               INTEGER PRIMARY KEY,           -- row ID in EquipParamAccessory, so re-imports keep the same IDs
    name             TEXT    NOT NULL,
    effect           TEXT,                          -- short effect text from the game, e.g. "Raises maximum HP"
    description      TEXT,                          -- longer in-game description
    weight           REAL    NOT NULL DEFAULT 0,
    sell_value       INTEGER,
    accessory_group  INTEGER,                       -- talismans in the same group can't be equipped together
    is_dlc           INTEGER NOT NULL DEFAULT 0     -- 1 = Shadow of the Erdtree
);

CREATE INDEX idx_talismans_name ON talismans(name);

COMMIT;

-- Migration 016: armor
--
-- Filled by import_armor.py from the game's own data:
--   - EquipParamProtector (protectorCategory gives the slot; damage cut rates, resistances, poise, weight)
--   - the ProtectorName / ProtectorInfo / ProtectorCaption text files

BEGIN TRANSACTION;

CREATE TABLE armor (
    id           INTEGER PRIMARY KEY,   -- row ID in EquipParamProtector
    name         TEXT    NOT NULL,
    category     TEXT    NOT NULL,      -- 'head', 'chest', 'arms' or 'legs', shown via the ArmorKind_<key> resource text
    effect       TEXT,                  -- short text from the game
    description  TEXT,
    weight       REAL    NOT NULL,
    poise        INTEGER NOT NULL,
    -- Damage negation as the game menu shows it: (1 - cut rate) * 100, e.g. 6.8
    physical     REAL    NOT NULL,
    strike       REAL    NOT NULL,
    slash        REAL    NOT NULL,
    pierce       REAL    NOT NULL,
    magic        REAL    NOT NULL,
    fire         REAL    NOT NULL,
    lightning    REAL    NOT NULL,
    holy         REAL    NOT NULL,
    -- Resistances
    immunity     INTEGER NOT NULL,      -- poison and scarlet rot
    robustness   INTEGER NOT NULL,      -- hemorrhage and frostbite
    focus        INTEGER NOT NULL,      -- sleep and madness
    vitality     INTEGER NOT NULL,      -- death blight
    sell_value   INTEGER,
    sort_id      INTEGER NOT NULL,      -- the game's inventory order
    is_dlc       INTEGER NOT NULL DEFAULT 0
);

COMMIT;

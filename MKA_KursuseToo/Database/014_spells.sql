-- Migration 014: sorceries and incantations
--
-- Filled by import_spells.py from the game's own data:
--   - EquipParamGoods (goodsType 5 / 17 = sorcery, 16 / 18 = incantation; sortId gives the school)
--   - Magic (FP and stamina cost, memory slots, stat requirements; same row ID as the goods row)
--   - the GoodsName / GoodsInfo / GoodsCaption text files

BEGIN TRANSACTION;

CREATE TABLE spells (
    id                    INTEGER PRIMARY KEY,   -- row ID in EquipParamGoods and Magic
    name                  TEXT    NOT NULL,
    type                  TEXT    NOT NULL,      -- 'sorcery' or 'incantation'
    category              TEXT    NOT NULL,      -- school, e.g. 'carian', shown via the SpellSchool_<key> resource text
    effect                TEXT,                  -- short text from the game
    description           TEXT,                  -- longer in-game description
    fp                    INTEGER NOT NULL,
    stamina               INTEGER NOT NULL,
    slots                 INTEGER NOT NULL,      -- memory slots it takes
    required_intelligence INTEGER NOT NULL,
    required_faith        INTEGER NOT NULL,
    required_arcane       INTEGER NOT NULL,
    sort_id               INTEGER NOT NULL,      -- the game's inventory order
    is_dlc                INTEGER NOT NULL DEFAULT 0
);

-- The app shows sorceries and incantations as two categories
CREATE VIEW sorceries AS SELECT * FROM spells WHERE type = 'sorcery';
CREATE VIEW incantations AS SELECT * FROM spells WHERE type = 'incantation';

COMMIT;

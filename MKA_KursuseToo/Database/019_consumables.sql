-- Migration 019: consumables
--
-- Filled by import_consumables.py from the game's own data:
--   - EquipParamGoods (goodsType 0 = normal item, 9 = Flask of Wondrous Physick)
--   - the GoodsName / GoodsInfo / GoodsCaption text files

BEGIN TRANSACTION;

CREATE TABLE consumables (
    id           INTEGER PRIMARY KEY,   -- row ID in EquipParamGoods
    name         TEXT    NOT NULL,
    category     TEXT    NOT NULL,      -- e.g. 'pot', 'grease', shown via the ConsumableKind_<key> resource text
    effect       TEXT,                  -- short text from the game
    description  TEXT,
    fp           INTEGER,               -- FP cost; NULL = uses no FP
    max_quantity INTEGER NOT NULL,      -- how many can be carried
    max_stored   INTEGER,               -- NULL = can't be stored in the chest
    sell_value   INTEGER,               -- NULL = can't be sold
    sort_id      INTEGER NOT NULL,
    is_dlc       INTEGER NOT NULL DEFAULT 0
);

COMMIT;

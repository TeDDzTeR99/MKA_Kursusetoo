-- Migration 020: crafting materials
--
-- Filled by import_crafting_materials.py from the game's own data:
--   - EquipParamGoods (goodsType 2 = crafting material)
--   - the GoodsName / GoodsCaption text files

BEGIN TRANSACTION;

CREATE TABLE crafting_materials (
    id           INTEGER PRIMARY KEY,   -- row ID in EquipParamGoods
    name         TEXT    NOT NULL,
    category     TEXT    NOT NULL,      -- 'fauna', 'flora' or 'mineral', via CraftingMaterialKind_<key>
    description  TEXT,
    max_quantity INTEGER NOT NULL,      -- how many can be carried
    max_stored   INTEGER,               -- NULL = can't be stored in the chest
    sell_value   INTEGER,               -- NULL = can't be sold
    sort_id      INTEGER NOT NULL,
    is_dlc       INTEGER NOT NULL DEFAULT 0
);

COMMIT;

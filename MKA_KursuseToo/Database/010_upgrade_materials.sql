-- Migration 010: upgrade materials
--
-- Filled by import_upgrade_materials.py from the game's own data:
--   - EquipParamGoods (goodsType 14 = upgrade material: smithing stones, gloveworts, ...)
--   - EquipMtrlSetParam (how many of which material each upgrade level costs)
--   - the GoodsName / GoodsInfo / GoodsCaption text files

BEGIN TRANSACTION;

CREATE TABLE upgrade_materials (
    id           INTEGER PRIMARY KEY,           -- row ID in EquipParamGoods
    name         TEXT    NOT NULL,
    effect       TEXT,                          -- short text from the game
    description  TEXT,                          -- longer in-game description
    sell_value   INTEGER,
    sort_id      INTEGER,                       -- the game's inventory order
    category     TEXT    NOT NULL,              -- e.g. 'smithing_stone', shown via the UpgradeCategory_<key> resource text
    is_dlc       INTEGER NOT NULL DEFAULT 0     -- 1 = Shadow of the Erdtree
);

-- What a material is used for, e.g. Smithing Stone [1]: weapon +1 needs 2, +2 needs 4, +3 needs 6
CREATE TABLE upgrade_material_costs (
    material_id   INTEGER NOT NULL REFERENCES upgrade_materials(id),
    item_kind     TEXT    NOT NULL,             -- 'weapon' (regular), 'somber_weapon' or 'spirit_ash'
    target_level  INTEGER NOT NULL,             -- the level the item is upgraded TO, e.g. 1 for +0 -> +1
    amount        INTEGER NOT NULL,
    PRIMARY KEY (material_id, item_kind, target_level)
);

COMMIT;

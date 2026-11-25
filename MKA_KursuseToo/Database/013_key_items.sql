-- Migration 013: key items
--
-- Filled by import_key_items.py from the game's own data:
--   - EquipParamGoods (goodsType 1 = key item, 11 = pot / perfume bottle, also in the game's Key Items tab)
--   - the GoodsName / GoodsInfo / GoodsCaption text files

BEGIN TRANSACTION;

CREATE TABLE key_items (
    id           INTEGER PRIMARY KEY,           -- row ID in EquipParamGoods
    name         TEXT    NOT NULL,              -- not unique: e.g. Unalloyed Gold Needle has a row per quest stage
    effect       TEXT,                          -- short text from the game
    description  TEXT,                          -- longer in-game description
    sell_value   INTEGER,
    sort_id      INTEGER NOT NULL,              -- the game's inventory order
    category     TEXT    NOT NULL,              -- e.g. 'bell_bearing', shown via the KeyItemCategory_<key> resource text
    is_dlc       INTEGER NOT NULL DEFAULT 0     -- 1 = Shadow of the Erdtree
);

-- What a bell bearing adds to the Twin Maiden Husks' shop (ShopLineupParam), or the recipes a cookbook
-- teaches (ShopLineupParam_Recipe), e.g. Smithing-Stone Miner's Bell Bearing [1]: Smithing Stone [1] for 200 runes
CREATE TABLE key_item_unlocks (
    key_item_id  INTEGER NOT NULL REFERENCES key_items(id),
    position     INTEGER NOT NULL,              -- order in the shop
    name         TEXT    NOT NULL,
    price        INTEGER,                       -- runes; NULL for recipes
    PRIMARY KEY (key_item_id, position)
);

COMMIT;

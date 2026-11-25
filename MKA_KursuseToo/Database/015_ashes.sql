-- Migration 015: ashes of war and spirit ashes
--
-- Filled by import_ashes.py from the game's own data:
--   - EquipParamGem (ashes of war: affinities, weapon types they fit; sortId gives the kind)
--   - SwordArtsParam (the skill's FP cost)
--   - EquipParamGoods (spirit ashes: goodsType 7 / 8, one row per upgrade level; summon FP / HP cost)
--   - the GemName / GemCaption and GoodsName / GoodsInfo / GoodsCaption text files

BEGIN TRANSACTION;

CREATE TABLE ashes_of_war (
    id               INTEGER PRIMARY KEY,   -- row ID in EquipParamGem
    name             TEXT    NOT NULL,      -- as in the game, e.g. "Ash of War: Lion's Claw"
    category         TEXT    NOT NULL,      -- e.g. 'keen', 'bow', shown via the AshOfWarKind_<key> resource text
    affinity         TEXT    NOT NULL,      -- default affinity, e.g. 'Heavy'
    affinities       TEXT    NOT NULL,      -- every affinity it can give, e.g. 'Standard, Heavy, Keen'
    weapon_types     TEXT    NOT NULL,      -- every weapon type it fits, e.g. 'Dagger, Straight Sword'
    fp               INTEGER,               -- skill FP cost (the highest of its parts); NULL = none
    description      TEXT,
    sort_id          INTEGER NOT NULL,      -- the game's inventory order
    is_dlc           INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE spirit_ashes (
    id               INTEGER PRIMARY KEY,   -- row ID in EquipParamGoods of the +0 version
    name             TEXT    NOT NULL,
    category         TEXT    NOT NULL,      -- 'grave_glovewort' or 'ghost_glovewort' (upgrade material),
                                            -- shown via the SpiritAshKind_<key> resource text
    effect           TEXT,
    description      TEXT,
    fp               INTEGER,               -- summoning cost; NULL = none
    hp               INTEGER,               -- some cost HP instead; NULL = none
    max_level        INTEGER NOT NULL,      -- e.g. 10 for +10
    sort_id          INTEGER NOT NULL,
    is_dlc           INTEGER NOT NULL DEFAULT 0
);

COMMIT;

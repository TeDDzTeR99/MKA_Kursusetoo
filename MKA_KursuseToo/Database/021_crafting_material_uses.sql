-- Migration 021: what each crafting material is used to craft
--
-- Filled by import_crafting_materials.py from ShopLineupParam_Recipe (the crafting recipes) and
-- EquipMtrlSetParam (the materials and amounts each recipe needs).

BEGIN TRANSACTION;

CREATE TABLE crafting_material_uses (
    material_id  INTEGER NOT NULL REFERENCES crafting_materials(id),
    position     INTEGER NOT NULL,      -- recipe order in the crafting menu
    item_name    TEXT    NOT NULL,      -- what is crafted, e.g. 'Fire Pot'
    amount       INTEGER NOT NULL,      -- how many of the material one craft needs
    PRIMARY KEY (material_id, position)
);

COMMIT;

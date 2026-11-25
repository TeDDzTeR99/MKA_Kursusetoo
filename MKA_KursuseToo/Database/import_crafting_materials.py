"""
Imports crafting materials from the game's own data into elden_ring_weapons.db (tables `crafting_materials` and
`crafting_material_uses`, see 020_crafting_materials.sql and 021_crafting_material_uses.sql).

Inputs, exported from your own copy of Elden Ring (see import_talismans.py for how):
  1. EquipParamGoods as CSV. Crafting materials are goodsType 2. The kind comes from the game's inventory order
     (sortId // 1000, see KINDS).
       - carried: maxNum, stored: maxRepositoryNum, sell value: sellValue
  2. The folder with the text files as XML: GoodsName*, GoodsCaption*, WeaponName*.fmg.xml
  3. ShopLineupParam_Recipe as CSV: the crafting recipes. mtrlId points at the recipe's row in
  4. EquipMtrlSetParam as CSV: up to six materials (materialId01-06) with their amounts (itemNum01-06).

All existing rows are replaced, so the script can be run again after a game patch.

Usage:
    python import_crafting_materials.py <EquipParamGoods.csv> <text folder> <ShopLineupParam_Recipe.csv>
                                         <EquipMtrlSetParam.csv> <path to elden_ring_weapons.db>
"""
import sqlite3
import sys

from import_consumables import positive
from import_key_items import EQUIP_NAMES
from import_talismans import number, read_param, read_texts

CRAFTING_MATERIAL = "2"                               # goodsType
KINDS = {100: "fauna", 101: "flora", 102: "mineral"}  # sortId // 1000 -> kind


def material_uses(materials, recipe_csv, material_set_csv, text_folder):
    """(material id, position, crafted item name, amount) for every recipe that needs one of the materials."""
    recipes = read_param(recipe_csv)
    sets = {number(row["id"], int): row for row in read_param(material_set_csv)}
    for column in ("equipid", "equiptype", "mtrlid"):
        if not recipes or column not in recipes[0]:
            sys.exit(f"{recipe_csv} has no {column} column; is it a ShopLineupParam_Recipe CSV export?")
    if not sets or "materialid01" not in next(iter(sets.values())):
        sys.exit(f"{material_set_csv} has no materialId01 column; is it an EquipMtrlSetParam CSV export?")
    texts = {kind: read_texts(text_folder, prefix)[0] for kind, prefix in EQUIP_NAMES.items()}

    uses, positions = [], {}
    for recipe in sorted(recipes, key=lambda r: number(r["id"], int)):
        material_set = sets.get(number(recipe["mtrlid"], int))
        name = texts.get(recipe["equiptype"], {}).get(number(recipe["equipid"], int))
        if material_set is None or name is None:   # e.g. the empty row 1
            continue
        for slot in range(1, 7):
            material_id = number(material_set[f"materialid0{slot}"], int)
            if material_id in materials:
                positions[material_id] = positions.get(material_id, 0) + 1
                uses.append((material_id, positions[material_id], name,
                             number(material_set[f"itemnum0{slot}"], int)))
    return uses


def main(goods_csv, text_folder, recipe_csv, material_set_csv, db_path):
    names, base_name_ids, name_files = read_texts(text_folder, "GoodsName")
    descriptions, _, _ = read_texts(text_folder, "GoodsCaption")
    if not name_files:
        sys.exit(f"No GoodsName*.fmg.xml files found under {text_folder}")

    goods = read_param(goods_csv)
    for column in ("goodstype", "sortid", "maxnum", "maxrepositorynum", "sellvalue"):
        if not goods or column not in goods[0]:
            sys.exit(f"{goods_csv} has no {column} column; is it an EquipParamGoods CSV export?")

    items = []
    for row in goods:
        item_id = number(row["id"], int)
        sort_id = number(row["sortid"], int)
        if row["goodstype"] != CRAFTING_MATERIAL or item_id not in names:
            continue
        if sort_id is None or sort_id // 1000 not in KINDS:
            sys.exit(f"{names[item_id]} (sortId {sort_id}) doesn't fit a known kind; add its block to KINDS")
        items.append((
            item_id,
            names[item_id],
            KINDS[sort_id // 1000],
            descriptions.get(item_id),
            number(row["maxnum"], int),
            positive(number(row["maxrepositorynum"], int)),
            positive(number(row["sellvalue"], int)),
            sort_id,
            0 if item_id in base_name_ids else 1,
        ))

    uses = material_uses({item[0] for item in items}, recipe_csv, material_set_csv, text_folder)

    con = sqlite3.connect(db_path)
    try:
        if not con.execute("SELECT 1 FROM sqlite_master WHERE name = 'crafting_materials'").fetchone():
            sys.exit("The crafting_materials table doesn't exist yet; run 020_crafting_materials.sql first")
        if not con.execute("SELECT 1 FROM sqlite_master WHERE name = 'crafting_material_uses'").fetchone():
            sys.exit("The crafting_material_uses table doesn't exist yet; run 021_crafting_material_uses.sql first")
        with con:   # one transaction: all rows are replaced, or nothing changes
            con.execute("DELETE FROM crafting_material_uses")
            con.execute("DELETE FROM crafting_materials")
            con.executemany(f"INSERT INTO crafting_materials VALUES ({', '.join('?' * len(items[0]))})", items)
            con.executemany("INSERT INTO crafting_material_uses VALUES (?, ?, ?, ?)", uses)
    finally:
        con.close()

    counts = {}
    for item in items:
        counts[item[2]] = counts.get(item[2], 0) + 1
    print(f"{len(items)} crafting materials imported: " + ", ".join(f"{k} {v}" for k, v in counts.items()))
    used = {use[0] for use in uses}
    print(f"{len(used)} of them are used in {len(uses)} recipe slots")
    unused = [item[1] for item in items if item[0] not in used]
    if unused:
        print("not used in any recipe:", ", ".join(unused))


if __name__ == "__main__":
    if len(sys.argv) != 6:
        sys.exit(__doc__)
    main(*sys.argv[1:])

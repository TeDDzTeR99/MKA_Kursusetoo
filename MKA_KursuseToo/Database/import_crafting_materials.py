"""
Imports crafting materials from the game's own data into elden_ring_weapons.db (table `crafting_materials`, see
020_crafting_materials.sql).

Inputs, exported from your own copy of Elden Ring (see import_talismans.py for how):
  1. EquipParamGoods as CSV. Crafting materials are goodsType 2. The kind comes from the game's inventory order
     (sortId // 1000, see KINDS).
       - carried: maxNum, stored: maxRepositoryNum, sell value: sellValue
  2. The folder with the text files as XML: GoodsName*, GoodsCaption*.fmg.xml

All existing rows are replaced, so the script can be run again after a game patch.

Usage:
    python import_crafting_materials.py <EquipParamGoods.csv> <text folder> <path to elden_ring_weapons.db>
"""
import sqlite3
import sys

from import_consumables import positive
from import_talismans import number, read_param, read_texts

CRAFTING_MATERIAL = "2"                               # goodsType
KINDS = {100: "fauna", 101: "flora", 102: "mineral"}  # sortId // 1000 -> kind


def main(goods_csv, text_folder, db_path):
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

    con = sqlite3.connect(db_path)
    try:
        if not con.execute("SELECT 1 FROM sqlite_master WHERE name = 'crafting_materials'").fetchone():
            sys.exit("The crafting_materials table doesn't exist yet; run 020_crafting_materials.sql first")
        with con:   # one transaction: all rows are replaced, or nothing changes
            con.execute("DELETE FROM crafting_materials")
            con.executemany(f"INSERT INTO crafting_materials VALUES ({', '.join('?' * len(items[0]))})", items)
    finally:
        con.close()

    counts = {}
    for item in items:
        counts[item[2]] = counts.get(item[2], 0) + 1
    print(f"{len(items)} crafting materials imported: " + ", ".join(f"{k} {v}" for k, v in counts.items()))


if __name__ == "__main__":
    if len(sys.argv) != 4:
        sys.exit(__doc__)
    main(*sys.argv[1:])

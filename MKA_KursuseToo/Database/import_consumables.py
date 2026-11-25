"""
Imports consumables from the game's own data into elden_ring_weapons.db (table `consumables`, see 019_consumables.sql).

Inputs, exported from your own copy of Elden Ring (see import_talismans.py for how):
  1. EquipParamGoods as CSV. Consumables are goodsType 0 (and 9, the Flask of Wondrous Physick). The kind comes
     from the game's inventory order (sortId // 1000, see KINDS). Gestures and notes share goodsType 0 and are
     left out, as are unused rows (sortId 999999, or no icon) and the flask's per-level and empty-flask rows.
       - FP cost: consumeMP, carried: maxNum, stored: maxRepositoryNum, sell value: sellValue
  2. The folder with the text files as XML: GoodsName*, GoodsInfo*, GoodsCaption*.fmg.xml

All existing rows are replaced, so the script can be run again after a game patch.

Usage:
    python import_consumables.py <EquipParamGoods.csv> <text folder> <path to elden_ring_weapons.db>
"""
import re
import sqlite3
import sys

from import_talismans import number, read_param, read_texts

CONSUMABLE_TYPES = {"0", "9"}   # goodsType
UNUSED_SORT_ID = 999999
# sortId // 1000 -> kind. Blocks not listed (451 notes, 990 gestures) aren't consumables.
KINDS = {
    0: "flask", 1: "food", 2: "pot", 3: "aromatic", 4: "throwable", 5: "special", 6: "grease",
    7: "tool", 8: "special", 9: "rune", 11: "multiplayer",
}
FLASK_LEVEL = re.compile(r"^Flask of .* \+\d+$")   # one row per upgrade level; the +0 row stands for the flask


def positive(value):
    return value if value is not None and value > 0 else None


def main(goods_csv, text_folder, db_path):
    names, base_name_ids, name_files = read_texts(text_folder, "GoodsName")
    effects, _, _ = read_texts(text_folder, "GoodsInfo")
    descriptions, _, _ = read_texts(text_folder, "GoodsCaption")
    if not name_files:
        sys.exit(f"No GoodsName*.fmg.xml files found under {text_folder}")

    goods = read_param(goods_csv)
    for column in ("goodstype", "sortid", "iconid", "consumemp", "maxnum", "maxrepositorynum", "sellvalue"):
        if not goods or column not in goods[0]:
            sys.exit(f"{goods_csv} has no {column} column; is it an EquipParamGoods CSV export?")

    items, seen = [], set()
    for row in sorted(goods, key=lambda r: number(r["id"], int)):
        item_id = number(row["id"], int)
        sort_id = number(row["sortid"], int)
        name = names.get(item_id)
        if (row["goodstype"] not in CONSUMABLE_TYPES or not name or sort_id is None or sort_id == UNUSED_SORT_ID
                or row["iconid"] == "0"   # sortId 0 is a real place (Flask of Crimson Tears); no icon = unused
                or sort_id // 1000 not in KINDS or FLASK_LEVEL.match(name) or name in seen):
            continue
        seen.add(name)   # same-named rows (full / empty flask, multiplayer items' second row): the first is kept
        items.append((
            item_id,
            name,
            KINDS[sort_id // 1000],
            effects.get(item_id),
            descriptions.get(item_id),
            positive(number(row["consumemp"], int)),
            number(row["maxnum"], int),
            positive(number(row["maxrepositorynum"], int)),
            positive(number(row["sellvalue"], int)),
            sort_id,
            0 if item_id in base_name_ids else 1,
        ))

    con = sqlite3.connect(db_path)
    try:
        if not con.execute("SELECT 1 FROM sqlite_master WHERE name = 'consumables'").fetchone():
            sys.exit("The consumables table doesn't exist yet; run 019_consumables.sql first")
        with con:   # one transaction: all rows are replaced, or nothing changes
            con.execute("DELETE FROM consumables")
            con.executemany(f"INSERT INTO consumables VALUES ({', '.join('?' * len(items[0]))})", items)
    finally:
        con.close()

    counts = {}
    for item in items:
        counts[item[2]] = counts.get(item[2], 0) + 1
    print(f"{len(items)} consumables imported: " + ", ".join(f"{k} {v}" for k, v in counts.items()))


if __name__ == "__main__":
    assert FLASK_LEVEL.match("Flask of Crimson Tears +12") and not FLASK_LEVEL.match("Flask of Crimson Tears")
    if len(sys.argv) != 4:
        sys.exit(__doc__)
    main(*sys.argv[1:])

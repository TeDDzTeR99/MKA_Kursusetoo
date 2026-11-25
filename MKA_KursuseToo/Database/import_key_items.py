"""
Imports key items from the game's own data into elden_ring_weapons.db (table `key_items`, see 013_key_items.sql).

Inputs, exported from your own copy of Elden Ring (see import_talismans.py for how):
  1. EquipParamGoods as CSV. Key items are the rows with goodsType 1, plus goodsType 11
     (Cracked Pot, Ritual Pot, Perfume Bottle), which the game also lists under Key Items.
  2. The folder with the text files as XML: GoodsName*, GoodsInfo*, GoodsCaption*.fmg.xml, plus
     WeaponName*, ProtectorName*, AccessoryName*, GemName* for the names of what bell bearings and cookbooks unlock.
  3. ShopLineupParam as CSV: what bell bearings add to the Twin Maiden Husks' shop (table `key_item_unlocks`).
  4. ShopLineupParam_Recipe as CSV: the crafting recipes each cookbook teaches (same table).

Rows with sortId 0 or 999999 are cut content (e.g. the Asimi quest items) and are skipped, as are rows without a name.
Some names appear more than once on purpose: the game has a row per quest stage (e.g. Unalloyed Gold Needle).
All existing rows are replaced, so the script can be run again after a game patch.

Usage:
    python import_key_items.py <EquipParamGoods.csv> <text folder> <ShopLineupParam.csv> <ShopLineupParam_Recipe.csv> <path to elden_ring_weapons.db>
"""
import sqlite3
import sys
from bisect import bisect_right

from import_talismans import number, read_param, read_texts

KEY_ITEM_TYPES = {"1", "11"}      # goodsType: key item, pot / perfume bottle
CUT_CONTENT = {0, 999999}         # sortIds of unused rows

# The game's inventory order (sortId) groups key items by kind. Each entry is the first sortId of a kind;
# a kind runs until the next entry. None = no known kind (the script stops and asks for the list to be updated).
SORT_BLOCKS = [
    (200000, "great_rune"),       # Great Rune of the Unborn
    (202000, "container"),        # Cracked Pots, Perfume Bottle, Memory Stone, Talisman Pouch
    (203000, "great_rune"),       # Mending Runes, Great Runes
    (203090, "key"),              # Stonesword Key, keys, medallions, Carian Inverted Statue
    (204000, "quest"),            # quest items
    (205000, "tool"),             # Crafting Kit
    (205010, "cookbook"),
    (206050, "tool"),             # Spirit Calling Bell, whetblades, sewing needles, Spectral Steed Regalia
    (207000, "map"),
    (208000, "bell_bearing"),
    (210000, "scroll"),           # scrolls and prayerbooks for the spell merchants
    (211000, None),
]
STARTS = [start for start, _ in SORT_BLOCKS]


def category_of(sort_id):
    index = bisect_right(STARTS, sort_id) - 1
    return SORT_BLOCKS[index][1] if index >= 0 else None


# Using some key items sets an event flag numbered after the item's goods ID; shop rows whose
# eventFlag_forRelease is that flag become available.
#   Bell bearings, given to the Twin Maiden Husks, unlock rows in ShopLineupParam: Smithing-Stone Miner's
#   Bell Bearing [1] (8951) -> flag 11109751, Mushroom-Seller's Bell Bearing [1] (2008903) -> flag 11109793.
#   Bearings of NPCs and travelling merchants (Patches', Nomadic Merchant's, ...) instead reopen that
#   merchant's whole shop through the game's talk scripts, which aren't in the params, so they get no rows.
#   Cookbooks unlock crafting recipes in ShopLineupParam_Recipe: Armorer's Cookbook [4] (9326) -> flag 67260,
#   Forager Brood Cookbook [6] (2009301) -> flag 68510.
def release_flag(item_id, category):
    if category == "bell_bearing":
        if 8900 <= item_id < 9000:
            return 11109700 + item_id - 8900
        if 2008900 <= item_id < 2008910:
            return 11109790 + item_id - 2008900
    if category == "cookbook":
        if 9300 <= item_id < 9450:
            return 67000 + (item_id - 9300) * 10
        if 2009300 <= item_id < 2009400:
            return 68500 + (item_id - 2009300) * 10
    return None


# ShopLineupParam equipType -> text file prefix of the item's name
EQUIP_NAMES = {"0": "WeaponName", "1": "ProtectorName", "2": "AccessoryName", "3": "GoodsName", "4": "GemName"}
RUNES = "0"   # costType


def shop_unlocks(items, category, shop_csv, texts, priced):
    """(key item id, position, name, price) for every shop row an item of the category unlocks.
    priced: the rows cost runes (bell bearings); recipes have no price (None)."""
    shop = read_param(shop_csv)
    for column in ("equipid", "equiptype", "value", "costtype", "eventflag_forrelease"):
        if not shop or column not in shop[0]:
            sys.exit(f"{shop_csv} has no {column} column; is it a ShopLineupParam CSV export?")

    by_flag = {}
    for row in sorted(shop, key=lambda r: number(r["id"], int)):
        by_flag.setdefault(number(row["eventflag_forrelease"], int), []).append(row)

    unlocks = []
    for item_id, *_, item_category, _ in items:
        if item_category != category:
            continue
        for position, row in enumerate(by_flag.get(release_flag(item_id, category), []), 1):
            name = texts.get(row["equiptype"], {}).get(number(row["equipid"], int))
            if name is None or (priced and row["costtype"] != RUNES):
                sys.exit(f"{shop_csv} row {row['id']} (unlocked by {item_id}) has an unknown item or currency")
            unlocks.append((item_id, position, name, number(row["value"], int) if priced else None))
    return unlocks


def main(goods_csv, text_folder, shop_csv, recipe_csv, db_path):
    names, base_name_ids, name_files = read_texts(text_folder, "GoodsName")
    effects, _, _ = read_texts(text_folder, "GoodsInfo")
    descriptions, _, _ = read_texts(text_folder, "GoodsCaption")
    if not name_files:
        sys.exit(f"No GoodsName*.fmg.xml files found under {text_folder}")

    goods = read_param(goods_csv)
    for column in ("goodstype", "sellvalue", "sortid"):
        if not goods or column not in goods[0]:
            sys.exit(f"{goods_csv} has no {column} column; is it an EquipParamGoods CSV export?")

    items = []
    for row in goods:
        item_id = number(row["id"], int)
        sort_id = number(row["sortid"], int)
        if row["goodstype"] not in KEY_ITEM_TYPES or item_id not in names or sort_id in CUT_CONTENT or sort_id is None:
            continue
        category = category_of(sort_id)
        if category is None:
            sys.exit(f"{names[item_id]} (sortId {sort_id}) doesn't fit a known kind; add its block to SORT_BLOCKS")
        sell_value = number(row["sellvalue"], int)
        items.append((
            item_id,
            names[item_id],
            effects.get(item_id),
            descriptions.get(item_id),
            sell_value if sell_value is not None and sell_value >= 0 else None,   # -1 = can't be sold
            sort_id,
            category,
            0 if item_id in base_name_ids else 1,
        ))

    texts = {kind: read_texts(text_folder, prefix)[0] for kind, prefix in EQUIP_NAMES.items()}
    unlocks = (shop_unlocks(items, "bell_bearing", shop_csv, texts, priced=True)
               + shop_unlocks(items, "cookbook", recipe_csv, texts, priced=False))

    con = sqlite3.connect(db_path)
    try:
        if not con.execute("SELECT 1 FROM sqlite_master WHERE name = 'key_item_unlocks'").fetchone():
            sys.exit("The key_items tables don't exist yet; run 013_key_items.sql first")
        with con:   # one transaction: all rows are replaced, or nothing changes
            con.execute("DELETE FROM key_item_unlocks")
            con.execute("DELETE FROM key_items")
            con.executemany(
                """INSERT INTO key_items (id, name, effect, description, sell_value, sort_id, category, is_dlc)
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?)""", items)
            con.executemany(
                "INSERT INTO key_item_unlocks (key_item_id, position, name, price) VALUES (?, ?, ?, ?)", unlocks)
    finally:
        con.close()

    counts = {}
    for item in items:
        counts[item[6]] = counts.get(item[6], 0) + 1
    print(f"{len(items)} key items imported: " + ", ".join(f"{k} {v}" for k, v in sorted(counts.items())))
    print(f"{len({u[0] for u in unlocks})} bell bearings and cookbooks unlock {len(unlocks)} shop items and recipes")
    unlocking = {u[0] for u in unlocks}
    missing = [item[1] for item in items if item[6] == "cookbook" and item[0] not in unlocking]
    if missing:
        print("cookbooks without recipes:", ", ".join(missing))


if __name__ == "__main__":
    assert category_of(203089) == "great_rune" and category_of(203090) == "key"
    assert category_of(205000) == "tool" and category_of(205010) == "cookbook" and category_of(211000) is None
    assert release_flag(8951, "bell_bearing") == 11109751 and release_flag(2008903, "bell_bearing") == 11109793
    assert release_flag(9326, "cookbook") == 67260 and release_flag(2009301, "cookbook") == 68510
    assert release_flag(10010, "key") is None
    if len(sys.argv) != 6:
        sys.exit(__doc__)
    main(*sys.argv[1:])

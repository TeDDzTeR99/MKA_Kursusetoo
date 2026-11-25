"""
Imports upgrade materials from the game's own data into elden_ring_weapons.db
(tables `upgrade_materials` and `upgrade_material_costs`, see 010_upgrade_materials.sql).

Inputs, exported from your own copy of Elden Ring (see import_talismans.py for how):
  1. EquipParamGoods as CSV. Upgrade materials are the rows with goodsType 14.
  2. EquipMtrlSetParam as CSV: the material cost of each upgrade level.
  3. EquipParamWeapon as CSV: which material sets weapons use (regular vs somber).
  4. The folder with the text files as XML: GoodsName*, GoodsInfo*, GoodsCaption*.fmg.xml

How the game links items to costs:
  - Weapons: cost of upgrading to +N = EquipMtrlSetParam row (weapon's materialSetId + N).
    Regular weapons have materialSetId 0, somber weapons 2200.
  - Spirit ashes: every level is its own goods row. Its reinforceMaterialId is the cost row
    for the next level, and reinforceGoodsId is the next level's goods row.

All existing rows are replaced, so the script can be run again after a game patch.

Usage:
    python import_upgrade_materials.py <EquipParamGoods.csv> <EquipMtrlSetParam.csv> <EquipParamWeapon.csv> <text folder> <path to elden_ring_weapons.db>
"""
import sqlite3
import sys

from import_talismans import number, read_param, read_texts

UPGRADE_MATERIAL = "14"            # goodsType of upgrade materials
GOODS = "4"                        # materialCate in EquipMtrlSetParam: the material is a goods row
SOMBER_SET = 2200                  # materialSetId of somber weapons; regular weapons use 0

# The game sorts its inventory by sortId in blocks of 100; each block is one kind of material
CATEGORIES = {
    1500: "flask",                  # Golden Seed, Sacred Tear
    1501: "blessing",               # Scadutree Fragment, Revered Spirit Ash
    1510: "smithing_stone",
    1520: "somber_smithing_stone",
    1530: "grave_glovewort",
    1531: "ghost_glovewort",
}


def set_materials(row):
    """The (material id, amount) pairs of one EquipMtrlSetParam row."""
    pairs = []
    for i in range(1, 7):
        material = number(row.get(f"materialid0{i}"), int)
        amount = number(row.get(f"itemnum0{i}"), int)
        if row.get(f"materialcate0{i}") == GOODS and material and material > 0 and amount and amount > 0:
            pairs.append((material, amount))
    return pairs


def weapon_costs(weapons, sets):
    """(material, kind, target level, amount) for regular and somber weapons."""
    set_ids = {number(w.get("materialsetid"), int) for w in weapons}
    costs = []
    for base, kind in ((0, "weapon"), (SOMBER_SET, "somber_weapon")):
        if base not in set_ids:
            sys.exit(f"No weapon in EquipParamWeapon uses materialSetId {base}; has the game data changed?")
        level = 1
        while (base + level) in sets and set_materials(sets[base + level]):
            costs += [(m, kind, level, n) for m, n in set_materials(sets[base + level])]
            level += 1
    return costs


def spirit_ash_costs(goods, sets):
    """(material, 'spirit_ash', target level, amount): walk each ash from +0 along reinforceGoodsId."""
    by_id = {number(g["id"], int): g for g in goods}
    upgrades = {i: number(g.get("reinforcegoodsid"), int) for i, g in by_id.items()}
    targets = {t for t in upgrades.values() if t and t > 0}
    costs = set()
    for start, row in by_id.items():
        if start in targets or not upgrades.get(start) or upgrades[start] <= 0:
            continue   # not a +0 ash
        level, seen = 0, set()
        while row is not None and number(row.get("id"), int) not in seen:
            seen.add(number(row.get("id"), int))
            cost_row = sets.get(number(row.get("reinforcematerialid"), int))
            if cost_row is None or not upgrades.get(number(row["id"], int), 0) > 0:
                break
            level += 1
            costs.update((m, "spirit_ash", level, n) for m, n in set_materials(cost_row))
            row = by_id.get(upgrades[number(row["id"], int)])
    return sorted(costs)


def main(goods_csv, sets_csv, weapons_csv, text_folder, db_path):
    names, base_name_ids, name_files = read_texts(text_folder, "GoodsName")
    effects, _, _ = read_texts(text_folder, "GoodsInfo")
    descriptions, _, _ = read_texts(text_folder, "GoodsCaption")
    if not name_files:
        sys.exit(f"No GoodsName*.fmg.xml files found under {text_folder}")

    goods = read_param(goods_csv)
    for column in ("goodstype", "sellvalue", "sortid", "reinforcegoodsid", "reinforcematerialid"):
        if not goods or column not in goods[0]:
            sys.exit(f"{goods_csv} has no {column} column; is it an EquipParamGoods CSV export?")
    sets = {number(r["id"], int): r for r in read_param(sets_csv)}
    weapons = read_param(weapons_csv)
    if not weapons or "materialsetid" not in weapons[0]:
        sys.exit(f"{weapons_csv} has no materialSetId column; is it an EquipParamWeapon CSV export?")

    materials = []
    for row in goods:
        material_id = number(row["id"], int)
        if row["goodstype"] != UPGRADE_MATERIAL or material_id not in names:
            continue
        sell_value = number(row["sellvalue"], int)
        sort_id = number(row["sortid"], int)
        category = CATEGORIES.get(sort_id // 100) if sort_id is not None else None
        if category is None:
            sys.exit(f"{names[material_id]} (sortId {sort_id}) doesn't fit a known category; add its block to CATEGORIES")
        materials.append((
            material_id,
            names[material_id],
            effects.get(material_id),
            descriptions.get(material_id),
            sell_value if sell_value is not None and sell_value >= 0 else None,   # -1 = can't be sold
            sort_id,
            category,
            0 if material_id in base_name_ids else 1,
        ))

    material_ids = {m[0] for m in materials}
    costs = [c for c in weapon_costs(weapons, sets) + spirit_ash_costs(goods, sets) if c[0] in material_ids]
    if len({c[:3] for c in costs}) != len(costs):
        sys.exit("A material has two different amounts for the same upgrade level; check the game data")

    con = sqlite3.connect(db_path)
    try:
        if not con.execute("SELECT 1 FROM sqlite_master WHERE name = 'upgrade_materials'").fetchone():
            sys.exit("The upgrade_materials table doesn't exist yet; run 010_upgrade_materials.sql first")
        with con:   # one transaction: all rows are replaced, or nothing changes
            con.execute("DELETE FROM upgrade_material_costs")
            con.execute("DELETE FROM upgrade_materials")
            con.executemany(
                """INSERT INTO upgrade_materials (id, name, effect, description, sell_value, sort_id, category, is_dlc)
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?)""", materials)
            con.executemany(
                """INSERT INTO upgrade_material_costs (material_id, item_kind, target_level, amount)
                   VALUES (?, ?, ?, ?)""", costs)
    finally:
        con.close()

    dlc = sum(m[7] for m in materials)
    used = {c[0] for c in costs}
    print(f"Imported {len(materials)} upgrade materials ({len(materials) - dlc} base game, {dlc} DLC) "
          f"and {len(costs)} upgrade costs")
    print("Not used for weapon or spirit ash upgrades: "
          + ", ".join(m[1] for m in materials if m[0] not in used))


if __name__ == "__main__":
    if len(sys.argv) != 6:
        sys.exit(__doc__)
    main(*sys.argv[1:])

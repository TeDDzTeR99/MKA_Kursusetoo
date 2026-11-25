"""
Imports ashes of war and spirit ashes from the game's own data into elden_ring_weapons.db
(tables `ashes_of_war` and `spirit_ashes`, see 015_ashes.sql).

Inputs, exported from your own copy of Elden Ring (see import_talismans.py for how):
  1. EquipParamGem as CSV: ashes of war. defaultWepAttr / configurableWepAttrNN give the affinities,
     canMountWep_* the weapon types it fits, swordArtsParamId the skill.
  2. SwordArtsParam as CSV: the skill's FP cost (useMagicPoint_L1 / L2 / R1 / R2).
  3. EquipParamGoods as CSV: spirit ashes are goodsType 7 (upgraded with Grave Glovewort) and 8 (Ghost Glovewort).
     Every upgrade level is its own row, linked by reinforceGoodsId; only the +0 row is imported.
  4. The folder with the text files as XML: GemName*, GemCaption*, GoodsName*, GoodsInfo*, GoodsCaption*.fmg.xml
  5. elden_ring_weapons.db, whose `affinities` table names the affinities (id = the game's number + 1).

All existing rows are replaced, so the script can be run again after a game patch.

Usage:
    python import_ashes.py <EquipParamGem.csv> <SwordArtsParam.csv> <EquipParamGoods.csv> <text folder> <path to elden_ring_weapons.db>
"""
import sqlite3
import sys

from import_talismans import number, read_param, read_texts

# Ashes of war: the game sorts them by sortId in blocks of 1000, one per default affinity, then bows and shields.
# 999 holds test rows.
ASH_KINDS = {
    350: "heavy", 351: "keen", 352: "quality", 353: "magic", 354: "fire", 355: "flame_art", 356: "lightning",
    357: "sacred", 358: "poison", 359: "blood", 360: "cold", 361: "occult", 362: "bow", 363: "shield",
}
TEST_BLOCK = 999

# canMountWep_<column> -> weapon type, in the game's order. SpearLarge, Sorcery and Talisman are never set on
# a real ash of war, so they are left out (the script stops if one ever is).
WEAPON_TYPES = {
    "dagger": "Dagger", "swordnormal": "Straight Sword", "swordlarge": "Greatsword", "swordgigantic": "Colossal Sword",
    "sabernormal": "Curved Sword", "saberlarge": "Curved Greatsword", "katana": "Katana",
    "sworddoubleedge": "Twinblade", "swordpierce": "Thrusting Sword", "rapierheavy": "Heavy Thrusting Sword",
    "axenormal": "Axe", "axelarge": "Greataxe", "hammernormal": "Hammer", "hammerlarge": "Great Hammer",
    "flail": "Flail", "spearnormal": "Spear", "spearheavy": "Great Spear", "spearaxe": "Halberd",
    "sickle": "Reaper", "knuckle": "Fist", "claw": "Claw", "whip": "Whip", "axhammerlarge": "Colossal Weapon",
    "bowsmall": "Light Bow", "bownormal": "Bow", "bowlarge": "Greatbow", "clossbow": "Crossbow",
    "ballista": "Ballista", "staff": "Glintstone Staff", "shieldsmall": "Small Shield",
    "shieldnormal": "Medium Shield", "shieldlarge": "Greatshield", "torch": "Torch",
    "handtohand": "Hand-to-Hand Art", "perfumebottle": "Perfume Bottle", "thrustingshield": "Thrusting Shield",
    "throwingweapon": "Throwing Blade", "reversehandsword": "Backhand Blade", "lightgreatsword": "Light Greatsword",
    "greatkatana": "Great Katana", "beastclaw": "Beast Claw",
}

SPIRIT_ASH_KINDS = {"7": "grave_glovewort", "8": "ghost_glovewort"}   # goodsType -> upgrade material


def positive(value):
    value = number(value, int)
    return value if value is not None and value > 0 else None


def ashes_of_war(gem_csv, arts_csv, text_folder, affinity_names):
    names, base_name_ids, name_files = read_texts(text_folder, "GemName")
    captions, _, _ = read_texts(text_folder, "GemCaption")
    if not name_files:
        sys.exit(f"No GemName*.fmg.xml files found under {text_folder}")
    gems = read_param(gem_csv)
    for column in ("sortid", "defaultwepattr", "swordartsparamid", "configurablewepattr00", "canmountwep_dagger"):
        if not gems or column not in gems[0]:
            sys.exit(f"{gem_csv} has no {column} column; is it an EquipParamGem CSV export?")
    arts = {number(r["id"], int): r for r in read_param(arts_csv)}

    rows = []
    for row in gems:
        gem_id = number(row["id"], int)
        sort_id = number(row["sortid"], int)
        if gem_id not in names or sort_id // 1000 == TEST_BLOCK:
            continue
        kind = ASH_KINDS.get(sort_id // 1000)
        if kind is None:
            sys.exit(f"{names[gem_id]} (sortId {sort_id}) doesn't fit a known kind; add its block to ASH_KINDS")

        affinities = [affinity_names[int(k[-2:])] for k in sorted(row)
                      if k.startswith("configurablewepattr") and row[k] == "1"]
        mounts = [k[len("canmountwep_"):] for k in row if k.startswith("canmountwep_") and row[k] == "1"]
        unknown = [m for m in mounts if m not in WEAPON_TYPES]
        if unknown:
            sys.exit(f"{names[gem_id]} fits {unknown}; add them to WEAPON_TYPES")
        skill = arts.get(number(row["swordartsparamid"], int), {})
        costs = [c for c in (positive(skill.get(f"usemagicpoint_{k}")) for k in ("l1", "l2", "r1", "r2")) if c]

        rows.append((
            gem_id,
            names[gem_id],
            kind,
            affinity_names[number(row["defaultwepattr"], int)],
            ", ".join(affinities),
            ", ".join(WEAPON_TYPES[m] for m in mounts),
            max(costs) if costs else None,
            captions.get(gem_id),
            sort_id,
            0 if gem_id in base_name_ids else 1,
        ))
    return rows


def spirit_ashes(goods_csv, text_folder):
    names, base_name_ids, name_files = read_texts(text_folder, "GoodsName")
    effects, _, _ = read_texts(text_folder, "GoodsInfo")
    descriptions, _, _ = read_texts(text_folder, "GoodsCaption")
    if not name_files:
        sys.exit(f"No GoodsName*.fmg.xml files found under {text_folder}")
    goods = read_param(goods_csv)
    for column in ("goodstype", "sortid", "reinforcegoodsid", "consumemp", "consumehp"):
        if not goods or column not in goods[0]:
            sys.exit(f"{goods_csv} has no {column} column; is it an EquipParamGoods CSV export?")

    ashes = {number(r["id"], int): r for r in goods
             if r["goodstype"] in SPIRIT_ASH_KINDS and number(r["id"], int) in names}
    next_level = {i: number(r["reinforcegoodsid"], int) for i, r in ashes.items()}
    upgraded = set(next_level.values())

    rows = []
    for ash_id, row in ashes.items():
        if ash_id in upgraded:   # a +1 ... +10 row
            continue
        level, current = 0, ash_id
        while next_level.get(current) in ashes:
            current = next_level[current]
            level += 1
        rows.append((
            ash_id,
            names[ash_id],
            SPIRIT_ASH_KINDS[row["goodstype"]],
            effects.get(ash_id),
            descriptions.get(ash_id),
            positive(row["consumemp"]),
            positive(row["consumehp"]),
            level,
            number(row["sortid"], int),
            0 if ash_id in base_name_ids else 1,
        ))
    return rows


def main(gem_csv, arts_csv, goods_csv, text_folder, db_path):
    con = sqlite3.connect(db_path)
    try:
        if not con.execute("SELECT 1 FROM sqlite_master WHERE name = 'spirit_ashes'").fetchone():
            sys.exit("The ashes tables don't exist yet; run 015_ashes.sql first")
        affinity_names = {i - 1: name for i, name in con.execute("SELECT id, name FROM affinities")}
        war = ashes_of_war(gem_csv, arts_csv, text_folder, affinity_names)
        spirits = spirit_ashes(goods_csv, text_folder)
        with con:   # one transaction: all rows are replaced, or nothing changes
            con.execute("DELETE FROM ashes_of_war")
            con.execute("DELETE FROM spirit_ashes")
            con.executemany("INSERT INTO ashes_of_war VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)", war)
            con.executemany("INSERT INTO spirit_ashes VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)", spirits)
    finally:
        con.close()
    print(f"{len(war)} ashes of war and {len(spirits)} spirit ashes imported")


if __name__ == "__main__":
    assert positive("-1") is None and positive("0") is None and positive("20") == 20
    if len(sys.argv) != 6:
        sys.exit(__doc__)
    main(*sys.argv[1:])

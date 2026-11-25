"""
Imports sorceries and incantations from the game's own data into elden_ring_weapons.db
(table `spells`, see 014_spells.sql).

Inputs, exported from your own copy of Elden Ring (see import_talismans.py for how):
  1. EquipParamGoods as CSV. Spells are the rows with goodsType 5 / 17 (sorcery) and 16 / 18 (incantation).
  2. Magic as CSV: FP (mp), stamina, slotLength and requirementIntellect / Faith / Luck (= arcane).
  3. The folder with the text files as XML: GoodsName*, GoodsInfo*, GoodsCaption*.fmg.xml

All existing rows are replaced, so the script can be run again after a game patch.

Usage:
    python import_spells.py <EquipParamGoods.csv> <Magic.csv> <text folder> <path to elden_ring_weapons.db>
"""
import sqlite3
import sys

from import_talismans import number, read_param, read_texts

TYPES = {"5": "sorcery", "17": "sorcery", "16": "incantation", "18": "incantation"}   # goodsType

# The game sorts spells by sortId in blocks of 1000; each block is one school
SCHOOLS = {
    250: "glintstone", 251: "primeval", 252: "carian", 253: "night", 254: "magma", 255: "cold",
    256: "crystalian", 257: "gravity", 258: "oracle", 259: "thorn", 260: "death",
    300: "two_fingers", 301: "assassin", 302: "gold", 303: "erdtree", 304: "golden_order", 305: "dragon_cult",
    306: "flame", 307: "godskin", 308: "bestial", 309: "bloodflame", 310: "rot", 311: "frenzied_flame",
    312: "dragon_communion", 313: "divine_beast",
}


def main(goods_csv, magic_csv, text_folder, db_path):
    names, base_name_ids, name_files = read_texts(text_folder, "GoodsName")
    effects, _, _ = read_texts(text_folder, "GoodsInfo")
    descriptions, _, _ = read_texts(text_folder, "GoodsCaption")
    if not name_files:
        sys.exit(f"No GoodsName*.fmg.xml files found under {text_folder}")

    goods = read_param(goods_csv)
    for column in ("goodstype", "sortid"):
        if not goods or column not in goods[0]:
            sys.exit(f"{goods_csv} has no {column} column; is it an EquipParamGoods CSV export?")
    magic = {number(r["id"], int): r for r in read_param(magic_csv)}
    for column in ("mp", "stamina", "slotlength", "requirementintellect", "requirementfaith", "requirementluck"):
        if not magic or column not in next(iter(magic.values())):
            sys.exit(f"{magic_csv} has no {column} column; is it a Magic CSV export?")

    spells = []
    for row in goods:
        spell_id = number(row["id"], int)
        if row["goodstype"] not in TYPES or spell_id not in names:
            continue
        sort_id = number(row["sortid"], int)
        school = SCHOOLS.get(sort_id // 1000) if sort_id is not None else None
        if school is None:
            sys.exit(f"{names[spell_id]} (sortId {sort_id}) doesn't fit a known school; add its block to SCHOOLS")
        stats = magic.get(spell_id)
        if stats is None:
            sys.exit(f"{names[spell_id]} ({spell_id}) has no row in {magic_csv}")
        spells.append((
            spell_id,
            names[spell_id],
            TYPES[row["goodstype"]],
            school,
            effects.get(spell_id),
            descriptions.get(spell_id),
            number(stats["mp"], int),
            number(stats["stamina"], int),
            number(stats["slotlength"], int),
            number(stats["requirementintellect"], int),
            number(stats["requirementfaith"], int),
            number(stats["requirementluck"], int),
            sort_id,
            0 if spell_id in base_name_ids else 1,
        ))

    con = sqlite3.connect(db_path)
    try:
        if not con.execute("SELECT 1 FROM sqlite_master WHERE name = 'spells'").fetchone():
            sys.exit("The spells table doesn't exist yet; run 014_spells.sql first")
        with con:   # one transaction: all rows are replaced, or nothing changes
            con.execute("DELETE FROM spells")
            con.executemany(
                """INSERT INTO spells (id, name, type, category, effect, description, fp, stamina, slots,
                                       required_intelligence, required_faith, required_arcane, sort_id, is_dlc)
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""", spells)
    finally:
        con.close()

    for kind in ("sorcery", "incantation"):
        print(f"{sum(s[2] == kind for s in spells)} {kind} rows imported")


if __name__ == "__main__":
    if len(sys.argv) != 5:
        sys.exit(__doc__)
    main(*sys.argv[1:])

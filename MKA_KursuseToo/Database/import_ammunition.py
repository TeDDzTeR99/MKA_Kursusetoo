"""
Imports arrows and bolts from the game's own data into elden_ring_weapons.db (table `ammunition`, see
018_ammunition.sql).

Inputs, exported from your own copy of Elden Ring (see import_talismans.py for how):
  1. EquipParamWeapon as CSV. Ammunition is wepType 81 (arrow), 83 (great arrow), 85 (bolt) and 86 (greatbolt).
     Only rows ending in 0000 are imported; sortId 9999999 marks unused / NPC-only rows.
       - damage: attackBase<type>, carried amount: maxArrowQuantity, sell value: sellValue
  2. The folder with the text files as XML: WeaponName*, WeaponCaption*.fmg.xml

All existing rows are replaced, so the script can be run again after a game patch.

Usage:
    python import_ammunition.py <EquipParamWeapon.csv> <text folder> <path to elden_ring_weapons.db>
"""
import sqlite3
import sys

from import_talismans import number, read_param, read_texts

KINDS = {"81": "arrow", "83": "great_arrow", "85": "bolt", "86": "greatbolt"}   # wepType
UNUSED_SORT_ID = 9999999
DAMAGE = ("attackbasephysics", "attackbasemagic", "attackbasefire", "attackbasethunder", "attackbasedark")


def main(weapon_csv, text_folder, db_path):
    names, base_name_ids, name_files = read_texts(text_folder, "WeaponName")
    descriptions, _, _ = read_texts(text_folder, "WeaponCaption")
    if not name_files:
        sys.exit(f"No WeaponName*.fmg.xml files found under {text_folder}")

    rows = read_param(weapon_csv)
    for column in ("weptype", "sortid", "maxarrowquantity", "sellvalue", *DAMAGE):
        if not rows or column not in rows[0]:
            sys.exit(f"{weapon_csv} has no {column} column; is it an EquipParamWeapon CSV export?")

    ammo = []
    for row in rows:
        ammo_id = number(row["id"], int)
        sort_id = number(row["sortid"], int)
        if (row["weptype"] not in KINDS or ammo_id % 10000 != 0 or ammo_id not in names
                or sort_id == UNUSED_SORT_ID):
            continue
        sell_value = number(row["sellvalue"], int)
        ammo.append((
            ammo_id,
            names[ammo_id],
            KINDS[row["weptype"]],
            descriptions.get(ammo_id),
            *(number(row[c], int) for c in DAMAGE),
            number(row["maxarrowquantity"], int),
            sell_value if sell_value > 0 else None,
            sort_id,
            0 if ammo_id in base_name_ids else 1,
        ))

    con = sqlite3.connect(db_path)
    try:
        if not con.execute("SELECT 1 FROM sqlite_master WHERE name = 'ammunition'").fetchone():
            sys.exit("The ammunition table doesn't exist yet; run 018_ammunition.sql first")
        with con:   # one transaction: all rows are replaced, or nothing changes
            con.execute("DELETE FROM ammunition")
            con.executemany(f"INSERT INTO ammunition VALUES ({', '.join('?' * len(ammo[0]))})", ammo)
    finally:
        con.close()

    counts = {}
    for item in ammo:
        counts[item[2]] = counts.get(item[2], 0) + 1
    print(f"{len(ammo)} arrows and bolts imported: " + ", ".join(f"{k} {v}" for k, v in counts.items()))


if __name__ == "__main__":
    if len(sys.argv) != 4:
        sys.exit(__doc__)
    main(*sys.argv[1:])

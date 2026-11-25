"""
Imports shields from the game's own data into elden_ring_weapons.db (table `shields`, see 017_shields.sql).

Inputs, exported from your own copy of Elden Ring (see import_talismans.py for how):
  1. EquipParamWeapon as CSV. Shields are wepType 65 (small), 67 (medium), 69 (greatshield) and 90 (thrusting
     shield, Shadow of the Erdtree). Only the Standard +0 row of each (ID ending in 0000) is imported.
       - guard negation: <type>GuardCutRate (already in %), guard boost: staminaGuardDef
       - damage: attackBase<type>, requirements: proper<stat>, scaling: correct<stat> (shown as a letter)
       - materialSetId 2200 = somber (max +10), otherwise +25
  2. The folder with the text files as XML: WeaponName*, WeaponCaption*.fmg.xml

All existing rows are replaced, so the script can be run again after a game patch.

Usage:
    python import_shields.py <EquipParamWeapon.csv> <text folder> <path to elden_ring_weapons.db>
"""
import sqlite3
import sys

from import_talismans import number, read_param, read_texts

KINDS = {"65": "small", "67": "medium", "69": "great", "90": "thrusting"}   # wepType
SOMBER_SET = 2200                                                             # materialSetId

GUARDS = ("physguardcutrate", "magguardcutrate", "fireguardcutrate", "thunguardcutrate", "darkguardcutrate")
DAMAGE = ("attackbasephysics", "attackbasemagic", "attackbasefire", "attackbasethunder", "attackbasedark")
STATS = ("strength", "agility", "magic", "faith", "luck")   # the game's names for Str, Dex, Int, Fai, Arc
# Scaling value -> letter, like the scaling_grades table: S >= 175, A >= 140, B >= 90, C >= 60, D >= 25, E > 0
GRADES = ((175, "S"), (140, "A"), (90, "B"), (60, "C"), (25, "D"))


def grade(value):
    if value <= 0:
        return "-"
    return next((letter for minimum, letter in GRADES if value >= minimum), "E")


def main(weapon_csv, text_folder, db_path):
    names, base_name_ids, name_files = read_texts(text_folder, "WeaponName")
    descriptions, _, _ = read_texts(text_folder, "WeaponCaption")
    if not name_files:
        sys.exit(f"No WeaponName*.fmg.xml files found under {text_folder}")

    rows = read_param(weapon_csv)
    needed = ("weptype", "sortid", "weight", "staminaguarddef", "materialsetid", *GUARDS, *DAMAGE,
              *(f"proper{s}" for s in STATS), *(f"correct{s}" for s in STATS))
    for column in needed:
        if not rows or column not in rows[0]:
            sys.exit(f"{weapon_csv} has no {column} column; is it an EquipParamWeapon CSV export?")

    shields = []
    for row in rows:
        shield_id = number(row["id"], int)
        if row["weptype"] not in KINDS or shield_id % 10000 != 0 or shield_id not in names:
            continue
        shields.append((
            shield_id,
            names[shield_id],
            KINDS[row["weptype"]],
            descriptions.get(shield_id),
            number(row["weight"], float),
            *(round(number(row[c], float)) for c in GUARDS),
            round(number(row["staminaguarddef"], float)),
            *(number(row[c], int) for c in DAMAGE),
            *(number(row[f"proper{s}"], int) for s in STATS),
            *(grade(number(row[f"correct{s}"], float)) for s in STATS),
            10 if number(row["materialsetid"], int) == SOMBER_SET else 25,
            number(row["sortid"], int),
            0 if shield_id in base_name_ids else 1,
        ))

    con = sqlite3.connect(db_path)
    try:
        if not con.execute("SELECT 1 FROM sqlite_master WHERE name = 'shields'").fetchone():
            sys.exit("The shields table doesn't exist yet; run 017_shields.sql first")
        with con:   # one transaction: all rows are replaced, or nothing changes
            con.execute("DELETE FROM shields")
            con.executemany(f"INSERT INTO shields VALUES ({', '.join('?' * len(shields[0]))})", shields)
    finally:
        con.close()

    counts = {}
    for shield in shields:
        counts[shield[2]] = counts.get(shield[2], 0) + 1
    print(f"{len(shields)} shields imported: " + ", ".join(f"{k} {v}" for k, v in counts.items()))


if __name__ == "__main__":
    assert grade(175) == "S" and grade(60) == "C" and grade(24) == "E" and grade(0) == "-"
    if len(sys.argv) != 4:
        sys.exit(__doc__)
    main(*sys.argv[1:])

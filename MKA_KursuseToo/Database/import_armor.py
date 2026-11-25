"""
Imports armor from the game's own data into elden_ring_weapons.db (table `armor`, see 016_armor.sql).

Inputs, exported from your own copy of Elden Ring (see import_talismans.py for how):
  1. EquipParamProtector as CSV. protectorCategory 0-3 = head, chest, arms, legs (4 = placeholder body types).
     Damage negation is (1 - <type>DamageCutRate) * 100; poise is toughnessCorrectRate * 1000.
  2. The folder with the text files as XML: ProtectorName*, ProtectorInfo*, ProtectorCaption*.fmg.xml

Rows with sortId 999999 are cut content (e.g. the Ragged set) and 99999 the "nothing equipped" placeholders
(Head, Body, ...); both are skipped, as are rows without a name.
All existing rows are replaced, so the script can be run again after a game patch.

Usage:
    python import_armor.py <EquipParamProtector.csv> <text folder> <path to elden_ring_weapons.db>
"""
import sqlite3
import sys

from import_talismans import number, read_param, read_texts

SLOTS = {"0": "head", "1": "chest", "2": "arms", "3": "legs"}   # protectorCategory
SKIPPED_SORT_IDS = {99999, 999999}                             # placeholders and cut content

# Table column -> EquipParamProtector cut rate column, in the game menu's order
NEGATIONS = {
    "physical": "neutraldamagecutrate", "strike": "blowdamagecutrate", "slash": "slashdamagecutrate",
    "pierce": "thrustdamagecutrate", "magic": "magicdamagecutrate", "fire": "firedamagecutrate",
    "lightning": "thunderdamagecutrate", "holy": "darkdamagecutrate",
}
# Each in-game resistance covers two statuses with the same value; the first of each pair is read
RESISTANCES = {"immunity": "resistpoison", "robustness": "resistblood", "focus": "resistsleep", "vitality": "resistcurse"}


def negation(cut_rate):
    return round((1 - number(cut_rate, float)) * 100, 1)


def main(protector_csv, text_folder, db_path):
    names, base_name_ids, name_files = read_texts(text_folder, "ProtectorName")
    effects, _, _ = read_texts(text_folder, "ProtectorInfo")
    descriptions, _, _ = read_texts(text_folder, "ProtectorCaption")
    if not name_files:
        sys.exit(f"No ProtectorName*.fmg.xml files found under {text_folder}")

    rows = read_param(protector_csv)
    needed = ("protectorcategory", "sortid", "weight", "toughnesscorrectrate", "sellvalue",
              *NEGATIONS.values(), *RESISTANCES.values())
    for column in needed:
        if not rows or column not in rows[0]:
            sys.exit(f"{protector_csv} has no {column} column; is it an EquipParamProtector CSV export?")

    armor = []
    for row in rows:
        armor_id = number(row["id"], int)
        sort_id = number(row["sortid"], int)
        if row["protectorcategory"] not in SLOTS or armor_id not in names or sort_id in SKIPPED_SORT_IDS:
            continue
        sell_value = number(row["sellvalue"], int)
        armor.append((
            armor_id,
            names[armor_id],
            SLOTS[row["protectorcategory"]],
            effects.get(armor_id),
            descriptions.get(armor_id),
            number(row["weight"], float),
            round(number(row["toughnesscorrectrate"], float) * 1000),
            *(negation(row[column]) for column in NEGATIONS.values()),
            *(number(row[column], int) for column in RESISTANCES.values()),
            sell_value if sell_value is not None and sell_value >= 0 else None,
            sort_id,
            0 if armor_id in base_name_ids else 1,
        ))

    con = sqlite3.connect(db_path)
    try:
        if not con.execute("SELECT 1 FROM sqlite_master WHERE name = 'armor'").fetchone():
            sys.exit("The armor table doesn't exist yet; run 016_armor.sql first")
        columns = ("id, name, category, effect, description, weight, poise, "
                   + ", ".join(NEGATIONS) + ", " + ", ".join(RESISTANCES) + ", sell_value, sort_id, is_dlc")
        with con:   # one transaction: all rows are replaced, or nothing changes
            con.execute("DELETE FROM armor")
            con.executemany(f"INSERT INTO armor ({columns}) VALUES ({', '.join('?' * len(armor[0]))})", armor)
    finally:
        con.close()

    counts = {}
    for piece in armor:
        counts[piece[2]] = counts.get(piece[2], 0) + 1
    print(f"{len(armor)} armor pieces imported: " + ", ".join(f"{k} {v}" for k, v in counts.items()))


if __name__ == "__main__":
    assert negation("0.932") == 6.8 and negation("1") == 0.0
    if len(sys.argv) != 4:
        sys.exit(__doc__)
    main(*sys.argv[1:])

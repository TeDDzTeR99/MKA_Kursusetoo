"""
Fills weapons.description (see 011_weapon_descriptions.sql) with the in-game description of each weapon.

Input: the folder with the text files as XML (see import_talismans.py), searched for
  WeaponName*.fmg.xml     -> name, used to find the weapon
  WeaponCaption*.fmg.xml  -> description
Our weapons table has its own ids, so weapons are matched by name, the same way as in
import_from_game_data.py (accents ignored, e.g. 'Miséricorde' == 'Misericorde').

Can be run again after a game patch: every matched weapon's description is overwritten.

Usage:
    python import_weapon_descriptions.py <text folder> <path to elden_ring_weapons.db>
"""
import sqlite3
import sys

from import_from_game_data import normalize
from import_talismans import read_texts


def main(text_folder, db_path):
    names, _, name_files = read_texts(text_folder, "WeaponName")
    captions, _, _ = read_texts(text_folder, "WeaponCaption")
    if not name_files:
        sys.exit(f"No WeaponName*.fmg.xml files found under {text_folder}")

    # Game name -> description. Every affinity has its own name ("Heavy Dagger"), so the plain name
    # is the weapon itself; keep the first id with a description if a name appears more than once.
    descriptions = {}
    for text_id in sorted(names):
        key = normalize(names[text_id])
        if key not in descriptions and captions.get(text_id):
            descriptions[key] = captions[text_id]

    con = sqlite3.connect(db_path)
    try:
        columns = [row[1] for row in con.execute("PRAGMA table_info(weapons)")]
        if "description" not in columns:
            sys.exit("weapons has no description column yet; run 011_weapon_descriptions.sql first")

        weapons = con.execute("SELECT id, name FROM weapons").fetchall()
        updates = [(descriptions[normalize(name)], weapon_id)
                   for weapon_id, name in weapons if normalize(name) in descriptions]
        with con:   # one transaction: all descriptions are written, or none
            con.executemany("UPDATE weapons SET description = ? WHERE id = ?", updates)
    finally:
        con.close()

    matched = {weapon_id for _, weapon_id in updates}
    unmatched = [name for weapon_id, name in weapons if weapon_id not in matched]
    print(f"Descriptions for {len(updates)} of {len(weapons)} weapons")
    if unmatched:
        print("No description found for: " + ", ".join(unmatched))


if __name__ == "__main__":
    if len(sys.argv) != 3:
        sys.exit(__doc__)
    main(*sys.argv[1:])

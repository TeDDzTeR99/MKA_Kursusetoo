"""
Imports exact attribute scaling for every weapon, affinity and upgrade level
into elden_ring_weapons.db, and corrects each weapon's upgrade path / max level.

Data source: Elden Ring regulation data (patch 1.17) extracted by the
elden-ring-weapon-calculator project by Tom Clark (MIT license):
https://github.com/ThomasJClark/elden-ring-weapon-calculator
File: public/regulation-vanilla-v1.17.js (plain JSON)

How scaling works in the game:
    scaling at level L = base scaling of the weapon/affinity * upgrade multiplier for level L
The number shown in game is that value * 100, rounded down. The letter grade is
derived from the value: S >= 175, A >= 140, B >= 90, C >= 60, D >= 25, E > 0.

Usage:
    python import_scaling_from_game_data.py <path to regulation-vanilla-v1.17.js> <path to elden_ring_weapons.db>
"""
import json
import math
import sqlite3
import sys
import unicodedata

ATTRIBUTES = ["str", "dex", "int", "fai", "arc"]

# affinityId in the game data -> name in our affinities table (-1 = unique weapon, no affinity)
AFFINITIES = {
    -1: "Standard", 0: "Standard", 1: "Heavy", 2: "Keen", 3: "Quality", 4: "Fire",
    5: "Flame Art", 6: "Lightning", 7: "Sacred", 8: "Magic", 9: "Cold",
    10: "Poison", 11: "Blood", 12: "Occult",
}


def normalize(name):
    """Compare names without accents, e.g. 'Miséricorde' == 'Misericorde'."""
    return unicodedata.normalize("NFKD", name).encode("ascii", "ignore").decode().lower()


def main(data_path, db_path):
    with open(data_path, encoding="utf-8") as f:
        data = json.load(f)

    tiers = data["scalingTiers"]                     # [[1.75, "S"], [1.4, "A"], ...]
    reinforce = data["reinforceTypes"]

    def grade_for(raw):
        for threshold, letter in tiers:
            if raw >= threshold:
                return letter
        return None

    game_weapons = {}
    for w in data["weapons"]:
        game_weapons.setdefault(normalize(w["weaponName"]), []).append(w)

    con = sqlite3.connect(db_path)
    cur = con.cursor()
    affinity_ids = dict(cur.execute("SELECT name, id FROM affinities"))
    grade_ids = dict(cur.execute("SELECT grade, id FROM scaling_grades"))

    imported, path_fixes, unmatched, rows = 0, [], [], 0

    cur.execute("BEGIN")
    for weapon_id, name, path, max_level in cur.execute(
            "SELECT id, name, upgrade_path, max_upgrade_level FROM weapons").fetchall():
        entries = game_weapons.get(normalize(name))
        if not entries:
            unmatched.append(name)
            continue

        game_max = len(reinforce[str(entries[0]["reinforceTypeId"])]) - 1
        game_path = "Somber" if game_max == 10 else "Standard"
        if (path, max_level) != (game_path, game_max):
            path_fixes.append((name, f"{path} +{max_level}", f"{game_path} +{game_max}"))
            cur.execute("UPDATE weapons SET upgrade_path = ?, max_upgrade_level = ? WHERE id = ?",
                        (game_path, game_max, weapon_id))

        cur.execute("DELETE FROM weapon_scaling WHERE weapon_id = ?", (weapon_id,))

        for entry in entries:
            affinity_id = affinity_ids[AFFINITIES[entry["affinityId"]]]
            base = dict(entry["attributeScaling"])
            levels = reinforce[str(entry["reinforceTypeId"])]

            for level, multipliers in enumerate(levels):
                grades, values = [], []
                for attr in ATTRIBUTES:
                    raw = base.get(attr, 0) * multipliers["attributeScaling"][attr]
                    letter = grade_for(raw) if raw > 0 else None
                    grades.append(grade_ids[letter] if letter else None)
                    values.append(math.floor(raw * 100 + 1e-9) if letter else None)

                cur.execute(
                    """INSERT INTO weapon_scaling (
                           weapon_id, affinity_id, upgrade_level,
                           strength_grade_id, dexterity_grade_id, intelligence_grade_id, faith_grade_id, arcane_grade_id,
                           strength_value, dexterity_value, intelligence_value, faith_value, arcane_value)
                       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
                    (weapon_id, affinity_id, level, *grades, *values))
                rows += 1
        imported += 1

    con.commit()

    print(f"Imported {imported} weapons ({rows} scaling rows)")
    print(f"Corrected upgrade path for {len(path_fixes)} weapons:")
    for fix in path_fixes:
        print(f"  {fix[0]}: {fix[1]} -> {fix[2]}")
    print(f"Not found in game data ({len(unmatched)}), left unchanged: {', '.join(unmatched)}")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        print(__doc__)
        sys.exit(1)
    main(sys.argv[1], sys.argv[2])

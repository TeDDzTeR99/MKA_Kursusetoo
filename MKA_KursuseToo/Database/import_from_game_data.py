"""
Imports weapon data from the game into elden_ring_weapons.db:
  - attribute scaling for every weapon, affinity and upgrade level (weapon_scaling)
  - attack damage and status buildup for every weapon, affinity and upgrade level (weapon_damage)
  - base damage (Standard, +0) in the weapons table
  - stat requirements (weapon_requirements)
  - upgrade path / max upgrade level
  - weapon type

Data source: Elden Ring regulation data (patch 1.17) extracted by the
elden-ring-weapon-calculator project by Tom Clark (MIT license):
https://github.com/ThomasJClark/elden-ring-weapon-calculator
File: public/regulation-vanilla-v1.17.js (plain JSON)

How scaling works in the game:
    scaling at level L = base scaling of the weapon/affinity * upgrade multiplier for level L
The number shown in game is that value * 100, rounded down. The letter grade is
derived from the value: S >= 175, A >= 140, B >= 90, C >= 60, D >= 25, E > 0.

Damage works the same way:
    damage at level L = base damage * upgrade multiplier for level L, rounded down

Usage:
    python import_from_game_data.py <path to regulation-vanilla-v1.17.js> <path to elden_ring_weapons.db>
"""
import json
import math
import sqlite3
import sys
import unicodedata

ATTRIBUTES = ["str", "dex", "int", "fai", "arc"]

# weaponType id in the game data -> name in our weapon_types table
# (ids from src/calculator/weaponTypes.ts in elden-ring-weapon-calculator)
WEAPON_TYPES = {
    1: "Dagger", 3: "Straight Sword", 5: "Greatsword", 7: "Colossal Sword",
    9: "Curved Sword", 11: "Curved Greatsword", 13: "Katana", 14: "Twinblade",
    15: "Thrusting Sword", 16: "Heavy Thrusting Sword", 17: "Axe", 19: "Greataxe",
    21: "Hammer", 23: "Great Hammer", 24: "Flail", 25: "Spear", 28: "Great Spear",
    29: "Halberd", 31: "Reaper", 35: "Fist", 37: "Claw", 39: "Whip",
    41: "Colossal Weapon", 50: "Light Bow", 51: "Bow", 53: "Greatbow",
    55: "Crossbow", 56: "Ballista", 57: "Glintstone Staff", 61: "Sacred Seal",
}

# Attack type ids in the game data: 0 physical, 1 magic, 2 fire, 3 lightning, 4 holy
DAMAGE_TYPES = [0, 1, 2, 3, 4]

# Status effect type ids: 5 poison, 6 scarlet rot, 7 bleed, 8 frost, 9 sleep, 10 madness, 11 death blight
# (same order as the *_buildup columns in weapon_damage)
STATUS_TYPES = [5, 6, 7, 8, 9, 10, 11]

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
    status_params = data["statusSpEffectParams"]

    def status_buildup(entry, multipliers):
        """Status buildup at one upgrade level, e.g. {7: 57} = bleed 57.
        Each weapon lists up to 3 status effect ids; upgrading adds an offset to each id
        (statusSpEffectId1..3 of that level), which is how affinity buildup grows with upgrades."""
        offsets = [multipliers.get("statusSpEffectId1", 0),
                   multipliers.get("statusSpEffectId2", 0),
                   multipliers.get("statusSpEffectId3", 0)]
        result = {}
        for i, effect_id in enumerate(entry.get("statusSpEffectParamIds") or []):
            if effect_id:
                params = status_params.get(str(effect_id + offsets[i])) or {}
                result.update({int(t): int(v) for t, v in params.items()})
        return result

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
    type_ids = dict(cur.execute("SELECT name, id FROM weapon_types"))
    type_fixes = []

    imported, path_fixes, unmatched, rows = 0, [], [], 0
    damage_fixes, requirement_fixes = 0, 0

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

        # Base entry = Standard affinity (0) or unique weapon (-1)
        base_entry = next(e for e in entries if e["affinityId"] in (0, -1))

        # Weapon type
        game_type = WEAPON_TYPES[base_entry["weaponType"]]
        old_type = cur.execute(
            """SELECT wt.name FROM weapons w JOIN weapon_types wt ON wt.id = w.weapon_type_id
               WHERE w.id = ?""", (weapon_id,)).fetchone()[0]
        if old_type != game_type:
            type_fixes.append((name, old_type, game_type))
            cur.execute("UPDATE weapons SET weapon_type_id = ? WHERE id = ?",
                        (type_ids[game_type], weapon_id))

        # Base damage (Standard, +0) in the weapons table
        base_attack = dict(base_entry["attack"])
        base_damage = [int(base_attack.get(t, 0)) for t in DAMAGE_TYPES]
        old_damage = cur.execute(
            """SELECT physical_damage, magic_damage, fire_damage, lightning_damage, holy_damage
               FROM weapons WHERE id = ?""", (weapon_id,)).fetchone()
        if list(old_damage) != base_damage:
            damage_fixes += 1
            cur.execute(
                """UPDATE weapons SET physical_damage = ?, magic_damage = ?, fire_damage = ?,
                                      lightning_damage = ?, holy_damage = ?
                   WHERE id = ?""", (*base_damage, weapon_id))

        # Requirements are the same for every affinity
        req = base_entry["requirements"]
        new_req = [req.get(a, 0) for a in ATTRIBUTES]
        old_req = cur.execute(
            """SELECT strength, dexterity, intelligence, faith, arcane
               FROM weapon_requirements WHERE weapon_id = ?""", (weapon_id,)).fetchone()
        if old_req is None or [x or 0 for x in old_req] != new_req:
            requirement_fixes += 1
            cur.execute("DELETE FROM weapon_requirements WHERE weapon_id = ?", (weapon_id,))
            cur.execute(
                """INSERT INTO weapon_requirements (weapon_id, strength, dexterity, intelligence, faith, arcane)
                   VALUES (?, ?, ?, ?, ?, ?)""", (weapon_id, *new_req))

        cur.execute("DELETE FROM weapon_scaling WHERE weapon_id = ?", (weapon_id,))
        cur.execute("DELETE FROM weapon_damage WHERE weapon_id = ?", (weapon_id,))

        for entry in entries:
            affinity_id = affinity_ids[AFFINITIES[entry["affinityId"]]]
            base = dict(entry["attributeScaling"])
            attack = dict(entry["attack"])
            levels = reinforce[str(entry["reinforceTypeId"])]

            for level, multipliers in enumerate(levels):
                damage = [math.floor(attack.get(t, 0) * multipliers["attack"][str(t)] + 1e-9)
                          for t in DAMAGE_TYPES]
                status = status_buildup(entry, multipliers)
                buildup = [status.get(t, 0) for t in STATUS_TYPES]
                cur.execute(
                    """INSERT INTO weapon_damage (
                           weapon_id, affinity_id, upgrade_level,
                           physical_damage, magic_damage, fire_damage, lightning_damage, holy_damage,
                           poison_buildup, scarlet_rot_buildup, bleed_buildup, frost_buildup,
                           sleep_buildup, madness_buildup, death_blight_buildup)
                       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
                    (weapon_id, affinity_id, level, *damage, *buildup))

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

    print(f"Imported {imported} weapons ({rows} scaling rows, {rows} damage rows)")
    print(f"Corrected base damage for {damage_fixes} weapons")
    print(f"Corrected requirements for {requirement_fixes} weapons")
    print(f"Corrected weapon type for {len(type_fixes)} weapons:")
    for fix in type_fixes:
        print(f"  {fix[0]}: {fix[1]} -> {fix[2]}")
    print(f"Corrected upgrade path for {len(path_fixes)} weapons:")
    for fix in path_fixes:
        print(f"  {fix[0]}: {fix[1]} -> {fix[2]}")
    print(f"Not found in game data ({len(unmatched)}), left unchanged: {', '.join(unmatched)}")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        print(__doc__)
        sys.exit(1)
    main(sys.argv[1], sys.argv[2])

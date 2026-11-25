"""
Imports talismans from the game's own data into elden_ring_weapons.db (table `talismans`, see 008_talismans.sql).

Inputs, exported from your own copy of Elden Ring:
  1. EquipParamAccessory as CSV (e.g. Smithbox: Param Editor -> EquipParamAccessory -> export as CSV).
     Used columns: ID, weight, sellValue, accessoryGroup (header names are matched without case).
  2. A folder with the talisman text files as XML, unpacked from msg/engus/item.msgbnd.dcx
     (e.g. with WitchyBND). The folder is searched recursively for:
       AccessoryName*.fmg.xml     -> name
       AccessoryInfo*.fmg.xml     -> effect (short text)
       AccessoryCaption*.fmg.xml  -> description
     Shadow of the Erdtree text is in the *_dlc01 / *_dlc02 files. A talisman whose name only
     appears in a DLC file is marked as DLC.
  3. SpEffectParam as CSV (exported the same way as 1). Each talisman's refId and residentSpEffectId1-4
     point to rows there; their numbers become rows in `talisman_stats` (see 009_talisman_stats.sql),
     e.g. maxHpRate 1.06 -> max_hp +6 %.

Only param rows that have a name in the text files are imported (the param also holds unused rows).
All existing rows in `talismans` and `talisman_stats` are replaced, so the script can be run again after a game patch.

Usage:
    python import_talismans.py <EquipParamAccessory.csv> <text folder> <SpEffectParam.csv> <path to elden_ring_weapons.db>
"""
import csv
import re
import sqlite3
import sys
import xml.etree.ElementTree as ET
from collections import Counter
from pathlib import Path

# Placeholder texts the game uses for rows without real text
PLACEHOLDERS = {"", "[ERROR]", "%null%", "null"}


def dlc_rank(path):
    """Sort key so the base game file comes first, then _dlc01, then _dlc02."""
    match = re.search(r"_dlc(\d+)", path.name, re.IGNORECASE)
    return int(match.group(1)) if match else 0


def read_texts(folder, prefix):
    """Reads every <prefix>*.fmg.xml under folder.
    Returns (id -> text, ids found in the base game file)."""
    # Exact match, so "AccessoryName" doesn't also pick up some other "AccessoryNameXyz" file
    files = sorted((p for p in Path(folder).rglob("*.fmg.xml")
                    if re.fullmatch(prefix + r"(_dlc\d+)?\.fmg\.xml", p.name, re.IGNORECASE)),
                   key=dlc_rank)
    texts, base_ids = {}, set()
    for path in files:
        root = ET.parse(path).getroot()
        for element in root.iter():
            text_id = element.get("id")
            if text_id is None:
                continue
            # Some names carry a leftover "[ERROR]" marker in front of the real text
            text = re.sub(r"^\[ERROR\]", "", (element.text or "").strip())
            if text in PLACEHOLDERS:
                continue
            # Later (DLC) files override earlier ones: they sometimes carry updated text
            texts[int(text_id)] = text
            if dlc_rank(path) == 0:
                base_ids.add(int(text_id))
    return texts, base_ids, files


def read_param(csv_path):
    """Reads the EquipParamAccessory CSV. Returns a list of dicts with lower-case keys."""
    with open(csv_path, newline="", encoding="utf-8-sig") as f:
        sample = f.read(4096)
        f.seek(0)
        dialect = csv.Sniffer().sniff(sample, delimiters=",;\t")
        reader = csv.DictReader(f, dialect=dialect)
        return [{(k or "").strip().lower(): (v or "").strip() for k, v in row.items()} for row in reader]


# ---------- Stats from SpEffectParam ----------

ELEMENTS = ("physics", "magic", "fire", "thunder", "dark")
ELEMENT_KEYS = {"physics": "physical", "magic": "magic", "fire": "fire", "thunder": "lightning", "dark": "holy"}
DAMAGE_CUT = ("slash", "blow", "thrust", "neutral") + ELEMENTS[1:]   # physical is split into 4 types here

# column -> (stat key, how to turn the value into an amount, is_percent)
RATE = lambda v: (v - 1) * 100          # 1.06 -> +6 %
FLAT = lambda v: v
SIMPLE = {
    "maxHpRate": ("max_hp", RATE, True),
    "maxMpRate": ("max_fp", RATE, True),
    "maxStaminaRate": ("max_stamina", RATE, True),
    "equipWeightChangeRate": ("equip_load", RATE, True),
    "soulRate": ("runes", RATE, True),
    "itemDropRate": ("item_discovery", lambda v: v * 100, False),   # 0.75 -> +75 discovery
    "staminaRecoverChangeSpeed": ("stamina_recovery", FLAT, False),
    "staminaAttackRate": ("stance_damage", RATE, True),
    "toughnessDamageCutRate": ("poise_damage_taken", RATE, True),
    "extendLifeRate": ("buff_duration", RATE, True),
    "magicConsumptionRate": ("spell_fp_cost", RATE, True),
    "artsConsumptionRate": ("skill_fp_cost", RATE, True),
    "changeHpEstusFlaskCorrectRate": ("hp_flask", RATE, True),
    "changeMpEstusFlaskCorrectRate": ("fp_flask", RATE, True),
    "guardStaminaMult": ("guard_stamina_cost", RATE, True),
    "bowDistRate": ("bow_range", FLAT, True),
    "fallDamageRate": ("fall_damage", RATE, True),
    "changeMagicSlot": ("memory_slots", FLAT, False),
    "dexterityCancelSystemOnlyAddDexterity": ("casting_speed", FLAT, False),
    "addLifeForceStatus": ("vigor", FLAT, False),
    "addWillpowerStatus": ("mind", FLAT, False),
    "addEndureStatus": ("endurance", FLAT, False),
    "addStrengthStatus": ("strength", FLAT, False),
    "addDexterityStatus": ("dexterity", FLAT, False),
    "addMagicStatus": ("intelligence", FLAT, False),
    "addFaithStatus": ("faith", FLAT, False),
    "addLuckStatus": ("arcane", FLAT, False),
    "changePoisonResistPoint": ("immunity_poison", FLAT, False),
    "changeDiseaseResistPoint": ("immunity_rot", FLAT, False),
    "changeBloodResistPoint": ("robustness_bleed", FLAT, False),
    "changeFreezeResistPoint": ("robustness_frost", FLAT, False),
    "changeSleepResistPoint": ("focus_sleep", FLAT, False),
    "changeMadnessResistPoint": ("focus_madness", FLAT, False),
    "changeCurseResistPoint": ("vitality_death", FLAT, False),
}
# Effects started by this one (timers, stacks, on-kill, on-trigger); their stats count for the talisman too
LINKS = ("cycleOccurrenceSpEffectId", "accumuOverFireId", "applyIdOnGetSoul", "spiritDeathSpEffectId", "replaceSpEffectId")


def read_speffects(csv_path):
    rows = read_param(csv_path)
    if not rows or "id" not in rows[0]:
        sys.exit(f"{csv_path} has no ID column; is it a SpEffectParam CSV export?")
    # The most common value per column is the "does nothing" default (1 for rates, 0 or -1 otherwise)
    defaults = {c: Counter(r[c] for r in rows).most_common(1)[0][0] for c in rows[0]}
    return {r["id"]: r for r in rows}, defaults


def grouped(row, defaults, columns, key_of, prefix):
    """Element columns with the same value become one stat: all of several -> "<prefix>_all", else one per element."""
    by_value = {}
    for element, column in columns:
        value = row.get(column.lower(), "")
        if value and value != defaults.get(column.lower()):
            by_value.setdefault(float(value), []).append(element)
    stats = []
    for value, elements in by_value.items():
        if len(columns) > 1 and len(elements) == len(columns):
            stats.append((f"{prefix}_all", value))
        else:
            stats += [(f"{prefix}_{key_of(e)}", value) for e in elements]
    return stats


def effect_stats(effect_id, effects, defaults, seen):
    """Stats of one SpEffect row plus the effects it links to, as (stat, amount, is_percent, interval_s)."""
    row = effects.get(effect_id)
    if row is None or effect_id in seen or effect_id in ("0", "-1"):
        return []
    seen.add(effect_id)
    changed = lambda c: row.get(c.lower(), "") not in ("", defaults.get(c.lower()))
    stats = []

    for column, (stat, convert, is_percent) in SIMPLE.items():
        if changed(column):
            stats.append((stat, convert(float(row[column.lower()])), is_percent, None))

    # Attack: plain multipliers, physical-only multipliers (Blue Dancer) and "vs enemy" damage correction.
    # The "vs player" (PvP) corrections are left out.
    for stat, value in (grouped(row, defaults, [(e, f"{e}AttackRate") for e in ELEMENTS], ELEMENT_KEYS.get, "attack")
                        + grouped(row, defaults, [("physics", "physicsAttackPowerRate")], ELEMENT_KEYS.get, "attack")
                        + grouped(row, defaults, [(e, f"atkEnemyDmgCorrectRate_{'Thunder' if e == 'thunder' else e.capitalize()}")
                                                  for e in ELEMENTS], ELEMENT_KEYS.get, "attack")):
        stats.append((stat, RATE(value), True, None))

    # Damage taken (negative = less damage)
    for stat, value in (grouped(row, defaults, [(e, f"{e}DamageCutRate") for e in DAMAGE_CUT],
                                lambda e: ELEMENT_KEYS.get(e, "physical"), "damage_taken")
                        + grouped(row, defaults, [(e, f"defEnemyDmgCorrectRate_{'Thunder' if e == 'thunder' else e.capitalize()}")
                                                  for e in ELEMENTS], ELEMENT_KEYS.get, "damage_taken")):
        stats.append((stat, RATE(value), True, None))

    # Healing: negative change = restore. Repeats every motionInterval seconds only on permanent effects.
    interval = float(row.get("motioninterval") or 0)
    every = interval if interval > 0 and row.get("effectendurance") == "-1" else None
    for column, stat, is_percent in (("changeHpRate", "hp_restore", True), ("changeHpPoint", "hp_restore", False),
                                     ("changeMpPoint", "fp_restore", False)):
        if changed(column):
            stats.append((stat, -float(row[column.lower()]), is_percent, every))

    for link in LINKS:
        if changed(link):
            stats += effect_stats(row[link.lower()], effects, defaults, seen)
    return stats


def talisman_stats(param_row, effects, defaults):
    ids = [param_row.get("refid", "")] + [param_row.get(f"residentspeffectid{i}", "") for i in range(1, 5)]
    seen, stats = set(), []
    for effect_id in ids:
        stats += effect_stats(effect_id, effects, defaults, seen)
    # A stack of the same effect repeats a stat with the same value; keep each (stat, value) once
    unique = []
    for stat in stats:
        rounded = (stat[0], round(stat[1], 2), stat[2], stat[3])
        if rounded not in unique and rounded[1] != 0:
            unique.append(rounded)
    return unique


def number(value, kind):
    try:
        return kind(float(value)) if kind is int else kind(value)
    except (TypeError, ValueError):
        return None


def main(param_csv, text_folder, speffect_csv, db_path):
    names, base_name_ids, name_files = read_texts(text_folder, "AccessoryName")
    effects, _, _ = read_texts(text_folder, "AccessoryInfo")
    descriptions, _, _ = read_texts(text_folder, "AccessoryCaption")
    if not name_files:
        sys.exit(f"No AccessoryName*.fmg.xml files found under {text_folder}")

    rows = read_param(param_csv)
    if not rows or "id" not in rows[0]:
        sys.exit(f"{param_csv} has no ID column; is it an EquipParamAccessory CSV export?")
    missing = [c for c in ("weight", "sellvalue", "accessorygroup") if c not in rows[0]]
    if missing:
        sys.exit(f"{param_csv} is missing columns: {', '.join(missing)}")

    speffects, defaults = read_speffects(speffect_csv)
    talismans, stats, skipped = [], [], 0
    for row in rows:
        talisman_id = number(row["id"], int)
        if talisman_id is None or talisman_id not in names:
            skipped += 1   # unused row, or no name text
            continue
        group = number(row["accessorygroup"], int)
        talismans.append((
            talisman_id,
            names[talisman_id],
            effects.get(talisman_id),
            descriptions.get(talisman_id),
            number(row["weight"], float) or 0.0,
            number(row["sellvalue"], int),
            group if group is not None and group >= 0 else None,
            0 if talisman_id in base_name_ids else 1,
        ))
        stats += [(talisman_id, position, *stat)
                  for position, stat in enumerate(talisman_stats(row, speffects, defaults))]

    con = sqlite3.connect(db_path)
    try:
        if not con.execute("SELECT 1 FROM sqlite_master WHERE name = 'talismans'").fetchone():
            sys.exit("The talismans table doesn't exist yet; run 008_talismans.sql first")
        if not con.execute("SELECT 1 FROM sqlite_master WHERE name = 'talisman_stats'").fetchone():
            sys.exit("The talisman_stats table doesn't exist yet; run 009_talisman_stats.sql first")
        with con:   # one transaction: all rows are replaced, or nothing changes
            con.execute("DELETE FROM talisman_stats")
            con.execute("DELETE FROM talismans")
            con.executemany(
                """INSERT INTO talismans (id, name, effect, description, weight, sell_value, accessory_group, is_dlc)
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?)""", talismans)
            con.executemany(
                """INSERT INTO talisman_stats (talisman_id, position, stat, amount, is_percent, interval_s)
                   VALUES (?, ?, ?, ?, ?, ?)""", stats)
    finally:
        con.close()

    dlc = sum(t[7] for t in talismans)
    no_effect = [t[1] for t in talismans if not t[2]]
    print(f"Imported {len(talismans)} talismans ({len(talismans) - dlc} base game, {dlc} DLC); "
          f"skipped {skipped} param rows without a name")
    with_stats = {s[0] for s in stats}
    print(f"{len(stats)} stats for {len(with_stats)} talismans; no stats in the param data for: "
          + ", ".join(t[1] for t in talismans if t[0] not in with_stats))
    print("Text files read: " + ", ".join(p.name for p in name_files))
    if no_effect:
        print(f"{len(no_effect)} talismans have no effect text: {', '.join(no_effect[:10])}"
              + (" ..." if len(no_effect) > 10 else ""))


if __name__ == "__main__":
    if len(sys.argv) != 5:
        sys.exit(__doc__)
    main(*sys.argv[1:])

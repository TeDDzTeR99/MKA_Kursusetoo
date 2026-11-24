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

Only param rows that have a name in the text files are imported (the param also holds unused rows).
All existing rows in `talismans` are replaced, so the script can be run again after a game patch.

Usage:
    python import_talismans.py <EquipParamAccessory.csv> <text folder> <path to elden_ring_weapons.db>
"""
import csv
import re
import sqlite3
import sys
import xml.etree.ElementTree as ET
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
            text = (element.text or "").strip()
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


def number(value, kind):
    try:
        return kind(float(value)) if kind is int else kind(value)
    except (TypeError, ValueError):
        return None


def main(param_csv, text_folder, db_path):
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

    talismans, skipped = [], 0
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

    con = sqlite3.connect(db_path)
    try:
        if not con.execute("SELECT 1 FROM sqlite_master WHERE name = 'talismans'").fetchone():
            sys.exit("The talismans table doesn't exist yet; run 008_talismans.sql first")
        with con:   # one transaction: all rows are replaced, or nothing changes
            con.execute("DELETE FROM talismans")
            con.executemany(
                """INSERT INTO talismans (id, name, effect, description, weight, sell_value, accessory_group, is_dlc)
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?)""", talismans)
    finally:
        con.close()

    dlc = sum(t[7] for t in talismans)
    no_effect = [t[1] for t in talismans if not t[2]]
    print(f"Imported {len(talismans)} talismans ({len(talismans) - dlc} base game, {dlc} DLC); "
          f"skipped {skipped} param rows without a name")
    print("Text files read: " + ", ".join(p.name for p in name_files))
    if no_effect:
        print(f"{len(no_effect)} talismans have no effect text: {', '.join(no_effect[:10])}"
              + (" ..." if len(no_effect) > 10 else ""))


if __name__ == "__main__":
    if len(sys.argv) != 4:
        sys.exit(__doc__)
    main(*sys.argv[1:])

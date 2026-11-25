"""
Writes the game's item icons to Images/<kind>/<id>.png, where the app looks for them (see ItemImages.cs).

Inputs, from your own copy of Elden Ring:
  1. The folder with the param CSVs (see import_talismans.py). The iconId column of EquipParamWeapon,
     EquipParamAccessory, EquipParamGem and EquipParamGoods (upgrade materials, key items, spells, spirit ashes) says which icon each item uses.
  2. The folder with the icon files MENU_Knowledge_<iconId, 5 digits>.tpf.dcx (unpacked from 00_solo.tpfbdt).
  3. WitchyBND.exe. The icon files are Oodle (KRAK) compressed, which Python cannot open.
  4. elden_ring_weapons.db. Every table's id is the game's param row id, so it matches the CSV ID column.
  5. The Images folder to write to.

Icons are 1024 px; they are saved at 256 px (the app shows at most 128 px). Needs Pillow (pip install pillow).
Existing pictures are overwritten, so the script can be run again after a game patch.

Usage:
    python import_icons.py <param CSV folder> <icon folder> <WitchyBND.exe> <path to elden_ring_weapons.db> <Images folder>
"""
import shutil
import sqlite3
import subprocess
import sys
import tempfile
from pathlib import Path

from PIL import Image

from import_talismans import read_param

SIZE = 256
# Image folder (Category.Kind in Main.cs) -> (DB table, param CSV)
KINDS = {
    "weapon": ("weapons", "EquipParamWeapon.csv"),
    "talisman": ("talismans", "EquipParamAccessory.csv"),
    "upgrade_material": ("upgrade_materials", "EquipParamGoods.csv"),
    "key_item": ("key_items", "EquipParamGoods.csv"),
    "sorcery": ("sorceries", "EquipParamGoods.csv"),
    "incantation": ("incantations", "EquipParamGoods.csv"),
    "ash_of_war": ("ashes_of_war", "EquipParamGem.csv"),
    "spirit_ash": ("spirit_ashes", "EquipParamGoods.csv"),
}


def main(param_folder, icon_folder, witchy, db_path, images_folder):
    con = sqlite3.connect(db_path)
    wanted = {}  # (kind, id) -> iconId
    for kind, (table, csv_name) in KINDS.items():
        icons = {int(row["id"]): int(row["iconid"]) for row in read_param(Path(param_folder) / csv_name)}
        for (item_id,) in con.execute(f"SELECT id FROM {table}"):
            if item_id in icons:
                wanted[(kind, item_id)] = icons[item_id]
            else:
                print(f"no param row for {kind} {item_id}")
    con.close()

    with tempfile.TemporaryDirectory() as work:
        work = Path(work)
        names = {}
        for icon in set(wanted.values()):
            source = Path(icon_folder) / f"MENU_Knowledge_{icon:05d}.tpf.dcx"
            if source.exists():
                shutil.copy(source, work)
                names[icon] = source.name
        # WitchyBND unpacks X.tpf.dcx into the folder X-tpf-dcx next to it.
        # Batches keep the command line under the Windows length limit.
        files = sorted(names.values())
        for start in range(0, len(files), 100):
            subprocess.run([witchy, "--passive", *files[start:start + 100]], cwd=work,
                           stdin=subprocess.DEVNULL, check=True)

        written, missing = 0, []
        for (kind, item_id), icon in sorted(wanted.items()):
            stem = f"MENU_Knowledge_{icon:05d}"
            dds = work / f"{stem}-tpf-dcx" / f"{stem}.dds"
            if not dds.exists():
                missing.append(f"{kind} {item_id} (icon {icon})")
                continue
            target = Path(images_folder) / kind / f"{item_id}.png"
            target.parent.mkdir(parents=True, exist_ok=True)
            with Image.open(dds) as image:
                image.convert("RGBA").resize((SIZE, SIZE), Image.LANCZOS).save(target, optimize=True)
            written += 1

    print(f"{written} pictures written, {len(missing)} missing")
    for line in missing:
        print("  missing:", line)


if __name__ == "__main__":
    if len(sys.argv) != 6:
        sys.exit(__doc__)
    main(*sys.argv[1:])

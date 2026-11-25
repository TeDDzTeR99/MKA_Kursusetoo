-- Migration 017: shields
--
-- Filled by import_shields.py from the game's own data:
--   - EquipParamWeapon (wepType 65 / 67 / 69 / 90 = small, medium, great, thrusting shield; Standard +0 values)
--   - the WeaponName / WeaponCaption text files
-- Shields get their own table: the weapons table comes from another source and has no guard values.

BEGIN TRANSACTION;

CREATE TABLE shields (
    id                     INTEGER PRIMARY KEY,   -- row ID in EquipParamWeapon (Standard, +0)
    name                   TEXT    NOT NULL,
    category               TEXT    NOT NULL,      -- 'small', 'medium', 'great' or 'thrusting', via ShieldKind_<key>
    description            TEXT,
    weight                 REAL    NOT NULL,
    -- Damage negation while blocking, in %, as the game menu shows it
    guard_physical         INTEGER NOT NULL,
    guard_magic            INTEGER NOT NULL,
    guard_fire             INTEGER NOT NULL,
    guard_lightning        INTEGER NOT NULL,
    guard_holy             INTEGER NOT NULL,
    guard_boost            INTEGER NOT NULL,
    physical_damage        INTEGER NOT NULL,
    magic_damage           INTEGER NOT NULL,
    fire_damage            INTEGER NOT NULL,
    lightning_damage       INTEGER NOT NULL,
    holy_damage            INTEGER NOT NULL,
    required_strength      INTEGER NOT NULL,
    required_dexterity     INTEGER NOT NULL,
    required_intelligence  INTEGER NOT NULL,
    required_faith         INTEGER NOT NULL,
    required_arcane        INTEGER NOT NULL,
    -- Scaling grade letters at +0: S, A, B, C, D, E or '-' (same thresholds as scaling_grades)
    strength_scaling       TEXT    NOT NULL,
    dexterity_scaling      TEXT    NOT NULL,
    intelligence_scaling   TEXT    NOT NULL,
    faith_scaling          TEXT    NOT NULL,
    arcane_scaling         TEXT    NOT NULL,
    max_upgrade_level      INTEGER NOT NULL,      -- 25, or 10 for somber shields
    sort_id                INTEGER NOT NULL,
    is_dlc                 INTEGER NOT NULL DEFAULT 0
);

COMMIT;

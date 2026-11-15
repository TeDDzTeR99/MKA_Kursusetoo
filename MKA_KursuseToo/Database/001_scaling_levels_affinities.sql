-- Migration 001: scaling per affinity and upgrade level
--
-- Before: weapon_scaling had one row per weapon.
-- After:  weapon_scaling has one row per (weapon, affinity, upgrade level).
--         Existing rows become (Standard, +0).
--         scaling_grades gets min_value / max_value so grade ranges live in the DB.

PRAGMA foreign_keys = OFF;
BEGIN TRANSACTION;

-- 1. Affinities -------------------------------------------------------------
CREATE TABLE affinities (
    id    INTEGER PRIMARY KEY AUTOINCREMENT,
    name  TEXT NOT NULL UNIQUE
);

INSERT INTO affinities (name) VALUES
    ('Standard'), ('Heavy'), ('Keen'), ('Quality'),
    ('Fire'), ('Flame Art'), ('Lightning'), ('Sacred'),
    ('Magic'), ('Cold'), ('Poison'), ('Blood'), ('Occult');

-- 2. Grade ranges -------------------------------------------------------------
ALTER TABLE scaling_grades ADD COLUMN min_value INTEGER;
ALTER TABLE scaling_grades ADD COLUMN max_value INTEGER;   -- NULL = no upper limit

UPDATE scaling_grades SET min_value = 175, max_value = NULL WHERE grade = 'S';
UPDATE scaling_grades SET min_value = 140, max_value = 174  WHERE grade = 'A';
UPDATE scaling_grades SET min_value = 90,  max_value = 139  WHERE grade = 'B';
UPDATE scaling_grades SET min_value = 60,  max_value = 89   WHERE grade = 'C';
UPDATE scaling_grades SET min_value = 25,  max_value = 59   WHERE grade = 'D';
UPDATE scaling_grades SET min_value = 1,   max_value = 24   WHERE grade = 'E';

-- 3. Rebuild weapon_scaling with affinity + upgrade level ----------------------
-- The overview view depends on weapon_scaling, so drop it first and recreate it below.
DROP VIEW IF EXISTS v_weapons_overview;

CREATE TABLE weapon_scaling_new (
    id             INTEGER PRIMARY KEY AUTOINCREMENT,
    weapon_id      INTEGER NOT NULL REFERENCES weapons(id) ON DELETE CASCADE,
    affinity_id    INTEGER NOT NULL REFERENCES affinities(id),
    upgrade_level  INTEGER NOT NULL DEFAULT 0,    -- 0-25 (Standard) or 0-10 (Somber)

    strength_grade_id     INTEGER REFERENCES scaling_grades(id),
    dexterity_grade_id    INTEGER REFERENCES scaling_grades(id),
    intelligence_grade_id INTEGER REFERENCES scaling_grades(id),
    faith_grade_id        INTEGER REFERENCES scaling_grades(id),
    arcane_grade_id       INTEGER REFERENCES scaling_grades(id),

    -- Exact scaling numbers, NULL when unknown
    strength_value     INTEGER,
    dexterity_value    INTEGER,
    intelligence_value INTEGER,
    faith_value        INTEGER,
    arcane_value       INTEGER,

    UNIQUE (weapon_id, affinity_id, upgrade_level)
);

INSERT INTO weapon_scaling_new (
    weapon_id, affinity_id, upgrade_level,
    strength_grade_id, dexterity_grade_id, intelligence_grade_id, faith_grade_id, arcane_grade_id,
    strength_value, dexterity_value, intelligence_value, faith_value, arcane_value)
SELECT
    weapon_id, (SELECT id FROM affinities WHERE name = 'Standard'), 0,
    strength_grade_id, dexterity_grade_id, intelligence_grade_id, faith_grade_id, arcane_grade_id,
    strength_value, dexterity_value, intelligence_value, faith_value, arcane_value
FROM weapon_scaling;

DROP TABLE weapon_scaling;
ALTER TABLE weapon_scaling_new RENAME TO weapon_scaling;

-- 4. Recreate the overview view (Standard affinity, +0) -----------------------
CREATE VIEW v_weapons_overview AS
SELECT
    w.id,
    w.name                              AS weapon_name,
    wt.name                             AS weapon_type,
    wt.category                         AS weapon_category,
    w.physical_damage,
    w.magic_damage,
    w.fire_damage,
    w.lightning_damage,
    w.holy_damage,
    w.weight,
    w.critical,
    w.guard_boost,
    w.upgrade_path,
    w.max_upgrade_level,
    w.passive_effect,
    w.location,
    COALESCE(r.strength,    0)          AS req_str,
    COALESCE(r.dexterity,   0)          AS req_dex,
    COALESCE(r.intelligence,0)          AS req_int,
    COALESCE(r.faith,       0)          AS req_fai,
    COALESCE(r.arcane,      0)          AS req_arc,
    gs_str.grade                        AS scale_str,
    gs_dex.grade                        AS scale_dex,
    gs_int.grade                        AS scale_int,
    gs_fai.grade                        AS scale_fai,
    gs_arc.grade                        AS scale_arc
FROM weapons w
JOIN  weapon_types       wt     ON wt.id    = w.weapon_type_id
LEFT JOIN weapon_requirements r ON r.weapon_id = w.id
LEFT JOIN weapon_scaling      s ON s.weapon_id = w.id
                               AND s.upgrade_level = 0
                               AND s.affinity_id = (SELECT id FROM affinities WHERE name = 'Standard')
LEFT JOIN scaling_grades gs_str ON gs_str.id = s.strength_grade_id
LEFT JOIN scaling_grades gs_dex ON gs_dex.id = s.dexterity_grade_id
LEFT JOIN scaling_grades gs_int ON gs_int.id = s.intelligence_grade_id
LEFT JOIN scaling_grades gs_fai ON gs_fai.id = s.faith_grade_id
LEFT JOIN scaling_grades gs_arc ON gs_arc.id = s.arcane_grade_id;

COMMIT;
PRAGMA foreign_keys = ON;

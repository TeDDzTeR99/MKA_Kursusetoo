-- Migration 003: attack damage per affinity and upgrade level
--
-- Same key as weapon_scaling: one row per (weapon, affinity, upgrade level).
-- The damage columns in `weapons` stay as the base values (Standard affinity, +0).
-- Filled by import_from_game_data.py.

BEGIN TRANSACTION;

CREATE TABLE weapon_damage (
    id               INTEGER PRIMARY KEY AUTOINCREMENT,
    weapon_id        INTEGER NOT NULL REFERENCES weapons(id) ON DELETE CASCADE,
    affinity_id      INTEGER NOT NULL REFERENCES affinities(id),
    upgrade_level    INTEGER NOT NULL DEFAULT 0,

    physical_damage  INTEGER NOT NULL DEFAULT 0,
    magic_damage     INTEGER NOT NULL DEFAULT 0,
    fire_damage      INTEGER NOT NULL DEFAULT 0,
    lightning_damage INTEGER NOT NULL DEFAULT 0,
    holy_damage      INTEGER NOT NULL DEFAULT 0,

    UNIQUE (weapon_id, affinity_id, upgrade_level)
);

COMMIT;

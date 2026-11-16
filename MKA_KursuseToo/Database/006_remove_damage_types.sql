-- Migration 006: remove the unused damage type tables
--
-- damage_types was meant to hold the weapon variants (Fire, Magic, Holy, ...). That job is now done
-- by the affinities table (001), and damage per element is stored in weapon_damage (003).
-- weapon_damage_types was never filled in, and nothing in the app uses either table.

BEGIN TRANSACTION;

DROP TABLE IF EXISTS weapon_damage_types;
DROP TABLE IF EXISTS damage_types;

COMMIT;

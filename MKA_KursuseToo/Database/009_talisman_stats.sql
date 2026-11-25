-- Migration 009: talisman stats
--
-- The numbers behind each talisman's effect (e.g. Crimson Amber Medallion: max HP +6%),
-- filled by import_talismans.py from SpEffectParam. One row per stat; a talisman with
-- stacking or conditional effects (e.g. Winged Sword Insignia: +3% / +5% / +10%) has
-- several rows for the same stat. Talismans whose effect isn't in the param data have no rows.

BEGIN TRANSACTION;

CREATE TABLE talisman_stats (
    talisman_id  INTEGER NOT NULL REFERENCES talismans(id),
    position     INTEGER NOT NULL,              -- display order within the talisman
    stat         TEXT    NOT NULL,              -- key, shown via the Stat_<key> resource text, e.g. "max_hp"
    amount       REAL    NOT NULL,              -- e.g. 6 for +6%, -10 for -10%, 5 for +5 Strength
    is_percent   INTEGER NOT NULL DEFAULT 0,
    interval_s   REAL,                          -- for regeneration: amount per this many seconds
    PRIMARY KEY (talisman_id, position)
);

COMMIT;

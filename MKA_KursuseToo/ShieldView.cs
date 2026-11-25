using System;
using System.Collections.Generic;
using System.Data;
using MKA_KursuseToo.Properties;

namespace MKA_KursuseToo
{
    // Details page for one shield (Standard affinity, +0)
    public class ShieldView : InfoboxView
    {
        private readonly int _shieldId;

        // Each labelled with its Field_<column> resource text (the damage, requirement and scaling labels
        // are the weapon pages' ones). Zero damage / requirements and "-" scaling are left out, like in the game.
        private static readonly string[] Guards =
            { "guard_physical", "guard_magic", "guard_fire", "guard_lightning", "guard_holy", "guard_boost" };
        private static readonly string[] Damage =
            { "physical_damage", "magic_damage", "fire_damage", "lightning_damage", "holy_damage" };
        private static readonly string[] Stats = { "strength", "dexterity", "intelligence", "faith", "arcane" };

        public ShieldView(int shieldId) : base("shield", shieldId)
        {
            _shieldId = shieldId;
        }

        protected override string NotFoundMessage => Resources.ShieldNotFound;

        protected override string LoadDetails(DataTable rows)
        {
            var dt = WeaponDatabase.Query(
                @"SELECT * FROM shields WHERE id = @id;",
                new Dictionary<string, object> { { "@id", _shieldId } });

            if (dt.Rows.Count == 0)
                return null;

            var row = dt.Rows[0];
            AddRow(rows, "weight", Convert.ToDouble(row["weight"]).ToString("0.0"));
            foreach (var column in Guards)
                AddRow(rows, column, row[column].ToString());
            foreach (var column in Damage)
                if (Convert.ToInt64(row[column]) > 0)
                    AddRow(rows, column, row[column].ToString());
            foreach (var stat in Stats)
                if (Convert.ToInt64(row["required_" + stat]) > 0)
                    AddRow(rows, "required_" + stat, row["required_" + stat].ToString());
            foreach (var stat in Stats)
                if (row[stat + "_scaling"].ToString() != "-")
                    AddRow(rows, stat + "_scaling", row[stat + "_scaling"].ToString());
            AddRow(rows, "max_upgrade_level", "+" + row["max_upgrade_level"]);
            if (Convert.ToInt64(row["is_dlc"]) == 1)
                AddRow(rows, "dlc", Resources.ShadowOfTheErdtree);
            AddRow(rows, "description", row["description"]?.ToString());

            return row["name"].ToString();
        }
    }
}

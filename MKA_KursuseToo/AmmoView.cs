using System;
using System.Collections.Generic;
using System.Data;
using MKA_KursuseToo.Properties;

namespace MKA_KursuseToo
{
    // Details page for one arrow or bolt
    public class AmmoView : InfoboxView
    {
        private readonly int _ammoId;

        // Labelled with the weapon pages' Field_<column> texts; zero damage is left out, like in the game
        private static readonly string[] Damage =
            { "physical_damage", "magic_damage", "fire_damage", "lightning_damage", "holy_damage" };

        public AmmoView(int ammoId) : base("ammo", ammoId)
        {
            _ammoId = ammoId;
        }

        protected override string NotFoundMessage => Resources.ArrowOrBoltNotFound;

        protected override string LoadDetails(DataTable rows)
        {
            var dt = WeaponDatabase.Query(
                @"SELECT * FROM ammunition WHERE id = @id;",
                new Dictionary<string, object> { { "@id", _ammoId } });

            if (dt.Rows.Count == 0)
                return null;

            var row = dt.Rows[0];
            foreach (var column in Damage)
                if (Convert.ToInt64(row[column]) > 0)
                    AddRow(rows, column, row[column].ToString());
            AddRow(rows, "max_quantity", row["max_quantity"].ToString());
            AddRow(rows, "sell_value", row["sell_value"]?.ToString());
            if (Convert.ToInt64(row["is_dlc"]) == 1)
                AddRow(rows, "dlc", Resources.ShadowOfTheErdtree);
            AddRow(rows, "description", row["description"]?.ToString());

            return row["name"].ToString();
        }
    }
}

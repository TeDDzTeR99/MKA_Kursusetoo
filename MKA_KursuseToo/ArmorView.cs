using System;
using System.Collections.Generic;
using System.Data;
using MKA_KursuseToo.Properties;

namespace MKA_KursuseToo
{
    // Details page for one armor piece: weight, poise, damage negation and resistances like the in-game menu
    public class ArmorView : InfoboxView
    {
        private readonly int _armorId;

        // Shown in this order, each labelled with its Field_<column> resource text
        private static readonly string[] Negations =
            { "physical", "strike", "slash", "pierce", "magic", "fire", "lightning", "holy" };
        private static readonly string[] Resistances = { "immunity", "robustness", "focus", "vitality" };

        public ArmorView(int armorId) : base("armor", armorId)
        {
            _armorId = armorId;
        }

        protected override string NotFoundMessage => Resources.ArmorNotFound;

        protected override string LoadDetails(DataTable rows)
        {
            var dt = WeaponDatabase.Query(
                @"SELECT * FROM armor WHERE id = @id;",
                new Dictionary<string, object> { { "@id", _armorId } });

            if (dt.Rows.Count == 0)
                return null;

            var row = dt.Rows[0];
            AddRow(rows, "effect", row["effect"]?.ToString());
            AddRow(rows, "weight", OneDecimal(row["weight"]));
            AddRow(rows, "poise", row["poise"].ToString());
            foreach (var column in Negations)
                AddRow(rows, column, OneDecimal(row[column]));
            foreach (var column in Resistances)
                AddRow(rows, column, row[column].ToString());
            AddRow(rows, "sell_value", row["sell_value"]?.ToString());
            if (Convert.ToInt64(row["is_dlc"]) == 1)
                AddRow(rows, "dlc", Resources.ShadowOfTheErdtree);
            AddRow(rows, "description", row["description"]?.ToString());

            return row["name"].ToString();
        }

        // Like the game menu: 7.0, not 7
        private static string OneDecimal(object value) => Convert.ToDouble(value).ToString("0.0");
    }
}

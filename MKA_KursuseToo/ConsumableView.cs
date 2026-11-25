using System;
using System.Collections.Generic;
using System.Data;
using MKA_KursuseToo.Properties;

namespace MKA_KursuseToo
{
    // Details page for one consumable (pot, grease, food, rune, ...)
    public class ConsumableView : InfoboxView
    {
        private readonly int _consumableId;

        public ConsumableView(int consumableId) : base("consumable", consumableId)
        {
            _consumableId = consumableId;
        }

        protected override string NotFoundMessage => Resources.ConsumableNotFound;

        protected override string LoadDetails(DataTable rows)
        {
            var dt = WeaponDatabase.Query(
                @"SELECT * FROM consumables WHERE id = @id;",
                new Dictionary<string, object> { { "@id", _consumableId } });

            if (dt.Rows.Count == 0)
                return null;

            // Empty values (no FP cost, can't be stored or sold) are skipped by AddRow
            var row = dt.Rows[0];
            AddRow(rows, "effect", row["effect"]?.ToString());
            AddRow(rows, "fp", row["fp"]?.ToString());
            AddRow(rows, "max_quantity", row["max_quantity"].ToString());
            AddRow(rows, "max_stored", row["max_stored"]?.ToString());
            AddRow(rows, "sell_value", row["sell_value"]?.ToString());
            if (Convert.ToInt64(row["is_dlc"]) == 1)
                AddRow(rows, "dlc", Resources.ShadowOfTheErdtree);
            AddRow(rows, "description", row["description"]?.ToString());

            return row["name"].ToString();
        }
    }
}

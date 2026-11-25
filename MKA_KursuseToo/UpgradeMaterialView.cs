using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MKA_KursuseToo.Properties;

namespace MKA_KursuseToo
{
    // Details page for one upgrade material (smithing stone, glovewort, ...)
    public class UpgradeMaterialView : InfoboxView
    {
        private readonly int _materialId;

        public UpgradeMaterialView(int materialId) : base("upgrade_material", materialId)
        {
            _materialId = materialId;
        }

        protected override string NotFoundMessage => Resources.UpgradeMaterialNotFound;

        protected override string LoadDetails(DataTable rows)
        {
            var dt = WeaponDatabase.Query(
                @"SELECT name, effect, description, sell_value, is_dlc
                  FROM upgrade_materials
                  WHERE id = @id;",
                new Dictionary<string, object> { { "@id", _materialId } });

            if (dt.Rows.Count == 0)
                return null;

            var row = dt.Rows[0];
            AddRow(rows, "effect", row["effect"]?.ToString());
            AddCostRows(rows);
            AddRow(rows, "sell_value", row["sell_value"]?.ToString());
            if (Convert.ToInt64(row["is_dlc"]) == 1)
                AddRow(rows, "dlc", Resources.ShadowOfTheErdtree);
            AddRow(rows, "description", row["description"]?.ToString());

            return row["name"].ToString();
        }

        // One row per kind of item it upgrades, e.g. "Weapon upgrade: +1 (×2), +2 (×4), +3 (×6)"
        private void AddCostRows(DataTable table)
        {
            var costs = WeaponDatabase.Query(
                @"SELECT item_kind, target_level, amount
                  FROM upgrade_material_costs
                  WHERE material_id = @id
                  ORDER BY item_kind, target_level;",
                new Dictionary<string, object> { { "@id", _materialId } });

            foreach (var kind in costs.Rows.Cast<DataRow>().GroupBy(r => r["item_kind"].ToString()))
            {
                var levels = kind.Select(r => $"+{r["target_level"]} (×{r["amount"]})");
                AddRow(table, "upgrades_" + kind.Key, string.Join(", ", levels));
            }
        }
    }
}

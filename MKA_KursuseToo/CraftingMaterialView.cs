using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MKA_KursuseToo.Properties;

namespace MKA_KursuseToo
{
    // Details page for one crafting material
    public class CraftingMaterialView : InfoboxView
    {
        private readonly int _materialId;

        public CraftingMaterialView(int materialId) : base("crafting_material", materialId)
        {
            _materialId = materialId;
        }

        protected override string NotFoundMessage => Resources.CraftingMaterialNotFound;

        protected override string LoadDetails(DataTable rows)
        {
            var dt = WeaponDatabase.Query(
                @"SELECT * FROM crafting_materials WHERE id = @id;",
                new Dictionary<string, object> { { "@id", _materialId } });

            if (dt.Rows.Count == 0)
                return null;

            var row = dt.Rows[0];
            AddRow(rows, "max_quantity", row["max_quantity"].ToString());
            AddRow(rows, "max_stored", row["max_stored"]?.ToString());
            AddRow(rows, "used_to_craft", UsedToCraft());
            AddRow(rows, "sell_value", row["sell_value"]?.ToString());
            if (Convert.ToInt64(row["is_dlc"]) == 1)
                AddRow(rows, "dlc", Resources.ShadowOfTheErdtree);
            AddRow(rows, "description", row["description"]?.ToString());

            return row["name"].ToString();
        }

        // Every recipe that needs this material, with how many one craft takes, like the upgrade materials' pages:
        // "Furlcalling Finger Remedy (×2), Grace Mimic (×1)"
        private string UsedToCraft()
        {
            var uses = WeaponDatabase.Query(
                @"SELECT item_name, amount FROM crafting_material_uses WHERE material_id = @id ORDER BY position;",
                new Dictionary<string, object> { { "@id", _materialId } });
            return string.Join(", ", uses.Rows.Cast<DataRow>().Select(r => $"{r["item_name"]} (×{r["amount"]})"));
        }
    }
}

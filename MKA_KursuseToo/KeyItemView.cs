using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MKA_KursuseToo.Properties;

namespace MKA_KursuseToo
{
    // Details page for one key item (key, map, cookbook, bell bearing, ...)
    public class KeyItemView : InfoboxView
    {
        private readonly int _itemId;

        public KeyItemView(int itemId) : base("key_item", itemId)
        {
            _itemId = itemId;
        }

        protected override string NotFoundMessage => Resources.KeyItemNotFound;

        protected override string LoadDetails(DataTable rows)
        {
            var dt = WeaponDatabase.Query(
                @"SELECT name, effect, description, sell_value, category, is_dlc
                  FROM key_items
                  WHERE id = @id;",
                new Dictionary<string, object> { { "@id", _itemId } });

            if (dt.Rows.Count == 0)
                return null;

            var row = dt.Rows[0];
            AddRow(rows, "effect", row["effect"]?.ToString());
            AddUnlocksRow(rows, row["category"].ToString());
            AddRow(rows, "sell_value", row["sell_value"]?.ToString());
            if (Convert.ToInt64(row["is_dlc"]) == 1)
                AddRow(rows, "dlc", Resources.ShadowOfTheErdtree);
            AddRow(rows, "description", row["description"]?.ToString());

            return row["name"].ToString();
        }

        // Bell bearings: what the Twin Maiden Husks sell once it's given to them, e.g. "Smithing Stone [1]
        // (200 runes), Smithing Stone [2] (400 runes)". Cookbooks: the recipes they teach (no price).
        // Labelled with the Field_unlocks_<category> resource. Other items have no such rows.
        private void AddUnlocksRow(DataTable table, string category)
        {
            var unlocks = WeaponDatabase.Query(
                @"SELECT name, price FROM key_item_unlocks
                  WHERE key_item_id = @id
                  ORDER BY position;",
                new Dictionary<string, object> { { "@id", _itemId } });

            var items = unlocks.Rows.Cast<DataRow>()
                .Select(u => u["price"] == DBNull.Value
                    ? u["name"].ToString()
                    : $"{u["name"]} ({string.Format(Resources.PriceRunes, u["price"])})");
            AddRow(table, "unlocks_" + category, string.Join(", ", items));   // empty -> no row
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using MKA_KursuseToo.Properties;

namespace MKA_KursuseToo
{
    // Details page for one talisman
    public class TalismanView : InfoboxView
    {
        private readonly int _talismanId;

        public TalismanView(int talismanId) : base("talisman", talismanId)
        {
            _talismanId = talismanId;
        }

        protected override string NotFoundMessage => Resources.TalismanNotFound;

        protected override string LoadDetails(DataTable rows)
        {
            var dt = WeaponDatabase.Query(
                @"SELECT name, effect, description, weight, sell_value, accessory_group, is_dlc
                  FROM talismans
                  WHERE id = @id;",
                new Dictionary<string, object> { { "@id", _talismanId } });

            if (dt.Rows.Count == 0)
                return null;

            var row = dt.Rows[0];
            AddRow(rows, "effect", row["effect"]?.ToString());
            AddStatRows(rows);
            AddRow(rows, "weight", Convert.ToDouble(row["weight"]).ToString("0.0", CultureInfo.CurrentCulture));
            AddRow(rows, "sell_value", row["sell_value"]?.ToString());
            AddRow(rows, "cannot_equip_with", ConflictingTalismans(row["accessory_group"]));
            if (Convert.ToInt64(row["is_dlc"]) == 1)
                AddRow(rows, "dlc", Resources.ShadowOfTheErdtree);
            AddRow(rows, "description", row["description"]?.ToString());

            return row["name"].ToString();
        }

        // One row per stat, e.g. "Max HP: +6%". A stat with several values (stacking or conditional
        // effects) shows them all in one row: "Attack power: +3%, +5%, +10%".
        private void AddStatRows(DataTable table)
        {
            var stats = WeaponDatabase.Query(
                @"SELECT stat, amount, is_percent, interval_s
                  FROM talisman_stats
                  WHERE talisman_id = @id
                  ORDER BY position;",
                new Dictionary<string, object> { { "@id", _talismanId } });

            foreach (var group in stats.Rows.Cast<DataRow>().GroupBy(r => r["stat"].ToString()))
            {
                var values = group.Select(r =>
                {
                    var text = Convert.ToDouble(r["amount"]).ToString("+0.##;-0.##", CultureInfo.CurrentCulture)
                               + (Convert.ToInt64(r["is_percent"]) == 1 ? "%" : "");
                    if (r["interval_s"] != DBNull.Value)
                        text += " " + string.Format(Resources.Stat_every,
                            Convert.ToDouble(r["interval_s"]).ToString("0.##", CultureInfo.CurrentCulture));
                    return text;
                });
                var label = Resources.ResourceManager.GetString("Stat_" + group.Key, Resources.Culture) ?? group.Key;
                AddLabeledRow(table, label, string.Join(", ", values));
            }
        }

        // Other talismans in the same accessory group, e.g. "Erdtree's Favor +1, Erdtree's Favor +2".
        // The game doesn't let you wear two talismans from one group at the same time.
        private string ConflictingTalismans(object group)
        {
            if (group == DBNull.Value)
                return null;

            var others = WeaponDatabase.Query(
                @"SELECT name FROM talismans
                  WHERE accessory_group = @group AND id <> @id
                  ORDER BY name;",
                new Dictionary<string, object> { { "@group", group }, { "@id", _talismanId } });

            return string.Join(", ", others.Rows.Cast<DataRow>().Select(r => r["name"].ToString()));
        }
    }
}

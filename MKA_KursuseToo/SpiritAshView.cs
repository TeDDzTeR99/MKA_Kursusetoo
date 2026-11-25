using System;
using System.Collections.Generic;
using System.Data;
using MKA_KursuseToo.Properties;

namespace MKA_KursuseToo
{
    // Details page for one spirit ash
    public class SpiritAshView : InfoboxView
    {
        private readonly int _ashId;

        public SpiritAshView(int ashId) : base("spirit_ash", ashId)
        {
            _ashId = ashId;
        }

        protected override string NotFoundMessage => Resources.AshNotFound;

        protected override string LoadDetails(DataTable rows)
        {
            var dt = WeaponDatabase.Query(
                @"SELECT name, category, effect, description, fp, hp, max_level, is_dlc
                  FROM spirit_ashes
                  WHERE id = @id;",
                new Dictionary<string, object> { { "@id", _ashId } });

            if (dt.Rows.Count == 0)
                return null;

            var row = dt.Rows[0];
            AddRow(rows, "effect", row["effect"]?.ToString());
            AddRow(rows, "fp", row["fp"]?.ToString());   // summoning cost; each ash costs FP or HP
            AddRow(rows, "hp", row["hp"]?.ToString());
            // Upgrade material: the same text as the kind tile, e.g. "Grave Glovewort"
            AddRow(rows, "upgrade_path",
                   Resources.ResourceManager.GetString("SpiritAshKind_" + row["category"], Resources.Culture));
            AddRow(rows, "max_upgrade_level", "+" + row["max_level"]);
            if (Convert.ToInt64(row["is_dlc"]) == 1)
                AddRow(rows, "dlc", Resources.ShadowOfTheErdtree);
            AddRow(rows, "description", row["description"]?.ToString());

            return row["name"].ToString();
        }
    }
}

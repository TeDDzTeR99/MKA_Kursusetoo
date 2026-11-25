using System;
using System.Collections.Generic;
using System.Data;
using MKA_KursuseToo.Properties;

namespace MKA_KursuseToo
{
    // Details page for one ash of war
    public class AshOfWarView : InfoboxView
    {
        private readonly int _ashId;

        public AshOfWarView(int ashId) : base("ash_of_war", ashId)
        {
            _ashId = ashId;
        }

        protected override string NotFoundMessage => Resources.AshNotFound;

        protected override string LoadDetails(DataTable rows)
        {
            var dt = WeaponDatabase.Query(
                @"SELECT name, affinity, affinities, weapon_types, fp, description, is_dlc
                  FROM ashes_of_war
                  WHERE id = @id;",
                new Dictionary<string, object> { { "@id", _ashId } });

            if (dt.Rows.Count == 0)
                return null;

            var row = dt.Rows[0];
            AddRow(rows, "fp", row["fp"]?.ToString());   // NULL = the skill costs no FP: no row
            AddRow(rows, "default_affinity", row["affinity"].ToString());
            AddRow(rows, "affinities", row["affinities"].ToString());
            AddRow(rows, "weapon_types", row["weapon_types"].ToString());
            if (Convert.ToInt64(row["is_dlc"]) == 1)
                AddRow(rows, "dlc", Resources.ShadowOfTheErdtree);
            AddRow(rows, "description", row["description"]?.ToString());

            return row["name"].ToString();
        }
    }
}

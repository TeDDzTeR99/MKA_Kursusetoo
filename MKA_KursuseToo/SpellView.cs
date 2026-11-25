using System;
using System.Collections.Generic;
using System.Data;
using MKA_KursuseToo.Properties;

namespace MKA_KursuseToo
{
    // Details page for one sorcery or incantation
    public class SpellView : InfoboxView
    {
        private readonly int _spellId;

        // kind: "sorcery" or "incantation", the category's Kind (picks the picture folder)
        public SpellView(int spellId, string kind) : base(kind, spellId)
        {
            _spellId = spellId;
        }

        protected override string NotFoundMessage => Resources.SpellNotFound;

        protected override string LoadDetails(DataTable rows)
        {
            var dt = WeaponDatabase.Query(
                @"SELECT name, effect, description, fp, stamina, slots,
                         required_intelligence, required_faith, required_arcane, is_dlc
                  FROM spells
                  WHERE id = @id;",
                new Dictionary<string, object> { { "@id", _spellId } });

            if (dt.Rows.Count == 0)
                return null;

            var row = dt.Rows[0];
            AddRow(rows, "effect", row["effect"]?.ToString());
            AddRow(rows, "fp", row["fp"].ToString());
            AddRow(rows, "stamina", row["stamina"].ToString());
            AddRow(rows, "slots", row["slots"].ToString());
            // Only the stats the spell needs, like the in-game menu
            foreach (var stat in new[] { "required_intelligence", "required_faith", "required_arcane" })
                if (Convert.ToInt64(row[stat]) > 0)
                    AddRow(rows, stat, row[stat].ToString());
            if (Convert.ToInt64(row["is_dlc"]) == 1)
                AddRow(rows, "dlc", Resources.ShadowOfTheErdtree);
            AddRow(rows, "description", row["description"]?.ToString());

            return row["name"].ToString();
        }
    }
}

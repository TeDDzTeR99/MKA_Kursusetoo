using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using MKA_KursuseToo.Properties;

namespace MKA_KursuseToo
{
    // Weapon details page, shown inside the main window
    public partial class DetailedView : UserControl
    {
        private readonly int _itemId;

        // Grade letter -> value range text, loaded from scaling_grades (e.g. "D" -> "25–59")
        private readonly Dictionary<string, string> _gradeRanges = new Dictionary<string, string>();

        // Field name -> tooltip text for the current grid contents
        private readonly Dictionary<string, string> _toolTips = new Dictionary<string, string>();

        // True while the dropdowns are being filled, so their change events don't reload the grid
        private bool _fillingDropdowns;

        public DetailedView(int itemId)
        {
            InitializeComponent();
            _itemId = itemId;
            ItemImages.AttachCutout(this, "weapon", itemId, DetailedViewGrid);
        }

        private void DetailedView_Load(object sender, EventArgs e)
        {
            // Texts come from Properties/Resources (English) or Resources.et (Estonian)
            lblAffinity.Text = Resources.AffinityLabel;
            lblLevel.Text = Resources.UpgradeLevelLabel;

            Theme.Apply(this);
            lblName.Font = Theme.TitleFont;
            lblName.ForeColor = Theme.Accent;

            DetailedViewGrid.ReadOnly = true;
            DetailedViewGrid.AllowUserToAddRows = false;
            DetailedViewGrid.AllowUserToDeleteRows = false;
            DetailedViewGrid.RowHeadersVisible = false;
            DetailedViewGrid.ColumnHeadersVisible = false;   // "Field" / "Value" headers aren't needed
            DetailedViewGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            DetailedViewGrid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            DetailedViewGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            // Infobox look: no stripes, and no visible selection since this table is read-only
            DetailedViewGrid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle();
            DetailedViewGrid.DefaultCellStyle.SelectionBackColor = Theme.Surface;
            DetailedViewGrid.DefaultCellStyle.SelectionForeColor = Theme.Text;

            try
            {
                LoadGradeRanges();
                LoadAffinities();   // also fills the level dropdown and the grid
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void LoadGradeRanges()
        {
            var grades = WeaponDatabase.Query("SELECT grade, min_value, max_value FROM scaling_grades;");

            foreach (DataRow g in grades.Rows)
            {
                if (g["min_value"] == DBNull.Value)
                    continue;   // "-" has no range

                var range = g["max_value"] == DBNull.Value
                    ? $"{g["min_value"]}+"
                    : $"{g["min_value"]}–{g["max_value"]}";

                _gradeRanges[g["grade"].ToString()] = range;
            }
        }

        // Fills the affinity dropdown with the affinities that have scaling data for this weapon
        private void LoadAffinities()
        {
            var affinities = WeaponDatabase.Query(
                @"SELECT DISTINCT a.id, a.name
                  FROM weapon_scaling ws
                  JOIN affinities a ON a.id = ws.affinity_id
                  WHERE ws.weapon_id = @id
                  ORDER BY a.id;",
                new Dictionary<string, object> { { "@id", _itemId } });

            _fillingDropdowns = true;
            cmbAffinity.DisplayMember = "name";
            cmbAffinity.ValueMember = "id";
            cmbAffinity.DataSource = affinities;
            _fillingDropdowns = false;

            if (affinities.Rows.Count > 0)
                LoadLevels();
            else
                LoadDetails();   // no scaling data at all, still show the rest
        }

        // Fills the level dropdown with the levels that have data for the selected affinity
        private void LoadLevels()
        {
            var levels = WeaponDatabase.Query(
                @"SELECT upgrade_level, '+' || upgrade_level AS label
                  FROM weapon_scaling
                  WHERE weapon_id = @id AND affinity_id = @affinity
                  ORDER BY upgrade_level;",
                new Dictionary<string, object>
                {
                    { "@id", _itemId },
                    { "@affinity", cmbAffinity.SelectedValue }
                });

            _fillingDropdowns = true;
            cmbLevel.DisplayMember = "label";
            cmbLevel.ValueMember = "upgrade_level";
            cmbLevel.DataSource = levels;
            _fillingDropdowns = false;

            LoadDetails();
        }

        private void LoadDetails()
        {
            const string query = @"
                SELECT
                    w.id AS weapon_id,
                    w.name,
                    wt.name AS weapon_type,
                    -- Damage for the selected affinity and level
                    wd.physical_damage,
                    wd.magic_damage,
                    wd.fire_damage,
                    wd.lightning_damage,
                    wd.holy_damage,
                    -- Status buildup for the selected affinity and level (combined into one row below)
                    wd.poison_buildup,
                    wd.scarlet_rot_buildup,
                    wd.bleed_buildup,
                    wd.frost_buildup,
                    wd.sleep_buildup,
                    wd.madness_buildup,
                    wd.death_blight_buildup,
                    w.critical,
                    w.guard_boost,
                    w.weight,
                    w.upgrade_path,
                    w.max_upgrade_level,
                    w.special_effect AS passive_effect,
                    w.location,

                    wr.strength AS required_strength,
                    wr.dexterity AS required_dexterity,
                    wr.intelligence AS required_intelligence,
                    wr.faith AS required_faith,
                    wr.arcane AS required_arcane,

                    g_str.grade AS strength_scaling,
                    g_dex.grade AS dexterity_scaling,
                    g_int.grade AS intelligence_scaling,
                    g_fai.grade AS faith_scaling,
                    g_arc.grade AS arcane_scaling,

                    ws.strength_value AS strength_scaling_value,
                    ws.dexterity_value AS dexterity_scaling_value,
                    ws.intelligence_value AS intelligence_scaling_value,
                    ws.faith_value AS faith_scaling_value,
                    ws.arcane_value AS arcane_scaling_value,

                    w.description   -- last, so the long text is the bottom row

                FROM weapons w
                LEFT JOIN weapon_types wt ON wt.id = w.weapon_type_id
                LEFT JOIN weapon_requirements wr ON w.id = wr.weapon_id
                LEFT JOIN weapon_damage wd ON w.id = wd.weapon_id
                                          AND wd.affinity_id = @affinity
                                          AND wd.upgrade_level = @level
                LEFT JOIN weapon_scaling ws ON w.id = ws.weapon_id
                                           AND ws.affinity_id = @affinity
                                           AND ws.upgrade_level = @level
                LEFT JOIN scaling_grades g_str ON g_str.id = ws.strength_grade_id
                LEFT JOIN scaling_grades g_dex ON g_dex.id = ws.dexterity_grade_id
                LEFT JOIN scaling_grades g_int ON g_int.id = ws.intelligence_grade_id
                LEFT JOIN scaling_grades g_fai ON g_fai.id = ws.faith_grade_id
                LEFT JOIN scaling_grades g_arc ON g_arc.id = ws.arcane_grade_id
                WHERE w.id = @id;";

            var dt = WeaponDatabase.Query(query, new Dictionary<string, object>
            {
                { "@id", _itemId },
                { "@affinity", cmbAffinity.SelectedValue ?? DBNull.Value },
                { "@level", cmbLevel.SelectedValue ?? DBNull.Value }
            });

            if (dt.Rows.Count == 0)
            {
                lblName.Text = Resources.WeaponNotFound;
                DetailedViewGrid.DataSource = null;
                return;
            }

            var row = dt.Rows[0];
            lblName.Text = row["name"].ToString();

            var displayTable = new DataTable();
            displayTable.Columns.Add("Field");
            displayTable.Columns.Add("Value");
            _toolTips.Clear();

            foreach (DataColumn col in dt.Columns)
            {
                // Id and name are already shown elsewhere (name is in lblName)
                // *_scaling_value columns are merged into their *_scaling row below
                // *_buildup columns are merged into the passive effect row below
                if (col.ColumnName == "weapon_id" || col.ColumnName == "name"
                    || col.ColumnName.EndsWith("_scaling_value")
                    || col.ColumnName.EndsWith("_buildup"))
                    continue;

                var value = col.ColumnName == "passive_effect"
                    ? PassiveEffectText(row)
                    : row[col]?.ToString();

                if (col.ColumnName.EndsWith("_scaling") && !string.IsNullOrEmpty(value))
                {
                    var grade = value;
                    var number = row[col.ColumnName + "_value"]?.ToString();

                    // Show the exact number when we have it, e.g. "52 (D)"; otherwise just "D"
                    if (!string.IsNullOrEmpty(number))
                        value = $"{number} ({grade})";

                    // Grade range goes in the tooltip, e.g. "D: 25–59"
                    if (_gradeRanges.TryGetValue(grade, out var range))
                        _toolTips[FieldLabel(col.ColumnName)] = $"{grade}: {range}";
                }

                displayTable.Rows.Add(FieldLabel(col.ColumnName), value);
            }

            DetailedViewGrid.DataSource = displayTable;

            // Field names in a shaded gold column, like a wiki infobox.
            // Columns["..."] returns null if no column has that name, so check before using it.
            var fieldColumn = DetailedViewGrid.Columns["Field"];
            if (fieldColumn != null)
            {
                var fieldStyle = fieldColumn.DefaultCellStyle;
                fieldStyle.BackColor = Theme.HeaderBack;
                fieldStyle.ForeColor = Theme.Accent;
                fieldStyle.SelectionBackColor = Theme.HeaderBack;
                fieldStyle.SelectionForeColor = Theme.Accent;
                fieldStyle.Font = Theme.BoldFont;
                fieldColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                // With wrapping on, the column would be sized to the narrowest wrapped width; names stay on one line
                fieldStyle.WrapMode = DataGridViewTriState.False;
            }

            // The value column takes the rest of the page width; long values (e.g. locations) wrap
            var valueColumn = DetailedViewGrid.Columns["Value"];
            if (valueColumn != null)
                valueColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private void cmbAffinity_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_fillingDropdowns) return;

            try { LoadLevels(); }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void cmbLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_fillingDropdowns) return;

            try { LoadDetails(); }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        // Status effects in display order: buildup column in weapon_damage -> resource key of the name
        private static readonly string[] StatusEffects =
        {
            "bleed", "frost", "poison", "scarlet_rot", "sleep", "madness", "death_blight"
        };

        // Builds the "Passive Effect" text, e.g. "Hemorrhage (57)" or "Hemorrhage (38), Poison (95)",
        // followed by any special effect such as "Boosts Glintblade Sorceries (15%)" on staffs and seals
        private static string PassiveEffectText(DataRow row)
        {
            // One "Name (buildup)" entry for every status effect this weapon builds up
            var parts = (from effect in StatusEffects
                         let buildup = row[effect + "_buildup"]
                         where buildup != DBNull.Value && Convert.ToInt64(buildup) > 0
                         let name = Resources.ResourceManager.GetString("Status_" + effect, Resources.Culture) ?? effect
                         select $"{name} ({buildup})").ToList();

            var special = row["passive_effect"]?.ToString();
            if (!string.IsNullOrEmpty(special))
                parts.Add(special);

            return string.Join(", ", parts);
        }

        // Turns a database column name into a label for the user, e.g. "weapon_type" -> "Weapon Type"
        // (or "Relva tüüp" in Estonian). The label is looked up in the resources as "Field_weapon_type";
        // if there is none, a readable version of the column name is used instead.
        private static string FieldLabel(string columnName)
        {
            var label = Resources.ResourceManager.GetString("Field_" + columnName, Resources.Culture);
            if (!string.IsNullOrEmpty(label))
                return label;

            var spaced = columnName.Replace('_', ' ');
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(spaced);
        }

        private void DetailedViewGrid_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // "is string field" checks the type and creates the variable in one step (no null check needed)
            if (DetailedViewGrid.Rows[e.RowIndex].Cells["Field"].Value is string field
                && _toolTips.TryGetValue(field, out var tip))
            {
                e.ToolTipText = tip;
            }
        }
    }
}

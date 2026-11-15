using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace MKA_KursuseToo
{
    public partial class DetailedView : Form
    {
        private readonly int _itemId;

        // Grade letter -> value range text, loaded from scaling_grades (e.g. "D" -> "25–59")
        private readonly Dictionary<string, string> _gradeRanges = new Dictionary<string, string>();

        // Field name -> tooltip text for the current grid contents
        private readonly Dictionary<string, string> _toolTips = new Dictionary<string, string>();

        // True while the dropdowns are being filled, so their change events don't reload the grid
        private bool _fillingDropdowns;

        private string DbPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "elden_ring_weapons.db");

        public DetailedView(int itemId)
        {
            InitializeComponent();
            _itemId = itemId;
        }

        private void DetailedView_Load(object sender, EventArgs e)
        {
            DetailedViewGrid.ReadOnly = true;
            DetailedViewGrid.AllowUserToAddRows = false;
            DetailedViewGrid.AllowUserToDeleteRows = false;
            DetailedViewGrid.RowHeadersVisible = false;
            DetailedViewGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            DetailedViewGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            DetailedViewGrid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            DetailedViewGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

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

        // Runs a query and returns the result as a DataTable
        private DataTable Query(string sql, Dictionary<string, object> parameters = null)
        {
            using (var con = new SqliteConnection($"Data Source={DbPath}"))
            {
                con.Open();

                using (var cmd = new SqliteCommand(sql, con))
                {
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                            cmd.Parameters.AddWithValue(param.Key, param.Value);
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        DataTable dt = new DataTable();
                        dt.Load(reader);
                        return dt;
                    }
                }
            }
        }

        private void LoadGradeRanges()
        {
            DataTable grades = Query("SELECT grade, min_value, max_value FROM scaling_grades;");

            foreach (DataRow g in grades.Rows)
            {
                if (g["min_value"] == DBNull.Value)
                    continue;   // "-" has no range

                string range = g["max_value"] == DBNull.Value
                    ? $"{g["min_value"]}+"
                    : $"{g["min_value"]}–{g["max_value"]}";

                _gradeRanges[g["grade"].ToString()] = range;
            }
        }

        // Fills the affinity dropdown with the affinities that have scaling data for this weapon
        private void LoadAffinities()
        {
            DataTable affinities = Query(
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
            DataTable levels = Query(
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
            string query = @"
                SELECT
                    w.id AS weapon_id,
                    w.name,
                    wt.name AS weapon_type,
                    w.physical_damage,
                    w.magic_damage,
                    w.fire_damage,
                    w.lightning_damage,
                    w.holy_damage,
                    w.critical,
                    w.guard_boost,
                    w.weight,
                    w.upgrade_path,
                    w.max_upgrade_level,
                    w.passive_effect,
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
                    ws.arcane_value AS arcane_scaling_value

                FROM weapons w
                LEFT JOIN weapon_types wt ON wt.id = w.weapon_type_id
                LEFT JOIN weapon_requirements wr ON w.id = wr.weapon_id
                LEFT JOIN weapon_scaling ws ON w.id = ws.weapon_id
                                           AND ws.affinity_id = @affinity
                                           AND ws.upgrade_level = @level
                LEFT JOIN scaling_grades g_str ON g_str.id = ws.strength_grade_id
                LEFT JOIN scaling_grades g_dex ON g_dex.id = ws.dexterity_grade_id
                LEFT JOIN scaling_grades g_int ON g_int.id = ws.intelligence_grade_id
                LEFT JOIN scaling_grades g_fai ON g_fai.id = ws.faith_grade_id
                LEFT JOIN scaling_grades g_arc ON g_arc.id = ws.arcane_grade_id
                WHERE w.id = @id;";

            DataTable dt = Query(query, new Dictionary<string, object>
            {
                { "@id", _itemId },
                { "@affinity", cmbAffinity.SelectedValue ?? (object)DBNull.Value },
                { "@level", cmbLevel.SelectedValue ?? (object)DBNull.Value }
            });

            if (dt.Rows.Count == 0)
            {
                MessageBox.Show("Weapon not found.");
                Close();
                return;
            }

            DataRow row = dt.Rows[0];
            lblName.Text = row["name"].ToString();

            DataTable displayTable = new DataTable();
            displayTable.Columns.Add("Field");
            displayTable.Columns.Add("Value");
            _toolTips.Clear();

            foreach (DataColumn col in dt.Columns)
            {
                // Id and name are already shown elsewhere (name is in lblName)
                // *_scaling_value columns are merged into their *_scaling row below
                if (col.ColumnName == "weapon_id" || col.ColumnName == "name"
                    || col.ColumnName.EndsWith("_scaling_value"))
                    continue;

                string value = row[col]?.ToString();

                if (col.ColumnName.EndsWith("_scaling") && !string.IsNullOrEmpty(value))
                {
                    string grade = value;
                    string number = row[col.ColumnName + "_value"]?.ToString();

                    // Show the exact number when we have it, e.g. "52 (D)"; otherwise just "D"
                    if (!string.IsNullOrEmpty(number))
                        value = $"{number} ({grade})";

                    // Grade range goes in the tooltip, e.g. "D: 25–59"
                    if (_gradeRanges.ContainsKey(grade))
                        _toolTips[col.ColumnName] = $"{grade}: {_gradeRanges[grade]}";
                }

                displayTable.Rows.Add(col.ColumnName, value);
            }

            DetailedViewGrid.DataSource = displayTable;
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

        private void DetailedViewGrid_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string field = DetailedViewGrid.Rows[e.RowIndex].Cells["Field"].Value as string;

            if (field != null && _toolTips.TryGetValue(field, out string tip))
                e.ToolTipText = tip;
        }
    }
}

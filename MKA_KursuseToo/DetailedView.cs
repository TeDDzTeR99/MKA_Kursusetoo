using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Drawing;
using System.Windows.Forms;

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

        public DetailedView(int itemId)
        {
            InitializeComponent();
            _itemId = itemId;
        }

        private void DetailedView_Load(object sender, EventArgs e)
        {
            Theme.Apply(this);
            lblName.Font = Theme.TitleFont;
            lblName.ForeColor = Theme.Accent;

            DetailedViewGrid.ReadOnly = true;
            DetailedViewGrid.AllowUserToAddRows = false;
            DetailedViewGrid.AllowUserToDeleteRows = false;
            DetailedViewGrid.RowHeadersVisible = false;
            DetailedViewGrid.ColumnHeadersVisible = false;   // "Field" / "Value" headers aren't needed
            DetailedViewGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;   // sized in FitWindowToContent
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
            DataTable grades = WeaponDatabase.Query("SELECT grade, min_value, max_value FROM scaling_grades;");

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
            DataTable affinities = WeaponDatabase.Query(
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
            DataTable levels = WeaponDatabase.Query(
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
            var query = @"
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

            DataTable dt = WeaponDatabase.Query(query, new Dictionary<string, object>
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

            var displayTable = new DataTable();
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
                        _toolTips[ToDisplayName(col.ColumnName)] = $"{grade}: {_gradeRanges[grade]}";
                }

                displayTable.Rows.Add(ToDisplayName(col.ColumnName), value);
            }

            DetailedViewGrid.DataSource = displayTable;

            // Field names in a shaded gold column, like a wiki infobox
            DataGridViewCellStyle fieldStyle = DetailedViewGrid.Columns["Field"].DefaultCellStyle;
            fieldStyle.BackColor = Theme.HeaderBack;
            fieldStyle.ForeColor = Theme.Accent;
            fieldStyle.SelectionBackColor = Theme.HeaderBack;
            fieldStyle.SelectionForeColor = Theme.Accent;
            fieldStyle.Font = Theme.BoldFont;

            FitWindowToContent();
        }

        // Long values (e.g. locations) wrap onto several lines instead of making the window very wide
        private const int MaxValueColumnWidth = 480;

        // Resizes the window so the whole grid fits without scrollbars (capped to the screen size)
        private void FitWindowToContent()
        {
            // Measure columns on one line first (with wrapping on, WinForms picks the narrowest possible width)
            DataGridViewColumn valueColumn = DetailedViewGrid.Columns["Value"];
            DetailedViewGrid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            DetailedViewGrid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

            // Then cap the value column and let only that column wrap
            if (valueColumn.Width > MaxValueColumnWidth)
                valueColumn.Width = MaxValueColumnWidth;
            valueColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            DetailedViewGrid.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);

            int gridWidth = DetailedViewGrid.Columns.GetColumnsWidth(DataGridViewElementStates.Visible)
                            + SystemInformation.VerticalScrollBarWidth   // room in case a scrollbar is still needed
                            + 2;                                         // grid border
            int headerHeight = DetailedViewGrid.ColumnHeadersVisible ? DetailedViewGrid.ColumnHeadersHeight : 0;
            int gridHeight = headerHeight
                             + DetailedViewGrid.Rows.GetRowsHeight(DataGridViewElementStates.Visible)
                             + 2;

            int width = Math.Max(gridWidth, Math.Max(lblName.PreferredWidth, pnlScalingOptions.PreferredSize.Width));
            // Space taken by everything above the grid (title + dropdowns), measured after layout
            PerformLayout();
            int spaceAboveGrid = ClientSize.Height - DetailedViewGrid.Height;
            int height = spaceAboveGrid + gridHeight;

            // Don't grow past the screen the window is on
            Rectangle screen = Screen.FromControl(this).WorkingArea;
            Size chrome = Size - ClientSize;   // title bar + borders
            width = Math.Min(width, screen.Width - chrome.Width);
            height = Math.Min(height, screen.Height - chrome.Height);

            ClientSize = new Size(width, height);
            KeepOnScreen();
        }

        // Moves the window so it is fully inside the screen (e.g. after it grew taller)
        private void KeepOnScreen()
        {
            Rectangle screen = Screen.FromControl(this).WorkingArea;
            int left = Math.Max(screen.Left, Math.Min(Left, screen.Right - Width));
            int top = Math.Max(screen.Top, Math.Min(Top, screen.Bottom - Height));
            Location = new Point(left, top);
        }

        private void DetailedView_Shown(object sender, EventArgs e)
        {
            // Rows get their final height only once the window is on screen, so measure again now.
            // This also moves the window back inside the screen if it got too tall.
            FitWindowToContent();
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

        // Turns a database column name into a label for the user, e.g. "weapon_type" -> "Weapon Type"
        private static string ToDisplayName(string columnName)
        {
            string spaced = columnName.Replace('_', ' ');
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(spaced);
        }

        private void DetailedViewGrid_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var field = DetailedViewGrid.Rows[e.RowIndex].Cells["Field"].Value as string;

            if (field != null && _toolTips.TryGetValue(field, out string tip))
                e.ToolTipText = tip;
        }
    }
}

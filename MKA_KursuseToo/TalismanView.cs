using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using MKA_KursuseToo.Properties;

namespace MKA_KursuseToo
{
    // Details window for one talisman: a title and a two-column "infobox" grid, like DetailedView
    // but without the affinity and level dropdowns (talismans have neither)
    public class TalismanView : Form
    {
        private const int MaxValueColumnWidth = 480;

        private readonly int _talismanId;
        private readonly Label _lblName = new Label();
        private readonly DataGridView _grid = new DataGridView();

        public TalismanView(int talismanId)
        {
            _talismanId = talismanId;

            Font = new Font("Segoe UI", 10.5F);
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(560, 300);

            _lblName.Dock = DockStyle.Top;
            _lblName.Height = 56;
            _lblName.Padding = new Padding(12, 0, 12, 0);
            _lblName.TextAlign = ContentAlignment.MiddleLeft;

            _grid.Dock = DockStyle.Fill;
            _grid.ReadOnly = true;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AllowUserToResizeRows = false;
            _grid.RowHeadersVisible = false;
            _grid.ColumnHeadersVisible = false;
            _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            // Fill control first, then the Top one, so docking lays them out correctly
            Controls.Add(_grid);
            Controls.Add(_lblName);

            Load += TalismanView_Load;
            Shown += (sender, e) => FitWindowToContent();   // rows get their final height only once shown
        }

        private void TalismanView_Load(object sender, EventArgs e)
        {
            Text = Resources.TalismanWindowTitle;

            Theme.Apply(this);
            _lblName.Font = Theme.TitleFont;
            _lblName.ForeColor = Theme.Accent;

            // Infobox look: no stripes, and no visible selection since this table is read-only
            _grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle();
            _grid.DefaultCellStyle.SelectionBackColor = Theme.Surface;
            _grid.DefaultCellStyle.SelectionForeColor = Theme.Text;

            try
            {
                LoadDetails();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void LoadDetails()
        {
            var dt = WeaponDatabase.Query(
                @"SELECT name, effect, description, weight, sell_value, accessory_group, is_dlc
                  FROM talismans
                  WHERE id = @id;",
                new Dictionary<string, object> { { "@id", _talismanId } });

            if (dt.Rows.Count == 0)
            {
                MessageBox.Show(Resources.TalismanNotFound);
                Close();
                return;
            }

            var row = dt.Rows[0];
            _lblName.Text = row["name"].ToString();

            var display = new DataTable();
            display.Columns.Add("Field");
            display.Columns.Add("Value");

            AddRow(display, "effect", row["effect"]?.ToString());
            AddStatRows(display);
            AddRow(display, "weight", Convert.ToDouble(row["weight"]).ToString("0.0", CultureInfo.CurrentCulture));
            AddRow(display, "sell_value", row["sell_value"]?.ToString());
            AddRow(display, "cannot_equip_with", ConflictingTalismans(row["accessory_group"]));
            if (Convert.ToInt64(row["is_dlc"]) == 1)
                AddRow(display, "dlc", Resources.ShadowOfTheErdtree);
            AddRow(display, "description", row["description"]?.ToString());

            _grid.DataSource = display;

            // Field names in a shaded gold column, like a wiki infobox
            var fieldStyle = _grid.Columns["Field"]?.DefaultCellStyle;
            if (fieldStyle != null)
            {
                fieldStyle.BackColor = Theme.HeaderBack;
                fieldStyle.ForeColor = Theme.Accent;
                fieldStyle.SelectionBackColor = Theme.HeaderBack;
                fieldStyle.SelectionForeColor = Theme.Accent;
                fieldStyle.Font = Theme.BoldFont;
            }

            FitWindowToContent();
        }

        // Empty values are left out instead of showing a blank row
        private static void AddRow(DataTable table, string field, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                table.Rows.Add(Resources.ResourceManager.GetString("Field_" + field, Resources.Culture) ?? field, value);
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
                table.Rows.Add(label, string.Join(", ", values));
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

        // Resizes the window so the whole grid fits without scrollbars (capped to the screen size)
        private void FitWindowToContent()
        {
            var valueColumn = _grid.Columns["Value"];
            if (valueColumn == null)
                return;   // nothing loaded yet

            // Measure on one line first, then cap the value column and let only that column wrap
            _grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            _grid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            if (valueColumn.Width > MaxValueColumnWidth)
                valueColumn.Width = MaxValueColumnWidth;
            valueColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            _grid.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);

            var gridWidth = _grid.Columns.GetColumnsWidth(DataGridViewElementStates.Visible)
                            + SystemInformation.VerticalScrollBarWidth + 2;
            // Ask each row how tall it wants to be with the new column widths. Row.Height can still hold
            // the old one-line height here: the grid applies the wrap change to its rows only when it repaints.
            var gridHeight = _grid.Rows.Cast<DataGridViewRow>()
                                 .Sum(r => r.GetPreferredHeight(r.Index, DataGridViewAutoSizeRowMode.AllCells, true))
                             + 2;   // grid border

            var width = Math.Max(gridWidth, _lblName.PreferredWidth);
            var height = _lblName.Height + gridHeight;

            var screen = Screen.FromControl(this).WorkingArea;
            var chrome = Size - ClientSize;   // title bar + borders
            ClientSize = new Size(Math.Min(width, screen.Width - chrome.Width),
                                  Math.Min(height, screen.Height - chrome.Height));

            // Keep the window fully on screen after it grew
            Location = new Point(Math.Max(screen.Left, Math.Min(Left, screen.Right - Width)),
                                 Math.Max(screen.Top, Math.Min(Top, screen.Bottom - Height)));
        }
    }
}

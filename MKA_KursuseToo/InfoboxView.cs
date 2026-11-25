using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MKA_KursuseToo.Properties;

namespace MKA_KursuseToo
{
    // Details window for one item: a title and a two-column "infobox" grid, like DetailedView
    // but without the affinity and level dropdowns. Subclasses (talismans, upgrade materials, ...)
    // only say which rows to show.
    public abstract class InfoboxView : Form
    {
        private const int MaxValueColumnWidth = 480;

        private readonly Label _lblName = new Label();
        private readonly DataGridView _grid = new DataGridView();

        protected InfoboxView()
        {
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

            Load += InfoboxView_Load;
            Shown += (sender, e) => FitWindowToContent();   // rows get their final height only once shown
        }

        // Window title, e.g. "Talisman Details"
        protected abstract string WindowTitle { get; }

        // Message shown when the item isn't in the database
        protected abstract string NotFoundMessage { get; }

        // Fills the Field/Value rows and returns the item's name, or null if the item doesn't exist
        protected abstract string LoadDetails(DataTable rows);

        private void InfoboxView_Load(object sender, EventArgs e)
        {
            Text = WindowTitle;

            Theme.Apply(this);
            _lblName.Font = Theme.TitleFont;
            _lblName.ForeColor = Theme.Accent;

            // Infobox look: no stripes, and no visible selection since this table is read-only
            _grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle();
            _grid.DefaultCellStyle.SelectionBackColor = Theme.Surface;
            _grid.DefaultCellStyle.SelectionForeColor = Theme.Text;

            try
            {
                ShowDetails();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ShowDetails()
        {
            var display = new DataTable();
            display.Columns.Add("Field");
            display.Columns.Add("Value");

            var name = LoadDetails(display);
            if (name == null)
            {
                MessageBox.Show(NotFoundMessage);
                Close();
                return;
            }

            _lblName.Text = name;
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

        // A row labelled with the Field_<field> resource text. Empty values are left out instead of
        // showing a blank row.
        protected static void AddRow(DataTable table, string field, string value) =>
            AddLabeledRow(table, Resources.ResourceManager.GetString("Field_" + field, Resources.Culture) ?? field, value);

        protected static void AddLabeledRow(DataTable table, string label, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                table.Rows.Add(label, value);
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

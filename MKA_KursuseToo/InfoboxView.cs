using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using MKA_KursuseToo.Properties;

namespace MKA_KursuseToo
{
    // Details page for one item, shown inside the main window: a title and a two-column "infobox" grid,
    // like DetailedView but without the affinity and level dropdowns. Subclasses (talismans, upgrade
    // materials, ...) only say which rows to show.
    public abstract class InfoboxView : UserControl
    {
        private readonly Label _lblName = new Label();
        private readonly DataGridView _grid = new DataGridView();

        protected InfoboxView()
        {
            Font = new Font("Segoe UI", 10.5F);

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
        }

        // Shown instead of the name when the item isn't in the database
        protected abstract string NotFoundMessage { get; }

        // Fills the Field/Value rows and returns the item's name, or null if the item doesn't exist
        protected abstract string LoadDetails(DataTable rows);

        private void InfoboxView_Load(object sender, EventArgs e)
        {
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
            _lblName.Text = name ?? NotFoundMessage;
            if (name == null)
                return;

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

            // Field names as wide as they need, values take the rest of the page width and wrap
            _grid.Columns["Field"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            _grid.Columns["Value"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _grid.Columns["Value"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
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
    }
}

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using MKA_KursuseToo.Properties;

namespace MKA_KursuseToo
{
    public partial class Main : Form
    {
        // What the grid is currently showing, so a click knows what to do
        private enum ViewMode
        {
            Weapons,
            Types
        }

        private ViewMode _currentViewMode = ViewMode.Weapons;

        public Main()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            // Texts come from Properties/Resources (English) or Resources.et (Estonian)
            Text = Resources.AppWindowTitle;
            lblTitle.Text = Resources.AppTitle;
            Search.Text = Resources.SearchButton;
            List_All_Button.Text = Resources.ListAllButton;
            List_All_Types.Text = Resources.CategoriesButton;

            DataOutput.ReadOnly = true;
            DataOutput.AllowUserToAddRows = false;
            DataOutput.AllowUserToDeleteRows = false;
            DataOutput.RowHeadersVisible = false;
            DataOutput.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            Theme.Apply(this);
            lblTitle.Font = Theme.TitleFont;
            lblTitle.ForeColor = Theme.Accent;

            if (!WeaponDatabase.Exists)
            {
                MessageBox.Show(
                    string.Format(Resources.DatabaseNotFound, WeaponDatabase.DbPath),
                    Resources.ErrorTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                // Nothing can be loaded without the database, so don't let the user try
                pnlToolbar.Enabled = false;
                return;
            }

            // Show all weapons on startup so the grid isn't empty
            LoadWeapons(@"SELECT id AS Id, name AS Name FROM weapons ORDER BY name;");
        }

        // Loads a list of weapons. The query must return ID and Name columns.
        private void LoadWeapons(string query, Dictionary<string, object> parameters = null)
        {
            ShowList(query, parameters, Resources.WeaponNameHeader, ViewMode.Weapons);
        }

        // Loads the list of weapon types that have at least one weapon
        private void LoadWeaponTypes()
        {
            ShowList(@"SELECT wt.id AS Id, wt.name AS Name
                       FROM weapon_types wt
                       WHERE EXISTS (SELECT 1 FROM weapons w WHERE w.weapon_type_id = wt.id)
                       ORDER BY wt.name;",
                     null, Resources.WeaponTypeHeader, ViewMode.Types);
        }

        // Loads all weapons for one type
        private void LoadWeaponsByTypeId(int typeId)
        {
            LoadWeapons(
                @"SELECT id AS Id, name AS Name
                  FROM weapons
                  WHERE weapon_type_id = @typeid
                  ORDER BY name;",
                new Dictionary<string, object>
                {
                    { "@typeid", typeId }
                });
        }

        // Shows a query result (ID + Name) in the grid: a hidden ID column and a clickable Name column
        private void ShowList(string query, Dictionary<string, object> parameters, string header, ViewMode mode)
        {
            try
            {
                var dt = WeaponDatabase.Query(query, parameters);

                DataOutput.DataSource = null;
                DataOutput.Columns.Clear();
                DataOutput.AutoGenerateColumns = false;

                var idColumn = new DataGridViewTextBoxColumn
                {
                    Name = "Id",
                    DataPropertyName = "Id",
                    Visible = false
                };
                DataOutput.Columns.Add(idColumn);

                var nameColumn = new DataGridViewLinkColumn
                {
                    Name = "Name",
                    HeaderText = header,
                    DataPropertyName = "Name",
                    UseColumnTextForLinkValue = false
                };
                Theme.StyleLinkColumn(nameColumn);
                DataOutput.Columns.Add(nameColumn);

                // Attach the data after the columns exist, otherwise WinForms can un-hide the Id column
                DataOutput.DataSource = dt;
                idColumn.Visible = false;

                _currentViewMode = mode;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Search_Click(object sender, EventArgs e)
        {
            var searchValue = SearchBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(searchValue))
            {
                MessageBox.Show(Resources.EnterSearchText);
                return;
            }

            LoadWeapons(
                @"SELECT id AS Id, name AS Name
                  FROM weapons
                  WHERE name LIKE @search ESCAPE '\'
                  ORDER BY name;",
                new Dictionary<string, object>
                {
                    { "@search", "%" + EscapeLike(searchValue) + "%" }
                });
        }

        // In LIKE, % and _ are wildcards. Put a \ in front of them (and of \ itself) so they are
        // searched for as normal characters, e.g. searching "_" finds names containing "_".
        private static string EscapeLike(string text)
        {
            return text.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
        }

        private void DataOutput_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            var id = Convert.ToInt32(DataOutput.Rows[e.RowIndex].Cells["Id"].Value);

            switch (_currentViewMode)
            {
                case ViewMode.Types:
                    LoadWeaponsByTypeId(id);
                    break;

                case ViewMode.Weapons:
                    // Only open details when the weapon name (the link) was clicked
                    if (DataOutput.Columns[e.ColumnIndex].Name != "Name")
                        break;

                    using (var detailsForm = new DetailedView(id))
                    {
                        detailsForm.ShowDialog();
                    }
                    break;

                // Can't happen with the two view modes above; if a new one is added and
                // forgotten here, this makes the mistake obvious instead of silently doing nothing
                default:
                    throw new ArgumentOutOfRangeException(nameof(_currentViewMode), _currentViewMode, null);
            }
        }

        private void List_All_Button_Click(object sender, EventArgs e)
        {
            LoadWeapons(@"SELECT id AS Id, name AS Name FROM weapons ORDER BY name;");
        }

        private void List_All_Types_Click(object sender, EventArgs e)
        {
            LoadWeaponTypes();
        }
    }
}

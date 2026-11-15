using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

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

        private void Form1_Load(object sender, EventArgs e)
        {
            DataOutput.ReadOnly = true;
            DataOutput.AllowUserToAddRows = false;
            DataOutput.AllowUserToDeleteRows = false;
            DataOutput.RowHeadersVisible = false;
            DataOutput.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            if (!WeaponDatabase.Exists)
            {
                MessageBox.Show(
                    "Database not found at:\n\n" + WeaponDatabase.DbPath,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }

            // Show all weapons on startup so the grid isn't empty
            LoadWeapons(@"SELECT id AS Id, name AS Name FROM weapons ORDER BY name;");
        }

        // Loads a list of weapons. The query must return Id and Name columns.
        private void LoadWeapons(string query, Dictionary<string, object> parameters = null)
        {
            ShowList(query, parameters, "Weapon Name", ViewMode.Weapons);
        }

        // Loads the list of weapon types
        private void LoadWeaponTypes()
        {
            ShowList(@"SELECT id AS Id, name AS Name FROM weapon_types ORDER BY name;",
                     null, "Weapon Type", ViewMode.Types);
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

        // Shows a query result (Id + Name) in the grid: a hidden Id column and a clickable Name column
        private void ShowList(string query, Dictionary<string, object> parameters, string header, ViewMode mode)
        {
            try
            {
                DataTable dt = WeaponDatabase.Query(query, parameters);

                DataOutput.Columns.Clear();
                DataOutput.AutoGenerateColumns = false;
                DataOutput.DataSource = dt;

                DataOutput.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Id",
                    DataPropertyName = "Id",
                    Visible = false
                });

                DataOutput.Columns.Add(new DataGridViewLinkColumn
                {
                    Name = "Name",
                    HeaderText = header,
                    DataPropertyName = "Name",
                    UseColumnTextForLinkValue = false
                });

                _currentViewMode = mode;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Search_Click(object sender, EventArgs e)
        {
            string searchValue = SearchBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(searchValue))
            {
                MessageBox.Show("Please enter something to search.");
                return;
            }

            LoadWeapons(
                @"SELECT id AS Id, name AS Name
                  FROM weapons
                  WHERE name LIKE @search
                  ORDER BY name;",
                new Dictionary<string, object>
                {
                    { "@search", "%" + searchValue + "%" }
                });
        }

        private void DataOutput_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            var id = Convert.ToInt32(DataOutput.Rows[e.RowIndex].Cells["Id"].Value);

            if (_currentViewMode == ViewMode.Types)
            {
                LoadWeaponsByTypeId(id);
            }
            else if (_currentViewMode == ViewMode.Weapons && DataOutput.Columns[e.ColumnIndex].Name == "Name")
            {
                using (var detailsForm = new DetailedView(id))
                {
                    detailsForm.ShowDialog();
                }
            }
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
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

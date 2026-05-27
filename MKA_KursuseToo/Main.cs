using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace MKA_KursuseToo
{
    public partial class Main : Form
    {
        private string currentViewMode = "weapons";

        public Main()
        {
            InitializeComponent();
        }

        private string DbPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "elden_ring_weapons.db");

        private void Form1_Load(object sender, EventArgs e)
        {
            if (!File.Exists(DbPath))
            {
                MessageBox.Show(
                    "❌ Database NOT found at:\n\n" + DbPath,
                    "Test Result"
                );
                return;
            }

            MessageBox.Show(
                "✅ Database file found at:\n\n" + DbPath,
                "Test Result"
            );
        }

        // Loads weapons (Id + Name)
        private void LoadWeapons(string query, Dictionary<string, object> parameters = null)
        {
            string connectionString = $"Data Source={DbPath}";

            try
            {
                using (var con = new SqliteConnection(connectionString))
                {
                    con.Open();

                    using (var cmd = new SqliteCommand(query, con))
                    {
                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                            {
                                cmd.Parameters.AddWithValue(param.Key, param.Value);
                            }
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            DataTable dt = new DataTable();
                            dt.Load(reader);

                            DataOutput.Columns.Clear();
                            DataOutput.AutoGenerateColumns = false;
                            DataOutput.DataSource = dt;

                            //ID column
                            DataGridViewTextBoxColumn idColumn = new DataGridViewTextBoxColumn
                            {
                                Name = "Id",
                                DataPropertyName = "Id",
                                Visible = false
                            };
                            DataOutput.Columns.Add(idColumn);

                            // Clickable Name column
                            DataGridViewLinkColumn nameColumn = new DataGridViewLinkColumn
                            {
                                Name = "Name",
                                HeaderText = "Weapon Name",
                                DataPropertyName = "Name",
                                UseColumnTextForLinkValue = false
                            };
                            DataOutput.Columns.Add(nameColumn);

                            // Formatting
                            DataOutput.ReadOnly = true;
                            DataOutput.AllowUserToAddRows = false;
                            DataOutput.AllowUserToDeleteRows = false;
                            DataOutput.RowHeadersVisible = false;
                            DataOutput.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                            currentViewMode = "weapons";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // Loads distinct weapon types from weapons table
        private void LoadWeaponTypes()
        {
            string connectionString = $"Data Source={DbPath}";

            try
            {
                using (var con = new SqliteConnection(connectionString))
                {
                    con.Open();

                    string query = @"SELECT id AS Id, name
                                     FROM weapon_types
                                     ORDER BY name;";

                    using (var cmd = new SqliteCommand(query, con))
                    using (var reader = cmd.ExecuteReader())
                    {
                        DataTable dt = new DataTable();
                        dt.Load(reader);

                        DataOutput.Columns.Clear();
                        DataOutput.AutoGenerateColumns = false;
                        DataOutput.DataSource = dt;

                        DataGridViewTextBoxColumn idColumn = new DataGridViewTextBoxColumn
                        {
                            Name = "Id",
                            DataPropertyName = "Id",
                            Visible = false
                        };
                        DataOutput.Columns.Add(idColumn);

                        DataGridViewLinkColumn typeColumn = new DataGridViewLinkColumn
                        {
                            Name = "name",
                            HeaderText = "Weapon Type",
                            DataPropertyName = "name",
                            UseColumnTextForLinkValue = false
                        };
                        DataOutput.Columns.Add(typeColumn);

                        DataOutput.ReadOnly = true;
                        DataOutput.AllowUserToAddRows = false;
                        DataOutput.AllowUserToDeleteRows = false;
                        DataOutput.RowHeadersVisible = false;
                        DataOutput.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                        currentViewMode = "types";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // Loads all weapons for one type
        private void LoadWeaponsByTypeId(int typeId)
        {
            LoadWeapons(
                @"SELECT id AS Id, name
                  FROM weapons
                  WHERE weapon_type_Id = @typeid
                  ORDER BY Name;",
                new Dictionary<string, object>
                {
                    { "@typeid", typeId }
                });
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
                @"SELECT Id, Name
                  FROM weapons
                  WHERE Name LIKE @search
                  ORDER BY Name;",
                new Dictionary<string, object>
                {
                    { "@search", "%" + searchValue + "%" }
                });
        }

        private void DataOutput_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (currentViewMode == "types")
            {
                    int typeId = Convert.ToInt32(DataOutput.Rows[e.RowIndex].Cells["Id"].Value);

                    LoadWeaponsByTypeId(typeId);
            
            }
            else if (currentViewMode == "weapons")
            {
                if (DataOutput.Columns[e.ColumnIndex].Name == "Name")
                {
                    int itemId = Convert.ToInt32(DataOutput.Rows[e.RowIndex].Cells["Id"].Value);

                    DetailedView detailsForm = new DetailedView(itemId);
                    detailsForm.ShowDialog();
                }
            }
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
        }

        private void List_All_Button_Click(object sender, EventArgs e)
        {
            LoadWeapons(@"SELECT Id, Name FROM weapons ORDER BY Name;");
        }

        private void List_All_Types_Click(object sender, EventArgs e)
        {
            LoadWeaponTypes();
        }
    }
}
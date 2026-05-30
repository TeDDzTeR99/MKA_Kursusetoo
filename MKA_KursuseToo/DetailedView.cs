using System;
using System.Data;
using System.IO;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace MKA_KursuseToo
{
    public partial class DetailedView : Form
    {
        private int _itemId;

        private string DbPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "elden_ring_weapons.db");

        public DetailedView(int itemId)
        {
            InitializeComponent();
            _itemId = itemId;
        }

        private void DetailedView_Load(object sender, EventArgs e)
        {
            string connectionString = $"Data Source={DbPath}";

            try
            {
                using (var con = new SqliteConnection(connectionString))
                {
                    con.Open();


                    string query = @"
                                    SELECT
                                        w.id AS weapon_id,
                                        w.name,
                                        w.weapon_type_id,
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

                                        ws.strength_grade_id,
                                        ws.dexterity_grade_id,
                                        ws.intelligence_grade_id,
                                        ws.faith_grade_id,
                                        ws.arcane_grade_id

                                    FROM weapons w
                                    LEFT JOIN weapon_requirements wr ON w.id = wr.weapon_id
                                    LEFT JOIN weapon_scaling ws ON w.id = ws.weapon_id
                                    WHERE w.id = @id;";



                    using (var cmd = new SqliteCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", _itemId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            DataTable dt = new DataTable();
                            dt.Load(reader);
                            if (dt.Rows.Count > 0)
                            {
                                lblName.Text = dt.Rows[0]["Name"].ToString();
                            }
                            else
                            {
                                MessageBox.Show("Weapon not found.");
                                this.Close();
                                return;
                            }
                            DataTable displayTable = new DataTable();
                            displayTable.Columns.Add("Field");
                            displayTable.Columns.Add("Value");

                            DataRow row = dt.Rows[0];

                            foreach (DataColumn col in dt.Columns)
                            {
                                displayTable.Rows.Add(col.ColumnName, row[col]?.ToString());
                            }
                            DetailedViewGrid.DataSource = displayTable;
                            foreach (DataRow r in displayTable.Rows)
                            {
                                if (r["Field"].ToString() == "Id" || r["Field"].ToString() == "Name")
                                {
                                    r.Delete();
                                }
                            }
                            displayTable.AcceptChanges();
                            DetailedViewGrid.ReadOnly = true;
                            DetailedViewGrid.RowHeadersVisible = false;
                            DetailedViewGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                            DetailedViewGrid.AllowUserToAddRows = false;
                            DetailedViewGrid.DataSource = displayTable;

                            DetailedViewGrid.ReadOnly = true;
                            DetailedViewGrid.AllowUserToAddRows = false;
                            DetailedViewGrid.AllowUserToDeleteRows = false;
                            DetailedViewGrid.RowHeadersVisible = false;

                            // Auto-size columns to fit content
                            DetailedViewGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

                            // Auto-size rows to fit wrapped text
                            DetailedViewGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
                            DetailedViewGrid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

                            // Optional: nicer selection behaviour
                            DetailedViewGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
    }
}
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
        private int _itemId;

        // Elden Ring scaling grade -> scaling value range
        private static readonly Dictionary<string, string> GradeRanges = new Dictionary<string, string>
        {
            { "S", "175+" },
            { "A", "140–174" },
            { "B", "90–139" },
            { "C", "60–89" },
            { "D", "25–59" },
            { "E", "1–24" }
        };

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
                                    LEFT JOIN scaling_grades g_str ON g_str.id = ws.strength_grade_id
                                    LEFT JOIN scaling_grades g_dex ON g_dex.id = ws.dexterity_grade_id
                                    LEFT JOIN scaling_grades g_int ON g_int.id = ws.intelligence_grade_id
                                    LEFT JOIN scaling_grades g_fai ON g_fai.id = ws.faith_grade_id
                                    LEFT JOIN scaling_grades g_arc ON g_arc.id = ws.arcane_grade_id
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
                                // Id and name are already shown elsewhere (name is in lblName)
                                // *_scaling_value columns are merged into their *_scaling row below
                                if (col.ColumnName == "weapon_id" || col.ColumnName == "name"
                                    || col.ColumnName.EndsWith("_scaling_value"))
                                    continue;

                                string value = row[col]?.ToString();

                                if (col.ColumnName.EndsWith("_scaling") && !string.IsNullOrEmpty(value))
                                {
                                    string number = row[col.ColumnName + "_value"]?.ToString();

                                    if (!string.IsNullOrEmpty(number))
                                        value = $"{number} ({value})";                       // e.g. "47 (D)"
                                    else if (GradeRanges.ContainsKey(value))
                                        value = $"{value} ({GradeRanges[value]})";           // e.g. "D (25–59)"
                                }

                                displayTable.Rows.Add(col.ColumnName, value);
                            }

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
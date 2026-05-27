namespace MKA_KursuseToo
{
    partial class Main
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SearchBox = new System.Windows.Forms.TextBox();
            this.Search = new System.Windows.Forms.Button();
            this.DataOutput = new System.Windows.Forms.DataGridView();
            this.List_All_Button = new System.Windows.Forms.Button();
            this.List_All_Types = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.DataOutput)).BeginInit();
            this.SuspendLayout();
            // 
            // SearchBox
            // 
            this.SearchBox.Location = new System.Drawing.Point(19, 28);
            this.SearchBox.Margin = new System.Windows.Forms.Padding(10, 8, 10, 8);
            this.SearchBox.Name = "SearchBox";
            this.SearchBox.Size = new System.Drawing.Size(456, 44);
            this.SearchBox.TabIndex = 0;
            this.SearchBox.TextChanged += new System.EventHandler(this.SearchBox_TextChanged);
            // 
            // Search
            // 
            this.Search.Location = new System.Drawing.Point(495, 8);
            this.Search.Margin = new System.Windows.Forms.Padding(10, 8, 10, 8);
            this.Search.Name = "Search";
            this.Search.Size = new System.Drawing.Size(238, 64);
            this.Search.TabIndex = 1;
            this.Search.Text = "Search";
            this.Search.UseVisualStyleBackColor = true;
            this.Search.Click += new System.EventHandler(this.Search_Click);
            // 
            // DataOutput
            // 
            this.DataOutput.AllowUserToAddRows = false;
            this.DataOutput.AllowUserToDeleteRows = false;
            this.DataOutput.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.DataOutput.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
            this.DataOutput.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.DataOutput.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.DataOutput.Location = new System.Drawing.Point(0, 92);
            this.DataOutput.Margin = new System.Windows.Forms.Padding(10, 8, 10, 8);
            this.DataOutput.Name = "DataOutput";
            this.DataOutput.ReadOnly = true;
            this.DataOutput.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.DataOutput.Size = new System.Drawing.Size(1240, 899);
            this.DataOutput.TabIndex = 2;
            this.DataOutput.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DataOutput_CellContentClick);
            // 
            // List_All_Button
            // 
            this.List_All_Button.Location = new System.Drawing.Point(746, 8);
            this.List_All_Button.Name = "List_All_Button";
            this.List_All_Button.Size = new System.Drawing.Size(238, 64);
            this.List_All_Button.TabIndex = 4;
            this.List_All_Button.Text = "List All";
            this.List_All_Button.UseVisualStyleBackColor = true;
            this.List_All_Button.Click += new System.EventHandler(this.List_All_Button_Click);
            // 
            // List_All_Types
            // 
            this.List_All_Types.Location = new System.Drawing.Point(990, 8);
            this.List_All_Types.Name = "List_All_Types";
            this.List_All_Types.Size = new System.Drawing.Size(238, 64);
            this.List_All_Types.TabIndex = 4;
            this.List_All_Types.Text = "Categories";
            this.List_All_Types.UseVisualStyleBackColor = true;
            this.List_All_Types.Click += new System.EventHandler(this.List_All_Types_Click);
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(19F, 36F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(1240, 991);
            this.Controls.Add(this.List_All_Types);
            this.Controls.Add(this.List_All_Button);
            this.Controls.Add(this.DataOutput);
            this.Controls.Add(this.Search);
            this.Controls.Add(this.SearchBox);
            this.Font = new System.Drawing.Font("Courier New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(10, 8, 10, 8);
            this.Name = "Main";
            this.Text = "Elden Ring Wiki";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.DataOutput)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox SearchBox;
        private System.Windows.Forms.Button Search;
        private System.Windows.Forms.DataGridView DataOutput;
        private System.Windows.Forms.Button List_All_Button;
        private System.Windows.Forms.Button List_All_Types;
    }
}


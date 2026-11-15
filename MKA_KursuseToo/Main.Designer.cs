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
            this.lblTitle = new System.Windows.Forms.Label();
            this.pnlToolbar = new System.Windows.Forms.FlowLayoutPanel();
            this.SearchBox = new System.Windows.Forms.TextBox();
            this.Search = new System.Windows.Forms.Button();
            this.List_All_Button = new System.Windows.Forms.Button();
            this.List_All_Types = new System.Windows.Forms.Button();
            this.DataOutput = new System.Windows.Forms.DataGridView();
            this.pnlToolbar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DataOutput)).BeginInit();
            this.SuspendLayout();
            //
            // lblTitle
            //
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTitle.Font = new System.Drawing.Font("Georgia", 20F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Padding = new System.Windows.Forms.Padding(12, 10, 0, 0);
            this.lblTitle.Size = new System.Drawing.Size(820, 56);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Elden Ring Weapons";
            //
            // pnlToolbar
            //
            this.pnlToolbar.AutoSize = true;
            this.pnlToolbar.Controls.Add(this.SearchBox);
            this.pnlToolbar.Controls.Add(this.Search);
            this.pnlToolbar.Controls.Add(this.List_All_Button);
            this.pnlToolbar.Controls.Add(this.List_All_Types);
            this.pnlToolbar.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlToolbar.Location = new System.Drawing.Point(0, 56);
            this.pnlToolbar.Name = "pnlToolbar";
            this.pnlToolbar.Padding = new System.Windows.Forms.Padding(8, 2, 8, 10);
            this.pnlToolbar.Size = new System.Drawing.Size(820, 52);
            this.pnlToolbar.TabIndex = 1;
            //
            // SearchBox
            //
            this.SearchBox.Margin = new System.Windows.Forms.Padding(4, 7, 4, 4);
            this.SearchBox.Name = "SearchBox";
            this.SearchBox.Size = new System.Drawing.Size(320, 26);
            this.SearchBox.TabIndex = 0;
            this.SearchBox.TextChanged += new System.EventHandler(this.SearchBox_TextChanged);
            //
            // Search
            //
            this.Search.Margin = new System.Windows.Forms.Padding(4);
            this.Search.Name = "Search";
            this.Search.Size = new System.Drawing.Size(120, 34);
            this.Search.TabIndex = 1;
            this.Search.Text = "Search";
            this.Search.Click += new System.EventHandler(this.Search_Click);
            //
            // List_All_Button
            //
            this.List_All_Button.Margin = new System.Windows.Forms.Padding(4);
            this.List_All_Button.Name = "List_All_Button";
            this.List_All_Button.Size = new System.Drawing.Size(120, 34);
            this.List_All_Button.TabIndex = 2;
            this.List_All_Button.Text = "List All";
            this.List_All_Button.Click += new System.EventHandler(this.List_All_Button_Click);
            //
            // List_All_Types
            //
            this.List_All_Types.Margin = new System.Windows.Forms.Padding(4);
            this.List_All_Types.Name = "List_All_Types";
            this.List_All_Types.Size = new System.Drawing.Size(120, 34);
            this.List_All_Types.TabIndex = 3;
            this.List_All_Types.Text = "Categories";
            this.List_All_Types.Click += new System.EventHandler(this.List_All_Types_Click);
            //
            // DataOutput
            //
            this.DataOutput.AllowUserToAddRows = false;
            this.DataOutput.AllowUserToDeleteRows = false;
            this.DataOutput.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.DataOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DataOutput.Location = new System.Drawing.Point(0, 108);
            this.DataOutput.Name = "DataOutput";
            this.DataOutput.ReadOnly = true;
            this.DataOutput.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.DataOutput.Size = new System.Drawing.Size(820, 612);
            this.DataOutput.TabIndex = 2;
            this.DataOutput.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DataOutput_CellContentClick);
            //
            // Main
            //
            this.AcceptButton = this.Search;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(820, 720);
            // Order matters for docking: Fill control first, then Top controls (last added = top-most)
            this.Controls.Add(this.DataOutput);
            this.Controls.Add(this.pnlToolbar);
            this.Controls.Add(this.lblTitle);
            this.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.MinimumSize = new System.Drawing.Size(640, 400);
            this.Name = "Main";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Elden Ring Wiki";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.pnlToolbar.ResumeLayout(false);
            this.pnlToolbar.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DataOutput)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.FlowLayoutPanel pnlToolbar;
        private System.Windows.Forms.TextBox SearchBox;
        private System.Windows.Forms.Button Search;
        private System.Windows.Forms.DataGridView DataOutput;
        private System.Windows.Forms.Button List_All_Button;
        private System.Windows.Forms.Button List_All_Types;
    }
}

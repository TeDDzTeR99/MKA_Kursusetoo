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
            this.btnLanguage = new System.Windows.Forms.Button();
            this.pnlToolbar = new System.Windows.Forms.FlowLayoutPanel();
            this.btnHome = new System.Windows.Forms.Button();
            this.btnBack = new System.Windows.Forms.Button();
            this.SearchBox = new System.Windows.Forms.TextBox();
            this.Search = new System.Windows.Forms.Button();
            this.lblPage = new System.Windows.Forms.Label();
            this.pnlTiles = new System.Windows.Forms.FlowLayoutPanel();
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
            this.lblTitle.Text = "Elden Ring Wiki";
            //
            // btnLanguage
            //
            this.btnLanguage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLanguage.Location = new System.Drawing.Point(704, 14);
            this.btnLanguage.Name = "btnLanguage";
            this.btnLanguage.Size = new System.Drawing.Size(100, 30);
            this.btnLanguage.TabIndex = 5;
            this.btnLanguage.Text = "Eesti";
            this.btnLanguage.Click += new System.EventHandler(this.btnLanguage_Click);
            //
            // pnlToolbar
            //
            this.pnlToolbar.AutoSize = true;
            this.pnlToolbar.Controls.Add(this.btnHome);
            this.pnlToolbar.Controls.Add(this.btnBack);
            this.pnlToolbar.Controls.Add(this.SearchBox);
            this.pnlToolbar.Controls.Add(this.Search);
            this.pnlToolbar.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlToolbar.Location = new System.Drawing.Point(0, 56);
            this.pnlToolbar.Name = "pnlToolbar";
            this.pnlToolbar.Padding = new System.Windows.Forms.Padding(8, 2, 8, 6);
            this.pnlToolbar.Size = new System.Drawing.Size(820, 48);
            this.pnlToolbar.TabIndex = 1;
            //
            // btnHome
            //
            this.btnHome.Margin = new System.Windows.Forms.Padding(4);
            this.btnHome.Name = "btnHome";
            this.btnHome.Size = new System.Drawing.Size(100, 34);
            this.btnHome.TabIndex = 0;
            this.btnHome.Text = "Home";
            this.btnHome.Click += new System.EventHandler(this.btnHome_Click);
            //
            // btnBack
            //
            this.btnBack.Margin = new System.Windows.Forms.Padding(4);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(100, 34);
            this.btnBack.TabIndex = 1;
            this.btnBack.Text = "Back";
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            //
            // SearchBox
            //
            this.SearchBox.Margin = new System.Windows.Forms.Padding(20, 7, 4, 4);
            this.SearchBox.Name = "SearchBox";
            this.SearchBox.Size = new System.Drawing.Size(340, 26);
            this.SearchBox.TabIndex = 2;
            //
            // Search
            //
            this.Search.Margin = new System.Windows.Forms.Padding(4);
            this.Search.Name = "Search";
            this.Search.Size = new System.Drawing.Size(120, 34);
            this.Search.TabIndex = 3;
            this.Search.Text = "Search";
            this.Search.Click += new System.EventHandler(this.Search_Click);
            //
            // lblPage
            //
            this.lblPage.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblPage.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblPage.Location = new System.Drawing.Point(0, 104);
            this.lblPage.Name = "lblPage";
            this.lblPage.Padding = new System.Windows.Forms.Padding(14, 0, 0, 0);
            this.lblPage.Size = new System.Drawing.Size(820, 36);
            this.lblPage.TabIndex = 2;
            this.lblPage.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // pnlTiles
            //
            this.pnlTiles.AutoScroll = true;
            this.pnlTiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlTiles.Location = new System.Drawing.Point(0, 140);
            this.pnlTiles.Name = "pnlTiles";
            this.pnlTiles.Padding = new System.Windows.Forms.Padding(8);
            this.pnlTiles.Size = new System.Drawing.Size(820, 580);
            this.pnlTiles.TabIndex = 3;
            //
            // DataOutput
            //
            this.DataOutput.AllowUserToAddRows = false;
            this.DataOutput.AllowUserToDeleteRows = false;
            this.DataOutput.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.DataOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DataOutput.Location = new System.Drawing.Point(0, 140);
            this.DataOutput.Name = "DataOutput";
            this.DataOutput.ReadOnly = true;
            this.DataOutput.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.DataOutput.Size = new System.Drawing.Size(820, 580);
            this.DataOutput.TabIndex = 4;
            this.DataOutput.Visible = false;
            this.DataOutput.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DataOutput_CellContentClick);
            //
            // Main
            //
            this.AcceptButton = this.Search;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(820, 720);
            // Order matters for docking: Fill controls first, then Top controls (last added = top-most).
            // Only one of pnlTiles and DataOutput is visible at a time.
            this.Controls.Add(this.DataOutput);
            this.Controls.Add(this.pnlTiles);
            this.Controls.Add(this.lblPage);
            this.Controls.Add(this.pnlToolbar);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.btnLanguage);
            this.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.MinimumSize = new System.Drawing.Size(720, 400);
            this.Name = "Main";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Elden Ring Wiki";
            this.Load += new System.EventHandler(this.Main_Load);
            this.pnlToolbar.ResumeLayout(false);
            this.pnlToolbar.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DataOutput)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Button btnLanguage;
        private System.Windows.Forms.FlowLayoutPanel pnlToolbar;
        private System.Windows.Forms.Button btnHome;
        private System.Windows.Forms.Button btnBack;
        private System.Windows.Forms.TextBox SearchBox;
        private System.Windows.Forms.Button Search;
        private System.Windows.Forms.Label lblPage;
        private System.Windows.Forms.FlowLayoutPanel pnlTiles;
        private System.Windows.Forms.DataGridView DataOutput;
    }
}

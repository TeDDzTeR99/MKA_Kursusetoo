namespace MKA_KursuseToo
{
    partial class DetailedView
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
            this.DetailedViewGrid = new System.Windows.Forms.DataGridView();
            this.lblName = new System.Windows.Forms.Label();
            this.pnlScalingOptions = new System.Windows.Forms.FlowLayoutPanel();
            this.lblAffinity = new System.Windows.Forms.Label();
            this.cmbAffinity = new System.Windows.Forms.ComboBox();
            this.lblLevel = new System.Windows.Forms.Label();
            this.cmbLevel = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)(this.DetailedViewGrid)).BeginInit();
            this.pnlScalingOptions.SuspendLayout();
            this.SuspendLayout();
            // 
            // DetailedViewGrid
            // 
            this.DetailedViewGrid.AllowUserToAddRows = false;
            this.DetailedViewGrid.AllowUserToDeleteRows = false;
            this.DetailedViewGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.DetailedViewGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DetailedViewGrid.Location = new System.Drawing.Point(0, 76);
            this.DetailedViewGrid.Name = "DetailedViewGrid";
            this.DetailedViewGrid.ReadOnly = true;
            this.DetailedViewGrid.Size = new System.Drawing.Size(800, 374);
            this.DetailedViewGrid.TabIndex = 0;
            this.DetailedViewGrid.CellToolTipTextNeeded += new System.Windows.Forms.DataGridViewCellToolTipTextNeededEventHandler(this.DetailedViewGrid_CellToolTipTextNeeded);
            //
            // pnlScalingOptions
            //
            this.pnlScalingOptions.AutoSize = true;
            this.pnlScalingOptions.Controls.Add(this.lblAffinity);
            this.pnlScalingOptions.Controls.Add(this.cmbAffinity);
            this.pnlScalingOptions.Controls.Add(this.lblLevel);
            this.pnlScalingOptions.Controls.Add(this.cmbLevel);
            this.pnlScalingOptions.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlScalingOptions.Font = new System.Drawing.Font("Courier New", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.pnlScalingOptions.Location = new System.Drawing.Point(0, 36);
            this.pnlScalingOptions.Name = "pnlScalingOptions";
            this.pnlScalingOptions.Padding = new System.Windows.Forms.Padding(4);
            this.pnlScalingOptions.Size = new System.Drawing.Size(800, 40);
            this.pnlScalingOptions.TabIndex = 2;
            //
            // lblAffinity
            //
            this.lblAffinity.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblAffinity.AutoSize = true;
            this.lblAffinity.Name = "lblAffinity";
            this.lblAffinity.Text = "Affinity:";
            //
            // cmbAffinity
            //
            this.cmbAffinity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAffinity.Name = "cmbAffinity";
            this.cmbAffinity.Size = new System.Drawing.Size(160, 26);
            this.cmbAffinity.TabIndex = 0;
            this.cmbAffinity.SelectedIndexChanged += new System.EventHandler(this.cmbAffinity_SelectedIndexChanged);
            //
            // lblLevel
            //
            this.lblLevel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblLevel.AutoSize = true;
            this.lblLevel.Margin = new System.Windows.Forms.Padding(20, 0, 3, 0);
            this.lblLevel.Name = "lblLevel";
            this.lblLevel.Text = "Upgrade level:";
            //
            // cmbLevel
            //
            this.cmbLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbLevel.Name = "cmbLevel";
            this.cmbLevel.Size = new System.Drawing.Size(80, 26);
            this.cmbLevel.TabIndex = 1;
            this.cmbLevel.SelectedIndexChanged += new System.EventHandler(this.cmbLevel_SelectedIndexChanged);
            // 
            // lblName
            // 
            this.lblName.AutoSize = true;
            this.lblName.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblName.Font = new System.Drawing.Font("Courier New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblName.Location = new System.Drawing.Point(0, 0);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(0, 36);
            this.lblName.TabIndex = 1;
            // 
            // DetailedView
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(800, 450);
            // Order matters for docking: Fill control first, then Top controls (last added = top-most)
            this.Controls.Add(this.DetailedViewGrid);
            this.Controls.Add(this.pnlScalingOptions);
            this.Controls.Add(this.lblName);
            this.Font = new System.Drawing.Font("Courier New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "DetailedView";
            this.Text = "DetailedView";
            this.Load += new System.EventHandler(this.DetailedView_Load);
            ((System.ComponentModel.ISupportInitialize)(this.DetailedViewGrid)).EndInit();
            this.pnlScalingOptions.ResumeLayout(false);
            this.pnlScalingOptions.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView DetailedViewGrid;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.FlowLayoutPanel pnlScalingOptions;
        private System.Windows.Forms.Label lblAffinity;
        private System.Windows.Forms.ComboBox cmbAffinity;
        private System.Windows.Forms.Label lblLevel;
        private System.Windows.Forms.ComboBox cmbLevel;
    }
}
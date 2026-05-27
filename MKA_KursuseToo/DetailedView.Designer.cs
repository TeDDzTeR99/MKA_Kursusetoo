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
            ((System.ComponentModel.ISupportInitialize)(this.DetailedViewGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // DetailedViewGrid
            // 
            this.DetailedViewGrid.AllowUserToAddRows = false;
            this.DetailedViewGrid.AllowUserToDeleteRows = false;
            this.DetailedViewGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.DetailedViewGrid.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.DetailedViewGrid.Location = new System.Drawing.Point(0, 44);
            this.DetailedViewGrid.Name = "DetailedViewGrid";
            this.DetailedViewGrid.ReadOnly = true;
            this.DetailedViewGrid.Size = new System.Drawing.Size(800, 406);
            this.DetailedViewGrid.TabIndex = 0;
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
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.DetailedViewGrid);
            this.Font = new System.Drawing.Font("Courier New", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "DetailedView";
            this.Text = "DetailedView";
            this.Load += new System.EventHandler(this.DetailedView_Load);
            ((System.ComponentModel.ISupportInitialize)(this.DetailedViewGrid)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView DetailedViewGrid;
        private System.Windows.Forms.Label lblName;
    }
}
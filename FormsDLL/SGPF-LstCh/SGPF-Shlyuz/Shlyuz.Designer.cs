namespace SGPF_LstCh
{
    partial class Shlyuz
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
            this.dgShlyuz = new System.Windows.Forms.DataGrid();
            this.lHeadP = new System.Windows.Forms.Label();
            this.lEAN = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // dgShlyuz
            // 
            this.dgShlyuz.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.dgShlyuz.ColumnHeadersVisible = false;
            this.dgShlyuz.Font = new System.Drawing.Font("Arial", 26F, System.Drawing.FontStyle.Regular);
            this.dgShlyuz.Location = new System.Drawing.Point(0, 24);
            this.dgShlyuz.Name = "dgShlyuz";
            this.dgShlyuz.RowHeadersVisible = false;
            this.dgShlyuz.Size = new System.Drawing.Size(237, 269);
            this.dgShlyuz.TabIndex = 4;
            this.dgShlyuz.LostFocus += new System.EventHandler(this.dgShlyuz_LostFocus);
            this.dgShlyuz.GotFocus += new System.EventHandler(this.dgShlyuz_GotFocus);
            // 
            // lHeadP
            // 
            this.lHeadP.BackColor = System.Drawing.Color.MediumAquamarine;
            this.lHeadP.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Bold);
            this.lHeadP.Location = new System.Drawing.Point(0, 0);
            this.lHeadP.Name = "lHeadP";
            this.lHeadP.Size = new System.Drawing.Size(240, 22);
            this.lHeadP.Text = "Выбор продукции";
            this.lHeadP.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // lEAN
            // 
            this.lEAN.BackColor = System.Drawing.Color.PaleTurquoise;
            this.lEAN.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Bold);
            this.lEAN.Location = new System.Drawing.Point(0, 295);
            this.lEAN.Name = "lEAN";
            this.lEAN.Size = new System.Drawing.Size(240, 22);
            this.lEAN.Text = "EAN";
            this.lEAN.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // Shlyuz
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoScroll = true;
            this.BackColor = System.Drawing.SystemColors.ControlLight;
            this.ClientSize = new System.Drawing.Size(240, 320);
            this.ControlBox = false;
            this.Controls.Add(this.lEAN);
            this.Controls.Add(this.lHeadP);
            this.Controls.Add(this.dgShlyuz);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.KeyPreview = true;
            this.Name = "Shlyuz";
            this.Activated += new System.EventHandler(this.Shlyuz_Activated);
            this.Closing += new System.ComponentModel.CancelEventHandler(this.Shlyuz_Closing);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Shlyuz_KeyPress);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Shlyuz_KeyDown);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGrid dgShlyuz;
        private System.Windows.Forms.Label lHeadP;
        private System.Windows.Forms.Label lEAN;
    }
}
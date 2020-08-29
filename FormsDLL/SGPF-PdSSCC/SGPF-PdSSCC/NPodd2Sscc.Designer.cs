namespace SGPF_PdSSCC
{
    partial class NPodd2Sscc
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
            this.tSSCC = new System.Windows.Forms.TextBox();
            this.lLstUch = new System.Windows.Forms.Label();
            this.lNomPodd = new System.Windows.Forms.Label();
            this.tNomPoddon = new System.Windows.Forms.TextBox();
            this.lNomsAvail = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.lMestOnPodd = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // tSSCC
            // 
            this.tSSCC.Enabled = false;
            this.tSSCC.Location = new System.Drawing.Point(57, 102);
            this.tSSCC.Multiline = true;
            this.tSSCC.Name = "tSSCC";
            this.tSSCC.Size = new System.Drawing.Size(152, 23);
            this.tSSCC.TabIndex = 13;
            this.tSSCC.Text = "01234567890123456789";
            this.tSSCC.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // lLstUch
            // 
            this.lLstUch.Font = new System.Drawing.Font("Tahoma", 10F, System.Drawing.FontStyle.Bold);
            this.lLstUch.Location = new System.Drawing.Point(13, 103);
            this.lLstUch.Name = "lLstUch";
            this.lLstUch.Size = new System.Drawing.Size(41, 20);
            this.lLstUch.Text = "SSCC";
            // 
            // lNomPodd
            // 
            this.lNomPodd.Font = new System.Drawing.Font("Tahoma", 10F, System.Drawing.FontStyle.Bold);
            this.lNomPodd.Location = new System.Drawing.Point(13, 18);
            this.lNomPodd.Name = "lNomPodd";
            this.lNomPodd.Size = new System.Drawing.Size(148, 20);
            this.lNomPodd.Text = "№ для комплектации";
            // 
            // tNomPoddon
            // 
            this.tNomPoddon.Enabled = false;
            this.tNomPoddon.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Regular);
            this.tNomPoddon.Location = new System.Drawing.Point(168, 17);
            this.tNomPoddon.Multiline = true;
            this.tNomPoddon.Name = "tNomPoddon";
            this.tNomPoddon.Size = new System.Drawing.Size(36, 23);
            this.tNomPoddon.TabIndex = 22;
            this.tNomPoddon.Text = "023";
            this.tNomPoddon.GotFocus += new System.EventHandler(this.SelAllTextF);
            // 
            // lNomsAvail
            // 
            this.lNomsAvail.Font = new System.Drawing.Font("Tahoma", 10F, System.Drawing.FontStyle.Bold);
            this.lNomsAvail.Location = new System.Drawing.Point(127, 46);
            this.lNomsAvail.Name = "lNomsAvail";
            this.lNomsAvail.Size = new System.Drawing.Size(79, 20);
            this.lNomsAvail.Text = "(23-987)";
            this.lNomsAvail.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.LightBlue;
            this.label1.Font = new System.Drawing.Font("Tahoma", 10F, System.Drawing.FontStyle.Bold);
            this.label1.Location = new System.Drawing.Point(0, 147);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(220, 20);
            this.label1.Text = "<ENT> - установить и выйти";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // lMestOnPodd
            // 
            this.lMestOnPodd.Font = new System.Drawing.Font("Tahoma", 10F, System.Drawing.FontStyle.Bold);
            this.lMestOnPodd.Location = new System.Drawing.Point(13, 69);
            this.lMestOnPodd.Name = "lMestOnPodd";
            this.lMestOnPodd.Size = new System.Drawing.Size(196, 20);
            this.lMestOnPodd.Text = "Мест";
            // 
            // NPodd2Sscc
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoScroll = true;
            this.BackColor = System.Drawing.Color.Gainsboro;
            this.ClientSize = new System.Drawing.Size(220, 169);
            this.ControlBox = false;
            this.Controls.Add(this.lMestOnPodd);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lNomsAvail);
            this.Controls.Add(this.lNomPodd);
            this.Controls.Add(this.tNomPoddon);
            this.Controls.Add(this.lLstUch);
            this.Controls.Add(this.tSSCC);
            this.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold);
            this.KeyPreview = true;
            this.Location = new System.Drawing.Point(0, 28);
            this.Name = "NPodd2Sscc";
            this.Text = "          Текущий  поддон";
            this.Activated += new System.EventHandler(this.NPodd2Sscc_Activated);
            this.Closing += new System.ComponentModel.CancelEventHandler(this.NPodd2Sscc_Closing);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.NPodd2Sscc_KeyPress);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.NPodd2Sscc_KeyDown);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox tSSCC;
        private System.Windows.Forms.Label lLstUch;
        private System.Windows.Forms.Label lNomPodd;
        private System.Windows.Forms.TextBox tNomPoddon;
        private System.Windows.Forms.Label lNomsAvail;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lMestOnPodd;
    }
}
namespace SGPF_Avtor
{
    partial class Avtor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param m_Name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
            this.lUser = new System.Windows.Forms.Label();
            this.lPass = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.cbReg = new System.Windows.Forms.ComboBox();
            this.lRegSm = new System.Windows.Forms.Label();
            this.tDefNameUch = new System.Windows.Forms.TextBox();
            this.tDefKSKL = new System.Windows.Forms.TextBox();
            this.tDefUch = new System.Windows.Forms.TextBox();
            this.tDefDateDoc = new System.Windows.Forms.TextBox();
            this.lDefUch = new System.Windows.Forms.Label();
            this.tDefNameSkl = new System.Windows.Forms.TextBox();
            this.lDefSklad = new System.Windows.Forms.Label();
            this.lDefDatDoc = new System.Windows.Forms.Label();
            this.tUser = new System.Windows.Forms.TextBox();
            this.tPass = new System.Windows.Forms.TextBox();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lUser
            // 
            this.lUser.Location = new System.Drawing.Point(7, 8);
            this.lUser.Name = "lUser";
            this.lUser.Size = new System.Drawing.Size(85, 20);
            this.lUser.Text = "Пользователь";
            // 
            // lPass
            // 
            this.lPass.Location = new System.Drawing.Point(7, 31);
            this.lPass.Name = "lPass";
            this.lPass.Size = new System.Drawing.Size(85, 20);
            this.lPass.Text = "Пароль";
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.ControlLight;
            this.panel1.Controls.Add(this.cbReg);
            this.panel1.Controls.Add(this.lRegSm);
            this.panel1.Controls.Add(this.tDefNameUch);
            this.panel1.Controls.Add(this.tDefKSKL);
            this.panel1.Controls.Add(this.tDefUch);
            this.panel1.Controls.Add(this.tDefDateDoc);
            this.panel1.Controls.Add(this.lDefUch);
            this.panel1.Controls.Add(this.tDefNameSkl);
            this.panel1.Controls.Add(this.lDefSklad);
            this.panel1.Controls.Add(this.lDefDatDoc);
            this.panel1.Location = new System.Drawing.Point(4, 56);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(210, 110);
            // 
            // cbReg
            // 
            this.cbReg.Enabled = false;
            this.cbReg.Location = new System.Drawing.Point(72, 76);
            this.cbReg.Name = "cbReg";
            this.cbReg.Size = new System.Drawing.Size(134, 23);
            this.cbReg.TabIndex = 3;
            // 
            // lRegSm
            // 
            this.lRegSm.BackColor = System.Drawing.Color.Lavender;
            this.lRegSm.Font = new System.Drawing.Font("Tahoma", 10F, System.Drawing.FontStyle.Bold);
            this.lRegSm.Location = new System.Drawing.Point(5, 78);
            this.lRegSm.Name = "lRegSm";
            this.lRegSm.Size = new System.Drawing.Size(60, 18);
            this.lRegSm.Text = "Режим";
            // 
            // tDefNameUch
            // 
            this.tDefNameUch.Location = new System.Drawing.Point(106, 31);
            this.tDefNameUch.Multiline = true;
            this.tDefNameUch.Name = "tDefNameUch";
            this.tDefNameUch.ReadOnly = true;
            this.tDefNameUch.Size = new System.Drawing.Size(100, 20);
            this.tDefNameUch.TabIndex = 8;
            // 
            // tDefKSKL
            // 
            this.tDefKSKL.Enabled = false;
            this.tDefKSKL.Font = new System.Drawing.Font("Tahoma", 10F, System.Drawing.FontStyle.Bold);
            this.tDefKSKL.Location = new System.Drawing.Point(72, 8);
            this.tDefKSKL.Multiline = true;
            this.tDefKSKL.Name = "tDefKSKL";
            this.tDefKSKL.Size = new System.Drawing.Size(32, 20);
            this.tDefKSKL.TabIndex = 0;
            this.tDefKSKL.Text = "658";
            this.tDefKSKL.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.tDefKSKL.GotFocus += new System.EventHandler(this.SelAllTextF);
            this.tDefKSKL.Validating += new System.ComponentModel.CancelEventHandler(this.tDefKSKL_Validating);
            // 
            // tDefUch
            // 
            this.tDefUch.Enabled = false;
            this.tDefUch.Font = new System.Drawing.Font("Tahoma", 10F, System.Drawing.FontStyle.Bold);
            this.tDefUch.Location = new System.Drawing.Point(72, 32);
            this.tDefUch.Multiline = true;
            this.tDefUch.Name = "tDefUch";
            this.tDefUch.Size = new System.Drawing.Size(32, 18);
            this.tDefUch.TabIndex = 1;
            this.tDefUch.Text = "22";
            this.tDefUch.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.tDefUch.GotFocus += new System.EventHandler(this.SelAllTextF);
            this.tDefUch.Validating += new System.ComponentModel.CancelEventHandler(this.tDefUch_Validating);
            // 
            // tDefDateDoc
            // 
            this.tDefDateDoc.Enabled = false;
            this.tDefDateDoc.Location = new System.Drawing.Point(72, 55);
            this.tDefDateDoc.Multiline = true;
            this.tDefDateDoc.Name = "tDefDateDoc";
            this.tDefDateDoc.Size = new System.Drawing.Size(60, 18);
            this.tDefDateDoc.TabIndex = 2;
            this.tDefDateDoc.Text = "22.04.07";
            this.tDefDateDoc.GotFocus += new System.EventHandler(this.SelAllTextF);
            this.tDefDateDoc.Validating += new System.ComponentModel.CancelEventHandler(this.tDefDateDoc_Validating);
            // 
            // lDefUch
            // 
            this.lDefUch.BackColor = System.Drawing.Color.Lavender;
            this.lDefUch.Font = new System.Drawing.Font("Tahoma", 10F, System.Drawing.FontStyle.Bold);
            this.lDefUch.Location = new System.Drawing.Point(5, 32);
            this.lDefUch.Name = "lDefUch";
            this.lDefUch.Size = new System.Drawing.Size(60, 18);
            this.lDefUch.Text = "Участок";
            // 
            // tDefNameSkl
            // 
            this.tDefNameSkl.Location = new System.Drawing.Point(106, 8);
            this.tDefNameSkl.Multiline = true;
            this.tDefNameSkl.Name = "tDefNameSkl";
            this.tDefNameSkl.ReadOnly = true;
            this.tDefNameSkl.Size = new System.Drawing.Size(100, 20);
            this.tDefNameSkl.TabIndex = 5;
            this.tDefNameSkl.Text = "Масло";
            this.tDefNameSkl.WordWrap = false;
            // 
            // lDefSklad
            // 
            this.lDefSklad.BackColor = System.Drawing.Color.Lavender;
            this.lDefSklad.Font = new System.Drawing.Font("Tahoma", 10F, System.Drawing.FontStyle.Bold);
            this.lDefSklad.Location = new System.Drawing.Point(5, 9);
            this.lDefSklad.Name = "lDefSklad";
            this.lDefSklad.Size = new System.Drawing.Size(60, 18);
            this.lDefSklad.Text = "Склад";
            // 
            // lDefDatDoc
            // 
            this.lDefDatDoc.BackColor = System.Drawing.Color.Lavender;
            this.lDefDatDoc.Font = new System.Drawing.Font("Tahoma", 10F, System.Drawing.FontStyle.Bold);
            this.lDefDatDoc.Location = new System.Drawing.Point(5, 55);
            this.lDefDatDoc.Name = "lDefDatDoc";
            this.lDefDatDoc.Size = new System.Drawing.Size(60, 18);
            this.lDefDatDoc.Text = "Дата";
            // 
            // tUser
            // 
            this.tUser.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.tUser.Location = new System.Drawing.Point(99, 6);
            this.tUser.MaxLength = 64;
            this.tUser.Name = "tUser";
            this.tUser.Size = new System.Drawing.Size(115, 21);
            this.tUser.TabIndex = 0;
            this.tUser.TextChanged += new System.EventHandler(this.tUser_TextChanged);
            this.tUser.Validated += new System.EventHandler(this.tUser_Validated);
            this.tUser.GotFocus += new System.EventHandler(this.SelAllTextF);
            this.tUser.Validating += new System.ComponentModel.CancelEventHandler(this.tUser_Validating);
            // 
            // tPass
            // 
            this.tPass.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Bold);
            this.tPass.Location = new System.Drawing.Point(99, 31);
            this.tPass.MaxLength = 64;
            this.tPass.Name = "tPass";
            this.tPass.PasswordChar = '*';
            this.tPass.Size = new System.Drawing.Size(115, 19);
            this.tPass.TabIndex = 1;
            this.tPass.GotFocus += new System.EventHandler(this.SelAllTextF);
            this.tPass.Validating += new System.ComponentModel.CancelEventHandler(this.tPass_Validating);
            // 
            // Avtor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoScroll = true;
            this.AutoValidate = System.Windows.Forms.AutoValidate.EnablePreventFocusChange;
            this.ClientSize = new System.Drawing.Size(218, 180);
            this.ControlBox = false;
            this.Controls.Add(this.tPass);
            this.Controls.Add(this.tUser);
            this.Controls.Add(this.lPass);
            this.Controls.Add(this.lUser);
            this.Controls.Add(this.panel1);
            this.KeyPreview = true;
            this.Name = "Avtor";
            this.Text = "Параметры сеанса";
            this.Load += new System.EventHandler(this.Avtor_Load);
            this.Closing += new System.ComponentModel.CancelEventHandler(this.Avtor_Closing);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Avtor_KeyPress);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Avtor_KeyDown);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lUser;
        private System.Windows.Forms.Label lPass;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TextBox tDefKSKL;
        private System.Windows.Forms.Label lDefUch;
        private System.Windows.Forms.TextBox tDefUch;
        private System.Windows.Forms.TextBox tDefNameSkl;
        private System.Windows.Forms.Label lDefSklad;
        private System.Windows.Forms.TextBox tDefDateDoc;
        private System.Windows.Forms.Label lDefDatDoc;
        private System.Windows.Forms.TextBox tUser;
        private System.Windows.Forms.TextBox tPass;
        private System.Windows.Forms.TextBox tDefNameUch;
        private System.Windows.Forms.ComboBox cbReg;
        private System.Windows.Forms.Label lRegSm;
    }
}
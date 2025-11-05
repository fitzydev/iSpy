namespace iSpyApplication
{
    partial class SSLConfig
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
            txtSSLCertificate = new TextBox();
            btnOK = new Button();
            button2 = new Button();
            tlpSSL = new TableLayoutPanel();
            label1 = new Label();
            chkRequireClientCertificate = new CheckBox();
            chkIgnorePolicyErrors = new CheckBox();
            chkCheckRevocation = new CheckBox();
            chkEnableSSL = new CheckBox();
            panel1 = new Panel();
            panel2 = new Panel();
            tlpSSL.SuspendLayout();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // txtSSLCertificate
            // 
            txtSSLCertificate.Location = new Point(146, 7);
            txtSSLCertificate.Margin = new Padding(7);
            txtSSLCertificate.Name = "txtSSLCertificate";
            txtSSLCertificate.ReadOnly = true;
            txtSSLCertificate.Size = new Size(138, 23);
            txtSSLCertificate.TabIndex = 0;
            // 
            // btnOK
            // 
            btnOK.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOK.Location = new Point(337, 4);
            btnOK.Margin = new Padding(4);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(84, 26);
            btnOK.TabIndex = 3;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += button1_Click;
            // 
            // button2
            // 
            button2.Location = new Point(295, 4);
            button2.Margin = new Padding(4);
            button2.Name = "button2";
            button2.Size = new Size(39, 26);
            button2.TabIndex = 4;
            button2.Text = "...";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // tlpSSL
            // 
            tlpSSL.ColumnCount = 3;
            tlpSSL.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 47.94007F));
            tlpSSL.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52.05993F));
            tlpSSL.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 143F));
            tlpSSL.Controls.Add(button2, 2, 0);
            tlpSSL.Controls.Add(txtSSLCertificate, 1, 0);
            tlpSSL.Controls.Add(label1, 0, 0);
            tlpSSL.Controls.Add(chkRequireClientCertificate, 1, 1);
            tlpSSL.Controls.Add(chkIgnorePolicyErrors, 1, 2);
            tlpSSL.Controls.Add(chkCheckRevocation, 1, 3);
            tlpSSL.Dock = DockStyle.Fill;
            tlpSSL.Location = new Point(0, 33);
            tlpSSL.Margin = new Padding(4);
            tlpSSL.Name = "tlpSSL";
            tlpSSL.RowCount = 4;
            tlpSSL.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            tlpSSL.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            tlpSSL.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            tlpSSL.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            tlpSSL.Size = new Size(435, 190);
            tlpSSL.TabIndex = 5;
            tlpSSL.Paint += tableLayoutPanel2_Paint;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(7, 7);
            label1.Margin = new Padding(7);
            label1.Name = "label1";
            label1.Size = new Size(61, 15);
            label1.TabIndex = 5;
            label1.Text = "Certificate";
            // 
            // chkRequireClientCertificate
            // 
            chkRequireClientCertificate.AutoSize = true;
            tlpSSL.SetColumnSpan(chkRequireClientCertificate, 2);
            chkRequireClientCertificate.Location = new Point(146, 43);
            chkRequireClientCertificate.Margin = new Padding(7);
            chkRequireClientCertificate.Name = "chkRequireClientCertificate";
            chkRequireClientCertificate.Size = new Size(157, 19);
            chkRequireClientCertificate.TabIndex = 8;
            chkRequireClientCertificate.Text = "Require Client Certificate";
            chkRequireClientCertificate.UseVisualStyleBackColor = true;
            // 
            // chkIgnorePolicyErrors
            // 
            chkIgnorePolicyErrors.AutoSize = true;
            tlpSSL.SetColumnSpan(chkIgnorePolicyErrors, 2);
            chkIgnorePolicyErrors.Location = new Point(146, 79);
            chkIgnorePolicyErrors.Margin = new Padding(7);
            chkIgnorePolicyErrors.Name = "chkIgnorePolicyErrors";
            chkIgnorePolicyErrors.Size = new Size(114, 19);
            chkIgnorePolicyErrors.TabIndex = 9;
            chkIgnorePolicyErrors.Text = "Ignore SSL Errors";
            chkIgnorePolicyErrors.UseVisualStyleBackColor = true;
            // 
            // chkCheckRevocation
            // 
            chkCheckRevocation.AutoSize = true;
            tlpSSL.SetColumnSpan(chkCheckRevocation, 2);
            chkCheckRevocation.Location = new Point(146, 115);
            chkCheckRevocation.Margin = new Padding(7);
            chkCheckRevocation.Name = "chkCheckRevocation";
            chkCheckRevocation.Size = new Size(142, 19);
            chkCheckRevocation.TabIndex = 10;
            chkCheckRevocation.Text = "Check SSL Revocation";
            chkCheckRevocation.UseVisualStyleBackColor = true;
            // 
            // chkEnableSSL
            // 
            chkEnableSSL.AutoSize = true;
            chkEnableSSL.Dock = DockStyle.Top;
            chkEnableSSL.Location = new Point(0, 0);
            chkEnableSSL.Margin = new Padding(4);
            chkEnableSSL.Name = "chkEnableSSL";
            chkEnableSSL.Padding = new Padding(7);
            chkEnableSSL.Size = new Size(435, 33);
            chkEnableSSL.TabIndex = 11;
            chkEnableSSL.Text = "Enable SSL";
            chkEnableSSL.UseVisualStyleBackColor = true;
            chkEnableSSL.CheckedChanged += chkEnableSSL_CheckedChanged;
            // 
            // panel1
            // 
            panel1.Controls.Add(tlpSSL);
            panel1.Controls.Add(panel2);
            panel1.Controls.Add(chkEnableSSL);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Margin = new Padding(4);
            panel1.Name = "panel1";
            panel1.Size = new Size(435, 264);
            panel1.TabIndex = 6;
            // 
            // panel2
            // 
            panel2.Controls.Add(btnOK);
            panel2.Dock = DockStyle.Bottom;
            panel2.Location = new Point(0, 223);
            panel2.Margin = new Padding(4);
            panel2.Name = "panel2";
            panel2.Size = new Size(435, 41);
            panel2.TabIndex = 12;
            // 
            // SSLConfig
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(435, 264);
            Controls.Add(panel1);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Margin = new Padding(4);
            Name = "SSLConfig";
            StartPosition = FormStartPosition.CenterParent;
            Text = "SSLConfig";
            Load += SSLConfig_Load;
            tlpSSL.ResumeLayout(false);
            tlpSSL.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel2.ResumeLayout(false);
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox txtSSLCertificate;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TableLayoutPanel tlpSSL;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox chkRequireClientCertificate;
        private System.Windows.Forms.CheckBox chkIgnorePolicyErrors;
        private System.Windows.Forms.CheckBox chkCheckRevocation;
        private System.Windows.Forms.CheckBox chkEnableSSL;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
    }
}
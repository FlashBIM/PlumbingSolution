using System;

namespace PlumbingSolution.UI.BeginUI
{
    partial class LoginForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LoginForm));
            this.chkRemKey = new System.Windows.Forms.CheckBox();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.keyBtn = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.licenseKeyTextBox = new System.Windows.Forms.TextBox();
            this.linkRegister = new System.Windows.Forms.LinkLabel();
            this.linkRequest = new System.Windows.Forms.LinkLabel();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.SuspendLayout();
            // 
            // chkRemKey
            // 
            this.chkRemKey.AutoSize = true;
            this.chkRemKey.Location = new System.Drawing.Point(123, 331);
            this.chkRemKey.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.chkRemKey.Name = "chkRemKey";
            this.chkRemKey.Size = new System.Drawing.Size(140, 24);
            this.chkRemKey.TabIndex = 11;
            this.chkRemKey.Text = "Remember me";
            this.chkRemKey.UseVisualStyleBackColor = true;
            // 
            // pictureBox2
            // 
            this.pictureBox2.Location = new System.Drawing.Point(192, 60);
            this.pictureBox2.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(375, 118);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox2.TabIndex = 7;
            this.pictureBox2.TabStop = false;
            // 
            // keyBtn
            // 
            this.keyBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.keyBtn.Location = new System.Drawing.Point(262, 392);
            this.keyBtn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.keyBtn.Name = "keyBtn";
            this.keyBtn.Size = new System.Drawing.Size(196, 58);
            this.keyBtn.TabIndex = 3;
            this.keyBtn.Text = "Sign In";
            this.keyBtn.UseVisualStyleBackColor = true;
            this.keyBtn.Click += new System.EventHandler(this.SignInBtn2_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(119, 219);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(120, 22);
            this.label5.TabIndex = 2;
            this.label5.Text = "Activated Key";
            // 
            // licenseKeyTextBox
            // 
            this.licenseKeyTextBox.Location = new System.Drawing.Point(123, 269);
            this.licenseKeyTextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.licenseKeyTextBox.Name = "licenseKeyTextBox";
            this.licenseKeyTextBox.Size = new System.Drawing.Size(502, 26);
            this.licenseKeyTextBox.TabIndex = 1;
            // 
            // linkRegister
            // 
            this.linkRegister.AutoSize = true;
            this.linkRegister.Location = new System.Drawing.Point(119, 505);
            this.linkRegister.Name = "linkRegister";
            this.linkRegister.Size = new System.Drawing.Size(136, 20);
            this.linkRegister.TabIndex = 12;
            this.linkRegister.TabStop = true;
            this.linkRegister.Text = "Register  Account";
            this.linkRegister.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkRegister_LinkClicked);
            // 
            // linkRequest
            // 
            this.linkRequest.AutoSize = true;
            this.linkRequest.Location = new System.Drawing.Point(462, 505);
            this.linkRequest.Name = "linkRequest";
            this.linkRequest.Size = new System.Drawing.Size(163, 20);
            this.linkRequest.TabIndex = 13;
            this.linkRequest.TabStop = true;
            this.linkRequest.Text = "Request for Free Trial";
            this.linkRequest.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkRequest_LinkClicked);
            // 
            // LoginForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.ClientSize = new System.Drawing.Size(726, 711);
            this.Controls.Add(this.linkRequest);
            this.Controls.Add(this.linkRegister);
            this.Controls.Add(this.keyBtn);
            this.Controls.Add(this.chkRemKey);
            this.Controls.Add(this.pictureBox2);
            this.Controls.Add(this.licenseKeyTextBox);
            this.Controls.Add(this.label5);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(748, 767);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(748, 767);
            this.Name = "LoginForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Login";
            this.Load += new System.EventHandler(this.LoginForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button keyBtn;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox licenseKeyTextBox;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.CheckBox chkRemKey;
        private System.Windows.Forms.LinkLabel linkRegister;
        private System.Windows.Forms.LinkLabel linkRequest;
    }
}
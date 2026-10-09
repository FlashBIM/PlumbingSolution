namespace PlumbingSolution.FireProtection.UI.GeneralUI
{
    partial class VerticalMEPForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VerticalMEPForm));
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel12 = new System.Windows.Forms.TableLayoutPanel();
            this.label7 = new System.Windows.Forms.Label();
            this.cboFamilyType = new System.Windows.Forms.ComboBox();
            this.lblServiceType = new System.Windows.Forms.Label();
            this.cboSystemType = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cboDiameter = new System.Windows.Forms.ComboBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.lblLevel = new System.Windows.Forms.Label();
            this.lblOffset = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.cboLevelTop = new System.Windows.Forms.ComboBox();
            this.txtOffsetTop = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.cboLevelBottom = new System.Windows.Forms.ComboBox();
            this.txtOffsetBottom = new System.Windows.Forms.TextBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.tableLayoutPanel1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tableLayoutPanel12.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            //
            // tableLayoutPanel1
            //
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.groupBox1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.groupBox2, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(384, 306);
            this.tableLayoutPanel1.TabIndex = 0;
            //
            // groupBox1
            //
            this.groupBox1.Controls.Add(this.tableLayoutPanel12);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(8, 8);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(8, 8, 8, 4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(368, 118);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Pipe";
            //
            // tableLayoutPanel12
            //
            this.tableLayoutPanel12.ColumnCount = 2;
            this.tableLayoutPanel12.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 96F));
            this.tableLayoutPanel12.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel12.Controls.Add(this.label7, 0, 0);
            this.tableLayoutPanel12.Controls.Add(this.cboFamilyType, 1, 0);
            this.tableLayoutPanel12.Controls.Add(this.lblServiceType, 0, 1);
            this.tableLayoutPanel12.Controls.Add(this.cboSystemType, 1, 1);
            this.tableLayoutPanel12.Controls.Add(this.label2, 0, 2);
            this.tableLayoutPanel12.Controls.Add(this.cboDiameter, 1, 2);
            this.tableLayoutPanel12.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel12.Location = new System.Drawing.Point(3, 19);
            this.tableLayoutPanel12.Name = "tableLayoutPanel12";
            this.tableLayoutPanel12.RowCount = 3;
            this.tableLayoutPanel12.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.tableLayoutPanel12.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.tableLayoutPanel12.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.34F));
            this.tableLayoutPanel12.Size = new System.Drawing.Size(360, 102);
            this.tableLayoutPanel12.TabIndex = 0;
            //
            // label7
            //
            this.label7.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(3, 9);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(58, 15);
            this.label7.TabIndex = 0;
            this.label7.Text = "Pipe Type";
            //
            // cboFamilyType
            //
            this.cboFamilyType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cboFamilyType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboFamilyType.FormattingEnabled = true;
            this.cboFamilyType.Location = new System.Drawing.Point(99, 5);
            this.cboFamilyType.Name = "cboFamilyType";
            this.cboFamilyType.Size = new System.Drawing.Size(258, 23);
            this.cboFamilyType.TabIndex = 1;
            this.cboFamilyType.SelectedIndexChanged += new System.EventHandler(this.cboFamilyType_SelectedIndexChanged);
            //
            // lblServiceType
            //
            this.lblServiceType.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblServiceType.AutoSize = true;
            this.lblServiceType.Location = new System.Drawing.Point(3, 43);
            this.lblServiceType.Name = "lblServiceType";
            this.lblServiceType.Size = new System.Drawing.Size(75, 15);
            this.lblServiceType.TabIndex = 2;
            this.lblServiceType.Text = "System Type";
            //
            // cboSystemType
            //
            this.cboSystemType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cboSystemType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSystemType.FormattingEnabled = true;
            this.cboSystemType.Location = new System.Drawing.Point(99, 39);
            this.cboSystemType.Name = "cboSystemType";
            this.cboSystemType.Size = new System.Drawing.Size(258, 23);
            this.cboSystemType.TabIndex = 3;
            //
            // label2
            //
            this.label2.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 77);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(55, 15);
            this.label2.TabIndex = 4;
            this.label2.Text = "Diameter";
            //
            // cboDiameter
            //
            this.cboDiameter.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cboDiameter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboDiameter.FormattingEnabled = true;
            this.cboDiameter.Location = new System.Drawing.Point(99, 73);
            this.cboDiameter.Name = "cboDiameter";
            this.cboDiameter.Size = new System.Drawing.Size(120, 23);
            this.cboDiameter.TabIndex = 5;
            //
            // groupBox2
            //
            this.groupBox2.Controls.Add(this.tableLayoutPanel4);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox2.Location = new System.Drawing.Point(8, 134);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(8, 4, 8, 8);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(368, 118);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Base && Top Elevation";
            //
            // tableLayoutPanel4
            //
            this.tableLayoutPanel4.ColumnCount = 3;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 96F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tableLayoutPanel4.Controls.Add(this.lblLevel, 1, 0);
            this.tableLayoutPanel4.Controls.Add(this.lblOffset, 2, 0);
            this.tableLayoutPanel4.Controls.Add(this.label4, 0, 1);
            this.tableLayoutPanel4.Controls.Add(this.cboLevelTop, 1, 1);
            this.tableLayoutPanel4.Controls.Add(this.txtOffsetTop, 2, 1);
            this.tableLayoutPanel4.Controls.Add(this.label6, 0, 2);
            this.tableLayoutPanel4.Controls.Add(this.cboLevelBottom, 1, 2);
            this.tableLayoutPanel4.Controls.Add(this.txtOffsetBottom, 2, 2);
            this.tableLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel4.Location = new System.Drawing.Point(3, 19);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 3;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel4.Size = new System.Drawing.Size(360, 96);
            this.tableLayoutPanel4.TabIndex = 0;
            //
            // lblLevel
            //
            this.lblLevel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblLevel.AutoSize = true;
            this.lblLevel.Location = new System.Drawing.Point(99, 4);
            this.lblLevel.Name = "lblLevel";
            this.lblLevel.Size = new System.Drawing.Size(34, 15);
            this.lblLevel.TabIndex = 0;
            this.lblLevel.Text = "Level";
            //
            // lblOffset
            //
            this.lblOffset.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblOffset.AutoSize = true;
            this.lblOffset.Location = new System.Drawing.Point(258, 4);
            this.lblOffset.Name = "lblOffset";
            this.lblOffset.Size = new System.Drawing.Size(39, 15);
            this.lblOffset.TabIndex = 1;
            this.lblOffset.Text = "Offset";
            //
            // label4
            //
            this.label4.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(3, 36);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(27, 15);
            this.label4.TabIndex = 2;
            this.label4.Text = "Top";
            //
            // cboLevelTop
            //
            this.cboLevelTop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cboLevelTop.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboLevelTop.FormattingEnabled = true;
            this.cboLevelTop.Location = new System.Drawing.Point(99, 32);
            this.cboLevelTop.Name = "cboLevelTop";
            this.cboLevelTop.Size = new System.Drawing.Size(153, 23);
            this.cboLevelTop.TabIndex = 3;
            //
            // txtOffsetTop
            //
            this.txtOffsetTop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOffsetTop.Location = new System.Drawing.Point(258, 32);
            this.txtOffsetTop.Name = "txtOffsetTop";
            this.txtOffsetTop.Size = new System.Drawing.Size(99, 23);
            this.txtOffsetTop.TabIndex = 4;
            this.txtOffsetTop.Text = "0";
            this.txtOffsetTop.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TxtInch_KeyDown);
            this.txtOffsetTop.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TxtInch_KeyPress);
            this.txtOffsetTop.Leave += new System.EventHandler(this.TxtInch_Leave);
            //
            // label6
            //
            this.label6.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(3, 72);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(31, 15);
            this.label6.TabIndex = 5;
            this.label6.Text = "Base";
            //
            // cboLevelBottom
            //
            this.cboLevelBottom.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cboLevelBottom.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboLevelBottom.FormattingEnabled = true;
            this.cboLevelBottom.Location = new System.Drawing.Point(99, 68);
            this.cboLevelBottom.Name = "cboLevelBottom";
            this.cboLevelBottom.Size = new System.Drawing.Size(153, 23);
            this.cboLevelBottom.TabIndex = 6;
            //
            // txtOffsetBottom
            //
            this.txtOffsetBottom.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOffsetBottom.Location = new System.Drawing.Point(258, 68);
            this.txtOffsetBottom.Name = "txtOffsetBottom";
            this.txtOffsetBottom.Size = new System.Drawing.Size(99, 23);
            this.txtOffsetBottom.TabIndex = 7;
            this.txtOffsetBottom.Text = "0";
            this.txtOffsetBottom.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TxtInch_KeyDown);
            this.txtOffsetBottom.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TxtInch_KeyPress);
            this.txtOffsetBottom.Leave += new System.EventHandler(this.TxtInch_Leave);
            //
            // tableLayoutPanel2
            //
            this.tableLayoutPanel2.ColumnCount = 3;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 96F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 96F));
            this.tableLayoutPanel2.Controls.Add(this.btnOK, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.btnCancel, 2, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 260);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.Padding = new System.Windows.Forms.Padding(0, 4, 6, 4);
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(384, 46);
            this.tableLayoutPanel2.TabIndex = 2;
            //
            // btnOK
            //
            this.btnOK.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnOK.Location = new System.Drawing.Point(191, 7);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(90, 32);
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            //
            // btnCancel
            //
            this.btnCancel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCancel.Location = new System.Drawing.Point(287, 7);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(90, 32);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            //
            // VerticalMEPForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(384, 306);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "VerticalMEPForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Vertical Pipe";
            this.Load += new System.EventHandler(this.VerticalMEPForm_Load);
            this.VisibleChanged += new System.EventHandler(this.UI_PlaceVerticalPipe_VisibleChanged);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.tableLayoutPanel12.ResumeLayout(false);
            this.tableLayoutPanel12.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel4.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel12;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox cboFamilyType;
        private System.Windows.Forms.Label lblServiceType;
        private System.Windows.Forms.ComboBox cboSystemType;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cboDiameter;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.Label lblLevel;
        private System.Windows.Forms.Label lblOffset;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cboLevelTop;
        private System.Windows.Forms.TextBox txtOffsetTop;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cboLevelBottom;
        private System.Windows.Forms.TextBox txtOffsetBottom;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
    }
}

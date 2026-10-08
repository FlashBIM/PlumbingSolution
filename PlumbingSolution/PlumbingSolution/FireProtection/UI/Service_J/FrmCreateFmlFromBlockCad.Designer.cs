using PlumbingSolution.FireProtection.UI.Service_J;

namespace WinFormsDesignHostLink
{
    partial class FrmCreateFmlFromBlockCad
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


        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
            this.btnRun = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tab1 = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel7 = new System.Windows.Forms.TableLayoutPanel();
            this.tbLinkCadName = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.btnExploreCAD = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.cbbFamily = new System.Windows.Forms.ComboBox();
            this.cbbFamilyType = new System.Windows.Forms.ComboBox();
            this.cbbLevel = new System.Windows.Forms.ComboBox();
            this.tbElevation = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.cbbCategoryFamily = new System.Windows.Forms.ComboBox();
            this.grbBlock = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.rbtnPickBlockFromList = new System.Windows.Forms.RadioButton();
            this.rbtnPickBlock = new System.Windows.Forms.RadioButton();
            this.cbbBlockName = new System.Windows.Forms.ComboBox();
            this.btnPickBlock = new System.Windows.Forms.Button();
            this.tbBlockName = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.tab2 = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel8 = new System.Windows.Forms.TableLayoutPanel();
            this.grbStructure = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel9 = new System.Windows.Forms.TableLayoutPanel();
            this.rbtnLinkElement = new System.Windows.Forms.RadioButton();
            this.rbtnProjectElement = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel10 = new System.Windows.Forms.TableLayoutPanel();
            this.rbtnCeilingElevation = new System.Windows.Forms.RadioButton();
            this.rbtnFloorElevation = new System.Windows.Forms.RadioButton();
            this.rbtnManualElevation = new System.Windows.Forms.RadioButton();
            this.rbtnFaceElevation = new System.Windows.Forms.RadioButton();
            this.btnLink = new System.Windows.Forms.Button();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel5.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tab1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.tableLayoutPanel7.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.grbBlock.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            this.tab2.SuspendLayout();
            this.tableLayoutPanel8.SuspendLayout();
            this.grbStructure.SuspendLayout();
            this.tableLayoutPanel9.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tableLayoutPanel10.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel5, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.tabControl1, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(409, 509);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // tableLayoutPanel5
            // 
            this.tableLayoutPanel5.ColumnCount = 3;
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.tableLayoutPanel5.Controls.Add(this.btnRun, 1, 0);
            this.tableLayoutPanel5.Controls.Add(this.btnCancel, 2, 0);
            this.tableLayoutPanel5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel5.Location = new System.Drawing.Point(3, 467);
            this.tableLayoutPanel5.Name = "tableLayoutPanel5";
            this.tableLayoutPanel5.RowCount = 1;
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 39F));
            this.tableLayoutPanel5.Size = new System.Drawing.Size(403, 39);
            this.tableLayoutPanel5.TabIndex = 4;
            // 
            // btnRun
            // 
            this.btnRun.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRun.Location = new System.Drawing.Point(228, 5);
            this.btnRun.Margin = new System.Windows.Forms.Padding(5);
            this.btnRun.Name = "btnRun";
            this.btnRun.Size = new System.Drawing.Size(80, 29);
            this.btnRun.TabIndex = 0;
            this.btnRun.Text = "Thực thi";
            this.btnRun.UseVisualStyleBackColor = true;
            this.btnRun.Click += new System.EventHandler(this.btnRun_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCancel.Location = new System.Drawing.Point(318, 5);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(5);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(80, 29);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tab1);
            this.tabControl1.Controls.Add(this.tab2);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(5, 5);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(5);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(399, 454);
            this.tabControl1.TabIndex = 1;
            // 
            // tab1
            // 
            this.tab1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(187)))), ((int)(((byte)(226)))), ((int)(((byte)(252)))));
            this.tab1.Controls.Add(this.tableLayoutPanel2);
            this.tab1.Location = new System.Drawing.Point(4, 22);
            this.tab1.Name = "tab1";
            this.tab1.Padding = new System.Windows.Forms.Padding(3);
            this.tab1.Size = new System.Drawing.Size(391, 428);
            this.tab1.TabIndex = 0;
            this.tab1.Text = "Thiết lập";
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Controls.Add(this.groupBox5, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.groupBox2, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this.grbBlock, 0, 1);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 4;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(385, 422);
            this.tableLayoutPanel2.TabIndex = 1;
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.tableLayoutPanel7);
            this.groupBox5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox5.Location = new System.Drawing.Point(5, 5);
            this.groupBox5.Margin = new System.Windows.Forms.Padding(5, 5, 8, 5);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(372, 90);
            this.groupBox5.TabIndex = 5;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Chọn Link CAD";
            // 
            // tableLayoutPanel7
            // 
            this.tableLayoutPanel7.ColumnCount = 2;
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel7.Controls.Add(this.tbLinkCadName, 1, 0);
            this.tableLayoutPanel7.Controls.Add(this.label6, 0, 0);
            this.tableLayoutPanel7.Controls.Add(this.btnExploreCAD, 1, 1);
            this.tableLayoutPanel7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel7.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanel7.Name = "tableLayoutPanel7";
            this.tableLayoutPanel7.RowCount = 2;
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel7.Size = new System.Drawing.Size(366, 71);
            this.tableLayoutPanel7.TabIndex = 0;
            // 
            // tbLinkCadName
            // 
            this.tbLinkCadName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbLinkCadName.Location = new System.Drawing.Point(155, 3);
            this.tbLinkCadName.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.tbLinkCadName.Name = "tbLinkCadName";
            this.tbLinkCadName.ReadOnly = true;
            this.tbLinkCadName.Size = new System.Drawing.Size(206, 20);
            this.tbLinkCadName.TabIndex = 4;
            this.tbLinkCadName.TextChanged += new System.EventHandler(this.tbLinkCadName_TextChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(5, 5);
            this.label6.Margin = new System.Windows.Forms.Padding(5);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(74, 13);
            this.label6.TabIndex = 5;
            this.label6.Text = "Tên Link CAD";
            // 
            // btnExploreCAD
            // 
            this.btnExploreCAD.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnExploreCAD.Location = new System.Drawing.Point(155, 35);
            this.btnExploreCAD.Margin = new System.Windows.Forms.Padding(5);
            this.btnExploreCAD.Name = "btnExploreCAD";
            this.btnExploreCAD.Size = new System.Drawing.Size(206, 31);
            this.btnExploreCAD.TabIndex = 6;
            this.btnExploreCAD.Text = "Xử lý link CAD";
            this.btnExploreCAD.UseVisualStyleBackColor = true;
            this.btnExploreCAD.Click += new System.EventHandler(this.btnExploreCAD_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.tableLayoutPanel3);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox2.Location = new System.Drawing.Point(5, 225);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(5, 5, 8, 5);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(372, 190);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Lựa chọn Family";
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 2;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Controls.Add(this.label1, 0, 1);
            this.tableLayoutPanel3.Controls.Add(this.label2, 0, 2);
            this.tableLayoutPanel3.Controls.Add(this.label3, 0, 3);
            this.tableLayoutPanel3.Controls.Add(this.label4, 0, 4);
            this.tableLayoutPanel3.Controls.Add(this.cbbFamily, 1, 1);
            this.tableLayoutPanel3.Controls.Add(this.cbbFamilyType, 1, 2);
            this.tableLayoutPanel3.Controls.Add(this.cbbLevel, 1, 3);
            this.tableLayoutPanel3.Controls.Add(this.tbElevation, 1, 4);
            this.tableLayoutPanel3.Controls.Add(this.label7, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.cbbCategoryFamily, 1, 0);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 5;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(366, 171);
            this.tableLayoutPanel3.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(5, 39);
            this.label1.Margin = new System.Windows.Forms.Padding(5);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(36, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Family";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(5, 73);
            this.label2.Margin = new System.Windows.Forms.Padding(5);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(63, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Family Type";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(5, 107);
            this.label3.Margin = new System.Windows.Forms.Padding(5);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(33, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Level";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(5, 141);
            this.label4.Margin = new System.Windows.Forms.Padding(5);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(74, 13);
            this.label4.TabIndex = 3;
            this.label4.Text = "Cao độ Family";
            // 
            // cbbFamily
            // 
            this.cbbFamily.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cbbFamily.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbbFamily.FormattingEnabled = true;
            this.cbbFamily.Location = new System.Drawing.Point(123, 37);
            this.cbbFamily.Name = "cbbFamily";
            this.cbbFamily.Size = new System.Drawing.Size(240, 21);
            this.cbbFamily.TabIndex = 4;
            this.cbbFamily.DropDown += new System.EventHandler(this.cbo_DropDown);
            this.cbbFamily.SelectedIndexChanged += new System.EventHandler(this.cbbFamily_SelectedIndexChanged);
            this.cbbFamily.DataSourceChanged += new System.EventHandler(this.cbbFamily_DataSourceChanged);
            // 
            // cbbFamilyType
            // 
            this.cbbFamilyType.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cbbFamilyType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbbFamilyType.FormattingEnabled = true;
            this.cbbFamilyType.Location = new System.Drawing.Point(123, 71);
            this.cbbFamilyType.Name = "cbbFamilyType";
            this.cbbFamilyType.Size = new System.Drawing.Size(240, 21);
            this.cbbFamilyType.TabIndex = 5;
            this.cbbFamilyType.DropDown += new System.EventHandler(this.cbo_DropDown);
            // 
            // cbbLevel
            // 
            this.cbbLevel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cbbLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbbLevel.FormattingEnabled = true;
            this.cbbLevel.Location = new System.Drawing.Point(123, 105);
            this.cbbLevel.Name = "cbbLevel";
            this.cbbLevel.Size = new System.Drawing.Size(240, 21);
            this.cbbLevel.TabIndex = 6;
            this.cbbLevel.DropDown += new System.EventHandler(this.cbo_DropDown);
            // 
            // tbElevation
            // 
            this.tbElevation.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbElevation.Location = new System.Drawing.Point(123, 139);
            this.tbElevation.Name = "tbElevation";
            this.tbElevation.Size = new System.Drawing.Size(240, 20);
            this.tbElevation.TabIndex = 7;
            this.tbElevation.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TextBox_KeyDown);
            this.tbElevation.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBox_KeyPress);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(5, 5);
            this.label7.Margin = new System.Windows.Forms.Padding(5);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(88, 13);
            this.label7.TabIndex = 8;
            this.label7.Text = "Danh mục Family";
            // 
            // cbbCategoryFamily
            // 
            this.cbbCategoryFamily.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cbbCategoryFamily.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbbCategoryFamily.FormattingEnabled = true;
            this.cbbCategoryFamily.Location = new System.Drawing.Point(123, 3);
            this.cbbCategoryFamily.Name = "cbbCategoryFamily";
            this.cbbCategoryFamily.Size = new System.Drawing.Size(240, 21);
            this.cbbCategoryFamily.TabIndex = 9;
            this.cbbCategoryFamily.SelectedIndexChanged += new System.EventHandler(this.cbbCategoryFamily_SelectedIndexChanged);
            // 
            // grbBlock
            // 
            this.grbBlock.Controls.Add(this.tableLayoutPanel4);
            this.grbBlock.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grbBlock.Location = new System.Drawing.Point(5, 105);
            this.grbBlock.Margin = new System.Windows.Forms.Padding(5, 5, 8, 5);
            this.grbBlock.Name = "grbBlock";
            this.grbBlock.Size = new System.Drawing.Size(372, 110);
            this.grbBlock.TabIndex = 0;
            this.grbBlock.TabStop = false;
            this.grbBlock.Text = "Chọn Block";
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.ColumnCount = 2;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel4.Controls.Add(this.rbtnPickBlockFromList, 0, 0);
            this.tableLayoutPanel4.Controls.Add(this.rbtnPickBlock, 0, 1);
            this.tableLayoutPanel4.Controls.Add(this.cbbBlockName, 1, 0);
            this.tableLayoutPanel4.Controls.Add(this.btnPickBlock, 1, 1);
            this.tableLayoutPanel4.Controls.Add(this.tbBlockName, 1, 2);
            this.tableLayoutPanel4.Controls.Add(this.label5, 0, 2);
            this.tableLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel4.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 3;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel4.Size = new System.Drawing.Size(366, 91);
            this.tableLayoutPanel4.TabIndex = 0;
            // 
            // rbtnPickBlockFromList
            // 
            this.rbtnPickBlockFromList.AutoSize = true;
            this.rbtnPickBlockFromList.Location = new System.Drawing.Point(5, 5);
            this.rbtnPickBlockFromList.Margin = new System.Windows.Forms.Padding(5);
            this.rbtnPickBlockFromList.Name = "rbtnPickBlockFromList";
            this.rbtnPickBlockFromList.Size = new System.Drawing.Size(115, 17);
            this.rbtnPickBlockFromList.TabIndex = 0;
            this.rbtnPickBlockFromList.Text = "Chọn từ danh sách";
            this.rbtnPickBlockFromList.UseVisualStyleBackColor = true;
            // 
            // rbtnPickBlock
            // 
            this.rbtnPickBlock.AutoSize = true;
            this.rbtnPickBlock.Location = new System.Drawing.Point(5, 35);
            this.rbtnPickBlock.Margin = new System.Windows.Forms.Padding(5);
            this.rbtnPickBlock.Name = "rbtnPickBlock";
            this.rbtnPickBlock.Size = new System.Drawing.Size(102, 17);
            this.rbtnPickBlock.TabIndex = 1;
            this.rbtnPickBlock.TabStop = true;
            this.rbtnPickBlock.Text = "Pick chọn block";
            this.rbtnPickBlock.UseVisualStyleBackColor = true;
            // 
            // cbbBlockName
            // 
            this.cbbBlockName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cbbBlockName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbbBlockName.FormattingEnabled = true;
            this.cbbBlockName.Location = new System.Drawing.Point(153, 3);
            this.cbbBlockName.Name = "cbbBlockName";
            this.cbbBlockName.Size = new System.Drawing.Size(210, 21);
            this.cbbBlockName.TabIndex = 2;
            // 
            // btnPickBlock
            // 
            this.btnPickBlock.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPickBlock.Location = new System.Drawing.Point(153, 33);
            this.btnPickBlock.Name = "btnPickBlock";
            this.btnPickBlock.Size = new System.Drawing.Size(210, 24);
            this.btnPickBlock.TabIndex = 3;
            this.btnPickBlock.Text = "Pick";
            this.btnPickBlock.UseVisualStyleBackColor = true;
            this.btnPickBlock.Click += new System.EventHandler(this.btnPickBlock_Click);
            // 
            // tbBlockName
            // 
            this.tbBlockName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbBlockName.Location = new System.Drawing.Point(155, 63);
            this.tbBlockName.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.tbBlockName.Name = "tbBlockName";
            this.tbBlockName.ReadOnly = true;
            this.tbBlockName.Size = new System.Drawing.Size(206, 20);
            this.tbBlockName.TabIndex = 4;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(5, 65);
            this.label5.Margin = new System.Windows.Forms.Padding(5);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(55, 13);
            this.label5.TabIndex = 5;
            this.label5.Text = "Tên block";
            // 
            // tab2
            // 
            this.tab2.Controls.Add(this.tableLayoutPanel8);
            this.tab2.Location = new System.Drawing.Point(4, 22);
            this.tab2.Name = "tab2";
            this.tab2.Padding = new System.Windows.Forms.Padding(3);
            this.tab2.Size = new System.Drawing.Size(391, 428);
            this.tab2.TabIndex = 1;
            this.tab2.Text = "Cài đặt";
            this.tab2.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel8
            // 
            this.tableLayoutPanel8.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(187)))), ((int)(((byte)(226)))), ((int)(((byte)(252)))));
            this.tableLayoutPanel8.ColumnCount = 1;
            this.tableLayoutPanel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel8.Controls.Add(this.grbStructure, 0, 0);
            this.tableLayoutPanel8.Controls.Add(this.groupBox1, 0, 0);
            this.tableLayoutPanel8.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel8.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel8.Name = "tableLayoutPanel8";
            this.tableLayoutPanel8.RowCount = 3;
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel8.Size = new System.Drawing.Size(385, 422);
            this.tableLayoutPanel8.TabIndex = 0;
            // 
            // grbStructure
            // 
            this.grbStructure.Controls.Add(this.tableLayoutPanel9);
            this.grbStructure.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grbStructure.Location = new System.Drawing.Point(5, 95);
            this.grbStructure.Margin = new System.Windows.Forms.Padding(5, 5, 8, 5);
            this.grbStructure.Name = "grbStructure";
            this.grbStructure.Size = new System.Drawing.Size(372, 80);
            this.grbStructure.TabIndex = 5;
            this.grbStructure.TabStop = false;
            this.grbStructure.Text = "Đối tượng xây dựng";
            // 
            // tableLayoutPanel9
            // 
            this.tableLayoutPanel9.ColumnCount = 2;
            this.tableLayoutPanel9.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel9.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel9.Controls.Add(this.rbtnLinkElement, 0, 1);
            this.tableLayoutPanel9.Controls.Add(this.rbtnProjectElement, 0, 0);
            this.tableLayoutPanel9.Controls.Add(this.btnLink, 1, 1);
            this.tableLayoutPanel9.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel9.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanel9.Name = "tableLayoutPanel9";
            this.tableLayoutPanel9.RowCount = 1;
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel9.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel9.Size = new System.Drawing.Size(366, 61);
            this.tableLayoutPanel9.TabIndex = 0;
            // 
            // rbtnLinkElement
            // 
            this.rbtnLinkElement.AutoSize = true;
            this.rbtnLinkElement.Location = new System.Drawing.Point(5, 35);
            this.rbtnLinkElement.Margin = new System.Windows.Forms.Padding(5);
            this.rbtnLinkElement.Name = "rbtnLinkElement";
            this.rbtnLinkElement.Size = new System.Drawing.Size(73, 17);
            this.rbtnLinkElement.TabIndex = 4;
            this.rbtnLinkElement.TabStop = true;
            this.rbtnLinkElement.Text = "Revit Link";
            this.rbtnLinkElement.UseVisualStyleBackColor = true;
            this.rbtnLinkElement.CheckedChanged += new System.EventHandler(this.rbtnLinkElement_CheckedChanged);
            // 
            // rbtnProjectElement
            // 
            this.rbtnProjectElement.AutoSize = true;
            this.rbtnProjectElement.Location = new System.Drawing.Point(5, 5);
            this.rbtnProjectElement.Margin = new System.Windows.Forms.Padding(5);
            this.rbtnProjectElement.Name = "rbtnProjectElement";
            this.rbtnProjectElement.Size = new System.Drawing.Size(107, 17);
            this.rbtnProjectElement.TabIndex = 1;
            this.rbtnProjectElement.Text = "Đối tượng Project";
            this.rbtnProjectElement.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.tableLayoutPanel10);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(5, 5);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(5, 5, 8, 5);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(372, 80);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Thiết lập cao độ";
            // 
            // tableLayoutPanel10
            // 
            this.tableLayoutPanel10.ColumnCount = 2;
            this.tableLayoutPanel10.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel10.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel10.Controls.Add(this.rbtnCeilingElevation, 1, 0);
            this.tableLayoutPanel10.Controls.Add(this.rbtnFloorElevation, 0, 1);
            this.tableLayoutPanel10.Controls.Add(this.rbtnManualElevation, 0, 0);
            this.tableLayoutPanel10.Controls.Add(this.rbtnFaceElevation, 1, 1);
            this.tableLayoutPanel10.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel10.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanel10.Name = "tableLayoutPanel10";
            this.tableLayoutPanel10.RowCount = 2;
            this.tableLayoutPanel10.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel10.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel10.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel10.Size = new System.Drawing.Size(366, 61);
            this.tableLayoutPanel10.TabIndex = 0;
            // 
            // rbtnCeilingElevation
            // 
            this.rbtnCeilingElevation.AutoSize = true;
            this.rbtnCeilingElevation.Location = new System.Drawing.Point(188, 5);
            this.rbtnCeilingElevation.Margin = new System.Windows.Forms.Padding(5);
            this.rbtnCeilingElevation.Name = "rbtnCeilingElevation";
            this.rbtnCeilingElevation.Size = new System.Drawing.Size(108, 17);
            this.rbtnCeilingElevation.TabIndex = 5;
            this.rbtnCeilingElevation.TabStop = true;
            this.rbtnCeilingElevation.Text = "Theo cao độ trần";
            this.rbtnCeilingElevation.UseVisualStyleBackColor = true;
            this.rbtnCeilingElevation.CheckedChanged += new System.EventHandler(this.rbtnCeilingElevation_CheckedChanged);
            // 
            // rbtnFloorElevation
            // 
            this.rbtnFloorElevation.AutoSize = true;
            this.rbtnFloorElevation.Location = new System.Drawing.Point(5, 35);
            this.rbtnFloorElevation.Margin = new System.Windows.Forms.Padding(5);
            this.rbtnFloorElevation.Name = "rbtnFloorElevation";
            this.rbtnFloorElevation.Size = new System.Drawing.Size(128, 17);
            this.rbtnFloorElevation.TabIndex = 4;
            this.rbtnFloorElevation.TabStop = true;
            this.rbtnFloorElevation.Text = "Theo cao độ đáy sàn";
            this.rbtnFloorElevation.UseVisualStyleBackColor = true;
            this.rbtnFloorElevation.CheckedChanged += new System.EventHandler(this.rbtnFloorElevation_CheckedChanged);
            // 
            // rbtnManualElevation
            // 
            this.rbtnManualElevation.AutoSize = true;
            this.rbtnManualElevation.Location = new System.Drawing.Point(5, 5);
            this.rbtnManualElevation.Margin = new System.Windows.Forms.Padding(5);
            this.rbtnManualElevation.Name = "rbtnManualElevation";
            this.rbtnManualElevation.Size = new System.Drawing.Size(87, 17);
            this.rbtnManualElevation.TabIndex = 1;
            this.rbtnManualElevation.Text = "Cao độ tự do";
            this.rbtnManualElevation.UseVisualStyleBackColor = true;
            this.rbtnManualElevation.CheckedChanged += new System.EventHandler(this.rbtnManualElevation_CheckedChanged);
            // 
            // rbtnFaceElevation
            // 
            this.rbtnFaceElevation.AutoSize = true;
            this.rbtnFaceElevation.Location = new System.Drawing.Point(188, 35);
            this.rbtnFaceElevation.Margin = new System.Windows.Forms.Padding(5);
            this.rbtnFaceElevation.Name = "rbtnFaceElevation";
            this.rbtnFaceElevation.Size = new System.Drawing.Size(125, 17);
            this.rbtnFaceElevation.TabIndex = 3;
            this.rbtnFaceElevation.TabStop = true;
            this.rbtnFaceElevation.Text = "Family tựa mặt phẳng";
            this.rbtnFaceElevation.UseVisualStyleBackColor = true;
            this.rbtnFaceElevation.CheckedChanged += new System.EventHandler(this.rbtnFaceElevation_CheckedChanged);
            // 
            // btnLink
            // 
            this.btnLink.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLink.Location = new System.Drawing.Point(188, 35);
            this.btnLink.Margin = new System.Windows.Forms.Padding(5);
            this.btnLink.Name = "btnLink";
            this.btnLink.Size = new System.Drawing.Size(173, 21);
            this.btnLink.TabIndex = 5;
            this.btnLink.Text = "Đối tượng Link";
            this.btnLink.UseVisualStyleBackColor = true;
            this.btnLink.Click += new System.EventHandler(this.btnLink_Click);
            // 
            // FrmCreateFmlFromBlockCad
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(187)))), ((int)(((byte)(226)))), ((int)(((byte)(252)))));
            this.ClientSize = new System.Drawing.Size(409, 509);
            this.Controls.Add(this.tableLayoutPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "FrmCreateFmlFromBlockCad";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Family tự động";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel5.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.tab1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.tableLayoutPanel7.ResumeLayout(false);
            this.tableLayoutPanel7.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            this.grbBlock.ResumeLayout(false);
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel4.PerformLayout();
            this.tab2.ResumeLayout(false);
            this.tableLayoutPanel8.ResumeLayout(false);
            this.grbStructure.ResumeLayout(false);
            this.tableLayoutPanel9.ResumeLayout(false);
            this.tableLayoutPanel9.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.tableLayoutPanel10.ResumeLayout(false);
            this.tableLayoutPanel10.PerformLayout();
            this.ResumeLayout(false);

        }


        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel5;
        private System.Windows.Forms.Button btnRun;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tab1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel7;
        private System.Windows.Forms.TextBox tbLinkCadName;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button btnExploreCAD;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cbbFamily;
        private System.Windows.Forms.ComboBox cbbFamilyType;
        private System.Windows.Forms.ComboBox cbbLevel;
        private System.Windows.Forms.TextBox tbElevation;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox cbbCategoryFamily;
        private System.Windows.Forms.GroupBox grbBlock;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.RadioButton rbtnPickBlockFromList;
        private System.Windows.Forms.RadioButton rbtnPickBlock;
        private System.Windows.Forms.ComboBox cbbBlockName;
        private System.Windows.Forms.Button btnPickBlock;
        private System.Windows.Forms.TextBox tbBlockName;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TabPage tab2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel8;
        private System.Windows.Forms.GroupBox grbStructure;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel9;
        private System.Windows.Forms.RadioButton rbtnLinkElement;
        private System.Windows.Forms.RadioButton rbtnProjectElement;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel10;
        private System.Windows.Forms.RadioButton rbtnCeilingElevation;
        private System.Windows.Forms.RadioButton rbtnFloorElevation;
        private System.Windows.Forms.RadioButton rbtnManualElevation;
        private System.Windows.Forms.RadioButton rbtnFaceElevation;
        private System.Windows.Forms.Button btnLink;
    }
}

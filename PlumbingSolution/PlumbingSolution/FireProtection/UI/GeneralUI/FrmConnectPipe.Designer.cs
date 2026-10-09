namespace PlumbingSolution.FireProtection.UI.GeneralUI
{
    partial class FrmConnectPipe
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.grbRoute = new System.Windows.Forms.GroupBox();
            this.flpRoute = new System.Windows.Forms.FlowLayoutPanel();
            this.rdnBranch = new System.Windows.Forms.RadioButton();
            this.rdnElbow = new System.Windows.Forms.RadioButton();
            this.rdnStraight = new System.Windows.Forms.RadioButton();
            this.rdnParallel = new System.Windows.Forms.RadioButton();
            this.grbConnection = new System.Windows.Forms.GroupBox();
            this.flpConnection = new System.Windows.Forms.FlowLayoutPanel();
            this.rdnElbow45 = new System.Windows.Forms.RadioButton();
            this.rdnElbow90 = new System.Windows.Forms.RadioButton();
            this.rdnSameElevation = new System.Windows.Forms.RadioButton();
            this.grbDirection = new System.Windows.Forms.GroupBox();
            this.flpDirection = new System.Windows.Forms.FlowLayoutPanel();
            this.rdnHorizontal = new System.Windows.Forms.RadioButton();
            this.rdnVertical = new System.Windows.Forms.RadioButton();
            this.rdnAuto = new System.Windows.Forms.RadioButton();
            this.pnlPreview = new System.Windows.Forms.Panel();
            this.picPreview = new System.Windows.Forms.PictureBox();
            this.lblNoPreview = new System.Windows.Forms.Label();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnPreview = new System.Windows.Forms.Button();
            this.tableLayoutPanel1.SuspendLayout();
            this.grbRoute.SuspendLayout();
            this.flpRoute.SuspendLayout();
            this.grbConnection.SuspendLayout();
            this.flpConnection.SuspendLayout();
            this.grbDirection.SuspendLayout();
            this.flpDirection.SuspendLayout();
            this.pnlPreview.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picPreview)).BeginInit();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            //
            // tableLayoutPanel1
            //
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.grbRoute, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.grbConnection, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.grbDirection, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.pnlPreview, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 3);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 4;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 138F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 62F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 0F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(384, 246);
            this.tableLayoutPanel1.TabIndex = 0;
            //
            // grbRoute
            //
            this.grbRoute.Controls.Add(this.flpRoute);
            this.grbRoute.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grbRoute.Location = new System.Drawing.Point(8, 8);
            this.grbRoute.Margin = new System.Windows.Forms.Padding(8, 8, 4, 4);
            this.grbRoute.Name = "grbRoute";
            this.grbRoute.Size = new System.Drawing.Size(180, 126);
            this.grbRoute.TabIndex = 0;
            this.grbRoute.TabStop = false;
            this.grbRoute.Text = "Route Type";
            //
            // flpRoute
            //
            this.flpRoute.Controls.Add(this.rdnBranch);
            this.flpRoute.Controls.Add(this.rdnElbow);
            this.flpRoute.Controls.Add(this.rdnStraight);
            this.flpRoute.Controls.Add(this.rdnParallel);
            this.flpRoute.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpRoute.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpRoute.Location = new System.Drawing.Point(3, 19);
            this.flpRoute.Name = "flpRoute";
            this.flpRoute.Padding = new System.Windows.Forms.Padding(4, 2, 0, 0);
            this.flpRoute.Size = new System.Drawing.Size(174, 104);
            this.flpRoute.TabIndex = 0;
            this.flpRoute.WrapContents = false;
            //
            // rdnBranch
            //
            this.rdnBranch.AutoSize = true;
            this.rdnBranch.Checked = true;
            this.rdnBranch.Location = new System.Drawing.Point(7, 5);
            this.rdnBranch.Name = "rdnBranch";
            this.rdnBranch.Size = new System.Drawing.Size(62, 19);
            this.rdnBranch.TabIndex = 0;
            this.rdnBranch.TabStop = true;
            this.rdnBranch.Text = "Branch";
            this.rdnBranch.UseVisualStyleBackColor = true;
            this.rdnBranch.CheckedChanged += new System.EventHandler(this.Option_CheckedChanged);
            //
            // rdnElbow
            //
            this.rdnElbow.AutoSize = true;
            this.rdnElbow.Location = new System.Drawing.Point(7, 30);
            this.rdnElbow.Name = "rdnElbow";
            this.rdnElbow.Size = new System.Drawing.Size(57, 19);
            this.rdnElbow.TabIndex = 1;
            this.rdnElbow.Text = "Elbow";
            this.rdnElbow.UseVisualStyleBackColor = true;
            this.rdnElbow.CheckedChanged += new System.EventHandler(this.Option_CheckedChanged);
            //
            // rdnStraight
            //
            this.rdnStraight.AutoSize = true;
            this.rdnStraight.Location = new System.Drawing.Point(7, 55);
            this.rdnStraight.Name = "rdnStraight";
            this.rdnStraight.Size = new System.Drawing.Size(66, 19);
            this.rdnStraight.TabIndex = 2;
            this.rdnStraight.Text = "Straight";
            this.rdnStraight.UseVisualStyleBackColor = true;
            this.rdnStraight.CheckedChanged += new System.EventHandler(this.Option_CheckedChanged);
            //
            // rdnParallel
            //
            this.rdnParallel.AutoSize = true;
            this.rdnParallel.Location = new System.Drawing.Point(7, 80);
            this.rdnParallel.Name = "rdnParallel";
            this.rdnParallel.Size = new System.Drawing.Size(64, 19);
            this.rdnParallel.TabIndex = 3;
            this.rdnParallel.Text = "Parallel";
            this.rdnParallel.UseVisualStyleBackColor = true;
            this.rdnParallel.CheckedChanged += new System.EventHandler(this.Option_CheckedChanged);
            //
            // grbConnection
            //
            this.grbConnection.Controls.Add(this.flpConnection);
            this.grbConnection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grbConnection.Location = new System.Drawing.Point(196, 8);
            this.grbConnection.Margin = new System.Windows.Forms.Padding(4, 8, 8, 4);
            this.grbConnection.Name = "grbConnection";
            this.grbConnection.Size = new System.Drawing.Size(180, 126);
            this.grbConnection.TabIndex = 1;
            this.grbConnection.TabStop = false;
            this.grbConnection.Text = "Connection Type";
            //
            // flpConnection
            //
            this.flpConnection.Controls.Add(this.rdnElbow45);
            this.flpConnection.Controls.Add(this.rdnElbow90);
            this.flpConnection.Controls.Add(this.rdnSameElevation);
            this.flpConnection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpConnection.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpConnection.Location = new System.Drawing.Point(3, 19);
            this.flpConnection.Name = "flpConnection";
            this.flpConnection.Padding = new System.Windows.Forms.Padding(4, 2, 0, 0);
            this.flpConnection.Size = new System.Drawing.Size(174, 104);
            this.flpConnection.TabIndex = 0;
            this.flpConnection.WrapContents = false;
            //
            // rdnElbow45
            //
            this.rdnElbow45.AutoSize = true;
            this.rdnElbow45.Checked = true;
            this.rdnElbow45.Location = new System.Drawing.Point(7, 5);
            this.rdnElbow45.Name = "rdnElbow45";
            this.rdnElbow45.Size = new System.Drawing.Size(71, 19);
            this.rdnElbow45.TabIndex = 0;
            this.rdnElbow45.TabStop = true;
            this.rdnElbow45.Text = "Elbow 45";
            this.rdnElbow45.UseVisualStyleBackColor = true;
            this.rdnElbow45.CheckedChanged += new System.EventHandler(this.Option_CheckedChanged);
            //
            // rdnElbow90
            //
            this.rdnElbow90.AutoSize = true;
            this.rdnElbow90.Location = new System.Drawing.Point(7, 30);
            this.rdnElbow90.Name = "rdnElbow90";
            this.rdnElbow90.Size = new System.Drawing.Size(71, 19);
            this.rdnElbow90.TabIndex = 1;
            this.rdnElbow90.Text = "Elbow 90";
            this.rdnElbow90.UseVisualStyleBackColor = true;
            this.rdnElbow90.CheckedChanged += new System.EventHandler(this.Option_CheckedChanged);
            //
            // rdnSameElevation
            //
            this.rdnSameElevation.AutoSize = true;
            this.rdnSameElevation.Location = new System.Drawing.Point(7, 55);
            this.rdnSameElevation.Name = "rdnSameElevation";
            this.rdnSameElevation.Size = new System.Drawing.Size(105, 19);
            this.rdnSameElevation.TabIndex = 2;
            this.rdnSameElevation.Text = "Same Elevation";
            this.rdnSameElevation.UseVisualStyleBackColor = true;
            this.rdnSameElevation.CheckedChanged += new System.EventHandler(this.Option_CheckedChanged);
            //
            // grbDirection
            //
            this.tableLayoutPanel1.SetColumnSpan(this.grbDirection, 2);
            this.grbDirection.Controls.Add(this.flpDirection);
            this.grbDirection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grbDirection.Location = new System.Drawing.Point(8, 142);
            this.grbDirection.Margin = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.grbDirection.Name = "grbDirection";
            this.grbDirection.Size = new System.Drawing.Size(368, 54);
            this.grbDirection.TabIndex = 2;
            this.grbDirection.TabStop = false;
            this.grbDirection.Text = "Direction";
            //
            // flpDirection
            //
            this.flpDirection.Controls.Add(this.rdnHorizontal);
            this.flpDirection.Controls.Add(this.rdnVertical);
            this.flpDirection.Controls.Add(this.rdnAuto);
            this.flpDirection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpDirection.Location = new System.Drawing.Point(3, 19);
            this.flpDirection.Name = "flpDirection";
            this.flpDirection.Padding = new System.Windows.Forms.Padding(4, 2, 0, 0);
            this.flpDirection.Size = new System.Drawing.Size(362, 32);
            this.flpDirection.TabIndex = 0;
            this.flpDirection.WrapContents = false;
            //
            // rdnHorizontal
            //
            this.rdnHorizontal.AutoSize = true;
            this.rdnHorizontal.Checked = true;
            this.rdnHorizontal.Location = new System.Drawing.Point(7, 5);
            this.rdnHorizontal.Margin = new System.Windows.Forms.Padding(3, 3, 24, 3);
            this.rdnHorizontal.Name = "rdnHorizontal";
            this.rdnHorizontal.Size = new System.Drawing.Size(80, 19);
            this.rdnHorizontal.TabIndex = 0;
            this.rdnHorizontal.TabStop = true;
            this.rdnHorizontal.Text = "Horizontal";
            this.rdnHorizontal.UseVisualStyleBackColor = true;
            this.rdnHorizontal.CheckedChanged += new System.EventHandler(this.Option_CheckedChanged);
            //
            // rdnVertical
            //
            this.rdnVertical.AutoSize = true;
            this.rdnVertical.Location = new System.Drawing.Point(114, 5);
            this.rdnVertical.Margin = new System.Windows.Forms.Padding(3, 3, 24, 3);
            this.rdnVertical.Name = "rdnVertical";
            this.rdnVertical.Size = new System.Drawing.Size(63, 19);
            this.rdnVertical.TabIndex = 1;
            this.rdnVertical.Text = "Vertical";
            this.rdnVertical.UseVisualStyleBackColor = true;
            this.rdnVertical.CheckedChanged += new System.EventHandler(this.Option_CheckedChanged);
            //
            // rdnAuto
            //
            this.rdnAuto.AutoSize = true;
            this.rdnAuto.Location = new System.Drawing.Point(204, 5);
            this.rdnAuto.Name = "rdnAuto";
            this.rdnAuto.Size = new System.Drawing.Size(51, 19);
            this.rdnAuto.TabIndex = 2;
            this.rdnAuto.Text = "Auto";
            this.rdnAuto.UseVisualStyleBackColor = true;
            this.rdnAuto.CheckedChanged += new System.EventHandler(this.Option_CheckedChanged);
            //
            // pnlPreview
            //
            this.tableLayoutPanel1.SetColumnSpan(this.pnlPreview, 2);
            this.pnlPreview.Controls.Add(this.picPreview);
            this.pnlPreview.Controls.Add(this.lblNoPreview);
            this.pnlPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlPreview.Location = new System.Drawing.Point(8, 200);
            this.pnlPreview.Margin = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.pnlPreview.Name = "pnlPreview";
            this.pnlPreview.Size = new System.Drawing.Size(368, 0);
            this.pnlPreview.TabIndex = 3;
            this.pnlPreview.Visible = false;
            //
            // picPreview
            //
            this.picPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picPreview.Location = new System.Drawing.Point(0, 0);
            this.picPreview.Name = "picPreview";
            this.picPreview.Size = new System.Drawing.Size(368, 0);
            this.picPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picPreview.TabIndex = 0;
            this.picPreview.TabStop = false;
            //
            // lblNoPreview
            //
            this.lblNoPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNoPreview.Location = new System.Drawing.Point(0, 0);
            this.lblNoPreview.Name = "lblNoPreview";
            this.lblNoPreview.Size = new System.Drawing.Size(368, 0);
            this.lblNoPreview.TabIndex = 1;
            this.lblNoPreview.Text = "No preview image";
            this.lblNoPreview.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblNoPreview.Visible = false;
            //
            // tableLayoutPanel2
            //
            this.tableLayoutPanel1.SetColumnSpan(this.tableLayoutPanel2, 2);
            this.tableLayoutPanel2.ColumnCount = 4;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 94F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 94F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 94F));
            this.tableLayoutPanel2.Controls.Add(this.btnOK, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.btnCancel, 2, 0);
            this.tableLayoutPanel2.Controls.Add(this.btnPreview, 3, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 200);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.Padding = new System.Windows.Forms.Padding(0, 4, 6, 4);
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(384, 46);
            this.tableLayoutPanel2.TabIndex = 4;
            //
            // btnOK
            //
            this.btnOK.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnOK.Location = new System.Drawing.Point(99, 7);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(88, 32);
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            //
            // btnCancel
            //
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCancel.Location = new System.Drawing.Point(193, 7);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(88, 32);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            //
            // btnPreview
            //
            this.btnPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPreview.Location = new System.Drawing.Point(287, 7);
            this.btnPreview.Name = "btnPreview";
            this.btnPreview.Size = new System.Drawing.Size(88, 32);
            this.btnPreview.TabIndex = 2;
            this.btnPreview.Text = "Preview";
            this.btnPreview.UseVisualStyleBackColor = true;
            this.btnPreview.Click += new System.EventHandler(this.btnPreview_Click);
            //
            // FrmConnectPipe
            //
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(384, 246);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmConnectPipe";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Connect Pipe";
            this.Load += new System.EventHandler(this.FrmConnectPipe_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.grbRoute.ResumeLayout(false);
            this.flpRoute.ResumeLayout(false);
            this.flpRoute.PerformLayout();
            this.grbConnection.ResumeLayout(false);
            this.flpConnection.ResumeLayout(false);
            this.flpConnection.PerformLayout();
            this.grbDirection.ResumeLayout(false);
            this.flpDirection.ResumeLayout(false);
            this.flpDirection.PerformLayout();
            this.pnlPreview.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picPreview)).EndInit();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.GroupBox grbRoute;
        private System.Windows.Forms.FlowLayoutPanel flpRoute;
        private System.Windows.Forms.RadioButton rdnBranch;
        private System.Windows.Forms.RadioButton rdnElbow;
        private System.Windows.Forms.RadioButton rdnStraight;
        private System.Windows.Forms.RadioButton rdnParallel;
        private System.Windows.Forms.GroupBox grbConnection;
        private System.Windows.Forms.FlowLayoutPanel flpConnection;
        private System.Windows.Forms.RadioButton rdnElbow45;
        private System.Windows.Forms.RadioButton rdnElbow90;
        private System.Windows.Forms.RadioButton rdnSameElevation;
        private System.Windows.Forms.GroupBox grbDirection;
        private System.Windows.Forms.FlowLayoutPanel flpDirection;
        private System.Windows.Forms.RadioButton rdnHorizontal;
        private System.Windows.Forms.RadioButton rdnVertical;
        private System.Windows.Forms.RadioButton rdnAuto;
        private System.Windows.Forms.Panel pnlPreview;
        private System.Windows.Forms.PictureBox picPreview;
        private System.Windows.Forms.Label lblNoPreview;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnPreview;
    }
}

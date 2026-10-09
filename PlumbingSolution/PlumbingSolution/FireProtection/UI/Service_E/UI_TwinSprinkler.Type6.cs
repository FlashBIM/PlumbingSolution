using Autodesk.Revit.DB;
using PlumbingSolution.FireProtection.Command.Modify;
using PlumbingSolution.FireProtection.Ultis;
using PlumbingSolution.FireProtection.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Control = System.Windows.Forms.Control;

namespace PlumbingSolution.FireProtection.UI.Service_E
{
    /// <summary>
    /// Twin Sprinkler Type 6 (sheet Fire Protection): khung thông số riêng hiện bên phải form khi chọn Type 6
    /// (form nới rộng ra), ẩn lại khi chọn type khác. Dựng bằng code để không đụng bố cục Designer của Type 1-5.
    /// </summary>
    public partial class UI_TwinSprinkler
    {
        private const int Type6PanelWidth = 330;

        private TableLayoutPanel pnlType6;
        private TextBox tbT6L1, tbT6L2, tbT6L3, tbT6Elevation;
        private RadioButton rbT6Tee, rbT6TeeE90, rbT6TeeE45, rbT6ByMep, rbT6Auto;
        private ComboBox cbT6Family, cbT6Type;
        private CheckBox chkT6Direct;
        private bool m_type6Shown;

        public bool IsType6 => rbC5Type6.Checked;
        public double L1_6 => Mm(tbT6L1);
        public double L2_6 => Mm(tbT6L2);
        public double L3_6 => Mm(tbT6L3);
        public double A_6 => Mm(tbC4L);
        public double UprightElevation6 => Mm(tbT6Elevation);
        public bool IsAuto6 => rbT6Auto.Checked;
        public bool ConnectTeeDirectly6 => chkT6Direct.Checked;
        public bool ElbowAtFreeEnd6 => chkC5TeeTap.Checked;
        public FamilySymbol UprightSymbol6 => cbT6Type.SelectedItem as FamilySymbol;

        public TwinMainFitting MainFitting6 =>
            rbT6TeeE90.Checked ? TwinMainFitting.TeeElbow90 : rbT6TeeE45.Checked ? TwinMainFitting.TeeElbow45 : TwinMainFitting.Tee;

        private static double Mm(TextBox tb)
        {
            return double.TryParse(tb.Text.Trim(), out double v) ? v : double.MinValue;
        }

        /// <summary>Gọi trong constructor, trước Common.SettingTemplate để khung mới cũng nhận giao diện chung.</summary>
        private void InitType6()
        {
            BuildType6Panel();
            LoadType6();
        }

        // Chỉ dựng control (không gọi Revit API) - Tools/FormSnapshot cũng gọi hàm này để chụp khung Type 6.
        private void BuildType6Panel()
        {
            pnlType6 = new TableLayoutPanel { Name = "pnlType6", Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Visible = false, Margin = new Padding(0, 0, 5, 0) };
            pnlType6.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlType6.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
            pnlType6.RowStyles.Add(new RowStyle(SizeType.Absolute, 98F));
            pnlType6.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
            pnlType6.RowStyles.Add(new RowStyle(SizeType.Absolute, 140F));
            pnlType6.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Parameters: L1 L2 L3 trên một hàng.
            var tlpParams = Grid(6, 1);
            tlpParams.ColumnStyles.Clear();
            for (int i = 0; i < 3; i++)
            {
                tlpParams.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 26F));
                tlpParams.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3F));
            }
            tbT6L1 = NumberBox("tbT6L1", "300");
            tbT6L2 = NumberBox("tbT6L2", "100");
            tbT6L3 = NumberBox("tbT6L3", "50");
            tlpParams.Controls.Add(Caption("L1"), 0, 0);
            tlpParams.Controls.Add(tbT6L1, 1, 0);
            tlpParams.Controls.Add(Caption("L2"), 2, 0);
            tlpParams.Controls.Add(tbT6L2, 3, 0);
            tlpParams.Controls.Add(Caption("L3"), 4, 0);
            tlpParams.Controls.Add(tbT6L3, 5, 0);
            pnlType6.Controls.Add(Group("Type 6 Parameters", tlpParams), 0, 0);

            // Main Pipe Fittings.
            var tlpFit = Grid(1, 3);
            rbT6Tee = Radio("rbT6Tee", "1 Tee/Tap", true);
            rbT6TeeE90 = Radio("rbT6TeeE90", "1 Tee/Tap & Elbow 90", false);
            rbT6TeeE45 = Radio("rbT6TeeE45", "1 Tee/Tap & Elbow 45", false);
            tlpFit.Controls.Add(rbT6Tee, 0, 0);
            tlpFit.Controls.Add(rbT6TeeE90, 0, 1);
            tlpFit.Controls.Add(rbT6TeeE45, 0, 2);
            foreach (var rb in new[] { rbT6Tee, rbT6TeeE90, rbT6TeeE45 })
                rb.CheckedChanged += (s, e) => RefreshType6State();
            pnlType6.Controls.Add(Group("Main Pipe Fittings", tlpFit), 0, 1);

            // Method: như Pendent Type 5/6 (không có By Distance).
            var tlpMethod = Grid(2, 1);
            rbT6ByMep = Radio("rbT6ByMep", "By Pick MEP Elements", true);
            rbT6Auto = Radio("rbT6Auto", "Auto", false);
            tlpMethod.Controls.Add(rbT6ByMep, 0, 0);
            tlpMethod.Controls.Add(rbT6Auto, 1, 0);
            pnlType6.Controls.Add(Group("Method", tlpMethod), 0, 2);

            // Upright Sprinkler: family / type do tool đặt, Elevation tính từ tim ống ngang tạo ra.
            var tlpUp = Grid(2, 4);
            tlpUp.ColumnStyles.Clear();
            tlpUp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            tlpUp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            cbT6Family = new ComboBox { Name = "cbT6Family", Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(3) };
            cbT6Type = new ComboBox { Name = "cbT6Type", Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(3), DisplayMember = "Name" };
            tbT6Elevation = NumberBox("tbT6Elevation", "100");
            chkT6Direct = new CheckBox { Name = "chkT6Direct", Text = "Sprinkler connect Tee directly", AutoSize = true, Margin = new Padding(3, 5, 3, 3) };
            chkT6Direct.CheckedChanged += (s, e) => RefreshType6State();
            cbT6Family.SelectedIndexChanged += (s, e) => FillUprightTypes();
            tlpUp.Controls.Add(Caption("Family"), 0, 0);
            tlpUp.Controls.Add(cbT6Family, 1, 0);
            tlpUp.Controls.Add(Caption("Type"), 0, 1);
            tlpUp.Controls.Add(cbT6Type, 1, 1);
            tlpUp.Controls.Add(Caption("Elevation"), 0, 2);
            tlpUp.Controls.Add(tbT6Elevation, 1, 2);
            tlpUp.Controls.Add(chkT6Direct, 0, 3);
            tlpUp.SetColumnSpan(chkT6Direct, 2);
            pnlType6.Controls.Add(Group("Upright Sprinkler", tlpUp), 0, 3);

            tableLayoutPanel1.ColumnCount = 3;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0F));
            tableLayoutPanel1.Controls.Add(pnlType6, 2, 0);
            tableLayoutPanel1.SetRowSpan(pnlType6, 2);
        }

        private void LoadType6()
        {
            FillUprightFamilies();
            AppUtils.ff(tbT6L1);
            AppUtils.ff(tbT6L2);
            AppUtils.ff(tbT6L3);
            AppUtils.ff(tbT6Elevation);
            AppUtils.ff(rbT6Tee);
            AppUtils.ff(rbT6TeeE90);
            AppUtils.ff(rbT6TeeE45);
            AppUtils.ff(rbT6ByMep);
            AppUtils.ff(rbT6Auto);
            AppUtils.ff(chkT6Direct);
            if (!rbT6ByMep.Checked && !rbT6Auto.Checked)
                rbT6ByMep.Checked = true;
            if (!rbT6Tee.Checked && !rbT6TeeE90.Checked && !rbT6TeeE45.Checked)
                rbT6Tee.Checked = true;
        }

        private void SaveType6()
        {
            AppUtils.sa(rbC5Type6);
            AppUtils.sa(tbT6L1);
            AppUtils.sa(tbT6L2);
            AppUtils.sa(tbT6L3);
            AppUtils.sa(tbT6Elevation);
            AppUtils.sa(rbT6Tee);
            AppUtils.sa(rbT6TeeE90);
            AppUtils.sa(rbT6TeeE45);
            AppUtils.sa(rbT6ByMep);
            AppUtils.sa(rbT6Auto);
            AppUtils.sa(chkT6Direct);
            AppUtils.sa(cbT6Family);
            AppUtils.sa(cbT6Type);
        }

        private string ValidateType6()
        {
            if (PipeSizeC5 == double.MaxValue)
                return "Select a Pipe Size.";
            if (L1_6 <= 0)
                return "L1 must be greater than 0.";
            if (L2_6 < 0)
                return "L2 must be 0 or greater.";
            if (L3_6 < 0)
                return "L3 must be 0 or greater.";
            if (MainFitting6 != TwinMainFitting.Tee && A_6 <= 0)
                return "A must be greater than 0.";
            if (UprightSymbol6 == null)
                return "Select the upright sprinkler Family and Type.";
            if (!ConnectTeeDirectly6 && UprightElevation6 <= 0)
                return "Elevation must be greater than 0.";
            return null;
        }

        // Chỉ family đặt tự do theo Level (đặt được không cần host).
        private void FillUprightFamilies()
        {
            cbT6Family.Items.Clear();
            var names = new FilteredElementCollector(Global.UIDoc.Document).OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Sprinklers).Cast<FamilySymbol>()
                .Where(x => x.Family != null && x.Family.FamilyPlacementType == FamilyPlacementType.OneLevelBased)
                .Select(x => x.FamilyName).Distinct().OrderBy(x => x).ToList();
            foreach (string n in names)
                cbT6Family.Items.Add(n);

            AppUtils.ff(cbT6Family);
            if (cbT6Family.SelectedItem == null && cbT6Family.Items.Count > 0)
            {
                int upright = names.FindIndex(n => n.IndexOf("upright", StringComparison.OrdinalIgnoreCase) >= 0);
                cbT6Family.SelectedIndex = upright >= 0 ? upright : 0;
            }
            FillUprightTypes();
        }

        private void FillUprightTypes()
        {
            cbT6Type.Items.Clear();
            string family = cbT6Family.SelectedItem as string;
            if (family == null)
                return;
            foreach (FamilySymbol s in new FilteredElementCollector(Global.UIDoc.Document).OfClass(typeof(FamilySymbol))
                         .OfCategory(BuiltInCategory.OST_Sprinklers).Cast<FamilySymbol>()
                         .Where(x => x.FamilyName == family).OrderBy(x => x.Name))
                cbT6Type.Items.Add(s);

            AppUtils.ff(cbT6Type);
            if (cbT6Type.SelectedItem == null && cbT6Type.Items.Count > 0)
                cbT6Type.SelectedIndex = 0;
        }

        private void rbC5Type6_CheckedChanged(object sender, EventArgs e)
        {
            // Chạy sau khi các radio khác xử lý xong sự kiện của chúng, để trạng thái cuối do Type 6 quyết định.
            if (IsHandleCreated)
                BeginInvoke((Action)RefreshType6State);
            else
                RefreshType6State();
        }

        /// <summary>Bật/tắt khung Type 6 và các ô dùng chung theo type đang chọn.</summary>
        private void RefreshType6State()
        {
            bool t6 = rbC5Type6.Checked;
            SetType6Panel(t6);
            if (t6)
            {
                tbC4L.Enabled = MainFitting6 != TwinMainFitting.Tee;   // A: chỉ khi có đoạn đứng / chéo
                tbC4L1.Enabled = false;                                 // B: không dùng
                chkVerticalTeeOffset.Enabled = false;                   // M
                txtVerticalTeeOffset.Enabled = false;
                chkUseNipple.Enabled = false;
                cbNipple.Enabled = false;
                chkC5TeeTap.Enabled = true;                             // Elbow 90: co ở đầu ống chính còn hở
                tbT6Elevation.Enabled = !chkT6Direct.Checked;
            }
            else
            {
                bool t5 = rbC5Type5.Checked;
                chkVerticalTeeOffset.Enabled = !t5;
                txtVerticalTeeOffset.Enabled = !t5 && chkVerticalTeeOffset.Checked;
                chkUseNipple.Enabled = !t5;
                cbNipple.Enabled = !t5 && chkUseNipple.Checked;
                DisableControl();
            }
            CheckPreviewImages();
        }

        private void SetType6Panel(bool show)
        {
            if (show == m_type6Shown)
                return;
            m_type6Shown = show;

            int delta = show ? Type6PanelWidth : -Type6PanelWidth;
            SuspendLayout();
            tableLayoutPanel1.ColumnStyles[2].Width = show ? Type6PanelWidth : 0;
            pnlType6.Visible = show;
            if (show)
            {
                MaximumSize = new System.Drawing.Size(MaximumSize.Width + delta, MaximumSize.Height);
                MinimumSize = new System.Drawing.Size(MinimumSize.Width + delta, MinimumSize.Height);
                Width += delta;
            }
            else
            {
                MinimumSize = new System.Drawing.Size(MinimumSize.Width + delta, MinimumSize.Height);
                Width += delta;
                MaximumSize = new System.Drawing.Size(MaximumSize.Width + delta, MaximumSize.Height);
            }
            ResumeLayout(true);
        }

        /// <summary>Ảnh preview Type 6 theo cách nối vào ống chính.</summary>
        private void ShowType6Preview()
        {
            string name = MainFitting6 == TwinMainFitting.TeeElbow90 ? "Twin_Type6_E90.png"
                        : MainFitting6 == TwinMainFitting.TeeElbow45 ? "Twin_Type6_E45.png" : "Twin_Type6_Tee.png";
            string file = Path.Combine(Common.GetPreviewFolder(), "PlumbingSolution", name);
            if (File.Exists(file))
                pictureBox1.Image = System.Drawing.Image.FromFile(file);
        }

        // ---- dựng control ----

        private static TableLayoutPanel Grid(int cols, int rows)
        {
            var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = cols, RowCount = rows };
            for (int i = 0; i < cols; i++)
                t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / cols));
            for (int i = 0; i < rows; i++)
                t.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / rows));
            return t;
        }

        private static GroupBox Group(string text, Control content)
        {
            var g = new GroupBox { Text = text, Dock = DockStyle.Fill, Margin = new Padding(5, 3, 5, 3) };
            g.Controls.Add(content);
            return g;
        }

        private static Label Caption(string text)
        {
            return new Label { Text = text, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3) };
        }

        private TextBox NumberBox(string name, string value)
        {
            var tb = new TextBox { Name = name, Text = value, Dock = DockStyle.Fill, Margin = new Padding(3) };
            tb.KeyPress += txbC3Length_KeyPress;
            return tb;
        }

        private static RadioButton Radio(string name, string text, bool check)
        {
            return new RadioButton { Name = name, Text = text, Checked = check, AutoSize = true, Margin = new Padding(3) };
        }
    }
}

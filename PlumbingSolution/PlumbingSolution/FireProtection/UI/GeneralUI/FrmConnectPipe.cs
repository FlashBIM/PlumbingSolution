using PlumbingSolution.FireProtection.Ultis;
using PlumbingSolution.FireProtection.Utils;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PlumbingSolution.FireProtection.UI.GeneralUI
{
    public enum ConnectRouteType { Branch, Elbow, Straight, Parallel }

    public enum ConnectFittingType { Elbow45, Elbow90, SameElevation }

    public enum ConnectDirection { Horizontal, Vertical, Auto }

    /// <summary>
    /// Form Connect Pipe (theo sheet): chọn Route Type + Connection Type (+ Direction) rồi OK.
    /// Lệnh CmdConnectPipe đọc lựa chọn và chạy đúng tool nối của Dirit ứng với tổ hợp đó.
    /// Preview: hiện ảnh minh hoạ của tổ hợp đang chọn (Icon\Preview\PlumbingSolution\ConnectPipe_*.png).
    /// </summary>
    public partial class FrmConnectPipe : Form
    {
        private const int PreviewHeight = 240;

        public FrmConnectPipe()
        {
            InitializeComponent();
            Common.SettingTemplate(this);
        }

        public ConnectRouteType RouteType
        {
            get
            {
                if (rdnElbow.Checked) return ConnectRouteType.Elbow;
                if (rdnStraight.Checked) return ConnectRouteType.Straight;
                if (rdnParallel.Checked) return ConnectRouteType.Parallel;
                return ConnectRouteType.Branch;
            }
        }

        public ConnectFittingType FittingType
        {
            get
            {
                if (rdnElbow90.Checked) return ConnectFittingType.Elbow90;
                if (rdnSameElevation.Checked) return ConnectFittingType.SameElevation;
                return ConnectFittingType.Elbow45;
            }
        }

        public ConnectDirection Direction
        {
            get
            {
                if (rdnVertical.Checked) return ConnectDirection.Vertical;
                if (rdnAuto.Checked) return ConnectDirection.Auto;
                return ConnectDirection.Horizontal;
            }
        }

        /// <summary>Direction chỉ có nghĩa với Parallel + Same Elevation (Horizontal / Vertical).</summary>
        public bool UsesDirection
        {
            get { return RouteType == ConnectRouteType.Parallel && FittingType == ConnectFittingType.SameElevation; }
        }

        private void FrmConnectPipe_Load(object sender, EventArgs e)
        {
            foreach (RadioButton r in new[] { rdnBranch, rdnElbow, rdnStraight, rdnParallel, rdnElbow45, rdnElbow90, rdnSameElevation, rdnHorizontal, rdnVertical, rdnAuto })
                AppUtils.ff(r);
            AppUtils.ff(this);

            UpdateState();
        }

        private void Option_CheckedChanged(object sender, EventArgs e)
        {
            if (sender is RadioButton r && !r.Checked)
                return;
            UpdateState();
        }

        // Làm mờ những lựa chọn không có cách nối tương ứng.
        private void UpdateState()
        {
            // Straight: chờ ghép từ SMEP.
            rdnStraight.Enabled = false;
            if (rdnStraight.Checked)
                rdnBranch.Checked = true;

            bool direction = UsesDirection;
            grbDirection.Enabled = direction;
            rdnAuto.Enabled = false; // Auto chỉ dùng cho Straight
            if (direction && rdnAuto.Checked)
                rdnHorizontal.Checked = true;

            if (pnlPreview.Visible)
                ShowPreview();
        }

        /// <summary>Tên ảnh minh hoạ: ConnectPipe_Branch_Elbow45.png, ConnectPipe_Parallel_SameElevation_Vertical.png ...</summary>
        public string PreviewFileName
        {
            get
            {
                string name = "ConnectPipe_" + RouteType + "_" + FittingType;
                if (UsesDirection)
                    name += "_" + Direction;
                return name + ".png";
            }
        }

        private void ShowPreview()
        {
            Image old = picPreview.Image;
            picPreview.Image = null;
            old?.Dispose();

            string file = Path.Combine(Common.GetPreviewFolder(), "PlumbingSolution", PreviewFileName);
            if (File.Exists(file))
            {
                // Đọc qua bộ nhớ để không khoá file ảnh.
                using (var ms = new MemoryStream(File.ReadAllBytes(file)))
                    picPreview.Image = Image.FromStream(ms);
            }

            picPreview.Visible = picPreview.Image != null;
            lblNoPreview.Visible = picPreview.Image == null;
        }

        private void btnPreview_Click(object sender, EventArgs e)
        {
            bool show = !pnlPreview.Visible;
            int delta = show ? PreviewHeight : -PreviewHeight;

            tableLayoutPanel1.RowStyles[2].Height = show ? PreviewHeight : 0;
            pnlPreview.Visible = show;
            ClientSize = new Size(ClientSize.Width, ClientSize.Height + delta);

            if (show)
                ShowPreview();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            foreach (RadioButton r in new[] { rdnBranch, rdnElbow, rdnStraight, rdnParallel, rdnElbow45, rdnElbow90, rdnSameElevation, rdnHorizontal, rdnVertical, rdnAuto })
                AppUtils.sa(r);
            AppUtils.sa(this);

            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            picPreview.Image?.Dispose();
            base.OnFormClosed(e);
        }
    }
}

using PlumbingSolution.LoginLicense;
using PlumbingSolution.Ultis;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace PlumbingSolution.UI.BeginUI
{
    /// <summary>
    /// Cửa sổ "My License" — giữ nguyên bố cục cũ theo yêu cầu (không thêm field mới).
    /// Chỉ đổi NGUỒN dữ liệu: đọc từ <see cref="App.Runtime"/> thay vì registry, và bỏ
    /// nhánh phân biệt "đăng nhập bằng tài khoản" (add-in giờ chỉ còn Activated Key).
    /// </summary>
    public partial class InformationForm : Form
    {
        public InformationForm()
        {
            InitializeComponent();
        }

        private void InformationForm_Load(object sender, EventArgs e)
        {
            try
            {
                var license = App.Runtime?.LicenseInfo;
                if (license == null)
                {
                    this.Close();
                    return;
                }

                TimeZoneInfo localTimeZone = TimeZoneInfo.Local;
                DateTime endDate = TimeZoneInfo.ConvertTimeFromUtc(license.EndDate, localTimeZone);
                DateTime startDate = TimeZoneInfo.ConvertTimeFromUtc(license.StartDate, localTimeZone);
                var periodDay = (int)(endDate - startDate).TotalDays / 30;

                LinkLabel emailLink = new LinkLabel();
                emailLink.LinkVisited = true;
                emailLink.AutoSize = true;
                emailLink.LinkColor = Color.Blue;
                emailLink.ActiveLinkColor = Color.Red;
                emailLink.VisitedLinkColor = Color.Blue;
                emailLink.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));

#if RELEASE2019_VN || RELEASE2020_VN || RELEASE2021_VN || RELEASE2022_VN || RELEASE2023_VN || RELEASE2024_VN
                this.Text = "Thông tin License";
                btnExit.Text = "Đóng";
                linkLabel1.Text = "Đăng xuất";

                lblProduct.Text = "Sản phẩm - " + (license.Product?.Name ?? "").Replace("&", "&&");
                lblLicenseType.Text = string.IsNullOrEmpty(license.CodeMasked)
                    ? "Loại License - Tài khoản"
                    : "Loại License - Key (" + license.CodeMasked + ")";
                label4.Text = "Kích hoạt cho " + (license.Email ?? "");
                lblPeriod.Text = "Thời hạn License - " + periodDay + " Tháng";

                if (license.Status == LoginLicense.Enums.LicenseStatus.Active)
                    lblStatus.Text = $"Có hiệu lực đến {endDate:d/M/yyyy, HH:mm}, {(int)(endDate - DateTime.Now).TotalDays} ngày còn lại";
                else if (license.Status == LoginLicense.Enums.LicenseStatus.Locked)
                {
                    lblStatus.Text = "Tạm khóa. Vui lòng liên hệ chúng tôi qua email. \n";
                    ChangeTextAndClickedLink(emailLink, "info@flashbim.vn", string.Empty);
                }
                else if (license.Status == LoginLicense.Enums.LicenseStatus.Expired)
                {
                    lblStatus.Text = "Hết hạn. Vui lòng truy cập website để mua. \n";
                    ChangeTextAndClickedLink(emailLink, string.Empty, "https://flashbim.vn");
                }
                else
                {
                    lblStatus.Text = "Giấy phép lỗi. Vui lòng liên hệ với chúng tôi qua email. \n";
                    ChangeTextAndClickedLink(emailLink, "info@flashbim.vn", string.Empty);
                }
#else
                lblProduct.Text = "Product of " + (license.Product?.Name ?? "").Replace("&", "&&");
                lblLicenseType.Text = string.IsNullOrEmpty(license.CodeMasked)
                    ? "Account License"
                    : "Key License (" + license.CodeMasked + ")";
                label4.Text = "Activated for " + (license.Email ?? "");
                lblPeriod.Text = "License Period of " + periodDay + " month";

                if (license.Status == LoginLicense.Enums.LicenseStatus.Active)
                    lblStatus.Text = $"Validate until {endDate:d/M/yyyy, HH:mm}, {(int)(endDate - DateTime.Now).TotalDays} days left";
                else if (license.Status == LoginLicense.Enums.LicenseStatus.Locked)
                {
                    lblStatus.Text = "Locked. Please contact us via email. \n";
                    ChangeTextAndClickedLink(emailLink, "info@flashbim.com", string.Empty);
                }
                else if (license.Status == LoginLicense.Enums.LicenseStatus.Expired)
                {
                    lblStatus.Text = "Expired. Please visit our website to buy. \n";
                    ChangeTextAndClickedLink(emailLink, string.Empty, "https://flashbim.com");
                }
                else
                {
                    lblStatus.Text = "Error License. Please contact us via email. \n";
                    ChangeTextAndClickedLink(emailLink, "info@flashbim.com", string.Empty);
                }
#endif
            }
            catch (Exception)
            {
            }
        }

        private void ChangeTextAndClickedLink(LinkLabel link, string linkEmailText, string linkWebText)
        {
            if (linkEmailText != string.Empty)
            {
                link.Text = linkEmailText;
                link.LinkClicked += new LinkLabelLinkClickedEventHandler(LinkLabel1_LinkClicked);
                link.Location = new Point(lblStatus.Location.X, lblStatus.Location.Y + lblStatus.Height + 5);
                link.Visible = true;
                this.Controls.Add(link);
            }

            if (linkWebText != string.Empty)
            {
                link.Text = linkWebText;
                link.LinkClicked += new LinkLabelLinkClickedEventHandler(LinkLabel1_LinkClickedLinkWeb);
                link.Location = new Point(lblStatus.Location.X, lblStatus.Location.Y + lblStatus.Height + 5);
                link.Visible = true;
                this.Controls.Add(link);
            }
        }

        private void LinkLabel1_LinkClickedLinkWeb(object sender, LinkLabelLinkClickedEventArgs e)
        {
#if RELEASE2019_VN || RELEASE2020_VN || RELEASE2021_VN || RELEASE2022_VN || RELEASE2023_VN || RELEASE2024_VN
            e.Link.Visited = true;
            Process.Start("https://flashbim.vn");
#else
            e.Link.Visited = true;
            Process.Start("https://flashbim.com");
#endif
        }

        private void LinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
#if RELEASE2019_VN || RELEASE2020_VN || RELEASE2021_VN || RELEASE2022_VN || RELEASE2023_VN || RELEASE2024_VN
            e.Link.Visited = true;
            Process.Start("mailto:info@flashbim.vn");
#else
            e.Link.Visited = true;
            Process.Start("mailto:info@flashbim.com");
#endif
        }

        /// <summary>
        /// Đăng xuất — CHỈ cục bộ (không gọi server, không tốn suất máy). Xem
        /// <see cref="LoginLicense.Runtime.LicenseRuntime.Logout"/>.
        /// </summary>
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            App.Runtime?.Logout();

            FormMessage formMessage = new FormMessage(Define.LicenseAccountLogOut, Define.LicenseStatusLogOut);
            formMessage.ShowDialog();

            this.Close();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}

using PlumbingSolution.LoginLicense;
using PlumbingSolution.LoginLicense.Client;
using PlumbingSolution.Ultis;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace PlumbingSolution.UI.BeginUI
{
    /// <summary>
    /// Kích hoạt bằng license key - giao diện lấy từ LoginForm của SMEP-App (logo Flash BIM,
    /// Remember me, Register Account / Request for Free Trial). Phần kích hoạt giữ nguyên luồng
    /// LicenseRuntime của PlumbingSolution; các hàm đăng nhập kiểu cũ của SMEP (LoginHandler, AppCommon,
    /// CheckLicenseHandler) đã bị comment/không dùng bên đó nên không mang sang.
    /// </summary>
    public partial class LoginForm : Form
    {
        // Mục registry lưu key khi tick "Remember me" (SMEP dùng "SmartMEPLicense").
        private const string RegistrySection = "PlumbingSolutionLicense";

        // TODO: điền mã sản phẩm PlumbingSolution trên flashbim.com để link "Request for Free Trial"
        // mở đúng sản phẩm. Để trống thì mở trang free-trial chung.
        private const string FreeTrialProductId = "";

        private static string AppName
        {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Name; }
        }

        public LoginForm()
        {
            InitializeComponent();
            this.AcceptButton = keyBtn;

            // Thiếu file logo thì form vẫn phải mở được - không để Image.FromFile ném lỗi.
            string logoPath = Path.Combine(DirectoryUtils.GetIconFolder(), "logo dai Flash BIM.png");
            if (File.Exists(logoPath))
                pictureBox2.Image = Image.FromFile(logoPath);
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
#if RELEASE2019_VN || RELEASE2020_VN || RELEASE2021_VN || RELEASE2022_VN || RELEASE2023_VN || RELEASE2024_VN
            this.Text = "Đăng nhập";
            label5.Text = "Key kích hoạt";
            keyBtn.Text = "Đăng nhập";
            chkRemKey.Text = "Lưu key";
            linkRegister.Text = "Đăng ký tài khoản";
            linkRequest.Text = "Đăng ký dùng thử";
#else
            this.Text = "Login";
#endif

            var checkRememberMeKey = Microsoft.VisualBasic.Interaction.GetSetting(AppName, RegistrySection, "RememberMeKey", "");
            if (!string.IsNullOrEmpty(checkRememberMeKey) && checkRememberMeKey == "1")
            {
                chkRemKey.Checked = true;
                licenseKeyTextBox.Text = Microsoft.VisualBasic.Interaction.GetSetting(AppName, RegistrySection, "Key", "");
            }
        }

        private async void SignInBtn2_Click(object sender, EventArgs e)
        {
            var licenseKey = licenseKeyTextBox.Text;

            if (App.Runtime == null)
            {
                FormMessage errorForm = new FormMessage(LicenseMessages.ForError(ActivationError.UnknownLogicError), Define.LicenseStatusLogIn);
                errorForm.ShowDialog();
                return;
            }

            keyBtn.Enabled = false;
            ActivationError? error;
            try
            {
                error = await App.Runtime.ActivateAsync(licenseKey, CancellationToken.None);
            }
            finally
            {
                keyBtn.Enabled = true;
            }

            if (error != null)
            {
                FormMessage errorForm = new FormMessage(LicenseMessages.ForError(error.Value), Define.LicenseStatusLogIn);
                errorForm.ShowDialog();
                return;
            }

            FormMessage successForm = new FormMessage(Define.LicenseAccountLogInSuccess, Define.LicenseStatusLogIn);
            successForm.ShowDialog();

            // Ribbon mở qua sự kiện Activated (App.OnLicenseActivated) — bắn trong nhịp
            // kế tiếp của vòng lặp nền, gần như tức thì nhờ WakeNow() bên trong ActivateAsync.

            if (chkRemKey.Checked)
            {
                Microsoft.VisualBasic.Interaction.SaveSetting(AppName, RegistrySection, "RememberMeKey", "1");
                Microsoft.VisualBasic.Interaction.SaveSetting(AppName, RegistrySection, "Key", licenseKeyTextBox.Text);
            }
            else
            {
                Microsoft.VisualBasic.Interaction.SaveSetting(AppName, RegistrySection, "RememberMeKey", "0");
                Microsoft.VisualBasic.Interaction.SaveSetting(AppName, RegistrySection, "Key", "");
            }

            this.Close();
        }

        private void linkRegister_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenUrl("https://flashbim.com/signup");
        }

        private void linkRequest_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string url = "https://flashbim.com/free-trial";
            if (!string.IsNullOrEmpty(FreeTrialProductId))
                url += "?product=" + FreeTrialProductId;
            OpenUrl(url);
        }

        private static void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
    }
}

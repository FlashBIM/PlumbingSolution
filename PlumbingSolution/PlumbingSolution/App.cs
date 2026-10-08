using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Autodesk.Windows;
using PlumbingSolution.Commands;
using PlumbingSolution.LoginLicense;
using PlumbingSolution.LoginLicense.Gate;
using PlumbingSolution.LoginLicense.Runtime;
using PlumbingSolution.Requests;
using PlumbingSolution.UI.BeginUI;
using PlumbingSolution.Ultis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace PlumbingSolution
{
    [Transaction(TransactionMode.Manual)]
    public partial class App : IExternalApplication
    {
        public static UIControlledApplication _AppCache;
        public static MyExternalEvent Handler;
        public static ExternalEvent MyExternalEvent;
        public static MyExternalEventEnable HandlerEnable;
        public static ExternalEvent MyExternalEventEnable;
        public static MyExternalEventDisable HandlerDisable;
        public static ExternalEvent MyExternalEventDisable;

        /// <summary>Vòng đời license của phiên — dựng ở OnStartup, LoginForm/InformationForm dùng để activate/logout.</summary>
        public static LicenseRuntime Runtime;

        public static UIControlledApplication g_UIControlledApplication;

        public static UIApplication g_UIApplication = null;

        public static UIDocument g_UIDocument = null;

        public static Document g_Document = null;
        public static bool IsChangeDoc { get; set; }

        public static bool isApply = true;
        private static ExternalEvent exEvent;

        public static string VersionRevit = string.Empty;

        private static WindowHandle hWndRevit;

        public Result OnStartup(UIControlledApplication application)
        {
            _AppCache = application;
            Handler = new MyExternalEvent();
            HandlerEnable = new MyExternalEventEnable();
            HandlerDisable = new MyExternalEventDisable();
            MyExternalEvent = ExternalEvent.Create(Handler);
            MyExternalEventEnable = ExternalEvent.Create(HandlerEnable);
            MyExternalEventDisable = ExternalEvent.Create(HandlerDisable);

            VersionRevit = application.ControlledApplication.VersionNumber;

            // Create ribbon tools
            g_UIControlledApplication = application;
            CreateRibbonButtons(application);
            StartLicenseRuntime();
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            if (Runtime != null)
            {
                Runtime.Dispose();
                Runtime = null;
            }

            return Result.Succeeded;
        }

        /// <summary>
        /// Dựng vòng đời license và bắt đầu chạy nền. Lỗi ở đây KHÔNG được làm hỏng cả
        /// add-in — ribbon giữ nguyên trạng thái khoá (đã đặt bởi <see cref="CreateRibbonButtons"/>),
        /// Revit vẫn mở bình thường.
        /// </summary>
        private void StartLicenseRuntime()
        {
            try
            {
                Runtime = LicenseRuntime.Start();
                LicenseGate.Bind(Runtime.State, OnToolBlocked);
                Runtime.Activated += OnLicenseActivated;
                Runtime.Lost += OnLicenseLost;
                Runtime.ConnectionLost += OnConnectionLost;

                _ = Task.Run(delegate { return Runtime.StartSessionAsync(CancellationToken.None); });
            }
            catch (Exception e)
            {
                LicenseLog.Write("khởi động license lỗi (ribbon giữ khoá): " + e.Message);
            }
        }

        private void CreateRibbonButtons(UIControlledApplication app)
        {
            //Get dll path
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string iconFolder = GetIconFolder();

            string tabName = Define.RibbonTabName;

            app.CreateRibbonTab(tabName);

            CreateLoginTab(app, tabName, assemblyPath, iconFolder);
            CreatePlumbingPanel(app, tabName, assemblyPath, iconFolder);

            //Check license
            RevitUtils.DisableItemRibbon(app);
        }

        private void CreateLoginTab(UIControlledApplication app,
                                                   string tabName,
                                                   string assemblyPath,
                                                   string iconFolder)
        {
            //Create ribbon panel
            Autodesk.Revit.UI.RibbonPanel loginHangerPanel = app.CreateRibbonPanel(tabName, Define.LoginLicenseTabName);

            //Create button
            PushButtonData loginData = new PushButtonData("btnLogin", "Login", assemblyPath, Define.CmdLoginClassName);
            AddImages(loginData, iconFolder, "Signin.psd-01.png", "Signin.psd-01.png");

            PushButtonData AboutUsData = new PushButtonData("btnAboutUs", "About Us", assemblyPath, Define.CmdAboutUsClassName);
            AddImages(AboutUsData, iconFolder, "About.psd-01.png", "About.psd-01.png");

            loginHangerPanel.AddItem(loginData);
            loginHangerPanel.AddItem(AboutUsData);
        }

        private static void EnsureRevitWindowHandle()
        {
            if (hWndRevit == null)
                hWndRevit = new WindowHandle(Process.GetCurrentProcess().MainWindowHandle);
        }

        public static void SetImgRibbonButton(string tabName, string panelName, string splitButtonText, string newButtonText, string newImgButton)
        {
            RibbonControl ribbon = ComponentManager.Ribbon;
            foreach (RibbonTab tab in ribbon.Tabs)
            {
                if (tab.Name == tabName)
                {
                    foreach (Autodesk.Windows.RibbonPanel panel in tab.Panels)
                    {
                        if (panel.Source.AutomationName == panelName)
                        {
                            foreach (Autodesk.Windows.RibbonItem item in panel.Source.Items)
                            {
                                if (splitButtonText == item.AutomationName)
                                {
                                    Autodesk.Windows.RibbonItem ribbonButton = item as Autodesk.Windows.RibbonButton;
                                    if (ribbonButton is Autodesk.Windows.RibbonButton rButton)
                                    {
                                        rButton.Text = newButtonText;
                                        rButton.LargeImage = new BitmapImage(new Uri(DirectoryUtils.GetIconFolder() + "\\" + newImgButton));
                                    }
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// License hợp lệ (kích hoạt lần đầu, resume lúc khởi động, hoặc refresh định kỳ).
        /// Chạy trên luồng nền của <see cref="LicenseRuntime"/> — phải đưa việc chạm Revit
        /// API về luồng giao diện qua ExternalEvent, không được gọi trực tiếp ở đây.
        /// </summary>
        private static void OnLicenseActivated()
        {
            HandlerEnable.m_licenseData = Runtime.LicenseInfo;
            MyExternalEventEnable.Raise();
        }

        /// <summary>
        /// Mất license hoặc chưa từng kích hoạt. "NOT_ACTIVATED"/"LOGGED_OUT" đưa nút về
        /// "Login" (chưa từng vào được); còn lại (EXPIRED/LOCKED/MACHINE_LIMIT/REVOKED/
        /// INVALID_TOKEN...) giữ nút "My License" nhưng khoá các lệnh — đúng ngữ nghĩa cũ
        /// của <see cref="Ultis.RevitUtils.DisableItemRibbonLocked"/> (đã từng vào được,
        /// giờ bị chặn) so với reset hoàn toàn.
        /// </summary>
        private static void OnLicenseLost(string reason)
        {
            if (reason == "NOT_ACTIVATED" || reason == "LOGGED_OUT")
            {
                MyExternalEvent.Raise();
            }
            else
            {
                MyExternalEventDisable.Raise();
            }
        }

        /// <summary>Mất liên lạc server quá lâu trong khi token còn hạn — CHỈ báo, không khoá.</summary>
        private static void OnConnectionLost()
        {
            LicenseLog.Write("app: mất liên lạc license server kéo dài — tool vẫn chạy tới khi token hết hạn");
        }

        /// <summary>
        /// Một lệnh bị <see cref="LicenseGate"/> chặn. Chưa từng có license trong phiên
        /// (chứ không phải đang trong vùng đệm/mất liên lạc) thì mở thẳng LoginForm cho
        /// tiện, thay vì bắt người dùng tự tìm nút Login.
        /// </summary>
        private static void OnToolBlocked()
        {
            LicenseRuntime runtime = Runtime;
            if (runtime != null)
            {
                runtime.WakeNow();
            }

            if (runtime == null || runtime.LicenseInfo == null)
            {
                var loginForm = new LoginForm();
                loginForm.ShowDialog();
            }
        }

        private void AddImages(PushButton pushButton, string iconFolder, string largeImage, string smallImage)
        {
            if (!string.IsNullOrEmpty(iconFolder)
                && Directory.Exists(iconFolder))
            {
                string largeImagePath = Path.Combine(iconFolder, largeImage);
                if (File.Exists(largeImagePath))
                    pushButton.LargeImage = new BitmapImage(new Uri(largeImagePath));

                string smallImagePath = Path.Combine(iconFolder, smallImage);
                if (File.Exists(smallImagePath))
                    pushButton.Image = new BitmapImage(new Uri(smallImagePath));
            }
        }

        /// <summary>
        /// Add images
        /// </summary>
        /// <param name="buttonData"></param>
        /// <param name="iconFolder"></param>
        /// <param name="largeImage"></param>
        /// <param name="smallImage"></param>
        private void AddImages(ButtonData buttonData,
                               string iconFolder,
                               string largeImage,
                               string smallImage)
        {
            if (!string.IsNullOrEmpty(iconFolder)
                && Directory.Exists(iconFolder))
            {
                string largeImagePath = Path.Combine(iconFolder, largeImage);
                if (File.Exists(largeImagePath))
                    buttonData.LargeImage = new BitmapImage(new Uri(largeImagePath));

                string smallImagePath = Path.Combine(iconFolder, smallImage);
                if (File.Exists(smallImagePath))
                    buttonData.Image = new BitmapImage(new Uri(smallImagePath));
            }
        }

        /// <summary>
        /// Get icon folder
        /// </summary>
        /// <returns></returns>
        private string GetIconFolder()
        {
            string appDir = GetAppFolder();
            string imageDir = Path.Combine(appDir, "Icon");
            return imageDir;
        }

        /// <summary>
        /// Get app folder
        /// </summary>
        /// <returns></returns>
        private string GetAppFolder()
        {
            string location = Assembly.GetExecutingAssembly().Location;
            string dir = Path.GetDirectoryName(location);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }
    }
}
using PlumbingSolution.FireProtection.Ultis;
using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ProgressBar = System.Windows.Controls.ProgressBar;
using System.Collections.Generic;
using System.IO;

namespace PlumbingSolution.FireProtection.UI.ProcessBarForm
{
    /// <summary>
    /// Interaction logic for FrmProcessbar.xaml
    /// </summary>
    public partial class FrmProcessbar : Window, IDisposable
    {
        [DllImport("user32.dll")]
        internal static extern int SetWindowLong(IntPtr hwnd, int index, int value);

        [DllImport("user32.dll")]
        internal static extern int GetWindowLong(IntPtr hwnd, int index);

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            const int GWL_STYLE = -16;
            const int WS_SYSMENU = 0x80000;

            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            long value = GetWindowLong(hwnd, GWL_STYLE);

            SetWindowLong(hwnd, GWL_STYLE, (int)(value & ~WS_SYSMENU));
        }

        private UpdateProgressBarDelegate updPb0Delegate = null;
        private double value0 = 0;

        public bool IsCancel = false;

        public FrmProcessbar(string title, string message, DiritIconTool diritIcon)
        {
            InitializeComponent();

            this.Title = title;
            this.tbxMessage.Text = message;
            string location = Assembly.GetExecutingAssembly().Location;
            string dir = System.IO.Path.GetDirectoryName(location);

            //Bitmap bitmap = null;

            //switch (diritIcon)
            //{
            //    case DiritIconTool.Drain:
            //        bitmap = AddinDiritResource.Properties.Resources.Drain_icon_psd_01.ToBitmap();
            //        break;

            //    case DiritIconTool.Fire:
            //        bitmap = AddinDiritResource.Properties.Resources.FIRE_ICON_01.ToBitmap();
            //        break;

            //    case DiritIconTool.Gen:
            //        bitmap = AddinDiritResource.Properties.Resources.GEN_ICON_psd_01.ToBitmap();
            //        break;

            //    case DiritIconTool.Mep:
            //        bitmap = AddinDiritResource.Properties.Resources.AUTO_HANGER_011.ToBitmap();
            //        break;

            //    default:
            //        break;
            //}

            //IntPtr hBitmap = bitmap.GetHbitmap();

            //ImageSource wpfBitmap =
            //     Imaging.CreateBitmapSourceFromHBitmap(
            //          hBitmap, IntPtr.Zero, Int32Rect.Empty,
            //          BitmapSizeOptions.FromEmptyOptions());

            //this.Icon = wpfBitmap;

            //if (diritIcon == DiritIconTool.Mep)
            //{
            //    string largeImagePath = Path.Combine(GetMepIconFolder(), "AUTO-HANGER-01.ico");

            //    this.Icon = new BitmapImage(new Uri(largeImagePath));
            //}

            //tbxFinish.Text = Common.GetTextLanguage("Complete");
            //btnCancel.Content = Common.GetTextLanguage("btnCloseProcessBar");
        }

        private string GetAppFolder()
        {
            string location = Assembly.GetExecutingAssembly().Location;
            string dir = Path.GetDirectoryName(location);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        private string GetIconFolder()
        {
            string appDir = GetAppFolder();
            string imageDir = Path.Combine(appDir, "Icon");
            return imageDir;
        }

        private string GetMepIconFolder()
        {
            string appDir = GetIconFolder();
            string imageDir = Path.Combine(appDir, "MEP");
            return imageDir;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (value0 < this.prgSingle.Maximum)
            {
                IsCancel = true;
            }
            this.Dispose();
            return;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            updPb0Delegate = new UpdateProgressBarDelegate(prgSingle.SetValue);
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
        }

        public void IncrementProgressBar()
        {
            value0++;
            Dispatcher.Invoke(
                updPb0Delegate,
                System.Windows.Threading.DispatcherPriority.Background, new object[]
                {
                    ProgressBar.ValueProperty, value0
                }
                );
        }

        public void UpdateProgressBar()
        {
            Dispatcher.Invoke(
                updPb0Delegate,
                System.Windows.Threading.DispatcherPriority.Background, new object[]
                {
                    ProgressBar.ValueProperty, value0
                }
                );
        }

        public void ResetProgressBar()
        {
            value0 = 0;
            Dispatcher.Invoke(
                updPb0Delegate,
                System.Windows.Threading.DispatcherPriority.Background, new object[]
                {
                    ProgressBar.ValueProperty, value0
                }
                );
        }

        public void UpdateMessage(string message)
        {
            tbxMessage.Text = message;
        }

        private delegate void UpdateProgressBarDelegate(System.Windows.DependencyProperty dp, Object value);


        public void Dispose()
        {
            this.Close();
        }

    }
}

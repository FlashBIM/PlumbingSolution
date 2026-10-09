using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Forms;

// FormSnapshot <PlumbingSolution.dll> <thư mục Icon> <thư mục ảnh ra> [raw]
// Dựng form bằng InitializeComponent (bỏ qua constructor và sự kiện Load vì chúng gọi Revit API),
// áp FormStyle (trừ khi "raw"), rồi lưu ảnh PNG từng form.
internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        string dll = Path.GetFullPath(args[0]);
        string iconDir = Path.GetFullPath(args[1]);
        string outDir = Path.GetFullPath(args[2]);
        bool styled = args.Length < 4 || args[3] != "raw";
        Directory.CreateDirectory(outDir);

        string dllDir = Path.GetDirectoryName(dll);
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
        {
            string p = Path.Combine(dllDir, new AssemblyName(e.Name).Name + ".dll");
            return File.Exists(p) ? Assembly.LoadFrom(p) : null;
        };

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        Assembly asm = Assembly.LoadFrom(dll);
        Type style = asm.GetType("PlumbingSolution.FireProtection.UI.FormStyle", true);

        var shots = new List<Tuple<string, string>>
        {
            Tuple.Create("UI_ConnectSprinkle", @"DIRIT FIRE\Phuonganlen1.png"),
            Tuple.Create("UI_SprinklerDown", @"DIRIT FIRE\Phuongan1.png"),
            Tuple.Create("UI_FlexSprinkler", @"DIRIT FIRE\PhuongAnMem1.png"),
            Tuple.Create("UI_TwinSprinkler", @"DIRIT FIRE\Twin sprinkler_1.jpg"),
            Tuple.Create("FrmCreateFmlFromBasePoint", ""),
            Tuple.Create("FrmCreateFmlFromBlockCad", ""),
            Tuple.Create("FrmCreateBranchPipeFire", ""),
            Tuple.Create("VerticalMEPForm", ""),
            Tuple.Create("FrmConnectPipe", ""),
        };

        int failures = 0;
        foreach (var shot in shots)
        {
            string file = Path.Combine(outDir, shot.Item1 + (styled ? "" : "_old") + ".png");
            try
            {
                // Không dùng asm.GetTypes(): nhiều type tham chiếu Revit API, không nạp được ngoài Revit.
                Type t = new[] { "Service_E", "Service_J", "GeneralUI", "DiritFire" }
                    .Select(ns => asm.GetType("PlumbingSolution.FireProtection.UI." + ns + "." + shot.Item1))
                    .FirstOrDefault(x => x != null) ?? throw new TypeLoadException(shot.Item1);
                Form f = (Form)FormatterServices.GetUninitializedObject(t);
                typeof(Form).GetConstructor(Type.EmptyTypes).Invoke(f, null);
                t.GetMethod("InitializeComponent", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(f, null);
                MethodInfo buildType6 = t.GetMethod("BuildType6Panel", BindingFlags.Instance | BindingFlags.NonPublic);
                buildType6?.Invoke(f, null);

                // Các handler (Load, đổi Pipe Type...) gọi Revit API - gỡ hết trước khi đụng tới control.
                ClearEvents(f);
                foreach (Control c in All(f))
                    ClearEvents(c);

                MethodInfo lang = t.GetMethod("SettingLanguage", BindingFlags.Instance | BindingFlags.NonPublic);
                if (lang != null)
                {
                    try { lang.Invoke(f, null); } catch (Exception ex) { Console.WriteLine(shot.Item1 + " SettingLanguage: " + ex.InnerException?.Message); }
                }

                if (styled)
                    style.GetMethod("Apply").Invoke(null, new object[] { f });

                PictureBox pic = All(f).OfType<PictureBox>().FirstOrDefault();
                string img = Path.Combine(iconDir, "Preview", shot.Item2);
                if (pic != null && shot.Item2 != "" && File.Exists(img))
                    pic.Image = Image.FromFile(img);

                foreach (ComboBox cb in All(f).OfType<ComboBox>())
                {
                    if (cb.Items.Count == 0 && cb.DataSource == null)
                        cb.Items.Add(cb.Name.IndexOf("Size", StringComparison.OrdinalIgnoreCase) >= 0 ? "25 mm" : "Black Steel Pipe");
                    if (cb.SelectedIndex < 0 && cb.Items.Count > 0)
                        cb.SelectedIndex = 0;
                }

                f.StartPosition = FormStartPosition.Manual;
                f.Location = new Point(40, 40);
                f.ShowInTaskbar = false;
                f.Show();
                Application.DoEvents();

                using (var bmp = new Bitmap(f.Width, f.Height))
                {
                    f.DrawToBitmap(bmp, new Rectangle(Point.Empty, f.Size));
                    bmp.Save(file, ImageFormat.Png);
                }
                Console.WriteLine("saved " + file + " " + f.Size);

                TabControl tabs = All(f).OfType<TabControl>().FirstOrDefault();
                for (int i = 1; tabs != null && i < tabs.TabCount; i++)
                {
                    tabs.SelectedIndex = i;
                    Application.DoEvents();
                    using (var bmp = new Bitmap(f.Width, f.Height))
                    {
                        f.DrawToBitmap(bmp, new Rectangle(Point.Empty, f.Size));
                        bmp.Save(Path.ChangeExtension(file, null) + "_tab" + (i + 1) + ".png", ImageFormat.Png);
                    }
                }
                f.Close();
            }
            catch (Exception ex)
            {
                failures++;
                File.AppendAllText(Path.Combine(outDir, "errors.txt"), shot.Item1 + ": " + (ex.InnerException ?? ex) + Environment.NewLine);
                Console.WriteLine(shot.Item1 + " FAILED: " + (ex.InnerException ?? ex).Message);
            }
        }
        return failures;
    }

    private static void ClearEvents(Component c)
    {
        var events = (EventHandlerList)typeof(Component).GetProperty("Events", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(c, null);
        typeof(EventHandlerList).GetField("head", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(events, null);
    }

    private static IEnumerable<Control> All(Control root)
    {
        foreach (Control c in root.Controls)
        {
            yield return c;
            foreach (Control d in All(c))
                yield return d;
        }
    }
}

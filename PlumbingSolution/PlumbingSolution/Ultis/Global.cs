using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Windows.Forms;

namespace PlumbingSolution.Ultis
{
    /// <summary>
    /// Lưu ngữ cảnh Revit hiện tại cho các modeless form và ExternalEvent.
    /// </summary>
    internal static class Global
    {
        public static Autodesk.Revit.ApplicationServices.Application RVTApp { get; set; }
        public static UIApplication UIApp { get; set; }
        public static Autodesk.Revit.Creation.Application AppCreation { get; set; }
        public static UIDocument UIDoc { get; set; }
        public static Document Doc => UIDoc?.Document;

        public static void Update(UIApplication uiApplication)
        {
            UIApp = uiApplication;
            RVTApp = uiApplication?.Application;
            UIDoc = uiApplication?.ActiveUIDocument;
            AppCreation = RVTApp?.Create;
        }
    }

    /// <summary>
    /// Bọc handle cửa sổ Revit để dùng làm owner của WinForms.
    /// </summary>
    internal sealed class WindowHandle : IWin32Window
    {
        public WindowHandle(IntPtr handle)
        {
            Handle = handle;
        }

        public IntPtr Handle { get; }
    }
}

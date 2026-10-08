using Autodesk.Revit.UI;
using PlumbingSolution.Commands;

namespace PlumbingSolution
{
    // Panel chức năng của PlumbingSolution, tách khỏi App.cs giống AppAnnotationRibbon.cs bên SmartTag.
    // Bật / tắt theo license (RevitUtils) duyệt mọi panel của tab nên áp luôn cho panel mới thêm ở đây.
    public partial class App
    {
        private void CreatePlumbingPanel(UIControlledApplication app,
                                         string tabName,
                                         string assemblyPath,
                                         string iconFolder)
        {
            Autodesk.Revit.UI.RibbonPanel panel = app.CreateRibbonPanel(tabName, "Plumbing");

            // Nút mẫu để kiểm tra cổng license; thay bằng các lệnh Plumbing thật.
            PushButtonData sample = new PushButtonData("PlumbingSample", "Plumbing\nSample",
                assemblyPath, typeof(CmdPlumbingSample).FullName);
            sample.ToolTip = "Lệnh mẫu: chỉ chạy được khi đã có license PlumbingSolution.";
            AddImages(sample, iconFolder, "PlumbingSolution_32x32.png", "PlumbingSolution_16x16.png");
            panel.AddItem(sample);
        }
    }
}
